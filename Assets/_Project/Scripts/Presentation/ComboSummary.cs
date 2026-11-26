using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Khối combo trong bảng Thắng / Hết giờ. Chưa phá kỷ lục: số combo + dòng "Kỷ lục".
    // Phá kỷ lục: số combo, rồi dải "KỶ LỤC MỚI!" lao vào (whoosh), đập xuống (vòng xung, sao bắn, rung thẻ) và nhấp nháy.
    // Thẻ hộp thoại tự cao thêm đúng phần khối này chiếm; không có gì để khoe thì giữ nguyên cỡ.
    public sealed class ComboSummary : MonoBehaviour
    {
        private const int Sparks = 8;
        [SerializeField, Tooltip("Thẻ hộp thoại sẽ cao thêm.")] private RectTransform card = null;
        [SerializeField, Tooltip("Cụm nút; khối combo được chèn ngay trên nó trong cùng cha.")] private RectTransform buttons = null;
        [SerializeField, Tooltip("Mẫu chữ cho dòng kỷ lục (font và material).")] private TMP_Text textTemplate = null;
        private UiTheme theme;
        private FeelConfig feel;
        private Vector2 cardSize, cardAt;
        private LayoutElement slot;
        private bool captured;
        private RectTransform area, bannerRoot;
        private ComboLabel label;
        private TextMeshProUGUI recordLine;
        private Image banner, streak, ring;
        private readonly Image[] sparks = new Image[Sparks];
        private float age;
        private bool playing, whooshed, slammed;

        public bool IsNewRecord { get; private set; }
        public int Combo { get; private set; }
        public int Record { get; private set; }
        public bool Visible => area != null && area.gameObject.activeSelf;
        public string RecordText => recordLine != null && recordLine.gameObject.activeSelf ? recordLine.text : "";
        public bool BannerShown => bannerRoot != null && bannerRoot.gameObject.activeSelf;
        public event System.Action Whoosh, Slam;

        public void Configure(UiTheme art, FeelConfig config)
        {
            theme = art;
            feel = config;
        }

        // Đặt nội dung và bố cục cho lượt vừa kết thúc; hiệu ứng chạy từ đầu.
        public void Show(int combo, int previousRecord, bool newRecord)
        {
            if (theme == null || feel == null) { return; }
            Combo = combo;
            Record = newRecord ? combo : previousRecord;
            IsNewRecord = newRecord;
            Build();
            bool hasCombo = combo >= ProgressStore.MinCombo;
            bool any = hasCombo || Record >= ProgressStore.MinCombo;
            float height = !any ? 0 : (hasCombo ? theme.SummaryComboHeight : 0) + (newRecord ? theme.SummaryBannerHeight : theme.SummaryLineHeight);
            area.gameObject.SetActive(any);
            Layout(height);
            label.Root.gameObject.SetActive(hasCombo);
            recordLine.gameObject.SetActive(any && !newRecord);
            bannerRoot.gameObject.SetActive(false);
            streak.enabled = ring.enabled = false;
            foreach (var spark in sparks) { spark.enabled = false; }
            if (!any) { playing = false; return; }
            float top = height * .5f;
            if (hasCombo)
            {
                label.Set(combo, theme.SummaryDigitHeight);
                label.Root.anchoredPosition = new Vector2(0, top - theme.SummaryComboHeight * .5f);
                label.Root.localScale = Vector3.zero;
            }

            float lineY = -top + (newRecord ? theme.SummaryBannerHeight : theme.SummaryLineHeight) * .5f;
            recordLine.rectTransform.anchoredPosition = new Vector2(0, lineY);
            recordLine.text = "Kỷ lục: Combo " + Record;
            bannerRoot.anchoredPosition = new Vector2(0, lineY);
            age = 0;
            playing = true;
            whooshed = slammed = false;
        }

        public void Hide()
        {
            playing = false;
            if (area != null) { area.gameObject.SetActive(false); }
            if (captured) { Layout(0); }
        }

        // Thân hộp thoại xếp con theo chiều dọc: khối combo là một hàng cao `height`, thẻ cao thêm đúng hàng đó.
        private void Layout(float height)
        {
            if (card == null || buttons == null) { return; }
            if (!captured)
            {
                captured = true;
                cardSize = card.sizeDelta;
                cardAt = card.anchoredPosition;
            }

            var group = buttons.parent.GetComponent<VerticalLayoutGroup>();
            float gap = height > 0 && group != null ? group.spacing : 0;
            card.sizeDelta = cardSize + new Vector2(0, height + gap);
            card.anchoredPosition = cardAt;
            if (area == null) { return; }
            area.sizeDelta = new Vector2(buttons.sizeDelta.x, height);
            slot.minHeight = slot.preferredHeight = height;
            area.SetSiblingIndex(Mathf.Max(0, buttons.GetSiblingIndex() - (area.GetSiblingIndex() < buttons.GetSiblingIndex() ? 1 : 0)));
        }

        private void Build()
        {
            if (area != null) { return; }
            var parent = buttons != null ? (RectTransform)buttons.parent : (RectTransform)transform;
            area = ComboLabel.Create("018 Combo kết quả", parent);
            slot = area.gameObject.AddComponent<LayoutElement>();
            label = new ComboLabel(area, theme);
            var line = ComboLabel.Create("Dòng kỷ lục", area);
            line.sizeDelta = new Vector2(700, theme.SummaryLineHeight);
            recordLine = line.gameObject.AddComponent<TextMeshProUGUI>();
            if (textTemplate != null)
            {
                recordLine.font = textTemplate.font;
                recordLine.fontSharedMaterial = textTemplate.fontSharedMaterial;
            }

            recordLine.fontSize = theme.SummaryLineFontSize;
            recordLine.alignment = TextAlignmentOptions.Center;
            recordLine.color = theme.SummaryLineColor;
            recordLine.raycastTarget = false;
            bannerRoot = ComboLabel.Create("Kỷ lục mới", area);
            streak = ComboLabel.Picture("Vệt lao", bannerRoot, theme.ComboStreak);
            ring = ComboLabel.Picture("Vòng đập", bannerRoot, theme.ComboRing);
            banner = ComboLabel.Picture("Dải chữ", bannerRoot, theme.RecordBanner);
            if (theme.RecordBanner != null)
            {
                float height = theme.SummaryBannerHeight;
                banner.rectTransform.sizeDelta = new Vector2(height * theme.RecordBanner.rect.width / theme.RecordBanner.rect.height, height);
            }

            for (int i = 0; i < sparks.Length; i++) { sparks[i] = ComboLabel.Picture("Sao " + i, bannerRoot, theme.ComboSpark); }
        }

        private void Update() { Advance(Time.unscaledDeltaTime); }

        // Dòng thời gian tính từ lúc hộp thoại hiện: chờ → số bật ra → whoosh → đập → nhấp nháy mãi.
        public void Advance(float seconds)
        {
            if (!playing || seconds < 0) { return; }
            age += seconds;
            float intro = feel.RecordIntroSeconds;
            if (label.Root.gameObject.activeSelf)
            {
                float pop = Mathf.Clamp01((age - intro * .4f) / feel.ComboPopSeconds);
                label.Root.localScale = Vector3.one * Overshoot(pop, feel.ComboPunchScale);
                label.Burst.localRotation = Quaternion.Euler(0, 0, -age * feel.ComboBurstDegrees);
            }

            if (!IsNewRecord || age < intro) { return; }
            bannerRoot.gameObject.SetActive(true);
            if (!whooshed)
            {
                whooshed = true;
                Whoosh?.Invoke();
            }

            float fly = Mathf.Clamp01((age - intro) / feel.RecordWhooshSeconds);
            if (fly < 1)
            {
                float ease = fly * fly;
                banner.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-feel.RecordFlyDistance, 0, ease), 0);
                banner.rectTransform.localScale = Vector3.one * Mathf.Lerp(feel.RecordSlamScale, 1, ease);
                banner.color = new Color(1, 1, 1, Mathf.Clamp01(fly * 2));
                streak.enabled = true;
                streak.rectTransform.sizeDelta = new Vector2(feel.RecordFlyDistance, theme.SummaryBannerHeight * .7f);
                streak.rectTransform.anchoredPosition = banner.rectTransform.anchoredPosition - new Vector2(feel.RecordFlyDistance * .5f, 0);
                streak.color = new Color(1, 1, 1, 1 - fly * .4f);
                return;
            }

            if (!slammed)
            {
                slammed = true;
                Slam?.Invoke();
            }

            float after = age - intro - feel.RecordWhooshSeconds;
            float hit = Mathf.Clamp01(after / feel.RecordImpactSeconds);
            streak.enabled = false;
            ring.enabled = hit < 1;
            ring.rectTransform.sizeDelta = Vector2.one * theme.SummaryBannerHeight * Mathf.Lerp(.8f, 4.5f, 1 - (1 - hit) * (1 - hit));
            ring.color = new Color(1, 1, 1, 1 - hit);
            // Sau cú đập: thẻ rung tắt dần, dải chữ phập phồng và chớp sáng vàng, sao lấp lánh quanh chữ.
            if (card != null) { card.anchoredPosition = cardAt + new Vector2(0, Mathf.Sin(after * 70) * feel.RecordShakeDistance * (1 - hit)); }
            float wave = .5f + .5f * Mathf.Sin(after / feel.RecordPulseSeconds * Mathf.PI * 2);
            float squash = hit < .35f ? Mathf.Sin(hit / .35f * Mathf.PI) * .18f : 0;
            banner.rectTransform.anchoredPosition = Vector2.zero;
            banner.rectTransform.localScale = new Vector3(1 + squash, 1 - squash, 1) * (1 + feel.RecordPulseScale * wave);
            banner.color = Color.Lerp(Color.white, theme.RecordFlashColor, wave);
            float halfWidth = banner.rectTransform.sizeDelta.x * .5f;
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = (i + .5f) / sparks.Length * Mathf.PI * 2;
                float reach = Mathf.Lerp(.35f, 1.08f, 1 - (1 - hit) * (1 - hit));
                float twinkle = Mathf.Abs(Mathf.Sin(after * 4 + i * 1.7f));
                sparks[i].enabled = true;
                sparks[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * halfWidth * reach,
                    Mathf.Sin(angle) * theme.SummaryBannerHeight * .62f * reach);
                sparks[i].rectTransform.sizeDelta = Vector2.one * theme.SummaryBannerHeight * (.14f + .2f * twinkle);
                sparks[i].color = new Color(1, 1, 1, .35f + .65f * twinkle);
            }
        }

        // 0 → vọt quá cỡ → về 1.
        public static float Overshoot(float t, float peak)
        {
            if (t <= 0) { return 0; }
            if (t >= 1) { return 1; }
            return t < .6f ? Mathf.Lerp(0, peak, t / .6f) : Mathf.Lerp(peak, 1, (t - .6f) / .4f);
        }
    }
}
