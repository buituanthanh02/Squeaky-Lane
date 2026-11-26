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
    public static class ToyTownTools
    {
        private const string Art = "Assets/_Project/Art/Environment/Town";
        private const string Prefabs = "Assets/_Project/Prefabs/Environment/Town";
        private const string PreviewPath = "Logs/016-town-preview.png";
        private const int TriangleLimit = 1200;

        // Mỗi màu lấy mẫu ở tâm một ô trong atlas Point. Cả bộ prop dùng chung một material.
        private enum Paint { Cream, Peach, Mint, Sand, Clay, Teal, Slate, Glass, Dark, Coral, White, Timber }
        private static readonly Color32[] Palette =
        {
            Rgb(0xEFE3CC), Rgb(0xF0CAB1), Rgb(0xB9D6C7), Rgb(0xE1C598),
            Rgb(0xC97D67), Rgb(0x6C9B8B), Rgb(0x829498), Rgb(0xACD9E4),
            Rgb(0x536571), Rgb(0xCC6966), Rgb(0xFCF0DA), Rgb(0xB68A69)
        };

        [MenuItem("Tools/Squeaky Lane/016 Toy/Create Toy Town Props")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi tạo Toy Town.");
            }
            var shader = Shader.Find("SqueakyLane/Toy Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Thiếu SqueakyLane/Toy Lit.");
            }
            EnsureFolder(Art);
            EnsureFolder(Prefabs);
            var roadTexture = GetTexture("road_toy", 256, 256, RoadPixel, TextureWrapMode.Repeat, TextureWrapMode.Clamp);
            var sidewalkTexture = GetTexture("sidewalk_toy", 128, 128, SidewalkPixel,
                TextureWrapMode.Repeat, TextureWrapMode.Repeat);
            var atlas = GetTexture("town_palette", Palette.Length, 1, (x, y) => Palette[x],
                TextureWrapMode.Clamp, TextureWrapMode.Clamp);
            var road = GetMaterial("M_ToyRoad", shader, roadTexture, 0f, 0f);
            var sidewalk = GetMaterial("M_ToySidewalk", shader, sidewalkTexture, 0f, 0f);
            var paint = GetMaterial("M_ToyTownProps", shader, atlas, .20f, .09f);

            var scene = EditorSceneManager.NewPreviewScene();
            GameObject[] props;
            try
            {
                props = new[]
                {
                    GetProp(scene, "TownHouse_A", paint, 1.4f, mesh => BuildHouse(mesh, false)),
                    GetProp(scene, "TownHouse_B", paint, 1.4f, mesh => BuildHouse(mesh, true)),
                    GetProp(scene, "TownShop", paint, 1.4f, BuildShop),
                    GetProp(scene, "TownGarage", paint, 1.4f, BuildGarage),
                    GetProp(scene, "TownLamp", paint, 1f, BuildLamp),
                    GetProp(scene, "TownBench", paint, .6f, BuildBench),
                    GetProp(scene, "TownHydrant", paint, .6f, BuildHydrant),
                    GetProp(scene, "TownSign", paint, 1f, BuildSign)
                };
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            SavePreview(road, sidewalk, props);
            Debug.Log($"016-c · Toy Town Props · {PreviewPath}");
        }

        private static Color32 RoadPixel(int x, int y)
        {
            // U chạy dọc đường: hai cột ở điểm nối giống hệt nhau, đầu/cuối chu kỳ nằm giữa khoảng trống.
            if ((y < 4 || y >= 252) || (y >= 122 && y < 134 && x >= 60 && x < 196))
            {
                return Rgb(0xEFE3CC);
            }
            int grainX = Mathf.Min(x, 255 - x);
            int grain = ((grainX * 73 ^ y * 127 ^ (grainX + y) * 37) & 7) - 3;
            return new Color32((byte)(91 + grain), (byte)(97 + grain), (byte)(112 + grain), 255);
        }

        private static Color32 SidewalkPixel(int x, int y)
        {
            // Hai hàng gạch lệch nửa viên, đường ron nối được ở cả U lẫn V.
            int brickX = (x + (y / 64) * 32) % 64;
            int brickY = y % 64;
            if (brickX < 2 || brickX >= 62 || brickY < 2 || brickY >= 62)
            {
                return Rgb(0xE1D4BC);
            }
            return brickX == 2 || brickY == 2 ? Rgb(0xF5EBD7) : Rgb(0xEFE3CC);
        }

        private static void BuildHouse(PropMesh mesh, bool tealRoof)
        {
            Paint siding = tealRoof ? Paint.Mint : Paint.Peach;
            Paint roof = tealRoof ? Paint.Teal : Paint.Clay;
            mesh.Box(Paint.Cream, 0, .045f, 0, 1.8f, .09f, 1.22f);
            mesh.Box(siding, 0, .48f, 0, 1.72f, .82f, 1.14f, .055f);
            var gable = new Vector2[]
            {
                new Vector2(-1f, .84f), new Vector2(1f, .84f), new Vector2(1f, .92f),
                new Vector2(.075f, 1.34f), new Vector2(-.075f, 1.34f), new Vector2(-1f, .92f)
            };
            mesh.Prism(roof, gable, .67f, .045f, Vector3.zero, Quaternion.identity);
            mesh.Box(Paint.White, tealRoof ? .53f : -.56f, 1.14f, -.26f, .18f, .45f, .19f);
            mesh.Box(roof, tealRoof ? .53f : -.56f, 1.36f, -.26f, .22f, .08f, .23f);
            float doorX = tealRoof ? -.34f : 0;
            FrontPanel(mesh, doorX, .30f, .57f, .34f, .48f, roof, Paint.Cream);
            Window(mesh, tealRoof ? .36f : -.52f, .58f, .57f, .40f, .31f);
            if (!tealRoof)
            {
                Window(mesh, .52f, .58f, .57f, .40f, .31f);
            }
            mesh.Box(Paint.Cream, doorX, .047f, .636f, .45f, .094f, .19f, .02f);
        }

        private static void BuildShop(PropMesh mesh)
        {
            mesh.Box(Paint.Cream, 0, .045f, 0, 2.16f, .09f, 1.21f);
            mesh.Box(Paint.Sand, 0, .54f, 0, 2.06f, .98f, 1.11f, .065f);
            mesh.Box(Paint.Teal, 0, 1.05f, 0, 2.18f, .16f, 1.23f, .05f);
            FrontPanel(mesh, 0, 1.045f, .615f, .66f, .135f, Paint.White, Paint.Clay);
            Window(mesh, -.43f, .47f, .555f, .87f, .55f);
            FrontPanel(mesh, .52f, .36f, .555f, .50f, .61f, Paint.Cream, Paint.Glass);
            mesh.Box(Paint.Dark, .66f, .33f, .594f, .048f, .095f, .035f, .01f);
            for (int stripe = 0; stripe < 7; stripe++)
            {
                float x = (stripe - 3) * .296f;
                var paint = stripe % 2 == 0 ? Paint.Clay : Paint.White;
                mesh.Box(paint, x, .83f, .716f, .296f, .11f, .44f, .014f, 17f);
            }
        }

        private static void BuildGarage(PropMesh mesh)
        {
            mesh.Box(Paint.Cream, 0, .045f, 0, 1.9f, .09f, 1.20f);
            mesh.Box(Paint.Peach, 0, .43f, 0, 1.84f, .82f, 1.14f, .048f);
            mesh.Box(Paint.Slate, 0, .89f, 0, 2f, .17f, 1.24f, .047f);
            FrontPanel(mesh, -.23f, .40f, .57f, 1.14f, .63f, Paint.Dark, Paint.Slate);
            for (int slat = 0; slat < 5; slat++)
            {
                mesh.Box(Paint.Cream, -.23f, .17f + slat * .105f, .602f, 1.038f, .025f, .018f, .007f);
            }
            Window(mesh, .56f, .58f, .57f, .31f, .25f);
        }

        private static void FrontPanel(PropMesh mesh, float x, float y, float z, float width, float height,
            Paint frame, Paint inset)
        {
            float border = Mathf.Min(.075f, height * .36f);
            mesh.Box(frame, x, y, z, width, height, .056f, .016f);
            mesh.Box(inset, x, y, z + .024f, width - border, height - border, .023f, .01f);
        }

        private static void Window(PropMesh mesh, float x, float y, float z, float width, float height)
        {
            FrontPanel(mesh, x, y, z, width, height, Paint.White, Paint.Glass);
            mesh.Box(Paint.White, x, y, z + .036f, .035f, height - .037f, .019f, .006f);
        }

        private static void BuildLamp(PropMesh mesh)
        {
            mesh.Cylinder(Paint.Slate, 0, .043f, 0, .104f, .086f);
            var arm = new Vector2[]
            {
                new Vector2(-.04f, .735f), new Vector2(.035f, .735f), new Vector2(.24f, .826f),
                new Vector2(.24f, .903f), new Vector2(.067f, .903f), new Vector2(-.04f, .792f)
            };
            // Hai khối lồi giúp phần tay đèn uốn cong mà vẫn giữ mặt cắt khép kín.
            mesh.Box(Paint.Dark, -.0075f, .412f, 0, .065f, .72f, .062f, .014f);
            mesh.Prism(Paint.Dark, arm, .032f, .015f, Vector3.zero, Quaternion.identity);
            mesh.Box(Paint.Slate, .205f, .854f, 0, .254f, .133f, .192f, .041f);
            mesh.Box(Paint.White, .217f, .787f, 0, .187f, .033f, .138f, .015f);
        }

        private static void BuildBench(PropMesh mesh)
        {
            foreach (float x in new[] { -.43f, .43f })
            {
                mesh.Box(Paint.Teal, x, .117f, .11f, .10f, .234f, .10f);
                mesh.Box(Paint.Teal, x, .212f, -.13f, .10f, .424f, .10f);
                mesh.Box(Paint.Teal, x, .19f, 0, .11f, .08f, .37f);
            }
            for (int slat = 0; slat < 3; slat++)
            {
                mesh.Box(Paint.Timber, 0, .25f, -.12f + slat * .12f, 1.10f, .058f, .108f, .014f);
            }
            for (int slat = 0; slat < 2; slat++)
            {
                mesh.Box(Paint.Timber, 0, .39f + slat * .097f, -.119f, 1.10f, .079f, .062f, .017f);
            }
        }

        private static void BuildHydrant(PropMesh mesh)
        {
            mesh.Cylinder(Paint.Clay, 0, .028f, 0, .122f, .056f);
            mesh.Cylinder(Paint.Coral, 0, .192f, 0, .087f, .292f);
            mesh.Cylinder(Paint.Coral, 0, .33f, 0, .115f, .119f);
            mesh.Box(Paint.Clay, 0, .397f, 0, .071f, .059f, .075f, .015f);
            mesh.Box(Paint.Coral, 0, .202f, 0, .273f, .102f, .104f, .025f);
            mesh.Box(Paint.Clay, 0, .205f, .093f, .12f, .11f, .098f, .031f);
        }

        private static void BuildSign(PropMesh mesh)
        {
            mesh.Box(Paint.Cream, 0, .049f, 0, .52f, .098f, .30f, .024f);
            mesh.Box(Paint.Slate, 0, .36f, 0, .092f, .65f, .09f, .021f);
            mesh.Box(Paint.Teal, 0, .684f, 0, .67f, .48f, .116f, .055f);
            mesh.Box(Paint.White, 0, .684f, .048f, .56f, .39f, .035f, .017f);
            var drop = new Vector2[]
            {
                new Vector2(-.05f, -.11f), new Vector2(.05f, -.11f), new Vector2(.098f, -.07f),
                new Vector2(.103f, -.017f), new Vector2(.074f, .043f), new Vector2(0, .143f),
                new Vector2(-.074f, .043f), new Vector2(-.103f, -.017f), new Vector2(-.098f, -.07f)
            };
            mesh.Prism(Paint.Teal, drop, .012f, .01f, new Vector3(0, .684f, .067f), Quaternion.identity);
            mesh.Box(Paint.Glass, -.046f, .651f, .08f, .029f, .056f, .012f, .005f, 0, -27f);
        }

        private static GameObject GetProp(Scene scene, string name, Material paint, float maxHeight, Action<PropMesh> build)
        {
            string path = $"{Prefabs}/{name}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return prefab;
            }
            RequireFreePath(path);
            string meshPath = $"{Art}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                RequireFreePath(meshPath);
                var builder = new PropMesh();
                build(builder);
                mesh = builder.Create(name, maxHeight);
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            var root = NewMeshObject(scene, name, mesh, paint);
            try
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Lưu prefab thất bại: {path}.");
                }
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Material GetMaterial(string name, Shader shader, Texture2D texture, float spec, float rim)
        {
            string path = $"{Art}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }
            RequireFreePath(path);
            material = new Material(shader) { name = name, mainTexture = texture, color = Color.white, enableInstancing = true };
            material.SetFloat("_SpecStrength", spec);
            material.SetFloat("_RimStrength", rim);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D GetTexture(string name, int width, int height, Func<int, int, Color32> pixel,
            TextureWrapMode wrapU, TextureWrapMode wrapV)
        {
            string path = $"{Art}/{name}.png";
            if (!File.Exists(path))
            {
                RequireFreePath(path);
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    var colors = new Color32[width * height];
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            colors[y * width + x] = pixel(x, y);
                        }
                    }
                    image.SetPixels32(colors);
                    image.Apply();
                    File.WriteAllBytes(path, image.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(image);
                }
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null)
            {
                throw new InvalidOperationException($"Thiếu TextureImporter: {path}.");
            }
            bool atlas = height == 1;
            var filter = atlas ? FilterMode.Point : FilterMode.Bilinear;
            if (importer.textureType != TextureImporterType.Default || !importer.sRGBTexture ||
                importer.mipmapEnabled == atlas || importer.filterMode != filter ||
                importer.npotScale != TextureImporterNPOTScale.None ||
                importer.wrapModeU != wrapU || importer.wrapModeV != wrapV ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = !atlas;
                importer.filterMode = filter;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapModeU = wrapU;
                importer.wrapModeV = wrapV;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return RequireAsset<Texture2D>(path);
        }

        private static Color32 Rgb(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        private static GameObject NewMeshObject(Scene scene, string name, Mesh mesh, Material material)
        {
            var root = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave,
                typeof(MeshFilter), typeof(MeshRenderer));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.hideFlags = HideFlags.None;
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return root;
        }

        private static void SavePreview(Material road, Material sidewalk, GameObject[] props)
        {
            const string toyArt = "Assets/_Project/Art/Environment/Toy";
            const string toyPrefabs = "Assets/_Project/Prefabs/Environment/Toy";
            var grass = RequireAsset<Material>(toyArt + "/M_ToyGround.mat");
            var tree = RequireAsset<GameObject>(toyPrefabs + "/ToyTree_Round.prefab");
            var preview = new PreviewRenderUtility();
            var surfaces = new List<Mesh>();
            try
            {
                // Mat road giữ U theo mét và V phủ đúng bề ngang; vỉa hè lặp một tile mỗi 0,8 ô.
                AddSurface("Ground", -4.2f, 4.2f, -3.5f, 3.5f, -.025f, 1, 1, grass, .5f / grass.mainTexture.width);
                AddSurface("Road", -3f, 3f, -.91f, .91f, -.012f, 6, 1, road);
                AddSurface("Far sidewalk", -3f, 3f, .91f, 2.95f, 0, 7.5f, 2.55f, sidewalk);
                AddSurface("Near sidewalk", -3f, 3f, -2.59f, -.91f, 0, 7.5f, 2.1f, sidewalk);

                Place(props[0], -2.04f, 2.22f, 180);
                Place(props[2], .03f, 2.16f, 180);
                Place(props[1], 2.13f, 2.29f, 180);
                Place(props[3], -1.88f, -1.89f, 172);
                Place(props[4], -.99f, 1.17f, -140);
                Place(props[4], 2.46f, -1.23f, 35);
                Place(props[5], 1.32f, -2.31f, 168);
                Place(props[6], -.44f, -1.39f, 145);
                Place(props[7], 1.13f, 1.45f, 180);
                Place(tree, 2.49f, -2.12f, 0);
                Place(tree, -3.39f, 1.78f, 0);

                var camera = preview.camera;
                preview.ambientColor = new Color32(154, 169, 194, 255);
                preview.lights[0].intensity = 1f;
                preview.lights[0].shadows = LightShadows.None;
                preview.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0);
                preview.lights[1].enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Rgb(0xB7DDEE);
                camera.renderingPath = RenderingPath.Forward;
                camera.orthographic = true;
                camera.orthographicSize = 3.44f;
                camera.allowHDR = false;
                var rotation = Quaternion.Euler(58f, 14f, 0);
                camera.transform.SetPositionAndRotation(new Vector3(0, .11f, .10f) + rotation * Vector3.back * 10f, rotation);
                preview.BeginStaticPreview(new Rect(0, 0, 1100, 840));
                preview.Render();
                var image = preview.EndStaticPreview();
                try
                {
                    Directory.CreateDirectory("Logs");
                    byte[] bytes = image.EncodeToPNG();
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
                foreach (var surface in surfaces)
                {
                    Object.DestroyImmediate(surface);
                }
            }

            void Place(GameObject prefab, float x, float z, float yaw)
            {
                var instance = preview.InstantiatePrefabInScene(prefab);
                instance.transform.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, yaw, 0));
            }

            void AddSurface(string name, float left, float right, float near, float far, float y,
                float u, float v, Material material, float? grassU = null)
            {
                var mesh = new Mesh { name = name };
                surfaces.Add(mesh);
                mesh.vertices = new[]
                {
                    new Vector3(left, y, near), new Vector3(left, y, far),
                    new Vector3(right, y, far), new Vector3(right, y, near)
                };
                mesh.uv = grassU.HasValue
                    ? new[] { new Vector2(grassU.Value, .25f), new Vector2(grassU.Value, .75f),
                        new Vector2(grassU.Value, .75f), new Vector2(grassU.Value, .25f) }
                    : new[] { Vector2.zero, new Vector2(0, v), new Vector2(u, v), new Vector2(u, 0) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateNormals();
                NewMeshObject(preview.camera.scene, name, mesh, material);
            }
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
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            public void Box(Paint paint, float x, float y, float z, float width, float height, float depth,
                float bevel = .026f, float pitch = 0, float roll = 0)
            {
                float w = width * .5f;
                float h = height * .5f;
                float d = depth * .5f;
                float b = Mathf.Min(bevel, Mathf.Min(w, h) * .45f);
                var outline = new Vector2[]
                {
                    new Vector2(-w + b, -h), new Vector2(w - b, -h),
                    new Vector2(w, -h + b), new Vector2(w, h - b),
                    new Vector2(w - b, h), new Vector2(-w + b, h),
                    new Vector2(-w, h - b), new Vector2(-w, -h + b)
                };
                Prism(paint, outline, d, Mathf.Min(b, d * .45f),
                    new Vector3(x, y, z), Quaternion.Euler(pitch, 0, roll));
            }

            public void Cylinder(Paint paint, float x, float y, float z, float radius, float height)
            {
                var outline = new Vector2[10];
                for (int i = 0; i < outline.Length; i++)
                {
                    float angle = 2 * Mathf.PI * i / outline.Length;
                    outline[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                Prism(paint, outline, height * .5f, Mathf.Min(.02f, height * .18f),
                    new Vector3(x, y, z), Quaternion.Euler(-90, 0, 0));
            }

            public void Prism(Paint paint, Vector2[] outline, float halfDepth, float bevel,
                Vector3 center, Quaternion rotation)
            {
                // Biên lồi ngược chiều kim đồng hồ khi nhìn mặt tiền (+Z).
                // Hai vòng ngoài giữ silhouette, hai mặt đầu thu vào để tạo cạnh vát bắt sáng.
                Vector2 low = outline[0];
                Vector2 high = outline[0];
                foreach (var point in outline)
                {
                    low = Vector2.Min(low, point);
                    high = Vector2.Max(high, point);
                }
                var middle = (low + high) * .5f;
                var size = high - low;
                var shrink = new Vector2(1 - Mathf.Min(.34f, 2 * bevel / size.x),
                    1 - Mathf.Min(.34f, 2 * bevel / size.y));
                var colorUv = new Vector2(((int)paint + .5f) / Palette.Length, .5f);
                int count = outline.Length;
                var rings = new Vector3[4, count];
                for (int i = 0; i < count; i++)
                {
                    var outer = outline[i];
                    var inner = middle + Vector2.Scale(outer - middle, shrink);
                    rings[0, i] = Place(inner, -halfDepth);
                    rings[1, i] = Place(outer, -halfDepth + bevel);
                    rings[2, i] = Place(outer, halfDepth - bevel);
                    rings[3, i] = Place(inner, halfDepth);
                }
                for (int band = 0; band < 3; band++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int next = (i + 1) % count;
                        int start = vertices.Count;
                        Add(rings[band, i]);
                        Add(rings[band, next]);
                        Add(rings[band + 1, next]);
                        Add(rings[band + 1, i]);
                        Triangle(start, start + 1, start + 2);
                        Triangle(start, start + 2, start + 3);
                    }
                }
                Cap(0, false);
                Cap(3, true);

                Vector3 Place(Vector2 point, float z) => center + rotation * new Vector3(point.x, point.y, z);

                void Add(Vector3 point)
                {
                    vertices.Add(point);
                    uv.Add(colorUv);
                }

                void Cap(int ring, bool front)
                {
                    int start = vertices.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Add(rings[ring, i]);
                    }
                    for (int i = 1; i < count - 1; i++)
                    {
                        Triangle(start, start + (front ? i : i + 1), start + (front ? i + 1 : i));
                    }
                }
            }

            public Mesh Create(string name, float maxHeight)
            {
                int count = triangles.Count / 3;
                if (count > TriangleLimit || Mathf.Abs(vertices.Min(point => point.y)) > .0001f ||
                    vertices.Max(point => point.y) > maxHeight + .0001f)
                {
                    throw new InvalidOperationException($"{name}: {count} tam giác hoặc chiều cao/pivot vượt giới hạn.");
                }
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                return mesh;
            }

            private void Triangle(int a, int b, int c)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }
        }
    }
}
