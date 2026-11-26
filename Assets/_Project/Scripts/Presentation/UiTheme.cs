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
        [Header("013 S3 · bộ sprite UI lọc từ Sand Puzzle Clone (Art/UI/Sprites/Pack, menu 'S3 Import UI Pack'); Image đổi sprite lúc chạy để đổi trạng thái")]
        [SerializeField, Tooltip("Pill đồng hồ: bình thường và sắp hết giờ (đỏ). Cùng cỡ để đổi sprite không nhảy bố cục.")] private Sprite clockNormal = null, clockLow = null;
        [SerializeField, Tooltip("Nút Gợi ý và huy hiệu số lượt: còn lượt / hết lượt.")] private Sprite hintOn = null, hintOff = null, badgeOn = null, badgeOff = null;
        [SerializeField] private Material hudOutline = null, hudSmall = null, cardText = null;
        [SerializeField, Tooltip("Viền chữ HUD/nút: indigo đậm hợp nền tím.")] private Color pillOutline = Hex("24104F");
        [SerializeField, Range(0, 1), Tooltip("Độ tối lớp nền sau hộp thoại.")] private float dimAlpha = .6f;
        [SerializeField, Range(.1f, .5f), Tooltip("Khung hộp thoại: pixel mockup trên mỗi pixel sprite; nhỏ hơn = viền khung mảnh hơn.")] private float frameScale = .2f;
        [SerializeField, Range(160, 280), Tooltip("Bề ngang thẻ hộp thoại theo pixel mockup 300 px.")] private float cardWidth = 236;
        [SerializeField, Range(.5f, 1), Tooltip("Nút bị khóa (vd Hoàn tác khi chưa có nước) chỉ tối nhẹ.")] private float disabledTint = .78f;
        [SerializeField, Range(.8f, 1)] private float pressScale = .94f;
        [SerializeField, Tooltip("Đơn vị Canvas trên một pixel mockup; menu lấy referenceResolution.x / 300.")] private float hudUnit = 3.6f;
        [SerializeField, Range(48, 90), Tooltip("Cỡ nút booster theo mockup 300 px, được nhân HudUnit.")] private float boosterSize = 66;
        [SerializeField, Range(1, 6), Tooltip("Giữ thông báo rồi mờ bằng unscaled time.")] private float messageHoldSeconds = 2;
        [SerializeField, Range(.1f, 2), Tooltip("Thời gian mờ dần của thông báo.")] private float messageFadeSeconds = .4f;
        [SerializeField, Range(0, .5f), Tooltip("Độ dày viền chữ HudOutline (tiêu đề, nút, giờ) và HudSmall (nhãn nhỏ).")] private float hudOutlineWidth = .2f, hudSmallOutlineWidth = .12f;
        [SerializeField, Range(0, .4f), Tooltip("Làm đậm nét chữ (TMP Face Dilate). Baloo 2 mảnh hơn viền nên cần dilate để chữ không bị rỗng; trên .3 các lỗ chữ bắt đầu bít.")] private float hudOutlineDilate = .25f, hudSmallDilate = .1f;
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
        [Header("013 S4 · bóng giả và khay; asset do menu tạo, không material runtime")]
        [SerializeField] private GameObject contactShadowPrefab = null, parkedCarPrefab = null;
        [SerializeField] private GameObject[] southPropPrefabs = System.Array.Empty<GameObject>();
        [SerializeField] private bool trayEnabled = false, southPropsEnabled = false;
        [SerializeField, Range(0, .5f)] private float contactShadowAlpha = .28f;
        [SerializeField, Range(.3f, 3)] private float contactShadowFalloff = .8f;
        [SerializeField, Range(.002f, .03f)] private float contactShadowLift = .008f;
        [SerializeField, Tooltip("Lệch bóng trên trục X/Z của bãi, không quay theo thân xe. Preset hướng đèn50/−30 đi về −X/+Z.")] private Vector2 contactShadowOffset = new Vector2(-.12f, .1f);
        [SerializeField, Range(.5f, 1.2f)] private float carShadowWidth = .9f, carShadowLengthRatio = .95f;
        [SerializeField, Range(.05f, .6f)] private float trayThickness = .25f;
        [SerializeField] private Color pavementSide = Hex("7E8B9A"), pavementHighlight = new Color(1, 1, 1, .6f);
        [SerializeField, Range(.01f, .12f)] private float pavementHighlightWidth = .035f;
        [SerializeField, Range(0, 1.2f), Tooltip("015: bán kính bo góc khay bãi, ô (0 = góc vuông như 013).")] private float trayCornerRadius = .45f;
        [SerializeField, Range(.2f, 2)] private float groundShadowSpread = 1;
        [Header("016 · Phố quanh trạm rửa: 2 phố ngang + 2 phố dọc chạy ra ngoài màn hình")]
        [SerializeField, Tooltip("Bật phố quanh bãi; xe ra cổng chạy lên phố (FeelConfig 016).")] private bool streetsEnabled = false;
        [SerializeField, Range(.7f, 4), Tooltip("Khoảng từ mép bãi tới mép trong của phố, ô (vỉa hè khay rộng Pavement Width). Càng xa thì bãi càng nhỏ trên màn dọc.")] private float streetInner = .8f;
        [SerializeField, Range(.6f, 2), Tooltip("Bề rộng mặt đường, ô.")] private float streetWidth = .95f;
        [SerializeField, Range(0, 1), Tooltip("Phần bề rộng phố hai bên mà camera giữ trong khung (1 = thấy trọn, nhỏ hơn = bãi to hơn).")] private float streetVisible = .9f;
        [SerializeField, Range(10, 80), Tooltip("Chiều dài mỗi phố tính từ tâm bãi ra mỗi phía, ô (đủ ra khỏi màn hình).")] private float streetLength = 40;
        [SerializeField, Tooltip("Material mặt đường (U dọc đường theo ô, V ngang đường 0..1).")] private Material roadMaterial = null;
        [SerializeField, Range(.1f, .5f), Tooltip("Độ cao tối đa props dải dưới; S4 giữ ≤0,5 ô.")] private float southPropHeight = .45f;
        [SerializeField, Tooltip("Cỡ lớn nhất của Garage/Office; số khởi điểm chờ vòng ảnh S4.")] private Vector2 buildingSizes = new Vector2(2.8f, 2);
        [SerializeField, Tooltip("Cỡ lớn nhất của cây nhỏ/lớn; số khởi điểm chờ vòng ảnh S4.")] private Vector2 treeSizes = new Vector2(1.1f, 1.5f);
        [SerializeField, Range(.5f, 3)] private float sceneryLightSize = 1.2f;
        [SerializeField, Range(2, 6)] private float sceneryRoadSize = 3.8f;

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
        public Sprite ClockNormal => clockNormal;
        public Sprite ClockLow => clockLow;
        public Sprite HintOn => hintOn;
        public Sprite HintOff => hintOff;
        public Sprite BadgeOn => badgeOn;
        public Sprite BadgeOff => badgeOff;
        public Material HudOutline => hudOutline;
        public Material HudSmall => hudSmall;
        public Material CardText => cardText;
        public Color PillOutline => pillOutline;
        public float DimAlpha => dimAlpha;
        public float FrameScale => frameScale;
        public float CardWidth => cardWidth;
        public float DisabledTint => disabledTint;
        public float PressScale => pressScale;
        public float HudUnit => hudUnit;
        public float BoosterSize => boosterSize;
        public float MessageHoldSeconds => messageHoldSeconds;
        public float MessageFadeSeconds => messageFadeSeconds;
        public float HudOutlineWidth => hudOutlineWidth;
        public float HudSmallOutlineWidth => hudSmallOutlineWidth;
        public float HudOutlineDilate => hudOutlineDilate;
        public float HudSmallDilate => hudSmallDilate;
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
        public GameObject ContactShadowPrefab => contactShadowPrefab;
        public GameObject ParkedCarPrefab => parkedCarPrefab;
        public GameObject[] SouthPropPrefabs => southPropPrefabs;
        public bool TrayEnabled => trayEnabled;
        public bool SouthPropsEnabled => southPropsEnabled;
        public float ContactShadowAlpha => contactShadowAlpha;
        public float ContactShadowFalloff => contactShadowFalloff;
        public float ContactShadowLift => contactShadowLift;
        public Vector2 ContactShadowOffset => contactShadowOffset;
        public float CarShadowWidth => carShadowWidth;
        public float CarShadowLengthRatio => carShadowLengthRatio;
        public float TrayThickness => trayThickness;
        public Color PavementSide => pavementSide;
        public Color PavementHighlight => pavementHighlight;
        public float PavementHighlightWidth => pavementHighlightWidth;
        public float TrayCornerRadius => trayCornerRadius;
        public float GroundShadowSpread => groundShadowSpread;
        public bool StreetsEnabled => streetsEnabled && roadMaterial != null;
        public float StreetInner => streetInner;
        public float StreetWidth => streetWidth;
        public float StreetVisible => streetVisible;
        public float StreetLength => streetLength;
        public Material RoadMaterial => roadMaterial;
        public float SouthPropHeight => southPropHeight;
        public Vector2 BuildingSizes => buildingSizes;
        public Vector2 TreeSizes => treeSizes;
        public float SceneryLightSize => sceneryLightSize;
        public float SceneryRoadSize => sceneryRoadSize;
        public string Stars(int count)
        {
            count = Mathf.Clamp(count, 0, 3);
            // Không dùng thuộc tính tint=: TMP 3.0.7 coi nó là tag sai và vẽ sprite "?" mặc định thay cho sao. Sprite UiIcons giữ nguyên màu gốc.
            string full = "<sprite name=star_full>", empty = "<sprite name=star_empty>";
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
            disabledTint = Clean(disabledTint, .5f, 1, .78f);
            dimAlpha = Clean(dimAlpha, 0, 1, .6f);
            frameScale = Clean(frameScale, .1f, .5f, .2f);
            cardWidth = Clean(cardWidth, 160, 280, 236);
            messageHoldSeconds = Clean(messageHoldSeconds, 1, 6, 2);
            messageFadeSeconds = Clean(messageFadeSeconds, .1f, 2, .4f);
            hudOutlineWidth = Clean(hudOutlineWidth, 0, .5f, .2f);
            hudOutlineDilate = Clean(hudOutlineDilate, 0, .4f, .25f);
            hudSmallDilate = Clean(hudSmallDilate, 0, .4f, .1f);
            hudSmallOutlineWidth = Clean(hudSmallOutlineWidth, 0, .5f, .12f);
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
            contactShadowAlpha = Clean(contactShadowAlpha, 0, .5f, .28f);
            contactShadowFalloff = Clean(contactShadowFalloff, .3f, 3, .8f);
            contactShadowLift = Clean(contactShadowLift, .002f, .03f, .008f);
            contactShadowOffset.x = Clean(contactShadowOffset.x, -.3f, .3f, -.12f);
            contactShadowOffset.y = Clean(contactShadowOffset.y, -.3f, .3f, .1f);
            carShadowWidth = Clean(carShadowWidth, .5f, 1.2f, .9f);
            carShadowLengthRatio = Clean(carShadowLengthRatio, .5f, 1.2f, .95f);
            trayThickness = Clean(trayThickness, .05f, .6f, .25f);
            pavementHighlightWidth = Clean(pavementHighlightWidth, .01f, .12f, .035f);
            trayCornerRadius = Clean(trayCornerRadius, 0, 1.2f, .45f);
            groundShadowSpread = Clean(groundShadowSpread, .2f, 2, 1);
            southPropHeight = Clean(southPropHeight, .1f, .5f, .45f);
            buildingSizes.x = Clean(buildingSizes.x, 1, 5, 2.8f);
            buildingSizes.y = Clean(buildingSizes.y, 1, 5, 2);
            treeSizes.x = Clean(treeSizes.x, .5f, 3, 1.1f);
            treeSizes.y = Clean(treeSizes.y, .5f, 3, 1.5f);
            sceneryLightSize = Clean(sceneryLightSize, .5f, 3, 1.2f);
            sceneryRoadSize = Clean(sceneryRoadSize, 2, 6, 3.8f);
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
