using FFXIVLooseTextureCompiler.PathOrganization;
using Lumina.Data.Parsing;
using Lumina.Models.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LooseTextureCompilerCore.Racial {
    public static class RaceEyePaths {
        private static readonly string[][] racialStrings = new string[][] {
        //Hyur Midlander Male
        new string[] {
            "chara/common/texture/eye/eye11_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Hyur Midlander Female
        new string[] {
            "chara/common/texture/eye/eye01_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Hyur Highlander Male
        new string[] {
            "chara/common/texture/eye/eye11_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Hyur Midlander Female
        new string[] {
            "chara/common/texture/eye/eye01_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Elezen Male 
        new string[] {
            "chara/common/texture/eye/eye09_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Elezen Female 
        new string[] {
            "chara/common/texture/eye/eye09_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Elezen Male 0101
        new string[] {
            "chara/common/texture/eye/eye09_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Elezen Female 0101
        new string[] {
            "chara/common/texture/eye/eye09_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Miqo'te Male
        new string[] {
            "chara/common/texture/eye/eye02_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Miqo'te Female
        new string[] {
            "chara/common/texture/eye/eye02_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Miqo'te Male 0101
        new string[] {
            "chara/common/texture/eye/eye03_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Miqo'te Female 0101
        new string[] {
            "chara/common/texture/eye/eye03_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Roegadyn Male
        new string[] {
            "chara/common/texture/eye/eye09_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Roegadyn Female
        new string[] {
            "chara/common/texture/eye/eye10_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Roegadyn Male 0101
        new string[] {
            "chara/common/texture/eye/eye09_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Roegadyn Female 0101
        new string[] {
            "chara/common/texture/eye/eye10_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Lalafell Male
        new string[] {
            "chara/common/texture/eye/eye04_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Lalafell Female
        new string[] {
            "chara/common/texture/eye/eye04_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Lalafell Male 0101
        new string[] {
            "chara/common/texture/eye/eye05_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Lalafell Female 0101
        new string[] {
            "chara/common/texture/eye/eye05_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //AuRa Male
        new string[] {
            "chara/common/texture/eye/eye10_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex" },
        //AuRa Female
        new string[] {
            "chara/common/texture/eye/eye10_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //AuRa Male 0101
        new string[] {
            "chara/common/texture/eye/eye14_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex" },
        //AuRa Female 0101
        new string[] {
            "chara/common/texture/eye/eye10_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Hrothgar Male
        new string[] {
            "chara/common/texture/eye/eye06_base.tex",
            "chara/common/texture/eye/eye06_norm.tex",
            "chara/common/texture/eye/eye06_mask.tex"
        },
        //Hrothgar Female
        new string[] {
            "chara/common/texture/eye/eye06_base.tex",
            "chara/common/texture/eye/eye06_norm.tex",
            "chara/common/texture/eye/eye06_mask.tex"
        },
        //Hrothgar Male 0101
        new string[] {
            "chara/common/texture/eye/eye07_base.tex",
            "chara/common/texture/eye/eye06_norm.tex",
            "chara/common/texture/eye/eye06_mask.tex"
        },
        //Hrothgar Female 0101
        new string[] {
            "chara/common/texture/eye/eye07_base.tex",
            "chara/common/texture/eye/eye06_norm.tex",
            "chara/common/texture/eye/eye06_mask.tex"
        },
        //Viera Male
        new string[] {
            "chara/common/texture/eye/eye11_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Viera Female
        new string[] {
            "chara/common/texture/eye/eye01_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Viera Male 0101
        new string[] {
            "chara/common/texture/eye/eye11_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        //Viera Female 0101
        new string[] {
            "chara/common/texture/eye/eye12_base.tex",
            "chara/common/texture/eye/eye01_norm.tex",
            "chara/common/texture/eye/eye01_mask.tex"},
        };

        public static void GetEyeTextureSet(int subRace, bool gender, TextureSet textureSet) {
            int index = ((1 + subRace) * 2) - (gender ? 0 : 1);
            string[] paths = racialStrings[index - 1];
            if (paths != null && paths.Length > 0) {
                textureSet.InternalBasePath = paths[0];
                textureSet.InternalNormalPath = paths[1];
                textureSet.InternalMaskPath = paths[2];
            }
        }
    }
}
