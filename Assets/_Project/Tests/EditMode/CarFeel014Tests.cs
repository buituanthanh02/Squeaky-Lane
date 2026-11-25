using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    // 014: bứt tốc và lắc khi đâm chỉ là số hình (FeelConfig), không chạm luật Core.
    public sealed class CarFeel014Tests
    {
        private FeelConfig feel;

        [SetUp]
        public void Setup() { feel = ScriptableObject.CreateInstance<FeelConfig>(); }

        [TearDown]
        public void Cleanup() { Object.DestroyImmediate(feel); }

        [Test]
        public void ZeroAccelKeepsOldEvenSlide()
        {
            Set("accelCells", 0f);
            for (int i = 0; i < 8; i++)
            {
                Assert.That(feel.StepDuration(i), Is.EqualTo(feel.StepSeconds).Within(1e-5f));
            }
        }

        [TestCase(.5f)]
        [TestCase(1.2f)]
        [TestCase(3f)]
        public void AccelerationStartsSlowThenReachesTopSpeed(float accel)
        {
            Set("accelCells", accel);
            float previous = float.MaxValue;
            for (int i = 0; i < 10; i++)
            {
                float step = feel.StepDuration(i);
                Assert.That(step, Is.GreaterThan(0));
                Assert.That(step, Is.LessThanOrEqualTo(previous + 1e-5f), "Ô sau không được chậm hơn ô trước");
                Assert.That(step, Is.GreaterThanOrEqualTo(feel.StepSeconds - 1e-5f), "Không vượt tốc độ tối đa");
                previous = step;
            }
            Assert.That(feel.StepDuration(0), Is.GreaterThan(feel.StepSeconds), "Ô đầu phải chậm hơn khi có bứt tốc");
            Assert.That(feel.StepDuration(Mathf.CeilToInt(accel) + 1), Is.EqualTo(feel.StepSeconds).Within(1e-5f));
        }

        [Test]
        public void WholeSlideCostsExactlyAccelCellsTimesStep()
        {
            Set("accelCells", 1.2f);
            float total = 0;
            for (int i = 0; i < 6; i++) { total += feel.StepDuration(i); }
            Assert.That(total, Is.EqualTo((6 + 1.2f) * feel.StepSeconds).Within(1e-4f));
            Assert.That(feel.TimeToCell(1.2f), Is.EqualTo(2 * 1.2f * feel.StepSeconds).Within(1e-5f), "Hai nhánh công thức khớp nhau ở mốc AccelCells");
        }

        [Test]
        public void WobbleIsBoundedAndSettles()
        {
            Assert.That(feel.CrashWobble(0), Is.EqualTo(0).Within(1e-5f));
            bool moved = false;
            for (int i = 0; i <= 200; i++)
            {
                float value = feel.CrashWobble(i * .005f);
                Assert.That(float.IsNaN(value), Is.False);
                Assert.That(value, Is.InRange(-1f, 1f));
                moved |= Mathf.Abs(value) > .2f;
            }
            Assert.That(moved, Is.True, "Lắc phải đủ thấy");
            Assert.That(feel.CrashWobble(10), Is.Zero);
            Assert.That(feel.CrashWobble(-1), Is.Zero);
        }

        private void Set(string field, object value)
        {
            typeof(FeelConfig).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(feel, value);
        }
    }
}
