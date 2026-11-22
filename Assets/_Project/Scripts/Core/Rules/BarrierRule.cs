using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class BarrierRule
    {
        public static bool SwitchPressed(LevelDef level, GameState state, SwitchDef button)
        {
            for (int i = 0; i < state.CarCount; i++)
            {
                var car = state.CarAt(i);
                if (!car.Alive) continue;
                for (int k = 0; k < level.Cars[i].Len; k++)
                {
                    var cell = car.CellAt(k);
                    if (cell.R == button.R && cell.C == button.C) return true;
                }
            }
            return false;
        }

        public static IReadOnlyList<BarrierDef> BarriersOpened(LevelDef level, int before, int after)
        {
            List<BarrierDef> opened = null;
            for (int i = 0; i < level.Barriers.Count; i++)
            {
                var barrier = level.Barriers[i];
                if (before < barrier.Need && after >= barrier.Need)
                {
                    if (opened == null) opened = new List<BarrierDef>();
                    opened.Add(barrier);
                }
            }
            // Chỉ tạo danh sách khi có barie vừa mở; không lộ List có thể sửa ra ngoài.
            return opened == null ? (IReadOnlyList<BarrierDef>)Array.Empty<BarrierDef>() : opened.AsReadOnly();
        }
    }
}
