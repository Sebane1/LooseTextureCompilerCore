using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    /// <summary>
    /// Minimal 16-bit TIFF writer that bypasses ImageSharp's broken TiffEncoder.
    /// Writes uncompressed 16-bit-per-channel RGBA TIFFs with correct IFD tags.
    /// </summary>
    public static class Tiff16Writer {
        /// <summary>
        /// Saves an Image&lt;Rgba64&gt; as a true 16-bit-per-channel RGBA TIFF.
        /// ImageSharp 3.1.4's TiffEncoder silently quantizes Rgba64 to 8-bit — this bypasses it entirely.
        /// </summary>
        public static void Save(Image<Rgba64> image, string outputPath) {
            int width = image.Width;
            int height = image.Height;
            int samplesPerPixel = 4; // RGBA
            int bitsPerSample = 16;
            int bytesPerPixel = samplesPerPixel * (bitsPerSample / 8); // 8 bytes
            long stripSize = (long)width * height * bytesPerPixel;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs)) {
                // === TIFF Header (8 bytes) ===
                bw.Write((byte)'I'); bw.Write((byte)'I'); // Little-endian byte order
                bw.Write((ushort)42);                      // TIFF magic number
                bw.Write((uint)8);                         // Offset to first IFD (immediately after header)

                // === IFD (Image File Directory) ===
                int tagCount = 11;
                bw.Write((ushort)tagCount);

                // We'll need to know where auxiliary data goes (after IFD + next-IFD pointer)
                // IFD size = 2 (count) + tagCount*12 (entries) + 4 (next IFD pointer)
                uint ifdSize = (uint)(2 + tagCount * 12 + 4);
                uint auxDataOffset = 8 + ifdSize; // right after the IFD

                // Aux data layout:
                // [0..7]   BitsPerSample: 4 x ushort = 8 bytes
                // [8..9]   ExtraSamples: 1 x ushort = 2 bytes (padded to 4 via tag)
                uint bitsPerSampleOffset = auxDataOffset;
                uint pixelDataOffset = auxDataOffset + 8; // BitsPerSample data, then pixel data starts

                // Tag 256: ImageWidth (SHORT)
                WriteTag(bw, 256, 3, 1, (uint)width);
                // Tag 257: ImageLength (SHORT)
                WriteTag(bw, 257, 3, 1, (uint)height);
                // Tag 258: BitsPerSample (SHORT, 4 values → offset)
                WriteTag(bw, 258, 3, 4, bitsPerSampleOffset);
                // Tag 259: Compression (SHORT) = 1 (None)
                WriteTag(bw, 259, 3, 1, 1);
                // Tag 262: PhotometricInterpretation (SHORT) = 2 (RGB)
                WriteTag(bw, 262, 3, 1, 2);
                // Tag 273: StripOffsets (LONG) = offset to pixel data
                WriteTag(bw, 273, 4, 1, pixelDataOffset);
                // Tag 277: SamplesPerPixel (SHORT) = 4
                WriteTag(bw, 277, 3, 1, 4);
                // Tag 278: RowsPerStrip (SHORT) = height (single strip)
                WriteTag(bw, 278, 3, 1, (uint)height);
                // Tag 279: StripByteCounts (LONG)
                WriteTag(bw, 279, 4, 1, (uint)stripSize);
                // Tag 284: PlanarConfiguration (SHORT) = 1 (Chunky/interleaved)
                WriteTag(bw, 284, 3, 1, 1);
                // Tag 338: ExtraSamples (SHORT) = 2 (Unassociated alpha)
                WriteTag(bw, 338, 3, 1, 2);

                // Next IFD offset = 0 (no more IFDs)
                bw.Write((uint)0);

                // === Auxiliary data: BitsPerSample values ===
                bw.Write((ushort)16); // R
                bw.Write((ushort)16); // G
                bw.Write((ushort)16); // B
                bw.Write((ushort)16); // A

                // === Pixel data ===
                image.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++) {
                        Span<Rgba64> row = accessor.GetRowSpan(y);
                        for (int x = 0; x < accessor.Width; x++) {
                            Rgba64 pixel = row[x];
                            bw.Write(pixel.R);
                            bw.Write(pixel.G);
                            bw.Write(pixel.B);
                            bw.Write(pixel.A);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Writes a single 12-byte IFD tag entry.
        /// </summary>
        private static void WriteTag(BinaryWriter bw, ushort tag, ushort type, uint count, uint value) {
            bw.Write(tag);
            bw.Write(type);
            bw.Write(count);
            bw.Write(value);
        }
    }
}
