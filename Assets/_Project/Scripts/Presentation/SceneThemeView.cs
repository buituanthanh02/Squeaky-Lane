using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Presentation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SceneThemeView : MonoBehaviour
    {
        private Mesh ownedMesh;
        private Vector3[] groundVertices;
        private float minimumX, minimumZ, overscan;
        private readonly List<GameObject> props = new List<GameObject>(12);
        public void Configure(LevelDef level, UiTheme theme)
        {
            if (ownedMesh != null) { Release(ownedMesh); }
            ownedMesh = BuildGround(level, theme);
            groundVertices = ownedMesh.vertices;
            minimumX = level.Cols * .5f + theme.GroundMargin;
            minimumZ = level.Rows * .5f + theme.GroundMargin;
            overscan = theme.GroundOverscan;
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            GetComponent<MeshRenderer>().sharedMaterial = theme.GroundMaterial;
            for (int i = 0; i < props.Count; i++) { Release(props[i]); }
            props.Clear();
            var prefabs = theme.PropPrefabs;
            if (prefabs == null || prefabs.Length == 0) { return; }
            for (int i = 0; i < theme.PropCount; i++)
            {
                var prefab = prefabs[i % prefabs.Length];
                if (prefab == null) { continue; }
                var root = Instantiate(prefab, transform, false);
                // Mọi prop nằm PHÍA SAU mép Bắc, cách mép ít nhất PropGap; không che xe ở camera nhìn từ Nam.
                int slot = (i + 1) / 2 * (i % 2 == 0 ? 1 : -1);
                float halfDepth = .5f;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    halfDepth = Mathf.Max(halfDepth, renderer.bounds.extents.z);
                }
                root.transform.localPosition = new Vector3(slot * theme.PropSpacing, LotSurfaceView.Top,
                    level.Rows * .5f + theme.PropGap + halfDepth);
                props.Add(root);
            }
        }

        // Chỉ chạy sau FitCamera/đổi safe area, không mỗi frame. Chỉ nới quad cỏ, không nới vỉa hè/cổng.
        public void EnsureGroundCoverage(Camera camera)
        {
            if (camera == null || ownedMesh == null) { return; }
            float x = minimumX, z = minimumZ;
            var plane = new Plane(transform.up, transform.TransformPoint(groundVertices[0]));
            for (int i = 0; i < 4; i++)
            {
                var ray = camera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0));
                if (!plane.Raycast(ray, out float distance)) { continue; }
                var local = transform.InverseTransformPoint(ray.GetPoint(distance));
                x = Mathf.Max(x, Mathf.Abs(local.x) + overscan);
                z = Mathf.Max(z, Mathf.Abs(local.z) + overscan);
            }
            float y = groundVertices[0].y;
            groundVertices[0] = new Vector3(-x, y, -z); groundVertices[1] = new Vector3(-x, y, z);
            groundVertices[2] = new Vector3(x, y, z); groundVertices[3] = new Vector3(x, y, -z);
            ownedMesh.vertices = groundVertices;
            ownedMesh.RecalculateBounds();
        }

        // Một mesh, một material atlas9 màu: cỏ/vỉa hè +6 màu xe+nước. Không material runtime.
        public static Mesh BuildGround(LevelDef level, UiTheme theme)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var uv = new List<Vector2>();
            float x = level.Cols * .5f, z = level.Rows * .5f;
            Quad(vertices, indices, uv, -x - theme.GroundMargin, -z - theme.GroundMargin,
                x + theme.GroundMargin, z + theme.GroundMargin, LotSurfaceView.Top - .04f, 0);
            Quad(vertices, indices, uv, -x - theme.PavementWidth, -z - theme.PavementWidth,
                x + theme.PavementWidth, z + theme.PavementWidth, LotSurfaceView.Top - .02f, 1);
            if (theme.GateStrips)
            {
                foreach (var gate in level.Gates)
                {
                    float a, b, c, d;
                    float halfLane = .4f;
                    switch (gate.Edge)
                    {
                        case GateEdge.Top:
                            a = gate.Index - (level.Cols - 1) * .5f - halfLane; c = a + halfLane * 2;
                            b = z; d = z + theme.GateStripDepth; break;
                        case GateEdge.Bottom:
                            a = gate.Index - (level.Cols - 1) * .5f - halfLane; c = a + halfLane * 2;
                            b = -z - theme.GateStripDepth; d = -z; break;
                        case GateEdge.Left:
                            b = (level.Rows - 1) * .5f - gate.Index - halfLane; d = b + halfLane * 2;
                            a = -x - theme.GateStripDepth; c = -x; break;
                        default:
                            b = (level.Rows - 1) * .5f - gate.Index - halfLane; d = b + halfLane * 2;
                            a = x; c = x + theme.GateStripDepth; break;
                    }
                    Quad(vertices, indices, uv, a, b, c, d, LotSurfaceView.Top + .001f, 2 + (int)gate.Color);
                }
            }
            var mesh = new Mesh { name = "013 Cỏ + vỉa hè + sơn cổng" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        private static void Quad(List<Vector3> v, List<int> t, List<Vector2> uv, float left, float back, float right, float front, float y, int color)
        {
            int first = v.Count;
            v.Add(new Vector3(left, y, back)); v.Add(new Vector3(left, y, front));
            v.Add(new Vector3(right, y, front)); v.Add(new Vector3(right, y, back));
            var point = new Vector2((color + .5f) / 9, .5f);
            for (int i = 0; i < 4; i++)
            {
                // Atlas hai hàng: cỏ gần ở hàng0, xa ở hàng1; các cột khác cùng màu cả hai hàng.
                uv.Add(color == 0 ? new Vector2(point.x, i == 0 || i == 3 ? .25f : .75f) : point);
            }
            t.Add(first); t.Add(first + 1); t.Add(first + 2); t.Add(first + 2); t.Add(first + 3); t.Add(first);
        }
        private static void Release(Object value)
        {
            if (value == null) { return; }
            if (Application.isPlaying) { Destroy(value); }
            else { DestroyImmediate(value); }
        }
        private void OnDestroy() { if (ownedMesh != null) { Release(ownedMesh); } }
    }
}
