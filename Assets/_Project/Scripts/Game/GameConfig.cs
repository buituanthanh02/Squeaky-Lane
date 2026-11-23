using UnityEngine;

namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(0), Tooltip("Giây nền khi màn chưa ghi đè thời gian.")] private float baseSeconds = 20;
        [SerializeField, Min(0), Tooltip("Giây cộng cho mỗi nước tối ưu.")] private float secondsPerMove = 5;
        [SerializeField, Min(0), Tooltip("Giây phạt mỗi hoàn tác có hiệu lực.")] private float undoPenalty = 3;
        [SerializeField, Min(0), Tooltip("Số gợi ý cho màn thường.")] private int hintsPerLevel = 3;
        [SerializeField, Min(0), Tooltip("Giây phạt khi nhận được gợi ý.")] private float hintPenalty = 5;
        [SerializeField, Range(0, 1), Tooltip("Tỷ lệ giờ còn lại để đạt ba sao.")] private float star3Ratio = .5f;
        [SerializeField, Range(0, 1), Tooltip("Tỷ lệ giờ còn lại để đạt hai sao.")] private float star2Ratio = .2f;
        [SerializeField, Min(1), Tooltip("FPS mục tiêu, chỉ GameBootstrap áp dụng.")] private int targetFps = 60;
        [SerializeField, Min(0), Tooltip("Số màn đầu bỏ đồng hồ và không giới hạn gợi ý.")] private int tutorialLevelsUntimed = 0;
        [SerializeField, Min(.1f), Tooltip("Ngân sách chung tìm gợi ý/kẹt mỗi frame, giới hạn mềm theo ms.")] private float searchMilliseconds = 2;
        [SerializeField, Min(1), Tooltip("Hết ngân sách trạng thái chỉ là chưa kết luận, không phải kẹt.")] private int searchMaxStates = 300000;
        [SerializeField, Min(1), Tooltip("Số nước tạm tính giờ cho màn 17–19 chưa biết tối ưu.")] private int unknownMinimumMoves = 20;
        [SerializeField, Range(.01f, .3f), Tooltip("Ngưỡng vuốt theo cạnh màn hình ngắn, dùng khi DPI không đáng tin.")] private float swipeShortSideRatio = .06f;
        [SerializeField, Min(9), Tooltip("Đường kính vùng chạm nút bàn xoay, mm; cần thử máy thật.")] private float tableTouchMillimeters = 9;
        [SerializeField, Tooltip("Mở mọi màn cho lead thử; tắt để màn kế chỉ mở sau khi thắng màn trước theo level_order.")] private bool unlockAll = true;
        [SerializeField, Range(0, 1), Tooltip("Tỷ lệ giờ còn lại để đổi màu đồng hồ và báo tiếng đếm ngược.")] private float lowTimeRatio = .2f;
        public float BaseSeconds => baseSeconds;
        public float SecondsPerMove => secondsPerMove;
        public float UndoPenalty => undoPenalty;
        public int HintsPerLevel => hintsPerLevel;
        public float HintPenalty => hintPenalty;
        public float Star3Ratio => star3Ratio;
        public float Star2Ratio => star2Ratio;
        public int TargetFps => targetFps;
        public int TutorialLevelsUntimed => tutorialLevelsUntimed;
        public float SearchMilliseconds => searchMilliseconds;
        public int SearchMaxStates => searchMaxStates;
        public int UnknownMinimumMoves => unknownMinimumMoves;
        public float SwipeShortSideRatio => swipeShortSideRatio;
        public float TableTouchMillimeters => tableTouchMillimeters;
        public bool UnlockAll => unlockAll;
        public float LowTimeRatio => lowTimeRatio;
        private void OnValidate()
        {
            baseSeconds = Clean(baseSeconds, 0, 20); secondsPerMove = Clean(secondsPerMove, 0, 5);
            undoPenalty = Clean(undoPenalty, 0, 3); hintPenalty = Clean(hintPenalty, 0, 5);
            star3Ratio = Mathf.Clamp01(Clean(star3Ratio, 0, .5f)); star2Ratio = Mathf.Min(star3Ratio, Mathf.Clamp01(Clean(star2Ratio, 0, .2f)));
            hintsPerLevel = Mathf.Max(0, hintsPerLevel); targetFps = Mathf.Max(1, targetFps); tutorialLevelsUntimed = Mathf.Max(0, tutorialLevelsUntimed);
            searchMilliseconds = Clean(searchMilliseconds, .1f, 2); searchMaxStates = Mathf.Max(1, searchMaxStates);
            unknownMinimumMoves = Mathf.Max(1, unknownMinimumMoves); swipeShortSideRatio = Mathf.Clamp(Clean(swipeShortSideRatio, .01f, .06f), .01f, .3f);
            tableTouchMillimeters = Clean(tableTouchMillimeters, 9, 9);
            lowTimeRatio = Mathf.Clamp01(Clean(lowTimeRatio, 0, .2f));
        }
        internal static float Clean(float value, float minimum, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(minimum, value);
    }
}
