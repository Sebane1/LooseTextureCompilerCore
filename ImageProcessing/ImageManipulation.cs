﻿using Lumina.Data.Files;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public class ImageManipulation {

        public static Bitmap BoostAboveThreshold(Bitmap file, int threshhold) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(sourcePixel.A,
                        FlattenToThreshold(sourcePixel.R, threshhold),
                        FlattenToThreshold(sourcePixel.G, threshhold),
                        FlattenToThreshold(sourcePixel.B, threshhold));
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }

        public static void BulkDDSToPng(IEnumerable<string> paths) {
            foreach (string path in paths) {
                TexIO.ResolveBitmap(path).Save(ImageManipulation.ReplaceExtension(path, ".png"), ImageFormat.Png);
            }
        }

        private static int FlattenToThreshold(float colourValue, float threshhold) {
            float nextPixel = ((colourValue / 255f) * (255 - threshhold)) + threshhold;
            if (nextPixel > 255f) {
                nextPixel = (nextPixel - 255f) + threshhold;
            }
            return (int)nextPixel;
        }

        public static Bitmap SanitizeArtifacts(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    if (sourcePixel.A < 255) {
                        Color col = Color.FromArgb(0, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                        source.SetPixel(x, y, col);
                    }
                }
            };
            source.UnlockBits();
            return image;
        }
        public static Bitmap Resize(Bitmap file, int width, int height) {
            return TexIO.Resize(file, width, height);
        }
        public static Bitmap CutInHalf(Bitmap file) {
            return file.Clone(new Rectangle(file.Width / 2, 0, file.Width / 2, file.Height), PixelFormat.Format32bppArgb);
        }

        public static Bitmap InvertImage(Bitmap file) {
            Bitmap invertedImage = TexIO.NewBitmap(file);
            using (LockBitmap invertedBits = new LockBitmap(invertedImage)) {
                for (int y = 0; y < invertedBits.Height; y++) {
                    for (int x = 0; x < invertedBits.Width; x++) {
                        Color invertedPixel = invertedBits.GetPixel(x, y);
                        invertedPixel = Color.FromArgb(invertedPixel.A, (255 - invertedPixel.R), (255 - invertedPixel.G), (255 - invertedPixel.B));
                        invertedBits.SetPixel(x, y, invertedPixel);
                    }
                }
            }
            return invertedImage;
        }

        public static Bitmap ResizeAndMerge(Bitmap target, Bitmap source) {
            Bitmap image = new Bitmap(target);
            Graphics g = Graphics.FromImage(image);
            g.DrawImage(source, 0, 0, target.Width, target.Height);
            return image;
        }
        public static Bitmap ExtractTransparency(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255, sourcePixel.A, sourcePixel.A, sourcePixel.A);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }
        public static Bitmap ReplaceBlackWithBackingImage(Bitmap file, Bitmap backingImage) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            Bitmap backingImageCopy = new Bitmap(backingImage);
            LockBitmap backingImageSource = new LockBitmap(backingImageCopy);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color backingPixel = backingImageSource.GetPixel(x, y);
                    if (sourcePixel.A < 200) {
                        Color col = Color.FromArgb(sourcePixel.A, backingPixel.R, backingPixel.G, backingPixel.B);
                        source.SetPixel(x, y, col);
                    }
                }
            };
            source.UnlockBits();
            return image;
        }
        public static Bitmap ExtractRGB(Bitmap file, bool isNormal = false) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                    if (isNormal) {
                        if (sourcePixel.R == 0 && sourcePixel.G == 0 & sourcePixel.B == 0) {
                            col = Color.FromArgb(255, 127, 128, 255);
                        }
                    }
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }
        public static Bitmap ExtractRed(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255, sourcePixel.R, sourcePixel.R, sourcePixel.R);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }
        public static Bitmap ExtractGreen(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255, sourcePixel.G, sourcePixel.G, sourcePixel.G);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }

        public static Bitmap GenerateFaceMulti(Bitmap file, bool asym) {
            Bitmap image = new Bitmap(Grayscale.MakeGrayscale(file));
            LockBitmap source = new LockBitmap(image);
            bool isEqualWidthAndHeight = file.Width == file.Height;
            float lipAreaWidth = (isEqualWidthAndHeight ? 0.125f : 0.25f) * (float)file.Width;
            Rectangle rectangle = new Rectangle(
                (int)(asym ? ((file.Width / 2) - (lipAreaWidth)) : 0),
                (int)(0.6f * (float)file.Height),
                (int)(lipAreaWidth * (asym ? 2 : 1)),
                (int)(0.8f * (float)file.Height));
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    bool insideRectangle = rectangle.Contains(x, y);
                    Color col = insideRectangle ?
                        Color.FromArgb(255,
                        Math.Clamp(sourcePixel.G < 135 ? 255 : sourcePixel.G + 100, 0, 255),

                        Math.Clamp(sourcePixel.G < 135 ? 180 : 126, 0, 255),

                        Math.Clamp(sourcePixel.G < 135 ? 255 : (sourcePixel.G < 20 ? sourcePixel.G : 0), 0, 255))
                        :

                        Color.FromArgb(255,

                        Math.Clamp(sourcePixel.G < 40 ? 130 : sourcePixel.G + 100, 0, 255),

                        Math.Clamp((sourcePixel.G < 40 && sourcePixel.G > 20 ? sourcePixel.G + 120
                        : (sourcePixel.G < 20 ? sourcePixel.G : 126)), 0, 255),
                        0);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }
        public static Bitmap GenerateSkinMulti(Bitmap file) {
            Bitmap image = new Bitmap(Grayscale.MakeGrayscale(file));
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255,
                        Math.Clamp(sourcePixel.G < 40 ? 130 : sourcePixel.G + 100, 0, 255),
                        Math.Clamp(sourcePixel.G < 40 && sourcePixel.G > 20 ? sourcePixel.G + 120 :
                        (sourcePixel.G < 20 ? sourcePixel.G : 126), 0, 255), 0);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }

        public static Bitmap ExtractBlue(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255, sourcePixel.B, sourcePixel.B, sourcePixel.B);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }

        public static Bitmap LipCorrection(Bitmap correctionMap, Bitmap file, bool blurResult) {
            Bitmap image = TexIO.NewBitmap(file);
            Bitmap correctionImage = new Bitmap(correctionMap, (int)((float)image.Height * 0.5f), image.Height);
            LockBitmap source = new LockBitmap(image);
            LockBitmap correctionSource = new LockBitmap(correctionImage);
            Color[] xColors = new Color[correctionMap.Width];
            source.LockBits();
            correctionSource.LockBits();
            Color correctionColor = Color.White;
            for (int y = 0; y < correctionImage.Height; y++) {
                for (int x = 0; x < correctionImage.Width; x++) {
                    Color correctionPixel = correctionSource.GetPixel(x, y);
                    Color sourcePixel = source.GetPixel(x, y);
                    if (correctionPixel.B == 0 && correctionPixel.R == 0 && correctionPixel.G == 255 && correctionPixel.A == 255) {
                        correctionColor = Color.FromArgb(sourcePixel.A, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                        correctionSource.SetPixel(x, y, xColors[x]);
                    }
                    if (correctionPixel.B == 255 && correctionPixel.R == 0 && correctionPixel.G == 0 && correctionPixel.A == 255) {
                        xColors[x] = Color.FromArgb(sourcePixel.A, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                        correctionSource.SetPixel(x, y, xColors[x]);
                    } else if (correctionPixel.R == 255 && correctionPixel.G == 0 && correctionPixel.G == 0 && correctionPixel.A == 255) {
                        correctionSource.SetPixel(x, y, x > 165 ? xColors[x] : correctionColor);
                    } else {
                        correctionSource.SetPixel(x, y, Color.FromArgb(0, sourcePixel.R, sourcePixel.G, sourcePixel.B));
                    }
                }
            };
            source.UnlockBits();
            correctionSource.UnlockBits();
            if (blurResult) {
                var blur = new GaussianBlur(correctionImage);
                var blurredLips = blur.Process(12);
                return blurredLips;
            } else {
                return correctionImage;
            }
        }
        public static Bitmap EyeCorrection(Bitmap correctionMap, Bitmap file, bool blurResult) {
            Bitmap image = TexIO.NewBitmap(file);
            Bitmap correctionImage = new Bitmap(correctionMap, (int)((float)image.Height * 0.5f), image.Height);
            LockBitmap source = new LockBitmap(image);
            LockBitmap correctionSource = new LockBitmap(correctionImage);
            Color[] xColors = new Color[correctionMap.Width];
            source.LockBits();
            correctionSource.LockBits();
            Color correctionColor = Color.White;
            for (int y = correctionImage.Height - 1; y > 0; y--) {
                for (int x = correctionImage.Width - 1; x > 0; x--) {
                    Color correctionPixel = correctionSource.GetPixel(x, y);
                    Color sourcePixel = source.GetPixel(x, y);
                    if (correctionPixel.B == 0 && correctionPixel.R == 0 && correctionPixel.G == 255 && correctionPixel.A == 255) {
                        correctionColor = Color.FromArgb(sourcePixel.A, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                        correctionSource.SetPixel(x, y, xColors[x]);
                    }
                    if (correctionPixel.B == 255 && correctionPixel.R == 0 && correctionPixel.G == 0 && correctionPixel.A == 255) {
                        xColors[x] = Color.FromArgb(sourcePixel.A, sourcePixel.R, sourcePixel.G, sourcePixel.B);
                        correctionSource.SetPixel(x, y, xColors[x]);
                    } else if (correctionPixel.R == 255 && correctionPixel.G == 0 && correctionPixel.G == 0 && correctionPixel.A == 255) {
                        correctionSource.SetPixel(x, y, x > 165 ? xColors[x] : correctionColor);
                    } else {
                        correctionSource.SetPixel(x, y, Color.FromArgb(0, sourcePixel.R, sourcePixel.G, sourcePixel.B));
                    }
                }
            };
            source.UnlockBits();
            correctionSource.UnlockBits();
            if (blurResult) {
                var blur = new GaussianBlur(correctionImage);
                var blurredEye = blur.Process(3);
                return blurredEye;
            } else {
                return correctionImage;
            }
        }

        public static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize) {
            Bitmap blurred = new Bitmap(image.Width, image.Height);

            // make an exact copy of the bitmap provided
            using (Graphics graphics = Graphics.FromImage(blurred)) {
                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            }
            // look at every pixel in the blur rectangle
            for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++) {
                for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++) {
                    int avgA = 0, avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (int x = xx; (x < xx + blurSize && x < image.Width); x++) {
                        for (int y = yy; (y < yy + blurSize && y < image.Height); y++) {
                            Color pixel = image.GetPixel(x, y);
                            avgA += pixel.A;
                            avgR += pixel.R;
                            avgG += pixel.G;
                            avgB += pixel.B;

                            blurPixelCount++;
                        }
                    }
                    avgA = avgA / blurPixelCount;
                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (int x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++) {
                        for (int y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++) {
                            blurred.SetPixel(x, y, Color.FromArgb(avgA, avgR, avgG, avgB));
                        }
                    }
                }
            }

            return blurred;
        }

        public static Bitmap ExtractAlpha(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(255, sourcePixel.A, sourcePixel.A, sourcePixel.A);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }

        public static Bitmap GenerateXNormalTranslationMap() {
            Bitmap image = new Bitmap(4096, 4096);
            using (LockBitmap bitmap = new LockBitmap(image)) {
                int i = int.MinValue;
                for (int x = 0; x < bitmap.Width; x++) {
                    for (int y = 0; y < bitmap.Height; y++) {
                        // Set to some colour
                        Color color = Color.FromArgb(i);
                        color = Color.FromArgb(255, color.R, color.G, color.B);
                        bitmap.SetPixel(x, y, color);
                        i++;
                    }
                }
            }
            return image;
        }

        public static Bitmap BitmapToEyeMulti(Bitmap image, string baseDirectory = null) {
            string gloss = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\gloss.png");
            string template = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\template.png");
            Bitmap canvas = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            Bitmap newEye = Brightness.BrightenImage(Grayscale.MakeGrayscale(image), 1.0f, 1.1f, 1);

            Graphics graphics = Graphics.FromImage(canvas);
            graphics.Clear(Color.Black);
            Bitmap white = new Bitmap(image.Width, image.Height);
            graphics = Graphics.FromImage(white);
            graphics.Clear(Color.White);

            graphics = Graphics.FromImage(canvas);
            graphics.DrawImage(new Bitmap(newEye), 0, 0, image.Width, image.Height);
            graphics.DrawImage(new Bitmap(template), 0, 0, image.Width, image.Height);

            return MergeGrayscalesToRGBA(canvas, new Bitmap(new Bitmap(gloss), image.Width, image.Height), white, new Bitmap(white));
        }

        public static Bitmap BitmapToEyeMultiDawntrail(Bitmap image, string baseDirectory = null) {
            int enforcedSize = 2048;
            string template = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\multi.png");
            Bitmap canvas = new Bitmap(enforcedSize, enforcedSize, PixelFormat.Format32bppArgb);
            Bitmap newEye = Brightness.BrightenImage(Grayscale.MakeGrayscale(image), 1.0f, 1.1f, 1);

            Graphics graphics = Graphics.FromImage(canvas);
            graphics.Clear(Color.Black);
            Bitmap white = new Bitmap(enforcedSize, enforcedSize);
            Bitmap black = new Bitmap(enforcedSize, enforcedSize);
            graphics = Graphics.FromImage(white);
            graphics.Clear(Color.White);
            graphics = Graphics.FromImage(black);
            graphics.Clear(Color.Black);
            var bitmapTemplate = new Bitmap(template);
            graphics = Graphics.FromImage(canvas);
            var mergedImage = MergeGrayscalesToRGBA(new Bitmap(newEye), new Bitmap(black, image.Width, image.Height),
                new Bitmap(white, image.Width, image.Height), new Bitmap(white, image.Width, image.Height));
            graphics.DrawImage(mergedImage,

               (enforcedSize / 2) - (((float)enforcedSize * 0.4096f) / 2), (enforcedSize / 2) - (((float)enforcedSize * 0.4096f) / 2),
                (float)enforcedSize * 0.4096f, (float)enforcedSize * 0.4096f);
            graphics.DrawImage(bitmapTemplate, 0, 0, enforcedSize, enforcedSize);

            return MergeGrayscalesToRGBA(new Bitmap(canvas), new Bitmap(new Bitmap(canvas), enforcedSize, enforcedSize),
                ImageManipulation.InvertImage(ExtractAlpha(new Bitmap(bitmapTemplate, enforcedSize, enforcedSize))), new Bitmap(white));
        }

        public static Bitmap BitmapToEyeDiffuseDawntrail(Bitmap image, string baseDirectory = null) {
            int enforcedSize = 2048;
            string template = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\diffuse.png");
            Bitmap canvas = new Bitmap(enforcedSize, enforcedSize, PixelFormat.Format32bppArgb);
            Bitmap newEye = Brightness.BrightenImage(Grayscale.MakeGrayscale(image), 1.0f, 1.1f, 1);

            Graphics graphics = Graphics.FromImage(canvas);
            graphics.Clear(Color.Black);
            Bitmap white = new Bitmap(enforcedSize, enforcedSize);
            graphics = Graphics.FromImage(white);
            graphics.Clear(Color.White);

            graphics = Graphics.FromImage(canvas);
            graphics.DrawImage(new Bitmap(newEye),
               (enforcedSize / 2) - (((float)enforcedSize * 0.4096f) / 2), (enforcedSize / 2) - (((float)enforcedSize * 0.4096f) / 2),
                (float)enforcedSize * 0.4096f, (float)enforcedSize * 0.4096f);
            graphics.DrawImage(new Bitmap(template), 0, 0, enforcedSize, enforcedSize);

            return canvas;
        }

        public static Bitmap GrayscaleToAlpha(Bitmap file) {
            Bitmap image = TexIO.NewBitmap(file);
            LockBitmap source = new LockBitmap(image);
            source.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color sourcePixel = source.GetPixel(x, y);
                    Color col = Color.FromArgb(sourcePixel.R, sourcePixel.R, sourcePixel.R, sourcePixel.R);
                    source.SetPixel(x, y, col);
                }
            };
            source.UnlockBits();
            return image;
        }

        public static Bitmap MergeGrayscalesToRGBA(Bitmap red, Bitmap green, Bitmap blue, Bitmap alpha) {
            Bitmap image = new Bitmap(red);
            LockBitmap destination = new LockBitmap(image);
            LockBitmap redBits = new LockBitmap(red);
            LockBitmap greenBits = new LockBitmap(green);
            LockBitmap blueBits = new LockBitmap(blue);
            LockBitmap alphaBits = new LockBitmap(alpha);
            redBits.LockBits();
            greenBits.LockBits();
            blueBits.LockBits();
            alphaBits.LockBits();
            destination.LockBits();
            try {
                for (int y = 0; y < image.Height; y++) {
                    for (int x = 0; x < image.Width; x++) {
                        Color redPixel = redBits.GetPixel(x, y);
                        Color greenPixel = greenBits.GetPixel(x, y);
                        Color bluePixel = blueBits.GetPixel(x, y);
                        Color alphaPixel = alphaBits.GetPixel(x, y);
                        Color col = Color.FromArgb(alphaPixel.R, redPixel.R, greenPixel.G, bluePixel.B);
                        destination.SetPixel(x, y, col);
                    }
                };
            } catch {
                // Todo send out an error.
            }
            redBits.UnlockBits();
            greenBits.UnlockBits();
            blueBits.UnlockBits();
            alphaBits.UnlockBits();
            destination.UnlockBits();
            return image;
        }

        public static void HairBaseToHairMaps(string filename) {
            Bitmap image = TexIO.ResolveBitmap(filename);
            Bitmap rgb = ImageManipulation.ExtractRGB(image);
            Bitmap alpha = ImageManipulation.ExtractAlpha(image);
            Bitmap hairNormalConversion = Normal.Calculate(rgb);
            Bitmap hairNormalFinal = ImageManipulation.MergeAlphaToRGB(alpha, hairNormalConversion);

            Bitmap hairSpecularGreyscale = ImageManipulation.BoostAboveThreshold(image, 127);
            Bitmap hairSpecularGreyscale2 = ImageManipulation.BoostAboveThreshold(image, 90);
            Bitmap blank = new Bitmap(hairSpecularGreyscale.Width, hairSpecularGreyscale.Height);
            Graphics graphics = Graphics.FromImage(blank);
            graphics.Clear(Color.White);
            Bitmap hairSpecularConversion = ImageManipulation.MergeGrayscalesToRGBA(hairSpecularGreyscale, hairSpecularGreyscale2, blank, alpha);

            LegacyHairNormalToDawntrailNormal(hairSpecularConversion, hairNormalFinal)
                .Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_normorm"), ".png"), ImageFormat.Png);
            LegacyHairMultiToDawntrailMulti(hairSpecularConversion)
                .Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_maskask"), ".png"), ImageFormat.Png);
        }

        public static void BaseToNormaMap(string filename) {
            Bitmap image = TexIO.ResolveBitmap(filename);
            Normal.Calculate(image)
                .Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_norm"), ".png"), ImageFormat.Png);
        }
        public static void BaseToInvertedNormaMap(string filename) {
            Bitmap image = TexIO.ResolveBitmap(filename);
            Normal.Calculate(ImageManipulation.InvertImage(image))
                .Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_norm"), ".png"), ImageFormat.Png);
        }

        public static Bitmap LegacyHairMultiToDawntrailMulti(Bitmap bitmap) {
            Bitmap blueChannel = new Bitmap(bitmap.Width, bitmap.Height);
            Graphics.FromImage(blueChannel).Clear(Color.FromArgb(255, 47, 47, 47));
            Bitmap redChannel = bitmap;
            return MergeGrayscalesToRGBA(redChannel, InvertImage(ExtractGreen(bitmap)), blueChannel, new Bitmap(redChannel));
        }

        public static Bitmap LegacyHairNormalToDawntrailNormal(Bitmap legacyMulti, Bitmap legacyNormal) {
            return MergeGrayscalesToRGBA(ExtractRed(legacyNormal), ExtractGreen(legacyNormal), ExtractAlpha(legacyMulti), ExtractAlpha(legacyNormal));
        }

        public static void ClothingBaseToClothingMultiAndNormalMaps(string filename) {
            Bitmap image = TexIO.ResolveBitmap(filename);
            Bitmap rgb = ImageManipulation.ExtractRGB(image);
            Bitmap alpha = ImageManipulation.ExtractAlpha(image);
            Bitmap clothingNormalConversion = Normal.Calculate(rgb);
            Bitmap clothingNormalFinal = ImageManipulation.MergeAlphaToRGB(Grayscale.MakeGrayscale(image), clothingNormalConversion);

            Bitmap clothingMultiGreyscale = ImageManipulation.BoostAboveThreshold(Grayscale.MakeGrayscale(image), 160);
            Bitmap clothingMultiGreyscale2 = ImageManipulation.BoostAboveThreshold(Grayscale.MakeGrayscale(image), 140);
            Bitmap blank = new Bitmap(clothingMultiGreyscale.Width, clothingMultiGreyscale.Height);
            Graphics graphics = Graphics.FromImage(blank);
            graphics.Clear(Color.White);
            Bitmap blank2 = new Bitmap(blank);
            Bitmap clothingMultiConversion = ImageManipulation.MergeGrayscalesToRGBA(clothingMultiGreyscale, blank, clothingMultiGreyscale2, blank2);

            clothingMultiGreyscale.Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_normp1"), ".png"), ImageFormat.Png);
            clothingMultiGreyscale2.Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_normp2"), ".png"), ImageFormat.Png);

            clothingNormalFinal.Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_norm"), ".png"), ImageFormat.Png);
            clothingMultiConversion.Save(ImageManipulation.ReplaceExtension(ImageManipulation.AddSuffix(filename, "_mask"), ".png"), ImageFormat.Png);
        }
        public static Bitmap MergeAlphaToRGB(Bitmap alpha, Bitmap rgb) {
            Bitmap image = new Bitmap(rgb.Width, rgb.Height, PixelFormat.Format32bppArgb);
            Graphics.FromImage(image).Clear(Color.Transparent);
            LockBitmap destination = new LockBitmap(image);
            LockBitmap alphaBits = new LockBitmap(alpha);
            LockBitmap rgbBits = new LockBitmap(rgb);
            alphaBits.LockBits();
            rgbBits.LockBits();
            destination.LockBits();
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Color alphaPixel = alphaBits.GetPixel(x, y);
                    Color rgbPixel = rgbBits.GetPixel(x, y);
                    Color col = Color.FromArgb(Math.Clamp(alphaPixel.R, (byte)1, (byte)255), rgbPixel.R, rgbPixel.G, rgbPixel.B);
                    destination.SetPixel(x, y, col);
                }
            };
            alphaBits.UnlockBits();
            rgbBits.UnlockBits();
            destination.UnlockBits();
            return image;
        }
        public static Bitmap MergeNormals(string inputFile, Bitmap baseTexture, Bitmap canvasImage, Bitmap normalMask, string baseTextureNormal) {
            Graphics g = Graphics.FromImage(canvasImage);
            g.Clear(Color.Transparent);
            g.DrawImage(baseTexture, 0, 0, baseTexture.Width, baseTexture.Height);
            Bitmap normal = Normal.Calculate(canvasImage, normalMask);
            using (Bitmap originalNormal = TexIO.ResolveBitmap(inputFile)) {
                using (Bitmap destination = new Bitmap(originalNormal, originalNormal.Width, originalNormal.Height)) {
                    try {
                        Bitmap resize = new Bitmap(originalNormal);
                        g = Graphics.FromImage(resize);
                        //g.Clear(originalNormal.GetPixel(originalNormal.Width / 2, 0));
                        g.DrawImage(normal, 0, 0, originalNormal.Width, originalNormal.Height);
                        //KVImage.ImageBlender imageBlender = new KVImage.ImageBlender();
                        return ImageManipulation.MergeAlphaToRGB(ImageManipulation.ExtractAlpha(originalNormal), resize);
                        //return imageBlender.BlendImages(destination, 0, 0, destination.Width, destination.Height,
                        //    resize, 0, 0, KVImage.ImageBlender.BlendOperation.Blend_Overlay);
                    } catch {
                        return normal;
                    }
                }
            }
        }
        public static Bitmap MergeNormals(Bitmap inputFile, Bitmap baseTexture, Bitmap canvasImage, Bitmap normalMask, string baseTextureNormal, bool modifier) {
            Graphics g = Graphics.FromImage(canvasImage);
            g.Clear(Color.Transparent);
            g.DrawImage(baseTexture, 0, 0, baseTexture.Width, baseTexture.Height);
            Bitmap normal = Normal.Calculate(modifier ? ImageManipulation.InvertImage(canvasImage) : canvasImage, normalMask);
            using (Bitmap originalNormal = inputFile) {
                using (Bitmap destination = new Bitmap(originalNormal, originalNormal.Width, originalNormal.Height)) {
                    try {
                        Bitmap resize = new Bitmap(originalNormal);
                        g = Graphics.FromImage(resize);
                        //g.Clear(originalNormal.GetPixel(originalNormal.Width / 2, 0));
                        g.DrawImage(normal, 0, 0, originalNormal.Width, originalNormal.Height);
                        //KVImage.ImageBlender imageBlender = new KVImage.ImageBlender();
                        return ImageManipulation.MergeAlphaToRGB(ImageManipulation.ExtractAlpha(originalNormal), resize);
                        //return imageBlender.BlendImages(destination, 0, 0, destination.Width, destination.Height,
                        //    resize, 0, 0, KVImage.ImageBlender.BlendOperation.Blend_Overlay);
                    } catch {
                        return normal;
                    }
                }
            }
        }

        public static Bitmap MirrorAndDuplicate(Bitmap file) {
            Bitmap canvas = TexIO.NewBitmap(file.Width * 2, file.Height);
            Graphics graphics = Graphics.FromImage(canvas);
            graphics.DrawImage(file, new Point(file.Width, 0));
            canvas.RotateFlip(RotateFlipType.RotateNoneFlipX);
            graphics = Graphics.FromImage(canvas);
            graphics.DrawImage(file, new Point(file.Width, 0));
            return canvas;
        }
        public static Bitmap SideBySide(Bitmap left, Bitmap right) {
            Bitmap canvas = new Bitmap(left.Width * 2, left.Height);
            Graphics graphics = Graphics.FromImage(canvas);
            graphics.DrawImage(left, new Point(0, 0));
            graphics.DrawImage(new Bitmap(right, left.Width, left.Height), new Point(left.Width, 0));
            return canvas;
        }

        public static Bitmap ImageToCatchlightLegacy(Bitmap file, string baseDirectory = null) {
            string catchlightTemplate = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\catchlight.png");
            Bitmap catchlight = Brightness.BrightenImage(Grayscale.MakeGrayscale(file), 0.6f, 1.5f, 1);
            Graphics graphics = Graphics.FromImage(catchlight);
            graphics.DrawImage(new Bitmap(new Bitmap(catchlightTemplate), catchlight.Width, catchlight.Height), 0, 0);
            return catchlight;
        }

        public static Bitmap ImageToEyeNormal(Bitmap file, string baseDirectory = null) {
            Bitmap newFile = TexIO.NewBitmap(file);
            string normalTemplate = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\normal.png");
            Bitmap normal = Normal.Calculate(InvertImage(Brightness.BrightenImage(Grayscale.MakeGrayscale(newFile), 0.8f, 1.5f, 1)));
            Graphics graphics = Graphics.FromImage(normal);
            graphics.DrawImage(new Bitmap(new Bitmap(normalTemplate), file.Width, file.Height), 0, 0);
            return normal;
        }

        public static Bitmap ConvertBaseToDawntrailSkinMulti(Bitmap image) {
            Bitmap inverted = ImageManipulation.InvertImage(image);
            Bitmap alpha = new Bitmap(image.Width, image.Height);
            Bitmap blueChannel = new Bitmap(image.Width, image.Height);
            Graphics.FromImage(alpha).Clear(Color.White);
            Graphics.FromImage(blueChannel).Clear(Color.FromArgb(152, 152, 152));
            return MergeGrayscalesToRGBA(ExtractRed(image), ImageManipulation.InvertImage(ExtractBlue(image)), blueChannel, alpha);
        }



        public static void ConvertImageToAsymEyeMaps(string filename1, string filename2, string output) {
            Bitmap image = TexIO.ResolveBitmap(filename1);
            Bitmap eyeMulti = BitmapToEyeMulti(image);
            Bitmap eyeGlow = GrayscaleToAlpha(eyeMulti);
            Bitmap catchLight = ImageToCatchlightLegacy(eyeMulti);
            Bitmap normal = ImageToEyeNormal(eyeMulti);

            if (filename1 != filename2) {
                Bitmap image2 = TexIO.ResolveBitmap(filename2);
                Bitmap eyeMulti2 = BitmapToEyeMulti(image2);
                Bitmap eyeGlow2 = GrayscaleToAlpha(eyeMulti2);
                Bitmap catchLight2 = ImageToCatchlightLegacy(eyeMulti2);
                Bitmap normal2 = ImageToEyeNormal(eyeMulti2);

                SideBySide(eyeMulti, eyeMulti2).Save(ReplaceExtension(AddSuffix(output, "_eye_maskulti_asym"), ".png"), ImageFormat.Png);
                SideBySide(eyeGlow, eyeGlow2).Save(ReplaceExtension(AddSuffix(output, "_eye_glow_asym"), ".png"), ImageFormat.Png);
                SideBySide(catchLight, catchLight2).Save(ReplaceExtension(AddSuffix(output, "_eye_catchlight_asym"), ".png"), ImageFormat.Png);
                SideBySide(normal, normal2).Save(ReplaceExtension(AddSuffix(output, "_eye_normormal_asym"), ".png"), ImageFormat.Png);
            } else {
                SideBySide(eyeMulti, eyeMulti).Save(ReplaceExtension(AddSuffix(output, "_eye_maskulti_asym"), ".png"), ImageFormat.Png);
                SideBySide(eyeGlow, eyeGlow).Save(ReplaceExtension(AddSuffix(output, "_eye_glow_asym"), ".png"), ImageFormat.Png);
                SideBySide(catchLight, catchLight).Save(ReplaceExtension(AddSuffix(output, "_eye_catchlight_asym"), ".png"), ImageFormat.Png);
                SideBySide(normal, normal).Save(ReplaceExtension(AddSuffix(output, "_eye_normormal_asym"), ".png"), ImageFormat.Png);
            }
        }
        public static void ConvertImageToEyeMapsLegacy(string filename, string baseDirectory = null) {
            Bitmap image = TexIO.ResolveBitmap(filename);
            Bitmap eyeMulti = BitmapToEyeMulti(image, baseDirectory);
            Bitmap eyeGlow = GrayscaleToAlpha(eyeMulti);
            Bitmap catchLight = ImageToCatchlightLegacy(eyeMulti, baseDirectory);
            Bitmap normal = ImageToEyeNormal(eyeMulti, baseDirectory);

            TexIO.SaveBitmap(eyeMulti, ReplaceExtension(AddSuffix(filename, "_eye_maskulti"), ".png"));
            TexIO.SaveBitmap(eyeGlow, ReplaceExtension(AddSuffix(filename, "_eye_glow"), ".png"));
            TexIO.SaveBitmap(catchLight, ReplaceExtension(AddSuffix(filename, "_eye_catchlight"), ".png"));
            TexIO.SaveBitmap(normal, ReplaceExtension(AddSuffix(filename, "_eye_normormal"), ".png"));
        }
        public static string[] ConvertImageToEyeMapsDawntrail(string filename, string baseDirectory = null,
            bool ignoreIfExists = false, bool wasEyeMulti = false) {
            string[] strings = new string[] {
                ReplaceExtension(AddSuffix(filename, "_eye_base"), ".png"),
                ReplaceExtension(AddSuffix(filename, "_eye_norm"), ".png"),
                ReplaceExtension(AddSuffix(filename, "_eye_mask"), ".png")
            };
            if (!ignoreIfExists || !File.Exists(strings[0])) {
                Bitmap image = !wasEyeMulti ? TexIO.ResolveBitmap(filename) : ExtractRed(TexIO.ResolveBitmap(filename));
                Bitmap eyeBase = BitmapToEyeBaseDawntrail(image, baseDirectory);
                Bitmap eyeMulti = BitmapToEyeMultiDawntrail(image, baseDirectory);
                Bitmap normal = ImageToEyeNormalDawntrail(image, baseDirectory);
                TexIO.SaveBitmap(eyeBase, strings[0]);
                TexIO.SaveBitmap(normal, strings[1]);
                TexIO.SaveBitmap(eyeMulti, strings[2]);
            }
            return strings;
        }

        public static void ConvertOldEyeMultiToDawntrailEyeMaps(string filename, string baseDirectory = null) {
            Bitmap image = ExtractRed(TexIO.ResolveBitmap(filename));
            Bitmap eyeBase = BitmapToEyeBaseDawntrail(image, baseDirectory);
            Bitmap eyeMulti = BitmapToEyeMultiDawntrail(image, baseDirectory);
            Bitmap normal = ImageToEyeNormalDawntrail(image, baseDirectory);

            TexIO.SaveBitmap(eyeBase, ReplaceExtension(AddSuffix(filename, "_eye_base"), ".png"));
            TexIO.SaveBitmap(normal, ReplaceExtension(AddSuffix(filename, "_eye_norm"), ".png"));
            TexIO.SaveBitmap(eyeMulti, ReplaceExtension(AddSuffix(filename, "_eye_mask"), ".png"));
        }

        private static Bitmap ImageToEyeNormalDawntrail(Bitmap image, string baseDirectory) {
            int enforcedSize = 2048;
            string template = Path.Combine(!string.IsNullOrEmpty(baseDirectory) ? baseDirectory
                : AppDomain.CurrentDomain.BaseDirectory, "res\\textures\\eyes\\normaldt.png");
            Bitmap canvas = new Bitmap(enforcedSize, enforcedSize, PixelFormat.Format32bppArgb);
            Bitmap normal = Normal.Calculate(InvertImage(Brightness.BrightenImage(Grayscale.MakeGrayscale(image), 0.8f, 1.5f, 1)));

            Graphics graphics = Graphics.FromImage(canvas);
            graphics.Clear(Color.Black);
            //Bitmap white = new Bitmap(enforcedSize, enforcedSize);
            //graphics = Graphics.FromImage(white);
            //graphics.Clear(Color.White);

            graphics = Graphics.FromImage(canvas);
            graphics.DrawImage(TexIO.NewBitmap(normal),
               (enforcedSize / 2) - (((float)enforcedSize * 0.4096f) / 2), (enforcedSize / 2) - (((float)enforcedSize * 0.4096f) / 2),
                (float)enforcedSize * 0.4096f, (float)enforcedSize * 0.4096f);
            graphics.DrawImage(TexIO.ResolveBitmap(template), 0, 0, enforcedSize, enforcedSize);

            return canvas;
        }

        public static string ReplaceExtension(string path, string extension) {
            return Path.ChangeExtension(path, extension);
        }
        public static string AddSuffix(string filename, string suffix) {
            string fDir = Path.GetDirectoryName(filename);
            string fName = Path.GetFileNameWithoutExtension(filename);
            string fExt = Path.GetExtension(filename);
            return !string.IsNullOrEmpty(filename) ? Path.Combine(fDir,
                String.Concat(fName, suffix, fExt)) : "";
        }

        public static void EraseTeeth(Bitmap bitmap) {
            LockBitmap source = new LockBitmap(bitmap);
            source.LockBits();
            for (int y = 0; y < bitmap.Height; y++) {
                if (y < ((float)bitmap.Height * 0.1372549019607843f)) {
                    for (int x = 0; x < bitmap.Width; x++) {
                        Color sourcePixel = source.GetPixel(x, y);
                        if (x < ((float)bitmap.Height * 0.24853515625f)) {
                            Color col = Color.FromArgb(0, 0, 0, 0);
                            source.SetPixel(x, y, col);
                        }
                    }
                } else {
                    break;
                }
            };
            source.UnlockBits();
        }

        public static Bitmap ClearOldHorns(Bitmap bitmap) {
            return TexIO.Clone(bitmap, new Rectangle(0, 0, bitmap.Width / 2, bitmap.Height));
        }

        public static void ConvertTextureToTex(string fileName) {
            TexIO.SaveBitmap(TexIO.ResolveBitmap(fileName), ImageManipulation.ReplaceExtension(fileName, ".tex"));
        }
    }
}
