using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class CoreFeelTests
    {
        private GameObject root;
        private BoardView board;
        private ActionAnimator animator;
        private FeelConfig feel;
        private VisualPalette palette;
        private Material material;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("012 test");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            material = new Material(Shader.Find("Standard"));
            Set(palette, "material", material);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(material);
        }

        [TestCase(.01f)]
        [TestCase(.22f)]
        [TestCase(1f)]
        public void SpringSettlesWithoutNaNOrExceedingImpulse(float duration)
        {
            Assert.That(BodySpring.Sample(0, duration, .375f), Is.EqualTo(1).Within(.0001f));
            Assert.That(BodySpring.Sample(duration * .5f, duration, .375f), Is.EqualTo(-.375f).Within(.0001f));
            for (int i = 0; i <= 1000; i++)
            {
                float value = BodySpring.Sample(duration * i / 500, duration, .375f);
                Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False);
                Assert.That(value, Is.InRange(-1, 1));
            }
            Assert.That(BodySpring.Sample(duration, duration, .375f), Is.Zero);
            Assert.That(BodySpring.Sample(duration * 100, duration, .375f), Is.Zero);
        }

        [TestCase(float.NaN, .22f)]
        [TestCase(float.PositiveInfinity, .22f)]
        [TestCase(.1f, 0)]
        [TestCase(.1f, float.NaN)]
        public void SpringRejectsInvalidTimeWithRestPose(float elapsed, float duration)
        {
            Assert.That(BodySpring.Sample(elapsed, duration, .375f), Is.Zero);
        }

        [Test]
        public void BodyMovesOnlyVisualAndUndoResetsImmediatelyWhilePauseFreezes()
        {
            var session = Bind();
            var car = board.Car(0);
            var visual = Model(car);
            var collider = car.GetComponent<BoxCollider>();
            var center = collider.center;
            var size = collider.size;
            var restRotation = visual.localRotation;
            var restScale = visual.localScale;
            Move(session);
            animator.Advance(.01f);
            Assert.That(Quaternion.Angle(visual.localRotation, restRotation), Is.GreaterThan(.1f));
            Assert.That(car.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(collider.center, Is.EqualTo(center));
            Assert.That(collider.size, Is.EqualTo(size));
            animator.Paused = true;
            var pausedPosition = car.transform.localPosition;
            var pausedRotation = visual.localRotation;
            animator.Advance(1);
            Assert.That(car.transform.localPosition, Is.EqualTo(pausedPosition));
            Assert.That(visual.localRotation, Is.EqualTo(pausedRotation));
            Assert.That(session.Undo(), Is.True);
            animator.CancelAndSnap(session.State);
            Assert.That(visual.localRotation, Is.EqualTo(restRotation));
            Assert.That(visual.localScale, Is.EqualTo(restScale));
            Assert.That(animator.IsBusy, Is.False);
        }

        [TestCase("Car_pink")]
        [TestCase("Car_blue_truck")]
        public void KenneyWheelsRollBothWaysWithoutOrbitingAndReset(string prefabName)
        {
            string path = "Assets/_Project/Prefabs/Cars/" + prefabName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var car = Object.Instantiate(prefab, root.transform).GetComponent<CarView>();
            var data = new SerializedObject(car);
            var model = (Transform)data.FindProperty("modelRoot").objectReferenceValue;
            var dirt = (Renderer)data.FindProperty("mudOverlay").objectReferenceValue;
            var body = car.GetComponent<CarBody>();
            if (body == null) { body = car.gameObject.AddComponent<CarBody>(); }
            body.Bind(feel, model, dirt.transform);
            int count = 0;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.StartsWith("wheel-")) { continue; }
                count++;
                var wheel = filter.transform;
                var position = wheel.localPosition;
                var rotation = wheel.localRotation;
                var center = wheel.TransformPoint(filter.sharedMesh.bounds.center);
                body.Roll(.2f);
                body.Advance(0);
                Assert.That(Quaternion.Angle(wheel.localRotation, rotation), Is.GreaterThan(1));
                Assert.That(Vector3.Distance(center, wheel.TransformPoint(filter.sharedMesh.bounds.center)), Is.LessThan(.0001f));
                body.Roll(-.2f);
                body.Advance(0);
                Assert.That(Quaternion.Angle(wheel.localRotation, rotation), Is.LessThan(.01f));
                body.Roll(.4f);
                body.Advance(0);
                body.ResetPose();
                Assert.That(wheel.localPosition, Is.EqualTo(position));
                Assert.That(wheel.localRotation, Is.EqualTo(rotation));
            }
            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public void DetailedStopEventKeepsCoreMetadataAndFiresWhenAnimationStarts()
        {
            var session = Bind();
            CarView source = null;
            MoveEvent stopped = default;
            animator.CarEventStarted += (car, ev) =>
            {
                if (ev.Kind == MoveEventKind.Stop) { source = car; stopped = ev; }
            };
            Move(session);
            Assert.That(source, Is.Null);
            animator.Advance(10);
            Assert.That(source, Is.SameAs(board.Car(0)));
            Assert.That(stopped.Sign, Is.EqualTo(1));
            Assert.That(stopped.Reason, Is.EqualTo(StopReason.Edge));
        }

        [Test]
        public void MudFadeUsesSharedAssetAndUndoRestoresOpaqueMud()
        {
            Set(feel, "mudFadeMaterial", material);
            var session = Bind(1);
            var car = board.Car(0);
            var data = new SerializedObject(car);
            var dirt = (Renderer)data.FindProperty("mudOverlay").objectReferenceValue;
            car.PlayFeel(new MoveEvent(MoveEventKind.Spray));
            car.Show(car.transform.localPosition, 0, TileColor.Pink, 0, true);
            car.AdvanceFeel(feel.MudFadeSeconds * .5f);
            var block = new MaterialPropertyBlock();
            dirt.GetPropertyBlock(block);
            Assert.That(dirt.sharedMaterial, Is.SameAs(material));
            Assert.That(block.GetColor("_Color").a, Is.EqualTo(palette.Mud.a * .5f).Within(.001f));
            animator.CancelAndSnap(session.State);
            dirt.GetPropertyBlock(block);
            Assert.That(block.GetColor("_Color").a, Is.EqualTo(palette.Mud.a).Within(.001f));
            Assert.That(dirt.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ConfettiSharesFiveSystemsCapsAllParticlesAndCancelClears()
        {
            Bind();
            var effects = root.AddComponent<FeelEffects>();
            var systems = new ParticleSystem[5];
            for (int i = 0; i < systems.Length; i++)
            {
                var node = new GameObject("pool " + i);
                node.transform.SetParent(root.transform);
                systems[i] = node.AddComponent<ParticleSystem>();
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = systems[i].emission;
                emission.enabled = false;
            }
            effects.ConfigurePrefab(systems[0], systems[1], systems[2], systems[3], systems[4]);
            effects.Initialize(animator, board, null, feel, palette);
            effects.Celebrate();
            Assert.That(effects.LiveCount, Is.EqualTo(feel.Confetti.Count), "Pool mới phải Emit được ngay trước frame mô phỏng đầu tiên.");
            effects.Celebrate();
            effects.Celebrate();
            Assert.That(effects.LiveCount, Is.EqualTo(feel.MaxLiveParticles));
            Assert.That(root.GetComponentsInChildren<ParticleSystem>().Length, Is.EqualTo(5));
            animator.Paused = true;
            effects.Advance(10);
            Assert.That(effects.LiveCount, Is.EqualTo(feel.MaxLiveParticles));
            animator.Paused = false;
            effects.Advance(feel.Confetti.Lifetime + .1f);
            Assert.That(effects.LiveCount, Is.Zero);
            effects.Celebrate();
            animator.CancelAndSnap(new GameSession(board.Level).State);
            Assert.That(effects.LiveCount, Is.Zero);
        }

        [Test]
        public void ShakeIsBoundedAndReturnsCameraOnCancel()
        {
            var session = Bind();
            var cameraNode = new GameObject("012 camera");
            cameraNode.transform.SetParent(root.transform);
            cameraNode.transform.position = new Vector3(2, 10, -4);
            var camera = cameraNode.AddComponent<Camera>();
            var rest = camera.transform.position;
            var effects = root.AddComponent<FeelEffects>();
            effects.Initialize(animator, board, camera, feel, palette);
            Move(session);
            animator.Advance(10);
            effects.Advance(feel.ShakeSeconds * .1f);
            Assert.That(Vector3.Distance(camera.transform.position, rest), Is.GreaterThan(.001f));
            Assert.That(Vector3.Distance(camera.transform.position, rest), Is.LessThanOrEqualTo(feel.ShakeDistance + .0001f));
            animator.CancelAndSnap(session.State);
            Assert.That(Vector3.Distance(camera.transform.position, rest), Is.LessThan(.0001f));
        }

        [Test]
        public void FeelValidationClampsUnsafeValuesWithoutChangingExistingTimings()
        {
            float step = feel.StepSeconds;
            Set(feel, "launchPitch", float.NaN);
            Set(feel, "brakeSeconds", 0f);
            Set(feel, "maxLiveParticles", 1000);
            Set(feel, "shakeDistance", float.PositiveInfinity);
            typeof(FeelConfig).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(feel, null);
            Assert.That(feel.LaunchPitch, Is.EqualTo(-5));
            Assert.That(feel.BrakeSeconds, Is.GreaterThan(0));
            Assert.That(feel.MaxLiveParticles, Is.EqualTo(60));
            Assert.That(feel.ShakeDistance, Is.EqualTo(.04f));
            Assert.That(feel.StepSeconds, Is.EqualTo(step));
        }

        [TestCase(-1f, 1f)]
        [TestCase(2f, 1.5f)]
        [TestCase(float.NaN, 1.06f)]
        [TestCase(float.PositiveInfinity, 1.06f)]
        public void BrakeWidthValidationKeepsFiniteSafeScale(float input, float expected)
        {
            Set(feel, "brakeWidth", input);
            typeof(FeelConfig).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(feel, null);
            Assert.That(feel.BrakeWidth, Is.EqualTo(expected).Within(.0001f));
        }

        [Test]
        public void HitStillMovesVisualWhenMudGroupIsDisabled()
        {
            Set(feel, "mudBodyEnabled", false);
            Bind();
            var car = board.Car(0);
            var model = Model(car);
            var rest = model.localPosition;
            var rootPosition = car.transform.localPosition;
            var body = car.GetComponent<CarBody>();
            body.Hit(Vector3.right);
            body.Advance(feel.HitSeconds * .5f);
            Assert.That(model.localPosition.x - rest.x, Is.EqualTo(feel.HitDistance).Within(.0001f));
            Assert.That(car.transform.localPosition, Is.EqualTo(rootPosition));
            body.ResetPose();
            Assert.That(model.localPosition, Is.EqualTo(rest));
        }

        [Test]
        public void BrakeSquashesLengthAndWidensOnlyVisualThenResets()
        {
            Bind();
            var car = board.Car(0);
            var model = Model(car);
            var rest = model.localScale;
            var body = car.GetComponent<CarBody>();
            body.Play(new MoveEvent(MoveEventKind.Stop, sign: 1, reason: StopReason.Wall));
            body.Advance(0);
            Assert.That(model.localScale.x, Is.EqualTo(rest.x * feel.BrakeLength).Within(.0001f));
            Assert.That(model.localScale.z, Is.EqualTo(rest.z * feel.BrakeWidth).Within(.0001f));
            Assert.That(car.transform.localScale, Is.EqualTo(Vector3.one));
            body.ResetPose();
            Assert.That(model.localScale, Is.EqualTo(rest));
        }

        private GameSession Bind(int mud = 0)
        {
            var builder = new LevelDefBuilder(5, 2);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink, mud: mud));
            var session = new GameSession(builder.Build());
            board.Build(session.Level, session.State, palette);
            animator.Initialize(board, feel, session.State);
            return session;
        }
        private void Move(GameSession session)
        {
            var before = session.State;
            var command = new SolverMove(SolverMoveKind.MoveCar, 0, 1);
            animator.Enqueue(command, before, session.Apply(command));
        }
        private static Transform Model(CarView car)
        {
            return (Transform)typeof(CarView).GetField("modelRoot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(car);
        }
        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
