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
        [SerializeField]
        private GameConfig gameConfig;
        [SerializeField]
        private FeelConfig feelConfig;
        [SerializeField]
        private CameraConfig cameraConfig;
        [SerializeField, Tooltip("Catalog prefab xe Kenney; thiếu sẽ dùng xe khối dự phòng.")]
        private CarCatalog carCatalog;
        [SerializeField, Tooltip("Catalog bãi và cơ chế 011; thiếu dùng khối dự phòng.")]
        private MechanicCatalog mechanicCatalog;
        [SerializeField]
        private VisualPalette palette;
        [SerializeField]
        private Camera boardCamera;
        [SerializeField]
        private Canvas canvas;
        [SerializeField]
        private BoardView board;
        [SerializeField]
        private ActionAnimator animator;
        [SerializeField]
        private InputController input;
        [SerializeField]
        private DebugHud hud;
        [Header("009 — bạn gắn trong scene sau khi tự dựng UI")]
        [SerializeField]
        private NavigationView navigationView;
        [SerializeField]
        private SfxPlayer sfxPlayer;
        [SerializeField]
        private Haptics haptics;
        [SerializeField, Tooltip("Prefab hạt dùng chung tạo bởi Tools/Squeaky Lane/Core Feel.")]
        private FeelEffects feelEffectsPrefab = null;
        private FeelEffects feelEffects;
        private Navigator navigator;
        private FeedbackRouter feedback;
        private TableButtons tableButtons;
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
            configs = new ConfigProvider(gameConfig, feelConfig, cameraConfig, carCatalog, mechanicCatalog);
            Input.simulateMouseWithTouches = true;
            Application.targetFrameRate = configs.Game.TargetFps;
            Application.runInBackground = true;
            progress = new ProgressStore();
            feedback = new FeedbackRouter(sfxPlayer, haptics, feelConfig, progress);
            tableButtons = new TableButtons();
            if (feelEffectsPrefab != null)
            {
                feelEffects = Instantiate(feelEffectsPrefab, transform, false);
            }
        }

        private void OnEnable()
        {
            input.Command += Apply;
            tableButtons.Command += Apply;
            hud.Undo += Undo;
            hud.Restart += Restart;
            hud.Hint += RequestHint;
            hud.ChangeLevel += ChangeLevel;
            feedback.Connect(input, animator, navigationView);
            if (feelEffects != null)
            {
                feelEffects.enabled = true;
                feelEffects.Initialize(animator, board, boardCamera, feelConfig, palette);
            }
            if (navigationView != null)
            {
                navigationView.Navigate += Navigate;
                navigationView.Undo += Undo;
                navigationView.Hint += RequestHint;
            }

            if (session != null)
            {
                stuck = new StuckDetector(session, gameConfig.SearchMaxStates);
                hints = new HintProvider(session, timer, savedHints, gameConfig.SearchMaxStates);
                animator.CancelAndSnap(session.State);
                mechanicsDirty = false;
            }
        }

        private void OnDisable()
        {
            input.Command -= Apply;
            tableButtons.Command -= Apply;
            hud.Undo -= Undo;
            hud.Restart -= Restart;
            hud.Hint -= RequestHint;
            hud.ChangeLevel -= ChangeLevel;
            feedback.Disconnect();
            if (feelEffects != null)
            {
                feelEffects.enabled = false;
            }
            if (navigationView != null)
            {
                navigationView.Navigate -= Navigate;
                navigationView.Undo -= Undo;
                navigationView.Hint -= RequestHint;
                navigationView.SetFade(0);
            }

            if (transition != null)
            {
                StopCoroutine(transition);
            }

            transition = null;
            transitioning = false;
            winCelebrating = false;
            winDelayRemaining = 0;
            input.Accepting = false;
            tableButtons.SetAccepting(false);
            if (hints != null)
            {
                savedHints = hints.Remaining;
            }

            DisposeSearch();
        }

        private void Start()
        {
            var data = JsonUtility.FromJson<OrderData>(configs.LevelOrder().text);
            if (data.ids == null || data.ids.Length == 0)
            {
                throw new InvalidOperationException("level_order rỗng");
            }

            order = data.ids;
            hud.Initialize(canvas, navigationView == null);
            tableButtons.Initialize(canvas);
            if (navigationView == null)
            {
                LoadLevel(0);
                return;
            }

            navigator = new Navigator(order.Length);
            navigationView.Initialize(order, progress, gameConfig.UnlockAll);
            ApplyPlayGate();
            board.SetVisible(false);
            tableButtons.SetVisible(false);
        }

        public void LoadLevel(int position)
        {
            winCelebrating = false;
            winDelayRemaining = 0;
            DisposeSearch();
            selected = (position % order.Length + order.Length) % order.Length;
            var json = configs.Level(order[selected]);
            var level = LevelJsonLoader.Load(json.text, json.name, out var metadata);
            levelName = metadata.Name;
            session = new GameSession(level);
            var setup = new LevelSetup(metadata, selected, gameConfig.TutorialLevelsUntimed, gameConfig.BaseSeconds, gameConfig.SecondsPerMove, gameConfig.UnknownMinimumMoves, gameConfig.HintsPerLevel);
            timer = new LevelTimer(setup.TimeLimit, gameConfig.UndoPenalty, gameConfig.HintPenalty, gameConfig.Star3Ratio, gameConfig.Star2Ratio, gameConfig.MaxTickSeconds);
            stuck = new StuckDetector(session, gameConfig.SearchMaxStates);
            hints = new HintProvider(session, timer, setup.Hints, gameConfig.SearchMaxStates);
            board.Build(session.Level, session.State, palette, configs.Cars, configs.Mechanics);
            animator.Initialize(board, feelConfig, session.State);
            input.Initialize(boardCamera, board, gameConfig);
            tableButtons.Bind(session, board, boardCamera, gameConfig);
            FitBoardCamera();
            SetMessage("Chạm nửa xe để chạy");
            mechanicsDirty = false;
            PeakSearchMilliseconds = 0;
            lastCountdownSecond = -1;
            animator.Paused = false;
            board.SetVisible(true);
            tableButtons.SetVisible(true);
            ApplyPlayGate();
            RefreshHud();
        }

        private void DisposeSearch()
        {
            if (stuck != null)
            {
                stuck.Dispose();
            }

            if (hints != null)
            {
                hints.Dispose();
            }

            stuck = null;
            hints = null;
        }

        public void Apply(SolverMove command)
        {
            if (!CanPlay || session == null || timer.IsOver || session.IsWin)
            {
                return;
            }

            var before = session.State;
            var result = session.Apply(command);
            if (!result.Moved)
            {
                SetMessage("Hướng này đang bị chặn");
                if (command.Kind == SolverMoveKind.MoveCar && result.Events.Count > 0)
                {
                    // Không thêm lượt/đổi Core, chỉ diễn Stop đã có cả khi xe bị chặn ngay ô đầu.
                    animator.Enqueue(command, before, result);
                }
                else
                {
                    feedback.Play(SoundCue.StopWall);
                }
                RefreshHud();
                return;
            }

            animator.Enqueue(command, before, result);
            mechanicsDirty = true;
            SetMessage("");
            tableButtons.Refresh();
            ApplyPlayGate();
            RefreshHud();
        }

        public void Undo()
        {
            if (DiagnosticsPaused || transitioning || (navigator != null && navigator.Screen != ScreenState.Playing) || session == null || session.IsWin || !session.Undo())
            {
                return;
            }

            timer.ChargeUndo();
            animator.CancelAndSnap(session.State);
            mechanicsDirty = false;
            SetMessage("Đã hoàn tác");
            tableButtons.Refresh();
            ApplyPlayGate();
            RefreshHud();
            feedback.Play(SoundCue.Undo);
            if (navigator != null)
            {
                ExecuteBanner(navigator.SetStuck(false));
            }
        }

        public void Restart()
        {
            if (navigator != null)
            {
                Navigate(NavigationAction.Restart, -1);
            }
            else if (!DiagnosticsPaused && order != null)
            {
                LoadLevel(selected);
            }
        }

        private void ChangeLevel(int delta)
        {
            if (!DiagnosticsPaused && order != null)
            {
                LoadLevel(selected + delta);
            }
        }

        private void RequestHint()
        {
            if (!CanPlay || session == null || timer.IsOver || session.IsWin)
            {
                return;
            }

            SetMessage(hints.Request() ? "Đang tìm gợi ý…" : "Đã hết gợi ý");
            RefreshHud();
            feedback.Play(SoundCue.Hint);
        }

        private void Update()
        {
            if (navigationView != null)
            {
                if (navigationView.RefreshSafeArea() && session != null)
                {
                    FitBoardCamera();
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Navigate(NavigationAction.Back, -1);
                }
            }

            ApplyPlayGate();
            if (session == null || DiagnosticsPaused)
            {
                return;
            }

            // Đồng hồ chọn unscaled có chủ đích; chỉ trạng thái thắng/thua mới pause, kẹt vẫn đếm.
            timer.Tick(Time.unscaledDeltaTime);
            ApplyPlayGate();
            if (navigator != null && navigator.Screen == ScreenState.Playing && !transitioning)
            {
                if (session.IsWin)
                {
                    // Core thắng trước hình. Đợi mọi xe chạy hết rồi mới chúc mừng, không cắt xe cuối bằng lớp mờ.
                    if (!animator.IsBusy)
                    {
                        if (!winCelebrating)
                        {
                            winCelebrating = true;
                            winDelayRemaining = feelConfig.WinDialogDelaySeconds;
                            feedback.Result(true);
                            if (feelEffects != null)
                            {
                                feelEffects.Celebrate();
                            }
                        }
                        else
                        {
                            winDelayRemaining -= Time.unscaledDeltaTime;
                        }

                        if (winDelayRemaining <= 0)
                        {
                            progress.RecordWin(order[selected], timer.StarsOnWin());
                            Navigate(NavigationAction.Won, -1);
                        }
                    }
                }
                else if (timer.IsOver)
                {
                    feedback.Result(false);
                    Navigate(NavigationAction.Lost, -1);
                }
            }

            if (input.Accepting)
            {
                long start = Stopwatch.GetTimestamp();
                var budget = SearchBudget.Start(gameConfig.SearchMilliseconds);
                hints.Poll(budget);
                stuck.Poll(budget);
                LastSearchMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                PeakSearchMilliseconds = Math.Max(PeakSearchMilliseconds, LastSearchMilliseconds);
                if (hints.TryTake(out var move))
                {
                    SetMessage(move.Kind == SolverMoveKind.MoveCar ? "Gợi ý: xe " + (move.Index + 1) + (move.Sign > 0 ? " → / ↓" : " ← / ↑") : move.Kind == SolverMoveKind.RotateTable ? "Gợi ý: xoay xe " + (move.Index + 1) : "Gợi ý: xoay trụ " + (move.Index + 1));
                    RefreshHud();
                }
                else if (hints.HasFinished && !hints.HasHint)
                {
                    SetMessage("Chưa tìm được gợi ý trong ngân sách");
                    hints.Cancel();
                    RefreshHud();
                }

                if (navigator != null)
                {
                    bool isStuck = stuck.Status == StuckStatus.Stuck;
                    if (isStuck && !navigator.IsStuck)
                    {
                        feedback.Play(SoundCue.Stuck);
                    }

                    ExecuteBanner(navigator.SetStuck(isStuck));
                }

                int second = Mathf.CeilToInt(timer.Remaining);
                if (timer.IsTimed && timer.Remaining / timer.TimeLimit < gameConfig.LowTimeRatio && second != lastCountdownSecond)
                {
                    lastCountdownSecond = second;
                    feedback.Play(SoundCue.Countdown);
                }
            }

            if (mechanicsDirty && !animator.IsBusy)
            {
                board.RefreshMechanics(session.State);
                mechanicsDirty = false;
            }
            if (hud.RefreshSafeArea())
            {
                FitBoardCamera();
            }

            ApplyPlayGate();
            hudIn -= Time.unscaledDeltaTime;
            if (hudIn <= 0)
            {
                hudIn = .1f;
                RefreshHud();
            } // Chữ đồng hồ chỉ cập nhật 10 Hz, không tạo chuỗi mỗi frame.
        }

        private void SetMessage(string value)
        {
            message = value;
            // Gửi theo sự kiện để thông báo trùng nội dung vẫn hiện lại; tick HUD 10 Hz không kéo dài tuổi bong bóng.
            if (navigationView != null)
            {
                navigationView.ShowMessage(value);
            }
        }

        private void FitBoardCamera()
        {
            var viewport = navigationView != null ? hud.BoardViewport(cameraConfig.PlayingBottomInset) : hud.BoardViewport();
            board.FitCamera(boardCamera, cameraConfig, viewport);
        }

        private void RefreshHud()
        {
            if (session == null)
            {
                return;
            }

            string time = timer.IsTimed ? Mathf.CeilToInt(timer.Remaining) + " s" : "Không tính giờ";
            string title = "Màn " + order[selected] + " · " + levelName + "\n" + time + " · " + session.State.Moves + " nước";
            string status = session.IsWin ? "Thắng · " + timer.StarsOnWin() + " sao" : timer.IsOver ? "Hết giờ" : stuck.Status == StuckStatus.Stuck && (navigationView == null || !navigationView.HasStuckEdge) ? "Kẹt xe · Hoàn tác hoặc chơi lại" : message;
            status += "\nGợi ý còn: " + (hints.Remaining < 0 ? "∞" : hints.Remaining.ToString());
            hud.Show(title, status, !session.IsWin && session.UndoCount > 0, !timer.Paused && hints.Remaining != 0);
            if (navigationView != null)
            {
                navigationView.RefreshHud("Màn " + order[selected] + " · " + levelName, timer, session.State.Moves, hints.Remaining, message, CanPlay && !session.IsWin && session.UndoCount > 0, !timer.Paused, order.Length, selected, gameConfig);
            }
        }

        private bool CanPlay => !DiagnosticsPaused && !transitioning && (navigator == null || navigator.Screen == ScreenState.Playing);

        private void ApplyPlayGate()
        {
            bool playing = CanPlay && !clockPaused && session != null && !session.IsWin && timer != null && !timer.IsOver;
            input.Accepting = playing;
            tableButtons.SetAccepting(playing);
            if (timer != null)
            {
                timer.Paused = !playing;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && navigator != null && navigator.Screen == ScreenState.Playing && session != null && !session.IsWin && !timer.IsOver)
            {
                Navigate(NavigationAction.Pause, -1);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                OnApplicationPause(true);
            }
        }

#endif
        private void Navigate(NavigationAction action, int index)
        {
            if (navigator == null || DiagnosticsPaused || transitioning)
            {
                return;
            }

            // Không mở Pause/Back chen vào giữa xe cuối ra cổng và bảng thắng.
            if (session != null && session.IsWin && navigator.Screen == ScreenState.Playing && (action == NavigationAction.Pause || action == NavigationAction.Back))
            {
                return;
            }

            if (action == NavigationAction.SelectLevel && !progress.IsUnlocked(order, index, gameConfig.UnlockAll))
            {
                return;
            }

            var commands = navigator.Handle(action, index);
            if (commands.Count == 0)
            {
                return;
            }

            clockPaused = true;
            ApplyPlayGate();
            animator.Paused = navigator.Screen == ScreenState.Paused;
            if (session != null && (action == NavigationAction.Restart || action == NavigationAction.NextLevel || action == NavigationAction.LevelSelect || navigator.Screen == ScreenState.LevelSelect))
            {
                animator.CancelAndSnap(session.State);
                feedback.StopAll();
            }

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
                    case NavigationCommandKind.StartLevel:
                    case NavigationCommandKind.RestartLevel:
                        LoadLevel(command.LevelIndex);
                        break;
                    case NavigationCommandKind.EndLevel:
                        DisposeSearch();
                        session = null;
                        timer = null;
                        board.SetVisible(false);
                        tableButtons.SetVisible(false);
                        break;
                    case NavigationCommandKind.PauseClock:
                        clockPaused = true;
                        break;
                    case NavigationCommandKind.ResumeClock:
                        clockPaused = false;
                        break;
                    case NavigationCommandKind.ShowScreen:
                        navigationView.ShowScreen(command.Screen);
                        navigationView.RefreshLevels(progress, gameConfig.UnlockAll);
                        break;
                    case NavigationCommandKind.ShowStuck:
                        navigationView.ShowStuck(true);
                        break;
                    case NavigationCommandKind.HideStuck:
                        navigationView.ShowStuck(false);
                        break;
                }
            }

            yield return Fade(1, 0);
            transitioning = false;
            transition = null;
            animator.Paused = navigator.Screen == ScreenState.Paused;
            ApplyPlayGate();
            RefreshHud();
        }

        private IEnumerator Fade(float from, float to)
        {
            float duration = feelConfig.TransitionSeconds * .5f;
            if (duration > 0)
            {
                for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    navigationView.SetFade(Mathf.Lerp(from, to, elapsed / duration));
                    yield return null;
                }
            }

            navigationView.SetFade(to);
        }

        private void ExecuteBanner(IReadOnlyList<NavigationCommand> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                navigationView.ShowStuck(commands[i].Kind == NavigationCommandKind.ShowStuck);
            }
        }

        private void LateUpdate()
        {
            tableButtons.UpdatePositions();
        }

        private void OnDestroy()
        {
            if (tableButtons != null)
            {
                tableButtons.Dispose();
            }
        }

        [Serializable]
        private sealed class OrderData
        {
            public int[] ids = null;
        }
    }
}
