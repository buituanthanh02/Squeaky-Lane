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
    public static class WashVfxTools
    {
        private const string Art = "Assets/_Project/Art/VFX";
        private const string Vfx = "Assets/_Project/Prefabs/VFX";
        private const string Mechanics = "Assets/_Project/Prefabs/Mechanics";
        private const string PreviewPath = "Logs/014-wash-preview.png";
        private const int SplatSides = 8;
        private const float RoofHalfDepth = .27f;
        private const float PadHalfSize = .46f;

        private static readonly Color32 PadBlue = new Color32(89, 200, 255, 255);
        private static readonly Color32 BubbleBlue = new Color32(189, 240, 255, 255);
        private static readonly Color32 CandyPink = new Color32(255, 92, 138, 255);
        private static readonly Color32 CandyBlue = new Color32(53, 195, 255, 255);
        private static readonly Color32 CandyYellow = new Color32(255, 210, 63, 255);

        // Bố cục trên mui, theo thứ tự vệt lớn, vừa, nhỏ rồi chấm li ti.
        // Dấu của Drip chọn mép Z; giá trị 0 giữ nguyên vệt trên mặt mui.
        private static readonly Splat[][] SplatLayouts =
        {
            new[]
            {
                new Splat(-.30f, -.065f, .090f, .061f, .046f),
                new Splat(.19f, -.224f, .085f, .046f, .042f, -1),
                new Splat(-.12f, .137f, .050f, .045f, .032f),
                new Splat(.13f, .056f, .049f, .032f, .029f),
                new Splat(.35f, .130f, .036f, .044f, .026f),
                new Splat(-.40f, .087f, .027f, .020f, .022f),
                new Splat(.005f, -.096f, .028f, .025f, .023f),
                new Splat(-.19f, -.167f, .019f, .013f, .015f),
                new Splat(.29f, -.067f, .014f, .018f, .015f),
                new Splat(.034f, .179f, .017f, .013f, .015f)
            },
            new[]
            {
                new Splat(-.19f, -.223f, .087f, .047f, .050f, -1),
                new Splat(.28f, .064f, .086f, .067f, .044f),
                new Splat(-.37f, .115f, .050f, .033f, .031f),
                new Splat(.035f, .154f, .042f, .050f, .035f),
                new Splat(.38f, -.155f, .029f, .032f, .022f),
                new Splat(-.11f, .015f, .028f, .024f, .022f),
                new Splat(-.42f, -.081f, .015f, .018f, .015f),
                new Splat(.105f, -.145f, .018f, .014f, .015f),
                new Splat(.183f, .203f, .014f, .015f, .015f),
                new Splat(-.239f, .147f, .016f, .012f, .015f)
            },
            new[]
            {
                new Splat(-.34f, -.155f, .083f, .064f, .045f),
                new Splat(.040f, .223f, .090f, .047f, .047f, 1),
                new Splat(.28f, -.229f, .051f, .041f, .035f, -1),
                new Splat(.28f, .061f, .052f, .049f, .034f),
                new Splat(-.25f, .144f, .047f, .041f, .027f),
                new Splat(-.09f, -.062f, .031f, .021f, .023f),
                new Splat(.416f, -.073f, .022f, .027f, .022f),
                new Splat(-.425f, .065f, .019f, .012f, .015f),
                new Splat(.07f, -.16f, .016f, .018f, .015f),
                new Splat(.154f, -.013f, .015f, .013f, .015f),
                new Splat(.398f, .167f, .012f, .016f, .015f)
            }
        };

        [MenuItem("Tools/Squeaky Lane/014 VFX/Create Wash Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi tạo asset trạm rửa.");
            }

            var mud = LoadMaterial("M_MudBlob");
            var bubble = LoadMaterial("M_Bubble");
            var unlit = Shader.Find("Unlit/Texture");
            var standard = Shader.Find("Standard");
            if (unlit == null || standard == null)
            {
                throw new InvalidOperationException("Thiếu shader Unlit/Texture hoặc Standard.");
            }

            EnsureFolder(Art + "/Textures");
            EnsureFolder(Art + "/Meshes");
            EnsureFolder(Vfx);
            EnsureFolder(Mechanics);

            var padMap = SaveTexture("wash_pad", 128, PadPixel, TextureWrapMode.Clamp);
            var stripeMap = SaveTexture("roller_stripes", 64, RollerPixel, TextureWrapMode.Repeat);
            var pad = SaveMaterial("M_WashPad", unlit, padMap, Color.white, 0f);
            var roller = SaveMaterial("M_WashRoller", standard, stripeMap, Color.white, .40f);
            var post = SaveMaterial("M_WashPost", standard, null, CandyYellow, .38f);

            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var splats = new GameObject[SplatLayouts.Length];
                for (int i = 0; i < splats.Length; i++)
                {
                    string name = $"MudSplat_{i}";
                    var builder = new MeshBuilder();
                    for (int j = 0; j < SplatLayouts[i].Length; j++)
                    {
                        AddSplat(builder, SplatLayouts[i][j], 1.17f * (j + 3 * i));
                    }

                    var mesh = SaveMesh(name, builder.Build(name, true));
                    splats[i] = SavePrefab(Vfx, name, () => AddMesh(scene, null, name, mesh, mud));
                }

                var padMesh = SaveMesh("WashPad", RoundedSlab("WashPad", PadHalfSize, PadHalfSize, .055f, 0f, .012f));
                var rollerMesh = SaveMesh("WashRoller", Lathe("WashRoller", .40f, .09f, .036f, 16));
                var postMesh = SaveMesh("WashPost", Lathe("WashPost", .10f, .10f, .025f, 12));
                var station = SavePrefab(Mechanics, "WashStation_0", () =>
                    BuildStation(scene, padMesh, rollerMesh, postMesh, pad, roller, post, bubble));
                SavePreview(scene, splats, station, standard);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Debug.Log($"014-c: đã tạo hoặc kiểm tra asset rửa xe và ảnh {PreviewPath}.");
        }

        private static void AddSplat(MeshBuilder mesh, Splat splat, float phase)
        {
            int edge = mesh.Count;
            // Mỗi vệt có một mép dưới và một vai thu nhỏ, đủ để làm mượt mép mà vẫn thấy hình vẩy.
            for (int ring = 0; ring < 2; ring++)
            {
                for (int i = 0; i < SplatSides; i++)
                {
                    float angle = i * (2f * Mathf.PI / SplatSides);
                    float ripple = .83f + .12f * Mathf.Sin(3f * angle + phase) +
                                   .05f * Mathf.Cos(2f * angle - phase);
                    float inset = ring == 0 ? 1f : .66f;
                    mesh.Add(new Vector3(
                        splat.X + splat.RadiusX * ripple * inset * Mathf.Cos(angle),
                        ring == 0 ? 0f : splat.Height * (.62f + .13f * Mathf.Sin(angle + phase)),
                        splat.Z + splat.RadiusZ * ripple * inset * Mathf.Sin(angle)));
                }
            }

            int bottom = mesh.Add(new Vector3(splat.X, 0f, splat.Z));
            int top = mesh.Add(new Vector3(splat.X + .13f * splat.RadiusX, splat.Height, splat.Z));
            int opening = splat.Drip > 0 ? 1 : 5;
            for (int i = 0; i < SplatSides; i++)
            {
                int next = (i + 1) % SplatSides;
                if (splat.Drip == 0 || (i != opening && i != opening + 1))
                {
                    mesh.Quad(edge + i, edge + SplatSides + i, edge + SplatSides + next, edge + next);
                }
                mesh.Triangle(bottom, edge + i, edge + next);
                mesh.Triangle(top, edge + SplatSides + next, edge + SplatSides + i);
            }

            if (splat.Drip != 0)
            {
                AddDrip(mesh, splat, edge + opening);
            }
        }

        private static void AddDrip(MeshBuilder mesh, Splat splat, int first)
        {
            // Dùng chung sáu đỉnh quanh lỗ trên vai vệt, để giọt là một phần của cùng khối bùn.
            var previous = new[]
            {
                first, first + SplatSides, first + SplatSides + 1,
                first + SplatSides + 2, first + 2, first + 1
            };
            float radius = Mathf.Min(.025f, splat.RadiusX * .35f);
            // Trục ngang của vòng đi theo chiều mẫu ở hai mép đối nhau.
            float tangent = -splat.Drip;
            for (int ring = 0; ring < 3; ring++)
            {
                float y = ring == 0 ? -.018f : ring == 1 ? -.078f : -.109f;
                float z = ring == 0 ? .289f : ring == 1 ? .351f : .368f;
                float width = radius * (ring == 0 ? .82f : ring == 1 ? 1f : .76f);
                var current = new int[previous.Length];
                for (int i = 0; i < current.Length; i++)
                {
                    float angle = (210f - i * 60f) * Mathf.Deg2Rad;
                    float across = Mathf.Cos(angle) * width;
                    float thickness = Mathf.Sin(angle) * width * .73f;
                    current[i] = mesh.Add(new Vector3(splat.X + tangent * across,
                        y + (ring == 0 ? thickness * .7f : thickness * .27f),
                        splat.Drip * (z + thickness)));
                }
                for (int i = 0; i < current.Length; i++)
                {
                    int next = (i + 1) % current.Length;
                    mesh.Quad(previous[i], previous[next], current[next], current[i]);
                }
                previous = current;
            }

            int tip = mesh.Add(new Vector3(splat.X, -.124f, splat.Drip * .372f));
            for (int i = 0; i < previous.Length; i++)
            {
                mesh.Triangle(previous[i], previous[(i + 1) % previous.Length], tip);
            }
        }

        private static Mesh RoundedSlab(string name, float halfX, float halfZ, float radius, float bottom, float top)
        {
            const int pointsPerCorner = 5;
            const int count = 4 * pointsPerCorner;
            var mesh = new MeshBuilder();
            for (int ring = 0; ring < 2; ring++)
            {
                for (int i = 0; i < count; i++)
                {
                    int corner = i / pointsPerCorner;
                    float angle = (corner + (i % pointsPerCorner) / (pointsPerCorner - 1f)) * Mathf.PI * .5f;
                    float centerX = corner == 0 || corner == 3 ? halfX - radius : -halfX + radius;
                    float centerZ = corner < 2 ? halfZ - radius : -halfZ + radius;
                    float x = centerX + radius * Mathf.Cos(angle);
                    float z = centerZ + radius * Mathf.Sin(angle);
                    mesh.Add(new Vector3(x, ring == 0 ? bottom : top, z),
                        new Vector2(x / (2f * halfX) + .5f, z / (2f * halfZ) + .5f));
                }
            }
            int bottomCenter = mesh.Add(new Vector3(0f, bottom, 0f), new Vector2(.5f, .5f));
            int topCenter = mesh.Add(new Vector3(0f, top, 0f), new Vector2(.5f, .5f));
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                mesh.Quad(i, i + count, next + count, next);
                mesh.Triangle(bottomCenter, i, next);
                mesh.Triangle(topCenter, next + count, i + count);
            }
            return mesh.Build(name, false);
        }

        private static Mesh Lathe(string name, float halfLength, float radius, float bevel, int segments)
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
                    // Đỉnh đầu/cuối trùng tọa độ và normal, nhưng UV đi từ 0 đến 1 quanh chu vi.
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
            return mesh.Build(name, false);
        }

        private static GameObject BuildStation(Scene scene, Mesh padMesh, Mesh rollerMesh, Mesh postMesh,
            Material pad, Material roller, Material post, Material bubble)
        {
            var root = NewObject(scene, null, "WashStation_0");
            AddMesh(scene, root.transform, "Pad", padMesh, pad);
            var sides = new[] { "N", "S", "E", "W" };
            var outward = new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            for (int i = 0; i < sides.Length; i++)
            {
                var holder = NewObject(scene, root.transform, "Roller " + sides[i]).transform;
                holder.localPosition = outward[i] * .50f + Vector3.up * .12f;
                // N/S chạy dọc X ô, E/W dọc Z ô. Spin luôn xoay quanh trục X local của nó.
                holder.localRotation = i < 2 ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                AddMesh(scene, holder, "Spin", rollerMesh, roller);

                var spray = NewObject(scene, root.transform, "Spray " + sides[i], typeof(ParticleSystem));
                spray.transform.localPosition = outward[i] * .44f + Vector3.up * .20f;
                spray.transform.localRotation = Quaternion.LookRotation(-outward[i] + Vector3.up * .18f);
                ConfigureSpray(spray.GetComponent<ParticleSystem>(), bubble, (uint)(1401 + i));
            }
            foreach (int z in new[] { 1, -1 })
            {
                foreach (int x in new[] { 1, -1 })
                {
                    string corner = (z > 0 ? "N" : "S") + (x > 0 ? "E" : "W");
                    var pillar = AddMesh(scene, root.transform, "Post " + corner, postMesh, post).transform;
                    pillar.localPosition = new Vector3(x * .50f, .10f, z * .50f);
                    pillar.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
            return root;
        }

        private static void ConfigureSpray(ParticleSystem spray, Material bubble, uint seed)
        {
            spray.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            spray.useAutoRandomSeed = false;
            spray.randomSeed = seed;

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

            var renderer = spray.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            SetRenderer(renderer, bubble);
        }

        private static Color32 PadPixel(float u, float v)
        {
            var pixel = (Color)PadBlue;
            foreach (var circle in PadCircles)
            {
                float dx = u - circle.x;
                float dz = v - circle.y;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float edge = 1f - Mathf.SmoothStep(0f, 1f, (distance - circle.z + .006f) / .012f);
                float rim = Mathf.SmoothStep(0f, 1f, (distance - circle.z + .021f) / .012f);
                pixel = Color.Lerp(pixel, Color.Lerp(BubbleBlue, Color.white, rim), .84f * edge);
            }
            return pixel;
        }

        // Tọa độ UV và bán kính của các vòng bọt trên tấm lót.
        private static readonly Vector3[] PadCircles =
        {
            new Vector3(.18f, .22f, .075f), new Vector3(.36f, .28f, .039f), new Vector3(.25f, .08f, .037f),
            new Vector3(.74f, .19f, .11f), new Vector3(.90f, .29f, .039f), new Vector3(.57f, .34f, .053f),
            new Vector3(.13f, .51f, .052f), new Vector3(.28f, .66f, .108f), new Vector3(.43f, .52f, .068f),
            new Vector3(.69f, .58f, .083f), new Vector3(.89f, .62f, .055f), new Vector3(.82f, .80f, .096f),
            new Vector3(.18f, .87f, .063f), new Vector3(.53f, .86f, .095f), new Vector3(.64f, .75f, .035f)
        };

        private static Color32 RollerPixel(float u, float v)
        {
            // Một số nguyên chu kỳ theo V giữ đường sọc nối kín ở mép UV quanh chổi.
            float phase = Mathf.Repeat(4.8f * u + 2f * v, 1f);
            const float edge = .022f;
            Color pink = CandyPink;
            Color blue = CandyBlue;
            var color = Color.Lerp(blue, pink, Mathf.SmoothStep(0f, 1f, (phase + edge) / (2f * edge)));
            color = Color.Lerp(color, Color.white, Mathf.SmoothStep(0f, 1f, (phase - .39f + edge) / (2f * edge)));
            color = Color.Lerp(color, blue, Mathf.SmoothStep(0f, 1f, (phase - .60f + edge) / (2f * edge)));
            return Color.Lerp(color, Color.Lerp(blue, pink, .5f),
                Mathf.SmoothStep(0f, 1f, (phase - 1f + edge) / edge));
        }

        private static Texture2D SaveTexture(string name, int size, Func<float, float, Color32> pixel, TextureWrapMode wrapV)
        {
            string path = Art + "/Textures/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        pixels[y * size + x] = pixel((x + .5f) / size, (y + .5f) / size);
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply(false);
                if (WriteIfDifferent(path, texture.EncodeToPNG()) || AssetImporter.GetAtPath(path) == null)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Default ||
                importer.alphaSource != TextureImporterAlphaSource.None || !importer.sRGBTexture || importer.mipmapEnabled ||
                importer.wrapModeU != TextureWrapMode.Clamp || importer.wrapModeV != wrapV ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.filterMode != FilterMode.Bilinear)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = wrapV;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material LoadMaterial(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/" + name + ".mat");
            if (material == null)
            {
                throw new InvalidOperationException($"Thiếu material dùng chung {name}.");
            }
            return material;
        }

        private static Material SaveMaterial(string name, Shader shader, Texture2D map, Color color, float smoothness)
        {
            string path = Art + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(shader) { name = name, mainTexture = map };
            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh SaveMesh(string name, Mesh generated)
        {
            string path = Art + "/Meshes/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            try
            {
                if (!saved.vertices.SequenceEqual(generated.vertices) || !saved.normals.SequenceEqual(generated.normals) ||
                    !saved.uv.SequenceEqual(generated.uv) || !saved.triangles.SequenceEqual(generated.triangles) ||
                    !saved.bounds.Equals(generated.bounds))
                {
                    EditorUtility.CopySerialized(generated, saved);
                    EditorUtility.SetDirty(saved);
                    AssetDatabase.SaveAssetIfDirty(saved);
                }
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(generated);
            }
        }

        private static GameObject SavePrefab(string folder, string name, Func<GameObject> build)
        {
            string path = folder + "/" + name + ".prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            // Giữ các tinh chỉnh Inspector và component Claude gắn sau lần tạo đầu.
            if (saved != null)
            {
                return saved;
            }

            var root = build();
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
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

        private static GameObject AddMesh(Scene scene, Transform parent, string name, Mesh mesh, Material material)
        {
            var item = NewObject(scene, parent, name, typeof(MeshFilter), typeof(MeshRenderer));
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            SetRenderer(item.GetComponent<MeshRenderer>(), material);
            return item;
        }

        private static void SetRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void SavePreview(Scene scene, GameObject[] splats, GameObject station, Shader standard)
        {
            const int width = 960;
            const int height = 300;
            var roofMesh = RoundedSlab("Roof Preview", .50f, RoofHalfDepth, .02f, -.16f, 0f);
            var carMesh = RoundedSlab("Car Preview", .80f, .39f, .03f, .015f, .22f);
            var pink = new Material(standard) { color = new Color32(232, 74, 140, 255) };
            var blue = new Material(standard) { color = CandyBlue };
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var preview = new Texture2D(width, 3 * height, TextureFormat.RGB24, false);
            var camera = NewObject(scene, null, "Wash Preview Camera", typeof(Camera)).GetComponent<Camera>();
            try
            {
                var light = NewObject(scene, null, "Wash Preview Light", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(55f, -28f, 0f);
                var fill = NewObject(scene, null, "Wash Preview Fill", typeof(Light)).GetComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = .60f;
                fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);

                var roofs = NewObject(scene, null, "Splat Previews");
                for (int i = 0; i < splats.Length; i++)
                {
                    var center = new Vector3((i - 1) * 1.19f, 0f, 0f);
                    AddMesh(scene, roofs.transform, "Pink Roof", roofMesh, pink).transform.localPosition = center;
                    var splat = (GameObject)PrefabUtility.InstantiatePrefab(splats[i], scene);
                    splat.transform.SetParent(roofs.transform, false);
                    splat.transform.localPosition = center + Vector3.up * .001f;
                }

                camera.cameraType = CameraType.Preview;
                camera.scene = scene;
                camera.enabled = false;
                camera.orthographic = true;
                camera.aspect = (float)width / height;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(58, 63, 75, 255);
                camera.allowHDR = false;
                camera.targetTexture = target;

                RenderPanel(camera, target, preview, 90f, .58f, 2 * height);
                RenderPanel(camera, target, preview, 70f, .58f, height);
                roofs.SetActive(false);

                var empty = (GameObject)PrefabUtility.InstantiatePrefab(station, scene);
                empty.transform.position = Vector3.left * .92f;
                var occupied = (GameObject)PrefabUtility.InstantiatePrefab(station, scene);
                occupied.transform.position = Vector3.right * .92f;
                AddMesh(scene, null, "Blue Car", carMesh, blue).transform.position = occupied.transform.position;
                RenderPanel(camera, target, preview, 70f, .66f, 0);

                Directory.CreateDirectory("Logs");
                WriteIfDifferent(PreviewPath, preview.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(preview);
                Object.DestroyImmediate(roofMesh);
                Object.DestroyImmediate(carMesh);
                Object.DestroyImmediate(pink);
                Object.DestroyImmediate(blue);
            }
        }

        private static void RenderPanel(Camera camera, RenderTexture target, Texture2D preview, float tilt, float size, int y)
        {
            var rotation = Quaternion.Euler(tilt, 0f, 0f);
            camera.transform.SetPositionAndRotation(rotation * Vector3.back * 4f, rotation);
            camera.orthographicSize = size;
            camera.Render();
            RenderTexture.active = target;
            preview.ReadPixels(new Rect(0, 0, target.width, target.height), 0, y, false);
        }

        private static bool WriteIfDifferent(string path, byte[] bytes)
        {
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes))
            {
                return false;
            }
            File.WriteAllBytes(path, bytes);
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private readonly struct Splat
        {
            public readonly float X, Z, RadiusX, RadiusZ, Height;
            public readonly int Drip;

            public Splat(float x, float z, float radiusX, float radiusZ, float height, int drip = 0)
            {
                X = x;
                Z = z;
                // 016-f: từng mảng bùn to và dày hơn bố cục gốc để đọc rõ trên điện thoại.
                RadiusX = radiusX * 1.6f;
                RadiusZ = radiusZ * 1.6f;
                Height = height * 1.45f;
                Drip = drip;
            }
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            public int Count => vertices.Count;

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

            public Mesh Build(string name, bool roofUv)
            {
                if (roofUv)
                {
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        var vertex = vertices[i];
                        uv[i] = new Vector2(vertex.x + .5f, vertex.z / (2f * RoofHalfDepth) + .5f);
                    }
                }

                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(triangles, 0);
                if (normals[0] == Vector3.zero)
                {
                    mesh.RecalculateNormals();
                }
                else
                {
                    mesh.SetNormals(normals);
                }
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
