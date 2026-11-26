using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    // 019: chạy thật từng bài tập trên bãi của nó, để sửa bãi hoặc luật làm hỏng bài thì test báo.
    public sealed class Tutorial019Tests
    {
        private const int MaxStates = 300000;

        private static TutorialLesson[] Lessons() => Resources.LoadAll<TextAsset>("Tutorial")
            .Select(asset => TutorialLesson.Parse(asset.text, asset.name)).OrderBy(lesson => lesson.Order).ToArray();

        private static IEnumerable<string> Keys() => Lessons().Select(lesson => lesson.Key);

        [Test]
        public void Lessons_HaveUniqueKeysAndOrder()
        {
            var lessons = Lessons();
            Assert.That(lessons.Length, Is.GreaterThan(0));
            Assert.That(lessons.Select(l => l.Key).Distinct().Count(), Is.EqualTo(lessons.Length));
            Assert.That(lessons.Select(l => l.Order).Distinct().Count(), Is.EqualTo(lessons.Length));
        }

        [TestCaseSource(nameof(Keys))]
        public void ScriptedSteps_CanBePlayed_AndNeverStrandThePlayer(string key)
        {
            var lesson = Lessons().Single(l => l.Key == key);
            var runner = new TutorialRunner(lesson);
            var session = new GameSession(lesson.Board);
            Assert.That(Solve(session), Is.True, "bãi tập phải giải được từ đầu");
            while (!runner.IsDone && !session.IsWin)
            {
                var step = runner.Current;
                switch (step.Action)
                {
                    case TutorialAction.Next:
                        runner.Next();
                        break;
                    case TutorialAction.Undo:
                        Assert.That(Solve(session), Is.False, "bước Hoàn tác chỉ có nghĩa khi đang kẹt (viền đỏ)");
                        Assert.That(session.Undo(), Is.True);
                        runner.Undone();
                        Assert.That(Solve(session), Is.True, "hoàn tác xong phải giải được");
                        break;
                    case TutorialAction.Win:
                        var path = new PathSearch(session.Level, session.State, MaxStates);
                        while (!path.IsDone) { path.Step(1000); }
                        Assert.That(path.Result.Moves, Is.Not.Null.And.Not.Empty, "bước tự chơi phải có đường thắng");
                        foreach (var move in path.Result.Moves)
                        {
                            Assert.That(runner.Allows(move, false), Is.True);
                            Assert.That(session.Apply(move).Moved, Is.True);
                        }

                        break;
                    default:
                        var command = Command(step, session);
                        Assert.That(runner.Allows(command, step.Gesture == TutorialGesture.Swipe), Is.True);
                        Assert.That(session.Apply(command).Moved, Is.True, "bước " + runner.Index + ": " + step.Text);
                        runner.Moved();
                        break;
                }
            }

            Assert.That(session.IsWin, Is.True, "bài phải kết thúc bằng thắng bãi tập");
        }

        // Mọi thế bãi người chơi tới được ở bước tự chơi cuối vẫn thắng được hoặc có Hoàn tác (viền đỏ lo phần đó);
        // riêng bài đầu tiên không được phép kẹt vì người chơi chưa biết Hoàn tác.
        [Test]
        public void FirstLesson_HasNoDeadEnd_AfterItsScriptedMoves()
        {
            var lesson = Lessons()[0];
            var runner = new TutorialRunner(lesson);
            var session = new GameSession(lesson.Board);
            while (runner.Current.Action != TutorialAction.Win)
            {
                if (runner.Current.Action == TutorialAction.Next) { runner.Next(); continue; }
                Assert.That(session.Apply(Command(runner.Current, session)).Moved, Is.True);
                runner.Moved();
            }

            var seen = new HashSet<string>();
            var open = new Queue<GameState>();
            open.Enqueue(session.State);
            seen.Add(Describe(session.State));
            while (open.Count > 0)
            {
                var state = open.Dequeue();
                var search = new WinSearch(lesson.Board, state, MaxStates);
                while (!search.IsDone) { search.Step(1000); }
                Assert.That(search.Win, Is.True, "kẹt ở thế " + Describe(state));
                for (int car = 0; car < state.CarCount; car++)
                {
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        var result = new SolverMove(SolverMoveKind.MoveCar, car, sign).Apply(lesson.Board, state);
                        if (result.Moved && seen.Add(Describe(result.State))) { open.Enqueue(result.State); }
                    }
                }
            }
        }

        [Test]
        public void EveryMechanicInPlayedLevels_HasALesson()
        {
            var lessons = Lessons();
            var order = JsonUtility.FromJson<Order>(Resources.Load<TextAsset>("Levels/level_order").text);
            string[] mechanics = { "mud", "water", "spray", "puddle", "splash", "paint", "table", "light", "truck", "portal", "pylon" };
            foreach (int id in order.ids)
            {
                var level = LevelJsonLoader.Load(Resources.Load<TextAsset>("Levels/level_" + id.ToString("00")).text, "level " + id);
                foreach (var mechanic in mechanics)
                {
                    if (TutorialMechanics.Has(level, mechanic))
                    {
                        Assert.That(lessons.Any(l => l.Teaches.Contains(mechanic)), Is.True, "màn " + id + ": chưa có bài cho " + mechanic);
                    }
                }

                // Cọc, barie, chốt chưa có bài: màn nào dùng thì phải thêm bài trước khi đưa vào danh sách.
                Assert.That(level.Cones.Count + level.Barriers.Count + level.Switches.Count, Is.Zero, "màn " + id);
            }
        }

        [Test]
        public void Pending_FollowsLessonOrder_AndSkipsSeenLessons()
        {
            var lessons = Lessons();
            var level = LevelJsonLoader.Load(Resources.Load<TextAsset>("Levels/level_04").text, "level 4");
            var seen = new HashSet<string>();
            var shown = new List<string>();
            for (var next = TutorialMechanics.Pending(lessons, level, seen.Contains); next != null;
                next = TutorialMechanics.Pending(lessons, level, seen.Contains))
            {
                shown.Add(next.Key);
                seen.Add(next.Key);
            }

            Assert.That(shown, Is.EqualTo(new[] { "basics", "mud", "splash" }));
        }

        [Test]
        public void Runner_BlocksWrongCar_WrongDirection_AndWrongGesture()
        {
            var lesson = Lessons().Single(l => l.Key == "basics");
            var runner = new TutorialRunner(lesson);
            var tap = lesson.Steps[0];
            var right = new SolverMove(SolverMoveKind.MoveCar, tap.Target, tap.Sign);
            Assert.That(runner.Allows(right, true), Is.False, "bước chạm không nhận vuốt");
            Assert.That(runner.Allows(new SolverMove(SolverMoveKind.MoveCar, tap.Target, -tap.Sign), false), Is.False);
            Assert.That(runner.Allows(new SolverMove(SolverMoveKind.MoveCar, 1 - tap.Target, tap.Sign), false), Is.False);
            Assert.That(runner.AllowsUndo, Is.False);
            Assert.That(runner.AllowsHint, Is.False);
            runner.Next();
            runner.Undone();
            Assert.That(runner.Index, Is.Zero, "chạm màn hình hay Hoàn tác không qua được bước chạy xe");
            Assert.That(runner.Allows(right, false), Is.True);
            runner.Moved();
            Assert.That(runner.Index, Is.EqualTo(1));
            runner.Finish();
            Assert.That(runner.IsDone, Is.True);
            Assert.That(runner.Allows(right, false), Is.False);
        }

        [Test]
        public void Seen_IsStoredPerLesson_AndCanBeReset()
        {
            var storage = new Memory();
            var progress = new ProgressStore(storage);
            Assert.That(progress.TutorialSeen("basics"), Is.False);
            progress.MarkTutorialSeen("basics");
            Assert.That(new ProgressStore(storage).TutorialSeen("basics"), Is.True);
            Assert.That(progress.TutorialSeen("mud"), Is.False);
            progress.ResetTutorial("basics");
            Assert.That(progress.TutorialSeen("basics"), Is.False);
        }

        [Test]
        public void Parse_RejectsBadLesson()
        {
            const string board = "{\"cols\":4,\"rows\":4,\"cars\":[{\"axis\":\"h\",\"r\":0,\"c\":0,\"color\":\"blue\",\"len\":2}],";
            Assert.Throws<System.FormatException>(() => TutorialLesson.Parse(board + "\"name\":\"x\"}", "thiếu lesson"));
            Assert.Throws<System.FormatException>(() => TutorialLesson.Parse(board
                + "\"lesson\":{\"key\":\"k\",\"teaches\":[\"always\"],\"steps\":[{\"action\":\"move\",\"car\":3,\"text\":\"a\"}]}}", "sai xe"));
            Assert.Throws<System.FormatException>(() => TutorialLesson.Parse(board
                + "\"lesson\":{\"key\":\"k\",\"teaches\":[\"gió\"],\"steps\":[{\"action\":\"win\",\"text\":\"a\"}]}}", "sai cơ chế"));
        }

        private static SolverMove Command(TutorialStep step, GameSession session)
        {
            if (step.Action == TutorialAction.RotatePylon) { return new SolverMove(SolverMoveKind.RotatePylon, step.Target); }
            if (step.Action == TutorialAction.RotateTable) { return new SolverMove(SolverMoveKind.RotateTable, step.Target); }
            if (step.Sign != 0) { return new SolverMove(SolverMoveKind.MoveCar, step.Target, step.Sign); }
            var forward = new SolverMove(SolverMoveKind.MoveCar, step.Target, 1);
            return forward.Apply(session.Level, session.State).Moved ? forward : new SolverMove(SolverMoveKind.MoveCar, step.Target, -1);
        }

        private static bool Solve(GameSession session)
        {
            var search = new WinSearch(session.Level, session.State, MaxStates);
            while (!search.IsDone) { search.Step(1000); }
            Assert.That(search.Win || search.Exhausted, Is.True, "solver phải kết luận được");
            return search.Win;
        }

        private static string Describe(GameState state)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < state.CarCount; i++)
            {
                var car = state.CarAt(i);
                text.Append(car.Alive ? car.R + "," + car.C + "," + car.Axis + "," + car.Color + "," + car.Mud : "x").Append(';');
            }

            text.Append(state.Moves & 1);
            return text.ToString();
        }

        [System.Serializable]
        private sealed class Order
        {
            public int[] ids = null;
        }

        private sealed class Memory : IProgressStorage
        {
            private readonly Dictionary<string, int> values = new Dictionary<string, int>();
            public int GetInt(string key, int fallback) => values.TryGetValue(key, out int value) ? value : fallback;
            public void SetInt(string key, int value) => values[key] = value;
            public void Save() { }
        }
    }
}
