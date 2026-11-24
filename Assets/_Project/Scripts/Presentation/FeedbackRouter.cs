using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Gom âm/rung và công tắc cài đặt; không sở hữu trạng thái luật hoặc điều hướng.
    public sealed class FeedbackRouter
    {
        private readonly SfxPlayer sfx;
        private readonly Haptics haptics;
        private readonly ProgressStore progress;
        private InputController input;
        private ActionAnimator animator;
        private NavigationView navigation;
        public FeedbackRouter(SfxPlayer player, Haptics vibration, FeelConfig feel, ProgressStore store)
        {
            sfx = player;
            haptics = vibration;
            progress = store;
            if (sfx != null)
            {
                sfx.Configure(feel, progress);
            }

            if (haptics != null)
            {
                haptics.Configure(feel, progress);
            }
        }

        public void Connect(InputController commands, ActionAnimator animation, NavigationView view)
        {
            Disconnect();
            input = commands;
            animator = animation;
            navigation = view;
            input.CarSelected += SelectedCar;
            animator.EventStarted += VisualEvent;
            animator.CarStarted += CarStarted;
            if (navigation != null)
            {
                navigation.ButtonClicked += UiClicked;
                navigation.SoundChanged += SoundChanged;
                navigation.HapticsChanged += HapticsChanged;
            }
        }

        public void Disconnect()
        {
            if (input != null)
            {
                input.CarSelected -= SelectedCar;
            }

            if (animator != null)
            {
                animator.EventStarted -= VisualEvent;
                animator.CarStarted -= CarStarted;
            }

            if (navigation != null)
            {
                navigation.ButtonClicked -= UiClicked;
                navigation.SoundChanged -= SoundChanged;
                navigation.HapticsChanged -= HapticsChanged;
            }

            input = null;
            animator = null;
            navigation = null;
        }

        public void Play(SoundCue cue)
        {
            if (sfx != null)
            {
                sfx.Play(cue);
            }
        }

        public void StopAll()
        {
            if (sfx != null)
            {
                sfx.StopAll();
            }
        }

        public void Result(bool won)
        {
            Play(won ? SoundCue.Won : SoundCue.Lost);
            Pulse(true);
        }

        private void Pulse(bool result = false)
        {
            if (haptics != null)
            {
                haptics.Pulse(result);
            }
        }

        private void VisualEvent(MoveEventKind kind, StopReason? reason)
        {
            if (sfx != null)
            {
                sfx.PlayEvent(kind, reason);
            }

            if (kind == MoveEventKind.Stop || kind == MoveEventKind.Exit)
            {
                Pulse();
            }
        }

        private void SelectedCar() => Play(SoundCue.SelectCar);
        private void CarStarted() => Play(SoundCue.Move);
        private void UiClicked() => Play(SoundCue.Ui);
        private void SoundChanged(bool enabled)
        {
            progress.SoundEnabled = enabled;
            if (!enabled)
            {
                StopAll();
            }

            navigation.RefreshSettings(progress);
        }

        private void HapticsChanged(bool enabled)
        {
            progress.HapticsEnabled = enabled;
            navigation.RefreshSettings(progress);
            if (enabled)
            {
                Pulse();
            }
        }
    }
}
