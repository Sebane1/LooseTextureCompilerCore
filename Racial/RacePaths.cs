using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Lumina.Data.Files.Pcb.PcbResourceFile;

namespace FFXIVLooseTextureCompiler.Racial {
    public static class RacePaths {
        private static bool otopopNotice;
        public static event EventHandler otopopNoticeTriggered;
        public static string VersionText { get; set; }
        public static string GetFacePath(int material, int gender, int subRaceValue, int facePart, int faceType, int auraFaceScales, bool asym, bool isMaterial = false) {
            string selectedText = RaceInfo.SubRaces[subRaceValue];
            string faceIdCheck = "00";
            if (facePart == 2 && asym) {
                if (selectedText.ToLower() == "the lost" || selectedText.ToLower() == "hellsguard" || selectedText.ToLower() == "highlander"
                    || selectedText.ToLower() == "duskwight" || selectedText.ToLower() == "keeper" || selectedText.ToLower() == "dunesfolk"
                    || (selectedText.ToLower() == "xaela") || (selectedText.ToLower() == "veena")) {
                    faceIdCheck = "10";
                }
                return "chara/asymeyes/" + RaceInfo.Races[RaceInfo.SubRaceToMainRace(subRaceValue)]
                    .ToLower().Replace("midlander", "hyur").Replace("highlander", "hyur")
                    .ToLower().Replace("xaela", "aura").Replace("raen", "aura")
                    .Replace(@"'", null) + "_"
                    + RaceInfo.SubRaces[subRaceValue].Replace(" ", null).ToLower()
                    + "_" + (gender == 0 ? "male" : "female") + "/f" + faceIdCheck + (faceType + 1)
                    + GetTextureType(material, 0, false, true) + (isMaterial ? ".mtrl" : ".tex");
            }
            if (material != 3) {
                faceIdCheck = "000";
                if (facePart < 3 && !isMaterial) {
                    return GetFacePathJson(material, gender, subRaceValue, facePart, faceType, auraFaceScales, asym);
                } else {
                    bool useSecondarySubracePrefix = selectedText.ToLower() == "the lost" || selectedText.ToLower() == "hellsguard" || selectedText.ToLower() == "highlander"
                        || selectedText.ToLower() == "duskwight" || selectedText.ToLower() == "keeper" || selectedText.ToLower() == "dunesfolk"
                        || (selectedText.ToLower() == "xaela" && facePart != 2 && (material == 0 || auraFaceScales == 2))
                        || (selectedText.ToLower() == "veena" && facePart == 1 && material != 2)
                        || (selectedText.ToLower() == "veena" && facePart == 2 && material == 2);
                    if (isMaterial) {
                        useSecondarySubracePrefix = selectedText.ToLower() == "the lost" || selectedText.ToLower() == "hellsguard" || selectedText.ToLower() == "highlander"
                        || selectedText.ToLower() == "duskwight" || selectedText.ToLower() == "keeper" || selectedText.ToLower() == "dunesfolk" || (selectedText.ToLower() == "xaela")
                        || (selectedText.ToLower() == "veena");
                    }
                    if (useSecondarySubracePrefix) {
                        faceIdCheck = "010";
                    }
                    string subRace = (gender == 0 ? RaceInfo.RaceCodeFace.Masculine[subRaceValue]
                        : RaceInfo.RaceCodeFace.Feminine[subRaceValue]);
                    int faceOffset = (faceType + (subRaceValue == 12 || subRaceValue == 13 ? 4 : 0)) + 1;
                    return "chara/human/c" + subRace + "/obj/face/f" + faceIdCheck + faceOffset + @"/" +
                        (isMaterial ? "material" : "texture") + (isMaterial ? "/mt_c" : "/c")
                        + subRace + "f" + faceIdCheck + faceOffset
                        + GetFacePart(facePart, asym) +
                        (isMaterial ? "" : GetTextureType(material, 0, true, true)) + (isMaterial ? ".mtrl" : ".tex");
                }
            }
            return "";
        }

        public static string GetFacePathJson(int material, int gender, int subRaceValue, int facePart,
                                             int faceType, int auraFaceScales, bool asym, bool isMaterial = false) {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            string maleFaces = resourceNames.LastOrDefault(str => str.Contains("maleface.json"));
            string femaleFaces = resourceNames.LastOrDefault(str => str.Contains("femaleface.json"));
            string maleEtcFaces = resourceNames.LastOrDefault(str => str.Contains("maleetc.json"));
            string femaleEtcFaces = resourceNames.LastOrDefault(str => str.Contains("femaleetc.json"));

            string maleFacePaths = "";
            string femaleFacePaths = "";
            string maleEtcFacePaths = "";
            string femaleEtcFacePaths = "";

            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(maleFaces))) {
                maleFacePaths = reader.ReadToEnd();
            }
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(femaleFaces))) {
                femaleFacePaths = reader.ReadToEnd();
            }
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(maleEtcFaces))) {
                maleEtcFacePaths = reader.ReadToEnd();
            }
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(femaleEtcFaces))) {
                femaleEtcFacePaths = reader.ReadToEnd();
            }

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> values = null;
            switch (facePart) {
                case 0:
                    switch (gender) {
                        case 0:
                            values = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(maleFacePaths);
                            break;
                        case 1:
                            values = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(femaleFacePaths);
                            break;
                    }
                    break;
                case 1:
                    switch (gender) {
                        case 0:
                            values = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(maleEtcFacePaths);
                            break;
                        case 1:
                            values = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(femaleEtcFacePaths);
                            break;
                    }
                    break;
            }

            try {
                string value = values.ElementAt(material).Value.ElementAt(subRaceValue).Value.ElementAt(faceType).Value;
                return value;
            } catch {
                return "Invalid selection";
            }
        }

        public static string PathCorrector(string path) {
            if (path.Contains("obj/face") || path.Contains("obj/body") || path.Contains("texture/eye") || path.Contains("obj/hair")) {
                path = path.Replace("--", null).Replace("_d.tex", "_base.tex").Replace("_n.tex", "_norm.tex").Replace("_s.tex", "_mask.tex");
            }
            if (path.Contains("bibo")) {
                if (path.Contains("_d") || path.Contains("_dif") || path.Contains("_base")) {
                    return BiboPathUpgraderBase(path);
                }
                if (path.Contains("_n") || path.Contains("_norm")) {
                    return BiboPathUpgraderNormal(path);
                }
                if (path.Contains("_s") || path.Contains("_m") || path.Contains("_mask")) {
                    return BiboPathUpgraderMask(path);
                }
            }
            return path;
        }
        public static string GetBodyTexturePath(int texture, int genderValue, int baseBody, int race, int tail, bool uniqueAuRa = false) {
            string result = "";
            string unique = RaceInfo.Races[race].Contains("Xaela") ? "0101" : "0001";
            switch (baseBody) {
                case 0:
                    // Vanila
                    if (texture == 2) {
                        result = @"chara/common/texture/skin_mask.tex";
                    } else {
                        string genderCode = (genderValue == 0 ? RaceInfo.RaceCodeBody.Masculine[race]
                            : RaceInfo.RaceCodeBody.Feminine[race]);
                        result = @"chara/human/c" + genderCode + @"/obj/body/b" + unique
                            + @"/texture/c" + genderCode + "b" + unique + GetTextureType(texture, baseBody, false, true) + ".tex";
                    }
                    break;
                case 1:
                    // Bibo+
                    if (race != 3) {
                        if (genderValue == 1) {
                            result = @"chara/bibo_" + RaceInfo.BodyIdentifiers[baseBody].RaceIdentifiers[race]
                                + GetTextureType(texture, baseBody, false, true) + ".tex";
                        } else {
                            result = "";
                        }
                    } else {
                        result = "Bibo+ is not compatible with lalafells";
                    }
                    break;
                case 2:
                    // Gen3 and T&F3
                    if (race != 3) {
                        if (genderValue == 1) {
                            result = @"chara/human/c" + RaceInfo.RaceCodeBody.Feminine[race] + @"/obj/body/b" + unique + @"/texture/tfgen3" +
                                RaceInfo.BodyIdentifiers[baseBody].RaceIdentifiers[race] + "f" + GetTextureType(texture, baseBody, false, true) + ".tex";
                        } else {
                            result = "Gen3 and T&F3 are only compatible with feminine characters";
                        }
                    } else {
                        result = "Gen3 and T&F3 are not compatible with lalafells";
                    }
                    break;
                case 3:
                    if (race != 3) {
                        if (genderValue == 0) {
                            // TBSE and HRBODY
                            if (texture == 1 || texture == 2) {
                                unique = uniqueAuRa ? "0101" : "0001";
                            }
                            result = @"chara/human/c" + (genderValue == 0 ? RaceInfo.RaceCodeBody.Masculine[race]
                                : RaceInfo.RaceCodeBody.Feminine[race]) + @"/obj/body/b" + unique
                                + @"/texture/c" + RaceInfo.RaceCodeBody.Masculine[race] + "b" + unique + "_b" + GetTextureType(texture, baseBody) + ".tex";
                        } else {
                            result = "TBSE and HRBODY are only compatible with masculine characters";
                        }
                    } else {
                        result = "TBSE and HRBODY are not compatible with lalafells";
                    }
                    break;
                case 4:
                    // Tails
                    string xaelaCheck = (race == 7 ? "010" : "000") + (tail + 1);
                    string gender = (genderValue == 0 ? RaceInfo.RaceCodeBody.Masculine[race]
                        : RaceInfo.RaceCodeBody.Feminine[race]);
                    result = @"chara/human/c" + gender + @"/obj/tail/t" + xaelaCheck + @"/texture/c" + gender + "t" +
                        xaelaCheck + "_etc" + GetTextureType(texture, baseBody, false, true) + ".tex";
                    break;
                case 5:
                    // Otopop
                    if (race == 3) {
                        if (texture == 0) {
                            if (!otopopNotice) {
                                if (otopopNoticeTriggered != null) {
                                    otopopNoticeTriggered.Invoke(new object(), EventArgs.Empty);
                                }
                                otopopNotice = true;
                            }
                        }
                        result = @"chara/human/c1101/obj/body/b0001/texture/v01_c1101b0001_g" + GetTextureType(texture, baseBody) + ".tex";

                    } else {
                        result = "Otopop is only compatible with lalafells";
                    }
                    break;
                case 6:
                    // Asymmetrical Vanilla Lalafell
                    if (race == 3) {
                        result = @"chara/human/c1101/obj/body/b0001/texture/v01_c1101b0001_b" + GetTextureType(texture, baseBody) + ".tex";
                    } else {
                        result = "Asymmetrical Vanilla Lalafell is only compatible with lalafells";
                    }
                    break;
            }
            return result;
        }
        public static string GetHairTexturePath(int material, int hairNumber, int gender, int race, int subRaceValue) {
            string hairValue = RaceInfo.NumberPadder(hairNumber + 1);
            string genderCode = (gender == 0 ? RaceInfo.RaceCodeBody.Masculine[race]
                : RaceInfo.RaceCodeBody.Feminine[race]);
            string subRace = (gender == 0 ? RaceInfo.RaceCodeFace.Masculine[subRaceValue]
                : RaceInfo.RaceCodeFace.Feminine[subRaceValue]);
            return "chara/human/c" + genderCode + "/obj/hair/h" + hairValue + "/texture/c"
                + genderCode + "h" + hairValue + "_hir" + GetTextureType(material, 0, true) + ".tex";
        }
        public static string GetTextureType(int material, int baseBodyIndex, bool isface = false, bool isVerbose = false) {
            switch (material) {
                case 0:
                    return (isVerbose ? "_base" : "_d");
                case 1:
                    return isVerbose ? "_norm" : "_n";
                case 2:
                    if (baseBodyIndex == 1 && !isface) {
                        return isVerbose ? "_mask" : "_m";
                    } else {
                        return isVerbose ? "_mask" : "_s";
                    }
                case 3:
                    return "_catchlight";
            }
            return null;
        }
        public static string GetFacePart(int material, bool asym) {
            switch (material) {
                case 0:
                    return asym ? "_fac_b" : "_fac";
                case 1:
                case 3:
                    return "_etc";
                case 2:
                    return "_iri_a";
                case 6:
                    return "_fac_b";
                case 7:
                    return "_etc_b";
            }
            return null;
        }

        public static string BiboPathUpgraderBase(string biboPath) {
            if (biboPath.ToLower().Contains("mid")) {
                return "chara/bibo_mid_base.tex";
            }
            if (biboPath.ToLower().Contains("high")) {
                return "chara/bibo_high_base.tex";
            }
            if (biboPath.ToLower().Contains("viera")) {
                return "chara/bibo_viera_base.tex";
            }
            if (biboPath.ToLower().Contains("raen")) {
                return "chara/bibo_raen_base.tex";
            }
            if (biboPath.ToLower().Contains("xaela")) {
                return "chara/bibo_xaela_base.tex";
            }
            if (biboPath.ToLower().Contains("helion")) {
                return "chara/bibo_hroth_base.tex";
            }
            if (biboPath.ToLower().Contains("lost")) {
                return "chara/bibo/bibo_hroth_base.tex";
            }
            return biboPath;
        }
        public static string BiboPathUpgraderNormal(string biboPath) {
            if (biboPath.ToLower().Contains("mid")) {
                return "chara/bibo_mid_norm.tex";
            }
            if (biboPath.ToLower().Contains("high")) {
                return "chara/bibo_high_norm.tex";
            }
            if (biboPath.ToLower().Contains("viera")) {
                return "chara/bibo_viera_norm.tex";
            }
            if (biboPath.ToLower().Contains("raen")) {
                return "chara/bibo_raen_norm.tex";
            }
            if (biboPath.ToLower().Contains("xaela")) {
                return "chara/bibo_xaela_norm.tex";
            }
            if (biboPath.ToLower().Contains("helion")) {
                return "chara/bibo_hroth_norm.tex";
            }
            if (biboPath.ToLower().Contains("lost")) {
                return "chara/bibo_hroth_norm.tex";
            }
            return biboPath;
        }
        public static string BiboPathUpgraderMask(string biboPath) {
            if (biboPath.ToLower().Contains("mid")) {
                return "chara/bibo_mid_mask.tex";
            }
            if (biboPath.ToLower().Contains("high")) {
                return "chara/bibo_high_mask.tex";
            }
            if (biboPath.ToLower().Contains("viera")) {
                return "chara/bibo_viera_mask.tex";
            }
            if (biboPath.ToLower().Contains("raen")) {
                return "chara/bibo_raen_mask.tex";
            }
            if (biboPath.ToLower().Contains("xaela")) {
                return "chara/bibo_xaela_mask.tex";
            }
            if (biboPath.ToLower().Contains("helion")) {
                return "chara/bibo_hroth_mask.tex";
            }
            if (biboPath.ToLower().Contains("lost")) {
                return "chara/bibo_hroth_mask.tex";
            }
            return biboPath;
        }

        public static string GetFaceTexturePath(int selectedIndex) {
            return "chara/common/texture/decal_face/_decal_" + (selectedIndex + 1) + ".tex";
        }
        public static string OldEyePathToNewEyeBasePath(string value) {
            if (value.Contains("c0201") || value.Contains("c0401") || value.Contains("c1801")) {
                return "chara/common/texture/eye/eye01_base.tex";
            }
            if (value.Contains("c0601") || value.Contains("c0501")) {
                return "chara/common/texture/eye/eye09_base.tex";
            }
            if (value.Contains("c0801") || value.Contains("c0701")) {
                return value.Contains("f010") ? "chara/common/texture/eye/eye03_base.tex" : "chara/common/texture/eye/eye02_base.tex";
            }
            if (value.Contains("c1001") || value.Contains("c1401") || value.Contains("c1301")) {
                return "chara/common/texture/eye/eye10_base.tex";
            }
            if (value.Contains("c1201") || value.Contains("c1101")) {
                return value.Contains("f010") ? "chara/common/texture/eye/eye05_base.tex" : "chara/common/texture/eye/eye04_base.tex";
            }
            if (value.Contains("c0101") || value.Contains("c0301") || value.Contains("c1701")) {
                return "chara/common/texture/eye/eye11_base.tex";
            }
            if (value.Contains("c1501") || value.Contains("c1601")) {
                return value.Contains("f010") ? "chara/common/texture/eye/eye07_base.tex" : "chara/common/texture/eye/eye06_base.tex";
            }
            return value;
        }
        public static string OldEyePathToNewEyeNormalPath(string value) {
            if (value.Contains("c1501") || value.Contains("c1601")) {
                return "chara/common/texture/eye/eye06_norm.tex";
            }
            return "chara/common/texture/eye/eye01_norm.tex";
        }
        public static string OldEyePathToNewEyeMultiPath(string value) {
            if (value.Contains("c1501") || value.Contains("c1601")) {
                return "chara/common/texture/eye/eye06_mask.tex";
            }
            return "chara/common/texture/eye/eye01_mask.tex";
        }
    }
}
