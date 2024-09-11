using FFXIVLooseTextureCompiler.Export;

namespace FFXIVLooseTextureCompiler.PathOrganization {
    public class TextureSet {
        private string _textureSetName = "";
        private string _groupName = "";

        private string _baseTexture = "";
        private string _normal = "";
        private string _mask = "";

        private string _internalBasePath = "";
        private string _internalNormalPath = "";
        private string _internalMaskPath = "";
        private string _normalMask = "";
        private string _glow = "";

        private bool _ignoreNormalGeneration;
        private bool _ignoreMaskGeneration;
        private bool _invertNormalGeneration;
        private bool _invertNormalAlpha;
        private bool _omniExportMode;
        private bool _usesScales;

        private int _skinType = 0;
        private BackupTexturePaths _backupTexturePaths;
        public bool IsChildSet {
            get {
                return TextureSetName.Contains("[IsChild]");
            }
        }

        List<TextureSet> _childSets = new List<TextureSet>();
        private string _normalCorrection = "";
        private string _material;
        private string _internalMaterialPath;

        public string TextureSetName { get => _textureSetName; set => _textureSetName = value; }

        #region Obsolete Properties 
        [Obsolete("This property is only here for backwards compatibility, please use TextureSetName instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public string MaterialSetName { set => _textureSetName = value; }

        [Obsolete("This property is only here for backwards compatibility, please use GroupName instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public string MaterialGroupName { set => _groupName = value; }

        [Obsolete("This property is only here for backwards compatibility, please use Mask instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public string Multi { set => Mask = value; }

        [Obsolete("This property is only here for backwards compatibility, please use InternalMaskPath instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public string InternalMultiPath { set => InternalMaskPath = value; }
        [Obsolete("This property is only here for backwards compatibility, please use InternalMaskPath instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public string InternalDiffusePath { set => InternalBasePath = value; }

        [Obsolete("This property is only here for backwards compatibility, please use IgnoreMaskGeneration instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public bool IgnoreMultiGeneration { set => IgnoreMaskGeneration = value; }


        [Obsolete("This property is only here for backwards compatibility, please use Base instead.")]
        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        public string Diffuse { set => _baseTexture = value; }
        #endregion


        public string Base { get { if (_baseTexture == null) { _baseTexture = ""; } return _baseTexture; } set => _baseTexture = value; }
        public string Normal { get { if (_normal == null) { _normal = ""; } return _normal; } set => _normal = value; }
        public string Mask { get { if (_mask == null) { _mask = ""; } return _mask; } set => _mask = value; }
        public string NormalMask { get { if (_normalMask == null) { _normalMask = ""; } return _normalMask; } set => _normalMask = value; }
        public string Material { get { if (_material == null) { _material = ""; } return _material; } set => _material = value; }


        public string Glow {
            get {
                if (_glow == null) {
                    _glow = "";
                }
                return _glow;
            }
            set {
                _glow = value;
            }
        }
        public string InternalBasePath {
            get {
                return _internalBasePath == null ? _internalBasePath = "" : _internalBasePath;
            }
            set => _internalBasePath = value;
        }
        public string InternalNormalPath {
            get {
                return _internalNormalPath == null ? _internalNormalPath = "" : _internalNormalPath;
            }
            set => _internalNormalPath = value;
        }
        public string InternalMaskPath {
            get {
                return _internalMaskPath == null ? _internalMaskPath = "" : _internalMaskPath;
            }
            set => _internalMaskPath = value;
        }
        public string InternalMaterialPath {
            get {
                return _internalMaterialPath == null ? _internalMaterialPath = "" : _internalMaterialPath;
            }
            set => _internalMaterialPath = value;
        }

        public string GroupName {
            get {
                if (string.IsNullOrEmpty(_groupName)) {
                    _groupName = _textureSetName;
                }
                return _groupName;
            }
            set => _groupName = value;
        }

        public bool IgnoreMaskGeneration { get => _ignoreMaskGeneration; set => _ignoreMaskGeneration = value; }
        public bool IgnoreNormalGeneration { get => _ignoreNormalGeneration; set => _ignoreNormalGeneration = value; }
        public bool OmniExportMode { get => _omniExportMode; set => _omniExportMode = value; }
        public List<TextureSet> ChildSets { get => _childSets; set => _childSets = value; }
        public BackupTexturePaths BackupTexturePaths { get => _backupTexturePaths; set => _backupTexturePaths = value; }
        public string NormalCorrection { get => _normalCorrection; set => _normalCorrection = value; }
        public bool InvertNormalGeneration { get => _invertNormalGeneration; set => _invertNormalGeneration = value; }
        public int SkinType { get => _skinType; set => _skinType = value; }
        public bool UsesScales { get => _usesScales; set => _usesScales = value; }
        public bool InvertNormalAlpha { get => _invertNormalAlpha; set => _invertNormalAlpha = value; }

        public override string ToString() {
            return _textureSetName + (GroupName != _textureSetName ? $" | Group({_groupName})" : "");
        }
    }
}
