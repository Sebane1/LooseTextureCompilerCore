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

        public static string GuessNormalPath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return "";
            return basePath
                .Replace("_d.tex", "_n.tex")
                .Replace("_base.tex", "_norm.tex")
                .Replace("_dif.tex", "_norm.tex");
        }

        public static string GuessMaskPath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return "";
            return basePath
                .Replace("_d.tex", "_m.tex")
                .Replace("_base.tex", "_mask.tex")
                .Replace("_dif.tex", "_mask.tex");
        }
    }
}
