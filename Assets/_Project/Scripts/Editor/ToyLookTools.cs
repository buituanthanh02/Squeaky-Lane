using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqueakyLane.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SqueakyLane.EditorTools
{
    public static class ToyLookTools
    {
        private const string Project = "Assets/_Project";
        private const string CarSource = Project + "/Art/Cars/Kenney/Materials";
        private const string CarArt = Project + "/Art/Cars/Toy";
        private const string EnvironmentArt = Project + "/Art/Environment/Toy";
        private const string PropPrefabs = Project + "/Prefabs/Environment/Toy";
        private const string PreviewPath = "Logs/015-toy-preview.png";
        private static readonly string[] CarColors = { "pink", "blue", "yellow", "green", "orange", "purple" };

        // Cùng thứ tự với các slot của mesh props.
        private enum Paint { Leaf, LightLeaf, Bark, Cream, Accents, Stone }
        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32[] AccentColors =
        {
            White, new Color32(255, 148, 191, 255), new Color32(255, 213, 92, 255),
            new Color32(255, 191, 141, 255)
        };

        [MenuItem("Tools/Squeaky Lane/015 Toy/Create Toy Look Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi tạo Toy Look.");
            }

            var lit = RequireShader("SqueakyLane/Toy Lit");
            var outline = RequireShader("SqueakyLane/Toy Lit Outline");
            var groundShader = RequireShader("SqueakyLane/Toy Ground");
            var carSources = CarColors.Select(color => RequireAsset<Material>($"{CarSource}/Car_{color}.mat")).ToArray();
            var pink = RequireAsset<GameObject>(Project + "/Prefabs/Cars/Car_pink.prefab");
            var yellow = RequireAsset<GameObject>(Project + "/Prefabs/Cars/Car_yellow.prefab");
            var theme = RequireAsset<UiTheme>(Project + "/Config/UiTheme.asset");
            var groundSource = theme.GroundMaterial;
            if (groundSource == null || groundSource.mainTexture == null ||
                carSources.Any(source => source.mainTexture == null))
            {
                throw new InvalidOperationException("Thiếu colormap Kenney hoặc atlas groundMaterial trong UiTheme.");
            }

            EnsureFolder(CarArt);
            EnsureFolder(EnvironmentArt + "/Meshes");
            EnsureFolder(PropPrefabs);
            var cars = new Material[CarColors.Length];
            var outlinedCars = new Material[CarColors.Length];
            for (int i = 0; i < CarColors.Length; i++)
            {
                string color = CarColors[i];
                var source = carSources[i];
                cars[i] = GetOrCreateMaterial(CarArt, "M_ToyCar_" + color, lit, source.color, source.mainTexture, source);
                outlinedCars[i] = GetOrCreateMaterial(CarArt, "M_ToyCarOutline_" + color, outline,
                    source.color, source.mainTexture, source);
            }

            var detail = GetOrCreateTexture(EnvironmentArt + "/grass_detail.png", BuildGrassPng, true);
            var ground = GetOrCreateMaterial(EnvironmentArt, "M_ToyGround", groundShader,
                Color.white, GetGroundAtlas(groundSource.mainTexture), groundSource, detail);
            var paint = CreatePropMaterials(lit);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var props = new[]
                {
                    GetOrCreateProp(scene, "ToyTree_Round", paint, BuildRoundTree),
                    GetOrCreateProp(scene, "ToyTree_Cone", paint, BuildConeTree),
                    GetOrCreateProp(scene, "ToyBush", paint, BuildBush),
                    GetOrCreateProp(scene, "ToyFlowers", paint, BuildFlowers),
                    GetOrCreateProp(scene, "ToyFence", paint, BuildFence),
                    GetOrCreateProp(scene, "ToyRock", paint, BuildRock)
                };
                SavePreview(ground, props, pink, yellow, carSources[0], carSources[2], cars[0], outlinedCars[2]);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Debug.Log($"015-b: Toy Look Assets · {PreviewPath}");
        }

        private static Texture GetGroundAtlas(Texture source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                importer.npotScale == TextureImporterNPOTScale.None && importer.filterMode == FilterMode.Point &&
                !importer.mipmapEnabled)
            {
                return source;
            }

            // Importer nguồn có thể resize atlas lẻ cột. Lấy PNG gốc trước khi Unity resize.
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                throw new InvalidOperationException($"Cần atlas nền PNG hoặc atlas đã import Point/NPOT None: {path}.");
            }
            return GetOrCreateTexture(EnvironmentArt + "/ground_colors_point.png", () => File.ReadAllBytes(path), false);
        }

        private static Material[] CreatePropMaterials(Shader lit)
        {
            var accents = GetOrCreateTexture(EnvironmentArt + "/prop_accents.png",
                () => EncodePng(AccentColors.Length, 1, AccentColors), false);
            return new[]
            {
                GetOrCreateMaterial(EnvironmentArt, "M_ToyLeaf", lit, new Color32(91, 200, 91, 255)),
                GetOrCreateMaterial(EnvironmentArt, "M_ToyLightLeaf", lit, new Color32(126, 224, 122, 255)),
                GetOrCreateMaterial(EnvironmentArt, "M_ToyBark", lit, new Color32(154, 107, 79, 255)),
                GetOrCreateMaterial(EnvironmentArt, "M_ToyCream", lit, new Color32(255, 244, 224, 255)),
                GetOrCreateMaterial(EnvironmentArt, "M_ToyAccents", lit, Color.white, accents),
                GetOrCreateMaterial(EnvironmentArt, "M_ToyStone", lit, new Color32(168, 156, 149, 255))
            };
        }

        private static Material GetOrCreateMaterial(string folder, string name, Shader shader, Color color,
            Texture texture = null, Material source = null, Texture detail = null)
        {
            string path = $"{folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            RequireFreePath(path);
            var material = new Material(shader) { name = name, color = color, enableInstancing = true };
            if (texture != null)
            {
                material.mainTexture = texture;
            }
            if (source != null)
            {
                material.mainTextureScale = source.mainTextureScale;
                material.mainTextureOffset = source.mainTextureOffset;
            }
            if (detail != null)
            {
                material.SetTexture("_DetailTex", detail);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D GetOrCreateTexture(string path, Func<byte[]> build, bool repeat)
        {
            if (!File.Exists(path))
            {
                RequireFreePath(path);
                File.WriteAllBytes(path, build());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Không tìm thấy TextureImporter: {path}.");
            }
            var filter = repeat ? FilterMode.Bilinear : FilterMode.Point;
            var wrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importer.textureType != TextureImporterType.Default || !importer.sRGBTexture ||
                importer.mipmapEnabled != repeat || importer.filterMode != filter || importer.wrapMode != wrap ||
                importer.npotScale != TextureImporterNPOTScale.None ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                (repeat && (importer.alphaSource != TextureImporterAlphaSource.FromInput || importer.alphaIsTransparency)))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = repeat;
                importer.filterMode = filter;
                importer.wrapMode = wrap;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (repeat)
                {
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = false;
                }
                importer.SaveAndReimport();
            }
            return RequireAsset<Texture2D>(path);
        }

        private static byte[] BuildGrassPng()
        {
            const int size = 256;
            // Alpha 0 nhân màu cỏ; alpha 255 trộn màu điểm hoa lên cỏ.
            var pixels = Enumerable.Repeat(new Color32(255, 255, 255, 0), size * size).ToArray();
            var tufts = new[]
            {
                new Vector2Int(31, 35), new Vector2Int(111, 26), new Vector2Int(213, 51), new Vector2Int(69, 100),
                new Vector2Int(154, 123), new Vector2Int(241, 145), new Vector2Int(38, 201), new Vector2Int(131, 218)
            };
            for (int i = 0; i < tufts.Length; i++)
            {
                var p = tufts[i];
                int lean = i % 3 - 1;
                var darkGreen = new Color32(108, 159, 99, 0);
                Stroke(p, p + new Vector2Int(-5 + lean, 6 + i % 3), darkGreen);
                Stroke(p, p + new Vector2Int(4 + lean, 5), darkGreen);
            }

            var flowers = new[] { new Vector2Int(64, 55), new Vector2Int(180, 80),
                new Vector2Int(105, 165), new Vector2Int(201, 218) };
            for (int i = 0; i < flowers.Length; i++)
            {
                var p = flowers[i];
                var petal = AccentColors[i % 3];
                Stamp(p.x - 2, p.y, 2, petal);
                Stamp(p.x + 2, p.y, 2, petal);
                Stamp(p.x, p.y - 2, 2, petal);
                Stamp(p.x, p.y + 2, 2, petal);
                Stamp(p.x, p.y, 1, AccentColors[2]);
            }
            return EncodePng(size, size, pixels);

            void Stamp(int x, int y, int radius, Color32 color)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            int px = (x + dx + size) % size;
                            int py = (y + dy + size) % size;
                            pixels[py * size + px] = color;
                        }
                    }
                }
            }

            void Stroke(Vector2Int start, Vector2Int end, Color32 color)
            {
                int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
                for (int i = 0; i <= steps; i++)
                {
                    var pixel = Vector2.Lerp(start, end, i / (float)steps);
                    Stamp(Mathf.RoundToInt(pixel.x), Mathf.RoundToInt(pixel.y), 1, color);
                }
            }
        }

        private static byte[] EncodePng(int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static GameObject GetOrCreateProp(Scene scene, string name, Material[] palette, Action<PropMesh> build)
        {
            string prefabPath = $"{PropPrefabs}/{name}.prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            // Giữ mesh/prefab và chỉnh sửa Inspector từ lần tạo trước.
            if (saved != null)
            {
                return saved;
            }

            RequireFreePath(prefabPath);
            var builder = new PropMesh();
            build(builder);
            var mesh = builder.Save(name);
            var root = NewMeshObject(scene, name, mesh, builder.Materials(palette));
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Lưu prefab thất bại: {prefabPath}.");
                }
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildRoundTree(PropMesh mesh)
        {
            mesh.Lathe(Paint.Bark, Vector3.zero, 8, new Vector2(0, 0), new Vector2(.07f, 0),
                new Vector2(.06f, .13f), new Vector2(.047f, .46f), new Vector2(0, .46f));
            mesh.Ellipsoid(Paint.Leaf, new Vector3(0, .54f, 0), new Vector3(.30f, .23f, .27f), 10, 5);
            mesh.Ellipsoid(Paint.LightLeaf, new Vector3(-.06f, .66f, -.025f), new Vector3(.24f, .19f, .22f), 10, 5);
        }

        private static void BuildConeTree(PropMesh mesh)
        {
            mesh.Lathe(Paint.Bark, Vector3.zero, 8, new Vector2(0, 0), new Vector2(.06f, 0),
                new Vector2(.046f, .3f), new Vector2(0, .3f));
            AddTier(Paint.Leaf, .13f, .29f, .43f);
            AddTier(Paint.Leaf, .33f, .23f, .40f);
            AddTier(Paint.LightLeaf, .52f, .17f, .38f);

            void AddTier(Paint paint, float bottom, float radius, float height)
            {
                mesh.Lathe(paint, new Vector3(0, bottom, 0), 10,
                    new Vector2(0, 0), new Vector2(radius * .75f, 0), new Vector2(radius, .04f),
                    new Vector2(radius * .82f, height * .30f), new Vector2(radius * .19f, height * .92f),
                    new Vector2(0, height));
            }
        }

        private static void BuildBush(PropMesh mesh)
        {
            mesh.Ellipsoid(Paint.Leaf, new Vector3(-.16f, .13f, 0), new Vector3(.18f, .13f, .16f), 8, 4);
            mesh.Ellipsoid(Paint.Leaf, new Vector3(.16f, .12f, .015f), new Vector3(.19f, .12f, .16f), 8, 4);
            mesh.Ellipsoid(Paint.LightLeaf, new Vector3(-.03f, .175f, .045f), new Vector3(.19f, .175f, .17f), 8, 4);
            mesh.Ellipsoid(Paint.Leaf, new Vector3(.04f, .135f, -.10f), new Vector3(.16f, .13f, .15f), 8, 4);
        }

        private static void BuildFlowers(PropMesh mesh)
        {
            mesh.Ellipsoid(Paint.Leaf, new Vector3(0, .033f, 0), new Vector3(.32f, .033f, .24f), 8, 4);
            var centers = new[] { new Vector2(-.17f, .07f), new Vector2(.015f, .095f),
                new Vector2(.18f, .045f), new Vector2(-.10f, -.09f), new Vector2(.105f, -.10f) };
            for (int i = 0; i < centers.Length; i++)
            {
                float y = .13f + .014f * (i % 3);
                var p = new Vector3(centers[i].x, y, centers[i].y);
                mesh.Lathe(Paint.Leaf, new Vector3(p.x, 0, p.z), 4,
                    new Vector2(0, .04f), new Vector2(.013f, .04f), new Vector2(.010f, y), new Vector2(0, y));
                for (int petal = 0; petal < 4; petal++)
                {
                    float angle = petal * Mathf.PI * .5f + i * .42f;
                    var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .036f;
                    mesh.Ellipsoid(Paint.Accents, p + offset, new Vector3(.034f, .017f, .034f),
                        5, 3, i % 3);
                }
                int centerColor = i % 3 == 2 ? 3 : 2;
                mesh.Ellipsoid(Paint.Accents, p + Vector3.up * .007f, new Vector3(.022f, .014f, .022f),
                    6, 2, centerColor);
            }
        }

        private static void BuildFence(PropMesh mesh)
        {
            foreach (float x in new[] { -.446f, 0f, .446f })
            {
                var position = new Vector3(x, 0, 0);
                mesh.Lathe(Paint.Cream, position, 8, new Vector2(0, 0), new Vector2(.054f, 0),
                    new Vector2(.054f, .228f), new Vector2(.046f, .271f), new Vector2(.025f, .294f),
                    new Vector2(0, .3f));
                mesh.Lathe(Paint.Accents, position, 8, 3, new Vector2(0, .022f), new Vector2(.058f, .022f),
                    new Vector2(.058f, .059f), new Vector2(0, .059f));
            }
            mesh.Box(Paint.Cream, new Vector3(0, .107f, 0), new Vector3(.90f, .054f, .055f));
            mesh.Box(Paint.Cream, new Vector3(0, .204f, 0), new Vector3(.90f, .054f, .055f));
        }

        private static void BuildRock(PropMesh mesh)
        {
            mesh.Ellipsoid(Paint.Stone, new Vector3(-.035f, .10f, 0), new Vector3(.24f, .10f, .16f), 9, 4);
            mesh.Ellipsoid(Paint.Stone, new Vector3(.15f, .064f, -.08f), new Vector3(.12f, .064f, .095f), 7, 3);
        }

        private static GameObject NewMeshObject(Scene scene, string name, Mesh mesh, Material[] materials)
        {
            var item = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave,
                typeof(MeshFilter), typeof(MeshRenderer));
            SceneManager.MoveGameObjectToScene(item, scene);
            item.hideFlags = HideFlags.None;
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return item;
        }

        private static void SavePreview(Material ground, GameObject[] props, GameObject pink, GameObject yellow,
            Material pinkSource, Material yellowSource, Material lit, Material outline)
        {
            var preview = new PreviewRenderUtility();
            var floor = new Mesh { name = "Toy Preview Ground" };
            try
            {
                const float halfFloor = 3f;
                floor.vertices = new[] { new Vector3(-halfFloor, 0, -halfFloor), new Vector3(-halfFloor, 0, halfFloor),
                    new Vector3(halfFloor, 0, halfFloor), new Vector3(halfFloor, 0, -halfFloor) };
                float u = .5f / ground.mainTexture.width;
                floor.uv = new[] { new Vector2(u, .25f), new Vector2(u, .75f),
                    new Vector2(u, .75f), new Vector2(u, .25f) };
                floor.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                floor.RecalculateNormals();
                floor.RecalculateBounds();
                var camera = preview.camera;
                NewMeshObject(camera.scene, floor.name, floor, new[] { ground });

                // Thứ tự trái sang phải: Kenney gốc, Toy Lit, Toy Lit Outline.
                AddPreviewCar(preview, pink, new Vector3(-1.72f, .33f, -.30f), pinkSource, pinkSource);
                AddPreviewCar(preview, pink, new Vector3(0, .33f, -.30f), pinkSource, lit);
                AddPreviewCar(preview, yellow, new Vector3(1.72f, .33f, -.30f), yellowSource, outline);
                var positions = new[] { new Vector3(-2.30f, 0, 1.45f), new Vector3(2.22f, 0, 1.52f),
                    new Vector3(-1.55f, 0, 1.41f), new Vector3(.95f, 0, -1.84f),
                    new Vector3(0, 0, 1.63f), new Vector3(-1.05f, 0, -1.87f) };
                for (int i = 0; i < props.Length; i++)
                {
                    var instance = preview.InstantiatePrefabInScene(props[i]);
                    instance.transform.position = positions[i];
                }

                preview.ambientColor = new Color32(154, 169, 194, 255);
                var key = preview.lights[0];
                key.intensity = 1f;
                key.shadows = LightShadows.None;
                key.transform.rotation = Quaternion.Euler(50f, -30f, 0);
                preview.lights[1].enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(183, 221, 238, 255);
                camera.renderingPath = RenderingPath.Forward;
                camera.orthographic = true;
                camera.orthographicSize = 3.0f;
                camera.allowHDR = false;
                var rotation = Quaternion.Euler(58f, 3f, 0);
                camera.transform.SetPositionAndRotation(new Vector3(0, .15f, 0) + rotation * Vector3.back * 8f, rotation);

                preview.BeginStaticPreview(new Rect(0, 0, 1000, 860));
                preview.Render();
                var image = preview.EndStaticPreview();
                try
                {
                    Directory.CreateDirectory("Logs");
                    var bytes = image.EncodeToPNG();
                    if (!File.Exists(PreviewPath) || !File.ReadAllBytes(PreviewPath).SequenceEqual(bytes))
                    {
                        File.WriteAllBytes(PreviewPath, bytes);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(image);
                }
            }
            finally
            {
                preview.Cleanup();
                Object.DestroyImmediate(floor);
            }
        }

        private static void AddPreviewCar(PreviewRenderUtility preview, GameObject prefab, Vector3 position,
            Material source, Material replacement)
        {
            var clone = preview.InstantiatePrefabInScene(prefab);
            clone.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 65f, 0));
            foreach (var renderer in clone.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var current = materials[i];
                    // Khi Claude đã áp Toy lên prefab, ảnh vẫn giữ một xe đối chứng Kenney.
                    if (current == source || (current != null && current.mainTexture == source.mainTexture &&
                        (current.shader.name == "SqueakyLane/Toy Lit" || current.shader.name == "SqueakyLane/Toy Lit Outline")))
                    {
                        materials[i] = replacement;
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Shader RequireShader(string name)
        {
            var shader = Shader.Find(name);
            if (shader == null)
            {
                throw new InvalidOperationException($"Thiếu shader {name}.");
            }
            return shader;
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Thiếu {typeof(T).Name}: {path}.");
            }
            return asset;
        }

        private static void RequireFreePath(string path)
        {
            if (File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException($"Đã có file khác tại {path}.");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private sealed class PropMesh
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly SortedDictionary<Paint, List<int>> triangles = new SortedDictionary<Paint, List<int>>();

            public Material[] Materials(Material[] palette) => triangles.Keys.Select(key => palette[(int)key]).ToArray();

            public void Ellipsoid(Paint paint, Vector3 center, Vector3 radii, int segments, int bands, int accent = 0)
            {
                Surface(paint, center, segments, bands + 1, accent, (ring, radial) =>
                {
                    float angle = Mathf.PI * ring / bands;
                    float width = ring == 0 || ring == bands ? 0 : Mathf.Sin(angle);
                    var sphere = new Vector3(radial.x * width, -Mathf.Cos(angle), radial.z * width);
                    var point = Vector3.Scale(sphere, radii);
                    var normal = new Vector3(sphere.x / radii.x, sphere.y / radii.y, sphere.z / radii.z).normalized;
                    return (point, normal);
                });
            }

            public void Lathe(Paint paint, Vector3 center, int segments, params Vector2[] profile)
            {
                Lathe(paint, center, segments, 0, profile);
            }

            public void Lathe(Paint paint, Vector3 center, int segments, int accent, params Vector2[] profile)
            {
                Surface(paint, center, segments, profile.Length, accent, (ring, radial) =>
                {
                    var point = profile[ring];
                    var tangent = profile[Mathf.Min(ring + 1, profile.Length - 1)] - profile[Mathf.Max(0, ring - 1)];
                    var normal = point.x == 0
                        ? (ring == 0 ? Vector3.down : Vector3.up)
                        : (radial * tangent.y - Vector3.up * tangent.x).normalized;
                    return (radial * point.x + Vector3.up * point.y, normal);
                });
            }

            public void Box(Paint paint, Vector3 center, Vector3 size)
            {
                foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down,
                             Vector3.forward, Vector3.back })
                {
                    var side = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, n);
                    var up = Vector3.Cross(n, side);
                    int a = Add(center + Vector3.Scale((n - side - up) * .5f, size), n, 0);
                    int b = Add(center + Vector3.Scale((n + side - up) * .5f, size), n, 0);
                    int c = Add(center + Vector3.Scale((n + side + up) * .5f, size), n, 0);
                    int d = Add(center + Vector3.Scale((n - side + up) * .5f, size), n, 0);
                    Triangle(paint, a, b, c);
                    Triangle(paint, a, c, d);
                }
            }

            public Mesh Save(string name)
            {
                string path = $"{EnvironmentArt}/Meshes/{name}.asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved != null)
                {
                    if (saved.subMeshCount != triangles.Count)
                    {
                        throw new InvalidOperationException($"Mesh cũ có số submesh khác: {path}.");
                    }
                    return saved;
                }
                RequireFreePath(path);
                int count = triangles.Values.Sum(indices => indices.Count / 3);
                if (count > 600 || vertices.Min(v => v.y) < -.0001f || vertices.Max(v => v.y) > .9001f)
                {
                    throw new InvalidOperationException($"{name} vượt giới hạn 600 tam giác hoặc cao 0,9 ô.");
                }

                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                mesh.subMeshCount = triangles.Count;
                int submesh = 0;
                foreach (var indices in triangles.Values)
                {
                    mesh.SetTriangles(indices, submesh++, false);
                }
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            private void Surface(Paint paint, Vector3 center, int segments, int rings, int accent,
                Func<int, Vector3, (Vector3 point, Vector3 normal)> sample)
            {
                // Một đỉnh ở mỗi cực, các vòng giữa nối quad. Không sinh tam giác suy biến ở cực.
                var bottom = sample(0, Vector3.right);
                int first = Add(center + bottom.point, bottom.normal, accent);
                for (int ring = 1; ring < rings - 1; ring++)
                {
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = 2f * Mathf.PI * i / segments;
                        var s = sample(ring, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
                        Add(center + s.point, s.normal, accent);
                    }
                }
                var top = sample(rings - 1, Vector3.right);
                int last = Add(center + top.point, top.normal, accent);
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    Triangle(paint, first, first + 1 + i, first + 1 + next);
                    for (int ring = 0; ring < rings - 3; ring++)
                    {
                        int row = first + 1 + ring * segments;
                        Triangle(paint, row + i, row + segments + i, row + segments + next);
                        Triangle(paint, row + i, row + segments + next, row + next);
                    }
                    Triangle(paint, last, last - segments + next, last - segments + i);
                }
            }

            private int Add(Vector3 point, Vector3 normal, int accent)
            {
                int index = vertices.Count;
                vertices.Add(point);
                normals.Add(normal);
                uv.Add(new Vector2((accent + .5f) / AccentColors.Length, .5f));
                return index;
            }

            private void Triangle(Paint paint, int a, int b, int c)
            {
                if (!triangles.TryGetValue(paint, out var indices))
                {
                    indices = new List<int>();
                    triangles.Add(paint, indices);
                }
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }
        }
    }
}
