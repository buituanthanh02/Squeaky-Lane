using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Core.Solver;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class StateKeyTests
    {
        [TestCase("r")] [TestCase("c")] [TestCase("axis")] [TestCase("color")] [TestCase("mud")]
        [TestCase("alive")] [TestCase("muds")] [TestCase("pyl")] [TestCase("phase")]
        public void EveryRuleField_ChangesKey(string field)
        {
            var level = KeyBoard(true); var encoder = new StateKeyEncoder(level);
            var car = new CarState(field == "r" ? 3 : 2, field == "c" ? 3 : 2,
                field == "axis" ? Axis.Vertical : Axis.Horizontal, field == "color" ? TileColor.Blue : TileColor.Pink,
                field == "mud" ? 1 : 0, field != "alive");
            var initial = new GameState(new[] { new CarState(2, 2, Axis.Horizontal, TileColor.Pink) }, new[] { true }, new[] { 0 });
            var changed = new GameState(new[] { car }, new[] { field != "muds" }, new[] { field == "pyl" ? 1 : 0 }, field == "phase" ? 1 : 0);
            Assert.That(encoder.Encode(changed), Is.Not.EqualTo(encoder.Encode(initial)), field);
        }

        [TestCase(true)] [TestCase(false)]
        public void MoveCount_UsesOnlyParityWhenLightsExist(bool lights)
        {
            var level = KeyBoard(lights); var encoder = new StateKeyEncoder(level);
            var cars = new[] { new CarState(2, 2, Axis.Horizontal, TileColor.Pink) };
            var a = encoder.Encode(new GameState(cars, new[] { true }, new[] { 0 }, 0));
            var b = encoder.Encode(new GameState(cars, new[] { true }, new[] { 0 }, 1));
            var c = encoder.Encode(new GameState(cars, new[] { true }, new[] { 0 }, 2));
            Assert.That(a.Equals(c), Is.True); Assert.That(a.Equals(b), Is.EqualTo(!lights));
        }

        [Test]
        public void ExitedCarPoseColorAndMud_AreCanonicalized()
        {
            var level = KeyBoard(false); var encoder = new StateKeyEncoder(level);
            var a = new GameState(new[] { new CarState(2, 2, Axis.Horizontal, TileColor.Pink, alive: false) }, new[] { true }, new[] { 0 });
            var b = new GameState(new[] { new CarState(5, 5, Axis.Vertical, TileColor.Purple, 2, false) }, new[] { true }, new[] { 0 }, 88);
            Assert.That(encoder.Encode(a), Is.EqualTo(encoder.Encode(b)));
        }

        [Test]
        public void WideCoordinatesAndMoreThan128Bits_ArePreserved()
        {
            var wide = new LevelDefBuilder(300, 300); wide.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            var level = wide.Build(); var encoder = new StateKeyEncoder(level);
            var a = new GameState(new[] { new CarState(0, 0, Axis.Horizontal, TileColor.Pink) }, new bool[0], new int[0]);
            var b = new GameState(new[] { new CarState(256, 256, Axis.Horizontal, TileColor.Pink) }, new bool[0], new int[0]);
            Assert.That(encoder.Encode(a), Is.Not.EqualTo(encoder.Encode(b)));
            var large = new LevelDefBuilder(7, 7);
            for (int r = 0; r < 5; r++) for (int c = 0; c < 6; c += 2) large.Cars.Add(new CarDef(Axis.Horizontal, r, c, TileColor.Pink));
            var largeLevel = large.Build();
            Assert.That(new StateKeyEncoder(largeLevel).Encode(GameState.Initial(largeLevel)).ByteCount, Is.GreaterThan(16));
        }

        [Test]
        public void HashCollision_DoesNotMergeDifferentKeys()
        {
            var level = KeyBoard(false); var encoder = new StateKeyEncoder(level);
            var dictionary = new Dictionary<StateKey, int>(new ConstantHashComparer());
            for (int r = 0; r < 6; r++)
            {
                var state = new GameState(new[] { new CarState(r, 2, Axis.Horizontal, TileColor.Pink) }, new[] { true }, new[] { 0 });
                dictionary.Add(encoder.Encode(state), r);
            }
            Assert.That(dictionary.Count, Is.EqualTo(6));
            for (int r = 0; r < 6; r++)
            {
                var state = new GameState(new[] { new CarState(r, 2, Axis.Horizontal, TileColor.Pink) }, new[] { true }, new[] { 0 });
                Assert.That(dictionary[encoder.Encode(state)], Is.EqualTo(r));
            }
        }

        [Test]
        public void InvalidStateCannotSilentlyTruncateIntoAnotherKey()
        {
            var level = KeyBoard(false); var encoder = new StateKeyEncoder(level);
            Assert.Throws<ArgumentException>(() => encoder.Encode(new GameState(new CarState[0], new[] { true }, new[] { 0 })));
            Assert.Throws<ArgumentException>(() => encoder.Encode(new GameState(new[] { new CarState(8, 2, Axis.Horizontal, TileColor.Pink) }, new[] { true }, new[] { 0 })));
            Assert.Throws<ArgumentException>(() => encoder.Encode(new GameState(new[] { new CarState(2, 2, Axis.Horizontal, TileColor.Water) }, new[] { true }, new[] { 0 })));
            Assert.Throws<ArgumentException>(() => encoder.Encode(new GameState(new[] { new CarState(2, 2, Axis.Horizontal, TileColor.Pink) }, new[] { true }, new[] { 4 })));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void AllReachableStatesOnSmallBoards_HaveExactKeys(int board)
        {
            var level = SmallBoard(board); var encoder = new StateKeyEncoder(level);
            var byText = new Dictionary<string, StateKey>();
            var byKey = new Dictionary<StateKey, string>(StateKeyComparer.Instance);
            var queue = new Queue<GameState>(); queue.Enqueue(GameState.Initial(level));
            while (queue.Count > 0)
            {
                var state = queue.Dequeue(); string text = ReferenceKey(level, state); var key = encoder.Encode(state);
                if (byKey.TryGetValue(key, out var existing)) Assert.That(text, Is.EqualTo(existing), "Khóa byte gộp nhầm trạng thái luật");
                if (byText.TryGetValue(text, out var prior)) { Assert.That(key, Is.EqualTo(prior)); continue; }
                byKey.Add(key, text); byText.Add(text, key);
                Assert.That(byText.Count, Is.LessThan(5000), "Màn test phải duyệt hết, không được cắt đồ thị");
                // Bộ duyệt độc lập với cursor của solver, dùng trực tiếp luật đã kiểm ở 006.
                for (int i = 0; i < state.CarCount; i++)
                {
                    if (!state.CarAt(i).Alive) continue;
                    Enqueue(queue, MoveResolver.Move(level, state, i, 1, false));
                    Enqueue(queue, MoveResolver.Move(level, state, i, -1, false));
                    if (level.Tables.Count > 0) Enqueue(queue, TableRule.Rotate(level, state, i, false));
                }
                for (int i = 0; i < state.PylonCount; i++)
                    if (!level.Pylons[i].Fixed) Enqueue(queue, PylonRule.RotatePylon(level, state, i, false));
            }
            Assert.That(byKey.Count, Is.EqualTo(byText.Count)); Assert.That(byKey.Count, Is.GreaterThan(1));
            TestContext.WriteLine($"KEY_GRAPH {board}: đã duyệt hết {byKey.Count} trạng thái luật.");
        }

        private static void Enqueue(Queue<GameState> queue, MoveResult result) { if (result.Moved) queue.Enqueue(result.State); }
        private static string ReferenceKey(LevelDef level, GameState state)
        {
            // Chuỗi chỉ dùng làm oracle trong test, độc lập cách đóng gói bit của production.
            var key = new StringBuilder();
            for (int i = 0; i < state.CarCount; i++)
            {
                var car = state.CarAt(i);
                if (!car.Alive) { key.Append("dead;"); continue; }
                key.Append(car.R).Append(',').Append(car.C).Append(',').Append(car.Axis).Append(',').Append(car.Color).Append(',').Append(car.Mud).Append(';');
            }
            key.Append('|'); for (int i = 0; i < state.MudCount; i++) key.Append(state.MudAt(i) ? '1' : '0');
            key.Append('|'); for (int i = 0; i < state.PylonCount; i++) key.Append(state.PylonTurnsAt(i));
            if (level.Lights.Count > 0) key.Append('|').Append(state.Moves & 1);
            return key.ToString();
        }
        private static LevelDef KeyBoard(bool lights)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(new CarDef(Axis.Horizontal, 2, 2, TileColor.Pink));
            b.Muds.Add(new Cell(1, 1)); if (lights) b.Lights.Add(new LightDef(0, 0, 0));
            b.Pylons.Add(new PylonDef(4, 4, 0, new[] { new PortalFace(0, 6, Side.N), new PortalFace(7, 6, Side.S) }));
            return b.Build();
        }
        private static LevelDef SmallBoard(int kind)
        {
            var b = new LevelDefBuilder(4, 4);
            if (kind == 0)
            {
                b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink)); b.Cars.Add(new CarDef(Axis.Vertical, 1, 3, TileColor.Blue));
                b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink)); b.Gates.Add(new GateDef(GateEdge.Bottom, 3, TileColor.Blue));
            }
            else if (kind == 1)
            {
                b.Cars.Add(new CarDef(Axis.Horizontal, 1, 1, TileColor.Pink)); b.Tables.Add(new Cell(1, 1));
                b.Paints.Add(new PaintDef(1, 0, TileColor.Blue)); b.Muds.Add(new Cell(3, 1)); b.Sprays.Add(new Cell(2, 2));
                b.Lights.Add(new LightDef(0, 0, 0));
            }
            else
            {
                b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink)); b.Gates.Add(new GateDef(GateEdge.Right, 3, TileColor.Pink));
                b.Pylons.Add(new PylonDef(1, 1, 0, new[] { new PortalFace(3, 0, Side.W), new PortalFace(0, 3, Side.N) }));
            }
            return b.Build();
        }
        private sealed class ConstantHashComparer : IEqualityComparer<StateKey>
        {
            public bool Equals(StateKey x, StateKey y) => x.Equals(y);
            public int GetHashCode(StateKey obj) => 0;
        }
    }
}
