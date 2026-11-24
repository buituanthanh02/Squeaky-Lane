using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.EditorTools;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class CarCatalogIntegrationTests
    {
        private GameObject root;
        private BoardView board;
        private ActionAnimator animator;
        private VisualPalette palette;
        private FeelConfig feel;
        private CarCatalog catalog;
        private Material shared, pink, blue;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("010 test");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            catalog = ScriptableObject.CreateInstance<CarCatalog>();
            shared = new Material(Shader.Find("Standard"));
            pink = new Material(Shader.Find("Standard"));
            blue = new Material(Shader.Find("Standard"));
            typeof(VisualPalette).GetField("material", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(palette, shared);
            var entries = new[]
            {
                new CarCatalog.Entry { color = TileColor.Pink, carPrefab = Prefab("Pink", pink), material = pink },
                new CarCatalog.Entry { color = TileColor.Blue, carPrefab = Prefab("Blue", blue), material = blue }
            };
            typeof(CarCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog, entries);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(shared);
            Object.DestroyImmediate(pink);
            Object.DestroyImmediate(blue);
        }

        [Test]
        public void PaintSwapsShapeAndSharedMaterialWithoutReplacingCarAndUndoRestoresBoth()
        {
            var builder = new LevelDefBuilder(6, 2);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            builder.Paints.Add(new PaintDef(0, 2, TileColor.Blue));
            var session = new GameSession(builder.Build());
            board.Build(session.Level, session.State, palette, catalog);
            animator.Initialize(board, feel, session.State);
            var view = board.Car(0);
            var collider = view.GetComponent<BoxCollider>();
            Vector3 initialPosition = view.transform.localPosition;
            Assert.That(Model(view).GetComponent<Renderer>().sharedMaterial, Is.SameAs(pink));
            var command = new SolverMove(SolverMoveKind.MoveCar, 0, 1);
            var before = session.State;
            var result = session.Apply(command);
            animator.Enqueue(command, before, result);
            for (int i = 0; i < 200 && Model(view).GetComponent<Renderer>().sharedMaterial != blue; i++)
            {
                animator.Advance(.01f);
            }
            Assert.That(animator.IsBusy, Is.True, "Cần đổi dáng ngay trong lượt đang chạy, không đợi lượt kết thúc.");
            Assert.That(board.Car(0), Is.SameAs(view));
            Assert.That(Model(view).name, Does.StartWith("Blue"));
            Assert.That(Model(view).GetComponent<Renderer>().sharedMaterial, Is.SameAs(blue));
            Assert.That(view.GetComponent<BoxCollider>(), Is.SameAs(collider));
            Assert.That(view.Index, Is.Zero);
            Assert.That(session.Undo(), Is.True);
            animator.CancelAndSnap(session.State);
            Assert.That(board.Car(0), Is.SameAs(view));
            Assert.That(Model(view).name, Does.StartWith("Pink"));
            Assert.That(Model(view).GetComponent<Renderer>().sharedMaterial, Is.SameAs(pink));
            Assert.That(view.transform.localPosition, Is.EqualTo(initialPosition));
            Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void MissingCatalogEntriesUseBlocksAndWarnOnlyOnceAcrossLevelReloads()
        {
            var empty = ScriptableObject.CreateInstance<CarCatalog>();
            try
            {
                var builder = new LevelDefBuilder(5, 2);
                builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
                builder.Cars.Add(new CarDef(Axis.Horizontal, 1, 0, TileColor.Blue));
                var level = builder.Build();
                var state = GameState.Initial(level);
                LogAssert.Expect(LogType.Warning, new Regex("CarCatalog thiếu prefab/material hợp lệ"));
                board.Build(level, state, palette, empty);
                Assert.That(board.Car(0).GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(board.Car(1).GetComponent<BoxCollider>(), Is.Not.Null);
                board.Build(level, state, palette, empty);
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.DestroyImmediate(empty); }
        }

        [Test]
        public void ColormapChangesBodySlotsButPreservesGlassWheelsAndLightsAndSource()
        {
            var original = new Color32[32];
            var unchanged = new Color32(11, 22, 33, 255);
            for (int i = 0; i < original.Length; i++) original[i] = unchanged;
            var tint = new Color32(255, 77, 145, 255);
            var result = KenneyCarsWindow.RecolorBody(original, 8, 4, tint);
            Assert.That(result[2 * 8 + 3], Is.EqualTo(tint));
            Assert.That(result[2 * 8 + 4], Is.EqualTo(tint));
            Assert.That(result[2 * 8 + 6], Is.EqualTo(tint));
            Assert.That(result[2 * 8 + 7], Is.EqualTo(tint));
            Assert.That(result[1 * 8 + 3], Is.EqualTo(unchanged), "kính");
            Assert.That(result[1 * 8 + 2], Is.EqualTo(unchanged), "bánh");
            Assert.That(result[1 * 8 + 6], Is.EqualTo(unchanged), "đèn");
            Assert.That(original[2 * 8 + 3], Is.EqualTo(unchanged), "không sửa texture nguồn");
        }

        [TestCase(2, 4f, 1f)]
        [TestCase(2, 2f, 2f)]
        [TestCase(3, 4f, 2f)]
        [TestCase(3, 8f, 1f)]
        public void WrapperPreservesAspectCentersOffOriginModelAndKeepsColliderOnGrid(int length, float sourceLength, float sourceWidth)
        {
            var visual = new GameObject("Visual lệch pivot").transform;
            visual.SetParent(root.transform, false);
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(visual, false);
            model.transform.localPosition = new Vector3(1.2f, 2.1f, -.4f);
            model.transform.localScale = new Vector3(sourceLength, 2, sourceWidth);
            var renderer = model.GetComponent<Renderer>();
            var before = renderer.bounds;
            const float bottom = -.33f;
            var footprint = KenneyCarsWindow.FitVisualToFootprint(visual, before, length, .18f, .78f, bottom);
            var after = renderer.bounds;
            Assert.That(visual.localScale.x, Is.EqualTo(visual.localScale.y));
            Assert.That(visual.localScale.x, Is.EqualTo(visual.localScale.z));
            Assert.That(after.size.x / after.size.z, Is.EqualTo(before.size.x / before.size.z).Within(.0001));
            Assert.That(after.center.x, Is.EqualTo(0).Within(.0001));
            Assert.That(after.center.z, Is.EqualTo(0).Within(.0001));
            Assert.That(after.min.y, Is.EqualTo(bottom).Within(.0001));
            Assert.That(after.size.x, Is.LessThanOrEqualTo(length - .18f + .0001f));
            Assert.That(after.size.z, Is.LessThanOrEqualTo(.7801f));
            Assert.That(footprint.size.x, Is.EqualTo(length - .18f));
            Assert.That(footprint.size.z, Is.EqualTo(.78f));
            Assert.That(footprint.size.y, Is.EqualTo(after.size.y).Within(.0001));
            Assert.That(footprint.min.y, Is.EqualTo(bottom).Within(.0001));
            Assert.That(after.min.x, Is.GreaterThanOrEqualTo(footprint.min.x - .0001f));
            Assert.That(after.max.x, Is.LessThanOrEqualTo(footprint.max.x + .0001f));
        }

        [Test]
        public void ExtendedTruckFillsThreeCellsButKeepsCabinWheelShapesAndOriginalMesh()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Cars/Kenney/truck.fbx");
            Assert.That(source, Is.Not.Null);
            var visual = new GameObject("Truck Visual").transform;
            visual.SetParent(root.transform, false);
            var model = Object.Instantiate(source, visual);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, 90, 0);
            model.transform.localScale = Vector3.one;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            var footprint = KenneyCarsWindow.FitVisualToFootprint(visual, bounds, 3, .18f, .78f, -.33f);
            var filters = visual.GetComponentsInChildren<MeshFilter>();
            var body = filters.Single(filter => filter.name == "body");
            Mesh original = body.sharedMesh;
            var originalVertices = original.vertices;
            var bodyBefore = body.GetComponent<Renderer>().bounds;
            float extra = footprint.size.x - bodyBefore.size.x;
            var wheels = filters.Where(filter => filter.name.StartsWith("wheel-")).ToArray();
            var wheelMeshes = wheels.Select(filter => filter.sharedMesh).ToArray();
            var wheelBounds = wheels.Select(filter => filter.GetComponent<Renderer>().bounds).ToArray();
            var bodyToRoot = root.transform.worldToLocalMatrix * body.transform.localToWorldMatrix;
            float cut = root.transform.InverseTransformPoint(model.transform.TransformPoint(new Vector3(0, 0, -.30f))).x;
            Mesh generated = KenneyCarsWindow.LengthenTruckBed(visual, footprint.size.x);
            try
            {
                var after = body.GetComponent<Renderer>().bounds;
                foreach (var wheel in wheels)
                {
                    after.Encapsulate(wheel.GetComponent<Renderer>().bounds);
                }
                Assert.That(after.size.x, Is.EqualTo(2.82f).Within(.0001f));
                Assert.That(after.size.z, Is.EqualTo(.78f).Within(.0001f));
                Assert.That(after.min.y, Is.EqualTo(-.33f).Within(.0001f));
                Assert.That(after.center.x, Is.EqualTo(0).Within(.0001f));
                Assert.That(after.center.z, Is.EqualTo(0).Within(.0001f));
                Assert.That(after.size.y, Is.EqualTo(footprint.size.y).Within(.0001f));
                Assert.That(visual.localScale.x, Is.EqualTo(visual.localScale.y));
                Assert.That(visual.localScale.x, Is.EqualTo(visual.localScale.z));
                for (int i = 0; i < wheels.Length; i++)
                {
                    var wheelAfter = wheels[i].GetComponent<Renderer>().bounds;
                    Assert.That(wheels[i].sharedMesh, Is.SameAs(wheelMeshes[i]), "Bánh dùng mesh nguồn, không giãn.");
                    Assert.That(Vector3.Distance(wheelAfter.size, wheelBounds[i].size), Is.LessThan(.0001f));
                    float shift = wheelBounds[i].center.x < cut ? -extra * .5f : extra * .5f;
                    Assert.That(Vector3.Distance(wheelAfter.center, wheelBounds[i].center + Vector3.right * shift), Is.LessThan(.0001f));
                }
                var output = generated.vertices.Select(bodyToRoot.MultiplyPoint3x4).ToArray();
                foreach (var vertex in originalVertices)
                {
                    var expected = bodyToRoot.MultiplyPoint3x4(vertex);
                    expected.x += expected.x <= cut ? -extra * .5f : extra * .5f;
                    Assert.That(output.Any(point => Vector3.SqrMagnitude(point - expected) < 1e-8f), Is.True,
                        "Mỗi đỉnh cabin/đuôi/vòm bánh chỉ được tịnh tiến, không bị co giãn.");
                }
                CollectionAssert.AreEqual(originalVertices, original.vertices, "Không sửa mesh FBX nguồn.");
            }
            finally
            {
                Object.DestroyImmediate(generated);
            }
        }

        [Test]
        public void BedCutCreatesContinuousSurfaceWithCorrectWindingAndPaletteUV()
        {
            var source = new Mesh
            {
                vertices = new[] { new Vector3(-1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, -1) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                normals = Enumerable.Repeat(Vector3.up, 4).ToArray(),
                uv = Enumerable.Repeat(new Vector2(.45f, .65f), 4).ToArray()
            };
            Mesh generated = KenneyTruckMesh.ExtendBed(source, Matrix4x4.identity, -.3f, 2);
            try
            {
                float area = 0;
                var positions = generated.vertices;
                var indices = generated.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 cross = Vector3.Cross(positions[indices[i + 1]] - positions[indices[i]], positions[indices[i + 2]] - positions[indices[i]]);
                    Assert.That(cross.y, Is.GreaterThan(0), "Mặt thùng nối vẫn nhìn từ trên, không đảo winding.");
                    area += cross.magnitude * .5f;
                }
                Assert.That(area, Is.EqualTo(8).Within(.0001f), "Diện tích bằng mặt gốc cộng đoạn chèn, không hở/chồng mặt.");
                Assert.That(generated.bounds.size.x, Is.EqualTo(4).Within(.0001f));
                foreach (var uv in generated.uv)
                {
                    Assert.That(Vector2.Distance(uv, new Vector2(.45f, .65f)), Is.LessThan(.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(generated);
                Object.DestroyImmediate(source);
            }
        }

        private GameObject Prefab(string label, Material material)
        {
            var prefab = new GameObject(label + " template");
            prefab.transform.SetParent(root.transform, false);
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = label + " model";
            model.transform.SetParent(prefab.transform, false);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().sharedMaterial = material;
            var dirt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirt.transform.SetParent(prefab.transform, false);
            Object.DestroyImmediate(dirt.GetComponent<Collider>());
            dirt.GetComponent<Renderer>().sharedMaterial = shared;
            var collider = prefab.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.82f, .5f, .78f);
            prefab.AddComponent<CarView>().ConfigurePrefab(model.transform, dirt.GetComponent<Renderer>(), 2);
            prefab.SetActive(false);
            return prefab;
        }

        private static Transform Model(CarView view)
        {
            return (Transform)typeof(CarView).GetField("modelRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        }
    }
}
