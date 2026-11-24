using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class PlayableViewTests
    {
        private GameObject root;
        private BoardView board;
        private ActionAnimator animator;
        private VisualPalette palette;
        private FeelConfig feel;
        private Material material;
        [TestCase(1)]
        [TestCase(-1)]
        public void SwipeFiresAtThresholdWhileHeldAndNeverRepeats(int direction)
        {
            var gesture = new CarGesture();
            gesture.Begin(Vector2.zero, Vector2.right, -direction, 40);
            Assert.That(gesture.TryDrag(Vector2.right * (39 * direction), out _), Is.False);
            Assert.That(gesture.TryDrag(Vector2.right * (40 * direction), out int sign), Is.True);
            Assert.That(sign, Is.EqualTo(direction));
            Assert.That(gesture.TryDrag(Vector2.right * (80 * direction), out _), Is.False);
            Assert.That(gesture.TryRelease(Vector2.right * (80 * direction), out _), Is.False);
        }

        [Test]
        public void TapWaitsForReleaseAndUsesHalfSelectedAtPress()
        {
            var gesture = new CarGesture();
            gesture.Begin(Vector2.zero, Vector2.up, -1, 40);
            Assert.That(gesture.TryDrag(new Vector2(3, 5), out _), Is.False);
            Assert.That(gesture.TryRelease(new Vector2(3, 5), out int sign), Is.True);
            Assert.That(sign, Is.EqualTo(-1));
            Assert.That(gesture.TryRelease(Vector2.zero, out _), Is.False);
        }

        [Test]
        public void SidewaysDragAndCancelledGestureCannotBecomeTap()
        {
            var gesture = new CarGesture();
            gesture.Begin(Vector2.zero, Vector2.up, 1, 40);
            Assert.That(gesture.TryDrag(Vector2.right * 60, out _), Is.False);
            Assert.That(gesture.TryRelease(Vector2.zero, out _), Is.False);
            gesture.Begin(Vector2.zero, Vector2.up, 1, 40);
            gesture.Cancel();
            Assert.That(gesture.TryDrag(Vector2.up * 60, out _), Is.False);
            Assert.That(gesture.TryRelease(Vector2.zero, out _), Is.False);
        }

        [Test]
        public void FastFlickAndNewGestureUseProjectedAxis()
        {
            var gesture = new CarGesture();
            var axis = new Vector2(.3f, .8f).normalized;
            gesture.Begin(Vector2.zero, axis, -1, 40);
            Assert.That(gesture.TryRelease(axis * 60, out int first), Is.True);
            Assert.That(first, Is.EqualTo(1));
            gesture.Begin(Vector2.zero, axis, 1, 40);
            Assert.That(gesture.TryDrag(-axis * 60, out int second), Is.True);
            Assert.That(second, Is.EqualTo(-1));
        }

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Test board");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            material = new Material(Shader.Find("Standard"));
            typeof(VisualPalette).GetField("material", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(palette, material);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(material);
        }

        [Test]
        public void IndependentLanesMoveInParallel()
        {
            var b = new LevelDefBuilder(6, 2);
            b.Cars.Add(new CarDef(Axis.Horizontal, 0, 1, TileColor.Pink));
            b.Cars.Add(new CarDef(Axis.Horizontal, 1, 1, TileColor.Blue));
            b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink));
            b.Gates.Add(new GateDef(GateEdge.Right, 1, TileColor.Blue));
            var session = Bind(b.Build());
            float first = board.Car(0).transform.localPosition.x, second = board.Car(1).transform.localPosition.x;
            Apply(session, 0);
            Apply(session, 1);
            animator.Advance(.05f);
            Assert.That(board.Car(0).transform.localPosition.x, Is.GreaterThan(first));
            Assert.That(board.Car(1).transform.localPosition.x, Is.GreaterThan(second));
        }

        [Test]
        public void FollowingCarWaitsForReservedPathWithoutVisualOverlap()
        {
            var b = new LevelDefBuilder(6, 2);
            b.Cars.Add(new CarDef(Axis.Horizontal, 0, 4, TileColor.Pink));
            b.Cars.Add(new CarDef(Axis.Horizontal, 0, 1, TileColor.Pink));
            b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink));
            var session = Bind(b.Build());
            Apply(session, 0);
            Apply(session, 1);
            for (int tick = 0; tick < 300 && animator.IsBusy; tick++)
            {
                animator.Advance(.01f);
                var a = board.Car(0);
                var c = board.Car(1);
                if (a.gameObject.activeSelf && c.gameObject.activeSelf)
                {
                    Assert.That(Mathf.Abs(a.transform.localPosition.x - c.transform.localPosition.x), Is.GreaterThanOrEqualTo(1.8f), "Thân hai xe chồng khi Core đã đi trước hình");
                }
            }

            Assert.That(animator.IsBusy, Is.False);
            Assert.That(board.Car(0).gameObject.activeSelf || board.Car(1).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void UndoCancelsQueuedExitAndKeepsCarAtRestoredCell()
        {
            var b = new LevelDefBuilder(4, 2);
            b.Cars.Add(new CarDef(Axis.Horizontal, 0, 1, TileColor.Pink));
            b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink));
            var session = Bind(b.Build());
            var expected = board.Car(0).transform.localPosition;
            Apply(session, 0);
            animator.Advance(.05f);
            session.Undo();
            animator.CancelAndSnap(session.State);
            animator.Advance(20);
            Assert.That(animator.IsBusy, Is.False);
            Assert.That(board.Car(0).gameObject.activeSelf, Is.True);
            Assert.That(board.Car(0).transform.localPosition, Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(17)]
        public void PortraitCameraIncludesBoardAndMechanicHeight(int levelId)
        {
            var level = SolverTestData.Load(levelId);
            Bind(level);
            var cameraObject = new GameObject("Test Camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            var texture = new RenderTexture(1080, 1920, 0);
            camera.targetTexture = texture;
            var config = ScriptableObject.CreateInstance<CameraConfig>();
            try
            {
                board.FitCamera(camera, config, new Rect(0, 0, 1, 1));
                for (int r = 0; r <= 1; r++)
                {
                    for (int c = 0; c <= 1; c++)
                    {
                        for (int y = 0; y <= 1; y++)
                        {
                            var point = camera.WorldToViewportPoint(board.Cell(r == 0 ? -.8f : level.Rows - .2f, c == 0 ? -.8f : level.Cols - .2f, y));
                            Assert.That(point.x, Is.InRange(0f, 1f));
                            Assert.That(point.y, Is.InRange(0f, 1f));
                            Assert.That(point.z, Is.GreaterThan(0));
                        }
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TableButtonsWorkWithoutVisibleDebugHudAndRespectPlayGate()
        {
            var canvasObject = new GameObject("Test UI", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var debug = root.AddComponent<DebugHud>();
            debug.Initialize(canvas, false);
            Assert.That(typeof(DebugHud).GetField("font", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(debug), Is.Null);
            var dialog = new GameObject("Dialog", typeof(RectTransform));
            dialog.transform.SetParent(canvas.transform, false);
            var builder = new LevelDefBuilder(3, 3);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            builder.Tables.Add(new Cell(0, 0));
            var session = Bind(builder.Build());
            using var tableButtons = new TableButtons();
            tableButtons.Initialize(canvas);
            var settings = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                tableButtons.Bind(session, board, null, settings);
                var button = canvas.GetComponentInChildren<Button>();
                int commands = 0;
                tableButtons.Command += command =>
                {
                    commands++;
                    session.Apply(command);
                };
                tableButtons.SetAccepting(true);
                Assert.That(button.interactable, Is.True);
                Assert.That(button.transform.parent.GetSiblingIndex(), Is.LessThan(dialog.transform.GetSiblingIndex()));
                button.onClick.Invoke();
                Assert.That(commands, Is.EqualTo(1));
                Assert.That(session.State.CarAt(0).Axis, Is.EqualTo(Axis.Vertical));
                tableButtons.SetAccepting(false);
                button.onClick.Invoke();
                Assert.That(commands, Is.EqualTo(1));
                Assert.That(button.interactable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        private GameSession Bind(LevelDef level)
        {
            var session = new GameSession(level);
            board.Build(level, session.State, palette);
            animator.Initialize(board, feel, session.State);
            return session;
        }

        private void Apply(GameSession session, int car)
        {
            var command = new SolverMove(SolverMoveKind.MoveCar, car, 1);
            var before = session.State;
            var result = session.Apply(command);
            Assert.That(result.Moved, Is.True);
            animator.Enqueue(command, before, result);
        }
    }
}
