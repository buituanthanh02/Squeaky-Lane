using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public static class SceneryTools
    {
        internal const string Art = UiThemeAssets.Art + "/City";
        internal const string ShadowPath = "Assets/_Project/Prefabs/Environment/ContactShadow.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string RootPath = "Assets/_Project/Prefabs/GameRoot.prefab";
        private static readonly string[] NorthModels = { "Racing/pitsGarage.fbx", "Racing/pitsOffice.fbx", "Racing/treeSmall.fbx", "Racing/treeLarge.fbx", "CityRoads/FBX format/light-square.fbx" };
        private static readonly string[] SouthModels = { "CityRoads/FBX format/road-straight.fbx", "CityRoads/FBX format/construction-cone.fbx", "Racing/fenceStraight.fbx" };
        private static StringBuilder report;

        [MenuItem("Tools/Squeaky Lane/013 Theme/S4 Finish Scenery and Open Lab")]
        public static void FinishAndOpenLab()
        {
            UiThemeAssets.RequireEditMode();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { throw new InvalidOperationException("Thoát Prefab Mode trước khi hoàn tất S4."); }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (current.path != ScenePath || current.isDirty)
            { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }
            Apply();
            AssetLabTools.Open();
        }

        [MenuItem("Tools/Squeaky Lane/013 Theme/S4 Apply Scenery")]
        public static void Apply()
        {
            UiThemeAssets.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || scene.path != ScenePath || scene.isDirty)
            { throw new InvalidOperationException("Mở Play.unity, thoát Prefab/Play Mode và lưu phần đang sửa trước S4."); }
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            var palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            var cars = AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/_Project/Config/CarCatalog.asset");
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            var board = UnityEngine.Object.FindObjectOfType<BoardView>(true);
            if (theme == null || palette == null || cars == null || nav == null || board == null || board.gameObject.scene != scene)
            { throw new InvalidOperationException("Thiếu theme/catalog/NavigationView/BoardView của Play."); }
            foreach (string model in NorthModels) { RequireModel(model); }
            foreach (string model in SouthModels) { RequireModel(model); }
            if (Shader.Find("SqueakyLane/Contact Shadow") == null) { throw new InvalidOperationException("Chờ Unity import ContactShadow.shader trước khi áp S4."); }
            string binding = UiSkinTools.CaptureBindings(nav);
            foreach (string path in new[] { ScenePath, RootPath, UiThemeAssets.ThemePath, "Assets/_Project/Prefabs/CityEnvironment.prefab" }) { UiThemeAssets.Backup(path); }
            report = new StringBuilder("\n## S4 · ánh sáng/bóng giả/khay/props · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n\n");
            Set(theme, "lightIntensity", 1f);
            Set(theme, "lightAngles", new Vector3(50, -30, 0));
            Set(theme, "contactShadowOffset", new Vector2(-.12f, .1f));
            Set(theme, "ambient", new Color32(154, 169, 194, 255));
            Set(theme, "pavement", new Color32(195, 204, 216, 255));
            Set(theme, "trayEnabled", true);
            Set(theme, "southPropsEnabled", true);
            Set(theme, "contactShadowPrefab", BuildShadow(theme));
            BuildProps(theme, cars);
            var oldGroundImporter = AssetImporter.GetAtPath(Art + "/GroundColors.png") as TextureImporter;
            string oldNpot = oldGroundImporter != null ? oldGroundImporter.npotScale.ToString() : "không";
            var ground = BrightGroundTools.BuildGroundMaterial(theme, palette);
            report.AppendLine(Art + "/GroundColors.png · TextureImporter · npotScale · " + oldNpot + " → None (giữ đúng11 cột màu)");
            BrightGroundTools.BuildEnvironment(ground);
            Set(theme, "groundMaterial", ground);
            Set(theme, "environmentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/CityEnvironment.prefab"));
            AssetDatabase.SaveAssetIfDirty(theme);
            var root = PrefabUtility.LoadPrefabContents(RootPath);
            try
            {
                Set(root.GetComponent<BoardView>(), "sceneTheme", theme);
                PrefabUtility.SaveAsPrefabAsset(root, RootPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Set(board, "sceneTheme", theme);
            foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>(true))
            {
                if (light.gameObject.scene != scene) { continue; }
                Change(light, "shadows", light.shadows.ToString(), LightShadows.None.ToString(), () => light.shadows = LightShadows.None);
                if (light.type != LightType.Directional) { continue; }
                Change(light, "intensity", light.intensity.ToString(), theme.LightIntensity.ToString(), () => light.intensity = theme.LightIntensity);
                Change(light.transform, "eulerAngles", light.transform.eulerAngles.ToString(), theme.LightAngles.ToString(), () => light.transform.eulerAngles = theme.LightAngles);
            }
            report.AppendLine("Play/RenderSettings · RenderSettings · ambientMode · " + RenderSettings.ambientMode + " → Flat");
            report.AppendLine("Play/RenderSettings · RenderSettings · ambientLight · " + RenderSettings.ambientLight + " → " + theme.Ambient);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = theme.Ambient;
            if (UiSkinTools.CaptureBindings(nav) != binding) { throw new InvalidOperationException("Binding/OnClick UI đã đổi ngoài S4; chưa lưu Play."); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            report.AppendLine("Runtime (sinh theo kích thước màn) · BoardView/SceneThemeView · trang trí · cũ → bóng trực tiếp root xe/vật, khay1mesh/1material, ≤12 props, không collider/material instance/Update bóng. Không thay tọa độ ô/luật/012.");
            WriteReport(report.ToString());
            Debug.Log("S4 đã áp. Run All + Console0; Play/reload/Undo/Restart, ảnh01/30/SE và Stats16 568×320. Một material chưa bảo đảm1batch: đo ngưỡng~200. Scene field: docs/handoff/013-scene-changes.md");
        }

        internal static GameObject BuildShadow(UiTheme theme, bool onlyIfMissing = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowPath);
            if (onlyIfMissing && existing != null) { return existing; }
            Folders();
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            string png = Art + "/ContactShadow.png";
            try
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (x + .5f) / size * 2 - 1, ny = (y + .5f) / size * 2 - 1;
                        float r = Mathf.Sqrt(nx * nx + ny * ny);
                        float alpha = Mathf.Pow(Mathf.Clamp01(1 - r), theme.ContactShadowFalloff);
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                }
                texture.Apply();
                UiThemeAssets.Backup(png);
                File.WriteAllBytes(png, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(png);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            string materialPath = Art + "/Materials/ContactShadow.mat";
            UiThemeAssets.Backup(materialPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("SqueakyLane/Contact Shadow")); AssetDatabase.CreateAsset(material, materialPath); }
            material.shader = Shader.Find("SqueakyLane/Contact Shadow");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            material.color = new Color(0, 0, 0, theme.ContactShadowAlpha);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            var mesh = new Mesh { name = "013 Contact Shadow Quad" };
            mesh.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var saved = SaveMesh(mesh, Art + "/Meshes/ContactShadow.asset");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("ContactShadow", typeof(ContactShadowView));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
                root.GetComponent<MeshFilter>().sharedMesh = saved;
                var renderer = root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                UiThemeAssets.Backup(ShadowPath);
                existing = PrefabUtility.SaveAsPrefabAsset(root, ShadowPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            AssetDatabase.SaveAssetIfDirty(material);
            return existing;
        }

        private static void BuildProps(UiTheme theme, CarCatalog cars)
        {
            Folders();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var north = new GameObject[6];
                north[0] = SceneThemeTools.BakeProp(NorthModels[0], theme.BuildingSizes.x, preview);
                north[1] = SceneThemeTools.BakeProp(NorthModels[1], theme.BuildingSizes.y, preview);
                north[2] = SceneThemeTools.BakeProp(NorthModels[2], theme.TreeSizes.x, preview);
                north[3] = SceneThemeTools.BakeProp(NorthModels[3], theme.TreeSizes.y, preview);
                north[4] = north[2];
                north[5] = SceneThemeTools.BakeProp(NorthModels[4], theme.SceneryLightSize, preview);
                SetArray(theme, "propPrefabs", north);
                Set(theme, "propCount", north.Length);
                var south = new GameObject[3];
                south[0] = SceneThemeTools.BakeProp(SouthModels[0], theme.SceneryRoadSize, preview, theme.SouthPropHeight);
                south[1] = SceneThemeTools.BakeProp(SouthModels[1], theme.SouthPropHeight, preview, theme.SouthPropHeight);
                south[2] = SceneThemeTools.BakeProp(SouthModels[2], theme.PropSpacing, preview, theme.SouthPropHeight);
                SetArray(theme, "southPropPrefabs", south);
                Set(theme, "parkedCarPrefab", BakeParkedCar(theme, cars, preview));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static GameObject BakeParkedCar(UiTheme theme, CarCatalog catalog, UnityEngine.SceneManagement.Scene preview)
        {
            var entry = catalog.Find(SqueakyLane.Core.Model.TileColor.Pink);
            if (entry == null || entry.carPrefab == null) { throw new InvalidOperationException("Thiếu xe mẫu Pink để bake xe đỗ không gameplay."); }
            var car = entry.carPrefab.GetComponent<CarView>();
            var modelRoot = new SerializedObject(car).FindProperty("modelRoot").objectReferenceValue as Transform;
            var root = new GameObject("ParkedCar");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
            var model = UnityEngine.Object.Instantiate(modelRoot, root.transform, false);
            var bounds = SceneThemeView.LocalBounds(root);
            float fit = theme.SouthPropHeight / Mathf.Max(.001f, bounds.size.y);
            model.localScale *= fit;
            model.localPosition *= fit;
            model.localPosition += new Vector3(-bounds.center.x * fit, -bounds.min.y * fit, -bounds.center.z * fit);
            var parts = new List<CombineInstance>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                { parts.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub, transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix }); }
            }
            var mesh = new Mesh { name = "ParkedCar combined" };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            var saved = SaveMesh(mesh, Art + "/Meshes/ParkedCar.asset");
            UnityEngine.Object.DestroyImmediate(model.gameObject);
            root.AddComponent<MeshFilter>().sharedMesh = saved;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = entry.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            const string path = "Assets/_Project/Prefabs/Environment/ParkedCar.prefab";
            UiThemeAssets.Backup(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            UiThemeAssets.Backup(path);
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, saved);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(saved); AssetDatabase.SaveAssetIfDirty(saved);
            return saved;
        }
        private static void Folders()
        {
            UiThemeAssets.EnsureFolder(Art + "/Materials");
            UiThemeAssets.EnsureFolder(Art + "/Meshes");
            UiThemeAssets.EnsureFolder("Assets/_Project/Prefabs/Environment");
        }
        private static void RequireModel(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Mechanics/Kenney/" + path) == null)
            { throw new FileNotFoundException("Thiếu model đã có trong repo: " + path); }
        }
        private static string PathOf(Transform node) => node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
        private static void Change(UnityEngine.Object owner, string field, string before, string after, Action apply)
        {
            Undo.RecordObject(owner, "013 S4 " + field);
            apply();
            EditorUtility.SetDirty(owner);
            if (before != after) { report.AppendLine((owner is Component c ? PathOf(c.transform) : AssetDatabase.GetAssetPath(owner)) + " · " + owner.GetType().Name + " · " + field + " · " + before + " → " + after); }
        }
        private static void Set(UnityEngine.Object owner, string field, object value)
        {
            var data = new SerializedObject(owner);
            var p = data.FindProperty(field);
            if (p == null) { throw new InvalidOperationException(owner.name + " thiếu " + field); }
            string before = Value(p);
            if (value is float number) { p.floatValue = number; }
            else if (value is int integer) { p.intValue = integer; }
            else if (value is bool boolean) { p.boolValue = boolean; }
            else if (value is Color color) { p.colorValue = color; }
            else if (value is Color32 color32) { p.colorValue = color32; }
            else if (value is Vector3 vector) { p.vector3Value = vector; }
            else if (value is Vector2 vector2) { p.vector2Value = vector2; }
            else { p.objectReferenceValue = (UnityEngine.Object)value; }
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);
            if (owner is Component c && PrefabUtility.IsPartOfPrefabInstance(c)) { PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            string after = Value(p);
            if (before != after) { report.AppendLine((owner is Component component ? PathOf(component.transform) : AssetDatabase.GetAssetPath(owner)) + " · " + owner.GetType().Name + " · " + field + " · " + before + " → " + after); }
        }
        private static string Value(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue == null ? "null" : AssetDatabase.GetAssetPath(p.objectReferenceValue);
                case SerializedPropertyType.Color: return "#" + ColorUtility.ToHtmlStringRGBA(p.colorValue);
                case SerializedPropertyType.Float: return p.floatValue.ToString("0.###");
                case SerializedPropertyType.Integer: return p.intValue.ToString();
                case SerializedPropertyType.Boolean: return p.boolValue.ToString();
                case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
                case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
                default: return p.propertyType.ToString();
            }
        }
        private static void SetArray(UiTheme owner, string field, GameObject[] values)
        {
            var data = new SerializedObject(owner);
            var p = data.FindProperty(field);
            var before = new StringBuilder();
            for (int i = 0; i < p.arraySize; i++) { before.Append(Value(p.GetArrayElementAtIndex(i))).Append(", "); }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) { p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);
            report.AppendLine(AssetDatabase.GetAssetPath(owner) + " · UiTheme · " + field + " · " + before + " → " + string.Join(", ", Array.ConvertAll(values, item => AssetDatabase.GetAssetPath(item))));
        }
        private static void WriteReport(string section)
        {
            const string path = "docs/handoff/013-scene-changes.md";
            string text = File.Exists(path) ? File.ReadAllText(path) : "# 013 — Thay đổi scene/prefab\n";
            int start = text.IndexOf("\n## S4 ·", StringComparison.Ordinal);
            if (start >= 0)
            {
                int end = text.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
                string old = text.Substring(start, (end >= 0 ? end : text.Length) - start);
                int lab = old.IndexOf("\n### Asset Lab\n", StringComparison.Ordinal);
                if (lab >= 0) { section += old.Substring(lab); }
                text = text.Substring(0, start) + section + (end >= 0 ? text.Substring(end) : "");
            }
            else { text += section; }
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }
    }
}
