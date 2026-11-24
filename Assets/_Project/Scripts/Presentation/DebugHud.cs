using System;
using UnityEngine;
using UnityEngine.UI;

namespace SqueakyLane.Presentation
{
    public sealed class DebugHud : MonoBehaviour
    {
        private RectTransform safe;
        private Text heading, status;
        private Font font;
        private Button undo, hint;
        private Rect lastSafe;
        private int lastWidth, lastHeight;
        public event Action Undo, Restart, Hint;
        public event Action<int> ChangeLevel;
        public void Initialize(Canvas target, bool showDebugControls = true)
        {
            if (!showDebugControls || !isActiveAndEnabled)
            {
                return;
            }

            // HUD gỡ lỗi dùng font hệ điều hành dynamic để không phụ thuộc TMP Essentials/atlas Latin thiếu dấu.
            // Không nhập thêm tệp font ngoài; UI chính thức của 009 sẽ kiểm lại font trên máy Android.
            font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Droid Sans", "sans-serif" }, 24);
            var go = new GameObject("Debug HUD", typeof(RectTransform));
            go.transform.SetParent(target.transform, false);
            safe = go.GetComponent<RectTransform>();
            heading = Text("Thông tin", safe, 29, TextAnchor.MiddleCenter);
            Rect(heading.rectTransform, new Vector2(0, .87f), Vector2.one, Vector2.zero, Vector2.zero);
            status = Text("Trạng thái", safe, 23, TextAnchor.MiddleCenter);
            Rect(status.rectTransform, new Vector2(0, .075f), new Vector2(1, .16f), Vector2.zero, Vector2.zero);
            Button("◀ Màn", 0, () => ChangeLevel?.Invoke(-1));
            undo = Button("Hoàn tác", 1, () => Undo?.Invoke());
            Button("Chơi lại", 2, () => Restart?.Invoke());
            hint = Button("Gợi ý", 3, () => Hint?.Invoke());
            Button("Màn ▶", 4, () => ChangeLevel?.Invoke(1));
            RefreshSafeArea();
        }

        public void Show(string title, string message, bool canUndo, bool canHint)
        {
            if (heading != null)
            {
                heading.text = title;
                status.text = message;
                undo.interactable = canUndo;
                hint.interactable = canHint;
            }
        }

        public Rect BoardViewport()
        {
            var area = Screen.safeArea;
            return new Rect(area.x / Screen.width, (area.y + area.height * .17f) / Screen.height, area.width / Screen.width, area.height * .69f / Screen.height);
        }

        public bool RefreshSafeArea()
        {
            if (safe == null)
            {
                return false;
            }

            if (lastSafe == Screen.safeArea && lastWidth == Screen.width && lastHeight == Screen.height)
            {
                return false;
            }

            lastSafe = Screen.safeArea;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            safe.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            return true;
        }

        private void OnEnable()
        {
            if (safe != null)
            {
                safe.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (safe != null)
            {
                safe.gameObject.SetActive(false);
            }
        }

        private Button Button(string label, int index, Action action)
        {
            var button = MakeButton(label, safe, action);
            var rect = button.GetComponent<RectTransform>();
            Rect(rect, new Vector2(index / 5f, 0), new Vector2((index + 1) / 5f, .07f), new Vector2(3, 4), new Vector2(-3, -4));
            return button;
        }

        private Button MakeButton(string label, Transform parent, Action action)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<Image>().color = new Color(.22f, .27f, .39f, .96f);
            var button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => action());
            var text = Text(label, obj.transform, 22, TextAnchor.MiddleCenter);
            Rect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private Text Text(string value, Transform parent, int size, TextAnchor alignment)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = low;
            rect.offsetMax = high;
        }

        private void OnDestroy()
        {
            if (safe != null)
            {
                Destroy(safe.gameObject);
            }

            if (font != null)
            {
                Destroy(font);
            }
        }
    }
}
