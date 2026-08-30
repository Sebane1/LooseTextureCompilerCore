using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    public static class ProteusNormalModifier {
        /// <summary>
        /// Sets all blue channel values to 255 (white) for Proteus normal maps.
        /// </summary>
        public static Bitmap SetProteusBlueChannelToWhite(Bitmap input) {
            // Check if the image is valid
            if (input == null || input.Width <= 0 || input.Height <= 0)
                return input;

            // Create a copy of the bitmap to avoid modifying the original
            Bitmap result = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb);
            
            // Lock bits for both bitmaps 
            using (LockBitmap source = new LockBitmap(input)) {
                using (LockBitmap destination = new LockBitmap(result)) {
                    source.LockBits();
                    destination.LockBits();

                    byte[] srcPixels = source.Pixels;
                    byte[] dstPixels = destination.Pixels;

                    // Process each pixel
                    for (int i = 0; i < srcPixels.Length; i += 4) {
                        // Set blue channel to 255 (white)
                        dstPixels[i] = 255; // Blue channel set to white
                        
                        // Keep other channels as they are
                        if (i + 1 < srcPixels.Length) 
                            dstPixels[i + 1] = srcPixels[i + 1]; // Green channel unchanged
                        if (i + 2 < srcPixels.Length)
                            dstPixels[i + 2] = srcPixels[i + 2]; // Red channel unchanged  
                        if (i + 3 < srcPixels.Length)
                            dstPixels[i + 3] = srcPixels[i + 3]; // Alpha channel unchanged
                    }

                    destination.UnlockBits();
                }
            }
            
            return result;
        }
    }
}