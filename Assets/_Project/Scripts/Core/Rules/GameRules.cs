using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class GameRules
    {
        public static bool IsWin(GameState state)
        {
            for (int i = 0; i < state.CarCount; i++) if (state.CarAt(i).Alive) return false;
            return true;
        }

        public static int WashedCount(GameState state)
        {
            int count = 0;
            for (int i = 0; i < state.CarCount; i++) if (!state.CarAt(i).Alive) count++;
            return count;
        }
    }
}
