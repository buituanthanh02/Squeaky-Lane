using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private FeelConfig feelConfig;
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private VisualPalette palette;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private BoardView board;
        [SerializeField] private ActionAnimator animator;
        [SerializeField] private InputController input;
        [SerializeField] private DebugHud hud;
        [Header("009 — bạn gắn trong scene sau khi tự dựng UI")]
        [SerializeField] private NavigationView navigationView;
        [SerializeField] private SfxPlayer sfxPlayer;
        [SerializeField] private Haptics haptics;
        private Navigator navigator;
        private ProgressStore progress;
        private Coroutine transition;
        private bool transitioning, clockPaused;
        private bool winCelebrating;
        private float winDelayRemaining;
        private int lastCountdownSecond = -1;
        private ConfigProvider configs;
        private GameSession session;
        private LevelTimer timer;
        private StuckDetector stuck;
        private HintProvider hints;
        private int[] order;
        private int selected;
        private string levelName, message = "";
        private float hudIn;
        private bool mechanicsDirty;
        private int savedHints;
        public bool DiagnosticsPaused { get; set; }
        public double LastSearchMilliseconds { get; private set; }
        public double PeakSearchMilliseconds { get; private set; }
        public GameSession Session => session;
        public LevelTimer Timer => timer;
        public ConfigProvider Configs => configs;

        private void Awake()
        {
            configs = new ConfigProvider(gameConfig, feelConfig, cameraConfig);
            Input.simulateMouseWithTouches = true;
            Application.targetFrameRate = configs.Game.TargetFps; Application.runInBackground = true;
            progress = new ProgressStore();
            if (sfxPlayer != null) sfxPlayer.Configure(feelConfig, progress);
            if (haptics != null) haptics.Configure(feelConfig, progress);
        }
        private void OnEnable()
        {
            input.Command += Apply; hud.Command += Apply; hud.Undo += Undo; hud.Restart += Restart; hud.Hint += RequestHint; hud.ChangeLevel += ChangeLevel;
            input.CarSelected += SelectedCar; animator.EventStarted += VisualEvent; animator.CarStarted += CarStarted;
            if (navigationView != null)
            {
                navigationView.Navigate += Navigate; navigationView.Undo += Undo; navigationView.Hint += RequestHint;
                navigationView.ButtonClicked += UiClicked; navigationView.SoundChanged += SoundChanged; navigationView.HapticsChanged += HapticsChanged;
            }
            if (session != null)
            {
                stuck = new StuckDetector(session, gameConfig.SearchMaxStates);
                hints = new HintProvider(session, timer, savedHints, gameConfig.SearchMaxStates);
                animator.CancelAndSnap(session.State); mechanicsDirty = false;
            }
        }
        private void OnDisable()
        {
            input.Command -= Apply; hud.Command -= Apply; hud.Undo -= Undo; hud.Restart -= Restart; hud.Hint -= RequestHint; hud.ChangeLevel -= ChangeLevel;
            input.CarSelected -= SelectedCar; animator.EventStarted -= VisualEvent; animator.CarStarted -= CarStarted;
            if (navigationView != null)
            {
                navigationView.Navigate -= Navigate; navigationView.Undo -= Undo; navigationView.Hint -= RequestHint;
                navigationView.ButtonClicked -= UiClicked; navigationView.SoundChanged -= SoundChanged; navigationView.HapticsChanged -= HapticsChanged;
                navigationView.SetFade(0);
            }
            if (transition != null) StopCoroutine(transition); transition = null; transitioning = false;
            winCelebrating = false; winDelayRemaining = 0;
            input.Accepting = false; if (hints != null) savedHints = hints.Remaining; DisposeSearch();
        }
        private void Start()
        {
            var data = JsonUtility.FromJson<OrderData>(configs.LevelOrder().text);
            if (data.ids == null || data.ids.Length == 0) throw new InvalidOperationException("level_order rỗng");
            order = data.ids; hud.Initialize(canvas, navigationView == null);
            if (navigationView == null) { LoadLevel(0); return; }
            navigator = new Navigator(order.Length);
            navigationView.Initialize(order, progress, gameConfig.UnlockAll);
            input.Accepting = false; board.SetVisible(false); hud.SetGameplayVisible(false);
        }
        public void LoadLevel(int position)
        {
            winCelebrating = false; winDelayRemaining = 0;
            DisposeSearch(); input.Accepting = false;
            selected = (position % order.Length + order.Length) % order.Length;
            var json = configs.Level(order[selected]); var metadata = JsonUtility.FromJson<Metadata>(json.text);
            levelName = metadata.name; session = new GameSession(LevelJsonLoader.Load(json.text, json.name));
            bool tutorial = selected < gameConfig.TutorialLevelsUntimed;
            float timeLimit = tutorial ? 0 : json.text.Contains("\"timeLimit\"") ? metadata.timeLimit :
                gameConfig.BaseSeconds + gameConfig.SecondsPerMove * (metadata.minMoves >= 0 ? metadata.minMoves : gameConfig.UnknownMinimumMoves);
            timer = new LevelTimer(timeLimit, gameConfig.UndoPenalty, gameConfig.HintPenalty, gameConfig.Star3Ratio, gameConfig.Star2Ratio);
            stuck = new StuckDetector(session, gameConfig.SearchMaxStates);
            hints = new HintProvider(session, timer, tutorial ? -1 : json.text.Contains("\"hints\"") ? metadata.hints : gameConfig.HintsPerLevel, gameConfig.SearchMaxStates);
            board.Build(session.Level, session.State, palette); animator.Initialize(board, feelConfig, session.State);
            input.Initialize(boardCamera, board, gameConfig); hud.Bind(session, board, boardCamera, gameConfig);
            board.FitCamera(boardCamera, cameraConfig, hud.BoardViewport()); message = "Chạm nửa xe để chạy"; mechanicsDirty = false;
            PeakSearchMilliseconds = 0; lastCountdownSecond = -1; animator.Paused = false;
            board.SetVisible(true); hud.SetGameplayVisible(true); RefreshHud();
        }
        private void DisposeSearch() { if (stuck != null) stuck.Dispose(); if (hints != null) hints.Dispose(); stuck = null; hints = null; }
        public void Apply(SolverMove command)
        {
            if (!CanPlay || session == null || timer.IsOver || session.IsWin) return;
            var before = session.State; var result = session.Apply(command);
            if (!result.Moved) { message = "Hướng này đang bị chặn"; Play(SoundCue.StopWall); RefreshHud(); return; }
            animator.Enqueue(command, before, result); mechanicsDirty = true; message = ""; hud.RefreshTables(); RefreshHud();
        }
        public void Undo()
        {
            if (DiagnosticsPaused || transitioning || (navigator != null && navigator.Screen != ScreenState.Playing) || session == null || session.IsWin || !session.Undo()) return;
            timer.ChargeUndo(); animator.CancelAndSnap(session.State); mechanicsDirty = false; message = "Đã hoàn tác"; hud.RefreshTables(); RefreshHud();
            Play(SoundCue.Undo); if (navigator != null) ExecuteBanner(navigator.SetStuck(false));
        }
        public void Restart() { if (navigator != null) Navigate(NavigationAction.Restart, -1); else if (!DiagnosticsPaused && order != null) LoadLevel(selected); }
        private void ChangeLevel(int delta) { if (!DiagnosticsPaused && order != null) LoadLevel(selected + delta); }
        private void RequestHint()
        {
            if (!CanPlay || session == null || timer.IsOver || session.IsWin) return;
            message = hints.Request() ? "Đang tìm gợi ý…" : "Đã hết gợi ý"; RefreshHud();
            Play(SoundCue.Hint);
        }
        private void Update()
        {
            if (navigationView != null)
            {
                if (navigationView.RefreshSafeArea() && session != null) board.FitCamera(boardCamera, cameraConfig, hud.BoardViewport());
                if (Input.GetKeyDown(KeyCode.Escape)) Navigate(NavigationAction.Back, -1);
            }
            if (session == null) return;
            if (DiagnosticsPaused) { input.Accepting = false; return; }
            bool finished = session.IsWin || timer.IsOver;
            timer.Paused = finished || !CanPlay || clockPaused;
            // Đồng hồ chọn unscaled có chủ đích; chỉ trạng thái thắng/thua mới pause, kẹt vẫn đếm.
            timer.Tick(Time.unscaledDeltaTime);
            input.Accepting = CanPlay && !session.IsWin && !timer.IsOver;
            if (navigator != null && navigator.Screen == ScreenState.Playing && !transitioning)
            {
                if (session.IsWin)
                {
                    // Core thắng trước hình. Đợi mọi xe chạy hết rồi mới chúc mừng, không cắt xe cuối bằng lớp mờ.
                    if (!animator.IsBusy)
                    {
                        if (!winCelebrating)
                        {
                            winCelebrating = true; winDelayRemaining = feelConfig.WinDialogDelaySeconds;
                            Play(SoundCue.Won); Pulse(true);
                        }
                        else winDelayRemaining -= Time.unscaledDeltaTime;
                        if (winDelayRemaining <= 0)
                        { progress.RecordWin(order[selected], timer.StarsOnWin()); Navigate(NavigationAction.Won, -1); }
                    }
                }
                else if (timer.IsOver) { Play(SoundCue.Lost); Pulse(true); Navigate(NavigationAction.Lost, -1); }
            }
            if (input.Accepting)
            {
                long start = Stopwatch.GetTimestamp(); var budget = SearchBudget.Start(gameConfig.SearchMilliseconds);
                hints.Poll(budget); stuck.Poll(budget);
                LastSearchMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                PeakSearchMilliseconds = Math.Max(PeakSearchMilliseconds, LastSearchMilliseconds);
                if (hints.TryTake(out var move))
                {
                    message = move.Kind == SolverMoveKind.MoveCar ? "Gợi ý: xe " + (move.Index + 1) + (move.Sign > 0 ? " → / ↓" : " ← / ↑") :
                        move.Kind == SolverMoveKind.RotateTable ? "Gợi ý: xoay xe " + (move.Index + 1) : "Gợi ý: xoay trụ " + (move.Index + 1);
                    RefreshHud();
                }
                else if (hints.HasFinished && !hints.HasHint) { message = "Chưa tìm được gợi ý trong ngân sách"; hints.Cancel(); RefreshHud(); }
                if (navigator != null)
                {
                    bool isStuck = stuck.Status == StuckStatus.Stuck;
                    if (isStuck && !navigator.IsStuck) Play(SoundCue.Stuck);
                    ExecuteBanner(navigator.SetStuck(isStuck));
                }
                int second = Mathf.CeilToInt(timer.Remaining);
                if (timer.IsTimed && timer.Remaining / timer.TimeLimit < gameConfig.LowTimeRatio && second != lastCountdownSecond)
                { lastCountdownSecond = second; Play(SoundCue.Countdown); }
            }
            if (mechanicsDirty && !animator.IsBusy) { board.RefreshMechanics(session.State); mechanicsDirty = false; }
            if (hud.RefreshSafeArea()) board.FitCamera(boardCamera, cameraConfig, hud.BoardViewport());
            hudIn -= Time.unscaledDeltaTime;
            if (hudIn <= 0) { hudIn = .1f; RefreshHud(); } // Chữ đồng hồ chỉ cập nhật 10 Hz, không tạo chuỗi mỗi frame.
        }
        private void RefreshHud()
        {
            if (session == null) return;
            timer.Paused = session.IsWin || timer.IsOver || !CanPlay || clockPaused;
            string time = timer.IsTimed ? Mathf.CeilToInt(timer.Remaining) + " s" : "Không tính giờ";
            string title = "Màn " + order[selected] + " · " + levelName + "\n" + time + " · " + session.State.Moves + " nước";
            string status = session.IsWin ? "Thắng · " + timer.StarsOnWin() + " sao" : timer.IsOver ? "Hết giờ" :
                stuck.Status == StuckStatus.Stuck ? "Kẹt xe · Hoàn tác hoặc chơi lại" : message;
            status += "\nGợi ý còn: " + (hints.Remaining < 0 ? "∞" : hints.Remaining.ToString());
            hud.Show(title, status, !session.IsWin && session.UndoCount > 0, !timer.Paused && hints.Remaining != 0, !timer.Paused);
            input.Accepting = !timer.Paused;
            if (navigationView != null)
                navigationView.RefreshHud("Màn " + order[selected] + " · " + levelName, timer, session.State.Moves, hints.Remaining, message,
                    CanPlay && !session.IsWin && session.UndoCount > 0, !timer.Paused, order.Length, selected, gameConfig);
        }
        private bool CanPlay => !DiagnosticsPaused && !transitioning && (navigator == null || navigator.Screen == ScreenState.Playing);
        private void Navigate(NavigationAction action, int index)
        {
            if (navigator == null || DiagnosticsPaused || transitioning) return;
            // Không mở Pause/Back chen vào giữa xe cuối ra cổng và bảng thắng.
            if (session != null && session.IsWin && navigator.Screen == ScreenState.Playing &&
                (action == NavigationAction.Pause || action == NavigationAction.Back)) return;
            if (action == NavigationAction.SelectLevel && !progress.IsUnlocked(order, index, gameConfig.UnlockAll)) return;
            var commands = navigator.Handle(action, index); if (commands.Count == 0) return;
            input.Accepting = false; clockPaused = true;
            animator.Paused = navigator.Screen == ScreenState.Paused;
            if (session != null && (action == NavigationAction.Restart || action == NavigationAction.NextLevel || action == NavigationAction.LevelSelect || navigator.Screen == ScreenState.LevelSelect))
            { animator.CancelAndSnap(session.State); if (sfxPlayer != null) sfxPlayer.StopAll(); }
            transition = StartCoroutine(Transition(commands));
        }
        private IEnumerator Transition(IReadOnlyList<NavigationCommand> commands)
        {
            transitioning = true;
            yield return Fade(0, 1);
            for (int i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                switch (command.Kind)
                {
                    case NavigationCommandKind.StartLevel: case NavigationCommandKind.RestartLevel: LoadLevel(command.LevelIndex); break;
                    case NavigationCommandKind.EndLevel:
                        DisposeSearch(); session = null; timer = null; board.SetVisible(false); hud.SetGameplayVisible(false); break;
                    case NavigationCommandKind.PauseClock: clockPaused = true; break;
                    case NavigationCommandKind.ResumeClock: clockPaused = false; break;
                    case NavigationCommandKind.ShowScreen:
                        navigationView.ShowScreen(command.Screen); navigationView.RefreshLevels(progress, gameConfig.UnlockAll); break;
                    case NavigationCommandKind.ShowStuck: navigationView.ShowStuck(true); break;
                    case NavigationCommandKind.HideStuck: navigationView.ShowStuck(false); break;
                }
            }
            yield return Fade(1, 0);
            transitioning = false; transition = null;
            animator.Paused = navigator.Screen == ScreenState.Paused;
            RefreshHud();
        }
        private IEnumerator Fade(float from, float to)
        {
            float duration = feelConfig.TransitionSeconds * .5f;
            if (duration > 0)
                for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                { navigationView.SetFade(Mathf.Lerp(from, to, elapsed / duration)); yield return null; }
            navigationView.SetFade(to);
        }
        private void ExecuteBanner(IReadOnlyList<NavigationCommand> commands)
        { for (int i = 0; i < commands.Count; i++) navigationView.ShowStuck(commands[i].Kind == NavigationCommandKind.ShowStuck); }
        private void Play(SoundCue cue) { if (sfxPlayer != null) sfxPlayer.Play(cue); }
        private void Pulse(bool result = false) { if (haptics != null) haptics.Pulse(result); }
        private void VisualEvent(MoveEventKind kind, StopReason? reason)
        { if (sfxPlayer != null) sfxPlayer.PlayEvent(kind, reason); if (kind == MoveEventKind.Stop || kind == MoveEventKind.Exit) Pulse(); }
        private void SelectedCar() { Play(SoundCue.SelectCar); }
        private void CarStarted() { Play(SoundCue.Move); }
        private void UiClicked() { Play(SoundCue.Ui); }
        private void SoundChanged(bool enabled)
        { progress.SoundEnabled = enabled; if (!enabled && sfxPlayer != null) sfxPlayer.StopAll(); navigationView.RefreshSettings(progress); }
        private void HapticsChanged(bool enabled)
        { progress.HapticsEnabled = enabled; navigationView.RefreshSettings(progress); if (enabled) Pulse(); }
        [Serializable] private sealed class OrderData { public int[] ids = null; }
        [Serializable] private sealed class Metadata { public string name = ""; public int minMoves = -1, hints = 0; public float timeLimit = 0; }
    }
}
