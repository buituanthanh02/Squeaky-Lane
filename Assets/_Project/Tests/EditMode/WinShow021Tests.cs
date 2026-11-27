using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 021: màn trình diễn thắng dùng theme thật: đủ hình, đúng thứ tự, bỏ qua được, chừa nền tối sau bảng và tái dùng object.
    public sealed class WinShow021Tests
    {
        private GameObject canvas;
        private RectTransform layer;
        private UiTheme theme;
        private FeelConfig feel;
        private WinShow show;

        [SetUp]
        public void SetUp()
        {
            canvas = new GameObject("Canvas test", typeof(RectTransform), typeof(Canvas));
            layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(canvas.transform, false);
            layer.sizeDelta = new Vector2(1080, 2340);
            new GameObject("Dialog", typeof(RectTransform)).transform.SetParent(layer, false);
            theme = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/_Project/Config/UiTheme.asset");
            feel = AssetDatabase.LoadAssetAtPath<FeelConfig>("Assets/_Project/Config/FeelConfig.asset");
            show = new WinShow(layer, 0, theme, feel);
        }

        [TearDown]
        public void TearDown()
        {
            show.Dispose();
            Object.DestroyImmediate(canvas);
        }

        [Test]
        public void Timeline_LettersInOrder_ThenWord_ThenAllFireworksEndBeforeDialog()
        {
            for (int i = 1; i < 7; i++)
            {
                Assert.That(WinShowTimeline.LetterStart(feel, i), Is.GreaterThan(WinShowTimeline.LetterStart(feel, i - 1)));
            }

            Assert.That(WinShowTimeline.WordStart(feel, 7), Is.GreaterThan(WinShowTimeline.LetterStart(feel, 6)));
            float lastEnd = WinShowTimeline.BurstStart(feel, theme.WinFireworkSpotCount - 1) + feel.WinRocketSeconds + feel.WinSparkSeconds;
            Assert.That(lastEnd, Is.LessThanOrEqualTo(feel.WinShowSeconds + .001f));
            Assert.That(feel.WinShowSkipAfter, Is.LessThan(feel.WinShowSeconds));
            Assert.That(WinShowTimeline.Slam(1, 2), Is.EqualTo(1));
            Assert.That(WinShowTimeline.Slam(0, 2), Is.EqualTo(2));
        }

        [Test]
        public void Theme_HasEveryLogoPiece()
        {
            Assert.That(theme.HasWinShow, Is.True, "chạy menu 021 UI/Setup Win Logo");
            var layout = JsonUtility.FromJson<WinShow.Layout>(theme.WinLogoLayout.text);
            int letters = 0;
            foreach (var piece in layout.pieces)
            {
                var sprite = theme.WinLogoSprite(piece.sprite);
                Assert.That(sprite, Is.Not.Null, piece.sprite);
                Assert.That(new[] { "back", "letter", "word", "deco" }, Does.Contain(piece.role), piece.sprite);
                if (piece.role == "letter") { letters++; }
            }

            Assert.That(letters, Is.EqualTo("SQUEAKY".Length));
            Assert.That(theme.WinFxDot, Is.Not.Null);
            Assert.That(theme.WinFxTrail, Is.Not.Null);
        }

        [Test]
        public void Plays_BlocksTouch_ThenDismissKeepsOnlyTheBackdrop()
        {
            show.Play();
            Assert.That(show.Playing, Is.True);
            var root = (RectTransform)layer.Find("021 WinShow");
            Assert.That(root.GetSiblingIndex(), Is.Zero, "phải nằm dưới bảng thắng");
            var group = root.GetComponent<CanvasGroup>();
            Assert.That(group.blocksRaycasts, Is.True);
            foreach (var image in root.Find("Show/Logo").GetComponentsInChildren<Image>()) { Assert.That(image.raycastTarget, Is.False); }

            show.Advance(feel.WinShowSeconds + 1);
            Assert.That(show.Playing, Is.False);
            Assert.That(show.Visible, Is.True, "giữ nền tối tới khi bảng thắng mở");
            show.Dismiss();
            Assert.That(group.blocksRaycasts, Is.False);
            show.Advance(feel.WinShowFadeSeconds + .01f);
            Assert.That(show.Visible, Is.True, "nền tối ở lại sau bảng thắng");
            Assert.That(root.Find("Show").GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(root.Find("Dim").GetComponent<Image>().color.a, Is.EqualTo(theme.WinBackdropAlpha).Within(.001f));
            show.Hide();
            Assert.That(show.Visible, Is.False);
            Assert.That(root.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Skip_IgnoredTooEarly_ThenJumpsToEnd()
        {
            show.Play();
            show.Skip();
            Assert.That(show.Playing, Is.True);
            show.Advance(feel.WinShowSkipAfter + .01f);
            show.Skip();
            Assert.That(show.Playing, Is.False);
            var logo = layer.Find("021 WinShow/Show/Logo");
            foreach (Transform piece in logo) { Assert.That(piece.GetComponent<Image>().color.a, Is.EqualTo(1).Within(.001f), piece.name); }
        }

        [Test]
        public void SecondPlay_ReusesSameObjects()
        {
            show.Play();
            int children = layer.Find("021 WinShow/Show").childCount;
            show.Hide();
            show.Play();
            Assert.That(layer.childCount, Is.EqualTo(2));
            Assert.That(layer.Find("021 WinShow/Show").childCount, Is.EqualTo(children));
            Assert.That(show.Age, Is.Zero);
        }
    }
}
