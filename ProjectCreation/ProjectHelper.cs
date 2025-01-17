using FFXIVLooseTextureCompiler.Export;
using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;
using FFXIVLooseTextureCompiler.Racial;
using FFXIVLooseTextureCompiler;
using LooseTextureCompilerCore.Racial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVLooseTextureCompiler.ImageProcessing.ImageManipulation;
using Penumbra.GameData.Enums;

namespace LooseTextureCompilerCore.ProjectCreation
{
    public static class ProjectHelper
    {
        static string[] _choiceTypes = new string[] { "Detailed", "Simple", "Dropdown", "Group Is Checkbox" };
        static string[] _bodyNames = new string[] { "Vanilla and Gen2", "BIBO+", "Gen3", "TBSE and HRBODY", "TAIL", "Otopop" };
        static string[] _bodyNamesSimplified = new string[] { "BIBO+", "Gen3", "TBSE and HRBODY", "Otopop" };
        static string[] _genders = new string[] { "Masculine", "Feminine" };
        static string[] _faceTypes = new string[] { "Face 1", "Face 2", "Face 3", "Face 4", "Face 5", "Face 6", "Face 7", "Face 8", "Face 9" };
        static string[] _faceParts = new string[] { "Face", "Eyebrows", "Eyes", "Ears", "Face Paint", "Hair", "Face B", "Etc B" };
        static string[] _faceScales = new string[] { "Vanilla Scales", "Scaleless Vanilla", "Scaleless Varied" };
        public static void ExportJson(string jsonFilePath)
        {
            string jsonText = @"{
  ""Name"": """",
  ""Priority"": 0,
  ""Files"": { },
  ""FileSwaps"": { },
  ""Manipulations"": []
}";
            if (jsonFilePath != null)
            {
                using (StreamWriter writer = new StreamWriter(jsonFilePath))
                {
                    writer.WriteLine(jsonText);
                }
            }
        }

        public static UVMapType SortUVTexture(TextureSet textureSet, string file)
        {
            bool foundStringIdentifier = false;
            UVMapType uVMapType = UVMapType.Base;
            if (file.ToLower().Contains("base"))
            {
                uVMapType = UVMapType.Base;
                foundStringIdentifier = true;
            }

            if (file.ToLower().Contains("norm"))
            {
                uVMapType = UVMapType.Normal;
                foundStringIdentifier = true;
            }

            if (file.ToLower().Contains("mask"))
            {
                uVMapType = UVMapType.Mask;
                foundStringIdentifier = true;
            }

            if (file.ToLower().Contains("glow"))
            {
                uVMapType = UVMapType.Glow;
                foundStringIdentifier = true;
            }



            if (!foundStringIdentifier)
            {
                uVMapType = ImageManipulation.UVMapTypeClassifier(file);
            }
            switch (uVMapType)
            {
                case UVMapType.Base:
                    textureSet.Base = file;
                    break;
                case UVMapType.Normal:
                    textureSet.Normal = file;
                    break;
                case UVMapType.Mask:
                    textureSet.Mask = file;
                    break;
                case UVMapType.Glow:
                    textureSet.Glow = file;
                    break;
            }
            return uVMapType;
        }
        public static void ExportMeta(string metaFilePath, string name, string author = "Loose Texture Compiler",
            string description = "Exported By Loose Texture Compiler", string modVersion = "0.0.0",
            string modWebsite = @"https://github.com/Sebane1/FFXIVLooseTextureCompiler")
        {
            string metaText = @"{
  ""FileVersion"": 3,
  ""Name"": """ + (!string.IsNullOrEmpty(name) ? name : "") + @""",
  ""Author"": """ + (!string.IsNullOrEmpty(author) ? author :
        "FFXIV Loose Texture Compiler") + @""",
  ""Description"": """ + (!string.IsNullOrEmpty(description) ? description :
        "Exported by FFXIV Loose Texture Compiler") + @""",
  ""Version"": """ + modVersion + @""",
  ""Website"": """ + modWebsite + @""",
  ""ModTags"": []
}";
            if (metaFilePath != null)
            {
                using (StreamWriter writer = new StreamWriter(metaFilePath))
                {
                    writer.WriteLine(metaText);
                }
            }
        }

        public static TextureSet CreateBodyTextureSet(int gender, int baseBody, int race, int tail, bool uniqueAuRa = false)
        {
            TextureSet textureSet = new TextureSet();
            textureSet.TextureSetName = _bodyNames[baseBody] + (_bodyNames[baseBody].ToLower().Contains("tail") ? " " +
                (tail + 1) : "") + ", " + (race == 5 ? "Unisex" : _genders[gender])
                + ", " + RaceInfo.Races[race];
            AddBodyPaths(textureSet, gender, baseBody, race, tail, uniqueAuRa);
            return textureSet;
        }

        public static TextureSet CreateFaceTextureSet(int faceType, int facePart, int faceExtra, int gender, int race, int subRace, int auraScales, bool asym)
        {
            TextureSet textureSet = new TextureSet();
            textureSet.TextureSetName = _faceParts[facePart] + (facePart == 4 ? " "
                + (faceExtra + 1) : "") + ", " + (facePart != 4 ? _genders[gender] : "Unisex")
                + ", " + (facePart != 4 ? RaceInfo.SubRaces[subRace] : "Multi Race") + ", "
                + (facePart != 4 ? _faceTypes[faceType] : "Multi Face");
            switch (facePart)
            {
                default:
                    AddFacePaths(textureSet, subRace, facePart, faceType, gender, auraScales, asym);
                    break;
                case 2:
                    AddEyePaths(textureSet, subRace, faceType, gender, auraScales, asym);
                    break;
                case 4:
                    AddDecalPath(textureSet, faceExtra);
                    break;
                case 5:
                    AddHairPaths(textureSet, gender, facePart, faceExtra, race, subRace);
                    break;
            }
            textureSet.IgnoreMultiGeneration = true;
            if (facePart == 0)
            {
                BackupTexturePaths.AddFaceBackupPaths(gender, subRace, faceExtra, textureSet);
            }
            return textureSet;
        }
        private static void AddBodyPaths(TextureSet textureSet, int gender, int baseBody, int race, int tail, bool uniqueAuRa = false)
        {
            if (race != 3 || baseBody != 6)
            {
                textureSet.InternalBasePath = RacePaths.GetBodyTexturePath(0, gender, baseBody, race, tail, uniqueAuRa);
            }
            textureSet.InternalNormalPath = RacePaths.GetBodyTexturePath(1, gender, baseBody, race, tail, uniqueAuRa);
            textureSet.InternalMultiPath = RacePaths.GetBodyTexturePath(2, gender, baseBody, race, tail, uniqueAuRa);
            textureSet.InternalMaterialPath = RacePaths.GetBodyMaterialPath(gender, baseBody, race, tail);
            BackupTexturePaths.AddBodyBackupPaths(gender, race, textureSet);
        }

        private static void AddDecalPath(TextureSet textureSet, int faceExtra)
        {
            textureSet.InternalBasePath = RacePaths.GetFaceTexturePath(faceExtra);
        }

        private static void AddHairPaths(TextureSet textureSet, int gender, int facePart, int faceExtra, int race, int subrace)
        {
            textureSet.TextureSetName = _faceParts[facePart] + " " + (faceExtra + 1)
                + ", " + _genders[gender] + ", " + RaceInfo.Races[race];

            textureSet.InternalNormalPath = RacePaths.GetHairTexturePath(1, faceExtra,
                gender, race, subrace);

            textureSet.InternalMultiPath = RacePaths.GetHairTexturePath(2, faceExtra,
                gender, race, subrace);
        }

        public static void AddEyePaths(TextureSet textureSet, int subrace, int faceType, int gender, int auraScales, bool asym)
        {
            RaceEyePaths.GetEyeTextureSet(subrace, faceType, gender == 1, textureSet);
        }

        public static void AddFacePaths(TextureSet textureSet, int subrace, int facePart, int faceType, int gender, int auraScales, bool asym)
        {
            if (facePart != 1)
            {
                textureSet.InternalBasePath = RacePaths.GetFacePath(0, gender, subrace,
                    facePart, faceType, auraScales, asym);
            }

            textureSet.InternalNormalPath = RacePaths.GetFacePath(1, gender, subrace,
            facePart, faceType, auraScales, asym);

            textureSet.InternalMaskPath = RacePaths.GetFacePath(2, gender, subrace,
            facePart, faceType, auraScales, asym);
        }
    }
}
