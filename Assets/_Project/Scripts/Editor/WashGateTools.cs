using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SqueakyLane.EditorTools
{
    public static class WashGateTools
    {
        private const string MeshFolder = "Assets/_Project/Art/VFX/Meshes";
        private const string MaterialFolder = "Assets/_Project/Art/VFX/Materials";
        private const string PrefabPath = "Assets/_Project/Prefabs/Mechanics/WashGate_0.prefab";
        private const string PreviewPath = "Logs/015-wash-gate-preview.png";
        private const float PadRadius = .46f;
        private const float PadEnd = 2f;
        private const float PadHeight = .012f;
        private const float RimHeight = .03f;
        private const float RimWidth = .035f;
        private const float FrontZ = .05f;
        private const float BackZ = .95f;
        private const float TunnelZ = .50f;
        private const float ArchY = .976f;
        private static readonly Color32 BubbleBlue = new Color32(189, 240, 255, 255);

        [MenuItem("Tools/Squeaky Lane/015 Toy/Create Wash Gate")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi tạo cổng rửa.");
            }

            var standard = Shader.Find("Standard");
            var previewShader = Shader.Find("Unlit/Color");
            if (standard == null || previewShader == null)
            {
                throw new InvalidOperationException("Thiếu shader Standard hoặc Unlit/Color.");
            }

            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var gate = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (gate == null)
                {
                    RequireFreePath(PrefabPath);
                    var root = BuildGate(scene, standard);
                    gate = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Object.DestroyImmediate(root);
                    if (gate == null)
                    {
                        throw new InvalidOperationException($"Lưu prefab thất bại: {PrefabPath}.");
                    }
                }

                // Giữ bản đã lưu để lần chạy sau không ghi đè chỉnh sửa trong Inspector.
                SavePreview(scene, gate, previewShader);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Debug.Log($"015-a: {PrefabPath} · {PreviewPath}");
        }

        private static GameObject BuildGate(Scene scene, Shader standard)
        {
            var pad = LoadMaterial("M_WashPad");
            var roller = LoadMaterial("M_WashRoller");
            var post = LoadMaterial("M_WashPost");
            var bubble = LoadMaterial("M_Bubble");
            var arch = GetOrCreateArchMaterial(standard);
            var padMesh = GetOrCreateMesh("WashGate_Pad", BuildPad);
            var rimMesh = GetOrCreateMesh("WashGate_Rim", BuildRim);
            var postMesh = GetOrCreateMesh("WashGate_Post", name => BuildLathe(name, .50f, .07f, .035f, 12));
            var archMesh = GetOrCreateMesh("WashGate_Arch", name => BuildLathe(name, .55f, .074f, .04f, 12));
            var rollerMesh = GetOrCreateMesh("WashGate_Roller", name => BuildLathe(name, .36f, .07f, .03f, 16));

            var root = NewObject(scene, null, "WashGate_0").transform;
            AddMesh(scene, root, "Pad", padMesh, pad);
            AddMesh(scene, root, "Pad Rim", rimMesh, post);
            AddFrame(scene, root, "Front", FrontZ, postMesh, archMesh, post, arch);
            AddFrame(scene, root, "Back", BackZ, postMesh, archMesh, post, arch);

            // Nghiêng root để X của Spin trùng Y. Hai chiều vuông góc trục được scale bằng nhau.
            var upright = Quaternion.Euler(0f, 0f, 90f);
            AddRoller(scene, root, "L", new Vector3(-.47f, .45f, TunnelZ), upright, Vector3.one, rollerMesh, roller);
            AddRoller(scene, root, "R", new Vector3(.47f, .45f, TunnelZ), upright, Vector3.one, rollerMesh, roller);
            AddRoller(scene, root, "Top", new Vector3(0f, .90f, TunnelZ), Quaternion.identity,
                new Vector3(.86f / .72f, .06f / .07f, .06f / .07f), rollerMesh, roller);

            AddSpray(scene, root, "Spray L", new Vector3(-.40f, .24f, TunnelZ),
                new Vector3(1f, .18f, 0f), bubble, 1501u);
            AddSpray(scene, root, "Spray R", new Vector3(.40f, .24f, TunnelZ),
                new Vector3(-1f, .18f, 0f), bubble, 1502u);
            AddSpray(scene, root, "Spray Top", new Vector3(0f, .84f, TunnelZ),
                Vector3.down, bubble, 1503u);
            var drip = AddSpray(scene, root, "Drip", new Vector3(0f, .90f, BackZ),
                Vector3.down, bubble, 1504u);
            var dripMain = drip.main;
            dripMain.maxParticles = 8;
            dripMain.startSpeed = new ParticleSystem.MinMaxCurve(.30f, .55f);
            dripMain.startSize = new ParticleSystem.MinMaxCurve(.035f, .065f);
            var dripEmission = drip.emission;
            dripEmission.rateOverTime = 10f;
            var dripShape = drip.shape;
            dripShape.angle = 8f;
            return root.gameObject;
        }

        private static void AddFrame(Scene scene, Transform root, string end, float z, Mesh postMesh, Mesh archMesh,
            Material post, Material arch)
        {
            var upright = Quaternion.Euler(0f, 0f, 90f);
            string side = end == "Front" ? "F" : "B";
            var left = AddMesh(scene, root, "Post " + side + "L", postMesh, post);
            var right = AddMesh(scene, root, "Post " + side + "R", postMesh, post);
            left.SetLocalPositionAndRotation(new Vector3(-.52f, .46f, z), upright);
            right.SetLocalPositionAndRotation(new Vector3(.52f, .46f, z), upright);
            var postScale = new Vector3(.92f, 1f, 1f);
            left.localScale = postScale;
            right.localScale = postScale;

            var beam = AddMesh(scene, root, "Arch " + end, archMesh, arch);
            beam.localPosition = new Vector3(0f, ArchY, z);
            if (end == "Front")
            {
                var badge = AddMesh(scene, beam, "Bubble Badge", postMesh, post);
                badge.SetLocalPositionAndRotation(new Vector3(0f, 0f, -.073f), Quaternion.Euler(0f, 90f, 0f));
                badge.localScale = new Vector3(.027f, .050f / .07f, .050f / .07f);
            }
        }

        private static void AddRoller(Scene scene, Transform root, string side, Vector3 position, Quaternion rotation,
            Vector3 spinScale, Mesh mesh, Material material)
        {
            var roller = NewObject(scene, root, "Roller " + side).transform;
            roller.SetLocalPositionAndRotation(position, rotation);
            AddMesh(scene, roller, "Spin", mesh, material).localScale = spinScale;
        }

        private static ParticleSystem AddSpray(Scene scene, Transform root, string name, Vector3 position, Vector3 direction,
            Material bubble, uint seed)
        {
            var item = NewObject(scene, root, name, typeof(ParticleSystem));
            item.transform.SetLocalPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.forward));
            var spray = item.GetComponent<ParticleSystem>();
            spray.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            spray.useAutoRandomSeed = false;
            spray.randomSeed = seed;

            // Cùng thông số với Spray N của WashStation_0.
            var main = spray.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .9f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 16;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.10f, .16f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .7f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, BubbleBlue);
            main.gravityModifier = .8f;

            var emission = spray.emission;
            emission.enabled = true;
            emission.rateOverTime = 24f;
            emission.rateOverDistance = 0f;
            emission.burstCount = 0;

            var shape = spray.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .016f;
            shape.angle = 25f;

            var color = spray.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .55f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);

            var trails = spray.trails;
            trails.enabled = false;
            var collision = spray.collision;
            collision.enabled = false;
            var subEmitters = spray.subEmitters;
            subEmitters.enabled = false;

            var renderer = item.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            SetRenderer(renderer, bubble);
            return spray;
        }

        private static Vector2[] PadOutline(float radius)
        {
            const int arcSegments = 16;
            var points = new Vector2[arcSegments + 3];
            points[0] = new Vector2(radius, 0f);
            points[1] = new Vector2(radius, PadEnd - PadRadius);
            for (int i = 1; i < arcSegments; i++)
            {
                float angle = i * Mathf.PI / arcSegments;
                points[i + 1] = new Vector2(radius * Mathf.Cos(angle), PadEnd - PadRadius + radius * Mathf.Sin(angle));
            }
            points[arcSegments + 1] = new Vector2(-radius, PadEnd - PadRadius);
            points[arcSegments + 2] = new Vector2(-radius, 0f);
            return points;
        }

        private static Mesh BuildPad(string name)
        {
            var mesh = new MeshBuilder();
            var outline = PadOutline(PadRadius);
            int center = mesh.Add(new Vector3(0f, PadHeight, 1f), new Vector2(.5f, .5f));
            for (int i = 0; i < outline.Length; i++)
            {
                var point = outline[i];
                mesh.Add(new Vector3(point.x, PadHeight, point.y),
                    new Vector2(point.x / (2f * PadRadius) + .5f, point.y / PadEnd));
            }
            for (int i = 0; i < outline.Length; i++)
            {
                mesh.Triangle(center, 1 + (i + 1) % outline.Length, 1 + i);
            }
            // Gờ vàng che hai cạnh dọc và đầu bo. Tại cửa vào chỉ còn mép thảm mỏng.
            float opening = PadRadius - RimWidth;
            mesh.Quad(new Vector3(-opening, 0f, 0f), new Vector3(-opening, PadHeight, 0f),
                new Vector3(opening, PadHeight, 0f), new Vector3(opening, 0f, 0f));
            return mesh.Build(name);
        }

        private static Mesh BuildRim(string name)
        {
            var mesh = new MeshBuilder();
            var outer = PadOutline(PadRadius);
            var inner = PadOutline(PadRadius - RimWidth);
            for (int i = 0; i < outer.Length; i++)
            {
                mesh.Add(new Vector3(outer[i].x, 0f, outer[i].y));
                mesh.Add(new Vector3(outer[i].x, RimHeight, outer[i].y));
                mesh.Add(new Vector3(inner[i].x, RimHeight, inner[i].y));
                mesh.Add(new Vector3(inner[i].x, PadHeight, inner[i].y));
            }
            for (int a = 0; a < 4 * (outer.Length - 1); a += 4)
            {
                int b = a + 4;
                mesh.Quad(a, a + 1, b + 1, b);
                mesh.Quad(a + 1, a + 2, b + 2, b + 1);
                mesh.Quad(a + 2, a + 3, b + 3, b + 2);
            }
            mesh.Quad(new Vector3(inner[0].x, 0f, 0f), new Vector3(inner[0].x, RimHeight, 0f),
                new Vector3(outer[0].x, RimHeight, 0f), new Vector3(outer[0].x, 0f, 0f));
            int last = inner.Length - 1;
            mesh.Quad(new Vector3(outer[last].x, 0f, 0f), new Vector3(outer[last].x, RimHeight, 0f),
                new Vector3(inner[last].x, RimHeight, 0f), new Vector3(inner[last].x, 0f, 0f));
            return mesh.Build(name);
        }

        private static Mesh BuildLathe(string name, float halfLength, float radius, float bevel, int segments)
        {
            var x = new[] { -halfLength, -halfLength + bevel * .34f, -halfLength + bevel,
                halfLength - bevel, halfLength - bevel * .34f, halfLength };
            var r = new[] { radius * .50f, radius * .86f, radius, radius, radius * .86f, radius * .50f };
            var mesh = new MeshBuilder();
            int stride = segments + 1;
            for (int ring = 0; ring < x.Length; ring++)
            {
                int before = Mathf.Max(0, ring - 1);
                int after = Mathf.Min(x.Length - 1, ring + 1);
                float slope = (r[after] - r[before]) / (x[after] - x[before]);
                for (int i = 0; i <= segments; i++)
                {
                    // Hai đỉnh tại đường nối có cùng vị trí và normal, với UV chạy hết một vòng V.
                    float angle = (i % segments) * (2f * Mathf.PI / segments);
                    float y = Mathf.Cos(angle);
                    float z = Mathf.Sin(angle);
                    mesh.Add(new Vector3(x[ring], r[ring] * y, r[ring] * z),
                        new Vector2((x[ring] / halfLength + 1f) * .5f, (float)i / segments),
                        new Vector3(-slope, y, z).normalized);
                }
            }
            for (int ring = 0; ring < x.Length - 1; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int a = ring * stride + i;
                    mesh.Quad(a, a + 1, a + stride + 1, a + stride);
                }
            }
            int end = (x.Length - 1) * stride;
            int left = mesh.Add(new Vector3(-halfLength, 0f, 0f), new Vector2(0f, .5f), Vector3.left);
            int right = mesh.Add(new Vector3(halfLength, 0f, 0f), new Vector2(1f, .5f), Vector3.right);
            for (int i = 0; i < segments; i++)
            {
                mesh.Triangle(left, i + 1, i);
                mesh.Triangle(right, end + i, end + i + 1);
            }
            return mesh.Build(name, true);
        }

        private static Material LoadMaterial(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
            if (material == null)
            {
                throw new InvalidOperationException($"Thiếu material dùng chung {name}. Chạy menu 014-c trước.");
            }
            return material;
        }

        private static Material GetOrCreateArchMaterial(Shader standard)
        {
            const string name = "M_WashGateArch";
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }
            RequireFreePath(path);
            material = new Material(standard) { name = name, color = new Color32(255, 244, 224, 255) };
            material.SetFloat("_Glossiness", .5f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh GetOrCreateMesh(string name, Func<string, Mesh> build)
        {
            string path = $"{MeshFolder}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }
            RequireFreePath(path);
            mesh = build(name);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void RequireFreePath(string path)
        {
            if (File.Exists(path))
            {
                throw new InvalidOperationException($"Đã có file khác tại {path}.");
            }
        }

        private static GameObject NewObject(Scene scene, Transform parent, string name, params Type[] components)
        {
            var item = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);
            SceneManager.MoveGameObjectToScene(item, scene);
            item.hideFlags = HideFlags.None;
            if (parent != null)
            {
                item.transform.SetParent(parent, false);
            }
            return item;
        }

        private static Transform AddMesh(Scene scene, Transform parent, string name, Mesh mesh, Material material)
        {
            var item = NewObject(scene, parent, name, typeof(MeshFilter), typeof(MeshRenderer));
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            SetRenderer(item.GetComponent<MeshRenderer>(), material);
            return item.transform;
        }

        private static void SetRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void SavePreview(Scene scene, GameObject gate, Shader unlit)
        {
            const int width = 800;
            const int height = 350;
            var cube = BuildPreviewCube();
            var asphalt = new Material(unlit) { color = new Color32(58, 63, 75, 255) };
            var carPaint = new Material(unlit) { color = new Color32(53, 195, 255, 255) };
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var preview = new Texture2D(width, 2 * height, TextureFormat.RGB24, false);
            var camera = NewObject(scene, null, "Wash Gate Camera", typeof(Camera)).GetComponent<Camera>();
            try
            {
                PrefabUtility.InstantiatePrefab(gate, scene);
                var road = AddMesh(scene, null, "Asphalt", cube, asphalt);
                road.localPosition = new Vector3(0f, -.045f, -1f);
                road.localScale = new Vector3(6f, .09f, 2f);
                var car = AddMesh(scene, null, "Car Clearance", cube, carPaint);
                car.localPosition = new Vector3(0f, .375f + PadHeight, TunnelZ);
                car.localScale = new Vector3(.78f, .75f, 1.6f);

                AddPreviewLight(scene, "Key Light", 1.15f, Quaternion.Euler(55f, -28f, 0f));
                AddPreviewLight(scene, "Fill Light", .60f, Quaternion.Euler(25f, 145f, 0f));
                camera.cameraType = CameraType.Preview;
                camera.scene = scene;
                camera.enabled = false;
                camera.orthographic = true;
                camera.aspect = (float)width / height;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(48, 52, 64, 255);
                camera.allowHDR = false;
                camera.targetTexture = target;

                RenderPanel(camera, target, preview, new Vector3(0f, .25f, .72f),
                    Quaternion.Euler(58f, -20f, 0f), 1.28f, height);
                RenderPanel(camera, target, preview, new Vector3(0f, .50f, .72f),
                    Quaternion.Euler(0f, -90f, 0f), .70f, 0);

                Directory.CreateDirectory("Logs");
                var bytes = preview.EncodeToPNG();
                if (!File.Exists(PreviewPath) || !File.ReadAllBytes(PreviewPath).SequenceEqual(bytes))
                {
                    File.WriteAllBytes(PreviewPath, bytes);
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(preview);
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(asphalt);
                Object.DestroyImmediate(carPaint);
            }
        }

        private static Mesh BuildPreviewCube()
        {
            var mesh = new MeshBuilder();
            foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down,
                         Vector3.forward, Vector3.back })
            {
                var across = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
                var up = Vector3.Cross(normal, across);
                var corner = (normal - across - up) * .5f;
                mesh.Quad(corner, corner + across, corner + across + up, corner + up);
            }
            return mesh.Build("Preview Cube");
        }

        private static void AddPreviewLight(Scene scene, string name, float intensity, Quaternion rotation)
        {
            var light = NewObject(scene, null, name, typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.transform.rotation = rotation;
        }

        private static void RenderPanel(Camera camera, RenderTexture target, Texture2D preview, Vector3 center,
            Quaternion rotation, float size, int y)
        {
            camera.transform.SetPositionAndRotation(center + rotation * Vector3.back * 4f, rotation);
            camera.orthographicSize = size;
            camera.Render();
            RenderTexture.active = target;
            preview.ReadPixels(new Rect(0, 0, target.width, target.height), 0, y, false);
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            public int Add(Vector3 position, Vector2 texcoord = default, Vector3 normal = default)
            {
                int index = vertices.Count;
                vertices.Add(position);
                uv.Add(texcoord);
                normals.Add(normal);
                return index;
            }

            public void Triangle(int a, int b, int c)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            public void Quad(int a, int b, int c, int d)
            {
                Triangle(a, b, c);
                Triangle(a, c, d);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Quad(Add(a), Add(b), Add(c), Add(d));
            }

            public Mesh Build(string name, bool hasNormals = false)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(triangles, 0);
                if (hasNormals)
                {
                    mesh.SetNormals(normals);
                }
                else
                {
                    mesh.RecalculateNormals();
                }
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
