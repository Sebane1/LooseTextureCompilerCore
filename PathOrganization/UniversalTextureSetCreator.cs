using FFXIVLooseTextureCompiler.Export;
using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.Racial;
using LooseTextureCompilerCore;
using LooseTextureCompilerCore.Export;
using Penumbra.GameData.Enums;
using System.Diagnostics;

namespace FFXIVLooseTextureCompiler.PathOrganization
{
    public static class UniversalTextureSetCreator
    {
        public static bool UseMemoryCache { get; set; } = true;
        /// <summary>
        /// Adds children to a primary texture set if asym.
        /// </summary>
        /// <param name="textureSet"></param>
        public static void ConfigureTextureSet(TextureSet textureSet)
        {
            textureSet.ChildSets.Clear();
            int race = RaceInfo.ReverseRaceLookup(textureSet.InternalBasePath);
            if (((textureSet.InternalBasePath.Contains("0001_d.tex") &&
                !textureSet.InternalBasePath.Contains("fac"))
                || (textureSet.InternalBasePath.Contains("0101_d.tex")
                && !textureSet.InternalBasePath.Contains("fac")))
                && !textureSet.InternalBasePath.Contains("--c1101b0001_"))
            {
                ConfigureVanillaFemaleCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("fac_b"))
            {
                ConfigureAsymFaceCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("fac_"))
            {
                ConfigureVanillaFaceCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("bibo"))
            {
                ConfigureBiboFemaleCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("gen3"))
            {
                ConfigureGen3FemaleCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("chara/human/c1101/obj/body/b0001/texture/v01_c1101b0001_g_d"))
            {
                ConfigureOtopopCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("--c1101b0001_"))
            {
                ConfigureLalafellVanillaCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("v01_c1101b0001_b"))
            {
                ConfigureAsymLalafellCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("_b_d")
                && !textureSet.InternalBasePath.Contains("fac"))
            {
                ConfigureTBSECrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
            else if (textureSet.InternalBasePath.Contains("relala"))
            {
                ConfigureRelalaCrossCompatibility(textureSet, race, textureSet.OmniExportMode);
            }
        }
        public static List<string> GetSkinTypeNames(TextureSet textureSet)
        {
            int race = RaceInfo.ReverseRaceLookup(textureSet.InternalBasePath);
            if (((textureSet.InternalBasePath.Contains("0001_base.tex") &&
                !textureSet.InternalBasePath.Contains("fac"))
                || (textureSet.InternalBasePath.Contains("0101_base.tex")
                && !textureSet.InternalBasePath.Contains("fac")))
                && !textureSet.InternalBasePath.Contains("--c1101b0001_"))
            {
                return GetSkinNames(BackupTexturePaths.Gen3SkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("fac_b"))
            {
                return null;
            }
            else if (textureSet.InternalBasePath.Contains("fac_"))
            {
                return null;
            }
            else if (textureSet.InternalBasePath.Contains("bibo"))
            {
                return GetSkinNames(BackupTexturePaths.BiboSkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("gen3"))
            {
                return GetSkinNames(BackupTexturePaths.Gen3SkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("chara/human/c1101/obj/body/b0001/texture/v01_c1101b0001_g"))
            {
                return GetSkinNames(BackupTexturePaths.OtopopSkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("--c1101b0001_"))
            {
                return GetSkinNames(BackupTexturePaths.OtopopSkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("v01_c1101b0001_b"))
            {
                return GetSkinNames(BackupTexturePaths.OtopopSkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("_b_base")
                && !textureSet.InternalBasePath.Contains("fac"))
            {
                return GetSkinNames(BackupTexturePaths.TbseSkinTypes);
            }
            else if (textureSet.InternalBasePath.Contains("relala"))
            {
                return GetSkinNames(BackupTexturePaths.RelalaSkinTypes);
            }
            return null;
        }

        private static List<string> GetSkinNames(List<SkinType> skinTypes)
        {
            List<string> strings = new List<string>();
            foreach (SkinType skinType in skinTypes)
            {
                strings.Add(skinType.Name);
            }
            return strings;
        }
        private static void ConfigureAsymFaceCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 0, 0, null, textureSet, 1);
            //BackupTexturePaths.AddFaceBackupPaths(0, race, 0, textureSet);

            if (omniExport)
            {
                TextureSet faceVanilla = new TextureSet();
                ConfigureTextureSet("Vanilla Face [IsChild]", "face_vanilla", race, 0, 0, faceVanilla, textureSet, 1, true);

                if (!string.IsNullOrEmpty(textureSet.Base))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Base), faceVanilla.Base);
                }

                if (!string.IsNullOrEmpty(textureSet.Normal))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Normal), faceVanilla.Normal);
                }

                if (!string.IsNullOrEmpty(textureSet.Mask))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Mask), faceVanilla.Mask);
                }

                if (!string.IsNullOrEmpty(textureSet.Glow))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Glow), faceVanilla.Glow);
                }

                if (!string.IsNullOrEmpty(textureSet.NormalMask))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.NormalMask), faceVanilla.NormalMask);
                }
                textureSet.ChildSets.Add(faceVanilla);
            }
        }

        private static void ConfigureVanillaFaceCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 0, 0, null, textureSet, 1);
            //BackupTexturePaths.AddFaceBackupPaths(0, race, 0, textureSet);

            if (omniExport)
            {
                TextureSet asymFace = new TextureSet();
                ConfigureTextureSet("Asym Face [IsChild]", "face_asym", race, 0, 0, asymFace, textureSet, 1);

                if (!string.IsNullOrEmpty(textureSet.Base))
                {
                    TexIO.SaveBitmap(ImageManipulation.MirrorAndDuplicate(TexIO.ResolveBitmap(textureSet.Base)), asymFace.Base);
                }

                if (!string.IsNullOrEmpty(textureSet.Normal))
                {
                    TexIO.SaveBitmap(ImageManipulation.MirrorAndDuplicate(TexIO.ResolveBitmap(textureSet.Normal)), asymFace.Normal);
                }
                if (!string.IsNullOrEmpty(textureSet.Mask))
                {
                    TexIO.SaveBitmap(ImageManipulation.MirrorAndDuplicate(TexIO.ResolveBitmap(textureSet.Mask)), asymFace.Mask);
                }

                if (!string.IsNullOrEmpty(textureSet.Glow))
                {
                    TexIO.SaveBitmap(ImageManipulation.MirrorAndDuplicate(TexIO.ResolveBitmap(textureSet.Glow)), asymFace.Glow);
                }

                if (!string.IsNullOrEmpty(textureSet.NormalMask))
                {
                    TexIO.SaveBitmap(ImageManipulation.MirrorAndDuplicate(TexIO.ResolveBitmap(textureSet.NormalMask)), asymFace.NormalMask);
                }
                textureSet.ChildSets.Add(asymFace);
            }
        }

        private static void ConfigureTextureSet(string name, string prefix, int race, int gender, int body,
            TextureSet destinationTextureSet, TextureSet baseTextureSet, int bodyPart = 0, bool asymFace = false, bool uniqueAuRa = false)
        {
            TextureSet newTextureSet = destinationTextureSet != null ? destinationTextureSet : baseTextureSet;
            newTextureSet.TextureSetName = name;
            switch (bodyPart)
            {
                case 0:
                    if (string.IsNullOrEmpty(newTextureSet.InternalBasePath))
                    {
                        newTextureSet.InternalBasePath = RacePaths.GetBodyTexturePath(0, gender, body, race, 0, uniqueAuRa);
                    }
                    if (string.IsNullOrEmpty(newTextureSet.InternalNormalPath))
                    {
                        newTextureSet.InternalNormalPath = RacePaths.GetBodyTexturePath(1, gender, body, race, 0, uniqueAuRa);
                    }
                    if (string.IsNullOrEmpty(newTextureSet.InternalMaskPath))
                    {
                        newTextureSet.InternalMaskPath = RacePaths.GetBodyTexturePath(2, gender, body, race, 0, uniqueAuRa);
                    }
                    if (string.IsNullOrEmpty(newTextureSet.InternalMaterialPath))
                    {
                        newTextureSet.InternalMaterialPath = RacePaths.GetBodyMaterialPath(gender, body, race, 0);
                    }
                    break;
                case 1:
                    if (destinationTextureSet != null)
                    {
                        if (asymFace)
                        {
                            newTextureSet.InternalBasePath = baseTextureSet.InternalBasePath.Replace("fac_b_", "fac_");
                            newTextureSet.InternalNormalPath = baseTextureSet.InternalNormalPath.Replace("fac_b_", "fac_");
                            newTextureSet.InternalMaskPath = baseTextureSet.InternalMaskPath.Replace("fac_b_", "fac_");
                        }
                        else
                        {
                            newTextureSet.InternalBasePath = baseTextureSet.InternalBasePath.Replace("fac_", "fac_b_");
                            newTextureSet.InternalNormalPath = baseTextureSet.InternalNormalPath.Replace("fac_", "fac_b_");
                            newTextureSet.InternalMaskPath = baseTextureSet.InternalMaskPath.Replace("fac_", "fac_b_");
                        }
                    }
                    if (string.IsNullOrEmpty(newTextureSet.InternalBasePath))
                    {
                        newTextureSet.InternalMaterialPath = newTextureSet.InternalBasePath.Replace("texture/c", "material/mt_c")
                        .Replace("b_base", "_b").Replace("face_base", "_a").Replace(".tex", ".mtrl");
                    }
                    break;
            }
            if (destinationTextureSet != null)
            {
                string extension = FFXIVLooseTextureCompiler.ImageProcessing.UVTransferMap.UseGPUAcceleration ? ".raw" : ".png";
                string memPrefix = (FFXIVLooseTextureCompiler.ImageProcessing.UVTransferMap.UseGPUAcceleration && UseMemoryCache) ? "memory://" : "";

                destinationTextureSet.Base = memPrefix + ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(baseTextureSet.FinalBase, $"_{prefix}_d_baseTexBaked"), extension);
                destinationTextureSet.Normal = memPrefix + ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(baseTextureSet.FinalNormal, $"_{prefix}_n_baseTexBaked"), extension);
                destinationTextureSet.Mask = memPrefix + ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(baseTextureSet.FinalMask, $"_{prefix}_m_baseTexBaked"), extension);
                destinationTextureSet.Glow = memPrefix + ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(baseTextureSet.Glow, $"_{prefix}_g_baseTexBaked"), extension);
                destinationTextureSet.NormalMask = memPrefix + ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(baseTextureSet.NormalMask, $"_{prefix}_nm_baseTexBaked"), extension);
                destinationTextureSet.IgnoreNormalGeneration = baseTextureSet.IgnoreNormalGeneration;
                destinationTextureSet.IgnoreMaskGeneration = baseTextureSet.IgnoreMaskGeneration;
                destinationTextureSet.InvertNormalGeneration = baseTextureSet.InvertNormalGeneration;
                destinationTextureSet.Material = baseTextureSet.Material;
            }
        }

        private static void ConfigureTBSECrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 0, 3, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.TbseOverride : BackupTexturePaths.TbseSkinTypes[textureSet.SkinType].BackupTextures[0];
            TextureSet tbseVanilla = new TextureSet();

            if (omniExport)
            {
                ConfigureTextureSet("Vanilla [IsChild]", "tbse_vanilla", race, 0, 0, tbseVanilla, textureSet);
                tbseVanilla.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.TbseSkinTypes[textureSet.SkinType].BackupTextures[3];

                Directory.CreateDirectory(
                    Path.GetDirectoryName(
                    Path.Combine(
                    GlobalPathStorage.OriginalBaseDirectory,
                    tbseVanilla.BackupTexturePaths.Base)));

                string vanillaBase =
                    Path.Combine(GlobalPathStorage.OriginalBaseDirectory,
                    tbseVanilla.BackupTexturePaths.Base);
                if (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(vanillaBase))
                {
                    TexIO.WriteImageToXOR(ImageManipulation.CutInHalf(
                        TexIO.ResolveBitmap(
                        Path.Combine(GlobalPathStorage.OriginalBaseDirectory,
                        textureSet.BackupTexturePaths.Base))), vanillaBase);
                }
                string vanillaRaen =
                     Path.Combine(GlobalPathStorage.OriginalBaseDirectory,
                     tbseVanilla.BackupTexturePaths.BaseSecondary);
                if (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(vanillaRaen))
                {
                    TexIO.WriteImageToXOR(ImageManipulation.CutInHalf(
                         TexIO.ResolveBitmap(
                         Path.Combine(GlobalPathStorage.OriginalBaseDirectory,
                         textureSet.BackupTexturePaths.BaseSecondary))), vanillaRaen);
                }
                string vanillaNormal = Path.Combine(GlobalPathStorage.OriginalBaseDirectory,
                    tbseVanilla.BackupTexturePaths.Normal);
                if (!FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(vanillaNormal))
                {
                    TexIO.WriteImageToXOR(ImageManipulation.CutInHalf(
                        TexIO.ResolveBitmap(Path.Combine(GlobalPathStorage.OriginalBaseDirectory,
                        textureSet.BackupTexturePaths.Normal))), vanillaNormal);
                }
                if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(textureSet.Base))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Base), tbseVanilla.Base);
                }
                if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(textureSet.Normal))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Normal), tbseVanilla.Normal);
                }
                if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(textureSet.Mask))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Mask), tbseVanilla.Mask);
                }
                if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(textureSet.Glow))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Glow), tbseVanilla.Glow);
                }
                if (FFXIVLooseTextureCompiler.ImageProcessing.TexIO.Exists(textureSet.NormalMask))
                {
                    TexIO.SaveBitmap(TexIO.ResolveBitmap(textureSet.Glow), tbseVanilla.NormalMask);
                }

                textureSet.ChildSets.Add(tbseVanilla);
            }
        }

        private static void ConfigureLalafellVanillaCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 0, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[2];

            if (omniExport)
            {
                TextureSet otopop = new TextureSet();
                ConfigureTextureSet("Otopop [IsChild]", "otopop", race, 1, 5, otopop, textureSet);
                otopop.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.OtopopOverride : BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[1];

                TextureSet asymLalaFell = new TextureSet();
                ConfigureTextureSet("Asym Lala [IsChild]", "asym_lala", race, 0, 6, asymLalaFell, textureSet);
                asymLalaFell.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.AsymOverridePath() : BackupTexturePaths.AsymLalaPath(textureSet.SkinType);

                TextureSet relala = new TextureSet();
                ConfigureTextureSet("ReLala [IsChild]", "relala", race, 1, 7, relala, textureSet);
                relala.BackupTexturePaths = BackupTexturePaths.RelalaSkinTypes[textureSet.SkinType].BackupTextures[0];

                textureSet.ChildSets.Add(asymLalaFell);
                textureSet.ChildSets.Add(otopop);
                textureSet.ChildSets.Add(relala);
            }
        }

        private static void ConfigureAsymLalafellCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 6, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.AsymOverridePath() : BackupTexturePaths.AsymLalaPath(textureSet.SkinType);

            if (omniExport)
            {
                TextureSet otopop = new TextureSet();
                ConfigureTextureSet("Otopop [IsChild]", "otopop", race, 0, 5, otopop, textureSet);
                otopop.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.OtopopOverride : BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[1];

                TextureSet vanilla = new TextureSet();
                ConfigureTextureSet("Vanilla [IsChild]", "vanilla_lala", race, 0, 0, vanilla, textureSet);
                vanilla.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[0];

                TextureSet relala = new TextureSet();
                ConfigureTextureSet("ReLala [IsChild]", "relala", race, 1, 7, relala, textureSet);
                relala.BackupTexturePaths = BackupTexturePaths.RelalaSkinTypes[textureSet.SkinType].BackupTextures[0];

                textureSet.ChildSets.Add(vanilla);
                textureSet.ChildSets.Add(otopop);
                textureSet.ChildSets.Add(relala);
            }
        }

        private static void ConfigureOtopopCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 5, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.OtopopOverride : BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[1];

            if (omniExport)
            {
                TextureSet vanilla = new TextureSet();
                ConfigureTextureSet("Vanilla [IsChild]", "vanilla_lala", race, 0, 0, vanilla, textureSet);
                vanilla.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[0];

                TextureSet asymLalafell = new TextureSet();
                ConfigureTextureSet("Asym Lala [IsChild]", "asym_lala", race, 0, 6, asymLalafell, textureSet);
                asymLalafell.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.AsymOverridePath() : BackupTexturePaths.AsymLalaPath(textureSet.SkinType);

                TextureSet relala = new TextureSet();
                ConfigureTextureSet("ReLala [IsChild]", "relala", race, 1, 7, relala, textureSet);
                relala.BackupTexturePaths = BackupTexturePaths.RelalaSkinTypes[textureSet.SkinType].BackupTextures[0];

                textureSet.ChildSets.Add(asymLalafell);
                textureSet.ChildSets.Add(vanilla);
                textureSet.ChildSets.Add(relala);
            }
        }

        private static void ConfigureRelalaCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 7, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.RelalaOverride : BackupTexturePaths.RelalaSkinTypes[textureSet.SkinType].BackupTextures[0];

            if (omniExport)
            {
                TextureSet vanilla = new TextureSet();
                ConfigureTextureSet("Vanilla [IsChild]", "vanilla_lala", race, 0, 0, vanilla, textureSet);
                vanilla.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.RelalaSkinTypes[textureSet.SkinType].BackupTextures[2];

                TextureSet otopop = new TextureSet();
                ConfigureTextureSet("Otopop [IsChild]", "otopop", race, 0, 5, otopop, textureSet);
                otopop.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.OtopopOverride : BackupTexturePaths.RelalaSkinTypes[textureSet.SkinType].BackupTextures[1];

                TextureSet asymLalafell = new TextureSet();
                ConfigureTextureSet("Asym Lala [IsChild]", "asym_lala", race, 0, 6, asymLalafell, textureSet);
                asymLalafell.BackupTexturePaths = BackupTexturePaths.AsymLalaPath(0);

                textureSet.ChildSets.Add(otopop);
                textureSet.ChildSets.Add(vanilla);
                textureSet.ChildSets.Add(asymLalafell);
            }
        }

        private static void ConfigureGen3FemaleCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 2, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen3Override : BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];

            if (omniExport)
            {
                TextureSet vanilla = new TextureSet();
                ConfigureTextureSet("Vanilla [IsChild]", "gen2", race, 1, 0, vanilla, textureSet);
                vanilla.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

                TextureSet bibo = new TextureSet();
                ConfigureTextureSet("Bibo+ [IsChild]", "bibo", race, 1, 1, bibo, textureSet);
                bibo.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.BiboOverride : BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[0];

                textureSet.ChildSets.Add(vanilla);
                textureSet.ChildSets.Add(bibo);
            }
        }

        private static void ConfigureBiboFemaleCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 1, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.BiboOverride : BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[0];

            if (omniExport)
            {
                TextureSet vanilla = new TextureSet();
                ConfigureTextureSet("Vanilla [IsChild]", "gen2", race, 1, 0, vanilla, textureSet);
                vanilla.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[2];

                TextureSet gen3 = new TextureSet();
                ConfigureTextureSet("Tight & Firm [IsChild]", "gen3", race, 1, 2, gen3, textureSet);
                gen3.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen3Override : BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[1];

                textureSet.ChildSets.Add(vanilla);
                textureSet.ChildSets.Add(gen3);
            }
        }

        private static void ConfigureVanillaFemaleCrossCompatibility(TextureSet textureSet, int race, bool omniExport)
        {
            ConfigureTextureSet(textureSet.TextureSetName, "", race, 1, 0, null, textureSet);
            textureSet.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen2Override : BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            if (omniExport)
            {
                TextureSet bibo = new TextureSet();
                ConfigureTextureSet("Bibo [IsChild]", "bibo", race, 1, 1, bibo, textureSet);
                bibo.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.BiboOverride : BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[0];

                TextureSet gen3 = new TextureSet();
                ConfigureTextureSet("Tight & Firm [IsChild]", "gen3", race, 1, 2, gen3, textureSet);
                gen3.BackupTexturePaths = BackupTexturePaths.OverrideMode ? BackupTexturePaths.Gen3Override : BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];

                textureSet.ChildSets.Add(bibo);
                textureSet.ChildSets.Add(gen3);
            }
        }
    }
}



