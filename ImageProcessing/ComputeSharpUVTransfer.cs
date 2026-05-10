using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ApplyTransferMapArrayShader : IComputeShader
    {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadOnlyBuffer<float> MapX;
        public readonly ReadOnlyBuffer<float> MapY;
        public readonly ReadOnlyBuffer<int> MapValid;
        public readonly ReadWriteTexture2D<Bgra32, float4> Destination;
        public readonly int DestWidth;
        public readonly int SourceWidth;
        public readonly int SourceHeight;
        public readonly int UseBilinear;

        public ApplyTransferMapArrayShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadOnlyBuffer<float> mapX, ReadOnlyBuffer<float> mapY, ReadOnlyBuffer<int> mapValid, ReadWriteTexture2D<Bgra32, float4> destination, int destWidth, int sourceWidth, int sourceHeight, int useBilinear)
        {
            Source = source;
            MapX = mapX;
            MapY = mapY;
            MapValid = mapValid;
            Destination = destination;
            DestWidth = destWidth;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
            UseBilinear = useBilinear;
        }

        public void Execute()
        {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * Destination.Height) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;

            if (MapValid[idx] == 0) return;

            float srcXf = MapX[idx] * (SourceWidth - 1);
            float srcYf = MapY[idx] * (SourceHeight - 1);

            int2 pos = new int2(x, y);

            if (UseBilinear == 1) {
                int x1 = (int)Hlsl.Floor(srcXf);
                int y1 = (int)Hlsl.Floor(srcYf);
                x1 = Hlsl.Clamp(x1, 0, SourceWidth - 1);
                y1 = Hlsl.Clamp(y1, 0, SourceHeight - 1);
                int x2 = Hlsl.Min(x1 + 1, SourceWidth - 1);
                int y2 = Hlsl.Min(y1 + 1, SourceHeight - 1);

                float xDiff = srcXf - x1;
                float yDiff = srcYf - y1;
                float w11 = (1f - xDiff) * (1f - yDiff);
                float w21 = xDiff * (1f - yDiff);
                float w12 = (1f - xDiff) * yDiff;
                float w22 = xDiff * yDiff;

                float4 color11 = Source[new int2(x1, y1)];
                float4 color21 = Source[new int2(x2, y1)];
                float4 color12 = Source[new int2(x1, y2)];
                float4 color22 = Source[new int2(x2, y2)];

                Destination[pos] = color11 * w11 + color21 * w21 + color12 * w12 + color22 * w22;
            } else {
                int srcXi = (int)Hlsl.Round(srcXf);
                int srcYi = (int)Hlsl.Round(srcYf);
                srcXi = Hlsl.Clamp(srcXi, 0, SourceWidth - 1);
                srcYi = Hlsl.Clamp(srcYi, 0, SourceHeight - 1);
                Destination[pos] = Source[new int2(srcXi, srcYi)];
            }
        }
    }

    public static class ComputeSharpUVTransfer {
        public static Bitmap ApplyTransferMap(Bitmap sourceTexture, float[] mapX, float[] mapY, bool[] mapValid, int destWidth, int destHeight, bool useBilinear) {
            Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            
            LockBitmap srcLock = new LockBitmap(sourceTexture);
            LockBitmap destLock = new LockBitmap(result);
            srcLock.LockBits();
            destLock.LockBits();

            try {
                using ReadOnlyTexture2D<Bgra32, float4> gpuSource = GraphicsDevice.GetDefault().AllocateReadOnlyTexture2D<Bgra32, float4>(sourceTexture.Width, sourceTexture.Height);
                using ReadWriteTexture2D<Bgra32, float4> gpuDest = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                using ReadOnlyBuffer<float> gpuMapX = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<float>(mapX);
                using ReadOnlyBuffer<float> gpuMapY = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<float>(mapY);
                
                int[] mapValidInt = new int[mapValid.Length];
                for (int i = 0; i < mapValid.Length; i++) mapValidInt[i] = mapValid[i] ? 1 : 0;
                using ReadOnlyBuffer<int> gpuMapValid = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<int>(mapValidInt);

                ReadOnlySpan<Bgra32> srcSpan = MemoryMarshal.Cast<byte, Bgra32>(new ReadOnlySpan<byte>(srcLock.Pixels));
                gpuSource.CopyFrom(srcSpan);

                GraphicsDevice.GetDefault().For(destWidth * destHeight, new ApplyTransferMapArrayShader(
                    gpuSource,
                    gpuMapX,
                    gpuMapY,
                    gpuMapValid,
                    gpuDest,
                    destWidth,
                    sourceTexture.Width,
                    sourceTexture.Height,
                    useBilinear ? 1 : 0
                ));

                Span<Bgra32> destSpan = MemoryMarshal.Cast<byte, Bgra32>(new Span<byte>(destLock.Pixels));
                gpuDest.CopyTo(destSpan);
            }
            finally {
                srcLock.UnlockBits();
                destLock.UnlockBits();
            }

            return result;
        }
    }
}
