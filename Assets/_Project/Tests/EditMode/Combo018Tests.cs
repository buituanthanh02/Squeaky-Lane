using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 018: kỷ lục combo theo màn, chữ Combo lúc chơi và bảng kết quả (thường / kỷ lục mới).
    public sealed class Combo018Tests
    {
        private sealed class Memory : IProgressStorage
        {
            public readonly Dictionary<string, int> Values = new Dictionary<string, int>();
            public int GetInt(string key, int fallback) => Values.TryGetValue(key, out int value) ? value : fallback;
            public void SetInt(string key, int value) { Values[key] = value; }
            public void Save() { }
        }

        private readonly List<GameObject> objects = new List<GameObject>();
        private UiTheme theme;
        private FeelConfig feel;

        [SetUp]
        public void Setup()
        {
            theme = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/_Project/Config/UiTheme.asset");
            feel = AssetDatabase.LoadAssetAtPath<FeelConfig>("Assets/_Project/Config/FeelConfig.asset");
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var item in objects) { Object.DestroyImmediate(item); }
            objects.Clear();
        }

        [Test]
        public void RecordIsPerLevelAndOnlyBeatenByABiggerCombo()
        {
            var store = new ProgressStore(new Memory());
            Assert.That(store.RecordCombo(3, 1), Is.False, "1 xe chưa phải combo");
            Assert.That(store.BestCombo(3), Is.Zero);
            Assert.That(store.RecordCombo(3, 3), Is.True);
            Assert.That(store.RecordCombo(3, 3), Is.False, "bằng kỷ lục không tính là mới");
            Assert.That(store.RecordCombo(3, 2), Is.False);
            Assert.That(store.BestCombo(3), Is.EqualTo(3));
            Assert.That(store.BestCombo(5), Is.Zero, "màn khác có kỷ lục riêng");
            Assert.That(store.RecordCombo(3, 4), Is.True);
        }

        [Test]
        public void ArtAndSoundsAreAssigned()
        {
            Assert.That(theme.ComboWord, Is.Not.Null);
            for (int i = 0; i < 10; i++) { Assert.That(theme.ComboDigit(i), Is.Not.Null, "chữ số " + i); }
            Assert.That(theme.RecordBadge, Is.Not.Null);
            var catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>("Assets/_Project/Config/SfxCatalog.asset");
            Assert.That(catalog.Find(SoundCue.RecordWhoosh), Is.Not.Null);
            Assert.That(catalog.Find(SoundCue.RecordSlam), Is.Not.Null);
            Assert.That(feel.RecordPulseSeconds, Is.GreaterThanOrEqualTo(.4f), "không chớp gắt");
        }

        [Test]
        public void PopupShowsFromTwoKeepsBestAndFadesAfterTheWindow()
        {
            var combo = Make<ComboFeedback>("combo");
            Set(combo, "theme", theme);
            Set(combo, "popupAnchor", (RectTransform)combo.transform);
            combo.Initialize(null, feel, null);
            combo.RegisterExit(1);
            Assert.That(combo.Showing, Is.Zero);
            combo.RegisterExit(2);
            combo.RegisterExit(12);
            combo.Advance(feel.ComboPopSeconds);
            Assert.That(combo.Showing, Is.EqualTo(12));
            combo.RegisterExit(1);
            Assert.That(combo.Best, Is.EqualTo(12), "chuỗi mới ngắn hơn không hạ combo cao nhất");
            combo.Advance(feel.ComboWindowSeconds + feel.ComboFadeSeconds + .01f);
            Assert.That(combo.Showing, Is.Zero);
            var store = new ProgressStore(new Memory());
            combo.FinishAttempt(store, 7);
            Assert.That(store.BestCombo(7), Is.EqualTo(12));
            combo.BeginAttempt();
            Assert.That(combo.Best, Is.Zero);
        }

        [Test]
        public void SummaryShowsRecordLineOrSlamsTheNewRecordAndRestoresTheCard()
        {
            var card = Make<RectTransform>("card");
            card.sizeDelta = new Vector2(850, 900);
            var buttons = Make<RectTransform>("buttons");
            buttons.SetParent(card, false);
            var summary = card.gameObject.AddComponent<ComboSummary>();
            Set(summary, "card", card);
            var screen = Make<RectTransform>("screen");
            screen.anchoredPosition = new Vector2(0, 12);
            Set(summary, "buttons", buttons);
            Set(summary, "shakeRoot", screen);
            summary.Configure(theme, feel);
            int whoosh = 0, slam = 0;
            summary.Whoosh += () => whoosh++;
            summary.Slam += () => slam++;

            summary.Show(2, 5, false);
            for (int i = 0; i < 60; i++) { summary.Advance(.05f); }
            Assert.That(summary.RecordText, Does.Contain("5"));
            Assert.That(summary.BadgeShown, Is.False);
            Assert.That(whoosh + slam, Is.Zero);
            float extra = theme.SummaryComboHeight + theme.SummaryLineHeight;
            Assert.That(card.sizeDelta.y, Is.EqualTo(900 + extra).Within(.01f));
            var row = card.Find("018 Combo kết quả");
            Assert.That(row.GetSiblingIndex(), Is.LessThan(buttons.GetSiblingIndex()), "khối combo nằm trên cụm nút");
            Assert.That(row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight, Is.EqualTo(extra).Within(.01f));

            summary.Show(6, 5, true);
            float shaken = 0;
            for (int i = 0; i < 60; i++)
            {
                summary.Advance(.05f);
                shaken = Mathf.Max(shaken, Vector2.Distance(screen.anchoredPosition, new Vector2(0, 12)));
            }

            Assert.That(shaken, Is.GreaterThan(5), "đóng huy hiệu thì giao diện rung");
            Assert.That(summary.BadgeShown, Is.True);
            Assert.That(summary.RecordText, Is.Empty);
            Assert.That(whoosh, Is.EqualTo(1));
            Assert.That(slam, Is.EqualTo(1));
            Assert.That(screen.anchoredPosition, Is.EqualTo(new Vector2(0, 12)), "hết rung thì về chỗ cũ");

            summary.Show(0, 0, false);
            Assert.That(summary.Visible, Is.False);
            Assert.That(card.sizeDelta.y, Is.EqualTo(900).Within(.01f));
            Assert.That(row.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ExitSoundGetsLouderAndAddsABiggerLayerAsTheComboGrows()
        {
            for (int combo = 2; combo <= 8; combo++)
            {
                Assert.That(feel.ComboGain(combo), Is.GreaterThan(feel.ComboGain(combo - 1)), "combo " + combo);
                Assert.That(feel.ComboLayerGain(combo), Is.GreaterThanOrEqualTo(feel.ComboLayerGain(combo - 1)));
            }

            Assert.That(feel.ComboGain(1), Is.EqualTo(1));
            Assert.That(feel.ComboGain(50), Is.EqualTo(feel.ComboGain(13)), "to tới trần rồi dừng");

            var player = Make<SfxPlayer>("sfx");
            var catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>("Assets/_Project/Config/SfxCatalog.asset");
            Set(player, "catalog", catalog);
            var store = new ProgressStore(new Memory());
            player.Configure(feel, store);
            player.PlayExit(1);
            float first = player.LiveVolume(SoundCue.Exit);
            Assert.That(player.LiveVoices(SoundCue.ComboUp) + player.LiveVoices(SoundCue.ComboBig), Is.Zero, "xe đầu chưa có lớp combo");
            player.StopAll();
            // Cooldown tính theo đồng hồ thật, EditMode không chạy: dùng một player mới cho mỗi mức.
            var second = Make<SfxPlayer>("sfx2");
            Set(second, "catalog", catalog);
            second.Configure(feel, store);
            second.PlayExit(2);
            Assert.That(second.LiveVolume(SoundCue.Exit), Is.GreaterThan(first));
            Assert.That(second.LiveVoices(SoundCue.ComboUp), Is.EqualTo(1));
            var third = Make<SfxPlayer>("sfx3");
            Set(third, "catalog", catalog);
            third.Configure(feel, store);
            third.PlayExit(feel.ComboBigFrom + 3);
            Assert.That(third.LiveVoices(SoundCue.ComboBig), Is.EqualTo(1));
            Assert.That(third.LiveVolume(SoundCue.ComboBig), Is.GreaterThan(second.LiveVolume(SoundCue.ComboUp)));
            Assert.That(third.LiveVolume(SoundCue.Exit), Is.LessThanOrEqualTo(1));
        }

        private T Make<T>(string name) where T : Component
        {
            var item = new GameObject(name, typeof(RectTransform));
            objects.Add(item);
            var existing = item.GetComponent<T>();
            return existing != null ? existing : item.AddComponent<T>();
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
