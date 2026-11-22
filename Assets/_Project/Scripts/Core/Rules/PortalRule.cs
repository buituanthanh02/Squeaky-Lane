using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    internal enum PortalMoveResult { None, Blocked, Teleported }

    public static class PortalRule
    {
        internal static PortalMoveResult TryMove(LevelDef level, GameState state, int carIndex,
            int[] occupancy, bool teleported, ref int sign, List<MoveEvent> events)
        {
            var car = state.CarAt(carIndex);
            int len = level.Cars[carIndex].Len;
            int hr = car.R + (car.Axis == Axis.Vertical && sign > 0 ? len - 1 : 0);
            int hc = car.C + (car.Axis == Axis.Horizontal && sign > 0 ? len - 1 : 0);
            var side = car.Axis == Axis.Horizontal ? (sign > 0 ? Side.E : Side.W) : (sign > 0 ? Side.S : Side.N);
            var origin = new PortalFace(hr, hc, side);
            if (!TryDestination(level, state, origin, out var destination)) return PortalMoveResult.None;
            if (teleported) return PortalMoveResult.Blocked;

            // Ô cổng đích là đuôi xe; thân trải vào trong, ngược hướng mặt tường đích.
            int dr = destination.Side == Side.N ? 1 : destination.Side == Side.S ? -1 : 0;
            int dc = destination.Side == Side.W ? 1 : destination.Side == Side.E ? -1 : 0;
            for (int k = 0; k < len; k++)
            {
                int r = destination.R + dr * k, c = destination.C + dc * k;
                if (!level.Contains(r, c) || occupancy[r * level.Cols + c] != Occupancy.Empty)
                    return PortalMoveResult.Blocked;
            }

            var axis = dr == 0 ? Axis.Horizontal : Axis.Vertical;
            int r0 = destination.R + (dr < 0 ? dr * (len - 1) : 0);
            int c0 = destination.C + (dc < 0 ? dc * (len - 1) : 0);
            state.SetCar(carIndex, new CarState(r0, c0, axis, car.Color, car.Mud, car.Alive));
            sign = dr + dc > 0 ? 1 : -1;
            events?.Add(new MoveEvent(MoveEventKind.Tele, r: r0, c: c0, sign: sign,
                fromFace: origin, toFace: destination, axis: axis));
            // Thứ tự đuôi → đầu giữ nguyên prototype, kể cả khi chiều mới là âm.
            for (int k = 0; k < len; k++)
                TileEffects.Apply(level, state, carIndex, destination.R + dr * k, destination.C + dc * k, events);
            return PortalMoveResult.Teleported;
        }

        private static bool TryDestination(LevelDef level, GameState state, PortalFace origin, out PortalFace destination)
        {
            // Duyệt theo thứ tự faceList của prototype, không cấp phát list/khóa chuỗi mỗi bước.
            for (int i = 0; i < level.Portals.Count; i++)
            {
                var portal = level.Portals[i];
                if (TryLink(origin, portal.A, portal.B, out destination)) return true;
            }
            for (int i = 0; i < level.Pylons.Count; i++)
            {
                var pylon = level.Pylons[i];
                PylonRule.PylonFaces(pylon, state.PylonTurnsAt(i), out var first, out var second);
                if (TryLink(origin, first, pylon.To[0], out destination) ||
                    TryLink(origin, second, pylon.To[1], out destination)) return true;
            }
            destination = default;
            return false;
        }

        private static bool TryLink(PortalFace origin, PortalFace a, PortalFace b, out PortalFace destination)
        {
            if (SameFace(origin, a)) { destination = b; return true; }
            if (SameFace(origin, b)) { destination = a; return true; }
            destination = default;
            return false;
        }

        private static bool SameFace(PortalFace a, PortalFace b) => a.R == b.R && a.C == b.C && a.Side == b.Side;
    }
}
