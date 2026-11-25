using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 014: bứt tốc và lắc khi đâm chỉ là số hình (FeelConfig), không chạm luật Core.
    public sealed class CarFeel014Tests
    {
        private FeelConfig feel;

        [SetUp]
        public void Setup() { feel = ScriptableObject.CreateInstance<FeelConfig>(); }

        [TearDown]
        public void Cleanup() { Object.DestroyImmediate(feel); }

        [Test]
        public void ZeroAccelKeepsOldEvenSlide()
        {
            Set("accelCells", 0f);
            for (int i = 0; i < 8; i++)
            {
                Assert.That(feel.StepDuration(i), Is.EqualTo(feel.StepSeconds).Within(1e-5f));
            }
        }

        [TestCase(.5f)]
        [TestCase(1.2f)]
        [TestCase(3f)]
        public void AccelerationStartsSlowThenReachesTopSpeed(float accel)
        {
            Set("accelCells", accel);
            float previous = float.MaxValue;
            for (int i = 0; i < 10; i++)
            {
                float step = feel.StepDuration(i);
                Assert.That(step, Is.GreaterThan(0));
                Assert.That(step, Is.LessThanOrEqualTo(previous + 1e-5f), "Ô sau không được chậm hơn ô trước");
                Assert.That(step, Is.GreaterThanOrEqualTo(feel.StepSeconds - 1e-5f), "Không vượt tốc độ tối đa");
                previous = step;
            }
            Assert.That(feel.StepDuration(0), Is.GreaterThan(feel.StepSeconds), "Ô đầu phải chậm hơn khi có bứt tốc");
            Assert.That(feel.StepDuration(Mathf.CeilToInt(accel) + 1), Is.EqualTo(feel.StepSeconds).Within(1e-5f));
        }

        [Test]
        public void WholeSlideCostsExactlyAccelCellsTimesStep()
        {
            Set("accelCells", 1.2f);
            float total = 0;
            for (int i = 0; i < 6; i++) { total += feel.StepDuration(i); }
            Assert.That(total, Is.EqualTo((6 + 1.2f) * feel.StepSeconds).Within(1e-4f));
            Assert.That(feel.TimeToCell(1.2f), Is.EqualTo(2 * 1.2f * feel.StepSeconds).Within(1e-5f), "Hai nhánh công thức khớp nhau ở mốc AccelCells");
        }

        [Test]
        public void WobbleIsBoundedAndSettles()
        {
            Assert.That(feel.CrashWobble(0), Is.EqualTo(0).Within(1e-5f));
            bool moved = false;
            for (int i = 0; i <= 200; i++)
            {
                float value = feel.CrashWobble(i * .005f);
                Assert.That(float.IsNaN(value), Is.False);
                Assert.That(value, Is.InRange(-1f, 1f));
                moved |= Mathf.Abs(value) > .2f;
            }
            Assert.That(moved, Is.True, "Lắc phải đủ thấy");
            Assert.That(feel.CrashWobble(10), Is.Zero);
            Assert.That(feel.CrashWobble(-1), Is.Zero);
        }

        private void Set(string field, object value)
        {
            typeof(FeelConfig).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(feel, value);
        }
    }

    // 014 bùn kiểu D: cục bùn khối thay tấm bùn phẳng; đặc khi bẩn, mờ dần cùng màu khi rửa, Hoàn tác trả lại.
    public sealed class MudBlob014Tests
    {
        private GameObject root, blob;
        private BoardView board;
        private ActionAnimator animator;
        private FeelConfig feel;
        private VisualPalette palette;
        private Material block, solid, fade;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("014 mud test");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            block = new Material(Shader.Find("Standard"));
            solid = new Material(Shader.Find("Standard")) { color = new Color(.35f, .2f, .09f, 1) };
            fade = new Material(Shader.Find("Standard"));
            Set(palette, "material", block);
            blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(blob.GetComponent<Collider>());
            blob.GetComponent<Renderer>().sharedMaterial = solid;
            Set(feel, "mudBlobPrefab", blob);
            Set(feel, "mudFadeMaterial", fade);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(blob);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(block);
            Object.DestroyImmediate(solid);
            Object.DestroyImmediate(fade);
        }

        [Test]
        public void BlobReplacesFlatMudFadesInSolidColourAndUndoRestores()
        {
            var builder = new LevelDefBuilder(5, 2);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink, mud: 1));
            var session = new GameSession(builder.Build());
            board.Build(session.Level, session.State, palette);
            animator.Initialize(board, feel, session.State);
            var car = board.Car(0);
            var dirt = Mud(car);
            Assert.That(dirt.name, Is.EqualTo("014 Mud Blob"));
            Assert.That(dirt.sharedMaterial, Is.SameAs(solid), "Bẩn hẳn dùng material đặc của cục bùn");
            Assert.That(dirt.gameObject.activeSelf, Is.True);
            Assert.That(car.HasMud, Is.True);
            int flat = 0;
            foreach (Transform child in car.transform) { if (child.name.StartsWith("Bùn bám") && child.gameObject.activeSelf) { flat++; } }
            Assert.That(flat, Is.Zero, "Tấm bùn phẳng cũ phải bị thay");

            car.PlayFeel(new MoveEvent(MoveEventKind.Spray));
            car.Show(car.transform.localPosition, 0, TileColor.Pink, 0, true);
            car.AdvanceFeel(feel.MudFadeSeconds * .5f);
            var props = new MaterialPropertyBlock();
            dirt.GetPropertyBlock(props);
            Assert.That(car.HasMud, Is.False);
            Assert.That(dirt.sharedMaterial, Is.SameAs(fade));
            Assert.That(props.GetColor("_Color").r, Is.EqualTo(solid.color.r).Within(.001f), "Mờ dần cùng màu cục bùn, không nhảy màu");
            Assert.That(props.GetColor("_Color").a, Is.EqualTo(.5f).Within(.01f));

            animator.CancelAndSnap(session.State);
            Assert.That(Mud(car), Is.SameAs(dirt));
            Assert.That(dirt.sharedMaterial, Is.SameAs(solid));
            Assert.That(dirt.gameObject.activeSelf, Is.True);
        }

        private static Renderer Mud(CarView car)
        {
            return (Renderer)typeof(CarView).GetField("mudOverlay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(car);
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }

    // 014 trạm rửa: xe dính bùn bò chậm qua ô vòi xịt, bùn trôi trễ rồi hết; WashStationView quay chổi và hủy sạch.
    public sealed class WashStation014Tests
    {
        private GameObject root;
        private BoardView board;
        private ActionAnimator animator;
        private FeelConfig feel;
        private VisualPalette palette;
        private Material block;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("014 wash test");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            block = new Material(Shader.Find("Standard"));
            Set(palette, "material", block);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(block);
        }

        // Xe 2 ô ở cột 0-1, ô vòi xịt ở cột 2: bước vào ô đó là bước đầu của lượt chạy sang phải.
        private GameSession Bind(int mud)
        {
            var builder = new LevelDefBuilder(5, 2);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink, mud: mud));
            builder.Sprays.Add(new Cell(0, 2));
            var session = new GameSession(builder.Build());
            board.Build(session.Level, session.State, palette);
            animator.Initialize(board, feel, session.State);
            var command = new SolverMove(SolverMoveKind.MoveCar, 0, 1);
            animator.Enqueue(command, session.State, session.Apply(command));
            return session;
        }

        [Test]
        public void MuddyCarCrawlsThroughSprayCellButCleanCarDoesNot()
        {
            Set(feel, "washCrawlSeconds", .8f);
            Bind(1);
            float start = board.Car(0).transform.localPosition.x;
            animator.Advance(.4f);
            Assert.That(board.Car(0).transform.localPosition.x - start, Is.LessThan(.9f), "Xe bẩn còn đang bò chậm qua ô rửa");
            animator.Advance(.5f);
            Assert.That(board.Car(0).transform.localPosition.x - start, Is.GreaterThanOrEqualTo(.99f), "Bò xong ô rửa trong WashCrawlSeconds");
        }

        [Test]
        public void CleanCarKeepsNormalSpeedAndZeroCrawlRestoresOldTiming()
        {
            Bind(0);
            float start = board.Car(0).transform.localPosition.x;
            animator.Advance(feel.StepDuration(0) + .01f);
            Assert.That(board.Car(0).transform.localPosition.x - start, Is.GreaterThanOrEqualTo(.99f), "Xe sạch không bò ở ô vòi xịt vì không có Spray");
        }

        [Test]
        public void ZeroCrawlSecondsDisablesSlowdown()
        {
            Set(feel, "washCrawlSeconds", 0f);
            Bind(1);
            float start = board.Car(0).transform.localPosition.x;
            animator.Advance(feel.StepDuration(0) + .01f);
            Assert.That(board.Car(0).transform.localPosition.x - start, Is.GreaterThanOrEqualTo(.99f));
        }

        [Test]
        public void MudStaysUntilDelayThenFadesOutDuringCrawl()
        {
            Set(feel, "washCrawlSeconds", .8f);
            Set(feel, "washMudDelaySeconds", .2f);
            Set(feel, "washMudFadeSeconds", .3f);
            Set(feel, "mudFadeMaterial", block);
            Bind(1);
            var car = board.Car(0);
            var overlay = (Renderer)typeof(CarView).GetField("mudOverlay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(car);
            animator.Advance(.1f);
            Assert.That(overlay.gameObject.activeSelf, Is.True, "Chổi chưa chạm xe nên bùn còn nguyên");
            animator.Advance(.3f);
            Assert.That(overlay.gameObject.activeSelf, Is.True, "Đang trôi dở");
            animator.Advance(.3f);
            Assert.That(overlay.gameObject.activeSelf, Is.False, "Hết bùn trước khi ra khỏi ô rửa");
        }

        [Test]
        public void StationSpinsRollersSprayAndCancelRestoresRest()
        {
            var station = new GameObject("station");
            var spin = new GameObject("Spin");
            var rollerN = new GameObject("Roller N");
            rollerN.transform.SetParent(station.transform, false);
            spin.transform.SetParent(rollerN.transform, false);
            var spray = new GameObject("Spray N");
            spray.transform.SetParent(station.transform, false);
            var particles = spray.AddComponent<ParticleSystem>();
            var view = station.AddComponent<WashStationView>();
            try
            {
                Assert.That(view.RollerCount, Is.EqualTo(1));
                var rest = spin.transform.localRotation;
                view.Begin(.5f, 360);
                Assert.That(view.IsActive, Is.True);
                view.Advance(.2f);
                Assert.That(Quaternion.Angle(rest, spin.transform.localRotation), Is.GreaterThan(1f), "Chổi phải quay");
                view.Advance(.5f);
                Assert.That(view.IsActive, Is.False, "Hết giờ rửa thì ngừng xịt");
                view.Begin(.5f, 360);
                view.Advance(.1f);
                view.Cancel();
                Assert.That(Quaternion.Angle(rest, spin.transform.localRotation), Is.LessThan(.01f), "Hủy đưa chổi về tư thế nghỉ");
                Assert.That(view.IsActive, Is.False);
                Assert.That(particles.particleCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(station); }
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
