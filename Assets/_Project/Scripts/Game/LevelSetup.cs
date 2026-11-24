using System;

namespace SqueakyLane.Game
{
    // Kết quả thiết lập phiên chơi; chỉ nhận dữ liệu thường để kiểm thử không cần asset.
    public readonly struct LevelSetup
    {
        public float TimeLimit { get; }
        public int Hints { get; }
        public bool IsTutorial { get; }

        public LevelSetup(LevelInfo info, int position, int tutorialLevelsUntimed, float baseSeconds, float secondsPerMove, int unknownMinimumMoves, int hintsPerLevel)
        {
            if (position < 0 || tutorialLevelsUntimed < 0 || baseSeconds < 0 || secondsPerMove < 0 || unknownMinimumMoves < 0 || hintsPerLevel < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            IsTutorial = position < tutorialLevelsUntimed;
            int minimum = info.MinMoves >= 0 ? info.MinMoves : unknownMinimumMoves;
            TimeLimit = IsTutorial ? 0 : info.TimeLimit ?? baseSeconds + secondsPerMove * minimum;
            Hints = IsTutorial ? -1 : info.Hints ?? hintsPerLevel;
        }
    }
}
