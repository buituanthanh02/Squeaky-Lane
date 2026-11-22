using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class Occupancy
    {
        public const int Empty = -1;
        public const int Obstacle = -2;

        public static int[] Build(LevelDef level, GameState state, int skipCar = -1)
        {
            var map = new int[level.Rows * level.Cols];
            for (int i = 0; i < map.Length; i++) map[i] = Empty;
            for (int i = 0; i < level.Cones.Count; i++)
            { var cell = level.Cones[i]; map[cell.R * level.Cols + cell.C] = Obstacle; }
            for (int i = 0; i < level.Pylons.Count; i++)
            { var pylon = level.Pylons[i]; PutObstacle(level, map, pylon.R, pylon.C); }
            int washed = GameRules.WashedCount(state);
            for (int i = 0; i < level.Barriers.Count; i++)
            {
                var barrier = level.Barriers[i];
                if (washed < barrier.Need) PutObstacle(level, map, barrier.R, barrier.C);
            }
            for (int i = 0; i < level.Lights.Count; i++)
            {
                var light = level.Lights[i];
                if (LightRule.IsRed(state, light)) map[light.R * level.Cols + light.C] = Obstacle;
            }
            for (int i = 0; i < level.Switches.Count; i++)
            {
                var button = level.Switches[i];
                // Kể cả skipCar cũng vẫn có thể đè chốt ở đầu lệnh.
                var closed = BarrierRule.SwitchPressed(level, state, button) ? button.A : button.B;
                for (int k = 0; k < closed.Count; k++) PutObstacle(level, map, closed[k].R, closed[k].C);
            }
            // Thứ tự như prototype: xe đang chiếm ô được ghi sau vật cản.
            for (int i = 0; i < state.CarCount; i++)
            {
                var car = state.CarAt(i);
                if (i == skipCar || !car.Alive) continue;
                for (int k = 0; k < level.Cars[i].Len; k++)
                { var cell = car.CellAt(k); map[cell.R * level.Cols + cell.C] = i; }
            }
            return map;
        }

        private static void PutObstacle(LevelDef level, int[] map, int r, int c)
        {
            // Prototype bỏ qua ô vật cản ngoài bãi; không để nó làm sai chỉ số mảng.
            if (level.Contains(r, c)) map[r * level.Cols + c] = Obstacle;
        }
    }
}
