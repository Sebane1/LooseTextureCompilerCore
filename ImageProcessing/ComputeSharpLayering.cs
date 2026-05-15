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

    public static class ComputeSharpLayering {
        
        private struct LayerPixelData {
            public byte[] Pixels;
            public int Width;
            public int Height;
        }

        private static LayerPixelData LoadPixelDataDirect(string path) {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return default;
            
            // For .tex and .dds files, fall back to TexIO which returns a Bitmap
            if (path.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".ltct", StringComparison.OrdinalIgnoreCase)) {
                using (var bitmap = TexIO.ResolveBitmap(path)) {
                    Bitmap safe = bitmap.PixelFormat == PixelFormat.Format32bppArgb ? bitmap : bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format32bppArgb);
                    var bmpData = safe.LockBits(new Rectangle(0, 0, safe.Width, safe.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    byte[] pixels = new byte[safe.Width * safe.Height * 4];
                    Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
                    safe.UnlockBits(bmpData);
                    if (safe != bitmap) safe.Dispose();
                    return new LayerPixelData { Pixels = pixels, Width = safe.Width, Height = safe.Height };
                }
            }
            
            // For PNG/JPG/BMP: decode directly to Bgra32 pixel array, no System.Drawing.Bitmap needed
            while (TexIO.IsFileLocked(path)) { System.Threading.Thread.Sleep(100); }
            using (var ms = new System.IO.MemoryStream()) {
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)) {
                    fs.CopyTo(ms);
                }
                ms.Position = 0;
                using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Bgra32>(ms)) {
                    byte[] pixels = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(pixels);
                    return new LayerPixelData { Pixels = pixels, Width = image.Width, Height = image.Height };
                }
            }
        }

        public static Bitmap MergeMultipleImagesGpuFromPaths(System.Collections.Generic.List<string> paths, int width, int height) {
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            // Load all layers in parallel directly to pixel arrays (no Bitmap)
            LayerPixelData[] layerData = new LayerPixelData[paths.Count];
            System.Threading.Tasks.Parallel.For(0, paths.Count, i => {
                layerData[i] = LoadPixelDataDirect(paths[i]);
            });

            using var ping = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            using var pong = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            
            // Initialize ping with first layer
            if (layerData.Length > 0 && layerData[0].Pixels != null) {
                var ld = layerData[0];
                if (ld.Width == width && ld.Height == height) {
                    ping.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(ld.Pixels));
                } else {
                    // Different size: upload to temp texture and use shader to scale
                    using (var gpuTemp = device.AllocateReadOnlyTexture2D<Bgra32, float4>(ld.Width, ld.Height)) {
                        gpuTemp.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(ld.Pixels));
                        // Use a blank base and merge layer 0 on top to handle scaling
                        byte[] blankPixels = new byte[totalPixels * 4];
                        ping.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(blankPixels));
                        device.For(totalPixels, new MergeImagesPingPongShader(ping, gpuTemp, pong, width, height, ld.Width, ld.Height));
                        // Result is now in pong, copy back to ping for consistency
                        pong.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(blankPixels));
                        ping.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(blankPixels));
                    }
                }
            } else {
                byte[] blankPixels = new byte[totalPixels * 4];
                ping.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(blankPixels));
            }
            
            bool isPing = true;

            for (int i = 1; i < layerData.Length; i++) {
                var ld = layerData[i];
                if (ld.Pixels == null) continue;
                
                using (var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(ld.Width, ld.Height)) {
                    gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(ld.Pixels));

                    if (isPing) {
                        device.For(totalPixels, new MergeImagesPingPongShader(ping, gpuTop, pong, width, height, ld.Width, ld.Height));
                    } else {
                        device.For(totalPixels, new MergeImagesPingPongShader(pong, gpuTop, ping, width, height, ld.Width, ld.Height));
                    }
                }
                isPing = !isPing;
                layerData[i].Pixels = null; // Allow GC to reclaim
            }
            layerData[0].Pixels = null;

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

