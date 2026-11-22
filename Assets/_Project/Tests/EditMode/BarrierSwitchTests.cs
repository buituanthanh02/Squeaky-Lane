using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using static SqueakyLane.Tests.EditMode.RuleTestSupport;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class BarrierSwitchTests
    {
        [Test]
        public void Barrier_OpensOnlyAfterEnoughCarsExit()
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Blue));
            b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink)); b.Barriers.Add(new BarrierDef(3, 4, 1));
            var level = b.Build(); var initial = GameState.Initial(level);
            var closed = MoveResolver.Move(level, initial, 1, 1);
            Assert.That(closed.State.CarAt(1).C, Is.EqualTo(2));
            Assert.That(Find(closed, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
            var washed = MoveResolver.Move(level, initial, 0, 1);
            Assert.That(washed.Exited, Is.True); Assert.That(GameRules.WashedCount(washed.State), Is.EqualTo(1));
            var opened = BarrierRule.BarriersOpened(level, 0, 1);
            Assert.That(opened.Count, Is.EqualTo(1)); Assert.That(opened[0].Need, Is.EqualTo(1));
            Assert.That(MoveResolver.Move(level, washed.State, 1, 1).State.CarAt(1).C, Is.EqualTo(6));
            Assert.That(GameRules.WashedCount(initial), Is.Zero); Assert.That(initial.Moves, Is.Zero);
        }

        [TestCase(0, 2, 2)] [TestCase(1, 2, 1)] [TestCase(1, 1, 0)] [TestCase(1, 0, 0)]
        public void Barrier_ReportsOnlyThresholdsCrossed(int before, int after, int expectedCount)
        {
            var b = new LevelDefBuilder(8, 8);
            for (int need = 0; need <= 3; need++) b.Barriers.Add(new BarrierDef(0, need, need));
            var level = b.Build(); var result = BarrierRule.BarriersOpened(level, before, after);
            Assert.That(result.Count, Is.EqualTo(expectedCount));
            for (int i = 0; i < result.Count; i++) Assert.That(result[i].Need, Is.EqualTo(before + i + 1));
        }

        [TestCase(0)] [TestCase(50)]
        public void Barrier_CountsWashedCarsInsteadOfMoves(int moves)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            b.Barriers.Add(new BarrierDef(3, 4, 1)); var level = b.Build();
            var state = new GameState(new[] { new CarState(0, 0, Axis.Horizontal, TileColor.Pink) }, new bool[0], new int[0], moves);
            Assert.That(Occupancy.Build(level, state)[3 * 8 + 4], Is.EqualTo(Occupancy.Obstacle));
        }

        [TestCase(false)] [TestCase(true)]
        public void Switch_ChangesBarriersOnlyForNextCommand(bool startsPressed)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink));
            var button = new SwitchDef(3, startsPressed ? 1 : 2,
                new[] { new Cell(3, startsPressed ? 4 : 6) }, new[] { new Cell(3, startsPressed ? 6 : 4) });
            b.Switches.Add(button); var level = b.Build(); var initial = GameState.Initial(level);
            Assert.That(BarrierRule.SwitchPressed(level, initial, button), Is.EqualTo(startsPressed));
            var first = MoveResolver.Move(level, initial, 0, 1);
            // Xe đi qua/rời chốt giữa lượt vẫn dừng ở barie đã đóng từ đầu lượt.
            Assert.That(first.State.CarAt(0).C, Is.EqualTo(2));
            Assert.That(BarrierRule.SwitchPressed(level, first.State, button), Is.EqualTo(!startsPressed));
            Assert.That(Find(first, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
            var second = MoveResolver.Move(level, first.State, 0, 1);
            Assert.That(second.State.CarAt(0).C, Is.EqualTo(4)); Assert.That(second.State.Moves, Is.EqualTo(2));
            Assert.That(initial.CarAt(0).C, Is.Zero); Assert.That(initial.Moves, Is.Zero);
        }

        [Test]
        public void Switch_IncludesSkippedCarButIgnoresExitedCar()
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink));
            var button = new SwitchDef(3, 1, new[] { new Cell(3, 4) }, new[] { new Cell(3, 5) }); b.Switches.Add(button);
            var level = b.Build(); var initial = GameState.Initial(level);
            var map = Occupancy.Build(level, initial, 0);
            Assert.That(BarrierRule.SwitchPressed(level, initial, button), Is.True);
            Assert.That(map[3 * 8 + 4], Is.EqualTo(Occupancy.Obstacle)); Assert.That(map[3 * 8 + 5], Is.EqualTo(Occupancy.Empty));
            var exited = new GameState(new[] { new CarState(3, 0, Axis.Horizontal, TileColor.Pink, alive: false) }, new bool[0], new int[0]);
            Assert.That(BarrierRule.SwitchPressed(level, exited, button), Is.False);
            Assert.That(Occupancy.Build(level, exited)[3 * 8 + 5], Is.EqualTo(Occupancy.Obstacle));
        }

        [Test]
        public void Occupancy_CarOverwritesClosedBarrierButSkipRestoresObstacle()
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink));
            b.Barriers.Add(new BarrierDef(3, 1, 1)); var level = b.Build(); var initial = GameState.Initial(level);
            Assert.That(Occupancy.Build(level, initial)[3 * 8 + 1], Is.Zero);
            Assert.That(Occupancy.Build(level, initial, 0)[3 * 8 + 1], Is.EqualTo(Occupancy.Obstacle));
        }

        [TestCase(1, 0)] [TestCase(-1, 0)]
        [TestCase(1, 1)] [TestCase(-1, 1)]
        [TestCase(1, 2)] [TestCase(-1, 2)]
        public void Truck_CannotPushWholeBodyIntoBarrierRedLightOrSwitch(int sign, int obstacle)
        {
            var b = PushBoard(sign); int c = MirrorCell(4, 8, sign);
            if (obstacle == 0) b.Barriers.Add(new BarrierDef(2, c, 1));
            if (obstacle == 1) b.Lights.Add(new LightDef(2, c, 1));
            if (obstacle == 2) b.Switches.Add(new SwitchDef(0, 0, new Cell[0], new[] { new Cell(2, c) }));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial));
            Assert.That(Count(result, MoveEventKind.Push), Is.Zero);
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Car));
        }

        [TestCase(1, false)] [TestCase(-1, false)] [TestCase(1, true)] [TestCase(-1, true)]
        public void Truck_CanPushAcrossOpenBarrierOrGreenLight(int sign, bool light)
        {
            var b = PushBoard(sign); int c = MirrorCell(4, 8, sign);
            if (light) b.Lights.Add(new LightDef(2, c, 0)); else b.Barriers.Add(new BarrierDef(2, c, 0));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(result.Moved, Is.True); Assert.That(Count(result, MoveEventKind.Push), Is.EqualTo(1));
            Assert.That(result.State.CarAt(1).C, Is.EqualTo(c));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Push));
            var silent = MoveResolver.Move(level, initial, 0, sign, false);
            SameState(result.State, silent.State); Assert.That(silent.Events, Is.Empty);
        }

        [TestCase(false)] [TestCase(true)]
        public void Truck_PushChecksSwitchAtCurrentPositionAfterApproaching(bool startsPressed)
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink, 3, true));
            b.Cars.Add(new CarDef(Axis.Vertical, 2, 4, TileColor.Blue));
            b.Switches.Add(new SwitchDef(3, startsPressed ? 0 : 3, new Cell[0], new[] { new Cell(2, 5) }));
            var level = b.Build(); var initial = GameState.Initial(level);
            // Xe tải tiến 1 ô rồi mới chạm xe đích; lúc đó nó đã vào/rời chốt.
            // Bản đồ chính vẫn chụp đầu lượt, nhưng tryPush phải kiểm state hiện tại như JS.
            var result = MoveResolver.Move(level, initial, 0, 1);
            Assert.That(result.Moved, Is.True);
            Assert.That(result.State.CarAt(0).C, Is.EqualTo(startsPressed ? 1 : 2));
            Assert.That(result.State.CarAt(1).C, Is.EqualTo(startsPressed ? 4 : 5));
            Assert.That(Count(result, MoveEventKind.Push), Is.EqualTo(startsPressed ? 0 : 1));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(startsPressed ? StopReason.Car : StopReason.Push));
            Assert.That(initial.CarAt(0).C, Is.Zero); Assert.That(initial.CarAt(1).C, Is.EqualTo(4));
        }

        private static LevelDefBuilder PushBoard(int sign)
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink, 3, true), 8, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 3, TileColor.Blue), 8, sign));
            return b;
        }
    }
}
