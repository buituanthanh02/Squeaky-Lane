using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class TileEffects
    {
        internal static void Apply(LevelDef level, GameState state, int carIndex, int r, int c, List<MoveEvent> events)
        {
            var car = state.CarAt(carIndex);
            // Thứ tự có ý nghĩa luật khi một ô chứa nhiều hiệu ứng.
            var paint = level.PaintAt(r, c);
            if (paint.HasValue && paint.Value != car.Color)
            {
                car = car.WithColor(paint.Value);
                events?.Add(new MoveEvent(MoveEventKind.Paint, color: paint.Value));
            }
            int mud = level.MudAt(r, c);
            if (mud >= 0 && state.MudAt(mud))
            {
                state.ConsumeMud(mud);
                car = car.WithMud(car.Mud < 1 ? 1 : car.Mud);
                events?.Add(new MoveEvent(MoveEventKind.Mud, mudIndex: mud));
            }
            if (level.SprayAt(r, c) && car.Mud > 0)
            {
                car = car.WithMud(0);
                events?.Add(new MoveEvent(MoveEventKind.Spray));
            }
            state.SetCar(carIndex, car);
        }
    }
}
