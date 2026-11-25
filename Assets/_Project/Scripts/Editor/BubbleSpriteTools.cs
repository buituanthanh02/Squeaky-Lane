using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SqueakyLane.EditorTools
{
    // Asset tĩnh của phiếu013-s4-bubble-sprite; không nối scene/hệ hạt hoặc tạo material runtime.
    public static class BubbleSpriteTools
    {
        private const string TexturePath = "Assets/_Project/Art/VFX/Textures/bubble.png";
        private const string MaterialPath = "Assets/_Project/Art/VFX/Materials/M_Bubble.mat";
        private const string PreviewPath = "Logs/013-bubble-preview.png";
        private const int Size = 128;
        // Số bake theo phiếu: bán kính58px chừa6px trong suốt, vành7%R, mép mềm1.5px.
        private const float Radius = 58, RimFraction = .07f, EdgeSoftness = 1.5f;
        private const float BodyAlpha = .14f, RimAlpha = .66f, HighlightAlpha = .9f;
        private static readonly Color BodyColor = new Color32(234, 246, 255, 255);

        [MenuItem("Tools/Squeaky Lane/014 VFX/Create Bubble Sprite")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { throw new InvalidOperationException("Thoát Play trước khi tạo sprite bong bóng."); }
            var shader = Shader.Find("Sprites/Default");
            if (shader == null || !shader.isSupported)
            { throw new InvalidOperationException("Thiếu shader Built-in Sprites/Default hoặc GPU không hỗ trợ."); }
            EnsureFolder("Assets/_Project/Art/VFX/Textures");
            EnsureFolder("Assets/_Project/Art/VFX/Materials");
            var texture = BuildTexture();
            try
            {
                bool textureChanged = WriteIfDifferent(TexturePath, texture.EncodeToPNG());
                if (textureChanged || AssetImporter.GetAtPath(TexturePath) == null)
                { AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport); }
                ConfigureImporter();
                ConfigureMaterial(shader);
                BuildPreview(texture);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            Debug.Log("014 Bubble: sprite128 RGBA + M_Bubble (Sprites/Default, ZWrite Off); preview128/64/24 trên navy/checker: " + PreviewPath + ". Không sửa scene/prefab.");
        }

        private static Texture2D BuildTexture()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            var center = Vector2.one * ((Size - 1) * .5f);
            var highlight = new Vector2(-.58f, .58f) * Radius;
            float highlightRadius = Radius * .08f;
            var secondaryDirection = new Vector2(1, -1).normalized;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2(x, y) - center;
                    float distance = p.magnitude;
                    if (distance >= Radius) { pixels[y * Size + x] = new Color32(0, 0, 0, 0); continue; }
                    float edge = 1 - Smooth(Radius - EdgeSoftness, Radius, distance);
                    float ring = Smooth(Radius * (1 - RimFraction) - 1, Radius * (1 - RimFraction) + 1, distance);
                    float alpha = Mathf.Lerp(BodyAlpha, RimAlpha, ring);
                    float spot = 1 - Smooth(highlightRadius * .5f, highlightRadius, (p - highlight).magnitude);
                    // Vệt phụ hẹp trên cung dưới-phải, yếu hơn điểm sáng chính.
                    float alignment = distance > 0 ? Vector2.Dot(p / distance, secondaryDirection) : -1;
                    float arc = Smooth(Mathf.Cos(28 * Mathf.Deg2Rad), Mathf.Cos(12 * Mathf.Deg2Rad), alignment);
                    float band = Mathf.Exp(-Mathf.Pow((distance - Radius * .87f) / 1.5f, 2));
                    float secondary = arc * band;
                    alpha = Mathf.Max(alpha, Mathf.Max(HighlightAlpha * spot, .28f * secondary)) * edge;
                    var color = Color.Lerp(BodyColor, Color.white, Mathf.Max(spot, secondary * .6f));
                    color.a = alpha;
                    pixels[y * Size + x] = color;
                }
            }
            // PNG straight alpha. Sprites/Default tự nhân RGB×A trong SpriteFrag, không nhân tại đây.
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static float Smooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3 - 2 * t);
        }

        private static void ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null) { throw new InvalidOperationException("Không tìm thấy TextureImporter cho bubble.png."); }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                !importer.alphaIsTransparency || importer.alphaSource != TextureImporterAlphaSource.FromInput ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.filterMode != FilterMode.Bilinear || importer.wrapMode != TextureWrapMode.Clamp ||
                importer.maxTextureSize != Size || importer.npotScale != TextureImporterNPOTScale.None ||
                !importer.sRGBTexture || importer.isReadable || importer.spritePixelsPerUnit != 100 ||
                importer.spriteBorder != Vector4.zero || settings.spriteMeshType != SpriteMeshType.FullRect ||
                settings.spriteAlignment != (int)SpriteAlignment.Center;
            if (!changed) { return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = Size;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = Vector4.zero;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void ConfigureMaterial(Shader shader)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) { throw new InvalidOperationException("bubble.png chưa import xong."); }
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            bool created = material == null;
            if (created)
            {
                material = new Material(shader) { name = "M_Bubble" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            bool changed = created || material.shader != shader || material.mainTexture != texture ||
                material.color != Color.white || material.renderQueue != (int)RenderQueue.Transparent ||
                material.mainTextureScale != Vector2.one || material.mainTextureOffset != Vector2.zero;
            if (!changed) { return; }
            material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static void BuildPreview(Texture2D bubble)
        {
            const int width = 432, height = 320, cell = 144;
            var preview = new Texture2D(width, height, TextureFormat.RGBA32, false);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bool top = y >= height / 2;
                        var background = top ? new Color32(20, 51, 106, 255) :
                            ((x / 12 + y / 12) % 2 == 0 ? new Color32(65, 71, 80, 255) : new Color32(102, 110, 121, 255));
                        int column = x / cell;
                        int size = column == 0 ? 128 : column == 1 ? 64 : 24;
                        int left = column * cell + (cell - size) / 2;
                        int bottom = (top ? height / 2 : 0) + (height / 2 - size) / 2;
                        Color output = background;
                        if (x >= left && x < left + size && y >= bottom && y < bottom + size)
                        {
                            var sample = bubble.GetPixelBilinear((x - left + .5f) / size, (y - bottom + .5f) / size);
                            // Blend như project Linear, nhưng PNG preview lưu màu sRGB để xem ngoài Editor.
                            output = linear ?
                                Color.Lerp(((Color)background).linear, sample.linear, sample.a).gamma : Color.Lerp(background, sample, sample.a);
                            output.a = 1;
                        }
                        preview.SetPixel(x, y, output);
                    }
                }
                preview.Apply(false, false);
                Directory.CreateDirectory("Logs");
                WriteIfDifferent(PreviewPath, preview.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
        }

        private static bool WriteIfDifferent(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                byte[] current = File.ReadAllBytes(path);
                bool same = current.Length == bytes.Length;
                for (int i = 0; same && i < bytes.Length; i++) { same = current[i] == bytes[i]; }
                if (same) { return false; }
            }
            File.WriteAllBytes(path, bytes);
            return true;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
