using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SceneThemeView : MonoBehaviour
    {
        private Mesh ownedMesh;
        private Vector3[] groundVertices;
        private float minimumX, minimumZ, overscan;
        private readonly List<GameObject> props = new List<GameObject>(12);
        public const int GroundColorCount = 11;
        public void Configure(LevelDef level, UiTheme theme, CarCatalog vehicles = null)
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
            if (theme.TrayEnabled && theme.ContactShadowPrefab != null)
            {
                var shadow = Instantiate(theme.ContactShadowPrefab, transform, false);
                shadow.name = "013 Tray Shadow";
                shadow.transform.localPosition = new Vector3(theme.ContactShadowOffset.x,
                    GroundY(theme) + theme.ContactShadowLift, theme.ContactShadowOffset.y);
                shadow.transform.localScale = new Vector3(level.Cols + 2 * (theme.PavementWidth + theme.GroundShadowSpread),
                    1, level.Rows + 2 * (theme.PavementWidth + theme.GroundShadowSpread));
                props.Add(shadow);
            }
            SpawnProps(level, theme, vehicles);
        }

        private void SpawnProps(LevelDef level, UiTheme theme, CarCatalog vehicles)
        {
            var prefabs = theme.PropPrefabs;
            int count = prefabs == null ? 0 : Mathf.Min(theme.PropCount, prefabs.Length, 12);
            int used = 0;
            for (int i = 0; i < count; i++)
            {
                if (prefabs[i] == null) { continue; }
                var root = Instantiate(prefabs[i], transform, false);
                var bounds = LocalBounds(root);
                float x;
                // Hai nhà cạnh nhau ở hàng trước; cây/đèn ở hàng sau, không xếp chồng hình.
                if (i < 2)
                {
                    float other = count > 1 && prefabs[1 - i] != null ? LocalBounds(prefabs[1 - i]).size.x : 0;
                    x = count > 1 ? (other + theme.PropGap) * .5f * (i == 0 ? -1 : 1) : 0;
                }
                else { x = (i - 2 - (count - 3) * .5f) * theme.PropSpacing; }
                float frontRowDepth = 0;
                if (i >= 2)
                {
                    for (int house = 0; house < Mathf.Min(count, 2); house++)
                    { if (prefabs[house] != null) { frontRowDepth = Mathf.Max(frontRowDepth, LocalBounds(prefabs[house]).size.z); } }
                    frontRowDepth += theme.PropGap;
                }
                root.transform.localPosition = new Vector3(x - bounds.center.x, GroundY(theme) - bounds.min.y,
                    level.Rows * .5f + theme.PropGap + frontRowDepth - bounds.min.z);
                props.Add(root);
                used++;
            }
            if (!theme.SouthPropsEnabled) { return; }
            var lower = theme.SouthPropPrefabs;
            float roadZ = -level.Rows * .5f - theme.PropGap;
            float roadHeight = 0;
            for (int i = 0; lower != null && i < lower.Length && used < 12; i++)
            {
                if (lower[i] == null) { continue; }
                var root = Instantiate(lower[i], transform, false);
                var bounds = LocalBounds(root);
                float x = i == 0 ? 0 : (i % 2 == 0 ? 1 : -1) * level.Cols * .3f;
                float z = -level.Rows * .5f - theme.PropGap - bounds.max.z;
                root.transform.localPosition = new Vector3(x - bounds.center.x, GroundY(theme) - bounds.min.y, z);
                if (i == 0) { roadZ = z; roadHeight = bounds.size.y; }
                props.Add(root);
                used++;
            }
            if (used >= 12 || theme.ParkedCarPrefab == null || vehicles == null) { return; }
            // Không dùng CarView/collider cho xe trang trí: raycast không thể ra lệnh nhầm cho xe đỗ.
            for (int color = 0; color < (int)TileColor.Water; color++)
            {
                bool gateColor = false;
                foreach (var gate in level.Gates) { if ((int)gate.Color == color) { gateColor = true; break; } }
                var entry = vehicles.Find((TileColor)color);
                if (gateColor || entry == null || entry.material == null) { continue; }
                var parked = Instantiate(theme.ParkedCarPrefab, transform, false);
                var bounds = LocalBounds(parked);
                parked.transform.localPosition = new Vector3(-bounds.center.x, GroundY(theme) + roadHeight - bounds.min.y + theme.ContactShadowLift,
                    Mathf.Min(roadZ, -level.Rows * .5f - theme.PropGap - bounds.max.z));
                parked.GetComponent<MeshRenderer>().sharedMaterial = entry.material;
                props.Add(parked);
                break;
            }
            // Nếu cả sáu màu đều có cổng thì bỏ xe trang trí thay vì phá quy tắc khác màu cổng.
        }

        public static Bounds LocalBounds(GameObject root)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool found = false;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) { continue; }
                var local = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var p = local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    p = root.transform.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if (found) { bounds.Encapsulate(p); }
                    else { bounds = new Bounds(p, Vector3.zero); found = true; }
                }
            }
            return bounds;
        }
        public static float GroundY(UiTheme theme) => LotSurfaceView.Top - (theme.TrayEnabled ? theme.TrayThickness + .02f : .04f);

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
            int columns = theme.TrayEnabled ? GroundColorCount : 9;
            Quad(vertices, indices, uv, -x - theme.GroundMargin, -z - theme.GroundMargin,
                x + theme.GroundMargin, z + theme.GroundMargin, GroundY(theme), 0, columns);
            Quad(vertices, indices, uv, -x - theme.PavementWidth, -z - theme.PavementWidth,
                x + theme.PavementWidth, z + theme.PavementWidth, LotSurfaceView.Top - .02f, 1, columns);
            if (theme.TrayEnabled)
            {
                float left = -x - theme.PavementWidth, right = x + theme.PavementWidth;
                float back = -z - theme.PavementWidth, front = z + theme.PavementWidth;
                float top = LotSurfaceView.Top - .02f, bottom = top - theme.TrayThickness;
                Face(vertices, indices, uv, new Vector3(left, bottom, back), new Vector3(left, top, back), new Vector3(right, top, back), new Vector3(right, bottom, back), 9);
                Face(vertices, indices, uv, new Vector3(right, bottom, front), new Vector3(right, top, front), new Vector3(left, top, front), new Vector3(left, bottom, front), 9);
                Face(vertices, indices, uv, new Vector3(left, bottom, front), new Vector3(left, top, front), new Vector3(left, top, back), new Vector3(left, bottom, back), 9);
                Face(vertices, indices, uv, new Vector3(right, bottom, back), new Vector3(right, top, back), new Vector3(right, top, front), new Vector3(right, bottom, front), 9);
                float w = theme.PavementHighlightWidth;
                // Viền sáng được trộn trắng/alpha sẵn vào atlas, không thêm draw/alpha layer trên nền.
                Quad(vertices, indices, uv, left, back, right, back + w, top + .001f, 10);
                Quad(vertices, indices, uv, left, front - w, right, front, top + .001f, 10);
                Quad(vertices, indices, uv, left, back + w, left + w, front - w, top + .001f, 10);
                Quad(vertices, indices, uv, right - w, back + w, right, front - w, top + .001f, 10);
            }
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
                    Quad(vertices, indices, uv, a, b, c, d, LotSurfaceView.Top + .001f, 2 + (int)gate.Color, columns);
                }
            }
            var mesh = new Mesh { name = "013 Cỏ + vỉa hè + sơn cổng" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        private static void Quad(List<Vector3> v, List<int> t, List<Vector2> uv, float left, float back, float right, float front, float y, int color, int columns = GroundColorCount)
        {
            int first = v.Count;
            v.Add(new Vector3(left, y, back)); v.Add(new Vector3(left, y, front));
            v.Add(new Vector3(right, y, front)); v.Add(new Vector3(right, y, back));
            var point = new Vector2((color + .5f) / columns, .5f);
            for (int i = 0; i < 4; i++)
            {
                // Atlas hai hàng: cỏ gần ở hàng0, xa ở hàng1; các cột khác cùng màu cả hai hàng.
                uv.Add(color == 0 ? new Vector2(point.x, i == 0 || i == 3 ? .25f : .75f) : point);
            }
            t.Add(first); t.Add(first + 1); t.Add(first + 2); t.Add(first + 2); t.Add(first + 3); t.Add(first);
        }
        private static void Face(List<Vector3> v, List<int> t, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int color)
        {
            int first = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int i = 0; i < 4; i++) { uv.Add(new Vector2((color + .5f) / GroundColorCount, .5f)); }
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
