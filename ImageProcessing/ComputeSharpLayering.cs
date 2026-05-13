using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct LayerImagesShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public LayerImagesShader(
            ReadOnlyTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];

            // Calculate scaled coordinates for the top layer
            // This mimics the CPU scaling: widthRatio = top.Width / top.Height
            // destWidthForTop = bottom.Height * widthRatio
            // We scale X based on that ratio.
            
            float widthRatio = (float)SrcWidth / (float)SrcHeight;
            int scaledTopWidth = (int)(DestHeight * widthRatio);
            
            float srcXf = (float)x / scaledTopWidth * SrcWidth;
            float srcYf = (float)y / DestHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            // Only sample if within the bounds of the drawn top layer
            if (x < scaledTopWidth && srcXf < SrcWidth && srcYf < SrcHeight) {
                // Nearest neighbor sampling to match standard DrawImage without explicit filters
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            // Alpha composite topLayer over bottomLayer's RGB (which is bottomPixel with Alpha=1.0)
            float topA = topPixel.W;
            
            float outR = topPixel.Z * topA + bottomPixel.Z * (1.0f - topA);
            float outG = topPixel.Y * topA + bottomPixel.Y * (1.0f - topA);
            float outB = topPixel.X * topA + bottomPixel.X * (1.0f - topA);
            
            // The alpha of the final image should remain the alpha of the bottom layer
            float outA = bottomPixel.W;

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MaxImagesShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MaxImagesShader(
            ReadOnlyTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;

            if (idx >= DestWidth * DestHeight) {
                return;
            }

            int x = idx % DestWidth;
            int y = idx / DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            
            float widthRatio = (float)SrcHeight / DestHeight;
            int scaledTopWidth = (int)(DestHeight * widthRatio);
            
            float srcXf = (float)x / scaledTopWidth * SrcWidth;
            float srcYf = (float)y / DestHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (x < scaledTopWidth && srcXf < SrcWidth && srcYf < SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            float outR = Hlsl.Max(topPixel.Z, bottomPixel.Z);
            float outG = Hlsl.Max(topPixel.Y, bottomPixel.Y);
            float outB = Hlsl.Max(topPixel.X, bottomPixel.X);
            
            float outA = Hlsl.Max(topPixel.W, bottomPixel.W);

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    public static class ComputeSharpLayering {
        public static Bitmap LayerImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(bottomLayer)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(topLayer)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            using var gpuBottom = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuBottom.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bottomPixels));
            gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(topPixels));

            device.For(totalPixels, new LayerImagesShader(gpuBottom, gpuTop, gpuOutput, width, height, topLayer.Width, topLayer.Height));

            byte[] resultPixels = new byte[totalPixels * 4];
            gpuOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
            result.UnlockBits(bmpData);

            return result;
        }

        public static Bitmap MaxImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(bottomLayer)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(topLayer)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            using var gpuBottom = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuBottom.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bottomPixels));
            gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(topPixels));

            device.For(totalPixels, new MaxImagesShader(gpuBottom, gpuTop, gpuOutput, width, height, topLayer.Width, topLayer.Height));

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

