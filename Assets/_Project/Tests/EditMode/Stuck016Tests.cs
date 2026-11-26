using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 016: kẹt xe báo bằng viền đỏ nhấp nháy thay cho chữ; không gán viền thì vẫn hiện bảng chữ như cũ.
    public sealed class Stuck016Tests
    {
        private GameObject host, bannerObject, edgeObject;
        private NavigationView view;
        private CanvasGroup banner, edge;

        [SetUp]
        public void Setup()
        {
            host = new GameObject("nav");
            bannerObject = new GameObject("banner");
            edgeObject = new GameObject("edge");
            view = host.AddComponent<NavigationView>();
            banner = bannerObject.AddComponent<CanvasGroup>();
            edge = edgeObject.AddComponent<CanvasGroup>();
            banner.alpha = 0; edge.alpha = 0;
            Set("stuckBanner", banner);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(host); Object.DestroyImmediate(bannerObject); Object.DestroyImmediate(edgeObject);
        }

        [Test]
        public void EdgePulsesInsteadOfShowingTheTextBanner()
        {
            Set("stuckEdge", edge);
            Assert.That(view.HasStuckEdge, Is.True);
            view.ShowStuck(true);
            Assert.That(banner.alpha, Is.Zero, "Không hiện chữ kẹt xe nữa");
            float high = edge.alpha;
            Assert.That(high, Is.GreaterThan(.8f));
            view.TickStuck(view.StuckPeriod * .5f);
            Assert.That(edge.alpha, Is.LessThan(high - .3f), "Nhấp nháy: nửa chu kỳ sau mờ đi rõ");
            view.ShowStuck(true); // gọi lặp không đặt lại nhịp
            Assert.That(edge.alpha, Is.LessThan(high - .3f));
            view.TickStuck(view.StuckPeriod * .5f);
            Assert.That(edge.alpha, Is.EqualTo(high).Within(.001f));
            Assert.That(edge.blocksRaycasts, Is.False, "Viền không được chặn chạm");
            view.ShowStuck(false);
            view.TickStuck(.3f);
            Assert.That(edge.alpha, Is.Zero);
            Assert.That(view.StuckPeriod, Is.GreaterThanOrEqualTo(.5f), "Không nháy quá 2 lần/giây");
        }

        [Test]
        public void WithoutEdgeTheBannerStillShows()
        {
            view.ShowStuck(true);
            Assert.That(banner.alpha, Is.EqualTo(1));
            view.ShowStuck(false);
            Assert.That(banner.alpha, Is.Zero);
        }

        private void Set(string field, object value)
        {
            typeof(NavigationView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, value);
        }
    }
}
