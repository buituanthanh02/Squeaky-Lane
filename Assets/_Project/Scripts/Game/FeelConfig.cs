using UnityEngine;
namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Feel Config")]
    public sealed class FeelConfig : ScriptableObject
    {
        [SerializeField, Min(.01f), Tooltip("Thời gian trượt một ô; số tạm để chơi khối.")] private float stepSeconds = .1f;
        [SerializeField, Min(.01f), Tooltip("Thời gian xe ra cổng.")] private float exitSeconds = .25f;
        [SerializeField, Min(.01f), Tooltip("Thời gian xoay bàn hoặc trụ.")] private float rotateSeconds = .25f;
        [SerializeField, Min(.01f), Tooltip("Thời gian biến mất/hiện qua portal.")] private float teleSeconds = .2f;
        [SerializeField, Min(.01f), Tooltip("Thời gian nảy nhẹ lúc dừng.")] private float bumpSeconds = .08f;
        [SerializeField, Range(0, .2f), Tooltip("Độ nảy theo ô, không ảnh hưởng luật.")] private float bumpDistance = .04f;
        [SerializeField, Min(0), Tooltip("Độ chui vào cổng nước theo ô; số tạm cho khối.")] private float waterDipCells = .6f;
        [SerializeField, Range(.1f, 1), Tooltip("Thu nhỏ giữa lúc xoay để hình không quệt xe cạnh bàn; số tạm.")] private float rotateMidScale = .65f;
        public float StepSeconds => stepSeconds; public float ExitSeconds => exitSeconds;
        public float RotateSeconds => rotateSeconds; public float TeleSeconds => teleSeconds;
        public float BumpSeconds => bumpSeconds; public float BumpDistance => bumpDistance;
        public float WaterDipCells => waterDipCells; public float RotateMidScale => rotateMidScale;
        private void OnValidate()
        {
            stepSeconds = GameConfig.Clean(stepSeconds, .01f, .1f); exitSeconds = GameConfig.Clean(exitSeconds, .01f, .25f);
            rotateSeconds = GameConfig.Clean(rotateSeconds, .01f, .25f); teleSeconds = GameConfig.Clean(teleSeconds, .01f, .2f);
            bumpSeconds = GameConfig.Clean(bumpSeconds, .01f, .08f); bumpDistance = Mathf.Clamp(GameConfig.Clean(bumpDistance, 0, .04f), 0, .2f);
            waterDipCells = GameConfig.Clean(waterDipCells, 0, .6f); rotateMidScale = Mathf.Clamp(GameConfig.Clean(rotateMidScale, .1f, .65f), .1f, 1);
        }
    }
}
