using System;
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
    public static class CarVfxTools
    {
        private const string ArtFolder = "Assets/_Project/Art/VFX";
        private const string PrefabFolder = "Assets/_Project/Prefabs/VFX";
        private const string PreviewPath = "Logs/014-car-vfx-preview.png";
        private const string MudPreviewPath = "Logs/014-mud-preview.png";
        private const string MudBlobName = "MudBlob";
        private const string ShaderName = "Legacy Shaders/Particles/Alpha Blended";
        private const string Tint = "_TintColor";
        private const int Width = 64;
        private const int TrailHeight = 32;
        private const int BeamHeight = 128;
        private const float BeamOffset = .22f;
        private const float HeadlightLength = .90f;
        private const float HeadlightWidth = .30f;
        private const float TaillightLength = .45f;
        private const float TaillightWidth = .26f;

        [MenuItem("Tools/Squeaky Lane/014 VFX/Create Car VFX Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi tạo asset VFX xe.");
            }

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Không tìm thấy shader {ShaderName}.");
            }
            var standard = Shader.Find("Standard");
            if (standard == null)
            {
                throw new InvalidOperationException("Không tìm thấy shader Standard cho cục bùn.");
            }

            EnsureFolder(ArtFolder + "/Textures");
            EnsureFolder(ArtFolder + "/Materials");
            EnsureFolder(ArtFolder + "/Meshes");
            EnsureFolder(PrefabFolder);

            var trailTexture = SaveTexture("tire_trail", TrailHeight, Width, TextureWrapMode.Repeat, TrailAlpha);
            var beamTexture = SaveTexture("headlight_beam", BeamHeight, BeamHeight, TextureWrapMode.Clamp, BeamAlpha);
            var trail = SaveMaterial("M_TireTrail", shader, trailTexture, new Color(.5f, .5f, .5f, .5f));
            // Claude 26/11: .55/.50 → .85/.80 sau ảnh Play: trên nhựa đường tối tia đèn gần như không thấy.
            var head = SaveMaterial("M_Headlight", shader, beamTexture, BeamTint(new Color32(255, 243, 196, 255), .85f));
            var tail = SaveMaterial("M_Taillight", shader, beamTexture, BeamTint(new Color32(255, 74, 61, 255), .80f));
            var mudTrail = SaveMaterial("M_MudTrail", shader, trailTexture, BeamTint(new Color32(150, 98, 47, 255), .95f)); // Claude 26/11: #6B4423/.75 chìm trên nhựa đường #3A3F4B → sáng hơn, đậm hơn
            var mudMaterial = SaveMudBlobMaterial(standard);
            var mudMesh = SaveMudBlobMesh();
            var quad = SaveBeamQuad();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var headPrefab = SavePrefab(scene, "HeadlightBeams", quad, head, HeadlightLength, HeadlightWidth);
                var tailPrefab = SavePrefab(scene, "TaillightBeams", quad, tail, TaillightLength, TaillightWidth);
                var mudPrefab = SaveMudBlobPrefab(scene, mudMesh, mudMaterial);
                SavePreview(scene, quad, trail, headPrefab, tailPrefab);
                SaveMudPreview(scene, quad, mudTrail, mudPrefab, standard);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Debug.Log($"014 Car VFX: đã tạo hoặc xác nhận texture, material, mesh, prefab và ảnh {PreviewPath}, {MudPreviewPath}.");
        }

        private static float TrailAlpha(int x, int y)
        {
            float v = y / (TrailHeight - 1f);
            float across = Mathf.SmoothStep(1f, 0f, Mathf.Abs(2f * v - 1f));
            // Hai dao động tuần hoàn với pha cố định. Cột đầu/cuối dùng cùng pha.
            float phase = x % (Width - 1) * (2f * Mathf.PI / (Width - 1));
            float ripple = .11f * Mathf.Sin(2f * phase + 2f * v)
                         + .04f * Mathf.Sin(5f * phase - 3f * v + .7f);
            return .85f * across * (1f + ripple);
        }

        private static float BeamAlpha(int x, int y)
        {
            float v = y / (BeamHeight - 1f);
            float edge = Width * .5f * Mathf.Lerp(.30f, .95f, v);
            float distance = Mathf.Abs(x - (Width - 1f) * .5f);
            float softness = Mathf.SmoothStep(0f, 1f, (edge - distance) / 3.5f);
            return .90f * Mathf.SmoothStep(1f, 0f, v) * softness;
        }

        private static Texture2D SaveTexture(string name, int height, int maxSize, TextureWrapMode wrapU,
            Func<int, int, float> alpha)
        {
            string path = ArtFolder + "/Textures/" + name + ".png";
            var pixels = new Color32[Width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    pixels[y * Width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * alpha(x, y)));
                }
            }

            var texture = new Texture2D(Width, height, TextureFormat.RGBA32, false);
            try
            {
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
                importer.alphaSource != TextureImporterAlphaSource.FromInput || !importer.alphaIsTransparency ||
                importer.mipmapEnabled || importer.filterMode != FilterMode.Bilinear ||
                importer.wrapModeU != wrapU || importer.wrapModeV != TextureWrapMode.Clamp ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.maxTextureSize != maxSize || importer.isReadable || !importer.sRGBTexture)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapModeU = wrapU;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = maxSize;
                importer.isReadable = false;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color BeamTint(Color displayColor, float opacity)
        {
            // Shader nhân cả RGB và alpha với 2. Color của material được lưu theo sRGB.
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            Color tint = linear ? (displayColor.linear * .5f).gamma : displayColor * .5f;
            tint.a = opacity * .5f;
            return tint;
        }

        private static Material SaveMaterial(string name, Shader shader, Texture2D texture, Color tint)
        {
            string path = ArtFolder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                material.mainTexture = texture;
                material.SetColor(Tint, tint);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader || material.mainTexture != texture || material.GetColor(Tint) != tint ||
                     material.mainTextureScale != Vector2.one || material.mainTextureOffset != Vector2.zero)
            {
                material.shader = shader;
                material.mainTexture = texture;
                material.mainTextureScale = Vector2.one;
                material.mainTextureOffset = Vector2.zero;
                material.SetColor(Tint, tint);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            return material;
        }

        private static Material SaveMudBlobMaterial(Shader shader)
        {
            const string name = "M_MudBlob";
            string path = ArtFolder + "/Materials/" + name + ".mat";
            Color mudColor = new Color32(90, 52, 24, 255);
            const float smoothness = .65f;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew)
            {
                material = new Material(shader) { name = name };
            }
            else if (material.shader == shader && material.color.Equals(mudColor) && material.mainTexture == null &&
                     material.GetFloat("_Glossiness") == smoothness && material.GetFloat("_Metallic") == 0f &&
                     material.GetFloat("_Mode") == 0f && material.GetFloat("_ZWrite") == 1f &&
                     material.GetFloat("_SrcBlend") == (float)BlendMode.One &&
                     material.GetFloat("_DstBlend") == (float)BlendMode.Zero &&
                     material.renderQueue == (int)RenderQueue.Geometry && material.GetTag("RenderType", false) == "Opaque" &&
                     !material.IsKeywordEnabled("_ALPHATEST_ON") && !material.IsKeywordEnabled("_ALPHABLEND_ON") &&
                     !material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
            {
                return material;
            }

            material.shader = shader;
            material.color = mudColor;
            material.mainTexture = null;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Mode", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            if (isNew)
            {
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            return material;
        }

        private static Mesh SaveMudBlobMesh()
        {
            string path = ArtFolder + "/Meshes/" + MudBlobName + ".asset";
            BuildMudGeometry(out var vertices, out var triangles);
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                uv[i] = new Vector2(vertices[i].x / 1.4f + .5f, vertices[i].z / 1.4f + .5f);
            }

            var generated = new Mesh { name = MudBlobName };
            try
            {
                generated.vertices = vertices;
                generated.uv = uv;
                generated.triangles = triangles;
                generated.RecalculateNormals();
                generated.RecalculateBounds();

                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved == null)
                {
                    AssetDatabase.CreateAsset(generated, path);
                    saved = generated;
                    generated = null;
                }
                else if (!saved.vertices.SequenceEqual(vertices) || !saved.uv.SequenceEqual(uv) ||
                         !saved.triangles.SequenceEqual(triangles) || !saved.normals.SequenceEqual(generated.normals) ||
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
                if (generated != null)
                {
                    Object.DestroyImmediate(generated);
                }
            }
        }

        private static void BuildMudGeometry(out Vector3[] vertices, out int[] triangles)
        {
            const int segments = 48;
            // Từng vòng đi từ đáy lên đỉnh. Hai hệ số cuối kéo bán kính và hạ giọt ở vòng đó.
            var rings = new (float radius, float height, float reach, float drop)[]
            {
                (.58f, .000f, .00f, .00f),
                (.86f, .003f, .65f, .88f),
                (1.0f, .021f, 1.0f, 1.0f),
                (.94f, .052f, .96f, .64f),
                (.77f, .084f, .51f, .25f),
                (.51f, .108f, .16f, .07f),
                (.26f, .122f, .00f, .00f)
            };
            // ±Z và +X, với bề rộng và tầm vươn khác nhau để viền bùn không đối xứng.
            var drips = new (int segment, float width, float tipRadius, float drop)[]
            {
                (12, .44f, .66f, .162f),
                (36, .37f, .63f, .149f),
                (0, .39f, .66f, .153f)
            };
            int top = segments * rings.Length + 1;
            vertices = new Vector3[top + 1];
            triangles = new int[segments * rings.Length * 6];
            vertices[0] = Vector3.zero;
            vertices[top] = new Vector3(-.018f, .129f, .012f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                // Các sóng có pha cố định, cho cùng hình mỗi lần chạy và liền tại điểm khép vòng.
                float radius = .451f + .018f * Mathf.Cos(3f * angle + .6f)
                    + .016f * Mathf.Sin(5f * angle - .4f) + .013f * Mathf.Cos(8f * angle + .2f);
                float ripple = .004f * Mathf.Sin(4f * angle) + .003f * Mathf.Cos(7f * angle + .8f);
                float reach = 0f;
                float drop = 0f;
                foreach (var drip in drips)
                {
                    int distance = Mathf.Abs(i - drip.segment);
                    distance = Mathf.Min(distance, segments - distance);
                    float strength = Mathf.SmoothStep(1f, 0f, distance * (2f * Mathf.PI / segments) / drip.width);
                    reach += (drip.tipRadius - radius) * strength;
                    drop += drip.drop * strength;
                }
                for (int ring = 0; ring < rings.Length; ring++)
                {
                    var profile = rings[ring];
                    float r = radius * profile.radius + reach * profile.reach;
                    float y = Mathf.Max(0f, profile.height + ripple * profile.height / .129f) - drop * profile.drop;
                    vertices[1 + ring * segments + i] = new Vector3(r * x, y, r * z);
                }
            }

            int index = 0;
            for (int i = 0; i < segments; i++)
            {
                int current = 1 + i;
                int next = 1 + (i + 1) % segments;
                triangles[index++] = 0;
                triangles[index++] = current;
                triangles[index++] = next;
                for (int ring = 1; ring < rings.Length; ring++)
                {
                    int upperCurrent = current + segments;
                    int upperNext = next + segments;
                    triangles[index++] = current;
                    triangles[index++] = upperCurrent;
                    triangles[index++] = next;
                    triangles[index++] = next;
                    triangles[index++] = upperCurrent;
                    triangles[index++] = upperNext;
                    current = upperCurrent;
                    next = upperNext;
                }
                triangles[index++] = current;
                triangles[index++] = top;
                triangles[index++] = next;
            }
        }

        private static Mesh SaveBeamQuad()
        {
            string path = ArtFolder + "/Meshes/CarVfxQuad.asset";
            var vertices = new[]
            {
                new Vector3(-.5f, 0f, -.5f), new Vector3(-.5f, 0f, .5f),
                new Vector3(.5f, 0f, -.5f), new Vector3(.5f, 0f, .5f)
            };
            var uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            var triangles = new[] { 0, 1, 2, 2, 1, 3 };
            var normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            // Shader particle cần luồng vertex color, nên quad khai báo trắng ở cả bốn đỉnh.
            var white = new Color32(255, 255, 255, 255);
            var colors = new[] { white, white, white, white };
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew)
            {
                mesh = new Mesh { name = "CarVfxQuad" };
            }
            else if (mesh.vertices.SequenceEqual(vertices) && mesh.uv.SequenceEqual(uv) &&
                     mesh.triangles.SequenceEqual(triangles) && mesh.normals.SequenceEqual(normals) &&
                     mesh.colors32.SequenceEqual(colors))
            {
                return mesh;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
            if (isNew)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }
            return mesh;
        }

        private static GameObject SavePrefab(Scene scene, string name, Mesh quad, Material material,
            float length, float width)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved != null && saved.name == name && saved.activeSelf && saved.GetComponents<Component>().Length == 1 &&
                MatchesTransform(saved.transform, Vector3.zero, Vector3.one) && saved.transform.childCount == 2 &&
                MatchesBeam(saved.transform.GetChild(0), "Beam L", quad, material, length, width, BeamOffset) &&
                MatchesBeam(saved.transform.GetChild(1), "Beam R", quad, material, length, width, -BeamOffset))
            {
                return saved;
            }

            var root = CreatePreviewObject(scene, name);
            AddBeam(scene, root.transform, "Beam L", quad, material, length, width, BeamOffset);
            AddBeam(scene, root.transform, "Beam R", quad, material, length, width, -BeamOffset);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Lưu prefab thất bại: {path}.");
            }
            return prefab;
        }

        private static bool MatchesBeam(Transform child, string name, Mesh quad, Material material,
            float length, float width, float z)
        {
            if (child.name != name || !child.gameObject.activeSelf || child.childCount != 0 ||
                child.GetComponents<Component>().Length != 3 ||
                !MatchesTransform(child, new Vector3(length * .5f, 0f, z), new Vector3(length, 1f, width)))
            {
                return false;
            }
            return MatchesRenderer(child.gameObject, quad, material);
        }

        private static bool MatchesRenderer(GameObject target, Mesh mesh, Material material)
        {
            return target.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh == mesh &&
                   target.TryGetComponent<MeshRenderer>(out var renderer) && renderer.enabled &&
                   renderer.sharedMaterials.Length == 1 && renderer.sharedMaterial == material &&
                   renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows &&
                   renderer.lightProbeUsage == LightProbeUsage.Off &&
                   renderer.reflectionProbeUsage == ReflectionProbeUsage.Off;
        }

        private static GameObject SaveMudBlobPrefab(Scene scene, Mesh mesh, Material material)
        {
            string path = PrefabFolder + "/" + MudBlobName + ".prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved != null && saved.name == MudBlobName && saved.activeSelf && saved.transform.childCount == 0 &&
                saved.GetComponents<Component>().Length == 3 &&
                MatchesTransform(saved.transform, Vector3.zero, Vector3.one) && MatchesRenderer(saved, mesh, material))
            {
                return saved;
            }

            var root = CreatePreviewObject(scene, MudBlobName, typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                SetRenderer(root, mesh, material);
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

        private static bool MatchesTransform(Transform transform, Vector3 position, Vector3 scale)
        {
            return transform.localPosition == position && transform.localRotation == Quaternion.identity &&
                   transform.localScale == scale;
        }

        private static void AddBeam(Scene scene, Transform root, string name, Mesh quad, Material material,
            float length, float width, float z)
        {
            var beam = CreatePreviewObject(scene, name, typeof(MeshFilter), typeof(MeshRenderer));
            beam.transform.SetParent(root, false);
            // Quad có V=0 tại -X và tâm ở 0, nên dời nửa chiều dài để đáy chạm pivot root.
            beam.transform.localPosition = new Vector3(length * .5f, 0f, z);
            beam.transform.localScale = new Vector3(length, 1f, width);
            SetRenderer(beam, quad, material);
        }

        private static void SetRenderer(GameObject target, Mesh quad, Material material)
        {
            target.GetComponent<MeshFilter>().sharedMesh = quad;
            var renderer = target.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static GameObject CreatePreviewObject(Scene scene, string name, params Type[] components)
        {
            var target = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);
            SceneManager.MoveGameObjectToScene(target, scene);
            target.hideFlags = HideFlags.None;
            return target;
        }

        private static void SavePreview(Scene scene, Mesh quad, Material trail, GameObject head, GameObject tail)
        {
            const int previewWidth = 288;
            const int panelHeight = 160;
            AddPreviewRow(scene, quad, trail, head, tail, 115f, 64f);
            AddPreviewRow(scene, quad, trail, head, tail, 38f, 32f);
            var camera = CreatePreviewObject(scene, "014 Preview Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(previewWidth * .5f, 10f, panelHeight * .5f),
                Quaternion.LookRotation(Vector3.down, Vector3.forward));
            camera.cameraType = CameraType.Preview;
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = panelHeight * .5f;
            camera.aspect = (float)previewWidth / panelHeight;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(previewWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var preview = new Texture2D(previewWidth, panelHeight * 2, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderPanel(camera, target, preview, new Color32(154, 163, 174, 255), panelHeight);
                RenderPanel(camera, target, preview, new Color32(74, 80, 96, 255), 0);
                Directory.CreateDirectory("Logs");
                WriteIfDifferent(PreviewPath, preview.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(preview);
            }
        }

        private static void AddPreviewRow(Scene scene, Mesh quad, Material trail, GameObject head, GameObject tail,
            float row, float size)
        {
            var ribbon = CreatePreviewObject(scene, "Tire Trail Preview", typeof(MeshFilter), typeof(MeshRenderer));
            ribbon.transform.SetPositionAndRotation(new Vector3(48f, 0f, row), Quaternion.Euler(0f, 90f, 0f));
            ribbon.transform.localScale = new Vector3(size * .30f, 1f, size);
            SetRenderer(ribbon, quad, trail);

            var headPair = (GameObject)PrefabUtility.InstantiatePrefab(head, scene);
            var tailPair = (GameObject)PrefabUtility.InstantiatePrefab(tail, scene);
            // Giữ cùng tỷ lệ world giữa hai cặp đèn. Hàng nhỏ thu xuống một nửa.
            float scale = size / HeadlightLength;
            headPair.transform.position = new Vector3(144f - size * .5f, 0f, row);
            tailPair.transform.position = new Vector3(240f - scale * TaillightLength * .5f, 0f, row);
            headPair.transform.localScale = Vector3.one * scale;
            tailPair.transform.localScale = Vector3.one * scale;
        }

        private static void SaveMudPreview(Scene scene, Mesh quad, Material trail, GameObject mudPrefab, Shader standard)
        {
            const int width = 640;
            const int height = 360;
            var roofCenter = new Vector3(-.53f, 0f, 0f);
            var roofMaterial = new Material(standard) { color = new Color32(192, 57, 43, 255) };
            var roof = CreatePreviewObject(scene, "Red Roof", typeof(MeshFilter), typeof(MeshRenderer));
            roof.transform.position = roofCenter;
            roof.transform.localScale = new Vector3(.9f, 1f, .7f);
            SetRenderer(roof, quad, roofMaterial);
            for (int i = 0; i < 4; i++)
            {
                bool ends = i % 2 == 0;
                var yaw = Quaternion.Euler(0f, i * 90f, 0f);
                var side = CreatePreviewObject(scene, "Red Roof Side", typeof(MeshFilter), typeof(MeshRenderer));
                side.transform.SetPositionAndRotation(roofCenter + Vector3.down * .1f +
                    yaw * Vector3.forward * (ends ? .35f : .45f), yaw * Quaternion.Euler(90f, 0f, 0f));
                side.transform.localScale = new Vector3(ends ? .9f : .7f, 1f, .2f);
                SetRenderer(side, quad, roofMaterial);
            }

            var mud = (GameObject)PrefabUtility.InstantiatePrefab(mudPrefab, scene);
            mud.transform.position = roofCenter + Vector3.up * .002f;
            mud.transform.localScale = new Vector3(.69f, 1f, .56f);
            var ribbon = CreatePreviewObject(scene, "Mud Trail Preview", typeof(MeshFilter), typeof(MeshRenderer));
            ribbon.transform.SetPositionAndRotation(new Vector3(.53f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            ribbon.transform.localScale = new Vector3(.30f, 1f, .82f);
            SetRenderer(ribbon, quad, trail);

            var light = CreatePreviewObject(scene, "Mud Preview Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(52f, -24f, 0f);

            var camera = CreatePreviewObject(scene, "Mud Preview Camera", typeof(Camera)).GetComponent<Camera>();
            var angle = Quaternion.Euler(70f, 0f, 0f);
            camera.transform.SetPositionAndRotation(angle * Vector3.back * 4f, angle);
            camera.cameraType = CameraType.Preview;
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = .61f;
            camera.aspect = (float)width / height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var preview = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderPanel(camera, target, preview, new Color32(58, 63, 75, 255), 0);
                WriteIfDifferent(MudPreviewPath, preview.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(preview);
                Object.DestroyImmediate(roofMaterial);
            }
        }

        private static void RenderPanel(Camera camera, RenderTexture target, Texture2D preview, Color background, int y)
        {
            camera.backgroundColor = background;
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
    }
}
