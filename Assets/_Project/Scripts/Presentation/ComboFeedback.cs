using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Combo = số xe ra cổng liên tiếp (animator đếm). Hiện "Combo N" lúc đang chơi, giữ combo cao nhất của lượt,
    // cuối lượt ghi kỷ lục theo màn và giao cho bảng kết quả. Không đụng luật.
    public sealed class ComboFeedback : MonoBehaviour
    {
        [SerializeField] private UiTheme theme = null;
        [SerializeField, Tooltip("Chỗ hiện 'Combo N' lúc đang chơi (dưới ô số nước).")] private RectTransform popupAnchor = null;
        [SerializeField] private ComboSummary wonSummary = null, lostSummary = null;
        private ActionAnimator animator;
        private FeelConfig feel;
        private FeedbackRouter feedback;
        private FeelEffects effects;
        private ComboLabel popup;
        private CanvasGroup popupGroup;
        private float age, hold;
        private int resultCombo, resultRecord;
        private bool resultIsNew;

        public int Best { get; private set; }
        public int Showing => popupGroup != null && popupGroup.alpha > 0 ? popup.Value : 0;

        public void Initialize(ActionAnimator animation, FeelConfig config, FeedbackRouter router, FeelEffects world = null)
        {
            Disconnect();
            animator = animation;
            feel = config;
            feedback = router;
            effects = world;
            if (animator != null)
            {
                animator.CarEventStarted += OnCarEvent;
                animator.Cancelled += HidePopup;
            }

            foreach (var summary in new[] { wonSummary, lostSummary })
            {
                if (summary == null) { continue; }
                summary.Configure(theme, config);
                summary.Whoosh += PlayWhoosh;
                summary.Slam += PlaySlam;
            }
        }

        public void Disconnect()
        {
            if (animator != null)
            {
                animator.CarEventStarted -= OnCarEvent;
                animator.Cancelled -= HidePopup;
            }

            foreach (var summary in new[] { wonSummary, lostSummary })
            {
                if (summary == null) { continue; }
                summary.Whoosh -= PlayWhoosh;
                summary.Slam -= PlaySlam;
            }

            animator = null;
        }

        // Lượt mới (vào màn, chơi lại). Hoàn tác không gọi: combo đã đạt trong lượt vẫn tính.
        public void BeginAttempt()
        {
            Best = 0;
            HidePopup();
            if (wonSummary != null) { wonSummary.Hide(); }
            if (lostSummary != null) { lostSummary.Hide(); }
        }

        // Gọi một lần khi lượt kết thúc (thắng hoặc hết giờ).
        public void FinishAttempt(ProgressStore progress, int levelId)
        {
            resultCombo = Best;
            resultRecord = progress.BestCombo(levelId);
            resultIsNew = progress.RecordCombo(levelId, Best);
        }

        // Gọi khi màn hình đã hiện; chỉ hộp Thắng / Hết giờ có khối combo.
        public void ShowScreen(ScreenState screen)
        {
            if (screen == ScreenState.Won && wonSummary != null) { wonSummary.Show(resultCombo, resultRecord, resultIsNew); }
            if (screen == ScreenState.Lost && lostSummary != null) { lostSummary.Show(resultCombo, resultRecord, resultIsNew); }
            if (screen != ScreenState.Playing && screen != ScreenState.Paused) { HidePopup(); }
        }

        public void RegisterExit(int chain)
        {
            Best = Mathf.Max(Best, chain);
            if (chain < ProgressStore.MinCombo || theme == null || popupAnchor == null || feel == null) { return; }
            if (popup == null)
            {
                popup = new ComboLabel(popupAnchor, theme);
                popupGroup = popup.Root.gameObject.AddComponent<CanvasGroup>();
                popupGroup.interactable = popupGroup.blocksRaycasts = false;
            }

            popup.Set(chain, theme.PopupDigitHeight);
            popup.Root.localScale = Vector3.zero;
            popupGroup.alpha = 1;
            age = 0;
            // Đứng yên đúng bằng thời gian còn nối được chuỗi, rồi mờ dần.
            hold = feel.ComboWindowSeconds;
        }

        private void OnCarEvent(CarView car, MoveEvent ev)
        {
            if (ev.Kind == MoveEventKind.Exit) { RegisterExit(animator.ExitCombo); }
        }

        private void Update()
        {
            if (animator == null || !animator.Paused) { Advance(Time.unscaledDeltaTime); }
        }

        public void Advance(float seconds)
        {
            if (popupGroup == null || popupGroup.alpha <= 0) { return; }
            age += seconds;
            float pop = Mathf.Clamp01(age / feel.ComboPopSeconds);
            popup.Root.localScale = Vector3.one * ComboSummary.Overshoot(pop, feel.ComboPunchScale);
            popup.Burst.localRotation = Quaternion.Euler(0, 0, -age * feel.ComboBurstDegrees);
            popupGroup.alpha = 1 - Mathf.Clamp01((age - hold) / feel.ComboFadeSeconds);
        }

        private void HidePopup()
        {
            if (popupGroup != null) { popupGroup.alpha = 0; }
        }

        private void PlayWhoosh() { feedback?.Play(SoundCue.RecordWhoosh); }
        // Đóng huy hiệu: tiếng + rung máy, và rung camera bãi phía sau hộp thoại cùng nhịp với giao diện.
        private void PlaySlam()
        {
            feedback?.Impact(SoundCue.RecordSlam);
            if (effects != null) { effects.Shake(feel.RecordCameraShake); }
        }
        private void OnDestroy() { Disconnect(); }
    }
}
