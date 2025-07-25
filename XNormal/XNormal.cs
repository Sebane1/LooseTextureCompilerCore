using FFXIVLooseTextureCompiler.ImageProcessing;
using LooseTextureCompilerCore;
using System.Diagnostics;

namespace FFXIVLooseTextureCompiler
{
    public class XNormal
    {
        private static string xmlFile = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
            "<Settings xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" Version=\"3.19.3\">\r\n" +
            "  <HighPolyModel DefaultMeshScale=\"100.000000\">\r\n" +
            "    <Mesh Visible=\"true\" Scale=\"100.000000\" IgnorePerVertexColor=\"true\" AverageNormals=\"AverageNormals\" BaseTexIsTSNM=\"{4}\" File=\"{0}\" PositionOffset=\"0.0000;0.0000;0.0000\" BaseTex=\"{1}\"/>\r\n" +
            "    <Mesh Visible=\"false\" Scale=\"100.000000\" IgnorePerVertexColor=\"true\" AverageNormals=\"AverageNormals\" BaseTexIsTSNM=\"false\" File=\"{2}\" PositionOffset=\"0.0000;0.0000;0.0000\"/>\r\n" +
            "  </HighPolyModel>\r\n" +
            "  <LowPolyModel DefaultMeshScale=\"100.000000\">\r\n" +
            "    <Mesh Visible=\"true\" File=\"{2}\" AverageNormals=\"AverageNormals\" MaxRayDistanceFront=\"100.000000\" MaxRayDistanceBack=\"100.000000\" UseCage=\"false\" NormapMapType=\"Tangent-space\" UsePerVertexColors=\"true\" UseFresnel=\"false\" FresnelRefractiveIndex=\"1.330000\" ReflectHDRMult=\"1.000000\" VectorDisplacementTS=\"false\" VDMSwizzleX=\"X+\" VDMSwizzleY=\"Y+\" VDMSwizzleZ=\"Z+\" BatchProtect=\"false\" CastShadows=\"true\" ReceiveShadows=\"true\" BackfaceCull=\"true\" NMSwizzleX=\"X+\" NMSwizzleY=\"Y+\" NMSwizzleZ=\"Z+\" HighpolyNormalsOverrideTangentSpace=\"true\" TransparencyMode=\"None\" AlphaTestValue=\"127\" Matte=\"false\" Scale=\"100.000000\" MatchUVs=\"false\" UOffset=\"0.000000\" VOffset=\"0.000000\" PositionOffset=\"0.0000;0.0000;0.0000\"/>\r\n" +
            "  </LowPolyModel>\r\n" +
            "  <GenerateMaps GenNormals=\"false\" Width=\"{5}\" Height=\"{6}\" EdgePadding=\"8\" BucketSize=\"32\" TangentSpace=\"true\" ClosestIfFails=\"{7}\" DiscardRayBackFacesHits=\"true\" File=\"{3}\" SwizzleX=\"X+\" SwizzleY=\"Y+\" SwizzleZ=\"Z+\" AA=\"4\" BakeHighpolyBaseTex=\"true\" BakeHighpolyBaseTextureDrawObjectIDIfNoTexture=\"false\" GenHeights=\"false\" HeightTonemap=\"Interactive\" HeightTonemapMin=\"-100.000000\" HeightTonemapMax=\"100.000000\" GenAO=\"false\" AORaysPerSample=\"128\" AODistribution=\"Uniform\" AOConeAngle=\"162.000000\" AOBias=\"0.080000\" AOAllowPureOccluded=\"true\" AOLimitRayDistance=\"false\" AOAttenConstant=\"1.000000\" AOAttenLinear=\"0.000000\" AOAttenCuadratic=\"0.000000\" AOJitter=\"false\" AOIgnoreBackfaceHits=\"false\" GenBent=\"false\" BentRaysPerSample=\"128\" BentConeAngle=\"162.000000\" BentBias=\"0.080000\" BentTangentSpace=\"false\" BentLimitRayDistance=\"false\" BentJitter=\"false\" BentDistribution=\"Uniform\" BentSwizzleX=\"X+\" BentSwizzleY=\"Y+\" BentSwizzleZ=\"Z+\" GenPRT=\"false\" PRTRaysPerSample=\"128\" PRTConeAngle=\"179.500000\" PRTBias=\"0.080000\" PRTLimitRayDistance=\"false\" PRTJitter=\"false\" PRTNormalize=\"true\" PRTThreshold=\"0.005000\" GenProximity=\"false\" ProximityRaysPerSample=\"128\" ProximityConeAngle=\"80.000000\" ProximityLimitRayDistance=\"true\" ProximityFlipNormals=\"false\" ProximityFlipValue=\"false\" GenConvexity=\"false\" ConvexityScale=\"1.000000\" GenThickness=\"false\" GenCavity=\"false\" CavityRaysPerSample=\"128\" CavityJitter=\"false\" CavitySearchRadius=\"0.500000\" CavityContrast=\"1.250000\" CavitySteps=\"4\" GenWireRays=\"false\" RenderRayFails=\"true\" RenderWireframe=\"true\" GenDirections=\"false\" DirectionsTS=\"false\" DirectionsSwizzleX=\"X+\" DirectionsSwizzleY=\"Y+\" DirectionsSwizzleZ=\"Z+\" DirectionsTonemap=\"Interactive\" DirectionsTonemapMin=\"false\" DirectionsTonemapMax=\"false\" GenRadiosityNormals=\"false\" RadiosityNormalsRaysPerSample=\"128\" RadiosityNormalsDistribution=\"Uniform\" RadiosityNormalsConeAngle=\"162.000000\" RadiosityNormalsBias=\"0.080000\" RadiosityNormalsLimitRayDistance=\"false\" RadiosityNormalsAttenConstant=\"1.000000\" RadiosityNormalsAttenLinear=\"0.000000\" RadiosityNormalsAttenCuadratic=\"0.000000\" RadiosityNormalsJitter=\"false\" RadiosityNormalsContrast=\"1.000000\" RadiosityNormalsEncodeAO=\"true\" RadiosityNormalsCoordSys=\"AliB\" RadiosityNormalsAllowPureOcclusion=\"false\" BakeHighpolyVCols=\"false\" GenCurv=\"false\" CurvRaysPerSample=\"128\" CurvBias=\"0.000100\" CurvConeAngle=\"162.000000\" CurvJitter=\"false\" CurvSearchDistance=\"1.000000\" CurvTonemap=\"3Col\" CurvDistribution=\"Cosine\" CurvAlgorithm=\"Average\" CurvSmoothing=\"true\" GenDerivNM=\"false\" GenTranslu=\"false\" TransluRaysPerSample=\"128\" TransluDistribution=\"Uniform\" TransluConeAngle=\"162.000000\" TransluBias=\"0.000500\" TransluDist=\"1.000000\" TransluJitter=\"false\">\r\n" +
            "    <NMBackgroundColor R=\"128\" G=\"128\" B=\"255\"/>\r\n" +
            "    <BakeHighpolyBaseTextureNoTexCol R=\"255\" G=\"0\" B=\"0\"/>\r\n" +
            "    <BakeHighpolyBaseTextureBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <HMBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <AOOccludedColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <AOUnoccludedColor R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <AOBackgroundColor R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <BentBackgroundColor R=\"127\" G=\"127\" B=\"255\"/>\r\n" +
            "    <PRTBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <ProximityBackgroundColor R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <ConvexityBackgroundColor R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <CavityBackgroundColor R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <RenderWireframeCol R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <RenderCWCol R=\"0\" G=\"0\" B=\"255\"/>\r\n" +
            "    <RenderSeamCol R=\"0\" G=\"255\" B=\"0\"/>\r\n" +
            "    <RenderRayFailsCol R=\"255\" G=\"0\" B=\"0\"/>\r\n" +
            "    <RenderWireframeBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <VDMBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <RadNMBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <BakeHighpolyVColsBackgroundCol R=\"255\" G=\"255\" B=\"255\"/>\r\n" +
            "    <CurvBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "    <DerivNMBackgroundColor R=\"127\" G=\"127\" B=\"0\"/>\r\n" +
            "    <TransluBackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n" +
            "  </GenerateMaps>\r\n" +
            "  <Detail Scale=\"0.500000\" Method=\"4Samples\"/>\r\n" +
            "  <Viewer3D ShowGrid=\"true\" ShowWireframe=\"false\" ShowTangents=\"false\" ShowNormals=\"false\" ShowBlockers=\"false\" MaxTessellationLevel=\"0\" LightIntensity=\"1.000000\" LightIndirectIntensity=\"0.000000\" Exposure=\"0.180000\" HDRThreshold=\"0.900000\" UseGlow=\"true\" GlowIntensity=\"1.000000\" SSAOEnabled=\"false\" SSAOBright=\"1.100000\" SSAOContrast=\"1.000000\" SSAOAtten=\"1.000000\" SSAORadius=\"0.250000\" SSAOBlurRadius=\"2.000000\" ParallaxStrength=\"0.000000\" ShowHighpolys=\"true\" ShowAO=\"false\" CageOpacity=\"0.700000\" DiffuseGIIntensity=\"1.000000\" CastShadows=\"false\" ShadowBias=\"0.100000\" ShadowArea=\"0.250000\" AxisScl=\"0.040000\" CameraOrbitDistance=\"0.500000\" CameraOrbitAutoCenter=\"true\" ShowStarfield=\"false\">\r\n    <LightAmbientColor R=\"33\" G=\"33\" B=\"33\"/>\r\n    <LightDiffuseColor R=\"229\" G=\"229\" B=\"229\"/>\r\n    <LightSpecularColor R=\"255\" G=\"255\" B=\"255\"/>\r\n    <LightSecondaryColor R=\"0\" G=\"0\" B=\"0\"/>\r\n    <LightTertiaryColor R=\"0\" G=\"0\" B=\"0\"/>\r\n    <BackgroundColor R=\"0\" G=\"0\" B=\"0\"/>\r\n    <GridColor R=\"180\" G=\"180\" B=\"220\"/>\r\n    <CageColor R=\"76\" G=\"76\" B=\"76\"/>\r\n    <CameraRotation e11=\"1.000000\" e12=\"0.000000\" e13=\"0.000000\" e21=\"0.000000\" e22=\"1.000000\" e23=\"0.000000\" e31=\"0.000000\" e32=\"0.000000\" e33=\"1.000000\"/>\r\n" +
            "    <CameraPosition x=\"0.000000\" y=\"1.000000\" z=\"0.000000\"/>\r\n" +
            "    <LightPosition x=\"0.000000\" y=\"2.000000\" z=\"5.000000\"/>\r\n" +
            "  </Viewer3D>\r\n" +
            "</Settings>\r\n";
        private const string xNormal = "res\\xnormal\\x64\\xNormal.exe";
        private static string xNormalPathOverride = "";
        private static string basePathOverride = "";
        private const string bibo = "res\\model\\bplus.sbm";
        private const string gen2 = "res\\model\\gen2.sbm";
        private const string gen3 = "res\\model\\gen3.sbm";
        private const string biboLegacy = "res\\model\\bplusLegacy.sbm";
        private const string gen3Legacy = "res\\model\\gen3Legacy.sbm";
        private const string vanillaLala = "res\\model\\vanillap.sbm";
        private const string otopop = "res\\model\\genp.sbm";
        private const string asymLala = "res\\model\\asymp.sbm";
        private static int outputSize = 4096;
        int count = 0;

        List<XNormalExportJob> biboToGen2Batch = new List<XNormalExportJob>();
        List<XNormalExportJob> biboToGen3Batch = new List<XNormalExportJob>();
        List<XNormalExportJob> gen3ToGen2Batch = new List<XNormalExportJob>();
        List<XNormalExportJob> gen3ToBiboBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> gen2ToBiboBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> gen2ToGen3Batch = new List<XNormalExportJob>();

        List<XNormalExportJob> otopopToVanillaLalaBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> otopopToAsymLalaBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> asymLalaToOtopopBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> asymLalaToVanillaLalaBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> vanillaLalaToOtopopBatch = new List<XNormalExportJob>();
        List<XNormalExportJob> vanillaLalaToAsymLalaBatch = new List<XNormalExportJob>();


        private static string xmlFileName = "template.xml";
        private static string userDataPath;

        public static string XmlFileName { get => xmlFileName; set => xmlFileName = value; }
        public string XNormalPathOverride { get => xNormalPathOverride; set => xNormalPathOverride = value; }
        public static string UserDataPath { get => userDataPath; set => userDataPath = value; }
        public static string LastArguments { get; set; }
        public string BasePathOverride { get => basePathOverride; set => basePathOverride = value; }
        public static int OutputSize { get => outputSize; set => outputSize = value; }

        public static void GenerateBasedOnSourceBody(string internalPath, string inputPath, string outputPath, bool isNormalMap)
        {
            if (internalPath.Contains("bibo"))
            {
                if (outputPath.Contains("gen2"))
                {
                    BiboToGen2(inputPath, outputPath);
                }
                if (outputPath.Contains("gen3"))
                {
                    BiboToGen3(inputPath, outputPath);
                }
            }
            else if (internalPath.Contains("eve") || internalPath.Contains("gen3"))
            {
                if (outputPath.Contains("gen2"))
                {
                    Gen3ToGen2(inputPath, outputPath);
                }
                if (outputPath.Contains("bibo"))
                {
                    Gen3ToBibo(inputPath, outputPath);
                }
            }
            else if (internalPath.Contains("body"))
            {
                if (outputPath.Contains("bibo"))
                {
                    Gen2ToBibo(inputPath, outputPath);
                }
                if (outputPath.Contains($"gen3"))
                {
                    Gen2ToGen3(inputPath, outputPath);
                }
            }
            else if (internalPath.Contains("skin_otopop") || internalPath.Contains("v01_c1101b0001_g"))
            {
                if (outputPath.Contains("vanilla_lala"))
                {
                    OtopopToVanillaLala(inputPath, outputPath);
                }
            }
            else if (internalPath.Contains("--c1101b0001"))
            {
                if (outputPath.Contains("otopop"))
                {
                    VanillaLalaToOtopop(inputPath, outputPath);
                }
            }
            else if (internalPath.Contains("v01_c1101b0001_b"))
            {
                if (outputPath.Contains("otopop"))
                {
                    AsymLalaToOtopop(inputPath, outputPath);
                }
                if (outputPath.Contains("vanilla_lala"))
                {
                    AsymLalaToVanillaLala(inputPath, outputPath);
                }
            }
        }

        public void AddToBatch(string internalPath, string inputPath, string outputPath, bool isNormalMap)
        {
            string environment = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            userDataPath = environment + @"\FFXIVLooseTextureCompiler\";
            if (File.Exists(inputPath))
            {
                if (internalPath.Contains("bibo"))
                {
                    if (outputPath.Contains("gen2"))
                    {
                        biboToGen2Batch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, biboLegacy, gen2,
                            count++ + ".xml", isNormalMap, 2048));
                    }
                    else if (outputPath.Contains("gen3"))
                    {
                        biboToGen3Batch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, isNormalMap ? biboLegacy : bibo,
                            isNormalMap ? gen3Legacy : gen3, count++ + ".xml", isNormalMap, outputSize, outputSize));
                    }
                }
                else if (internalPath.Contains("eve") || internalPath.Contains("gen3"))
                {
                    if (outputPath.Contains("gen2"))
                    {
                        gen3ToGen2Batch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, gen3Legacy, gen2,
                            count++ + ".xml", isNormalMap, outputSize / 2, outputSize));
                    }
                    else if (outputPath.Contains("bibo"))
                    {
                        gen3ToBiboBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, isNormalMap ? gen3Legacy : gen3,
                            isNormalMap ? biboLegacy : bibo, count++ + ".xml", isNormalMap, outputSize, outputSize));
                    }
                }
                else if (internalPath.Contains("body") || internalPath.Contains("skin_mask"))
                {
                    if (outputPath.Contains("bibo"))
                    {
                        gen2ToBiboBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, gen2, biboLegacy,
                            count++ + ".xml", isNormalMap, outputSize, outputSize));
                    }
                    else if (outputPath.Contains($"gen3"))
                    {
                        gen2ToGen3Batch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, gen2, gen3Legacy,
                            count++ + ".xml", isNormalMap, outputSize, outputSize));
                    }
                    else if (internalPath.Contains("--c1101b0001"))
                    {
                        if (outputPath.Contains("otopop"))
                        {
                            vanillaLalaToOtopopBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath,
                                vanillaLala, otopop, count++ + ".xml", isNormalMap, outputSize, outputSize));
                        }
                        else if (outputPath.Contains("asym_lala"))
                        {
                            vanillaLalaToAsymLalaBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath,
                                vanillaLala, asymLala, count++ + ".xml", isNormalMap, outputSize, outputSize));
                        }
                    }
                    else if (internalPath.Contains("v01_c1101b0001_g"))
                    {
                        if (outputPath.Contains("vanilla_lala"))
                        {
                            otopopToVanillaLalaBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, otopop,
                                vanillaLala, count++ + ".xml", isNormalMap, outputSize / 2, outputSize));
                        }
                        else if (outputPath.Contains("asym_lala"))
                        {
                            otopopToAsymLalaBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, otopop, asymLala,
                                count++ + ".xml", isNormalMap, outputSize, outputSize));
                        }
                    }
                    else if (internalPath.Contains("v01_c1101b0001_b"))
                    {
                        if (outputPath.Contains("vanilla_lala"))
                        {
                            asymLalaToVanillaLalaBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, asymLala,
                                vanillaLala, count++ + ".xml", isNormalMap, outputSize / 2));
                        }
                        else if (outputPath.Contains("otopop"))
                        {
                            asymLalaToOtopopBatch.Add(new XNormalExportJob(internalPath, inputPath, outputPath, asymLala, otopop,
                                count++ + ".xml", isNormalMap, outputSize, outputSize));
                        }
                    }
                }
            }
            else
            {
                // MessageBox.Show(inputPath + " does not exist!");
            }
        }
        public static void BiboToGen2(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, biboLegacy),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen2), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void BiboToGen3(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, bibo),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen3), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void Gen3ToGen2(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen3Legacy),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen2), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void Gen3ToBibo(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen3),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, bibo), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void Gen2ToBibo(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen2),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, biboLegacy), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void Gen2ToGen3(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen2),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, gen3Legacy), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }

        public static void OtopopToVanillaLala(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, otopop),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, vanillaLala), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }

        public static void VanillaLalaToOtopop(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, vanillaLala),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, otopop), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }

        public static void VanillaLalaToAsymLala(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, vanillaLala),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, asymLala), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void AsymLalaToVanillaLala(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, asymLala),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, vanillaLala), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }

        public static void AsymLalaToOtopop(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, asymLala),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, otopop), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }
        public static void OtopopToAsymLala(string inputImage, string outputImage)
        {
            CallXNormal(Path.Combine(GlobalPathStorage.OriginalBaseDirectory, otopop),
            Path.Combine(GlobalPathStorage.OriginalBaseDirectory, asymLala), inputImage, outputImage.Replace("_baseTexBaked", null),
            ImageManipulation.UVMapTypeClassifier(inputImage) == ImageManipulation.UVMapType.Normal);
        }

        public static void OpenXNormal()
        {
            string executable = !string.IsNullOrEmpty(xNormalPathOverride) ? xNormalPathOverride
            : Path.Combine(GlobalPathStorage.OriginalBaseDirectory, xNormal);
            Process process = Process.Start(executable);
        }
        public static void CallXNormal(string inputFBX, string outputFBX, string inputImage, string outputImage, bool isNormalMap = false, int width = 4096, int height = 4096, bool skipRgbSplit = false)
        {

            if (!skipRgbSplit)
            {
                var paths = ImageManipulation.SplitRGBAndAlphaToFile(inputImage);
                string path1 = ImageManipulation.AddSuffix(outputImage, "_rgb");
                string path2 = ImageManipulation.AddSuffix(outputImage, "_alpha");
                CallXNormalFinal(inputFBX, outputFBX, paths[0], path1, isNormalMap, width, height);
                CallXNormalFinal(inputFBX, outputFBX, paths[1], path2, false, width, height);
                TexIO.SaveBitmap(
                     ImageManipulation.MergeAlphaToRGB(
                     TexIO.ResolveBitmap(path2),
                     TexIO.ResolveBitmap(path1)),
                     outputImage);
                File.Delete(path1);
                File.Delete(path2);
            }
            else
            {
                CallXNormalFinal(inputFBX, outputFBX, inputImage, outputImage, isNormalMap, width, height);
            }
        }
        public static void CallXNormalFinal(string inputFBX, string outputFBX, string inputImage, string outputImage, bool isNormalMap = false, int width = 4096, int height = 4096)
        {
            userDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\FFXIVLooseTextureCompiler\";
            string path = Path.Combine(userDataPath, xmlFileName);
            string executable = !string.IsNullOrEmpty(xNormalPathOverride) ? xNormalPathOverride
            : Path.Combine(GlobalPathStorage.OriginalBaseDirectory, xNormal);

            if (!Directory.Exists(userDataPath))
            {
                Directory.CreateDirectory(userDataPath);
            }
            using (StreamWriter writer = new StreamWriter(path))
            {
                string inputString = CleanXmlEscapeSequences(Path.Combine(!string.IsNullOrEmpty(basePathOverride) ?
                    basePathOverride : GlobalPathStorage.OriginalBaseDirectory, inputFBX));
                string inputImageString = CleanXmlEscapeSequences(inputImage);
                string outputString = CleanXmlEscapeSequences(!string.IsNullOrEmpty(basePathOverride) ?
                    basePathOverride : Path.Combine(GlobalPathStorage.OriginalBaseDirectory, outputFBX));
                string outputImageString = CleanXmlEscapeSequences(outputImage);
                writer.Write(string.Format(xmlFile,
                    inputString,
                    inputImageString,
                    outputString,
                    outputImageString,
                    isNormalMap.ToString().ToLower(),
                    width.ToString(),
                    height.ToString(),
                    outputImage.Contains("gen2").ToString().ToLower())
                   );
            }
            ProcessStartInfo processStartInfo = new ProcessStartInfo(@"""" + executable + @"""");
            processStartInfo.UseShellExecute = true;
            processStartInfo.Arguments = @"""" + path + @"""";
            Process process = Process.Start(processStartInfo);
            process.WaitForExit();
            Thread.Sleep(100);
            string output = outputImage.Replace("_baseTexBaked", null);
            if (File.Exists(outputImage))
            {
                File.Delete(outputImage);
            }
            File.Move(ImageManipulation.AddSuffix(outputImage.Replace("_baseTexBaked", null), "_baseTexBaked"),
               output);
        }
        public void ProcessBatches()
        {
            List<XNormalExportJob> exportJobs = new List<XNormalExportJob>();
            //exportJobs.AddRange(biboToGen2Batch);
            exportJobs.AddRange(biboToGen3Batch);

            exportJobs.AddRange(gen3ToBiboBatch);
            //exportJobs.AddRange(gen3ToGen2Batch);

            exportJobs.AddRange(gen2ToBiboBatch);
            exportJobs.AddRange(gen2ToGen3Batch);

            exportJobs.AddRange(otopopToVanillaLalaBatch);
            exportJobs.AddRange(otopopToAsymLalaBatch);
            exportJobs.AddRange(asymLalaToOtopopBatch);
            exportJobs.AddRange(asymLalaToVanillaLalaBatch);
            exportJobs.AddRange(vanillaLalaToOtopopBatch);
            exportJobs.AddRange(vanillaLalaToAsymLalaBatch);

            if (exportJobs.Count > 0)
            {

                Dictionary<string, string> generationCache = new Dictionary<string, string>();
                string executable = !string.IsNullOrEmpty(xNormalPathOverride) ? xNormalPathOverride
                : Path.Combine(GlobalPathStorage.OriginalBaseDirectory, xNormal);
                ProcessStartInfo processStartInfo = new ProcessStartInfo(@"""" + executable + @"""");
                processStartInfo.UseShellExecute = true;
                processStartInfo.Arguments = "";
                if (!Directory.Exists(userDataPath))
                {
                    Directory.CreateDirectory(userDataPath);
                }
                foreach (XNormalExportJob xNormalExportJob in exportJobs)
                {
                    if (!generationCache.ContainsKey(xNormalExportJob.OutputTexturePath))
                    {
                        string path = userDataPath + xNormalExportJob.OutputXMLPath;
                        processStartInfo.Arguments += @"""" + path + @""" ";
                        using (StreamWriter writer = new StreamWriter(path))
                        {
                            writer.Write(string.Format(xmlFile,
                            CleanXmlEscapeSequences(Path.Combine(!string.IsNullOrEmpty(basePathOverride)
                            ? basePathOverride : GlobalPathStorage.OriginalBaseDirectory, xNormalExportJob.InputModel)),
                            CleanXmlEscapeSequences(xNormalExportJob.InputTexturePath),
                            CleanXmlEscapeSequences(Path.Combine(!string.IsNullOrEmpty(basePathOverride)
                            ? basePathOverride : GlobalPathStorage.OriginalBaseDirectory, xNormalExportJob.OutputModel)),
                            CleanXmlEscapeSequences(xNormalExportJob.OutputTexturePath.Replace("_baseTexBaked", null)),
                            xNormalExportJob.IsNormalMap.ToString().ToLower(), xNormalExportJob.Width.ToString(), xNormalExportJob.Height.ToString(),
                            xNormalExportJob.OutputModel.Contains("gen2").ToString().ToLower()));
                        }
                        generationCache.Add(xNormalExportJob.OutputTexturePath, xNormalExportJob.OutputTexturePath);
                    }
                }
                if (exportJobs.Count > 0)
                {
                    LastArguments = processStartInfo.Arguments = processStartInfo.Arguments.Trim();
                    Process process = Process.Start(processStartInfo);
                    process.WaitForExit();

                    // Bibo and Gen3 to Gen2 dont need XNormal
                    foreach (var item in biboToGen2Batch)
                    {
                        TexIO.SaveBitmap(ImageManipulation.CutInHalf(TexIO.ResolveBitmap(item.InputTexturePath)), ImageManipulation.AddSuffix(item.OutputTexturePath, "_baseTexBaked"));
                    }
                    foreach (var item in gen3ToGen2Batch)
                    {
                        TexIO.SaveBitmap(ImageManipulation.CutInHalf(TexIO.ResolveBitmap(
                            ImageManipulation.AddSuffix(item.OutputTexturePath.Replace("gen2", "bibo"), "_baseTexBaked"))),
                            ImageManipulation.AddSuffix(item.OutputTexturePath, "_baseTexBaked"));
                    }
                }
            }
            exportJobs.Clear();
            biboToGen2Batch?.Clear();
            gen3ToGen2Batch?.Clear();
            biboToGen3Batch?.Clear();
            gen3ToBiboBatch?.Clear();
            gen2ToGen3Batch?.Clear();
            gen2ToGen3Batch?.Clear();
            otopopToVanillaLalaBatch?.Clear();
            otopopToAsymLalaBatch?.Clear();
            asymLalaToOtopopBatch?.Clear();
            asymLalaToVanillaLalaBatch?.Clear();
            vanillaLalaToOtopopBatch?.Clear();
            vanillaLalaToAsymLalaBatch?.Clear();
        }
        public static string CleanXmlEscapeSequences(string input)
        {
            return input.Replace("&", @"&#38;").Replace("'", @"&#39;").Replace("'", @"&#39;")
                .Replace("<", @"&#60;").Replace(">", @"&#62;").Replace(@"""", @"&#34");
        }
    }
}
