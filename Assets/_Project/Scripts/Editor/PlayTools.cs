using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Profiling;
using SqueakyLane.Game;
using SqueakyLane.Presentation;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using Debug = UnityEngine.Debug;

namespace SqueakyLane.EditorTools
{
    public static class PlayTools
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Play.unity";
        private static bool measuring;

        [MenuItem("Tools/Squeaky Lane/Measure Search (Play)")]
        public static void MeasureSearch()
        {
            var bootstrap = UnityEngine.Object.FindObjectOfType<GameBootstrap>();
            if (!EditorApplication.isPlaying || bootstrap == null || bootstrap.Session == null)
            { Debug.Log("Measure Search: cần Play scene Play trước."); return; }
            if (measuring) return;
            bootstrap.StartCoroutine(Measure(bootstrap));
        }
        private static IEnumerator Measure(GameBootstrap bootstrap)
        {
            measuring = true;
            bool oldProfiler = UnityEngine.Profiling.Profiler.enabled, oldPause = bootstrap.DiagnosticsPaused;
            var recorder = default(ProfilerRecorder);
            var report = new StringBuilder();
            try
            {
                bootstrap.DiagnosticsPaused = true;
                // Đọc counter SAU khi frame được flush; bộ đo CurrentValue đồng bộ ở 007 không hoạt động ở máy này.
                UnityEngine.Profiling.Profiler.enabled = true;
                recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 16);
                yield return null; yield return null;
                var calibrationFrames = new long[4];
                var probe = new byte[1024 * 1024]; probe[0] = 1;
                long calibration = 0;
                // Coroutine có thể chạy trước lúc Profiler công bố frame vừa xong.
                // Theo dõi nhiều frame để không nhầm frame cũ ít byte với frame chứa mẫu 1 MiB.
                for (int frame = 0; frame < calibrationFrames.Length; frame++)
                {
                    yield return null;
                    calibrationFrames[frame] = recorder.Valid ? recorder.LastValue : 0;
                    calibration = Math.Max(calibration, calibrationFrames[frame]);
                }
                GC.KeepAlive(probe); probe = null;
                report.AppendLine("Unity=" + Application.unityVersion + "; Editor/Mono; services=HintProvider+StuckDetector; level=16");
                report.AppendLine("CPU=" + SystemInfo.processorType + "; RAM_MB=" + SystemInfo.systemMemorySize + "; calibration1MiBFrameBytes=" + calibration);
                report.AppendLine("calibrationFrames=" + string.Join(",", calibrationFrames));
                if (calibration < 1024 * 1024)
                {
                    report.AppendLine("ALLOCATION_UNAVAILABLE: counter chưa bắt được mẫu 1 MiB; không báo 0 byte là cấp phát thật. Bật Record trong Profiler và thử lại.");
                    yield break;
                }
                yield return null;
                long baseline = 0;
                for (int frame = 0; frame < 4; frame++) { yield return null; baseline += recorder.LastValue; }
                baseline /= 4;
                report.AppendLine("baselineFrameBytes=" + baseline + "; phép đo gồm mọi luồng/Editor/render; phần trừ nền chỉ là ước lượng, không phải peak RAM.");
                var level = LevelJsonLoader.Load(bootstrap.Configs.Level(16).text, "level_16");
                for (int sample = 0; sample < 3; sample++)
                {
                    var session = new GameSession(level); var timer = new LevelTimer(0, 0, 0, .5f, .2f);
                    using var stuck = new StuckDetector(session, bootstrap.Configs.Game.SearchMaxStates);
                    using var hint = new HintProvider(session, timer, 1, bootstrap.Configs.Game.SearchMaxStates);
                    hint.Request();
                    // Tách cấp phát nạp màn/tạo wrapper khỏi cửa sổ đo, kể cả counter công bố trễ.
                    for (int settle = 0; settle < 4; settle++) yield return null;
                    int frames = 0; long allocated = 0; double total = 0, peak = 0;
                    while (frames < 10000)
                    {
                        long start = Stopwatch.GetTimestamp();
                        var budget = SearchBudget.Start(bootstrap.Configs.Game.SearchMilliseconds);
                        hint.Poll(budget); stuck.Poll(budget);
                        double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                        total += ms; peak = Math.Max(peak, ms); frames++;
                        bool done = hint.HasFinished && stuck.Status != StuckStatus.Unknown;
                        yield return null; allocated += recorder.LastValue;
                        if (done) break;
                    }
                    // Thu cả phần counter có thể công bố sau khi search đã kết thúc.
                    // Đây là cửa sổ nhiều frame, báo riêng số frame thu và trừ nền tương ứng.
                    int captureFrames = frames;
                    for (int tail = 0; tail < 3; tail++) { yield return null; allocated += recorder.LastValue; captureFrames++; }
                    if (!hint.HasHint || stuck.Status != StuckStatus.Winnable)
                        throw new InvalidOperationException("Đo màn 16: tìm kiếm chưa kết luận thắng; không nhận số đo như đã qua kiểm.");
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "sample={0}; budgetMs={1:F3}; workFrames={2}; totalWorkMs={3:F3}; peakWorkFrameMs={4:F3}; rawWindowBytes={5}; estimatedSearchBytes={6}; stuckExplored={7}; captureFrames={8}",
                        sample + 1, bootstrap.Configs.Game.SearchMilliseconds, frames, total, peak, allocated, Math.Max(0, allocated - baseline * captureFrames), stuck.Explored, captureFrames));
                    yield return null;
                }
                report.AppendLine("Đây là tìm kiếm theo frame qua Game services, không phải BFS tối ưu hay số đo Android; một node có thể vượt ngân sách mềm.");
            }
            finally
            {
                if (recorder.Valid) recorder.Dispose(); UnityEngine.Profiling.Profiler.enabled = oldProfiler;
                if (bootstrap != null) bootstrap.DiagnosticsPaused = oldPause;
                measuring = false;
                string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs")); Directory.CreateDirectory(logs);
                File.WriteAllText(Path.Combine(logs, "008-search-performance.txt"), report.ToString(), new UTF8Encoding(false));
                Debug.Log("008 Search Measurement\n" + report);
            }
        }

        [MenuItem("Tools/Squeaky Lane/Sync Levels")]
        public static void SyncLevels()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play trước khi đồng bộ dữ liệu.");
            // Khi đã có kho thiết kế, không lấy golden ghi đè màn người làm sửa.
            if (LevelDesignIO.HasDesignSource) { LevelDesignIO.Export(); return; }
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/golden/levels"));
            var paths = Directory.GetFiles(source, "level_*.json"); Array.Sort(paths, StringComparer.Ordinal);
            if (paths.Length != 31) throw new InvalidOperationException("Cần đúng 31 màn chuẩn, hiện có " + paths.Length);
            var texts = new string[paths.Length]; var ids = new int[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                texts[i] = File.ReadAllText(paths[i]); LevelJsonLoader.Load(texts[i], paths[i]);
                ids[i] = int.Parse(Path.GetFileNameWithoutExtension(paths[i]).Substring(6));
            }
            Folder(Root + "/Resources/Levels");
            for (int i = 0; i < paths.Length; i++) WriteIfChanged(Root + "/Resources/Levels/" + Path.GetFileName(paths[i]), texts[i]);
            WriteIfChanged(Root + "/Resources/Levels/level_order.json", JsonUtility.ToJson(new LevelOrder { ids = ids }, true) + "\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Sync Levels: 31/31 JSON chuẩn và level_order; Unity sinh/giữ .meta.");
        }

        [MenuItem("Tools/Squeaky Lane/Setup Play")]
        public static void SetupPlay()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play trước khi tạo scene.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SyncLevels();
            Folder(Root + "/Config"); Folder(Root + "/Prefabs"); Folder(Root + "/Scenes");
            var game = Asset<GameConfig>(Root + "/Config/GameConfig.asset");
            var feel = Asset<FeelConfig>(Root + "/Config/FeelConfig.asset");
            var cameraConfig = Asset<CameraConfig>(Root + "/Config/CameraConfig.asset");
            var palette = Asset<VisualPalette>(Root + "/Config/VisualPalette.asset");
            string materialPath = Root + "/Config/BlockMaterial.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")); material.SetFloat("_Glossiness", .1f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Assign(palette, "material", material);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath); AddBuildScene();
                Debug.Log("Setup Play: mở scene đã có, không dựng đè nội dung."); return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.transform.SetPositionAndRotation(new Vector3(0, 20, -10), Quaternion.Euler(65, 0, 0));
            var lightObject = new GameObject("Directional Light", typeof(Light));
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1; light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            string prefabPath = Root + "/Prefabs/GameRoot.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                var template = new GameObject("GameRoot");
                var bootstrap = template.AddComponent<GameBootstrap>();
                var board = template.AddComponent<BoardView>(); var animator = template.AddComponent<ActionAnimator>();
                var input = template.AddComponent<InputController>(); var hud = template.AddComponent<DebugHud>();
                Assign(bootstrap, "gameConfig", game); Assign(bootstrap, "feelConfig", feel); Assign(bootstrap, "cameraConfig", cameraConfig); Assign(bootstrap, "palette", palette);
                Assign(bootstrap, "board", board); Assign(bootstrap, "animator", animator); Assign(bootstrap, "input", input); Assign(bootstrap, "hud", hud);
                prefab = PrefabUtility.SaveAsPrefabAsset(template, prefabPath);
                UnityEngine.Object.DestroyImmediate(template);
            }
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var instance = root.GetComponent<GameBootstrap>(); Assign(instance, "boardCamera", camera); Assign(instance, "canvas", canvas);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            EditorSceneManager.SaveScene(scene, ScenePath); AddBuildScene(); AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log("Setup Play: Play.unity có Camera, Light, GameRoot, EventSystem, Canvas; đã thêm Build Settings.");
        }
        private static void AddBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => s.path == ScenePath);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); else scenes[index].enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        private static void Assign(UnityEngine.Object target, string name, UnityEngine.Object value)
        { var serialized = new SerializedObject(target); serialized.FindProperty(name).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void WriteIfChanged(string path, string text) { if (!File.Exists(path) || File.ReadAllText(path) != text) File.WriteAllText(path, text, new System.Text.UTF8Encoding(false)); }
        [Serializable] private sealed class LevelOrder { public int[] ids; }
    }
}
