using System;
using System.Collections.Generic;
using NUnit.Framework;
using SqueakyLane.Game;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class NavigationTests
    {
        [Test]
        public void StartPauseResumeWinNextRestartAndReturnFormCompleteLoop()
        {
            var nav = new Navigator(3); Assert.That(nav.Screen, Is.EqualTo(ScreenState.LevelSelect));
            var commands = nav.Handle(NavigationAction.SelectLevel, 1);
            AssertKinds(commands, NavigationCommandKind.HideStuck, NavigationCommandKind.StartLevel, NavigationCommandKind.ResumeClock, NavigationCommandKind.ShowScreen);
            Assert.That(commands[1].LevelIndex, Is.EqualTo(1)); Assert.That(nav.Screen, Is.EqualTo(ScreenState.Playing));
            AssertKinds(nav.Handle(NavigationAction.Pause), NavigationCommandKind.PauseClock, NavigationCommandKind.ShowScreen);
            Assert.That(nav.Screen, Is.EqualTo(ScreenState.Paused));
            AssertKinds(nav.Handle(NavigationAction.Resume), NavigationCommandKind.ResumeClock, NavigationCommandKind.ShowScreen);
            Assert.That(nav.Screen, Is.EqualTo(ScreenState.Playing));
            nav.Handle(NavigationAction.Won); Assert.That(nav.Screen, Is.EqualTo(ScreenState.Won));
            commands = nav.Handle(NavigationAction.NextLevel); Assert.That(commands[1].Kind, Is.EqualTo(NavigationCommandKind.StartLevel)); Assert.That(nav.LevelIndex, Is.EqualTo(2));
            nav.Handle(NavigationAction.Lost); Assert.That(nav.Screen, Is.EqualTo(ScreenState.Lost));
            commands = nav.Handle(NavigationAction.Restart); Assert.That(commands[1].Kind, Is.EqualTo(NavigationCommandKind.RestartLevel)); Assert.That(nav.LevelIndex, Is.EqualTo(2));
            commands = nav.Handle(NavigationAction.LevelSelect); AssertKinds(commands, NavigationCommandKind.PauseClock, NavigationCommandKind.EndLevel, NavigationCommandKind.HideStuck, NavigationCommandKind.ShowScreen);
            Assert.That(nav.Screen, Is.EqualTo(ScreenState.LevelSelect)); Assert.That(nav.LevelIndex, Is.EqualTo(-1));
        }
        [TestCase(ScreenState.LevelSelect, ScreenState.LevelSelect)]
        [TestCase(ScreenState.Playing, ScreenState.Paused)]
        [TestCase(ScreenState.Paused, ScreenState.Playing)]
        [TestCase(ScreenState.Won, ScreenState.LevelSelect)]
        [TestCase(ScreenState.Lost, ScreenState.LevelSelect)]
        public void BackHasDefinedDestinationInEveryScreen(ScreenState from, ScreenState expected)
        {
            var nav = At(from); var commands = nav.Handle(NavigationAction.Back);
            Assert.That(nav.Screen, Is.EqualTo(expected));
            if (from == ScreenState.LevelSelect) Assert.That(commands, Is.Empty);
            else Assert.That(commands[commands.Count - 1].Screen, Is.EqualTo(expected));
        }
        [Test]
        public void WinningFinalLevelReturnsToSelectionRatherThanOutOfRange()
        {
            var nav = new Navigator(2); nav.Handle(NavigationAction.SelectLevel, 1); nav.Handle(NavigationAction.Won);
            var commands = nav.Handle(NavigationAction.NextLevel);
            Assert.That(nav.Screen, Is.EqualTo(ScreenState.LevelSelect)); Assert.That(nav.LevelIndex, Is.EqualTo(-1));
            Assert.That(commands[1].Kind, Is.EqualTo(NavigationCommandKind.EndLevel));
        }
        [Test]
        public void StuckIsNonBlockingAndClearsOnRestartOrEnd()
        {
            var nav = At(ScreenState.Playing);
            AssertKinds(nav.SetStuck(true), NavigationCommandKind.ShowStuck); Assert.That(nav.Screen, Is.EqualTo(ScreenState.Playing));
            Assert.That(nav.SetStuck(true), Is.Empty); AssertKinds(nav.SetStuck(false), NavigationCommandKind.HideStuck);
            nav.SetStuck(true); nav.Handle(NavigationAction.Pause); Assert.That(nav.IsStuck, Is.True);
            nav.Handle(NavigationAction.Resume); Assert.That(nav.IsStuck, Is.True);
            nav.Handle(NavigationAction.Restart); Assert.That(nav.IsStuck, Is.False);
            nav.SetStuck(true); nav.Handle(NavigationAction.Won); Assert.That(nav.IsStuck, Is.False);
        }
        [Test]
        public void InapplicableActionsAndLateResultsCannotChangeCurrentScreen()
        {
            var nav = At(ScreenState.LevelSelect);
            Assert.That(nav.Handle(NavigationAction.NextLevel), Is.Empty); Assert.That(nav.Handle(NavigationAction.Restart), Is.Empty);
            Assert.That(nav.Handle(NavigationAction.Won), Is.Empty); Assert.That(nav.SetStuck(true), Is.Empty);
            nav.Handle(NavigationAction.SelectLevel, 0); nav.Handle(NavigationAction.Pause);
            Assert.That(nav.Handle(NavigationAction.Won), Is.Empty); Assert.That(nav.Handle(NavigationAction.Lost), Is.Empty);
            Assert.That(nav.Screen, Is.EqualTo(ScreenState.Paused));
        }
        [Test]
        public void InvalidLevelArgumentsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Navigator(0));
            var nav = new Navigator(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => nav.Handle(NavigationAction.SelectLevel, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => nav.Handle(NavigationAction.SelectLevel, 2));
            Assert.That(nav.Screen, Is.EqualTo(ScreenState.LevelSelect));
        }
        [Test]
        public void ProgressNeverReplacesBetterStarsAndPersistsAcrossInstances()
        {
            var memory = new MemoryStorage(); var store = new ProgressStore(memory);
            store.RecordWin(21, 3); int writes = memory.Writes;
            store.RecordWin(21, 1); store.RecordWin(21, 3);
            Assert.That(store.Stars(21), Is.EqualTo(3)); Assert.That(memory.Writes, Is.EqualTo(writes));
            store.RecordWin(3, 1); store.RecordWin(3, 2);
            var reloaded = new ProgressStore(memory); Assert.That(reloaded.Stars(3), Is.EqualTo(2)); Assert.That(reloaded.Stars(21), Is.EqualTo(3));
        }
        [Test]
        public void UnlockingUsesPreviousEntryInOrderAndUnlockAllOverridesIt()
        {
            var store = new ProgressStore(new MemoryStorage()); var order = new[] { 21, 3, 17 };
            Assert.That(store.IsUnlocked(order, 0, false), Is.True); Assert.That(store.IsUnlocked(order, 1, false), Is.False);
            Assert.That(store.IsUnlocked(order, 2, true), Is.True); store.RecordWin(21, 1);
            Assert.That(store.IsUnlocked(order, 1, false), Is.True); Assert.That(store.IsUnlocked(order, 2, false), Is.False);
            store.RecordWin(3, 1); Assert.That(store.IsUnlocked(order, 2, false), Is.True);
        }
        [Test]
        public void SettingsPersistThroughSameStorageWithoutRepeatedWrites()
        {
            var memory = new MemoryStorage(); var store = new ProgressStore(memory);
            Assert.That(store.SoundEnabled && store.HapticsEnabled, Is.True);
            store.SoundEnabled = false; store.HapticsEnabled = false; int writes = memory.Writes;
            store.SoundEnabled = false; Assert.That(memory.Writes, Is.EqualTo(writes));
            var reloaded = new ProgressStore(memory); Assert.That(reloaded.SoundEnabled || reloaded.HapticsEnabled, Is.False);
        }
        [Test]
        public void InvalidProgressValuesDoNotWrite()
        {
            var memory = new MemoryStorage(); var store = new ProgressStore(memory);
            Assert.Throws<ArgumentOutOfRangeException>(() => store.RecordWin(1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.RecordWin(0, 2));
            Assert.That(memory.Writes, Is.Zero);
        }
        private static Navigator At(ScreenState state)
        {
            var nav = new Navigator(3); if (state == ScreenState.LevelSelect) return nav;
            nav.Handle(NavigationAction.SelectLevel, 0);
            if (state == ScreenState.Paused) nav.Handle(NavigationAction.Pause);
            if (state == ScreenState.Won) nav.Handle(NavigationAction.Won);
            if (state == ScreenState.Lost) nav.Handle(NavigationAction.Lost);
            return nav;
        }
        private static void AssertKinds(IReadOnlyList<NavigationCommand> commands, params NavigationCommandKind[] kinds)
        { Assert.That(commands.Count, Is.EqualTo(kinds.Length)); for (int i = 0; i < kinds.Length; i++) Assert.That(commands[i].Kind, Is.EqualTo(kinds[i])); }
        private sealed class MemoryStorage : IProgressStorage
        {
            private readonly Dictionary<string, int> values = new Dictionary<string, int>();
            public int Writes { get; private set; }
            public int GetInt(string key, int fallback) => values.TryGetValue(key, out int value) ? value : fallback;
            public void SetInt(string key, int value) { values[key] = value; Writes++; }
            public void Save() { }
        }
    }
}
