using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class TableRule
    {
        public static Cell? TableUnder(LevelDef level, CarState car, int len)
        {
            if (len != 2 || !car.Alive) return null;
            for (int i = 0; i < level.Tables.Count; i++)
            {
                var table = level.Tables[i];
                var end = car.CellAt(1);
                if (Inside(table, car.R, car.C) && Inside(table, end.R, end.C)) return table;
            }
            return null;
        }

        public static MoveResult Rotate(LevelDef level, GameState state, int car, bool recordEvents = true)
        {
            var old = state.CarAt(car);
            var table = TableUnder(level, old, level.Cars[car].Len);
            if (!table.HasValue) return MoveResult.Unchanged(state);
            var t = table.Value;
            var end = old.CellAt(1);
            // Toạ độ trong bàn: (dr,dc) → (dc,1-dr), tức chiều kim đồng hồ.
            int r0 = t.R + old.C - t.C, c0 = t.C + 1 - (old.R - t.R);
            int r1 = t.R + end.C - t.C, c1 = t.C + 1 - (end.R - t.R);
            var map = Occupancy.Build(level, state, car);
            if (map[r0 * level.Cols + c0] != Occupancy.Empty || map[r1 * level.Cols + c1] != Occupancy.Empty)
                return MoveResult.Unchanged(state);
            var next = state.Copy();
            next.SetCar(car, new CarState(Math.Min(r0, r1), Math.Min(c0, c1),
                old.Axis == Axis.Horizontal ? Axis.Vertical : Axis.Horizontal, old.Color, old.Mud, old.Alive));
            next.AdvanceMove();
            var events = recordEvents ? new List<MoveEvent> { new MoveEvent(MoveEventKind.Rotate) } : null;
            // Prototype không áp hiệu ứng ô khi xoay; không tự thêm luật ở đây.
            return new MoveResult(next, events, true);
        }

        private static bool Inside(Cell table, int r, int c) => r >= table.R && r <= table.R + 1 && c >= table.C && c <= table.C + 1;
    }
}
