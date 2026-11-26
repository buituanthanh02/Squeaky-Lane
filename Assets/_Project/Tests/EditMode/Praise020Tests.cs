using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 020: chữ khen trên HUD dùng đúng theme thật, nằm trong màn, không chồng nhau, tự tắt và tái dùng pool.
    public sealed class Praise020Tests
    {
        private GameObject canvas;
        private RectTransform layer;
        private UiTheme theme;
        private FeelConfig feel;
        private PraisePopups popups;

        [SetUp]
        public void SetUp()
        {
            canvas = new GameObject("Canvas test", typeof(RectTransform), typeof(Canvas));
            layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(canvas.transform, false);
            layer.sizeDelta = new Vector2(1080, 1920);
            theme = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/_Project/Config/UiTheme.asset");
            feel = AssetDatabase.LoadAssetAtPath<FeelConfig>("Assets/_Project/Config/FeelConfig.asset");
            popups = new PraisePopups(layer, theme);
        }

        [TearDown]
        public void TearDown()
        {
            popups.Dispose();
            Object.DestroyImmediate(canvas);
        }

        // Font TMP dạng tĩnh: thiếu một chữ có dấu thì trên máy hiện ô vuông. Đọc qua SerializedObject để test không cần tham chiếu TMP.
        [Test]
        public void Theme_HasFontWithEveryPraiseCharacter()
        {
            var data = new SerializedObject(theme);
            var font = data.FindProperty("titleFont").objectReferenceValue;
            Assert.That(font, Is.Not.Null);
            Assert.That(data.FindProperty("hudOutline").objectReferenceValue, Is.Not.Null);
            var table = new SerializedObject(font).FindProperty("m_CharacterTable");
            var known = new HashSet<long>();
            for (int i = 0; i < table.arraySize; i++) { known.Add(table.GetArrayElementAtIndex(i).FindPropertyRelative("m_Unicode").longValue); }
            foreach (char character in "Sạch bóng! Bóng loáng! Thơm tho!") { Assert.That(known, Does.Contain((long)character), "thiếu chữ " + character); }
        }

        [Test]
        public void ShowsAboveHud_ThenHidesAfterLifetime()
        {
            popups.Show("Sạch bóng!", Vector2.zero);
            Assert.That(popups.ActiveCount, Is.EqualTo(1));
            popups.Advance(feel.ComboPopSeconds, feel);
            var text = layer.Find("Praise 0/Text").GetComponent<Graphic>();
            Assert.That(new SerializedObject(text).FindProperty("m_text").stringValue, Is.EqualTo("Sạch bóng!"));
            foreach (var graphic in layer.GetComponentsInChildren<Graphic>()) { Assert.That(graphic.raycastTarget, Is.False, "chữ khen không được chặn chạm"); }
            popups.Advance(feel.PopupSeconds, feel);
            Assert.That(popups.ActiveCount, Is.Zero);
        }

        [Test]
        public void StaysInsideTheScreen_AndDoesNotStackOnEachOther()
        {
            var farOutside = new Vector2(-5000, -5000);
            popups.Show("Sạch bóng!", farOutside);
            popups.Show("Thơm tho!", farOutside);
            var first = (RectTransform)layer.GetChild(0);
            var second = (RectTransform)layer.GetChild(1);
            foreach (var item in new[] { first, second })
            {
                Assert.That(Mathf.Abs(item.anchoredPosition.x), Is.LessThan(layer.rect.width * .5f));
                Assert.That(Mathf.Abs(item.anchoredPosition.y), Is.LessThan(layer.rect.height * .5f));
            }

            Assert.That(Mathf.Abs(first.anchoredPosition.y - second.anchoredPosition.y), Is.GreaterThanOrEqualTo(theme.PraiseFontSize));
        }

        [Test]
        public void ReusesFourSlots_AndClearHidesAll()
        {
            for (int i = 0; i < 7; i++) { popups.Show("Sạch bóng!", Vector2.zero); }
            Assert.That(layer.childCount, Is.EqualTo(4));
            popups.Clear();
            Assert.That(popups.ActiveCount, Is.Zero);
        }
    }
}
