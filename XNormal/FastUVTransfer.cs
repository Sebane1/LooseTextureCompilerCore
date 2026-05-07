using FFXIVLooseTextureCompiler.ImageProcessing;
using LooseTextureCompilerCore;
using System.Drawing;
using System.IO;

namespace FFXIVLooseTextureCompiler
{
    public class FastUVTransfer
    {
        private static void PerformTransfer(string inputImage, string outputImage, string transferMapFilename, System.Action<string, string> xnormalFallback)
        {
            // If it's a normal map, we must use XNormal for proper tangent space re-calculation.
            if (ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal)
            {
                xnormalFallback(inputImage, outputImage);
                return;
            }

            string transferMapPath = Path.Combine(GlobalPathStorage.OriginalBaseDirectory, "res", "fastuvtransfer", "body", transferMapFilename);

            // If the map doesn't exist for some reason, fallback to XNormal
            if (!File.Exists(transferMapPath))
            {
                xnormalFallback(inputImage, outputImage);
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
            PerformTransfer(inputImage, outputImage, "bibo_to_gen2_transfer.tif", XNormal.BiboToGen2);
        }

        public static void BiboToGen3(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "bibo_to_gen3_transfer.tif", XNormal.BiboToGen3);
        }

        public static void Gen3ToGen2(string inputImage, string outputImage)
        {
            PerformTransfer(inputImage, outputImage, "gen3_to_gen2_transfer.tif", XNormal.Gen3ToGen2);
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
                if (outputPath.Contains("gen2")) BiboToGen2(inputPath, outputPath);
                else if (outputPath.Contains("gen3")) BiboToGen3(inputPath, outputPath);
                else wasHandled = false;
            }
            else if (internalPath.Contains("eve") || internalPath.Contains("gen3"))
            {
                if (outputPath.Contains("gen2")) Gen3ToGen2(inputPath, outputPath);
                else if (outputPath.Contains("bibo")) Gen3ToBibo(inputPath, outputPath);
                else wasHandled = false;
            }
            else if (internalPath.Contains("body"))
            {
                if (outputPath.Contains("bibo")) Gen2ToBibo(inputPath, outputPath);
                else if (outputPath.Contains("gen3")) Gen2ToGen3(inputPath, outputPath);
                else wasHandled = false;
            }
            else if (internalPath.Contains("skin_otopop") || internalPath.Contains("v01_c1101b0001_g"))
            {
                if (outputPath.Contains("vanilla_lala")) OtopopToVanillaLala(inputPath, outputPath);
                else wasHandled = false;
            }
            else if (internalPath.Contains("--c1101b0001"))
            {
                if (outputPath.Contains("otopop")) VanillaLalaToOtopop(inputPath, outputPath);
                else wasHandled = false;
            }
            else if (internalPath.Contains("v01_c1101b0001_b"))
            {
                if (outputPath.Contains("otopop")) AsymLalaToOtopop(inputPath, outputPath);
                else if (outputPath.Contains("vanilla_lala")) AsymLalaToVanillaLala(inputPath, outputPath);
                else wasHandled = false;
            }
            else
            {
                wasHandled = false;
            }

            // --- ADDITIVE MODULAR FALLBACK FOR FACES/OTHER MESHES ---
            // If the body path didn't explicitly handle it, but XNormal knows the mesh paths, we dynamically generate a transfer map!
            if (!wasHandled)
            {
                if (XNormal.TryGetMeshes(internalPath, outputPath, false, out string sourceMesh, out string targetMesh))
                {
                    PerformModularTransfer(sourceMesh, targetMesh, inputPath, outputPath);
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
