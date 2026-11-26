using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SqueakyLane.Presentation
{
    // Bàn tay chỉ dẫn trên màn hình: chạm tại From, hoặc kéo từ From tới To (tọa độ pixel).
    public readonly struct TutorialPointer
    {
        public enum Mode { None, Tap, Drag }

        public Mode Kind { get; }
        public Vector2 From { get; }
        public Vector2 To { get; }

        private TutorialPointer(Mode kind, Vector2 from, Vector2 to)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public static TutorialPointer Tap(Vector2 at) => new TutorialPointer(Mode.Tap, at, at);
        public static TutorialPointer Drag(Vector2 from, Vector2 to) => new TutorialPointer(Mode.Drag, from, to);
    }

    // 019: chỉ hiển thị lớp tối, viền sáng, bàn tay và dòng chữ. Không biết bài tập hay luật chơi.
    public sealed class TutorialView : MonoBehaviour
    {
        [SerializeField] private UiTheme theme = null;
        [SerializeField] private CanvasGroup group = null;
        [SerializeField] private SpotlightGraphic shade = null, ring = null;
        [SerializeField] private Image hand = null;
        [SerializeField] private CanvasGroup bubble = null;
        [SerializeField] private TMP_Text text = null;
        [SerializeField] private GameObject continueHint = null;
        [SerializeField] private Button skipButton = null;

        private readonly List<Rect> local = new List<Rect>(8);
        private float age, handAge, nudgeAge = 1;
        private bool visible, waitingTap, placeBubble;
        private TutorialPointer pointer;
        private Vector2 bubbleHome;

        public event Action Continue, Skip;
        public float HintSeconds => theme.TutorialHintSeconds;

        private void Awake()
        {
            shade.Clicked += () =>
            {
                // Chặn cú chạm dư của thao tác trước làm trôi mất bước giải thích.
                if (visible && waitingTap && age >= theme.TutorialTapDelay)
                {
                    Continue?.Invoke();
                }
            };
            skipButton.onClick.AddListener(() => Skip?.Invoke());
            Hide();
        }

        // Bước mới của bài tập: dòng chữ, nút Bỏ qua; waitTap = cả màn chờ một cú chạm.
        public void ShowStep(string message, bool waitTap, RectTransform passThrough)
        {
            Open();
            waitingTap = waitTap;
            text.text = message;
            bubble.alpha = 1;
            bubble.interactable = true;
            bubble.blocksRaycasts = true;
            continueHint.SetActive(waitTap);
            shade.raycastTarget = waitTap;
            shade.SetPassThrough(passThrough);
            placeBubble = true;
        }

        // Chỉ bàn tay và viền sáng (gợi ý trong màn chơi), không chữ và không chặn chạm.
        public void ShowPointerOnly()
        {
            Open();
            waitingTap = false;
            bubble.alpha = 0;
            bubble.interactable = false;
            bubble.blocksRaycasts = false;
            shade.raycastTarget = false;
            placeBubble = false;
        }

        // Gọi mỗi frame khi đang hiện: vùng sáng (pixel màn hình), có phủ tối không, chừa mép cho viền đỏ.
        public void Track(List<Rect> screenHoles, bool dim, float edgeInset, TutorialPointer guide)
        {
            if (!visible)
            {
                return;
            }

            var root = (RectTransform)transform;
            local.Clear();
            for (int i = 0; i < screenHoles.Count; i++)
            {
                var min = Local(root, screenHoles[i].min);
                var max = Local(root, screenHoles[i].max);
                float pad = theme.TutorialHolePadding;
                local.Add(Rect.MinMaxRect(min.x - pad, min.y - pad, max.x + pad, max.y + pad));
            }

            shade.enabled = dim;
            if (dim)
            {
                shade.Show(local, theme.TutorialCornerRadius, 0, edgeInset);
            }

            ring.Show(local, theme.TutorialCornerRadius, theme.TutorialRingWidth, 0);
            pointer = guide;
            if (placeBubble)
            {
                placeBubble = false;
                PlaceBubble(root);
            }

            Animate();
        }

        // Người chơi làm sai thao tác: diễn lại bàn tay và lắc dòng chữ.
        public void Nudge()
        {
            handAge = 0;
            nudgeAge = 0;
        }

        public void Hide()
        {
            visible = false;
            // Lúc đóng cảnh các object con có thể đã bị hủy trước.
            if (group != null)
            {
                group.alpha = 0;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            if (shade != null) { shade.raycastTarget = false; }
            if (hand != null) { hand.enabled = false; }
        }

        private void Open()
        {
            visible = true;
            age = 0;
            handAge = 0;
            nudgeAge = 1;
            group.alpha = 0;
            group.interactable = true;
            group.blocksRaycasts = true;
            hand.enabled = false;
        }

        private void Animate()
        {
            float delta = Time.unscaledDeltaTime;
            age += delta;
            handAge += delta;
            nudgeAge += delta;
            group.alpha = Mathf.Clamp01(age / theme.TutorialFadeSeconds);
            // Viền thở nhẹ để mắt tìm ra vùng cần nhìn; chỉ đổi alpha của renderer, không dựng lại mesh.
            float pulse = .5f + .5f * Mathf.Cos(age / theme.TutorialRingPulseSeconds * 2 * Mathf.PI);
            ring.canvasRenderer.SetAlpha(Mathf.Lerp(theme.TutorialRingAlphaMin, 1, pulse));
            float shake = nudgeAge < theme.TutorialNudgeSeconds
                ? Mathf.Sin(nudgeAge / theme.TutorialNudgeSeconds * Mathf.PI * 6) * theme.TutorialNudgeDistance * (1 - nudgeAge / theme.TutorialNudgeSeconds) : 0;
            ((RectTransform)bubble.transform).anchoredPosition = bubbleHome + Vector2.right * shake;
            AnimateHand();
        }

        private void AnimateHand()
        {
            var rect = hand.rectTransform;
            var root = (RectTransform)transform;
            if (pointer.Kind == TutorialPointer.Mode.None)
            {
                hand.enabled = false;
                return;
            }

            float alpha = 1;
            if (pointer.Kind == TutorialPointer.Mode.Tap)
            {
                var frames = theme.TutorialTapFrames;
                if (frames == null || frames.Length == 0)
                {
                    hand.enabled = false;
                    return;
                }

                // Một nhịp: tới gần, ấn xuống (chạy xuôi khung hình), giữ, nhả (chạy ngược), nghỉ.
                float t = Mathf.Repeat(handAge, theme.TutorialTapPeriod) / theme.TutorialTapPeriod;
                float press = t < .3f ? 0 : t < .5f ? (t - .3f) / .2f : t < .62f ? 1 : t < .82f ? 1 - (t - .62f) / .2f : 0;
                hand.sprite = frames[Mathf.Clamp(Mathf.RoundToInt(press * (frames.Length - 1)), 0, frames.Length - 1)];
                rect.pivot = theme.TutorialTapPivot;
                rect.sizeDelta = theme.TutorialTapSize;
                float approach = t < .3f ? 1 - t / .3f : 0;
                rect.anchoredPosition = Local(root, pointer.From) + new Vector2(1, -1) * (approach * theme.TutorialTapApproach);
            }
            else
            {
                // Nhịp kéo lấy từ TutorialHand.anim của bộ gốc: kéo 1 s, giữ .25 s, mờ .25 s, nghỉ .5 s.
                float t = Mathf.Repeat(handAge, theme.TutorialDragPeriod) / theme.TutorialDragPeriod;
                float move = Mathf.Clamp01(t / .5f);
                alpha = t < .075f ? t / .075f : t < .625f ? 1 : t < .75f ? 1 - (t - .625f) / .125f : 0;
                hand.sprite = theme.TutorialDragHand;
                rect.pivot = theme.TutorialDragPivot;
                rect.sizeDelta = theme.TutorialDragSize;
                rect.anchoredPosition = Vector2.Lerp(Local(root, pointer.From), Local(root, pointer.To), move);
            }

            hand.enabled = hand.sprite != null;
            hand.color = new Color(1, 1, 1, alpha);
        }

        // Chọn chỗ đặt chữ (trên hoặc dưới) che vùng sáng ít nhất.
        private void PlaceBubble(RectTransform root)
        {
            var rect = (RectTransform)bubble.transform;
            var size = rect.rect.size;
            var heights = theme.TutorialBubbleHeights;
            float best = float.MaxValue;
            for (int i = 0; i < 2; i++)
            {
                float y = root.rect.yMin + root.rect.height * (i == 0 ? heights.x : heights.y);
                var area = new Rect(root.rect.center.x - size.x * .5f, y - size.y * .5f, size.x, size.y);
                float covered = 0;
                for (int k = 0; k < local.Count; k++)
                {
                    float w = Mathf.Min(area.xMax, local[k].xMax) - Mathf.Max(area.xMin, local[k].xMin);
                    float h = Mathf.Min(area.yMax, local[k].yMax) - Mathf.Max(area.yMin, local[k].yMin);
                    covered += Mathf.Max(0, w) * Mathf.Max(0, h);
                }

                if (covered < best)
                {
                    best = covered;
                    bubbleHome = new Vector2(0, y - root.rect.center.y);
                }
            }

            rect.anchoredPosition = bubbleHome;
        }

        // Pixel màn hình → tọa độ cục bộ (Canvas Screen Space Overlay).
        private static Vector2 Local(RectTransform root, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var point);
            return point;
        }
    }
}
