using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    // S3: dựng lại HUD và các hộp thoại bằng bộ sprite lọc từ Sand Puzzle Clone (xem UiPackImport).
    // Mỗi phần giao diện là một lớp (GameObject + Image) riêng, tên bắt đầu "013 ", để người làm đổi sprite/màu/vị trí trong Inspector.
    // Menu idempotent: mỗi lần chạy dọn toàn bộ trang trí "013 " cũ trong SafeArea rồi dựng lại; object gốc, binding và On Click giữ nguyên (so trước/sau).
    public static class UiPackSkinTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/LevelButton.prefab";
        private const string ReportPath = "docs/handoff/013-scene-changes.md";
        private const string SectionHead = "\n## S3 ·";

        // Bố cục (pixel mockup rộng 300 px, nhân HudUnit khi áp). Màn dài như 12 Pro Max chỉ còn ~272 px bề ngang nên hàng trên phải gọn.
        private static readonly Vector2 LevelPillSize = new Vector2(62, 26), ClockPillSize = new Vector2(96, 32), MovesPillSize = new Vector2(58, 18), GearSize = new Vector2(26, 26);
        private static readonly Vector2 BubbleSize = new Vector2(200, 26), LabelPillSize = new Vector2(84, 22);
        private const float GearIcon = 16, BoosterIcon = 30, DialogButtonHeight = 36, HeaderWidth = 170, ToggleFrame = 50, ToggleIcon = 26, ToggleCaption = 14;
        private const float PausedHeight = 252, WonHeight = 258, LostHeight = 132;

        private static StringBuilder report;
        // Thay đổi field ghi theo giá trị ĐẦU → CUỐI của cả lần chạy (bước trung gian đổi qua đổi lại không vào báo cáo).
        private static readonly List<string> changeOrder = new List<string>();
        private static readonly Dictionary<string, string[]> changes = new Dictionary<string, string[]>();
        private static UiTheme theme;
        private static SerializedObject data;
        private static NavigationView nav;
        private static float Unit => theme.HudUnit;

        [MenuItem("Tools/Squeaky Lane/013 Theme/S3 Apply UI Pack Skin")]
        public static void Apply()
        {
            UiThemeAssets.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || scene.path != ScenePath || scene.isDirty)
            { throw new InvalidOperationException("Đóng Prefab Mode, mở Play.unity và lưu phần đang sửa trước khi áp S3."); }
            theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            UiSkinTools.ValidateTheme(theme);
            nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            if (nav == null || nav.gameObject.scene != scene) { throw new InvalidOperationException("Thiếu NavigationView trong Play."); }
            data = new SerializedObject(nav);
            var scaler = nav.GetComponentInParent<CanvasScaler>();
            if (scaler == null) { throw new InvalidOperationException("Thiếu CanvasScaler để đổi từ mockup 300 px."); }
            string bindings = UiSkinTools.CaptureBindings(nav);
            var originals = nav.GetComponentsInChildren<Transform>(true);
            var names = Array.ConvertAll(originals, node => node.name);
            foreach (string path in new[] { ScenePath, PrefabPath, UiThemeAssets.ThemePath }) { UiThemeAssets.Backup(path); }
            report = new StringBuilder(SectionHead + " bộ UI Pack · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n" +
                "Mục này ghi thay đổi của LẦN CHẠY CUỐI (menu idempotent: chạy lại trên scene đã áp thì gần như trống). Toàn bộ so với commit trước xem `git diff`.\n\n");
            changeOrder.Clear();
            changes.Clear();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("013 S3 UI Pack");
            try
            {
                Set(theme, "hudUnit", scaler.referenceResolution.x / 300f);
                BuildAssets();
                TearDown();
                Set(nav, "theme", theme);
                Set(nav, "normalClock", theme.ClockColor);
                Set(nav, "lowClock", theme.LowClock);
                BuildHud();
                BuildDialogs();
                BuildLevelSelect();
                BuildStuckBanner();
                var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    string before = UiSkinTools.CaptureButtonEvent(prefab.GetComponent<Button>());
                    StyleLevelButton(prefab.GetComponent<Button>());
                    if (UiSkinTools.CaptureButtonEvent(prefab.GetComponent<Button>()) != before)
                    { throw new InvalidOperationException("OnClick LevelButton đã đổi; không lưu prefab."); }
                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                // Object gốc (không phải trang trí "013 ") không được mất hay đổi tên.
                for (int i = 0; i < originals.Length; i++)
                {
                    if (names[i].StartsWith("013 ", StringComparison.Ordinal)) { continue; }
                    if (originals[i] == null || originals[i].name != names[i]) { throw new InvalidOperationException("Tên/object cũ đã đổi: " + names[i] + "; không lưu scene."); }
                }
                string bindingsAfter = UiSkinTools.CaptureBindings(nav);
                if (bindingsAfter != bindings) { throw new InvalidOperationException("Binding/OnClick NavigationView đã đổi; không lưu scene. " + FirstDifference(bindings, bindingsAfter)); }
                VerifyResult();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                foreach (string key in changeOrder)
                {
                    var change = changes[key];
                    if (change[1] != change[2]) { report.AppendLine(change[0] + " · " + change[1] + " → " + change[2]); }
                }
                string prior = File.Exists(ReportPath) ? File.ReadAllText(ReportPath) : "# 013 — Thay đổi scene/prefab\n";
                int start = prior.IndexOf(SectionHead, StringComparison.Ordinal);
                if (start >= 0)
                {
                    int end = prior.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
                    prior = prior.Substring(0, start) + report + (end >= 0 ? prior.Substring(end) : "");
                }
                else { prior += report; }
                File.WriteAllText(ReportPath, prior, new UTF8Encoding(false));
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("013 S3 đã áp: HUD (đồng hồ mm:ss, nút cài đặt nhỏ), hộp thoại nhiều lớp, LevelButton theo bộ UI Pack. Tên/binding/OnClick giữ nguyên. Run All + Console0, Play/reload, ảnh dọc. Field cũ→mới: " + ReportPath);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("S3 dừng, chưa xác nhận hoàn tất. Không lưu scene dở; đọc lỗi và backup Logs/013-asset-backups trước khi thử tiếp.");
                throw;
            }
        }

        // ---------------------------------------------------------------- tài nguyên dùng chung

        private static void BuildAssets()
        {
            Set(theme, "clockNormal", Spr("Gameplay_holder_baisc"));
            Set(theme, "clockLow", Spr("Gameplay_holder_superhard"));
            Set(theme, "hintOn", Spr("btn_booster_orange"));
            Set(theme, "hintOff", Spr("Btn_square_grey_v1"));
            Set(theme, "badgeOn", Spr("bg_booster_count_holder"));
            Set(theme, "badgeOff", Spr("bg_holder_info"));
            BuildTextMaterials();
            const string atlasPath = UiThemeAssets.Art + "/UiSprites.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, atlasPath); }
            UiThemeAssets.Backup(atlasPath);
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings { readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear });
            var platform = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            atlas.SetPlatformSettings(platform);
            atlas.SetIncludeInBuild(true);
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(UiThemeAssets.Art + "/Sprites");
            atlas.Remove(atlas.GetPackables());
            atlas.Add(new UnityEngine.Object[] { folder });
            // Thư mục con Pack phải nằm trong atlas (một atlas = ít draw call cho HUD); thêm riêng nếu Unity không gộp thư mục con.
            if (!atlas.CanBindTo(Spr("btn_booster_green")))
            {
                atlas.Add(new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(UiPackImport.PackFolder) });
                if (!atlas.CanBindTo(Spr("btn_booster_green"))) { throw new InvalidOperationException("UiSprites.spriteatlas không gồm Art/UI/Sprites/Pack."); }
                report.AppendLine(UiThemeAssets.Art + "/UiSprites.spriteatlas · SpriteAtlas · packable · thêm thư mục Sprites/Pack riêng (thư mục con không tự gộp)");
            }
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssetIfDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            report.AppendLine("Art/UI · assets · S3 · sprite từ Art/UI/Sprites/Pack trong UiSprites atlas (thư mục con); Static font padding≥9; preset HudOutline/HudSmall/CardText viền indigo đặc. Không đổi package/settings.");
        }

        // Font Static + 3 preset chữ dùng chung. Chạy riêng được bằng menu "S3 Apply Text Materials" (không đụng scene) khi chỉ chỉnh độ đậm/viền chữ.
        private static void BuildTextMaterials()
        {
            Set(theme, "pillOutline", Hex("24104F"));
            Set(theme, "bodyFont", UiThemeAssets.BakeFont("Baloo2-Bold", true));
            Set(theme, "titleFont", UiThemeAssets.BakeFont("Baloo2-ExtraBold", true));
            Set(theme, "hudOutline", Preset("HudOutline", theme.TitleFont, theme.HudOutlineWidth, theme.HudOutlineDilate, Color.white));
            Set(theme, "hudSmall", Preset("HudSmall", theme.BodyFont, theme.HudSmallOutlineWidth, theme.HudSmallDilate, Color.white));
            Set(theme, "cardText", Preset("CardText", theme.BodyFont, 0, 0, Hex("2A1760")));
            foreach (var font in new[] { theme.TitleFont, theme.BodyFont })
            {
                if (font.atlasPopulationMode != AtlasPopulationMode.Static || font.atlasPadding < 9 || font.atlasTextures.Length != 1 ||
                    !font.HasCharacters("ĐđẮằẾệỢự Thắng! ←↑→↓…∞"))
                { throw new InvalidOperationException("S3 yêu cầu font Static, padding≥9, đủ tiếng Việt/ký hiệu, một atlas: " + font.name); }
            }
        }

        [MenuItem("Tools/Squeaky Lane/013 Theme/S3 Apply Text Materials")]
        public static void ApplyTextMaterials()
        {
            UiThemeAssets.RequireEditMode();
            theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            UiSkinTools.ValidateTheme(theme);
            report = new StringBuilder();
            changeOrder.Clear();
            changes.Clear();
            UiThemeAssets.Backup(UiThemeAssets.ThemePath);
            // Số độ đậm/viền mặc định của S3; sau đó chỉnh trong UiTheme rồi chạy lại menu này.
            Set(theme, "hudOutlineWidth", .2f);
            Set(theme, "hudOutlineDilate", .25f);
            BuildTextMaterials();
            AssetDatabase.SaveAssets();
            foreach (string key in changeOrder)
            {
                var change = changes[key];
                if (change[1] != change[2]) { Debug.Log("013 S3 chữ · " + change[0] + " · " + change[1] + " → " + change[2]); }
            }
            Debug.Log("013 S3: đã áp preset chữ HudOutline (đậm " + theme.HudOutlineDilate + ", viền " + theme.HudOutlineWidth + "), HudSmall (đậm " + theme.HudSmallDilate + ", viền " + theme.HudSmallOutlineWidth + "). Không đổi scene.");
        }

        // Viền navy/indigo đặc, softness 0, không glow/underlay. TMP không tự tính lại tỉ lệ khi sửa bằng script nên gọi UpdateShaderRatios.
        private static Material Preset(string name, TMP_FontAsset font, float width, float dilate, Color face)
        {
            string path = UiThemeAssets.Art + "/Fonts/" + name + ".mat";
            UiThemeAssets.Backup(path);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(font.material); AssetDatabase.CreateAsset(material, path); }
            material.CopyPropertiesFromMaterial(font.material);
            material.name = name;
            material.SetColor("_FaceColor", HdrColor(face));
            material.SetColor("_OutlineColor", HdrColor(theme.PillOutline));
            material.SetFloat("_OutlineWidth", width);
            material.SetFloat("_OutlineSoftness", 0);
            // Face Dilate làm đậm nét: Baloo 2 mảnh hơn viền nên không có nó chữ trông rỗng.
            material.SetFloat("_FaceDilate", dilate);
            foreach (string keyword in new[] { "GLOW_ON", "UNDERLAY_ON", "UNDERLAY_INNER", "BEVEL_ON" }) { material.DisableKeyword(keyword); }
            foreach (string property in new[] { "_GlowPower", "_GlowOffset", "_GlowInner", "_GlowOuter", "_UnderlaySoftness", "_UnderlayDilate", "_UnderlayOffsetX", "_UnderlayOffsetY" })
            {
                if (material.HasProperty(property)) { material.SetFloat(property, 0); }
            }
            foreach (string property in new[] { "_GlowColor", "_UnderlayColor" })
            {
                if (material.HasProperty(property)) { material.SetColor(property, new Color(0, 0, 0, 0)); }
            }
            if (width > 0) { material.EnableKeyword("OUTLINE_ON"); }
            else { material.DisableKeyword("OUTLINE_ON"); }
            ShaderUtilities.UpdateShaderRatios(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        // Shader TMP khai báo _FaceColor/_OutlineColor là [HDR]: Unity KHÔNG đổi sRGB→linear cho thuộc tính HDR nên ở project Linear màu hiện sáng lên.
        // Truyền giá trị linear để ra đúng màu thiết kế. Màu chữ qua vertex color (m_fontColor) thì Unity đã tự đổi.
        private static Color HdrColor(Color color)
        {
            return PlayerSettings.colorSpace == ColorSpace.Linear ? color.linear : color;
        }

        private static Sprite Spr(string name) { return UiPackImport.Load(name); }

        private static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var color);
            return color;
        }

        // ---------------------------------------------------------------- dọn trang trí cũ

        // Xóa mọi trang trí "013 " (S2 cũ và S3 lần trước) trong SafeArea; cứu trước các object ràng buộc đang nằm trong chúng.
        private static void TearDown()
        {
            var safeArea = Ref<RectTransform>(data, "safeAreaRoot");
            var playing = Ref<CanvasGroup>(data, "playing").transform;
            foreach (string field in new[] { "clockText", "levelTitle", "movesText", "messageText" }) { Rescue(Ref<TMP_Text>(data, field).transform, playing); }
            Rescue(Ref<Image>(data, "clockFill").transform, playing);
            foreach (var button in nav.GetComponentsInChildren<Button>(true))
            {
                var label = FindLabel(button);
                if (label != null) { Rescue(label.transform, button.transform); }
            }
            foreach (string field in new[] { "wonText", "lostText" })
            {
                var text = Ref<TMP_Text>(data, field);
                Rescue(text.transform, Ref<CanvasGroup>(data, field == "wonText" ? "won" : "lost").transform);
            }
            // Nút và toggle gốc đang nằm trong khung 013 (lần chạy trước) trở về panel của chúng trước khi dọn.
            var panels = new[] { Ref<CanvasGroup>(data, "levelSelect"), Ref<CanvasGroup>(data, "playing"), Ref<CanvasGroup>(data, "paused"), Ref<CanvasGroup>(data, "won"), Ref<CanvasGroup>(data, "lost") };
            foreach (var control in nav.GetComponentsInChildren<Selectable>(true))
            {
                Transform home = null;
                for (var parent = control.transform.parent; parent != null && home == null; parent = parent.parent)
                {
                    foreach (var panel in panels) { if (panel.transform == parent) { home = parent; } }
                }
                if (home != null) { Rescue(control.transform, home); }
            }
            // Toggle: Background/Checkmark là object gốc, các lớp 013 trong chúng bị xóa và dựng lại bên dưới.
            foreach (var node in safeArea.GetComponentsInChildren<Transform>(true))
            {
                if (node == null || node == safeArea || !node.name.StartsWith("013 ", StringComparison.Ordinal)) { continue; }
                report.AppendLine(PathOf(node) + " · GameObject · tồn tại · có → không (trang trí cũ dựng lại)");
                Undo.DestroyObjectImmediate(node.gameObject);
            }
            foreach (var decoration in safeArea.GetComponentsInChildren<UiPanelDecoration>(true))
            {
                if (decoration != null && decoration.transform != Ref<CanvasGroup>(data, "levelSelect").transform) { Discard(decoration, "thẻ + lớp tối nay là các lớp riêng"); }
            }
        }

        private static void Rescue(Transform node, Transform home)
        {
            for (var parent = node.parent; parent != null; parent = parent.parent)
            {
                if (parent.name.StartsWith("013 ", StringComparison.Ordinal)) { Parent(node, home); return; }
            }
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildHud()
        {
            var playing = Ref<CanvasGroup>(data, "playing").transform;
            var clockText = Ref<TMP_Text>(data, "clockText");
            var clockFill = Ref<Image>(data, "clockFill");
            var moves = Ref<TMP_Text>(data, "movesText");
            var level = Ref<TMP_Text>(data, "levelTitle");
            var message = Ref<TMP_Text>(data, "messageText");
            // Pill "Màn N" ở góc trái trên.
            var levelPill = Plate(playing, "013 LevelPill", Spr("Gameplay_holder_baisc"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -10), LevelPillSize, true);
            Parent(level.transform, levelPill);
            Stretch(level.rectTransform, 2 * Unit);
            Text(level, theme.HudSmall, Color.white, 13 * Unit);
            Set(level, "m_text", "Màn 1");
            // Đồng hồ: chỉ còn chữ mm:ss trong pill (không dành chỗ cho thanh chạy). clockFill giữ vai trò Image nền, NavigationView đổi sprite khi sắp hết giờ.
            var clockPill = Node(playing, "013 ClockPill");
            Rect(clockPill, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -8), ClockPillSize);
            Parent(clockFill.transform, clockPill);
            Set(clockFill, "m_Sprite", theme.ClockNormal);
            Set(clockFill, "m_Color", Color.white);
            Set(clockFill, "m_FillAmount", 1f);
            Set(clockFill, "m_FillCenter", true);
            FitScaleBy(clockFill, ClockPillSize.y * Unit, true);
            First(clockFill.transform);
            Parent(clockText.transform, clockPill);
            Stretch(clockText.rectTransform, 2 * Unit);
            Text(clockText, theme.HudOutline, Color.white, 20 * Unit);
            Set(clockText, "m_text", "00:31");
            // Số nước dưới đồng hồ.
            var movesPill = Plate(playing, "013 MovesPill", Spr("bg_info_holder_purple"), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -44), MovesPillSize, true);
            Parent(moves.transform, movesPill);
            Stretch(moves.rectTransform, Unit);
            Text(moves, theme.HudSmall, Color.white, 10 * Unit);
            Set(moves, "m_text", "0 nước");
            // Nút cài đặt: bánh răng nhỏ trên nền tròn mờ ở góc phải trên (Pause là nút này; vẫn là pauseButton của NavigationView).
            var gear = Ref<Button>(data, "pauseButton");
            StyleHudButton(gear, Spr("bsc_circle_512px"), false, Spr("ic_settings"), GearIcon, new Color(.1f, .04f, .3f, .55f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -11), GearSize);
            // Hai booster ở đáy.
            Booster(Ref<Button>(data, "undoButton"), false);
            Booster(Ref<Button>(data, "hintButton"), true);
            var restarts = data.FindProperty("restartButtons");
            for (int i = 0; i < restarts.arraySize; i++)
            {
                var button = restarts.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                if (button != null && button.transform.IsChildOf(playing)) { Set(button.gameObject, "m_IsActive", false); }
            }
            // Thông báo ngắn: bong bóng trắng viền vàng; ẩn mặc định, NavigationView hiện/mờ.
            var bubble = Plate(playing, "013 MessageBubble", Spr("bg_coin_holder"), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -70), BubbleSize, true);
            Parent(message.transform, bubble);
            Stretch(message.rectTransform, 4 * Unit);
            Text(message, theme.CardText, Color.white, 12 * Unit);
            Set(message, "m_text", "");
            var group = Add<CanvasGroup>(bubble.gameObject);
            Set(group, "m_Alpha", 0f);
            Set(group, "m_Interactable", false);
            Set(group, "m_BlocksRaycasts", false);
            Set(nav, "messageBubble", group);
        }

        private static void Booster(Button button, bool hint)
        {
            var bodySprite = hint ? theme.HintOn : Spr("btn_booster_green");
            StyleHudButton(button, bodySprite, true, hint ? Spr("icon_hint") : Spr("ic_arrow"), BoosterIcon, Color.white,
                new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(hint ? 50 : -50, 44), Vector2.one * theme.BoosterSize);
            var visual = button.transform.Find("013 ButtonVisual");
            var label = FindLabel(button);
            var tag = Plate(visual, "013 LabelPill", Spr("bg_booster_timer_holder"), new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -9), LabelPillSize, true);
            Parent(label.transform, visual);
            Rect(label.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -9), LabelPillSize);
            Text(label, theme.HudSmall, Color.white, 11 * Unit);
            Set(label, "m_text", hint ? "Gợi ý" : "Hoàn tác");
            var badge = Plate(visual, "013 Badge", theme.BadgeOn, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(hint ? 22 : 34, 22), true);
            var number = TextNode(badge, "013 BadgeText");
            Stretch(number.rectTransform, 2 * Unit);
            Text(number, theme.HudSmall, Color.white, 12 * Unit);
            Set(number, "m_text", hint ? "3" : "-3s");
            Set(nav, hint ? "hintBadge" : "undoBadge", number);
            // NavigationView.Initialize lấy TMP đầu tiên trong nút làm nhãn nên nhãn phải đứng TRƯỚC số huy hiệu.
            int index = 0;
            foreach (var node in new[] { visual.Find("013 Body"), visual.Find("013 Icon"), (Transform)tag, label.transform, (Transform)badge })
            {
                if (node != null) { Sibling(node, index++); }
            }
            if (hint) { Set(nav, "hintBadgePlate", badge.Find("013 Skin").GetComponent<Image>()); }
        }

        // Nút HUD: root giữ vùng chạm; visual chứa MỘT Image thân (sprite có sẵn viền) + icon.
        private static void StyleHudButton(Button button, Sprite body, bool bodySliced, Sprite icon, float iconSize, Color tint,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var root = (RectTransform)button.transform;
            var hit = button.GetComponent<Image>();
            if (hit == null) { throw new InvalidOperationException("Nút thiếu Image: " + PathOf(root)); }
            Rect(root, anchor, pivot, position, size);
            var visual = Node(root, "013 ButtonVisual");
            Stretch(visual, 0);
            Set(hit, "m_Color", Color.clear);
            Set(hit, "m_RaycastTarget", true);
            var skin = Skin(visual, body, tint, "013 Body");
            FitBy(skin, size.y * Unit, bodySliced);
            Transitions(button, skin, button.name == "HintButton");
            var label = FindLabel(button);
            if (label != null)
            {
                Parent(label.transform, visual);
                Text(label, theme.HudSmall, Color.white, theme.BodyFontSize);
                if (button.name == "PauseButton") { Set(label.gameObject, "m_IsActive", false); }
            }
            Icon(visual, "013 Icon", icon, iconSize, Vector2.zero);
            if (label != null) { Last(label.transform); }
            var press = Add<UiButtonPress>(button.gameObject);
            Set(press, "theme", theme);
            Set(press, "label", visual);
        }

        private static void Transitions(Button button, Image target, bool keepWhenDisabled)
        {
            Set(button, "m_TargetGraphic", target);
            Set(button, "m_Transition", (int)Selectable.Transition.ColorTint);
            Set(button, "m_Colors.m_NormalColor", Color.white);
            Set(button, "m_Colors.m_HighlightedColor", Color.white);
            Set(button, "m_Colors.m_SelectedColor", Color.white);
            Set(button, "m_Colors.m_PressedColor", new Color(theme.PressedTint, theme.PressedTint, theme.PressedTint, 1));
            // Hint hết lượt đổi sang sprite xám nên giữ trắng; nút khác bị khóa chỉ tối nhẹ.
            Set(button, "m_Colors.m_DisabledColor", keepWhenDisabled ? Color.white : new Color(theme.DisabledTint, theme.DisabledTint, theme.DisabledTint, 1));
        }

        // ---------------------------------------------------------------- hộp thoại nhiều lớp

        // Cấu trúc mỗi hộp thoại (tên object = lớp, đổi sprite/màu/vị trí trong Inspector):
        //   <Panel>             Image: lớp tối toàn màn (chặn chạm)
        //     013 Card          khung giữa màn
        //       013 Frame       Image khung (bg_popup_purple)
        //       013 Header      Image ribbon + 013 Title (chữ tiêu đề)
        //       013 Close       nút X (chỉ Tạm dừng; là "Tiếp tục")
        //       013 Body        VerticalLayoutGroup: 013 Buttons (VerticalLayoutGroup) [+ 013 Toggles (HorizontalLayoutGroup)]
        private static void BuildDialogs()
        {
            var paused = Ref<CanvasGroup>(data, "paused");
            var body = DialogCard(paused, PausedHeight, "Tạm dừng", Color.white, true, out var pausedTitle);
            var buttons = ButtonColumn(body);
            DialogButton(buttons, Ref<Button>(data, "resumeButton"), "pop_up_btn_green");
            DialogButton(buttons, FindButton(paused, "RestartButton"), "pop_up_btn_orange");
            DialogButton(buttons, FindButton(paused, "SelectButton"), "pop_up_btn_purple_small");
            var toggles = Node(body, "013 Toggles");
            var row = Add<HorizontalLayoutGroup>(toggles.gameObject);
            ConfigureGroup(row, 14, TextAnchor.MiddleCenter, false);
            Layout(toggles, -1, (ToggleFrame + ToggleCaption + 4) * Unit);
            BuildToggle(FindToggle(paused, "SoundToggle"), "Âm thanh", Spr("ic_sound_green"), Spr("ic_sound_purple"), toggles, true);
            BuildToggle(FindToggle(paused, "HapticsToggle"), "Rung", Spr("ic_vibration_green"), Spr("ic_vibration_purple"), toggles, true);

            var won = Ref<CanvasGroup>(data, "won");
            var wonBody = DialogCard(won, WonHeight, "Thắng!", Hex("FFE066"), false, out _);
            var wonText = Ref<TMP_Text>(data, "wonText");
            Parent(wonText.transform, wonBody);
            Text(wonText, theme.HudSmall, Color.white, 18 * Unit);
            Set(wonText, "m_text", theme.Stars(3) + "\nMàn 1 · còn 36 s");
            Layout(wonText.rectTransform, -1, 60 * Unit);
            var wonButtons = ButtonColumn(wonBody);
            DialogButton(wonButtons, Ref<Button>(data, "nextButton"), "pop_up_btn_green");
            DialogButton(wonButtons, FindButton(won, "RestartButton"), "pop_up_btn_orange");
            DialogButton(wonButtons, FindButton(won, "SelectButton"), "pop_up_btn_purple_small");

            var lost = Ref<CanvasGroup>(data, "lost");
            var lostBody = DialogCard(lost, LostHeight, "", Hex("FF8A8A"), false, out var lostTitle);
            // lostText (NavigationView ghi "Hết giờ") chính là chữ tiêu đề trên ribbon.
            var lostText = Ref<TMP_Text>(data, "lostText");
            Parent(lostText.transform, lostTitle.transform.parent);
            Destroy(lostTitle.gameObject);
            Stretch(lostText.rectTransform, 0);
            Text(lostText, theme.HudOutline, Hex("FF8A8A"), 22 * Unit);
            Set(lostText, "m_text", "Hết giờ");
            var lostButtons = ButtonColumn(lostBody);
            DialogButton(lostButtons, FindButton(lost, "RestartButton"), "pop_up_btn_orange");
            DialogButton(lostButtons, FindButton(lost, "SelectButton"), "pop_up_btn_purple_small");
        }

        private static RectTransform DialogCard(CanvasGroup group, float height, string title, Color titleColor, bool close, out TMP_Text titleText)
        {
            var dim = group.GetComponent<Image>();
            if (dim == null) { throw new InvalidOperationException("Panel thiếu Image: " + group.name); }
            Set(dim, "m_Sprite", null);
            Set(dim, "m_Color", new Color(.06f, .03f, .16f, theme.DimAlpha));
            Set(dim, "m_Type", (int)UnityEngine.UI.Image.Type.Simple);
            Set(dim, "m_RaycastTarget", true);
            var card = Node(group.transform, "013 Card");
            Rect(card, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(theme.CardWidth, height));
            var frame = Skin(card, Spr("bg_popup_purple"), Color.white, "013 Frame");
            FitScale(frame, theme.FrameScale * Unit, true);
            // Ribbon tiêu đề nằm đè mép trên thẻ.
            var ribbon = Spr("bg_pop_up_header");
            var spec = UiPackImport.SpecOf(ribbon);
            float scale = HeaderWidth * Unit / spec.VisibleW;
            var header = Node(card, "013 Header");
            Rect(header, new Vector2(.5f, 1), new Vector2(.5f, .5f), new Vector2(0, 4), new Vector2(HeaderWidth, spec.VisibleH * scale / Unit));
            FitScale(Skin(header, ribbon, Color.white, "013 Skin"), scale, false);
            titleText = TextNode(header, "013 Title");
            Stretch(titleText.rectTransform, 0);
            Text(titleText, theme.HudOutline, titleColor, 22 * Unit);
            Set(titleText, "m_text", title);
            if (close)
            {
                var button = CloseButton(card);
                SetArray(nav, "extraResumeButtons", new[] { button });
            }
            var body = Node(card, "013 Body");
            Set(body, "m_AnchorMin", Vector2.zero);
            Set(body, "m_AnchorMax", Vector2.one);
            Set(body, "m_Pivot", new Vector2(.5f, .5f));
            Set(body, "m_OffsetMin", new Vector2(16 * Unit, 14 * Unit));
            Set(body, "m_OffsetMax", new Vector2(-16 * Unit, -34 * Unit));
            var layout = Add<VerticalLayoutGroup>(body.gameObject);
            ConfigureGroup(layout, 8, TextAnchor.UpperCenter, true);
            return body;
        }

        private static Button CloseButton(Transform card)
        {
            var node = Node(card, "013 Close");
            Rect(node, Vector2.one, new Vector2(.5f, .5f), new Vector2(-8, -4), Vector2.one * 32);
            var hit = Add<Image>(node.gameObject);
            Set(hit, "m_Color", Color.clear);
            Set(hit, "m_RaycastTarget", true);
            var button = Add<Button>(node.gameObject);
            var visual = Node(node, "013 ButtonVisual");
            Stretch(visual, 0);
            var skin = Skin(visual, Spr("ic_close"), Color.white, "013 Body");
            FitBy(skin, 32 * Unit, false);
            Transitions(button, skin, false);
            var press = Add<UiButtonPress>(node.gameObject);
            Set(press, "theme", theme);
            Set(press, "label", visual);
            return button;
        }

        private static RectTransform ButtonColumn(RectTransform body)
        {
            var column = Node(body, "013 Buttons");
            var layout = Add<VerticalLayoutGroup>(column.gameObject);
            ConfigureGroup(layout, 6, TextAnchor.UpperCenter, true);
            return column;
        }

        // Nút hộp thoại: root (vùng chạm, do layout định cỡ) + visual với một Image thân + nhãn.
        private static void DialogButton(RectTransform column, Button button, string spriteName)
        {
            var root = (RectTransform)button.transform;
            var hit = button.GetComponent<Image>();
            var label = FindLabel(button);
            if (hit == null || label == null) { throw new InvalidOperationException("Nút thiếu Image/TMP: " + PathOf(root)); }
            Parent(root, column);
            Layout(root, -1, DialogButtonHeight * Unit);
            Set(hit, "m_Color", Color.clear);
            Set(hit, "m_RaycastTarget", true);
            var visual = Node(root, "013 ButtonVisual");
            Stretch(visual, 0);
            var skin = Skin(visual, Spr(spriteName), Color.white, "013 Body");
            FitBy(skin, DialogButtonHeight * Unit, true);
            Transitions(button, skin, false);
            Parent(label.transform, visual);
            Stretch(label.rectTransform, 4 * Unit);
            Text(label, theme.HudOutline, Color.white, 17 * Unit);
            Set(label, "m_text", ButtonLabel(button.name));
            Last(label.transform);
            var press = Add<UiButtonPress>(button.gameObject);
            Set(press, "theme", theme);
            Set(press, "label", visual);
        }

        // Toggle: khung vàng + icon tắt (luôn có dưới) + icon bật (Checkmark của Toggle, chỉ hiện khi bật) + chữ chú thích.
        private static void BuildToggle(Toggle toggle, string caption, Sprite on, Sprite off, Transform parent, bool dialog)
        {
            var root = (RectTransform)toggle.transform;
            var background = toggle.targetGraphic as Image;
            var check = toggle.graphic as Image;
            if (background == null || check == null) { throw new InvalidOperationException("Toggle thiếu Background/Checkmark: " + PathOf(root)); }
            if (dialog)
            {
                Parent(root, parent);
                Layout(root, ToggleFrame * Unit, (ToggleFrame + ToggleCaption + 4) * Unit);
            }
            Set(background, "m_Sprite", Spr("bg_frame_setting_button_holder"));
            Set(background, "m_Color", Color.white);
            Set(background, "m_Type", (int)UnityEngine.UI.Image.Type.Simple);
            Set(background, "m_PreserveAspect", false);
            Set(background.rectTransform, "m_AnchorMin", dialog ? new Vector2(.5f, 1) : new Vector2(0, .5f));
            Set(background.rectTransform, "m_AnchorMax", dialog ? new Vector2(.5f, 1) : new Vector2(0, .5f));
            Set(background.rectTransform, "m_Pivot", dialog ? new Vector2(.5f, 1) : new Vector2(0, .5f));
            Set(background.rectTransform, "m_AnchoredPosition", Vector2.zero);
            Set(background.rectTransform, "m_SizeDelta", Vector2.one * (dialog ? ToggleFrame : 40) * Unit);
            // Icon tắt nằm dưới Checkmark; Checkmark (icon bật) do Toggle bật/tắt.
            var iconOff = Icon(background.transform, "013 IconOff", off, ToggleIcon * (dialog ? 1 : .8f), Vector2.zero);
            Parent(check.transform, background.transform);
            Set(check, "m_Sprite", on);
            Set(check, "m_Color", Color.white);
            Set(check, "m_Type", (int)UnityEngine.UI.Image.Type.Simple);
            Set(check, "m_PreserveAspect", true);
            var spec = UiPackImport.SpecOf(on);
            float scale = ToggleIcon * (dialog ? 1 : .8f) * Unit / Mathf.Max(spec.VisibleW, spec.VisibleH);
            Set(check.rectTransform, "m_AnchorMin", new Vector2(.5f, .5f));
            Set(check.rectTransform, "m_AnchorMax", new Vector2(.5f, .5f));
            Set(check.rectTransform, "m_Pivot", new Vector2(.5f, .5f));
            Set(check.rectTransform, "m_SizeDelta", new Vector2(spec.w, spec.h) * scale);
            Set(check.rectTransform, "m_AnchoredPosition", new Vector2(spec.right - spec.left, spec.top - spec.bottom) * scale * .5f);
            Sibling(check.transform, background.transform.childCount - 1);
            // Chữ Label cũ là UI.Text legacy (Arial, màu mặc định): ẩn đi và dùng chú thích TMP.
            foreach (var legacy in toggle.GetComponentsInChildren<Text>(true)) { Set(legacy.gameObject, "m_IsActive", false); }
            var text = TextNode(root, "013 Caption");
            if (dialog)
            {
                Rect(text.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(ToggleFrame + 14, ToggleCaption));
                Text(text, theme.HudSmall, Color.white, 11 * Unit);
            }
            else
            {
                Set(text.rectTransform, "m_AnchorMin", Vector2.zero);
                Set(text.rectTransform, "m_AnchorMax", Vector2.one);
                Set(text.rectTransform, "m_OffsetMin", new Vector2(46 * Unit, 0));
                Set(text.rectTransform, "m_OffsetMax", Vector2.zero);
                Text(text, theme.CardText, Color.white, 11 * Unit);
                Set(text, "m_HorizontalAlignment", (int)HorizontalAlignmentOptions.Left);
                // Chú thích một dòng ("Âm thanh"), không xuống dòng.
                Set(text, "m_enableWordWrapping", false);
            }
            Set(text, "m_text", caption);
            Set(toggle, "m_Transition", (int)Selectable.Transition.ColorTint);
            Set(toggle, "m_Colors.m_PressedColor", new Color(theme.PressedTint, theme.PressedTint, theme.PressedTint, 1));
            if (iconOff == null) { throw new InvalidOperationException("Không dựng được icon tắt: " + PathOf(root)); }
        }

        // ---------------------------------------------------------------- màn chọn, banner, LevelButton

        private static void BuildLevelSelect()
        {
            var select = Ref<CanvasGroup>(data, "levelSelect");
            // Lưới 3 cột phải lọt viewport trừ thanh cuộn (màn dài chỉ ~980 đơn vị Canvas): ô hẹp lại một chút và canh giữa.
            Set(theme, "levelCellSize", new Vector2(256, 180));
            var grid = Ref<Transform>(data, "levelButtonContent").GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                Set(grid, "m_CellSize", theme.LevelCellSize);
                Set(grid, "m_ChildAlignment", (int)TextAnchor.UpperCenter);
            }
            BuildToggle(FindToggle(select, "SoundToggle"), "Âm thanh", Spr("ic_sound_green"), Spr("ic_sound_purple"), select.transform, false);
            BuildToggle(FindToggle(select, "HapticsToggle"), "Rung", Spr("ic_vibration_green"), Spr("ic_vibration_purple"), select.transform, false);
        }

        private static void BuildStuckBanner()
        {
            var banner = Ref<CanvasGroup>(data, "stuckBanner");
            var image = banner.GetComponent<Image>();
            if (image != null)
            {
                // Banner kéo giãn theo neo (cao ≈ 7,5% màn tham chiếu), không có lề để đệm: dùng sprite đỏ cắt 9-slice theo cao đó.
                var rect = (RectTransform)banner.transform;
                float height = (rect.anchorMax.y - rect.anchorMin.y) * 1920f;
                var sprite = Spr("Gameplay_holder_superhard");
                var spec = UiPackImport.SpecOf(sprite);
                Set(image, "m_Sprite", sprite);
                Set(image, "m_Color", Color.white);
                Set(image, "m_Type", (int)UnityEngine.UI.Image.Type.Sliced);
                Set(image, "m_PixelsPerUnitMultiplier", spec.VisibleH / height);
            }
            var text = banner.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                Text(text, theme.HudOutline, Color.white, 22 * Unit);
                Set(text, "m_text", "Kẹt xe — hoàn tác?");
            }
        }

        // LevelButton: một Image thân xanh; ô bị khóa đổi sang sprite xám bằng SpriteSwap (không cần code).
        private static void StyleLevelButton(Button button)
        {
            var root = (RectTransform)button.transform;
            var hit = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (hit == null || label == null) { throw new InvalidOperationException("LevelButton thiếu Image/TMP"); }
            // Dọn trang trí cũ của S2/S3 lần trước trong prefab, giữ nhãn.
            if (label.transform.parent != root) { Parent(label.transform, root); }
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node == null || node == root || !node.name.StartsWith("013 ", StringComparison.Ordinal)) { continue; }
                report.AppendLine(PathOf(node) + " · GameObject · tồn tại · có → không (LevelButton dựng lại)");
                Undo.DestroyObjectImmediate(node.gameObject);
            }
            Set(hit, "m_Color", Color.clear);
            Set(hit, "m_RaycastTarget", true);
            var visual = Node(root, "013 ButtonVisual");
            Stretch(visual, 0);
            var green = Spr("Btn_square_green_v1");
            var grey = Spr("Btn_square_grey_v1");
            var skin = Skin(visual, green, Color.white, "013 Body");
            FitBy(skin, theme.LevelCellSize.y, true);
            Set(button, "m_TargetGraphic", skin);
            Set(button, "m_Transition", (int)Selectable.Transition.SpriteSwap);
            Set(button, "m_SpriteState.m_HighlightedSprite", green);
            Set(button, "m_SpriteState.m_PressedSprite", green);
            Set(button, "m_SpriteState.m_SelectedSprite", green);
            Set(button, "m_SpriteState.m_DisabledSprite", grey);
            Parent(label.transform, visual);
            Stretch(label.rectTransform, 8 * Unit);
            Text(label, theme.HudOutline, Color.white, theme.BodyFontSize);
            Set(label, "m_text", "Màn 1\n" + theme.Stars(0));
            var press = Add<UiButtonPress>(button.gameObject);
            Set(press, "theme", theme);
            Set(press, "label", visual);
        }

        // ---------------------------------------------------------------- dựng lớp và xếp rect

        // Node bố cục có sprite nền (một Image con "013 Skin" phủ phần thân cộng lề trong suốt của sprite).
        private static RectTransform Plate(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, bool sliced)
        {
            var node = Node(parent, name);
            Rect(node, anchor, pivot, position, size);
            FitBy(Skin(node, sprite, Color.white, "013 Skin"), size.y * Unit, sliced);
            return node;
        }

        private static Image Skin(Transform layout, Sprite sprite, Color color, string name)
        {
            var image = Add<Image>(Node(layout, name).gameObject);
            Set(image, "m_Sprite", sprite);
            Set(image, "m_Color", color);
            Set(image, "m_RaycastTarget", false);
            First(image.transform);
            return image;
        }

        // Co đều sao cho phần NHÌN THẤY của sprite cao đúng bodyHeight (đơn vị Canvas); lề trong suốt của sprite thò ra ngoài node bố cục.
        private static void FitBy(Image skin, float bodyHeight, bool sliced)
        {
            FitScale(skin, bodyHeight / UiPackImport.SpecOf(skin.sprite).VisibleH, sliced);
        }

        private static void FitScaleBy(Image skin, float bodyHeight, bool sliced) { FitBy(skin, bodyHeight, sliced); }

        private static void FitScale(Image skin, float scale, bool sliced)
        {
            var spec = UiPackImport.SpecOf(skin.sprite);
            var rect = skin.rectTransform;
            Set(rect, "m_AnchorMin", Vector2.zero);
            Set(rect, "m_AnchorMax", Vector2.one);
            Set(rect, "m_Pivot", new Vector2(.5f, .5f));
            Set(rect, "m_AnchoredPosition", new Vector2(spec.right - spec.left, spec.top - spec.bottom) * scale * .5f);
            Set(rect, "m_SizeDelta", new Vector2(spec.left + spec.right, spec.bottom + spec.top) * scale);
            Set(skin, "m_Type", (int)(sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple));
            Set(skin, "m_PreserveAspect", false);
            // Sprite PPU 100 và Canvas reference PPU 100: viền vẽ = border_px × scale.
            Set(skin, "m_PixelsPerUnitMultiplier", 1f / scale);
            Set(skin, "m_RaycastTarget", false);
        }

        private static Image Icon(Transform parent, string name, Sprite sprite, float sizeMock, Vector2 center)
        {
            var image = Add<Image>(Node(parent, name).gameObject);
            var spec = UiPackImport.SpecOf(sprite);
            float scale = sizeMock * Unit / Mathf.Max(spec.VisibleW, spec.VisibleH);
            var rect = image.rectTransform;
            Set(image, "m_Sprite", sprite);
            Set(image, "m_Color", Color.white);
            Set(image, "m_Type", (int)UnityEngine.UI.Image.Type.Simple);
            Set(image, "m_PreserveAspect", false);
            Set(image, "m_RaycastTarget", false);
            Set(rect, "m_AnchorMin", new Vector2(.5f, .5f));
            Set(rect, "m_AnchorMax", new Vector2(.5f, .5f));
            Set(rect, "m_Pivot", new Vector2(.5f, .5f));
            Set(rect, "m_SizeDelta", new Vector2(spec.w, spec.h) * scale);
            Set(rect, "m_AnchoredPosition", center * Unit + new Vector2(spec.right - spec.left, spec.top - spec.bottom) * scale * .5f);
            return image;
        }

        private static void ConfigureGroup(HorizontalOrVerticalLayoutGroup group, float spacing, TextAnchor alignment, bool expandChildWidth)
        {
            Undo.RecordObject(group, "013 S3 layout");
            group.spacing = spacing * Unit;
            group.childAlignment = alignment;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = expandChildWidth;
            group.childForceExpandHeight = false;
            EditorUtility.SetDirty(group);
        }

        private static void Layout(Transform node, float preferredWidth, float preferredHeight)
        {
            var element = Add<LayoutElement>(node.gameObject);
            Undo.RecordObject(element, "013 S3 layout");
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.minHeight = preferredHeight;
            EditorUtility.SetDirty(element);
        }

        // ---------------------------------------------------------------- kiểm và tiện ích

        private static void VerifyResult()
        {
            foreach (var text in nav.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text == "Button" || text.text == "New Text") { throw new InvalidOperationException("Còn placeholder: " + PathOf(text.transform)); }
                if (text.spriteAsset != theme.Icons) { throw new InvalidOperationException("TMP chưa gán Sprite Asset UiIcons (sao sẽ thành emoji mặc định): " + PathOf(text.transform)); }
            }
            foreach (var effect in nav.GetComponentsInChildren<Shadow>(true))
            {
                if (effect.name.StartsWith("013 ", StringComparison.Ordinal)) { throw new InvalidOperationException("Còn Outline/Shadow: " + PathOf(effect.transform)); }
            }
            foreach (var image in nav.GetComponentsInChildren<Image>(true))
            {
                if ((image.name == "013 Body" || image.name == "013 Skin" || image.name == "013 Frame" || image.name == "013 Icon") && image.sprite == null)
                { throw new InvalidOperationException("Skin thiếu sprite: " + PathOf(image.transform)); }
            }
            foreach (var toggle in nav.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.graphic == null || toggle.targetGraphic == null) { throw new InvalidOperationException("Toggle thiếu Background/Checkmark: " + PathOf(toggle.transform)); }
            }
            if (Ref<Image>(data, "clockFill").sprite != theme.ClockNormal) { throw new InvalidOperationException("clockFill phải là nền pill đồng hồ."); }
        }

        private static TMP_Text FindLabel(Button button)
        {
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name != "013 BadgeText") { return text; }
            }
            return null;
        }

        private static Button FindButton(CanvasGroup panel, string name)
        {
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name) { return button; }
            }
            throw new InvalidOperationException("Không thấy " + name + " trong " + panel.name);
        }

        private static Toggle FindToggle(CanvasGroup panel, string name)
        {
            foreach (var toggle in panel.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.name == name) { return toggle; }
            }
            throw new InvalidOperationException("Không thấy " + name + " trong " + panel.name);
        }

        private static string ButtonLabel(string name)
        {
            switch (name)
            {
                case "ResumeButton": return "Tiếp tục";
                case "NextButton": return "Màn kế";
                case "RestartButton": return "Chơi lại";
                case "SelectButton": return "Chọn màn";
                default: return "";
            }
        }

        // Chỉ ra chỗ khác đầu tiên giữa hai ảnh chụp binding để biết field/nút nào đổi.
        private static string FirstDifference(string before, string after)
        {
            int i = 0;
            while (i < before.Length && i < after.Length && before[i] == after[i]) { i++; }
            int from = Math.Max(0, i - 60);
            return "Trước: …" + before.Substring(from, Math.Min(140, before.Length - from)) + " | Sau: …" + after.Substring(from, Math.Min(140, after.Length - from));
        }

        private static T Ref<T>(SerializedObject source, string field) where T : UnityEngine.Object => UiSkinTools.Ref<T>(source, field);
        private static string PathOf(Transform node) => node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
        private static T Add<T>(GameObject node) where T : Component
        {
            var component = node.GetComponent<T>();
            if (component != null) { return component; }
            component = Undo.AddComponent<T>(node);
            report.AppendLine(PathOf(node.transform) + " · " + typeof(T).Name + " · component · không → có");
            return component;
        }
        private static RectTransform Node(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) { return (RectTransform)existing; }
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = parent.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(node, "013 S3 layer");
            node.transform.SetParent(parent, false);
            report.AppendLine(PathOf(node.transform) + " · GameObject · tồn tại · không → có (lớp giao diện)");
            return node.GetComponent<RectTransform>();
        }
        private static TMP_Text TextNode(Transform parent, string name) => Add<TextMeshProUGUI>(Node(parent, name).gameObject);
        private static void Destroy(GameObject node)
        {
            report.AppendLine(PathOf(node.transform) + " · GameObject · tồn tại · có → không");
            Undo.DestroyObjectImmediate(node);
        }
        private static void Discard(Component component, string why)
        {
            report.AppendLine(PathOf(component.transform) + " · " + component.GetType().Name + " · component · có → không (" + why + ")");
            Undo.DestroyObjectImmediate(component);
        }
        private static void Text(TMP_Text text, Material preset, Color color, float size)
        {
            // Đổi font làm TMP tự thay material bằng material của font mới (atlas khác), nên chỉ gán khi thật sự khác để lần chạy lại không đổi gì.
            var font = preset == theme.HudOutline ? theme.TitleFont : theme.BodyFont;
            if (text.font != font) { Set(text, "m_fontAsset", font); }
            if (text.fontSharedMaterial != preset) { Set(text, "m_sharedMaterial", preset); }
            // Mọi TMP trong UI phải có Sprite Asset UiIcons; thiếu thì TMP rơi về sprite mặc định EmojiOne.
            Set(text, "m_spriteAsset", theme.Icons);
            Set(text, "m_fontColor", color);
            Set(text, "m_fontSize", size);
            Set(text, "m_fontSizeBase", size);
            Set(text, "m_enableAutoSizing", true);
            Set(text, "m_fontSizeMin", size * .7f);
            Set(text, "m_fontSizeMax", size);
            Set(text, "m_fontStyle", 0);
            Set(text, "m_HorizontalAlignment", (int)HorizontalAlignmentOptions.Center);
            Set(text, "m_VerticalAlignment", (int)VerticalAlignmentOptions.Middle);
            Set(text, "m_RaycastTarget", false);
            text.UpdateMeshPadding();
        }
        private static void Parent(Transform child, Transform parent)
        {
            if (child.parent == parent) { return; }
            string old = PathOf(child);
            Undo.SetTransformParent(child, parent, "013 S3 parent");
            Set(child, "m_LocalScale", Vector3.one);
            Set(child, "m_LocalRotation", Quaternion.identity);
            report.AppendLine(PathOf(child) + " · Transform · parent · " + old + " → " + PathOf(child));
        }
        private static void Rect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            Set(rect, "m_AnchorMin", anchor);
            Set(rect, "m_AnchorMax", anchor);
            Set(rect, "m_Pivot", pivot);
            Set(rect, "m_AnchoredPosition", position * Unit);
            Set(rect, "m_SizeDelta", size * Unit);
        }
        private static void Stretch(RectTransform rect, float inset)
        {
            Set(rect, "m_AnchorMin", Vector2.zero);
            Set(rect, "m_AnchorMax", Vector2.one);
            Set(rect, "m_Pivot", new Vector2(.5f, .5f));
            Set(rect, "m_SizeDelta", Vector2.one * -inset * 2);
            Set(rect, "m_AnchoredPosition", Vector2.zero);
        }
        private static void First(Transform node) { Sibling(node, 0); }
        private static void Last(Transform node) { Sibling(node, node.parent.childCount - 1); }
        private static void Sibling(Transform node, int index)
        {
            int old = node.GetSiblingIndex();
            Undo.RecordObject(node, "013 S3 draw order");
            node.SetSiblingIndex(index);
            Track(PathOf(node) + "|Transform|siblingIndex", PathOf(node) + " · Transform · siblingIndex", old.ToString(), index.ToString());
        }
        // Ghi nhận một lần đổi field: giữ giá trị trước lần đổi ĐẦU TIÊN và giá trị sau lần đổi CUỐI CÙNG.
        private static void Track(string key, string head, string before, string after)
        {
            if (!changes.TryGetValue(key, out var entry))
            {
                changes[key] = new[] { head, before, after };
                changeOrder.Add(key);
            }
            else { entry[2] = after; }
        }
        private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var source = new SerializedObject(target);
            var property = source.FindProperty(field);
            if (property == null) { throw new InvalidOperationException(target.name + " thiếu field " + field); }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) { property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            source.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Track(PathOf(((Component)target).transform) + "|" + target.GetType().Name + "|" + field, PathOf(((Component)target).transform) + " · " + target.GetType().Name + " · " + field, "(cũ)", values.Length + " phần tử");
        }
        private static string Value(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    var obj = property.objectReferenceValue;
                    return obj == null ? "null" : obj is Component c ? PathOf(c.transform) + " [" + c.GetType().Name + "]" : obj.name + " [" + AssetDatabase.GetAssetPath(obj) + "]";
                case SerializedPropertyType.Color: return "#" + ColorUtility.ToHtmlStringRGBA(property.colorValue);
                case SerializedPropertyType.String: return property.stringValue.Replace("\n", "\\n");
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return property.vector3Value.ToString();
                case SerializedPropertyType.Quaternion: return property.quaternionValue.ToString();
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum: return property.intValue.ToString();
                case SerializedPropertyType.Float: return property.floatValue.ToString("0.###");
                default: return property.propertyType.ToString();
            }
        }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var source = new SerializedObject(target);
            var property = source.FindProperty(field);
            // OffsetMin/Max là property tính toán của RectTransform, không có field serialize.
            if (property == null && target is RectTransform rect && (field == "m_OffsetMin" || field == "m_OffsetMax"))
            {
                var old = field == "m_OffsetMin" ? rect.offsetMin : rect.offsetMax;
                Undo.RecordObject(rect, "013 S3 rect inset");
                if (field == "m_OffsetMin") { rect.offsetMin = (Vector2)value; }
                else { rect.offsetMax = (Vector2)value; }
                Track(PathOf(rect) + "|RectTransform|" + field, PathOf(rect) + " · RectTransform · " + field, old.ToString(), value.ToString());
                return;
            }
            if (property == null) { throw new InvalidOperationException(target.name + " thiếu field " + field); }
            string before = Value(property);
            if (value is Color color) { property.colorValue = color; }
            else if (value is Vector2 v2) { property.vector2Value = v2; }
            else if (value is Vector3 v3) { property.vector3Value = v3; }
            else if (value is Quaternion rotation) { property.quaternionValue = rotation; }
            else if (value is string text) { property.stringValue = text; }
            else if (value is float number) { property.floatValue = number; }
            else if (value is int integer) { property.intValue = integer; }
            else if (value is bool boolean) { property.boolValue = boolean; }
            else { property.objectReferenceValue = (UnityEngine.Object)value; }
            source.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            if (target is Component c && PrefabUtility.IsPartOfPrefabInstance(c)) { PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            string after = Value(property);
            string owner = target is Component component ? PathOf(component.transform) : AssetDatabase.GetAssetPath(target);
            Track(owner + "|" + target.GetType().Name + "|" + field, owner + " · " + target.GetType().Name + " · " + field, before, after);
        }
    }
}
