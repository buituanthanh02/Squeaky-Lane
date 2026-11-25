using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class SceneThemeTests
    {
        private GameObject root, cameraRoot;
        private UiTheme theme;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("013 Ground", typeof(SceneThemeView));
            cameraRoot = new GameObject("013 Camera", typeof(Camera));
            theme = ScriptableObject.CreateInstance<UiTheme>();
            typeof(UiTheme).GetField("propCount", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(theme, 0);
            typeof(UiTheme).GetField("gateStrips", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(theme, false);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(theme);
        }

        [TestCase(4, 4, 375f / 667)]
        [TestCase(4, 4, 428f / 926)]
        [TestCase(8, 8, 375f / 667)]
        [TestCase(8, 8, 428f / 926)]
        public void GrassCoversCameraCornersAfterFitAndResizeWithoutStretchingPavement(int cols, int rows, float aspect)
        {
            var view = root.GetComponent<SceneThemeView>();
            view.Configure(new LevelDefBuilder(cols, rows).Build(), theme);
            var mesh = root.GetComponent<MeshFilter>().sharedMesh;
            var pavementBefore = mesh.vertices;
            var camera = cameraRoot.GetComponent<Camera>();
            camera.orthographic = true; camera.aspect = aspect;
            camera.orthographicSize = (cols * .5f + 1) / (aspect * .9f);
            camera.transform.rotation = Quaternion.Euler(70, 0, 0);
            camera.transform.position = -camera.transform.forward * 25 - camera.transform.up * 1.4f;
            view.EnsureGroundCoverage(camera);
            AssertCornersOnGrass(camera, mesh.vertices);
            camera.aspect *= .85f; camera.orthographicSize *= 1.25f;
            view.EnsureGroundCoverage(camera);
            AssertCornersOnGrass(camera, mesh.vertices);
            var after = mesh.vertices;
            for (int i = 4; i < after.Length; i++) { Assert.That(after[i], Is.EqualTo(pavementBefore[i]), "Vỉa hè không được phóng theo camera."); }
            Assert.That(mesh.subMeshCount, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(root.transform.childCount, Is.Zero, "S1 không sinh props.");
        }

        private static void AssertCornersOnGrass(Camera camera, Vector3[] vertices)
        {
            var plane = new Plane(Vector3.up, vertices[0]);
            for (int i = 0; i < 4; i++)
            {
                var ray = camera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0));
                Assert.That(plane.Raycast(ray, out float distance), Is.True);
                var corner = ray.GetPoint(distance);
                Assert.That(corner.x, Is.InRange(vertices[0].x, vertices[2].x));
                Assert.That(corner.z, Is.InRange(vertices[0].z, vertices[2].z));
            }
        }
    }
}
