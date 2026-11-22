using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class PushRule
    {
        internal static bool TryChain(LevelDef level, GameState state, int truck, int sign, int first,
            out int[] chain, out int count)
        {
            var moving = state.CarAt(truck);
            int dr = moving.Axis == Axis.Vertical ? sign : 0;
            int dc = moving.Axis == Axis.Horizontal ? sign : 0;
            var map = Occupancy.Build(level, state);
            chain = new int[state.CarCount];
            var included = new bool[state.CarCount];
            chain[0] = first; included[first] = true; count = 1;
            // Duyệt cả thân xe để đẩy cả nhánh vuông góc, không chỉ ô trước đầu.
            for (int n = 0; n < count; n++)
            {
                int index = chain[n];
                var car = state.CarAt(index);
                for (int k = 0; k < level.Cars[index].Len; k++)
                {
                    var cell = car.CellAt(k);
                    int r = cell.R + dr, c = cell.C + dc;
                    if (!level.Contains(r, c)) return false;
                    int at = map[r * level.Cols + c];
                    if (at == Occupancy.Obstacle || at == truck) return false;
                    if (at >= 0 && !included[at])
                    { included[at] = true; chain[count++] = at; }
                }
            }
            return true;
        }

        internal static void Apply(LevelDef level, GameState state, int truck, int sign,
            int[] chain, int count, List<MoveEvent> events)
        {
            var moving = state.CarAt(truck);
            int dr = moving.Axis == Axis.Vertical ? sign : 0;
            int dc = moving.Axis == Axis.Horizontal ? sign : 0;
            var pushed = events == null ? null : new List<PushedCarEvent>(count);
            for (int n = 0; n < count; n++)
            {
                int index = chain[n], len = level.Cars[index].Len;
                var old = state.CarAt(index);
                var next = old.At(old.R + dr, old.C + dc);
                state.SetCar(index, next);
                var fx = events == null ? null : new List<MoveEvent>();
                // Thứ tự xe/ô giữ nguyên prototype; chỉ ô mới phủ mới gây hiệu ứng.
                for (int k = 0; k < len; k++)
                {
                    var cell = next.CellAt(k);
                    if (Contains(old, len, cell.R, cell.C)) continue;
                    TileEffects.Apply(level, state, index, cell.R, cell.C, fx);
                }
                if (pushed != null) pushed.Add(new PushedCarEvent(index, next.R, next.C, fx));
            }
            if (events != null) events.Add(new MoveEvent(MoveEventKind.Push, sign: sign, pushedCars: pushed));
        }

        private static bool Contains(CarState car, int len, int r, int c) => car.Axis == Axis.Horizontal
            ? r == car.R && c >= car.C && c < car.C + len
            : c == car.C && r >= car.R && r < car.R + len;
    }
}
