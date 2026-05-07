using System.Drawing;
using Color = System.Drawing.Color;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public class Normal {

        public static Bitmap Calculate(string file) {
            using (Bitmap image = (Bitmap)Bitmap.FromFile(file)) {
                int w = image.Width - 1;
                int h = image.Height - 1;
                float sample_l;
                float sample_r;
                float sample_u;
                float sample_d;
                float x_vector;
                float y_vector;
                Bitmap normal = new Bitmap(image.Width, image.Height);
                LockBitmap source = new LockBitmap(image);
                LockBitmap destination = new LockBitmap(normal);
                source.LockBits();
                destination.LockBits();
                float brightness_difference = 255 * 0.5f;
                System.Threading.Tasks.Parallel.For(0, h, y => {
                    for (int x = 0; x < w; x++) {
                        float l = x > 0 ? source.GetPixel(x - 1, y).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float r = x < w ? source.GetPixel(x + 1, y).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float u = y > 0 ? source.GetPixel(x, y - 1).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float d = y < h ? source.GetPixel(x, y + 1).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float x_v = (((l - r) + 1) * brightness_difference);
                        float y_v = (((u - d) + 1) * brightness_difference);
                        Color col = Color.FromArgb(255, (int)x_v, (int)y_v, 255);
                        destination.SetPixel(x, y, col);
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
            int w = image.Width - 1;
            int h = image.Height - 1;
            float sample_l;
            float sample_r;
            float sample_u;
            float sample_d;
            float x_vector;
            float y_vector;
            image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            Bitmap normal = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            LockBitmap source = new LockBitmap(image);
            LockBitmap destination = new LockBitmap(normal);
            LockBitmap maskReference = null;
            if (normalMask != null) {
                normalMask.RotateFlip(RotateFlipType.RotateNoneFlipX);
                maskReference = new LockBitmap(new Bitmap(normalMask, image.Width, image.Height));
                maskReference.LockBits();
            }
            source.LockBits();
            destination.LockBits();
            #endregion
            System.Threading.Tasks.Parallel.For(0, h + 1, y => {
                for (int x = 0; x < w + 1; x++) {
                    Color originalPixel = source.GetPixel(x, y);
                    if (normalMask == null || maskReference?.GetPixel(x, y).A == 0) {
                        float l = x > 0 ? source.GetPixel(x - 1, y).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float r = x < w ? source.GetPixel(x + 1, y).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float u = y > 0 ? source.GetPixel(x, y - 1).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float d = y < h ? source.GetPixel(x, y + 1).GetBrightness() : source.GetPixel(x, y).GetBrightness();
                        float x_v = (((l - r) + 1) * .5f) * 255;
                        float y_v = (((u - d) + 1) * .5f) * 255;
                        Color col = Color.FromArgb(255, (int)x_v, (int)y_v, 255);
                        destination.SetPixel(x, y, col);
                    } else {
                        destination.SetPixel(x, y, Color.FromArgb(255, 128, 127, 255));
                    }
                }
            });
            destination.UnlockBits();
            source.UnlockBits();
            maskReference?.UnlockBits();
            normal.RotateFlip(RotateFlipType.RotateNoneFlipX);
            Bitmap normal1 = new Bitmap(normal);
            Bitmap normal2 = new Bitmap(normal);
            KVImage.ImageBlender imageBlender = new KVImage.ImageBlender();
            image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            return ImageManipulation.MergeAlphaToRGB(ImageManipulation.ExtractAlpha(image), imageBlender.BlendImages(normal1, 0, 0, normal1.Width, normal1.Height,
                Contrast.AdjustContrast(normal2, 120), 0, 0, KVImage.ImageBlender.BlendOperation.Blend_Overlay));
        }
    }
}
