using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LooseTextureCompilerCore.Json
{
    public class OnionModData
    {
        public int FormatVersion { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Website { get; set; }
        public bool DisableEditing { get; set; }
        public bool Locked { get; set; }
        public List<ModLayer> Layers { get; set; } = new();
        public List<object> Groups { get; set; } = new();
        public int TotalLayerCount { get; set; }
        public string DirectoryPath { get; private set; }

        public static OnionModData Load(string directoryPath)
        {
            string file = System.IO.Path.Combine(directoryPath, "meta.json");
            if (System.IO.File.Exists(file))
            {
                var json = System.IO.File.ReadAllText(file);
                var onionLayer = Newtonsoft.Json.JsonConvert.DeserializeObject<OnionModData>(json);
                if (onionLayer != null)
                {
                    onionLayer.DirectoryPath = directoryPath;
                    return onionLayer;
                }
            }
            return new OnionModData { DirectoryPath = directoryPath };
        }
        public void Save()
        {
            if (string.IsNullOrEmpty(DirectoryPath)) return;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(System.IO.Path.Combine(DirectoryPath, "meta.json"), json);
        }
    }

    public class ModLayer
    {
        public string File { get; set; }
        public string Layout { get; set; }
        public string Map { get; set; }
        public string Mode { get; set; }
        public int Order { get; set; }
        public float Opacity { get; set; }
        public List<object> Races { get; set; } = new();
        public string? GeneratedFrom { get; set; }
        public string? SourceHash { get; set; }
        public int ParsedLayout { get; set; }
        public int ParsedMap { get; set; }
    }
}
