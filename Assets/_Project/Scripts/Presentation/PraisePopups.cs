using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Chữ khen khi xe được rửa xong và ra cổng: chữ đậm viền, chuyển màu vàng, kèm sao; bật ra, bay lên rồi mờ.
    // Vẽ trên HUD (không nằm phẳng trên mặt bãi) để đọc được trên mọi nền. Class thường do FeelEffects sở hữu.
    public sealed class PraisePopups
    {
        private const int Pool = 4, Sparks = 3;
        // Vị trí sao quanh chữ, theo nửa bề ngang chữ (x) và cỡ chữ (y).
        private static readonly Vector2[] SparkSpots = { new Vector2(-1.04f, .45f), new Vector2(1.03f, .2f), new Vector2(.4f, -.7f) };

        private readonly RectTransform layer;
        private readonly UiTheme theme;
        private readonly Item[] items = new Item[Pool];
        private int shown;

        public PraisePopups(RectTransform parent, UiTheme art)
        {
            layer = parent;
            theme = art;
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null && items[i].Active) { count++; }
                }

                return count;
            }
        }

        // screenPoint: pixel màn hình của chỗ muốn khen (gần cổng vừa ra).
        public void Show(string message, Vector2 screenPoint)
        {
            if (layer == null || theme == null || theme.TitleFont == null) { return; }
            var item = Take();
            item.Text.text = message;
            float size = theme.PraiseFontSize;
            float half = item.Text.GetPreferredValues(message).x * .5f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out var point);
            var area = layer.rect;
            // Giữ cả chữ và sao trong màn, kể cả khi cổng nằm sát mép.
            float margin = half + theme.PraiseSparkSize;
            point.x = Mathf.Clamp(point.x, area.xMin + margin, area.xMax - margin);
            point.y = Mathf.Clamp(point.y, area.yMin + size, area.yMax - size - theme.PraiseRise);
            point = Avoid(item, point, half, size);
            item.Start = point;
            item.Age = 0;
            item.Active = true;
            item.Root.anchoredPosition = point - area.center;
            item.Root.localScale = Vector3.zero;
            item.Root.localRotation = Quaternion.Euler(0, 0, (shown++ & 1) == 0 ? theme.PraiseTilt : -theme.PraiseTilt);
            item.Group.alpha = 1;
            item.Root.SetAsLastSibling();
            item.Root.gameObject.SetActive(true);
            for (int i = 0; i < Sparks; i++)
            {
                item.Sparks[i].anchoredPosition = new Vector2(SparkSpots[i].x * half, SparkSpots[i].y * size);
            }
        }

        public void Advance(float seconds, FeelConfig feel)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null || !item.Active) { continue; }
                item.Age += seconds;
                float t = item.Age / feel.PopupSeconds;
                if (t >= 1 || item.Root == null)
                {
                    Hide(item);
                    continue;
                }

                // Bật quá cỡ rồi về 1; cùng nhịp với chữ Combo.
                float pop = Mathf.Clamp01(item.Age / feel.ComboPopSeconds);
                item.Root.localScale = Vector3.one * ComboSummary.Overshoot(pop, feel.ComboPunchScale);
                float rise = 1 - (1 - t) * (1 - t);
                item.Root.anchoredPosition = item.Start - layer.rect.center + Vector2.up * (theme.PraiseRise * rise);
                item.Group.alpha = t < .7f ? 1 : 1 - (t - .7f) / .3f;
                for (int k = 0; k < Sparks; k++)
                {
                    float twinkle = Mathf.Abs(Mathf.Sin((t * 2.2f + k * .33f) * Mathf.PI));
                    item.Sparks[k].localScale = Vector3.one * (.35f + .65f * twinkle);
                    item.Sparks[k].localRotation = Quaternion.Euler(0, 0, item.Age * 90 * (k == 1 ? -1 : 1));
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null) { Hide(items[i]); }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].Root != null) { UnityObjects.Release(items[i].Root.gameObject); }
                items[i] = null;
            }
        }

        private static void Hide(Item item)
        {
            item.Active = false;
            if (item.Root != null) { item.Root.gameObject.SetActive(false); }
        }

        // Nhiều xe ra liền nhau cùng chỗ: đẩy chữ mới lên để không đè chữ cũ.
        private Vector2 Avoid(Item self, Vector2 point, float half, float size)
        {
            for (int pass = 0; pass < Pool; pass++)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    var other = items[i];
                    if (other == null || other == self || !other.Active || other.Root == null) { continue; }
                    Vector2 at = other.Root.anchoredPosition + layer.rect.center;
                    if (Mathf.Abs(at.y - point.y) < size * 1.1f && Mathf.Abs(at.x - point.x) < half * 2)
                    {
                        point.y = at.y + size * 1.15f;
                    }
                }
            }

            return point;
        }

        // Ô trống hoặc ô cũ nhất; mỗi ô chỉ tạo một lần.
        private Item Take()
        {
            int chosen = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null || items[i].Root == null)
                {
                    items[i] = Create(i);
                    return items[i];
                }

                if (!items[i].Active) { return items[i]; }
                if (items[i].Age > items[chosen].Age) { chosen = i; }
            }

            return items[chosen];
        }

        private Item Create(int index)
        {
            var root = Node("Praise " + index, layer);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            var sparks = new RectTransform[Sparks];
            for (int i = 0; i < Sparks; i++)
            {
                sparks[i] = Node("Spark " + i, root);
                sparks[i].sizeDelta = Vector2.one * theme.PraiseSparkSize;
                var image = sparks[i].gameObject.AddComponent<Image>();
                image.sprite = theme.ComboSpark;
                image.color = theme.PraiseSparkColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.enabled = theme.ComboSpark != null;
            }

            var label = Node("Text", root);
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = theme.TitleFont;
            if (theme.HudOutline != null) { text.fontSharedMaterial = theme.HudOutline; }
            text.fontSize = theme.PraiseFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(theme.PraiseTop, theme.PraiseTop, theme.PraiseBottom, theme.PraiseBottom);
            return new Item { Root = root, Group = group, Text = text, Sparks = sparks };
        }

        private static RectTransform Node(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.layer = parent.gameObject.layer;
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private sealed class Item
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public TMP_Text Text;
            public RectTransform[] Sparks;
            public Vector2 Start;
            public float Age;
            public bool Active;
        }
    }
}
