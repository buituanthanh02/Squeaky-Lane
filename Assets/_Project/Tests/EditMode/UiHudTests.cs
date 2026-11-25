using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class UiHudTests
    {
        private GameObject root;
        private UiTheme theme;
        private Material preset;
        private GameConfig config;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("HUD test", typeof(RectTransform));
            theme = ScriptableObject.CreateInstance<UiTheme>();
            config = ScriptableObject.CreateInstance<GameConfig>();
            preset = new Material(Shader.Find("UI/Default"));
            Set(theme, "hudOutline", preset);
            Set(theme, "hudSmall", preset);
            Set(theme, "cardText", preset);
        }
        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
            Object.DestroyImmediate(preset);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PressRestoresOriginalTransformAfterExitRepeatedDownAndDisable()
        {
            var button = root.AddComponent<Button>();
            var visual = Child("visual").GetComponent<RectTransform>();
            visual.anchoredPosition = new Vector2(12, 34);
            visual.localScale = new Vector3(1.2f, .9f, 1);
            var position = visual.anchoredPosition;
            var scale = visual.localScale;
            var press = root.AddComponent<UiButtonPress>();
            press.Configure(theme, visual);
            Invoke(press, "Awake");
            press.OnPointerDown(null);
            Assert.That(visual.localScale.x, Is.EqualTo(scale.x * theme.PressScale).Within(.0001f));
            Assert.That(visual.anchoredPosition.y, Is.LessThan(position.y));
            press.OnPointerDown(null);
            press.OnPointerExit(null);
            Assert.That(visual.anchoredPosition, Is.EqualTo(position));
            Assert.That(visual.localScale, Is.EqualTo(scale));
            press.OnPointerDown(null);
            Invoke(press, "OnDisable");
            Assert.That(visual.anchoredPosition, Is.EqualTo(position));
            Assert.That(visual.localScale, Is.EqualTo(scale));
            button.interactable = false;
            press.OnPointerDown(null);
            Assert.That(visual.anchoredPosition, Is.EqualTo(position), "Nút hết lượt không được nhún khi nhận pointer.");
        }

        [Test]
        public void RepeatedMessageRestartsBubbleButHudTicksDoNotKeepItAlive()
        {
            var nav = MakeHud();
            var bubble = (CanvasGroup)Field(nav, "messageBubble").GetValue(nav);
            nav.ShowMessage("Hướng này đang bị chặn");
            Invoke(nav, "TickMessage", theme.MessageHoldSeconds + theme.MessageFadeSeconds * .5f);
            Assert.That(bubble.alpha, Is.EqualTo(.5f).Within(.001f));
            Refresh(nav, new LevelTimer(60, 3, 5, .5f, .2f), 3);
            Invoke(nav, "TickMessage", theme.MessageFadeSeconds);
            Assert.That(bubble.alpha, Is.Zero, "RefreshHud 10 Hz không được kéo dài thông báo.");
            nav.ShowMessage("Hướng này đang bị chặn");
            Assert.That(bubble.alpha, Is.EqualTo(1), "Cùng một lỗi ở lần chạm sau vẫn phải hiện.");
            Assert.That(bubble.blocksRaycasts, Is.False);
            nav.ShowMessage("");
            Assert.That(bubble.alpha, Is.Zero);
        }

        [Test]
        public void ClockShowsMinutesAndSwapsPillSpriteWhenLowOrUntimed()
        {
            var textures = new System.Collections.Generic.List<Object>();
            Sprite MakeSprite()
            {
                var texture = new Texture2D(4, 4);
                var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
                textures.Add(texture);
                textures.Add(sprite);
                return sprite;
            }
            try
            {
                var normal = MakeSprite();
                var low = MakeSprite();
                Set(theme, "clockNormal", normal);
                Set(theme, "clockLow", low);
                var nav = MakeHud();
                var fill = (Image)Field(nav, "clockFill").GetValue(nav);
                var hint = (Button)Field(nav, "hintButton").GetValue(nav);
                var timer = new LevelTimer(10, 3, 5, .5f, .2f, 10);
                timer.Tick(9);
                Refresh(nav, timer, 0);
                Assert.That(GetText(nav, "clockText"), Is.EqualTo("00:01"), "Giờ hiển thị mm:ss, không còn thanh chạy.");
                Assert.That(fill.sprite, Is.SameAs(low), "Sắp hết giờ đổi sang pill đỏ.");
                Assert.That(fill.color, Is.EqualTo(Color.white), "Đổi sprite chứ không tint, tint sẽ nhuộm cả viền.");
                Assert.That(hint.interactable, Is.False);
                Assert.That(GetText(nav, "hintBadge"), Is.EqualTo("0"));
                Assert.That(GetText(nav, "levelTitle"), Is.EqualTo("Màn 30"), "ID màn không phải index+1, không hiện tên dài.");
                Assert.That(GetText(nav, "undoBadge"), Is.EqualTo("-" + config.UndoPenalty + "s"));
                timer.Tick(1);
                Refresh(nav, timer, 0);
                Assert.That(GetText(nav, "clockText"), Is.EqualTo("00:00"));
                Assert.That(fill.sprite, Is.SameAs(low));
                Refresh(nav, new LevelTimer(125, 3, 5, .5f, .2f), 1);
                Assert.That(GetText(nav, "clockText"), Is.EqualTo("02:05"));
                Assert.That(fill.sprite, Is.SameAs(normal), "Đủ giờ thì về pill thường.");
                Refresh(nav, new LevelTimer(0, 3, 5, .5f, .2f), -1);
                Assert.That(fill.sprite, Is.SameAs(normal), "Màn không tính giờ về pill thường.");
                Assert.That(hint.interactable, Is.True);
                Assert.That(GetText(nav, "hintBadge"), Is.EqualTo("∞"));
                Assert.That(GetText(nav, "clockText"), Is.EqualTo("∞"));
            }
            finally
            {
                foreach (var item in textures) { Object.DestroyImmediate(item); }
            }
        }

        [Test]
        public void HintSpriteAndBadgeSwapWhenExhaustedAndWonTextKeepsTitleOutOfBody()
        {
            var made = new System.Collections.Generic.List<Object>();
            Sprite MakeSprite()
            {
                var texture = new Texture2D(4, 4);
                var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
                made.Add(texture);
                made.Add(sprite);
                return sprite;
            }
            try
            {
                var on = MakeSprite();
                var off = MakeSprite();
                var badgeOn = MakeSprite();
                var badgeOff = MakeSprite();
                Set(theme, "hintOn", on);
                Set(theme, "hintOff", off);
                Set(theme, "badgeOn", badgeOn);
                Set(theme, "badgeOff", badgeOff);
                var nav = MakeHud();
                var hint = (Button)Field(nav, "hintButton").GetValue(nav);
                var image = hint.gameObject.AddComponent<Image>();
                hint.targetGraphic = image;
                var plate = (Image)Field(nav, "hintBadgePlate").GetValue(nav);
                var timer = new LevelTimer(60, 3, 5, .5f, .2f);
                Refresh(nav, timer, 0);
                Assert.That(image.sprite, Is.SameAs(off));
                Assert.That(plate.sprite, Is.SameAs(badgeOff));
                Refresh(nav, timer, 2);
                Assert.That(image.sprite, Is.SameAs(on));
                Assert.That(plate.sprite, Is.SameAs(badgeOn));
                Assert.That(GetText(nav, "wonText"), Does.StartWith("<sprite"), "Tiêu đề Thắng! nằm ở ribbon, thân chỉ có sao và giờ còn.");
                Assert.That(GetText(nav, "wonText"), Does.Contain("Màn 30"));
            }
            finally
            {
                foreach (var item in made) { Object.DestroyImmediate(item); }
            }
        }

        private NavigationView MakeHud()
        {
            var nav = root.AddComponent<NavigationView>();
            Set(nav, "theme", theme);
            Set(nav, "ids", new[] { 1, 30 });
            Set(nav, "playing", Child("playing").AddComponent<CanvasGroup>());
            Set(nav, "messageBubble", Child("bubble").AddComponent<CanvasGroup>());
            Set(nav, "clockFill", Child("fill").AddComponent<Image>());
            ((Image)Field(nav, "clockFill").GetValue(nav)).type = Image.Type.Sliced;
            Set(nav, "hintBadgePlate", Child("badge").AddComponent<Image>());
            Set(nav, "hintButton", Child("hint").AddComponent<Button>());
            Set(nav, "undoButton", Child("undo").AddComponent<Button>());
            Set(nav, "normalClock", theme.ClockColor);
            Set(nav, "lowClock", theme.LowClock);
            // Test assembly không thêm tham chiếu TMP/asmdef: lấy đúng loại component từ field đã serialize.
            foreach (string field in new[] { "clockText", "levelTitle", "movesText", "messageText", "wonText", "lostText", "undoLabel", "hintLabel", "nextLabel", "undoBadge", "hintBadge" })
            {
                var type = Field(nav, field).FieldType;
                var concrete = type.Assembly.GetType("TMPro.TextMeshProUGUI");
                Set(nav, field, Child(field).AddComponent(concrete));
            }
            return nav;
        }
        private void Refresh(NavigationView nav, LevelTimer timer, int hints)
        {
            nav.RefreshHud("Màn 30 · Tên màn dài", timer, 7, hints, "Hướng này đang bị chặn", true, true, 2, 1, config);
        }
        private GameObject Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child;
        }
        private static string GetText(NavigationView nav, string field)
        {
            var text = Field(nav, field).GetValue(nav);
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private static void Set(object target, string field, object value) { Field(target, field).SetValue(target, value); }
        private static void Invoke(object target, string method, params object[] arguments)
        { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments); }
    }
}
