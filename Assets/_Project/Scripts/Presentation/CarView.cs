using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Presentation
{
    public sealed class CarView : MonoBehaviour
    {
        private Renderer body, mud;
        private MaterialPropertyBlock block;
        private VisualPalette palette;
        public int Index { get; private set; }
        public int Length { get; private set; }
        public Vector3 PositiveDirection => transform.right;
        public void Initialize(int index, int length, VisualPalette colors, Renderer bodyRenderer, Renderer mudRenderer)
        { Index = index; Length = length; palette = colors; body = bodyRenderer; mud = mudRenderer; block = new MaterialPropertyBlock(); }
        public void Show(Vector3 position, float yaw, TileColor color, int dirt, bool alive, float scale = 1)
        {
            transform.localPosition = position; transform.localRotation = Quaternion.Euler(0, yaw, 0);
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(alive);
            block.SetColor("_Color", palette.Car(color)); body.SetPropertyBlock(block);
            mud.gameObject.SetActive(dirt > 0);
        }
    }
}
