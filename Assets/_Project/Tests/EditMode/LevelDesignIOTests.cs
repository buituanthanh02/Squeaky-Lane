using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.EditorTools;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class LevelDesignIOTests
    {
        public static IEnumerable<string> LevelSources()
        {
            foreach (int id in GoldenFiles.LevelNumbers())
            {
                yield return GoldenFiles.Level(id);
            }

            foreach (string path in LevelDesignIO.ListSources())
            {
                yield return path;
            }
        }

        [TestCaseSource(nameof(LevelSources))]
        public void ReadSerializeReadPreservesGeometryAndOptionalMetadata(string path)
        {
            CheckRoundTrip(File.ReadAllText(path));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ZeroOverridesAndIndependentPresenceSurviveRoundTrip(bool time, bool hints)
        {
            string json = File.ReadAllText(GoldenFiles.Level(1));
            var data = LevelDesignIO.Read(json);
            data.timeLimit = 0;
            data.hints = 0;
            string saved = LevelDesignIO.Serialize(data, time, hints, json);
            CheckRoundTrip(saved);
            Assert.That(LevelDesignIO.HasField(saved, "timeLimit"), Is.EqualTo(time));
            Assert.That(LevelDesignIO.HasField(saved, "hints"), Is.EqualTo(hints));
        }

        [Test]
        public void ObjectFieldsKeepsBracesCommasEscapedQuotesAndNestedValuesInsideStrings()
        {
            const string json = "{\"name\":\"a{b,c\\\"d\",\"nested\":{\"text\":\"},[\"},\"array\":[1,{\"x\":\",\"}]}";
            var fields = LevelDesignIO.ObjectFields(json);
            Assert.That(fields.Count, Is.EqualTo(3));
            Assert.That(JsonUtility.FromJson<TextValue>("{\"value\":" + fields["name"] + "}").value, Is.EqualTo("a{b,c\"d"));
            Assert.That(fields["nested"], Is.EqualTo("{\"text\":\"},[\"}"));
            Assert.That(fields["array"], Is.EqualTo("[1,{\"x\":\",\"}]"));
        }

        [Test]
        public void ObjectFieldsAcceptsEmptyObject()
        {
            Assert.That(LevelDesignIO.ObjectFields(" \n{ }\t"), Is.Empty);
        }

        [Test]
        public void ObjectFieldsRejectsDuplicateKeysEvenWithDifferentEscaping()
        {
            Assert.Throws<FormatException>(() => LevelDesignIO.ObjectFields("{\"name\":1,\"name\":2}"));
            Assert.Throws<FormatException>(() => LevelDesignIO.ObjectFields("{\"name\":1,\"na\\u006de\":2}"));
        }

        [Test]
        public void ObjectFieldsRejectsTruncatedStringAndTrailingData()
        {
            Assert.Throws<FormatException>(() => LevelDesignIO.ObjectFields("{\"name\":\"abc"));
            Assert.Throws<FormatException>(() => LevelDesignIO.ObjectFields("{}{}"));
        }

        private static void CheckRoundTrip(string json)
        {
            var original = LevelDesignIO.Read(json);
            bool time = LevelDesignIO.HasField(json, "timeLimit");
            bool hints = LevelDesignIO.HasField(json, "hints");
            string saved = LevelDesignIO.Serialize(original, time, hints, json);
            var reread = LevelDesignIO.Read(saved);
            Assert.That(LevelDesignIO.Geometry(reread), Is.EqualTo(LevelDesignIO.Geometry(original)));
            Assert.That(LevelDesignIO.HasField(saved, "timeLimit"), Is.EqualTo(time));
            Assert.That(LevelDesignIO.HasField(saved, "hints"), Is.EqualTo(hints));
            Assert.That(reread.timeLimit, Is.EqualTo(original.timeLimit));
            Assert.That(reread.hints, Is.EqualTo(original.hints));
            Assert.That(reread.name, Is.EqualTo(original.name));
            Assert.That(reread.minMoves, Is.EqualTo(original.minMoves));
            Assert.That(reread.solveExhausted, Is.EqualTo(original.solveExhausted));
        }

        [Serializable]
        private sealed class TextValue
        {
            public string value = null;
        }
    }
}
