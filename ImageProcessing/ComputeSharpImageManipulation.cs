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

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ExtractRedShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public ExtractRedShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
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
            // Bgra32 maps to X=B, Y=G, Z=R, W=A
            Output[pos] = new float4(pixel.Z, pixel.Z, pixel.Z, 1.0f);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ExtractGreenShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public ExtractGreenShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
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
            Output[pos] = new float4(pixel.Y, pixel.Y, pixel.Y, 1.0f);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ExtractBlueShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public ExtractBlueShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
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
            Output[pos] = new float4(pixel.X, pixel.X, pixel.X, 1.0f);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct GrayscaleToAlphaShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public GrayscaleToAlphaShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
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
            // Since it's grayscale, we can use Z (R) for all channels including Alpha
            Output[pos] = new float4(pixel.Z, pixel.Z, pixel.Z, pixel.Z);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeGrayscalesToRGBAShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Red;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Green;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Blue;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Alpha;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public MergeGrayscalesToRGBAShader(ReadOnlyTexture2D<Bgra32, float4> red, ReadOnlyTexture2D<Bgra32, float4> green, ReadOnlyTexture2D<Bgra32, float4> blue, ReadOnlyTexture2D<Bgra32, float4> alpha, ReadWriteTexture2D<Bgra32, float4> output, int width) {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
            Output = output;
            Width = width;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            // Since input textures are grayscale, their R, G, and B are identical. 
            // Bgra32 maps to X=B, Y=G, Z=R, W=A. So we use Z (R channel) for consistency.
            float r = Red[pos].Z;
            float g = Green[pos].Z;
            float b = Blue[pos].Z;
            float a = Alpha[pos].Z;

            Output[pos] = new float4(b, g, r, a);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct SanitizeArtifactsShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;

        public SanitizeArtifactsShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width) {
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
            // If Alpha is less than 255 (0.999f to account for float inaccuracy), set to 0.
            float alpha = pixel.W < 0.999f ? 0.0f : pixel.W;
            Output[pos] = new float4(pixel.X, pixel.Y, pixel.Z, alpha);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct BoostAboveThresholdShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;
        public readonly float Threshold;

        public BoostAboveThresholdShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> output, int width, float threshold) {
            Source = source;
            Output = output;
            Width = width;
            Threshold = threshold;
        }

        private float FlattenToThreshold(float colourValue) {
            float nextPixel = (colourValue * (255.0f - Threshold)) + Threshold;
            if (nextPixel > 255.0f) {
                nextPixel = (nextPixel - 255.0f) + Threshold;
            }
            return nextPixel / 255.0f;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 pixel = Source[pos];
            Output[pos] = new float4(
                FlattenToThreshold(pixel.X),
                FlattenToThreshold(pixel.Y),
                FlattenToThreshold(pixel.Z),
                pixel.W);
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
        public static Bitmap ExtractRedGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new ExtractRedShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap ExtractGreenGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new ExtractGreenShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap ExtractBlueGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new ExtractBlueShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap GrayscaleToAlphaGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new GrayscaleToAlphaShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap MergeGrayscalesToRGBAGpu(Bitmap red, Bitmap green, Bitmap blue, Bitmap alpha) {
            var device = GraphicsDevice.GetDefault();
            int width = red.Width;
            int height = red.Height;
            int totalPixels = width * height;

            byte[] redPixels = BitmapToBytes(red);
            byte[] greenPixels = BitmapToBytes(green);
            byte[] bluePixels = BitmapToBytes(blue);
            byte[] alphaPixels = BitmapToBytes(alpha);

            using var gpuRed = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuGreen = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuBlue = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuAlpha = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            
            gpuRed.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(redPixels));
            gpuGreen.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(greenPixels));
            gpuBlue.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bluePixels));
            gpuAlpha.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(alphaPixels));

            device.For(totalPixels, new MergeGrayscalesToRGBAShader(gpuRed, gpuGreen, gpuBlue, gpuAlpha, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap SanitizeArtifactsGpu(Bitmap file) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new SanitizeArtifactsShader(gpuFile, gpuOutput, width));
            return DownloadResult(gpuOutput, width, height);
        }

        public static Bitmap BoostAboveThresholdGpu(Bitmap file, int threshold) {
            var device = GraphicsDevice.GetDefault();
            int width = file.Width;
            int height = file.Height;
            int totalPixels = width * height;

            byte[] filePixels = BitmapToBytes(file);

            using var gpuFile = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            gpuFile.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(filePixels));

            device.For(totalPixels, new BoostAboveThresholdShader(gpuFile, gpuOutput, width, (float)threshold));
            return DownloadResult(gpuOutput, width, height);
        }
    }
}
