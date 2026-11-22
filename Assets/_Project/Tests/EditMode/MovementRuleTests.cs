using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using static SqueakyLane.Tests.EditMode.RuleTestSupport;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class MovementRuleTests
    {
        [TestCase(Axis.Horizontal, 1)] [TestCase(Axis.Horizontal, -1)]
        [TestCase(Axis.Vertical, 1)] [TestCase(Axis.Vertical, -1)]
        public void MatchingCleanGate_Exits(Axis axis, int sign)
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(LaneCar(axis, 3));
            b.Gates.Add(new GateDef(Edge(axis, sign), 3, TileColor.Pink));
            var level = b.Build(); var original = GameState.Initial(level);
            var result = MoveResolver.Move(level, original, 0, sign);
            Assert.That(result.Moved && result.Exited, Is.True);
            Assert.That(result.State.CarAt(0).Alive, Is.False);
            Assert.That(GameRules.IsWin(result.State), Is.True);
            Assert.That(GameRules.WashedCount(result.State), Is.EqualTo(1));
            Assert.That(result.State.Moves, Is.EqualTo(1));
            Assert.That(original.CarAt(0).Alive, Is.True);
            Assert.That(Find(result, MoveEventKind.Exit).Gate.Value.Edge, Is.EqualTo(Edge(axis, sign)));
            Assert.That(Find(result, MoveEventKind.Exit).Sign, Is.EqualTo(sign));
        }

        [TestCase(Axis.Horizontal, 1)] [TestCase(Axis.Horizontal, -1)]
        [TestCase(Axis.Vertical, 1)] [TestCase(Axis.Vertical, -1)]
        public void WrongColor_StopsAtEdge(Axis axis, int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(axis, 3));
            b.Gates.Add(new GateDef(Edge(axis, sign), 3, TileColor.Blue));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.Moved, Is.True);
            Assert.That(result.Exited, Is.False);
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.WrongGate));
            var car = result.State.CarAt(0);
            Assert.That(axis == Axis.Horizontal ? car.C : car.R, Is.EqualTo(sign > 0 ? 6 : 0));
        }

        [TestCase(Axis.Horizontal, 1)] [TestCase(Axis.Horizontal, -1)]
        [TestCase(Axis.Vertical, 1)] [TestCase(Axis.Vertical, -1)]
        public void MuddyCar_CannotExitEvenMatchingColor(Axis axis, int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(axis, 3, 1));
            b.Gates.Add(new GateDef(Edge(axis, sign), 3, TileColor.Pink));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.Exited, Is.False);
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Muddy));
        }

        [TestCase(Axis.Horizontal, 1)] [TestCase(Axis.Horizontal, -1)]
        [TestCase(Axis.Vertical, 1)] [TestCase(Axis.Vertical, -1)]
        public void Water_CleansBouncesAndCanExitOppositeGateInSameMove(Axis axis, int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(axis, 3, 1));
            b.Gates.Add(new GateDef(Edge(axis, sign), 3, TileColor.Water));
            b.Gates.Add(new GateDef(Edge(axis, -sign), 3, TileColor.Pink));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.Exited, Is.True);
            Assert.That(result.State.CarAt(0).Mud, Is.Zero);
            Assert.That(result.State.Moves, Is.EqualTo(1));
            Assert.That(Count(result, MoveEventKind.Water), Is.EqualTo(1));
            Assert.That(Find(result, MoveEventKind.Water).HadMud, Is.EqualTo(1));
            Assert.That(Find(result, MoveEventKind.Water).Sign, Is.EqualTo(sign));
            Assert.That(Find(result, MoveEventKind.Exit).Sign, Is.EqualTo(-sign));
        }

        [TestCase(Axis.Horizontal, 1)] [TestCase(Axis.Horizontal, -1)]
        [TestCase(Axis.Vertical, 1)] [TestCase(Axis.Vertical, -1)]
        public void TwoWaterGates_BounceOnlyOnce(Axis axis, int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(axis, 3, 1));
            b.Gates.Add(new GateDef(Edge(axis, sign), 3, TileColor.Water));
            b.Gates.Add(new GateDef(Edge(axis, -sign), 3, TileColor.Water));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(Count(result, MoveEventKind.Water), Is.EqualTo(1));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Edge));
            Assert.That(result.State.CarAt(0).Alive, Is.True);
            Assert.That(result.State.Moves, Is.EqualTo(1));
        }

        [TestCase(1)] [TestCase(-1)]
        public void WaterAtInitialBoundary_IsStillAnEffectiveMove(int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(Axis.Horizontal, sign > 0 ? 6 : 0, 1));
            b.Gates.Add(new GateDef(Edge(Axis.Horizontal, sign), 3, TileColor.Water));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.Moved, Is.True); Assert.That(result.State.Moves, Is.EqualTo(1));
            Assert.That(result.State.CarAt(0).Mud, Is.Zero);
        }

        [TestCase(Axis.Horizontal, 1)] [TestCase(Axis.Horizontal, -1)]
        [TestCase(Axis.Vertical, 1)] [TestCase(Axis.Vertical, -1)]
        public void AdjacentCone_IsNoOpAndKeepsOriginalReference(Axis axis, int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(axis, 3));
            int ahead = sign > 0 ? 5 : 2;
            b.Cones.Add(axis == Axis.Horizontal ? new Cell(3, ahead) : new Cell(ahead, 3));
            var level = b.Build(); var original = GameState.Initial(level);
            var result = MoveResolver.Move(level, original, 0, sign);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(original));
            Assert.That(result.State.Moves, Is.Zero);
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [TestCase(1)] [TestCase(-1)]
        public void AlreadyAtWrongGate_DoesNotConsumeTurn(int sign)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(LaneCar(Axis.Horizontal, sign > 0 ? 6 : 0));
            b.Gates.Add(new GateDef(Edge(Axis.Horizontal, sign), 3, TileColor.Blue));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.WrongGate));
        }

        [TestCase(1)] [TestCase(-1)]
        public void Lights_ChangeOnlyAfterAnEffectiveTurn(int sign)
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(LaneCar(Axis.Horizontal, 3)); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Blue));
            b.Lights.Add(new LightDef(3, sign > 0 ? 5 : 2, 1));
            var level = b.Build(); var initial = GameState.Initial(level);
            var blocked = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(blocked.Moved, Is.False);
            Assert.That(LightRule.IsRed(blocked.State, level.Lights[0]), Is.True);
            var tick = MoveResolver.Move(level, blocked.State, 1, 1);
            Assert.That(LightRule.IsRed(tick.State, level.Lights[0]), Is.False);
            var allowed = MoveResolver.Move(level, tick.State, 0, sign);
            Assert.That(allowed.Moved, Is.True);
            Assert.That(allowed.State.CarAt(0).C, Is.EqualTo(sign > 0 ? 6 : 0));
            Assert.That(allowed.State.Moves, Is.EqualTo(2));
            Assert.That(LightRule.IsRed(allowed.State, level.Lights[0]), Is.True);
        }

        [Test]
        public void Occupancy_UsesCarIndicesAndCanSkipMovingCar()
        {
            var b = new LevelDefBuilder(5, 5); b.Cars.Add(new CarDef(Axis.Vertical, 1, 1, TileColor.Blue));
            b.Cones.Add(new Cell(2, 4)); b.Lights.Add(new LightDef(0, 3, 1));
            var level = b.Build(); var state = GameState.Initial(level);
            var map = Occupancy.Build(level, state);
            Assert.That(map[1 * 5 + 1], Is.Zero); Assert.That(map[2 * 5 + 1], Is.Zero);
            Assert.That(map[2 * 5 + 4], Is.EqualTo(Occupancy.Obstacle));
            Assert.That(map[3], Is.EqualTo(Occupancy.Obstacle));
            Assert.That(Occupancy.Build(level, state, 0)[6], Is.EqualTo(Occupancy.Empty));
        }

        [Test]
        public void MovingExitedCar_IsNoOp()
        {
            var b = new LevelDefBuilder(4, 4); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink));
            var level = b.Build(); var won = MoveResolver.Move(level, GameState.Initial(level), 0, 1).State;
            var again = MoveResolver.Move(level, won, 0, -1);
            Assert.That(again.Moved, Is.False); Assert.That(again.State, Is.SameAs(won));
            Assert.That(again.Events, Is.Empty);
        }
    }
}
