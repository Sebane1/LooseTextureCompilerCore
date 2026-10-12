using System.Collections.Generic;

namespace FFXIVVoicePackCreator.Json {
    public class ModMeta {
        public int FileVersion { get; set; } = 4;
        public string Name { get; set; } = "";
        public string Author { get; set; } = "Loose Texture Compiler";
        public string Description { get; set; } = "Exported by FFXIV Loose Texture Compiler";
        public string Version { get; set; } = "0.0.0";
        public string Website { get; set; } = "https://github.com/Sebane1/FFXIVLooseTextureCompiler";
        public List<string> ModTags { get; set; } = new List<string>();
        
        public ModDataContainer DefaultData { get; set; } = new ModDataContainer();
        public List<Group> Groups { get; set; } = new List<Group>();
    }
}
