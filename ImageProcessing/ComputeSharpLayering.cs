using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    [ThreadGroupSize(1024, 1, 1)]
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

    [ThreadGroupSize(1024, 1, 1)]
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

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeImagesShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MergeImagesShader(
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
            
            float widthRatio = (float)SrcWidth / (float)SrcHeight;
            int scaledTopWidth = (int)(DestHeight * widthRatio);
            
            float srcXf = (float)x / scaledTopWidth * SrcWidth;
            float srcYf = (float)y / DestHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (x < scaledTopWidth && srcXf < SrcWidth && srcYf < SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            float topA = topPixel.W;
            float bottomA = bottomPixel.W;
            float outA = topA + bottomA * (1.0f - topA);
            
            float outR = 0;
            float outG = 0;
            float outB = 0;
            
            if (outA > 0) {
                outR = (topPixel.Z * topA + bottomPixel.Z * bottomA * (1.0f - topA)) / outA;
                outG = (topPixel.Y * topA + bottomPixel.Y * bottomA * (1.0f - topA)) / outA;
                outB = (topPixel.X * topA + bottomPixel.X * bottomA * (1.0f - topA)) / outA;
            }

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeImagesPingPongShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MergeImagesPingPongShader(
            ReadWriteTexture2D<Bgra32, float4> bottomLayer, 
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
            
            float widthRatio = (float)SrcWidth / (float)SrcHeight;
            int scaledTopWidth = (int)(DestHeight * widthRatio);
            
            float srcXf = (float)x / scaledTopWidth * SrcWidth;
            float srcYf = (float)y / DestHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (x < scaledTopWidth && srcXf < SrcWidth && srcYf < SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            float topA = topPixel.W;
            float bottomA = bottomPixel.W;
            float outA = topA + bottomA * (1.0f - topA);
            
            float outR = 0;
            float outG = 0;
            float outB = 0;
            
            if (outA > 0) {
                outR = (topPixel.Z * topA + bottomPixel.Z * bottomA * (1.0f - topA)) / outA;
                outG = (topPixel.Y * topA + bottomPixel.Y * bottomA * (1.0f - topA)) / outA;
                outB = (topPixel.X * topA + bottomPixel.X * bottomA * (1.0f - topA)) / outA;
            }

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct CopyShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Destination;
        public readonly int Width;
        public readonly int Height;

        public CopyShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> destination, int width, int height) {
            Source = source;
            Destination = destination;
            Width = width;
            Height = height;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= Width * Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            Destination[pos] = Source[pos];
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeAlphaToRGBScalingShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Rgb;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Alpha;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int AlphaWidth;
        public readonly int AlphaHeight;
        public readonly int InvertAlpha;

        public MergeAlphaToRGBScalingShader(ReadOnlyTexture2D<Bgra32, float4> rgb, ReadOnlyTexture2D<Bgra32, float4> alpha, ReadWriteTexture2D<Bgra32, float4> output, int destWidth, int destHeight, int alphaWidth, int alphaHeight, int invertAlpha) {
            Rgb = rgb;
            Alpha = alpha;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            AlphaWidth = alphaWidth;
            AlphaHeight = alphaHeight;
            InvertAlpha = invertAlpha;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 rgbPixel = Rgb[pos];
            
            float alphaXf = (float)x / DestWidth * AlphaWidth;
            float alphaYf = (float)y / DestHeight * AlphaHeight;
            int alphaX = Hlsl.Clamp((int)alphaXf, 0, AlphaWidth - 1);
            int alphaY = Hlsl.Clamp((int)alphaYf, 0, AlphaHeight - 1);
            float4 alphaPixel = Alpha[new int2(alphaX, alphaY)];
            
            float alphaVal = alphaPixel.Z;
            if (InvertAlpha == 1) {
                alphaVal = 1.0f - alphaVal;
            }

            Output[pos] = new float4(rgbPixel.X, rgbPixel.Y, rgbPixel.Z, alphaVal);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ClearShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;
        public readonly int Height;

        public ClearShader(ReadWriteTexture2D<Bgra32, float4> output, int width, int height) {
            Output = output;
            Width = width;
            Height = height;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= Width * Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            Output[pos] = new float4(0, 0, 0, 0);
        }
    }

    // Single-dispatch shader that composites ALL layers in one pass
    // Layer metadata layout in metaBuffer: [layerCount, destW, destH, <padding>,
    //                                       layer0_width, layer0_height, layer0_pixelOffset, <padding>,
    //                                       layer1_width, layer1_height, layer1_pixelOffset, <padding>, ...]
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MultiLayerMergeShader : IComputeShader {
        public readonly ReadOnlyBuffer<uint> AllPixels;
        public readonly ReadOnlyBuffer<int> Meta;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;

        public MultiLayerMergeShader(
            ReadOnlyBuffer<uint> allPixels,
            ReadOnlyBuffer<int> meta,
            ReadWriteTexture2D<Bgra32, float4> output) {
            AllPixels = allPixels;
            Meta = meta;
            Output = output;
        }

        public void Execute() {
            int layerCount = Meta[0];
            int destW = Meta[1];
            int destH = Meta[2];

            int idx = ThreadIds.X;
            if (idx >= destW * destH) return;

            int y = idx / destW;
            int x = idx % destW;

            // Accumulate: start transparent
            float accR = 0, accG = 0, accB = 0, accA = 0;

            for (int layer = 0; layer < layerCount; layer++) {
                int metaBase = 4 + layer * 4;
                int srcW = Meta[metaBase];
                int srcH = Meta[metaBase + 1];
                int pixelOffset = Meta[metaBase + 2];

                // Compute source coordinates with aspect-ratio scaling
                float widthRatio = (float)srcW / (float)srcH;
                int scaledTopWidth = (int)(destH * widthRatio);

                float srcXf = (float)x / scaledTopWidth * srcW;
                float srcYf = (float)y / destH * srcH;

                if (x < scaledTopWidth && srcXf < srcW && srcYf < srcH) {
                    int srcX = Hlsl.Clamp((int)srcXf, 0, srcW - 1);
                    int srcY = Hlsl.Clamp((int)srcYf, 0, srcH - 1);

                    uint packed = AllPixels[pixelOffset + srcY * srcW + srcX];
                    // BGRA byte order: B=byte0, G=byte1, R=byte2, A=byte3
                    float topB = (float)(packed & 0xFF) / 255.0f;
                    float topG = (float)((packed >> 8) & 0xFF) / 255.0f;
                    float topR = (float)((packed >> 16) & 0xFF) / 255.0f;
                    float topA = (float)((packed >> 24) & 0xFF) / 255.0f;

                    if (topA > 0) {
                        float outA = topA + accA * (1.0f - topA);
                        if (outA > 0) {
                            accR = (topR * topA + accR * accA * (1.0f - topA)) / outA;
                            accG = (topG * topA + accG * accA * (1.0f - topA)) / outA;
                            accB = (topB * topA + accB * accA * (1.0f - topA)) / outA;
                        }
                        accA = outA;
                    }
                }
            }

            Output[new int2(x, y)] = new float4(accB, accG, accR, accA);
        }
    }

    public static class ComputeSharpLayering {
        
        private static System.Collections.Concurrent.ConcurrentDictionary<string, (ReadOnlyTexture2D<Bgra32, float4> Texture, int Width, int Height)> _vramCache = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Pixels, int Width, int Height)> _cpuPixelCache = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.IO.FileSystemWatcher> _watchers = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, byte> _invalidatedPaths = new();

        // Cached working surfaces — reused across merge calls to avoid GPU alloc/dealloc churn
        private static ReadWriteTexture2D<Bgra32, float4> _cachedPing;
        private static ReadWriteTexture2D<Bgra32, float4> _cachedPong;
        private static int _cachedWidth;
        private static int _cachedHeight;
        private static byte[] _cachedResultBuffer;
        private static readonly object _gpuLock = new object();

        private static int _invalidationCount = 0;
        private static void OnFileChanged(object sender, System.IO.FileSystemEventArgs e) {
            var fullPath = e.FullPath;
            // Only invalidate if this file is actually in one of our caches
            bool wasCached = false;
            if (_vramCache.TryRemove(fullPath, out var entry)) {
                entry.Texture.Dispose();
                wasCached = true;
            }
            if (_cpuPixelCache.TryRemove(fullPath, out _)) {
                wasCached = true;
            }
            if (wasCached) {
                _invalidatedPaths[fullPath] = 0;
                _invalidationCount++;
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt"), 
                    $"  [CACHE INVALIDATED #{_invalidationCount}] {e.ChangeType}: {System.IO.Path.GetFileName(fullPath)}\r\n"); } catch {}
            }
        }

        private static void WatchDirectory(string filePath) {
            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir) || _watchers.ContainsKey(dir))
                return;

            try {
                var watcher = new System.IO.FileSystemWatcher(dir) {
                    NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                watcher.Changed += OnFileChanged;
                watcher.Renamed += (s, e) => OnFileChanged(s, e);
                watcher.Deleted += OnFileChanged;
                _watchers[dir] = watcher;
            } catch {
                // Directory may not exist or be inaccessible — silently skip
            }
        }

        public static void ClearCache() {
            foreach (var kvp in _vramCache) {
                kvp.Value.Texture.Dispose();
            }
            _vramCache.Clear();
            _cpuPixelCache.Clear();
            _invalidatedPaths.Clear();
            foreach (var kvp in _watchers) {
                kvp.Value.Dispose();
            }
            _watchers.Clear();
            _cachedPing?.Dispose();
            _cachedPong?.Dispose();
            _cachedPing = null;
            _cachedPong = null;
            _cachedResultBuffer = null;
        }

        // CPU-only pixel loading (thread-safe, parallelizable)
        private struct CpuLayerData {
            public byte[] Pixels;
            public int Width;
            public int Height;
            public string Path;
            public bool IsPhysicalFile;
            public bool CacheHit;
        }

        private static CpuLayerData LoadPixelsCpu(string path) {
            var result = new CpuLayerData { Path = path };
            if (string.IsNullOrEmpty(path))
                return result;

            result.IsPhysicalFile = !path.StartsWith("memory://", StringComparison.OrdinalIgnoreCase);

            // Fast path: if file is in CPU pixel cache and not invalidated, return cached pixels
            if (result.IsPhysicalFile && FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                bool inCache = _cpuPixelCache.TryGetValue(path, out var cpuCached);
                bool invalidated = _invalidatedPaths.ContainsKey(path);
                if (inCache && !invalidated) {
                    result.CacheHit = true;
                    result.Pixels = cpuCached.Pixels;
                    result.Width = cpuCached.Width;
                    result.Height = cpuCached.Height;
                    return result;
                }
                if (!inCache && _cpuPixelCache.Count > 0) {
                    // Find if same filename exists under a different full path
                    string lookupName = System.IO.Path.GetFileName(path);
                    string matchingCachedPath = "";
                    foreach (var k in _cpuPixelCache.Keys) {
                        if (System.IO.Path.GetFileName(k) == lookupName) { matchingCachedPath = k; break; }
                    }
                    string pathSuffix = path.Length > 80 ? "..." + path.Substring(path.Length - 80) : path;
                    string matchSuffix = string.IsNullOrEmpty(matchingCachedPath) ? "NOT_FOUND" : 
                        (matchingCachedPath.Length > 80 ? "..." + matchingCachedPath.Substring(matchingCachedPath.Length - 80) : matchingCachedPath);
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt"), 
                        $"  [CACHE MISS] lookup=\"{pathSuffix}\" cachedAs=\"{matchSuffix}\"\r\n"); } catch {}
                }
                // Clear invalidation flag — we're about to reload
                _invalidatedPaths.TryRemove(path, out _);
            }

            // Cache miss or invalidated — validate file exists before decoding
            if (!TexIO.Exists(path))
                return result;

            // Cache miss — decode pixels on CPU
            if (path.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".ltct", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".raw", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("memory://", StringComparison.OrdinalIgnoreCase)) {
                using (var bitmap = TexIO.ResolveBitmap(path)) {
                    Bitmap safe = bitmap.PixelFormat == PixelFormat.Format32bppArgb ? bitmap : bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format32bppArgb);
                    var bmpData = safe.LockBits(new Rectangle(0, 0, safe.Width, safe.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    result.Pixels = new byte[safe.Width * safe.Height * 4];
                    Marshal.Copy(bmpData.Scan0, result.Pixels, 0, result.Pixels.Length);
                    safe.UnlockBits(bmpData);
                    if (safe != bitmap) safe.Dispose();
                    result.Width = safe.Width;
                    result.Height = safe.Height;
                }
            } else {
                while (TexIO.IsFileLocked(path)) { System.Threading.Thread.Sleep(100); }
                using (var ms = new System.IO.MemoryStream()) {
                    using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)) {
                        fs.CopyTo(ms);
                    }
                    ms.Position = 0;
                    using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Bgra32>(ms)) {
                        result.Pixels = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(result.Pixels);
                        result.Width = image.Width;
                        result.Height = image.Height;
                    }
                }
            }

            // Store in CPU pixel cache for fast buffer packing on future calls
            if (result.Pixels != null && result.IsPhysicalFile && FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                _cpuPixelCache[path] = (result.Pixels, result.Width, result.Height);
                WatchDirectory(path);
            }

            return result;
        }

        // Phase 2: GPU upload (must be called sequentially on a single thread)
        private static (ReadOnlyTexture2D<Bgra32, float4> Texture, bool IsCached, int Width, int Height) UploadToVram(GraphicsDevice device, CpuLayerData cpuData) {
            if (string.IsNullOrEmpty(cpuData.Path))
                return (null, false, 0, 0);

            // Fast path: check VRAM cache directly
            if (_vramCache.TryGetValue(cpuData.Path, out var cached) && !_invalidatedPaths.ContainsKey(cpuData.Path)) {
                return (cached.Texture, true, cached.Width, cached.Height);
            }

            // Slow path: cache miss — dispose stale entry if present
            if (cpuData.IsPhysicalFile && FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                if (_vramCache.TryRemove(cpuData.Path, out var staleEntry)) {
                    staleEntry.Texture.Dispose();
                }
            }

            if (cpuData.Pixels == null) return (null, false, 0, 0);

            // Allocate and upload to GPU (single-threaded, no driver contention)
            var texture = device.AllocateReadOnlyTexture2D<Bgra32, float4>(cpuData.Width, cpuData.Height);
            texture.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(cpuData.Pixels));

            if (cpuData.IsPhysicalFile && FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                _vramCache[cpuData.Path] = (texture, cpuData.Width, cpuData.Height);
                WatchDirectory(cpuData.Path);
                return (texture, true, cpuData.Width, cpuData.Height);
            }

            return (texture, false, cpuData.Width, cpuData.Height);
        }

        public static Bitmap MergeMultipleImagesGpuFromPaths(System.Collections.Generic.List<string> paths, int width, int height) {
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Phase 1: Load/cache all layer pixels on CPU (parallel for cache misses)
            var cpuLayers = new CpuLayerData[paths.Count];
            bool allCached = true;
            for (int i = 0; i < paths.Count; i++) {
                cpuLayers[i] = LoadPixelsCpu(paths[i]);
                if (!cpuLayers[i].CacheHit && !string.IsNullOrEmpty(cpuLayers[i].Path)) {
                    allCached = false;
                    break;
                }
            }

            if (!allCached) {
                System.Threading.Tasks.Parallel.For(0, paths.Count, i => {
                    if (!cpuLayers[i].CacheHit) {
                        cpuLayers[i] = LoadPixelsCpu(paths[i]);
                    }
                });
            }
            long phase1Ms = sw.ElapsedMilliseconds;
            int cpuHits = 0, cpuMisses = 0, memoryPaths = 0;
            for (int i = 0; i < cpuLayers.Length; i++) {
                if (string.IsNullOrEmpty(cpuLayers[i].Path)) continue;
                if (!cpuLayers[i].IsPhysicalFile) memoryPaths++;
                if (cpuLayers[i].CacheHit) cpuHits++; else cpuMisses++;
            }
            sw.Restart();

            // All GPU work serialized
            lock (_gpuLock) {
                // Phase 2: Upload to VRAM (sequential, single-threaded)
                var textures = new (ReadOnlyTexture2D<Bgra32, float4> Tex, bool IsCached, int Width, int Height)[paths.Count];
                int vramHits = 0, vramMisses = 0;
                for (int i = 0; i < paths.Count; i++) {
                    textures[i] = UploadToVram(device, cpuLayers[i]);
                    if (textures[i].IsCached && cpuLayers[i].CacheHit) vramHits++; 
                    else if (textures[i].Tex != null) vramMisses++;
                }
                long phase2Ms = sw.ElapsedMilliseconds;
                sw.Restart();

                // Reuse cached ping/pong working textures if dimensions match
                if (_cachedPing == null || _cachedWidth != width || _cachedHeight != height) {
                    _cachedPing?.Dispose();
                    _cachedPong?.Dispose();
                    _cachedPing = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
                    _cachedPong = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
                    _cachedWidth = width;
                    _cachedHeight = height;
                    _cachedResultBuffer = new byte[totalPixels * 4];
                }
                var ping = _cachedPing;
                var pong = _cachedPong;

                // Phase 3: Batched GPU merge — all dispatches recorded into one command list
                bool isPing = true;
                using (var context = device.CreateComputeContext()) {
                    // Initialize base layer
                    if (textures.Length > 0 && textures[0].Tex != null) {
                        var ld = textures[0];
                        if (ld.Width == width && ld.Height == height) {
                            context.For(totalPixels, new CopyShader(ld.Tex, ping, width, height));
                        } else {
                            context.For(totalPixels, new ClearShader(ping, width, height));
                            context.For(totalPixels, new MergeImagesPingPongShader(ping, ld.Tex, pong, width, height, ld.Width, ld.Height));
                            isPing = false;
                        }
                    }

                    // Merge remaining layers — all recorded, no fence waits between them
                    for (int i = 1; i < textures.Length; i++) {
                        var ld = textures[i];
                        if (ld.Tex == null) continue;

                        if (isPing) {
                            context.For(totalPixels, new MergeImagesPingPongShader(ping, ld.Tex, pong, width, height, ld.Width, ld.Height));
                        } else {
                            context.For(totalPixels, new MergeImagesPingPongShader(pong, ld.Tex, ping, width, height, ld.Width, ld.Height));
                        }
                        isPing = !isPing;
                    }
                } // ComputeContext disposes here — submits ALL dispatches as one command list, ONE fence wait
                long phase3Ms = sw.ElapsedMilliseconds;
                sw.Restart();

                // Dispose non-cached textures
                for (int i = 0; i < textures.Length; i++) {
                    if (!textures[i].IsCached && textures[i].Tex != null)
                        textures[i].Tex.Dispose();
                }

                // Only GPU→CPU transfer: the final merged result (unavoidable for disk write)
                if (isPing) {
                    ping.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
                } else {
                    pong.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
                }
                long phase4Ms = sw.ElapsedMilliseconds;
                sw.Restart();

                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt"), 
                    $"  [GPU Phases] CpuCache={phase1Ms}ms(hit={cpuHits}/miss={cpuMisses}/mem={memoryPaths}) VramUpload={phase2Ms}ms(hit={vramHits}/miss={vramMisses}) GpuMerge={phase3Ms}ms Readback={phase4Ms}ms CacheSize(cpu={_cpuPixelCache.Count}/vram={_vramCache.Count})\r\n"); } catch {}
            } // release GPU lock

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpDataResult = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_cachedResultBuffer, 0, bmpDataResult.Scan0, _cachedResultBuffer.Length);
            result.UnlockBits(bmpDataResult);

            return result;
        }

        public static Bitmap MergeAlphaToRGBGpuFromPaths(string rgbPath, string alphaPath, int destWidth, int destHeight, bool invertAlpha) {
            var device = GraphicsDevice.GetDefault();
            int totalPixels = destWidth * destHeight;

            var cpuRgb = LoadPixelsCpu(rgbPath);
            var cpuAlpha = LoadPixelsCpu(alphaPath);
            if (!cpuRgb.CacheHit) cpuRgb = LoadPixelsCpu(rgbPath);
            if (!cpuAlpha.CacheHit) cpuAlpha = LoadPixelsCpu(alphaPath);

            lock (_gpuLock) {
                var rgbTex = UploadToVram(device, cpuRgb);
                var alphaTex = UploadToVram(device, cpuAlpha);

                if (_cachedPing == null || _cachedWidth != destWidth || _cachedHeight != destHeight) {
                    _cachedPing?.Dispose();
                    _cachedPong?.Dispose();
                    _cachedPing = device.AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                    _cachedPong = device.AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                    _cachedWidth = destWidth;
                    _cachedHeight = destHeight;
                    _cachedResultBuffer = new byte[totalPixels * 4];
                }
                var output = _cachedPing;

                using (var context = device.CreateComputeContext()) {
                    context.For(totalPixels, new MergeAlphaToRGBScalingShader(
                        rgbTex.Texture, alphaTex.Texture, output, 
                        destWidth, destHeight, 
                        alphaTex.Width, alphaTex.Height, 
                        invertAlpha ? 1 : 0));
                }

                if (!rgbTex.IsCached && rgbTex.Texture != null) rgbTex.Texture.Dispose();
                if (!alphaTex.IsCached && alphaTex.Texture != null) alphaTex.Texture.Dispose();

                output.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
            }

            Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            var bmpDataResult = result.LockBits(new Rectangle(0, 0, destWidth, destHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_cachedResultBuffer, 0, bmpDataResult.Scan0, _cachedResultBuffer.Length);
            result.UnlockBits(bmpDataResult);

            return result;
        }

        public static Bitmap MergeMultipleImagesGpu(Bitmap[] layers, int width, int height) {
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            using var ping = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            using var pong = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            
            if (layers.Length > 0 && layers[0] != null) {
                Bitmap layer0 = layers[0];
                Bitmap safe0 = layer0.PixelFormat == PixelFormat.Format32bppArgb ? layer0 : layer0.Clone(new Rectangle(0, 0, layer0.Width, layer0.Height), PixelFormat.Format32bppArgb);
                
                var bmpData0 = safe0.LockBits(new Rectangle(0, 0, safe0.Width, safe0.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                unsafe {
                    var span = new ReadOnlySpan<Bgra32>((void*)bmpData0.Scan0, safe0.Width * safe0.Height);
                    ping.CopyFrom(span);
                }
                safe0.UnlockBits(bmpData0);
                
                if (safe0 != layer0) safe0.Dispose();
            } else {
                byte[] blankPixels = new byte[totalPixels * 4];
                ping.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(blankPixels));
            }
            
            bool isPing = true;

            for (int i = 1; i < layers.Length; i++) {
                Bitmap topLayer = layers[i];
                if (topLayer == null) continue;
                
                Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);
                
                using (var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height)) {
                    var bmpDataTop = safeTop.LockBits(new Rectangle(0, 0, safeTop.Width, safeTop.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        var span = new ReadOnlySpan<Bgra32>((void*)bmpDataTop.Scan0, safeTop.Width * safeTop.Height);
                        gpuTop.CopyFrom(span);
                    }
                    safeTop.UnlockBits(bmpDataTop);
                    if (safeTop != topLayer) safeTop.Dispose();

                    if (isPing) {
                        device.For(totalPixels, new MergeImagesPingPongShader(ping, gpuTop, pong, width, height, topLayer.Width, topLayer.Height));
                    } else {
                        device.For(totalPixels, new MergeImagesPingPongShader(pong, gpuTop, ping, width, height, topLayer.Width, topLayer.Height));
                    }
                }
                isPing = !isPing;
            }

            byte[] resultPixels = new byte[totalPixels * 4];
            if (isPing) {
                ping.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));
            } else {
                pong.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));
            }

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpDataResult = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpDataResult.Scan0, resultPixels.Length);
            result.UnlockBits(bmpDataResult);

            return result;
        }
        public static Bitmap LayerImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            Bitmap safeBottom = bottomLayer.PixelFormat == PixelFormat.Format32bppArgb ? bottomLayer : bottomLayer.Clone(new Rectangle(0, 0, bottomLayer.Width, bottomLayer.Height), PixelFormat.Format32bppArgb);
            Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(safeBottom)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(safeTop)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            if (safeBottom != bottomLayer) safeBottom.Dispose();
            if (safeTop != topLayer) safeTop.Dispose();

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

            Bitmap safeBottom = bottomLayer.PixelFormat == PixelFormat.Format32bppArgb ? bottomLayer : bottomLayer.Clone(new Rectangle(0, 0, bottomLayer.Width, bottomLayer.Height), PixelFormat.Format32bppArgb);
            Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(safeBottom)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(safeTop)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            if (safeBottom != bottomLayer) safeBottom.Dispose();
            if (safeTop != topLayer) safeTop.Dispose();

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

        public static Bitmap MergeImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            Bitmap safeBottom = bottomLayer.PixelFormat == PixelFormat.Format32bppArgb ? bottomLayer : bottomLayer.Clone(new Rectangle(0, 0, bottomLayer.Width, bottomLayer.Height), PixelFormat.Format32bppArgb);
            Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(safeBottom)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(safeTop)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            if (safeBottom != bottomLayer) safeBottom.Dispose();
            if (safeTop != topLayer) safeTop.Dispose();

            using var gpuBottom = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuBottom.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bottomPixels));
            gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(topPixels));

            device.For(totalPixels, new MergeImagesShader(gpuBottom, gpuTop, gpuOutput, width, height, topLayer.Width, topLayer.Height));

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

