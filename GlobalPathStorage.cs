using System;
using System.Collections.Generic;
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
    }
}
