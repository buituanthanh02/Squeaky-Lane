using NUnit.Framework;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Tests.EditMode
{
    internal static class RuleTestSupport
    {
        internal static CarDef LaneCar(Axis axis, int position, int mud = 0) => axis == Axis.Horizontal
            ? new CarDef(axis, 3, position, TileColor.Pink, mud: mud)
            : new CarDef(axis, position, 3, TileColor.Pink, mud: mud);

        internal static GateEdge Edge(Axis axis, int sign) => axis == Axis.Horizontal
            ? (sign > 0 ? GateEdge.Right : GateEdge.Left)
            : (sign > 0 ? GateEdge.Bottom : GateEdge.Top);

        internal static int Count(MoveResult result, MoveEventKind kind)
        {
            int count = 0;
            for (int i = 0; i < result.Events.Count; i++) if (result.Events[i].Kind == kind) count++;
            return count;
        }

        internal static MoveEvent Find(MoveResult result, MoveEventKind kind)
        {
            for (int i = 0; i < result.Events.Count; i++) if (result.Events[i].Kind == kind) return result.Events[i];
            Assert.Fail("Thiếu sự kiện " + kind);
            return default;
        }

        internal static void SameState(GameState expected, GameState actual)
        {
            Assert.That(actual.Moves, Is.EqualTo(expected.Moves));
            Assert.That(actual.CarCount, Is.EqualTo(expected.CarCount));
            for (int i = 0; i < expected.CarCount; i++) Assert.That(actual.CarAt(i), Is.EqualTo(expected.CarAt(i)), "xe " + i);
            Assert.That(actual.MudCount, Is.EqualTo(expected.MudCount));
            for (int i = 0; i < expected.MudCount; i++) Assert.That(actual.MudAt(i), Is.EqualTo(expected.MudAt(i)));
            Assert.That(actual.PylonCount, Is.EqualTo(expected.PylonCount));
            for (int i = 0; i < expected.PylonCount; i++) Assert.That(actual.PylonTurnsAt(i), Is.EqualTo(expected.PylonTurnsAt(i)));
        }

        internal static CarDef Mirror(CarDef car, int width, int sign) => sign > 0 ? car :
            new CarDef(car.Axis, car.R, width - (car.Axis == Axis.Horizontal ? car.Len : 1) - car.C,
                car.Color, car.Len, car.Truck, car.Mud);

        internal static int MirrorCell(int c, int width, int sign) => sign > 0 ? c : width - 1 - c;
    }
}
