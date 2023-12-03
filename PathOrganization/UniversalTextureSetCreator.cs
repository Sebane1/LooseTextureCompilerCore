using FFXIVLooseTextureCompiler.Export;
using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.Racial;
using LooseTextureCompilerCore.Export;

namespace FFXIVLooseTextureCompiler.PathOrganization {
    public static class UniversalTextureSetCreator {
        /// <summary>
        /// Adds children to a primary texture set.
        /// </summary>
        /// <param name="textureSet"></param>
        public static void ConfigureOmniConfiguration(TextureSet textureSet) {
            textureSet.OmniExportMode = true;
            textureSet.ChildSets.Clear();
            int race = RaceInfo.ReverseRaceLookup(textureSet.InternalDiffusePath);
            if (((textureSet.InternalDiffusePath.Contains("0001_d.tex") &&
                !textureSet.InternalDiffusePath.Contains("fac"))
                || (textureSet.InternalDiffusePath.Contains("0101_d.tex")
                && !textureSet.InternalDiffusePath.Contains("fac")))
                && !textureSet.InternalDiffusePath.Contains("--c1101b0001_")) {
                ConfigureVanillaFemaleCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("fac_b")) {
                ConfigureAsymFaceCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("fac_")) {
                ConfigureVanillaFaceCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("bibo")) {
                ConfigureBiboFemaleCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("eve")) {
                ConfigureEveFemaleCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("gen3")) {
                ConfigureGen3FemaleCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("chara/human/c1101/obj/body/b0001/texture/v01_c1101b0001_g_d")) {
                ConfigureOtopopCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("--c1101b0001_")) {
                ConfigureLalafellVanillaCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("v01_c1101b0001_b")) {
                ConfigureAsymLalafellCrossCompatibility(textureSet, race);
            } else if (textureSet.InternalDiffusePath.Contains("_b_d")
                && !textureSet.InternalDiffusePath.Contains("fac")) {
                ConfigureTBSECrossCompatibility(textureSet, race);
            }
        }
        public static List<string> GetSkinTypeNames(TextureSet textureSet) {
            textureSet.OmniExportMode = true;
            textureSet.ChildSets.Clear();
            int race = RaceInfo.ReverseRaceLookup(textureSet.InternalDiffusePath);
            if (((textureSet.InternalDiffusePath.Contains("0001_d.tex") &&
                !textureSet.InternalDiffusePath.Contains("fac"))
                || (textureSet.InternalDiffusePath.Contains("0101_d.tex")
                && !textureSet.InternalDiffusePath.Contains("fac")))
                && !textureSet.InternalDiffusePath.Contains("--c1101b0001_")) {
                return GetSkinNames(BackupTexturePaths.Gen3SkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("fac_b")) {
                return null;
            } else if (textureSet.InternalDiffusePath.Contains("fac_")) {
                return null;
            } else if (textureSet.InternalDiffusePath.Contains("bibo")) {
                return GetSkinNames(BackupTexturePaths.BiboSkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("eve")) {
                return GetSkinNames(BackupTexturePaths.Gen3SkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("gen3")) {
                return GetSkinNames(BackupTexturePaths.Gen3SkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("chara/human/c1101/obj/body/b0001/texture/v01_c1101b0001_g_d")) {
                return GetSkinNames(BackupTexturePaths.OtopopSkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("--c1101b0001_")) {
                return GetSkinNames(BackupTexturePaths.OtopopSkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("v01_c1101b0001_b")) {
                return GetSkinNames(BackupTexturePaths.OtopopSkinTypes);
            } else if (textureSet.InternalDiffusePath.Contains("_b_d")
                && !textureSet.InternalDiffusePath.Contains("fac")) {
                return GetSkinNames(BackupTexturePaths.TbseSkinTypes);
            }
            return null;
        }

        private static List<string> GetSkinNames(List<SkinType> skinTypes) {
            List<string> strings = new List<string>();
            foreach (SkinType skinType in skinTypes) {
                strings.Add(skinType.Name);
            }
            return strings;
        }
        private static void ConfigureAsymFaceCrossCompatibility(TextureSet textureSet, int race) {
            TextureSet faceVanilla = new TextureSet();
            ConfigureTextureSet("Vanilla Face [IsChild]", "face_vanilla", race, 0, 0, faceVanilla, textureSet, 1, true);

            if (!string.IsNullOrEmpty(textureSet.Diffuse)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Diffuse)).Save(faceVanilla.Diffuse);
            }

            if (!string.IsNullOrEmpty(textureSet.Normal)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Normal)).Save(faceVanilla.Normal);
            }

            if (!string.IsNullOrEmpty(textureSet.Multi)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Multi)).Save(faceVanilla.Multi);
            }

            if (!string.IsNullOrEmpty(textureSet.Glow)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Glow)).Save(faceVanilla.Glow);
            }

            if (!string.IsNullOrEmpty(textureSet.NormalMask)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.NormalMask)).Save(faceVanilla.NormalMask);
            }
            textureSet.BackupTexturePaths = null;
            textureSet.ChildSets.Add(faceVanilla);
        }

        private static void ConfigureVanillaFaceCrossCompatibility(TextureSet textureSet, int race) {
            TextureSet asymFace = new TextureSet();
            ConfigureTextureSet("Asym Face [IsChild]", "face_asym", race, 0, 0, asymFace, textureSet, 1);

            if (!string.IsNullOrEmpty(textureSet.Diffuse)) {
                ImageManipulation.MirrorAndDuplicate(TexLoader.ResolveBitmap(textureSet.Diffuse)).Save(asymFace.Diffuse);
            }

            if (!string.IsNullOrEmpty(textureSet.Normal)) {
                ImageManipulation.MirrorAndDuplicate(TexLoader.ResolveBitmap(textureSet.Normal)).Save(asymFace.Normal);
            }
            if (!string.IsNullOrEmpty(textureSet.Multi)) {
                ImageManipulation.MirrorAndDuplicate(TexLoader.ResolveBitmap(textureSet.Multi)).Save(asymFace.Multi);
            }

            if (!string.IsNullOrEmpty(textureSet.Glow)) {
                ImageManipulation.MirrorAndDuplicate(TexLoader.ResolveBitmap(textureSet.Glow)).Save(asymFace.Glow);
            }

            if (!string.IsNullOrEmpty(textureSet.NormalMask)) {
                ImageManipulation.MirrorAndDuplicate(TexLoader.ResolveBitmap(textureSet.NormalMask)).Save(asymFace.NormalMask);
            }
            textureSet.BackupTexturePaths = null;
            textureSet.ChildSets.Add(asymFace);
        }

        private static void ConfigureTextureSet(string name, string prefix, int race, int gender, int body,
            TextureSet destinationTextureSet, TextureSet baseTextureSet, int bodyPart = 0, bool asymFace = false, bool uniqueAuRa = false) {
            destinationTextureSet.TextureSetName = name;
            switch (bodyPart) {
                case 0:
                    destinationTextureSet.InternalDiffusePath = RacePaths.GetBodyTexturePath(0, gender, body, race, 0, uniqueAuRa);
                    destinationTextureSet.InternalNormalPath = RacePaths.GetBodyTexturePath(1, gender, body, race, 0, uniqueAuRa);
                    destinationTextureSet.InternalMultiPath = RacePaths.GetBodyTexturePath(2, gender, body, race, 0, uniqueAuRa);
                    break;
                case 1:
                    if (asymFace) {
                        destinationTextureSet.InternalDiffusePath = baseTextureSet.InternalDiffusePath.Replace("fac_b_", "fac_");
                        destinationTextureSet.InternalNormalPath = baseTextureSet.InternalNormalPath.Replace("fac_b_", "fac_");
                        destinationTextureSet.InternalMultiPath = baseTextureSet.InternalMultiPath.Replace("fac_b_", "fac_");
                    } else {
                        destinationTextureSet.InternalDiffusePath = baseTextureSet.InternalDiffusePath.Replace("fac_", "fac_b_");
                        destinationTextureSet.InternalNormalPath = baseTextureSet.InternalNormalPath.Replace("fac_", "fac_b_");
                        destinationTextureSet.InternalMultiPath = baseTextureSet.InternalMultiPath.Replace("fac_", "fac_b_");
                    }
                    destinationTextureSet.BackupTexturePaths = null;
                    break;
            }
            destinationTextureSet.Diffuse = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(baseTextureSet.Diffuse, $"_{prefix}_d_baseTexBaked"), ".png");
            destinationTextureSet.Normal = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(baseTextureSet.Normal, $"_{prefix}_n_baseTexBaked"), ".png");
            destinationTextureSet.Multi = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(baseTextureSet.Multi, $"_{prefix}_m_baseTexBaked"), ".png");
            destinationTextureSet.Glow = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(baseTextureSet.Glow, $"_{prefix}_g_baseTexBaked"), ".png");
            destinationTextureSet.NormalMask = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(baseTextureSet.NormalMask, $"_{prefix}_nm_baseTexBaked"), ".png");
            destinationTextureSet.IgnoreNormalGeneration = baseTextureSet.IgnoreNormalGeneration;
            destinationTextureSet.IgnoreMultiGeneration = baseTextureSet.IgnoreMultiGeneration;
            destinationTextureSet.InvertNormalGeneration = baseTextureSet.InvertNormalGeneration;
        }

        private static void ConfigureTextureSet(string name, int race, int gender, int body,
           TextureSet destinationTextureSet, TextureSet baseTextureSet) {
            destinationTextureSet.TextureSetName = name;
            destinationTextureSet.InternalDiffusePath = RacePaths.GetBodyTexturePath(0, gender, body, 0, race);
            destinationTextureSet.InternalNormalPath = RacePaths.GetBodyTexturePath(1, gender, body, 0, race);
            destinationTextureSet.InternalMultiPath = RacePaths.GetBodyTexturePath(2, gender, body, 0, race);
            destinationTextureSet.Diffuse = baseTextureSet.Diffuse;
            destinationTextureSet.Normal = baseTextureSet.Normal;
            destinationTextureSet.Multi = baseTextureSet.Multi;
            destinationTextureSet.Glow = baseTextureSet.Glow;
            destinationTextureSet.NormalMask = baseTextureSet.NormalMask;
            destinationTextureSet.IgnoreNormalGeneration = baseTextureSet.IgnoreNormalGeneration;
            destinationTextureSet.IgnoreMultiGeneration = baseTextureSet.IgnoreMultiGeneration;
            destinationTextureSet.InvertNormalGeneration = baseTextureSet.InvertNormalGeneration;
        }


        private static void ConfigureTBSECrossCompatibility(TextureSet textureSet, int race) {
            TextureSet tbseVanilla = new TextureSet();
            ConfigureTextureSet("Vanilla [IsChild]", "tbse_vanilla", race, 0, 0, tbseVanilla, textureSet);
            tbseVanilla.BackupTexturePaths = BackupTexturePaths.TbseSkinTypes[textureSet.SkinType].BackupTextures[3];

            Directory.CreateDirectory(
                Path.GetDirectoryName(
                Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                tbseVanilla.BackupTexturePaths.Diffuse)));

            string vanillaDiffuse =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                tbseVanilla.BackupTexturePaths.Diffuse);
            if (!File.Exists(vanillaDiffuse)) {
                TexLoader.WriteImageToXOR(ImageManipulation.CutInHalf(
                    TexLoader.ResolveBitmap(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    textureSet.BackupTexturePaths.Diffuse))), vanillaDiffuse);
            }
            string vanillaRaen =
                 Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                 tbseVanilla.BackupTexturePaths.DiffuseRaen);
            if (!File.Exists(vanillaRaen)) {
                TexLoader.WriteImageToXOR(ImageManipulation.CutInHalf(
                     TexLoader.ResolveBitmap(
                     Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                     textureSet.BackupTexturePaths.DiffuseRaen))), vanillaRaen);
            }
            string vanillaNormal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                tbseVanilla.BackupTexturePaths.Normal);
            if (!File.Exists(vanillaNormal)) {
                TexLoader.WriteImageToXOR(ImageManipulation.CutInHalf(
                    TexLoader.ResolveBitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    textureSet.BackupTexturePaths.Normal))), vanillaNormal);
            }
            if (File.Exists(textureSet.Diffuse)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Diffuse)).Save(tbseVanilla.Diffuse);
            }
            if (File.Exists(textureSet.Normal)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Normal)).Save(tbseVanilla.Normal);
            }
            if (File.Exists(textureSet.Multi)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Multi)).Save(tbseVanilla.Multi);
            }
            if (File.Exists(textureSet.Glow)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Glow)).Save(tbseVanilla.Glow);
            }
            if (File.Exists(textureSet.NormalMask)) {
                ImageManipulation.CutInHalf(TexLoader.ResolveBitmap(textureSet.Glow)).Save(tbseVanilla.NormalMask);
            }

            textureSet.ChildSets.Add(tbseVanilla);
        }

        private static void ConfigureLalafellVanillaCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet otopop = new TextureSet();
            ConfigureTextureSet("Otopop [IsChild]", "otopop", race, 1, 7, otopop, textureSet);
            otopop.BackupTexturePaths = BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[1];

            TextureSet asymLalaFell = new TextureSet();
            ConfigureTextureSet("Asym Lala [IsChild]", "asym_lala", race, 0, 8, asymLalaFell, textureSet);
            asymLalaFell.BackupTexturePaths = BackupTexturePaths.AsymLalaPath(textureSet.SkinType);

            textureSet.ChildSets.Add(asymLalaFell);
            textureSet.ChildSets.Add(otopop);
        }

        private static void ConfigureAsymLalafellCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.AsymLalaPath(textureSet.SkinType);

            TextureSet otopop = new TextureSet();
            ConfigureTextureSet("Otopop [IsChild]", "otopop", race, 0, 7, otopop, textureSet);
            otopop.BackupTexturePaths = BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[1];

            TextureSet vanilla = new TextureSet();
            ConfigureTextureSet("Vanilla [IsChild]", "vanilla_lala", race, 0, 0, vanilla, textureSet);
            vanilla.BackupTexturePaths = BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[0];

            textureSet.ChildSets.Add(vanilla);
            textureSet.ChildSets.Add(otopop);
        }

        private static void ConfigureOtopopCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[1];

            TextureSet vanilla = new TextureSet();
            ConfigureTextureSet("Vanilla [IsChild]", "vanilla_lala", race, 0, 0, vanilla, textureSet);
            vanilla.BackupTexturePaths = BackupTexturePaths.OtopopSkinTypes[textureSet.SkinType].BackupTextures[0];

            TextureSet asymLalafell = new TextureSet();
            ConfigureTextureSet("Asym Lala [IsChild]", "asym_lala", race, 0, 8, asymLalafell, textureSet);
            asymLalafell.BackupTexturePaths = BackupTexturePaths.AsymLalaPath(textureSet.SkinType);

            textureSet.ChildSets.Add(asymLalafell);
            textureSet.ChildSets.Add(vanilla);
        }

        private static void ConfigureGen3FemaleCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];

            TextureSet vanilla = new TextureSet();
            ConfigureTextureSet("Vanilla [IsChild]", "gen2", race, 1, 0, vanilla, textureSet);
            vanilla.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet bibo = new TextureSet();
            ConfigureTextureSet("Bibo+ [IsChild]", "bibo", race, 1, 1, bibo, textureSet);
            bibo.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[0];

            TextureSet eve = new TextureSet();
            ConfigureTextureSet("Eve [IsChild]", race, 1, 2, eve, textureSet);
            eve.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            textureSet.ChildSets.Add(vanilla);
            textureSet.ChildSets.Add(bibo);
            textureSet.ChildSets.Add(eve);
        }

        private static void ConfigureEveFemaleCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet vanilla = new TextureSet();
            ConfigureTextureSet("Vanilla [IsChild]", "gen2", race, 1, 0, vanilla, textureSet);
            vanilla.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet bibo = new TextureSet();
            ConfigureTextureSet("Bibo+ [IsChild]", "bibo", race, 1, 1, bibo, textureSet);
            bibo.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[0];

            TextureSet gen3 = new TextureSet();
            ConfigureTextureSet("Tight & Firm [IsChild]", race, 1, 3, gen3, textureSet);
            gen3.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            textureSet.ChildSets.Add(vanilla);
            textureSet.ChildSets.Add(bibo);
            textureSet.ChildSets.Add(gen3);
        }

        private static void ConfigureBiboFemaleCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[0];

            TextureSet vanilla = new TextureSet();
            ConfigureTextureSet("Vanilla [IsChild]", "gen2", race, 1, 0, vanilla, textureSet);
            vanilla.BackupTexturePaths = BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet eve = new TextureSet();
            ConfigureTextureSet("Eve [IsChild]", "gen3", race, 1, 2, eve, textureSet);
            eve.BackupTexturePaths = BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet gen3 = new TextureSet();
            ConfigureTextureSet("Tight & Firm [IsChild]", "gen3", race, 1, 3, gen3, textureSet);
            gen3.BackupTexturePaths = BackupTexturePaths.BiboSkinTypes[textureSet.SkinType].BackupTextures[0];

            textureSet.ChildSets.Add(vanilla);
            textureSet.ChildSets.Add(eve);
            textureSet.ChildSets.Add(gen3);
        }

        private static void ConfigureVanillaFemaleCrossCompatibility(TextureSet textureSet, int race) {
            textureSet.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];

            TextureSet bibo = new TextureSet();
            ConfigureTextureSet("Bibo [IsChild]", "bibo", race, 1, 1, bibo, textureSet);
            bibo.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[0];

            TextureSet eve = new TextureSet();
            ConfigureTextureSet("Eve[IsChild]", "gen3", race, 1, 2, eve, textureSet);
            eve.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];

            TextureSet gen3 = new TextureSet();
            ConfigureTextureSet("Tight & Firm [IsChild]", "gen3", race, 1, 3, gen3, textureSet);
            gen3.BackupTexturePaths = BackupTexturePaths.Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];

            textureSet.ChildSets.Add(bibo);
            textureSet.ChildSets.Add(eve);
            textureSet.ChildSets.Add(gen3);
        }
    }
}
