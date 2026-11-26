using UnityEngine;
namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Camera Config")]
    public sealed class CameraConfig : ScriptableObject
    {
        [SerializeField, Range(45, 90), Tooltip("Góc nhìn xuống từ mặt phẳng bàn.")] private float tiltDegrees = 65;
        [SerializeField, Min(0), Tooltip("Khoảng trống quanh bãi, theo ô.")] private float padding = .5f;
        [SerializeField, Range(.08f, .25f), Tooltip("Phần safe area phía dưới dành cho HUD chơi chính thức.")] private float playingBottomInset = .17f;
        public float TiltDegrees => tiltDegrees; public float Padding => padding;
        public float PlayingBottomInset => playingBottomInset;
        private void OnValidate()
        {
            tiltDegrees = Mathf.Clamp(GameConfig.Clean(tiltDegrees, 45, 65), 45, 90);
            padding = GameConfig.Clean(padding, 0, .5f);
            playingBottomInset = Mathf.Clamp(GameConfig.Clean(playingBottomInset, .08f, .17f), .08f, .25f);
        }
    }
}
