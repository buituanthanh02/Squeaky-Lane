using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
using SqueakyLane.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static SqueakyLane.EditorTools.EditorAssets;

namespace SqueakyLane.EditorTools
{
    internal sealed class ToyMechanicsTools
    {
        private const string Art = "Assets/_Project/Art/Mechanics/Toy";
        private const string Prefabs = "Assets/_Project/Prefabs/Mechanics/Toy";
        private const string PreviewPath = "Logs/016-mechanics-preview.png";
        private const int PaletteWidth = 32;
        private const int GlowSize = 32;
        private const int TriangleLimit = 1500;
        private const float HeightLimit = .9f;
        private const float Tau = 2f * Mathf.PI;

        private enum Ink
        {
            White, Charcoal, OffLamp, Cone, StopRed, GoGreen, Table, TableEdge, Amber, Mud, MudLight,
            Gate0, Gate1, Gate2, Gate3, Gate4, Gate5,
            Portal0, Portal1, Portal2, PortalDeep0, PortalDeep1, PortalDeep2
        }

        private enum Finish { Plastic, Signal, Halo, Tint }

        private readonly MechanicCatalog.Design design;
        private readonly Material[] finishes;
        private readonly Material faded;
        private readonly Dictionary<(MechanicKind kind, int variant), GameObject> prefabs =
            new Dictionary<(MechanicKind, int), GameObject>();
        private readonly Scene scene;

        [MenuItem("Tools/Squeaky Lane/016 Toy/Create Toy Mechanics")]
        public static void CreateToyMechanics()
        {
            var catalog = RequireAsset<MechanicCatalog>("Assets/_Project/Config/MechanicCatalog.asset");
            var palette = RequireAsset<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            var blue = RequireAsset<GameObject>("Assets/_Project/Prefabs/Cars/Car_blue.prefab");
            EnsureFolder(Art + "/Meshes");
            EnsureFolder(Prefabs);
            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var builder = new ToyMechanicsTools(catalog.Settings, palette, previewScene);
                builder.BuildAll();
                builder.RenderPreview(blue);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private ToyMechanicsTools(MechanicCatalog.Design design, VisualPalette palette, Scene scene)
        {
            this.design = design;
            this.scene = scene;
            if (design.portalColors == null || design.portalColors.Length < 3)
            {
                throw new InvalidOperationException("MechanicCatalog cần ba màu portal cho 016-b.");
            }

            var inks = BuildInks(palette, design);
            var atlas = SaveTexture("ToyMechanicsPalette", PaletteWidth, 1, inks, FilterMode.Point);
            var glowAtlas = SaveTexture("ToyMechanicsGlow", GlowSize * 2, GlowSize, BuildGlow(inks), FilterMode.Bilinear);
            finishes = new[]
            {
                SaveMaterial("M_MechanicPlastic", "SqueakyLane/Toy Lit", atlas),
                SaveMaterial("M_MechanicSignal", "Unlit/Texture", atlas),
                SaveMaterial("M_MechanicHalo", "Unlit/Transparent", glowAtlas),
                // Một màu phẳng, BoardView tô riêng từng miệng trụ bằng property block (_Color).
                SaveMaterial("M_MechanicTint", "Unlit/Color", null)
            };
            // Toy Lit là shader đục. Standard ở chế độ Fade đọc cùng atlas và đổi alpha theo catalog.
            faded = SaveMaterial("M_MechanicOccluded", "Standard", atlas, design.occludedAlpha);
        }

        private void BuildAll()
        {
            for (int i = 0; i < (int)TileColor.Water; i++)
            {
                Ink tint = Ink.Gate0 + i;
                BuildPrefab(MechanicKind.ColorGate, i, root => AddOcclusion(root, ColorGate(root, tint)));
            }
            BuildPrefab(MechanicKind.Cone, 0, root => Attach(root.transform, Cone()));
            BuildPrefab(MechanicKind.TrafficLight, 0, TrafficLight);
            BuildPrefab(MechanicKind.Turntable, 0, root => Attach(root.transform, Turntable()));
            BuildPrefab(MechanicKind.Pylon, 0, Pylon);
            for (int i = 0; i < 3; i++)
            {
                Ink glow = Ink.Portal0 + i;
                Ink deep = Ink.PortalDeep0 + i;
                BuildPrefab(MechanicKind.Portal, i, root =>
                {
                    AddOcclusion(root, Attach(root.transform, Portal(glow, deep)));
                    // Thẻ màu nằm ngoài bãi, không mờ theo cổng: xe đậu đè lên cổng vẫn đọc được màu.
                    Attach(Child(root.transform, "Outer Pad"), PortalPad(glow));
                });
            }
            BuildPrefab(MechanicKind.Mud, 0, root => Attach(root.transform, Mud()));
        }

        private void BuildPrefab(MechanicKind kind, int variant, Action<GameObject> build)
        {
            var key = kind + "_" + variant;
            var root = new GameObject(key);
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                root.AddComponent<MechanicView>().Configure(kind);
                build(root);
                ValidatePrefab(root, kind);
                string path = Prefabs + "/" + key + ".prefab";
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!SamePrefab(saved, root, kind))
                {
                    saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                if (saved == null)
                {
                    throw new InvalidOperationException("Không lưu được " + path);
                }
                ValidatePrefab(saved, kind);
                prefabs.Add((kind, variant), saved);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private MeshRenderer ColorGate(GameObject root, Ink tint)
        {
            var mesh = new Geometry();
            float width = design.gateWidth;
            float postX = width * .445f;
            // Taxi đứng yên cao khoảng .78 ô từ mặt bãi. Chừa khoảng hở cho nóc xe qua cần.
            float barY = Mathf.Min(design.gateHeight, HeightLimit) - .062f;
            for (int side = -1; side <= 1; side += 2)
            {
                var post = new Vector3(side * postX, 0, 0);
                mesh.Turned(post, Quaternion.identity, 12,
                    Profile(.046f, 0, Ink.Charcoal), Profile(.054f, .012f, Ink.Charcoal),
                    Profile(.054f, .067f, tint), Profile(.047f, .088f, tint),
                    Profile(.045f, barY - .05f, tint), Profile(.052f, barY, tint),
                    Profile(.049f, barY + .025f, tint), Profile(.039f, barY + .034f, tint));
            }
            mesh.Bar(width * .89f, barY, tint);

            // Huy hiệu hướng lên để thấy được khi cổng nằm ở cả bốn mép bãi.
            var badge = new Vector3(-width * .413f, barY + .025f, 0);
            mesh.Turned(badge, Quaternion.identity, 16,
                Profile(.070f, 0, tint), Profile(.084f, .008f, tint),
                Profile(.084f, .020f, Ink.White), Profile(.073f, .027f, Ink.White));
            var icon = badge + Vector3.up * .028f;
            mesh.Polygon(icon, Quaternion.identity, Ink.Charcoal, Finish.Plastic,
                new Vector2(-.052f, -.023f), new Vector2(.054f, -.023f), new Vector2(.054f, .008f),
                new Vector2(.049f, .017f), new Vector2(.030f, .019f), new Vector2(.014f, .037f),
                new Vector2(-.023f, .037f), new Vector2(-.037f, .019f), new Vector2(-.052f, .013f));
            mesh.Polygon(icon + Vector3.up * .001f, Quaternion.identity, tint, Finish.Signal,
                new Vector2(-.049f, -.014f), new Vector2(.050f, -.014f), new Vector2(.050f, .008f),
                new Vector2(.044f, .014f), new Vector2(.027f, .016f), new Vector2(.013f, .034f),
                new Vector2(-.022f, .034f), new Vector2(-.036f, .015f), new Vector2(-.049f, .010f));
            mesh.Polygon(icon + Vector3.up * .002f, Quaternion.identity, Ink.Charcoal, Finish.Plastic,
                new Vector2(-.024f, .016f), new Vector2(.020f, .016f),
                new Vector2(.010f, .029f), new Vector2(-.016f, .029f));
            return Attach(root.transform, mesh);
        }

        private Geometry Cone()
        {
            var mesh = new Geometry();
            float h = design.coneHeight;
            mesh.Box(new Vector3(0, .032f, 0), new Vector3(.40f, .064f, .40f), .027f, Quaternion.identity, Ink.Charcoal);
            mesh.Turned(Vector3.zero, Quaternion.identity, 20,
                Profile(.163f, .047f, Ink.Cone), Profile(.166f, .062f, Ink.Cone),
                Profile(.148f, .095f, Ink.Cone), Profile(.111f, h * .38f, Ink.White),
                Profile(.094f, h * .49f, Ink.Cone), Profile(.077f, h * .62f, Ink.White),
                Profile(.064f, h * .73f, Ink.Cone), Profile(.055f, h - .023f, Ink.Cone),
                Profile(.044f, h - .007f, Ink.Cone), Profile(.030f, h, Ink.Cone));
            return mesh;
        }

        private void TrafficLight(GameObject root)
        {
            var body = new Geometry();
            var corner = design.poleOffset;
            var poleTop = corner + Vector3.up * (design.poleHeight - .04f);
            body.Turned(corner, Quaternion.identity, 12,
                Profile(.102f, 0, Ink.Charcoal), Profile(.114f, .018f, Ink.Charcoal),
                Profile(.102f, .045f, Ink.Charcoal), Profile(.052f, .066f, Ink.Charcoal),
                Profile(.041f, .084f, Ink.Charcoal), Profile(.041f, poleTop.y - corner.y, Ink.Charcoal));
            var boxSize = new Vector3(.245f, .32f, .195f);
            var box = new Vector3(corner.x, Mathf.Min(design.poleHeight + design.lampBoxHeight, HeightLimit) - .225f, corner.z);
            var pitch = Quaternion.Euler(28, 0, 0);
            body.Box(box, boxSize, .043f, pitch, Ink.Charcoal);
            Attach(root.transform, body);
            Transform red = LightState(root, box, pitch, true);
            Transform green = LightState(root, box, pitch, false);
            var view = root.GetComponent<MechanicView>();
            view.Configure(MechanicKind.TrafficLight, null, red.gameObject, green.gameObject);
            view.SetRed(false);
        }

        private Transform LightState(GameObject root, Vector3 box, Quaternion pitch, bool red)
        {
            var mesh = new Geometry();
            var lampRotation = pitch * Quaternion.Euler(-90, 0, 0);
            var active = red ? Ink.StopRed : Ink.GoGreen;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                bool lit = (sign == 1) == red;
                var center = box + pitch * new Vector3(0, sign * .076f, -.096f);
                Ink lens = lit ? active : Ink.OffLamp;
                mesh.Turned(center, lampRotation, 16, lit ? Finish.Signal : Finish.Plastic,
                    Profile(.064f, 0, lens), Profile(.069f, .013f, lens),
                    Profile(.060f, .026f, lens), Profile(.040f, .034f, lens));
                if (lit)
                {
                    mesh.Glow(center + lampRotation * Vector3.up * .036f, lampRotation, .103f, red);
                }
            }
            float edge = (design.stopSpan - design.stopWidth * .65f) * .5f;
            float y = Mathf.Max(.003f, design.overlayLift * 2);
            float halfLength = design.stopSpan * .30f;
            float halfWidth = design.stopWidth * .30f;
            for (int i = 0; i < 4; i++)
            {
                var rotation = Quaternion.Euler(0, i * 90, 0);
                var stripe = Vector3.up * y + rotation * Vector3.forward * edge;
                mesh.Polygon(stripe, rotation, active, Finish.Signal,
                    new Vector2(-halfLength, -halfWidth), new Vector2(halfLength, -halfWidth),
                    new Vector2(halfLength, halfWidth), new Vector2(-halfLength, halfWidth));
            }
            var state = Child(root.transform, red ? "Red" : "Green");
            Attach(state, mesh);
            return state;
        }

        private Geometry Turntable()
        {
            var mesh = new Geometry();
            float radius = design.tableDiameter * .5f;
            mesh.Turned(Vector3.zero, Quaternion.identity, 32,
                Profile(radius * .91f, 0, Ink.Charcoal), Profile(radius * .96f, .015f, Ink.TableEdge),
                Profile(radius * .963f, .039f, Ink.TableEdge), Profile(radius * .935f, .060f, Ink.TableEdge),
                Profile(radius * .858f, .060f, Ink.Table), Profile(radius * .827f, .054f, Ink.Table));
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Tau / 16;
                var rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
                var position = rotation * Vector3.right * (radius * .951f) + Vector3.up * .024f;
                mesh.Tooth(position, rotation, radius);
            }
            for (int i = 0; i < 2; i++)
            {
                mesh.CurvedArrow(i * Mathf.PI + Mathf.PI * .22f, radius * .67f, radius * .062f);
            }
            return mesh;
        }

        private void Pylon(GameObject root)
        {
            var baseMesh = new Geometry();
            float radius = design.pylonRingDiameter * .5f;
            float baseHeight = design.pylonDiscHeight;
            baseMesh.Turned(Vector3.zero, Quaternion.identity, 24,
                Profile(radius * .90f, 0, Ink.Charcoal), Profile(radius, .012f, Ink.Charcoal),
                Profile(radius, baseHeight * .75f, Ink.Amber), Profile(radius * .93f, baseHeight, Ink.Amber),
                Profile(radius * .82f, baseHeight, Ink.Charcoal), Profile(radius * .79f, baseHeight, Ink.Charcoal));
            Attach(root.transform, baseMesh);

            var moving = Child(root.transform, "Moving Part");
            var ring = new Geometry();
            float outerHeight = Mathf.Min(design.pylonDoorHeight, HeightLimit - baseHeight);
            const float tube = .045f;
            var center = Vector3.up * (baseHeight + outerHeight * .5f);
            var ellipse = new Vector2(design.pylonEntryWidth * .5f - tube, outerHeight * .5f - tube);
            var path = Arc(center, ellipse, 24, Tau);
            ring.Tube(path, tube, .5f * design.pylonDoorDepth, 10, true, Ink.Charcoal, Finish.Plastic);
            for (int side = -1; side <= 1; side += 2)
            {
                var face = path.Select(p => p + Vector3.forward * (side * design.pylonDoorDepth * .46f)).ToArray();
                ring.Tube(face, .019f, .013f, 6, true, Ink.White, Finish.Signal);
            }
            AddOcclusion(root, Attach(Child(moving, "Door"), ring));

            // Hai nửa mặt trụ: miệng A (+Z) và miệng B (−Z), cùng màu với cổng tường mà miệng đó nối tới.
            const int padSegments = 14;
            for (int side = 0; side < 2; side++)
            {
                var pad = new Geometry();
                var points = new Vector2[padSegments + 1];
                for (int i = 0; i <= padSegments; i++)
                {
                    float angle = i * Mathf.PI / padSegments;
                    points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * .93f);
                }
                pad.Polygon(Vector3.up * (baseHeight + design.overlayLift), Quaternion.Euler(0, side * 180, 0),
                    Ink.White, Finish.Tint, points);
                Attach(Child(moving, side == 0 ? "Face A" : "Face B"), pad);
            }

            var arrows = new Geometry();
            for (int side = -1; side <= 1; side += 2)
            {
                var arrow = Vector3.up * (baseHeight + design.overlayLift * 3);
                var rotation = Quaternion.Euler(0, side == 1 ? 0 : 180, 0);
                float tip = design.pylonEntryOffset - design.pylonArrowLength;
                float back = Mathf.Min(design.pylonEntryOffset, radius);
                float w = design.pylonArrowWidth * .47f;
                arrows.Polygon(arrow, rotation, Ink.Charcoal, Finish.Signal,
                    new Vector2(0, tip), new Vector2(w, back), new Vector2(-w, back));
            }
            Attach(Child(moving, "Entry Arrows"), arrows);
            root.GetComponent<MechanicView>().Configure(MechanicKind.Pylon, moving);
        }

        private Geometry Portal(Ink glow, Ink deep)
        {
            var mesh = new Geometry();
            float width = design.portalWidth;
            float height = Mathf.Min(design.portalHeight, HeightLimit);
            const float rim = .057f;
            float halfSpan = width * .46f;
            float spring = height * .45f;
            var arc = Arc(Vector3.up * spring, new Vector2(halfSpan, height - spring - rim - .012f), 16, Mathf.PI);
            var path = new List<Vector3> { new Vector3(halfSpan, 0, 0) };
            path.AddRange(arc);
            path.Add(new Vector3(-halfSpan, 0, 0));
            mesh.Tube(path, rim, .082f, 8, false, Ink.Charcoal, Finish.Plastic);
            for (int side = -1; side <= 1; side += 2)
            {
                var normal = Vector3.forward * side;
                var trim = path.Select(p => p + normal * .070f).ToArray();
                mesh.Tube(trim, .023f, .023f, 6, false, glow, Finish.Signal);
                var disk = path.Select(p => new Vector3(p.x * .94f, Mathf.Max(.021f, p.y), side * .014f)).ToArray();
                mesh.Fan(disk, Vector3.up * spring + normal * .014f, normal, deep, Finish.Signal);
                mesh.Spiral(new Vector3(0, height * .45f, side * .018f),
                    new Vector2(halfSpan * .73f, height * .34f), normal, Ink.White);
            }
            return mesh;
        }

        // Thẻ bo góc trên mặt bó vỉa; +Z của prefab là phía ngoài bãi (BoardView xoay theo mặt cổng).
        private Geometry PortalPad(Ink glow)
        {
            var mesh = new Geometry();
            float height = design.curbHeight + .006f;
            Plate(design.portalWidth * .5f, .06f, .42f, height, Ink.Charcoal);
            Plate(design.portalWidth * .5f - .04f, .095f, .385f, height + .004f, glow);
            return mesh;

            void Plate(float halfWidth, float near, float far, float y, Ink ink)
            {
                const float corner = .09f;
                var centers = new[]
                {
                    new Vector2(halfWidth - corner, far - corner), new Vector2(corner - halfWidth, far - corner),
                    new Vector2(corner - halfWidth, near + corner), new Vector2(halfWidth - corner, near + corner)
                };
                var outline = new List<Vector3>();
                for (int k = 0; k < 4; k++)
                {
                    for (int s = 0; s <= 4; s++)
                    {
                        float angle = (k + s / 4f) * Mathf.PI * .5f;
                        outline.Add(new Vector3(centers[k].x + Mathf.Cos(angle) * corner, y, centers[k].y + Mathf.Sin(angle) * corner));
                    }
                }
                mesh.Fan(outline, new Vector3(0, y, (near + far) * .5f), Vector3.up, ink, Finish.Signal);
            }
        }

        private Geometry Mud()
        {
            var mesh = new Geometry();
            const int segments = 28;
            var lower = new Vector3[segments];
            var upper = new Vector3[segments];
            float top = Mathf.Min(.025f, design.mudHeight);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Tau / segments;
                float radius = design.mudRadius * (.94f + .10f * Mathf.Sin(3 * angle + .6f) + .058f * Mathf.Cos(5 * angle));
                lower[i] = Flat(angle, radius) + Vector3.up * .002f;
                upper[i] = Flat(angle, radius * .92f) + Vector3.up * top;
            }
            mesh.Fan(upper, Vector3.up * top, Vector3.up, Ink.Mud, Finish.Plastic);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                mesh.Quad(lower[i], lower[next], upper[next], upper[i],
                    (Vector3.up + Flat((i + .5f) * Tau / segments, 1)).normalized, Ink.MudLight);
            }
            Bubble(new Vector3(-.14f, top, .10f), .066f, .018f);
            Bubble(new Vector3(.16f, top, -.08f), .045f, .014f);
            Bubble(new Vector3(.05f, top, .17f), .030f, .010f);
            return mesh;

            void Bubble(Vector3 position, float radius, float height)
            {
                mesh.Turned(position, Quaternion.identity, 12,
                    Profile(radius, -.003f, Ink.MudLight), Profile(radius * .88f, height * .54f, Ink.MudLight),
                    Profile(radius * .52f, height - .003f, Ink.MudLight));
            }
        }

        private MeshRenderer Attach(Transform target, Geometry geometry)
        {
            string key = target.root == target ? target.name : target.root.name + "_" + target.name.Replace(' ', '_');
            var mesh = geometry.ToMesh(key);
            target.gameObject.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh);
            var renderer = target.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = geometry.Finishes.Select(finish => finishes[(int)finish]).ToArray();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private void AddOcclusion(GameObject root, Renderer renderer)
        {
            var solid = renderer.sharedMaterials;
            root.AddComponent<MechanicOcclusion>().Configure(renderer, solid, solid.Select(_ => faded).ToArray());
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Vector3 Flat(float radians, float radius)
        {
            return new Vector3(Mathf.Cos(radians) * radius, 0, Mathf.Sin(radians) * radius);
        }

        private static Vector3[] Arc(Vector3 center, Vector2 radius, int segments, float angle)
        {
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i * angle / segments;
                points[i] = center + new Vector3(radius.x * Mathf.Cos(t), radius.y * Mathf.Sin(t), 0);
            }
            if (angle == Tau)
            {
                points[segments] = points[0];
            }
            return points;
        }

        private static (float radius, float y, Ink ink) Profile(float radius, float y, Ink ink)
        {
            return (radius, y, ink);
        }

        private static Color32[] BuildInks(VisualPalette palette, MechanicCatalog.Design design)
        {
            var inks = new Color32[PaletteWidth];
            inks[(int)Ink.White] = new Color32(255, 250, 242, 255);
            inks[(int)Ink.Charcoal] = new Color32(47, 53, 67, 255);
            inks[(int)Ink.OffLamp] = new Color32(98, 108, 124, 255);
            inks[(int)Ink.Cone] = new Color32(255, 122, 26, 255);
            inks[(int)Ink.StopRed] = Color.Lerp(design.red, new Color32(255, 56, 67, 255), .7f);
            inks[(int)Ink.GoGreen] = Color.Lerp(design.green, new Color32(65, 245, 151, 255), .75f);
            inks[(int)Ink.Table] = new Color32(53, 195, 255, 255);
            inks[(int)Ink.TableEdge] = new Color32(134, 224, 255, 255);
            inks[(int)Ink.Amber] = design.pylon;
            inks[(int)Ink.Mud] = new Color32(107, 66, 38, 255);
            inks[(int)Ink.MudLight] = new Color32(138, 90, 51, 255);
            for (int i = 0; i < (int)TileColor.Water; i++)
            {
                inks[(int)Ink.Gate0 + i] = palette.Car((TileColor)i);
            }
            for (int i = 0; i < 3; i++)
            {
                inks[(int)Ink.Portal0 + i] = design.portalColors[i];
                inks[(int)Ink.PortalDeep0 + i] = Color.Lerp(new Color32(24, 24, 53, 255), design.portalColors[i], .68f);
            }
            return inks;
        }

        private static Color32[] BuildGlow(Color32[] inks)
        {
            var pixels = new Color32[GlowSize * GlowSize * 2];
            for (int tile = 0; tile < 2; tile++)
            {
                var color = inks[tile == 0 ? (int)Ink.StopRed : (int)Ink.GoGreen];
                for (int y = 0; y < GlowSize; y++)
                {
                    for (int x = 0; x < GlowSize; x++)
                    {
                        var offset = new Vector2(x, y) / (GlowSize - 1) * 2 - Vector2.one;
                        float weight = 1 - Mathf.SmoothStep(.0f, 1, Mathf.InverseLerp(.25f, .96f, offset.magnitude));
                        color.a = (byte)Mathf.RoundToInt(weight * 94);
                        pixels[y * GlowSize * 2 + tile * GlowSize + x] = color;
                    }
                }
            }
            return pixels;
        }

        private static Texture2D SaveTexture(string name, int width, int height, Color32[] pixels, FilterMode filter)
        {
            string path = Art + "/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (saved != null && saved.width == width && saved.height == height && saved.format == TextureFormat.RGBA32 &&
                saved.mipmapCount == 1 && saved.filterMode == filter && saved.wrapMode == TextureWrapMode.Clamp &&
                saved.GetPixels32().SequenceEqual(pixels))
            {
                return saved;
            }
            bool isNew = saved == null;
            if (isNew)
            {
                saved = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }
            else if (!saved.Reinitialize(width, height, TextureFormat.RGBA32, false))
            {
                throw new InvalidOperationException("Không cập nhật được " + path);
            }
            saved.name = name;
            saved.filterMode = filter;
            saved.wrapMode = TextureWrapMode.Clamp;
            saved.SetPixels32(pixels);
            saved.Apply(false);
            if (isNew) { AssetDatabase.CreateAsset(saved, path); }
            else { SaveDirty(saved); }
            return saved;
        }

        private static Material SaveMaterial(string name, string shaderName, Texture2D texture, float? alpha = null)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { throw new InvalidOperationException("Thiếu shader " + shaderName); }
            var material = new Material(shader) { name = name };
            if (texture != null) { material.mainTexture = texture; }
            if (alpha.HasValue)
            {
                material.color = new Color(1, 1, 1, alpha.Value);
                material.SetFloat("_Mode", 2);
                material.SetFloat("_Glossiness", .1f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            string path = Art + "/" + name + ".mat";
            var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (saved == null)
            {
                AssetDatabase.CreateAsset(material, path);
                return material;
            }
            try
            {
                if (EditorJsonUtility.ToJson(saved) != EditorJsonUtility.ToJson(material))
                {
                    EditorUtility.CopySerialized(material, saved);
                    SaveDirty(saved);
                }
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        private static Mesh SaveMesh(Mesh mesh)
        {
            string path = Art + "/Meshes/" + mesh.name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            try
            {
                if (saved.subMeshCount != mesh.subMeshCount || !saved.vertices.SequenceEqual(mesh.vertices) ||
                    !saved.normals.SequenceEqual(mesh.normals) || !saved.uv.SequenceEqual(mesh.uv) ||
                    Enumerable.Range(0, mesh.subMeshCount).Any(i => !saved.GetIndices(i).SequenceEqual(mesh.GetIndices(i))))
                {
                    EditorUtility.CopySerialized(mesh, saved);
                    SaveDirty(saved);
                }
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        private static void SaveDirty(Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void ValidatePrefab(GameObject prefab, MechanicKind kind)
        {
            if (!MechanicView.Usable(prefab, kind))
            {
                throw new InvalidOperationException(prefab.name + " không đáp ứng MechanicView.Usable.");
            }
            if (kind == MechanicKind.ColorGate || kind == MechanicKind.Portal || kind == MechanicKind.Pylon)
            {
                var occlusion = prefab.GetComponent<MechanicOcclusion>();
                if (occlusion == null || !occlusion.IsConfigured)
                {
                    throw new InvalidOperationException(prefab.name + " thiếu thiết lập che mờ.");
                }
            }
            int triangles = 0;
            float maxY = float.MinValue;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    triangles += (int)mesh.GetIndexCount(i) / 3;
                }
                maxY = Mathf.Max(maxY, mesh.bounds.max.y);
            }
            float limit = kind == MechanicKind.Mud ? .04f : HeightLimit;
            if (triangles <= 0 || triangles > TriangleLimit || maxY > limit + .00001f)
            {
                throw new InvalidOperationException($"{prefab.name}: {triangles} tam giác, cao {maxY:F3} ô.");
            }
        }

        private static bool SamePrefab(GameObject stored, GameObject candidate, MechanicKind kind)
        {
            if (!MechanicView.Usable(stored, kind)) { return false; }
            var expected = candidate.GetComponentsInChildren<Transform>(true);
            var current = stored.GetComponentsInChildren<Transform>(true);
            if (current.Length != expected.Length) { return false; }
            for (int i = 0; i < current.Length; i++)
            {
                var a = current[i];
                var b = expected[i];
                if (a.name != b.name || a.gameObject.activeSelf != b.gameObject.activeSelf ||
                    a.localPosition != b.localPosition || a.localRotation != b.localRotation || a.localScale != b.localScale ||
                    Array.IndexOf(current, a.parent) != Array.IndexOf(expected, b.parent) ||
                    !a.GetComponents<Component>().Select(c => c.GetType()).SequenceEqual(b.GetComponents<Component>().Select(c => c.GetType())))
                {
                    return false;
                }
                var mesh = b.GetComponent<MeshFilter>();
                if (mesh != null)
                {
                    var renderer = a.GetComponent<MeshRenderer>();
                    if (a.GetComponent<MeshFilter>().sharedMesh != mesh.sharedMesh || !renderer.enabled ||
                        renderer.shadowCastingMode != ShadowCastingMode.Off || renderer.receiveShadows ||
                        !renderer.sharedMaterials.SequenceEqual(b.GetComponent<MeshRenderer>().sharedMaterials))
                    {
                        return false;
                    }
                }
            }

            var currentView = stored.GetComponent<MechanicView>();
            var expectedView = candidate.GetComponent<MechanicView>();
            if (!currentView.enabled || currentView.MovingPart.name != expectedView.MovingPart.name)
            {
                return false;
            }
            if (kind == MechanicKind.TrafficLight)
            {
                var a = new SerializedObject(currentView);
                var b = new SerializedObject(expectedView);
                if (!SameReferenceName(a, b, "redState") || !SameReferenceName(a, b, "greenState")) { return false; }
            }
            var occlusion = candidate.GetComponent<MechanicOcclusion>();
            if (occlusion == null) { return true; }
            var storedOcclusion = stored.GetComponent<MechanicOcclusion>();
            if (!storedOcclusion.enabled || !storedOcclusion.IsConfigured) { return false; }
            var old = new SerializedObject(storedOcclusion);
            var updated = new SerializedObject(occlusion);
            return SameReferenceName(old, updated, "target") &&
                SerializedProperty.DataEquals(old.FindProperty("solidMaterials"), updated.FindProperty("solidMaterials")) &&
                SerializedProperty.DataEquals(old.FindProperty("fadedMaterials"), updated.FindProperty("fadedMaterials"));
        }

        private static bool SameReferenceName(SerializedObject first, SerializedObject second, string field)
        {
            var a = first.FindProperty(field).objectReferenceValue;
            var b = second.FindProperty(field).objectReferenceValue;
            return a != null && b != null && a.name == b.name;
        }

        private void RenderPreview(GameObject blue)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    Place(prefabs[(MechanicKind.ColorGate, i)], new Vector3((i - 2.5f) * 1.32f, 0, 2.15f));
                }
                Place(prefabs[(MechanicKind.Cone, 0)], new Vector3(-2.85f, 0, .22f));
                var traffic = prefabs[(MechanicKind.TrafficLight, 0)];
                Place(traffic, new Vector3(-1.44f, 0, .22f)).GetComponent<MechanicView>().SetRed(true);
                Place(traffic, new Vector3(-.11f, 0, .22f));
                Place(prefabs[(MechanicKind.Turntable, 0)], new Vector3(1.63f, 0, .22f));
                Place(prefabs[(MechanicKind.Pylon, 0)], new Vector3(3.18f, 0, .22f));
                for (int i = 0; i < 3; i++)
                {
                    Place(prefabs[(MechanicKind.Portal, i)], new Vector3(-3.07f + i * 1.36f, 0, -1.85f));
                }
                var car = Place(blue, new Vector3(1.12f, .33f, -1.85f));
                car.transform.rotation = Quaternion.Euler(0, -19, 0);
                Place(prefabs[(MechanicKind.Mud, 0)], new Vector3(3.02f, 0, -1.85f));

                preview.ambientColor = new Color32(154, 169, 194, 255);
                preview.lights[0].intensity = 1;
                preview.lights[0].shadows = LightShadows.None;
                preview.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
                preview.lights[1].enabled = false;
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(58, 63, 75, 255);
                camera.orthographic = true;
                camera.orthographicSize = 2.57f;
                camera.renderingPath = RenderingPath.Forward;
                camera.allowHDR = false;
                var rotation = Quaternion.Euler(58, 0, 0);
                camera.transform.SetPositionAndRotation(Vector3.up * .14f + rotation * Vector3.back * 9, rotation);
                preview.BeginStaticPreview(new Rect(0, 0, 1280, 824));
                preview.Render();
                var image = preview.EndStaticPreview();
                if (image == null) { throw new InvalidOperationException("Không chụp được preview 016-b."); }
                try
                {
                    Directory.CreateDirectory("Logs");
                    var png = image.EncodeToPNG();
                    if (!File.Exists(PreviewPath) || !File.ReadAllBytes(PreviewPath).SequenceEqual(png))
                    {
                        File.WriteAllBytes(PreviewPath, png);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(image);
                }
            }
            finally
            {
                preview.Cleanup();
            }

            GameObject Place(GameObject prefab, Vector3 position)
            {
                var instance = preview.InstantiatePrefabInScene(prefab);
                instance.transform.position = position;
                return instance;
            }
        }

        // Mỗi loại vật liệu chỉ chiếm một submesh trên cùng một phần tĩnh hoặc phần chuyển động.
        private sealed class Geometry
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int>[] indices = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            public IEnumerable<Finish> Finishes => Enumerable.Range(0, indices.Length)
                .Where(i => indices[i].Count != 0).Select(i => (Finish)i);

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                var used = Finishes.ToArray();
                mesh.subMeshCount = used.Length;
                for (int i = 0; i < used.Length; i++)
                {
                    mesh.SetTriangles(indices[(int)used[i]], i, false);
                }
                mesh.RecalculateBounds();
                return mesh;
            }

            public void Turned(Vector3 center, Quaternion rotation, int segments,
                params (float radius, float y, Ink ink)[] profile)
            {
                Turned(center, rotation, segments, Finish.Plastic, profile);
            }

            public void Turned(Vector3 center, Quaternion rotation, int segments, Finish finish,
                params (float radius, float y, Ink ink)[] profile)
            {
                var points = new Vector3[profile.Length, segments];
                var sides = new Vector3[profile.Length, segments];
                for (int i = 0; i < profile.Length; i++)
                {
                    var before = profile[Mathf.Max(0, i - 1)];
                    var after = profile[Mathf.Min(profile.Length - 1, i + 1)];
                    var tangent = new Vector2(after.radius - before.radius, after.y - before.y).normalized;
                    for (int j = 0; j < segments; j++)
                    {
                        var radial = Flat(j * Tau / segments, 1);
                        points[i, j] = center + rotation * (radial * profile[i].radius + Vector3.up * profile[i].y);
                        sides[i, j] = rotation * (radial * tangent.y - Vector3.up * tangent.x);
                    }
                }
                for (int j = 0; j < segments; j++)
                {
                    int next = (j + 1) % segments;
                    for (int i = 0; i < profile.Length - 1; i++)
                    {
                        SmoothQuad(points[i, j], points[i, next], points[i + 1, next], points[i + 1, j],
                            sides[i, j], sides[i, next], sides[i + 1, next], sides[i + 1, j], profile[i].ink, finish);
                    }
                    int top = profile.Length - 1;
                    var up = rotation * Vector3.up;
                    PaintTriangle(center + up * profile[0].y, points[0, j], points[0, next], -up, profile[0].ink, finish);
                    PaintTriangle(center + up * profile[top].y, points[top, j], points[top, next], up, profile[top].ink, finish);
                }
            }

            public void Box(Vector3 center, Vector3 size, float bevel, Quaternion rotation, Ink ink)
            {
                var half = size * .5f;
                var core = half - Vector3.one * bevel;
                Face(Vector3.right, Vector3.up, Vector3.forward);
                Face(Vector3.left, Vector3.up, Vector3.forward);
                Face(Vector3.up, Vector3.right, Vector3.forward);
                Face(Vector3.down, Vector3.right, Vector3.forward);
                Face(Vector3.forward, Vector3.right, Vector3.up);
                Face(Vector3.back, Vector3.right, Vector3.up);

                void Face(Vector3 normal, Vector3 u, Vector3 v)
                {
                    var extent = Vector3.Scale(half, normal);
                    var h = new Vector2(Vector3.Dot(half, u), Vector3.Dot(half, v));
                    float[] us = { -h.x, -h.x + bevel, h.x - bevel, h.x };
                    float[] vs = { -h.y, -h.y + bevel, h.y - bevel, h.y };
                    var points = new Vector3[4, 4];
                    var side = new Vector3[4, 4];
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            var box = extent + u * us[i] + v * vs[j];
                            var inside = new Vector3(Mathf.Clamp(box.x, -core.x, core.x),
                                Mathf.Clamp(box.y, -core.y, core.y), Mathf.Clamp(box.z, -core.z, core.z));
                            var round = (box - inside).normalized;
                            points[i, j] = center + rotation * (inside + bevel * round);
                            side[i, j] = rotation * round;
                        }
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            SmoothQuad(points[i, j], points[i + 1, j], points[i + 1, j + 1], points[i, j + 1],
                                side[i, j], side[i + 1, j], side[i + 1, j + 1], side[i, j + 1], ink, Finish.Plastic);
                        }
                    }
                }
            }

            public void Bar(float length, float height, Ink tint)
            {
                var section = new[]
                {
                    new Vector2(.027f, -.022f), new Vector2(.027f, .022f), new Vector2(.017f, .033f),
                    new Vector2(-.017f, .033f), new Vector2(-.027f, .022f), new Vector2(-.027f, -.022f),
                    new Vector2(-.017f, -.033f), new Vector2(.017f, -.033f)
                };
                const int stripes = 7;
                const int rings = stripes + 3;
                var points = new Vector3[rings, section.Length];
                for (int i = 0; i < rings; i++)
                {
                    bool end = i == 0 || i == rings - 1;
                    float x = i == 0 ? -length * .5f : i == rings - 1 ? length * .5f :
                        Mathf.Lerp(-length * .445f, length * .445f, (i - 1f) / stripes);
                    for (int j = 0; j < section.Length; j++)
                    {
                        var yz = section[j] * (end ? .71f : 1);
                        points[i, j] = new Vector3(x + (end ? 0 : .62f * yz.x), height + yz.x, yz.y);
                    }
                }
                for (int i = 0; i < rings - 1; i++)
                {
                    var ink = i > 0 && i < rings - 2 && (i & 1) == 0 ? Ink.White : tint;
                    for (int j = 0; j < section.Length; j++)
                    {
                        int next = (j + 1) % section.Length;
                        var radial = section[j] + section[next];
                        Quad(points[i, j], points[i + 1, j], points[i + 1, next], points[i, next],
                            new Vector3(i == 0 ? -.010f : i == rings - 2 ? .010f : 0, radial.x, radial.y).normalized, ink);
                    }
                }
                for (int j = 0; j < section.Length; j++)
                {
                    int next = (j + 1) % section.Length;
                    PaintTriangle(new Vector3(-length * .5f, height, 0), points[0, j], points[0, next], Vector3.left, tint, Finish.Plastic);
                    PaintTriangle(new Vector3(length * .5f, height, 0), points[rings - 1, j], points[rings - 1, next], Vector3.right, tint, Finish.Plastic);
                }
            }

            public void Tooth(Vector3 center, Quaternion rotation, float radius)
            {
                var lower = new[]
                {
                    new Vector2(-.037f, -.060f), new Vector2(.047f, -.048f),
                    new Vector2(.047f, .048f), new Vector2(-.037f, .060f)
                };
                var bottom = lower.Select(p => center + rotation * new Vector3(p.x * radius, -.015f, p.y * radius)).ToArray();
                var top = lower.Select(p => center + rotation * new Vector3(p.x * radius * .83f, .015f, p.y * radius * .85f)).ToArray();
                for (int i = 0; i < lower.Length; i++)
                {
                    int next = (i + 1) % lower.Length;
                    var radial = (bottom[i] + bottom[next]) * .5f - center + Vector3.up * .027f;
                    Quad(bottom[i], bottom[next], top[next], top[i], radial.normalized, Ink.TableEdge);
                }
                Quad(top[0], top[1], top[2], top[3], Vector3.up, Ink.TableEdge);
            }

            public void CurvedArrow(float start, float radius, float halfWidth)
            {
                const int segments = 16;
                const float sweep = 2.02f;
                var y = Vector3.up * .056f;
                for (int i = 0; i < segments; i++)
                {
                    float a = start - sweep * i / segments;
                    float b = start - sweep * (i + 1) / segments;
                    Quad(y + Flat(a, radius - halfWidth), y + Flat(b, radius - halfWidth),
                        y + Flat(b, radius + halfWidth), y + Flat(a, radius + halfWidth),
                        Vector3.up, Ink.White, Finish.Signal);
                }
                float angle = start - sweep;
                var tangent = new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle));
                PaintTriangle(y + Flat(angle, radius) + tangent * (3.3f * halfWidth),
                    y + Flat(angle, radius + 1.85f * halfWidth), y + Flat(angle, radius - 1.85f * halfWidth),
                    Vector3.up, Ink.White, Finish.Signal);
            }

            public void Tube(IReadOnlyList<Vector3> path, float radius, float depth, int sides, bool loop, Ink ink, Finish finish)
            {
                int rings = path.Count - (loop ? 1 : 0);
                var points = new Vector3[rings, sides];
                var normals = new Vector3[rings, sides];
                for (int i = 0; i < rings; i++)
                {
                    int before = i == 0 ? (loop ? rings - 1 : 0) : i - 1;
                    int after = i == rings - 1 ? (loop ? 0 : i) : i + 1;
                    var tangent = (path[after] - path[before]).normalized;
                    var radial = Vector3.Cross(tangent, Vector3.forward);
                    for (int j = 0; j < sides; j++)
                    {
                        float theta = j * Tau / sides;
                        var planar = radial * Mathf.Cos(theta);
                        var z = Vector3.forward * Mathf.Sin(theta);
                        points[i, j] = path[i] + planar * radius + z * depth;
                        normals[i, j] = (planar / radius + z / depth).normalized;
                    }
                }
                for (int i = 0; i < (loop ? rings : rings - 1); i++)
                {
                    int nextRing = (i + 1) % rings;
                    for (int j = 0; j < sides; j++)
                    {
                        int nextSide = (j + 1) % sides;
                        SmoothQuad(points[i, j], points[nextRing, j], points[nextRing, nextSide], points[i, nextSide],
                            normals[i, j], normals[nextRing, j], normals[nextRing, nextSide], normals[i, nextSide], ink, finish);
                    }
                }
                if (loop) { return; }
                var firstNormal = (path[0] - path[1]).normalized;
                var lastNormal = (path[rings - 1] - path[rings - 2]).normalized;
                for (int j = 0; j < sides; j++)
                {
                    int next = (j + 1) % sides;
                    PaintTriangle(path[0], points[0, j], points[0, next], firstNormal, ink, finish);
                    PaintTriangle(path[rings - 1], points[rings - 1, j], points[rings - 1, next], lastNormal, ink, finish);
                }
            }

            public void Spiral(Vector3 center, Vector2 radius, Vector3 normal, Ink ink)
            {
                const int steps = 24;
                for (int arm = 0; arm < 2; arm++)
                {
                    var left = new Vector3[steps + 1];
                    var right = new Vector3[steps + 1];
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        var p = Curve(arm, t);
                        var tangent = Curve(arm, Mathf.Min(steps, i + 1) / (float)steps) -
                            Curve(arm, Mathf.Max(0, i - 1) / (float)steps);
                        var across = Vector3.Cross(tangent.normalized, normal) * Mathf.Lerp(.031f, .005f, t);
                        left[i] = p - across;
                        right[i] = p + across;
                    }
                    for (int i = 0; i < steps; i++)
                    {
                        Quad(left[i], left[i + 1], right[i + 1], right[i], normal, ink, Finish.Signal);
                    }
                }

                Vector3 Curve(int arm, float t)
                {
                    float angle = arm * Mathf.PI + (1.13f - t) * Tau * .93f;
                    float reach = Mathf.Lerp(.09f, 1, t);
                    return center + new Vector3(radius.x * reach * Mathf.Cos(angle) * normal.z,
                        radius.y * reach * Mathf.Sin(angle), 0);
                }
            }

            public void Fan(IReadOnlyList<Vector3> outline, Vector3 center, Vector3 normal, Ink ink, Finish finish)
            {
                for (int i = 0; i < outline.Count; i++)
                {
                    PaintTriangle(center, outline[i], outline[(i + 1) % outline.Count], normal, ink, finish);
                }
            }

            public void Polygon(Vector3 center, Quaternion rotation, Ink ink, Finish finish, params Vector2[] points)
            {
                var outline = points.Select(p => center + rotation * new Vector3(p.x, 0, p.y)).ToArray();
                var normal = rotation * Vector3.up;
                for (int i = 1; i < outline.Length - 1; i++)
                {
                    PaintTriangle(outline[0], outline[i], outline[i + 1], normal, ink, finish);
                }
            }

            public void Glow(Vector3 center, Quaternion rotation, float radius, bool red)
            {
                var normal = rotation * Vector3.up;
                var right = rotation * Vector3.right * radius;
                var forward = rotation * Vector3.forward * radius;
                var a = center - right - forward;
                var b = center - right + forward;
                var c = center + right + forward;
                var d = center + right - forward;
                float tile = red ? 0 : .5f;
                float inset = .5f / (GlowSize * 2);
                var uva = new Vector2(tile + inset, 0);
                var uvb = new Vector2(tile + inset, 1);
                var uvc = new Vector2(tile + .5f - inset, 1);
                var uvd = new Vector2(tile + .5f - inset, 0);
                Triangle(a, b, c, normal, normal, normal, uva, uvb, uvc, Finish.Halo);
                Triangle(a, c, d, normal, normal, normal, uva, uvc, uvd, Finish.Halo);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Ink ink, Finish finish = Finish.Plastic)
            {
                SmoothQuad(a, b, c, d, normal, normal, normal, normal, ink, finish);
            }

            private void SmoothQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Ink ink, Finish finish)
            {
                var pixel = Pixel(ink);
                Triangle(a, b, c, na, nb, nc, pixel, pixel, pixel, finish);
                Triangle(a, c, d, na, nc, nd, pixel, pixel, pixel, finish);
            }

            private void PaintTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Ink ink, Finish finish)
            {
                var pixel = Pixel(ink);
                Triangle(a, b, c, normal, normal, normal, pixel, pixel, pixel, finish);
            }

            private static Vector2 Pixel(Ink color)
            {
                return new Vector2(((int)color + .5f) / PaletteWidth, .5f);
            }

            private void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc,
                Vector2 uva, Vector2 uvb, Vector2 uvc, Finish finish)
            {
                var cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude < 1e-16f) { return; }
                if (Vector3.Dot(cross, na + nb + nc) < 0)
                {
                    (b, c) = (c, b);
                    (nb, nc) = (nc, nb);
                    (uvb, uvc) = (uvc, uvb);
                }
                Add(a, na, uva);
                Add(b, nb, uvb);
                Add(c, nc, uvc);

                void Add(Vector3 point, Vector3 normal, Vector2 texcoord)
                {
                    indices[(int)finish].Add(vertices.Count);
                    vertices.Add(point);
                    normals.Add(normal);
                    uv.Add(texcoord);
                }
            }
        }
    }
}
