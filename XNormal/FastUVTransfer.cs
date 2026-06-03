using FFXIVLooseTextureCompiler.ImageProcessing;
using LooseTextureCompilerCore;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System;

namespace FFXIVLooseTextureCompiler
{
    public class FastUVTransfer
    {
        public static List<Tuple<string, string>> biboToGen2Batch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> biboToGen3Batch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> biboToTbseBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> gen3ToGen2Batch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> gen3ToBiboBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> gen2ToBiboBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> gen2ToGen3Batch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> otopopToVanillaLalaBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> vanillaLalaToOtopopBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> vanillaLalaToAsymLalaBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> asymLalaToVanillaLalaBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> asymLalaToOtopopBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> otopopToAsymLalaBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> relalaToAsymLalaBatch = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> asymLalaToRelalaBatch = new List<Tuple<string, string>>();

        public class ModularTransferJob
        {
            public string SourceMesh { get; set; }
            public string TargetMesh { get; set; }
            public string Input { get; set; }
            public string Output { get; set; }
        }
        public static List<ModularTransferJob> modularBatch = new List<ModularTransferJob>();

        
        /// <summary>
        /// If a gen2 source is square-padded (content in left half), stretch the left half
        /// to fill the full width so transfer map UV coordinates align correctly.
        /// Returns the input unchanged if it's already 1:2 aspect ratio.
        /// </summary>
        private static Bitmap PrepareGen2ForTransfer(Bitmap input)
        {
            if (input == null) return null;
            int w = input.Width;
            int h = input.Height;
            if (w != h) return input; // Already 1:2, no prep needed
            // Square padded: gen2 content in left half, stretch to fill full width
            int halfW = w / 2;
            Bitmap stretched = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(stretched))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                var srcRect = new System.Drawing.Rectangle(0, 0, halfW, h);
                var destRect = new System.Drawing.Rectangle(0, 0, w, h);
                g.DrawImage(input, destRect, srcRect, System.Drawing.GraphicsUnit.Pixel);
            }
            return stretched;
        }

        /// <summary>
        /// File-based variant: loads, stretches if needed, saves to a temp VFS path, returns the path.
        /// </summary>
        private static string PrepareGen2FileForTransfer(string inputImage)
        {
            using (Bitmap source = TexIO.ResolveBitmap(inputImage))
            {
                if (source == null || source.Width != source.Height) return inputImage;
                using (Bitmap stretched = PrepareGen2ForTransfer(source))
                {
                    string preparedPath = inputImage + "_gen2_stretched.raw";
                    if (inputImage.StartsWith("memory:\\", StringComparison.OrdinalIgnoreCase))
                        preparedPath = inputImage + "_gen2_stretched.raw";
                    TexIO.SaveBitmap(stretched, preparedPath);
                    return preparedPath;
                }
            }
        }

        public static Bitmap PerformTransfer(Bitmap inputImage, string transferMapFilename)
        {
            string transferMapPath = Path.Combine(GlobalPathStorage.OriginalBaseDirectory, "res", "fastuvtransfer", "body", transferMapFilename);
            if (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(transferMapPath)) return null;
            if (UVTransferMap.UseGPUAcceleration)
            {
                return ComputeSharpUVTransfer.ApplyTransferMapFast(inputImage, transferMapPath, true);
            }
            return UVTransferMap.ApplyTransferMap(inputImage, transferMapPath);
        }

        public static Bitmap BiboToGen2(Bitmap inputImage) => PerformTransfer(inputImage, "bibo_to_gen2_transfer.tif");
        public static Bitmap BiboToGen3(Bitmap inputImage) => PerformTransfer(inputImage, "bibo_to_gen3_transfer.tif");
        public static Bitmap Gen3ToGen2(Bitmap inputImage) => PerformTransfer(inputImage, "gen3_to_gen2_transfer.tif");
        public static Bitmap Gen3ToBibo(Bitmap inputImage) => PerformTransfer(inputImage, "gen3_to_bibo_transfer.tif");
        public static Bitmap Gen2ToBibo(Bitmap inputImage) {
            if (inputImage == null) return null;
            int srcW = inputImage.Width;
            int srcH = inputImage.Height;
            // Gen2 content occupies the left half (or full width if 1:2).
            int halfW = (srcW == srcH) ? srcW / 2 : srcW;
            int outW = halfW * 2;
            int outH = srcH;
            // Extract left half into a temp bitmap
            var leftRect = new System.Drawing.Rectangle(0, 0, halfW, srcH);
            using (Bitmap leftHalf = new Bitmap(halfW, srcH, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gTemp = Graphics.FromImage(leftHalf))
                {
                    gTemp.DrawImage(inputImage, new System.Drawing.Rectangle(0, 0, halfW, srcH), leftRect, System.Drawing.GraphicsUnit.Pixel);
                }
                // Create flipped copy
                using (Bitmap flipped = (Bitmap)leftHalf.Clone())
                {
                    flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    Bitmap output = new Bitmap(outW, outH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(output))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        // Right half = original gen2 content
                        g.DrawImage(leftHalf, outW / 2, 0, halfW, srcH);
                        // Left half = mirrored gen2 content
                        g.DrawImage(flipped, 0, 0, halfW, srcH);
                    }
                    return output;
                }
            }
        }
        public static Bitmap Gen2ToGen3(Bitmap inputImage) {
            if (inputImage == null) return null;
            // If the input is square-padded (gen2 content in left half), stretch to fill 1:1
            Bitmap prepared = PrepareGen2ForTransfer(inputImage);
            Bitmap result = PerformTransfer(prepared, "gen2_to_gen3_transfer.tif");
            if (prepared != inputImage) prepared.Dispose();
            return result;
        }
        public static Bitmap OtopopToVanillaLala(Bitmap inputImage) => PerformTransfer(inputImage, "otopop_to_vanillalala_transfer.tif");
        public static Bitmap VanillaLalaToOtopop(Bitmap inputImage) => PerformTransfer(inputImage, "vanillalala_to_otopop_transfer.tif");
        public static Bitmap VanillaLalaToAsymLala(Bitmap inputImage) => PerformTransfer(inputImage, "vanillalala_to_asymlala_transfer.tif");
        public static Bitmap AsymLalaToVanillaLala(Bitmap inputImage) => PerformTransfer(inputImage, "asymlala_to_vanillalala_transfer.tif");
        public static Bitmap AsymLalaToOtopop(Bitmap inputImage) => PerformTransfer(inputImage, "asymlala_to_otopop_transfer.tif");
        public static Bitmap OtopopToAsymLala(Bitmap inputImage) => PerformTransfer(inputImage, "otopop_to_asymlala_transfer.tif");
        public static Bitmap RelalaToAsymLala(Bitmap inputImage) => PerformTransfer(inputImage, "relala_to_asymlala_transfer.tif");
        public static Bitmap AsymLalaToRelala(Bitmap inputImage) => PerformTransfer(inputImage, "asymlala_to_relala_transfer.tif");
        public static void ProcessBatches()
        {
            foreach (var item in gen3ToBiboBatch) Gen3ToBibo(item.Item1, item.Item2);
            foreach (var item in biboToGen3Batch) BiboToGen3(item.Item1, item.Item2);
            foreach (var item in biboToTbseBatch) PerformModularTransfer("bibo", "tbse", item.Item1, item.Item2, "bibo_to_tbse_transfer.tif");
            foreach (var item in gen2ToBiboBatch) Gen2ToBibo(item.Item1, item.Item2);
            foreach (var item in gen2ToGen3Batch) Gen2ToGen3(item.Item1, item.Item2);

            foreach (var item in otopopToVanillaLalaBatch) OtopopToVanillaLala(item.Item1, item.Item2);
            foreach (var item in vanillaLalaToOtopopBatch) VanillaLalaToOtopop(item.Item1, item.Item2);
            foreach (var item in vanillaLalaToAsymLalaBatch) VanillaLalaToAsymLala(item.Item1, item.Item2);
            foreach (var item in asymLalaToVanillaLalaBatch) AsymLalaToVanillaLala(item.Item1, item.Item2);
            foreach (var item in asymLalaToOtopopBatch) AsymLalaToOtopop(item.Item1, item.Item2);
            foreach (var item in otopopToAsymLalaBatch) OtopopToAsymLala(item.Item1, item.Item2);
            foreach (var item in relalaToAsymLalaBatch) RelalaToAsymLala(item.Item1, item.Item2);
            foreach (var item in asymLalaToRelalaBatch) AsymLalaToRelala(item.Item1, item.Item2);

            foreach (var item in modularBatch) PerformModularTransfer(item.SourceMesh, item.TargetMesh, item.Input, item.Output);

            foreach (var item in biboToGen2Batch)
            {
                while (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(item.Item1))
                {
                    Thread.Sleep(100);
                }
                TexIO.SaveBitmap(ImageManipulation.CutInHalf(TexIO.ResolveBitmap(item.Item1)), item.Item2);
            }
            foreach (var item in gen3ToGen2Batch)
            {
                string preBakedFile = item.Item2.Replace("gen2", "bibo");
                while (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(preBakedFile))
                {
                    Thread.Sleep(100);
                }
                TexIO.SaveBitmap(ImageManipulation.CutInHalf(TexIO.ResolveBitmap(preBakedFile)), item.Item2);
            }

            ClearBatches();
        }

        public static void ClearBatches()
        {
            biboToGen2Batch.Clear();
            biboToGen3Batch.Clear();
            biboToTbseBatch.Clear();
            gen3ToGen2Batch.Clear();
            gen3ToBiboBatch.Clear();
            gen2ToBiboBatch.Clear();
            gen2ToGen3Batch.Clear();
            otopopToVanillaLalaBatch.Clear();
            vanillaLalaToOtopopBatch.Clear();
            vanillaLalaToAsymLalaBatch.Clear();
            asymLalaToVanillaLalaBatch.Clear();
            asymLalaToOtopopBatch.Clear();
            otopopToAsymLalaBatch.Clear();
            relalaToAsymLalaBatch.Clear();
            asymLalaToRelalaBatch.Clear();
            modularBatch.Clear();
        }

        private static void PerformTransfer(string inputImage, string outputImage, string transferMapFilename, System.Action<string, string> xnormalFallback)
        {


            string transferMapPath = Path.Combine(GlobalPathStorage.OriginalBaseDirectory, "res", "fastuvtransfer", "body", transferMapFilename);

            // If the map doesn't exist for some reason, fallback to XNormal
            if (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(transferMapPath))
            {
                xnormalFallback(inputImage, outputImage);
                string xnormalOutput = ImageManipulation.AddSuffix(outputImage, "_baseTexBaked");
                if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(xnormalOutput))
                {
                    if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(outputImage)) File.Delete(outputImage);
                    File.Move(xnormalOutput, outputImage);
                }
                return;
            }
            // GPU fast path: file → GPU → file, zero Bitmap overhead
            if (UVTransferMap.UseGPUAcceleration)
            {
                try
                {
                    if (ComputeSharpUVTransfer.TransferFile(inputImage, outputImage, transferMapPath))
                        return;
                }
                catch (Exception e)
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gpu_fast_transfer_error.txt"), e.ToString());
                    // Fallback to Bitmap path
                }
            }

            using (Bitmap sourceTexture = TexIO.ResolveBitmap(inputImage))
            {
                using (Bitmap result = UVTransferMap.ApplyTransferMap(sourceTexture, transferMapPath))
                {
                    TexIO.SaveBitmap(result, outputImage);
                }
            }
        }

        public static void BiboToGen2(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "bibo_to_gen2_transfer.tif", XNormal.BiboToGen3);
        }

        public static void BiboToGen3(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "bibo_to_gen3_transfer.tif", XNormal.BiboToGen3);
        }

        public static void Gen3ToGen2(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "gen3_to_gen2_transfer.tif", XNormal.Gen3ToBibo);
        }

        public static void Gen3ToBibo(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "gen3_to_bibo_transfer.tif", XNormal.Gen3ToBibo);
        }

        public static void Gen2ToBibo(string inputImage, string outputImage)
        {
            // Gen2→Bibo is a simple horizontal mirror: the 2048x4096 gen2 body
            // becomes a 4096x4096 bibo body by mirroring left↔right.
            using (Bitmap source = TexIO.ResolveBitmap(inputImage))
            {
                if (source == null) return;
                int srcW = source.Width;
                int srcH = source.Height;
                int halfW = (srcW == srcH) ? srcW / 2 : srcW;
                int outW = halfW * 2;
                int outH = srcH;
                var leftRect = new System.Drawing.Rectangle(0, 0, halfW, srcH);
                using (Bitmap leftHalf = new Bitmap(halfW, srcH, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics gTemp = Graphics.FromImage(leftHalf))
                    {
                        gTemp.DrawImage(source, new System.Drawing.Rectangle(0, 0, halfW, srcH), leftRect, System.Drawing.GraphicsUnit.Pixel);
                    }
                    using (Bitmap flipped = (Bitmap)leftHalf.Clone())
                    {
                        flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        using (Bitmap result = new Bitmap(outW, outH, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g = Graphics.FromImage(result))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(leftHalf, outW / 2, 0, halfW, srcH);
                                g.DrawImage(flipped, 0, 0, halfW, srcH);
                            }
                            TexIO.SaveBitmap(result, outputImage);
                        }
                    }
                }
            }
        }

        public static void Gen2ToGen3(string inputImage, string outputImage)
        {
            // Pre-stretch the gen2 padded image to 1:1 before transfer map
            string preparedPath = PrepareGen2FileForTransfer(inputImage);
            PerformTransfer(preparedPath, outputImage, "gen2_to_gen3_transfer.tif", XNormal.Gen2ToGen3);
        }

        public static void OtopopToVanillaLala(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "otopop_to_vanilla_transfer.tif", XNormal.OtopopToVanillaLala);
        }

        public static void VanillaLalaToOtopop(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "vanilla_to_otopop_transfer.tif", XNormal.VanillaLalaToOtopop);
        }

        public static void VanillaLalaToAsymLala(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "vanilla_to_asymlala_transfer.tif", XNormal.VanillaLalaToAsymLala);
        }

        public static void AsymLalaToVanillaLala(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "asymlala_to_vanilla_transfer.tif", XNormal.AsymLalaToVanillaLala);
        }

        public static void AsymLalaToOtopop(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "asymlala_to_otopop_transfer.tif", XNormal.AsymLalaToOtopop);
        }

        public static void OtopopToAsymLala(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "otopop_to_asymlala_transfer.tif", XNormal.OtopopToAsymLala);
        }

        public static void RelalaToAsymLala(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "relala_to_asymlala_transfer.tif", XNormal.RelalaToAsymLala);
        }

        public static void AsymLalaToRelala(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "asymlala_to_relala_transfer.tif", XNormal.AsymLalaToRelala);
        }

        public static bool GenerateBasedOnSourceBody(string internalPath, string inputPath, string outputPath)
        {
            bool wasHandled = true;

            if (internalPath.Contains("bibo"))
            {
                if (outputPath.Contains("gen2")) biboToGen2Batch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("gen3")) biboToGen3Batch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("tbse")) biboToTbseBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else if (internalPath.Contains("eve") || internalPath.Contains("gen3"))
            {
                if (outputPath.Contains("gen2")) gen3ToGen2Batch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("bibo")) gen3ToBiboBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else if (internalPath.Contains("body"))
            {
                if (outputPath.Contains("bibo")) gen2ToBiboBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("gen3")) gen2ToGen3Batch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else if (internalPath.Contains("skin_otopop") || internalPath.Contains("v01_c1101b0001_g"))
            {
                if (outputPath.Contains("vanilla_lala")) otopopToVanillaLalaBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else if (internalPath.Contains("--c1101b0001"))
            {
                if (outputPath.Contains("otopop")) vanillaLalaToOtopopBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else if (internalPath.Contains("v01_c1101b0001_b"))
            {
                if (outputPath.Contains("otopop")) asymLalaToOtopopBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("vanilla_lala")) asymLalaToVanillaLalaBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("relala")) asymLalaToRelalaBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else if (internalPath.Contains("relala"))
            {
                if (outputPath.Contains("v01_c1101b0001_b")) relalaToAsymLalaBatch.Add(new Tuple<string, string>(inputPath, outputPath));
                else wasHandled = false;
            }
            else
            {
                wasHandled = false;
            }

            // --- ADDITIVE MODULAR FALLBACK FOR FACES/OTHER MESHES ---
            if (!wasHandled)
            {
                if (XNormal.TryGetMeshes(internalPath, outputPath, false, out string sourceMesh, out string targetMesh))
                {
                    modularBatch.Add(new ModularTransferJob() { SourceMesh = sourceMesh, TargetMesh = targetMesh, Input = inputPath, Output = outputPath });
                    return true;
                }
                else
                {
                    return false; // Unhandled entirely, TextureProcessor legacy path will catch it
                }
            }

            return true;
        }

        public static void PerformModularTransfer(string sourceMeshRelPath, string targetMeshRelPath, string inputImage, string outputImage, string transferMapNameOverride = null)
        {
            string sourceMeshName = Path.GetFileNameWithoutExtension(sourceMeshRelPath);
            string targetMeshName = Path.GetFileNameWithoutExtension(targetMeshRelPath);
            string transferMapName = transferMapNameOverride ?? $"{sourceMeshName}_to_{targetMeshName}_transfer.tif";

            // Note: Since this is additive for faces/extras, we can put it in a generic folder, but sticking to "body" works for now 
            // since that's where the res folder is, or we could use "dynamic". Let's use fastuvtransfer\dynamic
            string transferMapDir = Path.Combine(GlobalPathStorage.OriginalBaseDirectory, "res", "fastuvtransfer", "dynamic");
            Directory.CreateDirectory(transferMapDir);

            string transferMapPath = Path.Combine(transferMapDir, transferMapName);

            // If the map doesn't exist, generate it seamlessly using XNormal!
            // Also detect and purge maps baked with the buggy 8-bit coordinate_map.tif
            if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(transferMapPath))
            {
                if (IsQuantizedTransferMap(transferMapPath))
                {
                    try { File.Delete(transferMapPath); } catch { }
                }
            }
            if (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(transferMapPath))
            {
                XNormal.BakeTransferMap(sourceMeshRelPath, targetMeshRelPath, transferMapPath);
            }
            // GPU fast path: file → GPU → file, zero Bitmap overhead
            if (UVTransferMap.UseGPUAcceleration)
            {
                try
                {
                    if (ComputeSharpUVTransfer.TransferFile(inputImage, outputImage, transferMapPath))
                        return;
                }
                catch (Exception e)
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gpu_fast_transfer_error2.txt"), e.ToString());
                    // Fallback to Bitmap path
                }
            }

            using (Bitmap sourceTexture = TexIO.ResolveBitmap(inputImage))
            {
                using (Bitmap result = UVTransferMap.ApplyTransferMap(sourceTexture, transferMapPath))
                {
                    TexIO.SaveBitmap(result, outputImage);
                }
            }
        }

        /// <summary>
        /// Detects transfer maps that were baked from an 8-bit quantized coordinate map.
        /// In such maps, R and G values are exact multiples of 257 (0, 257, 514, ..., 65535).
        /// </summary>
        private static bool IsQuantizedTransferMap(string transferMapPath)
        {
            try
            {
                using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba64>(transferMapPath))
                {
                    int eightBitCount = 0;
                    int sampleCount = 0;
                    int target = 200;

                    image.ProcessPixelRows(accessor =>
                    {
                        int step = Math.Max(1, accessor.Height / 20);
                        for (int y = 0; y < accessor.Height && sampleCount < target; y += step)
                        {
                            var row = accessor.GetRowSpan(y);
                            int xStep = Math.Max(1, accessor.Width / 10);
                            for (int x = 0; x < accessor.Width && sampleCount < target; x += xStep)
                            {
                                var pixel = row[x];
                                if (pixel.A < 100) continue;
                                sampleCount++;
                                if (pixel.R % 257 == 0 && pixel.G % 257 == 0)
                                    eightBitCount++;
                            }
                        }
                    });

                    return sampleCount > 50 && (float)eightBitCount / sampleCount > 0.95f;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}



