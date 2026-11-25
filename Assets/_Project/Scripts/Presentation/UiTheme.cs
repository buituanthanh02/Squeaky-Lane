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
        [Header("013 V2 · S2: nút, pill, chữ dùng chung")]
        [SerializeField] private Sprite blueButton = null, blueSquare = null, yellowSquare = null, blueRound = null;
        [SerializeField] private Sprite softShadow = null, pillSprite = null, glintSprite = null;
        [SerializeField] private Material hudOutline = null, hudSmall = null, cardText = null;
        [SerializeField] private Color buttonBlue = Hex("3FA9F5"), buttonYellow = Hex("FFC93C"), buttonOutline = Hex("0E2A55");
        [SerializeField] private Color pillColor = Hex("14336A"), pillOutline = Hex("0A1E44"), pillHighlight = Hex("4F8FE0"), badgeColor = Hex("E24B4A"), exhaustedColor = Hex("8795A8");
        [SerializeField, Range(1, 5), Tooltip("Viền nút/pill theo mockup rộng 300 px.")] private float outlinePixels = 3;
        [SerializeField, Range(1, 3)] private float innerOutlinePixels = 2;
        [SerializeField, Range(0, 16)] private float shadowOffset = 9;
        [SerializeField, Range(0, 1)] private float shadowAlpha = .28f;
        [SerializeField, Range(.8f, 1)] private float pressScale = .94f;
        [SerializeField, Tooltip("Đơn vị Canvas trên một pixel mockup; menu lấy referenceResolution.x / 300.")] private float hudUnit = 3.6f;
        [SerializeField, Range(48, 90), Tooltip("Cỡ booster theo mockup 300 px, được nhân HudUnit.")] private float boosterSize = 66;
        [SerializeField, Range(1, 6), Tooltip("Giữ thông báo rồi mờ bằng unscaled time.")] private float messageHoldSeconds = 2;
        [SerializeField, Range(.1f, 2)] private float messageFadeSeconds = .4f;
        [SerializeField] private bool showGlint = true;
        [SerializeField] private Vector2 glintSize = new Vector2(14, 6), glintOffset = new Vector2(8, -6);
        [SerializeField, Range(0, 1)] private float glintAlpha = .7f;
        [SerializeField, Range(-45, 0)] private float glintAngle = -20;
        [SerializeField] private Vector2 iconShadowOffset = new Vector2(0, -2);
        [SerializeField, Range(0, 1)] private float iconShadowAlpha = .55f;
        [SerializeField, Range(0, .5f)] private float hudOutlineWidth = .3f, hudSmallOutlineWidth = .12f;
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
        public Sprite BlueButton => blueButton;
        public Sprite BlueSquare => blueSquare;
        public Sprite YellowSquare => yellowSquare;
        public Sprite BlueRound => blueRound;
        public Sprite SoftShadow => softShadow;
        public Sprite PillSprite => pillSprite;
        public Sprite GlintSprite => glintSprite;
        public Material HudOutline => hudOutline;
        public Material HudSmall => hudSmall;
        public Material CardText => cardText;
        public Color ButtonBlue => buttonBlue;
        public Color ButtonYellow => buttonYellow;
        public Color ButtonOutline => buttonOutline;
        public Color PillColor => pillColor;
        public Color PillOutline => pillOutline;
        public Color PillHighlight => pillHighlight;
        public Color BadgeColor => badgeColor;
        public Color ExhaustedColor => exhaustedColor;
        public float OutlinePixels => outlinePixels;
        public float InnerOutlinePixels => innerOutlinePixels;
        public float ShadowOffset => shadowOffset;
        public float ShadowAlpha => shadowAlpha;
        public float PressScale => pressScale;
        public float HudUnit => hudUnit;
        public float BoosterSize => boosterSize;
        public float MessageHoldSeconds => messageHoldSeconds;
        public float MessageFadeSeconds => messageFadeSeconds;
        public bool ShowGlint => showGlint;
        public Vector2 GlintSize => glintSize;
        public Vector2 GlintOffset => glintOffset;
        public float GlintAlpha => glintAlpha;
        public float GlintAngle => glintAngle;
        public Vector2 IconShadowOffset => iconShadowOffset;
        public float IconShadowAlpha => iconShadowAlpha;
        public float HudOutlineWidth => hudOutlineWidth;
        public float HudSmallOutlineWidth => hudSmallOutlineWidth;
        public bool HasHudSkin => hudOutline != null && hudSmall != null && cardText != null;
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
            pressScale = Clean(pressScale, .8f, 1, .94f);
            hudUnit = Clean(hudUnit, .1f, 12, 3.6f);
            boosterSize = Clean(boosterSize, 48, 90, 66);
            outlinePixels = Clean(outlinePixels, 1, 5, 3);
            innerOutlinePixels = Clean(innerOutlinePixels, 1, 3, 2);
            shadowOffset = Clean(shadowOffset, 0, 16, 9);
            shadowAlpha = Clean(shadowAlpha, 0, 1, .28f);
            messageHoldSeconds = Clean(messageHoldSeconds, 1, 6, 2);
            messageFadeSeconds = Clean(messageFadeSeconds, .1f, 2, .4f);
            hudOutlineWidth = Clean(hudOutlineWidth, 0, .5f, .3f);
            hudSmallOutlineWidth = Clean(hudSmallOutlineWidth, 0, .5f, .12f);
            glintAlpha = Clean(glintAlpha, 0, 1, .7f);
            glintAngle = Clean(glintAngle, -45, 0, -20);
            iconShadowAlpha = Clean(iconShadowAlpha, 0, 1, .55f);
            glintSize.x = Clean(glintSize.x, 1, 30, 14);
            glintSize.y = Clean(glintSize.y, 1, 20, 6);
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
