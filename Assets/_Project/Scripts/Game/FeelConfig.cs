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
        [SerializeField, Min(0), Tooltip("Thời gian mờ khi chuyển màn/panel; số tạm.")] private float transitionSeconds = .2f;
        [SerializeField, Min(0), Tooltip("Chờ thêm sau khi xe cuối ra hết và phát chúc mừng; 0 là mở bảng ngay khi hoạt hình xong.")] private float winDialogDelaySeconds = 0;
        [SerializeField, Range(0, 1), Tooltip("Âm lượng SFX chung.")] private float sfxVolume = .7f;
        [SerializeField, Range(1, 16), Tooltip("Số tiếng tối đa cùng lúc, cấp phát pool một lần.")] private int sfxVoices = 8;
        [SerializeField, Min(.05f), Tooltip("Khoảng cách tối thiểu giữa hai nhịp rung.")] private float hapticGapSeconds = .12f;
        [SerializeField, Min(1), Tooltip("Độ dài rung va nhẹ, ms; số tạm cần thử máy thật.")] private int shortVibrationMs = 20;
        [SerializeField, Min(1), Tooltip("Độ dài rung thắng/thua, ms.")] private int resultVibrationMs = 90;
        [SerializeField, Range(1, 255), Tooltip("Biên độ trên máy hỗ trợ, Android 26 trở lên.")] private int vibrationAmplitude = 80;
        public float StepSeconds => stepSeconds; public float ExitSeconds => exitSeconds;
        public float RotateSeconds => rotateSeconds; public float TeleSeconds => teleSeconds;
        public float BumpSeconds => bumpSeconds; public float BumpDistance => bumpDistance;
        public float WaterDipCells => waterDipCells; public float RotateMidScale => rotateMidScale;
        public float TransitionSeconds => transitionSeconds; public float SfxVolume => sfxVolume;
        public float WinDialogDelaySeconds => winDialogDelaySeconds;
        public int SfxVoices => sfxVoices; public float HapticGapSeconds => hapticGapSeconds;
        public int ShortVibrationMs => shortVibrationMs; public int ResultVibrationMs => resultVibrationMs; public int VibrationAmplitude => vibrationAmplitude;
        private void OnValidate()
        {
            stepSeconds = GameConfig.Clean(stepSeconds, .01f, .1f); exitSeconds = GameConfig.Clean(exitSeconds, .01f, .25f);
            rotateSeconds = GameConfig.Clean(rotateSeconds, .01f, .25f); teleSeconds = GameConfig.Clean(teleSeconds, .01f, .2f);
            bumpSeconds = GameConfig.Clean(bumpSeconds, .01f, .08f); bumpDistance = Mathf.Clamp(GameConfig.Clean(bumpDistance, 0, .04f), 0, .2f);
            waterDipCells = GameConfig.Clean(waterDipCells, 0, .6f); rotateMidScale = Mathf.Clamp(GameConfig.Clean(rotateMidScale, .1f, .65f), .1f, 1);
            transitionSeconds = GameConfig.Clean(transitionSeconds, 0, .2f); sfxVolume = Mathf.Clamp01(GameConfig.Clean(sfxVolume, 0, .7f));
            winDialogDelaySeconds = GameConfig.Clean(winDialogDelaySeconds, 0, 0);
            sfxVoices = Mathf.Clamp(sfxVoices, 1, 16); hapticGapSeconds = GameConfig.Clean(hapticGapSeconds, .05f, .12f);
            shortVibrationMs = Mathf.Max(1, shortVibrationMs); resultVibrationMs = Mathf.Max(1, resultVibrationMs); vibrationAmplitude = Mathf.Clamp(vibrationAmplitude, 1, 255);
        }
    }
}
