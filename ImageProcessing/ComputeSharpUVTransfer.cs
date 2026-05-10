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

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ApplyTransferMapTextureShader : IComputeShader
    {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadOnlyTexture2D<Rgba64, float4> Map;
        public readonly ReadWriteTexture2D<Bgra32, float4> Destination;
        public readonly int DestWidth;
        public readonly int SourceWidth;
        public readonly int SourceHeight;
        public readonly int UseBilinear;

        public ApplyTransferMapTextureShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadOnlyTexture2D<Rgba64, float4> map, ReadWriteTexture2D<Bgra32, float4> destination, int destWidth, int sourceWidth, int sourceHeight, int useBilinear)
        {
            Source = source;
            Map = map;
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

            int2 pos = new int2(x, y);
            float4 mapPixel = Map[pos];

            if (mapPixel.W < 0.999f) return;

            float srcXf = mapPixel.X * (SourceWidth - 1);
            float srcYf = mapPixel.Y * (SourceHeight - 1);

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

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ApplyTransferMapCombinedShader : IComputeShader
    {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadOnlyTexture2D<Rgba64, float4> Map;
        public readonly ReadWriteTexture2D<Bgra32, float4> DestinationRgb;
        public readonly ReadWriteTexture2D<Bgra32, float4> DestinationAlpha;
        public readonly int DestWidth;
        public readonly int SourceWidth;
        public readonly int SourceHeight;
        public readonly int UseBilinear;

        public ApplyTransferMapCombinedShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadOnlyTexture2D<Rgba64, float4> map, ReadWriteTexture2D<Bgra32, float4> destRgb, ReadWriteTexture2D<Bgra32, float4> destAlpha, int destWidth, int sourceWidth, int sourceHeight, int useBilinear)
        {
            Source = source;
            Map = map;
            DestinationRgb = destRgb;
            DestinationAlpha = destAlpha;
            DestWidth = destWidth;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
            UseBilinear = useBilinear;
        }

        public void Execute()
        {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestinationRgb.Height) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;

            int2 pos = new int2(x, y);
            float4 mapPixel = Map[pos];

            if (mapPixel.W < 0.999f) return;

            float srcXf = mapPixel.X * (SourceWidth - 1);
            float srcYf = mapPixel.Y * (SourceHeight - 1);

            float4 resultColor;

            if (UseBilinear == 1) {
                int x1 = (int)Hlsl.Floor(srcXf);
                int y1 = (int)Hlsl.Floor(srcYf);
                x1 = Hlsl.Clamp(x1, 0, SourceWidth - 1);
                y1 = Hlsl.Clamp(y1, 0, SourceHeight - 1);
                int x2 = Hlsl.Min(x1 + 1, SourceWidth - 1);
                int y2 = Hlsl.Min(y1 + 1, SourceHeight - 1);

                float dx = srcXf - x1;
                float dy = srcYf - y1;

                float4 color11 = Source[new int2(x1, y1)];
                float4 color21 = Source[new int2(x2, y1)];
                float4 color12 = Source[new int2(x1, y2)];
                float4 color22 = Source[new int2(x2, y2)];

                float w11 = (1.0f - dx) * (1.0f - dy);
                float w21 = dx * (1.0f - dy);
                float w12 = (1.0f - dx) * dy;
                float w22 = dx * dy;

                resultColor = color11 * w11 + color21 * w21 + color12 * w12 + color22 * w22;
            } else {
                int srcXi = (int)Hlsl.Round(srcXf);
                int srcYi = (int)Hlsl.Round(srcYf);
                srcXi = Hlsl.Clamp(srcXi, 0, SourceWidth - 1);
                srcYi = Hlsl.Clamp(srcYi, 0, SourceHeight - 1);
                resultColor = Source[new int2(srcXi, srcYi)];
            }

            DestinationRgb[pos] = new float4(resultColor.X, resultColor.Y, resultColor.Z, 1.0f);
            DestinationAlpha[pos] = new float4(resultColor.W, resultColor.W, resultColor.W, 1.0f);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeRgbAndAlphaShader : IComputeShader
    {
        public readonly ReadWriteTexture2D<Bgra32, float4> SourceRgb;
        public readonly ReadWriteTexture2D<Bgra32, float4> SourceAlpha;
        public readonly ReadWriteTexture2D<Bgra32, float4> Destination;
        public readonly int Width;

        public MergeRgbAndAlphaShader(ReadWriteTexture2D<Bgra32, float4> sourceRgb, ReadWriteTexture2D<Bgra32, float4> sourceAlpha, ReadWriteTexture2D<Bgra32, float4> destination, int width)
        {
            SourceRgb = sourceRgb;
            SourceAlpha = sourceAlpha;
            Destination = destination;
            Width = width;
        }

        public void Execute()
        {
            int idx = ThreadIds.X;
            if (idx >= Width * Destination.Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 rgb = SourceRgb[pos];
            float4 alpha = SourceAlpha[pos];

            Destination[pos] = new float4(rgb.X, rgb.Y, rgb.Z, alpha.X);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct DilateEdgesShader : IComputeShader
    {
        public readonly ReadWriteTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Destination;
        public readonly int Width;
        public readonly int Height;

        public DilateEdgesShader(ReadWriteTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> destination, int width, int height)
        {
            Source = source;
            Destination = destination;
            Width = width;
            Height = height;
        }

        public void Execute()
        {
            int idx = ThreadIds.X;
            if (idx >= Width * Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 center = Source[pos];
            if (center.W > 0.0f) {
                Destination[pos] = center;
                return;
            }

            float4 sum = float4.Zero;
            int count = 0;

            for (int dy = -1; dy <= 1; dy++) {
                for (int dx = -1; dx <= 1; dx++) {
                    if (dx == 0 && dy == 0) continue;
                    int2 nPos = new int2(pos.X + dx, pos.Y + dy);
                    
                    if (nPos.X >= 0 && nPos.X < Source.Width && nPos.Y >= 0 && nPos.Y < Source.Height) {
                        float4 neighbor = Source[nPos];
                        if (neighbor.W > 0.0f) {
                            sum += neighbor;
                            count++;
                        }
                    }
                }
            }

            if (count > 0) {
                float4 result = sum / count;
                result.W = 1.0f;
                Destination[pos] = result;
            } else {
                Destination[pos] = float4.Zero;
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

        private static System.Collections.Concurrent.ConcurrentDictionary<string, (int Width, int Height, ComputeSharp.Rgba64[] Data)> _gpuMapCache = new System.Collections.Concurrent.ConcurrentDictionary<string, (int, int, ComputeSharp.Rgba64[])>();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, ReadOnlyTexture2D<ComputeSharp.Rgba64, float4>> _gpuResidentMapCache = new System.Collections.Concurrent.ConcurrentDictionary<string, ReadOnlyTexture2D<ComputeSharp.Rgba64, float4>>();

        // Pool intermediate buffers per resolution to avoid 64MB VRAM allocations on every call
        [ThreadStatic]
        private static System.Collections.Generic.Dictionary<(int, int), (ReadWriteTexture2D<Bgra32, float4> DestRgb, ReadWriteTexture2D<Bgra32, float4> DestAlpha, ReadWriteTexture2D<Bgra32, float4> PingRgb, ReadWriteTexture2D<Bgra32, float4> PingAlpha)> _gpuBufferPool;

        private static (ReadWriteTexture2D<Bgra32, float4> DestRgb, ReadWriteTexture2D<Bgra32, float4> DestAlpha, ReadWriteTexture2D<Bgra32, float4> PingRgb, ReadWriteTexture2D<Bgra32, float4> PingAlpha) GetPooledBuffers(int width, int height) {
            if (_gpuBufferPool == null) _gpuBufferPool = new System.Collections.Generic.Dictionary<(int, int), (ReadWriteTexture2D<Bgra32, float4>, ReadWriteTexture2D<Bgra32, float4>, ReadWriteTexture2D<Bgra32, float4>, ReadWriteTexture2D<Bgra32, float4>)>();
            
            var key = (width, height);
            if (!_gpuBufferPool.TryGetValue(key, out var buffers)) {
                var device = GraphicsDevice.GetDefault();
                buffers = (
                    device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height),
                    device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height),
                    device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height),
                    device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height)
                );
                _gpuBufferPool[key] = buffers;
            }
            return buffers;
        }

        public static void ClearCache() {
            _gpuMapCache.Clear();
            foreach (var kvp in _gpuResidentMapCache) {
                try { kvp.Value.Dispose(); } catch { }
            }
            _gpuResidentMapCache.Clear();
            // Note: ThreadStatic pool cannot be cleared globally from here, but it's bound to thread lifetime
        }

        public static Bitmap ApplyTransferMapFast(Bitmap sourceTexture, string transferMapPath, bool useBilinear) {
            if (!_gpuMapCache.TryGetValue(transferMapPath, out var cachedMap)) {
                using (var transferMapImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba64>(transferMapPath)) {
                    int width = transferMapImage.Width;
                    int height = transferMapImage.Height;
                    ComputeSharp.Rgba64[] mapArray = new ComputeSharp.Rgba64[width * height];
                    
                    transferMapImage.ProcessPixelRows(accessor => {
                        for (int y = 0; y < accessor.Height; y++) {
                            var rowSpan = accessor.GetRowSpan(y);
                            var targetSpan = new Span<ComputeSharp.Rgba64>(mapArray, y * width, width);
                            MemoryMarshal.Cast<SixLabors.ImageSharp.PixelFormats.Rgba64, ComputeSharp.Rgba64>(rowSpan).CopyTo(targetSpan);
                        }
                    });
                    
                    cachedMap = (width, height, mapArray);
                    _gpuMapCache[transferMapPath] = cachedMap;
                }
            }

            var device = GraphicsDevice.GetDefault();

            int destWidth = cachedMap.Width;
            int destHeight = cachedMap.Height;
            int totalPixels = destWidth * destHeight;
            Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            
            LockBitmap srcLock = new LockBitmap(sourceTexture);
            LockBitmap destLock = new LockBitmap(result);
            srcLock.LockBits();
            destLock.LockBits();

            try {
                // Cache the transfer map on the GPU so repeat calls skip the CPU→GPU upload
                if (!_gpuResidentMapCache.TryGetValue(transferMapPath, out var gpuMap)) {
                    gpuMap = device.AllocateReadOnlyTexture2D<ComputeSharp.Rgba64, float4>(destWidth, destHeight);
                    gpuMap.CopyFrom(cachedMap.Data);
                    _gpuResidentMapCache[transferMapPath] = gpuMap;
                }

                using ReadOnlyTexture2D<Bgra32, float4> gpuSource = device.AllocateReadOnlyTexture2D<Bgra32, float4>(sourceTexture.Width, sourceTexture.Height);
                var pooled = GetPooledBuffers(destWidth, destHeight);
                var gpuDestRgb = pooled.DestRgb;
                var gpuDestAlpha = pooled.DestAlpha;
                var gpuPingRgb = pooled.PingRgb;
                var gpuPingAlpha = pooled.PingAlpha;
                
                ReadOnlySpan<Bgra32> srcSpan = MemoryMarshal.Cast<byte, Bgra32>(new ReadOnlySpan<byte>(srcLock.Pixels));
                gpuSource.CopyFrom(srcSpan);

                device.For(totalPixels, new ApplyTransferMapCombinedShader(
                    gpuSource,
                    gpuMap,
                    gpuDestRgb,
                    gpuDestAlpha,
                    destWidth,
                    sourceTexture.Width,
                    sourceTexture.Height,
                    useBilinear ? 1 : 0
                ));

                for (int i = 0; i < UVTransferMap.EdgePadding; i++) {
                    if (i % 2 == 0) {
                        device.For(totalPixels, new DilateEdgesShader(gpuDestRgb, gpuPingRgb, destWidth, destHeight));
                        device.For(totalPixels, new DilateEdgesShader(gpuDestAlpha, gpuPingAlpha, destWidth, destHeight));
                    } else {
                        device.For(totalPixels, new DilateEdgesShader(gpuPingRgb, gpuDestRgb, destWidth, destHeight));
                        device.For(totalPixels, new DilateEdgesShader(gpuPingAlpha, gpuDestAlpha, destWidth, destHeight));
                    }
                }

                // Merge back into gpuDestRgb (reuse — no extra allocation)
                device.For(totalPixels, new MergeRgbAndAlphaShader(gpuDestRgb, gpuDestAlpha, gpuPingRgb, destWidth));

                Span<Bgra32> destSpan = MemoryMarshal.Cast<byte, Bgra32>(new Span<byte>(destLock.Pixels));
                gpuPingRgb.CopyTo(destSpan);
            }
            finally {
                srcLock.UnlockBits();
                destLock.UnlockBits();
            }

            return result;
        }

        /// <summary>
        /// Fully Bitmap-free file-to-file transfer. Loads source with ImageSharp, processes on GPU,
        /// saves result with ImageSharp. Zero System.Drawing.Bitmap overhead.
        /// Returns true on success, false if the input format isn't supported (caller should fall back).
        /// </summary>
        public static bool TransferFile(string inputPath, string outputPath, string transferMapPath, bool useBilinear = true) {
            string ext = System.IO.Path.GetExtension(inputPath).ToLowerInvariant();
            // Only handle formats ImageSharp can load natively
            if (ext == ".tex" || ext == ".dds" || ext == ".ltct") return false;

            var totalSw = System.Diagnostics.Stopwatch.StartNew();
            var phaseSw = System.Diagnostics.Stopwatch.StartNew();

            // Load and cache transfer map data (same as ApplyTransferMapFast)
            if (!_gpuMapCache.TryGetValue(transferMapPath, out var cachedMap)) {
                using (var transferMapImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba64>(transferMapPath)) {
                    int w = transferMapImage.Width;
                    int h = transferMapImage.Height;
                    ComputeSharp.Rgba64[] mapArray = new ComputeSharp.Rgba64[w * h];
                    
                    transferMapImage.ProcessPixelRows(accessor => {
                        for (int y = 0; y < accessor.Height; y++) {
                            var rowSpan = accessor.GetRowSpan(y);
                            var targetSpan = new Span<ComputeSharp.Rgba64>(mapArray, y * w, w);
                            MemoryMarshal.Cast<SixLabors.ImageSharp.PixelFormats.Rgba64, ComputeSharp.Rgba64>(rowSpan).CopyTo(targetSpan);
                        }
                    });
                    
                    cachedMap = (w, h, mapArray);
                    _gpuMapCache[transferMapPath] = cachedMap;
                }
            }

            var device = GraphicsDevice.GetDefault();
            int destWidth = cachedMap.Width;
            int destHeight = cachedMap.Height;
            int totalPixels = destWidth * destHeight;

            long mapCacheMs = phaseSw.ElapsedMilliseconds;
            phaseSw.Restart();

            // Load source directly as Bgra32 bytes — no Bitmap!
            int srcWidth, srcHeight;
            byte[] srcPixels;
            using (var srcImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Bgra32>(inputPath)) {
                srcWidth = srcImage.Width;
                srcHeight = srcImage.Height;
                srcPixels = new byte[srcWidth * srcHeight * 4];
                srcImage.CopyPixelDataTo(srcPixels);
            }

            long loadMs = phaseSw.ElapsedMilliseconds;
            phaseSw.Restart();

            // Result buffer — no Bitmap!
            byte[] resultPixels = new byte[totalPixels * 4];

            // Cache the transfer map on the GPU
            if (!_gpuResidentMapCache.TryGetValue(transferMapPath, out var gpuMap)) {
                gpuMap = device.AllocateReadOnlyTexture2D<ComputeSharp.Rgba64, float4>(destWidth, destHeight);
                gpuMap.CopyFrom(cachedMap.Data);
                _gpuResidentMapCache[transferMapPath] = gpuMap;
            }

            using ReadOnlyTexture2D<Bgra32, float4> gpuSource = device.AllocateReadOnlyTexture2D<Bgra32, float4>(srcWidth, srcHeight);
            var pooled = GetPooledBuffers(destWidth, destHeight);
            var gpuDestRgb = pooled.DestRgb;
            var gpuDestAlpha = pooled.DestAlpha;
            var gpuPingRgb = pooled.PingRgb;
            var gpuPingAlpha = pooled.PingAlpha;
            
            ReadOnlySpan<Bgra32> srcSpan = MemoryMarshal.Cast<byte, Bgra32>(srcPixels);
            gpuSource.CopyFrom(srcSpan);

            device.For(totalPixels, new ApplyTransferMapCombinedShader(
                gpuSource,
                gpuMap,
                gpuDestRgb,
                gpuDestAlpha,
                destWidth,
                srcWidth,
                srcHeight,
                useBilinear ? 1 : 0
            ));

            for (int i = 0; i < UVTransferMap.EdgePadding; i++) {
                if (i % 2 == 0) {
                    device.For(totalPixels, new DilateEdgesShader(gpuDestRgb, gpuPingRgb, destWidth, destHeight));
                    device.For(totalPixels, new DilateEdgesShader(gpuDestAlpha, gpuPingAlpha, destWidth, destHeight));
                } else {
                    device.For(totalPixels, new DilateEdgesShader(gpuPingRgb, gpuDestRgb, destWidth, destHeight));
                    device.For(totalPixels, new DilateEdgesShader(gpuPingAlpha, gpuDestAlpha, destWidth, destHeight));
                }
            }

            device.For(totalPixels, new MergeRgbAndAlphaShader(gpuDestRgb, gpuDestAlpha, gpuPingRgb, destWidth));

            Span<Bgra32> destSpan = MemoryMarshal.Cast<byte, Bgra32>(resultPixels);
            gpuPingRgb.CopyTo(destSpan);

            long gpuMs = phaseSw.ElapsedMilliseconds;
            phaseSw.Restart();

            // Save directly as PNG via ImageSharp — no Bitmap conversion!
            // BestSpeed compression: these are intermediate files that get converted to .tex anyway
            using (var bgraImage = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Bgra32>(resultPixels, destWidth, destHeight))
            using (var resultImage = bgraImage.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgba32>()) {
                var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder() {
                    TransparentColorMode = SixLabors.ImageSharp.Formats.Png.PngTransparentColorMode.Preserve,
                    ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha,
                    CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.BestSpeed,
                };
                using (var fs = new System.IO.FileStream(outputPath, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                    resultImage.Save(fs, encoder);
                }
            }

            long saveMs = phaseSw.ElapsedMilliseconds;
            totalSw.Stop();

            System.Diagnostics.Debug.WriteLine($"[GPU TransferFile] Map:{mapCacheMs}ms | Load:{loadMs}ms | GPU:{gpuMs}ms | Save:{saveMs}ms | Total:{totalSw.ElapsedMilliseconds}ms — {System.IO.Path.GetFileName(inputPath)}");

            return true;
        }
    }
}
