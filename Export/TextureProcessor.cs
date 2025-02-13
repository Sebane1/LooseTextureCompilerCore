using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;
using FFXIVLooseTextureCompiler.Racial;
using FFXIVVoicePackCreator.Json;
using Newtonsoft.Json;
using Penumbra.GameData.Files;
using Penumbra.LTCImport.Dds;
using SixLabors.ImageSharp.PixelFormats;
using static FFXIVLooseTextureCompiler.TextureProcessor;
using Color = System.Drawing.Color;
using Group = FFXIVVoicePackCreator.Json.Group;
using Size = System.Drawing.Size;

namespace FFXIVLooseTextureCompiler {
    public class TextureProcessor {
        private Dictionary<string, TextureSet> _redirectionCache;
        private Dictionary<string, TextureSet> _mtrlCache;
        private Dictionary<string, Bitmap> _normalCache;
        private Dictionary<string, Bitmap> _maskCache;
        private Dictionary<string, Bitmap> _glowCache;
        private Dictionary<string, string> _xnormalCache;
        private XNormal _xnormal;
        private List<KeyValuePair<string, string>> _textureSetQueue;
        private int _fileCount;

        private bool _finalizeResults;
        private bool _generateNormals;
        private bool _generateMulti;

        string _basePath = "";
        int _exportCompletion = 0;
        private int _exportMax;

        public int ExportMax { get => _exportMax; }
        public int ExportCompletion { get => _exportCompletion; }
        public string BasePath { get => _basePath; set => _basePath = value; }

        public TextureProcessor(string basePath = null) {
            _basePath = !string.IsNullOrEmpty(basePath) ? basePath : AppDomain.CurrentDomain.BaseDirectory;
            OnProgressChange += delegate {
                _exportCompletion++;
            };
        }

        public event EventHandler OnProgressChange;
        public event EventHandler OnStartedProcessing;
        public event EventHandler OnLaunchedXnormal;
        public event EventHandler<string> OnError;

        private Bitmap GetMergedBitmap(string file) {
            if (file.Contains("baseTexBaked") && (file.Contains("_d_") || file.Contains("_g_") || file.Contains("_n_"))) {
                string path1 = file.Replace("baseTexBaked", "alpha_baseTexBaked");
                string path2 = file.Replace("baseTexBaked", "rgb_baseTexBaked");
                Bitmap alpha = TexIO.ResolveBitmap(path1);
                Bitmap rgb = TexIO.ResolveBitmap(path2);
                Bitmap merged = ImageManipulation.MergeAlphaToRGB(alpha, rgb);
                TexIO.SaveBitmap(merged, file);
                try {
                    File.Delete(path1);
                    File.Delete(path2);
                } catch {

                }
                return merged;
            } else {
                return TexIO.ResolveBitmap(file);
            }
        }

        public void BatchTextureSet(TextureSet parent, TextureSet child) {
            if (!string.IsNullOrEmpty(child.FinalBase)) {
                if (!_xnormalCache.ContainsKey(child.FinalBase)) {
                    string baseTextureAlpha = ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(parent.FinalBase, "_alpha"), ".png");
                    string baseTextureRGB = ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(parent.FinalBase, "_rgb"), ".png");
                    if (_finalizeResults || !File.Exists(child.FinalBase.Replace("baseTexBaked", "rgb_baseTexBaked"))
                        || !File.Exists(child.FinalBase.Replace("baseTexBaked", "alpha_baseTexBaked"))) {
                        if (child.FinalBase.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.FinalBase, child.FinalBase);
                            Bitmap baseTexture = TexIO.ResolveBitmap(parent.FinalBase);
                            if (Directory.Exists(Path.GetDirectoryName(baseTextureAlpha))
                                && Directory.Exists(Path.GetDirectoryName(baseTextureRGB))) {
                                string childAlpha = child.FinalBase.Replace("baseTexBaked", "alpha");
                                string childRGB = child.FinalBase.Replace("baseTexBaked", "rgb");
                                ImageManipulation.ExtractTransparency(baseTexture).Save(baseTextureAlpha, ImageFormat.Png);
                                ImageManipulation.ExtractRGB(baseTexture).Save(baseTextureRGB, ImageFormat.Png);
                                if (_finalizeResults) {
                                    _xnormal.AddToBatch(parent.InternalBasePath, baseTextureAlpha, childAlpha, false);
                                    _xnormal.AddToBatch(parent.InternalBasePath, baseTextureRGB, childRGB, false);
                                } else {
                                    if (!File.Exists(childAlpha)) {
                                        new Bitmap(1024, 1024).Save(ImageManipulation.AddSuffix(childAlpha, "_baseTexBaked"), ImageFormat.Png);
                                    }
                                    if (!File.Exists(childRGB)) {
                                        new Bitmap(1024, 1024).Save(ImageManipulation.AddSuffix(childRGB, "_baseTexBaked"), ImageFormat.Png);
                                    }
                                }
                            } else {
                                //MessageBox.Show("Something has gone terribly wrong. " + parent.Base + "is missing");
                            }
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.FinalNormal)) {
                if (!_xnormalCache.ContainsKey(child.FinalNormal)) {
                    string normalAlpha = ImageManipulation.AddSuffix(parent.FinalNormal, "_alpha");
                    string normalRGB = ImageManipulation.AddSuffix(parent.FinalNormal, "_rgb");
                    if (_finalizeResults || !File.Exists(child.FinalNormal.Replace("baseTexBaked", "rgb_baseTexBaked"))
                        || !File.Exists(child.FinalNormal.Replace("baseTexBaked", "alpha_baseTexBaked"))) {
                        if (child.FinalNormal.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.FinalNormal, child.FinalNormal);
                            Bitmap normal = TexIO.ResolveBitmap(parent.FinalNormal);
                            ImageManipulation.ExtractTransparency(normal).Save(normalAlpha, ImageFormat.Png);
                            ImageManipulation.ExtractRGB(normal, true).Save(normalRGB, ImageFormat.Png);
                            _xnormal.AddToBatch(parent.InternalBasePath, normalAlpha, child.FinalNormal.Replace("baseTexBaked", "alpha"), false);
                            _xnormal.AddToBatch(parent.InternalBasePath, normalRGB, child.FinalNormal.Replace("baseTexBaked", "rgb"), true);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.FinalMask)) {
                if (!_xnormalCache.ContainsKey(child.FinalMask)) {
                    if (_finalizeResults || !File.Exists(child.FinalMask)) {
                        if (child.FinalMask.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.FinalMask, child.FinalMask);
                            _xnormal.AddToBatch(parent.InternalMaskPath, parent.FinalMask, child.FinalMask, false);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.Glow)) {
                if (!_xnormalCache.ContainsKey(child.Glow)) {
                    string glowAlpha = ImageManipulation.AddSuffix(parent.Glow, "_alpha");
                    string glowRGB = ImageManipulation.AddSuffix(parent.Glow, "_rgb");
                    if (_finalizeResults || !File.Exists(child.Glow.Replace("baseTexBaked", "rgb_baseTexBaked"))
                        || !File.Exists(child.Glow.Replace("baseTexBaked", "alpha_baseTexBaked"))) {
                        if (child.Glow.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.Glow, child.Glow);
                            Bitmap glow = TexIO.ResolveBitmap(parent.Glow);
                            ImageManipulation.ExtractTransparency(glow).Save(glowAlpha, ImageFormat.Png);
                            ImageManipulation.ExtractRGB(glow).Save(glowRGB, ImageFormat.Png);
                            _xnormal.AddToBatch(parent.InternalBasePath, glowAlpha, child.Glow.Replace("baseTexBaked", "alpha"), false);
                            _xnormal.AddToBatch(parent.InternalBasePath, glowRGB, child.Glow.Replace("baseTexBaked", "rgb"), false);
                        }
                    }
                }
            }
        }

        public void Export(List<TextureSet> textureSetList, Dictionary<string, int> groupOptionTypes,
            string modPath, int generationType, bool generateNormals,
            bool generateMulti, bool useXNormal, string xNormalPathOverride = "") {
            Dictionary<string, List<TextureSet>> groups = new Dictionary<string, List<TextureSet>>();
            try {
                int i = 0;
                _fileCount = 0;
                _finalizeResults = useXNormal;
                _normalCache?.Clear();
                _maskCache?.Clear();
                _glowCache?.Clear();
                _xnormalCache?.Clear();
                _redirectionCache?.Clear();
                _normalCache = new Dictionary<string, Bitmap>();
                _maskCache = new Dictionary<string, Bitmap>();
                _glowCache = new Dictionary<string, Bitmap>();
                _xnormalCache = new Dictionary<string, string>();
                _redirectionCache = new Dictionary<string, TextureSet>();
                _xnormal = new XNormal();
                _xnormal.XNormalPathOverride = xNormalPathOverride;
                _xnormal.BasePathOverride = _basePath;
                _generateNormals = generateNormals;
                _generateMulti = generateMulti;
                _exportCompletion = 0;
                _exportMax = 0;
                _exportMax = textureSetList.Count * 4;
                Dictionary<string, string> alreadyCalculatedBases = new Dictionary<string, string>();
                Dictionary<string, string> alreadyCalculatedNormals = new Dictionary<string, string>();
                Dictionary<string, string> alreadyCalculatedMasks = new Dictionary<string, string>();

                foreach (TextureSet textureSet in textureSetList) {
                    if (!alreadyCalculatedBases.ContainsKey(textureSet.FinalBase) &&
                        (!string.IsNullOrEmpty(textureSet.Base) || textureSet.BaseOverlays.Count > 0)) {
                        List<string> images = new List<string>();
                        images.Add(textureSet.Base);
                        images.AddRange(textureSet.BaseOverlays);
                        ImageManipulation.MergeImageLayers(images, textureSet.FinalBase);
                        alreadyCalculatedBases[textureSet.FinalBase] = "";
                    }

                    if (!alreadyCalculatedNormals.ContainsKey(textureSet.FinalNormal) &&
                        (!string.IsNullOrEmpty(textureSet.Normal) || textureSet.NormalOverlays.Count > 0)) {
                        List<string> images = new List<string>();
                        images.Add(textureSet.Normal);
                        images.AddRange(textureSet.NormalOverlays);
                        ImageManipulation.MergeImageLayers(images, textureSet.FinalNormal);
                        alreadyCalculatedNormals[textureSet.FinalNormal] = "";
                    }

                    if (!alreadyCalculatedMasks.ContainsKey(textureSet.FinalMask) &&
                        (!string.IsNullOrEmpty(textureSet.Mask) || textureSet.MaskOverlays.Count > 0)) {
                        List<string> images = new List<string>();
                        images.Add(textureSet.Mask);
                        images.AddRange(textureSet.MaskOverlays);
                        ImageManipulation.MergeImageLayers(images, textureSet.FinalMask);
                        alreadyCalculatedMasks[textureSet.FinalMask] = "";
                    }

                    if (!groups.ContainsKey(textureSet.GroupName)) {
                        groups.Add(textureSet.GroupName, new List<TextureSet>() { textureSet });
                        foreach (TextureSet childSet in textureSet.ChildSets) {
                            childSet.GroupName = textureSet.GroupName;
                            groups[textureSet.GroupName].Add(childSet);
                            BatchTextureSet(textureSet, childSet);
                            _exportMax += 4;
                        }
                    } else {
                        groups[textureSet.GroupName].Add(textureSet);
                        foreach (TextureSet childSet in textureSet.ChildSets) {
                            childSet.GroupName = textureSet.GroupName;
                            groups[textureSet.GroupName].Add(childSet);
                            BatchTextureSet(textureSet, childSet);
                            _exportMax += 4;
                        }
                    }
                }
                if (_finalizeResults) {
                    if (OnLaunchedXnormal != null) {
                        OnLaunchedXnormal.Invoke(this, EventArgs.Empty);
                    }
                    _xnormal.ProcessBatches();
                }
                if (OnStartedProcessing != null) {
                    OnStartedProcessing.Invoke(this, EventArgs.Empty);
                }
                foreach (List<TextureSet> textureSets in groups.Values) {
                    int choiceOption = groupOptionTypes.ContainsKey(textureSets[0].GroupName)
                    ? (groupOptionTypes[textureSets[0].GroupName] == 0
                    ? generationType : groupOptionTypes[textureSets[0].GroupName] - 1)
                    : generationType;
                    Group group = new Group(textureSets[0].GroupName.Replace(@"/", "-").Replace(@"\", "-"), "", 0,
                        (choiceOption == 2 && textureSets.Count > 1) ? "Single" : "Multi", 0);
                    Option option = null;
                    Option baseTextureOption = null;
                    Option normalOption = null;
                    Option maskOption = null;
                    Option materialOption = null;
                    bool alreadySetOption = false;
                    foreach (TextureSet textureSet in textureSets) {
                        string textureSetHash = GetHashFromTextureSet(textureSet);
                        string baseTextureDiskPath = "";
                        string normalDiskPath = "";
                        string maskDiskPath = "";
                        string materialDiskPath = "";
                        bool skipTexExport = false;
                        if (_redirectionCache.ContainsKey(textureSetHash)) {
                            baseTextureDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalBasePath, modPath, textureSetHash);
                            normalDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalNormalPath, modPath, textureSetHash);
                            maskDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalMaskPath, modPath, textureSetHash);
                            materialDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalMaterialPath,
                                modPath,
                             (_redirectionCache[textureSetHash].InternalMaterialPath + textureSetHash + textureSet.InternalBasePath.GetHashCode().ToString() + textureSet.InternalNormalPath.GetHashCode().ToString() + textureSet.InternalMaskPath.GetHashCode().ToString()).GetHashCode().ToString());
                            skipTexExport = true;
                        } else {
                            baseTextureDiskPath = GetDiskPath(textureSet.InternalBasePath, modPath, textureSetHash);
                            normalDiskPath = GetDiskPath(textureSet.InternalNormalPath, modPath, textureSetHash);
                            maskDiskPath = GetDiskPath(textureSet.InternalMaskPath, modPath, textureSetHash);
                            materialDiskPath = GetDiskPath(textureSet.InternalMaterialPath,
                                modPath, (textureSet.InternalMaterialPath + textureSetHash + textureSet.InternalBasePath.GetHashCode().ToString() + textureSet.InternalNormalPath.GetHashCode().ToString() + textureSet.InternalMaskPath.GetHashCode().ToString()).GetHashCode().ToString());
                            _redirectionCache.Add(textureSetHash, textureSet);
                        }
                        switch (choiceOption) {
                            case 0:
                                if (!string.IsNullOrEmpty(textureSet.FinalBase) && !string.IsNullOrEmpty(textureSet.InternalBasePath)) {
                                    if (BaseLogic(textureSet, baseTextureDiskPath, skipTexExport)) {
                                        AddDetailedGroupOption(textureSet.InternalBasePath,
                                            baseTextureDiskPath.Replace(modPath + "\\", null), "Base", "", textureSet,
                                            textureSets, group, baseTextureOption, out baseTextureOption);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                                    if (NormalLogic(textureSet, normalDiskPath, skipTexExport)) {
                                        AddDetailedGroupOption(textureSet.InternalNormalPath,
                                            normalDiskPath.Replace(modPath + "\\", null), "Normal", "", textureSet,
                                            textureSets, group, normalOption, out normalOption);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalMaskPath)) {
                                    if (MaskLogic(textureSet, maskDiskPath, skipTexExport)) {
                                        AddDetailedGroupOption(textureSet.InternalMaskPath,
                                            maskDiskPath.Replace(modPath + "\\", null), "Mask", "", textureSet,
                                            textureSets, group, maskOption, out maskOption);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if ((!string.IsNullOrEmpty(textureSet.Material) || !string.IsNullOrEmpty(textureSet.Glow))
                                    && !string.IsNullOrEmpty(textureSet.InternalMaterialPath)) {
                                    if (MaterialLogic(textureSet, materialDiskPath, false)) {
                                        AddDetailedGroupOption(textureSet.InternalMaterialPath,
                                            materialDiskPath.Replace(modPath + "\\", null), "Material", "", textureSet,
                                            textureSets, group, materialOption, out materialOption);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                break;
                            case 1:
                            case 2:
                            case 3:
                                if ((!textureSet.IsChildSet && choiceOption != 3) || (choiceOption == 3 && !alreadySetOption)) {
                                    if (!string.IsNullOrEmpty(textureSet.FinalBase) ||
                                        !string.IsNullOrEmpty(textureSet.FinalNormal) ||
                                        !string.IsNullOrEmpty(textureSet.FinalMask) ||
                                        !string.IsNullOrEmpty(textureSet.Glow) ||
                                        !string.IsNullOrEmpty(textureSet.Material)) {
                                        option = new Option(textureSet.TextureSetName == textureSet.GroupName || choiceOption == 3 ? "Enable"
                                        : textureSet.TextureSetName + (textureSet.ChildSets.Count > 0 ? " (Universal)" : ""), 0);
                                        group.Options.Add(option);
                                        alreadySetOption = true;
                                    }
                                }
                                if (!string.IsNullOrEmpty(textureSet.FinalBase) && !string.IsNullOrEmpty(textureSet.InternalBasePath)) {
                                    if (BaseLogic(textureSet, baseTextureDiskPath, skipTexExport)) {
                                        option.Files[textureSet.InternalBasePath] =
                                           baseTextureDiskPath.Replace(modPath + "\\", null);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                                    if (NormalLogic(textureSet, normalDiskPath, skipTexExport)) {
                                        option.Files[textureSet.InternalNormalPath] =
                                            normalDiskPath.Replace(modPath + "\\", null);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalMaskPath)) {
                                    if (MaskLogic(textureSet, maskDiskPath, skipTexExport)) {
                                        option.Files[textureSet.InternalMaskPath] =
                                           maskDiskPath.Replace(modPath + "\\", null);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if ((!string.IsNullOrEmpty(textureSet.Material) || !string.IsNullOrEmpty(textureSet.Glow))
                                    && !string.IsNullOrEmpty(textureSet.InternalMaterialPath)) {
                                    if (MaterialLogic(textureSet, materialDiskPath, false)) {
                                        option.Files[textureSet.InternalMaterialPath] =
                                           materialDiskPath.Replace(modPath + "\\", null);
                                    } else {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                break;
                        }
                    }
                    if (group.Options.Count > 0) {
                        string groupPath = Path.Combine(modPath, $"group_" + (1 + i++).ToString()
                        .PadLeft(3, '0') + $"_{group.Name.ToLower().Replace(" ", "_")}.json");
                        ExportGroup(groupPath, group);
                    }
                }
                while (_exportCompletion < _exportMax) {
                    Thread.Sleep(500);
                }
                foreach (TextureSet textureSet in textureSetList) {
                    textureSet.CleanTempFiles();
                }
            } catch (Exception e) {
                OnError?.Invoke(this, e.Message);
            }
        }

        private string GetDiskPath(string internalPath, string modPath, string id) {
            return !string.IsNullOrEmpty(internalPath) ?
            Path.Combine(modPath, AppendIdentifier(ImageManipulation.AddSuffix(
            RedirectToDisk(internalPath), "_" + id))) : "";
        }

        private string GetHashFromTextureSet(TextureSet textureSet) {
            string backupHash = "";
            if (textureSet.BackupTexturePaths != null) {
                if (!textureSet.BackupTexturePaths.IsFace) {
                    backupHash = (RaceInfo.ReverseRaceLookup(textureSet.InternalBasePath) == 6 ?
                    textureSet.BackupTexturePaths.BaseSecondary : textureSet.BackupTexturePaths.Base).GetHashCode().ToString();
                } else {
                    backupHash = (textureSet.BackupTexturePaths.Base + textureSet.BackupTexturePaths.BaseSecondary).GetHashCode().ToString();
                }
            }
            return (textureSet.FinalBase.GetHashCode().ToString() +
                textureSet.GroupName.GetHashCode().ToString() +
                textureSet.FinalNormal.GetHashCode().ToString() +
                textureSet.FinalMask.GetHashCode().ToString() +
                textureSet.Glow.GetHashCode().ToString() +
                textureSet.Material.GetHashCode().ToString() + backupHash).GetHashCode().ToString();
        }

        public string RedirectToDisk(string path) {
            return @"do_not_edit\textures\" + Path.GetFileName(path.Replace("/", @"\"));
        }
        public void AddDetailedGroupOption(string path, string diskPath, string name, string alternateName,
            TextureSet textureSet, List<TextureSet> textureSets, Group group, Option inputOption, out Option outputOption) {
            if (!textureSet.IsChildSet) {
                outputOption = new Option((textureSets.Count > 1 ? textureSet.TextureSetName + " " : "")
                + name + (textureSet.ChildSets.Count > 0 ? " (Universal)" : ""), 0);
                group.Options.Add(outputOption);
            } else {
                outputOption = inputOption;
            }
            outputOption.Files.Add(path, diskPath);
        }
        private bool MaskLogic(TextureSet textureSet, string maskDiskPath, bool skipTexExport) {
            bool outputGenerated = false;
            if (!string.IsNullOrEmpty(textureSet.FinalMask) && !string.IsNullOrEmpty(textureSet.InternalMaskPath)) {
                if (!string.IsNullOrEmpty(textureSet.FinalBase) && !textureSet.InternalMaskPath.Contains("/eye/")
                    && (textureSet.InternalMaskPath.Contains("obj/face") || textureSet.InternalMaskPath.Contains("obj/body"))) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.FinalMask, maskDiskPath, ExportType.DTMask, "", textureSet.FinalBase));
                    }
                } else if (textureSet.InternalMaskPath.Contains("etc_") || textureSet.InternalMaskPath.Contains("hair")) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.FinalMask, maskDiskPath, ExportType.DontManipulate));
                    }
                } else {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.FinalMask, maskDiskPath, ExportType.None));
                    }
                }
                outputGenerated = true;
            } else if (!string.IsNullOrEmpty(textureSet.FinalBase) && !string.IsNullOrEmpty(textureSet.InternalMaskPath)
                      && _generateMulti && !(textureSet.InternalMaskPath.ToLower().Contains("iri"))) {
                if (!textureSet.IgnoreMaskGeneration) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.FinalBase, maskDiskPath, ExportType.Mask, "",
                        textureSet.FinalBase, textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Base : ""));
                    }
                    outputGenerated = true;
                }
            }
            if (skipTexExport) {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }
        private bool MaterialLogic(TextureSet textureSet, string materialDiskPath, bool skipMaterialExport) {
            bool outputGenerated = false;
            if ((!string.IsNullOrEmpty(textureSet.Material)
                && !string.IsNullOrEmpty(textureSet.InternalMaterialPath))
                || !string.IsNullOrEmpty(textureSet.Glow)) {
                if (!skipMaterialExport) {
                    Task.Run(() => {
                        try {
                            Directory.CreateDirectory(Path.GetDirectoryName(materialDiskPath));
                            string value = !string.IsNullOrEmpty(textureSet.Material) ?
                            textureSet.Material :
                            Path.Combine((!string.IsNullOrEmpty(BasePath) ? BasePath :
                            AppDomain.CurrentDomain.BaseDirectory),
                            textureSet.InternalBasePath.Contains("eye") ?
                            @"res\materials\eye_glow.mtrl"
                            : @"res\materials\skin_glow.mtrl");

                            // Read donor .mtrl file
                            var data = File.ReadAllBytes(value);
                            MtrlFile mtrlFile = new MtrlFile(data);
                            int index = 0;

                            // Set texture paths on material.
                            if (!string.IsNullOrEmpty(textureSet.InternalBasePath)) {
                                mtrlFile.Textures[index++].Path = textureSet.InternalBasePath;
                            }
                            mtrlFile.Textures[index++].Path = textureSet.InternalNormalPath;
                            mtrlFile.Textures[index++].Path = textureSet.InternalMaskPath;

                            if (!string.IsNullOrEmpty(textureSet.Glow)) {
                                // Get emmisive values
                                MtrlFile.Constant constant = new MtrlFile.Constant();
                                foreach (var item in mtrlFile.ShaderPackage.Constants) {
                                    if (item.Id == 0x38A64362) {
                                        Color colour = ImageManipulation.CalculateMajorityColour(GetMergedBitmap(textureSet.Glow));
                                        constant = item;
                                        var constantValue = mtrlFile.GetConstantValue<float>(constant);

                                        // Set emmisive colour RGB
                                        constantValue[0] = (float)colour.R / 255f;
                                        constantValue[1] = (float)colour.G / 255f;
                                        constantValue[2] = (float)colour.B / 255f;
                                        break;
                                    }
                                }
                            }
                            while (TexIO.IsFileLocked(materialDiskPath)){
                                Thread.Sleep(1000);
                            }
                            File.WriteAllBytes(materialDiskPath, mtrlFile.Write());
                        } catch (Exception e) {
                            OnError?.Invoke(this, e.Message);
                        }
                        OnProgressChange?.Invoke(this, EventArgs.Empty);
                    });
                    outputGenerated = true;
                }
            }
            if (skipMaterialExport) {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        private bool NormalLogic(TextureSet textureSet, string normalDiskPath, bool skipTexExport) {
            bool outputGenerated = false;
            if (!string.IsNullOrEmpty(textureSet.FinalNormal) && !string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                if (_generateNormals && !textureSet.IgnoreNormalGeneration && !string.IsNullOrEmpty(textureSet.FinalBase)) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.FinalNormal, normalDiskPath, ExportType.MergeNormal,
                        textureSet.FinalBase, textureSet.NormalMask,
                        textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : "", textureSet.NormalCorrection, !textureSet.InternalBasePath.Contains("eye") ? textureSet.Glow : ""));
                    }
                    outputGenerated = true;
                } else {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.FinalNormal, normalDiskPath, ExportType.None, "", "",
                    textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : "", "", !textureSet.InternalBasePath.Contains("eye") ? textureSet.Glow : "",
                    false, textureSet.InvertNormalAlpha || !string.IsNullOrEmpty(textureSet.Glow), !string.IsNullOrEmpty(textureSet.Glow)));
                    }
                    outputGenerated = true;
                }
            } else if ((!string.IsNullOrEmpty(textureSet.FinalBase) || !string.IsNullOrEmpty(textureSet.Glow))
                  && !string.IsNullOrEmpty(textureSet.InternalNormalPath) && _generateNormals) {
                if (!textureSet.IgnoreNormalGeneration) {
                    if (textureSet.BackupTexturePaths != null) {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex((Path.Combine(_basePath, textureSet.BackupTexturePaths.Normal)),
                            normalDiskPath, ExportType.MergeNormal, textureSet.FinalBase, textureSet.NormalMask,
                            (textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : ""),
                            textureSet.NormalCorrection, !textureSet.InternalBasePath.Contains("eye") ? textureSet.Glow : "", textureSet.InvertNormalGeneration));
                        }
                        outputGenerated = true;
                    } else {
                        if (!textureSet.InternalBasePath.Contains("eye")) {
                            if (!skipTexExport) {
                                Task.Run(() => ExportTex(textureSet.FinalBase, normalDiskPath,
                                ExportType.Normal, "", textureSet.NormalMask, textureSet.BackupTexturePaths != null ?
                                textureSet.BackupTexturePaths.Base : "",
                                textureSet.NormalCorrection, textureSet.Glow, textureSet.InvertNormalGeneration));
                            }
                        }
                        outputGenerated = true;
                    }
                }
            } else if (!string.IsNullOrEmpty(textureSet.Glow)
                  && !string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                if (!textureSet.InternalBasePath.Contains("eye")) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.BackupTexturePaths != null ?
                        textureSet.BackupTexturePaths.Normal : "", normalDiskPath,
                        ExportType.None, "", textureSet.NormalMask, "",
                        textureSet.NormalCorrection, textureSet.Glow, textureSet.InvertNormalGeneration, textureSet.InternalBasePath.Contains("fac_")));
                    }
                }
                outputGenerated = true;
            }
            if (skipTexExport) {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        private bool BaseLogic(TextureSet textureSet, string baseTextureDiskPath, bool skipTexExport) {
            bool outputGenerated = false;
            string underlay = "";
            if (textureSet.BackupTexturePaths != null) {
                if (!textureSet.BackupTexturePaths.IsFace) {
                    underlay = (RaceInfo.ReverseRaceLookup(textureSet.InternalBasePath) == 6 ?
                         textureSet.BackupTexturePaths.BaseSecondary : textureSet.BackupTexturePaths.Base);
                } else {
                    underlay = textureSet.BackupTexturePaths.Base;
                }
            }
            if (!string.IsNullOrEmpty(textureSet.FinalBase)) {
                if (!skipTexExport) {
                    Task.Run(() => ExportTex(textureSet.FinalBase, baseTextureDiskPath, ExportType.None, "", "", underlay));
                }
                outputGenerated = true;
            }
            if (skipTexExport) {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        public void CleanGeneratedAssets(string path) {
            foreach (string file in Directory.EnumerateFiles(path)) {
                if (file.Contains("_generated")) {
                    File.Delete(file);
                }
                if (file.EndsWith(".json")) {
                    bool isGenerated = false;
                    using (StreamReader jsonFile = File.OpenText(file)) {
                        try {
                            JsonSerializer serializer = new JsonSerializer();
                            Group group = (Group)serializer.Deserialize(jsonFile, typeof(Group));
                            if (!string.IsNullOrEmpty(group.Description) && group.Description.Contains("-generated")) {
                                isGenerated = true;
                            }
                        } catch {
                            // Todo: should we report when we skip a .json we cant read?
                        }
                    }
                    if (isGenerated) {
                        File.Delete(file);
                    }
                }
            }
            foreach (string directory in Directory.EnumerateDirectories(path)) {
                CleanGeneratedAssets(directory);
            }
        }

        private void ExportGroup(string path, Group group) {
            group.Description += " -generated";
            bool isSingle = group.Type == "Single";
            if (path != null) {
                if (group.Options.Count > (isSingle ? int.MaxValue : 32)) {
                    int groupsToSplitTo = group.Options.Count / 32;
                    for (int i = 0; i < groupsToSplitTo; i++) {
                        int rangeStartingPoint = 32 * i;
                        int maxRange = group.Options.Count - rangeStartingPoint;
                        Group newGroup = new Group(group.Name + $" ({i + 1})", group.Description + " -generated",
                                        group.Priority, group.Type, group.DefaultSettings);
                        newGroup.Options = group.Options.GetRange(rangeStartingPoint, maxRange > 32 ? 32 : maxRange);
                        using (StreamWriter file = File.CreateText(path.Replace(".", $" ({i})."))) {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Formatting = Formatting.Indented;
                            serializer.Serialize(file, newGroup);
                        }
                    }
                } else if (group.Options.Count > 0) {
                    using (StreamWriter file = File.CreateText(path)) {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Formatting.Indented;
                        serializer.Serialize(file, group);
                    }
                }
            }
        }

        public enum ExportType {
            None,
            Normal,
            Mask,
            MergeNormal,
            Glow,
            GlowEyeMask,
            XNormalImport,
            DontManipulate,
            DTMask,
        }
        public async Task<bool> ExportTex(string inputFile, string outputFile, ExportType exportType = ExportType.None,
            string baseTextureNormal = "", string modifierMap = "", string layeringImage = "",
            string normalCorrection = "", string alphaOverride = "", bool modifier = false, bool invertAlpha = false, bool dontInvertAlphaOverride = false) {
            byte[] data = new byte[0];
            bool skipPngTexConversion = false;
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    switch (exportType) {
                        case ExportType.None:
                            ExportTypeNone(inputFile, layeringImage, stream, alphaOverride, invertAlpha, dontInvertAlphaOverride);
                            break;
                        case ExportType.DontManipulate:
                            data = TexIO.GetTexBytes(inputFile);
                            skipPngTexConversion = true;
                            break;
                        case ExportType.Glow:
                            ExportTypeGlow(inputFile, modifierMap, layeringImage, stream);
                            break;
                        case ExportType.GlowEyeMask:
                            ExportTypeGlowEyeMask(inputFile, modifierMap, stream);
                            break;
                        case ExportType.DTMask:
                            ExportTypeDTMask(inputFile, modifierMap, stream);
                            break;
                        case ExportType.Normal:
                            ExportTypeNormal(inputFile, outputFile, modifierMap, normalCorrection, modifier, stream, alphaOverride, invertAlpha);
                            break;
                        case ExportType.Mask:
                            ExportTypeMask(inputFile, layeringImage, exportType, modifierMap, stream);
                            break;
                        case ExportType.MergeNormal:
                            ExportTypeMergeNormal(inputFile, outputFile, layeringImage, baseTextureNormal, modifierMap,
                            normalCorrection, stream, modifier, alphaOverride, invertAlpha);
                            break;
                        case ExportType.XNormalImport:
                            ExportTypeXNormalImport(inputFile, baseTextureNormal, stream);
                            break;
                    }
                    if (!skipPngTexConversion) {
                        stream.Flush();
                        stream.Position = 0;
                        if (stream.Length > 0) {
                            PenumbraTextureImporter.PngToTex(stream, out data);
                            stream.Position = 0;
                        }
                    }
                }
                if (data.Length > 0) {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                    while (TexIO.IsFileLocked(outputFile)) {
                        Thread.Sleep(500);
                    }
                    if (File.Exists(outputFile)) {
                        File.Delete(outputFile);
                    }
                    File.WriteAllBytes(outputFile, data);
                }
            } catch (Exception e) {
                OnError?.Invoke(this, e.Message);
            }
            if (OnProgressChange != null) {
                OnProgressChange.Invoke(this, EventArgs.Empty);
            }
            return true;
        }

        private void ExportTypeXNormalImport(string inputFile, string baseTextureNormal, Stream stream) {
            using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                if (bitmap != null) {
                    Bitmap underlay = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(underlay);
                    g.Clear(Color.FromArgb(255, 160, 113, 94));
                    if (!string.IsNullOrEmpty(baseTextureNormal)) {
                        //g.CompositingQuality = CompositingQuality.HighQuality;
                        //g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        //g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(TexIO.ResolveBitmap(baseTextureNormal), 0, 0, bitmap.Width, bitmap.Height);
                    }
                    MapWriting.TransplantData(underlay, bitmap).Save(stream, ImageFormat.Png);
                }
            }
        }

        private void ExportTypeMergeNormal(string inputFile, string outputFile, string layeringImage,
            string baseTextureNormal, string modifierMap, string normalCorrection, Stream stream, bool modifier, string alphaOverride, bool invertAlpha) {
            Bitmap output = null;
            if (!string.IsNullOrEmpty(baseTextureNormal)) {
                lock (_normalCache) {
                    if (!_normalCache.ContainsKey(baseTextureNormal)) {
                        using (Bitmap baseTexture = TexIO.ResolveBitmap(baseTextureNormal)) {
                            if (baseTexture != null) {
                                using (Bitmap canvasImage = new Bitmap(baseTexture.Size.Width,
                                    baseTexture.Size.Height, PixelFormat.Format32bppArgb)) {
                                    output = null;
                                    if (File.Exists(modifierMap)) {
                                        using (Bitmap normalMaskBitmap = TexIO.ResolveBitmap(modifierMap)) {
                                            output = ImageManipulation.MergeNormals(TexIO.ResolveBitmap(inputFile), baseTexture,
                                                canvasImage, normalMaskBitmap, baseTextureNormal, modifier);
                                        }
                                    } else {
                                        using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                                            if (bitmap != null) {
                                                if (!string.IsNullOrEmpty(layeringImage)) {
                                                    Bitmap bottomLayer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage));
                                                    Bitmap topLayer = GetMergedBitmap(inputFile);
                                                    output = ImageManipulation.MergeNormals(ImageManipulation.LayerImages(bottomLayer, topLayer), baseTexture, canvasImage, null, baseTextureNormal, modifier);
                                                } else {
                                                    output = ImageManipulation.MergeNormals(TexIO.ResolveBitmap(inputFile), baseTexture, canvasImage, null, baseTextureNormal, modifier);
                                                }
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(normalCorrection)) {
                                        output = ImageManipulation.ResizeAndMerge(output, TexIO.ResolveBitmap(normalCorrection));
                                    }
                                    if (!string.IsNullOrEmpty(alphaOverride)) {
                                        var bitmap = Grayscale.MakeGrayscale(TexIO.ResolveBitmap(alphaOverride));
                                        var rgb = ImageManipulation.ExtractRGB(output);
                                        if (output.Size.Height < bitmap.Size.Height) {
                                            rgb = ImageManipulation.Resize(rgb, bitmap.Size.Width, bitmap.Size.Height);
                                        } else {
                                            bitmap = ImageManipulation.Resize(bitmap, output.Size.Width, output.Size.Height);
                                        }
                                        output = ImageManipulation.MergeAlphaToRGB(bitmap, rgb);
                                    }
                                    output.Save(stream, ImageFormat.Png);
                                    _normalCache.Add(baseTextureNormal, output);
                                }
                            }
                        }
                    } else {
                        _normalCache[baseTextureNormal].Save(stream, ImageFormat.Png);
                    }
                }
            }
        }

        private void ExportTypeMask(string inputFile, string layeringImage, ExportType exportType, string modifierMap, Stream stream) {
            lock (_maskCache) {
                if (_maskCache.ContainsKey(inputFile)) {
                    TexIO.SaveBitmap(_maskCache[inputFile], stream);
                } else {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            Bitmap image;
                            if (layeringImage != null) {
                                image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                Bitmap layer = TexIO.ResolveBitmap(Path.Combine(_basePath,
                                    layeringImage));
                                Graphics g = Graphics.FromImage(image);
                                g.Clear(Color.Transparent);
                                //g.CompositingQuality = CompositingQuality.HighQuality;
                                //g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                //g.SmoothingMode = SmoothingMode.HighQuality;
                                g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                g.DrawImage(GetMergedBitmap(inputFile), 0, 0, bitmap.Width, bitmap.Height);
                            } else {
                                image = bitmap;
                            }
                            Bitmap generatedMulti = ImageManipulation.ConvertBaseToDawntrailSkinMulti(image);
                            Bitmap mask = !string.IsNullOrEmpty(modifierMap)
                                ? MapWriting.CalculateMulti(generatedMulti, TexIO.ResolveBitmap(modifierMap))
                                : generatedMulti;
                            mask.Save(stream, ImageFormat.Png);
                            _maskCache.Add(inputFile, mask);
                        }
                    }
                }
            }
        }

        private void ExportTypeNormal(string inputFile, string outputFile, string modifierMap,
            string normalCorrection, bool modifier, Stream stream, string alphaOverride, bool invertAlpha) {
            Bitmap output;
            lock (_normalCache) {
                if (_normalCache.ContainsKey(inputFile)) {
                    output = _normalCache[inputFile];
                } else {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                        using (Bitmap target = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, PixelFormat.Format32bppArgb)) {
                            Graphics g = Graphics.FromImage(target);
                            g.Clear(Color.Transparent);
                            ImageManipulation.DrawImage(target, bitmap, 0, 0, bitmap.Width, bitmap.Height);
                            if (File.Exists(modifierMap)) {
                                using (Bitmap normalMaskBitmap = TexIO.ResolveBitmap(modifierMap)) {
                                    output = Normal.Calculate(modifier ? ImageManipulation.InvertImage(target)
                                        : target, normalMaskBitmap);
                                }
                            } else {
                                output = Normal.Calculate(modifier ? ImageManipulation.InvertImage(target) : target);
                            }
                            if (!string.IsNullOrEmpty(alphaOverride)) {
                                output = ImageManipulation.LayerImages(output, output, alphaOverride, invertAlpha);
                            }
                            _normalCache.Add(inputFile, output);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(normalCorrection)) {
                output = ImageManipulation.ResizeAndMerge(output, TexIO.ResolveBitmap(normalCorrection));
            }
            output.Save(stream, ImageFormat.Png);
        }

        private void ExportTypeDTMask(string inputFile, string mask, Stream stream) {
            string descriminator = inputFile + mask + "glowMulti";
            Bitmap glowOutput;
            lock (_glowCache) {
                if (_glowCache.ContainsKey(descriminator)) {
                    glowOutput = _glowCache[descriminator];
                    glowOutput.Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            Bitmap maskChannelMap = MapWriting.CalculateMulti(bitmap, TexIO.ResolveBitmap(mask));
                            maskChannelMap.Save(stream, ImageFormat.Png);
                            _glowCache.Add(descriminator, maskChannelMap);
                        }
                    }
                }
            }
        }

        private void ExportTypeGlowEyeMask(string inputFile, string mask, Stream stream) {
            string descriminator = inputFile + mask + "glowEyeMulti";
            Bitmap glowOutput;
            lock (_glowCache) {
                if (_glowCache.ContainsKey(descriminator)) {
                    glowOutput = _glowCache[descriminator];
                    glowOutput.Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            Bitmap glowBitmap = MapWriting.CalculateEyeMulti(bitmap, TexIO.ResolveBitmap(mask));
                            glowBitmap.Save(stream, ImageFormat.Png);
                            _glowCache.Add(descriminator, glowBitmap);
                        }
                    }
                }
            }
        }

        private void ExportTypeGlow(string inputFile, string glowMap, string layeringImage, Stream stream) {
            Bitmap glowOutput = null;
            string descriminator = inputFile + glowMap + "glow";
            lock (_glowCache) {
                if (_glowCache.ContainsKey(descriminator)) {
                    glowOutput = _glowCache[descriminator];
                    glowOutput.Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            if (!string.IsNullOrEmpty(layeringImage)) {
                                Bitmap image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                Bitmap layer = TexIO.ResolveBitmap(Path.Combine(_basePath,
                                    layeringImage));
                                Graphics g = Graphics.FromImage(image);
                                g.Clear(Color.Transparent);
                                //g.CompositingQuality = CompositingQuality.HighQuality;
                                //g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                //g.SmoothingMode = SmoothingMode.HighQuality;
                                g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                g.DrawImage(GetMergedBitmap(inputFile), 0, 0, bitmap.Width, bitmap.Height);
                                Bitmap glowBitmap = MapWriting.CalculateBase(image,
                                    ImageManipulation.Resize(GetMergedBitmap(glowMap), bitmap.Width, bitmap.Height));
                                glowBitmap.Save(stream, ImageFormat.Png);
                                _glowCache.Add(descriminator, glowBitmap);
                            } else {
                                Bitmap glowBitmap = MapWriting.CalculateBase(bitmap, TexIO.ResolveBitmap(glowMap));
                                glowBitmap.Save(stream, ImageFormat.Png);
                                _glowCache.Add(descriminator, glowBitmap);
                            }
                        }
                    }
                }
            }
        }

        private void ExportTypeNone(string inputFile, string layeringImage, Stream stream, string alphaOverride = "", bool invertAlpha = false, bool dontInvertAlphaOverrid = false) {
            if (!string.IsNullOrEmpty(layeringImage)) {
                Bitmap bottomLayer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage));
                Bitmap topLayer = GetMergedBitmap(inputFile);
                TexIO.SaveBitmap(ImageManipulation.LayerImages(bottomLayer, topLayer, alphaOverride, invertAlpha, dontInvertAlphaOverrid), stream);
            } else {
                using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile.StartsWith(@"res\") ? Path.Combine(_basePath, inputFile) : inputFile)) {
                    if (bitmap != null) {
                        if (string.IsNullOrEmpty(alphaOverride)) {
                            TexIO.SaveBitmap(bitmap, stream);
                        } else {
                            TexIO.SaveBitmap(ImageManipulation.MergeAlphaToRGB(TexIO.Resize(Grayscale.MakeGrayscale(TexIO.ResolveBitmap(alphaOverride)), bitmap.Width, bitmap.Height), bitmap), stream);
                        }
                    }
                }
            }
        }



        public string AppendIdentifier(string value) {
            return ImageManipulation.AddSuffix(value, "_generated");
        }
    }
}
