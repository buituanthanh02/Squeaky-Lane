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
            RefreshLevels(progress, unlockAll); RefreshSettings(progress); SetFade(0); ShowScreen(ScreenState.LevelSelect); ShowStuck(false); RefreshSafeArea();
        }
        private void Send(NavigationAction action) { ButtonClicked?.Invoke(); Navigate?.Invoke(action, -1); }
        public void RefreshLevels(ProgressStore progress, bool unlockAll)
        {
            for (int i = 0; i < ids.Length; i++)
            {
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
            Group(playing, state != ScreenState.LevelSelect); // HUD vẫn thấy sau lớp tối của dialog.
            Group(paused, state == ScreenState.Paused); Group(won, state == ScreenState.Won); Group(lost, state == ScreenState.Lost);
            playing.interactable = playing.blocksRaycasts = state == ScreenState.Playing;
        }
        private static void Group(CanvasGroup group, bool visible)
        { group.alpha = visible ? 1 : 0; group.interactable = visible; group.blocksRaycasts = visible; }
        public void ShowStuck(bool visible)
        { stuckBanner.alpha = visible ? 1 : 0; stuckBanner.interactable = false; stuckBanner.blocksRaycasts = false; }
        public void SetFade(float alpha)
        { if (fadeOverlay == null) return; var color = fadeOverlay.color; color.a = alpha; fadeOverlay.color = color; fadeOverlay.raycastTarget = alpha > 0; }
        public void RefreshHud(string title, LevelTimer timer, int moves, int hintCount, string message, bool canUndo, bool canPlay, int totalLevels, int index, GameConfig config)
        {
            bool skinned = theme != null && theme.HasHudSkin;
            levelTitle.text = skinned && ids != null ? "Màn " + ids[index] : title;
            movesText.text = moves + " nước";
            clockText.text = timer.IsTimed ? Mathf.CeilToInt(timer.Remaining) + " s" : skinned ? "∞" : "Không tính giờ";
            float ratio = timer.IsTimed ? timer.Remaining / timer.TimeLimit : 1;
            clockFill.fillAmount = ratio;
            clockFill.color = ratio < config.LowTimeRatio ? lowClock : normalClock;
            if (skinned && clockFill.type == Image.Type.Sliced)
            {
                // Filled bỏ qua Border. Sliced thu rect trong track, giữ hai đầu bo ở mọi chiều rộng.
                var rect = clockFill.rectTransform;
                rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
                clockFill.enabled = ratio > 0;
            }
            if (!skinned)
            {
                messageText.text = message + "\nGợi ý: " + (hintCount < 0 ? "không giới hạn" : hintCount.ToString()) + " · phạt " + config.HintPenalty + " s";
            }
            else
            {
                if (undoBadge != null) { undoBadge.text = "-" + config.UndoPenalty + "s"; }
                if (hintBadge != null) { hintBadge.text = hintCount < 0 ? "∞" : hintCount.ToString(); }
                if (hintBadgePlate != null) { hintBadgePlate.color = hintCount == 0 ? theme.ExhaustedColor : theme.BadgeColor; }
                if (hintButton.targetGraphic is Image hintImage)
                {
                    hintImage.sprite = hintCount == 0 ? theme.RegularButton : theme.YellowSquare;
                    hintImage.color = hintCount == 0 ? Color.white : theme.ButtonYellow;
                }
            }
            undoButton.interactable = canUndo; hintButton.interactable = canPlay && hintCount != 0;
            // Giữ HUD hiện nhưng khóa các nút khi chờ hoạt hình/chúc mừng kết thúc màn.
            playing.interactable = canPlay;
            undoLabel.text = skinned ? "Hoàn tác" : (theme != null ? "<sprite name=undo> " : "") + "Hoàn tác (-" + config.UndoPenalty + "s)";
            hintLabel.text = skinned ? "Gợi ý" : (theme != null ? "<sprite name=hint> " : "") + "Gợi ý (-" + config.HintPenalty + "s)";
            wonText.text = "Thắng!\n" + (theme != null ? theme.Stars(timer.StarsOnWin()) : timer.StarsOnWin() + "/3 sao") + " · còn " + Mathf.CeilToInt(timer.Remaining) + " s";
            lostText.text = "Hết giờ";
            nextLabel.text = index + 1 < totalLevels ? "Màn kế" : "Chọn màn";
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
        public bool RefreshSafeArea()
        {
            if (lastSafe == Screen.safeArea && lastWidth == Screen.width && lastHeight == Screen.height) return false;
            lastSafe = Screen.safeArea; lastWidth = Screen.width; lastHeight = Screen.height;
            safeAreaRoot.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height); safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero; return true;
        }
    }
}
