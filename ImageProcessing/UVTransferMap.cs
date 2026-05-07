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
            Bitmap rgbSource = ImageManipulation.ExtractRGB(sourceTexture);
            Bitmap alphaSource = ImageManipulation.ExtractAlpha(sourceTexture);

            Bitmap rgbResult = ApplyTransferMapInternal(rgbSource, transferMapPath, useBilinear);
            Bitmap alphaResult = ApplyTransferMapInternal(alphaSource, transferMapPath, useBilinear);

            DilateEdges(rgbResult, 8);
            DilateEdges(alphaResult, 8);

            Bitmap finalResult = ImageManipulation.MergeAlphaToRGB(alphaResult, rgbResult);

            rgbSource.Dispose();
            alphaSource.Dispose();
            rgbResult.Dispose();
            alphaResult.Dispose();

            return finalResult;
        }

        /// <summary>
        /// Core transfer map application. Reads the transfer map into float arrays for maximum precision,
        /// detects 8-bit maps, and applies coordinate smoothing when needed.
        /// </summary>
        private static Bitmap ApplyTransferMapInternal(Bitmap sourceTexture, string transferMapPath, bool useBilinear) {
            using (var transferImage = Image.Load<Rgba64>(transferMapPath)) {
                int destWidth = transferImage.Width;
                int destHeight = transferImage.Height;
                int srcWidth = sourceTexture.Width;
                int srcHeight = sourceTexture.Height;

                // Read transfer map into float arrays (normalized 0.0 - 1.0)
                float[] mapX = new float[destWidth * destHeight];
                float[] mapY = new float[destWidth * destHeight];
                bool[] mapValid = new bool[destWidth * destHeight];

                bool is8Bit = false;

                transferImage.ProcessPixelRows(accessor => {
                    // Sample some pixels to detect if the map is 8-bit (values are multiples of 257)
                    int eightBitCount = 0;
                    int sampleCount = 0;

                    for (int y = 0; y < accessor.Height; y++) {
                        Span<Rgba64> rowSpan = accessor.GetRowSpan(y);
                        int rowOffset = y * destWidth;
                        for (int x = 0; x < accessor.Width; x++) {
                            Rgba64 pixel = rowSpan[x];
                            int idx = rowOffset + x;
                            bool valid = pixel.A >= 100;
                            mapValid[idx] = valid;

                            if (valid) {
                                mapX[idx] = pixel.R / 65535.0f;
                                mapY[idx] = pixel.G / 65535.0f;

                                // Check if value is a multiple of 257 (8-bit upscaled to 16-bit)
                                if (sampleCount < 1000) {
                                    if (pixel.R % 257 == 0 && pixel.G % 257 == 0) {
                                        eightBitCount++;
                                    }
                                    sampleCount++;
                                }
                            }
                        }
                    }
                    is8Bit = sampleCount > 0 && (float)eightBitCount / sampleCount > 0.95f;
                });

                // If the map is 8-bit, smooth the coordinates to recover sub-pixel precision.
                // Within UV islands, coordinates vary smoothly, so we can interpolate between
                // the quantized 8-bit values to reconstruct the original smooth mapping.
                if (is8Bit) {
                    SmoothCoordinates(mapX, mapY, mapValid, destWidth, destHeight);
                }

                // Now remap source pixels using the (possibly smoothed) coordinate arrays
                Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
                LockBitmap destLock = new LockBitmap(result);
                LockBitmap srcLock = new LockBitmap(sourceTexture);

                destLock.LockBits();
                srcLock.LockBits();

                int __safe_width = destWidth;
                int __safe_height = destHeight;
                System.Threading.Tasks.Parallel.For(0, __safe_height, y => {
                    int rowOffset = y * __safe_width;
                    for (int x = 0; x < __safe_width; x++) {
                        int idx = rowOffset + x;
                        if (!mapValid[idx]) continue;

                        float srcXf = mapX[idx] * (srcWidth - 1);
                        float srcYf = mapY[idx] * (srcHeight - 1);

                        Color sampledColor;
                        if (useBilinear) {
                            sampledColor = BilinearSample(srcLock, srcXf, srcYf, srcWidth, srcHeight);
                        } else {
                            int srcXi = Math.Clamp((int)Math.Round(srcXf), 0, srcWidth - 1);
                            int srcYi = Math.Clamp((int)Math.Round(srcYf), 0, srcHeight - 1);
                            sampledColor = srcLock.GetPixel(srcXi, srcYi);
                        }

                        destLock.SetPixel(x, y, Color.FromArgb(255, sampledColor.R, sampledColor.G, sampledColor.B));
                    }
                });

                destLock.UnlockBits();
                srcLock.UnlockBits();

                return result;
            }
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

            for (int iter = 0; iter < iterations; iter++) {
                LockBitmap lockBmp = new LockBitmap(image);
                lockBmp.LockBits();

                bool[] filled = new bool[width * height];
                Color[] colors = new Color[width * height];
                for (int y = 0; y < height; y++) {
                    int row = y * width;
                    for (int x = 0; x < width; x++) {
                        Color c = lockBmp.GetPixel(x, y);
                        int idx = row + x;
                        filled[idx] = c.A > 0;
                        colors[idx] = c;
                    }
                }

                int __safe_width = width;
                int __safe_height = height;
                System.Threading.Tasks.Parallel.For(0, __safe_height, y => {
                    for (int x = 0; x < __safe_width; x++) {
                        int idx = y * __safe_width + x;
                        if (filled[idx]) continue;

                        int sumR = 0, sumG = 0, sumB = 0, count = 0;
                        for (int dy = -1; dy <= 1; dy++) {
                            for (int dx = -1; dx <= 1; dx++) {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || nx >= __safe_width || ny < 0 || ny >= __safe_height) continue;
                                int nIdx = ny * __safe_width + nx;
                                if (!filled[nIdx]) continue;
                                sumR += colors[nIdx].R;
                                sumG += colors[nIdx].G;
                                sumB += colors[nIdx].B;
                                count++;
                            }
                        }
                        if (count > 0) {
                            lockBmp.SetPixel(x, y, Color.FromArgb(255, sumR / count, sumG / count, sumB / count));
                        }
                    }
                });

                lockBmp.UnlockBits();
            }
        }

        private static Color BilinearSample(LockBitmap srcLock, float x, float y, int width, int height) {
            int x1 = Math.Clamp((int)Math.Floor(x), 0, width - 1);
            int y1 = Math.Clamp((int)Math.Floor(y), 0, height - 1);
            int x2 = Math.Min(x1 + 1, width - 1);
            int y2 = Math.Min(y1 + 1, height - 1);

            float xDiff = x - x1;
            float yDiff = y - y1;
            float w11 = (1f - xDiff) * (1f - yDiff);
            float w21 = xDiff * (1f - yDiff);
            float w12 = (1f - xDiff) * yDiff;
            float w22 = xDiff * yDiff;

            Color c11 = srcLock.GetPixel(x1, y1);
            Color c21 = srcLock.GetPixel(x2, y1);
            Color c12 = srcLock.GetPixel(x1, y2);
            Color c22 = srcLock.GetPixel(x2, y2);

            return Color.FromArgb(
                Math.Clamp((int)(c11.A * w11 + c21.A * w21 + c12.A * w12 + c22.A * w22 + 0.5f), 0, 255),
                Math.Clamp((int)(c11.R * w11 + c21.R * w21 + c12.R * w12 + c22.R * w22 + 0.5f), 0, 255),
                Math.Clamp((int)(c11.G * w11 + c21.G * w21 + c12.G * w12 + c22.G * w22 + 0.5f), 0, 255),
                Math.Clamp((int)(c11.B * w11 + c21.B * w21 + c12.B * w12 + c22.B * w22 + 0.5f), 0, 255)
            );
        }
    }
}
