using System;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public sealed class KenneyCarsWindow : EditorWindow
    {
        private const string Art = "Assets/_Project/Art/Cars/Kenney";
        private const string Prefabs = "Assets/_Project/Prefabs/Cars";
        private const string CatalogPath = "Assets/_Project/Config/CarCatalog.asset";
        private const float TruckBedCutSourceZ = -.30f;
        [Serializable] private sealed class Source
        {
            public TileColor color;
            public GameObject model;
            public float yaw = 90;
        }
        [SerializeField] private Texture2D colormap;
        [SerializeField] private VisualPalette palette;
        [SerializeField] private GameObject truckModel;
        [SerializeField] private float truckYaw = 90;
        // BoardView đặt root xe tại Y=0.32; mặt bãi Y=-0.01 nên đáy model ở local Y=-0.33.
        [SerializeField] private float gap = .18f, width = .78f, bottom = -.33f;
        [SerializeField] private Source[] sources =
        {
            new Source { color = TileColor.Pink }, new Source { color = TileColor.Blue },
            new Source { color = TileColor.Yellow }, new Source { color = TileColor.Green },
            new Source { color = TileColor.Orange }, new Source { color = TileColor.Purple }
        };
        private string status = "";
        private Vector2 scroll;

        [MenuItem("Tools/Squeaky Lane/Kenney Cars")]
        public static void Open() => GetWindow<KenneyCarsWindow>("010 · Xe Kenney");

        private void OnEnable()
        {
            if (palette == null) palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            if (colormap == null) colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/colormap.png");
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Thoát Play trước khi tạo asset/prefab.", MessageType.Info);
                return;
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("1. Gán texture gốc + palette, tạo 6 bộ màu. 2. Gán FBX từng màu + xe tải. 3. Tạo từng prefab, mở kiểm +X/pivot/cỡ. Không sửa scene hay model nguồn.", MessageType.Info);
            colormap = (Texture2D)EditorGUILayout.ObjectField("Colormap gốc", colormap, typeof(Texture2D), false);
            palette = (VisualPalette)EditorGUILayout.ObjectField("Visual Palette", palette, typeof(VisualPalette), false);
            if (GUILayout.Button("1 · Tạo/cập nhật 6 bộ màu")) Run(CreateColors);
            EditorGUILayout.Space();
            gap = EditorGUILayout.Slider("Khe dọc thân (ô)", gap, .05f, .4f);
            width = EditorGUILayout.Slider("Bề ngang (ô)", width, .6f, .9f);
            bottom = EditorGUILayout.FloatField("Đáy model so với root", bottom);
            truckModel = (GameObject)EditorGUILayout.ObjectField("FBX xe tải", truckModel, typeof(GameObject), false);
            truckYaw = EditorGUILayout.FloatField("Yaw model tải (+Z → +X: 90)", truckYaw);
            if (GUILayout.Button("Sửa 6 tải đã có · nối dài riêng thùng"))
            {
                Run(UpdateTruckWrappers);
            }
            foreach (var source in sources)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(source.color.ToString(), EditorStyles.boldLabel);
                source.model = (GameObject)EditorGUILayout.ObjectField("FBX xe thường", source.model, typeof(GameObject), false);
                source.yaw = EditorGUILayout.FloatField("Yaw model con", source.yaw);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Tạo prefab thường")) Run(() => CreateWrapper(source.color, source.model, source.yaw, false));
                    if (GUILayout.Button("Tạo prefab tải")) Run(() => CreateWrapper(source.color, truckModel, truckYaw, true));
                }
            }
            if (GUILayout.Button("Chọn CarCatalog")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<CarCatalog>(CatalogPath);
            EditorGUILayout.HelpBox("Prefab đã có sẽ không bị ghi đè: mở Prefab Mode để chỉnh model con. Root giữ rotation 0, scale 1. Nút tạo prefab cũng cập nhật catalog.", MessageType.None);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        internal static Color32[] RecolorBody(Color32[] original, int textureWidth, int textureHeight, Color32 color)
        {
            if (textureWidth <= 0 || textureHeight <= 0 || textureWidth % 8 != 0 || textureHeight % 4 != 0 || original.Length != textureWidth * textureHeight)
            {
                throw new ArgumentException("Colormap Car Kit 3.1 cần lưới 8 cột × 4 hàng.");
            }
            var result = (Color32[])original.Clone();
            // UV Car Kit 3.1: 4 ô thân khác màu; kính/bánh/đèn ở các ô khác.
            // Tọa độ pixel của Unity đi từ dưới lên. Giữ tất cả ô còn lại nguyên vẹn.
            int cellWidth = textureWidth / 8, cellHeight = textureHeight / 4;
            foreach (int column in new[] { 3, 4, 6, 7 })
            {
                for (int y = 2 * cellHeight; y < 3 * cellHeight; y++)
                {
                    for (int x = column * cellWidth; x < (column + 1) * cellWidth; x++)
                    {
                        result[y * textureWidth + x] = color;
                    }
                }
            }
            return result;
        }

        internal static Bounds FitVisualToFootprint(Transform visual, Bounds bounds, int length, float gap, float width, float bottom)
        {
            float s = Mathf.Min((length - gap) / bounds.size.x, width / bounds.size.z);
            visual.localScale = Vector3.one * s;
            visual.localPosition = new Vector3(-bounds.center.x * s, bottom - bounds.min.y * s, -bounds.center.z * s);
            float height = bounds.size.y * s;
            // Model giữ tỷ lệ gốc; collider vẫn phủ footprint lưới, kể cả khi model ngắn hơn.
            return new Bounds(new Vector3(0, bottom + height * .5f, 0), new Vector3(length - gap, height, width));
        }

        private void CreateColors()
        {
            RequirePalette();
            string path = AssetDatabase.GetAssetPath(colormap);
            if (colormap == null || !path.StartsWith(Art + "/", StringComparison.Ordinal) || !path.EndsWith("/colormap.png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Gán colormap.png gốc đã chuyển vào Art/Cars/Kenney/Textures bằng cửa sổ Project.");
            }
            EnsureFolder(Art + "/Textures");
            EnsureFolder(Art + "/Materials");
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Không đọc được PNG gốc.");
                var pixels = source.GetPixels32();
                foreach (var item in sources)
                {
                    var target = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                    try
                    {
                        target.SetPixels32(RecolorBody(pixels, source.width, source.height, palette.Car(item.color)));
                        target.Apply();
                        string texturePath = Art + "/Textures/colormap_" + Key(item.color) + ".png";
                        File.WriteAllBytes(texturePath, target.EncodeToPNG());
                        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                        importer.sRGBTexture = true;
                        importer.mipmapEnabled = false;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.wrapMode = TextureWrapMode.Clamp;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.SaveAndReimport();
                        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(item.color));
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Standard"));
                            AssetDatabase.CreateAsset(material, MaterialPath(item.color));
                        }
                        material.color = Color.white;
                        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                        material.enableInstancing = true;
                        EditorUtility.SetDirty(material);
                        AssetDatabase.SaveAssetIfDirty(material);
                    }
                    finally { DestroyImmediate(target); }
                }
            }
            finally { DestroyImmediate(source); }
            UpdateCatalog();
            status = "Đã tạo 6 colormap + 6 material; màu thân lấy từ VisualPalette, không tint kính/bánh. Chưa tạo prefab.";
        }

        private void CreateWrapper(TileColor color, GameObject source, float yaw, bool truck)
        {
            RequirePalette();
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (source == null || !sourcePath.StartsWith(Art + "/", StringComparison.Ordinal) || !sourcePath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Gán FBX đã chuyển vào Art/Cars/Kenney qua cửa sổ Project.");
            }
            bool isTruckModel = string.Equals(Path.GetFileName(sourcePath), "truck.fbx", StringComparison.OrdinalIgnoreCase);
            if (truck != isTruckModel)
            {
                throw new InvalidOperationException(truck ? "Prefab tải phải dùng truck.fbx." : "truck.fbx chỉ dùng cho prefab tải, không dùng cho xe thường.");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(color));
            if (material == null) throw new InvalidOperationException("Tạo 6 bộ màu trước.");
            string path = PrefabPath(color, truck);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                throw new InvalidOperationException("Prefab đã có. Mở Prefab Mode để chỉnh; tool không ghi đè công sức chỉnh tay: " + path);
            }
            EnsureFolder(Prefabs);
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Car " + color + (truck ? " Truck" : ""));
            SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                var visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, visual);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                model.transform.localScale = Vector3.one;
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("FBX không có renderer.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterials.Length != 1) throw new InvalidOperationException("FBX phải dùng một colormap material mỗi renderer.");
                    bounds.Encapsulate(renderer.bounds);
                    renderer.sharedMaterial = material;
                }
                if (bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) throw new InvalidOperationException("Bounds model không hợp lệ.");
                int length = truck ? 3 : 2;
                var footprint = FitVisualToFootprint(visual, bounds, length, gap, width, bottom);
                if (truck)
                {
                    SaveTruckBody(visual, LengthenTruckBed(visual, footprint.size.x));
                }
                var collider = root.AddComponent<BoxCollider>();
                collider.size = footprint.size;
                collider.center = footprint.center;
                var mud = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mud.name = "Bùn bám";
                mud.transform.SetParent(root.transform, false);
                mud.transform.localPosition = new Vector3(0, footprint.max.y + .025f, 0);
                mud.transform.localScale = new Vector3((truck ? footprint.size.x : bounds.size.x * visual.localScale.x) * .6f, .05f, .55f);
                DestroyImmediate(mud.GetComponent<Collider>());
                var dirt = mud.GetComponent<Renderer>();
                dirt.sharedMaterial = palette.Material;
                mud.SetActive(false); // CarView bật và tô bùn khi state có bùn.
                root.AddComponent<CarView>().ConfigurePrefab(visual, dirt, length);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            UpdateCatalog();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            status = "Đã tạo " + path + ". Mở prefab kiểm root 0/1, mũi xe hướng +X và khoảng hở; bù thêm ở model con nếu cần.";
        }

        internal static Mesh LengthenTruckBed(Transform visual, float targetLength)
        {
            Transform model = visual.GetChild(0);
            if (Mathf.Abs(Mathf.DeltaAngle(model.localEulerAngles.y, 90)) > .01f)
            {
                throw new InvalidOperationException("Nối thùng truck.fbx cần yaw 90 để mũi hướng +X.");
            }
            MeshFilter body = null;
            var wheels = new System.Collections.Generic.List<MeshFilter>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.name == "body")
                {
                    body = filter;
                }
                else if (filter.name.StartsWith("wheel-", StringComparison.Ordinal))
                {
                    wheels.Add(filter);
                }
                else
                {
                    throw new InvalidOperationException("truck.fbx có phần mesh chưa hỗ trợ: " + filter.name);
                }
            }
            if (body == null || wheels.Count != 4)
            {
                throw new InvalidOperationException("Cần thân body và đủ 4 bánh của truck.fbx Car Kit 3.1.");
            }
            Transform root = visual.parent;
            var bodyToRoot = root.worldToLocalMatrix * body.transform.localToWorldMatrix;
            var bounds = body.GetComponent<Renderer>().bounds;
            float extra = targetLength - bounds.size.x;
            // Z=-0.30 của truck.fbx nằm sau vách cabin (-0.25), trước hết vòm bánh sau (-0.37).
            // Cắt tam giác tại đây rồi chèn đoạn phẳng, không kéo méo cabin/vòm bánh/thành đuôi.
            float cut = root.InverseTransformPoint(model.TransformPoint(new Vector3(0, 0, TruckBedCutSourceZ))).x;
            var mesh = KenneyTruckMesh.ExtendBed(body.sharedMesh, bodyToRoot, cut, extra);
            body.sharedMesh = mesh;
            foreach (var wheel in wheels)
            {
                float x = root.InverseTransformPoint(wheel.GetComponent<Renderer>().bounds.center).x;
                Vector3 shift = root.TransformVector(new Vector3(x < cut ? -extra * .5f : extra * .5f, 0, 0));
                wheel.transform.localPosition += wheel.transform.parent.InverseTransformVector(shift);
            }
            return mesh;
        }

        private static void SaveTruckBody(Transform visual, Mesh mesh)
        {
            EnsureFolder(Art + "/Meshes");
            float sourceLength = mesh.bounds.size.z; // FBX thân dài theo +Z; Visual giữ scale đều.
            string path = Art + "/Meshes/TruckBody_Bed1_" + sourceLength.ToString("F6", CultureInfo.InvariantCulture) + ".asset";
            var shared = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (shared == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return;
            }
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == mesh)
                {
                    filter.sharedMesh = shared;
                }
            }
            DestroyImmediate(mesh);
        }

        private void UpdateTruckWrappers()
        {
            RequirePalette();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/truck.fbx");
            if (source == null)
            {
                throw new InvalidOperationException("Không thấy Art/Cars/Kenney/truck.fbx.");
            }
            // Giữ GUID/root và tham chiếu catalog; chỉ dựng lại Visual trong 6 wrapper tải.
            string backup = "Logs/010-truck-backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            foreach (var item in sources)
            {
                string path = PrefabPath(item.color, true);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!CarView.Usable(prefab, 3))
                {
                    throw new InvalidOperationException("Prefab tải chưa hợp lệ: " + path);
                }
                if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(item.color)) == null)
                {
                    throw new InvalidOperationException("Thiếu material tải " + item.color);
                }
                File.Copy(path, backup + "/" + Path.GetFileName(path));
            }
            foreach (var item in sources)
            {
                string path = PrefabPath(item.color, true);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view = root.GetComponent<CarView>();
                    var data = new SerializedObject(view);
                    var visual = (Transform)data.FindProperty("modelRoot").objectReferenceValue;
                    var dirt = (Renderer)data.FindProperty("mudOverlay").objectReferenceValue;
                    while (visual.childCount > 0)
                    {
                        DestroyImmediate(visual.GetChild(0).gameObject);
                    }
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                    visual.localScale = Vector3.one;
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source, visual);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    model.transform.localScale = Vector3.one;
                    var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(item.color));
                    if (material == null)
                    {
                        throw new InvalidOperationException("Thiếu material tải " + item.color);
                    }
                    var renderers = visual.GetComponentsInChildren<Renderer>(true);
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers)
                    {
                        bounds.Encapsulate(renderer.bounds);
                        renderer.sharedMaterial = material;
                    }
                    var footprint = FitVisualToFootprint(visual, bounds, 3, gap, width, bottom);
                    SaveTruckBody(visual, LengthenTruckBed(visual, footprint.size.x));
                    var collider = root.GetComponent<BoxCollider>();
                    collider.center = footprint.center;
                    collider.size = footprint.size;
                    dirt.transform.localPosition = new Vector3(0, footprint.max.y + .025f, 0);
                    dirt.transform.localScale = new Vector3(footprint.size.x * .6f, .05f, .55f);
                    view.ConfigurePrefab(visual, dirt, 3);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            status = "Đã nối thùng 6 xe tải, giữ cabin/bánh, root/GUID/catalog. Bản sao prefab cũ: " + backup;
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(TileColor.Blue, true));
        }

        private void UpdateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = CreateInstance<CarCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("entries");
            entries.arraySize = sources.Length;
            for (int i = 0; i < sources.Length; i++)
            {
                TileColor color = sources[i].color;
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("color").enumValueIndex = (int)color;
                entry.FindPropertyRelative("carPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(color, false));
                entry.FindPropertyRelative("truckPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(color, true));
                entry.FindPropertyRelative("material").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(color));
            }
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        private void RequirePalette()
        {
            if (palette == null || palette.Material == null) throw new InvalidOperationException("Gán VisualPalette có material cho bùn/dự phòng.");
        }
        private static string Key(TileColor color) => color.ToString().ToLowerInvariant();
        private static string MaterialPath(TileColor color) => Art + "/Materials/Car_" + Key(color) + ".mat";
        private static string PrefabPath(TileColor color, bool truck) => Prefabs + "/Car_" + Key(color) + (truck ? "_truck" : "") + ".prefab";
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception error) { status = error.Message; Debug.LogException(error); }
        }
    }
}
