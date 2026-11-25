using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SqueakyLane.EditorTools
{
    // S3: nhập bộ sprite UI lọc từ dự án "Sand Puzzle Clone" (người làm chốt dùng 25/11, bài thực tập nội bộ, không phát hành).
    // PNG đã được chép vào Art/UI/Sprites/Pack (dùng ngay, nằm trong UiSprites.spriteatlas) và Art/UI/Library (icon dự phòng, không vào atlas
    // nên chỉ vào build khi được tham chiếu). Menu này đặt lại importer cho đúng: Sprite, 9-slice theo bảng border của dự án gốc, FullRect,
    // và ghi vào userData lề trong suốt quanh phần nhìn thấy để bước dựng UI canh thân nút theo phần nhìn thấy.
    public static class UiPackImport
    {
        internal const string PackFolder = UiThemeAssets.Art + "/Sprites/Pack";
        internal const string LibraryFolder = UiThemeAssets.Art + "/Library";

        // Border (trái, dưới, phải, trên) tính bằng pixel ảnh gốc, lấy từ .meta của dự án nguồn; sprite không có trong bảng thì không cắt 9-slice.
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase)
        {
            { "Gameplay_holder_superhard", new Vector4(39.9f, 45.9f, 38.9f, 40f) },
            { "bg_coin_holder", new Vector4(53f, 52f, 53f, 52f) },
            { "Gameplay_time_holder_basic", new Vector4(183.4f, 31.5f, 183.5f, 100.5f) },
            { "Gameplay_time_holder_superhard", new Vector4(183.4f, 90.5f, 183.5f, 100.5f) },
            { "Gameplay_holder_baisc", new Vector4(40f, 49f, 40f, 40f) },
            { "bg_gameplay_text_holder", new Vector4(82.5f, 29.5f, 82.5f, 29.5f) },
            { "Btn_square_grey_v1", new Vector4(64f, 64f, 64f, 64f) },
            { "Btn_square_green_v1", new Vector4(64f, 64f, 64f, 64f) },
            { "bg_booster_count_holder", new Vector4(34f, 35f, 34f, 35f) },
            { "bg_holder_info", new Vector4(85.9f, 77f, 87f, 74f) },
            { "bg_booster_timer_holder", new Vector4(34f, 34f, 34f, 34f) },
            { "bg_info_holder_purple", new Vector4(115f, 27.5f, 115f, 27.5f) },
            { "ic_settings", new Vector4(26f, 28f, 26f, 25f) },
            { "bg_popup_purple", new Vector4(188f, 193.9f, 183f, 206.9f) },
            { "ic_close", new Vector4(57f, 63.5f, 57f, 60.4f) },
            { "pop_up_btn_green", new Vector4(76f, 80f, 75.9f, 80f) },
            { "pop_up_btn_orange", new Vector4(76f, 80f, 75.9f, 80f) },
            { "pop_up_btn_purple_small", new Vector4(48f, 36f, 48f, 36f) },
            { "bg_frame_setting_button_holder", new Vector4(80f, 79.9f, 80f, 79.9f) },
            { "ic_vibration_green", new Vector4(50.5f, 36.5f, 50.5f, 36.5f) },
            { "ic_main_home", new Vector4(96.5f, 82.5f, 95.4f, 99.5f) },
            { "ic_time", new Vector4(41.9f, 42.9f, 42.9f, 42.9f) },
            { "ic_shop_coin_1", new Vector4(499f, 332.9f, 466.9f, 317.9f) },
            { "ic_health", new Vector4(82.9f, 80f, 82.9f, 76.9f) },
            { "ic_health_broken", new Vector4(86f, 71f, 85.9f, 86f) },
            { "ic_health_infinite", new Vector4(97f, 105f, 94.9f, 88f) },
            { "ic_plus", new Vector4(16.5f, 19.5f, 16.4f, 16.5f) },
            { "ic_booster_hammer", new Vector4(410.4f, 392.5f, 339.4f, 430.5f) },
            { "ic_booster_magnet", new Vector4(377.9f, 392f, 360f, 413.9f) },
            { "ic_booster_sweep", new Vector4(378.9f, 404f, 361f, 386f) },
            { "ic_booster_time_freeze", new Vector4(353f, 394f, 339f, 381f) },
            { "ic_booster_ufo", new Vector4(468f, 328f, 469f, 329.9f) },
            { "ic_rewarded", new Vector4(91.9f, 92.5f, 87.9f, 84.5f) },
            { "ic_loading", new Vector4(123f, 123f, 123f, 123f) },
            { "ic_main_settings", new Vector4(70.5f, 80.5f, 70.4f, 69.5f) },
            { "ic_vibration_notif", new Vector4(27.5f, 32.5f, 27.5f, 32.5f) },
            { "button_blue", new Vector4(96f, 64f, 96f, 64f) },
            { "button_purple", new Vector4(179f, 79f, 179f, 79f) },
            { "button_red", new Vector4(179f, 79f, 179f, 79f) },
            { "button_yellow", new Vector4(179f, 79f, 179f, 79f) },
            { "btn_green", new Vector4(88f, 85.9f, 88f, 87.9f) },
            { "btn_red", new Vector4(88f, 86.9f, 88f, 86.9f) },
            { "Btn_square_orange_v1", new Vector4(90.4f, 87.5f, 90.4f, 93.4f) },
            { "Btn_circle_booster", new Vector4(35.5f, 36.4f, 35.4f, 35.4f) },
            { "Btn_circle_booster_add", new Vector4(34.4f, 35.5f, 34.5f, 34.4f) },
            { "bg_popup_blue", new Vector4(189f, 206.9f, 182f, 186.9f) },
            { "bg_frame_setting_holder", new Vector4(104f, 104f, 104f, 104f) },
            { "bg_frame_button_gold", new Vector4(110f, 127f, 110f, 127f) },
            { "bg_switch_on", new Vector4(32f, 32f, 32f, 32f) },
            { "bg_switch_off", new Vector4(32f, 32f, 32f, 32f) },
            { "Popup_card_inner_bg", new Vector4(124.9f, 120.9f, 122.9f, 122.9f) },
            { "Popup_inner_panel", new Vector4(128f, 128f, 128f, 128f) },
            { "Popup_card_header_bg", new Vector4(95.9f, 128f, 96f, 40f) },
            { "Key_count_holder", new Vector4(54f, 44f, 54f, 44f) },
            { "cup_counter_holder", new Vector4(70f, 25f, 70f, 25f) },
            { "footer_holder_purple", new Vector4(485f, 95f, 485f, 95f) },
            { "footer_holder_orange", new Vector4(480.5f, 79f, 480.5f, 79f) },
            { "footer_holder_red", new Vector4(480.5f, 92f, 480.5f, 92f) },
            { "Gameplay_holder_hard", new Vector4(39.9f, 48f, 40f, 40f) },
            { "Gameplay_time_holder_hard", new Vector4(183.4f, 90.5f, 183.5f, 100.5f) },
        };

        // Lề trong suốt quanh phần nhìn thấy (alpha > ngưỡng), pixel ảnh.
        [Serializable]
        public sealed class Spec
        {
            public int w, h;
            public float left, bottom, right, top;
            public float VisibleW => w - left - right;
            public float VisibleH => h - bottom - top;
        }

        [MenuItem("Tools/Squeaky Lane/013 Theme/S3 Import UI Pack")]
        public static void Run()
        {
            UiThemeAssets.RequireEditMode();
            int count = 0;
            foreach (string folder in new[] { PackFolder, LibraryFolder })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    Configure(AssetDatabase.GUIDToAssetPath(guid));
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("013 S3: đã đặt importer cho " + count + " sprite (Sprites/Pack + Library): Sprite, PPU 100, 9-slice theo dự án gốc, userData = lề nhìn thấy.");
        }

        public static Sprite Load(string name)
        {
            foreach (string folder in new[] { PackFolder, LibraryFolder })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/" + name + ".png");
                if (sprite != null) { return sprite; }
            }
            throw new InvalidOperationException("Thiếu sprite " + name + " trong Art/UI/Sprites/Pack hoặc Art/UI/Library; chạy menu S3 Import UI Pack.");
        }

        public static Spec SpecOf(Sprite sprite)
        {
            if (sprite == null) { throw new InvalidOperationException("Thiếu sprite; chạy menu S3 Import UI Pack."); }
            string path = AssetDatabase.GetAssetPath(sprite);
            var importer = AssetImporter.GetAtPath(path);
            if (importer == null || string.IsNullOrEmpty(importer.userData)) { throw new InvalidOperationException("Sprite chưa qua menu S3 Import UI Pack: " + path); }
            var spec = JsonUtility.FromJson<Spec>(importer.userData);
            if (spec == null || spec.VisibleH <= 0) { throw new InvalidOperationException("userData sprite hỏng, chạy lại S3 Import UI Pack: " + path); }
            return spec;
        }

        private static void Configure(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            string name = Path.GetFileNameWithoutExtension(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = Borders.TryGetValue(name, out var border) ? border : Vector4.zero;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            // Không hạ cỡ ảnh: border 9-slice tính theo pixel ảnh gốc.
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.userData = JsonUtility.ToJson(Measure(path));
            importer.SaveAndReimport();
        }

        private static Spec Measure(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path), false)) { throw new InvalidOperationException("Không đọc được PNG: " + path); }
                var pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height, minX = w, maxX = -1, minY = h, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a <= 16) { continue; }
                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }
                if (maxX < 0) { throw new InvalidOperationException("PNG trong suốt hoàn toàn: " + path); }
                // y của Texture2D đi lên: minY là mép dưới.
                return new Spec { w = w, h = h, left = minX, right = w - 1 - maxX, bottom = minY, top = h - 1 - maxY };
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
