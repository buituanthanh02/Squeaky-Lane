using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using static SqueakyLane.Tests.EditMode.RuleTestSupport;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class PushRuleTests
    {
        [TestCase(1)] [TestCase(-1)]
        public void Truck_PushesPerpendicularCarOneCellAndStops(int sign)
        {
            var b = new LevelDefBuilder(10, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 1, TileColor.Pink, 3, true), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 4, TileColor.Blue), 10, sign));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(result.State.CarAt(0).C, Is.EqualTo(initial.CarAt(0).C + sign));
            Assert.That(result.State.CarAt(1).C, Is.EqualTo(initial.CarAt(1).C + sign));
            Assert.That(result.State.CarAt(1).Axis, Is.EqualTo(Axis.Vertical));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Push));
            Assert.That(Find(result, MoveEventKind.Push).PushedCars.Count, Is.EqualTo(1));
            Assert.That(result.Events[0].Kind, Is.EqualTo(MoveEventKind.Push));
            Assert.That(result.Events[1].Kind, Is.EqualTo(MoveEventKind.Step));
            Assert.That(initial.Moves, Is.Zero);
        }

        [TestCase(1)] [TestCase(-1)]
        public void Truck_PushesWholeTwoCarChain(int sign)
        {
            var b = new LevelDefBuilder(10, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 1, TileColor.Pink, 3, true), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 4, TileColor.Blue), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 5, TileColor.Yellow), 10, sign));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            var push = Find(result, MoveEventKind.Push);
            Assert.That(push.PushedCars.Count, Is.EqualTo(2));
            for (int i = 0; i < 3; i++) Assert.That(result.State.CarAt(i).C, Is.EqualTo(initial.CarAt(i).C + sign));
            Assert.That(push.PushedCars[0].Car, Is.EqualTo(1)); Assert.That(push.PushedCars[1].Car, Is.EqualTo(2));
        }

        [TestCase(1, false)] [TestCase(-1, false)]
        [TestCase(1, true)] [TestCase(-1, true)]
        public void Truck_CannotPushIntoWallOrPastBoundary(int sign, bool boundary)
        {
            int target = boundary ? 9 : 4, truck = target - 3;
            var b = new LevelDefBuilder(10, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, truck, TileColor.Pink, 3, true), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, target, TileColor.Blue), 10, sign));
            if (!boundary) b.Cones.Add(new Cell(2, MirrorCell(5, 10, sign)));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Car));
        }

        [TestCase(1)] [TestCase(-1)]
        public void Truck_PushedCarReceivesNewTileEffectsInOrder(int sign)
        {
            var b = new LevelDefBuilder(10, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 1, TileColor.Pink, 3, true), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 4, TileColor.Blue), 10, sign));
            int c = MirrorCell(5, 10, sign);
            b.Paints.Add(new PaintDef(2, c, TileColor.Yellow)); b.Muds.Add(new Cell(2, c)); b.Sprays.Add(new Cell(2, c));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.State.CarAt(1).Color, Is.EqualTo(TileColor.Yellow)); Assert.That(result.State.CarAt(1).Mud, Is.Zero);
            var fx = Find(result, MoveEventKind.Push).PushedCars[0].Effects;
            Assert.That(fx.Count, Is.EqualTo(3));
            Assert.That(fx[0].Kind, Is.EqualTo(MoveEventKind.Paint)); Assert.That(fx[1].Kind, Is.EqualTo(MoveEventKind.Mud));
            Assert.That(fx[2].Kind, Is.EqualTo(MoveEventKind.Spray));
        }

        [TestCase(1)] [TestCase(-1)]
        public void PushingFromAdjacentPosition_CanSplashEvenWithoutRunUp(int sign)
        {
            var b = new LevelDefBuilder(10, 5) { Splash = true };
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 1, TileColor.Pink, 3, true, 1), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 4, TileColor.Blue), 10, sign));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.Moved, Is.True); Assert.That(result.State.CarAt(1).Mud, Is.EqualTo(1));
            Assert.That(result.Events[0].Kind, Is.EqualTo(MoveEventKind.Push));
            Assert.That(result.Events[1].Kind, Is.EqualTo(MoveEventKind.Splash));
            Assert.That(result.Events[2].Kind, Is.EqualTo(MoveEventKind.Step));
        }

        [TestCase(1)] [TestCase(-1)]
        public void PushedCar_DoesNotReapplyEffectsOnCellsItAlreadyCovered(int sign)
        {
            var b = new LevelDefBuilder(10, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink, 3, true), 10, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 3, TileColor.Blue), 10, sign));
            b.Paints.Add(new PaintDef(3, MirrorCell(4, 10, sign), TileColor.Yellow));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.State.CarAt(1).Color, Is.EqualTo(TileColor.Blue));
            Assert.That(Find(result, MoveEventKind.Push).PushedCars[0].Effects, Is.Empty);
        }

        [Test]
        public void EventFreePush_ProducesSameStateWithoutEventObjects()
        {
            var b = new LevelDefBuilder(10, 5) { Splash = true };
            b.Cars.Add(new CarDef(Axis.Horizontal, 3, 1, TileColor.Pink, 3, true, 1));
            b.Cars.Add(new CarDef(Axis.Vertical, 2, 4, TileColor.Blue));
            b.Paints.Add(new PaintDef(2, 5, TileColor.Yellow)); b.Muds.Add(new Cell(3, 5));
            var level = b.Build(); var initial = GameState.Initial(level);
            var recorded = MoveResolver.Move(level, initial, 0, 1);
            var silent = MoveResolver.Move(level, initial, 0, 1, false);
            SameState(recorded.State, silent.State);
            Assert.That(silent.Moved, Is.EqualTo(recorded.Moved)); Assert.That(silent.Events, Is.Empty);
            Assert.That(recorded.Events.Count, Is.GreaterThan(0));
        }
    }
}
