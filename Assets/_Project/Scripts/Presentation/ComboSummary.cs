using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Khối combo trong bảng Thắng / Hết giờ. Chưa phá kỷ lục: số combo + dòng "Kỷ lục".
    // Phá kỷ lục: số combo, rồi huy hiệu rơi từ cao (whoosh), đóng "cộp" xuống (rung giao diện, vòng xung, sao bắn) và lấp lánh.
    // Thẻ hộp thoại tự cao thêm đúng phần khối này chiếm; không có gì để khoe thì giữ nguyên cỡ.
    public sealed class ComboSummary : MonoBehaviour
    {
        private const int Sparks = 8;
        [SerializeField, Tooltip("Thẻ hộp thoại sẽ cao thêm.")] private RectTransform card = null;
        [SerializeField, Tooltip("Cụm nút; khối combo được chèn ngay trên nó trong cùng cha.")] private RectTransform buttons = null;
        [SerializeField, Tooltip("Mẫu chữ cho dòng kỷ lục (font và material).")] private TMP_Text textTemplate = null;
        [SerializeField, Tooltip("Cả vùng giao diện rung khi đóng huy hiệu; trống thì chỉ rung thẻ.")] private RectTransform shakeRoot = null;
        private UiTheme theme;
        private FeelConfig feel;
        private Vector2 cardSize, cardAt;
        private LayoutElement slot;
        private bool captured;
        private RectTransform area, badgeRoot;
        private ComboLabel label;
        private TextMeshProUGUI recordLine;
        private Image badge, glow, ring;
        private Vector2 shakeAt;
        private bool shaking;
        private readonly Image[] sparks = new Image[Sparks];
        private float age;
        private bool playing, whooshed, slammed;

        public bool IsNewRecord { get; private set; }
        public int Combo { get; private set; }
        public int Record { get; private set; }
        public bool Visible => area != null && area.gameObject.activeSelf;
        public string RecordText => recordLine != null && recordLine.gameObject.activeSelf ? recordLine.text : "";
        public bool BadgeShown => badgeRoot != null && badgeRoot.gameObject.activeSelf;
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
            float height = !any ? 0 : (hasCombo ? theme.SummaryComboHeight : 0) + (newRecord ? theme.SummaryBadgeHeight : theme.SummaryLineHeight);
            StopShake();
            area.gameObject.SetActive(any);
            Layout(height);
            label.Root.gameObject.SetActive(hasCombo);
            recordLine.gameObject.SetActive(any && !newRecord);
            badgeRoot.gameObject.SetActive(false);
            ring.enabled = false;
            foreach (var spark in sparks) { spark.enabled = false; }
            if (!any) { playing = false; return; }
            float top = height * .5f;
            if (hasCombo)
            {
                label.Set(combo, theme.SummaryDigitHeight);
                label.Root.anchoredPosition = new Vector2(0, top - theme.SummaryComboHeight * .5f);
                label.Root.localScale = Vector3.zero;
            }

            float lineY = -top + (newRecord ? theme.SummaryBadgeHeight : theme.SummaryLineHeight) * .5f;
            recordLine.rectTransform.anchoredPosition = new Vector2(0, lineY);
            recordLine.text = "Kỷ lục: Combo " + Record;
            badgeRoot.anchoredPosition = new Vector2(0, lineY);
            age = 0;
            playing = true;
            whooshed = slammed = false;
        }

        public void Hide()
        {
            playing = false;
            StopShake();
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
            badgeRoot = ComboLabel.Create("Kỷ lục mới", area);
            glow = ComboLabel.Picture("Quầng", badgeRoot, theme.ComboBurst);
            glow.rectTransform.sizeDelta = Vector2.one * theme.SummaryBadgeHeight * 1.5f;
            ring = ComboLabel.Picture("Vòng đập", badgeRoot, theme.ComboRing);
            badge = ComboLabel.Picture("Huy hiệu", badgeRoot, theme.RecordBadge);
            if (theme.RecordBadge != null)
            {
                float height = theme.SummaryBadgeHeight;
                badge.rectTransform.sizeDelta = new Vector2(height * theme.RecordBadge.rect.width / theme.RecordBadge.rect.height, height);
            }

            for (int i = 0; i < sparks.Length; i++) { sparks[i] = ComboLabel.Picture("Sao " + i, badgeRoot, theme.ComboSpark); }
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
            badgeRoot.gameObject.SetActive(true);
            if (!whooshed)
            {
                whooshed = true;
                Whoosh?.Invoke();
            }

            var stamp = badge.rectTransform;
            float drop = Mathf.Clamp01((age - intro) / feel.RecordWhooshSeconds);
            if (drop < 1)
            {
                // Rơi nhanh dần từ cao, to → đúng cỡ, xoay dần về độ nghiêng cuối: cảm giác đóng dấu.
                float ease = drop * drop * drop;
                stamp.anchoredPosition = new Vector2(0, Mathf.Lerp(feel.RecordDropDistance, 0, ease));
                stamp.localScale = Vector3.one * Mathf.Lerp(feel.RecordSlamScale, 1, ease);
                stamp.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(feel.RecordTiltDegrees * 3, feel.RecordTiltDegrees, ease));
                badge.color = new Color(1, 1, 1, Mathf.Clamp01(drop * 2.5f));
                glow.enabled = false;
                return;
            }

            if (!slammed)
            {
                slammed = true;
                StartShake();
                Slam?.Invoke();
            }

            float after = age - intro - feel.RecordWhooshSeconds;
            float hit = Mathf.Clamp01(after / feel.RecordImpactSeconds);
            if (shaking)
            {
                float power = feel.RecordShakeDistance * (1 - hit) * (1 - hit);
                Shaken.anchoredPosition = shakeAt + new Vector2(Mathf.Sin(after * 83) * .6f, Mathf.Cos(after * 61)) * power;
                if (hit >= 1) { StopShake(); }
            }

            ring.enabled = hit < 1;
            ring.rectTransform.sizeDelta = Vector2.one * theme.SummaryBadgeHeight * Mathf.Lerp(.7f, 2.6f, 1 - (1 - hit) * (1 - hit));
            ring.color = new Color(1, 1, 1, 1 - hit);
            // Sau cú đóng: huy hiệu lún rồi nảy, phập phồng và chớp sáng; quầng xoay phía sau, sao lấp lánh quanh.
            float wave = .5f + .5f * Mathf.Sin(after / feel.RecordPulseSeconds * Mathf.PI * 2);
            float squash = hit < .35f ? Mathf.Sin(hit / .35f * Mathf.PI) * .16f : 0;
            stamp.anchoredPosition = Vector2.zero;
            stamp.localRotation = Quaternion.Euler(0, 0, feel.RecordTiltDegrees);
            stamp.localScale = new Vector3(1 + squash, 1 - squash, 1) * (1 + feel.RecordPulseScale * wave);
            badge.color = Color.Lerp(Color.white, theme.RecordFlashColor, wave * .6f);
            glow.enabled = true;
            glow.rectTransform.localRotation = Quaternion.Euler(0, 0, -after * feel.ComboBurstDegrees);
            var tint = theme.ComboBurstColor;
            tint.a *= hit * (.6f + .4f * wave);
            glow.color = tint;
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = (i + .5f) / sparks.Length * Mathf.PI * 2;
                float reach = Mathf.Lerp(.3f, 1, 1 - (1 - hit) * (1 - hit));
                float twinkle = Mathf.Abs(Mathf.Sin(after * 4 + i * 1.7f));
                sparks[i].enabled = true;
                sparks[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * theme.SummaryBadgeHeight * .75f * reach,
                    Mathf.Sin(angle) * theme.SummaryBadgeHeight * .5f * reach);
                sparks[i].rectTransform.sizeDelta = Vector2.one * theme.SummaryBadgeHeight * (.07f + .11f * twinkle);
                sparks[i].color = new Color(1, 1, 1, .35f + .65f * twinkle);
            }
        }

        private RectTransform Shaken => shakeRoot != null ? shakeRoot : card;
        private void StartShake()
        {
            if (Shaken == null) { return; }
            shaking = true;
            shakeAt = Shaken.anchoredPosition;
        }
        private void StopShake()
        {
            if (!shaking) { return; }
            shaking = false;
            Shaken.anchoredPosition = shakeAt;
        }
        private void OnDisable() { StopShake(); }

        // 0 → vọt quá cỡ → về 1.
        public static float Overshoot(float t, float peak)
        {
            if (t <= 0) { return 0; }
            if (t >= 1) { return 1; }
            return t < .6f ? Mathf.Lerp(0, peak, t / .6f) : Mathf.Lerp(peak, 1, (t - .6f) / .4f);
        }
    }
}
