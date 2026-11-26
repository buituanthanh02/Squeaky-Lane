using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class MechanicCatalogTests
    {
        private GameObject root, templates;
        private BoardView board;
        private VisualPalette palette;
        private MechanicCatalog catalog;
        private Material material;
        private readonly List<MechanicCatalog.Entry> entries = new List<MechanicCatalog.Entry>();

        [SetUp]
        public void Setup()
        {
            root = new GameObject("011 Board");
            templates = new GameObject("011 Templates");
            templates.SetActive(false);
            board = root.AddComponent<BoardView>();
            material = new Material(Shader.Find("Standard"));
            palette = ScriptableObject.CreateInstance<VisualPalette>();
            typeof(VisualPalette).GetField("material", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(palette, material);
            catalog = ScriptableObject.CreateInstance<MechanicCatalog>();
            entries.Clear();
        }
        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(templates);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(palette);
            Object.DestroyImmediate(material);
        }

        [Test]
        public void MissingOrInvalidMechanicsFallBackAndWarnOnceAcrossReloads()
        {
            var invalid = new GameObject("Sai root scale");
            invalid.transform.SetParent(templates.transform, false);
            invalid.AddComponent<MechanicView>().Configure(MechanicKind.Cone);
            invalid.transform.localScale = Vector3.one * 2;
            entries.Add(new MechanicCatalog.Entry { kind = MechanicKind.Cone, prefab = invalid });
            SetEntries();
            var builder = new LevelDefBuilder(4, 4);
            builder.Cones.Add(new Cell(0, 0));
            builder.Gates.Add(new GateDef(GateEdge.Top, 2, TileColor.Pink));
            var level = builder.Build();
            LogAssert.Expect(LogType.Warning, new Regex("MechanicCatalog thiếu prefab hợp lệ"));
            board.Build(level, GameState.Initial(level), palette, null, catalog);
            Assert.That(root.GetComponentsInChildren<Renderer>().Any(r => r.name == "Cọc"), Is.True);
            Assert.That(root.GetComponentsInChildren<Renderer>().Any(r => r.name == "Cổng Pink"), Is.True);
            board.Build(level, GameState.Initial(level), palette, null, catalog);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BoardFeedsLightMudAndPylonStateAndUndoRestoresWithoutChangingCatalog()
        {
            var surface = Template(MechanicKind.Surface);
            surface.gameObject.AddComponent<LotSurfaceView>();
            surface.GetComponent<Renderer>().sharedMaterials = new[] { material, material, material };
            Template(MechanicKind.Mud);
            var light = Template(MechanicKind.TrafficLight);
            var red = new GameObject("Red + stop line");
            var green = new GameObject("Green + stop line");
            red.transform.SetParent(light.transform, false);
            green.transform.SetParent(light.transform, false);
            light.Configure(MechanicKind.TrafficLight, null, red, green);
            var pylon = Template(MechanicKind.Pylon);
            var arm = new GameObject("Arm").transform;
            arm.SetParent(pylon.transform, false);
            pylon.Configure(MechanicKind.Pylon, arm);
            Template(MechanicKind.Portal);
            Template(MechanicKind.Portal);
            entries[entries.Count - 1].variant = 1; // 016: miệng B của trụ dùng màu cổng thứ hai
            SetEntries();
            var builder = new LevelDefBuilder(5, 5);
            builder.Lights.Add(new LightDef(0, 0));
            builder.Muds.Add(new Cell(0, 1));
            builder.Pylons.Add(new PylonDef(2, 2, 0, new[] { new PortalFace(0, 3, Side.N), new PortalFace(4, 3, Side.S) }));
            var session = new GameSession(builder.Build());
            var before = session.State;
            board.Build(session.Level, before, palette, null, catalog);
            var views = root.GetComponentsInChildren<MechanicView>();
            var lightView = views.Single(v => v.Kind == MechanicKind.TrafficLight);
            var pylonView = views.Single(v => v.Kind == MechanicKind.Pylon);
            var mud = views.Single(v => v.Kind == MechanicKind.Mud);
            Assert.That(lightView.transform.Find("Green + stop line").gameObject.activeSelf, Is.True);
            session.Apply(new SolverMove(SolverMoveKind.RotatePylon, 0));
            board.RefreshMechanics(session.State);
            Assert.That(lightView.transform.Find("Red + stop line").gameObject.activeSelf, Is.True);
            Assert.That(Quaternion.Angle(pylonView.MovingPart.localRotation, Quaternion.Euler(0, -90, 0)), Is.LessThan(.001f));
            board.ConsumeMud(0);
            Assert.That(mud.gameObject.activeSelf, Is.False);
            board.SetPylon(0, -45); // Animation đang dở; Snap sau Undo phải về đúng trạng thái.
            board.Snap(before);
            Assert.That(mud.gameObject.activeSelf, Is.True);
            Assert.That(lightView.transform.Find("Green + stop line").gameObject.activeSelf, Is.True);
            Assert.That(Quaternion.Angle(pylonView.MovingPart.localRotation, Quaternion.identity), Is.LessThan(.001f));
            Assert.That(root.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TextMesh>() == null)
                .All(r => r.sharedMaterials.All(m => m == material)), Is.True);
            Assert.That(pylon.MovingPart.localRotation, Is.EqualTo(Quaternion.identity), "Không sửa prefab/catalog nguồn.");
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(4, 4)]
        [TestCase(9, 12)]
        public void SurfaceIsOneMeshWithGridAndCurbGapAtGate(int cols, int rows)
        {
            var builder = new LevelDefBuilder(cols, rows);
            var closed = LotSurfaceView.BuildMesh(builder.Build(), catalog.Settings);
            builder.Gates.Add(new GateDef(GateEdge.Top, 1, TileColor.Blue));
            var open = LotSurfaceView.BuildMesh(builder.Build(), catalog.Settings);
            try
            {
                Assert.That(open.subMeshCount, Is.EqualTo(3));
                Assert.That(open.GetTriangles(2).Length, Is.EqualTo(closed.GetTriangles(2).Length - 36), "Một đoạn bó vỉa được bỏ ở cổng.");
                Assert.That(open.bounds.size.x, Is.EqualTo(cols + 2 * catalog.Settings.curbWidth).Within(.0001f));
                Assert.That(open.bounds.size.z, Is.EqualTo(rows + 2 * catalog.Settings.curbWidth).Within(.0001f));
                Assert.That(open.normals.All(n => n.sqrMagnitude > .9f), Is.True);
                Assert.That(open.GetTriangles(1).Length, Is.GreaterThan(0), "Có vạch ô trong cùng mesh.");
            }
            finally
            {
                Object.DestroyImmediate(closed);
                Object.DestroyImmediate(open);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void PylonEntranceLabelsFollowCoreReceivingFacesForAllFourTurns(int turns)
        {
            var surface = Template(MechanicKind.Surface);
            surface.gameObject.AddComponent<LotSurfaceView>();
            surface.GetComponent<Renderer>().sharedMaterials = new[] { material, material, material };
            var template = Template(MechanicKind.Pylon);
            var moving = new GameObject("Hai miệng nhận").transform;
            moving.SetParent(template.transform, false);
            template.Configure(MechanicKind.Pylon, moving);
            Template(MechanicKind.Portal);
            Template(MechanicKind.Portal);
            entries[entries.Count - 1].variant = 1; // 016: miệng B của trụ dùng màu cổng thứ hai
            SetEntries();
            var builder = new LevelDefBuilder(5, 5);
            var pylon = new PylonDef(2, 2, turns, new[] { new PortalFace(0, 1, Side.N), new PortalFace(4, 3, Side.S) });
            builder.Pylons.Add(pylon);
            var level = builder.Build();
            board.Build(level, GameState.Initial(level), palette, null, catalog);
            var view = root.GetComponentsInChildren<MechanicView>().Single(v => v.Kind == MechanicKind.Pylon);
            PylonRule.PylonFaces(pylon, turns, out var a, out var b);
            var center = board.Cell(2, 2, 0);
            var labelA = view.MovingPart.Find("1A");
            var labelB = view.MovingPart.Find("1B");
            Vector3 actualA = labelA.position - center, actualB = labelB.position - center;
            actualA.y = actualB.y = 0;
            Assert.That(Vector3.Dot(actualA.normalized, (board.Cell(a.R, a.C, 0) - center).normalized), Is.GreaterThan(.9999f));
            Assert.That(Vector3.Dot(actualB.normalized, (board.Cell(b.R, b.C, 0) - center).normalized), Is.GreaterThan(.9999f));
        }

        [Test]
        public void ToyPylonUsesOneColorPerMouthAndMatchingWallPortalsWithoutText()
        {
            var surface = Template(MechanicKind.Surface);
            surface.gameObject.AddComponent<LotSurfaceView>();
            surface.GetComponent<Renderer>().sharedMaterials = new[] { material, material, material };
            var template = Template(MechanicKind.Pylon);
            var moving = new GameObject("Moving").transform;
            moving.SetParent(template.transform, false);
            foreach (var name in new[] { "Face A", "Face B" })
            {
                var face = new GameObject(name, typeof(MeshRenderer));
                face.transform.SetParent(moving, false);
                face.GetComponent<Renderer>().sharedMaterial = material;
            }
            template.Configure(MechanicKind.Pylon, moving);
            for (int variant = 0; variant < 3; variant++)
            {
                var portal = Template(MechanicKind.Portal);
                portal.name = "Portal màu " + variant;
                var entry = entries[entries.Count - 1];
                entry.variant = variant;
                entries[entries.Count - 1] = entry;
            }
            SetEntries();
            var builder = new LevelDefBuilder(5, 5);
            builder.Pylons.Add(new PylonDef(2, 2, 0, new[] { new PortalFace(0, 1, Side.N), new PortalFace(4, 3, Side.S) }));
            var level = builder.Build();
            board.Build(level, GameState.Initial(level), palette, null, catalog);

            Assert.That(root.GetComponentsInChildren<TextMesh>(true).Where(label => label.text.StartsWith("1")), Is.Empty, "Không còn chữ 1A/1B");
            var view = root.GetComponentsInChildren<MechanicView>().Single(v => v.Kind == MechanicKind.Pylon);
            var block = new MaterialPropertyBlock();
            var colors = catalog.Settings.portalColors;
            view.MovingPart.Find("Face A").GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That((Vector4)block.GetColor("_Color"), Is.EqualTo((Vector4)colors[0]));
            view.MovingPart.Find("Face B").GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That((Vector4)block.GetColor("_Color"), Is.EqualTo((Vector4)colors[1]));
            var portals = root.GetComponentsInChildren<MechanicView>().Where(v => v.Kind == MechanicKind.Portal).ToArray();
            Assert.That(portals.Length, Is.EqualTo(2));
            Assert.That(portals.Single(v => v.name.StartsWith("Portal màu 0")).transform.localPosition.z, Is.GreaterThan(0), "Cổng nối miệng A ở mép Bắc");
            Assert.That(portals.Single(v => v.name.StartsWith("Portal màu 1")).transform.localPosition.z, Is.LessThan(0), "Cổng nối miệng B ở mép Nam");
        }

        [Test]
        public void OcclusionRequiresScreenOverlapAndAnObstacleInFrontAtSeventyDegrees()
        {
            var camera = new GameObject("Camera70").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.transform.rotation = Quaternion.Euler(70, 0, 0);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 1;
            var car = new Bounds(camera.transform.forward * 10, Vector3.one);
            var front = new Bounds(camera.transform.forward * 5, Vector3.one);
            Assert.That(MechanicOcclusion.CoversCar(camera, front, car, .03f), Is.True);
            var behind = new Bounds(camera.transform.forward * 15, Vector3.one);
            Assert.That(MechanicOcclusion.CoversCar(camera, behind, car, .03f), Is.False);
            var beside = new Bounds(front.center + camera.transform.right * 10, Vector3.one);
            Assert.That(MechanicOcclusion.CoversCar(camera, beside, car, .03f), Is.False);
            Assert.That(MechanicOcclusion.CoversCar(camera, new Bounds(-camera.transform.forward * 5, Vector3.one), car, .03f), Is.False);
        }

        [Test]
        public void OcclusionUsesSharedMaterialAssetsAndRestoresOnDisable()
        {
            var obj = new GameObject("Portal", typeof(MeshRenderer));
            obj.transform.SetParent(root.transform, false);
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            var faded = new Material(material);
            try
            {
                faded.color = new Color(1, 1, 1, .18f);
                var occlusion = obj.AddComponent<MechanicOcclusion>();
                // Test EditMode cần cho instance này nhận callback thật của Unity khi SetActive(false).
                occlusion.runInEditMode = true;
                occlusion.Configure(renderer, new[] { material }, new[] { faded });
                occlusion.SetFaded(true);
                Assert.That(renderer.sharedMaterial, Is.SameAs(faded));
                Assert.That(material.color.a, Is.EqualTo(1), "Material gốc không bị nhuộm/mờ theo instance.");
                occlusion.SetFaded(false);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                occlusion.SetFaded(true);
                obj.SetActive(false);
                Assert.That(occlusion.IsFaded, Is.False);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                obj.SetActive(true);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            }
            finally { Object.DestroyImmediate(faded); }
        }

        private MechanicView Template(MechanicKind kind)
        {
            var template = new GameObject(kind.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            template.transform.SetParent(templates.transform, false);
            template.GetComponent<Renderer>().sharedMaterial = material;
            var view = template.AddComponent<MechanicView>();
            view.Configure(kind);
            entries.Add(new MechanicCatalog.Entry { kind = kind, prefab = template });
            return view;
        }
        private void SetEntries()
        {
            typeof(MechanicCatalog).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(catalog, entries.ToArray());
        }
    }
}
