using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using static SqueakyLane.Tests.EditMode.RuleTestSupport;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class PortalPylonTests
    {
        [TestCase(Axis.Horizontal, 1, Side.N, Axis.Vertical, 0, 5, 1)]
        [TestCase(Axis.Horizontal, -1, Side.S, Axis.Vertical, 6, 5, -1)]
        [TestCase(Axis.Vertical, 1, Side.W, Axis.Horizontal, 5, 0, 1)]
        [TestCase(Axis.Vertical, -1, Side.E, Axis.Horizontal, 5, 6, -1)]
        [TestCase(Axis.Horizontal, 1, Side.E, Axis.Horizontal, 5, 6, -1)]
        [TestCase(Axis.Horizontal, -1, Side.W, Axis.Horizontal, 5, 0, 1)]
        [TestCase(Axis.Vertical, 1, Side.S, Axis.Vertical, 6, 5, -1)]
        [TestCase(Axis.Vertical, -1, Side.N, Axis.Vertical, 0, 5, 1)]
        public void Portal_PlacesWholeCarAndUsesDestinationDirection(Axis sourceAxis, int sourceSign,
            Side targetSide, Axis expectedAxis, int expectedR, int expectedC, int expectedSign)
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(BoundaryCar(sourceAxis, sourceSign));
            var source = SourceFace(sourceAxis, sourceSign); var target = Destination(targetSide);
            b.Portals.Add(new PortalDef(source, target)); BlockAfterLanding(b, targetSide);
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sourceSign);
            Assert.That(result.Moved, Is.True); Assert.That(result.Exited, Is.False);
            Assert.That(result.State.CarAt(0).R, Is.EqualTo(expectedR));
            Assert.That(result.State.CarAt(0).C, Is.EqualTo(expectedC));
            Assert.That(result.State.CarAt(0).Axis, Is.EqualTo(expectedAxis));
            var tele = Find(result, MoveEventKind.Tele);
            Assert.That(tele.FromFace.Value, Is.EqualTo(source)); Assert.That(tele.ToFace.Value, Is.EqualTo(target));
            Assert.That(tele.R, Is.EqualTo(expectedR)); Assert.That(tele.C, Is.EqualTo(expectedC));
            Assert.That(tele.Sign, Is.EqualTo(expectedSign)); Assert.That(tele.Axis, Is.EqualTo(expectedAxis));
            Assert.That(result.Events[0].Kind, Is.EqualTo(MoveEventKind.Tele));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
            Assert.That(result.State.Moves, Is.EqualTo(1)); Assert.That(initial.Moves, Is.Zero);
            Assert.That(initial.CarAt(0).Axis, Is.EqualTo(sourceAxis));
            var silent = MoveResolver.Move(level, initial, 0, sourceSign, false);
            SameState(result.State, silent.State); Assert.That(silent.Events, Is.Empty);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Portal_BlockedDestinationActsAsWallEvenWithMatchingGate(int blocker)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(BoundaryCar(Axis.Horizontal, 1));
            var target = blocker == 2 ? new PortalFace(7, 5, Side.N) : Destination(Side.N);
            b.Portals.Add(new PortalDef(SourceFace(Axis.Horizontal, 1), target));
            b.Gates.Add(new GateDef(GateEdge.Right, 2, TileColor.Pink));
            if (blocker == 0) b.Cones.Add(new Cell(1, 5));
            if (blocker == 1) b.Cars.Add(new CarDef(Axis.Horizontal, 1, 4, TileColor.Blue));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, 1);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial));
            Assert.That(result.Exited, Is.False); Assert.That(Count(result, MoveEventKind.Tele), Is.Zero);
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [Test]
        public void Portal_ChecksThirdCellOfTruck()
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(new CarDef(Axis.Horizontal, 4, 5, TileColor.Pink, 3, true));
            b.Portals.Add(new PortalDef(new PortalFace(4, 7, Side.E), Destination(Side.N)));
            b.Cones.Add(new Cell(2, 5));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, 1);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [Test]
        public void Portal_SecondCrossingIsBlockedInSameCommand()
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(BoundaryCar(Axis.Horizontal, 1));
            b.Portals.Add(new PortalDef(SourceFace(Axis.Horizontal, 1), Destination(Side.W)));
            b.Portals.Add(new PortalDef(new PortalFace(5, 7, Side.E), new PortalFace(6, 0, Side.W)));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(Count(result, MoveEventKind.Tele), Is.EqualTo(1));
            Assert.That(result.State.CarAt(0).R, Is.EqualTo(5)); Assert.That(result.State.CarAt(0).C, Is.EqualTo(6));
            Assert.That(result.State.Moves, Is.EqualTo(1));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [Test]
        public void Portal_RemainsUsedAfterWaterBounce()
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(new CarDef(Axis.Horizontal, 2, 6, TileColor.Pink, mud: 1));
            b.Portals.Add(new PortalDef(SourceFace(Axis.Horizontal, 1), Destination(Side.W)));
            b.Gates.Add(new GateDef(GateEdge.Right, 5, TileColor.Water));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(Count(result, MoveEventKind.Tele), Is.EqualTo(1)); Assert.That(Count(result, MoveEventKind.Water), Is.EqualTo(1));
            Assert.That(result.State.CarAt(0).C, Is.Zero); Assert.That(result.State.CarAt(0).R, Is.EqualTo(5));
            Assert.That(result.State.CarAt(0).Mud, Is.Zero); Assert.That(result.State.Moves, Is.EqualTo(1));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [TestCase(Side.N, 0, 1)] [TestCase(Side.S, 7, 6)]
        public void Portal_AppliesLandingTilesFromTailToHead(Side side, int tailR, int headR)
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(BoundaryCar(Axis.Horizontal, 1));
            b.Portals.Add(new PortalDef(SourceFace(Axis.Horizontal, 1), Destination(side))); BlockAfterLanding(b, side);
            b.Paints.Add(new PaintDef(tailR, 5, TileColor.Blue)); b.Muds.Add(new Cell(tailR, 5)); b.Sprays.Add(new Cell(headR, 5));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, 1);
            Assert.That(result.State.CarAt(0).Color, Is.EqualTo(TileColor.Blue)); Assert.That(result.State.CarAt(0).Mud, Is.Zero);
            Assert.That(result.State.MudAt(0), Is.False); Assert.That(initial.MudAt(0), Is.True);
            Assert.That(result.Events.Count, Is.EqualTo(5));
            Assert.That(result.Events[0].Kind, Is.EqualTo(MoveEventKind.Tele));
            Assert.That(result.Events[1].Kind, Is.EqualTo(MoveEventKind.Paint));
            Assert.That(result.Events[2].Kind, Is.EqualTo(MoveEventKind.Mud));
            Assert.That(result.Events[3].Kind, Is.EqualTo(MoveEventKind.Spray));
            Assert.That(result.Events[4].Kind, Is.EqualTo(MoveEventKind.Stop));
            var silent = MoveResolver.Move(level, initial, 0, 1, false);
            SameState(result.State, silent.State); Assert.That(silent.Events, Is.Empty);
        }

        [Test]
        public void Portal_TruckCanPushAlongItsNewAxis()
        {
            var b = new LevelDefBuilder(8, 8);
            b.Cars.Add(new CarDef(Axis.Horizontal, 2, 5, TileColor.Pink, 3, true));
            b.Cars.Add(new CarDef(Axis.Horizontal, 4, 4, TileColor.Blue));
            b.Portals.Add(new PortalDef(SourceFace(Axis.Horizontal, 1), Destination(Side.N)));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, 1);
            Assert.That(result.State.CarAt(0).Axis, Is.EqualTo(Axis.Vertical));
            Assert.That(result.State.CarAt(0).R, Is.EqualTo(2)); Assert.That(result.State.CarAt(0).C, Is.EqualTo(5));
            Assert.That(result.State.CarAt(1).R, Is.EqualTo(5)); Assert.That(result.State.CarAt(1).C, Is.EqualTo(4));
            Assert.That(Count(result, MoveEventKind.Push), Is.EqualTo(1));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Push));
            Assert.That(initial.CarAt(1).R, Is.EqualTo(4)); Assert.That(initial.CarAt(0).Axis, Is.EqualTo(Axis.Horizontal));
        }

        [Test]
        public void Portal_FirstMatchingLinkWinsEvenIfBlocked()
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(BoundaryCar(Axis.Horizontal, 1));
            var origin = SourceFace(Axis.Horizontal, 1);
            b.Portals.Add(new PortalDef(origin, Destination(Side.N)));
            b.Portals.Add(new PortalDef(origin, Destination(Side.W))); b.Cones.Add(new Cell(1, 5));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(result.Moved, Is.False); Assert.That(Count(result, MoveEventKind.Tele), Is.Zero);
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [Test]
        public void Portal_WrongFacingDoesNotReplaceColorGate()
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(BoundaryCar(Axis.Horizontal, 1));
            b.Portals.Add(new PortalDef(new PortalFace(2, 7, Side.N), Destination(Side.W)));
            b.Gates.Add(new GateDef(GateEdge.Right, 2, TileColor.Pink));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(result.Exited, Is.True); Assert.That(Count(result, MoveEventKind.Tele), Is.Zero);
        }

        [TestCase(0, 2, 3, Side.S, 4, 3, Side.N)]
        [TestCase(1, 3, 2, Side.E, 3, 4, Side.W)]
        [TestCase(2, 4, 3, Side.N, 2, 3, Side.S)]
        [TestCase(3, 3, 4, Side.W, 3, 2, Side.E)]
        public void Pylon_FacesKeepOrderedLinksAtAllFourTurns(int turns, int r0, int c0, Side side0, int r1, int c1, Side side1)
        {
            PylonRule.PylonFaces(Pylon(turns), turns, out var first, out var second);
            Assert.That(first, Is.EqualTo(new PortalFace(r0, c0, side0)));
            Assert.That(second, Is.EqualTo(new PortalFace(r1, c1, side1)));
        }

        [Test]
        public void Pylon_FourRotationsReturnToStartEvenWithAdjacentCar()
        {
            var b = new LevelDefBuilder(8, 8); b.Pylons.Add(Pylon(0));
            b.Cars.Add(new CarDef(Axis.Horizontal, 2, 2, TileColor.Pink));
            var level = b.Build(); var initial = GameState.Initial(level); var state = initial;
            for (int i = 1; i <= 4; i++)
            {
                var result = PylonRule.RotatePylon(level, state, 0);
                Assert.That(result.Moved, Is.True); Assert.That(result.Events.Count, Is.EqualTo(1));
                Assert.That(result.Events[0].Kind, Is.EqualTo(MoveEventKind.Pyl)); Assert.That(result.Events[0].PylonIndex, Is.Zero);
                Assert.That(result.State.Moves, Is.EqualTo(i)); Assert.That(result.State.PylonTurnsAt(0), Is.EqualTo(i % 4));
                state = result.State;
            }
            Assert.That(initial.PylonTurnsAt(0), Is.Zero); Assert.That(initial.Moves, Is.Zero);
            Assert.That(state.CarAt(0), Is.EqualTo(initial.CarAt(0)));
            var silent = PylonRule.RotatePylon(level, initial, 0, false);
            SameState(PylonRule.RotatePylon(level, initial, 0).State, silent.State); Assert.That(silent.Events, Is.Empty);
        }

        [TestCase(-1)] [TestCase(0)] [TestCase(1)]
        public void Pylon_FixedOrMissingPylonIsNoOp(int index)
        {
            var b = new LevelDefBuilder(8, 8); b.Pylons.Add(Pylon(0, true));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = PylonRule.RotatePylon(level, initial, index);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial)); Assert.That(result.Events, Is.Empty);
        }

        [TestCase(0, Axis.Vertical, 0, 3, Side.N, 0)]
        [TestCase(1, Axis.Horizontal, 3, 0, Side.N, 0)]
        [TestCase(2, Axis.Vertical, 0, 3, Side.S, 6)]
        [TestCase(3, Axis.Horizontal, 3, 0, Side.S, 6)]
        public void Pylon_EntryUsesCurrentOrientationAndCorrectTarget(int turns, Axis axis, int r, int c, Side targetSide, int expectedR)
        {
            var b = new LevelDefBuilder(8, 8); b.Pylons.Add(Pylon(turns));
            b.Cars.Add(new CarDef(axis, r, c, TileColor.Pink));
            b.Cones.Add(new Cell(targetSide == Side.N ? 2 : 5, 6));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(Count(result, MoveEventKind.Tele), Is.EqualTo(1));
            Assert.That(result.State.CarAt(0).R, Is.EqualTo(expectedR)); Assert.That(result.State.CarAt(0).C, Is.EqualTo(6));
            Assert.That(Find(result, MoveEventKind.Tele).ToFace.Value.Side, Is.EqualTo(targetSide));
        }

        [Test]
        public void Pylon_NonPortalSideIsWallAndRotationChangesActiveEntry()
        {
            var b = new LevelDefBuilder(8, 8); b.Pylons.Add(Pylon(0));
            b.Cars.Add(new CarDef(Axis.Vertical, 0, 3, TileColor.Pink)); b.Cones.Add(new Cell(2, 6));
            var level = b.Build(); var initial = GameState.Initial(level);
            Assert.That(Count(MoveResolver.Move(level, initial, 0, 1), MoveEventKind.Tele), Is.EqualTo(1));
            var rotated = PylonRule.RotatePylon(level, initial, 0).State;
            var blocked = MoveResolver.Move(level, rotated, 0, 1);
            Assert.That(Count(blocked, MoveEventKind.Tele), Is.Zero);
            Assert.That(blocked.State.CarAt(0).R, Is.EqualTo(1));
            Assert.That(Find(blocked, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Wall));
        }

        [Test]
        public void Pylon_LinkWorksBackFromDestinationToPylonFace()
        {
            var b = new LevelDefBuilder(8, 8); b.Pylons.Add(Pylon(0));
            b.Cars.Add(new CarDef(Axis.Vertical, 0, 6, TileColor.Pink)); b.Cones.Add(new Cell(0, 3));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, -1);
            var tele = Find(result, MoveEventKind.Tele);
            Assert.That(tele.ToFace.Value, Is.EqualTo(new PortalFace(2, 3, Side.S)));
            Assert.That(tele.Sign, Is.EqualTo(-1)); Assert.That(result.State.CarAt(0).R, Is.EqualTo(1));
            Assert.That(result.State.CarAt(0).C, Is.EqualTo(3));
        }

        [Test]
        public void Portal_OrdinaryLinkHasPriorityOverPylonLink()
        {
            var b = new LevelDefBuilder(8, 8); b.Cars.Add(BoundaryCar(Axis.Horizontal, 1));
            var origin = SourceFace(Axis.Horizontal, 1);
            b.Portals.Add(new PortalDef(origin, Destination(Side.W))); BlockAfterLanding(b, Side.W);
            b.Pylons.Add(new PylonDef(3, 3, 0, new[] { origin, new PortalFace(7, 6, Side.S) }));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(Find(result, MoveEventKind.Tele).ToFace.Value, Is.EqualTo(Destination(Side.W)));
            Assert.That(result.State.CarAt(0).R, Is.EqualTo(5)); Assert.That(result.State.CarAt(0).C, Is.Zero);
        }

        private static PylonDef Pylon(int turns, bool fixedRotation = false) =>
            new PylonDef(3, 3, turns, new[] { new PortalFace(0, 6, Side.N), new PortalFace(7, 6, Side.S) }, fixedRotation);

        private static CarDef BoundaryCar(Axis axis, int sign) => axis == Axis.Horizontal
            ? new CarDef(axis, 2, sign > 0 ? 6 : 0, TileColor.Pink)
            : new CarDef(axis, sign > 0 ? 6 : 0, 2, TileColor.Pink);

        private static PortalFace SourceFace(Axis axis, int sign) => axis == Axis.Horizontal
            ? new PortalFace(2, sign > 0 ? 7 : 0, sign > 0 ? Side.E : Side.W)
            : new PortalFace(sign > 0 ? 7 : 0, 2, sign > 0 ? Side.S : Side.N);

        private static PortalFace Destination(Side side)
        {
            switch (side)
            {
                case Side.N: return new PortalFace(0, 5, side);
                case Side.S: return new PortalFace(7, 5, side);
                case Side.W: return new PortalFace(5, 0, side);
                default: return new PortalFace(5, 7, side);
            }
        }

        private static void BlockAfterLanding(LevelDefBuilder b, Side side)
        {
            switch (side)
            {
                case Side.N: b.Cones.Add(new Cell(2, 5)); break;
                case Side.S: b.Cones.Add(new Cell(5, 5)); break;
                case Side.W: b.Cones.Add(new Cell(5, 2)); break;
                case Side.E: b.Cones.Add(new Cell(5, 5)); break;
            }
        }
    }
}
