using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 016: có phố quanh bãi thì xe ra cổng chạy tới tim phố, cua 90° về phía lối ra gần, rồi chạy khuất; không có phố thì như cũ.
    public sealed class StreetExit016Tests
    {
        private GameObject root, environment;
        private BoardView board;
        private ActionAnimator animator;
        private FeelConfig feel;
        private VisualPalette palette;
        private UiTheme theme;
        private Material block;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("016 street test");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            theme = ScriptableObject.CreateInstance<UiTheme>();
            block = new Material(Shader.Find("Standard"));
            Set(palette, "material", block);
            environment = new GameObject("environment", typeof(SceneThemeView));
            Set(theme, "environmentPrefab", environment);
            Set(theme, "groundMaterial", block);
            Set(theme, "roadMaterial", block);
            Set(theme, "streetsEnabled", true);
            Set(theme, "streetInner", 2f);
            Set(theme, "streetWidth", 1f);
            Set(board, "sceneTheme", theme);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(environment);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(theme);
            Object.DestroyImmediate(block);
        }

        // Bãi 4×3, xe hồng 2 ô ở hàng 1 cột 2-3, cổng hồng bên phải hàng 1: một chạm là ra cổng.
        private CarView Bind()
        {
            var builder = new LevelDefBuilder(4, 3);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 1, 2, TileColor.Pink));
            builder.Gates.Add(new GateDef(GateEdge.Right, 1, TileColor.Pink));
            var session = new GameSession(builder.Build());
            board.Build(session.Level, session.State, palette);
            animator.Initialize(board, feel, session.State);
            var command = new SolverMove(SolverMoveKind.MoveCar, 0, 1);
            animator.Enqueue(command, session.State, session.Apply(command));
            return board.Car(0);
        }

        [Test]
        public void CarDrivesToStreetCentreTurnsAndLeaves()
        {
            Assert.That(board.StreetCenterOffset, Is.EqualTo(-1), "Chưa dựng bãi thì chưa có phố");
            var car = Bind();
            Assert.That(board.StreetCenterOffset, Is.EqualTo(2.5f).Within(.0001f));
            float streetX = board.Cell(0, 3.5f).x + 2.5f;
            float farthestX = float.MinValue, farthestZ = 0;
            bool turned = false;
            for (int i = 0; i < 400 && animator.IsBusy; i++)
            {
                animator.Advance(.01f);
                var p = car.transform.localPosition;
                farthestX = Mathf.Max(farthestX, p.x);
                if (Mathf.Abs(Mathf.DeltaAngle(car.transform.localEulerAngles.y, 90)) < 1 || Mathf.Abs(Mathf.DeltaAngle(car.transform.localEulerAngles.y, -90)) < 1) { turned = true; }
                if (car.gameObject.activeSelf) { farthestZ = Mathf.Max(farthestZ, Mathf.Abs(p.z)); }
            }
            Assert.That(animator.IsBusy, Is.False);
            Assert.That(farthestX, Is.EqualTo(streetX).Within(.05f), "Xe dừng ngang ở tim phố, không chạy quá");
            Assert.That(turned, Is.True, "Xe cua 90° lên phố dọc");
            Assert.That(farthestZ, Is.GreaterThan(5f), "Xe chạy dọc phố ra xa trước khi biến mất");
            Assert.That(car.gameObject.activeSelf && car.transform.localScale.x > .01f, Is.False, "Cuối cùng xe ẩn đi");
        }

        [Test]
        public void WithoutStreetsExitIsUnchanged()
        {
            Set(theme, "streetsEnabled", false);
            var car = Bind();
            Assert.That(board.StreetCenterOffset, Is.LessThan(0));
            for (int i = 0; i < 400 && animator.IsBusy; i++)
            {
                animator.Advance(.01f);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(car.transform.localEulerAngles.y, 0)), Is.LessThan(.5f), "Không có phố thì xe không cua");
            }
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
