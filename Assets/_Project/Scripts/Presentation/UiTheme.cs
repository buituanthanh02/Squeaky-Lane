using TMPro;
using UnityEngine;

namespace SqueakyLane.Presentation
{
    [CreateAssetMenu(menuName = "Squeaky Lane/UI Theme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("013 · Sprite và font tạo sẵn bằng menu Editor")]
        [SerializeField] private Sprite primaryButton = null, regularButton = null, dangerButton = null, panel = null, clockTrack = null, clockFill = null, toggleBox = null, toggleCheck = null;
        [SerializeField] private TMP_FontAsset titleFont = null, bodyFont = null;
        [SerializeField] private TMP_SpriteAsset icons = null;
        [Header("Bảng màu Claude")]
        [SerializeField] private Color text = Hex("10315A"), secondaryText = Hex("2C5D8A"), primaryText = Hex("5A3B00");
        [SerializeField] private Color panelColor = Color.white, normalButton = Color.white, primaryColor = Color.white;
        [SerializeField] private Color clockColor = Hex("3FA9F5"), lowClock = Hex("E53935"), shade = new Color(20f / 255, 20f / 255, 40f / 255, .42f);
        [SerializeField] private Color skyTop = Hex("8FD3FA"), skyMiddle = Hex("C6EBFF"), skyBottom = Hex("E4F6FF"), cameraBackground = Hex("BFE6FB");
        [SerializeField] private Color grass = Hex("5C9C56"), pavement = Hex("9AA3AE");
        [Header("013 V2 · S1: nền sáng và vignette")]
        [SerializeField, Tooltip("Màu cỏ phía xa; grass là màu phía gần.")] private Color grassFar = Hex("86C876");
        [SerializeField, Tooltip("Navy ở mép màn; tâm ảnh trong suốt.")] private Color vignette = new Color(10f / 255, 30f / 255, 68f / 255, .38f);
        [SerializeField, Range(0, .9f), Tooltip("Bán kính phần tâm không có vignette, theo nửa màn.")] private float vignetteInnerRadius = .55f;
        [SerializeField, Range(.1f, 3), Tooltip("Cỏ kéo dư ngoài bốn góc camera, đơn vị ô bãi.")] private float groundOverscan = 1;
        [SerializeField, Range(0, 1), Tooltip("Menu S1 áp số này vào CameraConfig.padding; runtime vẫn đọc CameraConfig.")] private float cameraPadding = .1f;
        [Header("Cỡ theo Canvas 1080×1920; 1920px tương đương màn dọc tham chiếu")]
        [SerializeField, Range(48, 220), Tooltip("Chiều cao tối thiểu nút theo Canvas; chọn112 để còn ~48px ở màn nhỏ.")] private float buttonHeight = 112;
        [SerializeField, Range(20, 70)] private float bodyFontSize = 38;
        [SerializeField, Range(32, 110)] private float titleFontSize = 64;
        [SerializeField, Range(.5f, 1)] private float pressedTint = .85f;
        [SerializeField, Range(1, 6), Tooltip("Độ dịch nhãn xuống khi nhấn, pixel tham chiếu.")] private float pressOffset = 3;
        [SerializeField, Tooltip("Lề thẻ hộp thoại trong panel toàn màn; không đổi root hay vùng chặn chạm.")] private Vector2 dialogInset = new Vector2(.04f, .08f);
        [SerializeField] private Vector2 levelCellSize = new Vector2(280, 180), levelSpacing = new Vector2(24, 24);
        [SerializeField, Range(2, 5)] private int levelColumns = 3;
        [Header("Cảnh quanh bãi; model đã chuẩn hóa, material asset chung")]
        [SerializeField] private GameObject environmentPrefab = null;
        [SerializeField] private Material groundMaterial = null;
        [SerializeField] private GameObject[] propPrefabs = System.Array.Empty<GameObject>();
        [SerializeField, Range(2, 8)] private float groundMargin = 5;
        [SerializeField, Range(.2f, 1)] private float pavementWidth = .7f;
        [SerializeField, Range(1.1f, 3), Tooltip("Giữ trống ít nhất1 ô quanh mép bãi.")] private float propGap = 1.5f;
        [SerializeField, Range(0, 12)] private int propCount = 8;
        [SerializeField, Range(1.5f, 4)] private float propSpacing = 2.8f;
        [SerializeField, Tooltip("Bỏ số trên xe; không bỏ nhãn cặp portal vì còn giúp đọc luật.")] private bool hideCarNumbers = true;
        [SerializeField] private bool gateStrips = true;
        [SerializeField, Range(.05f, .6f)] private float gateStripDepth = .3f;
        [SerializeField, Range(0, 2)] private float lightIntensity = 1.1f;
        [SerializeField] private Vector3 lightAngles = new Vector3(50, -30, 0);
        [SerializeField] private Color ambient = Hex("4A5568");

        public Sprite PrimaryButton => primaryButton;
        public Sprite RegularButton => regularButton;
        public Sprite DangerButton => dangerButton;
        public Sprite Panel => panel;
        public Sprite ClockTrack => clockTrack;
        public Sprite ClockFill => clockFill;
        public Sprite ToggleBox => toggleBox;
        public Sprite ToggleCheck => toggleCheck;
        public TMP_FontAsset TitleFont => titleFont;
        public TMP_FontAsset BodyFont => bodyFont;
        public TMP_SpriteAsset Icons => icons;
        public Color Text => text;
        public Color SecondaryText => secondaryText;
        public Color PrimaryText => primaryText;
        public Color PanelColor => panelColor;
        public Color NormalButton => normalButton;
        public Color PrimaryColor => primaryColor;
        public Color ClockColor => clockColor;
        public Color LowClock => lowClock;
        public Color Shade => shade;
        public Color SkyTop => skyTop;
        public Color SkyMiddle => skyMiddle;
        public Color SkyBottom => skyBottom;
        public Color CameraBackground => cameraBackground;
        public Color Grass => grass;
        public Color GrassFar => grassFar;
        public Color Vignette => vignette;
        public float VignetteInnerRadius => vignetteInnerRadius;
        public float GroundOverscan => groundOverscan;
        public float CameraPadding => cameraPadding;
        public Color Pavement => pavement;
        public float ButtonHeight => buttonHeight;
        public float BodyFontSize => bodyFontSize;
        public float TitleFontSize => titleFontSize;
        public float PressedTint => pressedTint;
        public float PressOffset => pressOffset;
        public Vector2 DialogInset => dialogInset;
        public Vector2 LevelCellSize => levelCellSize;
        public Vector2 LevelSpacing => levelSpacing;
        public int LevelColumns => levelColumns;
        public GameObject EnvironmentPrefab => environmentPrefab;
        public Material GroundMaterial => groundMaterial;
        public GameObject[] PropPrefabs => propPrefabs;
        public float GroundMargin => groundMargin;
        public float PavementWidth => pavementWidth;
        public float PropGap => propGap;
        public int PropCount => propCount;
        public float PropSpacing => propSpacing;
        public bool HideCarNumbers => hideCarNumbers;
        public bool GateStrips => gateStrips;
        public float GateStripDepth => gateStripDepth;
        public float LightIntensity => lightIntensity;
        public Vector3 LightAngles => lightAngles;
        public Color Ambient => ambient;
        public string Stars(int count)
        {
            count = Mathf.Clamp(count, 0, 3);
            string full = "<sprite name=star_full tint=0>", empty = "<sprite name=star_empty tint=0>";
            return (count > 0 ? full : empty) + (count > 1 ? full : empty) + (count > 2 ? full : empty);
        }
        private static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var color);
            return color;
        }
        private void OnValidate()
        {
            buttonHeight = Clean(buttonHeight, 48, 220, 112);
            bodyFontSize = Clean(bodyFontSize, 20, 70, 38);
            titleFontSize = Clean(titleFontSize, 32, 110, 64);
            pressedTint = Clean(pressedTint, .5f, 1, .85f);
            pressOffset = Clean(pressOffset, 1, 6, 3);
            groundMargin = Clean(groundMargin, 2, 8, 5);
            vignetteInnerRadius = Clean(vignetteInnerRadius, 0, .9f, .55f);
            groundOverscan = Clean(groundOverscan, .1f, 3, 1);
            cameraPadding = Clean(cameraPadding, 0, 1, .1f);
            pavementWidth = Clean(pavementWidth, .2f, 1, .7f);
            propGap = Clean(propGap, 1.1f, 3, 1.5f);
            propCount = Mathf.Clamp(propCount, 0, 12);
            propSpacing = Clean(propSpacing, 1.5f, 4, 2.8f);
            gateStripDepth = Clean(gateStripDepth, .05f, .6f, .3f);
            lightIntensity = Clean(lightIntensity, 0, 2, 1.1f);
            dialogInset.x = Clean(dialogInset.x, 0, .2f, .04f);
            dialogInset.y = Clean(dialogInset.y, 0, .2f, .08f);
            levelCellSize.x = Clean(levelCellSize.x, 160, 400, 280);
            levelCellSize.y = Clean(levelCellSize.y, 112, 300, 180);
            levelSpacing.x = Clean(levelSpacing.x, 0, 60, 24);
            levelSpacing.y = Clean(levelSpacing.y, 0, 60, 24);
            levelColumns = Mathf.Clamp(levelColumns, 2, 5);
        }
        private static float Clean(float value, float min, float max, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }
    }
}
