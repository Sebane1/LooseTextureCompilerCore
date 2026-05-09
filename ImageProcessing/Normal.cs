using System.Drawing;
using Color = System.Drawing.Color;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public class Normal {

        public static Bitmap Calculate(string file) {
            using (Bitmap image = (Bitmap)Bitmap.FromFile(file)) {
                int width = image.Width;
                int height = image.Height;
                Bitmap normal = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                LockBitmap source = new LockBitmap(image);
                LockBitmap destination = new LockBitmap(normal);
                source.LockBits();
                destination.LockBits();

                float[] brightnessMap = new float[width * height];
                byte[] srcPixels = source.Pixels;
                int srcStep = source.Depth / 8;

                System.Threading.Tasks.Parallel.For(0, height, y => {
                    int rowStart = y * width;
                    int pixelIndex = rowStart * srcStep;
                    for (int x = 0; x < width; x++) {
                        byte b = srcPixels[pixelIndex];
                        byte g = srcPixels[pixelIndex + 1];
                        byte r = srcPixels[pixelIndex + 2];
                        pixelIndex += srcStep;

                        float fr = r / 255f;
                        float fg = g / 255f;
                        float fb = b / 255f;

                        float max = fr;
                        if (fg > max) max = fg;
                        if (fb > max) max = fb;

                        float min = fr;
                        if (fg < min) min = fg;
                        if (fb < min) min = fb;

                        brightnessMap[rowStart + x] = (max + min) * 0.5f;
                    }
                });

                byte[] dstPixels = destination.Pixels;

                System.Threading.Tasks.Parallel.For(0, height, y => {
                    int rowStart = y * width;
                    int dstPixelIndex = rowStart * 4;

                    for (int x = 0; x < width; x++) {
                        float l = x > 0 ? brightnessMap[rowStart + x - 1] : brightnessMap[rowStart + x];
                        float r = x < width - 1 ? brightnessMap[rowStart + x + 1] : brightnessMap[rowStart + x];
                        float u = y > 0 ? brightnessMap[(y - 1) * width + x] : brightnessMap[rowStart + x];
                        float d = y < height - 1 ? brightnessMap[(y + 1) * width + x] : brightnessMap[rowStart + x];

                        float x_v = (((l - r) + 1f) * 0.5f) * 255f;
                        float y_v = (((u - d) + 1f) * 0.5f) * 255f;

                        int ix_v = (int)x_v;
                        int iy_v = (int)y_v;
                        ix_v = ix_v > 255 ? 255 : (ix_v < 0 ? 0 : ix_v);
                        iy_v = iy_v > 255 ? 255 : (iy_v < 0 ? 0 : iy_v);

                        dstPixels[dstPixelIndex++] = 255;
                        dstPixels[dstPixelIndex++] = (byte)iy_v;
                        dstPixels[dstPixelIndex++] = (byte)ix_v;
                        dstPixels[dstPixelIndex++] = 255;
                    }
                });

                destination.UnlockBits();
                source.UnlockBits();
                return normal;
            }
        }
        //Optimized

        public static Bitmap Calculate(Bitmap file, Bitmap normalMask = null) {
            Bitmap image = file;
            #region Global Variables
            int width = image.Width;
            int height = image.Height;
            image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            Bitmap normal = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            LockBitmap source = new LockBitmap(image);
            LockBitmap destination = new LockBitmap(normal);
            LockBitmap maskReference = null;
            Bitmap scaledMask = null;
            if (normalMask != null) {
                normalMask.RotateFlip(RotateFlipType.RotateNoneFlipX);
                scaledMask = new Bitmap(normalMask, width, height);
                maskReference = new LockBitmap(scaledMask);
                maskReference.LockBits();
            }
            source.LockBits();
            destination.LockBits();
            #endregion

            float[] brightnessMap = new float[width * height];
            byte[] srcPixels = source.Pixels;
            int srcStep = source.Depth / 8;

            System.Threading.Tasks.Parallel.For(0, height, y => {
                int rowStart = y * width;
                int pixelIndex = rowStart * srcStep;
                for (int x = 0; x < width; x++) {
                    byte b = srcPixels[pixelIndex];
                    byte g = srcPixels[pixelIndex + 1];
                    byte r = srcPixels[pixelIndex + 2];
                    pixelIndex += srcStep;

                    float fr = r / 255f;
                    float fg = g / 255f;
                    float fb = b / 255f;

                    float max = fr;
                    if (fg > max) max = fg;
                    if (fb > max) max = fb;

                    float min = fr;
                    if (fg < min) min = fg;
                    if (fb < min) min = fb;

                    brightnessMap[rowStart + x] = (max + min) * 0.5f;
                }
            });

            byte[] dstPixels = destination.Pixels;
            byte[] maskPixels = maskReference?.Pixels;
            int maskStep = maskReference != null ? maskReference.Depth / 8 : 0;

            System.Threading.Tasks.Parallel.For(0, height, y => {
                int rowStart = y * width;
                int dstPixelIndex = rowStart * 4;
                int maskPixelIndex = rowStart * maskStep;

                for (int x = 0; x < width; x++) {
                    bool masked = false;
                    if (maskPixels != null) {
                        if (maskStep == 4) {
                            masked = maskPixels[maskPixelIndex + 3] == 0;
                        } else {
                            masked = false;
                        }
                        maskPixelIndex += maskStep;
                    }

                    if (normalMask == null || masked) {
                        float l = x > 0 ? brightnessMap[rowStart + x - 1] : brightnessMap[rowStart + x];
                        float r = x < width - 1 ? brightnessMap[rowStart + x + 1] : brightnessMap[rowStart + x];
                        float u = y > 0 ? brightnessMap[(y - 1) * width + x] : brightnessMap[rowStart + x];
                        float d = y < height - 1 ? brightnessMap[(y + 1) * width + x] : brightnessMap[rowStart + x];

                        float x_v = (((l - r) + 1f) * 0.5f) * 255f;
                        float y_v = (((u - d) + 1f) * 0.5f) * 255f;

                        int ix_v = (int)x_v;
                        int iy_v = (int)y_v;
                        ix_v = ix_v > 255 ? 255 : (ix_v < 0 ? 0 : ix_v);
                        iy_v = iy_v > 255 ? 255 : (iy_v < 0 ? 0 : iy_v);

                        dstPixels[dstPixelIndex++] = 255;
                        dstPixels[dstPixelIndex++] = (byte)iy_v;
                        dstPixels[dstPixelIndex++] = (byte)ix_v;
                        dstPixels[dstPixelIndex++] = 255;
                    } else {
                        dstPixels[dstPixelIndex++] = 255;
                        dstPixels[dstPixelIndex++] = 127;
                        dstPixels[dstPixelIndex++] = 128;
                        dstPixels[dstPixelIndex++] = 255;
                    }
                }
            });

            destination.UnlockBits();
            source.UnlockBits();
            maskReference?.UnlockBits();
            scaledMask?.Dispose();
            
            normal.RotateFlip(RotateFlipType.RotateNoneFlipX);
            image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            
            KVImage.ImageBlender imageBlender = new KVImage.ImageBlender();
            Bitmap result;
            using (Bitmap normal1 = new Bitmap(normal)) {
                using (Bitmap normal2 = new Bitmap(normal)) {
                    using (Bitmap contrasted = Contrast.AdjustContrast(normal2, 120)) {
                        using (Bitmap blended = imageBlender.BlendImages(normal1, 0, 0, normal1.Width, normal1.Height, contrasted, 0, 0, KVImage.ImageBlender.BlendOperation.Blend_Overlay)) {
                            using (Bitmap extAlpha = ImageManipulation.ExtractAlpha(image)) {
                                result = ImageManipulation.MergeAlphaToRGB(extAlpha, blended);
                            }
                        }
                    }
                }
            }
            normal.Dispose();
            return result;
        }
    }
}
