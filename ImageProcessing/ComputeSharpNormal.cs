using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    /// <summary>
    /// Pass 1: Compute brightness (HSL lightness) for each pixel.
    /// Input: source texture (BGRA). Output: float buffer of brightness values.
    /// The source is expected to be horizontally flipped already (CPU side).
    /// </summary>
    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct BrightnessMapShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteBuffer<float> Brightness;
        public readonly int Width;

        public BrightnessMapShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteBuffer<float> brightness, int width) {
            Source = source;
            Brightness = brightness;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;

            float4 pixel = Source[new int2(x, y)];
            // pixel is float4 normalized [0,1] in BGRA order → .X=B, .Y=G, .Z=R, .W=A
            float r = pixel.Z;
            float g = pixel.Y;
            float b = pixel.X;

            float max = r;
            if (g > max) max = g;
            if (b > max) max = b;

            float min = r;
            if (g < min) min = g;
            if (b < min) min = b;

            Brightness[idx] = (max + min) * 0.5f;
        }
    }

    /// <summary>
    /// Pass 2: Compute Sobel gradient from brightness, apply contrast + overlay self-blend,
    /// and merge the original alpha — all in one pass.
    /// </summary>
    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct NormalMapShader : IComputeShader {
        public readonly ReadWriteBuffer<float> Brightness;
        public readonly ReadOnlyTexture2D<Bgra32, float4> OriginalSource;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;
        public readonly int Height;
        public readonly int HasMask;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Mask;
        public readonly float ContrastValue; // pre-computed: ((100+120)/100)^2 = 4.84

        public NormalMapShader(ReadWriteBuffer<float> brightness, ReadOnlyTexture2D<Bgra32, float4> originalSource,
            ReadWriteTexture2D<Bgra32, float4> output, int width, int height,
            int hasMask, ReadOnlyTexture2D<Bgra32, float4> mask, float contrastValue) {
            Brightness = brightness;
            OriginalSource = originalSource;
            Output = output;
            Width = width;
            Height = height;
            HasMask = hasMask;
            Mask = mask;
            ContrastValue = contrastValue;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            // Check mask: if mask present and alpha > 0, this pixel is NOT masked → use flat normal
            if (HasMask == 1) {
                float4 maskPixel = Mask[pos];
                // maskPixel.W = alpha, in flipped coordinates
                if (maskPixel.W > 0.0f) {
                    // Not masked → flat normal (128, 127, 255, 255) in BGRA
                    // B=255, G=127, R=128, A=255 → float4(1.0, 0.498, 0.502, 1.0)
                    Output[pos] = new float4(1.0f, 0.498f, 0.502f, 1.0f);
                    return;
                }
            }

            // Sobel gradient from brightness map
            float center = Brightness[idx];
            float l = x > 0 ? Brightness[y * Width + x - 1] : center;
            float r = x < Width - 1 ? Brightness[y * Width + x + 1] : center;
            float u = y > 0 ? Brightness[(y - 1) * Width + x] : center;
            float d = y < Height - 1 ? Brightness[(y + 1) * Width + x] : center;

            float xv = (((l - r) + 1.0f) * 0.5f); // normalized [0,1]
            float yv = (((u - d) + 1.0f) * 0.5f);

            // Normal map channels: R=xv, G=yv, B=1.0, A=1.0
            // But we work in BGRA float4: X=B, Y=G, Z=R, W=A
            float normalR = xv;
            float normalG = yv;
            float normalB = 1.0f;

            // Apply contrast (value=120 → factor = ((100+120)/100)^2 = 4.84)
            float contR = Hlsl.Clamp(((normalR - 0.5f) * ContrastValue + 0.5f), 0.0f, 1.0f);
            float contG = Hlsl.Clamp(((normalG - 0.5f) * ContrastValue + 0.5f), 0.0f, 1.0f);
            float contB = Hlsl.Clamp(((normalB - 0.5f) * ContrastValue + 0.5f), 0.0f, 1.0f);

            // Overlay blend: base=normal, overlay=contrasted
            // overlay(a,b) = a < 0.5 ? 2*a*b : 1 - 2*(1-a)*(1-b)
            float blendR = normalR < 0.5f
                ? 2.0f * normalR * contR
                : 1.0f - 2.0f * (1.0f - normalR) * (1.0f - contR);
            float blendG = normalG < 0.5f
                ? 2.0f * normalG * contG
                : 1.0f - 2.0f * (1.0f - normalG) * (1.0f - contG);
            float blendB = normalB < 0.5f
                ? 2.0f * normalB * contB
                : 1.0f - 2.0f * (1.0f - normalB) * (1.0f - contB);

            blendR = Hlsl.Clamp(blendR, 0.0f, 1.0f);
            blendG = Hlsl.Clamp(blendG, 0.0f, 1.0f);
            blendB = Hlsl.Clamp(blendB, 0.0f, 1.0f);

            // Get original alpha from the source (which is flipped, we need to un-flip the x coordinate)
            int flippedX = Width - 1 - x;
            float4 origPixel = OriginalSource[new int2(flippedX, y)];
            float alpha = origPixel.W;

            // Output in BGRA: X=B, Y=G, Z=R, W=A
            Output[pos] = new float4(blendB, blendG, blendR, alpha);
        }
    }

    /// <summary>
    /// GPU-accelerated Normal.Calculate replacement.
    /// Fuses: brightness map → Sobel gradient → contrast → overlay blend → alpha merge
    /// into two GPU dispatches instead of 6+ CPU bitmap allocations.
    /// </summary>
    public static class ComputeSharpNormal {
        public static Bitmap CalculateGpu(Bitmap file, Bitmap normalMask = null) {
            var device = GraphicsDevice.GetDefault();

            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            // We need to flip the image horizontally for processing (matching CPU code)
            // Instead of mutating the input, we flip in the shader
            // Upload the source WITHOUT flipping — the shader will handle it

            // Get source pixels
            byte[] srcPixels;
            using (var srcLock = new LockBitmap(file)) {
                srcLock.LockBits();
                srcPixels = new byte[srcLock.Pixels.Length];
                Array.Copy(srcLock.Pixels, srcPixels, srcPixels.Length);
            }

            // The CPU code flips horizontally first, THEN computes brightness/gradients, 
            // THEN flips back the result, THEN does contrast+overlay+alpha.
            // We can avoid the flip entirely by reading coordinates in reverse in the brightness pass.
            // But to keep exact parity, let's flip the pixel array in-place (very fast).
            FlipHorizontalBgra(srcPixels, width, height);

            // Upload flipped source
            var srcSpan = MemoryMarshal.Cast<byte, Bgra32>(srcPixels);
            using var gpuSource = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            gpuSource.CopyFrom(srcSpan);

            // Also upload the un-flipped original for alpha extraction
            byte[] origPixels;
            using (var origLock = new LockBitmap(file)) {
                origLock.LockBits();
                origPixels = new byte[origLock.Pixels.Length];
                Array.Copy(origLock.Pixels, origPixels, origPixels.Length);
            }
            var origSpan = MemoryMarshal.Cast<byte, Bgra32>(origPixels);
            using var gpuOriginal = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            gpuOriginal.CopyFrom(origSpan);

            // Allocate GPU buffers
            using var gpuBrightness = device.AllocateReadWriteBuffer<float>(totalPixels);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            // Handle mask
            ReadOnlyTexture2D<Bgra32, float4> gpuMask = null;
            int hasMask = 0;
            try {
                if (normalMask != null) {
                    hasMask = 1;
                    using var scaledMask = new Bitmap(normalMask, width, height);
                    // Flip mask horizontally too
                    byte[] maskPixels;
                    using (var maskLock = new LockBitmap(scaledMask)) {
                        maskLock.LockBits();
                        maskPixels = new byte[maskLock.Pixels.Length];
                        Array.Copy(maskLock.Pixels, maskPixels, maskPixels.Length);
                    }
                    FlipHorizontalBgra(maskPixels, width, height);

                    var maskSpan = MemoryMarshal.Cast<byte, Bgra32>(maskPixels);
                    gpuMask = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
                    gpuMask.CopyFrom(maskSpan);
                } else {
                    // ComputeSharp requires a valid texture, allocate a 1x1 dummy
                    gpuMask = device.AllocateReadOnlyTexture2D<Bgra32, float4>(1, 1);
                }

                // Pass 1: Brightness map
                device.For(totalPixels, new BrightnessMapShader(gpuSource, gpuBrightness, width));

                // Pass 2: Sobel + contrast + overlay + alpha merge — all in one
                float contrastFactor = (100.0f + 120.0f) / 100.0f;
                contrastFactor *= contrastFactor; // 4.84

                device.For(totalPixels, new NormalMapShader(
                    gpuBrightness, gpuOriginal, gpuOutput,
                    width, height, hasMask, gpuMask, contrastFactor));

                // Download result
                byte[] resultPixels = new byte[totalPixels * 4];
                var destSpan = MemoryMarshal.Cast<byte, Bgra32>(resultPixels);
                gpuOutput.CopyTo(destSpan);

                // Flip the result back horizontally (matching the CPU code's final flip)
                FlipHorizontalBgra(resultPixels, width, height);

                // Create output bitmap
                Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var bmpData = result.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                System.Runtime.InteropServices.Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
                result.UnlockBits(bmpData);
                return result;
            } finally {
                gpuMask?.Dispose();
            }
        }

        /// <summary>
        /// Flip a BGRA pixel array horizontally (in-place).
        /// </summary>
        private static void FlipHorizontalBgra(byte[] pixels, int width, int height) {
            int stride = width * 4;
            for (int y = 0; y < height; y++) {
                int rowStart = y * stride;
                for (int x = 0; x < width / 2; x++) {
                    int leftIdx = rowStart + x * 4;
                    int rightIdx = rowStart + (width - 1 - x) * 4;
                    // Swap 4 bytes (BGRA)
                    byte tmpB = pixels[leftIdx]; pixels[leftIdx] = pixels[rightIdx]; pixels[rightIdx] = tmpB;
                    byte tmpG = pixels[leftIdx + 1]; pixels[leftIdx + 1] = pixels[rightIdx + 1]; pixels[rightIdx + 1] = tmpG;
                    byte tmpR = pixels[leftIdx + 2]; pixels[leftIdx + 2] = pixels[rightIdx + 2]; pixels[rightIdx + 2] = tmpR;
                    byte tmpA = pixels[leftIdx + 3]; pixels[leftIdx + 3] = pixels[rightIdx + 3]; pixels[rightIdx + 3] = tmpA;
                }
            }
        }
    }
}
