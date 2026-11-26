using UnityEngine;
using UnityEngine.UI;

namespace SqueakyLane.Presentation
{
    // Ghép hình "Combo" + các chữ số thành một cụm nằm giữa một RectTransform. Tạo Image một lần rồi dùng lại.
    public sealed class ComboLabel
    {
        private const int MaxDigits = 3;
        private readonly RectTransform root;
        private readonly Image burst, word;
        private readonly Image[] digits = new Image[MaxDigits];
        private readonly UiTheme theme;
        public RectTransform Root => root;
        public RectTransform Burst => burst.rectTransform;
        public int Value { get; private set; }

        public ComboLabel(RectTransform parent, UiTheme art)
        {
            theme = art;
            root = Create("Combo", parent);
            burst = Picture("Burst", root, art.ComboBurst);
            burst.color = art.ComboBurstColor;
            word = Picture("Word", root, art.ComboWord);
            for (int i = 0; i < digits.Length; i++) { digits[i] = Picture("Digit " + i, root, null); }
        }

        // digitHeight: cao của canvas chữ số, đơn vị Canvas; chữ "Combo" dùng cùng tỉ lệ pixel nên nhỏ hơn số.
        public void Set(int value, float digitHeight)
        {
            Value = Mathf.Clamp(value, 0, 999);
            var zero = theme.ComboDigit(0);
            if (zero == null || theme.ComboWord == null) { return; }
            float scale = digitHeight / zero.rect.height;
            float overlap = theme.ComboDigitOverlap * scale;
            int count = Value >= 100 ? 3 : Value >= 10 ? 2 : 1;
            float wordWidth = theme.ComboWord.rect.width * scale;
            float numberWidth = 0;
            for (int i = 0, rest = Value; i < count; i++, rest /= 10) { numberWidth += theme.ComboDigit(rest % 10).rect.width * scale; }
            numberWidth -= overlap * (count - 1);
            float left = -(wordWidth + numberWidth) * .5f;
            Place(word.rectTransform, left, wordWidth, theme.ComboWord.rect.height * scale, theme.ComboWordDrop * scale);
            float x = left + wordWidth;
            int divisor = count == 3 ? 100 : count == 2 ? 10 : 1;
            for (int i = 0; i < digits.Length; i++)
            {
                digits[i].enabled = i < count;
                if (i >= count) { continue; }
                var sprite = theme.ComboDigit(Value / divisor % 10);
                digits[i].sprite = sprite;
                float width = sprite.rect.width * scale;
                Place(digits[i].rectTransform, x, width, digitHeight, 0);
                x += width - overlap;
                divisor /= 10;
            }
            // Quầng sáng nằm sau con số.
            var glow = burst.rectTransform;
            glow.sizeDelta = Vector2.one * digitHeight * theme.ComboBurstScale;
            glow.anchoredPosition = new Vector2(left + wordWidth + numberWidth * .5f, 0);
        }

        private static void Place(RectTransform rect, float left, float width, float height, float drop)
        {
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left + width * .5f, -drop);
        }

        public static RectTransform Create(string name, RectTransform parent)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = parent.gameObject.layer;
            var rect = (RectTransform)node.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        public static Image Picture(string name, RectTransform parent, Sprite sprite)
        {
            var image = Create(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
    }
}
