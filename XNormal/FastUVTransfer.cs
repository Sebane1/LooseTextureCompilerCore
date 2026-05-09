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

        public class ModularTransferJob
        {
            public string SourceMesh { get; set; }
            public string TargetMesh { get; set; }
            public string Input { get; set; }
            public string Output { get; set; }
        }
        public static List<ModularTransferJob> modularBatch = new List<ModularTransferJob>();

        public static void ProcessBatches()
        {
            foreach (var item in gen3ToBiboBatch) Gen3ToBibo(item.Item1, item.Item2);
            foreach (var item in biboToGen3Batch) BiboToGen3(item.Item1, item.Item2);
            foreach (var item in gen2ToBiboBatch) Gen2ToBibo(item.Item1, item.Item2);
            foreach (var item in gen2ToGen3Batch) Gen2ToGen3(item.Item1, item.Item2);

            foreach (var item in otopopToVanillaLalaBatch) OtopopToVanillaLala(item.Item1, item.Item2);
            foreach (var item in vanillaLalaToOtopopBatch) VanillaLalaToOtopop(item.Item1, item.Item2);
            foreach (var item in vanillaLalaToAsymLalaBatch) VanillaLalaToAsymLala(item.Item1, item.Item2);
            foreach (var item in asymLalaToVanillaLalaBatch) AsymLalaToVanillaLala(item.Item1, item.Item2);
            foreach (var item in asymLalaToOtopopBatch) AsymLalaToOtopop(item.Item1, item.Item2);
            foreach (var item in otopopToAsymLalaBatch) OtopopToAsymLala(item.Item1, item.Item2);

            foreach (var item in modularBatch) PerformModularTransfer(item.SourceMesh, item.TargetMesh, item.Input, item.Output);

            foreach (var item in biboToGen2Batch)
            {
                while (!File.Exists(item.Item1))
                {
                    Thread.Sleep(100);
                }
                TexIO.SaveBitmap(ImageManipulation.CutInHalf(TexIO.ResolveBitmap(item.Item1)), item.Item2);
            }
            foreach (var item in gen3ToGen2Batch)
            {
                string preBakedFile = item.Item2.Replace("gen2", "bibo");
                while (!File.Exists(preBakedFile))
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
            modularBatch.Clear();
        }

        private static void PerformTransfer(string inputImage, string outputImage, string transferMapFilename, System.Action<string, string> xnormalFallback)
        {
            // If it's a normal map, we must use XNormal for proper tangent space re-calculation.
            if (ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal)
            {
                xnormalFallback(inputImage, outputImage);
                string xnormalOutput = ImageManipulation.AddSuffix(outputImage, "_normals");
                if (File.Exists(xnormalOutput))
                {
                    if (File.Exists(outputImage)) File.Delete(outputImage);
                    File.Move(xnormalOutput, outputImage);
                }
                return;
            }

            string transferMapPath = Path.Combine(GlobalPathStorage.OriginalBaseDirectory, "res", "fastuvtransfer", "body", transferMapFilename);

            // If the map doesn't exist for some reason, fallback to XNormal
            if (!File.Exists(transferMapPath))
            {
                xnormalFallback(inputImage, outputImage);
                string xnormalOutput = ImageManipulation.AddSuffix(outputImage, "_baseTexBaked");
                if (File.Exists(xnormalOutput))
                {
                    if (File.Exists(outputImage)) File.Delete(outputImage);
                    File.Move(xnormalOutput, outputImage);
                }
                return;
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
            PerformTransfer(inputImage, outputImage, "gen2_to_bibo_transfer.tif", XNormal.Gen2ToBibo);
        }

        public static void Gen2ToGen3(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "gen2_to_gen3_transfer.tif", XNormal.Gen2ToGen3);
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

        public static bool GenerateBasedOnSourceBody(string internalPath, string inputPath, string outputPath)
        {
            bool wasHandled = true;

            if (internalPath.Contains("bibo"))
            {
                if (outputPath.Contains("gen2")) biboToGen2Batch.Add(new Tuple<string, string>(inputPath, outputPath));
                else if (outputPath.Contains("gen3")) biboToGen3Batch.Add(new Tuple<string, string>(inputPath, outputPath));
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
            if (!File.Exists(transferMapPath))
            {
                XNormal.BakeTransferMap(sourceMeshRelPath, targetMeshRelPath, transferMapPath);
            }

            using (Bitmap sourceTexture = TexIO.ResolveBitmap(inputImage))
            {
                using (Bitmap result = UVTransferMap.ApplyTransferMap(sourceTexture, transferMapPath))
                {
                    TexIO.SaveBitmap(result, outputImage);
                }
            }
        }

    }
}
