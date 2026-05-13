using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    /// <summary>
    /// GPU replacement for ImageManipulation.SeperateByDifference
    /// Fuses: Difference Blend + Grayscale + BoostAlpha + MergeAlphaToRGB into a single dispatch!
    /// </summary>
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct SeperateByDifferenceShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Tattoo;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Underlay;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public SeperateByDifferenceShader(ReadOnlyTexture2D<Bgra32, float4> tattoo, ReadOnlyTexture2D<Bgra32, float4> underlay, ReadWriteTexture2D<Bgra32, float4> output, int width) {
            Tattoo = tattoo;
            Underlay = underlay;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 tattooPixel = Tattoo[pos];
            float4 underlayPixel = Underlay[pos];

            // 1. Difference Blend (RGB only, typical ImageBlender behavior ignores alpha or assumes 1.0)
            float diffR = Hlsl.Abs(underlayPixel.Z - tattooPixel.Z);
            float diffG = Hlsl.Abs(underlayPixel.Y - tattooPixel.Y);
            float diffB = Hlsl.Abs(underlayPixel.X - tattooPixel.X);

            // 2. Grayscale MakeGrayscale (0.299 R + 0.587 G + 0.114 B)
            float gray = (0.299f * diffR) + (0.587f * diffG) + (0.114f * diffB);

            // 3. BoostAlpha (if R > 5/255 -> 255)
            float threshold = 5.0f / 255.0f;
            float finalAlpha;
            if (gray > threshold) {
                finalAlpha = 1.0f;
            } else {
                finalAlpha = gray; // Original BoostAlpha leaves it alone if <= 5
            }

            // 4. MergeAlphaToRGB (Output RGB = Tattoo RGB, Alpha = new alpha)
            Output[pos] = new float4(tattooPixel.X, tattooPixel.Y, tattooPixel.Z, finalAlpha);
        }
    }

    public static class ComputeSharpUnderlay {
        public static Bitmap SeperateByDifferenceGpu(Bitmap tattoo, Bitmap underlay) {
            var device = GraphicsDevice.GetDefault();
            int width = tattoo.Width;
            int height = tattoo.Height;
            int totalPixels = width * height;

            byte[] tattooPixels;
            using (var lockTattoo = new LockBitmap(tattoo)) {
                lockTattoo.LockBits();
                tattooPixels = new byte[lockTattoo.Pixels.Length];
                Array.Copy(lockTattoo.Pixels, tattooPixels, tattooPixels.Length);
            }

            // Ensure underlay is resized to match tattoo before upload (if needed)
            byte[] underlayPixels;
            if (underlay.Width != width || underlay.Height != height) {
                using (Bitmap resizedUnderlay = ImageManipulation.Resize(underlay, width, height)) {
                    using (var lockUnderlay = new LockBitmap(resizedUnderlay)) {
                        lockUnderlay.LockBits();
                        underlayPixels = new byte[lockUnderlay.Pixels.Length];
                        Array.Copy(lockUnderlay.Pixels, underlayPixels, underlayPixels.Length);
                    }
                }
            } else {
                using (var lockUnderlay = new LockBitmap(underlay)) {
                    lockUnderlay.LockBits();
                    underlayPixels = new byte[lockUnderlay.Pixels.Length];
                    Array.Copy(lockUnderlay.Pixels, underlayPixels, underlayPixels.Length);
                }
            }

            using var gpuTattoo = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuUnderlay = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuTattoo.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(tattooPixels));
            gpuUnderlay.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(underlayPixels));

            device.For(totalPixels, new SeperateByDifferenceShader(gpuTattoo, gpuUnderlay, gpuOutput, width));

            byte[] resultPixels = new byte[totalPixels * 4];
            gpuOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
            result.UnlockBits(bmpData);

            return result;
        }
    }
}
