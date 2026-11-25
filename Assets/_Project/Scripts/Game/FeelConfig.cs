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
        [Header("012 — Thân và bánh (số khởi điểm Claude)")]
        [SerializeField, Tooltip("Bật nhún/chúi/rướn thân xe.")] private bool bodyEnabled = true;
        [SerializeField, Tooltip("Bật quay bánh theo quãng đường trượt.")] private bool wheelsEnabled = true;
        [SerializeField, Tooltip("Bật lún/rũ bùn và giật xe bị va.")] private bool mudBodyEnabled = true;
        [SerializeField, Range(-15, 0), Tooltip("Độ ngóc mũi lúc khởi hành, độ.")] private float launchPitch = -5;
        [SerializeField, Range(.01f, .5f), Tooltip("Nhịp khởi hành, giây.")] private float launchSeconds = .08f;
        [SerializeField, Range(0, 20), Tooltip("Độ chúi mũi khi phanh/va, độ.")] private float brakePitch = 8;
        [SerializeField, Range(0, 10), Tooltip("Độ bật ngược sau khi chúi, độ.")] private float brakeRebound = 3;
        [SerializeField, Range(.05f, 1), Tooltip("Tổng nhịp phanh và bật lại, giây.")] private float brakeSeconds = .22f;
        [SerializeField, Range(.5f, 1), Tooltip("Ép chiều dài khi phanh.")] private float brakeLength = .94f;
        [SerializeField, Range(1, 1.5f), Tooltip("Nở chiều cao khi phanh.")] private float brakeHeight = 1.04f;
        [SerializeField, Range(.01f, .5f), Tooltip("Nhịp ép thân khi phanh, giây.")] private float squashSeconds = .06f;
        [SerializeField, Range(1, 1.5f), Tooltip("Kéo dài theo thân khi ra cổng.")] private float exitStretch = 1.06f;
        [SerializeField, Range(-15, 0), Tooltip("Độ ngóc mũi khi ra cổng, độ.")] private float exitPitch = -4;
        [SerializeField, Range(0, .2f), Tooltip("Hạ thân khi lội bùn, ô.")] private float mudSink = .03f;
        [SerializeField, Range(.5f, 1), Tooltip("Ép chiều cao lúc lội bùn.")] private float mudHeight = .97f;
        [SerializeField, Range(.01f, 1), Tooltip("Nhịp lội bùn, giây.")] private float mudSeconds = .12f;
        [SerializeField, Range(0, 20), Tooltip("Biên độ rũ bùn quanh trục thân, độ.")] private float washRoll = 6;
        [SerializeField, Range(1, 6), Tooltip("Số nhịp rũ bùn.")] private int washCycles = 3;
        [SerializeField, Range(.05f, 1), Tooltip("Tổng nhịp rũ bùn, giây.")] private float washSeconds = .35f;
        [SerializeField, Range(.01f, 1), Tooltip("Thời gian lớp bùn mờ dần, giây.")] private float mudFadeSeconds = .2f;
        [SerializeField, Range(0, .2f), Tooltip("Giật xe đứng yên bị đụng theo hướng va, ô.")] private float hitDistance = .05f;
        [SerializeField, Range(.01f, .5f), Tooltip("Nhịp xe bị đụng, giây.")] private float hitSeconds = .12f;
        [Header("012 — Hạt và camera")]
        [SerializeField, Tooltip("Bật hạt ở cổng nước/vòi xịt.")] private bool waterParticlesEnabled = true;
        [SerializeField, Tooltip("Bật bọt và lấp lánh khi ra cổng màu.")] private bool gateParticlesEnabled = true;
        [SerializeField, Tooltip("Bật bụi va.")] private bool impactParticlesEnabled = true;
        [SerializeField, Tooltip("Bật pháo giấy khi thắng.")] private bool confettiEnabled = true;
        [SerializeField, Tooltip("Bật rung camera lúc va; không đổi camera config.")] private bool shakeEnabled = true;
        [SerializeField, Range(0, .15f), Tooltip("Biên độ rung camera, đơn vị thế giới.")] private float shakeDistance = .04f;
        [SerializeField, Range(.01f, .5f), Tooltip("Nhịp rung camera, giây.")] private float shakeSeconds = .1f;
        [SerializeField, Range(1, 12), Tooltip("Số dao động trong nhịp rung camera.")] private int shakeCycles = 3;
        [SerializeField, Range(1, 60), Tooltip("Trần hạt sống chung cho cả bãi.")] private int maxLiveParticles = 60;
        [SerializeField, Range(0, 4), Tooltip("Độ cao phát pháo giấy trên bãi, ô.")] private float confettiHeight = 3;
        [SerializeField, Range(0, 2), Tooltip("Nửa góc nón phun; tỉ lệ vận tốc ngang / vận tốc lên.")] private float particleSpread = .7f;
        [SerializeField, Range(0, 2), Tooltip("Độ tản nguồn pháo giấy theo kích thước bãi.")] private float confettiSpread = .4f;
        [SerializeField, Range(0, 1), Tooltip("Tỉ lệ hạt nước màu xanh, còn lại trắng.")] private float waterTintRatio = .65f;
        [SerializeField, Range(0, 1), Tooltip("Tỉ lệ tia nước, còn lại hơi nước.")] private float waterJetRatio = .7f;
        [SerializeField, Tooltip("Màu nước và hơi trắng theo phiếu012.")] private Color waterColor = new Color32(79, 195, 247, 255);
        [SerializeField, Tooltip("Màu hơi nước và sao.")] private Color sparkleColor = Color.white;
        [SerializeField, Tooltip("Màu bụi khi va.")] private Color impactColor = Color.gray;
        [SerializeField, Tooltip("Material bùn alpha tạo sẵn bằng Core Feel; không tạo material khi Play.")] private Material mudFadeMaterial = null;
        [SerializeField, Tooltip("14 hạt/0.4s: nước + hơi.")] private ParticleFeel water = new ParticleFeel(14, .4f, .13f, 1.8f, .2f);
        [SerializeField, Tooltip("10 bọt/0.5s theo màu xe.")] private ParticleFeel bubbles = new ParticleFeel(10, .5f, .17f, 1.2f, .1f);
        [SerializeField, Tooltip("4 sao/0.5s.")] private ParticleFeel sparkles = new ParticleFeel(4, .5f, .2f, 1.4f, .1f);
        [SerializeField, Tooltip("6 bụi/0.25s.")] private ParticleFeel impact = new ParticleFeel(6, .25f, .18f, .7f, .2f);
        [SerializeField, Tooltip("30 pháo giấy/1s, 6 màu xe.")] private ParticleFeel confetti = new ParticleFeel(30, 1, .14f, 1.5f, 1);

        public bool BodyEnabled => bodyEnabled;
        public bool WheelsEnabled => wheelsEnabled;
        public bool MudBodyEnabled => mudBodyEnabled;
        public float LaunchPitch => launchPitch;
        public float LaunchSeconds => launchSeconds;
        public float BrakePitch => brakePitch;
        public float BrakeRebound => brakeRebound;
        public float BrakeSeconds => brakeSeconds;
        public float BrakeLength => brakeLength;
        public float BrakeHeight => brakeHeight;
        public float SquashSeconds => squashSeconds;
        public float ExitStretch => exitStretch;
        public float ExitPitch => exitPitch;
        public float MudSink => mudSink;
        public float MudHeight => mudHeight;
        public float MudSeconds => mudSeconds;
        public float WashRoll => washRoll;
        public int WashCycles => washCycles;
        public float WashSeconds => washSeconds;
        public float MudFadeSeconds => mudFadeSeconds;
        public float HitDistance => hitDistance;
        public float HitSeconds => hitSeconds;
        public bool WaterParticlesEnabled => waterParticlesEnabled;
        public bool GateParticlesEnabled => gateParticlesEnabled;
        public bool ImpactParticlesEnabled => impactParticlesEnabled;
        public bool ConfettiEnabled => confettiEnabled;
        public bool ShakeEnabled => shakeEnabled;
        public float ShakeDistance => shakeDistance;
        public float ShakeSeconds => shakeSeconds;
        public int ShakeCycles => shakeCycles;
        public int MaxLiveParticles => maxLiveParticles;
        public float ConfettiHeight => confettiHeight;
        public float ParticleSpread => particleSpread;
        public float ConfettiSpread => confettiSpread;
        public float WaterTintRatio => waterTintRatio;
        public float WaterJetRatio => waterJetRatio;
        public Color WaterColor => waterColor;
        public Color SparkleColor => sparkleColor;
        public Color ImpactColor => impactColor;
        public Material MudFadeMaterial => mudFadeMaterial;
        public ParticleFeel Water => water;
        public ParticleFeel Bubbles => bubbles;
        public ParticleFeel Sparkles => sparkles;
        public ParticleFeel Impact => impact;
        public ParticleFeel Confetti => confetti;
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
            launchPitch = Limit(launchPitch, -15, 0, -5);
            launchSeconds = Limit(launchSeconds, .01f, .5f, .08f);
            brakePitch = Limit(brakePitch, 0, 20, 8);
            brakeRebound = Limit(brakeRebound, 0, 10, 3);
            brakeSeconds = Limit(brakeSeconds, .05f, 1, .22f);
            brakeLength = Limit(brakeLength, .5f, 1, .94f);
            brakeHeight = Limit(brakeHeight, 1, 1.5f, 1.04f);
            squashSeconds = Limit(squashSeconds, .01f, .5f, .06f);
            exitStretch = Limit(exitStretch, 1, 1.5f, 1.06f);
            exitPitch = Limit(exitPitch, -15, 0, -4);
            mudSink = Limit(mudSink, 0, .2f, .03f);
            mudHeight = Limit(mudHeight, .5f, 1, .97f);
            mudSeconds = Limit(mudSeconds, .01f, 1, .12f);
            washRoll = Limit(washRoll, 0, 20, 6);
            washCycles = Mathf.Clamp(washCycles, 1, 6);
            washSeconds = Limit(washSeconds, .05f, 1, .35f);
            mudFadeSeconds = Limit(mudFadeSeconds, .01f, 1, .2f);
            hitDistance = Limit(hitDistance, 0, .2f, .05f);
            hitSeconds = Limit(hitSeconds, .01f, .5f, .12f);
            shakeDistance = Limit(shakeDistance, 0, .15f, .04f);
            shakeSeconds = Limit(shakeSeconds, .01f, .5f, .1f);
            shakeCycles = Mathf.Clamp(shakeCycles, 1, 12);
            maxLiveParticles = Mathf.Clamp(maxLiveParticles, 1, 60);
            confettiHeight = Limit(confettiHeight, 0, 4, 3);
            particleSpread = Limit(particleSpread, 0, 2, .7f);
            confettiSpread = Limit(confettiSpread, 0, 2, .4f);
            waterTintRatio = Limit(waterTintRatio, 0, 1, .65f);
            waterJetRatio = Limit(waterJetRatio, 0, 1, .7f);
            water.Validate();
            bubbles.Validate();
            sparkles.Validate();
            impact.Validate();
            confetti.Validate();
        }

        private static float Limit(float value, float min, float max, float fallback)
        {
            return Mathf.Clamp(GameConfig.Clean(value, min, fallback), min, max);
        }

        [System.Serializable]
        public sealed class ParticleFeel
        {
            [SerializeField, Range(0, 60), Tooltip("Số hạt yêu cầu mỗi lần; còn chịu trần chung.")] private int count;
            [SerializeField, Range(.01f, 3), Tooltip("Tuổi thọ hạt, giây.")] private float lifetime;
            [SerializeField, Range(.01f, 1), Tooltip("Cỡ hạt theo ô.")] private float size;
            [SerializeField, Range(0, 5), Tooltip("Vận tốc phát, ô/giây.")] private float speed;
            [SerializeField, Range(0, 2), Tooltip("Tỉ lệ trọng lực.")] private float gravity;
            public int Count => count;
            public float Lifetime => lifetime;
            public float Size => size;
            public float Speed => speed;
            public float Gravity => gravity;
            public ParticleFeel(int particles, float seconds, float cells, float velocity, float fall)
            {
                count = particles;
                lifetime = seconds;
                size = cells;
                speed = velocity;
                gravity = fall;
            }
            internal void Validate()
            {
                count = Mathf.Clamp(count, 0, 60);
                lifetime = Limit(lifetime, .01f, 3, .4f);
                size = Limit(size, .01f, 1, .15f);
                speed = Limit(speed, 0, 5, 1);
                gravity = Limit(gravity, 0, 2, .2f);
            }
        }
    }
}
