using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using FFXIVLooseTextureCompiler.ImageProcessing;
using FFXIVLooseTextureCompiler.PathOrganization;
using FFXIVLooseTextureCompiler.Racial;
using FFXIVVoicePackCreator.Json;
using LooseTextureCompilerCore;
using Newtonsoft.Json;
using Penumbra.GameData.Files;
using Penumbra.LTCImport.Dds;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using static FFXIVLooseTextureCompiler.TextureProcessor;
using Color = System.Drawing.Color;
using Group = FFXIVVoicePackCreator.Json.Group;
using Path = System.IO.Path;
using Size = System.Drawing.Size;
using FFXIVLooseTextureCompiler.Export;

namespace FFXIVLooseTextureCompiler
{
    public class TextureProcessor
    {
        public bool ExportBc7 { get; set; } = false;
        private ConcurrentDictionary<string, TextureSet> _redirectionCache;
        private ConcurrentDictionary<string, TextureSet> _mtrlCache;
        private ConcurrentDictionary<string, Bitmap> _normalCache;
        private ConcurrentDictionary<string, Bitmap> _maskCache;
        private ConcurrentDictionary<string, Bitmap> _glowCache;
        private ConcurrentDictionary<string, string> _xnormalCache;
        private XNormal _xnormal;
        private List<KeyValuePair<string, string>> _textureSetQueue;
        private int _fileCount;
        private int _gcCounter;
        private System.Threading.SemaphoreSlim _exportSemaphore = new System.Threading.SemaphoreSlim(Environment.ProcessorCount);
        private int _activeExportThreads = 0;

        private bool _finalizeResults;
        private bool _generateNormals;
        private bool _generateMulti;
        public bool UseFastUVTransfer { get; set; } = true;

        string _basePath = "";
        int _exportCompletion = 0;
        private int _exportMax;
        private DifferenceHash _hashAlgorithm;

        public int ExportMax { get => _exportMax; }
        public int ExportCompletion { get => _exportCompletion; }
        public string BasePath { get => _basePath; set => _basePath = value; }

        public TextureProcessor(string basePath = null)
        {
            _basePath = !string.IsNullOrEmpty(basePath) ? basePath : GlobalPathStorage.OriginalBaseDirectory;
            OnProgressChange += delegate
            {
                _exportCompletion++;
            };
        }

        public event EventHandler OnProgressChange;
        public event EventHandler OnStartedProcessing;
        public event EventHandler OnLaunchedXnormal;
        public event EventHandler<string> OnProgressReport;
        public event EventHandler<string> OnError;

        private void AddToBitmapCache(ConcurrentDictionary<string, Bitmap> cache, string key, Bitmap bitmap)
        {
            if (cache.Count >= 250)
            {
                string firstKey = cache.Keys.First();
                if (cache.TryRemove(firstKey, out Bitmap old) && old != null) old.Dispose();
            }
            cache.TryAdd(key, bitmap);
        }

        private Bitmap GetMergedBitmap(string file)
        {
            if (file.Contains("gen3"))
            {
                object test = new object();
            }
            if (file.Contains("baseTexBaked") && (file.Contains("_d_") ||
                file.Contains("_g_") || file.Contains("_n_") || file.Contains("_m_")))
            {
                string path1 = file.Replace("baseTexBaked", "alpha_baseTexBaked");
                string path2 = file.Replace("baseTexBaked", "rgb_baseTexBaked");
                if (File.Exists(path1) && File.Exists(path2))
                {
                    Bitmap alpha = TexIO.ResolveBitmap(path1);
                    Bitmap rgb = TexIO.ResolveBitmap(path2);
                    Bitmap merged = ImageManipulation.MergeAlphaToRGB(alpha, rgb);
                    TexIO.SaveBitmap(merged, file);
                    try
                    {
                        Task.Run(() =>
                        {
                            Thread.Sleep(5000);
                            File.Delete(path1);
                            File.Delete(path2);
                        });
                    }
                    catch
                    {

                    }
                    alpha.Dispose();
                    rgb.Dispose();
                    return merged;
                }
            }
            return TexIO.ResolveBitmap(file);
        }
        public static ulong CreateHashLocal(string path)
        {
            var hashAlgorithm = new DifferenceHash();
            ulong hash = 0;
            using (var image = TexIO.ResolveBitmap(path))
            {
                if (image != null)
                {
                    using (var resized = TexIO.Resize(image, 100, 100))
                    {
                        using (var imageSharped = TexIO.BitmapToImageSharp(resized))
                        {
                            hash = hashAlgorithm.Hash(imageSharped);
                        }
                    }
                }
            }
            return hash;
        }
        public ulong CreateHash(string path)
        {
            if (_hashAlgorithm == null)
            {
                _hashAlgorithm = new DifferenceHash();
            }
            ulong hash = 0;
            OnProgressReport?.Invoke(this, "Preparing " + Path.GetFileNameWithoutExtension(path));
            using (var image = TexIO.ResolveBitmap(path))
            {
                if (image != null)
                {
                    OnProgressReport?.Invoke(this, "Scaling " + Path.GetFileNameWithoutExtension(path));
                    using (var resized = TexIO.Resize(image, 100, 100))
                    {
                        OnProgressReport?.Invoke(this, "Translating " + Path.GetFileNameWithoutExtension(path));
                        using (var imageSharped = TexIO.BitmapToImageSharp(resized))
                        {
                            OnProgressReport?.Invoke(this, "Hashing " + Path.GetFileNameWithoutExtension(path));
                            hash = _hashAlgorithm.Hash(imageSharped);
                            OnProgressReport?.Invoke(this, "Hash Calculated");
                        }
                    }
                }
            }
            return hash;
        }
        public void BatchTextureSet(TextureSet parent, TextureSet child)
        {
            OnProgressReport?.Invoke(this, "UV Transfer Batching " + parent.TextureSetName);
            if (!string.IsNullOrEmpty(child.FinalBase))
            {
                // Create a hash algorithm
                var hash = CreateHash(parent.FinalBase);

                if (!File.Exists(child.FinalBase) || !parent.Hashes.ContainsKey(child.FinalBase) || hash != parent.Hashes[child.FinalBase])
                {
                    OnProgressReport?.Invoke(this, "Queue For UV Transfer");
                    AddToXnormalPool(parent, child, XNormalTextureType.Base);
                    if (_finalizeResults)
                    {
                        parent.Hashes[child.FinalBase] = hash;
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.FinalNormal))
            {
                // Create a hash algorithm
                var hash = CreateHash(parent.FinalNormal);

                if (!File.Exists(child.FinalNormal) || !parent.Hashes.ContainsKey(child.FinalNormal) || hash != parent.Hashes[child.FinalNormal])
                {
                    OnProgressReport?.Invoke(this, "Queue For UV Transfer");
                    AddToXnormalPool(parent, child, XNormalTextureType.Normal);
                    if (_finalizeResults)
                    {
                        parent.Hashes[child.FinalNormal] = hash;
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.FinalMask))
            {
                // Create a hash algorithm
                var hash = CreateHash(parent.FinalMask);

                if (!File.Exists(child.FinalMask) || !parent.Hashes.ContainsKey(child.FinalMask) || hash != parent.Hashes[child.FinalMask])
                {
                    OnProgressReport?.Invoke(this, "Queue For UV Transfer");
                    AddToXnormalPool(parent, child, XNormalTextureType.Mask);
                    if (_finalizeResults)
                    {
                        parent.Hashes[child.FinalMask] = hash;
                    }
                }
            }
            if (!string.IsNullOrEmpty(child.FinalGlow))
            {
                // Create a hash algorithm
                var hash = CreateHash(parent.FinalGlow);

                if (!File.Exists(child.FinalGlow) || !parent.Hashes.ContainsKey(child.FinalGlow) || hash != parent.Hashes[child.FinalGlow])
                {
                    OnProgressReport?.Invoke(this, "Queue For UV Transfer");
                    AddToXnormalPool(parent, child, XNormalTextureType.Glow);
                    if (_finalizeResults)
                    {
                        parent.Hashes[child.FinalGlow] = hash;
                    }
                }
            }
        }
        public enum XNormalTextureType
        {
            Base, Normal, Mask, Glow
        }
        public void AddToXnormalPool(TextureSet parent, TextureSet child, XNormalTextureType xNormalTextureType)
        {
            string parentTexturePath = "";
            string childTexturePath = "";
            string internalPath = "";
            switch (xNormalTextureType)
            {
                case XNormalTextureType.Base:
                    parentTexturePath = parent.FinalBase;
                    childTexturePath = child.FinalBase;
                    internalPath = parent.InternalBasePath;
                    break;
                case XNormalTextureType.Normal:
                    parentTexturePath = parent.FinalNormal;
                    childTexturePath = child.FinalNormal;
                    internalPath = parent.InternalNormalPath;
                    break;
                case XNormalTextureType.Mask:
                    parentTexturePath = parent.FinalMask;
                    childTexturePath = child.FinalMask;
                    internalPath = parent.InternalMaskPath;
                    break;
                case XNormalTextureType.Glow:
                    parentTexturePath = parent.FinalGlow;
                    childTexturePath = child.FinalGlow;
                    internalPath = parent.InternalNormalPath;
                    break;
            }

            if (!_xnormalCache.ContainsKey(childTexturePath))
            {
                string baseTextureAlpha = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(parentTexturePath, "_alpha"), ".png");
                string baseTextureRGB = ImageManipulation.ReplaceExtension(
                ImageManipulation.AddSuffix(parentTexturePath, "_rgb"), ".png");
                if (_finalizeResults || !File.Exists(childTexturePath.Replace("baseTexBaked", "rgb_baseTexBaked"))
                    || !File.Exists(childTexturePath.Replace("baseTexBaked", "alpha_baseTexBaked")))
                {
                    if (childTexturePath.Contains("baseTexBaked"))
                    {
                        _xnormalCache.TryAdd(childTexturePath, childTexturePath);
                        Bitmap baseTexture = TexIO.ResolveBitmap(parentTexturePath);
                        if (Directory.Exists(Path.GetDirectoryName(baseTextureAlpha))
                            && Directory.Exists(Path.GetDirectoryName(baseTextureRGB)))
                        {
                            string childAlpha = childTexturePath.Replace("baseTexBaked", "alpha");
                            string childRGB = childTexturePath.Replace("baseTexBaked", "rgb");

                            bool useLegacy = true;
                            if (_finalizeResults && UseFastUVTransfer)
                            {
                                if (FastUVTransfer.GenerateBasedOnSourceBody(internalPath, parentTexturePath, childTexturePath))
                                {
                                    useLegacy = false;
                                }
                            }

                            if (useLegacy)
                            {
                                // Legacy XNormal path: requires RGB and Alpha to be split for precision baking.
                                TexIO.SaveBitmap(ImageManipulation.ExtractTransparency(baseTexture), baseTextureAlpha);
                                TexIO.SaveBitmap(ImageManipulation.ExtractRGB(baseTexture), baseTextureRGB);
                                if (_finalizeResults)
                                {
                                    _xnormal.AddToBatch(internalPath, baseTextureAlpha, childAlpha, false);
                                    _xnormal.AddToBatch(internalPath, baseTextureRGB, childRGB, xNormalTextureType == XNormalTextureType.Normal);
                                }
                                else
                                {
                                    if (!File.Exists(ImageManipulation.AddSuffix(childTexturePath, "_baseTexBaked")))
                                    {
                                        if (!File.Exists(childAlpha))
                                        {
                                            new Bitmap(1024, 1024).Save(ImageManipulation.AddSuffix(childAlpha, "_baseTexBaked"), ImageFormat.Png);
                                        }
                                        if (!File.Exists(childRGB))
                                        {
                                            new Bitmap(1024, 1024).Save(ImageManipulation.AddSuffix(childRGB, "_baseTexBaked"), ImageFormat.Png);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //MessageBox.Show("Something has gone terribly wrong. " + parent.Base + "is missing");
                        }
                    }
                }
            }
        }
        public void Export(List<TextureSet> textureSetList, Dictionary<string, int> groupOptionTypes,
            string modPath, int generationType, bool generateNormals,
            bool generateMulti, bool useXNormal, string xNormalPathOverride = "")
        {
            Dictionary<string, List<TextureSet>> groups = new Dictionary<string, List<TextureSet>>();
            try
            {
                int i = 0;
                _fileCount = 0;
                _finalizeResults = useXNormal;
                if (_normalCache != null) { foreach (var item in _normalCache.Values) item?.Dispose(); _normalCache.Clear(); }
                if (_maskCache != null) { foreach (var item in _maskCache.Values) item?.Dispose(); _maskCache.Clear(); }
                if (_glowCache != null) { foreach (var item in _glowCache.Values) item?.Dispose(); _glowCache.Clear(); }
                _mtrlCache?.Clear();

                foreach (TextureSet textureSet in textureSetList)
                {
                    textureSet.CancelCleanup();
                }
                _xnormalCache?.Clear();
                _redirectionCache?.Clear();
                _normalCache = new ConcurrentDictionary<string, Bitmap>();
                _maskCache = new ConcurrentDictionary<string, Bitmap>();
                _glowCache = new ConcurrentDictionary<string, Bitmap>();
                _xnormalCache = new ConcurrentDictionary<string, string>();
                _redirectionCache = new ConcurrentDictionary<string, TextureSet>();
                _mtrlCache = new ConcurrentDictionary<string, TextureSet>();
                _xnormal = new XNormal();
                _xnormal.XNormalPathOverride = xNormalPathOverride;
                _xnormal.BasePathOverride = _basePath;
                _generateNormals = generateNormals;
                _generateMulti = generateMulti;
                _exportCompletion = 0;
                _exportMax = 0;
                _exportMax = (textureSetList.Count * 4) + textureSetList.Count;
                Dictionary<string, string> alreadyCalculatedBases = new Dictionary<string, string>();
                Dictionary<string, string> alreadyCalculatedNormals = new Dictionary<string, string>();
                Dictionary<string, string> alreadyCalculatedMasks = new Dictionary<string, string>();
                Dictionary<string, string> alreadyCalculatedGlows = new Dictionary<string, string>();
                OnProgressReport?.Invoke(this, "Preparing Data");
                foreach (TextureSet textureSet in textureSetList)
                {
                    OnProgressReport?.Invoke(this, "Merging Layers " + textureSet.TextureSetName);
                    string targetUV = ImageManipulation.IdentifyTargetUV(textureSet.InternalBasePath);

                    if (!alreadyCalculatedBases.ContainsKey(textureSet.FinalBase) &&
                        (!string.IsNullOrEmpty(textureSet.Base) || textureSet.BaseOverlays.Count > 0))
                    {
                        List<string> images = new List<string>();
                        List<string> uvs = new List<string>();
                        images.Add(textureSet.Base);
                        if (string.IsNullOrEmpty(textureSet.BaseUV))
                        {
                            if (ImageManipulation.HasTextIdentifiers(textureSet.Base))
                            {
                                uvs.Add(ImageManipulation.IdentifyUV(textureSet.Base));
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        else if (textureSet.BaseUV.ToLower() == "none")
                        {
                            uvs.Add("");
                        }
                        else if (textureSet.BaseUV.ToLower() == "auto")
                        {
                            uvs.Add(ImageManipulation.IdentifyUV(textureSet.Base));
                        }
                        else
                        {
                            uvs.Add(textureSet.BaseUV);
                        }
                        for (int j = 0; j < textureSet.BaseOverlays.Count; j++)
                        {
                            images.Add(textureSet.BaseOverlays[j]);
                            if (j < textureSet.BaseOverlayUVs.Count && !string.IsNullOrEmpty(textureSet.BaseOverlayUVs[j]))
                            {
                                if (textureSet.BaseOverlayUVs[j].ToLower() == "auto")
                                {
                                    uvs.Add(ImageManipulation.IdentifyUV(textureSet.BaseOverlays[j]));
                                }
                                else if (textureSet.BaseOverlayUVs[j].ToLower() == "none")
                                {
                                    uvs.Add("");
                                }
                                else
                                {
                                    uvs.Add(textureSet.BaseOverlayUVs[j]);
                                }
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        ImageManipulation.MergeImageLayers(images, uvs, targetUV, textureSet.FinalBase);
                        alreadyCalculatedBases[textureSet.FinalBase] = "";
                    }

                    if (!alreadyCalculatedNormals.ContainsKey(textureSet.FinalNormal) &&
                        (!string.IsNullOrEmpty(textureSet.Normal) || textureSet.NormalOverlays.Count > 0))
                    {
                        List<string> images = new List<string>();
                        List<string> uvs = new List<string>();
                        images.Add(textureSet.Normal);
                        if (string.IsNullOrEmpty(textureSet.NormalUV))
                        {
                            if (ImageManipulation.HasTextIdentifiers(textureSet.Normal))
                            {
                                uvs.Add(ImageManipulation.IdentifyUV(textureSet.Normal));
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        else if (textureSet.NormalUV.ToLower() == "none")
                        {
                            uvs.Add("");
                        }
                        else if (textureSet.NormalUV.ToLower() == "auto")
                        {
                            uvs.Add(ImageManipulation.IdentifyUV(textureSet.Normal));
                        }
                        else
                        {
                            uvs.Add(textureSet.NormalUV);
                        }
                        for (int j = 0; j < textureSet.NormalOverlays.Count; j++)
                        {
                            images.Add(textureSet.NormalOverlays[j]);
                            if (j < textureSet.NormalOverlayUVs.Count && !string.IsNullOrEmpty(textureSet.NormalOverlayUVs[j]))
                            {
                                if (textureSet.NormalOverlayUVs[j].ToLower() == "auto")
                                {
                                    uvs.Add(ImageManipulation.IdentifyUV(textureSet.NormalOverlays[j]));
                                }
                                else if (textureSet.NormalOverlayUVs[j].ToLower() == "none")
                                {
                                    uvs.Add("");
                                }
                                else
                                {
                                    uvs.Add(textureSet.NormalOverlayUVs[j]);
                                }
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        ImageManipulation.MergeImageLayers(images, uvs, targetUV, textureSet.FinalNormal);
                        alreadyCalculatedNormals[textureSet.FinalNormal] = "";
                    }

                    if (!alreadyCalculatedMasks.ContainsKey(textureSet.FinalMask) &&
                        (!string.IsNullOrEmpty(textureSet.Mask) || textureSet.MaskOverlays.Count > 0))
                    {
                        List<string> images = new List<string>();
                        List<string> uvs = new List<string>();
                        images.Add(textureSet.Mask);
                        if (string.IsNullOrEmpty(textureSet.MaskUV))
                        {
                            if (ImageManipulation.HasTextIdentifiers(textureSet.Mask))
                            {
                                uvs.Add(ImageManipulation.IdentifyUV(textureSet.Mask));
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        else if (textureSet.MaskUV.ToLower() == "none")
                        {
                            uvs.Add("");
                        }
                        else if (textureSet.MaskUV.ToLower() == "auto")
                        {
                            uvs.Add(ImageManipulation.IdentifyUV(textureSet.Mask));
                        }
                        else
                        {
                            uvs.Add(textureSet.MaskUV);
                        }
                        for (int j = 0; j < textureSet.MaskOverlays.Count; j++)
                        {
                            images.Add(textureSet.MaskOverlays[j]);
                            if (j < textureSet.MaskOverlayUVs.Count && !string.IsNullOrEmpty(textureSet.MaskOverlayUVs[j]))
                            {
                                if (textureSet.MaskOverlayUVs[j].ToLower() == "auto")
                                {
                                    uvs.Add(ImageManipulation.IdentifyUV(textureSet.MaskOverlays[j]));
                                }
                                else if (textureSet.MaskOverlayUVs[j].ToLower() == "none")
                                {
                                    uvs.Add("");
                                }
                                else
                                {
                                    uvs.Add(textureSet.MaskOverlayUVs[j]);
                                }
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        ImageManipulation.MergeImageLayers(images, uvs, targetUV, textureSet.FinalMask);
                        alreadyCalculatedMasks[textureSet.FinalMask] = "";
                    }

                    if (!alreadyCalculatedGlows.ContainsKey(textureSet.FinalGlow) &&
                        (!string.IsNullOrEmpty(textureSet.Glow) || textureSet.GlowOverlays.Count > 0))
                    {
                        List<string> images = new List<string>();
                        List<string> uvs = new List<string>();
                        images.Add(textureSet.Glow);
                        if (string.IsNullOrEmpty(textureSet.GlowUV))
                        {
                            if (ImageManipulation.HasTextIdentifiers(textureSet.Glow))
                            {
                                uvs.Add(ImageManipulation.IdentifyUV(textureSet.Glow));
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        else if (textureSet.GlowUV.ToLower() == "none")
                        {
                            uvs.Add("");
                        }
                        else if (textureSet.GlowUV.ToLower() == "auto")
                        {
                            uvs.Add(ImageManipulation.IdentifyUV(textureSet.Glow));
                        }
                        else
                        {
                            uvs.Add(textureSet.GlowUV);
                        }
                        for (int j = 0; j < textureSet.GlowOverlays.Count; j++)
                        {
                            images.Add(textureSet.GlowOverlays[j]);
                            if (j < textureSet.GlowOverlayUVs.Count && !string.IsNullOrEmpty(textureSet.GlowOverlayUVs[j]))
                            {
                                if (textureSet.GlowOverlayUVs[j].ToLower() == "auto")
                                {
                                    uvs.Add(ImageManipulation.IdentifyUV(textureSet.GlowOverlays[j]));
                                }
                                else if (textureSet.GlowOverlayUVs[j].ToLower() == "none")
                                {
                                    uvs.Add("");
                                }
                                else
                                {
                                    uvs.Add(textureSet.GlowOverlayUVs[j]);
                                }
                            }
                            else
                            {
                                uvs.Add("");
                            }
                        }
                        ImageManipulation.MergeImageLayers(images, uvs, targetUV, textureSet.FinalGlow);
                        alreadyCalculatedGlows[textureSet.FinalGlow] = "";
                    }

                    if (!groups.ContainsKey(textureSet.GroupName))
                    {
                        groups.Add(textureSet.GroupName, new List<TextureSet>() { textureSet });
                        foreach (TextureSet childSet in textureSet.ChildSets)
                        {
                            childSet.GroupName = textureSet.GroupName;
                            groups[textureSet.GroupName].Add(childSet);
                            BatchTextureSet(textureSet, childSet);
                            _exportMax += 4;
                        }
                    }
                    else
                    {
                        groups[textureSet.GroupName].Add(textureSet);
                        foreach (TextureSet childSet in textureSet.ChildSets)
                        {
                            childSet.GroupName = textureSet.GroupName;
                            groups[textureSet.GroupName].Add(childSet);
                            BatchTextureSet(textureSet, childSet);
                            _exportMax += 4;
                        }
                    }
                    OnProgressChange.Invoke(this, EventArgs.Empty);
                }
                if (_finalizeResults)
                {
                    if (OnLaunchedXnormal != null)
                    {
                        OnLaunchedXnormal.Invoke(this, EventArgs.Empty);
                    }
                    try
                    {
                        FastUVTransfer.ProcessBatches();
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(this, "FastUVTransfer failed: " + ex.Message);
                    }
                    try
                    {
                        _xnormal.ProcessBatches();
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(this, "XNormal failed: " + ex.Message);
                    }
                }
                if (OnStartedProcessing != null)
                {
                    OnStartedProcessing.Invoke(this, EventArgs.Empty);
                }
                OnProgressReport?.Invoke(this, "Export To Penumbra");
                foreach (List<TextureSet> textureSets in groups.Values)
                {
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
                    foreach (TextureSet textureSet in textureSets)
                    {
                        string textureSetHash = GetHashFromTextureSet(textureSet);
                        string baseTextureDiskPath = "";
                        string normalDiskPath = "";
                        string maskDiskPath = "";
                        string materialDiskPath = "";
                        bool skipTexExport = false;
                        if (_redirectionCache.ContainsKey(textureSetHash))
                        {
                            baseTextureDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalBasePath, modPath, textureSetHash);
                            normalDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalNormalPath, modPath, textureSetHash);
                            maskDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalMaskPath, modPath, textureSetHash);
                            materialDiskPath = GetDiskPath(_redirectionCache[textureSetHash].InternalMaterialPath,
                                modPath,
                             (_redirectionCache[textureSetHash].InternalMaterialPath + textureSetHash + textureSet.InternalBasePath.GetHashCode().ToString() + textureSet.InternalNormalPath.GetHashCode().ToString() + textureSet.InternalMaskPath.GetHashCode().ToString()).GetHashCode().ToString());
                            skipTexExport = true;
                        }
                        else
                        {
                            baseTextureDiskPath = GetDiskPath(textureSet.InternalBasePath, modPath, textureSetHash);
                            normalDiskPath = GetDiskPath(textureSet.InternalNormalPath, modPath, textureSetHash);
                            maskDiskPath = GetDiskPath(textureSet.InternalMaskPath, modPath, textureSetHash);
                            materialDiskPath = GetDiskPath(textureSet.InternalMaterialPath,
                                modPath, (textureSet.InternalMaterialPath + textureSetHash + textureSet.InternalBasePath.GetHashCode().ToString() + textureSet.InternalNormalPath.GetHashCode().ToString() + textureSet.InternalMaskPath.GetHashCode().ToString()).GetHashCode().ToString());
                            _redirectionCache.TryAdd(textureSetHash, textureSet);
                        }
                        switch (choiceOption)
                        {
                            case 0:
                                if (!string.IsNullOrEmpty(textureSet.FinalBase) && !string.IsNullOrEmpty(textureSet.InternalBasePath))
                                {
                                    if (BaseLogic(textureSet, baseTextureDiskPath, skipTexExport))
                                    {
                                        AddDetailedGroupOption(textureSet.InternalBasePath,
                                            baseTextureDiskPath.Replace(modPath + "\\", null), "Base", "", textureSet,
                                            textureSets, group, baseTextureOption, out baseTextureOption);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalNormalPath))
                                {
                                    if (NormalLogic(textureSet, normalDiskPath, skipTexExport))
                                    {
                                        AddDetailedGroupOption(textureSet.InternalNormalPath,
                                            normalDiskPath.Replace(modPath + "\\", null), "Normal", "", textureSet,
                                            textureSets, group, normalOption, out normalOption);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalMaskPath))
                                {
                                    if (MaskLogic(textureSet, maskDiskPath, skipTexExport))
                                    {
                                        AddDetailedGroupOption(textureSet.InternalMaskPath,
                                            maskDiskPath.Replace(modPath + "\\", null), "Mask", "", textureSet,
                                            textureSets, group, maskOption, out maskOption);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if ((!string.IsNullOrEmpty(textureSet.Material) || !string.IsNullOrEmpty(textureSet.Glow))
                                    && !string.IsNullOrEmpty(textureSet.InternalMaterialPath))
                                {
                                    if (MaterialLogic(textureSet, materialDiskPath, false))
                                    {
                                        AddDetailedGroupOption(textureSet.InternalMaterialPath,
                                            materialDiskPath.Replace(modPath + "\\", null), "Material", "", textureSet,
                                            textureSets, group, materialOption, out materialOption);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                break;
                            case 1:
                            case 2:
                            case 3:
                                if ((!textureSet.IsChildSet && choiceOption != 3) || (choiceOption == 3 && !alreadySetOption))
                                {
                                    if (!string.IsNullOrEmpty(textureSet.FinalBase) ||
                                        !string.IsNullOrEmpty(textureSet.FinalNormal) ||
                                        !string.IsNullOrEmpty(textureSet.FinalMask) ||
                                        !string.IsNullOrEmpty(textureSet.Glow) ||
                                        !string.IsNullOrEmpty(textureSet.Material))
                                    {
                                        option = new Option(textureSet.TextureSetName == textureSet.GroupName || choiceOption == 3 ? "Enable"
                                        : textureSet.TextureSetName + (textureSet.ChildSets.Count > 0 ? " (Universal)" : ""), 0);
                                        group.Options.Add(option);
                                        alreadySetOption = true;
                                    }
                                }
                                if (!string.IsNullOrEmpty(textureSet.FinalBase) && !string.IsNullOrEmpty(textureSet.InternalBasePath))
                                {
                                    if (BaseLogic(textureSet, baseTextureDiskPath, skipTexExport))
                                    {
                                        option.Files[textureSet.InternalBasePath] =
                                           baseTextureDiskPath.Replace(modPath + "\\", null);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalNormalPath))
                                {
                                    if (NormalLogic(textureSet, normalDiskPath, skipTexExport))
                                    {
                                        option.Files[textureSet.InternalNormalPath] =
                                            normalDiskPath.Replace(modPath + "\\", null);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if (!string.IsNullOrEmpty(textureSet.InternalMaskPath))
                                {
                                    if (MaskLogic(textureSet, maskDiskPath, skipTexExport))
                                    {
                                        option.Files[textureSet.InternalMaskPath] =
                                           maskDiskPath.Replace(modPath + "\\", null);
                                    }
                                    else
                                    {
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                if ((!string.IsNullOrEmpty(textureSet.Material) || !string.IsNullOrEmpty(textureSet.Glow))
                                    && !string.IsNullOrEmpty(textureSet.InternalMaterialPath))
                                {
                                    Trace.WriteLine($"[Glow Debug] Export: calling MaterialLogic. Material='{textureSet.Material}', Glow='{textureSet.Glow}', InternalMtrlPath='{textureSet.InternalMaterialPath}', materialDiskPath='{materialDiskPath}', BasePath='{BasePath}'");
                                    if (MaterialLogic(textureSet, materialDiskPath, false))
                                    {
                                        option.Files[textureSet.InternalMaterialPath] =
                                           materialDiskPath.Replace(modPath + "\\", null);
                                        Trace.WriteLine($"[Glow Debug] Export: MaterialLogic succeeded, added to option files: '{textureSet.InternalMaterialPath}' -> '{materialDiskPath.Replace(modPath + "\\", null)}'");
                                    }
                                    else
                                    {
                                        Trace.WriteLine("[Glow Debug] Export: MaterialLogic returned false");
                                        OnProgressChange.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                else
                                {
                                    Trace.WriteLine($"[Glow Debug] Export: skipping MaterialLogic. Material='{textureSet.Material}', Glow='{textureSet.Glow}', InternalMtrlPath='{textureSet.InternalMaterialPath}'");
                                    OnProgressChange.Invoke(this, EventArgs.Empty);
                                }
                                break;
                        }
                    }
                    if (group.Options.Count > 0)
                    {
                        string groupPath = Path.Combine(modPath, $"group_" + (1 + i++).ToString()
                        .PadLeft(3, '0') + $"_{group.Name.ToLower().Replace(" ", "_")}.json");
                        ExportGroup(groupPath, group);
                    }
                }
                while (_exportCompletion < _exportMax)
                {
                    Thread.Sleep(500);
                }
                foreach (TextureSet textureSet in textureSetList)
                {
                    textureSet.CleanTempFiles();
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e.ToString());
            }
        }

        private string GetDiskPath(string internalPath, string modPath, string id)
        {
            return !string.IsNullOrEmpty(internalPath) ?
            Path.Combine(modPath, AppendIdentifier(ImageManipulation.AddSuffix(
            RedirectToDisk(internalPath), "_" + id))) : "";
        }

        private string GetHashFromTextureSet(TextureSet textureSet)
        {
            string backupHash = "";
            if (textureSet.BackupTexturePaths != null)
            {
                if (!textureSet.BackupTexturePaths.IsFace)
                {
                    backupHash = (RaceInfo.ReverseRaceLookup(textureSet.InternalBasePath) == 6 ?
                    textureSet.BackupTexturePaths.BaseSecondary : textureSet.BackupTexturePaths.Base).GetHashCode().ToString();
                }
                else
                {
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

        public string RedirectToDisk(string path)
        {
            return @"do_not_edit\textures\" + Path.GetFileName(path.Replace("/", @"\"));
        }
        public void AddDetailedGroupOption(string path, string diskPath, string name, string alternateName,
            TextureSet textureSet, List<TextureSet> textureSets, Group group, Option inputOption, out Option outputOption)
        {
            if (!textureSet.IsChildSet)
            {
                outputOption = new Option((textureSets.Count > 1 ? textureSet.TextureSetName + " " : "")
                + name + (textureSet.ChildSets.Count > 0 ? " (Universal)" : ""), 0);
                group.Options.Add(outputOption);
            }
            else
            {
                outputOption = inputOption;
            }
            if (!outputOption.Files.ContainsKey(path))
            {
                outputOption.Files.Add(path, diskPath);
            }
            else
            {
                outputOption.Files[path] = diskPath;
            }
        }
        private bool MaskLogic(TextureSet textureSet, string maskDiskPath, bool skipTexExport)
        {
            bool outputGenerated = false;
            if (!string.IsNullOrEmpty(textureSet.FinalMask) && !string.IsNullOrEmpty(textureSet.InternalMaskPath))
            {
                if (!string.IsNullOrEmpty(textureSet.FinalBase) && !textureSet.InternalMaskPath.Contains("/eye/")
                    && (textureSet.InternalMaskPath.Contains("obj/face") || textureSet.InternalMaskPath.Contains("obj/body")))
                {
                    if (!skipTexExport)
                    {
                        Task.Run(() => ExportTex(textureSet.FinalMask, maskDiskPath, ExportType.DTMask, "", textureSet.FinalBase));
                    }
                }
                else if (textureSet.InternalMaskPath.Contains("etc_") || textureSet.InternalMaskPath.Contains("hair"))
                {
                    if (!skipTexExport)
                    {
                        Task.Run(() => ExportTex(textureSet.FinalMask, maskDiskPath, ExportType.DontManipulate));
                    }
                }
                else
                {
                    if (!skipTexExport)
                    {
                        Task.Run(() => ExportTex(textureSet.FinalMask, maskDiskPath, ExportType.None));
                    }
                }
                outputGenerated = true;
            }
            else if (!string.IsNullOrEmpty(textureSet.FinalBase) && !string.IsNullOrEmpty(textureSet.InternalMaskPath)
                      && _generateMulti && !(textureSet.InternalMaskPath.ToLower().Contains("iri")))
            {
                if (!textureSet.IgnoreMaskGeneration)
                {
                    if (!skipTexExport)
                    {
                        Task.Run(() => ExportTex(textureSet.FinalBase, maskDiskPath, ExportType.Mask, "",
                        textureSet.FinalBase, textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Base : ""));
                    }
                    outputGenerated = true;
                }
            }
            if (skipTexExport && outputGenerated)
            {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }
        private bool MaterialLogic(TextureSet textureSet, string materialDiskPath, bool skipMaterialExport)
        {
            bool outputGenerated = false;
            if ((!string.IsNullOrEmpty(textureSet.Material)
                && !string.IsNullOrEmpty(textureSet.InternalMaterialPath))
                || !string.IsNullOrEmpty(textureSet.FinalGlow))
            {
                if (!skipMaterialExport)
                {
                    if (!_mtrlCache.ContainsKey(materialDiskPath))
                    {
                        _mtrlCache[materialDiskPath] = textureSet;
                        Task.Run(() =>
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(materialDiskPath));
                                string value = !string.IsNullOrEmpty(textureSet.Material) ?
                                textureSet.Material :
                                Path.Combine((!string.IsNullOrEmpty(BasePath) ? BasePath :
                                GlobalPathStorage.OriginalBaseDirectory),
                                textureSet.InternalBasePath.Contains("eye") ?
                                @"res\materials\eye_glow.mtrl"
                                : @"res\materials\skin_glow.mtrl");

                                Trace.WriteLine($"[Glow Debug] MaterialLogic: donor mtrl path = '{value}', exists = {File.Exists(value)}");
                                if (!File.Exists(value))
                                {
                                    OnError?.Invoke(this, $"[Glow] Donor material file not found: {value}");
                                    OnProgressChange?.Invoke(this, EventArgs.Empty);
                                    return;
                                }

                                // Read donor .mtrl file
                                var data = File.ReadAllBytes(value);
                                MtrlFile mtrlFile = new MtrlFile(data);
                                int index = 0;

                                // Set texture paths on material.
                                if (!string.IsNullOrEmpty(textureSet.InternalBasePath))
                                {
                                    mtrlFile.Textures[index++].Path = textureSet.InternalBasePath;
                                }
                                mtrlFile.Textures[index++].Path = textureSet.InternalNormalPath;
                                mtrlFile.Textures[index++].Path = textureSet.InternalMaskPath;

                                if (!string.IsNullOrEmpty(textureSet.FinalGlow))
                                {
                                    // Get emmisive values
                                    MtrlFile.Constant constant = new MtrlFile.Constant();
                                    foreach (var item in mtrlFile.ShaderPackage.Constants)
                                    {
                                        if (item.Id == 0x38A64362)
                                        {
                                            Color colour = ImageManipulation.CalculateMajorityColour(GetMergedBitmap(textureSet.FinalGlow));
                                            constant = item;
                                            var constantValue = mtrlFile.GetConstantValue<float>(constant);

                                            // Set emmisive colour RGB
                                            constantValue[0] = (float)colour.R / 255f;
                                            constantValue[1] = (float)colour.G / 255f;
                                            constantValue[2] = (float)colour.B / 255f;
                                            Trace.WriteLine($"[Glow Debug] MaterialLogic: emissive colour set to R={colour.R}, G={colour.G}, B={colour.B}");
                                            break;
                                        }
                                    }
                                }
                                Stopwatch timeoutTimer = new Stopwatch();
                                timeoutTimer.Start();
                                while (TexIO.IsFileLocked(materialDiskPath) && timeoutTimer.ElapsedMilliseconds < 30000)
                                {
                                    Thread.Sleep(1000);
                                }
                                File.WriteAllBytes(materialDiskPath, mtrlFile.Write());
                                Trace.WriteLine($"[Glow Debug] MaterialLogic: wrote mtrl to '{materialDiskPath}'");
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine($"[Glow Debug] MaterialLogic EXCEPTION: {e}");
                                OnError?.Invoke(this, e.ToString());
                            }
                            OnProgressChange?.Invoke(this, EventArgs.Empty);
                        });
                    }
                    else
                    {
                        OnProgressChange?.Invoke(this, EventArgs.Empty);
                    }
                    outputGenerated = true;
                }
            }
            if (skipMaterialExport && outputGenerated)
            {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        private bool NormalLogic(TextureSet textureSet, string normalDiskPath, bool skipTexExport)
        {
            bool outputGenerated = false;
            if (!string.IsNullOrEmpty(textureSet.FinalNormal) && !string.IsNullOrEmpty(textureSet.InternalNormalPath))
            {
                if (_generateNormals && !textureSet.IgnoreNormalGeneration && !string.IsNullOrEmpty(textureSet.FinalBase))
                {
                    if (!skipTexExport)
                    {
                        Task.Run(() => ExportTex(textureSet.FinalNormal, normalDiskPath, ExportType.MergeNormal,
                        textureSet.FinalBase, textureSet.NormalMask,
                        textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : "", textureSet.NormalCorrection, !textureSet.InternalBasePath.Contains("eye") ? textureSet.FinalGlow : ""));
                    }
                    outputGenerated = true;
                }
                else
                {
                    if (!skipTexExport)
                    {
                        Task.Run(() => ExportTex(textureSet.FinalNormal, normalDiskPath, ExportType.None, "", "",
                    textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : "", "", !textureSet.InternalBasePath.Contains("eye") ? textureSet.FinalGlow : "",
                    false, textureSet.InvertNormalAlpha || !string.IsNullOrEmpty(textureSet.FinalGlow), !string.IsNullOrEmpty(textureSet.FinalGlow)));
                    }
                    outputGenerated = true;
                }
            }
            else if ((!string.IsNullOrEmpty(textureSet.FinalBase) || !string.IsNullOrEmpty(textureSet.FinalGlow))
                  && !string.IsNullOrEmpty(textureSet.InternalNormalPath) && _generateNormals)
            {
                if (!textureSet.IgnoreNormalGeneration)
                {
                    if (textureSet.BackupTexturePaths != null)
                    {
                        if (!skipTexExport)
                        {
                            string normalPath = Path.IsPathRooted(textureSet.BackupTexturePaths.Normal) ? textureSet.BackupTexturePaths.Normal : Path.Combine(_basePath, textureSet.BackupTexturePaths.Normal);
                            Task.Run(() => ExportTex(normalPath,
                            normalDiskPath, ExportType.MergeNormal, textureSet.FinalBase, textureSet.NormalMask,
                            (textureSet.BackupTexturePaths != null ? textureSet.BackupTexturePaths.Normal : ""),
                            textureSet.NormalCorrection, !textureSet.InternalBasePath.Contains("eye") ? textureSet.FinalGlow : "", textureSet.InvertNormalGeneration));
                        }
                        outputGenerated = true;
                    }
                    else
                    {
                        if (!textureSet.InternalBasePath.Contains("eye"))
                        {
                            if (!skipTexExport)
                            {
                                Task.Run(() => ExportTex(textureSet.FinalBase, normalDiskPath,
                                ExportType.Normal, "", textureSet.NormalMask, textureSet.BackupTexturePaths != null ?
                                textureSet.BackupTexturePaths.Base : "",
                                textureSet.NormalCorrection, textureSet.FinalGlow, textureSet.InvertNormalGeneration));
                            }
                        }
                        outputGenerated = true;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(textureSet.FinalGlow)
                  && !string.IsNullOrEmpty(textureSet.InternalNormalPath))
            {
                if (!textureSet.InternalBasePath.Contains("eye"))
                {
                    if (!skipTexExport)
                    {
                        string glowNormalInput = textureSet.BackupTexturePaths != null ?
                        Path.IsPathRooted(textureSet.BackupTexturePaths.Normal) ? textureSet.BackupTexturePaths.Normal : Path.Combine(_basePath, textureSet.BackupTexturePaths.Normal) : "";
                        Task.Run(() => ExportTex(glowNormalInput, normalDiskPath,
                        ExportType.SkipLayering, "", textureSet.NormalMask, "",
                        textureSet.NormalCorrection, textureSet.FinalGlow, textureSet.InvertNormalGeneration, textureSet.InternalBasePath.Contains("fac_")));
                    }
                }
                outputGenerated = true;
            }
            if (skipTexExport && outputGenerated)
            {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        private bool BaseLogic(TextureSet textureSet, string baseTextureDiskPath, bool skipTexExport)
        {
            bool outputGenerated = false;
            if (textureSet == null) return false;
            string underlay = "";
            if (textureSet.BackupTexturePaths != null)
            {
                if (!textureSet.BackupTexturePaths.IsFace)
                {
                    underlay = (RaceInfo.ReverseRaceLookup(textureSet.InternalBasePath) == 6 ?
                         textureSet.BackupTexturePaths.BaseSecondary : textureSet.BackupTexturePaths.Base);
                }
                else
                {
                    underlay = textureSet.BackupTexturePaths.Base;
                }
            }
            if (!string.IsNullOrEmpty(textureSet.FinalBase))
            {
                if (!skipTexExport)
                {
                    Task.Run(() => ExportTex(textureSet.FinalBase, baseTextureDiskPath, ExportType.None, "", "", underlay));
                }
                outputGenerated = true;
            }
            if (skipTexExport && outputGenerated)
            {
                OnProgressChange?.Invoke(this, EventArgs.Empty);
            }
            return outputGenerated;
        }

        public void CleanGeneratedAssets(string path)
        {
            foreach (string file in Directory.EnumerateFiles(path))
            {
                if (file.Contains("_generated"))
                {
                    File.Delete(file);
                }
                if (file.EndsWith(".json"))
                {
                    bool isGenerated = false;
                    using (StreamReader jsonFile = File.OpenText(file))
                    {
                        try
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            Group group = (Group)serializer.Deserialize(jsonFile, typeof(Group));
                            if (!string.IsNullOrEmpty(group.Description) && group.Description.Contains("-generated"))
                            {
                                isGenerated = true;
                            }
                        }
                        catch
                        {
                            // Todo: should we report when we skip a .json we cant read?
                        }
                    }
                    if (isGenerated)
                    {
                        File.Delete(file);
                    }
                }
            }
            foreach (string directory in Directory.EnumerateDirectories(path))
            {
                CleanGeneratedAssets(directory);
            }
        }

        private void ExportGroup(string path, Group group)
        {
            group.Description += " -generated";
            bool isSingle = group.Type == "Single";
            if (path != null)
            {
                if (group.Options.Count > (isSingle ? int.MaxValue : 32))
                {
                    int groupsToSplitTo = group.Options.Count / 32;
                    for (int i = 0; i < groupsToSplitTo; i++)
                    {
                        int rangeStartingPoint = 32 * i;
                        int maxRange = group.Options.Count - rangeStartingPoint;
                        Group newGroup = new Group(group.Name + $" ({i + 1})", group.Description + " -generated",
                                        group.Priority, group.Type, group.DefaultSettings);
                        newGroup.Options = group.Options.GetRange(rangeStartingPoint, maxRange > 32 ? 32 : maxRange);
                        using (StreamWriter file = File.CreateText(path.Replace(".", $" ({i}).")))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Formatting = Formatting.Indented;
                            serializer.Serialize(file, newGroup);
                        }
                    }
                }
                else if (group.Options.Count > 0)
                {
                    using (StreamWriter file = File.CreateText(path))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Formatting.Indented;
                        serializer.Serialize(file, group);
                    }
                }
            }
        }

        public enum ExportType
        {
            None,
            Normal,
            Mask,
            MergeNormal,
            Glow,
            GlowEyeMask,
            XNormalImport,
            DontManipulate,
            DTMask,
            SkipLayering,
        }
        public async Task<bool> ExportTex(string inputFile, string outputFile, ExportType exportType = ExportType.None,
            string baseTextureNormal = "", string modifierMap = "", string layeringImage = "",
            string normalCorrection = "", string alphaOverride = "", bool modifier = false, bool invertAlpha = false, bool dontInvertAlphaOverride = false)
        {
            while (MemoryHelper.GetMaxSafeThreadsBasedOnRAM() <= _activeExportThreads)
            {
                await Task.Delay(250);
            }
            Interlocked.Increment(ref _activeExportThreads);
            await _exportSemaphore.WaitAsync();
            try
            {
                byte[] data = new byte[0];
                try
                {
                    {
                        string fileName = Path.GetFileName(outputFile).ToLower();
                        bool isNormalMap = exportType == ExportType.Normal
                            || exportType == ExportType.MergeNormal
                            || exportType == ExportType.XNormalImport
                            || fileName.Contains("_n_") || fileName.EndsWith("_n.tex")
                            || fileName.Contains("_norm_") || fileName.EndsWith("_norm.tex");

                        bool isMaskMap = exportType == ExportType.Mask
                            || exportType == ExportType.DTMask
                            || fileName.Contains("_m_") || fileName.EndsWith("_m.tex")
                            || fileName.Contains("_mask_") || fileName.EndsWith("_mask.tex");

                        bool actualExportBc7 = ExportBc7 && !isNormalMap;
                        bool actualUseGpu = !(isNormalMap);

                        switch (exportType)
                        {
                            case ExportType.None:
                                using (Bitmap resultBitmap = ExportTypeNone(inputFile, layeringImage, alphaOverride, invertAlpha, dontInvertAlphaOverride))
                                {
                                    if (resultBitmap != null)
                                    {
                                        PenumbraTextureImporter.BitmapToTex(resultBitmap, out data, actualExportBc7, actualUseGpu);
                                    }
                                }
                                break;
                            case ExportType.SkipLayering:
                                using (Bitmap resultBitmap = ExportTypeSkipLayering(inputFile, layeringImage, alphaOverride, invertAlpha, dontInvertAlphaOverride))
                                {
                                    if (resultBitmap != null)
                                    {
                                        PenumbraTextureImporter.BitmapToTex(resultBitmap, out data, actualExportBc7, actualUseGpu);
                                    }
                                }
                                break;
                            case ExportType.DontManipulate:
                                data = TexIO.GetTexBytes(inputFile);
                                break;
                            case ExportType.Glow:
                                using (Bitmap glowResult = ExportTypeGlowAsBitmap(inputFile, modifierMap, layeringImage))
                                {
                                    if (glowResult != null) PenumbraTextureImporter.BitmapToTex(glowResult, out data, actualExportBc7, actualUseGpu);
                                }
                                break;
                            case ExportType.GlowEyeMask:
                                using (Bitmap eyeResult = ExportTypeGlowEyeMaskAsBitmap(inputFile, modifierMap))
                                {
                                    if (eyeResult != null) PenumbraTextureImporter.BitmapToTex(eyeResult, out data, actualExportBc7, actualUseGpu);
                                }
                                break;
                            case ExportType.DTMask:
                                using (Bitmap dtResult = ExportTypeDTMaskAsBitmap(inputFile, modifierMap))
                                {
                                    if (dtResult != null) PenumbraTextureImporter.BitmapToTex(dtResult, out data, actualExportBc7, actualUseGpu);
                                }
                                break;
                            case ExportType.Normal:
                                using (Bitmap normalResult = ExportTypeNormalAsBitmap(inputFile, outputFile, modifierMap, normalCorrection, modifier, alphaOverride, invertAlpha))
                                {
                                    if (normalResult != null) PenumbraTextureImporter.BitmapToTex(normalResult, out data, exportBc7: false, actualUseGpu);
                                }
                                break;
                            case ExportType.Mask:
                                using (Bitmap maskResult = ExportTypeMaskAsBitmap(inputFile, layeringImage, exportType, modifierMap))
                                {
                                    if (maskResult != null) PenumbraTextureImporter.BitmapToTex(maskResult, out data, actualExportBc7, actualUseGpu);
                                }
                                break;
                            case ExportType.MergeNormal:
                                using (Bitmap mergeResult = ExportTypeMergeNormalAsBitmap(inputFile, outputFile, layeringImage, baseTextureNormal, modifierMap,
                                normalCorrection, modifier, alphaOverride, invertAlpha))
                                {
                                    if (mergeResult != null) PenumbraTextureImporter.BitmapToTex(mergeResult, out data, exportBc7: false, actualUseGpu);
                                }
                                break;
                            case ExportType.XNormalImport:
                                using (Bitmap xnResult = ExportTypeXNormalImportAsBitmap(inputFile, baseTextureNormal))
                                {
                                    if (xnResult != null) PenumbraTextureImporter.BitmapToTex(xnResult, out data, exportBc7: false, actualUseGpu);
                                }
                                break;
                        }
                    }
                    if (data.Length > 0)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                        while (TexIO.IsFileLocked(outputFile))
                        {
                            Thread.Sleep(500);
                        }
                        if (File.Exists(outputFile))
                        {
                            File.Delete(outputFile);
                        }
                        File.WriteAllBytes(outputFile, data);
                    }
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, e.ToString());
                }
                if (OnProgressChange != null)
                {
                    OnProgressChange.Invoke(this, EventArgs.Empty);
                }
                return true;
            }
            finally
            {
                _exportSemaphore.Release();
                Interlocked.Decrement(ref _activeExportThreads);
            }
        }

        private Bitmap ExportTypeSkipLayering(string inputFile, string layeringImage, string alphaOverride, bool invertAlpha, bool dontInvertAlphaOverride)
        {
            Bitmap input = null;
            using (Bitmap rgb = GetMergedBitmap(inputFile))
            {
                input = rgb;
                if (File.Exists(alphaOverride))
                {
                    using (Bitmap alpha = GetMergedBitmap(alphaOverride))
                    {
                        if (alpha.Width > rgb.Width || alpha.Height > rgb.Height)
                        {
                            input = ImageManipulation.Resize(rgb, alpha.Width, alpha.Height);
                        }
                        return ImageManipulation.MergeAlphaToRGB(invertAlpha && !dontInvertAlphaOverride ? ImageManipulation.InvertImage(alpha) : alpha, input);
                    }
                }
                return rgb;
            }

            return new Bitmap(4096, 4096);
        }

        // ── Bitmap-returning variants: skip the PNG compress→decompress round-trip ──

        private Bitmap ExportTypeXNormalImportAsBitmap(string inputFile, string baseTextureNormal)
        {
            using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
            {
                if (bitmap != null)
                {
                    using (Bitmap underlay = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(underlay))
                        {
                            g.Clear(Color.FromArgb(255, 160, 113, 94));
                            if (!string.IsNullOrEmpty(baseTextureNormal))
                            {
                                using (Bitmap baseTex = TexIO.ResolveBitmap(baseTextureNormal))
                                {
                                    g.DrawImage(baseTex, 0, 0, bitmap.Width, bitmap.Height);
                                }
                            }
                        }
                        return MapWriting.TransplantData(underlay, bitmap);
                    }
                }
            }
            return null;
        }

        private Bitmap ExportTypeGlowAsBitmap(string inputFile, string glowMap, string layeringImage)
        {
            string descriminator = inputFile + glowMap + "glow";
            lock (_glowCache)
            {
                if (_glowCache.ContainsKey(descriminator))
                {
                    return TexIO.NewBitmap(_glowCache[descriminator]);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            if (!string.IsNullOrEmpty(layeringImage))
                            {
                                using (Bitmap image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
                                {
                                    using (Bitmap layer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                                    {
                                        using (Graphics g = Graphics.FromImage(image))
                                        {
                                            g.Clear(Color.Transparent);
                                            g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                            using (Bitmap merged = GetMergedBitmap(inputFile))
                                            {
                                                g.DrawImage(merged, 0, 0, bitmap.Width, bitmap.Height);
                                            }
                                        }
                                    }
                                    using (Bitmap glowMapBitmap = GetMergedBitmap(glowMap))
                                    {
                                        using (Bitmap resizedGlowMap = ImageManipulation.Resize(glowMapBitmap, bitmap.Width, bitmap.Height))
                                        {
                                            Bitmap glowBitmap = MapWriting.CalculateBase(image, resizedGlowMap);
                                            AddToBitmapCache(_glowCache, descriminator, glowBitmap);
                                            return TexIO.NewBitmap(glowBitmap);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                using (Bitmap glowMapBitmap = TexIO.ResolveBitmap(glowMap))
                                {
                                    Bitmap glowBitmap = MapWriting.CalculateBase(bitmap, glowMapBitmap);
                                    AddToBitmapCache(_glowCache, descriminator, glowBitmap);
                                    return TexIO.NewBitmap(glowBitmap);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private Bitmap ExportTypeGlowEyeMaskAsBitmap(string inputFile, string mask)
        {
            string descriminator = inputFile + mask + "glowEyeMulti";
            lock (_glowCache)
            {
                if (_glowCache.ContainsKey(descriminator))
                {
                    return TexIO.NewBitmap(_glowCache[descriminator]);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            using (Bitmap maskBitmap = TexIO.ResolveBitmap(mask))
                            {
                                Bitmap glowBitmap = MapWriting.CalculateEyeMulti(bitmap, maskBitmap);
                                AddToBitmapCache(_glowCache, descriminator, glowBitmap);
                                return TexIO.NewBitmap(glowBitmap);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private Bitmap ExportTypeDTMaskAsBitmap(string inputFile, string mask)
        {
            string descriminator = inputFile + mask + "glowMulti";
            lock (_glowCache)
            {
                if (_glowCache.ContainsKey(descriminator))
                {
                    return TexIO.NewBitmap(_glowCache[descriminator]);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            using (Bitmap maskBitmap = TexIO.ResolveBitmap(mask))
                            {
                                Bitmap maskChannelMap = MapWriting.CalculateMulti(bitmap, maskBitmap);
                                AddToBitmapCache(_glowCache, descriminator, maskChannelMap);
                                return TexIO.NewBitmap(maskChannelMap);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private Bitmap ExportTypeNormalAsBitmap(string inputFile, string outputFile, string modifierMap,
            string normalCorrection, bool modifier, string alphaOverride, bool invertAlpha)
        {
            Bitmap output;
            lock (_normalCache)
            {
                if (_normalCache.ContainsKey(inputFile))
                {
                    output = _normalCache[inputFile];
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        using (Bitmap target = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g = Graphics.FromImage(target))
                            {
                                g.Clear(Color.Transparent);
                                ImageManipulation.DrawImage(target, bitmap, 0, 0, bitmap.Width, bitmap.Height);
                            }

                            Bitmap toCalculate = modifier ? ImageManipulation.InvertImage(target) : target;
                            if (File.Exists(modifierMap))
                            {
                                using (Bitmap normalMaskBitmap = TexIO.ResolveBitmap(modifierMap))
                                {
                                    output = Normal.Calculate(toCalculate, normalMaskBitmap);
                                }
                            }
                            else
                            {
                                output = Normal.Calculate(toCalculate);
                            }
                            if (modifier) toCalculate.Dispose();

                            if (!string.IsNullOrEmpty(alphaOverride))
                            {
                                Bitmap layered = ImageManipulation.LayerImages(output, output, alphaOverride, invertAlpha);
                                output.Dispose();
                                output = layered;
                            }
                            AddToBitmapCache(_normalCache, inputFile, output);
                        }
                    }
                }
            }
            Bitmap finalOutput = output;
            if (!string.IsNullOrEmpty(normalCorrection))
            {
                using (Bitmap correction = TexIO.ResolveBitmap(normalCorrection))
                {
                    finalOutput = ImageManipulation.ResizeAndMerge(output, correction);
                }
            }
            // Return a copy if it's the cached version, or the finalOutput directly if it was freshly created
            Bitmap result = (finalOutput == output) ? TexIO.NewBitmap(finalOutput) : finalOutput;
            return result;
        }

        private Bitmap ExportTypeMaskAsBitmap(string inputFile, string layeringImage, ExportType exportType, string modifierMap)
        {
            lock (_maskCache)
            {
                if (_maskCache.ContainsKey(inputFile))
                {
                    return TexIO.NewBitmap(_maskCache[inputFile]);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            Bitmap image = null;
                            if (layeringImage != null)
                            {
                                image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                using (Bitmap layer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                                {
                                    using (Graphics g = Graphics.FromImage(image))
                                    {
                                        g.Clear(Color.Transparent);
                                        g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                        using (Bitmap merged = GetMergedBitmap(inputFile))
                                        {
                                            g.DrawImage(merged, 0, 0, bitmap.Width, bitmap.Height);
                                        }
                                    }
                                }
                            }

                            Bitmap toProcess = image ?? bitmap;
                            Bitmap generatedMulti = ImageManipulation.ConvertBaseToDawntrailSkinMulti(toProcess);
                            if (image != null) image.Dispose();

                            Bitmap mask = generatedMulti;
                            if (!string.IsNullOrEmpty(modifierMap))
                            {
                                using (Bitmap modifierTex = TexIO.ResolveBitmap(modifierMap))
                                {
                                    mask = MapWriting.CalculateMulti(generatedMulti, modifierTex);
                                }
                                generatedMulti.Dispose();
                            }
                            AddToBitmapCache(_maskCache, inputFile, mask);
                            return TexIO.NewBitmap(mask);
                        }
                    }
                }
            }
            return null;
        }

        private Bitmap ExportTypeMergeNormalAsBitmap(string inputFile, string outputFile, string layeringImage,
            string baseTextureNormal, string modifierMap, string normalCorrection, bool modifier, string alphaOverride, bool invertAlpha)
        {
            Bitmap output = null;
            if (!string.IsNullOrEmpty(baseTextureNormal))
            {
                lock (_normalCache)
                {
                    if (!_normalCache.ContainsKey(baseTextureNormal))
                    {
                        using (Bitmap baseTexture = TexIO.ResolveBitmap(baseTextureNormal))
                        {
                            if (baseTexture != null)
                            {
                                using (Bitmap canvasImage = new Bitmap(baseTexture.Size.Width, baseTexture.Size.Height, PixelFormat.Format32bppArgb))
                                {
                                    if (File.Exists(modifierMap))
                                    {
                                        using (Bitmap normalMaskBitmap = TexIO.ResolveBitmap(modifierMap))
                                        {
                                            using (Bitmap inputTex = TexIO.ResolveBitmap(inputFile))
                                            {
                                                output = ImageManipulation.MergeNormals(inputTex, baseTexture,
                                                    canvasImage, normalMaskBitmap, baseTextureNormal, modifier);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                                        {
                                            if (bitmap != null)
                                            {
                                                if (!string.IsNullOrEmpty(layeringImage))
                                                {
                                                    using (Bitmap bottomLayer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                                                    {
                                                        using (Bitmap topLayer = GetMergedBitmap(inputFile))
                                                        {
                                                            using (Bitmap layered = ImageManipulation.LayerImages(bottomLayer, topLayer))
                                                            {
                                                                output = ImageManipulation.MergeNormals(layered, baseTexture, canvasImage, null, baseTextureNormal, modifier);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    output = ImageManipulation.MergeNormals(bitmap, baseTexture, canvasImage, null, baseTextureNormal, modifier);
                                                }
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(normalCorrection))
                                    {
                                        using (Bitmap correction = TexIO.ResolveBitmap(normalCorrection))
                                        {
                                            Bitmap newOutput = ImageManipulation.ResizeAndMerge(output, correction);
                                            output.Dispose();
                                            output = newOutput;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(alphaOverride))
                                    {
                                        using (Bitmap alphaOverrideBitmap = TexIO.ResolveBitmap(alphaOverride))
                                        {
                                            using (Bitmap alphaGray = Grayscale.MakeGrayscale(alphaOverrideBitmap))
                                            {
                                                Bitmap finalOutput = output;
                                                Bitmap finalAlpha = alphaGray;
                                                bool outputDisposed = false, alphaDisposed = false;
                                                if (output.Size.Height < alphaGray.Size.Height)
                                                {
                                                    finalOutput = ImageManipulation.Resize(output, alphaGray.Size.Width, alphaGray.Size.Height);
                                                    outputDisposed = true;
                                                }
                                                else
                                                {
                                                    finalAlpha = ImageManipulation.Resize(alphaGray, output.Size.Width, output.Size.Height);
                                                    alphaDisposed = true;
                                                }

                                                using (Bitmap rgb = ImageManipulation.ExtractRGB(finalOutput))
                                                {
                                                    Bitmap newOutput = ImageManipulation.MergeAlphaToRGB(finalAlpha, rgb);
                                                    if (outputDisposed) finalOutput.Dispose();
                                                    if (alphaDisposed) finalAlpha.Dispose();
                                                    output.Dispose();
                                                    output = newOutput;
                                                }
                                            }
                                        }
                                    }
                                    if (output != null)
                                    {
                                        AddToBitmapCache(_normalCache, baseTextureNormal, output);
                                        return TexIO.NewBitmap(output);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        return TexIO.NewBitmap(_normalCache[baseTextureNormal]);
                    }
                }
            }
            else
            {
                return ExportTypeNone(inputFile, layeringImage, alphaOverride, invertAlpha, false);
            }
            return null;
        }

        private void ExportTypeXNormalImport(string inputFile, string baseTextureNormal, Stream stream)
        {
            using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
            {
                if (bitmap != null)
                {
                    using (Bitmap underlay = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(underlay))
                        {
                            g.Clear(Color.FromArgb(255, 160, 113, 94));
                            if (!string.IsNullOrEmpty(baseTextureNormal))
                            {
                                using (Bitmap baseTex = TexIO.ResolveBitmap(baseTextureNormal))
                                {
                                    g.DrawImage(baseTex, 0, 0, bitmap.Width, bitmap.Height);
                                }
                            }
                        }
                        using (Bitmap result = MapWriting.TransplantData(underlay, bitmap))
                        {
                            TexIO.SaveBitmap(result, stream);
                        }
                    }
                }
            }
        }

        private void ExportTypeMergeNormal(string inputFile, string outputFile, string layeringImage,
            string baseTextureNormal, string modifierMap, string normalCorrection, Stream stream, bool modifier, string alphaOverride, bool invertAlpha)
        {
            Bitmap output = null;
            if (!string.IsNullOrEmpty(baseTextureNormal))
            {
                lock (_normalCache)
                {
                    if (!_normalCache.ContainsKey(baseTextureNormal))
                    {
                        using (Bitmap baseTexture = TexIO.ResolveBitmap(baseTextureNormal))
                        {
                            if (baseTexture != null)
                            {
                                using (Bitmap canvasImage = new Bitmap(baseTexture.Size.Width, baseTexture.Size.Height, PixelFormat.Format32bppArgb))
                                {
                                    if (File.Exists(modifierMap))
                                    {
                                        using (Bitmap normalMaskBitmap = TexIO.ResolveBitmap(modifierMap))
                                        {
                                            using (Bitmap inputTex = TexIO.ResolveBitmap(inputFile))
                                            {
                                                output = ImageManipulation.MergeNormals(inputTex, baseTexture,
                                                    canvasImage, normalMaskBitmap, baseTextureNormal, modifier);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                                        {
                                            if (bitmap != null)
                                            {
                                                if (!string.IsNullOrEmpty(layeringImage))
                                                {
                                                    using (Bitmap bottomLayer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                                                    {
                                                        using (Bitmap topLayer = GetMergedBitmap(inputFile))
                                                        {
                                                            using (Bitmap layered = ImageManipulation.LayerImages(bottomLayer, topLayer))
                                                            {
                                                                output = ImageManipulation.MergeNormals(layered, baseTexture, canvasImage, null, baseTextureNormal, modifier);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    output = ImageManipulation.MergeNormals(bitmap, baseTexture, canvasImage, null, baseTextureNormal, modifier);
                                                }
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(normalCorrection))
                                    {
                                        using (Bitmap correction = TexIO.ResolveBitmap(normalCorrection))
                                        {
                                            Bitmap newOutput = ImageManipulation.ResizeAndMerge(output, correction);
                                            output.Dispose();
                                            output = newOutput;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(alphaOverride))
                                    {
                                        using (Bitmap alphaOverrideBitmap = TexIO.ResolveBitmap(alphaOverride))
                                        {
                                            using (Bitmap alphaGray = Grayscale.MakeGrayscale(alphaOverrideBitmap))
                                            {
                                                Bitmap finalOutput = output;
                                                Bitmap finalAlpha = alphaGray;
                                                bool outputDisposed = false, alphaDisposed = false;
                                                if (output.Size.Height < alphaGray.Size.Height)
                                                {
                                                    finalOutput = ImageManipulation.Resize(output, alphaGray.Size.Width, alphaGray.Size.Height);
                                                    outputDisposed = true;
                                                }
                                                else
                                                {
                                                    finalAlpha = ImageManipulation.Resize(alphaGray, output.Size.Width, output.Size.Height);
                                                    alphaDisposed = true;
                                                }

                                                using (Bitmap rgb = ImageManipulation.ExtractRGB(finalOutput))
                                                {
                                                    Bitmap newOutput = ImageManipulation.MergeAlphaToRGB(finalAlpha, rgb);
                                                    if (outputDisposed) finalOutput.Dispose();
                                                    if (alphaDisposed) finalAlpha.Dispose();
                                                    output.Dispose();
                                                    output = newOutput;
                                                }
                                            }
                                        }
                                    }
                                    if (output != null)
                                    {
                                        TexIO.SaveBitmap(output, stream);
                                        AddToBitmapCache(_normalCache, baseTextureNormal, output);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        TexIO.SaveBitmap(_normalCache[baseTextureNormal], stream);
                    }
                }
            }
        }

        private void ExportTypeMask(string inputFile, string layeringImage, ExportType exportType, string modifierMap, Stream stream)
        {
            lock (_maskCache)
            {
                if (_maskCache.ContainsKey(inputFile))
                {
                    TexIO.SaveBitmap(_maskCache[inputFile], stream);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            Bitmap image = null;
                            if (layeringImage != null)
                            {
                                image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                using (Bitmap layer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                                {
                                    using (Graphics g = Graphics.FromImage(image))
                                    {
                                        g.Clear(Color.Transparent);
                                        g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                        using (Bitmap merged = GetMergedBitmap(inputFile))
                                        {
                                            g.DrawImage(merged, 0, 0, bitmap.Width, bitmap.Height);
                                        }
                                    }
                                }
                            }

                            Bitmap toProcess = image ?? bitmap;
                            Bitmap generatedMulti = ImageManipulation.ConvertBaseToDawntrailSkinMulti(toProcess);
                            if (image != null) image.Dispose();

                            Bitmap mask = generatedMulti;
                            if (!string.IsNullOrEmpty(modifierMap))
                            {
                                using (Bitmap modifierTex = TexIO.ResolveBitmap(modifierMap))
                                {
                                    mask = MapWriting.CalculateMulti(generatedMulti, modifierTex);
                                }
                                generatedMulti.Dispose();
                            }
                            TexIO.SaveBitmap(mask, stream);
                            AddToBitmapCache(_maskCache, inputFile, mask);
                        }
                    }
                }
            }
        }

        private void ExportTypeNormal(string inputFile, string outputFile, string modifierMap,
            string normalCorrection, bool modifier, Stream stream, string alphaOverride, bool invertAlpha)
        {
            Bitmap output;
            lock (_normalCache)
            {
                if (_normalCache.ContainsKey(inputFile))
                {
                    output = _normalCache[inputFile];
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        using (Bitmap target = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g = Graphics.FromImage(target))
                            {
                                g.Clear(Color.Transparent);
                                ImageManipulation.DrawImage(target, bitmap, 0, 0, bitmap.Width, bitmap.Height);
                            }

                            Bitmap toCalculate = modifier ? ImageManipulation.InvertImage(target) : target;
                            if (File.Exists(modifierMap))
                            {
                                using (Bitmap normalMaskBitmap = TexIO.ResolveBitmap(modifierMap))
                                {
                                    output = Normal.Calculate(toCalculate, normalMaskBitmap);
                                }
                            }
                            else
                            {
                                output = Normal.Calculate(toCalculate);
                            }
                            if (modifier) toCalculate.Dispose();

                            if (!string.IsNullOrEmpty(alphaOverride))
                            {
                                Bitmap layered = ImageManipulation.LayerImages(output, output, alphaOverride, invertAlpha);
                                output.Dispose();
                                output = layered;
                            }
                            AddToBitmapCache(_normalCache, inputFile, output);
                        }
                    }
                }
            }
            Bitmap finalOutput = output;
            if (!string.IsNullOrEmpty(normalCorrection))
            {
                using (Bitmap correction = TexIO.ResolveBitmap(normalCorrection))
                {
                    finalOutput = ImageManipulation.ResizeAndMerge(output, correction);
                }
            }
            TexIO.SaveBitmap(finalOutput, stream);
            if (finalOutput != output) finalOutput.Dispose();
        }

        private void ExportTypeDTMask(string inputFile, string mask, Stream stream)
        {
            string descriminator = inputFile + mask + "glowMulti";
            Bitmap glowOutput;
            lock (_glowCache)
            {
                if (_glowCache.ContainsKey(descriminator))
                {
                    glowOutput = _glowCache[descriminator];
                    TexIO.SaveBitmap(glowOutput, stream);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            using (Bitmap maskBitmap = TexIO.ResolveBitmap(mask))
                            {
                                Bitmap maskChannelMap = MapWriting.CalculateMulti(bitmap, maskBitmap);
                                TexIO.SaveBitmap(maskChannelMap, stream);
                                AddToBitmapCache(_glowCache, descriminator, maskChannelMap);
                            }
                        }
                    }
                }
            }
        }

        private void ExportTypeGlowEyeMask(string inputFile, string mask, Stream stream)
        {
            string descriminator = inputFile + mask + "glowEyeMulti";
            Bitmap glowOutput;
            lock (_glowCache)
            {
                if (_glowCache.ContainsKey(descriminator))
                {
                    glowOutput = _glowCache[descriminator];
                    TexIO.SaveBitmap(glowOutput, stream);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            using (Bitmap maskBitmap = TexIO.ResolveBitmap(mask))
                            {
                                Bitmap glowBitmap = MapWriting.CalculateEyeMulti(bitmap, maskBitmap);
                                TexIO.SaveBitmap(glowBitmap, stream);
                                AddToBitmapCache(_glowCache, descriminator, glowBitmap);
                            }
                        }
                    }
                }
            }
        }

        private void ExportTypeGlow(string inputFile, string glowMap, string layeringImage, Stream stream)
        {
            Bitmap glowOutput = null;
            string descriminator = inputFile + glowMap + "glow";
            lock (_glowCache)
            {
                if (_glowCache.ContainsKey(descriminator))
                {
                    glowOutput = _glowCache[descriminator];
                    TexIO.SaveBitmap(glowOutput, stream);
                }
                else
                {
                    using (Bitmap bitmap = TexIO.ResolveBitmap(inputFile))
                    {
                        if (bitmap != null)
                        {
                            if (!string.IsNullOrEmpty(layeringImage))
                            {
                                using (Bitmap image = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
                                {
                                    using (Bitmap layer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                                    {
                                        using (Graphics g = Graphics.FromImage(image))
                                        {
                                            g.Clear(Color.Transparent);
                                            g.DrawImage(layer, 0, 0, bitmap.Width, bitmap.Height);
                                            using (Bitmap merged = GetMergedBitmap(inputFile))
                                            {
                                                g.DrawImage(merged, 0, 0, bitmap.Width, bitmap.Height);
                                            }
                                        }
                                    }
                                    using (Bitmap glowMapBitmap = GetMergedBitmap(glowMap))
                                    {
                                        using (Bitmap resizedGlowMap = ImageManipulation.Resize(glowMapBitmap, bitmap.Width, bitmap.Height))
                                        {
                                            Bitmap glowBitmap = MapWriting.CalculateBase(image, resizedGlowMap);
                                            TexIO.SaveBitmap(glowBitmap, stream);
                                            AddToBitmapCache(_glowCache, descriminator, glowBitmap);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                using (Bitmap glowMapBitmap = TexIO.ResolveBitmap(glowMap))
                                {
                                    Bitmap glowBitmap = MapWriting.CalculateBase(bitmap, glowMapBitmap);
                                    TexIO.SaveBitmap(glowBitmap, stream);
                                    AddToBitmapCache(_glowCache, descriminator, glowBitmap);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Bitmap ExportTypeNone(string inputFile, string layeringImage, string alphaOverride = "", bool invertAlpha = false, bool dontInvertAlphaOverrid = false)
        {
            if (!string.IsNullOrEmpty(layeringImage))
            {
                using (Bitmap bottomLayer = TexIO.ResolveBitmap(Path.Combine(_basePath, layeringImage)))
                {
                    using (Bitmap topLayer = GetMergedBitmap(inputFile))
                    {
                        return ImageManipulation.LayerImages(bottomLayer, topLayer, alphaOverride, invertAlpha, dontInvertAlphaOverrid);
                    }
                }
            }
            else
            {
                Bitmap bitmap = GetMergedBitmap(inputFile.StartsWith(@"res\") ? Path.Combine(_basePath, inputFile) : inputFile);
                if (bitmap != null)
                {
                    if (string.IsNullOrEmpty(alphaOverride))
                    {
                        return bitmap;
                    }
                    else
                    {
                        Bitmap result = ImageManipulation.LayerImages(bitmap, bitmap, alphaOverride, invertAlpha, dontInvertAlphaOverrid);
                        bitmap.Dispose();
                        return result;
                    }
                }
            }
            return null;
        }



        public string AppendIdentifier(string value)
        {
            return ImageManipulation.AddSuffix(value, "_generated");
        }
    }
}
