using UnityEngine;

namespace SqueakyLane.Presentation
{
    // Các vật trong khu phố: cửa hàng, công viên, nhà, cây, đài phun, ghế, đèn.
    public static partial class CityBuilder
    {
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
    }
}
