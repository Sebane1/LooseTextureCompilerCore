using ComputeSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FFXIVLooseTextureCompiler.ImageProcessing {

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct LayerImagesShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public LayerImagesShader(
            ReadOnlyTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];

            float srcAspect = (float)SrcWidth / (float)SrcHeight;
            float scaledWidth = (float)DestHeight * srcAspect;
            float scaledHeight = (float)DestHeight;
            
            float srcXf = (float)x / scaledWidth * SrcWidth;
            float srcYf = (float)y / scaledHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (srcXf >= 0.0f && srcXf < (float)SrcWidth && srcYf >= 0.0f && srcYf < (float)SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            // Alpha composite topLayer over bottomLayer's RGB (which is bottomPixel with Alpha=1.0)
            float topA = topPixel.W;
            
            float outR = topPixel.Z * topA + bottomPixel.Z * (1.0f - topA);
            float outG = topPixel.Y * topA + bottomPixel.Y * (1.0f - topA);
            float outB = topPixel.X * topA + bottomPixel.X * (1.0f - topA);
            
            // The alpha of the final image should remain the alpha of the bottom layer
            float outA = bottomPixel.W;

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MaxImagesShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MaxImagesShader(
            ReadOnlyTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;

            if (idx >= DestWidth * DestHeight) {
                return;
            }

            int x = idx % DestWidth;
            int y = idx / DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            
            float widthRatio = (float)SrcHeight / DestHeight;
            int scaledTopWidth = (int)(DestHeight * widthRatio);
            
            float srcXf = (float)x / scaledTopWidth * SrcWidth;
            float srcYf = (float)y / DestHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (x < scaledTopWidth && srcXf < SrcWidth && srcYf < SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            float outR = Hlsl.Max(topPixel.Z, bottomPixel.Z);
            float outG = Hlsl.Max(topPixel.Y, bottomPixel.Y);
            float outB = Hlsl.Max(topPixel.X, bottomPixel.X);
            
            float outA = Hlsl.Max(topPixel.W, bottomPixel.W);

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeImagesShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MergeImagesShader(
            ReadOnlyTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            float srcAspect = (float)SrcWidth / (float)SrcHeight;
            float scaledWidth = (float)DestHeight * srcAspect;
            float scaledHeight = (float)DestHeight;
            
            float srcXf = (float)x / scaledWidth * SrcWidth;
            float srcYf = (float)y / scaledHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (srcXf >= 0.0f && srcXf < (float)SrcWidth && srcYf >= 0.0f && srcYf < (float)SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            float topA = topPixel.W;
            float bottomA = bottomPixel.W;
            float outA = topA + bottomA * (1.0f - topA);
            
            float outR = 0;
            float outG = 0;
            float outB = 0;
            
            if (outA > 0) {
                outR = (topPixel.Z * topA + bottomPixel.Z * bottomA * (1.0f - topA)) / outA;
                outG = (topPixel.Y * topA + bottomPixel.Y * bottomA * (1.0f - topA)) / outA;
                outB = (topPixel.X * topA + bottomPixel.X * bottomA * (1.0f - topA)) / outA;
            }

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct DawntrailSkinMultiShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Input;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;
        public readonly int Height;

        public DawntrailSkinMultiShader(
            ReadOnlyTexture2D<Bgra32, float4> input, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int width, int height) {
            Input = input;
            Output = output;
            Width = width;
            Height = height;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= Width * Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            float4 pixel = Input[pos];
            
            float outB = 152.0f / 255.0f;
            float outG = 1.0f - pixel.X;
            float outR = pixel.Z;
            float outA = 1.0f;

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeImagesPingPongShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MergeImagesPingPongShader(
            ReadWriteTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            float srcAspect = (float)SrcWidth / (float)SrcHeight;
            float scaledWidth = (float)DestHeight * srcAspect;
            float scaledHeight = (float)DestHeight;
            
            float srcXf = (float)x / scaledWidth * SrcWidth;
            float srcYf = (float)y / scaledHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (srcXf >= 0.0f && srcXf < (float)SrcWidth && srcYf >= 0.0f && srcYf < (float)SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            float topA = topPixel.W;
            float bottomA = bottomPixel.W;
            float outA = topA + bottomA * (1.0f - topA);
            
            float outR = 0;
            float outG = 0;
            float outB = 0;
            
            if (outA > 0) {
                outR = (topPixel.Z * topA + bottomPixel.Z * bottomA * (1.0f - topA)) / outA;
                outG = (topPixel.Y * topA + bottomPixel.Y * bottomA * (1.0f - topA)) / outA;
                outB = (topPixel.X * topA + bottomPixel.X * bottomA * (1.0f - topA)) / outA;
            }

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeImagesPingPongTintedShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;
        public readonly float4 Tint;

        public MergeImagesPingPongTintedShader(
            ReadWriteTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight, float4 tint) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
            Tint = tint;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            float srcAspect = (float)SrcWidth / (float)SrcHeight;
            float scaledWidth = (float)DestHeight * srcAspect;
            float scaledHeight = (float)DestHeight;
            
            float srcXf = (float)x / scaledWidth * SrcWidth;
            float srcYf = (float)y / scaledHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (srcXf >= 0.0f && srcXf < (float)SrcWidth && srcYf >= 0.0f && srcYf < (float)SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)] * Tint;
            }

            float topA = topPixel.W;
            float bottomA = bottomPixel.W;
            float outA = topA + bottomA * (1.0f - topA);
            
            float outR = 0;
            float outG = 0;
            float outB = 0;
            
            if (outA > 0) {
                outR = (topPixel.Z * topA + bottomPixel.Z * bottomA * (1.0f - topA)) / outA;
                outG = (topPixel.Y * topA + bottomPixel.Y * bottomA * (1.0f - topA)) / outA;
                outB = (topPixel.X * topA + bottomPixel.X * bottomA * (1.0f - topA)) / outA;
            }

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeGlowImagesPingPongShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;

        public MergeGlowImagesPingPongShader(
            ReadWriteTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            float srcAspect = (float)SrcWidth / (float)SrcHeight;
            float scaledWidth = (float)DestHeight * srcAspect;
            float scaledHeight = (float)DestHeight;
            
            float srcXf = (float)x / scaledWidth * SrcWidth;
            float srcYf = (float)y / scaledHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (srcXf >= 0.0f && srcXf < (float)SrcWidth && srcYf >= 0.0f && srcYf < (float)SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)];
            }

            // Glow blending: pick the brightest pixel instead of alpha blending
            float outB = Hlsl.Max(bottomPixel.X, topPixel.X);
            float outG = Hlsl.Max(bottomPixel.Y, topPixel.Y);
            float outR = Hlsl.Max(bottomPixel.Z, topPixel.Z);
            float outA = 1.0f; // Glow maps are opaque

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeGlowImagesPingPongTintedShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> BottomLayer;
        public readonly ReadOnlyTexture2D<Bgra32, float4> TopLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int SrcWidth;
        public readonly int SrcHeight;
        public readonly float4 Tint;

        public MergeGlowImagesPingPongTintedShader(
            ReadWriteTexture2D<Bgra32, float4> bottomLayer, 
            ReadOnlyTexture2D<Bgra32, float4> topLayer, 
            ReadWriteTexture2D<Bgra32, float4> output, 
            int destWidth, int destHeight, int srcWidth, int srcHeight, float4 tint) {
            BottomLayer = bottomLayer;
            TopLayer = topLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
            Tint = tint;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 bottomPixel = BottomLayer[pos];
            float srcAspect = (float)SrcWidth / (float)SrcHeight;
            float scaledWidth = (float)DestHeight * srcAspect;
            float scaledHeight = (float)DestHeight;
            
            float srcXf = (float)x / scaledWidth * SrcWidth;
            float srcYf = (float)y / scaledHeight * SrcHeight;

            float4 topPixel = float4.Zero;
            
            if (srcXf >= 0.0f && srcXf < (float)SrcWidth && srcYf >= 0.0f && srcYf < (float)SrcHeight) {
                int srcX = Hlsl.Clamp((int)srcXf, 0, SrcWidth - 1);
                int srcY = Hlsl.Clamp((int)srcYf, 0, SrcHeight - 1);
                topPixel = TopLayer[new int2(srcX, srcY)] * Tint;
            }

            // Glow blending: pick the brightest pixel instead of alpha blending
            float outB = Hlsl.Max(bottomPixel.X, topPixel.X);
            float outG = Hlsl.Max(bottomPixel.Y, topPixel.Y);
            float outR = Hlsl.Max(bottomPixel.Z, topPixel.Z);
            float outA = 1.0f; // Glow maps are opaque

            Output[pos] = new float4(outB, outG, outR, outA);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct CopyShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Source;
        public readonly ReadWriteTexture2D<Bgra32, float4> Destination;
        public readonly int Width;
        public readonly int Height;

        public CopyShader(ReadOnlyTexture2D<Bgra32, float4> source, ReadWriteTexture2D<Bgra32, float4> destination, int width, int height) {
            Source = source;
            Destination = destination;
            Width = width;
            Height = height;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= Width * Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            Destination[pos] = Source[pos];
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeAlphaToRGBScalingShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Rgb;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Alpha;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int AlphaWidth;
        public readonly int AlphaHeight;
        public readonly int InvertAlpha;

        public MergeAlphaToRGBScalingShader(ReadOnlyTexture2D<Bgra32, float4> rgb, ReadOnlyTexture2D<Bgra32, float4> alpha, ReadWriteTexture2D<Bgra32, float4> output, int destWidth, int destHeight, int alphaWidth, int alphaHeight, int invertAlpha) {
            Rgb = rgb;
            Alpha = alpha;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            AlphaWidth = alphaWidth;
            AlphaHeight = alphaHeight;
            InvertAlpha = invertAlpha;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 rgbPixel = Rgb[pos];
            
            float alphaXf = (float)x / DestWidth * AlphaWidth;
            float alphaYf = (float)y / DestHeight * AlphaHeight;
            int alphaX = Hlsl.Clamp((int)alphaXf, 0, AlphaWidth - 1);
            int alphaY = Hlsl.Clamp((int)alphaYf, 0, AlphaHeight - 1);
            float4 alphaPixel = Alpha[new int2(alphaX, alphaY)];
            
            float alphaVal = alphaPixel.Z;
            if (InvertAlpha == 1) {
                alphaVal = 1.0f - alphaVal;
            }

            Output[pos] = new float4(rgbPixel.X, rgbPixel.Y, rgbPixel.Z, alphaVal);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MergeAlphaChannelToRGBScalingShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Rgb;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Alpha;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int AlphaWidth;
        public readonly int AlphaHeight;
        public readonly int InvertAlpha;

        public MergeAlphaChannelToRGBScalingShader(ReadOnlyTexture2D<Bgra32, float4> rgb, ReadOnlyTexture2D<Bgra32, float4> alpha, ReadWriteTexture2D<Bgra32, float4> output, int destWidth, int destHeight, int alphaWidth, int alphaHeight, int invertAlpha) {
            Rgb = rgb;
            Alpha = alpha;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            AlphaWidth = alphaWidth;
            AlphaHeight = alphaHeight;
            InvertAlpha = invertAlpha;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 rgbPixel = Rgb[pos];
            
            float alphaXf = (float)x / DestWidth * AlphaWidth;
            float alphaYf = (float)y / DestHeight * AlphaHeight;
            int alphaX = Hlsl.Clamp((int)alphaXf, 0, AlphaWidth - 1);
            int alphaY = Hlsl.Clamp((int)alphaYf, 0, AlphaHeight - 1);
            float4 alphaPixel = Alpha[new int2(alphaX, alphaY)];
            
            float alphaVal = alphaPixel.W;
            if (InvertAlpha == 1) {
                alphaVal = 1.0f - alphaVal;
            }

            Output[pos] = new float4(rgbPixel.X, rgbPixel.Y, rgbPixel.Z, alphaVal);
        }
    }

    // Restores the base (underlay) layer's alpha channel onto the merged RGB result.
    // Used as a final pass in the ping-pong merge pipeline to preserve authoritative
    // alpha data (e.g. lip colour influence on face normals).
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct RestoreBaseAlphaShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> Source;
        public readonly ReadOnlyTexture2D<Bgra32, float4> BaseLayer;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int DestWidth;
        public readonly int DestHeight;
        public readonly int BaseWidth;
        public readonly int BaseHeight;

        public RestoreBaseAlphaShader(
            ReadWriteTexture2D<Bgra32, float4> source,
            ReadOnlyTexture2D<Bgra32, float4> baseLayer,
            ReadWriteTexture2D<Bgra32, float4> output,
            int destWidth, int destHeight, int baseWidth, int baseHeight) {
            Source = source;
            BaseLayer = baseLayer;
            Output = output;
            DestWidth = destWidth;
            DestHeight = destHeight;
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= DestWidth * DestHeight) return;

            int y = idx / DestWidth;
            int x = idx % DestWidth;
            int2 pos = new int2(x, y);

            float4 mergedPixel = Source[pos];

            // Sample the base layer's alpha at the corresponding position
            float baseXf = (float)x / DestWidth * BaseWidth;
            float baseYf = (float)y / DestHeight * BaseHeight;
            int baseX = Hlsl.Clamp((int)baseXf, 0, BaseWidth - 1);
            int baseY = Hlsl.Clamp((int)baseYf, 0, BaseHeight - 1);
            float4 basePixel = BaseLayer[new int2(baseX, baseY)];

            Output[pos] = new float4(mergedPixel.X, mergedPixel.Y, mergedPixel.Z, basePixel.W);
        }
    }

    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct ClearShader : IComputeShader {
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int Width;
        public readonly int Height;

        public ClearShader(ReadWriteTexture2D<Bgra32, float4> output, int width, int height) {
            Output = output;
            Width = width;
            Height = height;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= Width * Height) return;

            int y = idx / Width;
            int x = idx % Width;
            int2 pos = new int2(x, y);

            Output[pos] = new float4(0, 0, 0, 0);
        }
    }

    // Single-dispatch shader that composites ALL layers in one pass
    // Layer metadata layout in metaBuffer: [layerCount, destW, destH, <padding>,
    //                                       layer0_width, layer0_height, layer0_pixelOffset, <padding>,
    //                                       layer1_width, layer1_height, layer1_pixelOffset, <padding>, ...]
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MultiLayerMergeShader : IComputeShader {
        public readonly ReadOnlyBuffer<uint> AllPixels;
        public readonly ReadOnlyBuffer<int> Meta;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;

        public MultiLayerMergeShader(
            ReadOnlyBuffer<uint> allPixels,
            ReadOnlyBuffer<int> meta,
            ReadWriteTexture2D<Bgra32, float4> output) {
            AllPixels = allPixels;
            Meta = meta;
            Output = output;
        }

        public void Execute() {
            int layerCount = Meta[0];
            int destW = Meta[1];
            int destH = Meta[2];

            int idx = ThreadIds.X;
            if (idx >= destW * destH) return;

            int y = idx / destW;
            int x = idx % destW;

            // Accumulate: start transparent
            float accR = 0, accG = 0, accB = 0, accA = 0;

            for (int layer = 0; layer < layerCount; layer++) {
                int metaBase = 4 + layer * 4;
                int srcW = Meta[metaBase];
                int srcH = Meta[metaBase + 1];
                int pixelOffset = Meta[metaBase + 2];

                float srcAspect = (float)srcW / (float)srcH;
                float scaledWidth = (float)destH * srcAspect;
                float scaledHeight = (float)destH;
                
                float srcXf = (float)x / scaledWidth * srcW;
                float srcYf = (float)y / scaledHeight * srcH;

                if (srcXf >= 0.0f && srcXf < (float)srcW && srcYf >= 0.0f && srcYf < (float)srcH) {
                    int srcX = Hlsl.Clamp((int)srcXf, 0, srcW - 1);
                    int srcY = Hlsl.Clamp((int)srcYf, 0, srcH - 1);

                    uint packed = AllPixels[pixelOffset + srcY * srcW + srcX];
                    // BGRA byte order: B=byte0, G=byte1, R=byte2, A=byte3
                    float topB = (float)(packed & 0xFF) / 255.0f;
                    float topG = (float)((packed >> 8) & 0xFF) / 255.0f;
                    float topR = (float)((packed >> 16) & 0xFF) / 255.0f;
                    float topA = (float)((packed >> 24) & 0xFF) / 255.0f;

                    if (topA > 0) {
                        float outA = topA + accA * (1.0f - topA);
                        if (outA > 0) {
                            accR = (topR * topA + accR * accA * (1.0f - topA)) / outA;
                            accG = (topG * topA + accG * accA * (1.0f - topA)) / outA;
                            accB = (topB * topA + accB * accA * (1.0f - topA)) / outA;
                        }
                        accA = outA;
                    }
                }
            }

            Output[new int2(x, y)] = new float4(accB, accG, accR, accA);
        }
    }

    public static class ComputeSharpLayering {
        
        private static System.Collections.Concurrent.ConcurrentDictionary<string, (ReadOnlyTexture2D<Bgra32, float4> Texture, int Width, int Height)> _vramCache = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Pixels, int Width, int Height)> _cpuPixelCache = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.IO.FileSystemWatcher> _watchers = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, byte> _invalidatedPaths = new();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _lastAccess = new();
        private const int MAX_CACHE_SIZE = 15;

        private static readonly object _gpuLock = new object();

        public static void AuditVram() {
            if (_vramCache.Count > MAX_CACHE_SIZE) {
                var oldest = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderBy(_lastAccess, kvp => kvp.Value), _vramCache.Count - MAX_CACHE_SIZE), kvp => kvp.Key));
                foreach (var path in oldest) {
                    if (_vramCache.TryRemove(path, out var entry)) {
                        lock (_gpuLock) {
                            entry.Texture.Dispose();
                        }
                    }
                    _cpuPixelCache.TryRemove(path, out _);
                    _lastAccess.TryRemove(path, out _);
                }
            }
            if (_cpuPixelCache.Count > MAX_CACHE_SIZE) {
                var oldest = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderBy(_lastAccess, kvp => kvp.Value), _cpuPixelCache.Count - MAX_CACHE_SIZE), kvp => kvp.Key));
                foreach (var path in oldest) {
                    _cpuPixelCache.TryRemove(path, out _);
                    _lastAccess.TryRemove(path, out _);
                }
            }
        }

        // Cached working surfaces — reused across merge calls to avoid GPU alloc/dealloc churn
        private static ReadWriteTexture2D<Bgra32, float4> _cachedPing;
        private static ReadWriteTexture2D<Bgra32, float4> _cachedPong;
        private static int _cachedWidth;
        private static int _cachedHeight;
        private static byte[] _cachedResultBuffer;

        private static int _invalidationCount = 0;
        private static void OnFileChanged(object sender, System.IO.FileSystemEventArgs e) {
            var fullPath = e.FullPath;
            InvalidateCache(fullPath);
        }

        public static void InvalidateCache(string fullPath) {
            // Only invalidate if this file is actually in one of our caches
            bool wasCached = false;
            if (_vramCache.TryRemove(fullPath, out var entry)) {
                lock (_gpuLock) {
                    entry.Texture.Dispose();
                }
                wasCached = true;
            }
            if (_cpuPixelCache.TryRemove(fullPath, out _)) {
                wasCached = true;
            }
            if (wasCached) {
                _invalidatedPaths[fullPath] = 0;
                _invalidationCount++;
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt"), 
                    $"  [CACHE INVALIDATED #{_invalidationCount}] System IO Event: {System.IO.Path.GetFileName(fullPath)}\r\n"); } catch {}
            }
        }

        private static void WatchDirectory(string filePath) {
            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir) || _watchers.ContainsKey(dir))
                return;

            try {
                var watcher = new System.IO.FileSystemWatcher(dir) {
                    NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                watcher.Changed += OnFileChanged;
                watcher.Renamed += (s, e) => OnFileChanged(s, e);
                watcher.Deleted += OnFileChanged;
                _watchers[dir] = watcher;
            } catch {
                // Directory may not exist or be inaccessible — silently skip
            }
        }

        public static void ClearCache() {
            lock (_gpuLock) {
                foreach (var kvp in _vramCache) {
                    kvp.Value.Texture.Dispose();
                }
                _vramCache.Clear();
                _cpuPixelCache.Clear();
                _invalidatedPaths.Clear();
                foreach (var kvp in _watchers) {
                    kvp.Value.Dispose();
                }
                _watchers.Clear();
                _cachedPing?.Dispose();
                _cachedPong?.Dispose();
                _cachedPing = null;
                _cachedPong = null;
                _cachedResultBuffer = null;
            }
        }

        // CPU-only pixel loading (thread-safe, parallelizable)
        private struct CpuLayerData {
            public byte[] Pixels;
            public int Width;
            public int Height;
            public string Path;
            public bool IsPhysicalFile;
            public bool CacheHit;
        }

        public static (int Width, int Height) GetDimensions(string path) {
            if (string.IsNullOrEmpty(path)) return (0, 0);
            if (_cpuPixelCache.TryGetValue(path, out var cpuCached)) {
                return (cpuCached.Width, cpuCached.Height);
            }
            if (_vramCache.TryGetValue(path, out var vramCached)) {
                return (vramCached.Width, vramCached.Height);
            }
            return (0, 0);
        }

        private static CpuLayerData LoadPixelsCpu(string path) {
            var result = new CpuLayerData { Path = path };
            if (string.IsNullOrEmpty(path))
                return result;

            result.IsPhysicalFile = !path.StartsWith("memory://", StringComparison.OrdinalIgnoreCase);

            // Fast path: if file is in CPU pixel cache and not invalidated, return cached pixels
            if (FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                bool inCache = _cpuPixelCache.TryGetValue(path, out var cpuCached);
                bool invalidated = _invalidatedPaths.ContainsKey(path);
                if (inCache && !invalidated) {
                    _lastAccess[path] = DateTime.UtcNow;
                    result.CacheHit = true;
                    result.Pixels = cpuCached.Pixels;
                    result.Width = cpuCached.Width;
                    result.Height = cpuCached.Height;
                    return result;
                }
                if (!inCache && _cpuPixelCache.Count > 0 && result.IsPhysicalFile) {
                    // Find if same filename exists under a different full path
                    string lookupName = System.IO.Path.GetFileName(path);
                    string matchingCachedPath = "";
                    foreach (var k in _cpuPixelCache.Keys) {
                        if (System.IO.Path.GetFileName(k) == lookupName) { matchingCachedPath = k; break; }
                    }
                    string pathSuffix = path.Length > 80 ? "..." + path.Substring(path.Length - 80) : path;
                    string matchSuffix = string.IsNullOrEmpty(matchingCachedPath) ? "NOT_FOUND" : 
                        (matchingCachedPath.Length > 80 ? "..." + matchingCachedPath.Substring(matchingCachedPath.Length - 80) : matchingCachedPath);
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt"), 
                        $"  [CACHE MISS] lookup=\"{pathSuffix}\" cachedAs=\"{matchSuffix}\"\r\n"); } catch {}
                }
                // Clear invalidation flag — we're about to reload
                _invalidatedPaths.TryRemove(path, out _);
            }

            // Direct memory file fast path: read directly from VirtualFileSystem and bypass all file check/GDI+ decode routines
            if (!result.IsPhysicalFile) {
                if (TexIO.VirtualFileSystem.TryGetValue(path, out var file)) {
                    if (file.Data != null && file.Width > 0 && file.Height > 0) {
                        result.Pixels = new byte[file.Data.Length];
                        Array.Copy(file.Data, result.Pixels, file.Data.Length);
                        result.Width = file.Width;
                        result.Height = file.Height;
                        if (FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                            _cpuPixelCache[path] = (result.Pixels, result.Width, result.Height);
                            _lastAccess[path] = DateTime.UtcNow;
                        }
                        return result;
                    }
                }
            }

            // Cache miss or invalidated — validate file exists before decoding
            if (!TexIO.Exists(path))
                return result;

            // Cache miss — decode pixels on CPU
            if (path.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".ltct", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".raw", StringComparison.OrdinalIgnoreCase)) {
                using (var bitmap = TexIO.ResolveBitmap(path)) {
                    Bitmap safe = bitmap.PixelFormat == PixelFormat.Format32bppArgb ? bitmap : bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format32bppArgb);
                    var bmpData = safe.LockBits(new Rectangle(0, 0, safe.Width, safe.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    result.Pixels = new byte[safe.Width * safe.Height * 4];
                    Marshal.Copy(bmpData.Scan0, result.Pixels, 0, result.Pixels.Length);
                    safe.UnlockBits(bmpData);
                    if (safe != bitmap) safe.Dispose();
                    result.Width = safe.Width;
                    result.Height = safe.Height;
                }
            } else {
                while (TexIO.IsFileLocked(path)) { System.Threading.Thread.Sleep(100); }
                using (var ms = new System.IO.MemoryStream()) {
                    using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)) {
                        fs.CopyTo(ms);
                    }
                    ms.Position = 0;
                    using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Bgra32>(ms)) {
                        result.Pixels = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(result.Pixels);
                        result.Width = image.Width;
                        result.Height = image.Height;
                    }
                }
            }

            // Store in CPU pixel cache for fast buffer packing on future calls
            if (result.Pixels != null && FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                _cpuPixelCache[path] = (result.Pixels, result.Width, result.Height);
                _lastAccess[path] = DateTime.UtcNow;
                if (result.IsPhysicalFile) WatchDirectory(path);
            }

            return result;
        }

        // Phase 2: GPU upload (must be called sequentially on a single thread)
        private static (ReadOnlyTexture2D<Bgra32, float4> Texture, bool IsCached, int Width, int Height) UploadToVram(GraphicsDevice device, CpuLayerData cpuData) {
            if (string.IsNullOrEmpty(cpuData.Path))
                return (null, false, 0, 0);

            // Fast path: check VRAM cache directly
            if (_vramCache.TryGetValue(cpuData.Path, out var cached) && !_invalidatedPaths.ContainsKey(cpuData.Path)) {
                _lastAccess[cpuData.Path] = DateTime.UtcNow;
                return (cached.Texture, true, cached.Width, cached.Height);
            }

            // Slow path: cache miss — dispose stale entry if present
            if (FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                if (_vramCache.TryRemove(cpuData.Path, out var staleEntry)) {
                    staleEntry.Texture.Dispose();
                }
            }

            if (cpuData.Pixels == null) return (null, false, 0, 0);

            // Allocate and upload to GPU (single-threaded, no driver contention)
            var texture = device.AllocateReadOnlyTexture2D<Bgra32, float4>(cpuData.Width, cpuData.Height);
            texture.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(cpuData.Pixels));

            if (FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                _vramCache[cpuData.Path] = (texture, cpuData.Width, cpuData.Height);
                _lastAccess[cpuData.Path] = DateTime.UtcNow;
                if (cpuData.IsPhysicalFile) WatchDirectory(cpuData.Path);
                return (texture, true, cpuData.Width, cpuData.Height);
            }

            return (texture, false, cpuData.Width, cpuData.Height);
        }

        private static bool _gpuUnavailable = false;

        public static Bitmap MergeMultipleImagesGpuFromPaths(System.Collections.Generic.List<string> paths, int width, int height, System.Collections.Generic.List<System.Numerics.Vector4> tints = null, bool preserveBaseAlpha = false) {
            if (width <= 0 || height <= 0) {
                System.Diagnostics.Debug.WriteLine($"[MergeMultipleImagesGpuFromPaths] Invalid dimensions: {width}x{height}. Clearing cache and returning 1x1 fallback.");
                ClearCache();
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }
            int totalPixels = width * height;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            long[] cpuTimes = new long[paths.Count];
            long[] vramTimes = new long[paths.Count];

            // Phase 1: Load/cache all layer pixels on CPU (parallel for cache misses)
            var cpuLayers = new CpuLayerData[paths.Count];
            bool allCached = true;
            for (int i = 0; i < paths.Count; i++) {
                bool isLoaded = false;
                if (FFXIVLooseTextureCompiler.PathOrganization.UniversalTextureSetCreator.UseMemoryCache) {
                    bool inCpuCache = _cpuPixelCache.ContainsKey(paths[i]) && !_invalidatedPaths.ContainsKey(paths[i]);
                    if (inCpuCache) {
                        cpuLayers[i] = LoadPixelsCpu(paths[i]);
                        isLoaded = true;
                    }
                }
                if (!isLoaded) {
                    allCached = false;
                }
            }

            if (!allCached) {
                System.Threading.Tasks.Parallel.For(0, paths.Count, i => {
                    if (cpuLayers[i].Pixels == null) {
                        var layerSw = System.Diagnostics.Stopwatch.StartNew();
                        cpuLayers[i] = LoadPixelsCpu(paths[i]);
                        cpuTimes[i] = layerSw.ElapsedMilliseconds;
                    }
                });
            }
            long phase1Ms = sw.ElapsedMilliseconds;
            int cpuHits = 0, cpuMisses = 0, memoryPaths = 0;
            for (int i = 0; i < cpuLayers.Length; i++) {
                if (string.IsNullOrEmpty(cpuLayers[i].Path)) continue;
                if (!cpuLayers[i].IsPhysicalFile) memoryPaths++;
                if (cpuLayers[i].CacheHit) cpuHits++; else cpuMisses++;
            }
            sw.Restart();

            // Skip GPU entirely if we already know it's unavailable (e.g. Linux/Wine)
            if (_gpuUnavailable) {
                return MergeLayersCpuFallback(cpuLayers, width, height, tints, preserveBaseAlpha);
            }

            try {
                var device = GraphicsDevice.GetDefault();

                // All GPU work serialized
                lock (_gpuLock) {
                    // Phase 2: Upload to VRAM (sequential, single-threaded)
                    var textures = new (ReadOnlyTexture2D<Bgra32, float4> Tex, bool IsCached, int Width, int Height)[paths.Count];
                    int vramHits = 0, vramMisses = 0;
                    for (int i = 0; i < paths.Count; i++) {
                        var layerSw = System.Diagnostics.Stopwatch.StartNew();
                        textures[i] = UploadToVram(device, cpuLayers[i]);
                        vramTimes[i] = layerSw.ElapsedMilliseconds;
                        
                        if (textures[i].IsCached && cpuLayers[i].CacheHit) vramHits++; 
                        else if (textures[i].Tex != null) vramMisses++;
                    }
                    long phase2Ms = sw.ElapsedMilliseconds;
                    sw.Restart();

                    // Reuse cached ping/pong working textures if dimensions match
                    if (_cachedPing == null || _cachedWidth != width || _cachedHeight != height) {
                        _cachedPing?.Dispose();
                        _cachedPong?.Dispose();
                        _cachedPing = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
                        _cachedPong = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
                        _cachedWidth = width;
                        _cachedHeight = height;
                        _cachedResultBuffer = new byte[totalPixels * 4];
                    }
                    var ping = _cachedPing;
                    var pong = _cachedPong;

                    // Phase 3: Batched GPU merge — all dispatches recorded into one command list
                    bool isPing = true;
                    using (var context = device.CreateComputeContext()) {
                        // Initialize base layer
                        if (textures.Length > 0 && textures[0].Tex != null) {
                            var ld = textures[0];
                            float4 tint = tints != null && 0 < tints.Count ? new float4(tints[0].X, tints[0].Y, tints[0].Z, tints[0].W) : new float4(1,1,1,1);
                            context.For(totalPixels, new ClearShader(ping, width, height));
                            context.For(totalPixels, new MergeImagesPingPongTintedShader(ping, ld.Tex, pong, width, height, ld.Width, ld.Height, tint));
                            isPing = false;
                        }

                        // Merge remaining layers — all recorded, no fence waits between them
                        for (int i = 1; i < textures.Length; i++) {
                            var ld = textures[i];
                            if (ld.Tex == null) continue;

                            float4 tint = tints != null && i < tints.Count ? new float4(tints[i].X, tints[i].Y, tints[i].Z, tints[i].W) : new float4(1,1,1,1);
                            if (isPing) {
                                context.For(totalPixels, new MergeImagesPingPongTintedShader(ping, ld.Tex, pong, width, height, ld.Width, ld.Height, tint));
                            } else {
                                context.For(totalPixels, new MergeImagesPingPongTintedShader(pong, ld.Tex, ping, width, height, ld.Width, ld.Height, tint));
                            }
                            isPing = !isPing;
                        }

                        // Final pass: restore the base layer's alpha channel.
                        // The underlay's alpha is authoritative (e.g. lip colour on face normals).
                        if (preserveBaseAlpha && textures.Length > 0 && textures[0].Tex != null) {
                            var baseTex = textures[0];
                            if (isPing) {
                                context.For(totalPixels, new RestoreBaseAlphaShader(ping, baseTex.Tex, pong, width, height, baseTex.Width, baseTex.Height));
                            } else {
                                context.For(totalPixels, new RestoreBaseAlphaShader(pong, baseTex.Tex, ping, width, height, baseTex.Width, baseTex.Height));
                            }
                            isPing = !isPing;
                        }
                    } // ComputeContext disposes here — submits ALL dispatches as one command list, ONE fence wait
                    long phase3Ms = sw.ElapsedMilliseconds;
                    sw.Restart();

                    // Dispose non-cached textures
                    for (int i = 0; i < textures.Length; i++) {
                        if (!textures[i].IsCached && textures[i].Tex != null)
                            textures[i].Tex.Dispose();
                    }

                    // Only GPU→CPU transfer: the final merged result (unavoidable for disk write)
                    if (isPing) {
                        ping.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
                    } else {
                        pong.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
                    }
                    long phase4Ms = sw.ElapsedMilliseconds;
                    AuditVram();
                    sw.Restart();

                    var detailedLog = new System.Text.StringBuilder();
                    detailedLog.AppendLine($"--- Detailed GPU Merge Benchmark ({paths.Count} layers) ---");
                    detailedLog.AppendLine($"Total Phase 1 (CPU Load): {phase1Ms}ms");
                    for (int i = 0; i < paths.Count; i++) {
                        if (string.IsNullOrEmpty(paths[i])) continue;
                        string hitMiss = cpuLayers[i].CacheHit ? "HIT" : "MISS";
                        string pName = System.IO.Path.GetFileName(paths[i]);
                        detailedLog.AppendLine($"  [{i:D2}] CPU Load ({hitMiss}): {cpuTimes[i]}ms - {pName}");
                    }
                    detailedLog.AppendLine($"Total Phase 2 (VRAM Upload): {phase2Ms}ms");
                    for (int i = 0; i < paths.Count; i++) {
                        if (string.IsNullOrEmpty(paths[i])) continue;
                        string hitMiss = (textures[i].IsCached && cpuLayers[i].CacheHit) ? "HIT" : "MISS";
                        string pName = System.IO.Path.GetFileName(paths[i]);
                        detailedLog.AppendLine($"  [{i:D2}] VRAM Upload ({hitMiss}): {vramTimes[i]}ms - {pName}");
                    }
                    detailedLog.AppendLine($"Phase 3 (GPU Merge Dispatches): {phase3Ms}ms");
                    detailedLog.AppendLine($"Phase 4 (Readback to CPU): {phase4Ms}ms");
                    detailedLog.AppendLine($"Cache State: CPU={_cpuPixelCache.Count}, VRAM={_vramCache.Count}");
                    detailedLog.AppendLine();

                    // Safe to do inside _gpuLock because it guarantees serial writes from this method
                    try { 
                        string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt");
                        bool locked = true;
                        int retries = 5;
                        while (locked && retries > 0) {
                            try {
                                System.IO.File.AppendAllText(logPath, detailedLog.ToString());
                                locked = false;
                            } catch (System.IO.IOException) {
                                System.Threading.Thread.Sleep(5);
                                retries--;
                            }
                        }
                    } catch {}
                } // release GPU lock

                Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var bmpDataResult = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(_cachedResultBuffer, 0, bmpDataResult.Scan0, _cachedResultBuffer.Length);
                result.UnlockBits(bmpDataResult);

                return result;
            } catch (Exception ex) {
                // GPU unavailable (e.g. Linux/Wine without DirectX 12 compute support)
                _gpuUnavailable = true;
                System.Diagnostics.Debug.WriteLine($"[MergeMultipleImagesGpuFromPaths] GPU unavailable, using CPU fallback: {ex.Message}");
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GPU_Benchmark.txt"), 
                    $"[GPU UNAVAILABLE] Falling back to CPU compositing: {ex.Message}\r\n"); } catch {}
                return MergeLayersCpuFallback(cpuLayers, width, height, tints, preserveBaseAlpha);
            }
        }

        /// <summary>
        /// CPU-based alpha composite fallback for systems without DirectX 12 compute (Linux/Wine).
        /// Uses pre-loaded CpuLayerData from phase 1 — no extra file I/O needed.
        /// </summary>
        private static Bitmap MergeLayersCpuFallback(CpuLayerData[] cpuLayers, int width, int height, System.Collections.Generic.List<System.Numerics.Vector4> tints, bool preserveBaseAlpha = false) {
            byte[] output = new byte[width * height * 4];

            for (int layerIdx = 0; layerIdx < cpuLayers.Length; layerIdx++) {
                var layer = cpuLayers[layerIdx];
                if (layer.Pixels == null || layer.Width <= 0 || layer.Height <= 0) continue;

                float tR = 1f, tG = 1f, tB = 1f, tA = 1f;
                if (tints != null && layerIdx < tints.Count) {
                    tR = tints[layerIdx].X; tG = tints[layerIdx].Y; tB = tints[layerIdx].Z; tA = tints[layerIdx].W;
                }

                float scaleX = (float)layer.Width / width;
                float scaleY = (float)layer.Height / height;

                System.Threading.Tasks.Parallel.For(0, height, y => {
                    int srcY = Math.Clamp((int)(y * scaleY), 0, layer.Height - 1);
                    for (int x = 0; x < width; x++) {
                        int srcX = Math.Clamp((int)(x * scaleX), 0, layer.Width - 1);

                        int destIdx = (y * width + x) * 4;
                        int srcIdx = (srcY * layer.Width + srcX) * 4;

                        // BGRA byte order
                        float topB = (layer.Pixels[srcIdx] / 255f) * tB;
                        float topG = (layer.Pixels[srcIdx + 1] / 255f) * tG;
                        float topR = (layer.Pixels[srcIdx + 2] / 255f) * tR;
                        float topA = (layer.Pixels[srcIdx + 3] / 255f) * tA;

                        if (topA <= 0f) continue;

                        float accB = output[destIdx] / 255f;
                        float accG = output[destIdx + 1] / 255f;
                        float accR = output[destIdx + 2] / 255f;
                        float accA = output[destIdx + 3] / 255f;

                        float outA = topA + accA * (1f - topA);
                        if (outA > 0f) {
                            float outR = (topR * topA + accR * accA * (1f - topA)) / outA;
                            float outG = (topG * topA + accG * accA * (1f - topA)) / outA;
                            float outB = (topB * topA + accB * accA * (1f - topA)) / outA;

                            output[destIdx] = (byte)Math.Clamp((int)(outB * 255f + 0.5f), 0, 255);
                            output[destIdx + 1] = (byte)Math.Clamp((int)(outG * 255f + 0.5f), 0, 255);
                            output[destIdx + 2] = (byte)Math.Clamp((int)(outR * 255f + 0.5f), 0, 255);
                            output[destIdx + 3] = (byte)Math.Clamp((int)(outA * 255f + 0.5f), 0, 255);
                        }
                    }
                });
            }

            // Restore base layer's alpha if requested
            if (preserveBaseAlpha && cpuLayers.Length > 0 && cpuLayers[0].Pixels != null) {
                var baseLayer = cpuLayers[0];
                float scaleX = (float)baseLayer.Width / width;
                float scaleY = (float)baseLayer.Height / height;
                System.Threading.Tasks.Parallel.For(0, height, y => {
                    int srcY = Math.Clamp((int)(y * scaleY), 0, baseLayer.Height - 1);
                    for (int x = 0; x < width; x++) {
                        int srcX = Math.Clamp((int)(x * scaleX), 0, baseLayer.Width - 1);
                        int destIdx = (y * width + x) * 4;
                        int srcIdx = (srcY * baseLayer.Width + srcX) * 4;
                        output[destIdx + 3] = baseLayer.Pixels[srcIdx + 3]; // BGRA: alpha is byte 3
                    }
                });
            }

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(output, 0, bmpData.Scan0, output.Length);
            result.UnlockBits(bmpData);
            return result;
        }

        public static Bitmap MergeAlphaToRGBGpuFromPaths(string rgbPath, string alphaPath, int destWidth, int destHeight, bool invertAlpha) {
            if (destWidth <= 0 || destHeight <= 0) {
                System.Diagnostics.Debug.WriteLine($"[MergeAlphaToRGBGpuFromPaths] Invalid dimensions: {destWidth}x{destHeight}. Clearing cache and returning 1x1 fallback.");
                ClearCache();
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }
            var device = GraphicsDevice.GetDefault();
            int totalPixels = destWidth * destHeight;

            var cpuRgb = LoadPixelsCpu(rgbPath);
            var cpuAlpha = LoadPixelsCpu(alphaPath);

            lock (_gpuLock) {
                try {
                    var rgbTex = UploadToVram(device, cpuRgb);
                    var alphaTex = UploadToVram(device, cpuAlpha);

                    if (_cachedPing == null || _cachedWidth != destWidth || _cachedHeight != destHeight) {
                        _cachedPing?.Dispose();
                        _cachedPong?.Dispose();
                        _cachedPing = device.AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                        _cachedPong = device.AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                        _cachedWidth = destWidth;
                        _cachedHeight = destHeight;
                        _cachedResultBuffer = new byte[totalPixels * 4];
                    }
                    var output = _cachedPing;

                    using (var context = device.CreateComputeContext()) {
                        context.For(totalPixels, new MergeAlphaToRGBScalingShader(
                            rgbTex.Texture, alphaTex.Texture, output, 
                            destWidth, destHeight, 
                            alphaTex.Width, alphaTex.Height, 
                            invertAlpha ? 1 : 0));
                    }

                    if (!rgbTex.IsCached && rgbTex.Texture != null) rgbTex.Texture.Dispose();
                    if (!alphaTex.IsCached && alphaTex.Texture != null) alphaTex.Texture.Dispose();

                    output.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
                    AuditVram();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"[MergeAlphaToRGBGpuFromPaths] GPU failed, CPU fallback: {ex.Message}");
                    if (_cachedResultBuffer == null || _cachedResultBuffer.Length < totalPixels * 4) {
                        _cachedResultBuffer = new byte[totalPixels * 4];
                    }
                    if (cpuRgb.Pixels != null && cpuAlpha.Pixels != null) {
                        float sX = (float)cpuRgb.Width / destWidth;
                        float sY = (float)cpuRgb.Height / destHeight;
                        float aX = (float)cpuAlpha.Width / destWidth;
                        float aY = (float)cpuAlpha.Height / destHeight;
                        
                        System.Threading.Tasks.Parallel.For(0, destHeight, y => {
                            int rY = Math.Clamp((int)(y * sY), 0, cpuRgb.Height - 1);
                            int alY = Math.Clamp((int)(y * aY), 0, cpuAlpha.Height - 1);
                            for (int x = 0; x < destWidth; x++) {
                                int rX = Math.Clamp((int)(x * sX), 0, cpuRgb.Width - 1);
                                int alX = Math.Clamp((int)(x * aX), 0, cpuAlpha.Width - 1);
                                
                                int destIdx = (y * destWidth + x) * 4;
                                int rgbIdx = (rY * cpuRgb.Width + rX) * 4;
                                int alphaIdx = (alY * cpuAlpha.Width + alX) * 4;
                                
                                _cachedResultBuffer[destIdx] = cpuRgb.Pixels[rgbIdx];     // B
                                _cachedResultBuffer[destIdx+1] = cpuRgb.Pixels[rgbIdx+1]; // G
                                _cachedResultBuffer[destIdx+2] = cpuRgb.Pixels[rgbIdx+2]; // R
                                
                                byte alphaVal = cpuAlpha.Pixels[alphaIdx + 2]; // Red channel as Alpha
                                if (invertAlpha) alphaVal = (byte)(255 - alphaVal);
                                _cachedResultBuffer[destIdx+3] = alphaVal;
                            }
                        });
                    }
                }
            }

            Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            var bmpDataResult = result.LockBits(new Rectangle(0, 0, destWidth, destHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_cachedResultBuffer, 0, bmpDataResult.Scan0, _cachedResultBuffer.Length);
            result.UnlockBits(bmpDataResult);

            return result;
        }

        public static Bitmap MergeAlphaChannelToRGBGpuFromPaths(string rgbPath, string alphaPath, int destWidth, int destHeight, bool invertAlpha) {
            if (destWidth <= 0 || destHeight <= 0) {
                System.Diagnostics.Debug.WriteLine($"[MergeAlphaChannelToRGBGpuFromPaths] Invalid dimensions: {destWidth}x{destHeight}. Clearing cache and returning 1x1 fallback.");
                ClearCache();
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }
            var device = GraphicsDevice.GetDefault();
            int totalPixels = destWidth * destHeight;

            var cpuRgb = LoadPixelsCpu(rgbPath);
            var cpuAlpha = LoadPixelsCpu(alphaPath);

            lock (_gpuLock) {
                try {
                    var rgbTex = UploadToVram(device, cpuRgb);
                    var alphaTex = UploadToVram(device, cpuAlpha);

                    if (_cachedPing == null || _cachedWidth != destWidth || _cachedHeight != destHeight) {
                        _cachedPing?.Dispose();
                        _cachedPong?.Dispose();
                        _cachedPing = device.AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                        _cachedPong = device.AllocateReadWriteTexture2D<Bgra32, float4>(destWidth, destHeight);
                        _cachedWidth = destWidth;
                        _cachedHeight = destHeight;
                        _cachedResultBuffer = new byte[totalPixels * 4];
                    }
                    var output = _cachedPing;

                    using (var context = device.CreateComputeContext()) {
                        context.For(totalPixels, new MergeAlphaChannelToRGBScalingShader(
                            rgbTex.Texture, alphaTex.Texture, output, 
                            destWidth, destHeight, 
                            alphaTex.Width, alphaTex.Height, 
                            invertAlpha ? 1 : 0));
                    }

                    if (!rgbTex.IsCached && rgbTex.Texture != null) rgbTex.Texture.Dispose();
                    if (!alphaTex.IsCached && alphaTex.Texture != null) alphaTex.Texture.Dispose();

                    output.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(_cachedResultBuffer));
                    AuditVram();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"[MergeAlphaChannelToRGBGpuFromPaths] GPU failed, CPU fallback: {ex.Message}");
                    if (_cachedResultBuffer == null || _cachedResultBuffer.Length < totalPixels * 4) {
                        _cachedResultBuffer = new byte[totalPixels * 4];
                    }
                    if (cpuRgb.Pixels != null && cpuAlpha.Pixels != null) {
                        float sX = (float)cpuRgb.Width / destWidth;
                        float sY = (float)cpuRgb.Height / destHeight;
                        float aX = (float)cpuAlpha.Width / destWidth;
                        float aY = (float)cpuAlpha.Height / destHeight;
                        
                        System.Threading.Tasks.Parallel.For(0, destHeight, y => {
                            int rY = Math.Clamp((int)(y * sY), 0, cpuRgb.Height - 1);
                            int alY = Math.Clamp((int)(y * aY), 0, cpuAlpha.Height - 1);
                            for (int x = 0; x < destWidth; x++) {
                                int rX = Math.Clamp((int)(x * sX), 0, cpuRgb.Width - 1);
                                int alX = Math.Clamp((int)(x * aX), 0, cpuAlpha.Width - 1);
                                
                                int destIdx = (y * destWidth + x) * 4;
                                int rgbIdx = (rY * cpuRgb.Width + rX) * 4;
                                int alphaIdx = (alY * cpuAlpha.Width + alX) * 4;
                                
                                _cachedResultBuffer[destIdx] = cpuRgb.Pixels[rgbIdx];     // B
                                _cachedResultBuffer[destIdx+1] = cpuRgb.Pixels[rgbIdx+1]; // G
                                _cachedResultBuffer[destIdx+2] = cpuRgb.Pixels[rgbIdx+2]; // R
                                
                                byte alphaVal = cpuAlpha.Pixels[alphaIdx + 3]; // Alpha channel as Alpha
                                if (invertAlpha) alphaVal = (byte)(255 - alphaVal);
                                _cachedResultBuffer[destIdx+3] = alphaVal;
                            }
                        });
                    }
                }
            }

            Bitmap result = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            var bmpDataResult = result.LockBits(new Rectangle(0, 0, destWidth, destHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_cachedResultBuffer, 0, bmpDataResult.Scan0, _cachedResultBuffer.Length);
            result.UnlockBits(bmpDataResult);

            return result;
        }

        public static (int Width, int Height) GetImageDimensions(string path) {
            if (string.IsNullOrEmpty(path)) return (0, 0);
            if (path.StartsWith("memory://", StringComparison.OrdinalIgnoreCase)) {
                if (TexIO.VirtualFileSystem.TryGetValue(path, out var memFile)) {
                    return (memFile.Width, memFile.Height);
                }
            } else if (path.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)) {
                try {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                        var scratch = global::Penumbra.LTCImport.Textures.PenumbraTexFileParser.Parse(stream);
                        return (scratch.Meta.Width, scratch.Meta.Height);
                    }
                } catch {}
            } else {
                try {
                    var cachedDims = GetDimensions(path);
                    if (cachedDims.Width > 0 && cachedDims.Height > 0) {
                        return cachedDims;
                    }
                    var info = SixLabors.ImageSharp.Image.Identify(path);
                    if (info != null) {
                        return (info.Width, info.Height);
                    }
                } catch {}
            }
            try {
                using (var bitmap = TexIO.ResolveBitmap(path)) {
                    return (bitmap.Width, bitmap.Height);
                }
            } catch {
                return (0, 0);
            }
        }

        public static Bitmap MergeMultipleImagesGpu(Bitmap[] layers, int width, int height, System.Collections.Generic.List<System.Numerics.Vector4> tints = null) {
            var device = GraphicsDevice.GetDefault();
            int totalPixels = width * height;

            using var ping = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            using var pong = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);
            
            if (layers.Length > 0 && layers[0] != null) {
                Bitmap layer0 = layers[0];
                Bitmap safe0 = layer0.PixelFormat == PixelFormat.Format32bppArgb ? layer0 : layer0.Clone(new Rectangle(0, 0, layer0.Width, layer0.Height), PixelFormat.Format32bppArgb);
                
                var bmpData0 = safe0.LockBits(new Rectangle(0, 0, safe0.Width, safe0.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                unsafe {
                    var span = new ReadOnlySpan<Bgra32>((void*)bmpData0.Scan0, safe0.Width * safe0.Height);
                    ping.CopyFrom(span);
                }
                safe0.UnlockBits(bmpData0);
                
                if (safe0 != layer0) safe0.Dispose();
            } else {
                byte[] blankPixels = new byte[totalPixels * 4];
                ping.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(blankPixels));
            }
            
            bool isPing = true;

            for (int i = 1; i < layers.Length; i++) {
                Bitmap topLayer = layers[i];
                if (topLayer == null) continue;
                
                Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);
                
                using (var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height)) {
                    var bmpDataTop = safeTop.LockBits(new Rectangle(0, 0, safeTop.Width, safeTop.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        var span = new ReadOnlySpan<Bgra32>((void*)bmpDataTop.Scan0, safeTop.Width * safeTop.Height);
                        gpuTop.CopyFrom(span);
                    }
                    safeTop.UnlockBits(bmpDataTop);
                    if (safeTop != topLayer) safeTop.Dispose();

                    float4 tint = tints != null && i < tints.Count ? new float4(tints[i].X, tints[i].Y, tints[i].Z, tints[i].W) : new float4(1,1,1,1);
                    bool hasTint = (tint.X != 1.0f || tint.Y != 1.0f || tint.Z != 1.0f || tint.W != 1.0f);
                    if (!hasTint) {
                        if (isPing) {
                            device.For(totalPixels, new MergeImagesPingPongShader(ping, gpuTop, pong, width, height, topLayer.Width, topLayer.Height));
                        } else {
                            device.For(totalPixels, new MergeImagesPingPongShader(pong, gpuTop, ping, width, height, topLayer.Width, topLayer.Height));
                        }
                    } else {
                        if (isPing) {
                            device.For(totalPixels, new MergeImagesPingPongTintedShader(ping, gpuTop, pong, width, height, topLayer.Width, topLayer.Height, tint));
                        } else {
                            device.For(totalPixels, new MergeImagesPingPongTintedShader(pong, gpuTop, ping, width, height, topLayer.Width, topLayer.Height, tint));
                        }
                    }
                }
                isPing = !isPing;
            }

            byte[] resultPixels = new byte[totalPixels * 4];
            if (isPing) {
                ping.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));
            } else {
                pong.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));
            }

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpDataResult = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpDataResult.Scan0, resultPixels.Length);
            result.UnlockBits(bmpDataResult);

            return result;
        }
        public static Bitmap ConvertBaseToDawntrailSkinMultiGpu(Bitmap image) {
            try {
                int width = image.Width;
                int height = image.Height;

                Bitmap safeImage = image.PixelFormat == PixelFormat.Format32bppArgb ? image : image.Clone(new Rectangle(0, 0, width, height), PixelFormat.Format32bppArgb);

                using (var device = GraphicsDevice.GetDefault()) {
                    using (var lockImage = new LockBitmap(safeImage)) {
                        lockImage.LockBits();
                        byte[] imageBytes = lockImage.Pixels;
                        lockImage.UnlockBits();
                        
                        if (safeImage != image) safeImage.Dispose();
                        
                        using (var inputTex = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height)) {
                            inputTex.CopyFrom(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, Bgra32>(imageBytes));

                            using (var outputTex = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height)) {
                                using (var context = device.CreateComputeContext()) {
                                    context.For(width * height, new DawntrailSkinMultiShader(inputTex, outputTex, width, height));
                                }

                                byte[] resultPixels = new byte[width * height * 4];
                                outputTex.CopyTo(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

                                Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                                var bd = result.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                                System.Runtime.InteropServices.Marshal.Copy(resultPixels, 0, bd.Scan0, resultPixels.Length);
                                result.UnlockBits(bd);
                                return result;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[GPU] ConvertBaseToDawntrailSkinMultiGpu failed: {ex.Message}");
                return ImageManipulation.ConvertBaseToDawntrailSkinMulti(image);
            }
        }

        public static Bitmap LayerImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            Bitmap safeBottom = bottomLayer.PixelFormat == PixelFormat.Format32bppArgb ? bottomLayer : bottomLayer.Clone(new Rectangle(0, 0, bottomLayer.Width, bottomLayer.Height), PixelFormat.Format32bppArgb);
            Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(safeBottom)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(safeTop)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            if (safeBottom != bottomLayer) safeBottom.Dispose();
            if (safeTop != topLayer) safeTop.Dispose();

            using var gpuBottom = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuBottom.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bottomPixels));
            gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(topPixels));

            device.For(totalPixels, new LayerImagesShader(gpuBottom, gpuTop, gpuOutput, width, height, topLayer.Width, topLayer.Height));

            byte[] resultPixels = new byte[totalPixels * 4];
            gpuOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
            result.UnlockBits(bmpData);

            return result;
        }

        public static Bitmap MaxImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            Bitmap safeBottom = bottomLayer.PixelFormat == PixelFormat.Format32bppArgb ? bottomLayer : bottomLayer.Clone(new Rectangle(0, 0, bottomLayer.Width, bottomLayer.Height), PixelFormat.Format32bppArgb);
            Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(safeBottom)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(safeTop)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            if (safeBottom != bottomLayer) safeBottom.Dispose();
            if (safeTop != topLayer) safeTop.Dispose();

            using var gpuBottom = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuBottom.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bottomPixels));
            gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(topPixels));

            device.For(totalPixels, new MaxImagesShader(gpuBottom, gpuTop, gpuOutput, width, height, topLayer.Width, topLayer.Height));

            byte[] resultPixels = new byte[totalPixels * 4];
            gpuOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
            result.UnlockBits(bmpData);

            return result;
        }

        public static Bitmap MergeImagesGpu(Bitmap bottomLayer, Bitmap topLayer) {
            var device = GraphicsDevice.GetDefault();
            int width = bottomLayer.Width;
            int height = bottomLayer.Height;
            int totalPixels = width * height;

            Bitmap safeBottom = bottomLayer.PixelFormat == PixelFormat.Format32bppArgb ? bottomLayer : bottomLayer.Clone(new Rectangle(0, 0, bottomLayer.Width, bottomLayer.Height), PixelFormat.Format32bppArgb);
            Bitmap safeTop = topLayer.PixelFormat == PixelFormat.Format32bppArgb ? topLayer : topLayer.Clone(new Rectangle(0, 0, topLayer.Width, topLayer.Height), PixelFormat.Format32bppArgb);

            byte[] bottomPixels;
            using (var lockBottom = new LockBitmap(safeBottom)) {
                lockBottom.LockBits();
                bottomPixels = new byte[lockBottom.Pixels.Length];
                Array.Copy(lockBottom.Pixels, bottomPixels, bottomPixels.Length);
            }

            byte[] topPixels;
            using (var lockTop = new LockBitmap(safeTop)) {
                lockTop.LockBits();
                topPixels = new byte[lockTop.Pixels.Length];
                Array.Copy(lockTop.Pixels, topPixels, topPixels.Length);
            }

            if (safeBottom != bottomLayer) safeBottom.Dispose();
            if (safeTop != topLayer) safeTop.Dispose();

            using var gpuBottom = device.AllocateReadOnlyTexture2D<Bgra32, float4>(width, height);
            using var gpuTop = device.AllocateReadOnlyTexture2D<Bgra32, float4>(topLayer.Width, topLayer.Height);
            using var gpuOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(width, height);

            gpuBottom.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(bottomPixels));
            gpuTop.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(topPixels));

            device.For(totalPixels, new MergeImagesShader(gpuBottom, gpuTop, gpuOutput, width, height, topLayer.Width, topLayer.Height));

            byte[] resultPixels = new byte[totalPixels * 4];
            gpuOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(resultPixels));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, resultPixels.Length);
            result.UnlockBits(bmpData);

            return result;
        }

        /// <summary>
        /// GPU-accelerated frame compositing for animated layers.
        /// Stamps a frame image onto a base texture at a specific pixel position with opacity,
        /// and returns raw BGRA pixel bytes suitable for direct .tex file writing.
        /// GPU textures are cached and reused across consecutive calls for maximum throughput.
        /// </summary>
        private static ReadOnlyTexture2D<Bgra32, float4> _stampBase;
        private static ReadOnlyTexture2D<Bgra32, float4> _stampFrame;
        private static ReadWriteTexture2D<Bgra32, float4> _stampOutput;
        private static int _stampBaseW, _stampBaseH, _stampFrameW, _stampFrameH;
        private static bool _stampBaseUploaded;
        private static readonly object _stampLock = new object();

        public static byte[] CompositeFrameGpu(
            byte[] basePixels, int baseW, int baseH,
            byte[] framePixels, int frameW, int frameH,
            int stampX, int stampY, int stampW, int stampH,
            float opacity)
        {
            lock (_stampLock)
            {
                var device = GraphicsDevice.GetDefault();
                int totalPixels = baseW * baseH;

                // Allocate/reallocate base + output only when dimensions change
                if (_stampBase == null || _stampBaseW != baseW || _stampBaseH != baseH)
                {
                    _stampBase?.Dispose();
                    _stampOutput?.Dispose();
                    _stampBase = device.AllocateReadOnlyTexture2D<Bgra32, float4>(baseW, baseH);
                    _stampOutput = device.AllocateReadWriteTexture2D<Bgra32, float4>(baseW, baseH);
                    _stampBaseW = baseW;
                    _stampBaseH = baseH;
                    _stampBaseUploaded = false;
                }

                // Upload base only once (it's the same for every frame in a sequence)
                if (!_stampBaseUploaded)
                {
                    _stampBase.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(basePixels));
                    _stampBaseUploaded = true;
                }

                // Allocate/reallocate frame texture only when frame dimensions change
                if (_stampFrame == null || _stampFrameW != frameW || _stampFrameH != frameH)
                {
                    _stampFrame?.Dispose();
                    _stampFrame = device.AllocateReadOnlyTexture2D<Bgra32, float4>(frameW, frameH);
                    _stampFrameW = frameW;
                    _stampFrameH = frameH;
                }

                // Upload frame pixels
                _stampFrame.CopyFrom(MemoryMarshal.Cast<byte, Bgra32>(framePixels));

                // Dispatch shader
                device.For(totalPixels, new StampFrameShader(
                    _stampBase, _stampFrame, _stampOutput,
                    baseW, baseH, frameW, frameH,
                    stampX, stampY, stampW, stampH, opacity));

                // Read back
                byte[] result = new byte[totalPixels * 4];
                _stampOutput.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(result));
                return result;
            }
        }

        /// <summary>
        /// Call after a batch of CompositeFrameGpu calls to release cached GPU resources.
        /// </summary>
        public static void ReleaseStampResources()
        {
            lock (_stampLock)
            {
                _stampBase?.Dispose(); _stampBase = null;
                _stampFrame?.Dispose(); _stampFrame = null;
                _stampOutput?.Dispose(); _stampOutput = null;
                _stampBaseUploaded = false;
            }
        }
    }

    /// <summary>
    /// GPU shader that stamps a source frame onto a base texture at a specific pixel position.
    /// Supports opacity and bilinear-ish nearest-neighbor scaling of the frame to the stamp region.
    /// </summary>
    [ThreadGroupSize(1024, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct StampFrameShader : IComputeShader {
        public readonly ReadOnlyTexture2D<Bgra32, float4> Base;
        public readonly ReadOnlyTexture2D<Bgra32, float4> Frame;
        public readonly ReadWriteTexture2D<Bgra32, float4> Output;
        public readonly int BaseW;
        public readonly int BaseH;
        public readonly int FrameW;
        public readonly int FrameH;
        public readonly int StampX;
        public readonly int StampY;
        public readonly int StampW;
        public readonly int StampH;
        public readonly float Opacity;

        public StampFrameShader(
            ReadOnlyTexture2D<Bgra32, float4> baseT,
            ReadOnlyTexture2D<Bgra32, float4> frame,
            ReadWriteTexture2D<Bgra32, float4> output,
            int baseW, int baseH, int frameW, int frameH,
            int stampX, int stampY, int stampW, int stampH,
            float opacity) {
            Base = baseT;
            Frame = frame;
            Output = output;
            BaseW = baseW;
            BaseH = baseH;
            FrameW = frameW;
            FrameH = frameH;
            StampX = stampX;
            StampY = stampY;
            StampW = stampW;
            StampH = stampH;
            Opacity = opacity;
        }

        public void Execute() {
            int idx = ThreadIds.X;
            if (idx >= BaseW * BaseH) return;

            int y = idx / BaseW;
            int x = idx % BaseW;
            int2 pos = new int2(x, y);

            float4 basePixel = Base[pos];

            // Check if this pixel is inside the stamp region
            if (x >= StampX && x < StampX + StampW && y >= StampY && y < StampY + StampH) {
                // Map to frame coordinates
                float u = (float)(x - StampX) / (float)StampW;
                float v = (float)(y - StampY) / (float)StampH;
                int srcX = Hlsl.Clamp((int)(u * FrameW), 0, FrameW - 1);
                int srcY = Hlsl.Clamp((int)(v * FrameH), 0, FrameH - 1);

                float4 framePixel = Frame[new int2(srcX, srcY)];
                float topA = framePixel.W * Opacity;

                // Alpha composite: frame over base
                float outB = framePixel.X * topA + basePixel.X * (1.0f - topA);
                float outG = framePixel.Y * topA + basePixel.Y * (1.0f - topA);
                float outR = framePixel.Z * topA + basePixel.Z * (1.0f - topA);
                float outA = basePixel.W; // Preserve base alpha

                Output[pos] = new float4(outB, outG, outR, outA);
            } else {
                // Outside stamp region: pass through base
                Output[pos] = basePixel;
            }
        }
    }
}

