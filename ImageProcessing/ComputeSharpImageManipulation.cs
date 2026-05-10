using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ExtractRGBShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public ExtractRGBShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
            Source = source;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 pixel = Source[pos];
            // Set Alpha to 1.0
            Output[pos] = new float4(pixel.X, pixel.Y, pixel.Z, 1.0f);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ExtractAlphaShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public ExtractAlphaShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
            Source = source;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 pixel = Source[pos];
            // Grayscale of alpha: R=A, G=A, B=A, Alpha=1.0
            Output[pos] = new float4(pixel.W, pixel.W, pixel.W, 1.0f);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeAlphaToRGBShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Alpha;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Rgb;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public MergeAlphaToRGBShader(ReadOnlyTexture2D<Bgra32, float4> alpha, ReadOnlyTexture2D<Bgra32, float4> rgb, ReadWriteTexture2D<Bgra32, float4> output, int width) {
            Alpha = alpha;
            Rgb = rgb;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 rgbPixel = Rgb[pos];
            float4 alphaPixel = Alpha[pos];
            // Use alphaPixel's R (Z in BGRA) as the new alpha value
            Output[pos] = new float4(rgbPixel.X, rgbPixel.Y, rgbPixel.Z, alphaPixel.Z);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct InvertImageShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public InvertImageShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
            Source = source;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 pixel = Source[pos];
            // Invert RGB, keep Alpha
            Output[pos] = new float4(1.0f - pixel.X, 1.0f - pixel.Y, 1.0f - pixel.Z, pixel.W);
        }
    }

    public static class ComputeSharpImageManipulation {

        private static byte[] BitmapToBytes(Bitmap file) {
            using (var fileLock = new LockBitmap(file)) {
                fileLock.LockBits();
                byte[] pixels = new byte[fileLock.Pixels.Length];
                Array.Copy(fileLock.Pixels, pixels, pixels.Length);
                return pixels;
            }
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

        public static Bitmap ExtractRGBGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new ExtractRGBShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap ExtractAlphaGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new ExtractAlphaShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap MergeAlphaToRGBGpu(Bitmap alpha, Bitmap rgb) {
            var device = GraphicsDevice.GetDefault();
            // Assuming both are same size as per original code constraints
            int width = rgb.Width;
            int height = rgb.Height;
            int totalPixels = width * height;

            byte[] alphaPixels = BitmapToBytes(alpha);
            byte[] rgbPixels = BitmapToBytes(rgb);

            using var gpuAlpha = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuRgb = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            
            gpuAlpha.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(alphaPixels));
            gpuRgb.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(rgbPixels));

            device.For(totalPixels, new MergeAlphaToRGBShader(gpuAlpha, gpuRgb, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap InvertImageGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new InvertImageShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }
    }
}
