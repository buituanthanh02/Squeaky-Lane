using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public static class SceneThemeTools
    {
        private const string Art = UiThemeAssets.Art + "/City";
        private const string RootPath = "Assets/_Project/Prefabs/GameRoot.prefab";
        private const string EnvironmentPath = "Assets/_Project/Prefabs/CityEnvironment.prefab";
        private static readonly string[] Models =
        {
            "Racing/pitsGarage.fbx", "Racing/treeSmall.fbx", "Racing/treeLarge.fbx", "Racing/fenceStraight.fbx", "CityRoads/FBX format/light-square.fbx"
        };
        private static readonly float[] ModelSizes = { 2.4f, 1.1f, 1.5f, 1.8f, .9f };

        // Bản P2/P3 cũ chưa áp. Visual V2 tách S1 khỏi props/nhãn xe; không đưa menu cũ ra Editor.
        public static void Apply()
        {
            UiThemeAssets.RequireEditMode();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { throw new InvalidOperationException("Đóng Prefab Mode trước khi áp cảnh."); }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Play.unity" || scene.isDirty)
            { throw new InvalidOperationException("Mở Play và lưu phần đang sửa trước khi chạy menu3."); }
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            UiSkinTools.ValidateTheme(theme);
            var palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            foreach (string model in Models)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Mechanics/Kenney/" + model) == null)
                { throw new FileNotFoundException("Thiếu model đã có trong repo: " + model); }
            }
            UiThemeAssets.EnsureFolder(Art + "/Materials");
            UiThemeAssets.EnsureFolder(Art + "/Meshes");
            UiThemeAssets.EnsureFolder("Assets/_Project/Prefabs/Environment");
            UiThemeAssets.Backup(scene.path);
            UiThemeAssets.Backup(RootPath);
            UiThemeAssets.Backup(UiThemeAssets.ThemePath);
            var texture = new Texture2D(9, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, theme.Grass); texture.SetPixel(1, 0, theme.Pavement);
            for (int i = 0; i <= (int)TileColor.Water; i++) { texture.SetPixel(i + 2, 0, palette.Car((TileColor)i)); }
            texture.Apply();
            string texturePath = Art + "/GroundColors.png";
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var ground = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Ground.mat");
            if (ground == null)
            {
                ground = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(ground, Art + "/Materials/Ground.mat");
            }
            ground.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            ground.color = Color.white; ground.enableInstancing = true;
            ground.SetFloat("_Glossiness", 0);
            EditorUtility.SetDirty(ground); AssetDatabase.SaveAssetIfDirty(ground);
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("013 City Environment", typeof(SceneThemeView));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
            var prefabs = new GameObject[Models.Length];
            try
            {
                root.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                root.GetComponent<MeshRenderer>().receiveShadows = false;
                root.GetComponent<MeshRenderer>().sharedMaterial = ground;
                PrefabUtility.SaveAsPrefabAsset(root, EnvironmentPath);
                for (int i = 0; i < Models.Length; i++)
                {
                    prefabs[i] = BakeProp(Models[i], ModelSizes[i], preview);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            var data = new SerializedObject(theme);
            data.FindProperty("environmentPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath);
            data.FindProperty("groundMaterial").objectReferenceValue = ground;
            var array = data.FindProperty("propPrefabs"); array.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++) { array.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i]; }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(theme);
            var gameRoot = PrefabUtility.LoadPrefabContents(RootPath);
            try
            {
                var board = new SerializedObject(gameRoot.GetComponent<BoardView>());
                board.FindProperty("sceneTheme").objectReferenceValue = theme;
                board.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(gameRoot, RootPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(gameRoot); }
            var cameraConfig = AssetDatabase.LoadAssetAtPath<CameraConfig>("Assets/_Project/Config/CameraConfig.asset");
            UiThemeAssets.Backup(AssetDatabase.GetAssetPath(cameraConfig));
            var cameraData = new SerializedObject(cameraConfig);
            float oldPadding = cameraData.FindProperty("padding").floatValue;
            cameraData.FindProperty("padding").floatValue = .1f;
            cameraData.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(cameraConfig);
            var camera = Camera.main;
            string changes = "\n## 013 · Cảnh quanh bãi\nCameraConfig · padding · " + oldPadding + " → 0.1\nGameRoot/BoardView · sceneTheme · cũ → UiTheme\n";
            if (camera != null)
            {
                changes += camera.name + " · Camera · backgroundColor · " + camera.backgroundColor + " → " + theme.CameraBackground + "\n";
                var cameraSettings = new SerializedObject(camera);
                cameraSettings.FindProperty("m_BackGroundColor").colorValue = theme.CameraBackground;
                cameraSettings.FindProperty("m_ClearFlags").intValue = (int)CameraClearFlags.SolidColor;
                cameraSettings.ApplyModifiedProperties();
            }
            foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                if (light.type != LightType.Directional || light.gameObject.scene != scene) { continue; }
                changes += light.name + " · Light/Transform · intensity/rotation · " + light.intensity + "/" + light.transform.eulerAngles + " → " + theme.LightIntensity + "/" + theme.LightAngles + "\n";
                Undo.RecordObject(light, "013 daylight"); Undo.RecordObject(light.transform, "013 daylight rotation");
                light.intensity = theme.LightIntensity; light.transform.eulerAngles = theme.LightAngles;
            }
            changes += "RenderSettings · ambientLight · " + RenderSettings.ambientLight + " → " + theme.Ambient + " (màu trong phiếu, chờ Claude duyệt sáng hơn)\n";
            RenderSettings.ambientLight = theme.Ambient;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.AppendAllText("docs/handoff/013-scene-changes.md", changes);
            Debug.Log("013: đã tạo cảnh1 mesh/1 material,5 prefab prop (tối đa12 instance), chuẩn hóa/collider tắt; padding0.1, ánh sáng theo UiTheme. Không sửa Project Settings. Chạy Run All và Play/Stats16.");
        }

        internal static GameObject BakeProp(string sourcePath, float size, UnityEngine.SceneManagement.Scene preview, float maxHeight = float.PositiveInfinity)
        {
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            string path = "Assets/_Project/Prefabs/Environment/" + name + ".prefab";
            UiThemeAssets.Backup(path);
            var root = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Mechanics/Kenney/" + sourcePath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { throw new InvalidOperationException("Model không có renderer: " + sourcePath); }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) { bounds.Encapsulate(renderer.bounds); }
            float fit = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            fit = Mathf.Min(fit, maxHeight / Mathf.Max(.0001f, bounds.size.y));
            model.transform.localScale *= fit;
            model.transform.localPosition = new Vector3(-bounds.center.x * fit, -bounds.min.y * fit, -bounds.center.z * fit);
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var assigned = filter.GetComponent<Renderer>().sharedMaterials;
                for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                {
                    var original = assigned[Mathf.Min(sub, assigned.Length - 1)];
                    var material = SharedPropMaterial(original);
                    if (!groups.TryGetValue(material, out var parts)) { parts = new List<CombineInstance>(); groups.Add(material, parts); }
                    parts.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub, transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                }
            }
            var allParts = new List<CombineInstance>();
            var materials = new List<Material>();
            var temporary = new List<Mesh>();
            foreach (var pair in groups)
            {
                var part = new Mesh { indexFormat = IndexFormat.UInt32 };
                part.CombineMeshes(pair.Value.ToArray(), true, true);
                temporary.Add(part); materials.Add(pair.Key);
                allParts.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
            }
            var mesh = new Mesh { name = name + " combined", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(allParts.ToArray(), false, false);
            string meshPath = Art + "/Meshes/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
            else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); AssetDatabase.SaveAssetIfDirty(saved); }
            foreach (var part in temporary) { UnityEngine.Object.DestroyImmediate(part); }
            UnityEngine.Object.DestroyImmediate(model);
            root.AddComponent<MeshFilter>().sharedMesh = saved;
            var output = root.AddComponent<MeshRenderer>(); output.sharedMaterials = materials.ToArray();
            output.shadowCastingMode = ShadowCastingMode.Off; output.receiveShadows = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("013 prop " + name + ": " + saved.vertexCount + " vertices, " + materials.Count + " submesh/material; không collider.");
            return prefab;
        }
        private static Material SharedPropMaterial(Material source)
        {
            if (source == null) { throw new InvalidOperationException("Model có material trống."); }
            var texture = source.mainTexture;
            string key = ColorUtility.ToHtmlStringRGBA(source.color) + "_" + (texture != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture)) : "solid");
            string path = Art + "/Materials/Prop_" + key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) { return material; }
            material = new Material(Shader.Find("Standard")); material.color = source.color;
            material.mainTexture = texture; material.enableInstancing = true; material.SetFloat("_Glossiness", 0);
            if (texture != null && texture.name == "net")
            {
                material.SetFloat("_Mode", 1); material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout"); material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
