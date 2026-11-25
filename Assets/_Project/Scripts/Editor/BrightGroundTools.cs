using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public static class BrightGroundTools
    {
        private const string Art = UiThemeAssets.Art + "/City";
        private const string RootPath = "Assets/_Project/Prefabs/GameRoot.prefab";
        private const string EnvironmentPath = "Assets/_Project/Prefabs/CityEnvironment.prefab";
        private const string CameraPath = "Assets/_Project/Config/CameraConfig.asset";
        private const string ReportPath = "docs/handoff/013-scene-changes.md";

        [MenuItem("Tools/Squeaky Lane/013 Theme/S1 Apply Bright Ground")]
        public static void Apply()
        {
            UiThemeAssets.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || scene.path != "Assets/_Project/Scenes/Play.unity" || scene.isDirty)
            { throw new InvalidOperationException("Đóng Prefab Mode, mở Play.unity và lưu thay đổi đang sửa trước khi chạy S1."); }
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            UiSkinTools.ValidateTheme(theme);
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            var board = UnityEngine.Object.FindObjectOfType<BoardView>(true);
            var camera = Camera.main;
            var config = AssetDatabase.LoadAssetAtPath<CameraConfig>(CameraPath);
            var palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            var rootAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RootPath);
            if (nav == null || board == null || camera == null || config == null || palette == null || rootAsset == null ||
                nav.gameObject.scene != scene || board.gameObject.scene != scene || rootAsset.GetComponent<BoardView>() == null)
            { throw new InvalidOperationException("Thiếu NavigationView/BoardView/camera/config trong scene Play hoặc prefab GameRoot."); }
            var safeArea = new SerializedObject(nav).FindProperty("safeAreaRoot").objectReferenceValue as RectTransform;
            var canvas = safeArea != null ? safeArea.GetComponentInParent<Canvas>() : null;
            if (canvas == null) { throw new InvalidOperationException("Thiếu Canvas của safeAreaRoot."); }
            Transform uiRoot = safeArea;
            while (uiRoot.parent != canvas.transform) { uiRoot = uiRoot.parent; }
            var existing = canvas.transform.Find("013 Vignette");
            if (existing != null && existing.GetComponent<Image>() == null)
            { throw new InvalidOperationException("013 Vignette đã tồn tại nhưng không phải Image; dừng để không ghi đè object khác."); }
            if (Shader.Find("Unlit/Texture") == null) { throw new InvalidOperationException("Thiếu shader Built-in Unlit/Texture."); }

            string bindings = EditorJsonUtility.ToJson(nav);
            var report = new StringBuilder("\n## S1 · nền sáng · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n");
            foreach (string path in new[] { scene.path, RootPath, UiThemeAssets.ThemePath, CameraPath, EnvironmentPath }) { UiThemeAssets.Backup(path); }
            UiThemeAssets.EnsureFolder(Art + "/Materials");
            // Áp palette V2 vào asset P1; những field font/sprite/binding không bị đụng tới.
            var defaults = ScriptableObject.CreateInstance<UiTheme>();
            try { Set(theme, "grass", defaults.Grass, report); }
            finally { UnityEngine.Object.DestroyImmediate(defaults); }
            Set(theme, "propCount", 0, report);
            Set(theme, "hideCarNumbers", false, report);
            Set(theme, "gateStrips", false, report);
            var ground = BuildGroundMaterial(theme, palette);
            BuildEnvironment(ground);
            Set(theme, "groundMaterial", ground, report);
            Set(theme, "environmentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath), report);
            AssetDatabase.SaveAssetIfDirty(theme);
            Set(config, "padding", theme.CameraPadding, report);
            AssetDatabase.SaveAssetIfDirty(config);
            var prefab = PrefabUtility.LoadPrefabContents(RootPath);
            try
            {
                Set(prefab.GetComponent<BoardView>(), "sceneTheme", theme, report);
                PrefabUtility.SaveAsPrefabAsset(prefab, RootPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            Set(board, "sceneTheme", theme, report);
            Set(camera, "m_BackGroundColor", theme.CameraBackground, report);
            Set(camera, "m_ClearFlags", (int)CameraClearFlags.SolidColor, report);

            var vignette = existing != null ? existing.gameObject : new GameObject("013 Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(vignette, "013 S1 vignette");
                vignette.transform.SetParent(canvas.transform, false);
                report.AppendLine(PathOf(vignette.transform) + " · GameObject · tồn tại · không → Image mới, dưới UIRoot");
            }
            var rect = vignette.GetComponent<RectTransform>();
            Set(rect, "m_AnchorMin", Vector2.zero, report); Set(rect, "m_AnchorMax", Vector2.one, report);
            Set(rect, "m_AnchoredPosition", Vector2.zero, report); Set(rect, "m_SizeDelta", Vector2.zero, report);
            var image = vignette.GetComponent<Image>();
            Set(image, "m_Sprite", BuildVignette(theme), report);
            Set(image, "m_Color", Color.white, report); Set(image, "m_RaycastTarget", false, report);
            Set(image, "m_Type", (int)Image.Type.Simple, report);
            int oldIndex = rect.GetSiblingIndex();
            rect.SetAsLastSibling(); rect.SetSiblingIndex(uiRoot.GetSiblingIndex());
            report.AppendLine(PathOf(rect) + " · Transform · siblingIndex · " + oldIndex + " → " + rect.GetSiblingIndex() + " (trước " + uiRoot.name + ")");
            if (EditorJsonUtility.ToJson(nav) != bindings) { throw new InvalidOperationException("NavigationView bị thay đổi ngoài S1; không lưu scene."); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("Asset procedural: GroundColors.png (2 hàng, cỏ gần/xa), Ground.mat (Unlit/Texture), Vignette.png (alpha theo UiTheme). Không sửa ánh sáng/props/012; NavigationView giữ nguyên.");
            File.AppendAllText(ReportPath, report.ToString());
            Debug.Log("013 S1 đã áp: nền cỏ/vỉa hè 1 mesh/1 material, padding theo UiTheme, vignette dưới HUD không chặn input. Chạy Run All + Console, Play chụp màn01/30 và Stats16 trước S2. Báo cáo: " + ReportPath);
        }

        private static Material BuildGroundMaterial(UiTheme theme, VisualPalette palette)
        {
            var texture = new Texture2D(9, 2, TextureFormat.RGBA32, false);
            try
            {
                for (int row = 0; row < 2; row++)
                {
                    texture.SetPixel(0, row, row == 0 ? theme.Grass : theme.GrassFar);
                    texture.SetPixel(1, row, theme.Pavement);
                    for (int i = 0; i <= (int)TileColor.Water; i++) { texture.SetPixel(i + 2, row, palette.Car((TileColor)i)); }
                }
                texture.Apply();
                WriteTexture(Art + "/GroundColors.png", texture, false);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            string path = Art + "/Materials/Ground.mat";
            UiThemeAssets.Backup(path);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Unlit/Texture")); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Unlit/Texture");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/GroundColors.png");
            material.mainTextureScale = Vector2.one; material.mainTextureOffset = Vector2.zero;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void BuildEnvironment(Material material)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("013 City Environment", typeof(SceneThemeView));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
                var renderer = root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                PrefabUtility.SaveAsPrefabAsset(root, EnvironmentPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static Sprite BuildVignette(UiTheme theme)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float nx = x * 2f / (size - 1) - 1, ny = y * 2f / (size - 1) - 1;
                    float edge = Mathf.InverseLerp(theme.VignetteInnerRadius, 1, Mathf.Sqrt(nx * nx + ny * ny));
                    var color = theme.Vignette; color.a *= Mathf.SmoothStep(0, 1, edge);
                    texture.SetPixel(x, y, color);
                }
                texture.Apply(); WriteTexture(Art + "/Vignette.png", texture, true);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Vignette.png");
        }

        private static void WriteTexture(string path, Texture2D texture, bool sprite)
        {
            UiThemeAssets.Backup(path);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp; importer.alphaIsTransparency = sprite;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static string PathOf(Transform node) => node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
        private static string Value(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    var value = property.objectReferenceValue;
                    return value == null ? "null" : value.name + " [" + AssetDatabase.GetAssetPath(value) + "]";
                case SerializedPropertyType.Color: return "#" + ColorUtility.ToHtmlStringRGBA(property.colorValue);
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString();
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.Float: return property.floatValue.ToString("0.###");
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum: return property.intValue.ToString();
                default: return property.propertyType.ToString();
            }
        }
        private static void Set(UnityEngine.Object target, string field, object value, StringBuilder report)
        {
            var data = new SerializedObject(target);
            var property = data.FindProperty(field);
            if (property == null) { throw new InvalidOperationException(target.name + " thiếu field " + field); }
            string old = Value(property);
            if (value is Color color) { property.colorValue = color; }
            else if (value is bool boolean) { property.boolValue = boolean; }
            else if (value is int integer) { property.intValue = integer; }
            else if (value is float number) { property.floatValue = number; }
            else if (value is Vector2 vector) { property.vector2Value = vector; }
            else { property.objectReferenceValue = (UnityEngine.Object)value; }
            data.ApplyModifiedProperties();
            if (target is Component component && PrefabUtility.IsPartOfPrefabInstance(component))
            { PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
            EditorUtility.SetDirty(target);
            report.AppendLine((target is Component c ? PathOf(c.transform) : AssetDatabase.GetAssetPath(target)) + " · " + target.GetType().Name + " · " + field + " · " + old + " → " + Value(property));
        }
    }
}
