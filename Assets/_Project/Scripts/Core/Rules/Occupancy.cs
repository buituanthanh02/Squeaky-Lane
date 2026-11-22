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
            for (int i = 0; i < level.Lights.Count; i++)
            {
                var light = level.Lights[i];
                if (LightRule.IsRed(state, light)) map[light.R * level.Cols + light.C] = Obstacle;
            }
            // Thứ tự như prototype: xe đang chiếm ô được ghi sau vật cản.
            for (int i = 0; i < state.CarCount; i++)
            {
                var car = state.CarAt(i);
                if (i == skipCar || !car.Alive) continue;
                for (int k = 0; k < level.Cars[i].Len; k++)
                { var cell = car.CellAt(k); map[cell.R * level.Cols + cell.C] = i; }
            }
            // Portal/trụ/barie/chốt được nối ở 004, không giả vờ đã xử lý tại đây.
            return map;
        }
    }
}
