using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    internal static class GoldenFiles
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "golden"));
        internal static string Level(int id) => Path.Combine(Root, "levels", $"level_{id:00}.json");
        internal static string Trace(int id) => Path.Combine(Root, "traces", $"trace_{id:00}.json");
        public static IEnumerable<int> LevelNumbers() { for (int id = 1; id <= 31; id++) yield return id; }
    }

    public sealed class GoldenTraceTests
    {
        [TestCaseSource(typeof(GoldenFiles), nameof(GoldenFiles.LevelNumbers))]
        public void Replay_AllSequencesMatchPrototype(int id)
        {
            string json = File.ReadAllText(GoldenFiles.Level(id));
            var level = LevelJsonLoader.Load(json, $"level_{id:00}.json");
            var minimum = JsonUtility.FromJson<MinimumData>(json).minMoves;
            var trace = JsonUtility.FromJson<TraceData>(File.ReadAllText(GoldenFiles.Trace(id)));
            string root = $"màn {id}";
            Check(2, trace.schemaVersion, root + " · schemaVersion"); Check(id, trace.id, root + " · id");
            Check(minimum >= 0 ? 9 : 8, trace.sequences.Length, root + " · số chuỗi");
            int compared = 0;
            for (int sequence = 0; sequence < trace.sequences.Length; sequence++)
            {
                var expectedSequence = trace.sequences[sequence];
                string context = $"{root} · chuỗi {sequence + 1}";
                Check(sequence < 8 ? "random" : "solution", expectedSequence.kind, context + " · loại chuỗi");
                var state = GameState.Initial(level);
                var previous = trace.initialState;
                AssertState(previous, state, context + " · ban đầu");
                for (int step = 0; step < expectedSequence.moves.Length; step++)
                {
                    var expected = expectedSequence.moves[step];
                    string current = $"{context} · nước {step + 1}";
                    MoveResult result;
                    switch (expected.kind)
                    {
                        case "move": result = MoveResolver.Move(level, state, expected.car, expected.sign); break;
                        case "rot": result = TableRule.Rotate(level, state, expected.car); break;
                        case "pyl": result = PylonRule.RotatePylon(level, state, expected.car); break;
                        default: throw new AssertionException(current + " · loại lệnh lạ: " + expected.kind);
                    }
                    Check(expected.moved, result.Moved, current + " · moved");
                    Check(expected.exited, result.Exited, current + " · exited");
                    AssertEvents(expected.events ?? Array.Empty<EventData>(), result.Events, current);
                    AssertState(expected.state, result.State, current + " · state sau");
                    // Nếu Core sửa state đầu vào, Undo vẫn có thể sai dù kết quả cuối khớp.
                    AssertState(previous, state, current + " · state trước không đổi");
                    state = result.State; previous = expected.state; compared++;
                }
                if (expectedSequence.kind == "solution")
                {
                    Check(minimum, expectedSequence.moves.Length, context + " · độ dài lời giải");
                    Check(true, GameRules.IsWin(state), context + " · thắng");
                }
            }
            TestContext.WriteLine($"GOLDEN màn {id}: {trace.sequences.Length} chuỗi/{compared} nước khớp.");
        }

        [Test]
        public void Corpus_ContainsAllSequencesAndSteps()
        {
            int sequences = 0, steps = 0;
            foreach (int id in GoldenFiles.LevelNumbers())
            {
                var trace = JsonUtility.FromJson<TraceData>(File.ReadAllText(GoldenFiles.Trace(id)));
                sequences += trace.sequences.Length;
                foreach (var sequence in trace.sequences) steps += sequence.moves.Length;
            }
            Check(276, sequences, "corpus · tổng chuỗi"); Check(4754, steps, "corpus · tổng nước");
        }

        [Test]
        public void PushEffects_JsonKeysAndOrderMatchPrototypeFixture()
        {
            var builder = new LevelDefBuilder(8, 8);
            builder.Cars.Add(new CarDef(Axis.Horizontal, 3, 0, TileColor.Pink, 3, true));
            builder.Cars.Add(new CarDef(Axis.Vertical, 2, 3, TileColor.Blue));
            builder.Paints.Add(new PaintDef(2, 4, TileColor.Green));
            builder.Muds.Add(new Cell(2, 4)); builder.Sprays.Add(new Cell(2, 4));
            // Kết quả lấy bằng CW.move trên cùng màn nhỏ: corpus lời giải vẫn chưa có fx không rỗng.
            // Fixture riêng kiểm JsonUtility phân biệt root Pyl.k (số) và fx.k (chữ), không sửa trace chuẩn.
            const string json = @"{""kind"":""move"",""car"":0,""sign"":1,""moved"":true,""exited"":false,
                ""events"":[{""t"":""push"",""sign"":1,""cars"":[{""j"":1,""r"":2,""c"":4,""fx"":[{""k"":""paint"",""color"":""green""},{""k"":""mud"",""idx"":0},{""k"":""spray""}]}]},
                {""t"":""step"",""r"":3,""c"":1,""sign"":1},{""t"":""stop"",""why"":""push"",""sign"":1}],
                ""state"":{""cars"":[{""r"":3,""c"":1,""axis"":""h"",""color"":""pink"",""mud"":0,""alive"":true},{""r"":2,""c"":4,""axis"":""v"",""color"":""green"",""mud"":0,""alive"":true}],""muds"":[false],""pyl"":[],""moves"":1}}";
            var expected = JsonUtility.FromJson<StepData>(json);
            var level = builder.Build(); var initial = GameState.Initial(level);
            var result = MoveResolver.Move(level, initial, expected.car, expected.sign);
            Check(expected.moved, result.Moved, "fixture push.fx · moved");
            Check(expected.exited, result.Exited, "fixture push.fx · exited");
            AssertEvents(expected.events, result.Events, "fixture push.fx");
            AssertState(expected.state, result.State, "fixture push.fx · state");
            Check(true, initial.MudAt(0), "fixture push.fx · state trước không đổi");
        }

        private static void AssertState(StateData expected, GameState actual, string context)
        {
            Check(expected.moves, actual.Moves, context + ".moves");
            var cars = expected.cars ?? Array.Empty<CarData>();
            Check(cars.Length, actual.CarCount, context + ".cars.Length");
            for (int i = 0; i < cars.Length; i++)
            {
                var e = cars[i]; var a = actual.CarAt(i); string field = $"{context}.cars[{i}]";
                Check(e.r, a.R, field + ".r"); Check(e.c, a.C, field + ".c");
                Check(AxisOf(e.axis, field), a.Axis, field + ".axis");
                Check(EnumOf<TileColor>(e.color, field), a.Color, field + ".color");
                Check(e.mud, a.Mud, field + ".mud"); Check(e.alive, a.Alive, field + ".alive");
            }
            var muds = expected.muds ?? Array.Empty<bool>(); var pyl = expected.pyl ?? Array.Empty<int>();
            Check(muds.Length, actual.MudCount, context + ".muds.Length");
            Check(pyl.Length, actual.PylonCount, context + ".pyl.Length");
            for (int i = 0; i < muds.Length; i++) Check(muds[i], actual.MudAt(i), $"{context}.muds[{i}]");
            for (int i = 0; i < pyl.Length; i++) Check(pyl[i], actual.PylonTurnsAt(i), $"{context}.pyl[{i}]");
        }

        private static void AssertEvents(EventData[] expected, IReadOnlyList<MoveEvent> actual, string context)
        {
            Check(expected.Length, actual.Count, context + " · events.Length");
            for (int i = 0; i < expected.Length; i++)
            {
                var e = expected[i]; var a = actual[i]; string field = $"{context} · events[{i}]";
                var kind = EnumOf<MoveEventKind>(e.t, field);
                Check(kind, a.Kind, field + ".t");
                switch (kind)
                {
                    case MoveEventKind.Step:
                        Check(e.r, a.R, field + ".r"); Check(e.c, a.C, field + ".c"); Check(e.sign, a.Sign, field + ".sign"); break;
                    case MoveEventKind.Paint: AssertColor(e.color, a, field); break;
                    case MoveEventKind.Mud: Check(e.idx, a.MudIndex, field + ".idx"); break;
                    case MoveEventKind.Spray: break;
                    case MoveEventKind.Water:
                        AssertGate(e.gate, a.Gate, field); Check(e.sign, a.Sign, field + ".sign"); Check(e.hadMud, a.HadMud, field + ".hadMud"); break;
                    case MoveEventKind.Exit:
                        AssertGate(e.gate, a.Gate, field); Check(e.sign, a.Sign, field + ".sign"); break;
                    case MoveEventKind.Stop:
                        var reason = EnumOf<StopReason>(e.why, field);
                        Check(true, a.Reason.HasValue, field + ".why có dữ liệu"); Check(reason, a.Reason.Value, field + ".why");
                        Check(e.sign, a.Sign, field + ".sign"); AssertGate(e.gate, a.Gate, field);
                        if (reason == StopReason.Car) Check(e.j, a.OtherCar, field + ".j");
                        break;
                    case MoveEventKind.Splash:
                        Check(e.j, a.OtherCar, field + ".j"); Check(e.sign, a.Sign, field + ".sign"); break;
                    case MoveEventKind.Push:
                        Check(e.sign, a.Sign, field + ".sign");
                        AssertPush(e.cars ?? Array.Empty<PushedData>(), a.PushedCars, field); break;
                    case MoveEventKind.Tele:
                        Check(true, a.FromFace.HasValue, field + ".from có dữ liệu");
                        Check(true, a.ToFace.HasValue, field + ".to có dữ liệu");
                        AssertFace(e.fr, e.fc, e.fside, a.FromFace.Value, field + ".from");
                        AssertFace(e.tr, e.tc, e.tside, a.ToFace.Value, field + ".to");
                        Check(e.r, a.R, field + ".r"); Check(e.c, a.C, field + ".c");
                        Check(true, a.Axis.HasValue, field + ".axis có dữ liệu");
                        Check(AxisOf(e.axis, field), a.Axis.Value, field + ".axis"); Check(e.sign, a.Sign, field + ".sign"); break;
                    case MoveEventKind.Rotate: break;
                    case MoveEventKind.Pyl: Check(e.k, a.PylonIndex, field + ".k"); break;
                    default: throw new AssertionException(field + " · chưa so loại sự kiện " + kind);
                }
            }
        }

        private static void AssertPush(PushedData[] expected, IReadOnlyList<PushedCarEvent> actual, string context)
        {
            Check(expected.Length, actual.Count, context + ".cars.Length");
            for (int i = 0; i < expected.Length; i++)
            {
                var e = expected[i]; var a = actual[i]; string field = $"{context}.cars[{i}]";
                Check(e.j, a.Car, field + ".j"); Check(e.r, a.R, field + ".r"); Check(e.c, a.C, field + ".c");
                var effects = e.fx ?? Array.Empty<EffectData>(); Check(effects.Length, a.Effects.Count, field + ".fx.Length");
                for (int k = 0; k < effects.Length; k++)
                {
                    var effect = effects[k]; var actualEffect = a.Effects[k]; string fx = $"{field}.fx[{k}]";
                    var kind = EnumOf<MoveEventKind>(effect.k, fx); Check(kind, actualEffect.Kind, fx + ".k");
                    switch (kind)
                    {
                        case MoveEventKind.Paint: AssertColor(effect.color, actualEffect, fx); break;
                        case MoveEventKind.Mud: Check(effect.idx, actualEffect.MudIndex, fx + ".idx"); break;
                        case MoveEventKind.Spray: break;
                        default: throw new AssertionException(fx + " · loại hiệu ứng ô lạ: " + effect.k);
                    }
                }
            }
        }

        private static void AssertColor(string color, MoveEvent actual, string context)
        {
            Check(true, actual.Color.HasValue, context + ".color có dữ liệu");
            Check(EnumOf<TileColor>(color, context), actual.Color.Value, context + ".color");
        }
        private static void AssertFace(int r, int c, string side, PortalFace actual, string context)
        {
            Check(r, actual.R, context + ".r"); Check(c, actual.C, context + ".c");
            Check(EnumOf<Side>(side, context), actual.Side, context + ".side");
        }
        private static void AssertGate(GateData expected, GateDef? actual, string context)
        {
            // JsonUtility có thể để null hoặc DTO inline rỗng; side bắt buộc cho một cổng có nghĩa.
            bool hasGate = expected != null && !string.IsNullOrEmpty(expected.side);
            Check(hasGate, actual.HasValue, context + ".gate có dữ liệu");
            if (!hasGate) return;
            GateEdge edge;
            switch (expected.side)
            {
                case "t": edge = GateEdge.Top; break;
                case "b": edge = GateEdge.Bottom; break;
                case "l": edge = GateEdge.Left; break;
                case "r": edge = GateEdge.Right; break;
                default: throw new AssertionException(context + ".gate.side lạ: " + expected.side);
            }
            Check(edge, actual.Value.Edge, context + ".gate.side"); Check(expected.i, actual.Value.Index, context + ".gate.i");
            Check(EnumOf<TileColor>(expected.color, context), actual.Value.Color, context + ".gate.color");
        }
        private static Axis AxisOf(string axis, string context)
        {
            if (axis == "h") return Axis.Horizontal;
            if (axis == "v") return Axis.Vertical;
            throw new AssertionException(context + " · axis lạ: " + axis);
        }
        private static T EnumOf<T>(string text, string context) where T : struct
        {
            if (Enum.TryParse(text, true, out T value) && Enum.IsDefined(typeof(T), value)) return value;
            throw new AssertionException(context + " · khóa lạ: " + text);
        }
        private static void Check<T>(T expected, T actual, string field)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                Assert.Fail($"{field} · mong đợi: {expected} · thực tế: {actual}");
        }

        [Serializable] private sealed class MinimumData { public int minMoves = 0; }
        [Serializable] private sealed class TraceData
        {
            public int schemaVersion = 0, id = 0;
            public StateData initialState = null;
            public SequenceData[] sequences = null;
        }
        [Serializable] private sealed class SequenceData { public string kind = null; public StepData[] moves = null; }
        [Serializable] private sealed class StepData
        {
            public string kind = null;
            public int car = 0, sign = 0;
            public bool moved = false, exited = false;
            public EventData[] events = null;
            public StateData state = null;
        }
        [Serializable] private sealed class StateData
        {
            public int moves = 0;
            public CarData[] cars = null;
            public bool[] muds = null;
            public int[] pyl = null;
        }
        [Serializable] private sealed class CarData
        {
            public int r = 0, c = 0, mud = 0;
            public string axis = null, color = null;
            public bool alive = false;
        }
        [Serializable] private sealed class GateData { public string side = null, color = null; public int i = 0; }
        [Serializable] private sealed class EventData
        {
            public string t = null, color = null, why = null, fside = null, tside = null, axis = null;
            public int r = 0, c = 0, sign = 0, idx = 0, hadMud = 0, j = 0, k = 0, fr = 0, fc = 0, tr = 0, tc = 0;
            public GateData gate = null;
            public PushedData[] cars = null;
        }
        [Serializable] private sealed class PushedData { public int j = 0, r = 0, c = 0; public EffectData[] fx = null; }
        // k trong Pyl là int; k trong fx là string. Tách DTO để JsonUtility không ép sai kiểu.
        [Serializable] private sealed class EffectData { public string k = null, color = null; public int idx = 0; }
    }
}
