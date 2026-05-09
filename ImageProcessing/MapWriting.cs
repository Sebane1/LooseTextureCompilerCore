using System.Drawing;
using Bitmap = System.Drawing;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public class MapWriting {
        public static Bitmap.Bitmap CalculateBase(Bitmap.Bitmap file, Bitmap.Bitmap glow) {
            Bitmap.Bitmap baseTexture = new Bitmap.Bitmap(file);
            using (Bitmap.Bitmap image = new Bitmap.Bitmap(glow, file.Width, file.Height)) {
                using (Bitmap.Bitmap mergedImage = new Bitmap.Bitmap(baseTexture)) {
                    using (Bitmap.Bitmap glowMultiply = new Bitmap.Bitmap(mergedImage)) {
                        using (Graphics g = Graphics.FromImage(glowMultiply)) {
                            g.Clear(Bitmap.Color.White);
                            g.DrawImage(glow, 0, 0, glow.Width, glow.Height);
                        }
                        new KVImage.ImageBlender().BlendImages(mergedImage, glowMultiply, KVImage.ImageBlender.BlendOperation.Blend_Multiply);

                        LockBitmap source = new LockBitmap(image);
                        LockBitmap destination = new LockBitmap(baseTexture);
                        LockBitmap mergedImagePixels = new LockBitmap(mergedImage);

                        source.LockBits();
                        destination.LockBits();
                        mergedImagePixels.LockBits();
                        byte[] srcPixels = source.Pixels;
                        byte[] dstPixels = destination.Pixels;
                        byte[] mergedPixels = mergedImagePixels.Pixels;
                        int srcStep = source.Depth / 8;
                        int dstStep = destination.Depth / 8;
                        int mergedStep = mergedImagePixels.Depth / 8;
                        int width = image.Width;
                        int height = image.Height;

                        System.Threading.Tasks.Parallel.For(0, height, y => {
                            int rowStartSrc = y * width * srcStep;
                            int rowStartDst = y * width * dstStep;
                            int rowStartMerged = y * width * mergedStep;

                            for (int x = 0; x < width; x++) {
                                int srcIndex = rowStartSrc + (x * srcStep);
                                int dstIndex = rowStartDst + (x * dstStep);
                                int mergedIndex = rowStartMerged + (x * mergedStep);

                                byte srcB = srcPixels[srcIndex];
                                byte srcG = srcPixels[srcIndex + 1];
                                byte srcR = srcPixels[srcIndex + 2];
                                byte srcA = srcStep == 4 ? srcPixels[srcIndex + 3] : (byte)255;

                                byte mergedB = mergedPixels[mergedIndex];
                                byte mergedG = mergedPixels[mergedIndex + 1];
                                byte mergedR = mergedPixels[mergedIndex + 2];
                                byte mergedA = mergedStep == 4 ? mergedPixels[mergedIndex + 3] : (byte)255;

                                byte compR = srcR > 90 ? srcR : (byte)0;
                                byte compG = srcG > 90 ? srcG : (byte)0;
                                byte compB = srcB > 90 ? srcB : (byte)0;

                                if (!(compR == 0 && compG == 0 && compB == 0)) {
                                    if (srcA > 20) {
                                        dstPixels[dstIndex] = srcB;
                                        dstPixels[dstIndex + 1] = srcG;
                                        dstPixels[dstIndex + 2] = srcR;
                                        if (dstStep == 4) dstPixels[dstIndex + 3] = (byte)(255 - srcA);
                                    } else if (srcA > 10) {
                                        dstPixels[dstIndex] = mergedB;
                                        dstPixels[dstIndex + 1] = mergedG;
                                        dstPixels[dstIndex + 2] = mergedR;
                                        if (dstStep == 4) dstPixels[dstIndex + 3] = (byte)(255 - srcA);
                                    } else if (srcA > 0) {
                                        dstPixels[dstIndex] = mergedB;
                                        dstPixels[dstIndex + 1] = mergedG;
                                        dstPixels[dstIndex + 2] = mergedR;
                                        if (dstStep == 4) dstPixels[dstIndex + 3] = mergedA;
                                    }
                                } else {
                                    dstPixels[dstIndex] = mergedB;
                                    dstPixels[dstIndex + 1] = mergedG;
                                    dstPixels[dstIndex + 2] = mergedR;
                                    if (dstStep == 4) dstPixels[dstIndex + 3] = mergedA;
                                }
                            }
                        });
                        destination.UnlockBits();
                        source.UnlockBits();
                        mergedImagePixels.UnlockBits();
                    }
                }
            }
            return baseTexture;
        }
        public static Bitmap.Color FlattenColours(Bitmap.Color colour, int minBrightness = 90) {
            return Bitmap.Color.FromArgb(colour.A,
                colour.R > minBrightness ? colour.R : 0,
                colour.G > minBrightness ? colour.G : 0,
                colour.B > minBrightness ? colour.B : 0);
        }
        public static Bitmap.Bitmap CalculateEyeMulti(Bitmap.Bitmap file, Bitmap.Bitmap glow) {
            Bitmap.Bitmap multi = new Bitmap.Bitmap(file);
            using (Bitmap.Bitmap image = new Bitmap.Bitmap(glow, file.Width, file.Height)) {
                LockBitmap source = new LockBitmap(image);
                LockBitmap destination = new LockBitmap(multi);
                source.LockBits();
                destination.LockBits();
                System.Threading.Tasks.Parallel.For(0, image.Height, y => {
                    for (int x = 0; x < image.Width; x++) {
                        Bitmap.Color sourcePixel = source.GetPixel(x, y);
                        Bitmap.Color destinationPixel = destination.GetPixel(x, y);
                        if (sourcePixel.A > 0) {
                            Bitmap.Color col = Bitmap.Color.FromArgb(255 - sourcePixel.A,
                                destinationPixel.R, destinationPixel.G, destinationPixel.B);
                            destination.SetPixel(x, y, col);
                        }
                    }
                });
                destination.UnlockBits();
                source.UnlockBits();
            }
            return multi;
        }

        public static Bitmap.Bitmap CalculateMulti(Bitmap.Bitmap file, Bitmap.Bitmap glow) {
            Bitmap.Bitmap multi = new Bitmap.Bitmap(file);
            using (Bitmap.Bitmap image = new Bitmap.Bitmap(glow, file.Width, file.Height)) {
                LockBitmap source = new LockBitmap(image);
                LockBitmap destination = new LockBitmap(multi);
                source.LockBits();
                destination.LockBits();
                System.Threading.Tasks.Parallel.For(0, image.Height, y => {
                    for (int x = 0; x < image.Width; x++) {
                        Bitmap.Color sourcePixel = source.GetPixel(x, y);
                        Bitmap.Color destinationPixel = destination.GetPixel(x, y);
                        if (sourcePixel.A > 20) {
                            Bitmap.Color col = Bitmap.Color.FromArgb(destinationPixel.A,
                                destinationPixel.R,
                                destinationPixel.G,
                                sourcePixel.A);
                            destination.SetPixel(x, y, col);
                        }
                    }
                });
                destination.UnlockBits();
                source.UnlockBits();
            }
            return multi;
        }
        public static Bitmap.Bitmap CalculateLegacyAtraMulti(Bitmap.Bitmap file, Bitmap.Bitmap glow) {
            Bitmap.Bitmap multi = new Bitmap.Bitmap(file);
            using (Bitmap.Bitmap image = new Bitmap.Bitmap(glow, file.Width, file.Height)) {
                LockBitmap source = new LockBitmap(image);
                LockBitmap destination = new LockBitmap(multi);
                source.LockBits();
                destination.LockBits();
                System.Threading.Tasks.Parallel.For(0, image.Height, y => {
                    for (int x = 0; x < image.Width; x++) {
                        Bitmap.Color sourcePixel = source.GetPixel(x, y);
                        Bitmap.Color destinationPixel = destination.GetPixel(x, y);
                        Bitmap.Color comparisonColour = FlattenColours(sourcePixel, 90);
                        if (!(comparisonColour.R == 0 && comparisonColour.G == 0 && comparisonColour.B == 0)) {
                            if (sourcePixel.A > 20) {
                                Bitmap.Color col = Bitmap.Color.FromArgb(destinationPixel.A,
                                    destinationPixel.R,
                                    destinationPixel.G,
                                    255 - sourcePixel.A);
                                destination.SetPixel(x, y, col);
                            }
                        }
                    }
                });
                destination.UnlockBits();
                source.UnlockBits();
            }
            return multi;
        }

        public static Bitmap.Bitmap TransplantData(Bitmap.Bitmap file, Bitmap.Bitmap glow) {
            Bitmap.Bitmap image = glow;
            Bitmap.Bitmap baseTexture = new Bitmap.Bitmap(file);
            using (Bitmap.Bitmap mergedImage = new Bitmap.Bitmap(baseTexture)) {
                using (Bitmap.Bitmap glowMultiply = new Bitmap.Bitmap(mergedImage)) {
                    using (Graphics g = Graphics.FromImage(glowMultiply)) {
                        g.Clear(Bitmap.Color.White);
                        g.DrawImage(glow, 0, 0, glow.Width, glow.Height);
                    }
                    new KVImage.ImageBlender().BlendImages(mergedImage, glowMultiply, KVImage.ImageBlender.BlendOperation.Blend_Multiply);

                    LockBitmap source = new LockBitmap(image);
                    LockBitmap destination = new LockBitmap(baseTexture);
                    LockBitmap mergedImagePixels = new LockBitmap(mergedImage);
                    source.LockBits();
                    destination.LockBits();
                    mergedImagePixels.LockBits();
                    if (file.Width == glow.Width && file.Height == glow.Height) {
                        System.Threading.Tasks.Parallel.For(0, image.Height, y => {
                            for (int x = 0; x < image.Width; x++) {
                                Bitmap.Color sourcePixel = source.GetPixel(x, y);
                                Bitmap.Color mergedPixel = mergedImagePixels.GetPixel(x, y);
                                if (sourcePixel.A > 0) {
                                    Bitmap.Color col = Bitmap.Color.FromArgb(sourcePixel.A, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                                    destination.SetPixel(x, y, col);
                                }
                            }
                        });
                    }
                    destination.UnlockBits();
                    source.UnlockBits();
                    mergedImagePixels.UnlockBits();
                }
            }
            return baseTexture;
        }

        public static Bitmap.Bitmap ExtractGlowMapFromBase(Bitmap.Bitmap file) {
            Bitmap.Bitmap image = new Bitmap.Bitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            System.Threading.Tasks.Parallel.For(0, image.Height, y => {
                for (int x = 0; x < image.Width; x++) {
                    Bitmap.Color sourcePixel = source.GetPixel(x, y);
                    Bitmap.Color col = Bitmap.Color.FromArgb(255 - sourcePixel.A, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                    source.SetPixel(x, y, col);
                }
            });
            source.UnlockBits();
            return image;
        }

        byte Calc(byte c1, byte c2) {
            var cr = c1 / 255d * c2 / 255d * 255d;
            return (byte)(cr > 255 ? 255 : cr);
        }
    }
}
