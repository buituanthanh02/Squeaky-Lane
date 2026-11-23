using UnityEngine;
using SqueakyLane.Game;
namespace SqueakyLane.Presentation
{
    public sealed class Haptics : MonoBehaviour
    {
        private FeelConfig feel;
        private ProgressStore progress;
        private float nextPulse;
        public void Configure(FeelConfig settings, ProgressStore store) { feel = settings; progress = store; }
        public void Pulse(bool result = false)
        {
            if (feel == null || progress == null || !progress.HapticsEnabled || Time.unscaledTime < nextPulse) return;
            nextPulse = Time.unscaledTime + feel.HapticGapSeconds;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                long duration = result ? feel.ResultVibrationMs : feel.ShortVibrationMs;
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                if (version.GetStatic<int>("SDK_INT") >= 26)
                {
                    using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", duration, feel.VibrationAmplitude);
                    vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", duration);
            }
            catch (AndroidJavaException)
            {
                // Dự phòng cũng giúp Unity nhận biết tính năng rung và thêm quyền VIBRATE vào manifest sinh khi build.
                Handheld.Vibrate();
            }
#endif
        }
    }
}
