using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Presentation
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Visual Palette")]
    public sealed class VisualPalette : ScriptableObject
    {
        [SerializeField, Tooltip("Vật liệu chung; màu từng vật dùng MaterialPropertyBlock.")] private Material material;
        [SerializeField] private Color pink = new Color(1f, .3f, .57f), blue = new Color(.16f, .59f, 1f), yellow = new Color(1f, .8f, .1f);
        [SerializeField] private Color green = new Color(.25f, .8f, .39f), orange = new Color(1f, .43f, .1f), purple = new Color(.65f, .32f, .93f), water = new Color(.25f, .95f, 1f);
        [SerializeField] private Color background = new Color(.10f, .13f, .2f), floor = new Color(.52f, .57f, .65f), wall = new Color(.24f, .28f, .36f);
        [SerializeField] private Color mud = new Color(.28f, .13f, .055f), cone = new Color(1f, .48f, .15f), table = new Color(.78f, .7f, .95f);
        [SerializeField] private Color portal = new Color(.25f, 1f, .8f), closed = new Color(.95f, .2f, .25f), open = new Color(.25f, .95f, .35f);
        public Material Material => material;
        public Color Background => background; public Color Floor => floor; public Color Wall => wall;
        public Color Mud => mud; public Color Cone => cone; public Color Table => table; public Color Portal => portal;
        public Color Closed => closed; public Color Open => open;
        public Color Car(TileColor color)
        {
            switch (color)
            {
                case TileColor.Pink: return pink; case TileColor.Blue: return blue; case TileColor.Yellow: return yellow;
                case TileColor.Green: return green; case TileColor.Orange: return orange; case TileColor.Purple: return purple;
                default: return water;
            }
        }
    }
}
