using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public static class CoreFeelTools
    {
        private const string Art = "Assets/_Project/Art/VFX/Kenney";
        private const string PrefabPath = "Assets/_Project/Prefabs/CoreFeel.prefab";
        private const string RootPath = "Assets/_Project/Prefabs/GameRoot.prefab";

        [MenuItem("Tools/Squeaky Lane/Core Feel/Setup Assets and GameRoot")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi chạy Core Feel.");
            }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                throw new InvalidOperationException("Đóng Prefab Mode trước khi chạy Core Feel để tránh ghi đè phần đang sửa.");
            }
            string source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "kenney_particle-pack");
            string[] textures = { "circle_01", "circle_05", "smoke_01", "star_05", "spark_05" };
            foreach (string texture in textures)
            {
                if (!File.Exists(Path.Combine(source, "PNG (Transparent)", texture + ".png")))
                {
                    throw new FileNotFoundException("Thiếu sprite Particle Pack đã duyệt: " + texture, source);
                }
            }
            var feel = AssetDatabase.LoadAssetAtPath<FeelConfig>("Assets/_Project/Config/FeelConfig.asset");
            var palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/_Project/Config/CarCatalog.asset");
            if (feel == null || palette == null || catalog == null)
            {
                throw new InvalidOperationException("Cần FeelConfig, VisualPalette và CarCatalog hiện có.");
            }
            var particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            var mudShader = Shader.Find("Standard");
            if (particleShader == null || mudShader == null)
            {
                throw new InvalidOperationException("Thiếu shader Built-in cho hạt/bùn.");
            }
            EnsureFolder(Art + "/Materials");
            string backup = "Logs/012-asset-backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            Backup("Assets/_Project/Config/FeelConfig.asset", backup);
            Backup(RootPath, backup);
            Backup(PrefabPath, backup);
            var materials = new Material[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                string path = Art + "/" + textures[i] + ".png";
                if (!File.Exists(path))
                {
                    File.Copy(Path.Combine(source, "PNG (Transparent)", textures[i] + ".png"), path);
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
                materials[i] = MaterialAt(Art + "/Materials/" + textures[i] + ".mat", particleShader, backup);
                materials[i].mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                // Shader Legacy nhân màu vertex với tint ×2; 0.5 là tint trung tính.
                materials[i].SetColor("_TintColor", new Color(.5f, .5f, .5f, .5f));
                EditorUtility.SetDirty(materials[i]);
            }
            string license = Art + "/License.txt";
            if (!File.Exists(license))
            {
                File.Copy(Path.Combine(source, "License.txt"), license);
                AssetDatabase.ImportAsset(license);
            }
            var mud = MaterialAt(Art + "/Materials/MudFade.mat", mudShader, backup);
            mud.SetFloat("_Mode", 2);
            mud.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mud.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mud.SetInt("_ZWrite", 0);
            mud.DisableKeyword("_ALPHATEST_ON");
            mud.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mud.EnableKeyword("_ALPHABLEND_ON");
            mud.SetOverrideTag("RenderType", "Transparent");
            mud.renderQueue = (int)RenderQueue.Transparent;
            mud.SetColor("_Color", palette.Mud);
            EditorUtility.SetDirty(mud);
            var data = new SerializedObject(feel);
            data.FindProperty("mudFadeMaterial").objectReferenceValue = mud;
            // Cho thấy đủ một nhịp pháo giấy trước khi bảng thắng bắt đầu fade.
            if (data.FindProperty("winDialogDelaySeconds").floatValue == 0)
            {
                data.FindProperty("winDialogDelaySeconds").floatValue = feel.Confetti.Lifetime;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feel);

            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("012 Core Feel");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                var effects = root.AddComponent<FeelEffects>();
                var jets = SystemAt(root.transform, "Nước", materials[0]);
                var bubbles = SystemAt(root.transform, "Bọt", materials[1]);
                var smoke = SystemAt(root.transform, "Hơi và bụi", materials[2]);
                var stars = SystemAt(root.transform, "Sao", materials[3]);
                var confetti = SystemAt(root.transform, "Pháo giấy", materials[4]);
                effects.ConfigurePrefab(jets, smoke, bubbles, stars, confetti);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            var gameRoot = PrefabUtility.LoadPrefabContents(RootPath);
            try
            {
                var setup = new SerializedObject(gameRoot.GetComponent<GameBootstrap>());
                setup.FindProperty("feelEffectsPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<FeelEffects>();
                setup.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(gameRoot, RootPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(gameRoot);
            }
            // Thêm component vào prefab; fallback khối vẫn tự thêm khi Initialize.
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Cars" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.GetComponent<CarView>() == null || asset.GetComponent<CarBody>() != null) { continue; }
                Backup(path, backup);
                var car = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    car.AddComponent<CarBody>();
                    PrefabUtility.SaveAsPrefabAsset(car, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(car);
                }
            }
            AssetDatabase.SaveAssets();
            Selection.activeObject = feel;
            Debug.Log("012: đã nhập 5 PNG + License CC0, tạo material/CoreFeel, nối GameRoot và thêm CarBody vào prefab xe. " +
                "Không sửa scene. WinDialogDelaySeconds: 0 → tuổi thọ pháo giấy nếu trước đó bằng 0. Backup: " + backup);
        }

        private static ParticleSystem SystemAt(Transform parent, string name, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var system = root.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return system;
        }

        private static Material MaterialAt(string path, Shader shader, string backup)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                Backup(path, backup);
                material.shader = shader;
            }
            return material;
        }
        private static void Backup(string path, string folder)
        {
            if (File.Exists(path)) { File.Copy(path, folder + "/" + Path.GetFileName(path), true); }
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
