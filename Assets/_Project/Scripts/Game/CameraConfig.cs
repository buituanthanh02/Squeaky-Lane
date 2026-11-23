using UnityEngine;
namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Camera Config")]
    public sealed class CameraConfig : ScriptableObject
    {
        [SerializeField, Range(45, 90), Tooltip("Góc nhìn xuống từ mặt phẳng bàn.")] private float tiltDegrees = 65;
        [SerializeField, Min(0), Tooltip("Khoảng trống quanh bãi, theo ô.")] private float padding = .5f;
        public float TiltDegrees => tiltDegrees; public float Padding => padding;
        private void OnValidate() { tiltDegrees = Mathf.Clamp(GameConfig.Clean(tiltDegrees, 45, 65), 45, 90); padding = GameConfig.Clean(padding, 0, .5f); }
    }
}
