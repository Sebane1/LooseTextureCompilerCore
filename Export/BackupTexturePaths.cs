using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;
using LooseTextureCompilerCore.Export;
using Lumina.Models.Materials;

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
        private static List<SkinType> _biboSkinTypes = new List<SkinType>() {
            new SkinType("Bibo Default",new BackupTexturePaths(@"res\textures\bibo\bibo\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen3\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen2\")),
            new SkinType("Bibo Smooth",new BackupTexturePaths(@"res\textures\biboSmooth\bibo\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen3\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen2\")),
            new SkinType("Bibo Soft Finger",new BackupTexturePaths(@"res\textures\biboSoftFinger\bibo\"),
                         new BackupTexturePaths(@"res\textures\biboSoftFinger\gen3\"),
                         new BackupTexturePaths(@"res\textures\biboSoftFinger\gen2\")),
            new SkinType("Gen3 Default",new BackupTexturePaths(@"res\textures\gen3\bibo\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen3\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen2\")),
            new SkinType("Pythia",new BackupTexturePaths(@"res\textures\pythia\bibo\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen3\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen2\")),
        };

        private static List<SkinType> _gen3SkinTypes = new List<SkinType>() {
            new SkinType("Gen3 Default",new BackupTexturePaths(@"res\textures\gen3\bibo\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen3\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen2\")),
            new SkinType("Bibo Default",new BackupTexturePaths(@"res\textures\bibo\bibo\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen3\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen2\")),
            new SkinType("Bibo Smooth",new BackupTexturePaths(@"res\textures\biboSmooth\bibo\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen3\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen2\")),
            new SkinType("Bibo Soft Finger",new BackupTexturePaths(@"res\textures\biboSoftFinger\bibo\"),
                         new BackupTexturePaths(@"res\textures\biboSoftFinger\gen3\"),
                         new BackupTexturePaths(@"res\textures\biboSoftFinger\gen2\")),
            new SkinType("Pythia",new BackupTexturePaths(@"res\textures\pythia\bibo\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen3\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen2\")),
        };

        private static List<SkinType> _tbseSkinTypes = new List<SkinType>() {
            new SkinType("Default",new BackupTexturePaths(@"res\textures\tbse\tbse\"),
                         new BackupTexturePaths(@"res\textures\tbse\highlander\"),
                         new BackupTexturePaths(@"res\textures\tbse\viera\"),
                         new BackupTexturePaths(@"res\textures\tbse\vanilla\")),
        };

        private static List<SkinType> _otopopSkinTypes = new List<SkinType>() {
            new SkinType("Default",new BackupTexturePaths(@"es\textures\otopop\otopop\"),
                         new BackupTexturePaths(@"res\textures\otopop\asym\"),
                         new BackupTexturePaths(@"res\textures\otopop\vanilla\")),
        };

        public static void AddBackupPaths(int gender, int race, TextureSet textureSet) {
            if (gender != 0) {
                if (textureSet.InternalDiffusePath.Contains("bibo")) {
                    textureSet.BackupTexturePaths = BiboSkinTypes[textureSet.SkinType].BackupTextures[0];
                } else if (textureSet.InternalDiffusePath.Contains("gen3") || textureSet.InternalDiffusePath.Contains("eve")) {
                    textureSet.BackupTexturePaths = Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];
                } else if (textureSet.InternalDiffusePath.Contains("v01_c1101b0001_g")) {
                    textureSet.BackupTexturePaths = OtopopSkinTypes[textureSet.SkinType].BackupTextures[0];
                } else {
                    textureSet.BackupTexturePaths = race == 5 ?
                     OtopopSkinTypes[textureSet.SkinType].BackupTextures[2] : Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];
                }
            } else {
                if (race == 5) {
                    textureSet.BackupTexturePaths = OtopopSkinTypes[textureSet.SkinType].BackupTextures[2];
                } else {
                    // Midlander, Elezen, Miqo'te
                    if (textureSet.InternalDiffusePath.Contains("--c0101b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[0];
                    } else
                    // Highlander
                    if (textureSet.InternalDiffusePath.Contains("--c0301b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[1];
                    } else
                    // Viera
                    if (textureSet.InternalDiffusePath.Contains("--c1701b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[2];
                    } else if (textureSet.InternalDiffusePath.Contains("1_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[0];
                    }
                }
            }
        }
        public static BackupTexturePaths AsymLalaPath (int skinType) {
                if (!File.Exists(_otopopSkinTypes[skinType].BackupTextures[1].Diffuse)) {
                    Directory.CreateDirectory(
                    Path.GetDirectoryName(
                    Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    _otopopSkinTypes[skinType].BackupTextures[1].Diffuse)));

                    TexLoader.WriteImageToXOR(ImageManipulation.MirrorAndDuplicate(
                    TexLoader.ResolveBitmap(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        _otopopSkinTypes[skinType].BackupTextures[2].Diffuse))),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        _otopopSkinTypes[skinType].BackupTextures[1].Diffuse));

                    TexLoader.WriteImageToXOR(ImageManipulation.MirrorAndDuplicate(
                        TexLoader.ResolveBitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        _otopopSkinTypes[skinType].BackupTextures[2].Normal))),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        _otopopSkinTypes[skinType].BackupTextures[1].Normal));
                }
                return _otopopSkinTypes[skinType].BackupTextures[1];
        }

        internal static List<SkinType> BiboSkinTypes { get => _biboSkinTypes; set => _biboSkinTypes = value; }
        internal static List<SkinType> Gen3SkinTypes { get => _gen3SkinTypes; set => _gen3SkinTypes = value; }
        internal static List<SkinType> TbseSkinTypes { get => _tbseSkinTypes; set => _tbseSkinTypes = value; }
        internal static List<SkinType> OtopopSkinTypes { get => _otopopSkinTypes; set => _otopopSkinTypes = value; }
    }
}
