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
            if (theme.StreetsEnabled)
            {
                SpawnStreets(level, theme);
                if (theme.CityShops != null && theme.CityPark != null) { SpawnIllustratedTown(level, theme); }
                else { SpawnTown(level, theme); }
            }
            else { SpawnProps(level, theme, vehicles); }
        }

        // Các sprite phẳng cùng mặt phẳng XZ của đường. Dời cả dải ra ngoài thảm nếu cạnh đó có hầm rửa.
        private void SpawnIllustratedTown(LevelDef level, UiTheme theme)
        {
            float innerX = level.Cols * .5f + theme.StreetInner;
            float roadDepth = theme.StreetInner + theme.StreetWidth;
            float north = level.Rows * .5f + roadDepth;
            float south = north;
            foreach (var gate in level.Gates)
            {
                if (gate.Color != TileColor.Water) { continue; }
                if (gate.Edge == GateEdge.Top) { north = level.Rows * .5f + Mathf.Max(roadDepth, WashGateReach); }
                if (gate.Edge == GateEdge.Bottom) { south = level.Rows * .5f + Mathf.Max(roadDepth, WashGateReach); }
            }

            float width = innerX * 2 * theme.CityBlockWidth;
            PlaceTownStrip(theme.CityShops, "016 Cửa hàng 2D", new Vector2(width, theme.CityBlockDepth.x), 0, north, 1, theme);
            PlaceTownStrip(theme.CityPark, "016 Công viên 2D", new Vector2(width, theme.CityBlockDepth.y), 0, south, -1, theme);
            if (theme.CitySidewalk == null) { return; }

            // Vỉa hè dọc nằm ở hai góc xen kẽ, phía trong hai phố dọc, ngoài vùng bàn và cổng.
            float sideWidth = Mathf.Min(theme.CitySideSize.x, innerX - width * .5f);
            var sideSize = new Vector2(sideWidth, theme.CitySideSize.y);
            float sideX = innerX - sideWidth * .5f;
            PlaceTownStrip(theme.CitySidewalk, "016 Vỉa hè dọc trên", sideSize, sideX, north, 1, theme);
            PlaceTownStrip(theme.CitySidewalk, "016 Vỉa hè dọc dưới", sideSize, -sideX, south, -1, theme);
        }

        private void PlaceTownStrip(Sprite sprite, string label, Vector2 maxSize, float x, float roadEdge, int direction, UiTheme theme)
        {
            var size = sprite.bounds.size;
            float scale = Mathf.Min(maxSize.x / size.x, maxSize.y / size.y);
            float z = direction * (roadEdge + theme.CityStreetGap + size.y * scale * .5f);
            var node = new GameObject(label, typeof(SpriteRenderer));
            node.transform.SetParent(transform, false);
            node.transform.localPosition = new Vector3(x, GroundY(theme) + theme.ContactShadowLift, z);
            node.transform.localRotation = Quaternion.Euler(90, 0, 0);
            node.transform.localScale = Vector3.one * scale;
            node.GetComponent<SpriteRenderer>().sprite = sprite;
            props.Add(node);
        }

        // 016: khi có phố, nhà/cây xếp thành hàng ngoài hai phố ngang (giữa hai phố dọc), không đè đường và thảm hầm rửa.
        // Danh sách Prop Prefabs = dãy phía trên, South Prop Prefabs = dãy phía dưới (vật thấp, vì nút bấm nằm đó).
        private const float TownGap = .25f, TownStreetMargin = .2f;
        private const int TownRows = 2, TownLimit = 12;
        private void SpawnTown(LevelDef level, UiTheme theme)
        {
            float half = level.Cols * .5f + theme.StreetInner - TownStreetMargin;
            float outerZ = level.Rows * .5f + theme.StreetInner + theme.StreetWidth + TownStreetMargin;
            int used = TownRow(level, theme, theme.PropPrefabs, half, outerZ, 1, 0);
            TownRow(level, theme, theme.SouthPropsEnabled ? theme.SouthPropPrefabs : null, half, outerZ, -1, used);
        }
        // Xếp lần lượt từ trái sang phải; đầy hàng thì sang hàng xa hơn; mỗi hàng được căn giữa. Trả về số vật đã đặt.
        private int TownRow(LevelDef level, UiTheme theme, GameObject[] prefabs, float half, float startZ, int direction, int used)
        {
            if (prefabs == null) { return used; }
            int index = 0;
            float z = startZ;
            for (int row = 0; row < TownRows && index < prefabs.Length && used < TownLimit; row++)
            {
                var line = new List<GameObject>();
                var sizes = new List<Bounds>();
                float width = 0, depth = 0;
                while (index < prefabs.Length && used + line.Count < TownLimit)
                {
                    if (prefabs[index] == null) { index++; continue; }
                    var size = LocalBounds(prefabs[index]);
                    float next = width + (line.Count > 0 ? TownGap : 0) + size.size.x;
                    if (line.Count > 0 && next > half * 2) { break; }
                    line.Add(Instantiate(prefabs[index], transform, false)); sizes.Add(size);
                    width = next; depth = Mathf.Max(depth, size.size.z); index++;
                }
                float x = -width * .5f;
                for (int i = 0; i < line.Count; i++)
                {
                    var item = line[i]; var size = sizes[i];
                    float zPos = direction > 0 ? z - size.min.z : -z - size.max.z;
                    item.transform.localPosition = new Vector3(x + size.extents.x - size.center.x, GroundY(theme) - size.min.y, zPos);
                    x += size.size.x + TownGap;
                    if (OnWashGatePad(level, item)) { Release(item); continue; }
                    props.Add(item); used++;
                }
                z += depth + TownGap * 2;
            }
            return used;
        }

        // 016: lưới phố hình # quanh bãi: 2 phố ngang chạy suốt, 2 phố dọc bỏ đoạn giao nhau (khỏi chồng mặt).
        // Một mesh, một material đường; U dọc đường tính bằng ô để vạch giữa lặp đều, V ngang đường 0..1.
        private Mesh streetMesh;
        private void SpawnStreets(LevelDef level, UiTheme theme)
        {
            if (streetMesh != null) { Release(streetMesh); }
            streetMesh = BuildStreets(level, theme);
            var road = new GameObject("016 Streets", typeof(MeshFilter), typeof(MeshRenderer));
            road.transform.SetParent(transform, false);
            road.GetComponent<MeshFilter>().sharedMesh = streetMesh;
            var renderer = road.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = theme.RoadMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            props.Add(road);
        }
        public static Mesh BuildStreets(LevelDef level, UiTheme theme)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float y = GroundY(theme) + .004f, w = theme.StreetWidth, length = theme.StreetLength;
            float nearX = level.Cols * .5f + theme.StreetInner, farX = nearX + w;
            float nearZ = level.Rows * .5f + theme.StreetInner, farZ = nearZ + w;
            // Phố ngang trên/dưới: chạy dọc X.
            Strip(v, t, uv, new Vector3(-length, y, nearZ), new Vector3(length, y, nearZ), new Vector3(0, 0, w));
            Strip(v, t, uv, new Vector3(-length, y, -farZ), new Vector3(length, y, -farZ), new Vector3(0, 0, w));
            // Phố dọc trái/phải: chạy dọc Z, chia 3 đoạn để chừa chỗ giao với phố ngang.
            foreach (float x in new[] { -farX, nearX })
            {
                Strip(v, t, uv, new Vector3(x, y, -length), new Vector3(x, y, -farZ), new Vector3(w, 0, 0));
                Strip(v, t, uv, new Vector3(x, y, -nearZ), new Vector3(x, y, nearZ), new Vector3(w, 0, 0));
                Strip(v, t, uv, new Vector3(x, y, farZ), new Vector3(x, y, length), new Vector3(w, 0, 0));
            }
            var mesh = new Mesh { name = "016 Phố quanh bãi" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        // Dải đường từ a tới b (tim một mép), bề ngang theo `across`; mặt hướng lên.
        private static void Strip(List<Vector3> v, List<int> t, List<Vector2> uv, Vector3 a, Vector3 b, Vector3 across)
        {
            int first = v.Count;
            float lengthCells = Vector3.Distance(a, b);
            float startU = Vector3.Dot(a, (b - a).normalized);
            v.Add(a); v.Add(a + across); v.Add(b + across); v.Add(b);
            uv.Add(new Vector2(startU, 0)); uv.Add(new Vector2(startU, 1)); uv.Add(new Vector2(startU + lengthCells, 1)); uv.Add(new Vector2(startU + lengthCells, 0));
            // Chọn chiều tam giác để mặt hướng lên (+Y).
            var normal = Vector3.Cross(across, b - a);
            if (normal.y >= 0) { t.Add(first); t.Add(first + 1); t.Add(first + 2); t.Add(first + 2); t.Add(first + 3); t.Add(first); }
            else { t.Add(first); t.Add(first + 3); t.Add(first + 2); t.Add(first + 2); t.Add(first + 1); t.Add(first); }
        }
        // Hàng props cách bãi: có phố thì đặt ngoài mép xa của phố.
        private static float RowGap(UiTheme theme) => theme.StreetsEnabled ? Mathf.Max(theme.PropGap, theme.StreetInner + theme.StreetWidth + .35f) : theme.PropGap;

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
                    level.Rows * .5f + RowGap(theme) + frontRowDepth - bounds.min.z);
                if (OnWashGatePad(level, root)) { Release(root); continue; }
                props.Add(root);
                used++;
            }
            if (!theme.SouthPropsEnabled) { return; }
            var lower = theme.SouthPropPrefabs;
            float roadZ = -level.Rows * .5f - RowGap(theme);
            float roadHeight = 0;
            for (int i = 0; lower != null && i < lower.Length && used < 12; i++)
            {
                if (lower[i] == null) { continue; }
                var root = Instantiate(lower[i], transform, false);
                var bounds = LocalBounds(root);
                float x = i == 0 ? 0 : (i % 2 == 0 ? 1 : -1) * level.Cols * .3f;
                float z = -level.Rows * .5f - RowGap(theme) - bounds.max.z;
                root.transform.localPosition = new Vector3(x - bounds.center.x, GroundY(theme) - bounds.min.y, z);
                if (OnWashGatePad(level, root)) { Release(root); continue; }
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
        // 015: hầm rửa ở cổng nước có thảm dài ~2 ô ra ngoài bãi (WashGate_0); props không được mọc đè lên đó.
        private const float WashGateReach = 2.2f, WashGateHalfWidth = .65f;
        private bool OnWashGatePad(LevelDef level, GameObject prop)
        {
            var b = LocalBounds(prop); b.center += prop.transform.localPosition;
            float x = level.Cols * .5f, z = level.Rows * .5f;
            foreach (var gate in level.Gates)
            {
                if (gate.Color != TileColor.Water) { continue; }
                Rect pad;
                float along = gate.Edge == GateEdge.Top || gate.Edge == GateEdge.Bottom ? gate.Index - (level.Cols - 1) * .5f : (level.Rows - 1) * .5f - gate.Index;
                switch (gate.Edge)
                {
                    case GateEdge.Top: pad = Rect.MinMaxRect(along - WashGateHalfWidth, z, along + WashGateHalfWidth, z + WashGateReach); break;
                    case GateEdge.Bottom: pad = Rect.MinMaxRect(along - WashGateHalfWidth, -z - WashGateReach, along + WashGateHalfWidth, -z); break;
                    case GateEdge.Left: pad = Rect.MinMaxRect(-x - WashGateReach, along - WashGateHalfWidth, -x, along + WashGateHalfWidth); break;
                    default: pad = Rect.MinMaxRect(x, along - WashGateHalfWidth, x + WashGateReach, along + WashGateHalfWidth); break;
                }
                if (pad.Overlaps(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z))) { return true; }
            }
            return false;
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
            if (theme.TrayEnabled && theme.TrayCornerRadius > 0)
            {
                RoundedTray(vertices, indices, uv, x + theme.PavementWidth, z + theme.PavementWidth, theme);
            }
            else
            {
                Quad(vertices, indices, uv, -x - theme.PavementWidth, -z - theme.PavementWidth,
                    x + theme.PavementWidth, z + theme.PavementWidth, LotSurfaceView.Top - .02f, 1, columns);
            }
            if (theme.TrayEnabled && theme.TrayCornerRadius <= 0)
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
        // 015: khay bo góc: mặt trên (quạt tam giác, màu vỉa hè 1), thành bên theo chu vi (màu 9), viền sáng chạy theo góc (màu 10).
        // Vẫn chung một mesh/material với cỏ nên không thêm draw call. Chu vi đi theo chiều kim đồng hồ nhìn từ trên xuống.
        private const int CornerSegments = 6;
        private static void RoundedTray(List<Vector3> v, List<int> t, List<Vector2> uv, float halfX, float halfZ, UiTheme theme)
        {
            float top = LotSurfaceView.Top - .02f, bottom = top - theme.TrayThickness;
            float radius = Mathf.Min(theme.TrayCornerRadius, halfX, halfZ);
            float inset = Mathf.Min(theme.PavementHighlightWidth, radius * .9f);
            var outer = Perimeter(halfX, halfZ, radius);
            var inner = Perimeter(halfX - inset, halfZ - inset, radius - inset);
            int n = outer.Count;
            // Mặt trên: quạt từ tâm, dùng phần bên trong viền sáng.
            int center = v.Count;
            v.Add(new Vector3(0, top, 0)); uv.Add(Palette(1));
            for (int i = 0; i < n; i++) { v.Add(new Vector3(inner[i].x, top, inner[i].y)); uv.Add(Palette(1)); }
            for (int i = 0; i < n; i++) { t.Add(center); t.Add(center + 1 + i); t.Add(center + 1 + (i + 1) % n); }
            for (int i = 0; i < n; i++)
            {
                var a = outer[i]; var b = outer[(i + 1) % n]; var qa = inner[i]; var qb = inner[(i + 1) % n];
                // Viền sáng: dải giữa chu vi ngoài và chu vi trong, cao hơn mặt khay một chút.
                int first = v.Count;
                v.Add(new Vector3(a.x, top + .001f, a.y)); v.Add(new Vector3(b.x, top + .001f, b.y));
                v.Add(new Vector3(qb.x, top + .001f, qb.y)); v.Add(new Vector3(qa.x, top + .001f, qa.y));
                for (int k = 0; k < 4; k++) { uv.Add(Palette(10)); }
                t.Add(first); t.Add(first + 1); t.Add(first + 2); t.Add(first + 2); t.Add(first + 3); t.Add(first);
                // Thành bên hướng ra ngoài.
                Face(v, t, uv, new Vector3(b.x, bottom, b.y), new Vector3(b.x, top, b.y), new Vector3(a.x, top, a.y), new Vector3(a.x, bottom, a.y), 9);
            }
        }
        private static List<Vector2> Perimeter(float halfX, float halfZ, float radius)
        {
            var points = new List<Vector2>(4 * (CornerSegments + 1));
            radius = Mathf.Max(0, radius);
            var centers = new[] { new Vector2(-halfX + radius, halfZ - radius), new Vector2(halfX - radius, halfZ - radius),
                new Vector2(halfX - radius, -halfZ + radius), new Vector2(-halfX + radius, -halfZ + radius) };
            for (int corner = 0; corner < 4; corner++)
            {
                float start = 180 - corner * 90;
                for (int s = 0; s <= CornerSegments; s++)
                {
                    float angle = (start - 90f * s / CornerSegments) * Mathf.Deg2Rad;
                    points.Add(centers[corner] + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
            }
            return points;
        }
        private static Vector2 Palette(int color) => new Vector2((color + .5f) / GroundColorCount, .5f);
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
        private void OnDestroy() { if (ownedMesh != null) { Release(ownedMesh); } if (streetMesh != null) { Release(streetMesh); } }
    }
}
