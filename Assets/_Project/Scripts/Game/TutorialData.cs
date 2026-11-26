using System;
using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Game
{
    // Việc người chơi phải làm để qua một bước.
    public enum TutorialAction { Next, Move, RotateTable, RotatePylon, Undo, Win }
    public enum TutorialGesture { Any, Tap, Swipe }
    // Thứ được chiếu sáng. Gate dùng Edge + A (hàng/cột); Cell dùng A,B = hàng,cột; còn lại A = chỉ số.
    public enum TutorialTargetKind { Car, Cell, Gate, Table, Pylon, Undo, Hint, Clock }

    public readonly struct TutorialTarget
    {
        public TutorialTargetKind Kind { get; }
        public int A { get; }
        public int B { get; }
        public GateEdge Edge { get; }

        public TutorialTarget(TutorialTargetKind kind, int a = 0, int b = 0, GateEdge edge = GateEdge.Left)
        {
            Kind = kind;
            A = a;
            B = b;
            Edge = edge;
        }
    }

    public sealed class TutorialStep
    {
        public string Text { get; }
        public TutorialAction Action { get; }
        // Chỉ số xe (Move, RotateTable) hoặc trụ (RotatePylon).
        public int Target { get; }
        // -1 trái/lên, +1 phải/xuống, 0 = hướng nào cũng được.
        public int Sign { get; }
        public TutorialGesture Gesture { get; }
        public IReadOnlyList<TutorialTarget> Focus { get; }

        public TutorialStep(string text, TutorialAction action, int target, int sign, TutorialGesture gesture, IReadOnlyList<TutorialTarget> focus)
        {
            Text = text;
            Action = action;
            Target = target;
            Sign = sign;
            Gesture = gesture;
            Focus = focus;
        }
    }

    // Một bài tập: bãi riêng (không nằm trong level_order) và các bước theo thứ tự.
    public sealed class TutorialLesson
    {
        public string Key { get; }
        public int Order { get; }
        public IReadOnlyList<string> Teaches { get; }
        public LevelDef Board { get; }
        public IReadOnlyList<TutorialStep> Steps { get; }

        private TutorialLesson(string key, int order, string[] teaches, LevelDef board, TutorialStep[] steps)
        {
            Key = key;
            Order = order;
            Teaches = teaches;
            Board = board;
            Steps = steps;
        }

        // Bài nằm cùng file với bãi: các field màn ở gốc như level_NN.json, thêm object "lesson".
        public static TutorialLesson Parse(string json, string sourceName)
        {
            var board = LevelJsonLoader.Load(json, sourceName);
            var data = JsonUtility.FromJson<Root>(json).lesson;
            if (data == null || string.IsNullOrEmpty(data.key) || data.steps == null || data.steps.Length == 0)
            {
                throw Bad(sourceName, "lesson", "cần key và ít nhất một bước");
            }

            if (data.teaches == null || data.teaches.Length == 0)
            {
                throw Bad(sourceName, "lesson.teaches", "cần ít nhất một cơ chế");
            }

            foreach (var mechanic in data.teaches)
            {
                if (!TutorialMechanics.IsKnown(mechanic))
                {
                    throw Bad(sourceName, "lesson.teaches", "không biết cơ chế " + mechanic);
                }
            }

            var steps = new TutorialStep[data.steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = ReadStep(data.steps[i], board, sourceName, "lesson.steps[" + i + "]");
            }

            return new TutorialLesson(data.key, data.order, data.teaches, board, steps);
        }

        private static TutorialStep ReadStep(StepData data, LevelDef board, string source, string field)
        {
            TutorialAction action;
            switch (data.action)
            {
                case "next": action = TutorialAction.Next; break;
                case "move": action = TutorialAction.Move; break;
                case "rotate": action = TutorialAction.RotateTable; break;
                case "pylon": action = TutorialAction.RotatePylon; break;
                case "undo": action = TutorialAction.Undo; break;
                case "win": action = TutorialAction.Win; break;
                default: throw Bad(source, field + ".action", "không biết " + data.action);
            }

            TutorialGesture gesture;
            switch (data.input)
            {
                case null:
                case "":
                case "any": gesture = TutorialGesture.Any; break;
                case "tap": gesture = TutorialGesture.Tap; break;
                case "swipe": gesture = TutorialGesture.Swipe; break;
                default: throw Bad(source, field + ".input", "không biết " + data.input);
            }

            if ((action == TutorialAction.Move || action == TutorialAction.RotateTable) && (data.car < 0 || data.car >= board.Cars.Count))
            {
                throw Bad(source, field + ".car", "ngoài danh sách xe");
            }

            if (action == TutorialAction.RotatePylon && (data.pylon < 0 || data.pylon >= board.Pylons.Count))
            {
                throw Bad(source, field + ".pylon", "ngoài danh sách trụ");
            }

            if (data.sign < -1 || data.sign > 1)
            {
                throw Bad(source, field + ".sign", "chỉ nhận -1, 0, 1");
            }

            if (string.IsNullOrEmpty(data.text))
            {
                throw Bad(source, field + ".text", "thiếu chữ hướng dẫn");
            }

            var focus = data.focus ?? Array.Empty<string>();
            var targets = new TutorialTarget[focus.Length];
            for (int i = 0; i < focus.Length; i++)
            {
                targets[i] = ReadTarget(focus[i], board, source, field + ".focus[" + i + "]");
            }

            int index = action == TutorialAction.RotatePylon ? data.pylon : data.car;
            return new TutorialStep(data.text, action, index, data.sign, gesture, targets);
        }

        // Dạng chuỗi ngắn: "car:0", "cell:1,2", "gate:r,1", "table:0", "pylon:0", "undo", "hint", "clock".
        private static TutorialTarget ReadTarget(string value, LevelDef board, string source, string field)
        {
            var parts = (value ?? "").Split(':');
            var args = parts.Length > 1 ? parts[1].Split(',') : Array.Empty<string>();
            switch (parts[0])
            {
                case "undo": return new TutorialTarget(TutorialTargetKind.Undo);
                case "hint": return new TutorialTarget(TutorialTargetKind.Hint);
                case "clock": return new TutorialTarget(TutorialTargetKind.Clock);
                case "car": return new TutorialTarget(TutorialTargetKind.Car, Index(args, 0, board.Cars.Count, source, field));
                case "table": return new TutorialTarget(TutorialTargetKind.Table, Index(args, 0, board.Tables.Count, source, field));
                case "pylon": return new TutorialTarget(TutorialTargetKind.Pylon, Index(args, 0, board.Pylons.Count, source, field));
                case "cell": return new TutorialTarget(TutorialTargetKind.Cell, Index(args, 0, board.Rows, source, field), Index(args, 1, board.Cols, source, field));
                case "gate":
                    GateEdge edge;
                    switch (args.Length > 0 ? args[0] : "")
                    {
                        case "t": edge = GateEdge.Top; break;
                        case "b": edge = GateEdge.Bottom; break;
                        case "l": edge = GateEdge.Left; break;
                        case "r": edge = GateEdge.Right; break;
                        default: throw Bad(source, field, "cạnh cổng phải là t/b/l/r");
                    }

                    bool vertical = edge == GateEdge.Left || edge == GateEdge.Right;
                    int at = Index(args, 1, vertical ? board.Rows : board.Cols, source, field);
                    if (!board.GateAt(edge, at).HasValue)
                    {
                        throw Bad(source, field, "không có cổng ở đó");
                    }

                    return new TutorialTarget(TutorialTargetKind.Gate, at, 0, edge);
                default: throw Bad(source, field, "không biết đích " + value);
            }
        }

        private static int Index(string[] args, int position, int count, string source, string field)
        {
            if (position >= args.Length || !int.TryParse(args[position], out int value) || value < 0 || value >= count)
            {
                throw Bad(source, field, "chỉ số không hợp lệ");
            }

            return value;
        }

        private static FormatException Bad(string source, string field, string reason) => new FormatException(source + " · " + field + ": " + reason);

        [Serializable]
        private sealed class Root
        {
            public LessonData lesson = null;
        }

        [Serializable]
        private sealed class LessonData
        {
            public string key = null;
            public int order = 0;
            public string[] teaches = null;
            public StepData[] steps = null;
        }

        [Serializable]
        private sealed class StepData
        {
            public string text = null;
            public string action = null;
            public int car = -1;
            public int pylon = -1;
            public int sign = 0;
            public string input = null;
            public string[] focus = null;
        }
    }

    // Màn nào có cơ chế nào: bài tập tự hiện trước màn đầu tiên cần nó, kể cả khi đổi thứ tự hoặc thêm màn.
    public static class TutorialMechanics
    {
        public const string Always = "always";
        private static readonly string[] Names =
        {
            Always, "mud", "water", "spray", "puddle", "splash", "paint", "table", "light", "truck", "portal", "pylon"
        };

        public static bool IsKnown(string name) => Array.IndexOf(Names, name) >= 0;

        public static bool Has(LevelDef level, string name)
        {
            switch (name)
            {
                case Always: return true;
                case "mud":
                    for (int i = 0; i < level.Cars.Count; i++)
                    {
                        if (level.Cars[i].Mud > 0) { return true; }
                    }

                    return false;
                case "water":
                    for (int i = 0; i < level.Gates.Count; i++)
                    {
                        if (level.Gates[i].Color == TileColor.Water) { return true; }
                    }

                    return false;
                case "spray": return level.Sprays.Count > 0;
                case "puddle": return level.Muds.Count > 0;
                case "splash": return level.Splash;
                case "paint": return level.Paints.Count > 0;
                case "table": return level.Tables.Count > 0;
                case "light": return level.Lights.Count > 0;
                case "truck":
                    for (int i = 0; i < level.Cars.Count; i++)
                    {
                        if (level.Cars[i].Truck) { return true; }
                    }

                    return false;
                case "portal": return level.Portals.Count > 0;
                case "pylon": return level.Pylons.Count > 0;
                default: return false;
            }
        }

        // Bài chưa học đầu tiên (theo order) mà màn này cần; null = vào thẳng màn.
        public static TutorialLesson Pending(IReadOnlyList<TutorialLesson> lessons, LevelDef level, Func<string, bool> seen)
        {
            TutorialLesson best = null;
            for (int i = 0; i < lessons.Count; i++)
            {
                var lesson = lessons[i];
                if ((best != null && best.Order <= lesson.Order) || seen(lesson.Key))
                {
                    continue;
                }

                for (int k = 0; k < lesson.Teaches.Count; k++)
                {
                    if (Has(level, lesson.Teaches[k]))
                    {
                        best = lesson;
                        break;
                    }
                }
            }

            return best;
        }
    }
}
