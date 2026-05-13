using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    /// <summary>
    /// GPU CalculateBase: Combines file + glow via multiply blend, then composites
    /// based on alpha thresholds. Replaces the CPU path that used the single-threaded
    /// KVImage.ImageBlender.BlendImages + Parallel.For pixel loop.
    /// </summary>
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct CalculateBaseShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> File;       // base texture
        public readonly ReadOnlyTexture2D<Bgra32, float4> Glow;       // glow/mask overlay
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public CalculateBaseShader(ReadOnlyTexture2D<Bgra32, float4> file,
            ReadOnlyTexture2D<Bgra32, float4> glow,
            ReadWriteTexture2D<Bgra32, float4> output, int width) {
            File = file;
            Glow = glow;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 filePixel = File[pos];    // BGRA: X=B, Y=G, Z=R, W=A
            float4 glowPixel = Glow[pos];

            // Multiply blend: merged = file * glow (per channel, treating glow as white where no data)
            // The CPU code fills a white background then draws glow on it, then multiplies with file
            float mergedB = filePixel.X * glowPixel.X;
            float mergedG = filePixel.Y * glowPixel.Y;
            float mergedR = filePixel.Z * glowPixel.Z;
            float mergedA = filePixel.W;

            // Source pixel = resized glow
            float srcB = glowPixel.X;
            float srcG = glowPixel.Y;
            float srcR = glowPixel.Z;
            float srcA = glowPixel.W;

            // Threshold check: flatten colours > 90/255 ≈ 0.353
            float compR = srcR > 0.353f ? srcR : 0.0f;
            float compG = srcG > 0.353f ? srcG : 0.0f;
            float compB = srcB > 0.353f ? srcB : 0.0f;

            float outB, outG, outR, outA;

            if (!(compR == 0.0f && compG == 0.0f && compB == 0.0f)) {
                if (srcA > 0.078f) {         // > 20/255
                    outB = srcB;
                    outG = srcG;
                    outR = srcR;
                    outA = 1.0f - srcA;
                } else if (srcA > 0.039f) {  // > 10/255
                    outB = mergedB;
                    outG = mergedG;
                    outR = mergedR;
                    outA = 1.0f - srcA;
                } else if (srcA > 0.0f) {
                    outB = mergedB;
                    outG = mergedG;
                    outR = mergedR;
                    outA = mergedA;
                } else {
                    outB = mergedB;
                    outG = mergedG;
                    outR = mergedR;
                    outA = mergedA;
                }
            } else {
                outB = mergedB;
                outG = mergedG;
                outR = mergedR;
                outA = mergedA;
            }

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    /// <summary>
    /// GPU CalculateMulti: If glow alpha > 20/255, write glow alpha into destination's blue channel.
    /// </summary>
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct CalculateMultiShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> File;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Glow;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public CalculateMultiShader(ReadOnlyTexture2D<Bgra32, float4> file,
            ReadOnlyTexture2D<Bgra32, float4> glow,
            ReadWriteTexture2D<Bgra32, float4> output, int width) {
            File = file;
            Glow = glow;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 filePixel = File[pos];
            float4 glowPixel = Glow[pos];

            // If source alpha > 20/255 ≈ 0.078, replace dest blue channel with source alpha
            // CPU code: Color.FromArgb(dest.A, dest.R, dest.G, sourcePixel.A)
            // In BGRA float4: X=B, Y=G, Z=R, W=A
            // "B" in the Color is the LAST arg = sourcePixel.A, which maps to X in BGRA
            if (glowPixel.W > 0.078f) {
                Output[pos] = new float4(glowPixel.W, filePixel.Y, filePixel.Z, filePixel.W);
            } else {
                Output[pos] = filePixel;
            }
        }
    }

    /// <summary>
    /// GPU CalculateEyeMulti: If glow alpha > 0, write (255-alpha) into destination's alpha channel.
    /// </summary>
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct CalculateEyeMultiShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> File;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Glow;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public CalculateEyeMultiShader(ReadOnlyTexture2D<Bgra32, float4> file,
            ReadOnlyTexture2D<Bgra32, float4> glow,
            ReadWriteTexture2D<Bgra32, float4> output, int width) {
            File = file;
            Glow = glow;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 filePixel = File[pos];
            float4 glowPixel = Glow[pos];

            // CPU: Color.FromArgb(255 - sourcePixel.A, dest.R, dest.G, dest.B)
            // In BGRA float4: W=A → set W to 1.0 - glowAlpha
            if (glowPixel.W > 0.0f) {
                Output[pos] = new float4(filePixel.X, filePixel.Y, filePixel.Z, 1.0f - glowPixel.W);
            } else {
                Output[pos] = filePixel;
            }
        }
    }

    /// <summary>
    /// GPU TransplantData: If glow pixel alpha > 0, copy it over the base.
    /// </summary>
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct TransplantDataShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> File;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Glow;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public TransplantDataShader(ReadOnlyTexture2D<Bgra32, float4> file,
            ReadOnlyTexture2D<Bgra32, float4> glow,
            ReadWriteTexture2D<Bgra32, float4> output, int width) {
            File = file;
            Glow = glow;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 filePixel = File[pos];
            float4 glowPixel = Glow[pos];

            if (glowPixel.W > 0.0f) {
                Output[pos] = glowPixel;
            } else {
                Output[pos] = filePixel;
            }
        }
    }

    /// <summary>
    /// Helper class to run MapWriting operations on the GPU.
    /// </summary>
    public static class ComputeSharpMapWriting {

        private static (byte[] filePixels, byte[] glowPixels, int width, int height) PrepareInputs(Bitmap file, Bitmap glow) {
            int width = file.Width;
            int height = file.Height;

            byte[] filePixels;
            using (var fileLock = new LockBitmap(file)) {
                fileLock.LockBits();
                filePixels = new byte[fileLock.Pixels.Length];
                Array.Copy(fileLock.Pixels, filePixels, filePixels.Length);
            }

            byte[] glowPixels;
            using (var resizedGlow = new Bitmap(glow, width, height)) {
                using (var glowLock = new LockBitmap(resizedGlow)) {
                    glowLock.LockBits();
                    glowPixels = new byte[glowLock.Pixels.Length];
                    Array.Copy(glowLock.Pixels, glowPixels, glowPixels.Length);
                }
            }

            return (filePixels, glowPixels, width, height);
        }

        private static Bitmap DownloadResult(ReadWriteTexture2D<Bgra32, float4> gpuOutput, int width, int height) {
            int totalPixels = width * height;
            byte[] resultPixels = new byte[totalPixels * 4];
            gpuOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
            result.UnlockBits(bmpData);
            return result;
        }

        public static Bitmap CalculateBaseGpu(Bitmap file, Bitmap glow) {
            var (filePixels, glowPixels, width, height) = PrepareInputs(file, glow);
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuGlow = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));
            gpuGlow.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(glowPixels));

            device.For(totalPixels, new CalculateBaseShader(gpuFile, gpuGlow, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap CalculateMultiGpu(Bitmap file, Bitmap glow) {
            var (filePixels, glowPixels, width, height) = PrepareInputs(file, glow);
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuGlow = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));
            gpuGlow.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(glowPixels));

            device.For(totalPixels, new CalculateMultiShader(gpuFile, gpuGlow, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap CalculateEyeMultiGpu(Bitmap file, Bitmap glow) {
            var (filePixels, glowPixels, width, height) = PrepareInputs(file, glow);
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuGlow = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));
            gpuGlow.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(glowPixels));

            device.For(totalPixels, new CalculateEyeMultiShader(gpuFile, gpuGlow, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap TransplantDataGpu(Bitmap file, Bitmap glow) {
            var (filePixels, glowPixels, width, height) = PrepareInputs(file, glow);
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuGlow = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));
            gpuGlow.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(glowPixels));

            device.For(totalPixels, new TransplantDataShader(gpuFile, gpuGlow, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }
    }
}

