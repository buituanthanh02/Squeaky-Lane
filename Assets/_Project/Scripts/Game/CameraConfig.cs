using UnityEngine;
namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Camera Config")]
    public sealed class CameraConfig : ScriptableObject
    {
        [SerializeField, Range(45, 90), Tooltip("Góc nhìn xuống từ mặt phẳng bàn.")] private float tiltDegrees = 65;
        [SerializeField, Min(0), Tooltip("Khoảng trống quanh bãi, theo ô.")] private float padding = .5f;
        [SerializeField, Range(.08f, .25f), Tooltip("Phần safe area phía dưới dành cho HUD chơi chính thức.")] private float playingBottomInset = .17f;
        [SerializeField, Min(1), Tooltip("Tới số ô này, camera chừa thêm chỗ để nhìn phố.")] private int wideBoardCells = 25;
        [SerializeField, Min(2), Tooltip("Từ số ô này, camera ưu tiên kích thước bãi và khung cổng.")] private int tightBoardCells = 36;
        [SerializeField, Min(0), Tooltip("Số ô chừa thêm mỗi phía ở bãi nhỏ.")] private float wideBoardPadding = .32f;
        [SerializeField, Min(1.1f), Tooltip("Khoảng chừa ngoài mép cho khung hầm rửa khi zoom sát, tính theo ô.")] private float washGateFrameReach = 1.1f;
        public float TiltDegrees => tiltDegrees; public float Padding => padding;
        public float PlayingBottomInset => playingBottomInset;
        [SerializeField, Min(1.1f), Tooltip("Khoảng chừa tối đa cho hầm rửa ở bãi ít ô; xe chạy xa hơn thì cho ra ngoài khung để bãi không bé đi.")] private float washGateSceneryReach = 1.8f;
        public float WashGateSceneryReach => washGateSceneryReach;
        public float WideBoardPadding => wideBoardPadding;
        public float WashGateFrameReach => washGateFrameReach;

        // Âm: mở rộng khung nhỏ để thấy phố. Dương: nhường phần cảnh ngoài bãi để ô chơi lớn hơn.
        public float SceneryFraming(int cells) => 2 * Mathf.InverseLerp(wideBoardCells, tightBoardCells, cells) - 1;

        private void OnValidate()
        {
            tiltDegrees = Mathf.Clamp(GameConfig.Clean(tiltDegrees, 45, 65), 45, 90);
            padding = GameConfig.Clean(padding, 0, .5f);
            playingBottomInset = Mathf.Clamp(GameConfig.Clean(playingBottomInset, .08f, .17f), .08f, .25f);
            wideBoardCells = Mathf.Clamp(wideBoardCells, 1, int.MaxValue - 1);
            tightBoardCells = Mathf.Max(wideBoardCells + 1, tightBoardCells);
            wideBoardPadding = GameConfig.Clean(wideBoardPadding, 0, .32f);
            washGateFrameReach = GameConfig.Clean(washGateFrameReach, 1.1f, 1.1f);
            washGateSceneryReach = Mathf.Max(washGateFrameReach, GameConfig.Clean(washGateSceneryReach, 1.1f, 1.8f));
        }
    }
}
