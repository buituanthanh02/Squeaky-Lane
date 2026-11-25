using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SqueakyLane.Presentation
{
    // Trang trí ngay trên Image đã có: không thay object, vùng raycast hay CanvasGroup.
    [RequireComponent(typeof(Image))]
    public sealed class UiPanelDecoration : BaseMeshEffect
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private bool dialog;
        private readonly List<UIVertex> stream = new List<UIVertex>(64);
        public void Configure(UiTheme value, bool isDialog)
        {
            theme = value;
            dialog = isDialog;
            graphic.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || theme == null) { return; }
            var rect = graphic.rectTransform.rect;
            if (!dialog)
            {
                UIVertex vertex = default;
                for (int i = 0; i < helper.currentVertCount; i++)
                {
                    helper.PopulateUIVertex(ref vertex, i);
                    float t = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                    vertex.color = t < .5f ? Color.Lerp(theme.SkyBottom, theme.SkyMiddle, t * 2) : Color.Lerp(theme.SkyMiddle, theme.SkyTop, (t - .5f) * 2);
                    helper.SetUIVertex(vertex, i);
                }
                return;
            }
            stream.Clear();
            helper.GetUIVertexStream(stream);
            helper.Clear();
            var image = (Image)graphic;
            // Góc giữa sprite panel là vùng trắng, dùng nó vẽ lớp tối toàn màn trong cùng draw call.
            var uv = image.sprite != null ? UnityEngine.Sprites.DataUtility.GetInnerUV(image.sprite) : Vector4.zero;
            var centerUv = new Vector2((uv.x + uv.z) * .5f, (uv.y + uv.w) * .5f);
            var shade = UIVertex.simpleVert;
            shade.color = theme.Shade;
            shade.uv0 = centerUv;
            shade.position = new Vector3(rect.xMin, rect.yMin); helper.AddVert(shade);
            shade.position = new Vector3(rect.xMin, rect.yMax); helper.AddVert(shade);
            shade.position = new Vector3(rect.xMax, rect.yMax); helper.AddVert(shade);
            shade.position = new Vector3(rect.xMax, rect.yMin); helper.AddVert(shade);
            helper.AddTriangle(0, 1, 2);
            helper.AddTriangle(2, 3, 0);
            var inset = theme.DialogInset;
            for (int i = 0; i < stream.Count; i++)
            {
                var vertex = stream[i];
                vertex.position.x = rect.center.x + (vertex.position.x - rect.center.x) * (1 - inset.x * 2);
                vertex.position.y = rect.center.y + (vertex.position.y - rect.center.y) * (1 - inset.y * 2);
                stream[i] = vertex;
            }
            helper.AddUIVertexTriangleStream(stream);
        }
    }
}
