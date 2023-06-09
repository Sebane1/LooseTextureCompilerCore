﻿using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;

namespace FFXIVLooseTextureCompiler.Export {
    public class BackupTexturePaths {
        public BackupTexturePaths(string path) {
            _path = path;
        }
        const string _diffuse = "diffuse.ltct";
        const string _diffuseRaen = "diffuseRaen.ltct";
        const string _normal = "normal.ltct";
        string _path;

        public string Diffuse { get => _path + _diffuse; }
        public string DiffuseRaen { get => _path + _diffuseRaen; }
        public string Normal { get => _path + _normal; }
        public string InternalPath {
            get => _path; set {
                _path = value;
            }
        }

        private static BackupTexturePaths biboPath = new BackupTexturePaths(@"res\textures\bibo\bibo\");
        private static BackupTexturePaths biboGen3Path = new BackupTexturePaths(@"res\textures\bibo\gen3\");
        private static BackupTexturePaths biboGen2Path = new BackupTexturePaths(@"res\textures\bibo\gen2\");

        private static BackupTexturePaths gen3BiboPath = new BackupTexturePaths(@"res\textures\gen3\bibo\");
        private static BackupTexturePaths gen3Path = new BackupTexturePaths(@"res\textures\gen3\gen3\");
        private static BackupTexturePaths gen3Gen2Path = new BackupTexturePaths(@"res\textures\gen3\gen2\");

        private static BackupTexturePaths tbsePath = new BackupTexturePaths(@"res\textures\tbse\tbse\");
        private static BackupTexturePaths tbsePathHighlander = new BackupTexturePaths(@"res\textures\tbse\highlander\");
        private static BackupTexturePaths tbsePathViera = new BackupTexturePaths(@"res\textures\tbse\viera\");

        private static BackupTexturePaths otopopLalaPath = new BackupTexturePaths(@"res\textures\otopop\otopop\");
        private static BackupTexturePaths asymLalaPath = new BackupTexturePaths(@"res\textures\otopop\asym\");
        private static BackupTexturePaths vanillaLalaPath = new BackupTexturePaths(@"res\textures\otopop\vanilla\");

        public static BackupTexturePaths BiboPath { get => biboPath; }
        public static BackupTexturePaths BiboGen3Path { get => biboGen3Path; }
        public static BackupTexturePaths BiboGen2Path { get => biboGen2Path; }
        public static BackupTexturePaths Gen3BiboPath { get => gen3BiboPath; }
        public static BackupTexturePaths Gen3Path { get => gen3Path; }
        public static BackupTexturePaths Gen3Gen2Path { get => gen3Gen2Path; }
        public static BackupTexturePaths OtopopLalaPath { get => otopopLalaPath; }
        public static BackupTexturePaths TbsePath { get => tbsePath; }
        public static BackupTexturePaths VanillaLalaPath { get => vanillaLalaPath; }
        public static BackupTexturePaths TbsePathHighlander { get => tbsePathHighlander; }
        public static BackupTexturePaths TbsePathViera { get => tbsePathViera; }

        public static void AddBackupPaths(int gender, int race, TextureSet textureSet) {
            if (gender != 0) {
                if (textureSet.InternalDiffusePath.Contains("bibo")) {
                    textureSet.BackupTexturePaths = BiboPath;
                } else if (textureSet.InternalDiffusePath.Contains("gen3") || textureSet.InternalDiffusePath.Contains("eve")) {
                    textureSet.BackupTexturePaths = Gen3Path;
                } else if (textureSet.InternalDiffusePath.Contains("v01_c1101b0001_g")) {
                    textureSet.BackupTexturePaths = OtopopLalaPath;
                } else {
                    textureSet.BackupTexturePaths = race == 5 ?
                    VanillaLalaPath : Gen3Gen2Path;
                }
            } else {
                if (race == 5) {
                    textureSet.BackupTexturePaths = VanillaLalaPath;
                } else {
                    // Midlander, Elezen, Miqo'te
                    if (textureSet.InternalDiffusePath.Contains("--c0101b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbsePath;
                    } else
                    // Highlander
                    if (textureSet.InternalDiffusePath.Contains("--c0301b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbsePathHighlander;
                    } else
                    // Viera
                    if (textureSet.InternalDiffusePath.Contains("--c1701b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbsePathViera;
                    } else if (textureSet.InternalDiffusePath.Contains("1_b_d")) {
                        textureSet.BackupTexturePaths = TbsePath;
                    }
                }
            }
        }
        public static BackupTexturePaths AsymLalaPath {
            get {
                if (!File.Exists(asymLalaPath.Diffuse)) {
                    Directory.CreateDirectory(
                    Path.GetDirectoryName(
                    Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    asymLalaPath.Diffuse)));

                    TexLoader.WriteImageToXOR(ImageManipulation.MirrorAndDuplicate(
                    TexLoader.ResolveBitmap(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        vanillaLalaPath.Diffuse))),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        asymLalaPath.Diffuse));

                    TexLoader.WriteImageToXOR(ImageManipulation.MirrorAndDuplicate(
                        TexLoader.ResolveBitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        vanillaLalaPath.Normal))),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        asymLalaPath.Normal));
                }
                return asymLalaPath;
            }
        }
    }
}
