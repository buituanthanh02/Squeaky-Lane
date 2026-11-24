using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
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
