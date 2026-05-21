using Lumina.Data.Files;
using SixLabors.ImageSharp.PixelFormats;

namespace Penumbra.LTCImport.Dds;

public static class PenumbraTextureImporter {
    private static void WriteHeader(byte[] target, int width, int height) {
        using var mem = new MemoryStream(target);
        using var bw = new BinaryWriter(mem);
        bw.Write((uint)TexFile.Attribute.TextureType2D);
        bw.Write((uint)TexFile.TextureFormat.B8G8R8A8);
        bw.Write((ushort)width);
        bw.Write((ushort)height);
        bw.Write((ushort)1);
        bw.Write((ushort)1);
        bw.Write(0);
        bw.Write(1);
        bw.Write(2);
        bw.Write(80);
        for (var i = 1; i < 13; ++i) {
            bw.Write(0);
        }
    }
    private static readonly object _gpuLock = new object();

    public static bool RgbaBytesToTex(byte[] rgba, int width, int height, out byte[] texData, bool exportBc7 = false, bool useGpu = true) {
        texData = Array.Empty<byte>();
        if (rgba.Length != width * height * 4) {
            return false;
        }

        try {
            if (exportBc7) {
                using var scratch = OtterTex.ScratchImage.FromRGBA(rgba, width, height);
                using var mipmapped = scratch.GenerateMipMaps(0, OtterTex.FilterFlags.Default);
                
                IntPtr d3dDevice = useGpu ? Direct3D11Helper.GetDevice() : IntPtr.Zero;
                OtterTex.ScratchImage compressed;
                
                if (d3dDevice != IntPtr.Zero) {
                    lock (_gpuLock) {
                        compressed = mipmapped.Compress(d3dDevice, OtterTex.DXGIFormat.BC7UNorm, OtterTex.CompressFlags.Default, 0.5f);
                    }
                } else {
                    compressed = mipmapped.Compress(OtterTex.DXGIFormat.BC7UNorm, OtterTex.CompressFlags.Parallel, 0.5f);
                }

                using (compressed) {
                    var header = Penumbra.LTCImport.Textures.PenumbraTexFileParser.ToTexHeader(compressed);
                    
                    using var mem = new MemoryStream();
                    using var bw = new BinaryWriter(mem);
                    Penumbra.LTCImport.Textures.PenumbraTexFileParser.Write(header, bw);
                    bw.Flush(); // Ensure header is fully written to MemoryStream before appending pixels!
                    
                    var pixels = compressed.Pixels;
                    mem.Write(pixels);
                    
                    texData = mem.ToArray();
                    return true;
                }
            }
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Failed to compress to BC7: " + ex.Message);
        }
        
        texData = new byte[80 + width * height * 4];
        WriteHeader(texData, width, height);
        rgba.CopyTo(texData.AsSpan(80));
        for (var i = 80; i < texData.Length; i += 4)
            (texData[i], texData[i + 2]) = (texData[i + 2], texData[i]);
        return true;
    }

    public static bool PngToTex(string inputFile, out byte[] texData, bool exportBc7 = false, bool useGpu = true) {
        using (var file = File.OpenRead(inputFile)) {
            return PngToTex(file, out texData, exportBc7, useGpu);
        }
    }
    public static bool PngToTex(Stream file, out byte[] texData, bool exportBc7 = false, bool useGpu = true) {
        using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(file)) {
            var rgbaPixels = new byte[image.Height * image.Width * 4];
            image.CopyPixelDataTo(rgbaPixels);
            return RgbaBytesToTex(rgbaPixels, image.Width, image.Height, out texData, exportBc7, useGpu);
        }
    }

    public static unsafe bool BitmapToTex(System.Drawing.Bitmap file, out byte[] texData, bool exportBc7 = false, bool useGpu = true) {
        int width = file.Width;
        int height = file.Height;
        int totalPixels = width * height;

        var bmpData = file.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try {
            if (exportBc7) {
                byte[] rgbaPixels = new byte[totalPixels * 4];
                byte* src = (byte*)bmpData.Scan0;
                fixed (byte* dst = rgbaPixels) {
                    for (int i = 0; i < totalPixels * 4; i += 4) {
                        dst[i]     = src[i + 2]; // R
                        dst[i + 1] = src[i + 1]; // G
                        dst[i + 2] = src[i];     // B
                        dst[i + 3] = src[i + 3]; // A
                    }
                }
                if (RgbaBytesToTex(rgbaPixels, width, height, out texData, true, useGpu)) return true;
            }

            // Uncompressed path: Bitmap is already BGRA, which matches uncompressed .tex
            texData = new byte[80 + totalPixels * 4];
            WriteHeader(texData, width, height);
            fixed (byte* dst = &texData[80]) {
                System.Buffer.MemoryCopy((void*)bmpData.Scan0, dst, totalPixels * 4, totalPixels * 4);
            }
            return true;
        } finally {
            file.UnlockBits(bmpData);
        }
    }
}
