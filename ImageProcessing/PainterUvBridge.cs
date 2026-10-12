using System;
using System.IO;
using System.Numerics;
using LooseTextureCompilerCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FFXIVLooseTextureCompiler.ImageProcessing;

/// <summary>
/// UV bridge between canvas (texture edit space) and preview mesh space.
/// Forward display and inverse paint use opposite map directions:
/// - Forward (canvas texture → preview mesh): canvas_to_preview map via ApplyTransferMap
/// - Inverse (preview raycast → canvas paint UV): preview_to_canvas map sampled at preview UV
/// </summary>
public sealed class PainterUvBridge : IDisposable
{
    private float[] _inverseMapX;
    private float[] _inverseMapY;
    private bool[] _inverseMapValid;
    private int _inverseMapWidth;
    private int _inverseMapHeight;
    private float[] _fallbackMapX;
    private float[] _fallbackMapY;
    private bool[] _fallbackMapValid;
    private int _fallbackMapWidth;
    private int _fallbackMapHeight;
    private string _forwardMapPath;
    private string _inverseMapPath;
    private string _lastLookupPath;
    private string _canvasType;
    private string _previewType;

    public bool IsActive =>
        !string.IsNullOrEmpty(_canvasType)
        && !string.IsNullOrEmpty(_previewType)
        && !string.Equals(_canvasType, _previewType, StringComparison.OrdinalIgnoreCase)
        && _inverseMapX != null;

    public string CanvasType => _canvasType;
    public string PreviewType => _previewType;
    public string ForwardMapPath => _forwardMapPath;
    public string InverseMapPath => _inverseMapPath;
    public string LoadedMapPath => _forwardMapPath;
    public string LastLookupPath => _lastLookupPath;

    public void Configure(string canvasType, string previewType, string modDirectoryHint = null)
    {
        canvasType = NormalizeBodyType(canvasType);
        previewType = NormalizeBodyType(previewType);

        if (string.Equals(canvasType, previewType, StringComparison.OrdinalIgnoreCase))
        {
            Clear();
            _canvasType = canvasType;
            _previewType = previewType;
            _lastLookupPath = null;
            return;
        }

        if (string.Equals(_canvasType, canvasType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_previewType, previewType, StringComparison.OrdinalIgnoreCase)
            && _inverseMapX != null)
        {
            return;
        }

        Clear();
        _canvasType = canvasType;
        _previewType = previewType;

        GlobalPathStorage.ResolveResourceBaseDirectory(modDirectoryHint);

        // canvas_to_preview: dest texels are preview-layout; R/G store canvas source UV.
        // Forward display = ApplyTransferMap(canvas, this map).
        // Inverse paint   = sample this map at preview raycast UV.
        _forwardMapPath = FastUVTransfer.GetBodyTransferMapPath(canvasType, previewType, modDirectoryHint);
        _inverseMapPath = _forwardMapPath;
        _lastLookupPath = _forwardMapPath;

        if (_inverseMapPath == null)
        {
            string filename = $"{canvasType}_to_{previewType}_transfer.tif";
            foreach (string baseDir in GlobalPathStorage.GetResourceBaseCandidates(modDirectoryHint))
            {
                _lastLookupPath = Path.Combine(baseDir, "res", "fastuvtransfer", "body", filename);
                break;
            }
            return;
        }

        LoadInverseMap(_inverseMapPath);

        string fallbackPath = FastUVTransfer.GetBodyTransferMapPath(previewType, canvasType, modDirectoryHint);
        if (!string.Equals(fallbackPath, _inverseMapPath, StringComparison.OrdinalIgnoreCase))
            LoadFallbackMap(fallbackPath);
    }

    /// <summary>
    /// Reprojects a canvas-space bitmap onto the preview mesh UV layout.
    /// </summary>
    public System.Drawing.Bitmap TransferCanvasToPreview(System.Drawing.Bitmap canvasBitmap)
    {
        if (canvasBitmap == null || string.IsNullOrEmpty(_forwardMapPath))
            return null;
        return UVTransferMap.ApplyTransferMap(canvasBitmap, _forwardMapPath);
    }

    public bool TryMapPreviewToCanvas(Vector2 previewUv, out Vector2 canvasUv)
    {
        canvasUv = default;
        if (!IsActive)
            return false;

        if (TryMapWithMap(previewUv, _inverseMapX, _inverseMapY, _inverseMapValid, _inverseMapWidth, _inverseMapHeight, out canvasUv))
            return true;

        if (_fallbackMapX != null
            && TryMapWithMap(previewUv, _fallbackMapX, _fallbackMapY, _fallbackMapValid, _fallbackMapWidth, _fallbackMapHeight, out canvasUv))
            return true;

        return false;
    }

    private static bool TryMapWithMap(
        Vector2 previewUv,
        float[] mapX,
        float[] mapY,
        bool[] mapValid,
        int mapWidth,
        int mapHeight,
        out Vector2 canvasUv)
    {
        canvasUv = default;
        if (mapX == null || mapWidth <= 1 || mapHeight <= 1)
            return false;

        previewUv = new Vector2(
            Math.Clamp(previewUv.X, 0f, 1f),
            Math.Clamp(previewUv.Y, 0f, 1f));

        float fx = previewUv.X * (mapWidth - 1);
        float fy = previewUv.Y * (mapHeight - 1);

        int x0 = Math.Clamp((int)MathF.Floor(fx), 0, mapWidth - 1);
        int y0 = Math.Clamp((int)MathF.Floor(fy), 0, mapHeight - 1);
        int x1 = Math.Min(x0 + 1, mapWidth - 1);
        int y1 = Math.Min(y0 + 1, mapHeight - 1);

        float tx = fx - x0;
        float ty = fy - y0;

        if (!SampleFrom(mapX, mapY, mapValid, mapWidth, mapHeight, x0, y0, out float u00, out float v00)
            || !SampleFrom(mapX, mapY, mapValid, mapWidth, mapHeight, x1, y0, out float u10, out float v10)
            || !SampleFrom(mapX, mapY, mapValid, mapWidth, mapHeight, x0, y1, out float u01, out float v01)
            || !SampleFrom(mapX, mapY, mapValid, mapWidth, mapHeight, x1, y1, out float u11, out float v11))
        {
            return false;
        }

        float u = u00 * (1f - tx) * (1f - ty)
                + u10 * tx * (1f - ty)
                + u01 * (1f - tx) * ty
                + u11 * tx * ty;
        float v = v00 * (1f - tx) * (1f - ty)
                + v10 * tx * (1f - ty)
                + v01 * (1f - tx) * ty
                + v11 * tx * ty;

        if (float.IsNaN(u) || float.IsNaN(v))
            return false;

        canvasUv = new Vector2(u, v);
        return canvasUv.X >= 0f && canvasUv.X <= 1f && canvasUv.Y >= 0f && canvasUv.Y <= 1f;
    }

    public void Dispose()
    {
        Clear();
    }

    public static string NormalizeBodyType(string bodyType)
    {
        if (string.IsNullOrWhiteSpace(bodyType))
            return null;

        string lower = bodyType.ToLowerInvariant();
        if (lower is "bibo" or "b+" or "bibo+")
            return "bibo";
        if (lower is "gen3" or "tfgen3")
            return "gen3";
        if (lower is "tbse" or "the body se" or "hrbody")
            return "tbse";
        if (lower is "gen2" or "vanilla" or "legacy")
            return "gen2";
        return lower;
    }

    public static string BodyTypeIndexToKeyword(int bodyTypeIndex)
    {
        return bodyTypeIndex switch
        {
            1 => "bibo",
            2 => "gen3",
            3 => "tbse",
            _ => "gen2"
        };
    }

    private void LoadInverseMap(string mapPath) => LoadMapData(mapPath, true);

    private void LoadFallbackMap(string mapPath)
    {
        if (string.IsNullOrEmpty(mapPath))
            return;
        LoadMapData(mapPath, false);
    }

    private void LoadMapData(string mapPath, bool primary)
    {
        try
        {
            using var transferImage = Image.Load<Rgba64>(mapPath);
            int width = transferImage.Width;
            int height = transferImage.Height;
            int count = width * height;
            var mapX = new float[count];
            var mapY = new float[count];
            var mapValid = new bool[count];

            transferImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba64> rowSpan = accessor.GetRowSpan(y);
                    int rowOffset = y * width;
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        Rgba64 pixel = rowSpan[x];
                        int idx = rowOffset + x;
                        bool valid = pixel.A >= 65535;
                        mapValid[idx] = valid;
                        if (valid)
                        {
                            mapX[idx] = pixel.R / 65535.0f;
                            mapY[idx] = pixel.G / 65535.0f;
                        }
                    }
                }
            });

            if (primary)
            {
                _inverseMapWidth = width;
                _inverseMapHeight = height;
                _inverseMapX = mapX;
                _inverseMapY = mapY;
                _inverseMapValid = mapValid;
            }
            else
            {
                _fallbackMapWidth = width;
                _fallbackMapHeight = height;
                _fallbackMapX = mapX;
                _fallbackMapY = mapY;
                _fallbackMapValid = mapValid;
            }
        }
        catch
        {
            if (primary)
                ClearMapData();
            else
                ClearFallbackMapData();
        }
    }

    private static bool SampleFrom(
        float[] mapX,
        float[] mapY,
        bool[] mapValid,
        int mapWidth,
        int mapHeight,
        int x,
        int y,
        out float u,
        out float v)
    {
        u = v = 0f;
        if (mapValid == null || x < 0 || y < 0 || x >= mapWidth || y >= mapHeight)
            return false;

        int idx = y * mapWidth + x;
        if (!mapValid[idx])
            return false;

        u = mapX[idx];
        v = mapY[idx];
        return true;
    }

    private void Clear()
    {
        ClearMapData();
        _canvasType = null;
        _previewType = null;
    }

    private void ClearMapData()
    {
        _inverseMapX = null;
        _inverseMapY = null;
        _inverseMapValid = null;
        _inverseMapWidth = 0;
        _inverseMapHeight = 0;
        _forwardMapPath = null;
        _inverseMapPath = null;
        ClearFallbackMapData();
    }

    private void ClearFallbackMapData()
    {
        _fallbackMapX = null;
        _fallbackMapY = null;
        _fallbackMapValid = null;
        _fallbackMapWidth = 0;
        _fallbackMapHeight = 0;
    }
}
