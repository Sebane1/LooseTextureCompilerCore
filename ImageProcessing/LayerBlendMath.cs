using System;

namespace FFXIVLooseTextureCompiler.ImageProcessing {
    /// <summary>
    /// Per-channel blend math shared by CPU compositing fallbacks.
    /// </summary>
    public static class LayerBlendMath {
        public static void ApplyBlend(int mode, float baseR, float baseG, float baseB, float topR, float topG, float topB, out float outR, out float outG, out float outB) {
            if (mode == (int)LayerBlendMode.Normal) {
                outR = topR; outG = topG; outB = topB;
                return;
            }

            outR = BlendChannel(mode, baseR, topR);
            outG = BlendChannel(mode, baseG, topG);
            outB = BlendChannel(mode, baseB, topB);
        }

        public static void CompositePixel(int mode, float baseR, float baseG, float baseB, float baseA, float topR, float topG, float topB, float topA,
            out float outR, out float outG, out float outB, out float outA) {
            if (topA <= 0f) {
                outR = baseR; outG = baseG; outB = baseB; outA = baseA;
                return;
            }

            ApplyBlend(mode, baseR, baseG, baseB, topR, topG, topB, out float blendedR, out float blendedG, out float blendedB);
            outA = topA + baseA * (1f - topA);
            if (outA > 0f) {
                outR = (blendedR * topA + baseR * baseA * (1f - topA)) / outA;
                outG = (blendedG * topA + baseG * baseA * (1f - topA)) / outA;
                outB = (blendedB * topA + baseB * baseA * (1f - topA)) / outA;
            } else {
                outR = outG = outB = 0f;
            }
        }

        private static float BlendChannel(int mode, float b, float t) {
            switch (mode) {
                case (int)LayerBlendMode.Multiply:
                    return b * t;
                case (int)LayerBlendMode.Screen:
                    return 1f - (1f - b) * (1f - t);
                case (int)LayerBlendMode.Overlay:
                    return b < 0.5f ? 2f * b * t : 1f - 2f * (1f - b) * (1f - t);
                case (int)LayerBlendMode.SoftLight:
                    return t < 0.5f
                        ? 2f * b * t + b * b * (1f - 2f * t)
                        : (float)(Math.Sqrt(b) * (2f * t - 1f) + 2f * b * (1f - t));
                case (int)LayerBlendMode.HardLight:
                    return t < 0.5f ? 2f * b * t : 1f - 2f * (1f - b) * (1f - t);
                case (int)LayerBlendMode.ColorDodge:
                    return t >= 1f ? 1f : Math.Min(1f, b / Math.Max(1e-6f, 1f - t));
                case (int)LayerBlendMode.ColorBurn:
                    return t <= 0f ? 0f : Math.Max(0f, 1f - (1f - b) / Math.Max(1e-6f, t));
                case (int)LayerBlendMode.Darken:
                    return Math.Min(b, t);
                case (int)LayerBlendMode.Lighten:
                    return Math.Max(b, t);
                case (int)LayerBlendMode.Difference:
                    return Math.Abs(b - t);
                case (int)LayerBlendMode.Exclusion:
                    return b + t - 2f * b * t;
                default:
                    return t;
            }
        }
    }
}
