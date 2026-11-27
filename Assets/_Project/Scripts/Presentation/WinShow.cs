using System;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Mốc thời gian của màn trình diễn thắng, tách thuần để kiểm bằng test: tối dần → nền logo → từng chữ → chữ LANE đập xuống → pháo hoa.
    public static class WinShowTimeline
    {
        public static float Progress(float age, float start, float seconds)
        {
            return seconds <= 0 ? (age >= start ? 1 : 0) : Mathf.Clamp01((age - start) / seconds);
        }

        public static float LetterStart(FeelConfig feel, int index)
        {
            return feel.WinLetterStart + index * feel.WinLetterStep;
        }

        // Chữ cuối cùng của logo đập xuống sau khi chữ rời cuối cùng đã bắt đầu hiện.
        public static float WordStart(FeelConfig feel, int letters)
        {
            return LetterStart(feel, Mathf.Max(0, letters - 1)) + feel.WinLetterSeconds * .5f;
        }

        public static float BurstStart(FeelConfig feel, int index)
        {
            return feel.WinFireworkStart + index * feel.WinFireworkStep;
        }

        // Rơi từ cỡ lớn xuống, lún nhẹ dưới cỡ thật rồi về 1.
        public static float Slam(float t, float from)
        {
            if (t >= 1) { return 1; }
            return t < .7f ? Mathf.Lerp(from, .9f, t / .7f) : Mathf.Lerp(.9f, 1, (t - .7f) / .3f);
        }
    }

    // 021: lớp phủ khi thắng màn: nền tối, logo bật từng mảnh, pháo hoa; chạm để bỏ qua.
    // Bảng thắng mở thì logo và pháo tan đi, nền tối ở lại phía sau bảng tới khi rời màn hình Thắng.
    // Dựng bằng code trên Canvas (không sửa scene), tạo object một lần rồi dùng lại. Class thường do GameBootstrap sở hữu.
    public sealed class WinShow
    {
        private const int Sparks = 34, MaxBursts = 5;
        private const float Bleed = 600;

        private enum Stage { Hidden, Showing, Backdrop }

        private readonly RectTransform parent;
        private readonly int siblingIndex;
        private readonly UiTheme theme;
        private readonly FeelConfig feel;
        private Layout layout;
        private RectTransform root, stageRoot, logo;
        private CanvasGroup group, stageGroup;
        private Image dim;
        private Piece[] pieces;
        private Image[] sparks, rockets;
        private int letters, bursts;
        private float age, fadeAge, height, width;
        private Stage stage;

        public WinShow(RectTransform layer, int index, UiTheme art, FeelConfig settings)
        {
            parent = layer;
            siblingIndex = index;
            theme = art;
            feel = settings;
        }

        public bool Available => parent != null && feel != null && theme != null && theme.HasWinShow;
        // Đang chiếu và chưa tới lúc mở bảng thắng.
        public bool Playing => stage == Stage.Showing && age < feel.WinShowSeconds;
        // Còn trên màn, kể cả khi chỉ còn nền tối sau bảng thắng.
        public bool Visible => stage != Stage.Hidden;
        public float Age => age;

        public void Play()
        {
            if (!Available) { return; }
            if (root == null) { Build(); }
            stage = Stage.Showing;
            age = 0;
            fadeAge = 0;
            width = parent.rect.width;
            height = parent.rect.height;
            float scale = layout.width > 0 ? width * theme.WinLogoWidth / layout.width : 1;
            logo.localScale = Vector3.one * scale;
            logo.anchoredPosition = new Vector2(0, height * theme.WinLogoLift);
            stageGroup.alpha = 1;
            group.blocksRaycasts = group.interactable = true;
            root.gameObject.SetActive(true);
            Pose();
        }

        // Chạm vào nền: nhảy tới cuối. Chờ một nhịp để cú chạm của nước đi cuối không bỏ qua ngay.
        public void Skip()
        {
            if (!Playing || age < feel.WinShowSkipAfter) { return; }
            age = feel.WinShowSeconds;
            Pose();
        }

        // Bảng thắng đã mở bên trên: nhả chạm ngay; logo và pháo tan dần, nền chuyển sang độ tối phía sau bảng.
        public void Dismiss()
        {
            if (stage != Stage.Showing) { return; }
            stage = Stage.Backdrop;
            fadeAge = 0;
            group.blocksRaycasts = group.interactable = false;
        }

        public void Hide()
        {
            stage = Stage.Hidden;
            if (root != null) { root.gameObject.SetActive(false); }
        }

        public void Advance(float seconds)
        {
            if (stage == Stage.Hidden || root == null) { return; }
            if (stage == Stage.Backdrop)
            {
                if (fadeAge >= feel.WinShowFadeSeconds) { return; }
                fadeAge += seconds;
                float t = WinShowTimeline.Progress(fadeAge, 0, feel.WinShowFadeSeconds);
                stageGroup.alpha = 1 - t;
                var back = theme.WinDimColor;
                back.a = Mathf.Lerp(back.a, theme.WinBackdropAlpha, t);
                dim.color = back;
                return;
            }

            if (age >= feel.WinShowSeconds) { return; }
            age = Mathf.Min(feel.WinShowSeconds, age + seconds);
            Pose();
        }

        public void Dispose()
        {
            if (root != null) { UnityObjects.Release(root.gameObject); }
            root = null;
            stage = Stage.Hidden;
        }

        private void Pose()
        {
            var shade = theme.WinDimColor;
            shade.a *= WinShowTimeline.Progress(age, feel.WinDimStart, feel.WinDimSeconds);
            dim.color = shade;

            float wordStart = WinShowTimeline.WordStart(feel, letters);
            float landed = wordStart + feel.WinWordSeconds * .7f;
            // Nền logo giật nhẹ đúng lúc chữ lớn chạm xuống.
            float punch = Mathf.Sin(WinShowTimeline.Progress(age, landed, .2f) * Mathf.PI) * .06f;
            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                float t, size = 1, lift = 0, turn = 0;
                if (piece.Role == Role.Back)
                {
                    t = WinShowTimeline.Progress(age, feel.WinBackStart, feel.WinBackSeconds);
                    size = ComboSummary.Overshoot(t, 1.12f) + punch;
                }
                else if (piece.Role == Role.Letter)
                {
                    t = WinShowTimeline.Progress(age, WinShowTimeline.LetterStart(feel, piece.Order), feel.WinLetterSeconds);
                    size = WinShowTimeline.Slam(t, 1.8f);
                    lift = (1 - t) * (1 - t) * 90;
                }
                else if (piece.Role == Role.Word)
                {
                    t = WinShowTimeline.Progress(age, wordStart, feel.WinWordSeconds);
                    size = WinShowTimeline.Slam(t, 2.3f);
                }
                else
                {
                    t = WinShowTimeline.Progress(age, landed + piece.Order * .06f, .3f);
                    size = ComboSummary.Overshoot(t, 1.35f);
                    lift = Mathf.Sin(age * 2.6f + piece.Order * 2.1f) * 7;
                    turn = Mathf.Sin(age * 3 + piece.Order) * 10;
                }

                piece.Rect.anchoredPosition = piece.Home + new Vector2(0, lift);
                piece.Rect.localScale = Vector3.one * size;
                piece.Rect.localRotation = Quaternion.Euler(0, 0, turn);
                var color = Color.white;
                color.a = Mathf.Clamp01(t * 4);
                piece.Image.color = color;
            }

            for (int b = 0; b < bursts; b++) { PoseBurst(b); }
        }

        private void PoseBurst(int burst)
        {
            var spot = theme.WinFireworkSpot(burst);
            var origin = new Vector2(spot.x * width, spot.y * height);
            var color = theme.WinFireworkColor(burst);
            float start = WinShowTimeline.BurstStart(feel, burst);

            // Quả pháo bay từ đáy lên điểm nổ.
            float rise = WinShowTimeline.Progress(age, start, feel.WinRocketSeconds);
            var rocket = rockets[burst];
            rocket.enabled = rise > 0 && rise < 1;
            if (rocket.enabled)
            {
                float ease = 1 - (1 - rise) * (1 - rise);
                rocket.rectTransform.anchoredPosition = new Vector2(origin.x, Mathf.Lerp(-height * .5f, origin.y, ease));
                rocket.rectTransform.sizeDelta = new Vector2(theme.WinSparkSize * .6f, theme.WinSparkSize * 6 * (1 - rise * .6f));
                rocket.color = color;
            }

            float t = WinShowTimeline.Progress(age, start + feel.WinRocketSeconds, feel.WinSparkSeconds);
            float spread = 1 - (1 - t) * (1 - t) * (1 - t);
            for (int k = 0; k < Sparks; k++)
            {
                var spark = sparks[burst * Sparks + k];
                spark.enabled = t > 0 && t < 1;
                if (!spark.enabled) { continue; }
                // Góc đều quanh vòng, tầm xa giả ngẫu nhiên nhưng cố định theo chỉ số.
                float angle = (k + Mathf.Repeat(burst * .37f, 1)) * Mathf.PI * 2 / Sparks;
                float reach = .45f + .55f * Mathf.Repeat(k * .618f + burst * .29f, 1);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var position = origin + direction * (theme.WinBurstRadius * reach * spread) + Vector2.down * (theme.WinSparkDrop * t * t);
                spark.rectTransform.anchoredPosition = position;
                bool streak = k % 3 != 0;
                float size = theme.WinSparkSize * (1 - t * .5f);
                spark.rectTransform.sizeDelta = streak ? new Vector2(size * .45f, size * (1 + 2.6f * (1 - t))) : Vector2.one * size;
                spark.rectTransform.localRotation = Quaternion.Euler(0, 0, streak ? angle * Mathf.Rad2Deg - 90 : 0);
                var tint = k % 5 == 0 ? Color.white : color;
                tint.a = 1 - t * t;
                spark.color = tint;
            }
        }

        private void Build()
        {
            layout = JsonUtility.FromJson<Layout>(theme.WinLogoLayout.text);
            if (layout == null || layout.pieces == null || layout.pieces.Length == 0)
            {
                throw new InvalidOperationException("logo_layout.json rỗng hoặc sai dạng.");
            }

            root = Node("021 WinShow", parent);
            root.SetSiblingIndex(siblingIndex);
            Stretch(root, 0);
            group = root.gameObject.AddComponent<CanvasGroup>();

            // Nền tối tràn ra ngoài safe area để che cả vùng tai thỏ.
            var shade = Node("Dim", root);
            Stretch(shade, Bleed);
            dim = shade.gameObject.AddComponent<Image>();
            var skip = shade.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(Skip);

            // Logo và pháo hoa chung một nhóm để tan cùng nhau, không kéo theo nền tối.
            stageRoot = Node("Show", root);
            Stretch(stageRoot, 0);
            stageGroup = stageRoot.gameObject.AddComponent<CanvasGroup>();
            stageGroup.blocksRaycasts = false;

            bursts = Mathf.Min(MaxBursts, theme.WinFireworkSpotCount);
            rockets = new Image[bursts];
            sparks = new Image[bursts * Sparks];
            for (int b = 0; b < bursts; b++)
            {
                rockets[b] = Spark("Rocket " + b, theme.WinFxTrail);
                for (int k = 0; k < Sparks; k++)
                {
                    sparks[b * Sparks + k] = Spark("Spark " + b + "." + k, k % 3 != 0 ? theme.WinFxTrail : theme.WinFxDot);
                }
            }

            logo = Node("Logo", stageRoot);
            logo.sizeDelta = new Vector2(layout.width, layout.height);
            pieces = new Piece[layout.pieces.Length];
            letters = 0;
            int decorations = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                var data = layout.pieces[i];
                var rect = Node(data.sprite, logo);
                rect.sizeDelta = new Vector2(data.w, data.h);
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = theme.WinLogoSprite(data.sprite);
                image.raycastTarget = false;
                var role = data.role == "back" ? Role.Back : data.role == "letter" ? Role.Letter : data.role == "word" ? Role.Word : Role.Deco;
                int order = role == Role.Letter ? letters++ : role == Role.Deco ? decorations++ : 0;
                image.enabled = image.sprite != null;
                pieces[i] = new Piece { Rect = rect, Image = image, Home = new Vector2(data.x, data.y), Role = role, Order = order };
            }
        }

        private Image Spark(string name, Sprite sprite)
        {
            var rect = Node(name, stageRoot);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static RectTransform Node(string name, Transform owner)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.layer = owner.gameObject.layer;
            var rect = (RectTransform)item.transform;
            rect.SetParent(owner, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float bleed)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-bleed, -bleed);
            rect.offsetMax = new Vector2(bleed, bleed);
        }

        private enum Role { Back, Letter, Word, Deco }

        private struct Piece
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Home;
            public Role Role;
            public int Order;
        }

        [Serializable]
        public sealed class Layout
        {
            public float width, height;
            public LayoutPiece[] pieces;
        }

        [Serializable]
        public sealed class LayoutPiece
        {
            public string sprite, role;
            public float x, y, w, h;
        }
    }
}
