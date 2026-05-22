using System.Collections.Generic;

namespace FFXIVLooseTextureCompiler.Racial
{
    /// <summary>
    /// Builds candidate game paths for worn equipment textures (human gear).
    /// </summary>
    public static class EquipmentPathBuilder
    {
        public static string ToEquipmentSetId(uint modelMain)
            => $"e{modelMain:X4}";

        public static IEnumerable<string> BuildHumanTextureCandidates(string raceCode, string equipSetId, string slotSuffix, string variant = "01")
        {
            if (string.IsNullOrEmpty(raceCode) || string.IsNullOrEmpty(equipSetId) || string.IsNullOrEmpty(slotSuffix))
                yield break;

            string core = $"{raceCode}{equipSetId}";
            string humanPrefix = $"chara/human/{raceCode}/obj/equipment/{equipSetId}/texture/";
            string equipPrefix = $"chara/equipment/{equipSetId}/texture/";

            foreach (string mapSuffix in new[] { "d", "n", "m", "base", "norm", "mask" })
            {
                yield return $"{humanPrefix}v{variant}_{core}_{slotSuffix}_{mapSuffix}.tex";
                yield return $"{humanPrefix}{core}_{slotSuffix}_{mapSuffix}.tex";
                yield return $"{equipPrefix}v{variant}_{core}_{slotSuffix}_{mapSuffix}.tex";
                yield return $"{equipPrefix}{core}_{slotSuffix}_{mapSuffix}.tex";
            }

            // Legacy / alternate minus-prefix (some modded gear)
            yield return $"{humanPrefix}--{core.TrimStart('c')}_{slotSuffix}_d.tex";
        }

        public static IEnumerable<string> BuildHumanMtrlCandidates(string raceCode, string equipSetId, string slotSuffix, string variant = "0001")
        {
            if (string.IsNullOrEmpty(raceCode) || string.IsNullOrEmpty(equipSetId) || string.IsNullOrEmpty(slotSuffix))
                yield break;

            string core = $"{raceCode}{equipSetId}";
            string[] letters = new[] { "a", "b", "c", "d" };
            foreach (var letter in letters)
            {
                yield return $"chara/human/{raceCode}/obj/equipment/{equipSetId}/material/v{variant}/mt_{core}_{slotSuffix}_{letter}.mtrl";
                yield return $"chara/human/{raceCode}/obj/equipment/{equipSetId}/material/mt_{core}_{slotSuffix}_{letter}.mtrl";
                yield return $"chara/equipment/{equipSetId}/material/v{variant}/mt_{core}_{slotSuffix}_{letter}.mtrl";
                yield return $"chara/equipment/{equipSetId}/material/mt_{core}_{slotSuffix}_{letter}.mtrl";
            }
            yield return $"chara/human/{raceCode}/obj/equipment/{equipSetId}/material/v{variant}/mt_{core}_{slotSuffix}.mtrl";
            yield return $"chara/human/{raceCode}/obj/equipment/{equipSetId}/material/mt_{core}_{slotSuffix}.mtrl";
            yield return $"chara/equipment/{equipSetId}/material/v{variant}/mt_{core}_{slotSuffix}.mtrl";
            yield return $"chara/equipment/{equipSetId}/material/mt_{core}_{slotSuffix}.mtrl";
        }

        public static IEnumerable<string> BuildEquipmentModelCandidates(string raceCode, string equipSetId, string slotSuffix)
        {
            if (string.IsNullOrEmpty(raceCode) || string.IsNullOrEmpty(equipSetId) || string.IsNullOrEmpty(slotSuffix))
                yield break;

            yield return $"chara/equipment/{equipSetId}/model/{raceCode}{equipSetId}_{slotSuffix}.mdl";
            yield return $"chara/human/{raceCode}/obj/equipment/{equipSetId}/model/{raceCode}{equipSetId}_{slotSuffix}.mdl";
        }

        public static IEnumerable<string> BuildHairTextureCandidates(string raceCode, string hairId, string variant = "01")
        {
            string core = $"{raceCode}{hairId}_hir";
            string prefix = $"chara/human/{raceCode}/obj/hair/{hairId}/texture/";

            foreach (string mapSuffix in new[] { "d", "n", "m", "base", "norm", "mask" })
            {
                yield return $"{prefix}v{variant}_{core}_{mapSuffix}.tex";
                yield return $"{prefix}{core}_{mapSuffix}.tex";
            }
        }

        public static IEnumerable<string> BuildHairMtrlCandidates(string raceCode, string hairId, string variant = "0001")
        {
            string core = $"{raceCode}{hairId}_hir";
            string[] letters = new[] { "a", "b", "c", "d" };
            foreach (var letter in letters)
            {
                yield return $"chara/human/{raceCode}/obj/hair/{hairId}/material/v{variant}/mt_{core}_{letter}.mtrl";
                yield return $"chara/human/{raceCode}/obj/hair/{hairId}/material/mt_{core}_{letter}.mtrl";
            }
            yield return $"chara/human/{raceCode}/obj/hair/{hairId}/material/v{variant}/mt_{core}.mtrl";
            yield return $"chara/human/{raceCode}/obj/hair/{hairId}/material/mt_{core}.mtrl";
        }

        public static IEnumerable<string> BuildHairModelCandidates(string raceCode, string hairId)
        {
            yield return $"chara/human/{raceCode}/obj/hair/{hairId}/model/{raceCode}{hairId}_hir.mdl";
        }

        public static IEnumerable<string> BuildTailTextureCandidates(string raceCode, string tailId, string variant = "01")
        {
            string core = $"{raceCode}{tailId}_til";
            string prefix = $"chara/human/{raceCode}/obj/tail/{tailId}/texture/";

            foreach (string mapSuffix in new[] { "d", "n", "m", "base", "norm", "mask" })
            {
                yield return $"{prefix}v{variant}_{core}_{mapSuffix}.tex";
                yield return $"{prefix}{core}_{mapSuffix}.tex";
            }
        }

        public static IEnumerable<string> BuildTailMtrlCandidates(string raceCode, string tailId, string variant = "0001")
        {
            string core = $"{raceCode}{tailId}_til";
            string[] letters = new[] { "a", "b", "c", "d" };
            foreach (var letter in letters)
            {
                yield return $"chara/human/{raceCode}/obj/tail/{tailId}/material/v{variant}/mt_{core}_{letter}.mtrl";
                yield return $"chara/human/{raceCode}/obj/tail/{tailId}/material/mt_{core}_{letter}.mtrl";
            }
            yield return $"chara/human/{raceCode}/obj/tail/{tailId}/material/v{variant}/mt_{core}.mtrl";
            yield return $"chara/human/{raceCode}/obj/tail/{tailId}/material/mt_{core}.mtrl";
        }

        public static IEnumerable<string> BuildTailModelCandidates(string raceCode, string tailId)
        {
            yield return $"chara/human/{raceCode}/obj/tail/{tailId}/model/{raceCode}{tailId}_til.mdl";
        }

        public static string GuessNormalPath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return "";
            return basePath
                .Replace("_d.tex", "_n.tex")
                .Replace("_base", "_norm")
                .Replace("_dif", "_norm");
        }

        public static string GuessMaskPath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return "";
            return basePath
                .Replace("_d.tex", "_m.tex")
                .Replace("_base", "_mask")
                .Replace("_dif", "_mask");
        }
    }
}
