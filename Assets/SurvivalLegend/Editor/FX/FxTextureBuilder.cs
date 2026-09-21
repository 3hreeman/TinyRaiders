using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SurvivalLegend.Editor.FX
{
    /// <summary>Saved, tintable particle art. Generates only missing assets so artist edits survive.</summary>
    public static class FxTextureBuilder
    {
        public const string ArtFolder = "Assets/SurvivalLegend/Resources/Art/FX";
        public const string SoftGlowPath = ArtFolder + "/soft-glow.png";
        public const string SparkStreakPath = ArtFolder + "/spark-streak.png";
        public const string CrescentSlashPath = ArtFolder + "/crescent-slash.png";
        public const string VortexSoftPath = ArtFolder + "/vortex-soft.png";
        public const string AdditiveMaterialPath = ArtFolder + "/FXAdditive.mat";
        public const string AlphaMaterialPath = ArtFolder + "/FXAlpha.mat";
        public const string AdditiveShaderPath = "Assets/SurvivalLegend/Shaders/FXAdditive.shader";
        public const string AlphaShaderPath = "Assets/SurvivalLegend/Shaders/FXAlpha.shader";

        public static Material AdditiveMaterial => AssetDatabase.LoadAssetAtPath<Material>(AdditiveMaterialPath);
        public static Material AlphaMaterial => AssetDatabase.LoadAssetAtPath<Material>(AlphaMaterialPath);

        [MenuItem("Survival Legend/Build FX Textures")]
        public static void EnsureAssets()
        {
            Directory.CreateDirectory(ArtFolder);
            var glow = EnsureTexture(SoftGlowPath, 128, GlowPixel);
            EnsureTexture(SparkStreakPath, 128, SparkPixel);
            EnsureTexture(CrescentSlashPath, 256, SlashPixel);
            var vortex = EnsureTexture(VortexSoftPath, 256, VortexPixel);
            EnsureMaterial(AdditiveMaterialPath, AdditiveShaderPath, "SurvivalLegend/FXAdditive", glow);
            EnsureMaterial(AlphaMaterialPath, AlphaShaderPath, "SurvivalLegend/FXAlpha", vortex);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D EnsureTexture(string path, int size, Func<float, float, byte> alpha)
        {
            if (!File.Exists(path))
            {
                var image = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
                { name = Path.GetFileNameWithoutExtension(path), filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f - size * .5f) / (size * .5f);
                    float v = (y + .5f - size * .5f) / (size * .5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha(u, v));
                }
                image.SetPixels32(pixels); image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("FX texture importer unavailable: " + path);
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = size;
                importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new InvalidOperationException("FX texture unavailable: " + path);
            return texture;
        }

        private static Material EnsureMaterial(string path, string shaderPath, string shaderName, Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null) shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("FX shader unavailable: " + shaderPath);
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path), mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static byte Alpha(float value) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        private static float Bell(float x) => Mathf.Exp(-x * x);
        private static float Smooth(float a, float b, float value)
        {
            float t = Mathf.Clamp01((value - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static byte GlowPixel(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float soft = Mathf.Pow(Mathf.Clamp01(1f - r), 2.8f);
            float core = .16f * Bell(r / .15f);
            return Alpha(Mathf.Min(.74f, soft * .60f + core));
        }

        private static byte SparkPixel(float u, float v)
        {
            float length = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u)), .7f);
            float width = .027f + .045f * length;
            float shaft = Bell(v / width) * length;
            float cross = .28f * Bell(u / .025f) * Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v) / .34f), 1.8f);
            float core = .18f * Bell(Mathf.Sqrt(u * u + v * v) / .065f);
            return Alpha(Mathf.Min(1f, shaft * .9f + cross + core));
        }

        private static byte SlashPixel(float u, float v)
        {
            float radius = Mathf.Sqrt(u * u + v * v);
            float degrees = Mathf.Atan2(v, u) * Mathf.Rad2Deg;
            if (degrees < -145f || degrees > 75f) return 0;
            float t = (degrees + 145f) / 220f;
            float taper = Mathf.Pow(Mathf.Sin(Mathf.PI * t), .8f);
            float center = .66f + .045f * Mathf.Sin(t * Mathf.PI * 2f);
            float thickness = .027f + .10f * taper;
            float blade = Bell((radius - center) / thickness);
            float lip = .25f * Bell((radius - center - thickness * .72f) / .022f);
            return Alpha((blade * .84f + lip) * Smooth(0f, .075f, t) * (1f - Smooth(.925f, 1f, t)));
        }

        private static byte VortexPixel(float u, float v)
        {
            float radius = Mathf.Sqrt(u * u + v * v);
            if (radius >= .98f) return 0;
            float theta = Mathf.Atan2(v, u);
            float ring = .30f * Bell((radius - .56f) / .24f);
            float swirl = Mathf.Pow((Mathf.Cos(theta * 3f + radius * 10f) + 1f) * .5f, 10f);
            float arms = .43f * swirl * Smooth(.13f, .28f, radius) * (1f - Smooth(.80f, .98f, radius));
            float center = .08f * (1f - Smooth(.05f, .55f, radius));
            return Alpha((ring + arms + center) * (1f - Smooth(.88f, .98f, radius)));
        }
    }
}
