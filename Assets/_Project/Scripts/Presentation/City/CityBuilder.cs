using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Presentation
{
    // 016: khu phố hoạt hình sinh theo kích thước màn (concept docs/handoff/016-ref/city-3d-topdown.png). Chỉ là hình, không chạm luật.
    // Nền + đường là một mesh không viền; cụm gần bãi và bốn vùng cảnh xa là các mesh có viền.
    public static partial class CityBuilder
    {
        public const float Ground = LotSurfaceView.Top - .08f;
        private const float Far = 24, ShopsWidth = 6.2f, ParkWidth = 5.2f;
        // Lấy từ UiTheme mỗi lần dựng để tim phố trùng đường xe ra cổng.
        private static float StreetInner = .8f, StreetWidth = .95f;

        // Dựng toàn bộ khu phố cho một màn; toạ độ theo BoardView. Người gọi sở hữu và huỷ các mesh.
        public static CityMeshes Build(LevelDef level, float streetInner, float streetWidth)
        {
            StreetInner = streetInner; StreetWidth = streetWidth;
            var clusters = new Dictionary<string, Cluster> { { "CityShops", Shops() }, { "CityPark", Park() }, { "CitySideTree", SideTree() }, { "CitySideFlowers", SideFlowers() } };
            var places = Layout(level);
            var flat = BuildGround(level);
            var fill = BuildFill(level, places, clusters);
            var near = new Mb();
            var down = Matrix4x4.Translate(new Vector3(0, -Ground, 0));
            var lift = Matrix4x4.Translate(new Vector3(0, Ground, 0));
            foreach (var region in fill) { flat.Append(region.Flat, Matrix4x4.identity); }
            foreach (var place in places)
            {
                flat.Append(clusters[place.Name].Flat, down * place.Matrix);
                near.Append(clusters[place.Name].Solid, place.Matrix);
            }
            var ground = new Mb();
            ground.Append(flat, lift);
            var far = new Mesh[4];
            var names = new[] { "North", "South", "West", "East" };
            for (int i = 0; i < 4; i++)
            {
                var placed = new Mb();
                placed.Append(fill[i].Solid, lift);
                far[i] = placed.ToMesh("016 City " + names[i]);
            }
            return new CityMeshes(ground.ToMesh("016 City Ground"), near.ToMesh("016 City Near"), far[0], far[1], far[2], far[3]);
        }

        // ---------- Cảnh nền lấp kín bốn phía (xa hơn khung của bãi nhỏ, máy bảng, máy dài) ----------
        private const float FillHalfX = 8f, FillHalfZ = 15f;
        private static readonly C[] Roofs = { C.Teal, C.Coral, C.Yellow, C.Lilac, C.Sky };
        private static readonly C[] Walls = { C.TealWall, C.CoralWall, C.YellowWall, C.LilacWall, C.SkyWall };

        private static Rect Footprint(Cluster cluster, Matrix4x4 matrix)
        {
            Vector2 min = new Vector2(1e5f, 1e5f), max = -min;
            foreach (var list in new[] { cluster.Solid.V, cluster.Flat.V })
            {
                foreach (var vertex in list)
                {
                    var world = matrix.MultiplyPoint3x4(vertex);
                    min = Vector2.Min(min, new Vector2(world.x, world.z)); max = Vector2.Max(max, new Vector2(world.x, world.z));
                }
            }
            return Rect.MinMaxRect(min.x - .08f, min.y - .08f, max.x + .08f, max.y + .08f);
        }
        private static Cluster[] BuildFill(LevelDef level, List<Place> places, Dictionary<string, Cluster> clusters)
        {
            var reserved = new List<Rect>();
            foreach (var place in places) { reserved.Add(Footprint(clusters[place.Name], place.Matrix)); }
            float x = level.Cols * .5f, z = level.Rows * .5f;
            foreach (var gate in level.Gates)
            {
                if (gate.Color != TileColor.Water) { continue; }
                bool horizontal = gate.Edge == GateEdge.Top || gate.Edge == GateEdge.Bottom;
                float along = horizontal ? gate.Index - (level.Cols - 1) * .5f : (level.Rows - 1) * .5f - gate.Index;
                float sign = gate.Edge == GateEdge.Top || gate.Edge == GateEdge.Right ? 1 : -1;
                float far = sign * ((horizontal ? z : x) + 2.35f), near = sign * (horizontal ? z : x);
                reserved.Add(horizontal ? Rect.MinMaxRect(along - .75f, Mathf.Min(near, far), along + .75f, Mathf.Max(near, far))
                    : Rect.MinMaxRect(Mathf.Min(near, far), along - .75f, Mathf.Max(near, far), along + .75f));
            }
            float ix = x + StreetInner, ox = ix + StreetWidth, iz = z + StreetInner, oz = iz + StreetWidth;
            var spanX = new[] { new Vector2(-Far, -ox), new Vector2(-ix, ix), new Vector2(ox, Far) };
            var spanZ = new[] { new Vector2(-Far, -oz), new Vector2(-iz, iz), new Vector2(oz, Far) };
            var regions = new[] { new Cluster(), new Cluster(), new Cluster(), new Cluster() }; // bắc, nam, tây, đông
            lowDetail = true;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (i == 1 && j == 1) { continue; }
                        // Tách vùng để camera bỏ được phần ngoài khung (điện thoại dọc không thấy hai bên).
                        var fill = regions[i == 0 ? 2 : i == 2 ? 3 : j == 2 ? 0 : 1];
                        float x0 = Mathf.Max(spanX[i].x + .26f, -FillHalfX), x1 = Mathf.Min(spanX[i].y - .26f, FillHalfX);
                        float z0 = Mathf.Max(spanZ[j].x + .26f, -FillHalfZ), z1 = Mathf.Min(spanZ[j].y - .26f, FillHalfZ);
                        if (x1 - x0 < .6f || z1 - z0 < .6f) { continue; }
                        if (i != 1 && x1 - x0 > 2f)
                        {
                            float inner = i == 0 ? x1 - 1f : x0;
                            for (float at = z0; at + 1f <= z1; at += 1.15f)
                            {
                                var strip = new Rect(inner, at, 1f, 1.1f);
                                if (Free(strip, reserved)) { Greenery(fill, strip, Mathf.Abs(Mathf.RoundToInt(at * 3 + i * 7 + j))); }
                            }
                            if (i == 0) { x1 -= 1.05f; } else { x0 += 1.05f; }
                        }
                        int nx = Mathf.Max(1, Mathf.FloorToInt((x1 - x0) / 2f)), nz = Mathf.Max(1, Mathf.FloorToInt((z1 - z0) / 1.9f));
                        float w = (x1 - x0) / nx, d = (z1 - z0) / nz;
                        for (int a = 0; a < nx; a++)
                        {
                            for (int b = 0; b < nz; b++)
                            {
                                // Đếm từ mép gần bãi ra xa để phần hay nhìn thấy được xếp ngay sát phố.
                                int ca = i == 0 ? nx - 1 - a : a, cb = j == 0 ? nz - 1 - b : b;
                                var cell = new Rect(x0 + ca * w, z0 + cb * d, w, d);
                                int seed = (i * 5 + j * 11 + a * 3 + b * 7) % 97;
                                if (Free(cell, reserved)) { Scene3D(fill, cell, seed, j == 0); continue; }
                                for (int q = 0; q < 4; q++)
                                {
                                    var part = new Rect(cell.x + q % 2 * w * .5f, cell.y + q / 2 * d * .5f, w * .5f, d * .5f);
                                    if (part.width >= .55f && part.height >= .55f && Free(part, reserved)) { Greenery(fill, part, seed + q); }
                                }
                            }
                        }
                    }
                }
            }
            finally { lowDetail = false; }
            return regions;
        }
        private static bool Free(Rect area, List<Rect> reserved)
        {
            foreach (var other in reserved) { if (area.Overlaps(other)) { return false; } }
            return true;
        }
        private static Mb BuildGround(LevelDef level)
        {
            var g = new Mb();
            float x = level.Cols * .5f, z = level.Rows * .5f;
            float ix = x + StreetInner, ox = ix + StreetWidth, iz = z + StreetInner, oz = iz + StreetWidth;
            Cap(g, Shape(0, 0, Far * 2, Far * 2, R(0), 0), 0, C.Road);
            var spanX = new[] { new Vector2(-Far, -ox), new Vector2(-ix, ix), new Vector2(ox, Far) };
            var spanZ = new[] { new Vector2(-Far, -oz), new Vector2(-iz, iz), new Vector2(oz, Far) };
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    float w = spanX[i].y - spanX[i].x, d = spanZ[j].y - spanZ[j].x;
                    float cx = (spanX[i].x + spanX[i].y) * .5f, cz = (spanZ[j].x + spanZ[j].y) * .5f;
                    bool centre = i == 1 && j == 1;
                    Slab(g, cx, cz, w, d, .004f, R(centre ? .46f : .4f), C.Walk, .04f);
                    if (!centre) { Slab(g, cx, cz, w - .4f, d - .4f, .006f, R(.24f), C.Grass, .022f); }
                }
            }
            // Mạch gạch trên vỉa quanh bãi và quanh các ô phố trên/dưới.
            for (float t = -ix + .5f; t < ix - .3f; t += .5f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Slab(g, t, side * (z + iz) * .5f + side * .06f, .025f, StreetInner - .22f, .008f, R(.008f), C.WalkSeam);
                    Slab(g, t, side * (oz + .1f), .022f, .17f, .008f, R(.008f), C.WalkSeam);
                }
            }
            for (float t = -iz + .5f; t < iz - .3f; t += .5f)
            {
                foreach (float side in new[] { -1f, 1f }) { Slab(g, side * (x + ix) * .5f + side * .06f, t, StreetInner - .22f, .025f, .008f, R(.008f), C.WalkSeam); }
            }
            float mx = (ix + ox) * .5f, mz = (iz + oz) * .5f;
            for (float t = -18; t <= 18; t += .82f)
            {
                if (Mathf.Abs(Mathf.Abs(t) - mx) > .75f) { Slab(g, t, mz, .4f, .075f, .002f, R(.035f), C.Dash); Slab(g, t, -mz, .4f, .075f, .002f, R(.035f), C.Dash); }
                if (Mathf.Abs(Mathf.Abs(t) - mz) > .75f) { Slab(g, mx, t, .075f, .4f, .002f, R(.035f), C.Dash); Slab(g, -mx, t, .075f, .4f, .002f, R(.035f), C.Dash); }
            }
            // Cỏ lấm tấm ở các ô phố.
            for (int i = 0; i < 90; i++)
            {
                float tx = Mathf.Repeat(i * 3.71f, 14) - 7, tz = Mathf.Repeat(i * 5.37f, 22) - 11;
                bool nearRoad = Mathf.Abs(Mathf.Abs(tx) - mx) < .85f || Mathf.Abs(Mathf.Abs(tz) - mz) < .85f;
                if (!nearRoad && (Mathf.Abs(tx) > ox || Mathf.Abs(tz) > oz))
                { Slab(g, tx, tz, .1f, .04f, .0065f, R(.02f), C.Tuft); Slab(g, tx + .05f, tz + .045f, .03f, .07f, .0065f, R(.015f), C.Tuft); }
            }
            // Dải màu trước cổng như game đang dùng.
            foreach (var gate in level.Gates)
            {
                if (gate.Color == TileColor.Water) { continue; }
                bool horizontal = gate.Edge == GateEdge.Top || gate.Edge == GateEdge.Bottom;
                float along = horizontal ? gate.Index - (level.Cols - 1) * .5f : (level.Rows - 1) * .5f - gate.Index;
                float sign = gate.Edge == GateEdge.Top || gate.Edge == GateEdge.Right ? 1 : -1;
                float across = sign * ((horizontal ? z : x) + .27f);
                Slab(g, horizontal ? along : across, horizontal ? across : along, horizontal ? .8f : .5f, horizontal ? .5f : .8f,
                    .014f, R(.08f), (C)((int)C.GatePink + (int)gate.Color), .018f);
            }
            return g;
        }

        private struct Place { public string Name; public Matrix4x4 Matrix; }
        private static List<Place> Layout(LevelDef level)
        {
            float x = level.Cols * .5f, z = level.Rows * .5f;
            bool waterTop = false, waterBottom = false;
            foreach (var gate in level.Gates)
            {
                if (gate.Color != TileColor.Water) { continue; }
                waterTop |= gate.Edge == GateEdge.Top; waterBottom |= gate.Edge == GateEdge.Bottom;
            }
            float outer = StreetInner + StreetWidth + .22f;
            float room = level.Cols + StreetInner * 2 - .3f;
            float shopScale = Mathf.Min(1, room / ShopsWidth), parkScale = Mathf.Min(1, room / ParkWidth);
            return new List<Place>
            {
                new Place { Name = "CityShops", Matrix = Matrix4x4.TRS(new Vector3(0, Ground, z + (waterTop ? Mathf.Max(outer, 2.3f) : outer)), Quaternion.identity, Vector3.Scale(new Vector3(1, 1.15f, 1.25f), Vector3.one * shopScale)) },
                new Place { Name = "CityPark", Matrix = Matrix4x4.TRS(new Vector3(0, Ground, -(z + (waterBottom ? Mathf.Max(outer, 2.3f) : outer))), Quaternion.identity, Vector3.Scale(new Vector3(1, 1, 1.18f), Vector3.one * parkScale)) },
                new Place { Name = "CitySideTree", Matrix = Matrix4x4.TRS(new Vector3(-(x + outer + .4f), Ground, level.Rows * .12f), Quaternion.identity, Vector3.one) },
                new Place { Name = "CitySideFlowers", Matrix = Matrix4x4.TRS(new Vector3(x + outer + .35f, Ground, -level.Rows * .18f), Quaternion.identity, Vector3.one) }
            };
        }
    }
}
