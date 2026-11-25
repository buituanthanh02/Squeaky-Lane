using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SqueakyLane.Presentation
{
    // S2 co/hạ cả nút. Luôn phục hồi khi nhả, kéo ra ngoài hoặc panel bị tắt.
    public sealed class UiButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private RectTransform label;
        private Vector2 offset;
        private Vector3 originalScale;
        private bool pressed;
        private Selectable selectable;
        public void Configure(UiTheme value, RectTransform text) { theme = value; label = text; }
        private void Awake() { selectable = GetComponent<Selectable>(); }
        public void OnPointerDown(PointerEventData data)
        {
            Restore();
            if (theme == null || label == null) { return; }
            if (selectable != null && !selectable.IsInteractable()) { return; }
            originalScale = label.localScale;
            pressed = true;
            offset = Vector2.down * theme.PressOffset * (theme.HasHudSkin ? theme.HudUnit : 1);
            label.anchoredPosition += offset;
            label.localScale = originalScale * theme.PressScale;
        }
        public void OnPointerUp(PointerEventData data) { Restore(); }
        public void OnPointerExit(PointerEventData data) { Restore(); }
        private void Restore()
        {
            if (pressed && label != null)
            {
                label.anchoredPosition -= offset;
                label.localScale = originalScale;
            }
            pressed = false;
            offset = Vector2.zero;
        }
        private void OnDisable() { Restore(); }
    }
}
