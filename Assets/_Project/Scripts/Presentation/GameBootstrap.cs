using System;
using System.Collections;
using System.Collections.Generic;
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
        [Header("UI dựng trong scene")]
        [SerializeField]
        private NavigationView navigationView;
        [SerializeField]
        private SfxPlayer sfxPlayer;
        [SerializeField]
        private Haptics haptics;
        [SerializeField, Tooltip("018: chữ Combo và bảng kỷ lục; trống thì không hiện.")]
        private ComboFeedback comboFeedback;
        [SerializeField, Tooltip("019: lớp hướng dẫn; trống thì không có bãi tập, vào thẳng màn.")]
        private TutorialView tutorialView = null;
        [SerializeField, Tooltip("Prefab hạt dùng chung tạo bởi Tools/Squeaky Lane/Core Feel.")]
        private FeelEffects feelEffectsPrefab = null;
        private FeelEffects feelEffects;
        private Navigator navigator;
        private FeedbackRouter feedback;
        private TableButtons tableButtons;
        private TutorialDirector tutorial;
        private TutorialLesson[] lessons = Array.Empty<TutorialLesson>();
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
        public GameSession Session => session;
        private bool CanPlay => !transitioning && navigator != null && navigator.Screen == ScreenState.Playing;

        private void Awake()
        {
            if (navigationView == null)
            {
                throw new InvalidOperationException("GameBootstrap cần NavigationView trong scene.");
            }

            configs = new ConfigProvider(gameConfig, feelConfig, cameraConfig, carCatalog, mechanicCatalog);
            Input.simulateMouseWithTouches = true;
            Application.targetFrameRate = configs.Game.TargetFps;
            Application.runInBackground = true;
            progress = new ProgressStore();
            feedback = new FeedbackRouter(sfxPlayer, haptics, feelConfig, progress);
            tableButtons = new TableButtons();
            tutorial = new TutorialDirector(tutorialView, navigationView, tableButtons);
            if (feelEffectsPrefab != null)
            {
                feelEffects = Instantiate(feelEffectsPrefab, transform, false);
            }
        }

        private void OnEnable()
        {
            input.Command += Apply;
            tableButtons.Command += Apply;
            feedback.Connect(input, animator, navigationView);
            if (feelEffects != null)
            {
                feelEffects.enabled = true;
                feelEffects.Initialize(animator, board, boardCamera, feelConfig, palette);
                feelEffects.SetTextLayer(navigationView.EffectsLayer);
            }
            if (comboFeedback != null)
            {
                comboFeedback.Initialize(animator, feelConfig, feedback, feelEffects);
            }

            navigationView.Navigate += Navigate;
            navigationView.Undo += Undo;
            navigationView.Hint += RequestHint;
            navigationView.ReplayTutorial += ResetTutorials;
            tutorial.SkipRequested += SkipLesson;
            tutorial.Connect();
            if (session != null)
            {
                CreateSearch(savedHints);
                animator.CancelAndSnap(session.State);
                mechanicsDirty = false;
            }
        }

        private void OnDisable()
        {
            input.Command -= Apply;
            tableButtons.Command -= Apply;
            feedback.Disconnect();
            if (comboFeedback != null)
            {
                comboFeedback.Disconnect();
            }

            if (feelEffects != null)
            {
                feelEffects.enabled = false;
            }
            navigationView.Navigate -= Navigate;
            navigationView.Undo -= Undo;
            navigationView.Hint -= RequestHint;
            navigationView.ReplayTutorial -= ResetTutorials;
            tutorial.SkipRequested -= SkipLesson;
            tutorial.Disconnect();
            navigationView.SetFade(0);

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
            if (tutorial.Available)
            {
                var files = configs.TutorialLessons();
                lessons = new TutorialLesson[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    lessons[i] = TutorialLesson.Parse(files[i].text, files[i].name);
                }
            }

            tableButtons.Initialize(canvas);
            navigator = new Navigator(order.Length);
            navigationView.Initialize(order, progress, gameConfig.UnlockAll);
            ApplyPlayGate();
            SetBoardVisible(false);
        }

        private void LoadLevel(int position)
        {
            winCelebrating = false;
            winDelayRemaining = 0;
            DisposeSearch();
            selected = (position % order.Length + order.Length) % order.Length;
            var json = configs.Level(order[selected]);
            var level = LevelJsonLoader.Load(json.text, json.name, out var metadata);
            levelName = metadata.Name;
            // Màn cần cơ chế chưa học thì vào bãi tập trước; xong bài sẽ nạp lại đúng vị trí này.
            var lesson = TutorialMechanics.Pending(lessons, level, progress.TutorialSeen);
            session = new GameSession(lesson != null ? lesson.Board : level);
            var setup = new LevelSetup(metadata, selected, gameConfig.TutorialLevelsUntimed, gameConfig.BaseSeconds, gameConfig.SecondsPerMove, gameConfig.UnknownMinimumMoves, gameConfig.HintsPerLevel);
            // Bãi tập: không đồng hồ, gợi ý không giới hạn.
            timer = new LevelTimer(lesson != null ? 0 : setup.TimeLimit, gameConfig.UndoPenalty, gameConfig.HintPenalty, gameConfig.Star3Ratio, gameConfig.Star2Ratio, gameConfig.MaxTickSeconds);
            CreateSearch(lesson != null ? -1 : setup.Hints);
            navigationView.PracticeTitle = lesson != null ? "Bãi tập" : null;
            board.Build(session.Level, session.State, palette, configs.Cars, configs.Mechanics);
            animator.Initialize(board, feelConfig, session.State);
            if (comboFeedback != null)
            {
                comboFeedback.BeginAttempt();
            }

            input.Initialize(boardCamera, board, gameConfig);
            tableButtons.Bind(session, board, boardCamera, gameConfig);
            FitBoardCamera();
            tutorial.Begin(lesson, board, boardCamera, session);
            SetMessage("Chạm nửa xe để chạy");
            mechanicsDirty = false;
            lastCountdownSecond = -1;
            animator.Paused = false;
            SetBoardVisible(true);
            ApplyPlayGate();
            RefreshHud();
        }

        private void SetBoardVisible(bool visible)
        {
            board.SetVisible(visible);
            tableButtons.SetVisible(visible);
        }

        // Dò kẹt và tìm gợi ý bám theo một phiên chơi; tạo và hủy cùng nhau.
        private void CreateSearch(int hintCount)
        {
            stuck = new StuckDetector(session, gameConfig.SearchMaxStates);
            hints = new HintProvider(session, timer, hintCount, gameConfig.SearchMaxStates);
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

            if (!tutorial.Allows(command, input.LastCommandWasSwipe))
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
            tutorial.Moved();
            SetMessage("");
            RefreshControls();
        }

        private void RefreshControls()
        {
            tableButtons.Refresh();
            ApplyPlayGate();
            RefreshHud();
        }

        public void Undo()
        {
            if (!CanPlay || session == null || session.IsWin || !tutorial.AllowsUndo || !session.Undo())
            {
                return;
            }

            timer.ChargeUndo();
            animator.CancelAndSnap(session.State);
            mechanicsDirty = false;
            tutorial.Undone();
            SetMessage("Đã hoàn tác");
            RefreshControls();
            feedback.Play(SoundCue.Undo);
            SetStuck(false);
        }

        private void RequestHint()
        {
            if (!CanPlay || session == null || timer.IsOver || session.IsWin || !tutorial.AllowsHint)
            {
                return;
            }

            SetMessage(hints.Request() ? "Đang tìm gợi ý…" : "Đã hết gợi ý");
            RefreshHud();
            feedback.Play(SoundCue.Hint);
        }

        private void Update()
        {
            if (navigationView.RefreshSafeArea() && session != null)
            {
                FitBoardCamera();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Navigate(NavigationAction.Back, -1);
            }

            ApplyPlayGate();
            if (session == null)
            {
                return;
            }

            // Đồng hồ chọn unscaled có chủ đích; chỉ trạng thái thắng/thua mới pause, kẹt vẫn đếm.
            timer.Tick(Time.unscaledDeltaTime);
            ApplyPlayGate();
            if (CanPlay)
            {
                CheckLevelEnd();
            }

            // Bước giải thích không nhận lệnh nhưng vẫn dò kẹt để viền đỏ hiện đúng lúc.
            if (input.Accepting || tutorial.Active && CanPlay)
            {
                PollSearch();
            }

            if (input.Accepting)
            {
                PlayCountdown();
            }

            if (mechanicsDirty && !animator.IsBusy)
            {
                board.RefreshMechanics(session.State);
                mechanicsDirty = false;
            }

            ApplyPlayGate();
            hudIn -= Time.unscaledDeltaTime;
            if (hudIn <= 0)
            {
                hudIn = .1f;
                RefreshHud();
            } // Chữ đồng hồ chỉ cập nhật 10 Hz, không tạo chuỗi mỗi frame.
        }

        private void CheckLevelEnd()
        {
            // Bài tập kết thúc khi bãi tập thắng hoặc hết bước.
            if (session.IsWin || tutorial.Finished)
            {
                // Core thắng trước hình. Đợi mọi xe chạy hết rồi mới chúc mừng, không cắt xe cuối bằng lớp mờ.
                if (animator.IsBusy)
                {
                    return;
                }

                if (session.IsWin && !winCelebrating)
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
                    if (tutorial.Active)
                    {
                        CompleteLesson();
                        return;
                    }

                    progress.RecordWin(order[selected], timer.StarsOnWin());
                    FinishAttempt(NavigationAction.Won);
                }
            }
            else if (timer.IsOver)
            {
                feedback.Result(false);
                FinishAttempt(NavigationAction.Lost);
            }
        }

        private void FinishAttempt(NavigationAction result)
        {
            // Navigate bị bỏ qua khi đang chuyển cảnh; chỉ chốt combo khi lượt thật sự kết thúc.
            if (!CanPlay)
            {
                return;
            }

            if (comboFeedback != null)
            {
                comboFeedback.FinishAttempt(progress, order[selected]);
            }

            Navigate(result, -1);
        }

        // Đánh dấu đã học rồi nạp lại vị trí đang chọn: còn bài thì vào bài kế, hết thì vào màn.
        private void CompleteLesson()
        {
            if (!CanPlay || !tutorial.Active)
            {
                return;
            }

            progress.MarkTutorialSeen(tutorial.Lesson.Key);
            tutorial.End();
            Navigate(NavigationAction.Restart, -1);
        }

        private void SkipLesson()
        {
            if (CanPlay && tutorial.Active)
            {
                feedback.Play(SoundCue.Hint);
                CompleteLesson();
            }
        }

        private void ResetTutorials()
        {
            for (int i = 0; i < lessons.Length; i++)
            {
                progress.ResetTutorial(lessons[i].Key);
            }

            navigationView.ShowTutorialReset("Đã bật lại!");
        }

        // Gợi ý và dò kẹt chia nhau một ngân sách thời gian mỗi frame.
        private void PollSearch()
        {
            var budget = SearchBudget.Start(gameConfig.SearchMilliseconds);
            hints.Poll(budget);
            stuck.Poll(budget);
            if (hints.TryTake(out var move))
            {
                // Có lớp hướng dẫn thì chỉ bằng bàn tay; chữ "xe N" chỉ còn dùng khi thiếu lớp này.
                SetMessage(tutorial.ShowHint(move) ? "Làm theo bàn tay" : HintText(move));
                RefreshHud();
            }
            else if (hints.HasFinished && !hints.HasHint)
            {
                SetMessage("Chưa tìm được gợi ý trong ngân sách");
                hints.Cancel();
                RefreshHud();
            }

            bool isStuck = stuck.Status == StuckStatus.Stuck;
            if (isStuck && !navigator.IsStuck)
            {
                feedback.Play(SoundCue.Stuck);
            }

            SetStuck(isStuck);
        }

        private static string HintText(SolverMove move)
        {
            string number = (move.Index + 1).ToString();
            switch (move.Kind)
            {
                case SolverMoveKind.MoveCar: return "Gợi ý: xe " + number + (move.Sign > 0 ? " → / ↓" : " ← / ↑");
                case SolverMoveKind.RotateTable: return "Gợi ý: xoay xe " + number;
                default: return "Gợi ý: xoay trụ " + number;
            }
        }

        private void PlayCountdown()
        {
            int second = Mathf.CeilToInt(timer.Remaining);
            if (timer.IsTimed && timer.Remaining / timer.TimeLimit < gameConfig.LowTimeRatio && second != lastCountdownSecond)
            {
                lastCountdownSecond = second;
                feedback.Play(SoundCue.Countdown);
            }
        }

        private void SetMessage(string value)
        {
            // Bãi tập đã có dòng hướng dẫn, không chồng thêm bong bóng thông báo.
            if (tutorial != null && tutorial.Active)
            {
                value = "";
            }

            message = value;
            // Gửi theo sự kiện để thông báo trùng nội dung vẫn hiện lại; tick HUD 10 Hz không kéo dài tuổi bong bóng.
            navigationView.ShowMessage(value);
        }

        private void FitBoardCamera()
        {
            board.FitCamera(boardCamera, cameraConfig, NavigationView.BoardViewport(cameraConfig.PlayingBottomInset));
        }

        private void RefreshHud()
        {
            if (session == null)
            {
                return;
            }

            navigationView.RefreshHud("Màn " + order[selected] + " · " + levelName, timer, session.State.Moves, hints.Remaining, message, CanPlay && !session.IsWin && session.UndoCount > 0, !timer.Paused, order.Length, selected, gameConfig);
        }

        private void ApplyPlayGate()
        {
            bool playing = CanPlay && !clockPaused && session != null && !session.IsWin && timer != null && !timer.IsOver;
            bool boardOpen = playing && (tutorial == null || tutorial.AcceptsBoard);
            input.Accepting = boardOpen;
            tableButtons.SetAccepting(boardOpen);
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
            if (navigator == null || transitioning)
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
                        tutorial.End();
                        navigationView.PracticeTitle = null;
                        DisposeSearch();
                        session = null;
                        timer = null;
                        SetBoardVisible(false);
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
                        if (comboFeedback != null)
                        {
                            comboFeedback.ShowScreen(command.Screen);
                        }

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

        private void SetStuck(bool value)
        {
            var commands = navigator.SetStuck(value);
            for (int i = 0; i < commands.Count; i++)
            {
                navigationView.ShowStuck(commands[i].Kind == NavigationCommandKind.ShowStuck);
            }
        }

        private void LateUpdate()
        {
            tableButtons.UpdatePositions();
            if (session != null)
            {
                tutorial.Tick(CanPlay && !clockPaused && !session.IsWin, animator.IsBusy || mechanicsDirty, Time.unscaledDeltaTime);
            }
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
