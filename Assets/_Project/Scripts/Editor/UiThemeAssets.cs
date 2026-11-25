using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    // Chỉ chạy ngoài Play. AssetDatabase tạo GUID/meta; không viết YAML asset mới.
    public static class UiThemeAssets
    {
        internal const string Art = "Assets/_Project/Art/UI";
        internal const string ThemePath = "Assets/_Project/Config/UiTheme.asset";
        private static string Downloads => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        private static readonly string[] IconNames = { "pause", "home", "undo", "hint", "restart", "sound_on", "sound_off", "phone", "next", "star_full", "star_empty" };
        private static readonly string[] IconSources =
        {
            "kenney_game-icons/PNG/White/1x/pause.png", "kenney_game-icons/PNG/White/1x/home.png",
            "kenney_game-icons/PNG/White/1x/return.png", "kenney_game-icons/PNG/White/1x/question.png",
            "kenney_ui-pack/PNG/Extra/Default/icon_repeat_light.png", "kenney_game-icons/PNG/White/1x/audioOn.png",
            "kenney_game-icons/PNG/White/1x/audioOff.png", "kenney_game-icons/PNG/White/1x/phone.png",
            "kenney_game-icons/PNG/White/1x/next.png", "kenney_ui-pack/PNG/Yellow/Default/star.png",
            "kenney_ui-pack/PNG/Yellow/Default/star_outline.png"
        };
        private static readonly string[] UiSources =
        {
            "Yellow/Default/button_rectangle_depth_gradient", "Grey/Default/button_rectangle_depth_gradient",
            "Red/Default/button_rectangle_depth_gradient", "Grey/Default/button_square_depth_border",
            "Grey/Default/slide_horizontal_grey", "Blue/Default/slide_horizontal_color",
            "Grey/Default/check_square_grey", "Blue/Default/check_square_color_checkmark"
        };

        [MenuItem("Tools/Squeaky Lane/013 Theme/1 Import UI Assets")]
        public static void Import()
        {
            RequireEditMode();
            // Kiểm đủ toàn bộ nguồn trước khi nhập bất kỳ file nào, không tự vẽ icon thiếu.
            foreach (string file in IconSources) { RequireSource(file); }
            foreach (string file in UiSources) { RequireSource("kenney_ui-pack/PNG/" + file + ".png"); }
            RequireSource("Baloo_2/static/Baloo2-Bold.ttf");
            RequireSource("Baloo_2/static/Baloo2-ExtraBold.ttf");
            RequireSource("kenney_ui-pack/License.txt");
            RequireSource("kenney_game-icons/license.txt");
            RequireSource("Baloo_2/OFL.txt");
            EnsureFolder(Art + "/Sprites");
            EnsureFolder(Art + "/Fonts");
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            Backup(ThemePath);
            var data = new SerializedObject(theme);
            string[] properties = { "primaryButton", "regularButton", "dangerButton", "panel", "clockTrack", "clockFill", "toggleBox", "toggleCheck" };
            for (int i = 0; i < UiSources.Length; i++)
            {
                string source = "kenney_ui-pack/PNG/" + UiSources[i] + ".png";
                string target = Art + "/Sprites/" + properties[i] + ".png";
                var border = i <= 3 ? new Vector4(14, 14, 14, 14) : i <= 5 ? new Vector4(8, 6, 8, 6) : Vector4.zero;
                data.FindProperty(properties[i]).objectReferenceValue = ImportSprite(source, target, border);
            }
            for (int i = 0; i < IconNames.Length; i++)
            {
                ImportSprite(IconSources[i], Art + "/Sprites/icon_" + IconNames[i] + ".png", Vector4.zero, true);
            }
            CopySource("kenney_ui-pack/License.txt", Art + "/KenneyUi-License.txt");
            CopySource("kenney_game-icons/license.txt", Art + "/KenneyIcons-License.txt");
            CopySource("Baloo_2/OFL.txt", Art + "/Fonts/OFL.txt");
            CopySource("Baloo_2/static/Baloo2-Bold.ttf", Art + "/Fonts/Baloo2-Bold.ttf");
            CopySource("Baloo_2/static/Baloo2-ExtraBold.ttf", Art + "/Fonts/Baloo2-ExtraBold.ttf");
            try
            {
                EditorUtility.DisplayProgressBar("013 · Font tĩnh", "Baloo2 Bold", .25f);
                data.FindProperty("bodyFont").objectReferenceValue = BakeFont("Baloo2-Bold");
                EditorUtility.DisplayProgressBar("013 · Font tĩnh", "Baloo2 ExtraBold", .7f);
                data.FindProperty("titleFont").objectReferenceValue = BakeFont("Baloo2-ExtraBold");
                data.FindProperty("icons").objectReferenceValue = BakeIcons();
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssetIfDirty(theme);
                Selection.activeObject = theme;
                Debug.Log("013: đã nhập đúng sprite dùng tới + 2 TTF static + 3 license; tạo font TMP STATIC tiếng Việt và atlas11 icon. Tiếp menu2 Apply UI Skin.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        internal static TMP_FontAsset BakeFont(string name, bool rebuildForS2 = false)
        {
            string path = Art + "/Fonts/" + name + " SDF.asset";
            var saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (saved != null && (!rebuildForS2 || saved.atlasPadding >= 10))
            {
                if (saved.atlasPopulationMode != AtlasPopulationMode.Static || !saved.HasCharacters("ĐđẮằẾệỢự Thắng! ←↑→↓…"))
                {
                    throw new InvalidOperationException("Font đã có nhưng sai Static/thiếu glyph; không ghi đè GUID: " + path);
                }
                return saved;
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/" + name + ".ttf");
            // Dynamic chỉ là trạng thái bake ngoài Editor; khóa Static trước khi lưu asset vào repo.
            var asset = TMP_FontAsset.CreateFontAsset(font, 64, rebuildForS2 ? 10 : 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (asset == null) { throw new InvalidOperationException("Không nạp được font " + name); }
            asset.name = name + " SDF";
            var codepoints = new List<uint>();
            for (uint code = 0x20; code <= 0x24f; code++)
            {
                if (FontEngine.TryGetGlyphIndex(code, out uint index) && index != 0) { codepoints.Add(code); }
            }
            for (uint code = 0x1ea0; code <= 0x1ef9; code++) { codepoints.Add(code); }
            foreach (char code in "←↑→↓…–—·∞") { codepoints.Add(code); }
            if (!asset.TryAddCharacters(codepoints.ToArray(), out uint[] missing))
            {
                UnityEngine.Object.DestroyImmediate(asset.material);
                foreach (var texture in asset.atlasTextures) { UnityEngine.Object.DestroyImmediate(texture); }
                UnityEngine.Object.DestroyImmediate(asset);
                throw new InvalidOperationException("Font không chứa/không đủ atlas cho glyph: " + string.Join(",", missing));
            }
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (saved != null)
            {
                // Bake thành công ở bộ nhớ trước. Giữ nguyên GUID, material và texture subasset cũ.
                Backup(path);
                var oldMaterial = saved.material;
                var oldTexture = saved.atlasTextures[0];
                var bakedMaterial = asset.material;
                var bakedTexture = asset.atlasTextures[0];
                EditorUtility.CopySerialized(bakedTexture, oldTexture);
                EditorUtility.CopySerialized(bakedMaterial, oldMaterial);
                oldMaterial.mainTexture = oldTexture;
                asset.material = oldMaterial;
                asset.atlasTextures = new[] { oldTexture };
                EditorUtility.CopySerialized(asset, saved);
                saved.ReadFontAssetDefinition();
                EditorUtility.SetDirty(saved);
                EditorUtility.SetDirty(oldMaterial);
                EditorUtility.SetDirty(oldTexture);
                UnityEngine.Object.DestroyImmediate(bakedMaterial);
                UnityEngine.Object.DestroyImmediate(bakedTexture);
                UnityEngine.Object.DestroyImmediate(asset);
                AssetDatabase.SaveAssetIfDirty(saved);
                return saved;
            }
            AssetDatabase.CreateAsset(asset, path);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures)
            {
                texture.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(texture, asset);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static TMP_SpriteAsset BakeIcons()
        {
            string path = Art + "/UiIcons.asset";
            var saved = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);
            if (saved != null) { return saved; }
            var textures = new Texture2D[IconNames.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Sprites/icon_" + IconNames[i] + ".png");
            }
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var rects = atlas.PackTextures(textures, 2, 1024, false);
            string atlasPath = Art + "/UiIcons.png";
            File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "UiIcons";
            // Tránh đường upgrade dữ liệu sprite cũ khi gán material.
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.spriteSheet = texture;
            for (int i = 0; i < rects.Length; i++)
            {
                Rect r = rects[i];
                var box = new GlyphRect(Mathf.RoundToInt(r.x * texture.width), Mathf.RoundToInt(r.y * texture.height),
                    Mathf.RoundToInt(r.width * texture.width), Mathf.RoundToInt(r.height * texture.height));
                var glyph = new TMP_SpriteGlyph((uint)i, new GlyphMetrics(box.width, box.height, 0, box.height, box.width), box, 1, 0);
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xfffe, asset, glyph) { name = IconNames[i], scale = 1 });
            }
            asset.material = new Material(Shader.Find("TextMeshPro/Sprite"));
            asset.material.mainTexture = texture;
            asset.material.name = "UiIcons Material";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        internal static Sprite ImportSprite(string source, string target, Vector4 border, bool readable = false)
        {
            CopySource(source, target);
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = readable;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }
        private static void CopySource(string source, string target)
        {
            if (!File.Exists(target)) { File.Copy(Path.Combine(Downloads, source), target); }
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
        }
        private static void RequireSource(string relative)
        {
            string path = Path.Combine(Downloads, relative);
            if (!File.Exists(path)) { throw new FileNotFoundException("Thiếu asset đã duyệt; dừng, không tự vẽ thay.", path); }
        }
        internal static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { throw new InvalidOperationException("Thoát Play trước khi chạy menu013."); }
        }
        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        internal static void Backup(string path)
        {
            if (!File.Exists(path)) { return; }
            string root = "Logs/013-asset-backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(root);
            File.Copy(path, root + "/" + Path.GetFileName(path));
        }
    }
}
