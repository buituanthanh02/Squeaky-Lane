using System;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class CarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Model nguồn đã bù hướng/cỡ ở con; không đặt collider trong đây.")] private Transform modelRoot;
        [SerializeField, Tooltip("Lớp bùn riêng, nằm trên mái xe.")] private Renderer mudOverlay;
        [SerializeField, Min(2), Tooltip("Footprint prefab: 2 cho xe thường, 3 cho xe tải.")] private int footprintLength = 2;
        private Renderer[] modelRenderers;
        private Renderer blockBody;
        private BoxCollider footprint;
        private MaterialPropertyBlock block;
        private VisualPalette palette;
        private CarCatalog catalog;
        private Action<TileColor, bool> missing;
        private GameObject sourcePrefab;
        private Transform numberLabel;
        private TileColor lastColor;
        private bool truck, appearanceReady;
        private CarBody body;
        private FeelConfig feel;
        private int lastDirt = -1;
        private float mudAlpha, mudFadeTime;
        private bool fadingMud;
        private ContactShadowView contactShadow;
        private CarRoadFx roadFx;
        // 014 bùn kiểu D: mudOverlay có thể là cục bùn khối (prefab FeelConfig) thay cho tấm phẳng của prefab xe.
        private bool mudIsBlob;
        private Material mudSolid;
        public TileColor Color => lastColor;
        // Xe đang mang bùn (chưa rửa); FeelEffects dùng để thả giọt bùn khi chạy.
        public bool HasMud => lastDirt > 0 && !fadingMud;
        public int Index { get; private set; }
        public int Length { get; private set; }
        public Vector3 PositiveDirection => transform.right;
        public void ConfigureShadow(UiTheme theme)
        {
            if (contactShadow != null) { return; }
            if (theme == null) { return; }
            contactShadow = ContactShadowView.Attach(transform, theme,
                new Vector2(Length * theme.CarShadowLengthRatio, theme.CarShadowWidth), LotSurfaceView.Top);
        }
        public void ConfigureFeel(FeelConfig config)
        {
            feel = config;
            body = GetComponent<CarBody>();
            if (body == null) { body = gameObject.AddComponent<CarBody>(); }
            SwapMudBlob();
            BindBody();
            // Vệt bánh/đèn dựng một lần cho mỗi xe; thân dài Length - 0,2 ô như bao chiếm chỗ của ActionAnimator.
            if (roadFx == null && config != null) { roadFx = new CarRoadFx(transform, config, Length - .2f, LotSurfaceView.Top); }
        }
        // 014: tốc độ có dấu đã chuẩn hóa (+1 chạy tới hết tốc, -1 lùi hết tốc); ActionAnimator gọi mỗi frame.
        public void Drive(float signedSpeed) { if (roadFx != null) { roadFx.Drive(signedSpeed); } }
        public void ClearTrail() { if (roadFx != null) { roadFx.Clear(); } }
        private void BindBody()
        {
            if (body != null && modelRoot != null && mudOverlay != null)
            {
                body.Bind(feel, modelRoot, mudOverlay.transform);
            }
        }
        public void StartMotion(int sign) { if (body != null) { body.StartMotion(sign); } }
        public void PlayFeel(MoveEvent ev, int slideCells = 0)
        {
            if (body != null) { body.Play(ev, slideCells); }
            if (ev.Kind == MoveEventKind.Water || ev.Kind == MoveEventKind.Spray) { WashMud(); }
        }
        public void Hit(Vector3 worldDirection) { if (body != null) { body.Hit(worldDirection); } }
        public void Roll(float distance) { if (body != null) { body.Roll(distance); } }
        public void AdvanceFeel(float seconds)
        {
            if (body != null) { body.Advance(seconds); }
            if (roadFx != null) { roadFx.Advance(seconds); }
            if (!fadingMud || feel == null) { return; }
            mudFadeTime += seconds;
            mudAlpha = 1 - Mathf.Clamp01(mudFadeTime / feel.MudFadeSeconds);
            ApplyMudAlpha();
            if (mudAlpha <= 0) { fadingMud = false; }
        }
        public void ResetFeel()
        {
            if (body != null) { body.ResetPose(); }
            if (roadFx != null) { roadFx.Clear(); }
            fadingMud = false;
            lastDirt = -1;
        }
        private void WashMud()
        {
            if (feel == null || !feel.MudBodyEnabled || mudAlpha <= 0 || fadingMud) { return; }
            fadingMud = true;
            mudFadeTime = 0;
        }
        private void ApplyMudAlpha()
        {
            // Cục bùn khối dùng material đặc (Opaque) khi đủ bùn để không lỗi sắp xếp mặt trong suốt;
            // chỉ lúc đang rửa (mờ dần) mới đổi sang MudFade, cùng màu với material đặc.
            bool solid = mudSolid != null && mudAlpha >= .999f;
            var wanted = solid ? mudSolid : feel != null ? feel.MudFadeMaterial : null;
            if (wanted != null && mudOverlay.sharedMaterial != wanted)
            {
                mudOverlay.sharedMaterial = wanted;
            }
            if (solid)
            {
                mudOverlay.SetPropertyBlock(null);
            }
            else
            {
                var color = mudSolid != null ? mudSolid.color : palette.Mud;
                color.a *= mudAlpha;
                block.SetColor("_Color", color);
                mudOverlay.SetPropertyBlock(block);
            }
            mudOverlay.gameObject.SetActive(mudAlpha > 0);
        }

        // Thay tấm bùn phẳng (prefab xe hoặc khối dự phòng) bằng cục bùn khối, đặt đáy cục bùn đúng mặt nóc
        // và giãn X/Z theo cỡ tấm cũ (Y giữ đơn vị thế giới). Chạy một lần mỗi lần dựng hình xe.
        private void SwapMudBlob()
        {
            if (mudIsBlob || feel == null || feel.MudBlobPrefab == null || mudOverlay == null) { return; }
            var old = mudOverlay.transform;
            var blob = Instantiate(feel.MudBlobPrefab, transform, false);
            blob.name = "014 Mud Blob";
            var renderer = blob.GetComponent<Renderer>();
            if (renderer == null)
            {
                Release(blob);
                return;
            }
            blob.transform.localPosition = old.localPosition - new Vector3(0, old.localScale.y * .5f, 0);
            blob.transform.localRotation = old.localRotation;
            blob.transform.localScale = new Vector3(old.localScale.x, 1, old.localScale.z);
            blob.SetActive(old.gameObject.activeSelf);
            mudSolid = renderer.sharedMaterial;
            old.gameObject.SetActive(false);
            Release(old.gameObject);
            mudOverlay = renderer;
            mudIsBlob = true;
        }
        public bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            if (!gameObject.activeInHierarchy || modelRenderers == null) { return false; }
            foreach (var renderer in modelRenderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) { continue; }
                if (found) { bounds.Encapsulate(renderer.bounds); }
                else { bounds = renderer.bounds; found = true; }
            }
            return found;
        }
        public static bool Usable(GameObject prefab, int length)
        {
            if (prefab == null) return false;
            var view = prefab.GetComponent<CarView>();
            return view != null && view.modelRoot != null && view.mudOverlay != null &&
                view.footprintLength == length && prefab.GetComponent<BoxCollider>() != null &&
                prefab.transform.localPosition == Vector3.zero && prefab.transform.localScale == Vector3.one && prefab.transform.localRotation == Quaternion.identity;
        }

        public void ConfigurePrefab(Transform model, Renderer dirt, int length)
        {
            modelRoot = model;
            mudOverlay = dirt;
            footprintLength = length;
        }

        public void Initialize(int index, int length, VisualPalette colors, CarCatalog vehicles,
            bool isTruck, TileColor color, Action<TileColor, bool> warnMissing)
        {
            Index = index;
            Length = length;
            palette = colors;
            catalog = vehicles;
            truck = isTruck;
            missing = warnMissing;
            block = new MaterialPropertyBlock();
            footprint = GetComponent<BoxCollider>();
            if (footprint == null) footprint = gameObject.AddComponent<BoxCollider>();
            var initial = catalog != null ? catalog.Prefab(color, truck) : null;
            sourcePrefab = modelRoot != null && Usable(initial, Length) ? initial : null;
            appearanceReady = false;
        }

        public void SetLabel(Transform label)
        {
            numberLabel = label;
        }

        public void Show(Vector3 position, float yaw, TileColor color, int dirt, bool alive, float scale = 1)
        {
            transform.localPosition = position; transform.localRotation = Quaternion.Euler(0, yaw, 0);
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(alive);
            if (alive && contactShadow != null) { contactShadow.Project(); }
            if (alive && roadFx != null) { roadFx.Project(); }
            if (!appearanceReady || color != lastColor)
            {
                ApplyAppearance(color);
            }
            if (dirt != lastDirt)
            {
                bool snap = lastDirt < 0;
                if (!snap && dirt == 0 && feel != null && feel.MudBodyEnabled && mudAlpha > 0)
                {
                    WashMud();
                }
                else
                {
                    fadingMud = false;
                    mudAlpha = dirt > 0 ? 1 : 0;
                }
                lastDirt = dirt;
                if (roadFx != null) { roadFx.SetMud(dirt > 0); }
            }
            ApplyMudAlpha();
        }

        private void ApplyAppearance(TileColor color)
        {
            var entry = catalog != null ? catalog.Find(color) : null;
            var prefab = entry == null ? null : truck ? entry.truckPrefab : entry.carPrefab;
            if (!Usable(prefab, Length) || entry.material == null)
            {
                prefab = null;
                missing?.Invoke(color, truck);
            }
            if (modelRoot == null || sourcePrefab != prefab || (prefab == null && blockBody == null))
            {
                ReleaseVisual();
                if (prefab != null)
                {
                    var template = prefab.GetComponent<CarView>();
                    modelRoot = Instantiate(template.modelRoot, transform, false);
                    mudOverlay = Instantiate(template.mudOverlay, transform, false);
                    var templateCollider = prefab.GetComponent<BoxCollider>();
                    footprint.center = templateCollider.center;
                    footprint.size = templateCollider.size;
                }
                else
                {
                    CreateBlock(color);
                }
                sourcePrefab = prefab;
            }
            // Cache sau spawn/đổi dáng, không dò renderer hoặc đổi material mỗi frame.
            modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (prefab != null)
            {
                for (int i = 0; i < modelRenderers.Length; i++)
                {
                    modelRenderers[i].SetPropertyBlock(null);
                    modelRenderers[i].sharedMaterial = entry.material;
                }
            }
            else
            {
                block.SetColor("_Color", palette.Car(color));
                blockBody.SetPropertyBlock(block);
            }
            block.SetColor("_Color", palette.Mud);
            mudOverlay.SetPropertyBlock(block);
            SwapMudBlob();
            if (numberLabel != null)
            {
                numberLabel.localPosition = new Vector3(0, Mathf.Max(.47f, footprint.center.y + footprint.size.y * .5f + .08f), 0);
            }
            lastColor = color;
            appearanceReady = true;
            BindBody();
        }

        private void CreateBlock(TileColor color)
        {
            modelRoot = new GameObject("Visual dự phòng").transform;
            modelRoot.SetParent(transform, false);
            blockBody = Box("Thân", Vector3.zero, new Vector3(Length - .18f, .5f, .78f), palette.Car(color), modelRoot);
            Box("Mui", new Vector3(0, .28f, 0), new Vector3(Length * .5f, .18f, .63f), palette.Wall, modelRoot);
            mudOverlay = Box("Bùn bám", new Vector3(0, .4f, 0), new Vector3(Length * .6f, .05f, .55f), palette.Mud, transform);
            footprint.center = Vector3.zero;
            footprint.size = new Vector3(Length - .18f, .5f, .78f);
        }

        private Renderer Box(string label, Vector3 position, Vector3 size, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Release(collider);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = palette.Material;
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
            return renderer;
        }

        private void ReleaseVisual()
        {
            if (body != null) { body.ResetPose(); }
            if (modelRoot != null)
            {
                modelRoot.gameObject.SetActive(false);
                Release(modelRoot.gameObject);
            }
            if (mudOverlay != null)
            {
                mudOverlay.gameObject.SetActive(false);
                Release(mudOverlay.gameObject);
            }
            modelRoot = null;
            mudOverlay = null;
            mudIsBlob = false;
            mudSolid = null;
        }

        private static void Release(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
