using UnityEngine;
using UnityEngine.EventSystems;

namespace SqueakyLane.Presentation
{
    // Chỉ dịch nhãn trong nút. Root nhận raycast/OnClick và vị trí nút không đổi.
    public sealed class UiButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private RectTransform label;
        private Vector2 offset;
        public void Configure(UiTheme value, RectTransform text) { theme = value; label = text; }
        public void OnPointerDown(PointerEventData data)
        {
            Restore();
            if (theme == null || label == null) { return; }
            offset = Vector2.down * theme.PressOffset;
            label.anchoredPosition += offset;
        }
        public void OnPointerUp(PointerEventData data) { Restore(); }
        public void OnPointerExit(PointerEventData data) { Restore(); }
        private void Restore()
        {
            if (label != null) { label.anchoredPosition -= offset; }
            offset = Vector2.zero;
        }
        private void OnDisable() { Restore(); }
    }
}
