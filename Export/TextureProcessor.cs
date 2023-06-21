using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;
using FFXIVLooseTextureCompiler.Racial;
using FFXIVVoicePackCreator.Json;
using Newtonsoft.Json;
using Penumbra.LTCImport.Dds;
using static FFXIVLooseTextureCompiler.TextureProcessor;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace FFXIVLooseTextureCompiler {
    public class TextureProcessor {
        private Dictionary<string, TextureSet> _redirectionCache;
        private Dictionary<string, Bitmap> _normalCache;
        private Dictionary<string, Bitmap> _multiCache;
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

        private Bitmap GetMergedBitmap(string file) {
            if (file.Contains("baseTexBaked") && (file.Contains("_d_") || file.Contains("_g_") || file.Contains("_n_"))) {
                Bitmap alpha = TexLoader.ResolveBitmap(file.Replace("baseTexBaked", "alpha_baseTexBaked"));
                Bitmap rgb = TexLoader.ResolveBitmap(file.Replace("baseTexBaked", "rgb_baseTexBaked"));
                Bitmap merged = ImageManipulation.MergeAlphaToRGB(alpha, rgb);
                merged.Save(file, ImageFormat.Png);
                return merged;
            } else {
                return TexLoader.ResolveBitmap(file);
            }
        }

        public void BatchTextureSet(TextureSet parent, TextureSet child) {
            if (!string.IsNullOrEmpty(child.Diffuse)) {
                if (!_xnormalCache.ContainsKey(child.Diffuse)) {
                    string diffuseAlpha = ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(parent.Diffuse, "_alpha"), ".png");
                    string diffuseRGB = ImageManipulation.ReplaceExtension(
                    ImageManipulation.AddSuffix(parent.Diffuse, "_rgb"), ".png");
                    if (_finalizeResults || !File.Exists(child.Diffuse.Replace("baseTexBaked", "rgb_baseTexBaked"))
                        || !File.Exists(child.Diffuse.Replace("baseTexBaked", "alpha_baseTexBaked"))) {
                        if (child.Diffuse.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.Diffuse, child.Diffuse);
                            Bitmap diffuse = TexLoader.ResolveBitmap(parent.Diffuse);
                            if (Directory.Exists(Path.GetDirectoryName(diffuseAlpha))
                                && Directory.Exists(Path.GetDirectoryName(diffuseRGB))) {
                                string childAlpha = child.Diffuse.Replace("baseTexBaked", "alpha");
                                string childRGB = child.Diffuse.Replace("baseTexBaked", "rgb");
                                ImageManipulation.ExtractTransparency(diffuse).Save(diffuseAlpha, ImageFormat.Png);
                                ImageManipulation.ExtractRGB(diffuse).Save(diffuseRGB, ImageFormat.Png);
                                if (_finalizeResults) {
                                    _xnormal.AddToBatch(parent.InternalDiffusePath, diffuseAlpha, childAlpha, false);
                                    _xnormal.AddToBatch(parent.InternalDiffusePath, diffuseRGB, childRGB, false);
                                } else {
                                    if (!File.Exists(childAlpha)) {
                                        new Bitmap(1024, 1024).Save(ImageManipulation.AddSuffix(childAlpha, "_baseTexBaked"), ImageFormat.Png);
                                    }
                                    if (!File.Exists(childRGB)) {
                                        new Bitmap(1024, 1024).Save(ImageManipulation.AddSuffix(childRGB, "_baseTexBaked"), ImageFormat.Png);
                                    }
                                }
                            } else {
                                //MessageBox.Show("Something has gone terribly wrong. " + parent.Diffuse + "is missing");
                            }
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.Normal)) {
                if (!_xnormalCache.ContainsKey(child.Normal)) {
                    string normalAlpha = ImageManipulation.AddSuffix(parent.Normal, "_alpha");
                    string normalRGB = ImageManipulation.AddSuffix(parent.Normal, "_rgb");
                    if (_finalizeResults || !File.Exists(child.Normal.Replace("baseTexBaked", "rgb_baseTexBaked"))
                        || !File.Exists(child.Normal.Replace("baseTexBaked", "alpha_baseTexBaked"))) {
                        if (child.Normal.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.Normal, child.Normal);
                            Bitmap normal = TexLoader.ResolveBitmap(parent.Normal);
                            ImageManipulation.ExtractTransparency(normal).Save(normalAlpha, ImageFormat.Png);
                            ImageManipulation.ExtractRGB(normal, true).Save(normalRGB, ImageFormat.Png);
                            _xnormal.AddToBatch(parent.InternalDiffusePath, normalAlpha, child.Normal.Replace("baseTexBaked", "alpha"), false);
                            _xnormal.AddToBatch(parent.InternalDiffusePath, normalRGB, child.Normal.Replace("baseTexBaked", "rgb"), true);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.Multi)) {
                if (!_xnormalCache.ContainsKey(child.Multi)) {
                    if (_finalizeResults || !File.Exists(child.Multi)) {
                        if (child.Multi.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.Multi, child.Multi);
                            _xnormal.AddToBatch(parent.InternalMultiPath, parent.Multi, child.Multi, false);
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
                            Bitmap glow = TexLoader.ResolveBitmap(parent.Glow);
                            ImageManipulation.ExtractTransparency(glow).Save(glowAlpha, ImageFormat.Png);
                            ImageManipulation.ExtractRGB(glow).Save(glowRGB, ImageFormat.Png);
                            _xnormal.AddToBatch(parent.InternalDiffusePath, glowAlpha, child.Glow.Replace("baseTexBaked", "alpha"), false);
                            _xnormal.AddToBatch(parent.InternalDiffusePath, glowRGB, child.Glow.Replace("baseTexBaked", "rgb"), false);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.NormalMask)) {
                if (!_xnormalCache.ContainsKey(child.NormalMask)) {
                    string normalMaskAlpha = ImageManipulation.AddSuffix(parent.NormalMask, "_alpha");
                    string normalMaskRGB = ImageManipulation.AddSuffix(parent.NormalMask, "_rgb");
                    if (_finalizeResults || !File.Exists(child.NormalMask.Replace("baseTexBaked", "rgb_baseTexBaked"))
                        || !File.Exists(child.NormalMask.Replace("baseTexBaked", "alpha_baseTexBaked"))) {
                        if (child.NormalMask.Contains("baseTexBaked")) {
                            _xnormalCache.Add(child.NormalMask, child.NormalMask);
                            Bitmap normalMask = TexLoader.ResolveBitmap(parent.NormalMask);
                            ImageManipulation.ExtractTransparency(normalMask).Save(normalMaskAlpha, ImageFormat.Png);
                            ImageManipulation.ExtractRGB(normalMask).Save(normalMaskRGB, ImageFormat.Png);
                            _xnormal.AddToBatch(parent.InternalDiffusePath, normalMaskAlpha, child.NormalMask.Replace("baseTexBaked", "alpha"), false);
                            _xnormal.AddToBatch(parent.InternalDiffusePath, normalMaskRGB, child.NormalMask.Replace("baseTexBaked", "rgb"), false);
                        }
                    }
                }
            }
        }

        public async Task<bool> Export(List<TextureSet> textureSetList, Dictionary<string, int> groupOptionTypes,
            string modPath, int generationType, bool generateNormals,
            bool generateMulti, bool useXNormal, string xNormalPathOverride = "") {
            Dictionary<string, List<TextureSet>> groups = new Dictionary<string, List<TextureSet>>();
            int i = 0;
            _fileCount = 0;
            _finalizeResults = useXNormal;
            _normalCache?.Clear();
            _multiCache?.Clear();
            _glowCache?.Clear();
            _xnormalCache?.Clear();
            _redirectionCache?.Clear();
            _normalCache = new Dictionary<string, Bitmap>();
            _multiCache = new Dictionary<string, Bitmap>();
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
            _exportMax = textureSetList.Count * 3;
            foreach (TextureSet textureSet in textureSetList) {
                if (!groups.ContainsKey(textureSet.GroupName)) {
                    groups.Add(textureSet.GroupName, new List<TextureSet>() { textureSet });
                    foreach (TextureSet childSet in textureSet.ChildSets) {
                        childSet.GroupName = textureSet.GroupName;
                        groups[textureSet.GroupName].Add(childSet);
                        BatchTextureSet(textureSet, childSet);
                        _exportMax += 3;
                    }
                } else {
                    groups[textureSet.GroupName].Add(textureSet);
                    foreach (TextureSet childSet in textureSet.ChildSets) {
                        childSet.GroupName = textureSet.GroupName;
                        groups[textureSet.GroupName].Add(childSet);
                        BatchTextureSet(textureSet, childSet);
                        _exportMax += 3;
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
                Option diffuseOption = null;
                Option normalOption = null;
                Option multiOption = null;
                bool alreadySetOption = false;
                foreach (TextureSet textureSet in textureSets) {
                    string textureSetHash = GetHashFromTextureSet(textureSet);
                    string diffuseDiskPath = "";
                    string normalDiskPath = "";
                    string multiDiskPath = "";
                    bool skipTexExport = false;
                    if (_redirectionCache.ContainsKey(textureSetHash)) {
                        diffuseDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalDiffusePath, modPath, textureSetHash);
                        normalDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalNormalPath, modPath, textureSetHash);
                        multiDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalMultiPath, modPath, textureSetHash);
                        skipTexExport = true;
                    } else {
                        diffuseDiskPath = GetDiskPath(textureSet.InternalDiffusePath, modPath, textureSetHash);
                        normalDiskPath = GetDiskPath(textureSet.InternalNormalPath, modPath, textureSetHash);
                        multiDiskPath = GetDiskPath(textureSet.InternalMultiPath, modPath, textureSetHash);
                        _redirectionCache.Add(textureSetHash, textureSet);
                    }
                    switch (choiceOption) {
                        case 0:
                            if (!string.IsNullOrEmpty(textureSet.Diffuse) && !string.IsNullOrEmpty(textureSet.InternalDiffusePath)) {
                                if (DiffuseLogic(textureSet, diffuseDiskPath, skipTexExport)) {
                                    AddDetailedGroupOption(textureSet.InternalDiffusePath, 
                                        diffuseDiskPath.Replace(modPath, null), "Diffuse", "Normal", textureSet,
                                        textureSets, group, diffuseOption, out diffuseOption);
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                            } else {
                                OnProgressChange.Invoke(this, EventArgs.Empty);
                            }
                            if (!string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                                if (NormalLogic(textureSet, normalDiskPath, skipTexExport)) {
                                    AddDetailedGroupOption(textureSet.InternalNormalPath, 
                                        normalDiskPath.Replace(modPath, null), "Normal", "Multi", textureSet,
                                        textureSets, group, normalOption, out normalOption);
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                            } else {
                                OnProgressChange.Invoke(this, EventArgs.Empty);
                            }
                            if (!string.IsNullOrEmpty(textureSet.InternalMultiPath)) {
                                if (MultiLogic(textureSet, multiDiskPath, skipTexExport)) {
                                    AddDetailedGroupOption(textureSet.InternalMultiPath, 
                                        multiDiskPath.Replace(modPath, null), "Multi", "Catchlight", textureSet,
                                        textureSets, group, multiOption, out multiOption);
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
                                if (!string.IsNullOrEmpty(textureSet.Diffuse) ||
                                    !string.IsNullOrEmpty(textureSet.Normal) ||
                                    !string.IsNullOrEmpty(textureSet.Multi)) {
                                    option = new Option(textureSet.TextureSetName == textureSet.GroupName || choiceOption == 3 ? "Enable"
                                    : textureSet.TextureSetName + (textureSet.ChildSets.Count > 0 ? " (Universal)" : ""), 0);
                                    group.Options.Add(option);
                                    alreadySetOption = true;
                                }
                            }
                            if (!string.IsNullOrEmpty(textureSet.Diffuse) && !string.IsNullOrEmpty(textureSet.InternalDiffusePath)) {
                                if (DiffuseLogic(textureSet, diffuseDiskPath, skipTexExport)) {
                                    option.Files[textureSet.InternalDiffusePath] =
                                       diffuseDiskPath.Replace(modPath, null);
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                            } else {
                                OnProgressChange.Invoke(this, EventArgs.Empty);
                            }
                            if (!string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                                if (NormalLogic(textureSet, normalDiskPath, skipTexExport)) {
                                    option.Files[textureSet.InternalNormalPath] =
                                        normalDiskPath.Replace(modPath, null);
                                } else {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                            } else {
                                OnProgressChange.Invoke(this, EventArgs.Empty);
                            }
                            if (!string.IsNullOrEmpty(textureSet.InternalMultiPath)) {
                                if (MultiLogic(textureSet, multiDiskPath, skipTexExport)) {
                                    option.Files[textureSet.InternalMultiPath] =
                                       multiDiskPath.Replace(modPath, null);
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
                    .PadLeft(3,'0') + $"_{group.Name.ToLower().Replace(" ", "_")}.json");
                    ExportGroup(groupPath, group);
                }
            }
            while (_exportCompletion < _exportMax) {
                Thread.Sleep(500);
            }
            foreach (Bitmap value in _normalCache.Values) {
                value.Dispose();
            }
            foreach (Bitmap value in _multiCache.Values) {
                value.Dispose();
            }
            foreach (Bitmap value in _glowCache.Values) {
                value.Dispose();
            }
            return true;
        }

        private string GetDiskPath(string internalPath, string modPath, string id) {
            return !string.IsNullOrEmpty(internalPath) ?
            Path.Combine(modPath, AppendIdentifier(ImageManipulation.AddSuffix(
            RedirectToDisk(internalPath), "_" + id))) : "";
        }

        private string GetHashFromTextureSet(TextureSet textureSet) {
            return textureSet.Diffuse.GetHashCode().ToString() +
                textureSet.Normal.GetHashCode().ToString() +
                textureSet.Multi.GetHashCode().ToString() +
                textureSet.Glow.GetHashCode().ToString() +
                textureSet.NormalMask.GetHashCode().ToString();
        }

        public string RedirectToDisk(string path) {
            return @"do_not_edit\" + path.Replace("/", @"\");
        }
        public void AddDetailedGroupOption(string path, string diskPath, string name, string alternateName,
            TextureSet textureSet, List<TextureSet> textureSets, Group group, Option inputOption, out Option outputOption) {
            if (!textureSet.IsChildSet) {
                outputOption = new Option((textureSets.Count > 1 ? textureSet.TextureSetName + " " : "")
                + (textureSet.InternalMultiPath.ToLower().Contains("catchlight") ? alternateName : name)
                + (textureSet.ChildSets.Count > 0 ? " (Universal)" : ""), 0);
                group.Options.Add(outputOption);
            } else {
                outputOption = inputOption;
            }
            outputOption.Files.Add(path, diskPath);
        }
        private bool MultiLogic(TextureSet textureSet, string multiDiskPath, bool skipTexExport) {
            bool outputGenerated = false;
            if (!string.IsNullOrEmpty(textureSet.Multi) && !string.IsNullOrEmpty(textureSet.InternalMultiPath)) {
                if (!string.IsNullOrEmpty(textureSet.Glow) && !textureSet.InternalMultiPath.Contains("catchlight")) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.Multi, multiDiskPath, ExportType.GlowMulti, "", textureSet.Glow));
                    }
                } else {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.Multi, multiDiskPath, ExportType.None));
                    }
                }
                outputGenerated = true;
            } else if (!string.IsNullOrEmpty(textureSet.Diffuse) && !string.IsNullOrEmpty(textureSet.InternalMultiPath)
                  && _generateMulti && !(textureSet.InternalMultiPath.ToLower().Contains("catchlight"))) {
                if (!textureSet.IgnoreMultiGeneration) {
                    if (textureSet.InternalDiffusePath.Contains("b0001_b_d") || textureSet.InternalDiffusePath.Contains("b0101_b_d")) {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex(textureSet.Diffuse, multiDiskPath, ExportType.MultiTbse, "", textureSet.Glow,
                        textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Diffuse : ""));
                        }
                    } else if (textureSet.InternalDiffusePath.Contains("fac_b_d")) {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex(textureSet.Diffuse, multiDiskPath, ExportType.MultiFaceAsym, "",
                        textureSet.Glow, textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Diffuse : ""));
                        }
                    } else if (textureSet.InternalDiffusePath.Contains("fac_d")) {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex(textureSet.Diffuse, multiDiskPath, ExportType.MultiFace, "",
                        textureSet.Glow, textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Diffuse : ""));
                        }
                    } else {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex(textureSet.Diffuse, multiDiskPath, ExportType.Multi, "",
                        textureSet.Glow, textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Diffuse : ""));
                        }
                    }
                    outputGenerated = true;
                }
            }
            if (skipTexExport) {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        private bool NormalLogic(TextureSet textureSet, string normalDiskPath, bool skipTexExport) {
            bool outputGenerated = false;
            if (!string.IsNullOrEmpty(textureSet.Normal) && !string.IsNullOrEmpty(textureSet.InternalNormalPath)) {
                if (_generateNormals && !textureSet.InternalMultiPath.ToLower().Contains("catchlight")) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.Normal, normalDiskPath, ExportType.MergeNormal,
                        textureSet.Diffuse, textureSet.NormalMask,
                        textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : "", textureSet.NormalCorrection));
                    }
                    outputGenerated = true;
                } else {
                    if (!string.IsNullOrEmpty(textureSet.Glow) && (textureSet.InternalMultiPath.ToLower().Contains("catchlight"))) {
                        Task.Run(() => ExportTex(textureSet.Normal, normalDiskPath, ExportType.GlowEyeMulti, "", textureSet.Glow));
                        outputGenerated = true;
                    } else {
                        Task.Run(() => ExportTex(textureSet.Normal, normalDiskPath, ExportType.None, "", "",
                        textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : ""));
                        outputGenerated = true;
                    }
                }
            } else if (!string.IsNullOrEmpty(textureSet.Diffuse) && !string.IsNullOrEmpty(textureSet.InternalNormalPath)
              && _generateNormals && !(textureSet.InternalMultiPath.ToLower().Contains("catchlight"))) {
                if (!textureSet.IgnoreNormalGeneration) {
                    if (textureSet.BackupTexturePaths != null) {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex((Path.Combine(_basePath, textureSet.BackupTexturePaths.Normal)),
                            normalDiskPath, ExportType.MergeNormal, textureSet.Diffuse, textureSet.NormalMask,
                            (textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : ""),
                            textureSet.NormalCorrection, textureSet.InvertNormalGeneration));
                        }
                        outputGenerated = true;
                    } else {
                        if (!skipTexExport) {
                            Task.Run(() => ExportTex(textureSet.Diffuse, normalDiskPath,
                            ExportType.Normal, "", textureSet.NormalMask, textureSet.BackupTexturePaths != null ?
                            textureSet.BackupTexturePaths.Diffuse : "",
                            textureSet.NormalCorrection, textureSet.InvertNormalGeneration));
                        }
                        outputGenerated = true;
                    }
                }
            }
            if (skipTexExport) {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        private bool DiffuseLogic(TextureSet textureSet, string diffuseDiskPath, bool skipTexExport) {
            bool outputGenerated = false;
            if ((textureSet.InternalMultiPath.ToLower().Contains("catchlight") && _generateNormals)) {
                if (!skipTexExport) {
                    Task.Run(() => ExportTex(textureSet.Diffuse, diffuseDiskPath, ExportType.Normal, textureSet.Diffuse));
                }
                outputGenerated = true;
            } else {
                string underlay = textureSet.BackupTexturePaths != null ? (RaceInfo.ReverseRaceLookup(textureSet.InternalDiffusePath) == 6 ?
                     textureSet.BackupTexturePaths.DiffuseRaen : textureSet.BackupTexturePaths.Diffuse) : "";
                if (string.IsNullOrEmpty(textureSet.Glow) || textureSet.InternalMultiPath.ToLower().Contains("catchlight")) {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.Diffuse, diffuseDiskPath, ExportType.None, "", "", underlay));
                    }
                    outputGenerated = true;
                } else {
                    if (!skipTexExport) {
                        Task.Run(() => ExportTex(textureSet.Diffuse, diffuseDiskPath, ExportType.Glow, "", textureSet.Glow, underlay));
                    }
                    outputGenerated = true;
                }
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
                            if (group.Description.Contains("-generated")) {
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
            if (path != null) {
                if (group.Options.Count > 32) {
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
            Multi,
            MergeNormal,
            Glow,
            GlowEyeMulti,
            XNormalImport,
            DontManipulate,
            GlowMulti,
            MultiTbse,
            MultiFace,
            MultiFaceAsym
        }
        public async Task<bool> ExportTex(string inputFile, string outputFile, ExportType exportType = ExportType.None,
            string diffuseNormal = "", string modifierMap = "", string layeringImage = "", string normalCorrection = "", bool modifier = false) {
            byte[] data = new byte[0];
            int contrast = 500;
            int contrastFace = 100;
            bool skipPngTexConversion = false;
            using (MemoryStream stream = new MemoryStream()) {
                switch (exportType) {
                    case ExportType.None:
                        ExportTypeNone(inputFile, layeringImage, stream);
                        break;
                    case ExportType.DontManipulate:
                        data = TexLoader.GetTexBytes(inputFile);
                        skipPngTexConversion = true;
                        break;
                    case ExportType.Glow:
                        ExportTypeGlow(inputFile, modifierMap, layeringImage, stream);
                        break;
                    case ExportType.GlowEyeMulti:
                        ExportTypeGlowEyeMulti(inputFile, modifierMap, stream);
                        break;
                    case ExportType.GlowMulti:
                        ExportTypeGlowMulti(inputFile, modifierMap, stream);
                        break;
                    case ExportType.Normal:
                        ExportTypeNormal(inputFile, outputFile, modifierMap, normalCorrection, modifier, stream);
                        break;
                    case ExportType.Multi:
                    case ExportType.MultiFace:
                    case ExportType.MultiFaceAsym:
                    case ExportType.MultiTbse:
                        ExportTypeMulti(inputFile, layeringImage, exportType, modifierMap, stream);
                        break;
                    case ExportType.MergeNormal:
                        ExportTypeMergeNormal(inputFile, outputFile, layeringImage, diffuseNormal, modifierMap, normalCorrection, stream);
                        break;
                    case ExportType.XNormalImport:
                        ExportTypeXNormalImport(inputFile, diffuseNormal, stream);
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
                while (File.Exists(outputFile) && TexLoader.IsFileLocked(outputFile)) {
                    Thread.Sleep(500);
                }
                File.WriteAllBytes(outputFile, data);
                if (OnProgressChange != null) {
                    OnProgressChange.Invoke(this, EventArgs.Empty);
                }
            }
            return true;
        }

        private void ExportTypeXNormalImport(string inputFile, string diffuseNormal, Stream stream) {
            using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                if (bitmap != null) {
                    Bitmap underlay = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(underlay);
                    g.Clear(Color.FromArgb(255, 160, 113, 94));
                    if (!string.IsNullOrEmpty(diffuseNormal)) {
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(TexLoader.ResolveBitmap(diffuseNormal), 0, 0, bitmap.Width, bitmap.Height);
                    }
                    AtramentumLuminisGlow.TransplantData(underlay, bitmap).Save(stream, ImageFormat.Png);
                }
            }
        }

        private void ExportTypeMergeNormal(string inputFile, string outputFile, string layeringImage,
            string diffuseNormal, string modifierMap, string normalCorrection, Stream stream) {
            Bitmap output = null;
            if (!string.IsNullOrEmpty(diffuseNormal)) {
                lock (_normalCache) {
                    if (!_normalCache.ContainsKey(diffuseNormal)) {
                        using (Bitmap diffuse = TexLoader.ResolveBitmap(diffuseNormal)) {
                            if (diffuse != null) {
                                using (Bitmap canvasImage = new Bitmap(diffuse.Size.Width,
                                    diffuse.Size.Height, PixelFormat.Format32bppArgb)) {
                                    output = null;
                                    if (File.Exists(modifierMap)) {
                                        using (Bitmap normalMaskBitmap = TexLoader.ResolveBitmap(modifierMap)) {
                                            if (outputFile.Contains("fac_b_n")) {
                                                Bitmap resize = new Bitmap(diffuse, new Size(1024, 1024));
                                                output = ImageManipulation.MergeNormals(inputFile, resize,
                                                    canvasImage, normalMaskBitmap, diffuseNormal);
                                            } else {
                                                output = ImageManipulation.MergeNormals(inputFile, diffuse,
                                                    canvasImage, normalMaskBitmap, diffuseNormal);
                                            }
                                        }
                                    } else {
                                        using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                                            if (bitmap != null) {
                                                if (!string.IsNullOrEmpty(layeringImage)) {
                                                    Bitmap image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                                    Bitmap layer = TexLoader.ResolveBitmap(Path.Combine(_basePath,
                                                        layeringImage));
                                                    Graphics g = Graphics.FromImage(image);
                                                    g.Clear(Color.Transparent);
                                                    g.CompositingQuality = CompositingQuality.HighQuality;
                                                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                                    g.SmoothingMode = SmoothingMode.HighQuality;
                                                    g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                                    g.DrawImage(GetMergedBitmap(inputFile), 0, 0, bitmap.Width, bitmap.Height);
                                                    output = ImageManipulation.MergeNormals(image, diffuse, canvasImage, null, diffuseNormal);
                                                } else {
                                                    output = ImageManipulation.MergeNormals(inputFile, diffuse, canvasImage, null, diffuseNormal);
                                                }
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(normalCorrection)) {
                                        output = ImageManipulation.ResizeAndMerge(output, TexLoader.ResolveBitmap(normalCorrection));
                                    }
                                    output.Save(stream, ImageFormat.Png);
                                    _normalCache.Add(diffuseNormal, output);
                                }
                            }
                        }
                    } else {
                        _normalCache[diffuseNormal].Save(stream, ImageFormat.Png);
                    }
                }
            }
        }

        private void ExportTypeMulti(string inputFile, string layeringImage, ExportType exportType, string modifierMap, Stream stream) {
            lock (_multiCache) {
                if (_multiCache.ContainsKey(inputFile)) {
                    _multiCache[inputFile].Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            Bitmap image;
                            if (layeringImage != null) {
                                image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                Bitmap layer = TexLoader.ResolveBitmap(Path.Combine(_basePath,
                                    layeringImage));
                                Graphics g = Graphics.FromImage(image);
                                g.Clear(Color.Transparent);
                                g.CompositingQuality = CompositingQuality.HighQuality;
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                g.DrawImage(GetMergedBitmap(inputFile), 0, 0, bitmap.Width, bitmap.Height);
                            } else {
                                image = bitmap;
                            }
                            Bitmap generatedMulti = exportType != ExportType.MultiTbse
                            ? (exportType == ExportType.MultiFace || exportType == ExportType.MultiFaceAsym) ?
                            ImageManipulation.GenerateFaceMulti(image, exportType == ExportType.MultiFaceAsym) :
                            ImageManipulation.GenerateSkinMulti(image)
                            : MultiplyFilter.MultiplyImage(Brightness.BrightenImage(Grayscale.MakeGrayscale(image)), 255,
                            (byte)0, 0);
                            Bitmap multi = !string.IsNullOrEmpty(modifierMap)
                                ? AtramentumLuminisGlow.CalculateMulti(generatedMulti, TexLoader.ResolveBitmap(modifierMap))
                                : generatedMulti;
                            multi.Save(stream, ImageFormat.Png);
                            _multiCache.Add(inputFile, multi);
                        }
                    }
                }
            }
        }

        private void ExportTypeNormal(string inputFile, string outputFile, string modifierMap, string normalCorrection, bool modifier, Stream stream) {
            Bitmap output;
            lock (_normalCache) {
                if (_normalCache.ContainsKey(inputFile)) {
                    output = _normalCache[inputFile];
                } else {
                    using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                        using (Bitmap target = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, PixelFormat.Format32bppArgb)) {
                            Graphics g = Graphics.FromImage(target);
                            g.Clear(Color.Transparent);
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                            if (File.Exists(modifierMap)) {
                                using (Bitmap normalMaskBitmap = TexLoader.ResolveBitmap(modifierMap)) {
                                    output = Normal.Calculate(modifier ? ImageManipulation.InvertImage(target)
                                        : target, normalMaskBitmap);
                                }
                            } else {
                                output = Normal.Calculate(modifier ? ImageManipulation.InvertImage(target) : target);
                            }
                            if (outputFile.Contains("fac_b_n")) {
                                output = new Bitmap(output, new Size(1024, 1024));
                            }
                            _normalCache.Add(inputFile, output);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(normalCorrection)) {
                output = ImageManipulation.ResizeAndMerge(output, TexLoader.ResolveBitmap(normalCorrection));
            }
            output.Save(stream, ImageFormat.Png);
        }

        private void ExportTypeGlowMulti(string inputFile, string mask, Stream stream) {
            string descriminator = inputFile + mask + "glowMulti";
            Bitmap glowOutput;
            lock (_glowCache) {
                if (_glowCache.ContainsKey(descriminator)) {
                    glowOutput = _glowCache[descriminator];
                    glowOutput.Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            Bitmap glowBitmap = AtramentumLuminisGlow.CalculateMulti(bitmap, TexLoader.ResolveBitmap(mask));
                            glowBitmap.Save(stream, ImageFormat.Png);
                            _glowCache.Add(descriminator, glowBitmap);
                        }
                    }
                }
            }
        }

        private void ExportTypeGlowEyeMulti(string inputFile, string mask, Stream stream) {
            string descriminator = inputFile + mask + "glowEyeMulti";
            Bitmap glowOutput;
            lock (_glowCache) {
                if (_glowCache.ContainsKey(descriminator)) {
                    glowOutput = _glowCache[descriminator];
                    glowOutput.Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            Bitmap glowBitmap = AtramentumLuminisGlow.CalculateEyeMulti(bitmap, TexLoader.ResolveBitmap(mask));
                            glowBitmap.Save(stream, ImageFormat.Png);
                            _glowCache.Add(descriminator, glowBitmap);
                        }
                    }
                }
            }
        }

        private void ExportTypeGlow(string inputFile, string glowMap, string layeringImage, Stream stream) {
            Bitmap glowOutput = null;
            string descriminator = inputFile + glowMap + "glowEyeMulti";
            lock (_glowCache) {
                if (_glowCache.ContainsKey(descriminator)) {
                    glowOutput = _glowCache[descriminator];
                    glowOutput.Save(stream, ImageFormat.Png);
                } else {
                    using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                        if (bitmap != null) {
                            if (!string.IsNullOrEmpty(layeringImage)) {
                                Bitmap image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                Bitmap layer = TexLoader.ResolveBitmap(Path.Combine(_basePath,
                                    layeringImage));
                                Graphics g = Graphics.FromImage(image);
                                g.Clear(Color.Transparent);
                                g.CompositingQuality = CompositingQuality.HighQuality;
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                g.DrawImage(GetMergedBitmap(inputFile), 0, 0, bitmap.Width, bitmap.Height);
                                Bitmap glowBitmap = AtramentumLuminisGlow.CalculateDiffuse(image,
                                    ImageManipulation.Resize(GetMergedBitmap(glowMap), bitmap.Width, bitmap.Height));
                                glowBitmap.Save(stream, ImageFormat.Png);
                                _glowCache.Add(descriminator, glowBitmap);
                            } else {
                                Bitmap glowBitmap = AtramentumLuminisGlow.CalculateDiffuse(bitmap, TexLoader.ResolveBitmap(glowMap));
                                glowBitmap.Save(stream, ImageFormat.Png);
                                _glowCache.Add(descriminator, glowBitmap);
                            }
                        }
                    }
                }
            }
        }

        private void ExportTypeNone(string inputFile, string layeringImage, Stream stream) {
            using (Bitmap bitmap = TexLoader.ResolveBitmap(inputFile)) {
                if (bitmap != null) {
                    if (!string.IsNullOrEmpty(layeringImage)) {
                        Bitmap image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                        Bitmap layer = TexLoader.ResolveBitmap(Path.Combine(_basePath,
                            layeringImage));
                        Graphics g = Graphics.FromImage(image);
                        g.Clear(Color.Transparent);
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                        g.DrawImage(GetMergedBitmap(inputFile), 0, 0, bitmap.Width, bitmap.Height);
                        image.Save(stream, ImageFormat.Png);
                    } else {
                        bitmap.Save(stream, ImageFormat.Png);
                    }
                }
            }
        }

        public string AppendIdentifier(string value) {
            return ImageManipulation.AddSuffix(value, "_generated");
        }
    }
}
