using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using PuzzleSolver = SqueakyLane.Core.Solver.Solver;

namespace SqueakyLane.Tests.EditMode
{
    internal static class SolverTestData
    {
        internal static LevelDef Load(int id) => LevelJsonLoader.Load(File.ReadAllText(GoldenFiles.Level(id)), $"level_{id:00}.json");
        internal static int Minimum(int id) => JsonUtility.FromJson<MinimumData>(File.ReadAllText(GoldenFiles.Level(id))).minMoves;
        public static IEnumerable<int> KnownLevels()
        {
            // Không gọi Unity API lúc NUnit khám phá test; test coverage bên dưới kiểm lại ba màn chưa biết.
            foreach (int id in GoldenFiles.LevelNumbers()) if (id < 17 || id > 19) yield return id;
        }
        internal static GameState Replay(LevelDef level, GameState state, IReadOnlyList<SolverMove> moves)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                var result = moves[i].Apply(level, state);
                Assert.That(result.Moved, Is.True, "Đường tìm được có lệnh không hiệu lực ở bước " + i);
                state = result.State;
            }
            return state;
        }
        [Serializable] private sealed class MinimumData { public int minMoves = 0; }
    }

    public sealed class SolverTests
    {
        [TestCaseSource(typeof(SolverTestData), nameof(SolverTestData.KnownLevels))]
        public void BreadthFirst_MatchesGoldenMinimumAndReplaysToWin(int id)
        {
            var level = SolverTestData.Load(id); var initial = GameState.Initial(level);
            var result = PuzzleSolver.Solve(level, initial, 300000);
            Assert.That(result.Exhausted, Is.True, "Màn " + id + " chưa có kết luận");
            Assert.That(result.Moves, Is.Not.Null, "Màn " + id + " không tìm được đường thắng");
            Assert.That(result.Moves.Count, Is.EqualTo(SolverTestData.Minimum(id)), "Màn " + id + " lệch minMoves");
            Assert.That(GameRules.IsWin(SolverTestData.Replay(level, initial, result.Moves)), Is.True);
            Assert.That(initial.Moves, Is.Zero);
        }

        [Test]
        public void GoldenMinimumCoverage_Uses28KnownAndLeavesThreeUnknown()
        {
            int known = 0; var unknown = new List<int>();
            foreach (int id in GoldenFiles.LevelNumbers())
                if (SolverTestData.Minimum(id) >= 0) known++; else unknown.Add(id);
            Assert.That(known, Is.EqualTo(28)); Assert.That(unknown, Is.EqualTo(new[] { 17, 18, 19 }));
        }

        [TestCaseSource(typeof(GoldenFiles), nameof(GoldenFiles.LevelNumbers))]
        public void PathSearch_FindsReplayableWinInSmallSteps(int id)
        {
            var level = SolverTestData.Load(id); var initial = GameState.Initial(level);
            var search = new PathSearch(level, initial, 300000);
            int calls = 0;
            while (!search.IsDone)
            {
                int before = search.Expanded;
                bool done = search.Step(4);
                Assert.That(search.Expanded - before, Is.LessThanOrEqualTo(4));
                Assert.That(done, Is.EqualTo(search.IsDone));
                Assert.That(++calls, Is.LessThan(300001), "Search không dừng");
            }
            Assert.That(search.Result.Exhausted, Is.True, "Màn " + id + " hết ngân sách");
            Assert.That(search.Result.Moves, Is.Not.Null, "Màn " + id + " không có đường thắng");
            Assert.That(GameRules.IsWin(SolverTestData.Replay(level, initial, search.Result.Moves)), Is.True);
            Assert.That(initial.Moves, Is.Zero);
        }

        [TestCase(true)] [TestCase(false)]
        public void WinSearch_DistinguishesWinningAndDeadlockedBoard(bool winning)
        {
            var level = Lane(winning); var initial = GameState.Initial(level);
            var search = new WinSearch(level, initial, 100);
            while (!search.Step(1)) { }
            Assert.That(search.IsDone, Is.True); Assert.That(search.Exhausted, Is.True);
            Assert.That(search.Win, Is.EqualTo(winning)); Assert.That(initial.Moves, Is.Zero);
            int explored = search.Explored;
            Assert.That(search.Step(1), Is.True); Assert.That(search.Explored, Is.EqualTo(explored));
        }

        [Test]
        public void InsufficientBudget_IsUnknownRatherThanDeadlocked()
        {
            var level = Lane(false); var state = GameState.Initial(level);
            var result = PuzzleSolver.Solve(level, state, 1);
            Assert.That(result.Moves, Is.Null); Assert.That(result.Exhausted, Is.False); Assert.That(result.Explored, Is.EqualTo(1));
            var win = new WinSearch(level, state, 1); while (!win.Step(1)) { }
            Assert.That(win.Win, Is.False); Assert.That(win.Exhausted, Is.False);
            var path = new PathSearch(level, state, 1); while (!path.Step(1)) { }
            Assert.That(path.Result.Moves, Is.Null); Assert.That(path.Result.Exhausted, Is.False);
        }

        [Test]
        public void Step_ExposesRunningStateAndHonorsNodeBudget()
        {
            var level = Lane(true); var state = GameState.Initial(level);
            var path = new PathSearch(level, state, 100);
            Assert.That(path.Result, Is.Null); Assert.That(path.IsDone, Is.False);
            Assert.That(path.Step(1), Is.False); Assert.That(path.Expanded, Is.EqualTo(1));
            while (!path.Step(1)) { }
            Assert.That(path.Result.Moves, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => ((IList<SolverMove>)path.Result.Moves)[0] = default);
        }

        [Test]
        public void AlreadyWon_HasEmptyPathAndNoExpansion()
        {
            var level = new LevelDefBuilder(1, 1).Build(); var state = GameState.Initial(level);
            var result = PuzzleSolver.Solve(level, state, 1);
            Assert.That(result.Moves, Is.Empty); Assert.That(result.Exhausted, Is.True); Assert.That(result.Explored, Is.Zero);
            var win = new WinSearch(level, state, 1);
            Assert.That(win.IsDone && win.Win && win.Exhausted, Is.True); Assert.That(win.Expanded, Is.Zero);
            var path = new PathSearch(level, state, 1);
            Assert.That(path.Step(1), Is.True); Assert.That(path.Result.Moves, Is.Empty);
        }

        [Test]
        public void InvalidArguments_DoNotStartSearch()
        {
            var level = Lane(true); var state = GameState.Initial(level);
            Assert.Throws<ArgumentOutOfRangeException>(() => PuzzleSolver.Solve(level, state, 0));
            Assert.Throws<ArgumentNullException>(() => PuzzleSolver.Solve(null, state, 10));
            Assert.Throws<ArgumentNullException>(() => new PathSearch(level, null, 10));
            var search = new WinSearch(level, state, 10);
            Assert.Throws<ArgumentOutOfRangeException>(() => search.Step(0));
            Assert.That(search.Expanded, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SolverMove(SolverMoveKind.MoveCar, 0, 0));
        }

        private static LevelDef Lane(bool winning)
        {
            var b = new LevelDefBuilder(4, 2); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 1, TileColor.Pink));
            if (winning) b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink));
            return b.Build();
        }
    }
}
