namespace FFXIVLooseTextureCompiler.ImageProcessing {
    /// <summary>
    /// Photoshop-style layer blend modes for texture stack compositing.
    /// Values are stored as ints in configuration for JSON serialization compatibility.
    /// </summary>
    public enum LayerBlendMode : int {
        Normal = 0,
        Multiply = 1,
        Screen = 2,
        Overlay = 3,
        SoftLight = 4,
        HardLight = 5,
        ColorDodge = 6,
        ColorBurn = 7,
        Darken = 8,
        Lighten = 9,
        Difference = 10,
        Exclusion = 11,
    }

    public static class LayerBlendModeNames {
        public static readonly string[] All = {
            "Normal",
            "Multiply",
            "Screen",
            "Overlay",
            "Soft Light",
            "Hard Light",
            "Color Dodge",
            "Color Burn",
            "Darken",
            "Lighten",
            "Difference",
            "Exclusion",
        };

        public static string GetName(int mode) {
            if (mode >= 0 && mode < All.Length) return All[mode];
            return All[0];
        }
    }

    public static class LayerBlendModeDescriptions {
        public static readonly string[] All = {
            "Standard alpha compositing.",
            "Darkens by multiplying colors together.",
            "Lightens by inverting, multiplying, and inverting again.",
            "Combines multiply and screen based on the base brightness.",
            "Soft contrast boost; gentle highlight and shadow shaping.",
            "Strong contrast; multiply on dark base, screen on light base.",
            "Brightens the base toward white.",
            "Darkens the base toward black.",
            "Keeps the darker of the base or blend color per channel.",
            "Keeps the lighter of the base or blend color per channel.",
            "Absolute difference between base and blend colors.",
            "Similar to difference, but lower contrast.",
        };

        public static string GetDescription(int mode) {
            if (mode >= 0 && mode < All.Length) return All[mode];
            return All[0];
        }
    }
}
