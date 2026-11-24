using System;

namespace SqueakyLane.Game
{
    public sealed class LevelTimer
    {
        private readonly float undoPenalty, hintPenalty, star3, star2;
        private readonly float maxTickSeconds;
        public float TimeLimit { get; }
        public float Remaining { get; private set; }
        public bool Paused { get; set; }
        public bool IsTimed => TimeLimit > 0;
        public bool IsOver => IsTimed && Remaining <= 0;

        public LevelTimer(float timeLimit, float undoPenalty, float hintPenalty, float star3, float star2, float maxTickSeconds = .25f)
        {
            if (!Finite(timeLimit) || !Finite(undoPenalty) || !Finite(hintPenalty) || !Finite(star3) || !Finite(star2) || undoPenalty < 0 || hintPenalty < 0 || star2 < 0 || star3 < star2 || star3 > 1 || !Finite(maxTickSeconds) || maxTickSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            TimeLimit = timeLimit;
            Remaining = Math.Max(0, timeLimit);
            this.undoPenalty = undoPenalty;
            this.hintPenalty = hintPenalty;
            this.star3 = star3;
            this.star2 = star2;
            this.maxTickSeconds = maxTickSeconds;
        }

        public void Tick(float dt)
        {
            if (!Finite(dt) || dt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dt));
            }

            if (!Paused && IsTimed)
            {
                Remaining = Math.Max(0, Remaining - Math.Min(dt, maxTickSeconds));
            }
        }

        // Phạt là hành động tường minh, không phụ thuộc trạng thái pause của đồng hồ.
        public void ChargeUndo()
        {
            if (IsTimed)
            {
                Remaining = Math.Max(0, Remaining - undoPenalty);
            }
        }

        public void ChargeHint()
        {
            if (IsTimed)
            {
                Remaining = Math.Max(0, Remaining - hintPenalty);
            }
        }

        public int StarsOnWin()
        {
            if (!IsTimed)
            {
                return 3;
            }

            // 0,2f không biểu diễn chính xác trong nhị phân; Mono có thể giữ phép chia ở độ chính xác khác.
            // Sai số một phần mười triệu tỷ lệ chỉ bù làm tròn, không nới ngưỡng thiết kế.
            const double roundingTolerance = 1e-7;
            double ratio = (double)Remaining / TimeLimit;
            return ratio + roundingTolerance >= star3 ? 3 : ratio + roundingTolerance >= star2 ? 2 : 1;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
