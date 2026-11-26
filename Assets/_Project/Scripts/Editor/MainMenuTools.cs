using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Presentation;
using static SqueakyLane.EditorTools.EditorAssets;

namespace SqueakyLane.EditorTools
{
    // 016: dựng main menu (ảnh IP + nút MÀN n + thanh điều hướng Chọn màn / Trang chủ / Cài đặt + hộp Cài đặt)
    // và nút "về menu" trên màn Chọn màn, rồi gán vào NavigationView. Chạy lại được: xóa phần "016 …" cũ rồi dựng lại.
    // Chỉ dùng sprite đã có trong Art/UI (bộ Sand Puzzle Clone đã duyệt 25/11) và ảnh IP Codex tạo (016-d).
    public static class MainMenuTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string Pack = UiArt + "/Sprites/Pack/";
        private const string Library = UiArt + "/Library/";
        private const string HeroPath = UiArt + "/Menu/menu_ip.png";
        private static StringBuilder report;

        [MenuItem("Tools/Squeaky Lane/016 UI/Build Main Menu")]
        public static void Build()
        {
            RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) { throw new InvalidOperationException("Mở Play.unity trước."); }
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemePath);
            if (nav == null || theme == null || theme.HudOutline == null || theme.TitleFont == null) { throw new InvalidOperationException("Thiếu NavigationView hoặc UiTheme/preset chữ 013."); }
            ImportSprites();
            report = new StringBuilder("\n## 016 · Main menu + nút về menu · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n");
            var so = new SerializedObject(nav);
            var safe = (RectTransform)so.FindProperty("safeAreaRoot").objectReferenceValue;
            var levelSelect = (CanvasGroup)so.FindProperty("levelSelect").objectReferenceValue;
            Remove(safe, "016 MainMenu");
            Remove(levelSelect.transform, "016 HomeButton");

            // Gốc menu: phủ cả màn (tràn ra ngoài safe area để ảnh không hở viền), nằm trên LevelSelect, dưới hộp thoại.
            var menu = Node(safe, "016 MainMenu");
            Stretch(menu, 0, -HeroBleed);
            menu.SetSiblingIndex(levelSelect.transform.GetSiblingIndex() + 1);
            var group = menu.gameObject.AddComponent<CanvasGroup>();
            var hero = Hero(menu);
            hero.raycastTarget = true; // chặn chạm xuống bãi phía sau

            // Tên game trên nền trời.
            var title = MakeText(menu, "016 Title", "Squeaky Lane", theme, 118, Color.white);
            Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -330), new Vector2(980, 150));
            var subtitle = MakeText(menu, "016 Subtitle", "Trạm rửa xe phố", theme, 54, new Color32(255, 242, 150, 255));
            Place(subtitle.rectTransform, new Vector2(.5f, 1), new Vector2(0, -440), new Vector2(900, 80));

            // Nút MÀN n (đỏ to, như nút LEVEL của ảnh mẫu).
            var play = MakeButton(menu, "016 PlayButton", Load(Library + "btn_red.png"), new Vector2(.5f, 0), new Vector2(0, HeroBleed + 480), new Vector2(620, 220));
            var playLabel = MakeText(play.transform, "016 PlayLabel", "MÀN 1", theme, 92, Color.white);
            Stretch(playLabel.rectTransform, 0, 0);

            // Thanh điều hướng dưới: Chọn màn | Trang chủ (đang chọn) | Cài đặt. Chỉ nút có chức năng thật.
            var bar = Img(menu, "016 NavBar", Load(Pack + "bg_main_nav_bg.png"), Color.white);
            bar.type = Image.Type.Sliced;
            // Thanh phủ luôn phần dưới safe area; các tab nằm trong 300 đơn vị trên cùng của thanh.
            bar.rectTransform.anchorMin = new Vector2(0, 0); bar.rectTransform.anchorMax = new Vector2(1, 0); bar.rectTransform.pivot = new Vector2(.5f, 0);
            bar.rectTransform.sizeDelta = new Vector2(80, 300 + HeroBleed); bar.rectTransform.anchoredPosition = Vector2.zero;
            var levels = Tab(bar.transform, "016 TabLevels", "Chọn màn", Load(Library + "ic_calendar.png"), -330, false, theme);
            var home = Tab(bar.transform, "016 TabHome", "Trang chủ", Load(Pack + "ic_main_home.png"), 0, true, theme);
            home.interactable = false;
            var settings = Tab(bar.transform, "016 TabSettings", "Cài đặt", Load(Library + "ic_main_settings.png"), 330, false, theme);

            // Hộp Cài đặt: lớp tối + khung tím + 2 công tắc nhân bản từ màn Chọn màn (cùng hình, cùng hành vi).
            var settingsRoot = Node(menu, "016 MenuSettings");
            Stretch(settingsRoot, 0, 0);
            var settingsGroup = settingsRoot.gameObject.AddComponent<CanvasGroup>();
            var dim = Img(settingsRoot, "016 Dim", null, new Color(.06f, .03f, .16f, theme.DimAlpha));
            Stretch(dim.rectTransform, 0, 0);
            dim.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>(); dimButton.transition = Selectable.Transition.None;
            var panel = Img(settingsRoot, "016 Panel", Load(Pack + "bg_popup_purple.png"), Color.white);
            var header = MakeText(panel.transform, "016 Header", "Cài đặt", theme, theme.TitleFontSize, Color.white);
            var close = MakeButton(panel.transform, "016 Close", Load(Pack + "ic_close.png"), new Vector2(1, 1), Vector2.zero, Vector2.one);
            var sound = CloneToggle(levelSelect.transform, "SoundToggle", panel.transform);
            var haptics = CloneToggle(levelSelect.transform, "HapticsToggle", panel.transform);
            LayoutSettings(panel, header, close, sound, haptics, theme);

            // Nút "về menu" ở góc trên trái màn Chọn màn.
            var homeButton = MakeButton(levelSelect.transform, "016 HomeButton", Load(Pack + "btn_round_green.png"), new Vector2(0, 1), new Vector2(70, -70), new Vector2(130, 130));
            homeButton.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            var homeIcon = Img(homeButton.transform, "016 Icon", Load(Pack + "ic_main_home.png"), Color.white);
            homeIcon.preserveAspect = true; homeIcon.raycastTarget = false;
            Stretch(homeIcon.rectTransform, -22, -22);

            StyleLevelSelect((RectTransform)levelSelect.transform, theme);

            SetRef(so, "mainMenu", group);
            SetRef(so, "menuSettings", settingsGroup);
            SetRef(so, "menuPlayButton", play);
            SetRef(so, "menuLevelsButton", levels);
            SetRef(so, "menuSettingsButton", settings);
            SetRef(so, "menuPlayLabel", playLabel);
            SetArray(so, "homeButtons", new UnityEngine.Object[] { homeButton });
            SetArray(so, "menuSettingsCloseButtons", new UnityEngine.Object[] { close, dimButton });
            SetRef(so, "levelCurrentSprite", Load(Library + "Btn_square_orange_v1.png"));
            if (sound != null) { Append(so, "soundToggles", sound); }
            if (haptics != null) { Append(so, "hapticsToggles", haptics); }
            so.ApplyModifiedPropertiesWithoutUndo();
            settingsGroup.alpha = 0; settingsGroup.interactable = false; settingsGroup.blocksRaycasts = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            System.IO.File.AppendAllText("docs/handoff/016-scene-changes.md", report.ToString());
            Debug.Log("016: đã dựng main menu và nút về menu; ghi thay đổi ở docs/handoff/016-scene-changes.md");
        }

        // Ảnh IP tràn ngang màn hình (cắt 4% mỗi bên), neo mép trên; phần dư phía dưới tô đúng màu sân trong ảnh.
        private const float HeroBleed = 170, HeroOverscan = 40;
        private static readonly Color32 HeroFloor = new Color32(135, 150, 184, 255);
        private static Image Hero(Transform parent)
        {
            var floor = Img(parent, "016 HeroFloor", null, HeroFloor);
            Stretch(floor.rectTransform, 0, 0);
            var hero = Img(floor.transform, "016 Hero", Load(HeroPath), Color.white);
            var rect = hero.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(HeroOverscan * 2, 0);
            var fitter = hero.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 1080f / 1920f;
            floor.raycastTarget = true;
            return floor;
        }

        // Màn Chọn màn: nền ảnh IP phủ lớp tím, tiêu đề trên ribbon, lưới màn trong khung popup, chữ công tắc trắng viền.
        private static void StyleLevelSelect(RectTransform root, UiTheme theme)
        {
            Remove(root, "016 Backdrop");
            Remove(root, "016 Ribbon");
            var backdrop = Node(root, "016 Backdrop");
            Stretch(backdrop, 0, -HeroBleed);
            backdrop.SetAsFirstSibling();
            Hero(backdrop).raycastTarget = false;
            var shade = Img(backdrop, "016 Shade", null, new Color(.12f, .08f, .3f, .55f));
            Stretch(shade.rectTransform, 0, 0);

            var title = root.Find("Title");
            if (title != null)
            {
                var ribbon = Img(root, "016 Ribbon", Load(Pack + "bg_pop_up_header.png"), Color.white);
                ribbon.type = ribbon.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                var rect = ribbon.rectTransform;
                rect.anchorMin = new Vector2(.2f, .905f); rect.anchorMax = new Vector2(.86f, .995f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                ribbon.transform.SetSiblingIndex(title.GetSiblingIndex());
                var text = title.GetComponent<TMP_Text>();
                var titleRect = (RectTransform)title;
                titleRect.anchorMin = rect.anchorMin; titleRect.anchorMax = rect.anchorMax;
                titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
                text.text = "CHỌN MÀN"; text.font = theme.TitleFont; text.fontSharedMaterial = theme.HudOutline;
                text.enableAutoSizing = false; text.fontSize = 66; text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
                report.AppendLine("LevelSelect/Title · TMP_Text · 'Squeaky Lane…' CardText → 'CHỌN MÀN' HudOutline 66 trên ribbon");
            }
            var scroll = root.Find("LevelScroll");
            if (scroll != null)
            {
                var image = scroll.GetComponent<Image>();
                image.sprite = Load(Pack + "bg_popup_purple.png"); image.type = Image.Type.Sliced; image.color = new Color(1, 1, 1, .94f);
                var scrollRect = scroll.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.horizontal = false;
                    foreach (var bar in new[] { scrollRect.horizontalScrollbar, scrollRect.verticalScrollbar })
                    {
                        if (bar == null) { continue; }
                        var track = bar.GetComponent<Image>();
                        if (track != null) { track.color = new Color(1, 1, 1, 0); }
                        if (bar.targetGraphic != null) { bar.targetGraphic.color = new Color(1, 1, 1, 0); }
                    }
                    var grid = scrollRect.content != null ? scrollRect.content.GetComponent<GridLayoutGroup>() : null;
                    if (grid != null) { grid.padding = new RectOffset(0, 0, 36, 36); }
                }
                report.AppendLine("LevelSelect/LevelScroll · Image/ScrollRect · nền khung popup tím, ẩn thanh cuộn, đệm lưới 36");
            }
            foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                var caption = toggle.transform.Find("013 Caption");
                var text = caption != null ? caption.GetComponent<TMP_Text>() : null;
                if (text == null) { continue; }
                text.fontSharedMaterial = theme.HudOutline; text.font = theme.TitleFont; text.color = Color.white;
                report.AppendLine("LevelSelect/" + toggle.name + "/013 Caption · TMP_Text · CardText → HudOutline trắng");
            }
        }

        // Sprite mới chép từ project cũ: đặt importer Sprite + 9-slice; ảnh IP: Sprite, tối đa 2048, nén thường.
        private static void ImportSprites()
        {
            var borders = new Dictionary<string, Vector4>
            {
                { Pack + "bg_main_nav_bg.png", new Vector4(60, 60, 60, 60) },
                { Pack + "bg_main_nav.png", new Vector4(70, 70, 70, 90) },
            };
            foreach (var item in borders) { Importer(item.Key, item.Value, 1024); }
            Importer(HeroPath, Vector4.zero, 2048);
        }
        private static void Importer(string path, Vector4 border, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { throw new InvalidOperationException("Thiếu " + path); }
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border || importer.maxTextureSize != maxSize
                || importer.spritePixelsPerUnit != 100 || importer.mipmapEnabled;
            if (!changed) { return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) { throw new InvalidOperationException("Thiếu sprite " + path); }
            return sprite;
        }
        private static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null) { UnityEngine.Object.DestroyImmediate(old.gameObject); report?.AppendLine(parent.name + "/" + name + " · GameObject · xóa bản cũ để dựng lại"); }
        }
        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            report.AppendLine(PathOf(go.transform) + " · GameObject · tạo mới");
            return (RectTransform)go.transform;
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        private static void Stretch(RectTransform rect, float x, float y)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y);
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static Image Img(Transform parent, string name, Sprite sprite, Color color)
        {
            var image = Node(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false;
            return image;
        }
        private static TMP_Text MakeText(Transform parent, string name, string value, UiTheme theme, float size, Color color)
        {
            var text = Node(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = theme.TitleFont; text.fontSharedMaterial = theme.HudOutline;
            text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.enableWordWrapping = false; text.raycastTarget = false;
            return text;
        }
        private static Button MakeButton(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Img(parent, name, sprite, Color.white);
            image.raycastTarget = true; image.preserveAspect = false;
            if (sprite != null && sprite.border != Vector4.zero) { image.type = Image.Type.Sliced; }
            Place(image.rectTransform, anchor, position, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
        // Một ô của thanh điều hướng: nền tab (ô đang chọn dùng tab vàng nhô lên), icon và chữ.
        private static Button Tab(Transform bar, string name, string label, Sprite icon, float x, bool active, UiTheme theme)
        {
            var tab = MakeButton(bar, name, active ? Load(Pack + "bg_main_nav.png") : null, new Vector2(.5f, 1), new Vector2(x, active ? -120 : -150), active ? new Vector2(300, 330) : new Vector2(300, 260));
            var image = tab.GetComponent<Image>();
            if (!active) { image.color = new Color(1, 1, 1, 0); }
            var glyph = Img(tab.transform, "016 Icon", icon, Color.white);
            glyph.preserveAspect = true;
            // Sprite bánh răng có lề trong suốt rộng nên phóng lớn hơn cho cân với icon khác.
            float glyphSize = active ? 150 : icon.name == "ic_main_settings" ? 175 : 125;
            Place(glyph.rectTransform, new Vector2(.5f, .5f), new Vector2(0, active ? 40 : 30), new Vector2(glyphSize, glyphSize));
            var caption = MakeText(tab.transform, "016 Caption", label, theme, active ? 46 : 40, Color.white);
            Place(caption.rectTransform, new Vector2(.5f, .5f), new Vector2(0, active ? -70 : -65), new Vector2(300, 60));
            return tab;
        }
        private static void LayoutSettings(Image panel, TMP_Text header, Button close, Toggle sound, Toggle haptics, UiTheme theme)
        {
            var center = new Vector2(.5f, .5f);
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = 1f / (theme.FrameScale * theme.HudUnit);
            panel.raycastTarget = true;
            Place(panel.rectTransform, center, Vector2.zero, theme.MenuSettingsSize);

            header.fontSize = theme.TitleFontSize;
            var headerSize = new Vector2(theme.MenuSettingsSize.x - 4 * theme.MenuSettingsCloseInset.x, theme.MenuSettingsHeaderInset);
            Place(header.rectTransform, new Vector2(.5f, 1), Vector2.down * theme.MenuSettingsHeaderInset, headerSize);
            close.image.type = Image.Type.Simple;
            close.image.preserveAspect = true;
            var closeInset = theme.MenuSettingsCloseInset;
            Place((RectTransform)close.transform, Vector2.one, new Vector2(-closeInset.x, -closeInset.y), Vector2.one * theme.MenuSettingsCloseSize);

            var togglePosition = theme.MenuSettingsTogglePosition;
            LayoutSettingsToggle(sound, new Vector2(-togglePosition.x, togglePosition.y), theme);
            LayoutSettingsToggle(haptics, togglePosition, theme);
        }

        private static void LayoutSettingsToggle(Toggle toggle, Vector2 position, UiTheme theme)
        {
            if (toggle == null) { return; }
            var rect = (RectTransform)toggle.transform;
            var size = theme.MenuSettingsToggleSize;
            Place(rect, new Vector2(.5f, .5f), position, size);

            var frame = (RectTransform)toggle.targetGraphic.transform;
            float iconSize = theme.MenuSettingsToggleIconSize;
            Place(frame, new Vector2(.5f, 1), Vector2.down * (iconSize * .5f), Vector2.one * iconSize);

            // Công tắc gốc nằm trước nhãn theo hàng ngang. Trong popup, đặt nhãn riêng bên dưới icon.
            var captionNode = rect.Find("013 Caption");
            if (captionNode == null) { return; }
            var caption = captionNode.GetComponent<TMP_Text>();
            caption.font = theme.TitleFont;
            caption.fontSharedMaterial = theme.HudOutline;
            caption.color = Color.white;
            caption.alignment = TextAlignmentOptions.Center;
            caption.enableAutoSizing = false;
            caption.fontSize = theme.MenuSettingsToggleFontSize;
            float captionHeight = size.y - iconSize;
            Place(caption.rectTransform, new Vector2(.5f, 0), Vector2.up * (captionHeight * .5f), new Vector2(size.x, captionHeight));
        }

        private static Toggle CloneToggle(Transform levelSelect, string name, Transform parent)
        {
            Toggle source = null;
            foreach (var toggle in levelSelect.GetComponentsInChildren<Toggle>(true)) { if (toggle.name == name) { source = toggle; break; } }
            if (source == null) { report.AppendLine("Không thấy " + name + " trên LevelSelect; bỏ qua công tắc này"); return null; }
            var copy = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            copy.name = "016 " + name;
            copy.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            var layout = copy.GetComponent<LayoutElement>();
            if (layout != null) { layout.ignoreLayout = true; }
            report.AppendLine(PathOf(copy.transform) + " · Toggle · nhân bản từ LevelSelect/" + name);
            return copy.GetComponent<Toggle>();
        }
        private static void SetRef(SerializedObject so, string field, UnityEngine.Object value)
        {
            var property = so.FindProperty(field);
            report.AppendLine("NavigationView · " + field + " · " + (property.objectReferenceValue != null ? property.objectReferenceValue.name : "null") + " → " + (value != null ? value.name : "null"));
            property.objectReferenceValue = value;
        }
        private static void SetArray(SerializedObject so, string field, UnityEngine.Object[] values)
        {
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) { property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            report.AppendLine("NavigationView · " + field + " · → " + values.Length + " phần tử");
        }
        // Thêm vào mảng toggle nếu chưa có; bỏ các ô trống (do lần dựng trước bị xóa).
        private static void Append(SerializedObject so, string field, UnityEngine.Object value)
        {
            var property = so.FindProperty(field);
            var keep = new List<UnityEngine.Object>();
            for (int i = 0; i < property.arraySize; i++)
            {
                var item = property.GetArrayElementAtIndex(i).objectReferenceValue;
                if (item != null && item != value) { keep.Add(item); }
            }
            keep.Add(value);
            SetArray(so, field, keep.ToArray());
        }
    }
}
