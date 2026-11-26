using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SqueakyLane.Presentation
{
    // Bộ gom đỉnh và hình khối bo tròn dùng chung; mọi màu lấy từ một texture palette (PaletteHex).
    public static partial class CityBuilder
    {
        private enum C
        {
            Grass, GrassShade, Tuft, Road, Dash, Line, Walk, WalkSeam, WalkShade, Sand,
            Teal, TealWall, TealDark, Coral, CoralWall, CoralDark, Yellow, YellowWall, YellowDark, White,
            Wood, WoodDark, Glass, Gray, GrayDark, Pot, Leaf, LeafLight, LeafDark, Pink, Flower, Soil,
            GatePink, GateBlue, GateYellow, GateGreen, GateOrange, GatePurple,
            Lilac, LilacWall, Sky, SkyWall, Stone
        }
        public static IReadOnlyList<string> PaletteHex => Hex;
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
                var normals = m.inverse.transpose; // đúng cả khi co giãn không đều
                for (int i = 0; i < other.V.Count; i++) { V.Add(m.MultiplyPoint3x4(other.V[i])); N.Add(normals.MultiplyVector(other.N[i]).normalized); U.Add(other.U[i]); }
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
    }
}
