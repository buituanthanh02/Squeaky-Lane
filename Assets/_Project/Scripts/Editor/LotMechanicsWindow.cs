using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    public sealed class LotMechanicsWindow : EditorWindow
    {
        private const string CatalogPath = "Assets/_Project/Config/MechanicCatalog.asset";
        private const string Art = "Assets/_Project/Art/Mechanics";
        private const string Prefabs = "Assets/_Project/Prefabs/Mechanics";
        private const string ConePath = Art + "/Kenney/cone.fbx";
        private const string ColormapPath = "Assets/_Project/Art/Cars/Kenney/Textures/colormap.png";
        [SerializeField] private MechanicCatalog catalog;
        [SerializeField] private VisualPalette palette;
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly List<Mesh> temporaryMeshes = new List<Mesh>();
        private string status = "";
        private MechanicCatalog.Design D => catalog.Settings;

        [MenuItem("Tools/Squeaky Lane/Lot Mechanics")]
        public static void Open() => GetWindow<LotMechanicsWindow>("011 · Bãi & cơ chế");
        private void OnEnable()
        {
            catalog = AssetDatabase.LoadAssetAtPath<MechanicCatalog>(CatalogPath);
            palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("P1 bước 1→4. Tạo catalog → nhập riêng cone.fbx → dựng prefab → tự gán catalog vào GameRoot. Số chỉnh ở Inspector của catalog; dựng lại sau khi chỉnh. Không sửa scene/FBX nguồn.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                catalog = (MechanicCatalog)EditorGUILayout.ObjectField("Mechanic Catalog", catalog, typeof(MechanicCatalog), false);
                palette = (VisualPalette)EditorGUILayout.ObjectField("Visual Palette", palette, typeof(VisualPalette), false);
                if (GUILayout.Button("1 · Tạo/chọn MechanicCatalog")) { Run(CreateCatalog); }
                if (GUILayout.Button("2 · Nhập riêng cone.fbx từ Downloads")) { Run(ImportCone); }
                if (GUILayout.Button("3 · Tạo prefab P1 còn thiếu")) { Run(() => Build(false)); }
                if (GUILayout.Button("4 · Cập nhật cổng tele xoay + chống che")) { Run(() => Build(true, true)); }
                if (GUILayout.Button("Dựng lại P1 theo catalog · sao lưu prefab trước")) { Run(() => Build(true)); }
                EditorGUILayout.HelpBox("Dựng lại sẽ thay hình trong prefab được sinh bởi tool; bản sao nằm ở Logs/011-prefab-backups. Không thay City/Racing ở bước này. Các prefab không có collider để chạm xuyên tới xe.", MessageType.None);
            }
            if (!string.IsNullOrEmpty(status)) { EditorGUILayout.HelpBox(status, MessageType.Info); }
        }
        private void CreateCatalog()
        {
            if (catalog == null)
            {
                catalog = CreateInstance<MechanicCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            Selection.activeObject = catalog;
            status = "Đã có catalog. Inspector chứa toàn bộ số/màu visual 011.";
        }
        private void ImportCone()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ConePath) != null)
            {
                status = "cone.fbx đã có trong Art/Mechanics/Kenney.";
                return;
            }
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads/kenney_car-kit/Models/FBX format");
            string source = EditorUtility.OpenFilePanel("Chọn cone.fbx của Kenney Car Kit", folder, "fbx");
            if (string.IsNullOrEmpty(source)) { return; }
            if (!string.Equals(Path.GetFileName(source), "cone.fbx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Bước này chỉ nhập cone.fbx, không nhập cả kit.");
            }
            EnsureFolder(Art + "/Kenney");
            if (File.Exists(ConePath)) { throw new InvalidOperationException("File cone.fbx đã tồn tại; chờ Unity import."); }
            File.Copy(source, ConePath);
            AssetDatabase.ImportAsset(ConePath, ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ConePath);
            status = "Đã nhập riêng cone.fbx. Dùng lại colormap Car Kit có sẵn từ 010 (CC0).";
        }
        private void Build(bool overwrite, bool correctionsOnly = false)
        {
            if (catalog == null || palette == null) { throw new InvalidOperationException("Tạo/gán catalog và VisualPalette trước."); }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ConePath) == null) { throw new InvalidOperationException("Nhập cone.fbx ở bước2 trước."); }
            EnsureFolder(Prefabs);
            EnsureFolder(Art + "/Materials");
            EnsureFolder(Art + "/Meshes");
            materials.Clear();
            var entries = new List<MechanicCatalog.Entry>();
            string backup = "Logs/011-prefab-backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            foreach (MechanicKind kind in Enum.GetValues(typeof(MechanicKind)))
            {
                int variants = kind == MechanicKind.ColorGate ? 6 : kind == MechanicKind.Portal ? D.portalColors.Length : 1;
                for (int variant = 0; variant < variants; variant++)
                {
                    string key = kind + "_" + variant;
                    string path = Prefabs + "/" + key + ".prefab";
                    bool selected = !correctionsOnly || kind == MechanicKind.Pylon || kind == MechanicKind.Portal ||
                        kind == MechanicKind.ColorGate || kind == MechanicKind.WaterGate;
                    if (selected && (overwrite || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null))
                    {
                        if (File.Exists(path))
                        {
                            Directory.CreateDirectory(backup);
                            File.Copy(path, backup + "/" + Path.GetFileName(path));
                        }
                        BuildPrefab(kind, variant, key, path);
                    }
                    entries.Add(new MechanicCatalog.Entry { kind = kind, variant = variant, prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) });
                }
            }
            var data = new SerializedObject(catalog);
            var list = data.FindProperty("entries");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                var row = list.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("kind").enumValueIndex = (int)entries[i].kind;
                row.FindPropertyRelative("variant").intValue = entries[i].variant;
                row.FindPropertyRelative("prefab").objectReferenceValue = entries[i].prefab;
            }
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(catalog);
            Selection.activeObject = catalog;
            status = correctionsOnly ? "Đã cập nhật11 prefab cổng/trụ, giữ catalog/GUID. Run All rồi kiểm màn29/30; backup: " + backup :
                "Đã nối " + entries.Count + " prefab P1. Gán Mechanic Catalog trên GameRoot ngoài Play; Run All rồi Play kiểm.";
        }
        private void BuildPrefab(MechanicKind kind, int variant, string key, string path)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject(key);
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var view = root.AddComponent<MechanicView>();
                view.Configure(kind);
                if (kind == MechanicKind.Surface)
                {
                    root.AddComponent<LotSurfaceView>();
                    root.GetComponent<MeshFilter>().sharedMesh = SaveMesh(LotSurfaceView.BuildMesh(new LevelDefBuilder(4, 4).Build(), D), key);
                    root.GetComponent<MeshRenderer>().sharedMaterials = new[] { Mat("Asphalt", D.asphalt), Mat("Markings", D.markings), Mat("Curb", D.curb) };
                }
                else if (kind == MechanicKind.Cone) { Cone(root.transform); }
                else if (kind == MechanicKind.TrafficLight) { Traffic(root.transform, view, key); }
                else if (kind == MechanicKind.Pylon) { Pylon(root.transform, view, key); }
                else
                {
                    if (kind == MechanicKind.ColorGate || kind == MechanicKind.WaterGate) { Gate(root.transform, kind == MechanicKind.WaterGate, variant); }
                    if (kind == MechanicKind.Mud) { Puddle(root.transform); }
                    if (kind == MechanicKind.Turntable) { Table(root.transform); }
                    if (kind == MechanicKind.Portal) { Portal(root.transform, variant); }
                    Bake(root.transform, key);
                    if (kind == MechanicKind.ColorGate || kind == MechanicKind.WaterGate || kind == MechanicKind.Portal)
                    {
                        ConfigureOcclusion(root, root.GetComponent<Renderer>());
                    }
                }
                if (!MechanicView.Usable(root, kind)) { throw new InvalidOperationException("Prefab sinh ra chưa hợp lệ: " + key); }
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) { throw new InvalidOperationException("Không lưu được " + path); }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var mesh in temporaryMeshes) { if (mesh != null) { DestroyImmediate(mesh); } }
                temporaryMeshes.Clear();
            }
        }
        private void Cone(Transform root)
        {
            var visual = Child(root, "Visual");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ConePath);
            PrefabUtility.InstantiatePrefab(source, visual);
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) { bounds.Encapsulate(renderer.bounds); }
            float s = D.coneHeight / bounds.size.y;
            visual.localScale = Vector3.one * s;
            visual.localPosition = new Vector3(-bounds.center.x * s, -bounds.min.y * s, -bounds.center.z * s);
            var material = Mat("Cone_Kenney", Color.white);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ColormapPath);
            if (texture == null) { throw new InvalidOperationException("Thiếu colormap Car Kit gốc từ010."); }
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            foreach (var renderer in renderers) { renderer.sharedMaterial = material; }
        }
        private void Gate(Transform root, bool water, int variant)
        {
            Color color = water ? Color.white : palette.Car((TileColor)variant);
            var frame = Mat(water ? "GateWhite" : "Gate_" + variant, color);
            float x = (D.gateWidth - D.gatePost) * .5f;
            float postHeight = water ? D.gateHeight - D.gateWidth * .5f : D.gateHeight;
            Box(root, "Cột trái", new Vector3(-x, postHeight * .5f, 0), new Vector3(D.gatePost, postHeight, D.gateDepth), frame);
            Box(root, "Cột phải", new Vector3(x, postHeight * .5f, 0), new Vector3(D.gatePost, postHeight, D.gateDepth), frame);
            if (water)
            {
                Ring(root, "Vòm nước tròn", new Vector3(0, postHeight, 0), D.gateWidth * .5f, D.gateWidth * .5f, D.gatePost, true, false, frame);
            }
            else
            {
                Box(root, "Xà ngang", new Vector3(0, D.gateHeight - D.gatePost * .5f, 0), new Vector3(D.gateWidth, D.gatePost, D.gateDepth), frame);
            }
            var curtainColor = water ? D.water : color;
            curtainColor.a = D.curtainAlpha;
            var curtain = Mat(water ? "CurtainWater" : "Curtain_" + variant, curtainColor);
            float inner = D.gateWidth - 2 * D.gatePost;
            float top = water ? postHeight : D.gateHeight - D.gatePost;
            for (int i = 0; i < D.curtainStripes; i++)
            {
                float stripe = inner / D.curtainStripes;
                Box(root, "Rèm sọc", new Vector3(-inner * .5f + (i + .5f) * stripe, (top + D.curtainBottom) * .5f, 0),
                    new Vector3(stripe * D.curtainFill, top - D.curtainBottom, D.curtainDepth), curtain);
            }
            if (water)
            {
                for (int i = 0; i < D.nozzles; i++)
                {
                    float angle = Mathf.Lerp(Mathf.PI * .25f, Mathf.PI * .75f, i / (float)(D.nozzles - 1));
                    float radius = D.gateWidth * .5f - D.gatePost * .5f;
                    Primitive(root, PrimitiveType.Cylinder, "Vòi nước", new Vector3(Mathf.Cos(angle) * radius, postHeight + Mathf.Sin(angle) * radius - D.nozzleSize * .5f, 0),
                        new Vector3(D.nozzleSize, D.nozzleSize * .5f, D.nozzleSize), Mat("Nozzle", D.dark));
                }
            }
        }
        private void Puddle(Transform root)
        {
            int n = D.roundSegments;
            var vertices = new List<Vector3> { Vector3.up * D.mudHeight };
            var indices = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float angle = i * Mathf.PI * 2 / n;
                float radius = D.mudRadius * (1 + D.puddleIrregularity * Mathf.Sin(i * D.puddleFrequency));
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, D.mudHeight, Mathf.Sin(angle) * radius));
                indices.Add(0); indices.Add((i + 1) % n + 1); indices.Add(i + 1);
            }
            // Thành đĩa chạm mặt bãi; không để mặt bùn lơ lửng.
            for (int i = 0; i < n; i++)
            {
                Vector3 rim = vertices[i + 1];
                vertices.Add(new Vector3(rim.x, 0, rim.z));
                int next = (i + 1) % n;
                indices.AddRange(new[] { i + 1, next + 1, n + 1 + i, next + 1, n + 1 + next, n + 1 + i });
            }
            MeshPart(root, "Vũng bùn méo", vertices, indices, Mat("Mud", D.mud, .55f));
        }
        private void Table(Transform root)
        {
            Primitive(root, PrimitiveType.Cylinder, "Đĩa bàn xoay", Vector3.up * D.tableHeight * .5f,
                new Vector3(D.tableDiameter, D.tableHeight * .5f, D.tableDiameter), Mat("Table", D.table));
            float radius = D.tableDiameter * D.arrowRadiusFraction;
            Ring(root, "Vòng chỉ xoay", Vector3.up * (D.tableHeight + D.overlayLift), radius, radius, D.arrowWidth, false, true, Mat("White", Color.white));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2 / 3;
                Vector3 center = new Vector3(Mathf.Cos(angle) * radius, D.tableHeight + 2 * D.overlayLift, Mathf.Sin(angle) * radius);
                // Core/TableRule xoay thuận kim đồng hồ: phía +X đi về -Z.
                Vector3 tangent = new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle));
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                MeshPart(root, "Mũi tên xoay", new List<Vector3> { center + tangent * D.arrowWidth * D.arrowTipLength, center + radial * D.arrowWidth * D.arrowWingWidth, center - radial * D.arrowWidth * D.arrowWingWidth },
                    new List<int> { 0, 1, 2 }, Mat("White", Color.white), true);
            }
        }
        private void Traffic(Transform root, MechanicView view, string key)
        {
            var body = Child(root, "Thân đèn");
            Box(body, "Cột", D.poleOffset + Vector3.up * D.poleHeight * .5f, new Vector3(D.poleWidth, D.poleHeight, D.poleWidth), Mat("Dark", D.dark));
            Box(body, "Hộp đèn", D.poleOffset + Vector3.up * (D.poleHeight + D.lampBoxHeight * .5f),
                new Vector3(D.lampBoxWidth, D.lampBoxHeight, D.lampBoxWidth), Mat("Dark", D.dark));
            Bake(body, key + "_Body");
            var red = Child(root, "Đỏ + vạch dừng");
            var green = Child(root, "Xanh + vạch dừng");
            LightState(red, true); LightState(green, false);
            Bake(red, key + "_Red"); Bake(green, key + "_Green");
            view.Configure(MechanicKind.TrafficLight, null, red.gameObject, green.gameObject);
            view.SetRed(false);
        }
        private void LightState(Transform root, bool red)
        {
            var material = Mat(red ? "LightRed" : "LightGreen", red ? D.red : D.green, 0, true);
            float lampY = D.poleHeight + D.lampBoxHeight * (red ? .75f : .25f);
            Primitive(root, PrimitiveType.Sphere, "Đèn sáng", D.poleOffset + new Vector3(0, lampY, -D.lampBoxWidth * .5f), Vector3.one * D.lampDiameter, material);
            float span = D.stopSpan, line = D.stopWidth;
            foreach (int sign in new[] { -1, 1 })
            {
                Box(root, "Vạch dừng ngang", new Vector3(0, D.overlayLift, sign * (span - line) * .5f), new Vector3(span, D.overlayLift, line), material);
                Box(root, "Vạch dừng dọc", new Vector3(sign * (span - line) * .5f, D.overlayLift, 0), new Vector3(line, D.overlayLift, span - 2 * line), material);
            }
        }
        private void Pylon(Transform root, MechanicView view, string key)
        {
            var body = Child(root, "Thân trụ");
            Primitive(body, PrimitiveType.Cylinder, "Đĩa thấp", Vector3.up * D.pylonDiscHeight * .5f,
                new Vector3(D.pylonRingDiameter, D.pylonDiscHeight * .5f, D.pylonRingDiameter), Mat("Dark", D.dark));
            Ring(body, "Vòng tròn vàng", Vector3.up * (D.pylonDiscHeight + D.overlayLift),
                D.pylonRingDiameter * .5f, D.pylonRingDiameter * .5f, D.pylonRingWidth, false, true, Mat("Pylon", D.pylon));
            Bake(body, key + "_Body");
            var entrances = Child(root, "Cổng tele xoay A-B");
            var door = Child(entrances, "Vòng cửa dựng đứng");
            DoorRing(door);
            Bake(door, key + "_Door");
            ConfigureOcclusion(root.gameObject, door.GetComponent<Renderer>());
            var arrows = Child(entrances, "Hướng vào hai mặt");
            foreach (int sign in new[] { 1, -1 })
            {
                // Turns=0: miệng A ở +Z (Bắc), B ở -Z (Nam), khớp PylonRule.PylonFaces.
                float y = D.pylonDiscHeight + D.overlayLift * 2;
                float back = sign * D.pylonEntryOffset;
                float tip = sign * (D.pylonEntryOffset - D.pylonArrowLength);
                MeshPart(arrows, "Hướng xe vào tâm", new List<Vector3>
                {
                    new Vector3(0, y, tip), new Vector3(-D.pylonArrowWidth * .5f, y, back), new Vector3(D.pylonArrowWidth * .5f, y, back)
                }, new List<int> { 0, 1, 2 }, Mat("White", Color.white), true);
            }
            Bake(arrows, key + "_Arm");
            view.Configure(MechanicKind.Pylon, entrances);
        }
        private void Portal(Transform root, int variant)
        {
            var frame = Mat("Dark", D.dark);
            float x = D.portalWidth * .5f;
            Box(root, "Khung trái", new Vector3(-x, D.portalHeight * .5f, 0), new Vector3(D.portalRim, D.portalHeight, D.gateDepth), frame);
            Box(root, "Khung phải", new Vector3(x, D.portalHeight * .5f, 0), new Vector3(D.portalRim, D.portalHeight, D.gateDepth), frame);
            Box(root, "Khung trên", new Vector3(0, D.portalHeight - D.portalRim * .5f, 0), new Vector3(D.portalWidth, D.portalRim, D.gateDepth), frame);
            Ring(root, "Vòng sáng cặp " + (variant + 1), Vector3.up * D.portalHeight * .5f, x - D.portalRim,
                D.portalHeight * .5f - D.portalRim, D.portalRim, false, false, Mat("Portal_" + variant, D.portalColors[variant], 0, true));
        }
        private void DoorRing(Transform root)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var center = Vector3.up * (D.pylonDiscHeight + D.pylonDoorHeight * .5f);
            float rx = D.pylonEntryWidth * .5f, ry = D.pylonDoorHeight * .5f;
            Vector3 depth = Vector3.forward * D.pylonDoorDepth;
            for (int i = 0; i < D.roundSegments; i++)
            {
                float a = i * Mathf.PI * 2 / D.roundSegments, b = (i + 1) * Mathf.PI * 2 / D.roundSegments;
                Vector3 outerA = center + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, -D.pylonDoorDepth * .5f);
                Vector3 innerA = center + new Vector3(Mathf.Cos(a) * (rx - D.pylonRingWidth), Mathf.Sin(a) * (ry - D.pylonRingWidth), -D.pylonDoorDepth * .5f);
                Vector3 outerB = center + new Vector3(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry, -D.pylonDoorDepth * .5f);
                Vector3 innerB = center + new Vector3(Mathf.Cos(b) * (rx - D.pylonRingWidth), Mathf.Sin(b) * (ry - D.pylonRingWidth), -D.pylonDoorDepth * .5f);
                DoorQuad(vertices, indices, outerA, innerA, innerB, outerB);
                DoorQuad(vertices, indices, outerB + depth, innerB + depth, innerA + depth, outerA + depth);
                DoorQuad(vertices, indices, outerA, outerB, outerB + depth, outerA + depth);
                DoorQuad(vertices, indices, innerB, innerA, innerA + depth, innerB + depth);
            }
            MeshPart(root, "Cổng tele tròn hai mặt có độ dày", vertices, indices, Mat("PylonPortal", D.pylon, 0, true));
        }
        private static void DoorQuad(List<Vector3> vertices, List<int> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        private void Ring(Transform parent, string name, Vector3 center, float rx, float ry, float thickness, bool half, bool flat, Material material)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            int segments = D.roundSegments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * (half ? Mathf.PI : Mathf.PI * 2) / segments;
                Vector3 outer = new Vector3(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry, 0);
                Vector3 inner = new Vector3(Mathf.Cos(angle) * (rx - thickness), Mathf.Sin(angle) * (ry - thickness), 0);
                if (flat) { outer = new Vector3(outer.x, 0, outer.y); inner = new Vector3(inner.x, 0, inner.y); }
                vertices.Add(center + outer); vertices.Add(center + inner);
                if (i == segments) { continue; }
                int a = i * 2;
                indices.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
            }
            MeshPart(parent, name, vertices, indices, material, true);
        }
        private void MeshPart(Transform parent, string name, List<Vector3> vertices, List<int> indices, Material material, bool doubleSided = false)
        {
            if (doubleSided)
            {
                int count = vertices.Count, indexCount = indices.Count;
                vertices.AddRange(vertices.ToArray());
                for (int i = 0; i < indexCount; i += 3)
                {
                    indices.Add(indices[i] + count); indices.Add(indices[i + 2] + count); indices.Add(indices[i + 1] + count);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            temporaryMeshes.Add(mesh);
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
            => Primitive(parent, PrimitiveType.Cube, name, position, size, material);
        private static void Primitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }
        private Material Mat(string key, Color color, float gloss = 0, bool emissive = false)
        {
            if (materials.TryGetValue(key, out var material)) { return material; }
            string path = Art + "/Materials/" + key + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", gloss);
            material.enableInstancing = true;
            bool transparent = color.a < .999f;
            material.SetFloat("_Mode", transparent ? 2 : 0);
            material.SetInt("_SrcBlend", transparent ? (int)UnityEngine.Rendering.BlendMode.SrcAlpha : (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", transparent ? (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", transparent ? 0 : 1);
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (transparent) { material.EnableKeyword("_ALPHABLEND_ON"); } else { material.DisableKeyword("_ALPHABLEND_ON"); }
            material.renderQueue = transparent ? (int)UnityEngine.Rendering.RenderQueue.Transparent : -1;
            if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color); }
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            materials.Add(key, material);
            return material;
        }
        private void ConfigureOcclusion(GameObject root, Renderer renderer)
        {
            var solid = renderer.sharedMaterials;
            var faded = new Material[solid.Length];
            for (int i = 0; i < faded.Length; i++) { faded[i] = FadeVariant(solid[i]); }
            root.AddComponent<MechanicOcclusion>().Configure(renderer, solid, faded);
        }
        private Material FadeVariant(Material source)
        {
            string key = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source)) + "_Occluded";
            if (materials.TryGetValue(key, out var cached)) { return cached; }
            string path = Art + "/Materials/" + key + ".mat";
            var faded = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (faded == null)
            {
                faded = new Material(source);
                AssetDatabase.CreateAsset(faded, path);
            }
            else { faded.CopyPropertiesFromMaterial(source); }
            var color = source.color;
            color.a = Mathf.Min(color.a, D.occludedAlpha);
            faded.color = color;
            faded.SetFloat("_Mode", 2);
            faded.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            faded.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            faded.SetInt("_ZWrite", 0);
            faded.SetOverrideTag("RenderType", "Transparent");
            faded.DisableKeyword("_ALPHATEST_ON"); faded.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            faded.EnableKeyword("_ALPHABLEND_ON");
            faded.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            faded.enableInstancing = true;
            EditorUtility.SetDirty(faded);
            AssetDatabase.SaveAssetIfDirty(faded);
            materials.Add(key, faded);
            return faded;
        }
        private void Bake(Transform group, string key)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in group.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                var renderer = filter.GetComponent<Renderer>();
                var assigned = renderer.sharedMaterials;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var material = assigned[Mathf.Min(sub, assigned.Length - 1)];
                    if (!groups.TryGetValue(material, out var parts)) { parts = new List<CombineInstance>(); groups.Add(material, parts); }
                    parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = group.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                }
            }
            var combined = new List<CombineInstance>();
            var assignedMaterials = new List<Material>();
            foreach (var pair in groups)
            {
                var mesh = new Mesh();
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                temporaryMeshes.Add(mesh);
                combined.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
                assignedMaterials.Add(pair.Key);
            }
            var result = new Mesh { name = key };
            result.CombineMeshes(combined.ToArray(), false, false);
            var saved = SaveMesh(result, key);
            while (group.childCount > 0) { DestroyImmediate(group.GetChild(0).gameObject); }
            group.gameObject.AddComponent<MeshFilter>().sharedMesh = saved;
            var output = group.gameObject.AddComponent<MeshRenderer>();
            output.sharedMaterials = assignedMaterials.ToArray();
            output.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static Mesh SaveMesh(Mesh mesh, string key)
        {
            string path = Art + "/Meshes/" + key + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, saved);
            DestroyImmediate(mesh);
            AssetDatabase.SaveAssetIfDirty(saved);
            return saved;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception error) { status = error.Message; Debug.LogException(error); }
        }
    }
}
