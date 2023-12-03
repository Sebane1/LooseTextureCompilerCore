using FFXIVLooseTextureCompiler.Export;

namespace FFXIVLooseTextureCompiler.PathOrganization {
    public class TextureSet {
     private   string _textureSetName = "";
        private string _groupName = "";

        private string _diffuse = "";
        private string _normal = "";
        private string _multi = "";

        private string _internalDiffusePath = "";
        private string _internalNormalPath = "";
        private string _internalMultiPath = "";
        private string _normalMask = "";
        private string _glow = "" ;

        private bool _ignoreNormalGeneration;
        private bool _ignoreMultiGeneration;
        private bool _invertNormalGeneration;
        private bool _omniExportMode;

        private int _skinType = 0;
        private BackupTexturePaths _backupTexturePaths;
        public bool IsChildSet {
            get {
                return TextureSetName.Contains("[IsChild]");
            }
        }

        List<TextureSet> _childSets = new List<TextureSet>();
        private string _normalCorrection ="";

        public string TextureSetName { get => _textureSetName; set => _textureSetName = value; }

        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>
        [Obsolete("This property is only here for backwards compatibility, please use TextureSetName instead.")]
        public string MaterialSetName { set => _textureSetName = value; }
        public string Diffuse { get { if (_diffuse == null) { _diffuse = ""; } return _diffuse; } set => _diffuse = value; }
        public string Normal { get { if (_normal == null) { _normal = ""; } return _normal; } set => _normal = value; }
        public string Multi { get { if (_multi == null) { _multi = ""; } return _multi; } set => _multi = value; }
        public string NormalMask { get { if (_normalMask == null) { _normalMask = ""; } return _normalMask; } set => _normalMask = value; }
        public string Glow {
            get {
                if (_glow == null) {
                    _glow = "";
                }
                return _glow;
            }
            set {
                if (!string.IsNullOrEmpty(value)) {
                    IgnoreMultiGeneration = false;
                }
                _glow = value;
            }
        }
        public string InternalDiffusePath {
            get {
                return _internalDiffusePath == null ? _internalDiffusePath = "" : _internalDiffusePath;
            }
            set => _internalDiffusePath = value; }
        public string InternalNormalPath {
            get {
                return _internalNormalPath == null ? _internalNormalPath = "" : _internalNormalPath;
            }
            set => _internalNormalPath = value;
        }
        public string InternalMultiPath
        {
            get
            {
                return _internalMultiPath == null ? _internalMultiPath = "" : _internalMultiPath;
            }
            set => _internalMultiPath = value;
        }


        public string GroupName
        {
            get
            {
                if (string.IsNullOrEmpty(_groupName))
                {
                    _groupName = _textureSetName;
                }
                return _groupName;
            }
            set => _groupName = value;
        }


        /// <summary>
        /// This only exists for backwards compatibility
        /// </summary>

        [Obsolete("This property is only here for backwards compatibility, please use GroupName instead.")]
        public string MaterialGroupName {
            set => _groupName = value;
        }

        public bool IgnoreMultiGeneration { get => _ignoreMultiGeneration; set => _ignoreMultiGeneration = value; }
        public bool IgnoreNormalGeneration { get => _ignoreNormalGeneration; set => _ignoreNormalGeneration = value; }
        public bool OmniExportMode { get => _omniExportMode; set => _omniExportMode = value; }
        public List<TextureSet> ChildSets { get => _childSets; set => _childSets = value; }
        public BackupTexturePaths BackupTexturePaths { get => _backupTexturePaths; set => _backupTexturePaths = value; }
        public string NormalCorrection { get => _normalCorrection; set => _normalCorrection = value; }
        public bool InvertNormalGeneration { get => _invertNormalGeneration; set => _invertNormalGeneration = value; }
        public int SkinType { get => _skinType; set => _skinType = value; }

        public override string ToString() {
            return _textureSetName + (GroupName != _textureSetName ? $" | Group({_groupName})" : "");
        }
    }
}
