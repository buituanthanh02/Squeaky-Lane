using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using static SqueakyLane.Tests.EditMode.RuleTestSupport;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class TileAndSplashTests
    {
        [TestCase(1)] [TestCase(-1)]
        public void MudPuddle_IsConsumedByFirstCarOnly(int sign)
        {
            var b = new LevelDefBuilder(9, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink), 9, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 7, TileColor.Blue), 9, sign));
            b.Muds.Add(new Cell(3, MirrorCell(4, 9, sign)));
            var level = b.Build(); var initial = GameState.Initial(level);
            var first = MoveResolver.Move(level, initial, 0, sign);
            var backed = MoveResolver.Move(level, first.State, 0, -sign);
            var second = MoveResolver.Move(level, backed.State, 1, -sign);
            Assert.That(first.State.CarAt(0).Mud, Is.EqualTo(1));
            Assert.That(first.State.MudAt(0), Is.False);
            Assert.That(second.State.CarAt(1).Mud, Is.Zero);
            Assert.That(Count(second, MoveEventKind.Mud), Is.Zero);
            Assert.That(initial.MudAt(0), Is.True);
            Assert.That(initial.CarAt(0).Mud, Is.Zero); Assert.That(initial.Moves, Is.Zero);
        }

        [TestCase(1)] [TestCase(-1)]
        public void Spray_CleansCarWithoutStopping(int sign)
        {
            var b = new LevelDefBuilder(8, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink, mud: 1), 8, sign));
            b.Sprays.Add(new Cell(3, MirrorCell(3, 8, sign)));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.State.CarAt(0).Mud, Is.Zero);
            Assert.That(result.State.CarAt(0).C, Is.EqualTo(sign > 0 ? 6 : 0));
            Assert.That(Count(result, MoveEventKind.Spray), Is.EqualTo(1));
        }

        [TestCase(1)] [TestCase(-1)]
        public void Paint_ChangesColorAndExitEligibility(int sign)
        {
            var b = new LevelDefBuilder(8, 5);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink), 8, sign));
            b.Paints.Add(new PaintDef(3, MirrorCell(3, 8, sign), TileColor.Blue));
            b.Gates.Add(new GateDef(Edge(Axis.Horizontal, sign), 3, TileColor.Blue));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            Assert.That(result.Exited, Is.True); Assert.That(result.State.CarAt(0).Color, Is.EqualTo(TileColor.Blue));
            Assert.That(Find(result, MoveEventKind.Paint).Color, Is.EqualTo(TileColor.Blue));
        }

        [TestCase(1)] [TestCase(-1)]
        public void CoLocatedEffects_ArePaintThenMudThenSpray(int sign)
        {
            var b = new LevelDefBuilder(8, 5); int c = MirrorCell(3, 8, sign);
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink), 8, sign));
            b.Paints.Add(new PaintDef(3, c, TileColor.Blue)); b.Muds.Add(new Cell(3, c)); b.Sprays.Add(new Cell(3, c));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, sign);
            int paint = -1, mud = -1, spray = -1;
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i].Kind == MoveEventKind.Paint) paint = i;
                if (result.Events[i].Kind == MoveEventKind.Mud) mud = i;
                if (result.Events[i].Kind == MoveEventKind.Spray) spray = i;
            }
            Assert.That(paint, Is.GreaterThan(0)); Assert.That(mud, Is.EqualTo(paint + 1)); Assert.That(spray, Is.EqualTo(mud + 1));
            Assert.That(result.State.CarAt(0).Color, Is.EqualTo(TileColor.Blue));
            Assert.That(result.State.CarAt(0).Mud, Is.Zero); Assert.That(result.State.MudAt(0), Is.False);
        }

        [TestCase(1, false)] [TestCase(-1, false)]
        [TestCase(1, true)] [TestCase(-1, true)]
        public void Splash_RequiresRunUpForOrdinaryCollision(int sign, bool adjacent)
        {
            var b = new LevelDefBuilder(9, 5) { Splash = true };
            b.Cars.Add(Mirror(new CarDef(Axis.Horizontal, 3, adjacent ? 4 : 0, TileColor.Pink, mud: 1), 9, sign));
            b.Cars.Add(Mirror(new CarDef(Axis.Vertical, 2, 6, TileColor.Blue), 9, sign));
            var level = b.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, 0, sign);
            Assert.That(result.State.CarAt(1).Mud, Is.EqualTo(adjacent ? 0 : 1));
            Assert.That(Count(result, MoveEventKind.Splash), Is.EqualTo(adjacent ? 0 : 1));
            Assert.That(result.Moved, Is.EqualTo(!adjacent));
            Assert.That(Find(result, MoveEventKind.Stop).Reason, Is.EqualTo(StopReason.Car));
            Assert.That(initial.CarAt(1).Mud, Is.Zero);
            if (adjacent) Assert.That(result.State, Is.SameAs(initial));
        }

        [Test]
        public void DisabledSplash_DoesNotDirtyTarget()
        {
            var b = new LevelDefBuilder(9, 5);
            b.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink, mud: 1));
            b.Cars.Add(new CarDef(Axis.Vertical, 2, 6, TileColor.Blue));
            var level = b.Build(); var result = MoveResolver.Move(level, GameState.Initial(level), 0, 1);
            Assert.That(result.State.CarAt(1).Mud, Is.Zero); Assert.That(Count(result, MoveEventKind.Splash), Is.Zero);
        }
    }
}
