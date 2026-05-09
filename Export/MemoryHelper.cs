using System;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.Export {
    public static class MemoryHelper {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        /// <summary>
        /// Returns the available physical memory in bytes.
        /// </summary>
        public static ulong GetAvailablePhysicalMemoryBytes() {
            var stat = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(stat)) {
                return stat.ullAvailPhys;
            }
            return 0;
        }

        /// <summary>
        /// Calculates the maximum number of safe concurrent threads based on available RAM,
        /// assuming roughly 640MB (0.625 GB) is used per thread.
        /// It reserves at least 1GB of system RAM to prevent out-of-memory crashes.
        /// </summary>
        public static int GetMaxSafeThreadsBasedOnRAM() {
            ulong availableBytes = GetAvailablePhysicalMemoryBytes();
            
            // Reserve 1 GB (1024 * 1024 * 1024 bytes) for the OS and other apps
            ulong reservedBytes = 1073741824UL; 
            
            if (availableBytes <= reservedBytes) {
                return 1; // Always allow at least 1 thread to make progress
            }

            ulong usableBytes = availableBytes - reservedBytes;
            
            // 0.625 GB per thread = 671,088,640 bytes
            ulong bytesPerThread = 671088640UL;
            
            int safeThreads = (int)(usableBytes / bytesPerThread);
            
            // Return at least 1, but cap at processor count to avoid context switching overhead
            return Math.Clamp(safeThreads, 1, Environment.ProcessorCount);
        }
    }
}
