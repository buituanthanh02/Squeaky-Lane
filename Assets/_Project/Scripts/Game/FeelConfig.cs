using UnityEngine;
namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Feel Config")]
    public sealed class FeelConfig : ScriptableObject
    {
        [SerializeField, Min(.01f), Tooltip("Thời gian trượt một ô; số tạm để chơi khối.")] private float stepSeconds = .1f;
        [SerializeField, Min(.01f), Tooltip("Thời gian xe chui vào/ra cổng nước. Lượt ra cổng màu dùng Gate Pass Seconds.")] private float exitSeconds = .25f;
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
        [SerializeField, Tooltip("Bật lún/rũ bùn; cú giật khi bị va thuộc nhóm thân xe.")] private bool mudBodyEnabled = true;
        [SerializeField, Range(-15, 0), Tooltip("Độ ngóc mũi lúc khởi hành, độ.")] private float launchPitch = -5;
        [SerializeField, Range(.01f, .5f), Tooltip("Nhịp khởi hành, giây.")] private float launchSeconds = .08f;
        [SerializeField, Range(0, 20), Tooltip("Độ chúi mũi khi phanh/va, độ.")] private float brakePitch = 8;
        [SerializeField, Range(0, 10), Tooltip("Độ bật ngược sau khi chúi, độ.")] private float brakeRebound = 3;
        [SerializeField, Range(.05f, 1), Tooltip("Tổng nhịp phanh và bật lại, giây.")] private float brakeSeconds = .22f;
        [SerializeField, Range(.5f, 1), Tooltip("Ép chiều dài khi phanh.")] private float brakeLength = .90f;
        [SerializeField, Range(1, 1.5f), Tooltip("Phình ngang khi phanh để đọc được ở góc nhìn từ trên xuống.")] private float brakeWidth = 1.06f;
        [SerializeField, Range(1, 1.5f), Tooltip("Nở chiều cao khi phanh.")] private float brakeHeight = 1.04f;
        [SerializeField, Range(.01f, .5f), Tooltip("Nhịp ép thân khi phanh, giây.")] private float squashSeconds = .10f;
        [SerializeField, Range(1, 1.5f), Tooltip("Kéo dài theo thân khi ra cổng.")] private float exitStretch = 1.10f;
        [SerializeField, Range(-15, 0), Tooltip("Độ ngóc mũi khi ra cổng, độ.")] private float exitPitch = -4;
        [SerializeField, Range(0, .2f), Tooltip("Hạ thân khi lội bùn, ô.")] private float mudSink = .03f;
        [SerializeField, Range(.5f, 1), Tooltip("Ép chiều cao lúc lội bùn.")] private float mudHeight = .97f;
        [SerializeField, Range(.01f, 1), Tooltip("Nhịp lội bùn, giây.")] private float mudSeconds = .12f;
        [SerializeField, Range(0, 20), Tooltip("Biên độ rũ bùn quanh trục thân, độ.")] private float washRoll = 6;
        [SerializeField, Range(1, 6), Tooltip("Số nhịp rũ bùn.")] private int washCycles = 3;
        [SerializeField, Range(.05f, 1), Tooltip("Tổng nhịp rũ bùn, giây.")] private float washSeconds = .35f;
        [SerializeField, Range(.01f, 1), Tooltip("Thời gian lớp bùn mờ dần, giây.")] private float mudFadeSeconds = .2f;
        [SerializeField, Range(0, .2f), Tooltip("Giật xe đứng yên bị đụng theo hướng va, ô.")] private float hitDistance = .08f;
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
        [SerializeField, Range(0, 2), Tooltip("Nửa góc nón phun; tỉ lệ vận tốc ngang / vận tốc lên.")] private float particleSpread = 1.2f;
        [SerializeField, Range(0, 2), Tooltip("Độ tản nguồn pháo giấy theo kích thước bãi.")] private float confettiSpread = .4f;
        [SerializeField, Range(0, 1), Tooltip("Tỉ lệ hạt nước màu xanh, còn lại trắng.")] private float waterTintRatio = .65f;
        [SerializeField, Range(0, 1), Tooltip("Tỉ lệ tia nước, còn lại hơi nước.")] private float waterJetRatio = .7f;
        [SerializeField, Tooltip("Màu nước và hơi trắng theo phiếu012.")] private Color waterColor = new Color32(79, 195, 247, 255);
        [SerializeField, Tooltip("Màu hơi nước và sao.")] private Color sparkleColor = Color.white;
        [SerializeField, Tooltip("Màu bụi khi va.")] private Color impactColor = new Color32(217, 212, 199, 255);
        [SerializeField, Tooltip("Material bùn alpha tạo sẵn bằng Core Feel; không tạo material khi Play.")] private Material mudFadeMaterial = null;
        [SerializeField, Tooltip("14 hạt/0.4s: nước + hơi.")] private ParticleFeel water = new ParticleFeel(14, .4f, .25f, 1.8f, .2f);
        [SerializeField, Tooltip("12 bọt/0.5s theo màu xe.")] private ParticleFeel bubbles = new ParticleFeel(12, .5f, .34f, 1.2f, .1f);
        [SerializeField, Tooltip("6 sao/0.5s.")] private ParticleFeel sparkles = new ParticleFeel(6, .5f, .4f, 1.4f, .1f);
        [SerializeField, Tooltip("6 bụi/0.25s.")] private ParticleFeel impact = new ParticleFeel(6, .25f, .30f, .7f, .2f);
        [SerializeField, Tooltip("30 pháo giấy/1s, 6 màu xe.")] private ParticleFeel confetti = new ParticleFeel(30, 1, .25f, 1.5f, 1);
        [Header("012 gói B — Ra cổng được rửa")]
        [SerializeField, Range(.1f, 1.5f), Tooltip("Thời gian xe lướt xuyên cổng, nhanh dần. Chỉ dùng cho sự kiện Exit; nhịp chui cổng nước vẫn theo Exit Seconds.")] private float gatePassSeconds = .5f;
        [SerializeField, Range(.05f, .6f), Tooltip("Phần cuối của lượt ra cổng dùng để thu nhỏ xe; trước đó xe giữ nguyên cỡ.")] private float gateTailRatio = .25f;
        [SerializeField, Tooltip("Bật chữ nổi 'Sạch bóng!' khi xe ra cổng.")] private bool popupTextEnabled = true;
        [SerializeField, Range(.02f, .2f), Tooltip("characterSize của chữ nổi; chữ phẳng trên mặt bãi như nhãn 1A/2B.")] private float popupTextSize = .06f;
        [SerializeField, Range(.3f, 3), Tooltip("Thời gian chữ nổi tồn tại, giây.")] private float popupSeconds = 1.1f;
        [SerializeField, Range(0, 2), Tooltip("Chữ nổi trôi lên (ngược hướng nhìn) bao nhiêu ô trong thời gian tồn tại.")] private float popupRise = .6f;
        [SerializeField, Range(.3f, 5), Tooltip("Hai xe ra cổng cách nhau không quá số giây này thì tính là chuỗi liên tiếp.")] private float comboWindowSeconds = 1.6f;
        [SerializeField, Range(0, 4), Tooltip("Mỗi xe liên tiếp tiếng ra cổng cao thêm bấy nhiêu bán cung.")] private float comboSemitones = 2;
        [SerializeField, Range(1, 8), Tooltip("Chuỗi tối đa tính tới xe thứ mấy; từ đó tiếng không cao thêm.")] private int comboMaxSteps = 5;
        [Header("012 gói C — Va có lực")]
        [SerializeField, Range(2, 8), Tooltip("Xe trượt từ ngần này ô trở lên thì va mạnh nhất.")] private int impactFullCells = 5;
        [SerializeField, Range(1, 3), Tooltip("Hệ số khuếch đại va ở mức mạnh nhất; xe bị chặn ngay hoặc trượt ít có hệ số 1.")] private float impactMaxScale = 1.8f;
        [SerializeField, Range(1, 8), Tooltip("Chỉ rung camera khi xe trượt ít nhất ngần này ô trước khi va.")] private int shakeMinCells = 2;
        [SerializeField, Range(0, 12), Tooltip("Số tia sao ở đầu xe khi va nhẹ nhất.")] private int impactSparksMin = 2;
        [SerializeField, Range(0, 12), Tooltip("Số tia sao ở đầu xe khi va mạnh nhất.")] private int impactSparksMax = 7;
        [Header("014 — Xe chạy: bứt tốc, lắc khi đâm, vệt bánh, đèn (số khởi điểm Claude)")]
        [SerializeField, Range(0, 4), Tooltip("Số ô xe cần để tăng tốc từ đứng yên lên tốc độ tối đa (1/Step Seconds ô/giây). 0 = trượt đều như cũ. Mỗi lượt chạy chậm hơn đúng Accel Cells × Step Seconds giây.")] private float accelCells = 1.2f;
        [SerializeField, Tooltip("Bật lắc lư thân khi đâm/bị đụng.")] private bool crashWobbleEnabled = true;
        [SerializeField, Range(0, 20), Tooltip("Biên độ lắc nghiêng quanh trục thân khi đâm, độ (nhân hệ số va theo số ô đã trượt).")] private float crashWobbleDegrees = 8;
        [SerializeField, Range(0, 1), Tooltip("Lắc đầu (quay quanh trục đứng) bằng tỉ lệ này của độ nghiêng; nhìn từ trên xuống dễ thấy hơn nghiêng.")] private float crashWobbleYawRatio = .8f;
        [SerializeField, Range(.1f, 1.5f), Tooltip("Tổng thời gian lắc tắt dần, giây.")] private float crashWobbleSeconds = .5f;
        [SerializeField, Range(.5f, 6), Tooltip("Số nhịp lắc trong thời gian lắc.")] private float crashWobbleCycles = 2.5f;
        [SerializeField, Range(0, 1), Tooltip("Xe đứng yên bị đụng lắc bằng tỉ lệ này của xe đâm.")] private float hitWobbleRatio = .6f;
        [SerializeField, Tooltip("Bật vệt khói/bánh sau xe.")] private bool trailEnabled = true;
        [SerializeField, Tooltip("Material vệt bánh tạo sẵn (Art/VFX/Materials/M_TireTrail); trống = không có vệt.")] private Material tireTrailMaterial = null;
        [SerializeField, Range(.1f, 2), Tooltip("Vệt tồn tại bao lâu rồi mờ hết, giây.")] private float trailSeconds = .6f;
        [SerializeField, Range(.05f, .8f), Tooltip("Bề rộng vệt, ô.")] private float trailWidth = .4f;
        [SerializeField, Range(0, 1), Tooltip("Độ đục đầu vệt (sát xe); đuôi về 0.")] private float trailAlpha = .8f;
        [SerializeField, Tooltip("Bật tia đèn ở đầu đang chạy (trắng khi tới, đỏ khi lùi).")] private bool headlightsEnabled = true;
        [SerializeField, Tooltip("Prefab tia đèn trước (Prefabs/VFX/HeadlightBeams); +X là hướng chiếu, gốc ở đáy tia.")] private GameObject headlightPrefab = null;
        [SerializeField, Tooltip("Prefab tia đèn khi lùi (Prefabs/VFX/TaillightBeams).")] private GameObject taillightPrefab = null;
        [SerializeField, Range(1, 40), Tooltip("Tốc độ tia đèn dài ra/thu lại theo tốc độ xe, đơn vị tỉ lệ/giây.")] private float lightResponse = 10;
        [SerializeField, Range(0, .1f), Tooltip("Nâng vệt và tia đèn khỏi mặt bãi để không z-fight, ô.")] private float roadFxLift = .015f;
        [Header("014 — Bùn kiểu D: cục bùn trên nóc, giọt rơi, vệt bùn (người làm chốt 26/11)")]
        [SerializeField, Tooltip("Prefab cục bùn khối (Prefabs/VFX/MudBlob), thay tấm bùn phẳng của prefab xe; trống = giữ tấm cũ.")] private GameObject mudBlobPrefab = null;
        [SerializeField, Tooltip("Material vệt bánh khi xe dính bùn (Art/VFX/Materials/M_MudTrail); trống = vệt trắng như thường.")] private Material mudTrailMaterial = null;
        [SerializeField, Tooltip("Bật giọt bùn rơi sau đuôi mỗi ô xe bẩn chạy qua, và bùn bắn khi lội vũng bùn.")] private bool mudDripsEnabled = true;
        [SerializeField, Tooltip("Giọt bùn mỗi ô xe bẩn chạy: 1 giọt/0,45s, rơi nhanh.")] private ParticleFeel mudDrip = new ParticleFeel(2, .5f, .2f, .5f, 1.2f);
        [SerializeField, Tooltip("Bùn bắn khi xe lội vào vũng bùn.")] private ParticleFeel mudSplash = new ParticleFeel(8, .4f, .16f, 1.6f, 1.2f);

        public bool BodyEnabled => bodyEnabled;
        public bool WheelsEnabled => wheelsEnabled;
        public bool MudBodyEnabled => mudBodyEnabled;
        public float LaunchPitch => launchPitch;
        public float LaunchSeconds => launchSeconds;
        public float BrakePitch => brakePitch;
        public float BrakeRebound => brakeRebound;
        public float BrakeSeconds => brakeSeconds;
        public float BrakeLength => brakeLength;
        public float BrakeWidth => brakeWidth;
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
        public float GatePassSeconds => gatePassSeconds;
        public float GateTailRatio => gateTailRatio;
        public bool PopupTextEnabled => popupTextEnabled;
        public float PopupTextSize => popupTextSize;
        public float PopupSeconds => popupSeconds;
        public float PopupRise => popupRise;
        public float ComboWindowSeconds => comboWindowSeconds;
        public int ImpactFullCells => impactFullCells;
        public float ImpactMaxScale => impactMaxScale;
        public int ShakeMinCells => shakeMinCells;
        public int ImpactSparksMin => impactSparksMin;
        public int ImpactSparksMax => impactSparksMax;
        public float AccelCells => accelCells;
        public bool CrashWobbleEnabled => crashWobbleEnabled;
        public float CrashWobbleDegrees => crashWobbleDegrees;
        public float CrashWobbleYawRatio => crashWobbleYawRatio;
        public float HitWobbleRatio => hitWobbleRatio;
        public bool TrailEnabled => trailEnabled;
        public Material TireTrailMaterial => tireTrailMaterial;
        public float TrailSeconds => trailSeconds;
        public float TrailWidth => trailWidth;
        public float TrailAlpha => trailAlpha;
        public bool HeadlightsEnabled => headlightsEnabled;
        public GameObject HeadlightPrefab => headlightPrefab;
        public GameObject TaillightPrefab => taillightPrefab;
        public float LightResponse => lightResponse;
        public float RoadFxLift => roadFxLift;
        public GameObject MudBlobPrefab => mudBlobPrefab;
        public Material MudTrailMaterial => mudTrailMaterial;
        public bool MudDripsEnabled => mudDripsEnabled;
        public ParticleFeel MudDrip => mudDrip;
        public ParticleFeel MudSplash => mudSplash;

        // 014: thời điểm xe tới mốc `cells` ô kể từ lúc đứng yên. Gia tốc đều tới tốc độ tối đa v = 1/stepSeconds
        // sau accelCells ô, rồi chạy đều. Đoạn tăng tốc: s = v²t²/(4a) => t = 2·√(s·a)/v; sau đó t = (s + a)/v.
        // Với accelCells = 0 thì t = s·stepSeconds, đúng nhịp trượt đều cũ.
        public float TimeToCell(float cells)
        {
            if (cells <= 0) { return 0; }
            float a = accelCells;
            return cells <= a ? 2 * Mathf.Sqrt(cells * a) * stepSeconds : (cells + a) * stepSeconds;
        }

        // Thời lượng trượt ô thứ `index` (0 = ô đầu tiên của lượt chạy).
        public float StepDuration(int index)
        {
            int k = Mathf.Max(0, index);
            return Mathf.Max(.01f, TimeToCell(k + 1) - TimeToCell(k));
        }

        // Hệ số lắc chuẩn hóa (-1..1) tại `elapsed` giây sau va: sin tắt dần theo bình phương, về 0 đúng lúc hết giờ.
        public float CrashWobble(float elapsed)
        {
            if (elapsed < 0 || elapsed >= crashWobbleSeconds) { return 0; }
            float t = elapsed / crashWobbleSeconds;
            return Mathf.Sin(t * crashWobbleCycles * 2 * Mathf.PI) * (1 - t) * (1 - t);
        }

        // Hệ số va theo số ô xe trượt trước khi dừng: 0 ô (bị chặn ngay) và 1 ô = 1, đủ ImpactFullCells ô = ImpactMaxScale.
        public float ImpactPower(int slideCells)
        {
            return Mathf.Clamp01((slideCells - 1) / Mathf.Max(1f, impactFullCells - 1f));
        }
        public float ImpactScale(int slideCells) { return Mathf.Lerp(1, impactMaxScale, ImpactPower(slideCells)); }

        // Cao độ tiếng ra cổng theo xe thứ combo trong chuỗi (1 = bình thường). Mỗi xe thêm comboSemitones bán cung, có trần.
        public float ComboPitch(int combo)
        {
            int steps = Mathf.Clamp(combo, 1, comboMaxSteps) - 1;
            return Mathf.Pow(2, steps * comboSemitones / 12f);
        }

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
            brakeLength = Limit(brakeLength, .5f, 1, .90f);
            brakeWidth = Limit(brakeWidth, 1, 1.5f, 1.06f);
            brakeHeight = Limit(brakeHeight, 1, 1.5f, 1.04f);
            squashSeconds = Limit(squashSeconds, .01f, .5f, .10f);
            exitStretch = Limit(exitStretch, 1, 1.5f, 1.10f);
            exitPitch = Limit(exitPitch, -15, 0, -4);
            mudSink = Limit(mudSink, 0, .2f, .03f);
            mudHeight = Limit(mudHeight, .5f, 1, .97f);
            mudSeconds = Limit(mudSeconds, .01f, 1, .12f);
            washRoll = Limit(washRoll, 0, 20, 6);
            washCycles = Mathf.Clamp(washCycles, 1, 6);
            washSeconds = Limit(washSeconds, .05f, 1, .35f);
            mudFadeSeconds = Limit(mudFadeSeconds, .01f, 1, .2f);
            hitDistance = Limit(hitDistance, 0, .2f, .08f);
            hitSeconds = Limit(hitSeconds, .01f, .5f, .12f);
            shakeDistance = Limit(shakeDistance, 0, .15f, .04f);
            shakeSeconds = Limit(shakeSeconds, .01f, .5f, .1f);
            shakeCycles = Mathf.Clamp(shakeCycles, 1, 12);
            maxLiveParticles = Mathf.Clamp(maxLiveParticles, 1, 60);
            confettiHeight = Limit(confettiHeight, 0, 4, 3);
            particleSpread = Limit(particleSpread, 0, 2, 1.2f);
            confettiSpread = Limit(confettiSpread, 0, 2, .4f);
            waterTintRatio = Limit(waterTintRatio, 0, 1, .65f);
            waterJetRatio = Limit(waterJetRatio, 0, 1, .7f);
            gatePassSeconds = Limit(gatePassSeconds, .1f, 1.5f, .5f);
            gateTailRatio = Limit(gateTailRatio, .05f, .6f, .25f);
            popupTextSize = Limit(popupTextSize, .02f, .2f, .06f);
            popupSeconds = Limit(popupSeconds, .3f, 3, 1.1f);
            popupRise = Limit(popupRise, 0, 2, .6f);
            comboWindowSeconds = Limit(comboWindowSeconds, .3f, 5, 1.6f);
            comboSemitones = Limit(comboSemitones, 0, 4, 2);
            comboMaxSteps = Mathf.Clamp(comboMaxSteps, 1, 8);
            impactFullCells = Mathf.Clamp(impactFullCells, 2, 8);
            impactMaxScale = Limit(impactMaxScale, 1, 3, 1.8f);
            shakeMinCells = Mathf.Clamp(shakeMinCells, 1, 8);
            impactSparksMin = Mathf.Clamp(impactSparksMin, 0, 12);
            impactSparksMax = Mathf.Clamp(impactSparksMax, impactSparksMin, 12);
            accelCells = Limit(accelCells, 0, 4, 1.2f);
            crashWobbleDegrees = Limit(crashWobbleDegrees, 0, 20, 8);
            crashWobbleYawRatio = Limit(crashWobbleYawRatio, 0, 1, .8f);
            crashWobbleSeconds = Limit(crashWobbleSeconds, .1f, 1.5f, .5f);
            crashWobbleCycles = Limit(crashWobbleCycles, .5f, 6, 2.5f);
            hitWobbleRatio = Limit(hitWobbleRatio, 0, 1, .6f);
            trailSeconds = Limit(trailSeconds, .1f, 2, .6f);
            trailWidth = Limit(trailWidth, .05f, .8f, .4f);
            trailAlpha = Limit(trailAlpha, 0, 1, .8f);
            lightResponse = Limit(lightResponse, 1, 40, 10);
            roadFxLift = Limit(roadFxLift, 0, .1f, .015f);
            water.Validate();
            bubbles.Validate();
            sparkles.Validate();
            impact.Validate();
            confetti.Validate();
            mudDrip.Validate();
            mudSplash.Validate();
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
