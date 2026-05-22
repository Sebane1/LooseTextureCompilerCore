using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LooseTextureCompilerCore {
    public class GlobalPathStorage {
        /// <summary>
        /// Use this to track the original launch directory of the application via "Environment.CurrentDirectory" or a relevant path you expect main files to be. 
        /// Necessary due to how single file published applications work.
        /// AppDomain.CurrentDomain.BaseDirectory and GlobalPathStorage.OriginalBaseDirectory 
        /// wont give the expected paths when using single file publishing.
        /// </summary>
        public static string OriginalBaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Validates that critical application assets exist on disk.
        /// Returns a list of human-readable warnings for any missing assets.
        /// </summary>
        public static List<string> ValidateAssets() {
            List<string> missing = new List<string>();
            string baseDir = OriginalBaseDirectory;

            // Check that the res directory itself exists
            string resDir = Path.Combine(baseDir, "res");
            if (!Directory.Exists(resDir)) {
                missing.Add($"The entire 'res' resource folder is missing from '{baseDir}'. " +
                    "Please reinstall or re-extract the application.");
                return missing;
            }

            // Critical resource directories
            string[] requiredDirs = new string[] {
                Path.Combine("res", "textures", "eyes"),
                Path.Combine("res", "textures", "face"),
                Path.Combine("res", "model"),
                Path.Combine("res", "fastuvtransfer"),
            };

            foreach (string dir in requiredDirs) {
                string fullPath = Path.Combine(baseDir, dir);
                if (!Directory.Exists(fullPath)) {
                    missing.Add($"Resource directory missing: {dir}");
                }
            }

            // Critical template files used by eye conversion, face processing, etc.
            string[] requiredFiles = new string[] {
                Path.Combine("res", "textures", "eyes", "mask.png"),
                Path.Combine("res", "textures", "eyes", "diffuse.png"),
                Path.Combine("res", "textures", "eyes", "normaldt.png"),
                Path.Combine("res", "textures", "eyes", "template.png"),
                Path.Combine("res", "textures", "eyes", "normal.png"),
                Path.Combine("res", "textures", "eyes", "catchlight.png"),
            };

            foreach (string file in requiredFiles) {
                string fullPath = Path.Combine(baseDir, file);
                if (!File.Exists(fullPath)) {
                    missing.Add($"Template file missing: {file}");
                }
            }

            return missing;
        }
    }
}
