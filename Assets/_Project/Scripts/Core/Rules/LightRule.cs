using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class LightRule
    {
        public static bool IsRed(GameState state, LightDef light) => ((state.Moves + light.Phase) & 1) == 1;
    }
}
