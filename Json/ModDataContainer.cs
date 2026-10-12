using System.Collections.Generic;

namespace FFXIVVoicePackCreator.Json {
    public class ModDataContainer {
        public Dictionary<string, string> Files { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> FileSwaps { get; set; } = new Dictionary<string, string>();
        public List<Manipulations> Manipulations { get; set; } = new List<Manipulations>();
    }
}
