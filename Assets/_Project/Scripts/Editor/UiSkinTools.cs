using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public static class UiSkinTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string LevelButtonPath = "Assets/_Project/Prefabs/LevelButton.prefab";
        private static readonly string[] Bindings =
        {
            "safeAreaRoot", "levelSelect", "playing", "paused", "won", "lost", "stuckBanner", "fadeOverlay",
            "levelButtonContent", "levelButtonPrefab", "levelTitle", "clockText", "movesText", "messageText", "wonText", "lostText", "clockFill",
            "pauseButton", "resumeButton", "nextButton", "undoButton", "hintButton", "restartButtons", "levelSelectButtons", "soundToggles", "hapticsToggles"
        };
        private static StringBuilder report;

        [MenuItem("Tools/Squeaky Lane/013 Theme/2 Apply UI Skin")]
        public static void Apply()
        {
            UiThemeAssets.RequireEditMode();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { throw new InvalidOperationException("Đóng Prefab Mode trước khi áp skin."); }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty) { throw new InvalidOperationException("Mở scene Play và lưu phần đang sửa trước khi Apply UI Skin."); }
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            ValidateTheme(theme);
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            if (nav == null || nav.gameObject.scene != scene) { throw new InvalidOperationException("Thiếu NavigationView trong Play."); }
            string bindings = CaptureBindings(nav);
            UiThemeAssets.Backup(ScenePath);
            UiThemeAssets.Backup(LevelButtonPath);
            report = new StringBuilder("# 013 — Thay đổi UI scene/prefab\n\nĐường dẫn · component · field · cũ → mới. Binding/On Click được so trước/sau.\n\n");
            var data = new SerializedObject(nav);
            foreach (var text in nav.GetComponentsInChildren<TMP_Text>(true))
            {
                bool title = text.name == "Title" || text.name == "LevelTitle" || text == Ref<TMP_Text>(data, "wonText") || text == Ref<TMP_Text>(data, "lostText");
                StyleText(text, theme, title);
            }
            Set(nav, "theme", theme);
            Set(nav, "normalClock", theme.ClockColor);
            Set(nav, "lowClock", theme.LowClock);
            foreach (var button in nav.GetComponentsInChildren<Button>(true))
            {
                bool primary = button == Ref<Button>(data, "resumeButton") || button == Ref<Button>(data, "nextButton") || button.transform.parent.name == "Lost" && button.name == "RestartButton";
                StyleButton(button, theme, primary);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) { throw new InvalidOperationException("Nút thiếu nhãn TMP: " + PathOf(button.transform)); }
                string labelContent = button.name == "PauseButton" ? "<sprite name=pause>" :
                    button.name == "ResumeButton" ? "Tiếp tục" : button.name == "NextButton" ? "<sprite name=next> Màn kế" :
                    button.name == "UndoButton" ? "<sprite name=undo> Hoàn tác" : button.name == "HintButton" ? "<sprite name=hint> Gợi ý" :
                    button.name == "RestartButton" ? "<sprite name=restart> Chơi lại" : button.name == "SelectButton" ? "<sprite name=home> Chọn màn" : label.text;
                Set(label, "m_text", labelContent);
            }
            var select = Ref<CanvasGroup>(data, "levelSelect");
            var selectImage = select.GetComponent<Image>();
            if (selectImage != null)
            {
                Set(selectImage, "m_Sprite", null);
                Set(selectImage, "m_Color", Color.white);
                Decorate(selectImage, theme, false);
            }
            var titleNode = select.transform.Find("Title");
            if (titleNode != null) { Set(titleNode.GetComponent<TMP_Text>(), "m_text", "Squeaky Lane\n<size=65%>Trạm rửa xe phố</size>"); }
            foreach (string field in new[] { "paused", "won", "lost" })
            {
                var image = Ref<CanvasGroup>(data, field).GetComponent<Image>();
                if (image == null) { throw new InvalidOperationException("Panel thiếu Image: " + field); }
                StyleImage(image, theme.Panel, theme.PanelColor);
                Decorate(image, theme, true);
            }
            foreach (var toggle in nav.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.targetGraphic is Image background) { StyleImage(background, theme.ToggleBox, Color.white); }
                if (toggle.graphic is Image checkmark) { StyleImage(checkmark, theme.ToggleCheck, Color.white); }
                var label = toggle.GetComponentInChildren<TMP_Text>(true);
                if (label != null) { Set(label, "m_text", toggle.name.Contains("Sound") ? "<sprite name=sound_on> Âm thanh" : "<sprite name=phone> Rung"); }
            }
            var clock = Ref<Image>(data, "clockFill");
            Set(clock, "m_Sprite", theme.ClockFill);
            Set(clock, "m_Type", (int)Image.Type.Filled);
            Set(clock, "m_FillMethod", (int)Image.FillMethod.Horizontal);
            Set(clock, "m_FillOrigin", 0);
            Set(clock, "m_Color", theme.ClockColor);
            Set(clock, "m_RaycastTarget", false);
            AddClockTrack(clock, theme);
            var banner = Ref<CanvasGroup>(data, "stuckBanner");
            if (banner.GetComponent<Image>() != null) { StyleImage(banner.GetComponent<Image>(), theme.Panel, theme.PanelColor); }
            var bannerText = banner.GetComponentInChildren<TMP_Text>(true);
            if (bannerText == null)
            {
                var node = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(node, "013 stuck label");
                node.transform.SetParent(banner.transform, false);
                bannerText = node.GetComponent<TMP_Text>();
                bannerText.rectTransform.anchorMin = Vector2.zero;
                bannerText.rectTransform.anchorMax = Vector2.one;
                bannerText.rectTransform.offsetMin = bannerText.rectTransform.offsetMax = Vector2.zero;
                report.AppendLine(PathOf(node.transform) + " · TMP_Text · nhãn StuckBanner · chưa có → có (trong widget đã yêu cầu)");
            }
            StyleText(bannerText, theme, false);
            Set(bannerText, "m_text", "Kẹt xe — hoàn tác?");
            var content = Ref<Transform>(data, "levelButtonContent");
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                Set(grid, "m_CellSize", theme.LevelCellSize);
                Set(grid, "m_Spacing", theme.LevelSpacing);
                Set(grid, "m_Constraint", (int)GridLayoutGroup.Constraint.FixedColumnCount);
                Set(grid, "m_ConstraintCount", theme.LevelColumns);
            }
            FixViewportMask(content);
            var prefab = PrefabUtility.LoadPrefabContents(LevelButtonPath);
            try
            {
                StyleText(prefab.GetComponentInChildren<TMP_Text>(true), theme, false);
                StyleButton(prefab.GetComponent<Button>(), theme, false);
                Set(prefab.GetComponentInChildren<TMP_Text>(true), "m_text", "Màn 1\n" + theme.Stars(0));
                PrefabUtility.SaveAsPrefabAsset(prefab, LevelButtonPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            if (CaptureBindings(nav) != bindings) { throw new InvalidOperationException("Binding/On Click bị đổi ngoài phạm vi; chưa lưu scene, dùng Undo để hoàn."); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("docs/handoff/013-scene-changes.md", report.ToString(), new UTF8Encoding(false));
            Debug.Log("013: đã skin UI, binding/OnClick giữ nguyên. Báo cáo docs/handoff/013-scene-changes.md. Kiểm từng bước S1→S5 theo phiếu Visual V2.");
        }

        [MenuItem("Tools/Squeaky Lane/013 Theme/S1 Fix Level Select Mask")]
        public static void RepairLevelSelectMask()
        {
            UiThemeAssets.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || scene.path != ScenePath || scene.isDirty)
            { throw new InvalidOperationException("Đóng Prefab Mode, mở Play.unity và lưu phần đang sửa trước khi sửa Mask."); }
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            if (nav == null || nav.gameObject.scene != scene) { throw new InvalidOperationException("Thiếu NavigationView trong Play."); }
            var content = Ref<Transform>(new SerializedObject(nav), "levelButtonContent");
            if (content == null || content.parent == null || content.parent.GetComponent<Mask>() == null || content.parent.GetComponent<Image>() == null)
            { throw new InvalidOperationException("Viewport của levelButtonContent phải có Image + Mask; không sửa object khác."); }
            string bindings = CaptureBindings(nav);
            UiThemeAssets.Backup(ScenePath);
            report = new StringBuilder("\n## S1 · sửa Mask chọn màn · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n");
            FixViewportMask(content);
            if (CaptureBindings(nav) != bindings) { throw new InvalidOperationException("Binding/On Click thay đổi; chưa lưu scene."); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.AppendAllText("docs/handoff/013-scene-changes.md", report.ToString(), new UTF8Encoding(false));
            Debug.Log("013: đã sửa Viewport Image alpha=1 + Mask Show Mask Graphic=false; binding/OnClick giữ nguyên. Vào Play mở Chọn màn để kiểm nút và cuộn danh sách.");
        }

        private static void FixViewportMask(Transform content)
        {
            var viewport = content.parent.GetComponent<Image>();
            if (viewport == null) { return; }
            var mask = viewport.GetComponent<Mask>();
            // Mask cần Image có alpha để ghi stencil. Ẩn nền bằng showMaskGraphic, không bằng alpha0.
            if (mask != null)
            {
                Set(viewport, "m_Color", Color.white);
                Set(mask, "m_ShowMaskGraphic", false);
            }
            else { Set(viewport, "m_Color", Color.clear); }
        }

        private static void AddClockTrack(Image clock, UiTheme theme)
        {
            var node = clock.transform.parent.Find("013 ClockTrack");
            Image track;
            if (node == null)
            {
                var root = new GameObject("013 ClockTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(root, "013 clock track");
                root.transform.SetParent(clock.transform.parent, false);
                track = root.GetComponent<Image>();
                report.AppendLine(PathOf(root.transform) + " · Image · nền thanh giờ · chưa có → có (trang trí ClockFill)");
            }
            else { track = node.GetComponent<Image>(); }
            var a = clock.rectTransform;
            var b = track.rectTransform;
            Undo.RecordObject(b, "013 clock track rect");
            b.anchorMin = a.anchorMin; b.anchorMax = a.anchorMax; b.pivot = a.pivot;
            b.sizeDelta = a.sizeDelta; b.anchoredPosition = a.anchoredPosition;
            b.SetSiblingIndex(clock.transform.GetSiblingIndex());
            StyleImage(track, theme.ClockTrack, Color.white);
            Set(track, "m_RaycastTarget", false);
        }
        private static void Decorate(Image image, UiTheme theme, bool dialog)
        {
            var effect = image.GetComponent<UiPanelDecoration>();
            if (effect == null) { effect = Undo.AddComponent<UiPanelDecoration>(image.gameObject); }
            effect.Configure(theme, dialog);
            EditorUtility.SetDirty(effect);
            report.AppendLine(PathOf(image.transform) + " · UiPanelDecoration · mode · cũ → " + (dialog ? "thẻ+shade" : "gradient trời"));
        }
        internal static void ValidateTheme(UiTheme theme)
        {
            if (theme == null || theme.Icons == null || theme.TitleFont == null || theme.BodyFont == null || theme.Panel == null)
            { throw new InvalidOperationException("Chạy menu1 Import UI Assets trước."); }
            if (theme.TitleFont.atlasPopulationMode != AtlasPopulationMode.Static || theme.BodyFont.atlasPopulationMode != AtlasPopulationMode.Static)
            { throw new InvalidOperationException("Font phải Static trước khi áp UI."); }
        }
        private static void StyleButton(Button button, UiTheme theme, bool primary)
        {
            var image = button.targetGraphic as Image;
            if (image == null) { throw new InvalidOperationException("Nút thiếu Image: " + button.name); }
            StyleImage(image, primary ? theme.PrimaryButton : theme.RegularButton, primary ? theme.PrimaryColor : theme.NormalButton);
            Set(button, "m_Colors.m_NormalColor", Color.white);
            Set(button, "m_Colors.m_HighlightedColor", Color.white);
            Set(button, "m_Colors.m_SelectedColor", Color.white);
            Set(button, "m_Colors.m_PressedColor", new Color(theme.PressedTint, theme.PressedTint, theme.PressedTint, 1));
            var rect = (RectTransform)button.transform;
            if (rect.rect.height < theme.ButtonHeight)
            {
                float old = rect.rect.height;
                Undo.RecordObject(rect, "013 button size");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, theme.ButtonHeight);
                report.AppendLine(PathOf(rect) + " · RectTransform · height · " + old + " → " + theme.ButtonHeight);
            }
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) { return; }
            Set(label, "m_fontColor", primary ? theme.PrimaryText : theme.Text);
            var press = button.GetComponent<UiButtonPress>();
            if (press == null) { press = Undo.AddComponent<UiButtonPress>(button.gameObject); }
            press.Configure(theme, label.rectTransform);
            EditorUtility.SetDirty(press);
        }
        private static void StyleText(TMP_Text text, UiTheme theme, bool title)
        {
            if (text == null) { return; }
            var font = title ? theme.TitleFont : theme.BodyFont;
            Set(text, "m_fontAsset", font); Set(text, "m_sharedMaterial", font.material); Set(text, "m_spriteAsset", theme.Icons);
            Set(text, "m_fontColor", theme.Text);
            Set(text, "m_fontSize", title ? theme.TitleFontSize : theme.BodyFontSize);
            Set(text, "m_fontSizeBase", title ? theme.TitleFontSize : theme.BodyFontSize);
            Set(text, "m_enableAutoSizing", true);
            Set(text, "m_fontSizeMin", theme.BodyFontSize * .7f);
            Set(text, "m_fontSizeMax", title ? theme.TitleFontSize : theme.BodyFontSize);
            Set(text, "m_fontStyle", 0); Set(text, "m_RaycastTarget", false);
        }
        private static void StyleImage(Image image, Sprite sprite, Color color)
        {
            Set(image, "m_Sprite", sprite); Set(image, "m_Color", color); Set(image, "m_Type", (int)Image.Type.Sliced);
        }
        internal static T Ref<T>(SerializedObject data, string field) where T : UnityEngine.Object
        {
            var property = data.FindProperty(field);
            if (property == null || property.objectReferenceValue == null) { throw new InvalidOperationException("Thiếu tham chiếu " + field); }
            return (T)property.objectReferenceValue;
        }
        internal static string CaptureBindings(NavigationView nav)
        {
            var data = new SerializedObject(nav);
            var snapshot = new StringBuilder();
            foreach (string field in Bindings)
            {
                var property = data.FindProperty(field);
                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    for (int i = 0; i < property.arraySize; i++) { snapshot.Append(field).Append(property.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue); }
                }
                else { snapshot.Append(field).Append(property.objectReferenceInstanceIDValue); }
            }
            // Nút do menu 013 tự thêm (vd dấu X "013 Close") không phải nút gốc nên không đưa vào so sánh trước/sau.
            // Sắp xếp để việc dời nút sang khung mới (đổi thứ tự trong hierarchy) không bị tính là đổi binding.
            var events = new System.Collections.Generic.List<string>();
            foreach (var button in nav.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith("013 ", StringComparison.Ordinal)) { events.Add(CaptureButtonEvent(button)); }
            }
            events.Sort(StringComparer.Ordinal);
            foreach (string item in events) { snapshot.Append(item); }
            foreach (var toggle in nav.GetComponentsInChildren<Toggle>(true)) { CaptureEvent(snapshot, toggle.onValueChanged); }
            return snapshot.ToString();
        }
        internal static string CaptureButtonEvent(Button button)
        {
            // Gồm cả mode/arguments của persistent listener, không chỉ tên method.
            return button.GetInstanceID() + JsonUtility.ToJson(new ButtonEventSnapshot { callback = button.onClick });
        }
        [Serializable]
        private sealed class ButtonEventSnapshot
        {
            public Button.ButtonClickedEvent callback;
        }
        private static void CaptureEvent(StringBuilder snapshot, UnityEventBase callback)
        {
            for (int i = 0; i < callback.GetPersistentEventCount(); i++)
            { snapshot.Append(callback.GetPersistentTarget(i)).Append(callback.GetPersistentMethodName(i)).Append(callback.GetPersistentListenerState(i)); }
        }
        private static string PathOf(Transform node) { return node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name; }
        private static string Value(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference: return Convert.ToString(property.objectReferenceValue);
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Color: return property.colorValue.ToString();
                case SerializedPropertyType.Float: return property.floatValue.ToString();
                case SerializedPropertyType.Integer: case SerializedPropertyType.Enum: return property.intValue.ToString();
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString();
                default: return property.propertyType.ToString();
            }
        }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            if (target == null) { throw new InvalidOperationException("Thiếu object khi gán " + field); }
            var data = new SerializedObject(target);
            var property = data.FindProperty(field);
            if (property == null) { throw new InvalidOperationException(target.GetType().Name + " không có field serialize " + field); }
            string old = Value(property);
            if (value == null || value is UnityEngine.Object) { property.objectReferenceValue = (UnityEngine.Object)value; }
            else if (value is string text) { property.stringValue = text; }
            else if (value is Color color) { property.colorValue = color; }
            else if (value is float number) { property.floatValue = number; }
            else if (value is int integer) { property.intValue = integer; }
            else if (value is bool boolean) { property.boolValue = boolean; }
            else if (value is Vector2 vector) { property.vector2Value = vector; }
            else { throw new InvalidOperationException("Kiểu field chưa hỗ trợ: " + field); }
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            var component = target as Component;
            report.AppendLine((component != null ? PathOf(component.transform) : target.name) + " · " + target.GetType().Name + " · " + field + " · " + old + " → " + Value(property));
        }
    }
}
