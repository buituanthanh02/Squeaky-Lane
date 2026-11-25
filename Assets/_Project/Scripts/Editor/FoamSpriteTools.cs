using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SqueakyLane.EditorTools
{
    // Asset tĩnh của phiếu013-s4-foam-sprite; không nối scene/hệ hạt hoặc tạo material runtime.
    public static class FoamSpriteTools
    {
        private const string TexturePath = "Assets/_Project/Art/VFX/Textures/foam.png";
        private const string MaterialPath = "Assets/_Project/Art/VFX/Materials/M_Foam.mat";
        private const string PreviewPath = "Logs/013-foam-preview.png";
        private const int Size = 128;
        // Số bake theo Verdict vòng2; không phải tuning runtime.
        private const float EdgeSoftness = 1.5f;
        private const float BodyAlpha = .8f, HighlightAlpha = .95f;
        private const float RimWidth = 1.25f, LowerRimOpacity = .6f;
        private static readonly Color ShadowColor = new Color32(191, 217, 242, 255);
        private static readonly Color LowerRimColor = new Color32(159, 196, 232, 255);
        // Tọa độ cố định thay hạt giống random: x/y từ tâm PNG, z là bán kính.
        // Thứ tự lớp sau → trước; đây là số bake theo phiếu, không phải tuning runtime.
        private static readonly Vector3[] Bubbles =
        {
            new Vector3(-27, 8, 21), new Vector3(-5, 3, 30),
            new Vector3(25, 8, 24), new Vector3(-32, -10, 20),
            new Vector3(31, -11, 21), new Vector3(7, -13, 24),
            new Vector3(-14, -13, 22),
            // Tám bọt nhỏ ở lớp trước tạo rìa lồi lõm và chặn mái vòm đơn.
            new Vector3(-49, 6, 7), new Vector3(-42, 23, 6),
            new Vector3(-22, 29, 8), new Vector3(13, 29, 9),
            new Vector3(43, 22, 5), new Vector3(49, -6, 7),
            new Vector3(40, -25, 5), new Vector3(-37, -27, 4)
        };

        [MenuItem("Tools/Squeaky Lane/014 VFX/Create Foam Sprite")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { throw new InvalidOperationException("Thoát Play trước khi tạo sprite bọt xà phòng."); }
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
            Debug.Log("014 Foam: sprite128 RGBA + M_Foam (Sprites/Default, ZWrite Off); preview128/64/32 trên navy/đỏ: " + PreviewPath + ". Không sửa scene/prefab.");
        }

        private static Texture2D BuildTexture()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            var center = Vector2.one * ((Size - 1) * .5f);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2(x, y) - center;
                    Color result = Color.clear;
                    foreach (var bubble in Bubbles)
                    {
                        var local = p - new Vector2(bubble.x, bubble.y);
                        float radius = bubble.z;
                        float distance = local.magnitude;
                        if (distance >= radius) { continue; }
                        float edge = 1 - Smooth(radius - EdgeSoftness, radius, distance);
                        float localShadow = 1 - Smooth(-.65f, .55f, local.y / radius);
                        float clusterShadow = 1 - Smooth(-30, 24, p.y);
                        Color color = Color.Lerp(Color.white, ShadowColor, .72f * localShadow + .28f * clusterShadow);
                        // Vành nằm phía trong cạnh mềm để vẫn đọc được ở chỗ hai bọt chồng nhau.
                        // Độ rộng giữa hai điểm nửa cường độ là1.25px, độc lập cỡ bọt.
                        float rim = Mathf.Exp(-4 * Mathf.Log(2) *
                            Mathf.Pow((distance - radius + EdgeSoftness + RimWidth * .5f) / RimWidth, 2));
                        float upperRim = Smooth(-.15f, .3f, local.y / radius);
                        // Alpha0.6 là độ phủ nét xanh lên thân, không cộng vào alpha thân0.8.
                        color = Color.Lerp(color, LowerRimColor, rim * (1 - upperRim) * LowerRimOpacity);
                        float spot = 0;
                        if (radius >= 20)
                        {
                            var highlight = new Vector2(-.36f, .48f) * radius;
                            spot = 1 - Smooth(radius * .055f, radius * .12f, (local - highlight).magnitude);
                            var smallHighlight = new Vector2(-.10f, .64f) * radius;
                            spot = Mathf.Max(spot, 1 - Smooth(.6f, 1.3f, (local - smallHighlight).magnitude));
                        }
                        color = Color.Lerp(color, Color.white, Mathf.Max(rim * upperRim, spot));
                        float alpha = Mathf.Lerp(BodyAlpha, HighlightAlpha, spot) * edge;
                        // Ghép cạnh mềm vào lớp sau, giữ alpha thân không cộng dồn thành đĩa đặc.
                        float previousAlpha = result.a;
                        result = previousAlpha > 0 ? Color.Lerp(result, color, edge) : color;
                        result.a = Mathf.Max(previousAlpha, alpha);
                    }
                    pixels[y * Size + x] = result;
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
            if (importer == null) { throw new InvalidOperationException("Không tìm thấy TextureImporter cho foam.png."); }
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
            if (texture == null) { throw new InvalidOperationException("foam.png chưa import xong."); }
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            bool created = material == null;
            if (created)
            {
                material = new Material(shader) { name = "M_Foam" };
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

        private static void BuildPreview(Texture2D foam)
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
                            new Color32(192, 57, 43, 255);
                        int column = x / cell;
                        int size = column == 0 ? 128 : column == 1 ? 64 : 32;
                        int left = column * cell + (cell - size) / 2;
                        int bottom = (top ? height / 2 : 0) + (height / 2 - size) / 2;
                        Color output = background;
                        if (x >= left && x < left + size && y >= bottom && y < bottom + size)
                        {
                            var sample = foam.GetPixelBilinear((x - left + .5f) / size, (y - bottom + .5f) / size);
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
