using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;
using SqueakyLane.Presentation;
using static SqueakyLane.EditorTools.EditorAssets;
using Object = UnityEngine.Object;

namespace SqueakyLane.EditorTools
{
    // 019: nhập hình bàn tay, dựng lớp hướng dẫn và nút "Xem lại hướng dẫn" vào Play.unity.
    // Chạy lại được: dựng lại hai object của mình, không đụng object khác.
    public static class TutorialUiTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string Art = UiArt + "/Library/";
        private const string GameConfigPath = "Assets/_Project/Config/GameConfig.asset";
        private const string RootName = "019 Tutorial", ReplayName = "019 ReplayTutorial";
        private const int TapFrames = 7, UntimedLevels = 3;
        // Panel cao thêm và hai công tắc nhích lên để chừa chỗ cho nút bên dưới.
        private const float ReplayRoom = 130, ToggleLift = 85, ReplayBottom = 108;

        [MenuItem("Tools/Squeaky Lane/019 UI/Build Tutorial")]
        public static void Build()
        {
            RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) { throw new InvalidOperationException("Mở Play.unity trước."); }
            var theme = RequireAsset<UiTheme>(UiThemePath);
            AssignHands(theme);
            var bootstrap = Object.FindObjectOfType<GameBootstrap>(true);
            var navigation = Object.FindObjectOfType<NavigationView>(true);
            if (bootstrap == null || navigation == null) { throw new InvalidOperationException("Thiếu GameBootstrap/NavigationView."); }
            var view = BuildView(navigation, theme);
            BuildReplayButton(navigation, theme);
            var data = new SerializedObject(bootstrap);
            data.FindProperty("tutorialView").objectReferenceValue = view;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(bootstrap);
            SetUntimedLevels();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("019: đã dựng Canvas/" + RootName + ", nút xem lại hướng dẫn và đặt " + UntimedLevels + " màn đầu không tính giờ.");
        }

        private static TutorialView BuildView(NavigationView navigation, UiTheme theme)
        {
            var data = new SerializedObject(navigation);
            var fade = (Image)data.FindProperty("fadeOverlay").objectReferenceValue;
            var movesText = (TMP_Text)data.FindProperty("movesText").objectReferenceValue;
            var outline = ((TMP_Text)data.FindProperty("clockText").objectReferenceValue).fontSharedMaterial;
            var titleFont = ((TMP_Text)data.FindProperty("clockText").objectReferenceValue).font;
            var canvas = fade.transform.parent;
            var old = canvas.Find(RootName);
            if (old != null) { Object.DestroyImmediate(old.gameObject); }

            var root = Node(canvas, RootName);
            Stretch(root);
            // Trên mọi HUD, dưới lớp chuyển cảnh.
            root.SetSiblingIndex(fade.transform.GetSiblingIndex());
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;

            var shade = Spotlight(root, "Shade", false, theme.TutorialDim);
            var ring = Spotlight(root, "Ring", true, theme.TutorialRing);

            var bubble = Node(root, "Bubble");
            Place(bubble, new Vector2(.5f, .5f), Vector2.zero, new Vector2(940, 225));
            var bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
            var back = bubble.gameObject.AddComponent<Image>();
            back.sprite = RequireAsset<Sprite>(UiArt + "/Sprites/Pack/bg_popup_purple.png");
            back.type = Image.Type.Sliced;
            back.pixelsPerUnitMultiplier = 1.6f;
            // Chữ không chặn chạm: xe nằm dưới khung vẫn bấm được.
            back.raycastTarget = false;

            var text = Label(bubble, "Text", titleFont, outline, 52, "Hướng dẫn");
            Place(text.rectTransform, new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(860, 100));
            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = 36;
            text.fontSizeMax = 52;

            var hint = Label(bubble, "Continue", movesText.font, movesText.fontSharedMaterial, 36, "Chạm để tiếp tục");
            Place(hint.rectTransform, new Vector2(0, 0), new Vector2(270, 56), new Vector2(440, 56));
            hint.alignment = TextAlignmentOptions.Left;
            hint.color = new Color(1, .9f, .55f, 1);

            var skip = Node(bubble, "Skip");
            Place(skip, new Vector2(1, 0), new Vector2(-165, 62), new Vector2(220, 76));
            var skipImage = skip.gameObject.AddComponent<Image>();
            skipImage.sprite = RequireAsset<Sprite>(UiArt + "/Sprites/Pack/bg_info_holder_purple.png");
            skipImage.type = Image.Type.Sliced;
            var skipButton = skip.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipImage;
            var navigationOff = skipButton.navigation;
            navigationOff.mode = UnityEngine.UI.Navigation.Mode.None;
            skipButton.navigation = navigationOff;
            Stretch(Label(skip, "Label", movesText.font, movesText.fontSharedMaterial, 36, "Bỏ qua »").rectTransform);

            var handNode = Node(root, "Hand");
            Place(handNode, new Vector2(.5f, .5f), Vector2.zero, theme.TutorialTapSize);
            var hand = handNode.gameObject.AddComponent<Image>();
            hand.raycastTarget = false;
            hand.preserveAspect = true;
            hand.enabled = false;

            var view = root.gameObject.AddComponent<TutorialView>();
            Assign(view, ("theme", theme), ("group", group), ("shade", shade), ("ring", ring), ("hand", hand), ("bubble", bubbleGroup),
                ("text", text), ("continueHint", hint.gameObject), ("skipButton", skipButton));
            return view;
        }

        private static void BuildReplayButton(NavigationView navigation, UiTheme theme)
        {
            var data = new SerializedObject(navigation);
            var settings = (CanvasGroup)data.FindProperty("menuSettings").objectReferenceValue;
            var clock = (TMP_Text)data.FindProperty("clockText").objectReferenceValue;
            var panel = settings != null ? settings.transform.Find("016 Panel") as RectTransform : null;
            if (panel == null) { throw new InvalidOperationException("Thiếu 016 MenuSettings/016 Panel; chạy menu 016 UI/Build Main Menu trước."); }
            foreach (var toggle in panel.GetComponentsInChildren<Toggle>(true))
            {
                var rect = (RectTransform)toggle.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, theme.MenuSettingsTogglePosition.y + ToggleLift);
            }

            var old = panel.Find(ReplayName);
            if (old != null) { Object.DestroyImmediate(old.gameObject); }
            panel.sizeDelta = new Vector2(theme.MenuSettingsSize.x, theme.MenuSettingsSize.y + ReplayRoom);

            var node = Node(panel, ReplayName);
            Place(node, new Vector2(.5f, 0), new Vector2(0, ReplayBottom), new Vector2(560, 130));
            var image = node.gameObject.AddComponent<Image>();
            image.sprite = RequireAsset<Sprite>(UiArt + "/Library/btn_red.png");
            image.type = Image.Type.Sliced;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var mode = button.navigation;
            mode.mode = UnityEngine.UI.Navigation.Mode.None;
            button.navigation = mode;
            var label = Label(node, "Label", clock.font, clock.fontSharedMaterial, 44, "Xem lại hướng dẫn");
            Stretch(label.rectTransform);
            Assign(navigation, ("replayTutorialButton", button), ("replayTutorialLabel", label));
        }

        private static void AssignHands(UiTheme theme)
        {
            var data = new SerializedObject(theme);
            var frames = data.FindProperty("tutorialTapFrames");
            frames.arraySize = TapFrames;
            for (int i = 0; i < TapFrames; i++) { frames.GetArrayElementAtIndex(i).objectReferenceValue = Import("finger" + (i + 1)); }
            data.FindProperty("tutorialDragHand").objectReferenceValue = Import("ic_finger");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssetIfDirty(theme);
        }

        private static Sprite Import(string name)
        {
            string path = Art + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { throw new InvalidOperationException("Thiếu hình " + path); }
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || !importer.alphaIsTransparency
                || importer.spriteImportMode != SpriteImportMode.Single || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return RequireAsset<Sprite>(path);
        }

        private static void SetUntimedLevels()
        {
            var config = RequireAsset<GameConfig>(GameConfigPath);
            var data = new SerializedObject(config);
            data.FindProperty("tutorialLevelsUntimed").intValue = UntimedLevels;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        private static SpotlightGraphic Spotlight(Transform parent, string name, bool outline, Color color)
        {
            var node = Node(parent, name);
            Stretch(node);
            var graphic = node.gameObject.AddComponent<SpotlightGraphic>();
            graphic.color = color;
            graphic.raycastTarget = false;
            var data = new SerializedObject(graphic);
            data.FindProperty("outline").boolValue = outline;
            data.ApplyModifiedPropertiesWithoutUndo();
            return graphic;
        }

        private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, Material material, float size, string value)
        {
            var node = Node(parent, name);
            var text = node.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = material;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.layer = parent.gameObject.layer;
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Assign(Object target, params (string name, Object value)[] fields)
        {
            var data = new SerializedObject(target);
            foreach (var field in fields) { data.FindProperty(field.name).objectReferenceValue = field.value; }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
