using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class LevelJsonLoaderTests
    {
        [TestCaseSource(typeof(GoldenFiles), nameof(GoldenFiles.LevelNumbers))]
        public void GoldenLevel_Loads(int id)
        {
            var level = LevelJsonLoader.Load(File.ReadAllText(GoldenFiles.Level(id)), $"level_{id:00}.json");
            Assert.That(level.Cols, Is.GreaterThan(0)); Assert.That(level.Rows, Is.GreaterThan(0));
            Assert.That(GameState.Initial(level).CarCount, Is.EqualTo(level.Cars.Count));
        }

        [TestCase("pink", TileColor.Pink)] [TestCase("blue", TileColor.Blue)]
        [TestCase("yellow", TileColor.Yellow)] [TestCase("green", TileColor.Green)]
        [TestCase("orange", TileColor.Orange)] [TestCase("purple", TileColor.Purple)]
        public void CarKeys_MapToCorrectTypes(string key, TileColor expected)
        {
            var level = LevelJsonLoader.Load(Board(@"""cars"":[{""axis"":""v"",""r"":1,""c"":2,""color"":""COLOR"",""len"":2}]".Replace("COLOR", key)), "màu xe");
            Assert.That(level.Cars[0].Color, Is.EqualTo(expected)); Assert.That(level.Cars[0].Axis, Is.EqualTo(Axis.Vertical));
            Assert.That(level.Cars[0].R, Is.EqualTo(1)); Assert.That(level.Cars[0].C, Is.EqualTo(2));
            Assert.That(level.Cars[0].Mud, Is.Zero); Assert.That(level.Cars[0].Truck, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void OptionalCollections_AreEmptyWhenAbsentOrNull(bool explicitNull)
        {
            string fields = explicitNull ? @"""cars"":null,""gates"":null,""muds"":null,""pylons"":null,""switches"":null" : @"""cars"":[]";
            var level = LevelJsonLoader.Load(Board(fields), "mảng tùy chọn");
            Assert.That(level.Cars, Is.Empty); Assert.That(level.Gates, Is.Empty); Assert.That(level.Cones, Is.Empty);
            Assert.That(level.Muds, Is.Empty); Assert.That(level.Sprays, Is.Empty); Assert.That(level.Tables, Is.Empty);
            Assert.That(level.Paints, Is.Empty); Assert.That(level.Lights, Is.Empty); Assert.That(level.Barriers, Is.Empty);
            Assert.That(level.Portals, Is.Empty); Assert.That(level.Pylons, Is.Empty); Assert.That(level.Switches, Is.Empty);
            Assert.That(level.Splash, Is.False);
        }

        [TestCase("h", 0)] [TestCase("v", 1)]
        public void PylonOrientation_IsMappedAndFixedIsPreserved(string orientation, int turns)
        {
            string fields = @"""pylons"":[{""r"":1,""c"":1,""o"":""ORIENT"",""fixed"":true,""to"":[{""r"":0,""c"":0,""side"":""N""},{""r"":3,""c"":3,""side"":""S""}]}]";
            var level = LevelJsonLoader.Load(Board(fields.Replace("ORIENT", orientation)), "trụ");
            Assert.That(level.Pylons[0].Turns, Is.EqualTo(turns)); Assert.That(level.Pylons[0].Fixed, Is.True);
            Assert.That(level.Pylons[0].To[0].Side, Is.EqualTo(Side.N)); Assert.That(level.Pylons[0].To[1].Side, Is.EqualTo(Side.S));
        }

        [Test]
        public void GateWater_IsAllowedAndEdgeKeysAreMapped()
        {
            string fields = @"""gates"":[{""side"":""t"",""i"":1,""color"":""water""},{""side"":""b"",""i"":2,""color"":""pink""},{""side"":""l"",""i"":0,""color"":""blue""},{""side"":""r"",""i"":3,""color"":""yellow""}]";
            var level = LevelJsonLoader.Load(Board(fields), "cổng");
            Assert.That(level.Gates[0].Edge, Is.EqualTo(GateEdge.Top)); Assert.That(level.Gates[0].Color, Is.EqualTo(TileColor.Water));
            Assert.That(level.Gates[1].Edge, Is.EqualTo(GateEdge.Bottom)); Assert.That(level.Gates[2].Edge, Is.EqualTo(GateEdge.Left));
            Assert.That(level.Gates[3].Edge, Is.EqualTo(GateEdge.Right));
        }

        [TestCaseSource(nameof(BrokenLevels))]
        public void InvalidData_ReportsSourceAndReason(string json, string reason)
        {
            const string source = "màn-hỏng-cố-ý.json";
            var error = Assert.Throws<FormatException>(() => LevelJsonLoader.Load(json, source));
            Assert.That(error.Message, Does.Contain(source)); Assert.That(error.Message, Does.Contain(reason));
            Assert.That(error.InnerException, Is.Not.Null);
        }

        public static IEnumerable<TestCaseData> BrokenLevels()
        {
            yield return Bad("", "rỗng", "Empty");
            yield return Bad("{\"cols\":4,", "JSON", "Malformed");
            yield return Bad("{}", "kích thước", "DimensionsMissing");
            const string car = @"""cars"":[{""axis"":""h"",""r"":0,""c"":0,""color"":""pink"",""len"":2}]";
            yield return Bad(Board(car.Replace("pink", "red")), "color", "UnknownColor");
            yield return Bad(Board(car.Replace("pink", "water")), "color", "WaterCar");
            yield return Bad(Board(car.Replace("\"h\"", "\"diagonal\"")), "axis", "UnknownAxis");
            yield return Bad(Board(car.Replace("\"len\":2", "\"len\":0")), "len", "ZeroLength");
            yield return Bad(Board(car.Replace(",\"len\":2", "")), "len", "LengthMissing");
            yield return Bad(Board(car.Replace("\"len\":2", "\"len\":2,\"truck\":true")), "Xe tải", "ShortTruck");
            yield return Bad(Board(car.Replace("\"len\":2", "\"len\":2,\"mud\":-1")), "mud", "NegativeMud");
            yield return Bad(Board(car.Replace("\"c\":0", "\"c\":3")), "ngoài bãi", "CarOutside");
            yield return Bad(Board(@"""cars"":[{""axis"":""h"",""r"":0,""c"":0,""color"":""pink"",""len"":2},{""axis"":""v"",""r"":0,""c"":1,""color"":""blue"",""len"":2}]"), "chồng", "OverlappingCars");
            yield return Bad(Board(@"""gates"":[{""side"":""N"",""i"":0,""color"":""pink""}]"), "side", "UnknownGateEdge");
            yield return Bad(Board(@"""gates"":[{""side"":""t"",""i"":4,""color"":""pink""}]"), "Cổng", "GateOutside");
            yield return Bad(Board(@"""cones"":[{""r"":4,""c"":0}]"), "cones", "ConeOutside");
            yield return Bad(Board(@"""tables"":[{""r"":3,""c"":3}]"), "ngoài bãi", "TableOutside");
            yield return Bad(Board(@"""paints"":[{""r"":0,""c"":0,""color"":""water""}]"), "color", "WaterPaint");
            yield return Bad(Board(@"""lights"":[{""r"":0,""c"":0,""phase"":2}]"), "Phase", "InvalidLightPhase");
            yield return Bad(Board(@"""barriers"":[{""r"":0,""c"":0,""need"":-1}]"), "need", "InvalidNeed");
            yield return Bad(Board(@"""switches"":[{""r"":0,""c"":0,""a"":[{""r"":0,""c"":4}]}]"), "switches", "SwitchOutside");
            yield return Bad(Board(@"""portals"":[{""a"":{""r"":0,""c"":0,""side"":""n""},""b"":{""r"":3,""c"":3,""side"":""S""}}]"), "side", "UnknownFaceSide");
            yield return Bad(Board(@"""pylons"":[{""r"":1,""c"":1,""o"":""h"",""to"":[]}]"), "to", "PylonMissingTargets");
            yield return Bad(Board(@"""cars"":[null]"), "cars", "NullCar");
        }

        private static TestCaseData Bad(string json, string reason, string name) => new TestCaseData(json, reason).SetName("InvalidData_" + name);
        private static string Board(string fields) => "{\"cols\":4,\"rows\":4," + fields + "}";
    }
}
