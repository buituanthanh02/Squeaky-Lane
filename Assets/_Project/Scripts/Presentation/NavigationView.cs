using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class NavigationView : MonoBehaviour
    {
        [Header("Vùng an toàn và các panel do bạn dựng trong Canvas")]
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private CanvasGroup levelSelect, playing, paused, won, lost, stuckBanner;
        [SerializeField] private Image fadeOverlay;
        [Header("Danh sách màn")]
        [SerializeField] private Transform levelButtonContent;
        [SerializeField] private Button levelButtonPrefab;
        [Header("Chữ và thanh giờ")]
        [SerializeField] private TMP_Text levelTitle, clockText, movesText, messageText, wonText, lostText;
        [SerializeField] private Image clockFill;
        [SerializeField] private Color normalClock = new Color(.3f, .8f, 1), lowClock = new Color(1, .3f, .35f);
        [SerializeField, Tooltip("Skin013; thiếu giữ hiển thị cũ. Không sở hữu điều hướng.")] private UiTheme theme = null;
        [Header("Nút — không cần gán On Click thủ công")]
        [SerializeField] private Button pauseButton, resumeButton, nextButton, undoButton, hintButton;
        [SerializeField] private Button[] restartButtons = Array.Empty<Button>();
        [SerializeField] private Button[] levelSelectButtons = Array.Empty<Button>();
        [SerializeField] private Toggle[] soundToggles = Array.Empty<Toggle>(), hapticsToggles = Array.Empty<Toggle>();
        [Header("013 S2 · trang trí HUD; không phải control điều hướng")]
        [SerializeField] private TMP_Text undoBadge = null, hintBadge = null;
        [SerializeField] private Image hintBadgePlate = null;
        [SerializeField] private CanvasGroup messageBubble = null;
        [Header("013 S3 · nút phụ")]
        [SerializeField, Tooltip("Nút phụ cũng là Tiếp tục (vd dấu X ở góc hộp thoại Tạm dừng).")] private Button[] extraResumeButtons = Array.Empty<Button>();
        [Header("016 · Main menu (để trống = mở thẳng Chọn màn như trước). Menu Editor '016 UI/Build Main Menu' dựng và gán.")]
        [SerializeField] private CanvasGroup mainMenu = null, menuSettings = null;
        [SerializeField] private Button menuPlayButton = null, menuLevelsButton = null, menuSettingsButton = null;
        [SerializeField] private TMP_Text menuPlayLabel = null;
        [SerializeField, Tooltip("Nút về main menu (ví dụ nút nhà trên màn Chọn màn).")] private Button[] homeButtons = Array.Empty<Button>();
        [SerializeField, Tooltip("Nút đóng hộp Cài đặt của main menu.")] private Button[] menuSettingsCloseButtons = Array.Empty<Button>();
        [SerializeField, Tooltip("Sprite nền ô màn đang chơi tới (nổi bật). Trống = giữ sprite prefab.")] private Sprite levelCurrentSprite = null;
        [SerializeField, Tooltip("016: viền đỏ nhấp nháy khi kẹt xe; gán rồi thì không hiện bảng chữ Kẹt xe. Menu '016 UI/Build Stuck Edge' dựng và gán.")] private CanvasGroup stuckEdge = null;
        [SerializeField, Tooltip("019: nút Xem lại hướng dẫn trong Cài đặt của main menu. Menu '019 UI/Build Tutorial' dựng và gán.")] private Button replayTutorialButton = null;
        [SerializeField] private TMP_Text replayTutorialLabel = null;
        private string replayTutorialText;
        // 019: đang ở bãi tập thì ô tên màn ghi chữ này thay cho "Màn N".
        public string PracticeTitle { get; set; }
        public RectTransform PauseRect => pauseButton != null ? (RectTransform)pauseButton.transform : null;
        public RectTransform UndoRect => (RectTransform)undoButton.transform;
        public RectTransform HintRect => (RectTransform)hintButton.transform;
        public RectTransform ClockRect => clockFill.rectTransform;
        public bool StuckEdgeShowing => stuckEdge != null && stuckPulsing;
        public float StuckEdgeWidth => theme != null ? theme.StuckEdgeWidth : 0;
        public event Action ReplayTutorial;
        private bool stuckPulsing;
        private float stuckAge;
        private int nextLevelIndex;
        private Sprite levelNormalSprite;
        private float messageAge;
        private Button[] levelButtons;
        private TMP_Text[] levelLabels;
        private int[] ids;
        private Rect lastSafe;
        private int lastWidth, lastHeight;
        private bool initialized;
        private TMP_Text nextLabel, undoLabel, hintLabel;
        public event Action<NavigationAction, int> Navigate;
        public event Action Undo, Hint, ButtonClicked;
        public event Action<bool> SoundChanged, HapticsChanged;

        public void Initialize(int[] order, ProgressStore progress, bool unlockAll)
        {
            if (initialized) return;
            if (safeAreaRoot == null || levelSelect == null || playing == null || paused == null || won == null || lost == null || stuckBanner == null ||
                fadeOverlay == null || levelButtonContent == null || levelButtonPrefab == null || levelTitle == null || clockText == null || movesText == null ||
                messageText == null || wonText == null || lostText == null || clockFill == null || pauseButton == null || resumeButton == null || nextButton == null || undoButton == null || hintButton == null)
                throw new InvalidOperationException("NavigationView còn thiếu ô Inspector. Xem bảng gán trong hướng dẫn 009; chưa cần sửa scene bằng code.");
            initialized = true; ids = order;
            nextLabel = nextButton.GetComponentInChildren<TMP_Text>(true);
            if (nextLabel == null) throw new InvalidOperationException("Next Button cần TextMeshPro con");
            undoLabel = undoButton.GetComponentInChildren<TMP_Text>(true); hintLabel = hintButton.GetComponentInChildren<TMP_Text>(true);
            if (undoLabel == null || hintLabel == null) throw new InvalidOperationException("Undo/Hint Button cần TextMeshPro con");
            pauseButton.onClick.AddListener(() => Send(NavigationAction.Pause));
            resumeButton.onClick.AddListener(() => Send(NavigationAction.Resume));
            nextButton.onClick.AddListener(() => Send(NavigationAction.NextLevel));
            foreach (var button in extraResumeButtons) { if (button == null) throw new InvalidOperationException("Extra Resume Buttons có ô trống"); button.onClick.AddListener(() => Send(NavigationAction.Resume)); }
            undoButton.onClick.AddListener(() => { ButtonClicked?.Invoke(); Undo?.Invoke(); });
            hintButton.onClick.AddListener(() => { ButtonClicked?.Invoke(); Hint?.Invoke(); });
            foreach (var button in restartButtons) { if (button == null) throw new InvalidOperationException("Restart Buttons có ô trống"); button.onClick.AddListener(() => Send(NavigationAction.Restart)); }
            foreach (var button in levelSelectButtons) { if (button == null) throw new InvalidOperationException("Level Select Buttons có ô trống"); button.onClick.AddListener(() => Send(NavigationAction.LevelSelect)); }
            foreach (var toggle in soundToggles) if (toggle != null) toggle.onValueChanged.AddListener(value => SoundChanged?.Invoke(value));
            foreach (var toggle in hapticsToggles) if (toggle != null) toggle.onValueChanged.AddListener(value => HapticsChanged?.Invoke(value));
            levelButtons = new Button[ids.Length]; levelLabels = new TMP_Text[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                int index = i;
                // Bạn thiết kế một prefab nút; chỉ danh sách theo dữ liệu được nhân bản lúc chạy.
                var button = Instantiate(levelButtonPrefab, levelButtonContent); button.name = "Level " + ids[i];
                levelButtons[i] = button; levelLabels[i] = button.GetComponentInChildren<TMP_Text>(true);
                if (levelLabels[i] == null) throw new InvalidOperationException("Level Button Prefab phải có TextMeshPro con.");
                button.onClick.AddListener(() => { ButtonClicked?.Invoke(); Navigate?.Invoke(NavigationAction.SelectLevel, index); });
            }
            // 016: main menu nằm trên màn Chọn màn (cùng trạng thái LevelSelect của Navigator), nên không đổi luồng điều hướng tầng Game.
            if (menuPlayButton != null) { menuPlayButton.onClick.AddListener(() => { ButtonClicked?.Invoke(); Navigate?.Invoke(NavigationAction.SelectLevel, nextLevelIndex); }); }
            if (menuLevelsButton != null) { menuLevelsButton.onClick.AddListener(() => { ButtonClicked?.Invoke(); SetMainMenu(false); }); }
            if (menuSettingsButton != null) { menuSettingsButton.onClick.AddListener(() => { ButtonClicked?.Invoke(); SetMenuSettings(true); }); }
            foreach (var button in homeButtons) { if (button != null) { button.onClick.AddListener(() => { ButtonClicked?.Invoke(); SetMainMenu(true); }); } }
            foreach (var button in menuSettingsCloseButtons) { if (button != null) { button.onClick.AddListener(() => { ButtonClicked?.Invoke(); SetMenuSettings(false); }); } }
            if (replayTutorialButton != null) { replayTutorialText = replayTutorialLabel != null ? replayTutorialLabel.text : null; replayTutorialButton.onClick.AddListener(() => { ButtonClicked?.Invoke(); ReplayTutorial?.Invoke(); }); }
            RefreshLevels(progress, unlockAll); RefreshSettings(progress); SetFade(0); ShowScreen(ScreenState.LevelSelect); ShowStuck(false); RefreshSafeArea();
            SetMainMenu(mainMenu != null);
        }
        public bool MainMenuOpen => mainMenu != null && mainMenu.alpha > .5f;
        // Mở/đóng main menu; luôn đóng hộp Cài đặt của menu.
        public void SetMainMenu(bool open)
        {
            if (mainMenu != null) { Group(mainMenu, open); }
            SetMenuSettings(false);
        }
        // Báo cho người chơi biết bài tập sẽ hiện lại; mở Cài đặt lần sau chữ trở về như cũ.
        public void ShowTutorialReset(string confirmation)
        {
            if (replayTutorialLabel != null) { replayTutorialLabel.text = confirmation; }
        }
        private void SetMenuSettings(bool open)
        {
            if (open && replayTutorialLabel != null && replayTutorialText != null) { replayTutorialLabel.text = replayTutorialText; }
            if (menuSettings != null) { Group(menuSettings, open && MainMenuOpen); }
        }
        // Ô màn đang chơi tới đổi sang sprite nổi bật; các ô khác giữ sprite gốc của prefab.
        private void HighlightLevel(Button button, bool current)
        {
            if (levelCurrentSprite == null || button == null) { return; }
            var image = button.targetGraphic as Image;
            if (image == null) { return; }
            if (levelNormalSprite == null && image.sprite != levelCurrentSprite) { levelNormalSprite = image.sprite; }
            image.sprite = current ? levelCurrentSprite : levelNormalSprite;
        }
        private void Send(NavigationAction action) { ButtonClicked?.Invoke(); Navigate?.Invoke(action, -1); }
        public void RefreshLevels(ProgressStore progress, bool unlockAll)
        {
            // Màn "chơi tiếp" = màn mở khóa đầu tiên chưa có sao; hết thì màn mở khóa cuối cùng.
            nextLevelIndex = -1;
            int lastUnlocked = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                bool unlocked = progress.IsUnlocked(ids, i, unlockAll);
                if (unlocked) { lastUnlocked = i; }
                if (unlocked && nextLevelIndex < 0 && progress.Stars(ids[i]) == 0) { nextLevelIndex = i; }
            }
            if (nextLevelIndex < 0) { nextLevelIndex = lastUnlocked; }
            if (menuPlayLabel != null && ids.Length > 0) { menuPlayLabel.text = "MÀN " + ids[nextLevelIndex]; }
            for (int i = 0; i < ids.Length; i++)
            {
                HighlightLevel(levelButtons[i], i == nextLevelIndex);
                levelButtons[i].interactable = progress.IsUnlocked(ids, i, unlockAll);
                levelLabels[i].text = "Màn " + ids[i] + "\n" + (theme != null ? theme.Stars(progress.Stars(ids[i])) : progress.Stars(ids[i]) + "/3 sao");
            }
        }
        public void RefreshSettings(ProgressStore progress)
        {
            foreach (var toggle in soundToggles) if (toggle != null) toggle.SetIsOnWithoutNotify(progress.SoundEnabled);
            foreach (var toggle in hapticsToggles) if (toggle != null) toggle.SetIsOnWithoutNotify(progress.HapticsEnabled);
        }
        public void ShowScreen(ScreenState state)
        {
            Group(levelSelect, state == ScreenState.LevelSelect);
            if (state != ScreenState.LevelSelect) { SetMainMenu(false); }
            Group(playing, state != ScreenState.LevelSelect); // HUD vẫn thấy sau lớp tối của dialog.
            Group(paused, state == ScreenState.Paused); Group(won, state == ScreenState.Won); Group(lost, state == ScreenState.Lost);
            playing.interactable = playing.blocksRaycasts = state == ScreenState.Playing;
        }
        private static void Group(CanvasGroup group, bool visible)
        { group.alpha = visible ? 1 : 0; group.interactable = visible; group.blocksRaycasts = visible; }
        public bool HasStuckEdge => stuckEdge != null;
        public float StuckPeriod => theme != null ? theme.StuckPulseSeconds : .9f;
        public void ShowStuck(bool visible)
        {
            bool edge = stuckEdge != null;
            if (stuckBanner != null) { stuckBanner.alpha = visible && !edge ? 1 : 0; stuckBanner.interactable = false; stuckBanner.blocksRaycasts = false; }
            if (!edge) { return; }
            if (visible && !stuckPulsing) { stuckAge = 0; }
            stuckPulsing = visible;
            stuckEdge.interactable = false; stuckEdge.blocksRaycasts = false;
            stuckEdge.alpha = visible ? PulseAlpha() : 0;
        }
        // Nhấp nháy mềm theo hình cos, bắt đầu ở mức đậm nhất; dùng thời gian không scale.
        public void TickStuck(float deltaTime)
        {
            if (!stuckPulsing || stuckEdge == null) { return; }
            stuckAge = Mathf.Repeat(stuckAge + deltaTime, StuckPeriod);
            stuckEdge.alpha = PulseAlpha();
        }
        private float PulseAlpha()
        {
            float low = theme != null ? theme.StuckAlphaMin : .2f, high = theme != null ? theme.StuckAlphaMax : 1;
            return Mathf.Lerp(low, high, .5f + .5f * Mathf.Cos(stuckAge / StuckPeriod * 2 * Mathf.PI));
        }
        public void SetFade(float alpha)
        { if (fadeOverlay == null) return; var color = fadeOverlay.color; color.a = alpha; fadeOverlay.color = color; fadeOverlay.raycastTarget = alpha > 0; }
        public void RefreshHud(string title, LevelTimer timer, int moves, int hintCount, string message, bool canUndo, bool canPlay, int totalLevels, int index, GameConfig config)
        {
            bool skinned = theme != null && theme.HasHudSkin;
            levelTitle.text = PracticeTitle ?? (skinned && ids != null ? "Màn " + ids[index] : title);
            movesText.text = moves + " nước";
            clockText.text = timer.IsTimed ? (skinned ? FormatClock(timer.Remaining) : Mathf.CeilToInt(timer.Remaining) + " s") : skinned ? "∞" : "Không tính giờ";
            float ratio = timer.IsTimed ? timer.Remaining / timer.TimeLimit : 1;
            if (skinned)
            {
                // Không còn thanh chạy: giờ hiển thị dạng mm:ss trong một pill, sắp hết giờ thì đổi sang sprite pill đỏ.
                // clockFill giờ là Image nền của pill (giữ nguyên tham chiếu Inspector).
                var pill = timer.IsTimed && ratio < config.LowTimeRatio ? theme.ClockLow : theme.ClockNormal;
                if (pill != null) { clockFill.sprite = pill; }
                clockFill.color = Color.white;
            }
            else
            {
                clockFill.fillAmount = ratio;
                clockFill.color = ratio < config.LowTimeRatio ? lowClock : normalClock;
            }
            if (!skinned)
            {
                messageText.text = message + "\nGợi ý: " + (hintCount < 0 ? "không giới hạn" : hintCount.ToString()) + " · phạt " + config.HintPenalty + " s";
            }
            else
            {
                if (undoBadge != null) { undoBadge.text = "-" + config.UndoPenalty + "s"; }
                if (hintBadge != null) { hintBadge.text = hintCount < 0 ? "∞" : hintCount.ToString(); }
                // Sprite có sẵn màu + viền: đổi sprite chứ không tint (tint sẽ nhuộm cả viền).
                var plate = hintCount == 0 ? theme.BadgeOff : theme.BadgeOn;
                if (hintBadgePlate != null && plate != null) { hintBadgePlate.sprite = plate; }
                var square = hintCount == 0 ? theme.HintOff : theme.HintOn;
                if (hintButton.targetGraphic is Image hintImage && square != null) { hintImage.sprite = square; }
            }
            undoButton.interactable = canUndo; hintButton.interactable = canPlay && hintCount != 0;
            // Giữ HUD hiện nhưng khóa các nút khi chờ hoạt hình/chúc mừng kết thúc màn.
            playing.interactable = canPlay;
            undoLabel.text = skinned ? "Hoàn tác" : (theme != null ? "<sprite name=undo> " : "") + "Hoàn tác (-" + config.UndoPenalty + "s)";
            hintLabel.text = skinned ? "Gợi ý" : (theme != null ? "<sprite name=hint> " : "") + "Gợi ý (-" + config.HintPenalty + "s)";
            // Có skin: tiêu đề "Thắng!" nằm ở dải ribbon của thẻ nên chữ thân chỉ gồm sao và thời gian còn lại.
            int levelId = ids != null ? ids[index] : index + 1;
            wonText.text = skinned ? theme.Stars(timer.StarsOnWin()) + "\nMàn " + levelId + " · còn " + Mathf.CeilToInt(timer.Remaining) + " s"
                : "Thắng!\n" + (theme != null ? theme.Stars(timer.StarsOnWin()) : timer.StarsOnWin() + "/3 sao") + " · còn " + Mathf.CeilToInt(timer.Remaining) + " s";
            lostText.text = "Hết giờ";
            nextLabel.text = index + 1 < totalLevels ? "Màn kế" : "Chọn màn";
        }
        private static string FormatClock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
        public void ShowMessage(string message)
        {
            if (theme == null || !theme.HasHudSkin || messageBubble == null) { return; }
            messageText.text = message;
            messageAge = 0;
            messageBubble.alpha = string.IsNullOrEmpty(message) ? 0 : 1;
            messageBubble.interactable = false;
            messageBubble.blocksRaycasts = false;
        }
        private void Update()
        {
            TickMessage(Time.unscaledDeltaTime);
            TickStuck(Time.unscaledDeltaTime);
        }
        private void TickMessage(float deltaTime)
        {
            if (theme == null || messageBubble == null || messageBubble.alpha <= 0) { return; }
            messageAge += deltaTime;
            messageBubble.alpha = 1 - Mathf.Clamp01((messageAge - theme.MessageHoldSeconds) / theme.MessageFadeSeconds);
        }
        private void OnDisable()
        {
            if (messageBubble != null) { messageBubble.alpha = 0; }
        }
        // Khung nhìn (tỉ lệ 0–1) dành cho bãi: trong safe area, chừa đỉnh cho đồng hồ và đáy cho nút.
        public static Rect BoardViewport(float bottomInset)
        {
            const float topEdge = .86f;
            var area = Screen.safeArea;
            return new Rect(area.x / Screen.width, (area.y + area.height * bottomInset) / Screen.height,
                area.width / Screen.width, area.height * (topEdge - bottomInset) / Screen.height);
        }
        public bool RefreshSafeArea()
        {
            if (lastSafe == Screen.safeArea && lastWidth == Screen.width && lastHeight == Screen.height) return false;
            lastSafe = Screen.safeArea; lastWidth = Screen.width; lastHeight = Screen.height;
            safeAreaRoot.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height); safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero; return true;
        }
    }
}
