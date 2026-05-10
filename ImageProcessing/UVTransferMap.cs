using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Tiff;
using Color = System.Drawing.Color;
using Image = SixLabors.ImageSharp.Image;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public class UVTransferMap {
        public static bool UseGPUAcceleration { get; set; } = true;
        public static int EdgePadding { get; set; } = 16;
        
        /// <summary>
        /// Generates a perfect 16-bit Identity Map where Red = X and Green = Y.
        /// Feed this image into XNormal's base texture slot to bake the coordinate transformation.
        /// Supports both PNG (16-bit) and TIFF (16-bit) output based on file extension.
        /// For best results when baking in XNormal, save the XNormal OUTPUT as .tif to preserve 16-bit precision.
        /// </summary>
        public static void GenerateCoordinateMap(int width, int height, string outputPath) {
            using (var image = new Image<Rgba64>(width, height)) {
                image.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++) {
                        Span<Rgba64> pixelRow = accessor.GetRowSpan(y);
                        
                        ushort g = (ushort)Math.Round((y / (double)(height - 1)) * 65535.0);
                        
                        for (int x = 0; x < accessor.Width; x++) {
                            ushort r = (ushort)Math.Round((x / (double)(width - 1)) * 65535.0);
                            pixelRow[x] = new Rgba64(r, g, 0, 65535);
                        }
                    }
                });
                
                image.Save(outputPath);
            }
        }

        /// <summary>
        /// Applies the XNormal-baked Transfer Map to a source texture.
        /// Processes RGB and Alpha channels separately to prevent alpha bleed at UV boundaries,
        /// then applies edge dilation to fill UV seam gaps (matching XNormal's EdgePadding behavior).
        /// Automatically detects 8-bit transfer maps and applies coordinate smoothing to reduce pixelation.
        /// For best quality, use 16-bit TIFF transfer maps (.tif).
        /// </summary>
        public static Bitmap ApplyTransferMap(Bitmap sourceTexture, string transferMapPath, bool useBilinear = true) {
            if (UseGPUAcceleration && !transferMapPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
                try {
                    Bitmap finalFast = ComputeSharpUVTransfer.ApplyTransferMapFast(sourceTexture, transferMapPath, useBilinear);
                    return finalFast;
                } catch (Exception e) {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gpu_transfer_error.txt"), e.ToString());
                    // Fallback to CPU processing
                }
            }

            Bitmap rgbSource = ImageManipulation.ExtractRGB(sourceTexture);
            Bitmap alphaSource = ImageManipulation.ExtractAlpha(sourceTexture);

            float[] mapX, mapY;
            bool[] mapValid;
            int destWidth, destHeight;
            bool is8Bit;

            using (var transferImage = Image.Load<Rgba64>(transferMapPath)) {
                destWidth = transferImage.Width;
                destHeight = transferImage.Height;
                mapX = new float[destWidth * destHeight];
                mapY = new float[destWidth * destHeight];
                mapValid = new bool[destWidth * destHeight];

                int eightBitCount = 0;
                int sampleCount = 0;

                transferImage.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++) {
                        Span<Rgba64> rowSpan = accessor.GetRowSpan(y);
                        int rowOffset = y * destWidth;
                        for (int x = 0; x < accessor.Width; x++) {
                            Rgba64 pixel = rowSpan[x];
                            int idx = rowOffset + x;
                            bool valid = pixel.A >= 65535;
                            mapValid[idx] = valid;

                            if (valid) {
                                mapX[idx] = pixel.R / 65535.0f;
                                mapY[idx] = pixel.G / 65535.0f;

                                if (sampleCount < 1000) {
                                    if (pixel.R % 257 == 0 && pixel.G % 257 == 0) {
                                        eightBitCount++;
                                    }
                                    sampleCount++;
                                }
                            }
                        }
                    }
                });

                is8Bit = sampleCount > 0 && (float)eightBitCount / sampleCount > 0.95f;
            }

            if (is8Bit) {
                SmoothCoordinates(mapX, mapY, mapValid, destWidth, destHeight);
            }

            Bitmap rgbResult = ApplyTransferMapInternal(rgbSource, mapX, mapY, mapValid, destWidth, destHeight, useBilinear);
            Bitmap alphaResult = ApplyTransferMapInternal(alphaSource, mapX, mapY, mapValid, destWidth, destHeight, useBilinear);

            DilateEdges(rgbResult, EdgePadding);
            DilateEdges(alphaResult, EdgePadding);

            Bitmap finalResult = ImageManipulation.MergeAlphaToRGB(alphaResult, rgbResult);

            rgbSource.Dispose();
            alphaSource.Dispose();
            rgbResult.Dispose();
            alphaResult.Dispose();

            return finalResult;
        }

        private static Bitmap ApplyTransferMapInternal(Bitmap sourceTexture, float[] mapX, float[] mapY, bool[] mapValid, int destWidth, int destHeight, bool useBilinear) {
            int srcWidth = sourceTexture.Width;
            int srcHeight = sourceTexture.Height;

            Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            LockBitmap destLock = new LockBitmap(result);
            LockBitmap srcLock = new LockBitmap(sourceTexture);

            destLock.LockBits();
            srcLock.LockBits();

            byte[] srcPixels = srcLock.Pixels;
            byte[] dstPixels = destLock.Pixels;
            int srcStep = srcLock.Depth / 8;

            if (UseGPUAcceleration) {
                try {
                    srcLock.UnlockBits();
                    destLock.UnlockBits();
                    return ComputeSharpUVTransfer.ApplyTransferMap(sourceTexture, mapX, mapY, mapValid, destWidth, destHeight, useBilinear);
                } catch {
                    // Fallback to CPU if GPU acceleration fails or is unsupported
                    destLock = new LockBitmap(result);
                    srcLock = new LockBitmap(sourceTexture);
                    destLock.LockBits();
                    srcLock.LockBits();
                    srcPixels = srcLock.Pixels;
                    dstPixels = destLock.Pixels;
                    srcStep = srcLock.Depth / 8;
                }
            }

            int __safe_width = destWidth;
            int __safe_height = destHeight;
            System.Threading.Tasks.Parallel.For(0, __safe_height, y => {
                int rowOffset = y * __safe_width;
                int dstRowStart = y * __safe_width * 4;
                for (int x = 0; x < __safe_width; x++) {
                    int idx = rowOffset + x;
                    int dstIdx = dstRowStart + x * 4;
                    if (!mapValid[idx]) continue;

                    float srcXf = mapX[idx] * (srcWidth - 1);
                    float srcYf = mapY[idx] * (srcHeight - 1);

                    if (useBilinear) {
                        int x1 = Math.Clamp((int)Math.Floor(srcXf), 0, srcWidth - 1);
                        int y1 = Math.Clamp((int)Math.Floor(srcYf), 0, srcHeight - 1);
                        int x2 = Math.Min(x1 + 1, srcWidth - 1);
                        int y2 = Math.Min(y1 + 1, srcHeight - 1);

                        float xDiff = srcXf - x1;
                        float yDiff = srcYf - y1;
                        float w11 = (1f - xDiff) * (1f - yDiff);
                        float w21 = xDiff * (1f - yDiff);
                        float w12 = (1f - xDiff) * yDiff;
                        float w22 = xDiff * yDiff;

                        int i11 = (y1 * srcWidth + x1) * srcStep;
                        int i21 = (y1 * srcWidth + x2) * srcStep;
                        int i12 = (y2 * srcWidth + x1) * srcStep;
                        int i22 = (y2 * srcWidth + x2) * srcStep;

                        byte b11 = srcPixels[i11], g11 = srcPixels[i11 + 1], r11 = srcPixels[i11 + 2];
                        byte b21 = srcPixels[i21], g21 = srcPixels[i21 + 1], r21 = srcPixels[i21 + 2];
                        byte b12 = srcPixels[i12], g12 = srcPixels[i12 + 1], r12 = srcPixels[i12 + 2];
                        byte b22 = srcPixels[i22], g22 = srcPixels[i22 + 1], r22 = srcPixels[i22 + 2];

                        dstPixels[dstIdx] = (byte)Math.Clamp((int)(b11 * w11 + b21 * w21 + b12 * w12 + b22 * w22 + 0.5f), 0, 255);
                        dstPixels[dstIdx + 1] = (byte)Math.Clamp((int)(g11 * w11 + g21 * w21 + g12 * w12 + g22 * w22 + 0.5f), 0, 255);
                        dstPixels[dstIdx + 2] = (byte)Math.Clamp((int)(r11 * w11 + r21 * w21 + r12 * w12 + r22 * w22 + 0.5f), 0, 255);
                        dstPixels[dstIdx + 3] = 255;
                    } else {
                        int srcXi = Math.Clamp((int)Math.Round(srcXf), 0, srcWidth - 1);
                        int srcYi = Math.Clamp((int)Math.Round(srcYf), 0, srcHeight - 1);
                        int srcIdx = (srcYi * srcWidth + srcXi) * srcStep;
                        
                        dstPixels[dstIdx] = srcPixels[srcIdx];
                        dstPixels[dstIdx + 1] = srcPixels[srcIdx + 1];
                        dstPixels[dstIdx + 2] = srcPixels[srcIdx + 2];
                        dstPixels[dstIdx + 3] = 255;
                    }
                }
            });

            destLock.UnlockBits();
            srcLock.UnlockBits();

            return result;
        }

        /// <summary>
        /// Smooths quantized 8-bit coordinate data by applying a separable Gaussian-weighted
        /// average within UV islands. This reconstructs the smooth UV mapping that was lost
        /// to 8-bit quantization, effectively recovering sub-pixel coordinate precision.
        /// Uses a radius proportional to the quantization step size (source_size / 256).
        /// </summary>
        private static void SmoothCoordinates(float[] mapX, float[] mapY, bool[] valid, int width, int height) {
            // 8-bit gives 256 values across the range. The quantization step in normalized coords = 1/255.
            // We smooth with a radius that covers roughly half a quantization step in pixel space.
            // For 4096 source: step = 16px, radius = 8. For 2048: step = 8, radius = 4.
            // Since we work in normalized coords, we use a fixed pixel-space radius.
            int radius = 8;
            float[] smoothX = new float[width * height];
            float[] smoothY = new float[width * height];
            Array.Copy(mapX, smoothX, mapX.Length);
            Array.Copy(mapY, smoothY, mapY.Length);

            // Horizontal pass
            float[] tempX = new float[width * height];
            float[] tempY = new float[width * height];
            Array.Copy(smoothX, tempX, smoothX.Length);
            Array.Copy(smoothY, tempY, smoothY.Length);

            int __w = width;
            int __h = height;
            System.Threading.Tasks.Parallel.For(0, __h, y => {
                int row = y * __w;
                for (int x = 0; x < __w; x++) {
                    int idx = row + x;
                    if (!valid[idx]) continue;

                    float centerX = smoothX[idx];
                    float centerY = smoothY[idx];
                    float sumX = 0, sumY = 0, weightSum = 0;

                    for (int dx = -radius; dx <= radius; dx++) {
                        int nx = x + dx;
                        if (nx < 0 || nx >= __w) continue;
                        int nIdx = row + nx;
                        if (!valid[nIdx]) continue;

                        // Only average with neighbors that are in the same UV island
                        // (coordinates should be close, not across a UV seam)
                        float diffX = Math.Abs(smoothX[nIdx] - centerX);
                        float diffY = Math.Abs(smoothY[nIdx] - centerY);
                        if (diffX > 0.05f || diffY > 0.05f) continue;

                        float dist = Math.Abs(dx);
                        float weight = 1.0f / (1.0f + dist * dist * 0.1f);
                        sumX += smoothX[nIdx] * weight;
                        sumY += smoothY[nIdx] * weight;
                        weightSum += weight;
                    }

                    if (weightSum > 0) {
                        tempX[idx] = sumX / weightSum;
                        tempY[idx] = sumY / weightSum;
                    }
                }
            });

            // Vertical pass
            System.Threading.Tasks.Parallel.For(0, __h, y => {
                int row = y * __w;
                for (int x = 0; x < __w; x++) {
                    int idx = row + x;
                    if (!valid[idx]) continue;

                    float centerX = tempX[idx];
                    float centerY = tempY[idx];
                    float sumX = 0, sumY = 0, weightSum = 0;

                    for (int dy = -radius; dy <= radius; dy++) {
                        int ny = y + dy;
                        if (ny < 0 || ny >= __h) continue;
                        int nIdx = ny * __w + x;
                        if (!valid[nIdx]) continue;

                        float diffX = Math.Abs(tempX[nIdx] - centerX);
                        float diffY = Math.Abs(tempY[nIdx] - centerY);
                        if (diffX > 0.05f || diffY > 0.05f) continue;

                        float dist = Math.Abs(dy);
                        float weight = 1.0f / (1.0f + dist * dist * 0.1f);
                        sumX += tempX[nIdx] * weight;
                        sumY += tempY[nIdx] * weight;
                        weightSum += weight;
                    }

                    if (weightSum > 0) {
                        mapX[idx] = sumX / weightSum;
                        mapY[idx] = sumY / weightSum;
                    }
                }
            });
        }

        /// <summary>
        /// Dilates filled pixels outward to cover empty gaps at UV seam edges.
        /// Equivalent to XNormal's EdgePadding parameter.
        /// </summary>
        private static void DilateEdges(Bitmap image, int iterations) {
            int width = image.Width;
            int height = image.Height;

            LockBitmap lockBmp = new LockBitmap(image);
            lockBmp.LockBits();
            byte[] pixels = lockBmp.Pixels;

            bool[] filled = new bool[width * height];
            for (int i = 0; i < width * height; i++) {
                filled[i] = pixels[i * 4 + 3] > 0;
            }

            for (int iter = 0; iter < iterations; iter++) {
                byte[] tempPixels = new byte[pixels.Length];
                Buffer.BlockCopy(pixels, 0, tempPixels, 0, pixels.Length);
                bool[] newFilled = new bool[width * height];
                Array.Copy(filled, newFilled, filled.Length);

                int __safe_width = width;
                int __safe_height = height;
                System.Threading.Tasks.Parallel.For(0, __safe_height, y => {
                    for (int x = 0; x < __safe_width; x++) {
                        int idx = y * __safe_width + x;
                        if (filled[idx]) continue;

                        int sumB = 0, sumG = 0, sumR = 0, count = 0;
                        for (int dy = -1; dy <= 1; dy++) {
                            int ny = y + dy;
                            if (ny < 0 || ny >= __safe_height) continue;
                            for (int dx = -1; dx <= 1; dx++) {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx;
                                if (nx < 0 || nx >= __safe_width) continue;

                                int nIdx = ny * __safe_width + nx;
                                if (!filled[nIdx]) continue;
                                
                                int pxIdx = nIdx * 4;
                                sumB += tempPixels[pxIdx];
                                sumG += tempPixels[pxIdx + 1];
                                sumR += tempPixels[pxIdx + 2];
                                count++;
                            }
                        }
                        if (count > 0) {
                            int pxIdx = idx * 4;
                            pixels[pxIdx] = (byte)(sumB / count);
                            pixels[pxIdx + 1] = (byte)(sumG / count);
                            pixels[pxIdx + 2] = (byte)(sumR / count);
                            pixels[pxIdx + 3] = 255;
                            newFilled[idx] = true;
                        }
                    }
                });
                filled = newFilled;
            }

            lockBmp.UnlockBits();
        }
    }
}
