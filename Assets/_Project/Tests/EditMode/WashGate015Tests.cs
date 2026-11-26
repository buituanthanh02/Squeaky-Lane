using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 015 cổng rửa: có hầm rửa (WashGate) thì xe chạy xuyên hẳn ra ngoài bãi, bò chậm, rồi lùi về; luật Core không đổi.
    public sealed class WashGate015Tests
    {
        private GameObject root, gatePrefab;
        private BoardView board;
        private ActionAnimator animator;
        private FeelConfig feel;
        private VisualPalette palette;
        private MechanicCatalog catalog;
        private Material block;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("015 wash gate test");
            board = root.AddComponent<BoardView>();
            animator = root.AddComponent<ActionAnimator>();
            feel = ScriptableObject.CreateInstance<FeelConfig>();
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            catalog = ScriptableObject.CreateInstance<MechanicCatalog>();
            block = new Material(Shader.Find("Standard"));
            Set(palette, "material", block);
            // Prefab giả: chỉ cần MechanicView loại WashGate + WashStationView + một chổi đứng (Spin xoay để X local chỉ lên).
            gatePrefab = new GameObject("WashGate test");
            gatePrefab.AddComponent<MechanicView>().Configure(MechanicKind.WashGate);
            var roller = new GameObject("Roller L");
            roller.transform.SetParent(gatePrefab.transform, false);
            var spin = new GameObject("Spin");
            spin.transform.SetParent(roller.transform, false);
            spin.transform.localRotation = Quaternion.Euler(0, 0, 90);
            gatePrefab.AddComponent<WashStationView>();
            var entry = new MechanicCatalog.Entry { kind = MechanicKind.WashGate, variant = 0, prefab = gatePrefab };
            Set(catalog, "entries", new[] { entry });
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(gatePrefab);
            Object.DestroyImmediate(feel);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(block);
        }

        // Bãi 4×1, xe hồng 2 ô ở cột 0-1 dính bùn, cổng nước bên phải hàng 0: chạy phải 2 ô, rửa, lùi về cột 0.
        private GameSession Bind()
        {
            var builder = new LevelDefBuilder(4, 1);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink, mud: 1));
            builder.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Water));
            var session = new GameSession(builder.Build());
            board.Build(session.Level, session.State, palette, null, catalog);
            animator.Initialize(board, feel, session.State);
            var command = new SolverMove(SolverMoveKind.MoveCar, 0, 1);
            animator.Enqueue(command, session.State, session.Apply(command));
            return session;
        }

        [Test]
        public void CarDrivesFullyThroughGateThenBacksIntoLot()
        {
            Set(feel, "washGateExtraCells", .3f);
            Set(feel, "washGateCrawlSeconds", 1f);
            Set(feel, "washGatePauseSeconds", .1f);
            Set(feel, "washGateBackSeconds", .4f);
            var session = Bind();
            var car = board.Car(0);
            float edge = board.Cell(0, 3.5f).x;
            var station = board.WashGateAt(new GateDef(GateEdge.Right, 0, TileColor.Water));
            Assert.That(station, Is.Not.Null, "Cổng nước phải dựng thành hầm rửa khi catalog có WashGate");

            float farthest = float.MinValue;
            bool washing = false;
            for (int i = 0; i < 300; i++)
            {
                animator.Advance(.01f);
                farthest = Mathf.Max(farthest, car.transform.localPosition.x);
                washing |= station.IsActive;
            }
            Assert.That(washing, Is.True, "Chổi/vòi của hầm phải chạy khi xe đi qua");
            Assert.That(farthest - car.Length * .5f, Is.GreaterThan(edge + .25f), "Đuôi xe phải ra hẳn ngoài mép bãi (cả thân qua cổng)");
            Assert.That(animator.IsBusy, Is.False);
            Assert.That(car.transform.localPosition.x, Is.EqualTo(board.Position(session.State.CarAt(0), car.Length).x).Within(.001f),
                "Kết thúc đúng chỗ Core trả về (lùi về cột 0)");
            Assert.That(session.State.CarAt(0).Mud, Is.Zero);
            Assert.That(car.HasMud, Is.False);
        }

        [Test]
        public void UndoMidWashStopsGateAndRestoresMud()
        {
            var session = Bind();
            var station = board.WashGateAt(new GateDef(GateEdge.Right, 0, TileColor.Water));
            for (int i = 0; i < 60; i++) { animator.Advance(.01f); }
            Assert.That(station.IsActive, Is.True);
            session.Undo();
            animator.CancelAndSnap(session.State);
            Assert.That(station.IsActive, Is.False);
            Assert.That(board.Car(0).HasMud, Is.True);
        }

        [Test]
        public void StandingRollerSpinsAroundItsOwnAxis()
        {
            var view = gatePrefab.GetComponent<WashStationView>();
            var spin = gatePrefab.transform.Find("Roller L/Spin");
            var rest = spin.localRotation;
            var longAxis = spin.TransformDirection(Vector3.right);
            view.Begin(.5f, 360);
            view.Advance(.2f);
            Assert.That(Quaternion.Angle(rest, spin.localRotation), Is.GreaterThan(1f), "Chổi phải quay");
            Assert.That(Vector3.Angle(longAxis, spin.TransformDirection(Vector3.right)), Is.LessThan(.01f), "Quay quanh trục dài nên trục dài không đổi hướng");
            view.Cancel();
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
