using System;
using System.Collections.Generic;
using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using static SqueakyLane.Tests.EditMode.RuleTestSupport;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class TableAndModelTests
    {
        [TestCase(Axis.Horizontal, 2, 2, Axis.Vertical, 2, 3)]
        [TestCase(Axis.Horizontal, 3, 2, Axis.Vertical, 2, 2)]
        [TestCase(Axis.Vertical, 2, 2, Axis.Horizontal, 2, 2)]
        [TestCase(Axis.Vertical, 2, 3, Axis.Horizontal, 3, 2)]
        public void Table_RotatesClockwiseFromAllFourSides(Axis axis, int r, int c, Axis expectedAxis, int expectedR, int expectedC)
        {
            var b = new LevelDefBuilder(6, 6); b.Tables.Add(new Cell(2, 2));
            b.Cars.Add(new CarDef(axis, r, c, TileColor.Pink));
            var level = b.Build(); var original = GameState.Initial(level);
            var result = TableRule.Rotate(level, original, 0);
            Assert.That(result.Moved, Is.True);
            Assert.That(result.State.CarAt(0).Axis, Is.EqualTo(expectedAxis));
            Assert.That(result.State.CarAt(0).R, Is.EqualTo(expectedR)); Assert.That(result.State.CarAt(0).C, Is.EqualTo(expectedC));
            Assert.That(result.State.Moves, Is.EqualTo(1)); Assert.That(original.Moves, Is.Zero);
            Assert.That(result.Events.Count, Is.EqualTo(1)); Assert.That(result.Events[0].Kind, Is.EqualTo(MoveEventKind.Rotate));
        }

        [TestCase(false)] [TestCase(true)]
        public void Table_RejectsRotationBlockedByConeOrCar(bool carBlocker)
        {
            var b = new LevelDefBuilder(6, 6); b.Tables.Add(new Cell(2, 2));
            b.Cars.Add(new CarDef(Axis.Horizontal, 2, 2, TileColor.Pink));
            if (carBlocker) b.Cars.Add(new CarDef(Axis.Vertical, 3, 3, TileColor.Blue));
            else b.Cones.Add(new Cell(3, 3));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = TableRule.Rotate(level, initial, 0);
            Assert.That(result.Moved, Is.False); Assert.That(result.State, Is.SameAs(initial));
        }

        [Test]
        public void FourRotations_ReturnToOriginalPoseWithoutApplyingTiles()
        {
            var b = new LevelDefBuilder(6, 6); b.Tables.Add(new Cell(2, 2));
            b.Cars.Add(new CarDef(Axis.Horizontal, 2, 2, TileColor.Pink, mud: 1));
            b.Paints.Add(new PaintDef(3, 3, TileColor.Blue)); b.Muds.Add(new Cell(3, 3)); b.Sprays.Add(new Cell(3, 3));
            var level = b.Build(); var initial = GameState.Initial(level); var state = initial;
            for (int i = 0; i < 4; i++) state = TableRule.Rotate(level, state, 0).State;
            Assert.That(state.CarAt(0), Is.EqualTo(initial.CarAt(0))); Assert.That(state.Moves, Is.EqualTo(4));
            Assert.That(state.MudAt(0), Is.True); Assert.That(initial.Moves, Is.Zero);
        }

        [Test]
        public void Table_RejectsTruckOrCarNotFullyOnTable()
        {
            var b = new LevelDefBuilder(6, 6); b.Tables.Add(new Cell(2, 2));
            b.Cars.Add(new CarDef(Axis.Horizontal, 2, 1, TileColor.Blue, 3, true));
            var level = b.Build(); var initial = GameState.Initial(level);
            Assert.That(TableRule.Rotate(level, initial, 0).Moved, Is.False);
            var other = new LevelDefBuilder(6, 6); other.Tables.Add(new Cell(2, 2));
            other.Cars.Add(new CarDef(Axis.Horizontal, 2, 1, TileColor.Blue));
            var otherLevel = other.Build();
            Assert.That(TableRule.Rotate(otherLevel, GameState.Initial(otherLevel), 0).Moved, Is.False);
        }

        [Test]
        public void EventFreeRotation_ProducesSameState()
        {
            var b = new LevelDefBuilder(6, 6); b.Tables.Add(new Cell(2, 2)); b.Cars.Add(new CarDef(Axis.Horizontal, 2, 2, TileColor.Pink));
            var level = b.Build(); var initial = GameState.Initial(level);
            var a = TableRule.Rotate(level, initial, 0); var z = TableRule.Rotate(level, initial, 0, false);
            SameState(a.State, z.State); Assert.That(z.Events, Is.Empty);
        }

        [Test]
        public void LevelDef_IsUnaffectedByLaterBuilderOrNestedListChanges()
        {
            var b = new LevelDefBuilder(6, 6); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            var gateCells = new List<Cell> { new Cell(1, 1) };
            b.Switches.Add(new SwitchDef(4, 4, gateCells, new[] { new Cell(5, 5) }));
            b.Pylons.Add(new PylonDef(3, 3, -1, new[] { new PortalFace(0, 3, Side.N), new PortalFace(5, 3, Side.S) }));
            b.Portals.Add(new PortalDef(new PortalFace(0, 0, Side.N), new PortalFace(5, 5, Side.S)));
            b.Barriers.Add(new BarrierDef(2, 4, 2));
            var level = b.Build();
            b.Cars.Clear(); b.Switches.Clear(); gateCells.Clear();
            Assert.That(level.Cars.Count, Is.EqualTo(1)); Assert.That(level.Switches[0].A.Count, Is.EqualTo(1));
            Assert.That(level.Portals.Count, Is.EqualTo(1)); Assert.That(level.Barriers[0].Need, Is.EqualTo(2));
            Assert.That(GameState.Initial(level).PylonTurnsAt(0), Is.EqualTo(3));
            Assert.Throws<NotSupportedException>(() => ((IList<CarDef>)level.Cars).Clear());
        }

        [Test]
        public void StateConstructor_CopiesCallerArrays()
        {
            var cars = new[] { new CarState(0, 0, Axis.Horizontal, TileColor.Pink) };
            var muds = new[] { true }; var pyl = new[] { 1 };
            var state = new GameState(cars, muds, pyl);
            cars[0] = cars[0].WithMud(2); muds[0] = false; pyl[0] = 2;
            Assert.That(state.CarAt(0).Mud, Is.Zero); Assert.That(state.MudAt(0), Is.True); Assert.That(state.PylonTurnsAt(0), Is.EqualTo(1));
        }

        [Test]
        public void Builder_RejectsWaterCarWrongTruckLengthAndOverlap()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CarDef(Axis.Horizontal, 0, 0, TileColor.Water));
            Assert.Throws<ArgumentException>(() => new CarDef(Axis.Horizontal, 0, 0, TileColor.Blue, 2, true));
            var b = new LevelDefBuilder(5, 5); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink));
            b.Cars.Add(new CarDef(Axis.Vertical, 0, 1, TileColor.Blue));
            Assert.Throws<ArgumentException>(() => b.Build());
        }

        [Test]
        public void EmptyBoard_IsWon()
        {
            Assert.That(GameRules.IsWin(GameState.Initial(new LevelDefBuilder(3, 3).Build())), Is.True);
        }
    }
}
