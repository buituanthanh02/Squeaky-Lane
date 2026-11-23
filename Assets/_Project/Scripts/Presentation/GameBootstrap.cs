using System;
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
        }
        private void OnEnable()
        {
            input.Command += Apply; hud.Command += Apply; hud.Undo += Undo; hud.Restart += Restart; hud.Hint += RequestHint; hud.ChangeLevel += ChangeLevel;
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
            input.Accepting = false; if (hints != null) savedHints = hints.Remaining; DisposeSearch();
        }
        private void Start()
        {
            var data = JsonUtility.FromJson<OrderData>(configs.LevelOrder().text);
            if (data.ids == null || data.ids.Length == 0) throw new InvalidOperationException("level_order rỗng");
            order = data.ids; hud.Initialize(canvas); LoadLevel(0);
        }
        public void LoadLevel(int position)
        {
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
            PeakSearchMilliseconds = 0; RefreshHud();
        }
        private void DisposeSearch() { if (stuck != null) stuck.Dispose(); if (hints != null) hints.Dispose(); stuck = null; hints = null; }
        public void Apply(SolverMove command)
        {
            if (DiagnosticsPaused || session == null || timer.IsOver || session.IsWin) return;
            var before = session.State; var result = session.Apply(command);
            if (!result.Moved) { message = "Hướng này đang bị chặn"; RefreshHud(); return; }
            animator.Enqueue(command, before, result); mechanicsDirty = true; message = ""; hud.RefreshTables(); RefreshHud();
        }
        public void Undo()
        {
            if (DiagnosticsPaused || session == null || !session.Undo()) return;
            timer.ChargeUndo(); animator.CancelAndSnap(session.State); mechanicsDirty = false; message = "Đã hoàn tác"; hud.RefreshTables(); RefreshHud();
        }
        public void Restart() { if (!DiagnosticsPaused && order != null) LoadLevel(selected); }
        private void ChangeLevel(int delta) { if (!DiagnosticsPaused && order != null) LoadLevel(selected + delta); }
        private void RequestHint()
        {
            if (DiagnosticsPaused || timer.IsOver || session.IsWin) return;
            message = hints.Request() ? "Đang tìm gợi ý…" : "Đã hết gợi ý"; RefreshHud();
        }
        private void Update()
        {
            if (session == null) return;
            if (DiagnosticsPaused) { input.Accepting = false; return; }
            bool finished = session.IsWin || timer.IsOver;
            timer.Paused = finished;
            // Đồng hồ chọn unscaled có chủ đích; chỉ trạng thái thắng/thua mới pause, kẹt vẫn đếm.
            timer.Tick(Time.unscaledDeltaTime);
            input.Accepting = !session.IsWin && !timer.IsOver;
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
            }
            if (mechanicsDirty && !animator.IsBusy) { board.RefreshMechanics(session.State); mechanicsDirty = false; }
            if (hud.RefreshSafeArea()) board.FitCamera(boardCamera, cameraConfig, hud.BoardViewport());
            hudIn -= Time.unscaledDeltaTime;
            if (hudIn <= 0) { hudIn = .1f; RefreshHud(); } // Chữ đồng hồ chỉ cập nhật 10 Hz, không tạo chuỗi mỗi frame.
        }
        private void RefreshHud()
        {
            timer.Paused = session.IsWin || timer.IsOver;
            string time = timer.IsTimed ? Mathf.CeilToInt(timer.Remaining) + " s" : "Không tính giờ";
            string title = "Màn " + order[selected] + " · " + levelName + "\n" + time + " · " + session.State.Moves + " nước";
            string status = session.IsWin ? "Thắng · " + timer.StarsOnWin() + " sao" : timer.IsOver ? "Hết giờ" :
                stuck.Status == StuckStatus.Stuck ? "Kẹt xe · Hoàn tác hoặc chơi lại" : message;
            status += "\nGợi ý còn: " + (hints.Remaining < 0 ? "∞" : hints.Remaining.ToString());
            hud.Show(title, status, session.UndoCount > 0, !timer.Paused && hints.Remaining != 0, !timer.Paused);
            input.Accepting = !timer.Paused;
        }
        [Serializable] private sealed class OrderData { public int[] ids = null; }
        [Serializable] private sealed class Metadata { public string name = ""; public int minMoves = -1, hints = 0; public float timeLimit = 0; }
    }
}
