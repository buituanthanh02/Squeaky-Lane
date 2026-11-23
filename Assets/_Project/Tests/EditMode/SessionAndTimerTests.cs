using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class SessionAndTimerTests
    {
        [Test]
        public void UndoRestoresExactSnapshotsAndRestartClearsHistory()
        {
            var session = new GameSession(Lane(true)); var initial = session.State;
            var first = session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, -1));
            Assert.That(first.Moved, Is.True); var middle = session.State;
            session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, 1));
            Assert.That(session.IsWin, Is.True); Assert.That(session.UndoCount, Is.EqualTo(2));
            Assert.That(session.Undo(), Is.True); Assert.That(session.State, Is.SameAs(middle));
            Assert.That(session.Undo(), Is.True); Assert.That(session.State, Is.SameAs(initial));
            Assert.That(session.Undo(), Is.False);
            session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, 1)); session.Restart();
            Assert.That(session.State.Moves, Is.Zero); Assert.That(session.IsWin, Is.False); Assert.That(session.UndoCount, Is.Zero);
        }
        [Test]
        public void InvalidMoveDoesNotAddUndoOrNotify()
        {
            var session = new GameSession(Lane(false)); int notifications = 0;
            session.Changed += () => notifications++;
            session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, -1));
            int previous = notifications, undo = session.UndoCount;
            var result = session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, -1));
            Assert.That(result.Moved, Is.False); Assert.That(notifications, Is.EqualTo(previous)); Assert.That(session.UndoCount, Is.EqualTo(undo));
        }
        [Test]
        public void ClockAcceptsDtPausesAndClampsAtZero()
        {
            var timer = Timer(100); timer.Tick(20); timer.Paused = true; timer.Tick(90);
            Assert.That(timer.Remaining, Is.EqualTo(80)); timer.Paused = false; timer.Tick(200);
            Assert.That(timer.IsOver, Is.True); Assert.That(timer.Remaining, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(float.NaN));
        }
        [TestCase(50, 3)] [TestCase(49, 2)] [TestCase(20, 2)] [TestCase(19, 1)] [TestCase(0, 1)]
        public void StarsIncludeExactBoundary(int remaining, int stars)
        { var timer = Timer(100); timer.Tick(100 - remaining); Assert.That(timer.StarsOnWin(), Is.EqualTo(stars)); }
        [TestCase(0)] [TestCase(-1)]
        public void UntimedDoesNotLoseTimeOrStars(float time)
        {
            var timer = Timer(time); timer.Tick(1000); timer.ChargeUndo(); timer.ChargeHint();
            Assert.That(timer.IsOver, Is.False); Assert.That(timer.StarsOnWin(), Is.EqualTo(3));
        }
        [Test]
        public void SuccessfulUndoAndDeliveredHintChargeOnce()
        {
            var session = new GameSession(Lane(true)); var timer = Timer(100);
            session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, -1));
            if (session.Undo()) timer.ChargeUndo();
            Assert.That(timer.Remaining, Is.EqualTo(97));
            using var hint = new HintProvider(session, timer, 1, 100);
            Assert.That(hint.Request(), Is.True); Assert.That(timer.Remaining, Is.EqualTo(97));
            hint.Poll(SearchBudget.Start(100)); Assert.That(hint.TryTake(out var move), Is.True);
            Assert.That(timer.Remaining, Is.EqualTo(92)); Assert.That(hint.Remaining, Is.Zero);
            Assert.That(hint.TryTake(out _), Is.False); Assert.That(hint.Request(), Is.False);
            Assert.That(move.Apply(session.Level, session.State).Moved, Is.True);
        }
        [TestCase(true)] [TestCase(false)]
        public void DetectorConcludesOnlyAfterSearchAndCancelsOnChange(bool winnable)
        {
            var session = new GameSession(Lane(winnable));
            using var detector = new StuckDetector(session, 100);
            Assert.That(detector.Poll(SearchBudget.Start(0)), Is.EqualTo(StuckStatus.Unknown));
            Assert.That(detector.Poll(SearchBudget.Start(100)), Is.EqualTo(winnable ? StuckStatus.Winnable : StuckStatus.Stuck));
            session.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, -1));
            Assert.That(detector.Status, Is.EqualTo(StuckStatus.Unknown));
            detector.Poll(SearchBudget.Start(100)); session.Undo();
            Assert.That(detector.Status, Is.EqualTo(StuckStatus.Unknown));
            detector.Poll(SearchBudget.Start(100)); session.Restart();
            Assert.That(detector.Status, Is.EqualTo(StuckStatus.Unknown));
        }
        [Test]
        public void ExhaustedBudgetCannotReportStuckAndOldHintCannotEscape()
        {
            var session = new GameSession(Lane(false));
            using var detector = new StuckDetector(session, 1);
            Assert.That(detector.Poll(SearchBudget.Start(100)), Is.EqualTo(StuckStatus.Unknown));
            var winning = new GameSession(Lane(true)); using var hint = new HintProvider(winning, Timer(100), 3, 100);
            hint.Request(); hint.Poll(SearchBudget.Start(100)); Assert.That(hint.HasHint, Is.True);
            winning.Apply(new SolverMove(SolverMoveKind.MoveCar, 0, -1)); Assert.That(hint.HasHint, Is.False);
            Assert.That(hint.TryTake(out _), Is.False);
            hint.Request(); hint.Poll(SearchBudget.Start(100)); winning.Undo(); Assert.That(hint.HasHint, Is.False);
        }
        [Test]
        public void ConfigValidationPreventsInvalidValuesWithoutChangingRuntimeState()
        {
            var game = ScriptableObject.CreateInstance<GameConfig>(); var feel = ScriptableObject.CreateInstance<FeelConfig>(); var camera = ScriptableObject.CreateInstance<CameraConfig>();
            try
            {
                Set(game, "undoPenalty", -5f); Set(game, "star2Ratio", .9f); Set(game, "targetFps", 0); Set(game, "searchMilliseconds", float.NaN);
                Set(feel, "stepSeconds", -1f); Set(camera, "tiltDegrees", 100f);
                Validate(game); Validate(feel); Validate(camera);
                Assert.That(game.UndoPenalty, Is.Zero); Assert.That(game.Star2Ratio, Is.LessThanOrEqualTo(game.Star3Ratio));
                Assert.That(game.TargetFps, Is.GreaterThan(0)); Assert.That(game.SearchMilliseconds, Is.EqualTo(2));
                Assert.That(feel.StepSeconds, Is.GreaterThan(0)); Assert.That(camera.TiltDegrees, Is.EqualTo(90));
                var timer = new LevelTimer(100, game.UndoPenalty, game.HintPenalty, game.Star3Ratio, game.Star2Ratio); timer.Tick(30);
                Assert.That(game.BaseSeconds, Is.EqualTo(20));
            }
            finally { UnityEngine.Object.DestroyImmediate(game); UnityEngine.Object.DestroyImmediate(feel); UnityEngine.Object.DestroyImmediate(camera); }
        }
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        private static void Validate(object obj) => obj.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
        private static LevelTimer Timer(float seconds) => new LevelTimer(seconds, 3, 5, .5f, .2f);
        private static LevelDef Lane(bool gate)
        { var b = new LevelDefBuilder(4, 2); b.Cars.Add(new CarDef(Axis.Horizontal, 0, 1, TileColor.Pink)); if (gate) b.Gates.Add(new GateDef(GateEdge.Right, 0, TileColor.Pink)); return b.Build(); }
    }
}
