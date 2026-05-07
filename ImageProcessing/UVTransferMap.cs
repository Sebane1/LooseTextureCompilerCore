using System;
using System.Drawing;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;
using Image = SixLabors.ImageSharp.Image;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public class UVTransferMap {
        
        /// <summary>
        /// Generates a perfect 16-bit Identity Map where Red = X and Green = Y.
        /// Feed this image into XNormal's base texture slot to bake the coordinate transformation.
        /// </summary>
        public static void GenerateCoordinateMap(int width, int height, string outputPath) {
            using (var image = new Image<Rgba64>(width, height)) {
                image.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++) {
                        Span<Rgba64> pixelRow = accessor.GetRowSpan(y);
                        
                        // Y coordinate mapped exactly to a 16-bit integer scale (0 - 65535)
                        ushort g = (ushort)Math.Round((y / (float)(height - 1)) * 65535.0f);
                        
                        for (int x = 0; x < accessor.Width; x++) {
                            // X coordinate mapped exactly to a 16-bit integer scale (0 - 65535)
                            ushort r = (ushort)Math.Round((x / (float)(width - 1)) * 65535.0f);
                            
                            // Write out the raw coordinate map
                            pixelRow[x] = new Rgba64(r, g, 0, 65535);
                        }
                    }
                });
                
                // Save it out as a 16-bit PNG (which XNormal perfectly supports and interpolates smoothly)
                image.Save(outputPath);
            }
        }

        /// <summary>
        /// Applies the XNormal-baked Transfer Map to a source texture.
        /// Completely bypasses XNormal and maps pixels instantly in C#.
        /// </summary>
        public static Bitmap ApplyTransferMap(Bitmap sourceTexture, string transferMapPath, bool useBilinear = true) {
            // Load the 16-bit output from XNormal via ImageSharp. 
            // IMPORTANT: We cannot use System.Drawing.Bitmap here because it automatically downgrades 16-bit PNGs to 8-bit, destroying our precision!
            using (var transferImage = Image.Load<Rgba64>(transferMapPath)) {
                int destWidth = transferImage.Width;
                int destHeight = transferImage.Height;
                
                int srcWidth = sourceTexture.Width;
                int srcHeight = sourceTexture.Height;

                Bitmap result = new Bitmap(destWidth, destHeight);
                LockBitmap destLock = new LockBitmap(result);
                LockBitmap srcLock = new LockBitmap(sourceTexture);
                
                destLock.LockBits();
                srcLock.LockBits();

                // Process single-threaded because Span<T> cannot be captured in a Parallel.For lambda.
                // It is still blisteringly fast (milliseconds).
                transferImage.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++) {
                        Span<Rgba64> rowSpan = accessor.GetRowSpan(y);
                        
                        for (int x = 0; x < accessor.Width; x++) {
                            Rgba64 pixel = rowSpan[x];
                            
                            // If XNormal marked this pixel as totally transparent (no ray hit / empty space), we skip it
                            if (pixel.A < 100) {
                                continue;
                            }
                            
                            // Decode X and Y floats perfectly from the 16-bit channels
                            float srcX_float = (pixel.R / 65535.0f) * (srcWidth - 1);
                            float srcY_float = (pixel.G / 65535.0f) * (srcHeight - 1);
                            
                            if (useBilinear) {
                                Color sampledColor = BilinearSample(srcLock, srcX_float, srcY_float, srcWidth, srcHeight);
                                destLock.SetPixel(x, y, sampledColor);
                            } else {
                                int srcX = (int)Math.Round(srcX_float);
                                int srcY = (int)Math.Round(srcY_float);
                                
                                srcX = Math.Max(0, Math.Min(srcX, srcWidth - 1));
                                srcY = Math.Max(0, Math.Min(srcY, srcHeight - 1));
                                
                                Color sampledColor = srcLock.GetPixel(srcX, srcY);
                                destLock.SetPixel(x, y, sampledColor);
                            }
                        }
                    }
                });

                destLock.UnlockBits();
                srcLock.UnlockBits();
                
                return result;
            }
        }

        private static Color BilinearSample(LockBitmap srcLock, float x, float y, int width, int height) {
            int x1 = (int)Math.Floor(x);
            int y1 = (int)Math.Floor(y);
            int x2 = Math.Min(x1 + 1, width - 1);
            int y2 = Math.Min(y1 + 1, height - 1);

            float xDiff = x - x1;
            float yDiff = y - y1;

            Color c11 = srcLock.GetPixel(x1, y1);
            Color c12 = srcLock.GetPixel(x1, y2);
            Color c21 = srcLock.GetPixel(x2, y1);
            Color c22 = srcLock.GetPixel(x2, y2);

            float a = c11.A * (1 - xDiff) * (1 - yDiff) + c21.A * xDiff * (1 - yDiff) + c12.A * (1 - xDiff) * yDiff + c22.A * xDiff * yDiff;
            float r = c11.R * (1 - xDiff) * (1 - yDiff) + c21.R * xDiff * (1 - yDiff) + c12.R * (1 - xDiff) * yDiff + c22.R * xDiff * yDiff;
            float g = c11.G * (1 - xDiff) * (1 - yDiff) + c21.G * xDiff * (1 - yDiff) + c12.G * (1 - xDiff) * yDiff + c22.G * xDiff * yDiff;
            float b = c11.B * (1 - xDiff) * (1 - yDiff) + c21.B * xDiff * (1 - yDiff) + c12.B * (1 - xDiff) * yDiff + c22.B * xDiff * yDiff;

            return Color.FromArgb(
                (int)Math.Max(0, Math.Min(255, a)), 
                (int)Math.Max(0, Math.Min(255, r)), 
                (int)Math.Max(0, Math.Min(255, g)), 
                (int)Math.Max(0, Math.Min(255, b))
            );
        }
    }
}
