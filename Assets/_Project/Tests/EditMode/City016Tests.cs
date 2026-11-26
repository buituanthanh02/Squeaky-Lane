using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 016: khu phố sinh theo màn phải nằm ngoài bãi, thấp ở phía nam, không đè thảm hầm rửa và trong ngân sách tam giác.
    public sealed class City016Tests
    {
        [TestCase(4, 5)]
        [TestCase(7, 8)]
        public void SceneryStaysOutsideLotLowInSouthAndWithinBudget(int cols, int rows)
        {
            var builder = new LevelDefBuilder(cols, rows);
            builder.Gates.Add(new GateDef(GateEdge.Left, 1, TileColor.Water));
            builder.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink));
            var city = CityBuilder.Build(builder.Build(), .8f, .95f);
            try
            {
                int triangles = city.Ground.triangles.Length / 3;
                float padZ = (rows - 1) * .5f - 1, padNear = cols * .5f;
                var solids = new[] { city.Near, city.North, city.South, city.West, city.East };
                foreach (var mesh in solids)
                {
                    triangles += mesh.triangles.Length / 3;
                    foreach (var v in mesh.vertices)
                    {
                        Assert.That(Mathf.Abs(v.x) > cols * .5f + .75f || Mathf.Abs(v.z) > rows * .5f + .75f, Is.True, mesh.name + " đè vào bãi: " + v);
                        if (v.z < -rows * .5f) { Assert.That(v.y - CityBuilder.Ground, Is.LessThan(1.35f), mesh.name + " phía nam quá cao: " + v); }
                        bool onPad = v.x < -padNear - .2f && v.x > -padNear - 1.9f && Mathf.Abs(v.z - padZ) < .5f;
                        Assert.That(onPad, Is.False, mesh.name + " đè thảm hầm rửa: " + v);
                    }
                }
                Assert.That(triangles, Is.LessThan(150000));
                Assert.That(city.North.vertexCount, Is.GreaterThan(0));
                Assert.That(city.West.vertexCount, Is.GreaterThan(0), "Máy rộng thấy hai bên phải có cảnh");
            }
            finally
            {
                foreach (var mesh in new[] { city.Ground, city.Near, city.North, city.South, city.West, city.East }) { Object.DestroyImmediate(mesh); }
            }
        }

        [Test]
        public void ThemeViewBuildsCityAndKeepsGroundWhenCameraFits()
        {
            var theme = ScriptableObject.CreateInstance<UiTheme>();
            var material = new Material(Shader.Find("Standard"));
            var root = new GameObject("city theme", typeof(SceneThemeView));
            var cameraObject = new GameObject("camera", typeof(Camera));
            try
            {
                Set(theme, "cityEnabled", true);
                Set(theme, "cityGroundMaterial", material);
                Set(theme, "cityPropMaterial", material);
                Assert.That(theme.StreetsEnabled, Is.True, "Bật khu phố thì xe ra cổng chạy theo phố");
                var view = root.GetComponent<SceneThemeView>();
                var level = new LevelDefBuilder(5, 6).Build();
                view.Configure(level, theme);
                view.Configure(level, theme);
                Assert.That(root.transform.childCount, Is.EqualTo(5), "Cụm gần + 4 vùng xa, không nhân đôi khi dựng lại");
                var before = root.GetComponent<MeshFilter>().sharedMesh.vertices;
                view.EnsureGroundCoverage(cameraObject.GetComponent<Camera>());
                Assert.That(root.GetComponent<MeshFilter>().sharedMesh.vertices, Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(theme); Object.DestroyImmediate(material);
            }
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
