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
            "chara/common/texture/eye/eye11_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Hyur Midlander Female
        new string[] {
            "chara/common/texture/eye/eye01_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Hyur Highlander Male
        new string[] {
            "chara/common/texture/eye/eye11_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Hyur Midlander Female
        new string[] {
            "chara/common/texture/eye/eye01_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Elezen Male 
        new string[] {
            "chara/common/texture/eye/eye09_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Elezen Female 
        new string[] {
            "chara/common/texture/eye/eye09_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Elezen Male 0101
        new string[] {
            "chara/common/texture/eye/eye09_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Elezen Female 0101
        new string[] {
            "chara/common/texture/eye/eye09_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Miqo'te Male
        new string[] {
            "chara/common/texture/eye/eye02_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Miqo'te Female
        new string[] {
            "chara/common/texture/eye/eye02_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Miqo'te Male 0101
        new string[] {
            "chara/common/texture/eye/eye03_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Miqo'te Female 0101
        new string[] {
            "chara/common/texture/eye/eye03_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Roegadyn Male
        new string[] {
            "chara/common/texture/eye/eye09_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Roegadyn Female
        new string[] {
            "chara/common/texture/eye/eye10_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Roegadyn Male 0101
        new string[] {
            "chara/common/texture/eye/eye09_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Roegadyn Female 0101
        new string[] {
            "chara/common/texture/eye/eye10_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Lalafell Male
        new string[] {
            "chara/common/texture/eye/eye04_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Lalafell Female
        new string[] {
            "chara/common/texture/eye/eye04_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Lalafell Male 0101
        new string[] {
            "chara/common/texture/eye/eye05_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Lalafell Female 0101
        new string[] {
            "chara/common/texture/eye/eye05_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //AuRa Male
        new string[] {
            "chara/common/texture/eye/eye10_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex" },
        //AuRa Female
        new string[] {
            "chara/common/texture/eye/eye10_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //AuRa Male 0101
        new string[] {
            "chara/common/texture/eye/eye10_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex" },
        //AuRa Female 0101
        new string[] {
            "chara/common/texture/eye/eye10_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Hrothgar Male
        new string[] {
            "chara/common/texture/eye/eye06_d.tex",
            "chara/common/texture/eye/eye06_n.tex",
            "chara/common/texture/eye/eye06_s.tex"
        },
        //Hrothgar Female
        new string[] {
            "chara/common/texture/eye/eye06_d.tex",
            "chara/common/texture/eye/eye06_n.tex",
            "chara/common/texture/eye/eye06_s.tex"
        },
        //Hrothgar Male 0101
        new string[] {
            "chara/common/texture/eye/eye07_d.tex",
            "chara/common/texture/eye/eye06_n.tex",
            "chara/common/texture/eye/eye06_s.tex"
        },
        //Hrothgar Female 0101
        new string[] {
            "chara/common/texture/eye/eye07_d.tex",
            "chara/common/texture/eye/eye06_n.tex",
            "chara/common/texture/eye/eye06_s.tex"
        },
        //Viera Male
        new string[] {
            "chara/common/texture/eye/eye11_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Viera Female
        new string[] {
            "chara/common/texture/eye/eye01_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Viera Male 0101
        new string[] {
            "chara/common/texture/eye/eye11_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        //Viera Female 0101
        new string[] {
            "chara/common/texture/eye/eye01_d.tex",
            "chara/common/texture/eye/eye01_n.tex",
            "chara/common/texture/eye/eye01_s.tex"},
        };

        public static void GetEyeTextureSet(int subRace, bool gender, TextureSet textureSet) {
            int index = ((1 + subRace) * 2) - (gender ? 0 : 1);
            string[] paths = racialStrings[index - 1];
            if (paths != null && paths.Length > 0) {
                textureSet.InternalDiffusePath = paths[0];
                textureSet.InternalNormalPath = paths[1];
                textureSet.InternalMaskPath = paths[2];
            }
        }
    }
}
