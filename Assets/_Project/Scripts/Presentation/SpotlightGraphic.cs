using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SqueakyLane.Presentation
{
    // 019: lớp tối khoét lỗ bo góc (vùng sáng) hoặc viền quanh lỗ. Vẽ bằng mesh, không cần shader/material riêng.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpotlightGraphic : MaskableGraphic, IPointerClickHandler, ICanvasRaycastFilter
    {
        private const int CornerSteps = 8;

        [SerializeField, Tooltip("Bật: vẽ viền quanh lỗ. Tắt: phủ tối phần ngoài lỗ.")] private bool outline = false;

        private readonly List<Rect> holes = new List<Rect>(8);
        private readonly List<float> cuts = new List<float>(128);
        private readonly List<Vector4> spans = new List<Vector4>(8);
        private readonly List<Vector2> ring = new List<Vector2>(48);
        private float radius, ringWidth, edgeInset;
        private RectTransform passThrough;

        public event Action Clicked;

        // Lỗ tính theo tọa độ cục bộ của RectTransform này; chỉ dựng lại mesh khi có thay đổi nhìn thấy được.
        public void Show(List<Rect> localHoles, float cornerRadius, float width, float inset)
        {
            bool same = localHoles.Count == holes.Count && Mathf.Approximately(radius, cornerRadius)
                && Mathf.Approximately(ringWidth, width) && Mathf.Approximately(edgeInset, inset);
            for (int i = 0; same && i < holes.Count; i++)
            {
                same = Vector2.SqrMagnitude(holes[i].min - localHoles[i].min) < .25f && Vector2.SqrMagnitude(holes[i].max - localHoles[i].max) < .25f;
            }

            if (same)
            {
                return;
            }

            holes.Clear();
            holes.AddRange(localHoles);
            radius = cornerRadius;
            ringWidth = width;
            edgeInset = inset;
            SetVerticesDirty();
        }

        // Nút nằm dưới lớp tối vẫn bấm được (nút Tạm dừng ở bước "chạm để tiếp tục").
        public void SetPassThrough(RectTransform target) => passThrough = target;

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
            passThrough == null || !RectTransformUtility.RectangleContainsScreenPoint(passThrough, screenPoint, eventCamera);

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (outline)
            {
                for (int i = 0; i < holes.Count; i++)
                {
                    AddRing(mesh, holes[i]);
                }
            }
            else
            {
                AddShade(mesh);
                AddEdgeFade(mesh);
            }
        }

        // Chia màn thành dải ngang; mỗi dải tô phần nằm giữa các lỗ. Góc bo được chia nhỏ thành nhiều dải.
        private void AddShade(VertexHelper mesh)
        {
            var area = rectTransform.rect;
            area.xMin += edgeInset;
            area.xMax -= edgeInset;
            area.yMin += edgeInset;
            area.yMax -= edgeInset;
            cuts.Clear();
            cuts.Add(area.yMin);
            cuts.Add(area.yMax);
            for (int i = 0; i < holes.Count; i++)
            {
                var hole = holes[i];
                float r = Radius(hole);
                for (int k = 0; k <= CornerSteps; k++)
                {
                    // Góc đều theo cung tròn để mép cong mịn.
                    float offset = r * (1 - Mathf.Cos(k * Mathf.PI * .5f / CornerSteps));
                    AddCut(hole.yMin + offset, area);
                    AddCut(hole.yMax - offset, area);
                }
            }

            cuts.Sort();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float low = cuts[i], high = cuts[i + 1];
                if (high - low < .01f)
                {
                    continue;
                }

                spans.Clear();
                for (int h = 0; h < holes.Count; h++)
                {
                    var hole = holes[h];
                    if (hole.yMin > low + .01f || hole.yMax < high - .01f)
                    {
                        continue;
                    }

                    float a = Indent(hole, low), b = Indent(hole, high);
                    // x trái dưới, x phải dưới, x trái trên, x phải trên.
                    Insert(new Vector4(hole.xMin + a, hole.xMax - a, hole.xMin + b, hole.xMax - b));
                }

                float left0 = area.xMin, left1 = area.xMin;
                for (int s = 0; s < spans.Count; s++)
                {
                    var span = spans[s];
                    AddQuad(mesh, new Vector2(left0, low), new Vector2(Mathf.Max(left0, span.x), low),
                        new Vector2(Mathf.Max(left1, span.z), high), new Vector2(left1, high));
                    left0 = Mathf.Max(left0, span.y);
                    left1 = Mathf.Max(left1, span.w);
                }

                AddQuad(mesh, new Vector2(left0, low), new Vector2(Mathf.Max(left0, area.xMax), low),
                    new Vector2(Mathf.Max(left1, area.xMax), high), new Vector2(left1, high));
            }
        }

        // Dải chuyển từ trong suốt ở mép màn tới độ tối đầy đủ, để viền đỏ cảnh báo vẫn rõ mà không lộ cạnh cứng.
        private void AddEdgeFade(VertexHelper mesh)
        {
            if (edgeInset <= 0)
            {
                return;
            }

            var outer = rectTransform.rect;
            var inner = Rect.MinMaxRect(outer.xMin + edgeInset, outer.yMin + edgeInset, outer.xMax - edgeInset, outer.yMax - edgeInset);
            var clear = color;
            clear.a = 0;
            AddQuad(mesh, new Vector2(outer.xMin, outer.yMin), new Vector2(outer.xMax, outer.yMin), new Vector2(inner.xMax, inner.yMin), new Vector2(inner.xMin, inner.yMin), clear, color);
            AddQuad(mesh, new Vector2(outer.xMax, outer.yMin), new Vector2(outer.xMax, outer.yMax), new Vector2(inner.xMax, inner.yMax), new Vector2(inner.xMax, inner.yMin), clear, color);
            AddQuad(mesh, new Vector2(outer.xMax, outer.yMax), new Vector2(outer.xMin, outer.yMax), new Vector2(inner.xMin, inner.yMax), new Vector2(inner.xMax, inner.yMax), clear, color);
            AddQuad(mesh, new Vector2(outer.xMin, outer.yMax), new Vector2(outer.xMin, outer.yMin), new Vector2(inner.xMin, inner.yMin), new Vector2(inner.xMin, inner.yMax), clear, color);
        }

        private void AddCut(float y, Rect area)
        {
            if (y > area.yMin && y < area.yMax)
            {
                cuts.Add(y);
            }
        }

        private void Insert(Vector4 span)
        {
            int at = 0;
            while (at < spans.Count && spans[at].x + spans[at].z <= span.x + span.z)
            {
                at++;
            }

            spans.Insert(at, span);
        }

        private float Radius(Rect hole) => Mathf.Min(radius, Mathf.Min(hole.width, hole.height) * .5f);

        // Độ thụt vào của mép lỗ tại độ cao y do góc bo.
        private float Indent(Rect hole, float y)
        {
            float r = Radius(hole);
            float into = Mathf.Max(hole.yMin + r - y, y - (hole.yMax - r));
            if (into <= 0)
            {
                return 0;
            }

            into = Mathf.Min(into, r);
            return r - Mathf.Sqrt(Mathf.Max(0, r * r - into * into));
        }

        private void AddRing(VertexHelper mesh, Rect hole)
        {
            float r = Radius(hole);
            ring.Clear();
            AddCorner(new Vector2(hole.xMax - r, hole.yMax - r), 0);
            AddCorner(new Vector2(hole.xMin + r, hole.yMax - r), 90);
            AddCorner(new Vector2(hole.xMin + r, hole.yMin + r), 180);
            AddCorner(new Vector2(hole.xMax - r, hole.yMin + r), 270);
            // Mỗi phần tử là hướng ra ngoài; tâm cung lưu xen kẽ.
            for (int i = 0; i < ring.Count; i += 2)
            {
                int next = (i + 2) % ring.Count;
                Vector2 centerA = ring[i], outA = ring[i + 1], centerB = ring[next], outB = ring[next + 1];
                AddQuad(mesh, centerA + outA * r, centerA + outA * (r + ringWidth), centerB + outB * (r + ringWidth), centerB + outB * r);
            }
        }

        private void AddCorner(Vector2 center, float startDegrees)
        {
            for (int k = 0; k <= CornerSteps; k++)
            {
                float angle = (startDegrees + k * 90f / CornerSteps) * Mathf.Deg2Rad;
                ring.Add(center);
                ring.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            }
        }

        private void AddQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d) => AddQuad(mesh, a, b, c, d, color, color);

        // a, b mang màu first; c, d mang màu second.
        private static void AddQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 first, Color32 second)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, first, Vector2.zero);
            mesh.AddVert(b, first, Vector2.zero);
            mesh.AddVert(c, second, Vector2.zero);
            mesh.AddVert(d, second, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
