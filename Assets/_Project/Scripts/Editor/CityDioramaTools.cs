using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    // 016-e: dựng khu phố hoạt hình 3D theo docs/handoff/016-ref/city-3d-topdown.png và chụp nhiều góc để người làm duyệt.
    // CHƯA nối vào game: chỉ tạo asset (Art/Environment/City3D, Prefabs/Environment/City3D) và ảnh ở Logs/016-city.
    // Ngân sách: nền + đường = 1 mesh không viền (1 draw); mỗi cụm vật = 1 mesh có viền (2 draw); tổng 9 draw.
    public static class CityDioramaTools
    {
        private const string Art = "Assets/_Project/Art/Environment/City3D";
        private const string Prefabs = "Assets/_Project/Prefabs/Environment/City3D";
        private const string Shots = "Logs/016-city";
        private const string PreviewScene = "Assets/_Project/Scenes/CityPreview.unity";
        private const string Concept = "docs/handoff/016-ref/city-3d-topdown.png";

        // Trùng UiTheme đang dùng để tim đường xe chạy không đổi.
        private const float StreetInner = .8f, StreetWidth = .95f, StreetVisible = .5f;
        private const float Ground = LotSurfaceView.Top - .08f, Far = 24;
        private const float ShopsWidth = 6.2f, ParkWidth = 5.2f;

        private enum C
        {
            Grass, GrassShade, Tuft, Road, Dash, Line, Walk, WalkSeam, WalkShade, Sand,
            Teal, TealWall, TealDark, Coral, CoralWall, CoralDark, Yellow, YellowWall, YellowDark, White,
            Wood, WoodDark, Glass, Gray, GrayDark, Pot, Leaf, LeafLight, LeafDark, Pink, Flower, Soil,
            GatePink, GateBlue, GateYellow, GateGreen, GateOrange, GatePurple,
            Lilac, LilacWall, Sky, SkyWall, Stone
        }
        private static readonly string[] Hex =
        {
            "A3C75F", "8DB04E", "7FA845", "737C92", "F6ECD6", "2E2F3A", "EDD9C8", "D6BCA8", "D9C2AF", "F4D398",
            "52C7C0", "8FDCD3", "3BAAA6", "FF8F6C", "FFB896", "E9754F", "FFCB55", "FFE08F", "EFAE35", "FFF8EC",
            "D98A4B", "A9622F", "A6DDF2", "C9C4CC", "8E8A99", "D9824F", "6FB83A", "93D14A", "4E9A33", "FF7FA4", "FFE066", "8A5A3C",
            "E84A8C", "2F8CF0", "FFD23F", "35B45A", "FF8A26", "9B4FE0",
            "B795E8", "D5C0F5", "6FB4F2", "B4D9FA", "CFC6BE"
        };

        // Bộ gom đỉnh: mọi tam giác dùng một ô màu của palette nên cả cụm chỉ cần một material.
        private sealed class Mb
        {
            public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
            public readonly List<Vector2> U = new List<Vector2>();
            public readonly List<int> T = new List<int>();
            public int Add(Vector3 p, Vector3 n, C color)
            {
                V.Add(p); N.Add(n.normalized); U.Add(new Vector2(((int)color + .5f) / Hex.Length, .5f));
                return V.Count - 1;
            }
            public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }
            public void Quad(int a, int b, int c, int d) { Tri(a, b, c); Tri(c, d, a); }
            public void Append(Mb other, Matrix4x4 m)
            {
                int first = V.Count;
                for (int i = 0; i < other.V.Count; i++) { V.Add(m.MultiplyPoint3x4(other.V[i])); N.Add(m.MultiplyVector(other.N[i]).normalized); U.Add(other.U[i]); }
                foreach (int index in other.T) { T.Add(first + index); }
            }
            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt16 };
                if (V.Count > 65000) { mesh.indexFormat = IndexFormat.UInt32; }
                mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetUVs(0, U); mesh.SetTriangles(T, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // Một cụm: phần khối (có viền) và phần dán đất (bóng, lối đi; gộp vào mesh nền).
        private sealed class Cluster { public readonly Mb Solid = new Mb(), Flat = new Mb(); }

        private struct Rim { public Vector2 P, N; }
        private static bool lowDetail; // canh nen xa: it dinh hon de giu ngan sach tam giac

        // Chu vi chữ nhật bo góc (thứ tự góc: trên-trái, trên-phải, dưới-phải, dưới-trái), theo chiều kim đồng hồ nhìn từ trên.
        private static Rim[] Shape(float cx, float cz, float w, float d, Vector4 radius, float inset)
        {
            int Seg = lowDetail ? 2 : 6;
            var list = new Rim[4 * (Seg + 1)];
            float hx = Mathf.Max(.001f, w * .5f - inset), hz = Mathf.Max(.001f, d * .5f - inset);
            for (int corner = 0; corner < 4; corner++)
            {
                float r = Mathf.Clamp(radius[corner] - inset, .001f, Mathf.Min(hx, hz));
                float sx = corner == 1 || corner == 2 ? 1 : -1, sz = corner < 2 ? 1 : -1;
                var centre = new Vector2(cx + sx * (hx - r), cz + sz * (hz - r));
                for (int s = 0; s <= Seg; s++)
                {
                    float angle = (180 - corner * 90 - 90f * s / Seg) * Mathf.Deg2Rad;
                    var n = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    list[corner * (Seg + 1) + s] = new Rim { P = centre + n * r, N = n };
                }
            }
            return list;
        }
        private static Vector4 R(float r) => new Vector4(r, r, r, r);

        private static void Cap(Mb mb, Rim[] ring, float y, C color)
        {
            var mid = Vector2.zero;
            foreach (var p in ring) { mid += p.P; }
            mid /= ring.Length;
            int centre = mb.Add(new Vector3(mid.x, y, mid.y), Vector3.up, color), first = mb.V.Count;
            foreach (var p in ring) { mb.Add(new Vector3(p.P.x, y, p.P.y), Vector3.up, color); }
            for (int i = 0; i < ring.Length; i++) { mb.Tri(centre, first + i, first + (i + 1) % ring.Length); }
        }
        private static void Band(Mb mb, Rim[] low, float y0, float up0, Rim[] high, float y1, float up1, C color)
        {
            int n = low.Length, first = mb.V.Count;
            for (int i = 0; i < n; i++)
            {
                mb.Add(new Vector3(low[i].P.x, y0, low[i].P.y), new Vector3(low[i].N.x * (1 - up0), up0, low[i].N.y * (1 - up0)), color);
                mb.Add(new Vector3(high[i].P.x, y1, high[i].P.y), new Vector3(high[i].N.x * (1 - up1), up1, high[i].N.y * (1 - up1)), color);
            }
            for (int i = 0; i < n; i++)
            {
                int a = first + i * 2, b = first + (i + 1) % n * 2;
                mb.Quad(a, b, b + 1, a + 1);
            }
        }
        // Khối đùn thẳng đứng, mép trên vát bo; pháp tuyến liền nên viền shader không bị hở.
        private static void Block(Mb mb, float cx, float cz, float w, float d, float y0, float y1, Vector4 radius, float bevel, C side, C top)
        {
            bevel = Mathf.Min(bevel, (y1 - y0) * .9f, Mathf.Min(w, d) * .4f);
            var a = Shape(cx, cz, w, d, radius, 0);
            if (lowDetail)
            {
                // Cảnh xa: một dải vát duy nhất là đủ đọc ra khối bo.
                var inner = Shape(cx, cz, w, d, radius, bevel);
                Band(mb, a, y0, 0, a, y1 - bevel, 0, side);
                Band(mb, a, y1 - bevel, 0, inner, y1, 1, top);
                Cap(mb, inner, y1, top);
                return;
            }
            var b = Shape(cx, cz, w, d, radius, bevel * .3f);
            var c = Shape(cx, cz, w, d, radius, bevel);
            Band(mb, a, y0, 0, a, y1 - bevel, 0, side);
            Band(mb, a, y1 - bevel, 0, b, y1 - bevel * .3f, .5f, side);
            Band(mb, b, y1 - bevel * .3f, .5f, c, y1, 1, top);
            Cap(mb, c, y1, top);
        }
        private static void Box(Mb mb, float cx, float y0, float cz, float w, float h, float d, C color, float r = .03f)
            => Block(mb, cx, cz, w, d, y0, y0 + h, R(r), r, color, color);
        private static void Slab(Mb mb, float cx, float cz, float w, float d, float y, Vector4 radius, C color, float line = 0)
        {
            if (line > 0) { Cap(mb, Shape(cx, cz, w + line * 2, d + line * 2, radius + R(line), 0), y - .001f, C.Line); }
            Cap(mb, Shape(cx, cz, w, d, radius, 0), y, color);
        }
        private static void Ball(Mb mb, Vector3 centre, Vector3 radii, C color, int lat = 7, int lon = 12)
        {
            if (lowDetail) { lat = Mathf.Min(lat, 4); lon = Mathf.Min(lon, 6); }
            int first = mb.V.Count;
            for (int i = 0; i <= lat; i++)
            {
                float v = Mathf.PI * i / lat - Mathf.PI * .5f;
                for (int j = 0; j <= lon; j++)
                {
                    float u = Mathf.PI * 2 * j / lon;
                    var n = new Vector3(Mathf.Cos(v) * Mathf.Cos(u), Mathf.Sin(v), Mathf.Cos(v) * Mathf.Sin(u));
                    mb.Add(centre + Vector3.Scale(n, radii), new Vector3(n.x / radii.x, n.y / radii.y, n.z / radii.z), color);
                }
            }
            for (int i = 0; i < lat; i++)
            {
                for (int j = 0; j < lon; j++)
                {
                    int a = first + i * (lon + 1) + j, b = a + lon + 1;
                    mb.Quad(a, b, b + 1, a + 1);
                }
            }
        }
        private static void Face4(Mb mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, C color)
        {
            var n = Vector3.Cross(b - a, c - a);
            mb.Quad(mb.Add(a, n, color), mb.Add(b, n, color), mb.Add(c, n, color), mb.Add(d, n, color));
        }
        // Mái hiên sọc dốc về phía trước (−Z) có diềm vỏ sò.
        private static void Awning(Mb mb, float x0, float x1, float zBack, float yBack, float zFront, float yFront, C a, C b, int stripes)
        {
            float step = (x1 - x0) / stripes, t = .045f;
            for (int i = 0; i < stripes; i++)
            {
                float l = x0 + i * step, r = l + step;
                var color = i % 2 == 0 ? a : b;
                var bl = new Vector3(l, yBack, zBack); var br = new Vector3(r, yBack, zBack);
                var fl = new Vector3(l, yFront, zFront); var fr = new Vector3(r, yFront, zFront);
                var down = Vector3.up * t;
                Face4(mb, fl, bl, br, fr, color);
                Face4(mb, fl - down, fl, fr, fr - down, color);
                if (i == 0) { Face4(mb, bl - down, bl, fl, fl - down, color); }
                if (i == stripes - 1) { Face4(mb, fr - down, fr, br, br - down, color); }
                Ball(mb, new Vector3((l + r) * .5f, yFront - t, zFront), new Vector3(step * .5f, .06f, .03f), color, 5, 8);
            }
        }
        private static void Shadow(Mb flat, float cx, float cz, float w, float d, float radius, C color)
            => Slab(flat, cx + .12f, cz - .1f, w, d, .010f, R(radius), color);

        private static void Bush(Mb mb, float x, float y, float z, float s, C color)
        {
            Ball(mb, new Vector3(x, y + s * .55f, z), new Vector3(s, s * .75f, s * .85f), color);
            Ball(mb, new Vector3(x - s * .7f, y + s * .4f, z + s * .1f), Vector3.one * s * .6f, color);
            Ball(mb, new Vector3(x + s * .7f, y + s * .4f, z - s * .05f), Vector3.one * s * .65f, C.LeafLight);
        }
        private static void Blooms(Mb mb, float x, float y, float z, float spread, C color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? i / (count - 1f) - .5f : 0;
                var at = new Vector3(x + t * spread, y + (i % 2) * .025f, z + (i % 3 - 1) * .035f);
                Ball(mb, at, new Vector3(.08f, .055f, .075f), color, 5, 10);
                Ball(mb, at + new Vector3(0, .052f, -.01f), new Vector3(.028f, .018f, .028f), C.Flower, 3, 8);
            }
        }
        private static void Planter(Cluster c, float x, float z, float w, C bloom, C shade)
        {
            Shadow(c.Flat, x, z, w, .26f, .1f, shade);
            Box(c.Solid, x, 0, z, w, .14f, .25f, C.Pot, .07f);
            Bush(c.Solid, x, .1f, z + .02f, Mathf.Min(.15f, w * .28f), C.LeafLight);
            Blooms(c.Solid, x, .22f, z - .08f, w * .62f, bloom, w > .45f ? 3 : 2);
        }
        private static void Tree(Cluster c, float x, float z, float s, C shade, float tall = 0)
        {
            float v = tall > 0 ? tall : s; // v: cỡ theo chiều cao, tách khỏi bề ngang để cây dải dưới to mà không cao
            Slab(c.Flat, x + .18f * s, z - .14f * s, .95f * s, .75f * s, .010f, R(.37f * s), shade);
            Slab(c.Flat, x, z, .46f * s, .38f * s, .012f, R(.19f * s), C.Soil, .02f);
            Block(c.Solid, x, z, .17f * s, .17f * s, 0, .45f * v, R(.085f * s), .03f, C.Wood, C.Wood);
            Ball(c.Solid, new Vector3(x, .68f * v, z), new Vector3(.4f * s, .33f * v, .38f * s), C.Leaf, 9, 16);
            var puffs = new[]
            {
                new Vector4(-.28f, .64f, -.12f, .22f), new Vector4(.29f, .66f, -.08f, .22f), new Vector4(-.18f, .7f, .23f, .21f),
                new Vector4(.2f, .72f, .21f, .2f), new Vector4(0, .62f, -.27f, .2f)
            };
            foreach (var p in puffs) { Ball(c.Solid, new Vector3(x + p.x * s, p.y * v, z + p.z * s), new Vector3(p.w * s, p.w * v, p.w * s), C.Leaf); }
            Ball(c.Solid, new Vector3(x - .08f * s, .93f * v, z + .02f * s), new Vector3(.22f * s, .13f * v, .2f * s), C.LeafLight);
            Ball(c.Solid, new Vector3(x - .27f * s, .82f * v, z - .1f * s), new Vector3(.13f * s, .09f * v, .12f * s), C.LeafLight);
        }
        private static void Table(Cluster c, float x, float z)
        {
            Shadow(c.Flat, x, z, .3f, .24f, .12f, C.WalkShade);
            Block(c.Solid, x, z, .07f, .07f, 0, .19f, R(.035f), .01f, C.WoodDark, C.WoodDark);
            Block(c.Solid, x, z, .34f, .34f, .19f, .25f, R(.17f), .025f, C.White, C.White);
            foreach (float side in new[] { -1f, 1f })
            {
                Box(c.Solid, x + side * .26f, 0, z, .15f, .13f, .17f, C.CoralDark, .06f);
                Box(c.Solid, x + side * .34f, .06f, z, .05f, .23f, .17f, C.CoralDark, .025f);
            }
        }
        private static void Window(Mb mb, float x, float z, float w, float y0, float h)
        {
            Box(mb, x, y0 - .025f, z, w + .06f, h + .05f, .045f, C.White, .018f);
            Box(mb, x, y0, z - .018f, w, h, .03f, C.Glass, .012f);
            Box(mb, x, y0, z - .036f, .022f, h, .012f, C.White, .005f);
        }
        private static void RoofKit(Mb mb, float x, float y, float z)
        {
            Box(mb, x, y, z, .3f, .12f, .25f, C.Gray, .04f);
            Block(mb, x, z, .17f, .17f, y + .12f, y + .135f, R(.085f), .006f, C.GrayDark, C.GrayDark);
        }

        private static Cluster Shops()
        {
            var c = new Cluster();
            Slab(c.Flat, -2.05f, .8f, 2.04f, 1.6f, .0085f, R(.28f), C.Walk, .035f);
            Slab(c.Flat, 0, .85f, 1.5f, 1.5f, .0085f, R(.28f), C.Walk, .035f);
            Slab(c.Flat, 2.08f, .8f, 2.04f, 1.6f, .0085f, R(.28f), C.Walk, .035f);

            // Quán xanh ngọc.
            Shadow(c.Flat, -2.05f, 1.07f, 1.98f, .94f, .18f, C.WalkShade);
            Block(c.Solid, -2.05f, 1.07f, 1.84f, .82f, 0, .56f, R(.12f), .05f, C.TealWall, C.TealWall);
            Block(c.Solid, -2.05f, 1.08f, 1.98f, .94f, .56f, .73f, R(.18f), .09f, C.Teal, C.Teal);
            Slab(c.Solid, -2.05f, 1.1f, 1.7f, .66f, .732f, R(.1f), C.TealDark);
            RoofKit(c.Solid, -2.55f, .73f, 1.18f);
            Box(c.Solid, -2.05f, 0, .65f, .3f, .38f, .05f, C.WoodDark, .02f);
            Window(c.Solid, -2.6f, .65f, .4f, .13f, .22f);
            Window(c.Solid, -1.5f, .65f, .4f, .13f, .22f);
            Awning(c.Solid, -2.93f, -1.17f, .66f, .53f, .32f, .37f, C.TealDark, C.White, 7);
            Table(c, -2.6f, .18f);
            Table(c, -1.82f, .18f);
            Planter(c, -1.24f, .18f, .36f, C.Pink, C.WalkShade);

            // Tiệm màu đào.
            Shadow(c.Flat, 0, 1.1f, 1.4f, .86f, .16f, C.WalkShade);
            Block(c.Solid, 0, 1.1f, 1.26f, .74f, 0, .5f, R(.1f), .05f, C.CoralWall, C.CoralWall);
            Block(c.Solid, 0, 1.11f, 1.4f, .86f, .5f, .66f, R(.16f), .08f, C.Coral, C.Coral);
            Slab(c.Solid, 0, 1.13f, 1.14f, .6f, .662f, R(.09f), C.CoralDark);
            RoofKit(c.Solid, .33f, .66f, 1.18f);
            Box(c.Solid, -.28f, 0, .72f, .28f, .35f, .05f, C.WoodDark, .02f);
            Window(c.Solid, .22f, .72f, .46f, .12f, .21f);
            Awning(c.Solid, -.65f, .65f, .73f, .48f, .39f, .33f, C.CoralDark, C.White, 5);
            Planter(c, .22f, .22f, .55f, C.Pink, C.WalkShade);

            // Nhà góc phố vàng bơ, góc trước phải bo tròn có cửa kính cong.
            var wall = new Vector4(.1f, .1f, .5f, .1f);
            Block(c.Flat, 2.2f, .95f, 1.9f, .95f, .009f, .01f, wall, 0, C.WalkShade, C.WalkShade);
            Block(c.Solid, 2.05f, 1.06f, 1.86f, .92f, 0, .6f, wall, .05f, C.YellowWall, C.YellowWall);
            Block(c.Solid, 2.07f, 1.05f, 2.02f, 1.06f, .6f, .78f, new Vector4(.18f, .18f, .58f, .18f), .09f, C.Yellow, C.Yellow);
            Slab(c.Solid, 2.07f, 1.07f, 1.74f, .78f, .782f, new Vector4(.1f, .1f, .45f, .1f), C.YellowDark);
            RoofKit(c.Solid, 1.45f, .78f, 1.22f);
            Box(c.Solid, 1.4f, 0, .59f, .28f, .38f, .05f, C.Teal, .02f);
            Window(c.Solid, 1.95f, .59f, .42f, .13f, .23f);
            var arc = new Vector2(2.05f + .93f - .5f, 1.06f - .46f + .5f);
            foreach (float degrees in new[] { -68f, -45f, -22f })
            {
                var pane = new Mb();
                Window(pane, 0, 0, .15f, .13f, .26f);
                var dir = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad));
                c.Solid.Append(pane, Matrix4x4.TRS(new Vector3(arc.x, 0, arc.y) + dir * .505f, Quaternion.LookRotation(-dir), Vector3.one));
            }
            Awning(c.Solid, 1.16f, 2.36f, .6f, .55f, .27f, .38f, C.YellowDark, C.White, 5);
            Planter(c, 1.5f, .14f, .5f, C.Pink, C.WalkShade);
            Bush(c.Solid, 2.95f, 0, .28f, .12f, C.LeafLight);
            Box(c.Solid, 2.95f, 0, .28f, .24f, .11f, .22f, C.Pot, .07f);
            // Ba nhà cao thấp khác nhau cho ra khối: quán vừa, tiệm giữa thấp, nhà góc cao nhất.
            for (int i = 0; i < c.Solid.V.Count; i++)
            {
                var vertex = c.Solid.V[i];
                float tall = vertex.x < -.9f ? 1.3f : vertex.x > .9f ? 1.65f : 1.05f;
                c.Solid.V[i] = new Vector3(vertex.x, vertex.y * tall, vertex.z);
            }
            return c;
        }

        private static Cluster Park()
        {
            var c = new Cluster();
            const float Top = .15f, Depth = 2.2f, Mid = -1.1f;
            // Nền công viên nâng hẳn lên có bậc vỉa; mọi vật đặt trên mặt nền này.
            Shadow(c.Flat, 0, Mid, ParkWidth, Depth, .85f, C.GrassShade);
            Block(c.Solid, 0, Mid, ParkWidth, Depth, 0, Top - .01f, R(.85f), .05f, C.Walk, C.Walk);
            Slab(c.Solid, 0, Mid, ParkWidth - .22f, Depth - .22f, Top, R(.74f), C.Grass, .022f);
            var up = new Cluster();
            Walkway(up.Flat, new Vector2(-2.4f, -1.25f), new Vector2(-1.4f, -.7f), new Vector2(-1f, -1.9f), new Vector2(-.45f, -1.2f), .24f);
            Walkway(up.Flat, new Vector2(.45f, -1.2f), new Vector2(1f, -.55f), new Vector2(1.5f, -1.8f), new Vector2(2.4f, -1.05f), .24f);
            Slab(up.Flat, 0, -1.15f, 1.3f, 1.1f, .0108f, R(.55f), C.Sand, .025f);
            for (int i = 0; i < 18; i++)
            { Slab(up.Flat, -2.2f + i * .26f, i % 2 == 0 ? -.32f - (i % 3) * .07f : -1.75f + (i % 4) * .05f, .09f, .04f, .0125f, R(.02f), C.Tuft); }
            Fountain(up, 0, -1.15f);
            // Bồn hoa hai tầng sát phố.
            Shadow(up.Flat, 0, -.34f, 1.6f, .36f, .18f, C.GrassShade);
            Block(up.Solid, 0, -.34f, 1.6f, .36f, 0, .11f, R(.18f), .035f, C.Walk, C.Walk);
            Block(up.Solid, 0, -.3f, .85f, .24f, .11f, .23f, R(.12f), .03f, C.Stone, C.Stone);
            for (int i = 0; i < 5; i++) { Bush(up.Solid, -.62f + i * .31f, i > 0 && i < 4 ? .2f : .09f, -.33f, .1f, i % 2 == 0 ? C.LeafLight : C.Leaf); }
            for (int i = 0; i < 4; i++) { Blooms(up.Solid, -.46f + i * .31f, i == 1 || i == 2 ? .39f : .28f, -.42f, .02f, i % 2 == 0 ? C.White : C.Pink, 1); }
            Mound(up, -.95f, -.58f, .42f, .17f);
            Mound(up, 1.05f, -1.75f, .5f, .2f);
            Blooms(up.Solid, -.95f, .17f, -.62f, .36f, C.Pink, 3);
            Blooms(up.Solid, 1.05f, .2f, -1.8f, .4f, C.White, 3);
            Tree(up, -1.82f, -.66f, 1.4f, C.GrassShade, 1f);
            Tree(up, 1.86f, -.6f, 1.15f, C.GrassShade, .82f);
            Pine(up, -2.12f, -1.55f, .8f);
            Pine(up, -1.7f, -1.72f, .56f);
            Pine(up, 2.18f, -1.5f, .92f);
            Lamp(up, -.98f, -1.62f);
            Lamp(up, 1.12f, -.4f);
            foreach (float side in new[] { -1f, 1f })
            {
                var bench = new Cluster();
                Bench(bench, 0, 0);
                var at = Matrix4x4.TRS(new Vector3(side * 1f, 0, -1.2f), Quaternion.Euler(0, side * 65, 0), new Vector3(1.05f, 1.15f, 1.15f));
                up.Solid.Append(bench.Solid, at); up.Flat.Append(bench.Flat, Matrix4x4.TRS(new Vector3(side * 1f, 0, -1.2f), Quaternion.Euler(0, side * 65, 0), Vector3.one));
            }
            for (int i = 0; i < 9; i++)
            {
                float x = -1.28f + i * .32f;
                Block(up.Solid, x, -1.96f, .29f, .15f, 0, .13f + (i % 2) * .04f, R(.07f), .05f, C.LeafDark, C.Leaf);
            }
            var lift = Matrix4x4.Translate(new Vector3(0, Top, 0));
            c.Solid.Append(up.Solid, lift);
            c.Flat.Append(up.Flat, lift);
            return c;
        }
        private static void Fountain(Cluster c, float x, float z)
        {
            Shadow(c.Flat, x, z, .92f, .8f, .4f, C.WalkShade);
            Block(c.Solid, x, z, .92f, .82f, 0, .17f, R(.41f), .05f, C.Stone, C.White);
            Slab(c.Solid, x, z, .72f, .62f, .172f, R(.31f), C.Glass, .02f);
            Block(c.Solid, x, z, .15f, .15f, .17f, .44f, R(.075f), .02f, C.Stone, C.Stone);
            Block(c.Solid, x, z, .46f, .42f, .42f, .52f, R(.21f), .04f, C.Stone, C.White);
            Slab(c.Solid, x, z, .32f, .28f, .522f, R(.14f), C.Glass);
            Block(c.Solid, x, z, .08f, .08f, .52f, .68f, R(.04f), .015f, C.Stone, C.Stone);
            Ball(c.Solid, new Vector3(x, .74f, z), new Vector3(.09f, .12f, .09f), C.Glass);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Ball(c.Solid, new Vector3(x + Mathf.Cos(angle) * .27f, .4f, z + Mathf.Sin(angle) * .25f), new Vector3(.035f, .065f, .035f), C.Glass, 5, 8);
            }
        }
        private static void Mound(Cluster c, float x, float z, float radius, float height)
        {
            Ball(c.Solid, new Vector3(x, 0, z), new Vector3(radius, height, radius * .72f), C.LeafLight);
            Bush(c.Solid, x + radius * .55f, 0, z + radius * .2f, .09f, C.Leaf);
        }
        private static void ConeShape(Mb mb, float x, float y, float z, float radius, float height, C color)
        {
            int sides = lowDetail ? 7 : 16, first = mb.V.Count;
            for (int i = 0; i <= sides; i++)
            {
                float angle = Mathf.PI * 2 * i / sides;
                var outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var normal = outward * height + Vector3.up * radius;
                mb.Add(new Vector3(x, y, z) + outward * radius, normal, color);
                mb.Add(new Vector3(x, y + height, z), normal, color);
            }
            for (int i = 0; i < sides; i++) { int k = first + i * 2; mb.Quad(k, k + 1, k + 3, k + 2); }
        }
        private static void Pine(Cluster c, float x, float z, float height)
        {
            Shadow(c.Flat, x, z, height * .6f, height * .45f, height * .22f, C.GrassShade);
            Block(c.Solid, x, z, height * .13f, height * .13f, 0, height * .25f, R(height * .065f), .015f, C.WoodDark, C.WoodDark);
            ConeShape(c.Solid, x, height * .18f, z, height * .34f, height * .42f, C.LeafDark);
            ConeShape(c.Solid, x, height * .42f, z, height * .27f, height * .37f, C.Leaf);
            ConeShape(c.Solid, x, height * .66f, z, height * .19f, height * .34f, C.LeafLight);
        }
        private static void Lamp(Cluster c, float x, float z)
        {
            Shadow(c.Flat, x, z, .2f, .14f, .07f, C.GrassShade);
            Block(c.Solid, x, z, .13f, .13f, 0, .07f, R(.065f), .02f, C.GrayDark, C.GrayDark);
            Block(c.Solid, x, z, .05f, .05f, .07f, .68f, R(.025f), .008f, C.GrayDark, C.GrayDark);
            Ball(c.Solid, new Vector3(x, .76f, z), Vector3.one * .1f, C.Flower);
            Block(c.Solid, x, z, .22f, .22f, .83f, .88f, R(.11f), .025f, C.GrayDark, C.GrayDark);
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
        private static Cluster BuildFill(LevelDef level, List<Place> places, Dictionary<string, Cluster> clusters)
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
            var fill = new Cluster();
            lowDetail = true;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (i == 1 && j == 1) { continue; }
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
            return fill;
        }
        private static bool Free(Rect area, List<Rect> reserved)
        {
            foreach (var other in reserved) { if (area.Overlaps(other)) { return false; } }
            return true;
        }
        private static void Greenery(Cluster c, Rect area, int seed)
        {
            var at = area.center + new Vector2((seed % 5 - 2) * .06f, (seed % 7 - 3) * .045f);
            float s = Mathf.Min(area.width, area.height) * (.85f + seed % 4 * .07f);
            if (seed % 3 == 0) { Tree(c, at.x, at.y, Mathf.Min(.85f, s * .8f), C.GrassShade); }
            else if (seed % 3 == 1) { Pine(c, at.x, at.y, Mathf.Min(.85f, s * .85f)); }
            else
            {
                Shadow(c.Flat, at.x, at.y, s * .6f, s * .4f, s * .18f, C.GrassShade);
                Bush(c.Solid, at.x, 0, at.y, s * .22f, C.Leaf);
                Blooms(c.Solid, at.x, s * .3f, at.y - s * .18f, s * .3f, seed % 2 == 0 ? C.Pink : C.White, 2);
            }
        }
        private static void Scene3D(Cluster target, Rect cell, int seed, bool south)
        {
            var item = new Cluster();
            int kind = seed % 8;
            if (kind <= 2) { House(item, seed, south ? 0 : kind); }
            else if (kind == 3)
            {
                Tree(item, -.4f, .12f, 1.05f, C.GrassShade); Tree(item, .46f, -.28f, .7f, C.GrassShade); Pine(item, .5f, .38f, .75f);
                Bush(item.Solid, -.55f, 0, -.45f, .13f, C.LeafLight);
            }
            else if (kind == 4)
            {
                Pine(item, -.45f, .2f, 1f); Pine(item, .08f, -.12f, .72f); Pine(item, .52f, .22f, .86f);
                Bush(item.Solid, .4f, 0, -.45f, .12f, C.Leaf); Blooms(item.Solid, -.35f, .05f, -.45f, .4f, C.Pink, 3);
            }
            else if (kind == 5)
            {
                Slab(item.Flat, 0, 0, 1.5f, 1.1f, .0085f, R(.5f), C.Walk, .03f);
                Fountain(item, 0, .05f);
                Bush(item.Solid, -.68f, 0, .4f, .12f, C.LeafLight); Bush(item.Solid, .66f, 0, -.36f, .13f, C.Leaf);
            }
            else if (kind == 6)
            {
                Slab(item.Flat, 0, 0, 1.6f, 1.2f, .0085f, R(.25f), C.Walk, .03f);
                Parasol(item, -.4f, .1f, Roofs[seed % Roofs.Length]); Parasol(item, .42f, -.14f, Roofs[(seed + 2) % Roofs.Length]);
                Planter(item, .05f, .44f, .6f, C.Pink, C.WalkShade);
            }
            else
            {
                for (int row = 0; row < 2; row++)
                {
                    float z = -.3f + row * .56f;
                    Block(item.Solid, 0, z, 1.35f, .28f, 0, .1f + row * .07f, R(.14f), .035f, C.Pot, C.Soil);
                    for (int i = 0; i < 4; i++)
                    {
                        Bush(item.Solid, -.48f + i * .32f, .06f + row * .07f, z, .1f, C.LeafLight);
                        Blooms(item.Solid, -.48f + i * .32f, .24f + row * .07f, z - .08f, .02f, (i + row) % 2 == 0 ? C.Pink : C.White, 1);
                    }
                }
            }
            float scale = Mathf.Min(1.12f, cell.width / 1.9f, cell.height / 1.75f);
            var shift = new Vector3(cell.center.x + (seed % 5 - 2) * .04f, 0, cell.center.y + (seed % 3 - 1) * .05f);
            // Dải phía nam nằm trước camera: ép thấp để không che phố và bãi.
            var matrix = Matrix4x4.TRS(shift, Quaternion.identity, new Vector3(scale, south ? Mathf.Min(.85f, scale) : scale, scale));
            target.Solid.Append(item.Solid, matrix);
            target.Flat.Append(item.Flat, Matrix4x4.TRS(shift, Quaternion.identity, new Vector3(scale, 1, scale)));
        }
        private static void House(Cluster c, int seed, int storeys)
        {
            C roof = Roofs[seed % Roofs.Length], wall = Walls[seed % Walls.Length];
            float h = .46f + storeys * .24f;
            Slab(c.Flat, 0, -.05f, 1.66f, 1.36f, .0085f, R(.26f), C.Walk, .03f);
            Shadow(c.Flat, 0, .08f, 1.42f, .96f, .18f, C.WalkShade);
            Block(c.Solid, 0, .08f, 1.28f, .78f, 0, h, R(.1f), .04f, wall, wall);
            Block(c.Solid, 0, .09f, 1.44f, .94f, h, h + .17f, R(.18f), .085f, roof, roof);
            Slab(c.Solid, 0, .11f, 1.16f, .66f, h + .172f, R(.1f), wall);
            if (seed % 2 == 0) { RoofKit(c.Solid, -.3f, h + .17f, .18f); }
            else
            {
                Block(c.Solid, .1f, .12f, .8f, .5f, h + .17f, h + .34f, R(.2f), .15f, roof, roof);
                Box(c.Solid, .38f, h + .2f, .22f, .14f, .26f, .14f, C.Stone, .04f);
            }
            Box(c.Solid, -.3f, 0, -.31f, .24f, .32f, .05f, C.WoodDark, .02f);
            Window(c.Solid, .26f, -.31f, .38f, .11f, .2f);
            if (storeys > 0) { Window(c.Solid, -.28f, -.31f, .26f, .46f, .16f); Window(c.Solid, .26f, -.31f, .26f, .46f, .16f); }
            if (seed % 3 == 0) { Awning(c.Solid, .0f, .54f, -.3f, .43f, -.52f, .34f, roof, C.White, 3); }
            Bush(c.Solid, -.68f, 0, -.48f, .11f, C.LeafLight);
            Bush(c.Solid, .66f, 0, -.52f, .09f, C.Leaf);
        }
        private static void Parasol(Cluster c, float x, float z, C color)
        {
            Table(c, x, z);
            Block(c.Solid, x, z, .045f, .045f, .25f, .6f, R(.022f), .008f, C.White, C.White);
            ConeShape(c.Solid, x, .55f, z, .36f, .2f, color);
            Ball(c.Solid, new Vector3(x, .76f, z), Vector3.one * .04f, C.White, 4, 8);
        }
        private static void Bench(Cluster c, float x, float z)
        {
            Shadow(c.Flat, x, z, .66f, .3f, .08f, C.GrassShade);
            foreach (float side in new[] { -.25f, .25f }) { Box(c.Solid, x + side, 0, z, .05f, .13f, .24f, C.GrayDark, .015f); }
            for (int i = 0; i < 3; i++) { Box(c.Solid, x, .13f, z - .08f + i * .085f, .66f, .035f, .07f, C.Wood, .015f); }
            Box(c.Solid, x, .21f, z - .13f, .66f, .07f, .04f, C.Wood, .015f);
            Box(c.Solid, x, .31f, z - .14f, .66f, .06f, .035f, C.WoodDark, .012f);
        }
        private static void Walkway(Mb mb, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float width)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float half = (pass == 0 ? width + .05f : width) * .5f, y = pass == 0 ? .0105f : .0115f;
                var color = pass == 0 ? C.WalkSeam : C.Sand;
                int first = mb.V.Count;
                const int Steps = 28;
                for (int i = 0; i <= Steps; i++)
                {
                    float t = i / (float)Steps, u = 1 - t;
                    var p = u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
                    var dir = (3 * u * u * (b - a) + 6 * u * t * (c - b) + 3 * t * t * (d - c)).normalized;
                    var side = new Vector2(-dir.y, dir.x) * half;
                    mb.Add(new Vector3(p.x + side.x, y, p.y + side.y), Vector3.up, color);
                    mb.Add(new Vector3(p.x - side.x, y, p.y - side.y), Vector3.up, color);
                }
                for (int i = 0; i < Steps; i++) { int k = first + i * 2; mb.Quad(k, k + 2, k + 3, k + 1); }
            }
        }

        private static Cluster SideTree()
        {
            var c = new Cluster();
            Slab(c.Flat, 0, 0, .84f, 1.2f, .0085f, R(.22f), C.Walk, .035f);
            Shadow(c.Flat, 0, -.3f, .5f, .34f, .1f, C.WalkShade);
            Box(c.Solid, 0, 0, -.3f, .5f, .16f, .34f, C.Pot, .09f);
            Tree(c, 0, .12f, .72f, C.WalkShade);
            Blooms(c.Solid, 0, .2f, -.32f, .32f, C.Pink, 4);
            return c;
        }
        private static Cluster SideFlowers()
        {
            var c = new Cluster();
            Slab(c.Flat, 0, 0, .74f, 1.2f, .0085f, R(.22f), C.Walk, .035f);
            Shadow(c.Flat, 0, 0, .42f, .92f, .14f, C.WalkShade);
            Box(c.Solid, 0, 0, 0, .42f, .17f, .92f, C.Pot, .14f);
            for (int i = 0; i < 4; i++)
            {
                float z = -.3f + i * .2f;
                Bush(c.Solid, 0, .13f, z, .1f, C.LeafLight);
                Blooms(c.Solid, 0, .29f, z, .2f, i % 2 == 0 ? C.Pink : C.White, 3);
            }
            return c;
        }

        // Nền theo kích thước màn: đường là lớp đáy, 9 ô phố bo góc đặt lên trên nên giao lộ tự bo, đường chạy ra khỏi khung.
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

        [MenuItem("Tools/Squeaky Lane/016 Toy/City Diorama")]
        public static void Run()
        {
            UiThemeAssets.RequireEditMode();
            Folder(Art); Folder(Prefabs); Directory.CreateDirectory(Shots);
            var texture = PaletteTexture();
            var flat = MakeMaterial("M_CityGround", "Unlit/Texture", texture);
            var toy = MakeMaterial("M_CityToy", "SqueakyLane/Toy Lit Outline", texture);
            toy.SetFloat("_TopTint", 1); toy.SetFloat("_TopSaturation", 1);
            toy.SetColor("_ShadowColor", new Color32(178, 166, 196, 255));
            toy.SetFloat("_RampThreshold", .62f); toy.SetFloat("_RampSmooth", .05f);
            toy.SetFloat("_SpecStrength", 0); toy.SetFloat("_RimStrength", .1f);
            toy.SetFloat("_OutlineWidth", 1.9f); toy.SetColor("_OutlineColor", new Color32(36, 38, 43, 255));
            toy.enableInstancing = true;
            EditorUtility.SetDirty(toy);

            var clusters = new Dictionary<string, Cluster> { { "CityShops", Shops() }, { "CityPark", Park() }, { "CitySideTree", SideTree() }, { "CitySideFlowers", SideFlowers() } };
            var prefabs = new Dictionary<string, GameObject>();
            int solidTriangles = 0;
            foreach (var pair in clusters)
            {
                solidTriangles += pair.Value.Solid.T.Count / 3;
                var mesh = SaveMesh(pair.Value.Solid.ToMesh(pair.Key), Art + "/" + pair.Key + ".asset");
                SaveMesh(pair.Value.Flat.ToMesh(pair.Key + "_Flat"), Art + "/" + pair.Key + "_Flat.asset");
                var temp = MakeRenderer(pair.Key, mesh, toy);
                prefabs[pair.Key] = PrefabUtility.SaveAsPrefabAsset(temp, Prefabs + "/" + pair.Key + ".prefab");
                UnityEngine.Object.DestroyImmediate(temp);
            }
            AssetDatabase.SaveAssets();

            string report = "";
            report += GameShot("01-game-level03", "level_03", clusters, prefabs, flat, LotBlue, true);
            report += GameShot("02-game-level16", "level_16", clusters, prefabs, flat, LotBlue, true);
            Variants(clusters, prefabs, flat);
            Angles(clusters, prefabs, flat);
            Devices(clusters, prefabs, flat);
            SavePreviewScene(clusters, prefabs, flat);
            Debug.Log("016-e City Diorama: ảnh ở " + Shots + " · tam giác khối " + solidTriangles + " · " + report + " · 11 draw (nền 1 + 4 cụm × 2 + cảnh xa 2)");
        }

        private static readonly Color[] LotDark = null;
        private static Color[] LotBlue => new[] { Rgb("737CA3"), Rgb("98A0C4"), Rgb("E4E0F2") };
        private static Color[] LotLavender => new[] { Rgb("9A8FD0"), Rgb("B4ABE0"), Rgb("EADCF5") };

        private sealed class Stage : IDisposable
        {
            public Scene Scene;
            public BoardView Board;
            public Camera Camera;
            public LevelDef Level;
            public int Longest, GroundTriangles, FillTriangles;
            private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
            public void Own(UnityEngine.Object value) { owned.Add(value); }
            public void Dispose()
            {
                EditorSceneManager.ClosePreviewScene(Scene);
                foreach (var item in owned) { if (item != null) { UnityEngine.Object.DestroyImmediate(item); } }
            }
        }

        // Cảnh xem trước tạm: bãi thật của game (BoardView, không gán theme nên không hiện phố cũ) + khu phố mới.
        private static Stage Open(string levelName, Dictionary<string, Cluster> clusters, Dictionary<string, GameObject> prefabs, Material flat, Color[] lot, Scene? into = null)
        {
            var stage = new Stage { Scene = into ?? EditorSceneManager.NewPreviewScene() };
            var json = Resources.Load<TextAsset>("Levels/" + levelName);
            if (json == null) { throw new InvalidOperationException("Không thấy màn " + levelName); }
            var level = LevelJsonLoader.Load(json.text, json.name, out _);
            stage.Level = level;
            var session = new GameSession(level);
            var boardObject = Spawn(stage, "Board");
            stage.Board = boardObject.AddComponent<BoardView>();
            stage.Board.Build(session.Level, session.State, Load<VisualPalette>("Assets/_Project/Config/VisualPalette.asset"),
                Load<CarCatalog>("Assets/_Project/Config/CarCatalog.asset"), Load<MechanicCatalog>("Assets/_Project/Config/MechanicCatalog.asset"));
            var feel = Load<FeelConfig>("Assets/_Project/Config/FeelConfig.asset");
            for (int i = 0; i < session.State.CarCount; i++) { stage.Board.Car(i).ConfigureFeel(feel); }
            for (int i = 0; i < session.State.CarCount; i++) { stage.Board.Car(i).Show(stage.Board.Position(session.State.CarAt(i), stage.Board.Car(i).Length), BoardView.Yaw(session.State.CarAt(i).Axis), session.State.CarAt(i).Color, session.State.CarAt(i).Mud, true); }
            for (int i = 0; i < session.State.CarCount; i++) { stage.Longest = Mathf.Max(stage.Longest, stage.Board.Car(i).Length); }
            if (lot != null)
            {
                var surface = stage.Board.GetComponentInChildren<LotSurfaceView>();
                if (surface != null)
                {
                    var renderer = surface.GetComponent<MeshRenderer>();
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length && i < lot.Length; i++)
                    {
                        materials[i] = new Material(materials[i]) { color = lot[i] };
                        stage.Own(materials[i]);
                    }
                    renderer.sharedMaterials = materials;
                }
            }
            var ground = BuildGround(level);
            var places = Layout(level);
            var down = Matrix4x4.Translate(new Vector3(0, -Ground, 0));
            var fill = BuildFill(level, places, clusters);
            ground.Append(fill.Flat, Matrix4x4.identity);
            var fillMesh = fill.Solid.ToMesh("CityFill");
            stage.Own(fillMesh);
            stage.FillTriangles = fill.Solid.T.Count / 3;
            var fillObject = MakeRenderer("CityFill", fillMesh, prefabs["CityShops"].GetComponent<MeshRenderer>().sharedMaterial);
            SceneManager.MoveGameObjectToScene(fillObject, stage.Scene);
            fillObject.transform.position = Vector3.up * Ground;
            foreach (var place in places)
            {
                ground.Append(clusters[place.Name].Flat, down * place.Matrix);
                var item = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[place.Name], stage.Scene);
                item.transform.position = place.Matrix.GetColumn(3);
                item.transform.localScale = place.Matrix.lossyScale;
            }
            stage.GroundTriangles = ground.T.Count / 3;
            var groundMesh = ground.ToMesh("CityGround");
            stage.Own(groundMesh);
            var groundObject = MakeRenderer("CityGround", groundMesh, flat);
            SceneManager.MoveGameObjectToScene(groundObject, stage.Scene);
            groundObject.transform.position = Vector3.up * Ground;

            var light = Spawn(stage, "Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1; light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            stage.Camera = Spawn(stage, "Camera").AddComponent<Camera>();
            stage.Camera.enabled = into.HasValue;
            if (!into.HasValue) { stage.Camera.scene = stage.Scene; }
            stage.Camera.clearFlags = CameraClearFlags.SolidColor; stage.Camera.backgroundColor = Rgb("A3C75F");
            stage.Camera.nearClipPlane = .1f; stage.Camera.farClipPlane = 200; stage.Camera.allowHDR = false;
            return stage;
        }
        private static GameObject Spawn(Stage stage, string name)
        {
            var item = new GameObject(name);
            SceneManager.MoveGameObjectToScene(item, stage.Scene);
            return item;
        }

        // Cùng công thức BoardView.FitCamera + phần chừa phố, viewport theo safe area iPhone 12 Pro Max.
        private static void FitGame(Stage stage, float aspect)
        {
            var config = Load<CameraConfig>("Assets/_Project/Config/CameraConfig.asset");
            var view = new Rect(0, .192f, 1, .63f);
            float washSide = 0, washEnd = 0;
            foreach (var gate in stage.Level.Gates)
            {
                if (gate.Color != TileColor.Water) { continue; }
                if (gate.Edge == GateEdge.Left || gate.Edge == GateEdge.Right) { washSide = stage.Longest + .4f; } else { washEnd = stage.Longest + .4f; }
            }
            float street = StreetInner + StreetWidth * StreetVisible, sin = Mathf.Sin(config.TiltDegrees * Mathf.Deg2Rad);
            float halfWidth = stage.Level.Cols * .5f + Mathf.Max(.9f, washSide, street) + config.Padding;
            float halfHeight = (stage.Level.Rows * .5f + Mathf.Max(.9f, washEnd, street) + config.Padding) * sin + .7f;
            var camera = stage.Camera;
            camera.orthographic = true; camera.transform.rotation = Quaternion.Euler(config.TiltDegrees, 0, 0);
            camera.orthographicSize = Mathf.Max(halfHeight / view.height, halfWidth / (aspect * view.width));
            float offsetY = (view.center.y * 2 - 1) * camera.orthographicSize;
            camera.transform.position = -camera.transform.forward * 30 - camera.transform.up * offsetY;
        }

        private static Texture2D Render(Stage stage, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            var previous = RenderTexture.active;
            stage.Camera.targetTexture = target; stage.Camera.aspect = width / (float)height;
            stage.Camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            RenderTexture.active = previous; stage.Camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            return image;
        }
        private static void Save(Texture2D image, string name)
        {
            File.WriteAllBytes(Shots + "/" + name + ".png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        // Làm tối vùng HUD (đồng hồ) và vùng booster để người xem biết phần nào bị che khi chơi.
        private static void MarkHud(Texture2D image)
        {
            var pixels = image.GetPixels32();
            for (int row = 0; row < image.height; row++)
            {
                float fromTop = 1 - row / (float)image.height;
                bool covered = fromTop < .16f || fromTop > .765f && fromTop < .95f;
                if (!covered) { continue; }
                for (int column = 0; column < image.width; column++)
                {
                    int i = row * image.width + column;
                    bool stripe = (row + column) % 18 < 2;
                    pixels[i] = Color32.Lerp(pixels[i], stripe ? new Color32(255, 255, 255, 255) : new Color32(25, 15, 55, 255), stripe ? .25f : .22f);
                }
            }
            image.SetPixels32(pixels); image.Apply();
        }
        private static string GameShot(string name, string level, Dictionary<string, Cluster> clusters, Dictionary<string, GameObject> prefabs, Material flat, Color[] lot, bool hud)
        {
            using (var stage = Open(level, clusters, prefabs, flat, lot))
            {
                FitGame(stage, 1284f / 2778);
                var image = Render(stage, 856, 1852);
                if (hud) { MarkHud(image); }
                Save(image, name);
                return level + " nền " + stage.GroundTriangles + " + cảnh xa " + stage.FillTriangles + " tam giác; ";
            }
        }
        private static void Variants(Dictionary<string, Cluster> clusters, Dictionary<string, GameObject> prefabs, Material flat)
        {
            var options = new[] { LotDark, LotBlue, LotLavender };
            const int Width = 642, Height = 1389, CropY = 370, CropHeight = 700, Gap = 12;
            var sheet = new Texture2D(Width * 3 + Gap * 2, CropHeight, TextureFormat.RGB24, false);
            var white = new Color[sheet.width * sheet.height];
            for (int i = 0; i < white.Length; i++) { white[i] = Color.white; }
            sheet.SetPixels(white);
            for (int i = 0; i < options.Length; i++)
            {
                using (var stage = Open("level_16", clusters, prefabs, flat, options[i]))
                {
                    FitGame(stage, 1284f / 2778);
                    var image = Render(stage, Width, Height);
                    sheet.SetPixels(i * (Width + Gap), 0, Width, CropHeight, image.GetPixels(0, CropY, Width, CropHeight));
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            sheet.Apply();
            Save(sheet, "07-lot-variants");
        }
        private static void Angles(Dictionary<string, Cluster> clusters, Dictionary<string, GameObject> prefabs, Material flat)
        {
            using (var stage = Open("level_16", clusters, prefabs, flat, LotLavender))
            {
                var camera = stage.Camera;
                camera.orthographic = true; camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                camera.transform.position = new Vector3(0, 30, 0); camera.orthographicSize = 7.5f;
                var top = Render(stage, 1122, 1402);
                Save(SideBySide(top), "03-topdown-vs-concept");

                camera.orthographic = false; camera.fieldOfView = 28;
                camera.transform.rotation = Quaternion.Euler(37, -28, 0);
                camera.transform.position = new Vector3(0, 0, .2f) - camera.transform.forward * 28;
                Save(Render(stage, 1600, 1200), "04-hero");

                float shopZ = stage.Level.Rows * .5f + 2.6f;
                camera.fieldOfView = 24; camera.transform.rotation = Quaternion.Euler(42, 12, 0);
                camera.transform.position = new Vector3(0, .25f, shopZ) - camera.transform.forward * 12.5f;
                Save(Render(stage, 1600, 900), "05-shops");
                camera.transform.rotation = Quaternion.Euler(29, -17, 0);
                camera.transform.position = new Vector3(0, .35f, -shopZ - .25f) - camera.transform.forward * 12f;
                Save(Render(stage, 1600, 900), "06-park");
            }
        }
        // Cùng góc camera game trên bãi nhỏ/lớn và ba tỉ lệ máy để thấy bốn phía luôn có cảnh.
        private static void Devices(Dictionary<string, Cluster> clusters, Dictionary<string, GameObject> prefabs, Material flat)
        {
            var shots = new[]
            {
                new KeyValuePair<string, float>("level_01", 1284f / 2778), new KeyValuePair<string, float>("level_01", 1080f / 1920),
                new KeyValuePair<string, float>("level_01", 1536f / 2048), new KeyValuePair<string, float>("level_19", 1284f / 2778)
            };
            const int Height = 1200, Gap = 14;
            int total = 0;
            foreach (var shot in shots) { total += Mathf.RoundToInt(Height * shot.Value) + Gap; }
            var sheet = new Texture2D(total - Gap, Height, TextureFormat.RGB24, false);
            var white = new Color32[sheet.width * Height];
            for (int i = 0; i < white.Length; i++) { white[i] = new Color32(255, 255, 255, 255); }
            sheet.SetPixels32(white);
            int left = 0;
            foreach (var shot in shots)
            {
                int width = Mathf.RoundToInt(Height * shot.Value);
                using (var stage = Open(shot.Key, clusters, prefabs, flat, LotBlue))
                {
                    FitGame(stage, shot.Value);
                    var image = Render(stage, width, Height);
                    sheet.SetPixels(left, 0, width, Height, image.GetPixels());
                    UnityEngine.Object.DestroyImmediate(image);
                }
                left += width + Gap;
            }
            sheet.Apply();
            Save(sheet, "08-devices");
        }

        // Scene xem thử (không nằm trong Build Settings): mở ra xoay quanh trong Scene view; Game view dùng đúng góc camera game.
        private static void SavePreviewScene(Dictionary<string, Cluster> clusters, Dictionary<string, GameObject> prefabs, Material flat)
        {
            if (SceneManager.GetActiveScene().isDirty) { Debug.LogWarning("016-e: scene đang mở có thay đổi chưa lưu, bỏ qua bước tạo CityPreview."); return; }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Rgb("9AA9C2");
            var stage = Open("level_16", clusters, prefabs, flat, LotBlue, scene);
            FitGame(stage, 1284f / 2778);
            EditorSceneManager.SaveScene(scene, PreviewScene);
            var view = SceneView.lastActiveSceneView;
            if (view != null) { view.orthographic = false; view.LookAt(new Vector3(0, 0, .3f), Quaternion.Euler(48, -18, 0), 9); view.Repaint(); }
        }
        private static Texture2D SideBySide(Texture2D render)
        {
            var concept = new Texture2D(2, 2);
            concept.LoadImage(File.ReadAllBytes(Concept));
            const int Gap = 16;
            var sheet = new Texture2D(render.width * 2 + Gap, render.height, TextureFormat.RGB24, false);
            var pixels = new Color32[sheet.width * sheet.height];
            var right = render.GetPixels32();
            for (int y = 0; y < sheet.height; y++)
            {
                for (int x = 0; x < sheet.width; x++)
                {
                    Color32 color = new Color32(255, 255, 255, 255);
                    if (x < render.width) { color = concept.GetPixelBilinear((x + .5f) / render.width, (y + .5f) / render.height); }
                    else if (x >= render.width + Gap) { color = right[y * render.width + x - render.width - Gap]; }
                    pixels[y * sheet.width + x] = color;
                }
            }
            sheet.SetPixels32(pixels); sheet.Apply();
            UnityEngine.Object.DestroyImmediate(concept); UnityEngine.Object.DestroyImmediate(render);
            return sheet;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { throw new InvalidOperationException("Thiếu " + path); }
            return asset;
        }
        private static Color Rgb(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }
        private static GameObject MakeRenderer(string name, Mesh mesh, Material material)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return item;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        private static Texture2D PaletteTexture()
        {
            string path = Art + "/city_palette.png";
            var source = new Texture2D(Hex.Length, 1, TextureFormat.RGBA32, false);
            for (int i = 0; i < Hex.Length; i++) { source.SetPixel(i, 0, Rgb(Hex[i])); }
            source.Apply();
            var bytes = source.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(source);
            bool same = File.Exists(path) && Convert.ToBase64String(File.ReadAllBytes(path)) == Convert.ToBase64String(bytes);
            if (!same) { File.WriteAllBytes(path, bytes); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return Load<Texture2D>(path);
        }
        private static Material MakeMaterial(string name, string shaderName, Texture2D texture)
        {
            string path = Art + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) { throw new InvalidOperationException("Thiếu shader " + shaderName); }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }
        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            // Ghi qua API để Unity nạp lại cả vertex lẫn index lên GPU (CopySerialized để lại hình cũ trong bộ nhớ).
            existing.Clear();
            existing.indexFormat = mesh.indexFormat;
            existing.vertices = mesh.vertices; existing.normals = mesh.normals; existing.uv = mesh.uv;
            existing.triangles = mesh.triangles; existing.RecalculateBounds();
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
