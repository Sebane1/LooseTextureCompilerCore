using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing
{
    /// <summary>
    /// Bakes Proteus overlay sidecars into static textures (color table, index, opacity, masks).
    /// Shader-only features (gear shells, scroll emissive, sphere maps) are intentionally excluded.
    /// </summary>
    public static class ProteusOverlayCompositor
    {
        public struct ColorTableSubRow
        {
            public float DiffuseR;
            public float DiffuseG;
            public float DiffuseB;
            public float Emissive;
            public int Opacity;

            public static ColorTableSubRow White => new()
            {
                DiffuseR = 1f,
                DiffuseG = 1f,
                DiffuseB = 1f,
                Emissive = 0f,
                Opacity = 0,
            };
        }

        public struct ColorTableRow
        {
            public ColorTableSubRow A;
            public ColorTableSubRow B;

            public static ColorTableRow White => new() { A = ColorTableSubRow.White, B = ColorTableSubRow.White };
        }

        public static (float R, float G, float B) ParseHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return (1f, 1f, 1f);

            hex = hex.Trim();
            if (hex.StartsWith('#'))
                hex = hex.Substring(1);

            if (hex.Length == 3)
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                return (1f, 1f, 1f);

            return ((rgb >> 16 & 0xFF) / 255f, (rgb >> 8 & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }

        public static Dictionary<int, ColorTableRow> BuildRowDictionary(
            IEnumerable<(int Row, ColorTableSubRow? SubRowA, ColorTableSubRow? SubRowB)> rows)
        {
            var dict = new Dictionary<int, ColorTableRow>();
            if (rows == null)
                return dict;

            foreach (var (row, subA, subB) in rows)
            {
                if (row < 1 || row > 16)
                    continue;

                int pairIdx = row - 1;
                dict[pairIdx] = new ColorTableRow
                {
                    A = subA ?? ColorTableSubRow.White,
                    B = subB ?? ColorTableSubRow.White,
                };
            }

            return dict;
        }

        public static byte ScaleOverlayAlpha(byte alpha, int opacity)
        {
            if (alpha == 0 || opacity == 0)
                return alpha;

            float a = alpha / 255f;
            float newA = opacity >= 0
                ? a + (1f - a) * (opacity / 100f)
                : a * (100f + opacity) / 100f;

            newA = Math.Clamp(newA, 0f, 1f);
            return (byte)(newA * 255f + 0.5f);
        }

        public static ColorTableSubRow ResolveIndexedSubRow(byte indexR, byte indexG, Dictionary<int, ColorTableRow> rows)
        {
            int pairIdx = indexR / 17;
            if (!rows.TryGetValue(pairIdx, out var row))
                return ColorTableSubRow.White;

            float blendA = indexG / 255f;
            return LerpSubRow(row.B, row.A, blendA);
        }

        public static ColorTableSubRow ResolveFlatSubRow(Dictionary<int, ColorTableRow> rows)
        {
            if (rows.TryGetValue(15, out var row16))
                return row16.A;
            return ColorTableSubRow.White;
        }

        private static ColorTableSubRow LerpSubRow(ColorTableSubRow b, ColorTableSubRow a, float t)
        {
            return new ColorTableSubRow
            {
                DiffuseR = b.DiffuseR + (a.DiffuseR - b.DiffuseR) * t,
                DiffuseG = b.DiffuseG + (a.DiffuseG - b.DiffuseG) * t,
                DiffuseB = b.DiffuseB + (a.DiffuseB - b.DiffuseB) * t,
                Emissive = b.Emissive + (a.Emissive - b.Emissive) * t,
                Opacity = (int)Math.Round(b.Opacity + (a.Opacity - b.Opacity) * t),
            };
        }

        public static Bitmap GenerateDiffuseFromNormal(Bitmap normal, ColorTableSubRow row16)
        {
            int w = normal.Width;
            int h = normal.Height;
            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var normalData = normal.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var outData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = normalData.Stride;
                int bytes = Math.Abs(stride) * h;
                var nBuf = new byte[bytes];
                var oBuf = new byte[bytes];
                Marshal.Copy(normalData.Scan0, nBuf, 0, bytes);

                for (int y = 0; y < h; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x * 4;
                        float coverage = nBuf[i] / 255f; // blue channel
                        byte a = (byte)(coverage * 255f + 0.5f);
                        oBuf[i + 3] = a;
                        oBuf[i + 2] = (byte)(row16.DiffuseR * coverage * 255f + 0.5f);
                        oBuf[i + 1] = (byte)(row16.DiffuseG * coverage * 255f + 0.5f);
                        oBuf[i] = (byte)(row16.DiffuseB * coverage * 255f + 0.5f);
                    }
                }

                Marshal.Copy(oBuf, 0, outData.Scan0, bytes);
            }
            finally
            {
                normal.UnlockBits(normalData);
                result.UnlockBits(outData);
            }

            return result;
        }

        public static Bitmap BakeDiffuseOverlay(
            Bitmap sourceDiffuse,
            Bitmap indexMap,
            Dictionary<int, ColorTableRow> rows,
            IReadOnlyList<Bitmap> coverageMasks)
        {
            int w = sourceDiffuse.Width;
            int h = sourceDiffuse.Height;
            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            var srcData = sourceDiffuse.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var idxData = indexMap?.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var outData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            var maskDatas = new List<(BitmapData Data, Bitmap Owner)>();
            try
            {
                int stride = srcData.Stride;
                int bytes = Math.Abs(stride) * h;
                var sBuf = new byte[bytes];
                var oBuf = new byte[bytes];
                byte[] iBuf = null;
                Marshal.Copy(srcData.Scan0, sBuf, 0, bytes);

                if (idxData != null)
                {
                    iBuf = new byte[bytes];
                    Marshal.Copy(idxData.Scan0, iBuf, 0, bytes);
                }

                if (coverageMasks != null)
                {
                    foreach (var mask in coverageMasks)
                    {
                        if (mask == null)
                            continue;

                        var md = mask.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        maskDatas.Add((md, mask));
                    }
                }

                var maskBuffers = new List<byte[]>();
                foreach (var (md, _) in maskDatas)
                {
                    var mBuf = new byte[bytes];
                    Marshal.Copy(md.Scan0, mBuf, 0, bytes);
                    maskBuffers.Add(mBuf);
                }

                bool hasRows = rows != null && rows.Count > 0;
                var flatRow = ResolveFlatSubRow(rows ?? new Dictionary<int, ColorTableRow>());

                for (int y = 0; y < h; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x * 4;
                        byte b = sBuf[i];
                        byte g = sBuf[i + 1];
                        byte r = sBuf[i + 2];
                        byte a = sBuf[i + 3];

                        if (a == 0)
                        {
                            oBuf[i] = oBuf[i + 1] = oBuf[i + 2] = oBuf[i + 3] = 0;
                            continue;
                        }

                        ColorTableSubRow sub = flatRow;
                        if (indexMap != null && iBuf != null && hasRows)
                        {
                            byte idxR = iBuf[i + 2];
                            byte idxG = iBuf[i + 1];
                            sub = ResolveIndexedSubRow(idxR, idxG, rows);
                        }
                        else if (hasRows)
                        {
                            sub = flatRow;
                        }

                        byte finalAlpha = ScaleOverlayAlpha(a, sub.Opacity);
                        foreach (var mBuf in maskBuffers)
                            finalAlpha = ApplyCoverageMaskByte(finalAlpha, a, mBuf[i + 2], mBuf[i + 3]);

                        oBuf[i + 3] = finalAlpha;
                        if (finalAlpha == 0)
                        {
                            oBuf[i] = oBuf[i + 1] = oBuf[i + 2] = 0;
                        }
                        else
                        {
                            oBuf[i + 2] = (byte)Math.Clamp(r * sub.DiffuseR, 0, 255);
                            oBuf[i + 1] = (byte)Math.Clamp(g * sub.DiffuseG, 0, 255);
                            oBuf[i] = (byte)Math.Clamp(b * sub.DiffuseB, 0, 255);
                        }
                    }
                }

                Marshal.Copy(oBuf, 0, outData.Scan0, bytes);
            }
            finally
            {
                sourceDiffuse.UnlockBits(srcData);
                if (idxData != null)
                    indexMap.UnlockBits(idxData);
                foreach (var (data, owner) in maskDatas)
                    owner.UnlockBits(data);
                result.UnlockBits(outData);
            }

            return result;
        }

        public static byte ApplyCoverageMaskByte(byte coverage, byte originalOverlayAlpha, byte maskGray, byte maskAlpha)
        {
            if (originalOverlayAlpha == 0)
                return 0;
            float visibility = 1.0f - (maskAlpha / 255f);
            float newCov = coverage / 255f * visibility;
            return (byte)(Math.Clamp(newCov, 0f, 1f) * 255f + 0.5f);
        }

        public static Bitmap BakeEmissiveOverlay(
            Bitmap coverageSource,
            Bitmap indexMap,
            Dictionary<int, ColorTableRow> rows)
        {
            int w = coverageSource.Width;
            int h = coverageSource.Height;
            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            var covData = coverageSource.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var idxData = indexMap?.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var outData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int stride = covData.Stride;
                int bytes = Math.Abs(stride) * h;
                var cBuf = new byte[bytes];
                var oBuf = new byte[bytes];
                byte[] iBuf = null;
                Marshal.Copy(covData.Scan0, cBuf, 0, bytes);

                if (idxData != null)
                {
                    iBuf = new byte[bytes];
                    Marshal.Copy(idxData.Scan0, iBuf, 0, bytes);
                }

                bool hasRows = rows != null && rows.Count > 0;
                var flatRow = ResolveFlatSubRow(rows ?? new Dictionary<int, ColorTableRow>());

                for (int y = 0; y < h; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x * 4;
                        byte covA = cBuf[i + 3];
                        if (covA == 0)
                        {
                            oBuf[i] = oBuf[i + 1] = oBuf[i + 2] = oBuf[i + 3] = 0;
                            continue;
                        }

                        ColorTableSubRow sub = flatRow;
                        if (indexMap != null && iBuf != null && hasRows)
                        {
                            sub = ResolveIndexedSubRow(iBuf[i + 2], iBuf[i + 1], rows);
                        }

                        float intensity = sub.Emissive * (covA / 255f);
                        byte v = (byte)(Math.Clamp(intensity, 0f, 1f) * 255f + 0.5f);
                        oBuf[i] = oBuf[i + 1] = oBuf[i + 2] = v;
                        oBuf[i + 3] = v > 0 ? (byte)255 : (byte)0;
                    }
                }

                Marshal.Copy(oBuf, 0, outData.Scan0, bytes);
            }
            finally
            {
                coverageSource.UnlockBits(covData);
                if (idxData != null)
                    indexMap.UnlockBits(idxData);
                result.UnlockBits(outData);
            }

            return result;
        }

        public static Bitmap LoadBitmapOrNull(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (!File.Exists(path) && !path.StartsWith("memory:\\", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                return TexIO.ResolveBitmap(path);
            }
            catch
            {
                return null;
            }
        }

        public static List<Bitmap> LoadCoverageMasks(IReadOnlyList<string> maskPaths, int width, int height)
        {
            var masks = new List<Bitmap>();
            if (maskPaths == null)
                return masks;

            foreach (var path in maskPaths)
            {
                using var loaded = LoadBitmapOrNull(path);
                if (loaded == null)
                    continue;

                if (loaded.Width != width || loaded.Height != height)
                {
                    var scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(loaded, 0, 0, width, height);
                    }
                    masks.Add(scaled);
                }
                else
                {
                    masks.Add(new Bitmap(loaded));
                }
            }

            return masks;
        }

        public sealed class BakeResult : IDisposable
        {
            public string DiffuseMemoryPath { get; set; }
            public string EmissiveMemoryPath { get; set; }
            public System.Numerics.Vector4 DefaultTint { get; set; } = System.Numerics.Vector4.One;
            public System.Numerics.Vector4 DefaultEmissiveTint { get; set; }

            public void Dispose()
            {
            }
        }

        public static BakeResult BakeResolvedOverlay(
            string diffusePath,
            string normalPath,
            string indexPath,
            bool generateDiffuse,
            Dictionary<int, ColorTableRow> rows,
            IReadOnlyList<string> coverageMaskPaths,
            string cacheKeyPrefix)
        {
            Bitmap diffuse = null;
            Bitmap index = null;
            var masks = new List<Bitmap>();
            try
            {
                if (!string.IsNullOrEmpty(diffusePath))
                    diffuse = LoadBitmapOrNull(diffusePath);
                else if (generateDiffuse && !string.IsNullOrEmpty(normalPath))
                {
                    using var normal = LoadBitmapOrNull(normalPath);
                    if (normal != null)
                    {
                        var row16 = ResolveFlatSubRow(rows ?? new Dictionary<int, ColorTableRow>());
                        diffuse = GenerateDiffuseFromNormal(normal, row16);
                    }
                }

                if (diffuse == null)
                    return null;

                if (!string.IsNullOrEmpty(indexPath))
                    index = LoadBitmapOrNull(indexPath);

                if (index != null && (index.Width != diffuse.Width || index.Height != diffuse.Height))
                {
                    var scaled = new Bitmap(diffuse.Width, diffuse.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(index, 0, 0, diffuse.Width, diffuse.Height);
                    }
                    index.Dispose();
                    index = scaled;
                }

                masks = LoadCoverageMasks(coverageMaskPaths, diffuse.Width, diffuse.Height);

                bool needsBake = (rows != null && rows.Count > 0) || index != null || masks.Count > 0
                    || (string.IsNullOrEmpty(diffusePath) && generateDiffuse);

                Bitmap bakedDiffuse = needsBake
                    ? BakeDiffuseOverlay(diffuse, index, rows ?? new Dictionary<int, ColorTableRow>(), masks)
                    : new Bitmap(diffuse);

                var row16Tint = ResolveFlatSubRow(rows ?? new Dictionary<int, ColorTableRow>());
                var result = new BakeResult
                {
                    DefaultTint = new System.Numerics.Vector4(row16Tint.DiffuseR, row16Tint.DiffuseG, row16Tint.DiffuseB, 1f),
                    DefaultEmissiveTint = new System.Numerics.Vector4(row16Tint.DiffuseR, row16Tint.DiffuseG, row16Tint.DiffuseB, 1f),
                };

                if (rows != null && rows.Count > 0 && rows.Values.Any(r => r.A.Emissive > 0f || r.B.Emissive > 0f))
                {
                    using var emissive = BakeEmissiveOverlay(bakedDiffuse, index, rows);
                    string emMem = $"memory:\\proteus_{cacheKeyPrefix}_e";
                    TexIO.SaveMemoryBitmap(emissive, emMem);
                    result.EmissiveMemoryPath = emMem;
                }

                string diffMem = $"memory:\\proteus_{cacheKeyPrefix}_d";
                TexIO.SaveMemoryBitmap(bakedDiffuse, diffMem);
                result.DiffuseMemoryPath = diffMem;
                bakedDiffuse.Dispose();

                return result;
            }
            finally
            {
                diffuse?.Dispose();
                index?.Dispose();
                foreach (var m in masks)
                    m.Dispose();
            }
        }
    }
}
