using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SqueakyLane.Core.Model;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class SceneryTests
    {
        private readonly List<Object> owned = new List<Object>();
        private UiTheme theme;
        private Material material;
        private Mesh quad;

        [SetUp]
        public void Setup()
        {
            theme = Own(ScriptableObject.CreateInstance<UiTheme>());
            material = Own(new Material(Shader.Find("UI/Default")));
            quad = Own(new Mesh());
            quad.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) };
            quad.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            quad.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            quad.RecalculateBounds();
            var prefab = Own(new GameObject("Shadow template", typeof(ContactShadowView)));
            prefab.GetComponent<MeshFilter>().sharedMesh = quad;
            prefab.GetComponent<MeshRenderer>().sharedMaterial = material;
            Set(theme, "contactShadowPrefab", prefab);
        }
        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--) { if (owned[i] != null) { Object.DestroyImmediate(owned[i]); } }
            owned.Clear();
        }

        [Test]
        public void ShadowSharesResourcesAndStaysOnBoardWhileOwnerRotatesScalesAndModelBounces()
        {
            var board = Own(new GameObject("board"));
            board.transform.position = new Vector3(3, 2, -5);
            board.transform.rotation = Quaternion.Euler(0, 17, 0);
            var car = Own(new GameObject("car"));
            car.transform.SetParent(board.transform, false);
            car.transform.localPosition = new Vector3(1, .32f, 2);
            var model = Own(new GameObject("modelRoot"));
            model.transform.SetParent(car.transform, false);
            var shadow = ContactShadowView.Attach(car.transform, theme, new Vector2(1.9f, .9f), LotSurfaceView.Top);
            Assert.That(shadow.transform.parent, Is.EqualTo(car.transform));
            var filter = shadow.GetComponent<MeshFilter>();
            Assert.That(filter.sharedMesh, Is.SameAs(quad));
            Assert.That(shadow.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(material));
            foreach (float yaw in new[] { 0f, 45, 90, 270 })
            {
                car.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                car.transform.localScale = Vector3.one * .7f;
                model.transform.localPosition = Vector3.up * 3;
                shadow.Project();
                var point = board.transform.InverseTransformPoint(shadow.transform.position);
                Assert.That(point.x, Is.EqualTo(1 + theme.ContactShadowOffset.x).Within(.0001f));
                Assert.That(point.z, Is.EqualTo(2 + theme.ContactShadowOffset.y).Within(.0001f));
                Assert.That(point.y, Is.EqualTo(LotSurfaceView.Top + theme.ContactShadowLift).Within(.0001f));
            }
            car.SetActive(false);
            Assert.That(shadow.gameObject.activeInHierarchy, Is.False);
            car.SetActive(true);
            shadow.Project();
            Assert.That(shadow.gameObject.activeInHierarchy, Is.True);
            Assert.That(car.GetComponentsInChildren<ContactShadowView>(true).Length, Is.EqualTo(1));
            Assert.That(shadow.GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void TrayHasVerticalSidesHighlightOneSubmeshAndDoesNotChangeBoardTop()
        {
            Set(theme, "trayEnabled", true);
            Set(theme, "gateStrips", false);
            Set(theme, "trayCornerRadius", 0f); // khay vuông kiểu 013; khay bo góc 015 có test riêng
            var mesh = Own(SceneThemeView.BuildGround(new LevelDefBuilder(4, 6).Build(), theme));
            Assert.That(mesh.subMeshCount, Is.EqualTo(1));
            Assert.That(mesh.vertexCount, Is.EqualTo(40)); // cỏ, mặt khay, 4 mặt bên, 4 vạch sáng.
            Assert.That(mesh.vertices[4].y, Is.EqualTo(LotSurfaceView.Top - .02f).Within(.0001f));
            Assert.That(mesh.vertices[9].y - mesh.vertices[8].y, Is.EqualTo(theme.TrayThickness).Within(.0001f));
            Assert.That(mesh.normals[8].z, Is.LessThan(-.99f));
            Assert.That(mesh.uv[8].x, Is.EqualTo(9.5f / SceneThemeView.GroundColorCount).Within(.0001f));
            Assert.That(mesh.vertices[0].y, Is.EqualTo(SceneThemeView.GroundY(theme)));
        }

        // 015: khay bo góc vẫn một mesh/một submesh, mặt khay giữ cao độ, thành bên đủ dày, góc ngoài bị cắt tròn.
        [Test]
        public void RoundedTrayKeepsOneSubmeshHeightAndCutsCorners()
        {
            Set(theme, "trayEnabled", true);
            Set(theme, "gateStrips", false);
            Set(theme, "trayCornerRadius", .45f);
            var level = new LevelDefBuilder(4, 6).Build();
            var mesh = Own(SceneThemeView.BuildGround(level, theme));
            Assert.That(mesh.subMeshCount, Is.EqualTo(1));
            float halfX = 2 + theme.PavementWidth, halfZ = 3 + theme.PavementWidth, top = LotSurfaceView.Top - .02f;
            float minY = float.MaxValue, maxY = float.MinValue, cornerDistance = float.MaxValue;
            var vertices = mesh.vertices;
            for (int i = 4; i < vertices.Length; i++) // bỏ quad cỏ (4 đỉnh đầu)
            {
                var v = vertices[i];
                Assert.That(Mathf.Abs(v.x), Is.LessThanOrEqualTo(halfX + .0001f));
                Assert.That(Mathf.Abs(v.z), Is.LessThanOrEqualTo(halfZ + .0001f));
                minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
                cornerDistance = Mathf.Min(cornerDistance, Vector2.Distance(new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.z)), new Vector2(halfX, halfZ)));
            }
            Assert.That(maxY, Is.EqualTo(top + .001f).Within(.0001f), "Viền sáng nằm ngay trên mặt khay");
            Assert.That(maxY - minY, Is.EqualTo(theme.TrayThickness + .001f).Within(.0001f), "Thành bên đủ dày khay");
            float expected = .45f * (Mathf.Sqrt(2) - 1);
            Assert.That(cornerDistance, Is.EqualTo(expected).Within(.01f), "Góc ngoài cắt tròn theo bán kính");
            Assert.That(vertices[0].y, Is.EqualTo(SceneThemeView.GroundY(theme)));
        }

        // 015: thảm hầm rửa ở cổng nước kéo ra ~2 ô ngoài bãi; props không được mọc đè lên thảm.
        [Test]
        public void PropsSkipWashGatePads()
        {
            Set(theme, "trayEnabled", true);
            Set(theme, "southPropsEnabled", true);
            Set(theme, "gateStrips", false);
            var north = BoxTemplate("North", new Vector3(.6f, .8f, .6f));
            var south = BoxTemplate("South", new Vector3(.6f, .3f, .6f));
            Set(theme, "propPrefabs", new[] { north, north, north, north, north, north });
            Set(theme, "southPropPrefabs", new[] { south, south, south });
            Set(theme, "propCount", 6);
            var builder = new LevelDefBuilder(4, 6);
            builder.Gates.Add(new GateDef(GateEdge.Top, 1, TileColor.Water));
            builder.Gates.Add(new GateDef(GateEdge.Bottom, 2, TileColor.Water));
            var level = builder.Build();
            var root = Own(new GameObject("environment", typeof(SceneThemeView)));
            root.GetComponent<SceneThemeView>().Configure(level, theme);
            var topPad = Rect.MinMaxRect(-.5f - .65f, 3, -.5f + .65f, 5.2f);
            var bottomPad = Rect.MinMaxRect(.5f - .65f, -5.2f, .5f + .65f, -3);
            int props = 0;
            foreach (Transform child in root.transform)
            {
                if (child.name.Contains("Tray Shadow")) { continue; }
                props++;
                var b = SceneThemeView.LocalBounds(child.gameObject); b.center += child.localPosition;
                var footprint = Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
                Assert.That(footprint.Overlaps(topPad) || footprint.Overlaps(bottomPad), Is.False, child.name + " đè lên thảm hầm rửa");
            }
            Assert.That(props, Is.GreaterThan(0).And.LessThan(9), "Bỏ props đè thảm, vẫn giữ phần còn lại");
        }

        [Test]
        public void PropsRespectBoardGapAndBudgetWhenConfiguredTwice()
        {
            Set(theme, "trayEnabled", true);
            Set(theme, "southPropsEnabled", true);
            Set(theme, "gateStrips", false);
            var north = BoxTemplate("North", new Vector3(1.2f, 1.5f, .8f));
            var south = BoxTemplate("South", new Vector3(1.2f, .45f, .8f));
            Set(theme, "propPrefabs", new[] { north, north, north, north, north, north });
            Set(theme, "southPropPrefabs", new[] { south, south, south });
            Set(theme, "propCount", 6);
            var root = Own(new GameObject("environment", typeof(SceneThemeView)));
            var view = root.GetComponent<SceneThemeView>();
            var level = new LevelDefBuilder(4, 6).Build();
            for (int round = 0; round < 2; round++)
            {
                view.Configure(level, theme);
                Assert.That(root.transform.childCount, Is.EqualTo(10), "9 props +1 bóng khay, không nhân đôi khi cấu hình lại.");
                foreach (Transform child in root.transform)
                {
                    if (child.name == "013 Tray Shadow") { continue; }
                    var bounds = SceneThemeView.LocalBounds(child.gameObject);
                    if (child.name.StartsWith("South"))
                    {
                        Assert.That(child.localPosition.z + bounds.max.z, Is.LessThanOrEqualTo(-3 - theme.PropGap + .0001f));
                        Assert.That(bounds.size.y, Is.LessThanOrEqualTo(.5f));
                    }
                    else { Assert.That(child.localPosition.z + bounds.min.z, Is.GreaterThanOrEqualTo(3 + theme.PropGap - .0001f)); }
                }
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
        }

        [Test]
        public void AssetLabSwitchesSampleWithoutLeakingPreviewOrMutatingSourceMaterial()
        {
            var previous = SceneManager.GetActiveScene();
            // Scene phụ cho gọn. Scene đang mở là "Untitled" chưa lưu (Unity CLI/batch) thì Unity không cho tạo thêm: dùng luôn scene đó, đồ thử do Own() dọn.
            bool extraScene = !string.IsNullOrEmpty(previous.path);
            var scene = extraScene ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive) : previous;
            SceneManager.SetActiveScene(scene);
            try
            {
                var first = BoxTemplate("First", new Vector3(2, .6f, .8f));
                var second = BoxTemplate("Second", new Vector3(1, 1, 1));
                var config = Own(ScriptableObject.CreateInstance<AssetLabConfig>());
                Set(config, "samples", new[] { new AssetLabConfig.Sample { label = "first", prefab = first }, new AssetLabConfig.Sample { label = "second", prefab = second } });
                Set(config, "shadowPrefab", theme.ContactShadowPrefab);
                Set(config, "groundMaterial", material);
                var root = Own(new GameObject("Asset Lab", typeof(AssetLabView)));
                var view = root.GetComponent<AssetLabView>();
                Set(view, "config", config);
                view.RefreshPreview();
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                Assert.That(root.transform.GetChild(0).Find("First"), Is.Not.Null);
                Set(config, "selectedSample", 1);
                Set(config, "shadowAlpha", .1f);
                Set(config, "modelAngles", new Vector3(35, 30, 0));
                Set(config, "modelLift", .5f);
                typeof(AssetLabConfig).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(config, null);
                typeof(AssetLabView).GetMethod("EditorTick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                var generated = root.transform.GetChild(0);
                Assert.That(generated.Find("First"), Is.Null);
                var model = generated.Find("Second");
                Assert.That(model, Is.Not.Null);
                var shadow = model.GetChild(0).GetComponent<MeshRenderer>();
                Assert.That(Vector3.Dot(shadow.transform.up, Vector3.up), Is.GreaterThan(.999f), "Bóng nằm trên nền cả khi asset nghiêng.");
                Assert.That(shadow.transform.position.y, Is.EqualTo(config.ShadowLift).Within(.0001f));
                var block = new MaterialPropertyBlock();
                shadow.GetPropertyBlock(block);
                Assert.That(block.GetColor("_Color").a, Is.EqualTo(.1f).Within(.0001f));
                Assert.That(material.color, Is.EqualTo(Color.white));
                Assert.That(second.GetComponent<MeshRenderer>().shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
                foreach (Transform node in generated.GetComponentsInChildren<Transform>(true))
                { Assert.That((node.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0, Is.True); }
            }
            finally
            {
                if (extraScene)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid()) { SceneManager.SetActiveScene(previous); }
                }
            }
        }

        private GameObject BoxTemplate(string name, Vector3 size)
        {
            var root = Own(new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)));
            var mesh = Own(new Mesh());
            var vertices = new Vector3[8];
            for (int i = 0; i < 8; i++)
            { vertices[i] = new Vector3((i & 1) == 0 ? -size.x * .5f : size.x * .5f, (i & 2) == 0 ? 0 : size.y, (i & 4) == 0 ? -size.z * .5f : size.z * .5f); }
            mesh.vertices = vertices; mesh.RecalculateBounds();
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            root.GetComponent<MeshRenderer>().sharedMaterial = material;
            return root;
        }
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    }
}
