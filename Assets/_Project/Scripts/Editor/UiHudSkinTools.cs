using System;
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
    // S2 là một lần sửa hàng loạt có backup và so binding; không chạy lại P1/S1 hay áp S3.
    public static class UiHudSkinTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/LevelButton.prefab";
        private const string ReportPath = "docs/handoff/013-scene-changes.md";
        private static StringBuilder report;
        private static UiTheme theme;
        private static float Unit => theme.HudUnit;

        [MenuItem("Tools/Squeaky Lane/013 Theme/S2 Apply Chunky HUD")]
        public static void Apply()
        {
            UiThemeAssets.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || scene.path != ScenePath || scene.isDirty)
            { throw new InvalidOperationException("Đóng Prefab Mode, mở Play.unity và lưu phần đang sửa trước khi áp S2."); }
            theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            UiSkinTools.ValidateTheme(theme);
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            if (nav == null || nav.gameObject.scene != scene) { throw new InvalidOperationException("Thiếu NavigationView trong Play."); }
            var data = new SerializedObject(nav);
            var scaler = nav.GetComponentInParent<CanvasScaler>();
            if (scaler == null) { throw new InvalidOperationException("Thiếu CanvasScaler để đổi từ mockup 300 px."); }
            string bindings = UiSkinTools.CaptureBindings(nav);
            var originals = nav.GetComponentsInChildren<Transform>(true);
            var names = Array.ConvertAll(originals, node => node.name);
            foreach (string path in new[] { ScenePath, PrefabPath, UiThemeAssets.ThemePath }) { UiThemeAssets.Backup(path); }
            report = new StringBuilder("\n## S2 · công thức nút + HUD · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n");
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("013 S2 Chunky HUD");
            try
            {
                Set(theme, "hudUnit", scaler.referenceResolution.x / 300f);
                BuildAssets();
                foreach (var text in nav.GetComponentsInChildren<TMP_Text>(true))
                {
                    bool hud = text.transform.IsChildOf(Ref<CanvasGroup>(data, "playing").transform);
                    Text(text, hud ? theme.HudSmall : theme.CardText, Color.white, text.fontSize);
                    if (text.text == "Button" || text.text == "New Text") { Set(text, "m_text", Placeholder(text)); }
                }
                Set(nav, "theme", theme);
                Set(nav, "normalClock", theme.ClockColor);
                Set(nav, "lowClock", theme.LowClock);
                foreach (var button in nav.GetComponentsInChildren<Button>(true))
                {
                    var playing = Ref<CanvasGroup>(data, "playing").transform;
                    if (button.name == "RestartButton" && button.transform.IsChildOf(playing)) { continue; }
                    bool primary = button == Ref<Button>(data, "resumeButton") || button == Ref<Button>(data, "nextButton") ||
                        button == Ref<Button>(data, "hintButton") || button.name == "RestartButton" && button.transform.parent.name == "Lost";
                    bool pause = button == Ref<Button>(data, "pauseButton");
                    bool booster = button == Ref<Button>(data, "undoButton") || button == Ref<Button>(data, "hintButton");
                    StyleButton(button, primary, pause, booster);
                }
                BuildHud(nav, data);
                var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    string before = UiSkinTools.CaptureButtonEvent(prefab.GetComponent<Button>());
                    StyleButton(prefab.GetComponent<Button>(), false, false, false);
                    Text(prefab.GetComponentInChildren<TMP_Text>(true), theme.HudSmall, Color.white, theme.BodyFontSize);
                    if (UiSkinTools.CaptureButtonEvent(prefab.GetComponent<Button>()) != before)
                    { throw new InvalidOperationException("OnClick LevelButton đã đổi; không lưu prefab."); }
                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                for (int i = 0; i < originals.Length; i++)
                {
                    if (originals[i] == null || originals[i].name != names[i]) { throw new InvalidOperationException("Tên/object cũ đã đổi; không lưu scene."); }
                }
                if (UiSkinTools.CaptureBindings(nav) != bindings) { throw new InvalidOperationException("Binding/OnClick NavigationView đã đổi; không lưu scene."); }
                foreach (var text in nav.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.text == "Button" || text.text == "New Text") { throw new InvalidOperationException("Còn placeholder: " + PathOf(text.transform)); }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                // Chạy lại menu thay đúng mục S2, không để nhiều bản cũ/mới trái nhau.
                string prior = File.Exists(ReportPath) ? File.ReadAllText(ReportPath) : "# 013 — Thay đổi scene/prefab\n";
                int start = prior.IndexOf("\n## S2 ·", StringComparison.Ordinal);
                if (start >= 0)
                {
                    int end = prior.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
                    prior = prior.Substring(0, start) + report + (end >= 0 ? prior.Substring(end) : "");
                }
                else { prior += report; }
                File.WriteAllText(ReportPath, prior, new UTF8Encoding(false));
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("013 S2 đã áp: Static padding10, đúng3 preset, atlas UI, nút5 lớp/pill/2 booster. Tên/binding/OnClick giữ nguyên. Run All + Console0, Play/reload, ảnh dọc01/30/SE + Stats16 568×320. Field cũ→mới: " + ReportPath);
            }
            catch
            {
                // Khôi phục scene trong bộ nhớ nếu kiểm tra thất bại; asset có backup ở Logs.
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("S2 dừng, chưa xác nhận hoàn tất. Không lưu scene dở; đọc lỗi và backup Logs/013-asset-backups trước khi thử tiếp.");
                throw;
            }
        }

        private static void BuildAssets()
        {
            const string sprites = UiThemeAssets.Art + "/Sprites/";
            Set(theme, "blueButton", UiThemeAssets.ImportSprite("kenney_ui-pack/PNG/Blue/Default/button_rectangle_depth_gradient.png", sprites + "blueButton.png", new Vector4(16, 16, 16, 16)));
            Set(theme, "blueSquare", UiThemeAssets.ImportSprite("kenney_ui-pack/PNG/Blue/Default/button_square_depth_gradient.png", sprites + "blueSquare.png", new Vector4(16, 16, 16, 16)));
            Set(theme, "yellowSquare", UiThemeAssets.ImportSprite("kenney_ui-pack/PNG/Yellow/Default/button_square_depth_gradient.png", sprites + "yellowSquare.png", new Vector4(16, 16, 16, 16)));
            Set(theme, "blueRound", UiThemeAssets.ImportSprite("kenney_ui-pack/PNG/Blue/Default/button_round_depth_gradient.png", sprites + "blueRound.png", new Vector4(31, 31, 31, 31)));
            // Sprite Grey có ruột trắng: tint ra đúng navy/đỏ; sprite Blue nhân màu đỏ sẽ thành tím.
            Set(theme, "pillSprite", theme.ClockTrack);
            Set(theme, "clockFill", theme.ClockTrack);
            Set(theme, "glintSprite", theme.PillSprite);
            Set(theme, "softShadow", BuildShadow(sprites + "softShadow.png"));
            SetBorder(theme.ClockTrack, new Vector4(8, 7, 8, 7));
            if (theme.ClockFill != theme.ClockTrack) { SetBorder(theme.ClockFill, new Vector4(8, 7, 8, 7)); }
            Set(theme, "bodyFont", UiThemeAssets.BakeFont("Baloo2-Bold", true));
            Set(theme, "titleFont", UiThemeAssets.BakeFont("Baloo2-ExtraBold", true));
            Set(theme, "hudOutline", Preset("HudOutline", theme.TitleFont, theme.HudOutlineWidth, Color.white));
            Set(theme, "hudSmall", Preset("HudSmall", theme.BodyFont, theme.HudSmallOutlineWidth, Color.white));
            Set(theme, "cardText", Preset("CardText", theme.BodyFont, 0, theme.Text));
            foreach (var font in new[] { theme.TitleFont, theme.BodyFont })
            {
                if (font.atlasPopulationMode != AtlasPopulationMode.Static || font.atlasPadding < 9 || font.atlasTextures.Length != 1 ||
                    !font.HasCharacters("ĐđẮằẾệỢự Thắng! ←↑→↓…∞"))
                { throw new InvalidOperationException("S2 yêu cầu font Static, padding≥9, đủ tiếng Việt/ký hiệu, một atlas: " + font.name); }
            }
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
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssetIfDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            report.AppendLine("Art/UI · assets · S2 · P1 padding8/không preset → Static padding10/GUID font+atlas+material cũ giữ nguyên, HudOutline/HudSmall/CardText, UiSprites atlas. Không đổi package/settings.");
        }

        private static Material Preset(string name, TMP_FontAsset font, float width, Color face)
        {
            string path = UiThemeAssets.Art + "/Fonts/" + name + ".mat";
            UiThemeAssets.Backup(path);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(font.material); AssetDatabase.CreateAsset(material, path); }
            material.CopyPropertiesFromMaterial(font.material);
            material.name = name;
            material.SetColor("_FaceColor", face);
            material.SetColor("_OutlineColor", theme.PillOutline);
            material.SetFloat("_OutlineWidth", width);
            material.SetFloat("_OutlineSoftness", 0);
            if (width > 0) { material.EnableKeyword("OUTLINE_ON"); }
            else { material.DisableKeyword("OUTLINE_ON"); }
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void SetBorder(Sprite sprite, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            string old = importer.spriteBorder.ToString();
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            report.AppendLine(AssetDatabase.GetAssetPath(sprite) + " · TextureImporter · spriteBorder · " + old + " → " + border);
        }

        private static Sprite BuildShadow(string path)
        {
            // PNG procedural duy nhất của S2. Màu/alpha và khoảng lệch vẫn lấy từ UiTheme.
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(Mathf.Abs((x + .5f) / size * 2 - 1) - .55f, 0);
                        float dy = Mathf.Max(Mathf.Abs((y + .5f) / size * 2 - 1) - .55f, 0);
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = 1 - Mathf.SmoothStep(0, .45f, distance);
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                }
                texture.Apply();
                UiThemeAssets.Backup(path);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(48, 48, 48, 48);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void StyleButton(Button button, bool primary, bool round, bool booster)
        {
            var root = (RectTransform)button.transform;
            var oldImage = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (oldImage == null || label == null) { throw new InvalidOperationException("Nút thiếu Image/TMP: " + PathOf(root)); }
            var visual = Node(root, "013 ButtonVisual");
            Stretch(visual, 0);
            // Root Image chỉ giữ vùng chạm. Các lớp con vẽ đúng thứ tự: bóng → thân → glint → icon/nhãn.
            Set(oldImage, "m_Color", Color.clear);
            Set(oldImage, "m_RaycastTarget", true);
            var shadow = ImageNode(visual, "013 SoftShadow", theme.SoftShadow, new Color(0, 0, 0, theme.ShadowAlpha));
            Stretch(shadow.rectTransform, -5 * Unit);
            Set(shadow.rectTransform, "m_AnchoredPosition", Vector2.down * theme.ShadowOffset * Unit);
            First(shadow.transform);
            var body = ImageNode(visual, "013 Body", round ? theme.BlueRound : booster ? primary ? theme.YellowSquare : theme.BlueSquare : primary ? theme.PrimaryButton : theme.BlueButton,
                primary ? theme.ButtonYellow : theme.ButtonBlue);
            Stretch(body.rectTransform, 0);
            Outline(body, theme.ButtonOutline, theme.OutlinePixels * Unit);
            Set(button, "m_TargetGraphic", body);
            Set(button, "m_Transition", (int)Selectable.Transition.ColorTint);
            Set(button, "m_Colors.m_NormalColor", Color.white);
            Set(button, "m_Colors.m_HighlightedColor", Color.white);
            Set(button, "m_Colors.m_SelectedColor", Color.white);
            Set(button, "m_Colors.m_PressedColor", new Color(theme.PressedTint, theme.PressedTint, theme.PressedTint, 1));
            Set(button, "m_Colors.m_DisabledColor", button.name == "HintButton" ? Color.white : theme.ExhaustedColor);
            var glint = ImageNode(visual, "013 Glint", theme.GlintSprite, new Color(1, 1, 1, theme.GlintAlpha));
            Rect(glint.rectTransform, new Vector2(0, 1), new Vector2(0, 1), theme.GlintOffset, theme.GlintSize);
            Set(glint.rectTransform, "m_LocalRotation", Quaternion.Euler(0, 0, theme.GlintAngle));
            Set(glint.gameObject, "m_IsActive", theme.ShowGlint);
            Parent(label.transform, visual);
            Text(label, theme.HudSmall, Color.white, theme.BodyFontSize);
            string icon = IconName(button.name);
            if (!string.IsNullOrEmpty(icon))
            {
                var image = ImageNode(visual, "013 Icon", AssetDatabase.LoadAssetAtPath<Sprite>(UiThemeAssets.Art + "/Sprites/icon_" + icon + ".png"), Color.white);
                Set(image, "m_Type", (int)UnityEngine.UI.Image.Type.Simple);
                Set(image, "m_PreserveAspect", true);
                Rect(image.rectTransform, round || booster ? new Vector2(.5f, .5f) : new Vector2(0, .5f), new Vector2(.5f, .5f),
                    round || booster ? Vector2.zero : new Vector2(22, 0), new Vector2(round ? 22 : 30, round ? 22 : 30));
                var shadowEffect = Add<Shadow>(image.gameObject);
                Set(shadowEffect, "m_EffectColor", new Color(theme.PillOutline.r, theme.PillOutline.g, theme.PillOutline.b, theme.IconShadowAlpha));
                Set(shadowEffect, "m_EffectDistance", theme.IconShadowOffset * Unit);
                if (round || booster) { Set(label, "m_text", round ? "" : booster && primary ? "Gợi ý" : "Hoàn tác"); }
                else
                {
                    Set(label, "m_text", ButtonLabel(button.name));
                    Set(label.rectTransform, "m_OffsetMin", new Vector2(40 * Unit, 0));
                    Set(label.rectTransform, "m_OffsetMax", new Vector2(-4 * Unit, 0));
                }
            }
            if (round) { Set(label.gameObject, "m_IsActive", false); }
            var press = Add<UiButtonPress>(button.gameObject);
            Set(press, "theme", theme);
            Set(press, "label", visual);
        }

        private static void BuildHud(NavigationView nav, SerializedObject data)
        {
            var playing = Ref<CanvasGroup>(data, "playing").transform;
            var clockText = Ref<TMP_Text>(data, "clockText");
            var clock = Ref<Image>(data, "clockFill");
            var moves = Ref<TMP_Text>(data, "movesText");
            var level = Ref<TMP_Text>(data, "levelTitle");
            var pause = Ref<Button>(data, "pauseButton");
            Rect((RectTransform)pause.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(42, 42));
            var clockPill = Pill(playing, "013 ClockPill");
            Rect(clockPill, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(128, 52));
            Parent(clockText.transform, clockPill);
            Rect(clockText.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(120, 40));
            Text(clockText, theme.HudOutline, Color.white, 25 * Unit);
            var trackNode = playing.Find("013 ClockTrack");
            if (trackNode == null) { trackNode = clockPill.Find("013 ClockTrack"); }
            var track = trackNode != null ? trackNode.GetComponent<Image>() : ImageNode(clockPill, "013 ClockTrack", theme.ClockTrack, Color.white);
            Parent(track.transform, clockPill);
            Image(track, theme.ClockTrack, theme.PillOutline);
            Rect(track.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 6), new Vector2(102, 5));
            Set(track, "m_PixelsPerUnitMultiplier", theme.ClockTrack.rect.height / (5 * Unit));
            Parent(clock.transform, track.transform);
            Stretch(clock.rectTransform, 0);
            Set(clock.rectTransform, "m_Pivot", new Vector2(0, .5f));
            Image(clock, theme.ClockFill, theme.ClockColor);
            Set(clock, "m_PixelsPerUnitMultiplier", theme.ClockFill.rect.height / (5 * Unit));
            Set(clock, "m_FillAmount", 1f);
            var levelPill = Pill(playing, "013 LevelPill");
            Rect(levelPill, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -20), new Vector2(64, 34));
            Parent(level.transform, levelPill);
            Stretch(level.rectTransform, 3 * Unit);
            Text(level, theme.HudSmall, Color.white, 14 * Unit);
            Set(level, "m_text", "Màn 1");
            var movesPill = Pill(playing, "013 MovesPill");
            Rect(movesPill, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(64, 22));
            Parent(moves.transform, movesPill);
            Stretch(moves.rectTransform, Unit);
            Text(moves, theme.HudSmall, Color.white, 11 * Unit);
            Set(moves, "m_text", "0 nước");
            var undo = Ref<Button>(data, "undoButton");
            var hint = Ref<Button>(data, "hintButton");
            Booster(nav, undo, false);
            Booster(nav, hint, true);
            var restarts = data.FindProperty("restartButtons");
            for (int i = 0; i < restarts.arraySize; i++)
            {
                var button = restarts.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                if (button != null && button.transform.IsChildOf(playing)) { Set(button.gameObject, "m_IsActive", false); }
            }
            var message = Ref<TMP_Text>(data, "messageText");
            var bubble = ImageNode(playing, "013 MessageBubble", theme.Panel, Color.white);
            Rect(bubble.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -106), new Vector2(236, 28));
            Parent(message.transform, bubble.transform);
            Stretch(message.rectTransform, 4 * Unit);
            Text(message, theme.CardText, Color.white, 12 * Unit);
            Set(message, "m_text", "");
            var group = Add<CanvasGroup>(bubble.gameObject);
            Set(group, "m_Alpha", 0f);
            Set(group, "m_Interactable", false);
            Set(group, "m_BlocksRaycasts", false);
            Set(nav, "messageBubble", group);
        }

        private static void Booster(NavigationView nav, Button button, bool hint)
        {
            Rect((RectTransform)button.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(hint ? 50 : -50, 44), new Vector2(theme.BoosterSize, theme.BoosterSize));
            var visual = button.transform.Find("013 ButtonVisual");
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var tag = ImageNode(visual, "013 LabelPill", theme.PillSprite, theme.PillColor);
            Rect(tag.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -9), new Vector2(84, 20));
            Outline(tag, theme.PillOutline, 2 * Unit);
            Rect(label.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -9), new Vector2(80, 20));
            Text(label, theme.HudSmall, Color.white, 11 * Unit);
            Last(label.transform);
            var badge = ImageNode(visual, "013 Badge", theme.PillSprite, theme.BadgeColor);
            Rect(badge.rectTransform, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(hint ? 24 : 34, 24));
            Outline(badge, theme.PillOutline, 2 * Unit);
            var number = TextNode(badge.transform, "013 BadgeText");
            Stretch(number.rectTransform, Unit);
            Text(number, theme.HudSmall, Color.white, 12 * Unit);
            Set(number, "m_text", hint ? "3" : "-3s");
            Set(nav, hint ? "hintBadge" : "undoBadge", number);
            if (hint) { Set(nav, "hintBadgePlate", badge); }
        }

        private static RectTransform Pill(Transform parent, string name)
        {
            var root = ImageNode(parent, name, theme.PillSprite, theme.PillOutline);
            Outline(root, theme.PillOutline, theme.OutlinePixels * Unit);
            var shadow = ImageNode(root.transform, "013 SoftShadow", theme.SoftShadow, new Color(0, 0, 0, theme.ShadowAlpha));
            Stretch(shadow.rectTransform, -4 * Unit);
            Set(shadow.rectTransform, "m_AnchoredPosition", Vector2.down * theme.ShadowOffset * Unit);
            First(shadow.transform);
            var rim = ImageNode(root.transform, "013 InnerRim", theme.PillSprite, theme.PillHighlight);
            Stretch(rim.rectTransform, Unit);
            var body = ImageNode(root.transform, "013 PillBody", theme.PillSprite, theme.PillColor);
            Stretch(body.rectTransform, (1 + theme.InnerOutlinePixels) * Unit);
            return root.rectTransform;
        }

        private static string IconName(string name)
        {
            switch (name)
            {
                case "PauseButton": return "pause";
                case "UndoButton": return "undo";
                case "HintButton": return "hint";
                case "RestartButton": return "restart";
                case "SelectButton": return "home";
                case "NextButton": return "next";
                default: return "";
            }
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
        private static string Placeholder(TMP_Text text)
        {
            if (text.transform.parent.GetComponent<Button>() != null) { return ButtonLabel(text.transform.parent.name); }
            if (text.transform.parent.name == "LevelSelect") { return "Squeaky Lane\nTrạm rửa xe phố"; }
            return "";
        }
        private static T Ref<T>(SerializedObject data, string field) where T : UnityEngine.Object => UiSkinTools.Ref<T>(data, field);
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
            Undo.RegisterCreatedObjectUndo(node, "013 S2 decoration");
            node.transform.SetParent(parent, false);
            report.AppendLine(PathOf(node.transform) + " · GameObject · tồn tại · không → có (trang trí)");
            return node.GetComponent<RectTransform>();
        }
        private static Image ImageNode(Transform parent, string name, Sprite sprite, Color color)
        {
            var image = Add<Image>(Node(parent, name).gameObject);
            Image(image, sprite, color);
            return image;
        }
        private static TMP_Text TextNode(Transform parent, string name) => Add<TextMeshProUGUI>(Node(parent, name).gameObject);
        private static void Image(Image image, Sprite sprite, Color color)
        {
            Set(image, "m_Sprite", sprite);
            Set(image, "m_Color", color);
            Set(image, "m_Type", (int)UnityEngine.UI.Image.Type.Sliced);
            Set(image, "m_PixelsPerUnitMultiplier", 1 / Unit);
            Set(image, "m_RaycastTarget", false);
        }
        private static void Outline(Image image, Color color, float distance)
        {
            var effect = Add<Outline>(image.gameObject);
            Set(effect, "m_EffectColor", color);
            Set(effect, "m_EffectDistance", new Vector2(distance, -distance));
        }
        private static void Text(TMP_Text text, Material preset, Color color, float size)
        {
            Set(text, "m_fontAsset", preset == theme.HudOutline ? theme.TitleFont : theme.BodyFont);
            Set(text, "m_sharedMaterial", preset);
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
            // Không tạo material instance khi runtime đổi màu/viền chữ.
            text.UpdateMeshPadding();
        }
        private static void Parent(Transform child, Transform parent)
        {
            if (child.parent == parent) { return; }
            string old = PathOf(child);
            Undo.SetTransformParent(child, parent, "013 S2 parent decoration");
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
            rect.ForceUpdateRectTransforms();
            foreach (var image in rect.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == theme.PillSprite && image.rectTransform.rect.height > 0)
                { Set(image, "m_PixelsPerUnitMultiplier", theme.PillSprite.rect.height / image.rectTransform.rect.height); }
            }
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
            Undo.RecordObject(node, "013 S2 draw order");
            node.SetSiblingIndex(index);
            if (old != index) { report.AppendLine(PathOf(node) + " · Transform · siblingIndex · " + old + " → " + index); }
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
            var data = new SerializedObject(target);
            var property = data.FindProperty(field);
            // OffsetMin/Max là property tính toán của RectTransform, không có field serialize.
            if (property == null && target is RectTransform rect && (field == "m_OffsetMin" || field == "m_OffsetMax"))
            {
                var old = field == "m_OffsetMin" ? rect.offsetMin : rect.offsetMax;
                Undo.RecordObject(rect, "013 S2 rect inset");
                if (field == "m_OffsetMin") { rect.offsetMin = (Vector2)value; }
                else { rect.offsetMax = (Vector2)value; }
                report.AppendLine(PathOf(rect) + " · RectTransform · " + field + " · " + old + " → " + value);
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
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            if (target is Component c && PrefabUtility.IsPartOfPrefabInstance(c)) { PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            string after = Value(property);
            if (before != after) { report.AppendLine((target is Component component ? PathOf(component.transform) : AssetDatabase.GetAssetPath(target)) + " · " + target.GetType().Name + " · " + field + " · " + before + " → " + after); }
        }
    }
}
