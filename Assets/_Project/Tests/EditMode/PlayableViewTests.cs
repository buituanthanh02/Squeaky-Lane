using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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

        [TestCase(1, -1)]
        [TestCase(3, 0)]
        [TestCase(16, 1)]
        [TestCase(19, 1)]
        [TestCase(29, -1)]
        public void PhoneCameraAdaptsButKeepsGateFramesAndBothCarHalves(int levelId, int zoomDirection)
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/_Project/Resources/Levels/level_{levelId:00}.json");
            var level = LevelJsonLoader.Load(json.text, json.name);
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/_Project/Config/UiTheme.asset");
            var mechanics = AssetDatabase.LoadAssetAtPath<MechanicCatalog>("Assets/_Project/Config/MechanicCatalog.asset");
            var cars = AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/_Project/Config/CarCatalog.asset");
            typeof(BoardView).GetField("sceneTheme", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(board, theme);
            board.Build(level, new GameSession(level).State, palette, cars, mechanics);

            var cameraObject = new GameObject("016-f iPhone camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.GetComponent<Camera>();
            var texture = new RenderTexture(1284, 2778, 0);
            camera.targetTexture = texture;
            const float safeHeight = 2535f / 2778f;
            var config = AssetDatabase.LoadAssetAtPath<CameraConfig>("Assets/_Project/Config/CameraConfig.asset");
            var viewport = new Rect(0, 102f / 2778f + safeHeight * config.PlayingBottomInset,
                1, safeHeight * (.86f - config.PlayingBottomInset));
            var baseline = Object.Instantiate(config);
            int cells = level.Cols * level.Rows;
            typeof(CameraConfig).GetField("wideBoardCells", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(baseline, cells - 1);
            typeof(CameraConfig).GetField("tightBoardCells", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(baseline, cells + 1);
            try
            {
                board.FitCamera(camera, baseline, viewport);
                float before = texture.height / (2 * camera.orthographicSize);
                board.FitCamera(camera, config, viewport);
                float after = texture.height / (2 * camera.orthographicSize);
                TestContext.WriteLine($"{levelId:00}: {before:0.0} → {after:0.0} px/ô");
                if (zoomDirection == 0) { Assert.That(after / before, Is.InRange(.99f, 1.01f)); }
                else { Assert.That(zoomDirection * (after - before), Is.GreaterThan(1f)); }

                AssertInViewport(camera, viewport, new Bounds(Vector3.zero, new Vector3(level.Cols, 0, level.Rows)));
                foreach (var view in board.GetComponentsInChildren<MechanicView>())
                {
                    if (view.Kind != MechanicKind.ColorGate && view.Kind != MechanicKind.WaterGate && view.Kind != MechanicKind.WashGate) { continue; }
                    foreach (var mesh in view.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (mesh.name == "Pad" || mesh.name == "Pad Rim" || mesh.GetComponentInParent<ContactShadowView>() != null) { continue; }
                        AssertInViewport(camera, viewport, mesh.bounds);
                    }
                }

                Physics.SyncTransforms();
                for (int i = 0; i < level.Cars.Count; i++)
                {
                    var car = board.Car(i);
                    var box = car.GetComponent<BoxCollider>();
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector3 sample = box.center + new Vector3(sign * box.size.x * .25f, box.size.y * .5f, 0);
                        Vector3 world = box.transform.TransformPoint(sample);
                        AssertInViewport(camera, viewport, new Bounds(world, Vector3.zero));
                        Assert.That(Physics.Raycast(camera.ScreenPointToRay(camera.WorldToScreenPoint(world)), out var hit, 100), Is.True);
                        Assert.That(hit.collider.GetComponentInParent<CarView>(), Is.SameAs(car));
                        Assert.That(sign * Vector3.Dot(hit.point - car.transform.position, car.PositiveDirection), Is.GreaterThan(0));
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(baseline);
            }
        }

        private static void AssertInViewport(Camera camera, Rect viewport, Bounds bounds)
        {
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                var point = camera.WorldToViewportPoint(corner);
                Assert.That(viewport.Contains(point) && point.z > 0, Is.True, $"{corner} → {point}, khung {viewport}");
            }
        }

        [Test]
        public void TableButtonsRespectPlayGate()
        {
            var canvasObject = new GameObject("Test UI", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
