using System;
using System.Collections.Generic;

namespace SqueakyLane.Game
{
    public enum ScreenState { LevelSelect, Playing, Paused, Won, Lost }
    public enum NavigationAction { SelectLevel, Pause, Resume, Back, Won, Lost, Restart, NextLevel, LevelSelect }
    public enum NavigationCommandKind { StartLevel, RestartLevel, EndLevel, PauseClock, ResumeClock, ShowScreen, ShowStuck, HideStuck }

    public readonly struct NavigationCommand
    {
        public NavigationCommandKind Kind { get; }
        public int LevelIndex { get; }
        public ScreenState Screen { get; }
        public NavigationCommand(NavigationCommandKind kind, int levelIndex = -1, ScreenState screen = ScreenState.LevelSelect)
        { Kind = kind; LevelIndex = levelIndex; Screen = screen; }
    }

    // Không biết Unity, timer hay session. Lớp nối bên ngoài thực hiện các lệnh theo đúng thứ tự trả về.
    public sealed class Navigator
    {
        public ScreenState Screen { get; private set; } = ScreenState.LevelSelect;
        public int LevelIndex { get; private set; } = -1;
        public int LevelCount { get; }
        public bool IsStuck { get; private set; }
        public Navigator(int levelCount)
        {
            if (levelCount < 1) throw new ArgumentOutOfRangeException(nameof(levelCount));
            LevelCount = levelCount;
        }
        public IReadOnlyList<NavigationCommand> Handle(NavigationAction action, int levelIndex = -1)
        {
            switch (action)
            {
                case NavigationAction.SelectLevel:
                    if (Screen != ScreenState.LevelSelect) return None();
                    if (levelIndex < 0 || levelIndex >= LevelCount) throw new ArgumentOutOfRangeException(nameof(levelIndex));
                    return Start(levelIndex, false);
                case NavigationAction.Pause:
                    return Screen == ScreenState.Playing ? Dialog(ScreenState.Paused) : None();
                case NavigationAction.Resume:
                    if (Screen != ScreenState.Paused) return None();
                    Screen = ScreenState.Playing;
                    return new[] { new NavigationCommand(NavigationCommandKind.ResumeClock), Show(Screen) };
                case NavigationAction.Back:
                    if (Screen == ScreenState.LevelSelect) return None();
                    if (Screen == ScreenState.Playing) return Dialog(ScreenState.Paused);
                    if (Screen == ScreenState.Paused) return Handle(NavigationAction.Resume);
                    return Select(); // Won/Lost đã kết thúc; Back về chọn, không tiếp tục màn cũ.
                case NavigationAction.Won:
                    return Screen == ScreenState.Playing ? Dialog(ScreenState.Won) : None();
                case NavigationAction.Lost:
                    return Screen == ScreenState.Playing ? Dialog(ScreenState.Lost) : None();
                case NavigationAction.Restart:
                    return Screen == ScreenState.LevelSelect ? None() : Start(LevelIndex, true);
                case NavigationAction.NextLevel:
                    if (Screen != ScreenState.Won) return None();
                    return LevelIndex + 1 < LevelCount ? Start(LevelIndex + 1, false) : Select();
                case NavigationAction.LevelSelect:
                    return Screen == ScreenState.LevelSelect ? None() : Select();
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
        public IReadOnlyList<NavigationCommand> SetStuck(bool stuck)
        {
            if (Screen != ScreenState.Playing || IsStuck == stuck) return None();
            IsStuck = stuck;
            return new[] { new NavigationCommand(stuck ? NavigationCommandKind.ShowStuck : NavigationCommandKind.HideStuck) };
        }
        private IReadOnlyList<NavigationCommand> Start(int index, bool restart)
        {
            LevelIndex = index; Screen = ScreenState.Playing; IsStuck = false;
            return new[] {
                new NavigationCommand(NavigationCommandKind.HideStuck),
                new NavigationCommand(restart ? NavigationCommandKind.RestartLevel : NavigationCommandKind.StartLevel, index),
                new NavigationCommand(NavigationCommandKind.ResumeClock), Show(Screen)
            };
        }
        private IReadOnlyList<NavigationCommand> Dialog(ScreenState screen)
        {
            Screen = screen;
            // Pause giữ trạng thái banner để Resume không làm mất thông tin kẹt.
            if (screen == ScreenState.Paused) return new[] { new NavigationCommand(NavigationCommandKind.PauseClock), Show(screen) };
            IsStuck = false;
            return new[] { new NavigationCommand(NavigationCommandKind.HideStuck), new NavigationCommand(NavigationCommandKind.PauseClock), Show(screen) };
        }
        private IReadOnlyList<NavigationCommand> Select()
        {
            Screen = ScreenState.LevelSelect; LevelIndex = -1; IsStuck = false;
            return new[] {
                new NavigationCommand(NavigationCommandKind.PauseClock), new NavigationCommand(NavigationCommandKind.EndLevel),
                new NavigationCommand(NavigationCommandKind.HideStuck), Show(Screen)
            };
        }
        private static NavigationCommand Show(ScreenState screen) => new NavigationCommand(NavigationCommandKind.ShowScreen, screen: screen);
        private static NavigationCommand[] None() => Array.Empty<NavigationCommand>();
    }
}
