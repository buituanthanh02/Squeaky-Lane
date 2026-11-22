using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class GateRule
    {
        public static GateEdge EdgeOf(Axis axis, int sign) => axis == Axis.Horizontal
            ? (sign > 0 ? GateEdge.Right : GateEdge.Left)
            : (sign > 0 ? GateEdge.Bottom : GateEdge.Top);

        public static bool CanBounce(GateDef? gate, bool bounced) =>
            gate.HasValue && gate.Value.Color == TileColor.Water && !bounced;

        public static bool CanExit(GateDef? gate, CarState car) =>
            gate.HasValue && gate.Value.Color != TileColor.Water &&
            gate.Value.Color == car.Color && car.Mud == 0;

        public static StopReason StopAt(GateDef? gate, CarState car)
        {
            if (!gate.HasValue || gate.Value.Color == TileColor.Water) return StopReason.Edge;
            return car.Mud > 0 ? StopReason.Muddy : StopReason.WrongGate;
        }
    }
}
