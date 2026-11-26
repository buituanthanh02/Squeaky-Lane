using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SqueakyLane.Presentation;
using static SqueakyLane.EditorTools.EditorAssets;
using Object = UnityEngine.Object;

namespace SqueakyLane.EditorTools
{
    // 018: đặt importer cho hình combo, gán vào UiTheme/SfxCatalog và gắn ComboFeedback + ComboSummary vào Play.unity.
    // Chạy lại được: dùng lại object/component đã có, giữ vị trí người làm đã chỉnh.
    public static class ComboUiTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string Art = UiArt + "/Combo/";
        private const string CatalogPath = "Assets/_Project/Config/SfxCatalog.asset";
        private const string Sounds = "Assets/_Project/Audio/Sfx/";

        [MenuItem("Tools/Squeaky Lane/018 UI/Build Combo")]
        public static void Build()
        {
            RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) { throw new InvalidOperationException("Mở Play.unity trước."); }
            var theme = RequireAsset<UiTheme>(UiThemePath);
            AssignArt(theme);
            AssignSounds();
            var bootstrap = Object.FindObjectOfType<GameBootstrap>(true);
            var navigation = Object.FindObjectOfType<NavigationView>(true);
            if (bootstrap == null || navigation == null) { throw new InvalidOperationException("Thiếu GameBootstrap/NavigationView."); }
            var data = new SerializedObject(navigation);
            var playing = (CanvasGroup)data.FindProperty("playing").objectReferenceValue;
            var won = (CanvasGroup)data.FindProperty("won").objectReferenceValue;
            var lost = (CanvasGroup)data.FindProperty("lost").objectReferenceValue;
            var template = (Component)data.FindProperty("wonText").objectReferenceValue;

            var anchor = playing.transform.Find("018 Combo") as RectTransform;
            if (anchor == null)
            {
                anchor = ComboLabel.Create("018 Combo", (RectTransform)playing.transform);
                anchor.anchorMin = anchor.anchorMax = new Vector2(.5f, 1);
                anchor.anchoredPosition = new Vector2(0, -440);
            }

            var feedback = anchor.GetComponent<ComboFeedback>();
            if (feedback == null) { feedback = anchor.gameObject.AddComponent<ComboFeedback>(); }
            var wonSummary = Summary(won, template);
            var lostSummary = Summary(lost, template);
            Assign(feedback, ("theme", theme), ("popupAnchor", anchor), ("wonSummary", wonSummary), ("lostSummary", lostSummary));
            Assign(bootstrap, ("comboFeedback", feedback));
            PrefabUtility.RecordPrefabInstancePropertyModifications(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("018: đã gán hình/tiếng combo và gắn ComboFeedback, ComboSummary vào Play.unity.");
        }

        private static ComboSummary Summary(CanvasGroup dialog, Component template)
        {
            var card = dialog.transform.Find("013 Card") as RectTransform;
            var buttons = card != null ? card.Find("013 Body/013 Buttons") as RectTransform : null;
            if (buttons == null) { throw new InvalidOperationException(dialog.name + ": thiếu 013 Card/013 Body/013 Buttons."); }
            var summary = card.GetComponent<ComboSummary>();
            if (summary == null) { summary = card.gameObject.AddComponent<ComboSummary>(); }
            Assign(summary, ("card", card), ("buttons", buttons), ("textTemplate", template));
            return summary;
        }

        private static void Assign(Object target, params (string name, Object value)[] fields)
        {
            var data = new SerializedObject(target);
            foreach (var field in fields) { data.FindProperty(field.name).objectReferenceValue = field.value; }
            data.ApplyModifiedPropertiesWithoutUndo();
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
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return RequireAsset<Sprite>(path);
        }

        private static void AssignArt(UiTheme theme)
        {
            var data = new SerializedObject(theme);
            string[] fields = { "comboWord", "recordBanner", "comboBurst", "comboSpark", "comboStreak", "comboRing" };
            string[] files = { "combo_word", "record_new", "fx_burst", "fx_spark", "fx_streak", "fx_ring" };
            for (int i = 0; i < fields.Length; i++) { data.FindProperty(fields[i]).objectReferenceValue = Import(files[i]); }
            var digits = data.FindProperty("comboDigits");
            digits.arraySize = 10;
            for (int i = 0; i < 10; i++) { digits.GetArrayElementAtIndex(i).objectReferenceValue = Import("combo_digit_" + i); }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssetIfDirty(theme);
        }

        private static void AssignSounds()
        {
            var catalog = RequireAsset<SfxCatalog>(CatalogPath);
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("entries");
            SetSound(entries, SoundCue.RecordWhoosh, "sfx_record_whoosh", .55f);
            SetSound(entries, SoundCue.RecordSlam, "sfx_record_slam", .75f);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        // Đã có dòng thì giữ âm lượng người làm đã chỉnh.
        private static void SetSound(SerializedProperty entries, SoundCue cue, string file, float volume)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("cue").intValue == (int)cue) { return; }
            }

            var clip = RequireAsset<AudioClip>(Sounds + file + ".wav");
            entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("cue").intValue = (int)cue;
            var clips = entry.FindPropertyRelative("clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            entry.FindPropertyRelative("volume").floatValue = volume;
            entry.FindPropertyRelative("pitchMin").floatValue = 1;
            entry.FindPropertyRelative("pitchMax").floatValue = 1;
            entry.FindPropertyRelative("cooldown").floatValue = .1f;
        }
    }
}
