using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FFXIVLooseTextureCompiler.Export;

namespace FFXIVLooseTextureCompiler.ImageProcessing
{
    /// <summary>
    /// In-memory virtual filesystem (memory:\\ paths) with byte accounting and LRU eviction.
    /// </summary>
    public sealed class VirtualFileSystemStore : IEnumerable<KeyValuePair<string, TexIO.MemoryFile>>
    {
        public const long MinimumMaxBytes = 2L * 1024 * 1024 * 1024;
        public const long AbsoluteMaxBytes = 8L * 1024 * 1024 * 1024;

        private readonly ConcurrentDictionary<string, TexIO.MemoryFile> _files = new();
        private readonly ConcurrentDictionary<string, long> _lastAccessUtcTicks = new();
        private long _totalBytes;
        private readonly object _evictLock = new();

        private long _maxBytes = GetDefaultMaxBytes();

        public long MaxBytes
        {
            get => Interlocked.Read(ref _maxBytes);
            set => Interlocked.Exchange(ref _maxBytes, Math.Max(value, MinimumMaxBytes));
        }

        public int Count => _files.Count;

        public long TotalBytes => Interlocked.Read(ref _totalBytes);

        public static long EstimateBytes(TexIO.MemoryFile file) =>
            file.Data != null ? file.Data.LongLength : 0;

        private static long GetDefaultMaxBytes()
        {
            ulong availableBytes = MemoryHelper.GetAvailablePhysicalMemoryBytes();
            if (availableBytes == 0)
                return MinimumMaxBytes;

            long halfAvailable = (long)Math.Min(availableBytes / 2, long.MaxValue);
            return Math.Clamp(halfAvailable, MinimumMaxBytes, AbsoluteMaxBytes);
        }

        public bool TryGetValue(string key, out TexIO.MemoryFile value)
        {
            if (_files.TryGetValue(key, out value))
            {
                Touch(key);
                return true;
            }

            value = default;
            return false;
        }

        public bool ContainsKey(string key) => _files.ContainsKey(key);

        public TexIO.MemoryFile this[string key]
        {
            get
            {
                _files.TryGetValue(key, out var value);
                if (value.Data != null)
                    Touch(key);
                return value;
            }
            set => Set(key, value);
        }

        public void Set(string key, TexIO.MemoryFile value)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (_files.TryRemove(key, out var old))
                Interlocked.Add(ref _totalBytes, -EstimateBytes(old));

            _files[key] = value;
            _lastAccessUtcTicks[key] = DateTime.UtcNow.Ticks;
            Interlocked.Add(ref _totalBytes, EstimateBytes(value));
            ComputeSharpLayering.InvalidateCache(key);
            TrimToMaxBytes();
        }

        public void Clear()
        {
            _files.Clear();
            _lastAccessUtcTicks.Clear();
            Interlocked.Exchange(ref _totalBytes, 0);
        }

        /// <summary>Trim cached memory:\\ entries after an export completes.</summary>
        public void TrimAfterExport() => TrimToMaxBytes();

        public void TrimToMaxBytes(long? maxBytes = null)
        {
            long limit = maxBytes ?? MaxBytes;
            if (Interlocked.Read(ref _totalBytes) <= limit)
                return;

            lock (_evictLock)
            {
                if (Interlocked.Read(ref _totalBytes) <= limit)
                    return;

                var victims = _lastAccessUtcTicks
                     .OrderBy(kvp => kvp.Value)
                     .Select(kvp => kvp.Key)
                     .ToList();

                int removed = 0;
                foreach (string key in victims)
                {
                    if (Interlocked.Read(ref _totalBytes) <= limit)
                        break;

                    if (_files.TryRemove(key, out var removedFile))
                    {
                        Interlocked.Add(ref _totalBytes, -EstimateBytes(removedFile));
                        _lastAccessUtcTicks.TryRemove(key, out _);
                        ComputeSharpLayering.InvalidateCache(key);
                        removed++;
                    }
                }

                if (removed > 0)
                {
                    Trace.WriteLine(
                        $"[VFS] Evicted {removed} least-recently-used memory:\\ file(s); " +
                        $"{Interlocked.Read(ref _totalBytes) / (1024 * 1024)} MB retained (limit {limit / (1024 * 1024)} MB).");
                }
            }
        }

        private void Touch(string key)
        {
            if (_files.ContainsKey(key))
                _lastAccessUtcTicks[key] = DateTime.UtcNow.Ticks;
        }

        public IEnumerator<KeyValuePair<string, TexIO.MemoryFile>> GetEnumerator() => _files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
