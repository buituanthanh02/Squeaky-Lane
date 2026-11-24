using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class LevelSetupTests
    {
        [Test]
        public void TutorialUsesPositionAndOverridesExplicitMetadata()
        {
            var info = new LevelInfo("Học chơi", 8, 90, 2);
            var tutorial = Setup(info, 1, 2);
            Assert.That(tutorial.IsTutorial, Is.True);
            Assert.That(tutorial.TimeLimit, Is.Zero);
            Assert.That(tutorial.Hints, Is.EqualTo(-1));
            var next = Setup(info, 2, 2);
            Assert.That(next.IsTutorial, Is.False);
            Assert.That(next.TimeLimit, Is.EqualTo(90));
            Assert.That(next.Hints, Is.EqualTo(2));
        }

        [TestCase(4, 40)]
        [TestCase(0, 20)]
        [TestCase(-1, 120)]
        public void MissingOverridesUseFormulaAndFallbackMinimum(int minimum, float seconds)
        {
            var setup = Setup(new LevelInfo("Màn", minimum));
            Assert.That(setup.TimeLimit, Is.EqualTo(seconds));
            Assert.That(setup.Hints, Is.EqualTo(3));
        }

        [TestCase(0, 0)]
        [TestCase(17, -1)]
        [TestCase(50, 2)]
        public void ExplicitOverridesPreserveZeroAndUnlimitedHints(float seconds, int hints)
        {
            var setup = Setup(new LevelInfo("Màn", 8, seconds, hints));
            Assert.That(setup.TimeLimit, Is.EqualTo(seconds));
            Assert.That(setup.Hints, Is.EqualTo(hints));
        }

        [Test]
        public void MetadataDistinguishesMissingFromZeroAndIgnoresFieldNamesInValues()
        {
            var missing = LevelJsonLoader.LoadInfo("{\"name\":\"timeLimit\"}");
            Assert.That(missing.Name, Is.EqualTo("timeLimit"));
            Assert.That(missing.MinMoves, Is.EqualTo(-1));
            Assert.That(missing.TimeLimit, Is.Null);
            Assert.That(missing.Hints, Is.Null);
            var zero = LevelJsonLoader.LoadInfo("{\"name\":\"hints\",\"timeLimit\":0,\"hints\":0,\"minMoves\":0}");
            Assert.That(zero.MinMoves, Is.Zero);
            Assert.That(zero.TimeLimit, Is.EqualTo(0));
            Assert.That(zero.Hints, Is.EqualTo(0));
            Assert.That(Setup(missing).TimeLimit, Is.EqualTo(120));
            Assert.That(Setup(zero).TimeLimit, Is.Zero);
        }

        [TestCaseSource(typeof(GoldenFiles), nameof(GoldenFiles.LevelNumbers))]
        public void CombinedLoadReturnsTheSameMetadataAsInfoLoad(int id)
        {
            string json = File.ReadAllText(GoldenFiles.Level(id));
            var level = LevelJsonLoader.Load(json, "golden", out var combined);
            var info = LevelJsonLoader.LoadInfo(json);
            Assert.That(level.Cars.Count, Is.GreaterThan(0));
            Assert.That(combined.Name, Is.EqualTo(info.Name));
            Assert.That(combined.MinMoves, Is.EqualTo(info.MinMoves));
            Assert.That(combined.TimeLimit, Is.EqualTo(info.TimeLimit));
            Assert.That(combined.Hints, Is.EqualTo(info.Hints));
        }

        // So với đúng công thức/DTO trước refactor và config asset thật; không ghi asset khi test.
        [TestCaseSource(nameof(RuntimePositions))]
        public void EveryRuntimeLevelKeepsItsPreviousTimeAndHints(int position)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Config/GameConfig.asset");
            Assert.That(config, Is.Not.Null);
            var order = ReadOrder();
            int id = order.ids[position];
            string json = File.ReadAllText(RuntimeFolder + "/level_" + id.ToString("00") + ".json");
            var old = JsonUtility.FromJson<PreviousMetadata>(json);
            bool tutorial = position < config.TutorialLevelsUntimed;
            float previousTime = tutorial ? 0 : json.Contains("\"timeLimit\"") ? old.timeLimit : config.BaseSeconds + config.SecondsPerMove * (old.minMoves >= 0 ? old.minMoves : config.UnknownMinimumMoves);
            int previousHints = tutorial ? -1 : json.Contains("\"hints\"") ? old.hints : config.HintsPerLevel;
            var setup = new LevelSetup(LevelJsonLoader.LoadInfo(json), position, config.TutorialLevelsUntimed, config.BaseSeconds, config.SecondsPerMove, config.UnknownMinimumMoves, config.HintsPerLevel);
            Assert.That(setup.TimeLimit, Is.EqualTo(previousTime), "timeLimit màn " + id);
            Assert.That(setup.Hints, Is.EqualTo(previousHints), "hints màn " + id);
            Assert.That(setup.IsTutorial, Is.EqualTo(tutorial));
            TestContext.WriteLine($"Màn {id:00} | giờ {previousTime} → {setup.TimeLimit} | gợi ý {previousHints} → {setup.Hints}");
        }

        private const string RuntimeFolder = "Assets/_Project/Resources/Levels";
        private static IEnumerable<int> RuntimePositions()
        {
            int count = ReadOrder().ids.Length;
            for (int i = 0; i < count; i++)
            {
                yield return i;
            }
        }

        private static Order ReadOrder()
        {
            return JsonUtility.FromJson<Order>(File.ReadAllText(RuntimeFolder + "/level_order.json"));
        }

        private static LevelSetup Setup(LevelInfo info, int position = 0, int tutorials = 0)
        {
            return new LevelSetup(info, position, tutorials, 20, 5, 20, 3);
        }

        [Serializable]
        private sealed class Order
        {
            public int[] ids = null;
        }

        [Serializable]
        private sealed class PreviousMetadata
        {
            public int minMoves = -1;
            public float timeLimit = 0;
            public int hints = 0;
        }
    }
}
