using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public static class AssetLabTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/AssetLab.unity";
        private const string ConfigPath = "Assets/_Project/Config/AssetLabConfig.asset";

        [MenuItem("Tools/Squeaky Lane/013 Theme/S4 Open Asset Lab")]
        public static void Open()
        {
            UiThemeAssets.RequireEditMode();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { throw new InvalidOperationException("Thoát Prefab Mode trước khi mở Asset Lab."); }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            var config = AssetDatabase.LoadAssetAtPath<AssetLabConfig>(ConfigPath);
            if (config == null) { config = BuildConfig(); }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Asset Lab", typeof(AssetLabView));
                var light = new GameObject("Preview Directional Light", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                var camera = new GameObject("Preview Camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(70, 0, 0);
                camera.transform.position = -camera.transform.forward * 12;
                camera.orthographic = true;
                camera.orthographicSize = 4;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(110, 124, 145, 255);
                var data = new SerializedObject(root.GetComponent<AssetLabView>());
                data.FindProperty("config").objectReferenceValue = config;
                data.FindProperty("previewLight").objectReferenceValue = light;
                data.ApplyModifiedPropertiesWithoutUndo();
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = config.Ambient;
                light.intensity = config.LightIntensity;
                light.transform.rotation = Quaternion.Euler(config.LightAngles);
                EditorSceneManager.SaveScene(scene, ScenePath);
                ReportCreatedLab(config);
            }
            var view = UnityEngine.Object.FindObjectOfType<AssetLabView>();
            if (view != null) { view.RefreshPreview(); }
            Selection.activeObject = config;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.rotation = Quaternion.Euler(45, -30, 0);
                SceneView.lastActiveSceneView.sceneLighting = true;
                SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, Vector3.one * 5), false);
            }
            Debug.Log("Asset Lab sẵn: chọn AssetLabConfig trong Inspector, đổi Mẫu; Scene view cập nhật ngay, không cần Play. Bật Scene Lighting (biểu tượng mặt trời) để thấy ánh sáng config. Chỉnh thử không sửa prefab/material/UiTheme của Play. Không thêm AssetLab vào Build Settings.");
        }

        private static AssetLabConfig BuildConfig()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            if (theme == null) { throw new InvalidOperationException("Thiếu UiTheme của 013."); }
            var shadow = SceneryTools.BuildShadow(theme, true);
            const string floorPath = SceneryTools.Art + "/Materials/AssetLabFloor.mat";
            var floor = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
            if (floor == null)
            {
                floor = new Material(Shader.Find("Standard"));
                floor.color = Color.white;
                floor.SetFloat("_Glossiness", 0);
                AssetDatabase.CreateAsset(floor, floorPath);
            }
            var samples = new List<AssetLabConfig.Sample>();
            var cars = AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/_Project/Config/CarCatalog.asset");
            for (int color = 0; cars != null && color < (int)TileColor.Water; color++)
            {
                var entry = cars.Find((TileColor)color);
                if (entry == null) { continue; }
                Add(samples, "Xe / " + entry.color, entry.carPrefab);
                Add(samples, "Xe tải / " + entry.color, entry.truckPrefab);
            }
            var mechanics = AssetDatabase.LoadAssetAtPath<MechanicCatalog>("Assets/_Project/Config/MechanicCatalog.asset");
            if (mechanics != null)
            {
                var entries = new SerializedObject(mechanics).FindProperty("entries");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    Add(samples, "Cơ chế / " + (MechanicKind)entry.FindPropertyRelative("kind").intValue + " / " + entry.FindPropertyRelative("variant").intValue,
                        entry.FindPropertyRelative("prefab").objectReferenceValue as GameObject);
                }
            }
            foreach (string path in new[] { "Racing/pitsGarage", "Racing/pitsOffice", "Racing/treeSmall", "Racing/treeLarge", "Racing/fenceStraight", "CityRoads/FBX format/light-square", "CityRoads/FBX format/road-straight", "CityRoads/FBX format/construction-cone" })
            { Add(samples, "Prop / " + System.IO.Path.GetFileName(path), AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Mechanics/Kenney/" + path + ".fbx")); }
            var config = ScriptableObject.CreateInstance<AssetLabConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            var data = new SerializedObject(config);
            data.FindProperty("shadowPrefab").objectReferenceValue = shadow;
            data.FindProperty("groundMaterial").objectReferenceValue = floor;
            data.FindProperty("mechanics").objectReferenceValue = mechanics;
            var array = data.FindProperty("samples");
            array.arraySize = samples.Count;
            for (int i = 0; i < samples.Count; i++)
            {
                array.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue = samples[i].label;
                array.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = samples[i].prefab;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            return config;
        }
        private static void Add(List<AssetLabConfig.Sample> list, string label, GameObject prefab)
        { if (prefab != null) { list.Add(new AssetLabConfig.Sample { label = label, prefab = prefab }); } }

        private static void ReportCreatedLab(AssetLabConfig config)
        {
            const string path = "docs/handoff/013-scene-changes.md";
            var lab = new StringBuilder("\n### Asset Lab\n\n");
            lab.AppendLine(ScenePath + " · Scene · tồn tại · không → scene kiểm riêng, không thêm Build Settings");
            lab.AppendLine("Asset Lab · AssetLabView · config · không → " + ConfigPath);
            lab.AppendLine("Asset Lab · AssetLabView · previewLight · không → Preview Directional Light");
            lab.AppendLine("Preview Directional Light · Light · type/shadows/intensity · không → Directional/None/" + config.LightIntensity);
            lab.AppendLine("Preview Directional Light · Transform · eulerAngles · không → " + config.LightAngles);
            lab.AppendLine("Preview Camera · Camera · projection/orthographicSize/clearFlags/backgroundColor · không → Orthographic/4/SolidColor/#6E7C91");
            lab.AppendLine("Preview Camera · Transform · rotation/position · không → Euler(70,0,0)/−forward×12");
            lab.AppendLine("AssetLab/RenderSettings · RenderSettings · ambientMode/ambientLight · scene mặc định → Flat/" + config.Ambient);
            lab.AppendLine(ConfigPath + " · AssetLabConfig · samples/shadowPrefab/groundMaterial · không → " + config.Samples.Length + " mẫu/" + SceneryTools.ShadowPath + "/AssetLabFloor.mat");
            lab.AppendLine("Object preview · GameObject · lưu vào scene/build · không → vẫn không (DontSaveInEditor/Build), chỉ hiển thị Editor và hủy khi đổi mẫu.");
            string text = File.Exists(path) ? File.ReadAllText(path) : "# 013 — Thay đổi scene/prefab\n";
            int start = text.IndexOf("\n## S4 ·", StringComparison.Ordinal);
            if (start < 0) { text += "\n## S4 · Asset Lab (scenery chưa áp)\n" + lab; }
            else
            {
                int end = text.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
                if (end < 0) { end = text.Length; }
                string section = text.Substring(start, end - start);
                int previous = section.IndexOf("\n### Asset Lab\n", StringComparison.Ordinal);
                if (previous >= 0) { section = section.Substring(0, previous); }
                text = text.Substring(0, start) + section + lab + text.Substring(end);
            }
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }
    }

    [CustomEditor(typeof(AssetLabConfig))]
    public sealed class AssetLabConfigInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (AssetLabConfig)target;
            var labels = Array.ConvertAll(config.Samples, sample => sample == null ? "(trống)" : sample.label);
            var index = serializedObject.FindProperty("selectedSample");
            index.intValue = EditorGUILayout.Popup("Mẫu đang xem", index.intValue, labels);
            EditorGUILayout.HelpBox("Đổi mẫu hoặc thông số: Scene view cập nhật ngay, không cần Play. Bật Scene Lighting (mặt trời). Đây là config thử riêng, không chỉnh asset game.", MessageType.Info);
            DrawPropertiesExcluding(serializedObject, "m_Script", "selectedSample", "samples", "shadowPrefab", "groundMaterial", "mechanics");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("samples"), new GUIContent("Danh sách mẫu (nâng cao)"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
