using FFXIVLooseTextureCompiler.Export;

namespace LooseTextureCompilerCore.Export {
    public class SkinType {
        string _name;
        List<BackupTexturePaths> _backupTextures = new List<BackupTexturePaths>();

        public SkinType(string name, params BackupTexturePaths[] backupTextures) {
            _name = name;
            _backupTextures.AddRange(backupTextures);
        }

        public List<BackupTexturePaths> BackupTextures { get => _backupTextures; set => _backupTextures = value; }
        public string Name { get => _name; set => _name = value; }
    }
}
