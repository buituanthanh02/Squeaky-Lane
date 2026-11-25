using System;

namespace SqueakyLane.Game
{
    // Nghiệm lò xo tắt dần theo thời gian tuyệt đối: không tích phân Euler nên không nổ khi frame chậm.
    public static class BodySpring
    {
        public static float Sample(float elapsed, float duration, float reboundRatio)
        {
            if (float.IsNaN(elapsed) || float.IsInfinity(elapsed) ||
                float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0 || elapsed >= duration)
            {
                return 0;
            }
            double t = Math.Max(0, elapsed) / duration;
            double ratio = float.IsNaN(reboundRatio) || float.IsInfinity(reboundRatio)
                ? .375 : Math.Max(.01, Math.Min(.8, reboundRatio));
            double decay = -2 * Math.Log(ratio);
            double frequency = Math.PI * 2;
            double value = Math.Exp(-decay * t) *
                (Math.Cos(frequency * t) + decay / frequency * Math.Sin(frequency * t));
            // Cuối nhịp ép về đúng 0, tránh giữ độ nghiêng rất nhỏ mãi mãi.
            if (t > .5)
            {
                value *= 1 - Math.Pow((t - .5) * 2, 2);
            }
            return (float)Math.Max(-1, Math.Min(1, value));
        }
    }
}
