using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;
using FFXIVLooseTextureCompiler.Racial;
using LooseTextureCompilerCore.Export;
using Newtonsoft.Json;

namespace FFXIVLooseTextureCompiler.Export {
    public class BackupTexturePaths {
        public BackupTexturePaths(string path, bool isFace = false, int gender = 0, int subRace = 0, int face = 0) {
            _path = path;
            if (!isFace) {
                _baseTexture = "diffuse.ltct";
                _baseTextureSecondary = "diffuseRaen.ltct";
                _normal = "normal.ltct";
            } else {
                string fileName = (((subRace == 5 && gender == 1) || subRace == 11 ? 101 : 1) + face) + ".png";
                _baseTexture = "\\" + fileName;
                _normal = "\\" + (face + 1) + "n.png";
            }
            _isFace = isFace;
        }

        [JsonProperty]
        string _baseTexture = "";
        [JsonProperty]
        string _baseTextureSecondary = "";
        [JsonProperty]
        string _normal = "";
        [JsonProperty]
        string _path;

        [JsonIgnore]
        public string Base { get => _path + _baseTexture; }
        [JsonIgnore]
        public string BaseSecondary { get => _path + _baseTextureSecondary; }
        [JsonIgnore]
        public string Normal { get => _path + _normal; }
        [JsonIgnore]
        public string InternalPath {
            get => _path; set {
                _path = value;
            }
        }
        private static List<SkinType> _biboSkinTypes = new List<SkinType>() {
            new SkinType("Bibo Detailed",
                         new BackupTexturePaths(@"res\textures\bibo\bibo\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen3\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen2\")),
            new SkinType("Bibo Smooth",
                         new BackupTexturePaths(@"res\textures\biboSmooth\bibo\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen3\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen2\")),
            new SkinType("Gen3 Default",
                         new BackupTexturePaths(@"res\textures\gen3\bibo\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen3\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen2\")),
            new SkinType("Pythia",
                         new BackupTexturePaths(@"res\textures\pythia\bibo\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen3\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen2\")),
            new SkinType("Freyja",
                         new BackupTexturePaths(@"res\textures\freyja\bibo\"),
                         new BackupTexturePaths(@"res\textures\freyja\gen3\"),
                         new BackupTexturePaths(@"res\textures\freyja\gen2\")),
            new SkinType("Eve",
                         new BackupTexturePaths(@"res\textures\eve\bibo\"),
                         new BackupTexturePaths(@"res\textures\eve\gen3\"),
                         new BackupTexturePaths(@"res\textures\eve\gen2\")),
        };

        private static List<SkinType> _gen3SkinTypes = new List<SkinType>() {
            new SkinType("Gen3 Default",
                         new BackupTexturePaths(@"res\textures\gen3\bibo\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen3\"),
                         new BackupTexturePaths(@"res\textures\gen3\gen2\")),
            new SkinType("Bibo Detailed",
                         new BackupTexturePaths(@"res\textures\bibo\bibo\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen3\"),
                         new BackupTexturePaths(@"res\textures\bibo\gen2\")),
            new SkinType("Bibo Smooth",
                         new BackupTexturePaths(@"res\textures\biboSmooth\bibo\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen3\"),
                         new BackupTexturePaths(@"res\textures\biboSmooth\gen2\")),
            new SkinType("Pythia",
                         new BackupTexturePaths(@"res\textures\pythia\bibo\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen3\"),
                         new BackupTexturePaths(@"res\textures\pythia\gen2\")),
            new SkinType("Freyja",
                         new BackupTexturePaths(@"res\textures\freyja\bibo\"),
                         new BackupTexturePaths(@"res\textures\freyja\gen3\"),
                         new BackupTexturePaths(@"res\textures\freyja\gen2\")),
            new SkinType("Eve",
                         new BackupTexturePaths(@"res\textures\eve\bibo\"),
                         new BackupTexturePaths(@"res\textures\eve\gen3\"),
                         new BackupTexturePaths(@"res\textures\eve\gen2\")),
        };

        private static List<SkinType> _tbseSkinTypes = new List<SkinType>() {
            new SkinType("Default",
                         new BackupTexturePaths(@"res\textures\tbse\tbse\"),
                         new BackupTexturePaths(@"res\textures\tbse\highlander\"),
                         new BackupTexturePaths(@"res\textures\tbse\viera\"),
                         new BackupTexturePaths(@"res\textures\tbse\vanilla\")),
        };

        private static List<SkinType> _otopopSkinTypes = new List<SkinType>() {
            new SkinType("Default",
                         new BackupTexturePaths(@"res\textures\otopop\otopop\"),
                         new BackupTexturePaths(@"res\textures\otopop\asym\"),
                         new BackupTexturePaths(@"res\textures\otopop\vanilla\")),
        };
        private bool _isFace;

        public static void AddFaceBackupPaths(int gender, int subRace, int face, TextureSet textureSet) {
            string outputTexture = @"res\textures\face\" + (gender == 1 ? "feminine" : "masculine") + @"\" +
            RaceInfo.ModelRaces[RaceInfo.SubRaceToModelRace(subRace)].ToLower();

            textureSet.BackupTexturePaths = new BackupTexturePaths(outputTexture, true, gender, subRace, face);
        }

        public static void AddBodyBackupPaths(int gender, int race, TextureSet textureSet) {
            if (gender != 0) {
                if (textureSet.SkinType > -1) {
                    if (textureSet.InternalBasePath.Contains("bibo")) {
                        textureSet.BackupTexturePaths = BiboSkinTypes[textureSet.SkinType].BackupTextures[0];
                    } else if (textureSet.InternalBasePath.Contains("gen3") || textureSet.InternalBasePath.Contains("eve")) {
                        textureSet.BackupTexturePaths = Gen3SkinTypes[textureSet.SkinType].BackupTextures[1];
                    } else if (textureSet.InternalBasePath.Contains("v01_c1101b0001_g")) {
                        textureSet.BackupTexturePaths = OtopopSkinTypes[textureSet.SkinType].BackupTextures[0];
                    } else {
                        textureSet.BackupTexturePaths = race == 5 ?
                         OtopopSkinTypes[textureSet.SkinType].BackupTextures[2] : Gen3SkinTypes[textureSet.SkinType].BackupTextures[2];
                    }
                }
            } else {
                if (race == 5) {
                    textureSet.BackupTexturePaths = OtopopSkinTypes[textureSet.SkinType].BackupTextures[2];
                } else {
                    // Midlander, Elezen, Miqo'te
                    if (textureSet.InternalBasePath.Contains("--c0101b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[0];
                    } else
                    // Highlander
                    if (textureSet.InternalBasePath.Contains("--c0301b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[1];
                    } else
                    // Viera
                    if (textureSet.InternalBasePath.Contains("--c1701b0001_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[2];
                    } else if (textureSet.InternalBasePath.Contains("1_b_d")) {
                        textureSet.BackupTexturePaths = TbseSkinTypes[textureSet.SkinType].BackupTextures[0];
                    }
                }
            }
        }
        public static BackupTexturePaths AsymLalaPath(int skinType) {
            if (!File.Exists(_otopopSkinTypes[skinType].BackupTextures[1].Base)) {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                _otopopSkinTypes[skinType].BackupTextures[1].Base)));

                TexIO.WriteImageToXOR(ImageManipulation.MirrorAndDuplicate(
                TexIO.ResolveBitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    _otopopSkinTypes[skinType].BackupTextures[2].Base))),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    _otopopSkinTypes[skinType].BackupTextures[1].Base));

                TexIO.WriteImageToXOR(ImageManipulation.MirrorAndDuplicate(
                    TexIO.ResolveBitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
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
        public bool IsFace { get => _isFace; set => _isFace = value; }
    }
}
