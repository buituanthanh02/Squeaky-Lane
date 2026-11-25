using System;
using UnityEngine;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Asset Lab Config")]
    public sealed class AssetLabConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Sample
        {
            public string label;
            public GameObject prefab;
        }
        [SerializeField, Tooltip("Chọn mẫu trong dropdown của Inspector; không cần Play.")] private int selectedSample;
        [SerializeField] private Sample[] samples = Array.Empty<Sample>();
        [Header("Ánh sáng thử riêng; không sửa UiTheme hoặc Play.unity")]
        [SerializeField, Range(0, 2)] private float lightIntensity = 1;
        [SerializeField] private Vector3 lightAngles = new Vector3(50, -30, 0);
        [SerializeField] private Color ambient = new Color32(154, 169, 194, 255);
        [Header("Asset và mặt nền")]
        [SerializeField] private Vector3 modelAngles = Vector3.zero;
        [SerializeField, Range(.1f, 3)] private float modelScale = 1;
        [SerializeField, Range(0, 2), Tooltip("Nhấc asset để so bóng với mặt đất; không thay pivot của prefab nguồn.")] private float modelLift;
        [SerializeField] private Vector2 groundSize = new Vector2(6, 6);
        [SerializeField] private Color groundColor = new Color32(58, 63, 71, 255);
        [Header("Bóng thử riêng; không ghi vào material asset đang dùng trong game")]
        [SerializeField] private bool showShadow = true;
        [SerializeField, Range(0, .5f)] private float shadowAlpha = .28f;
        [SerializeField] private Vector2 shadowOffset = new Vector2(-.12f, .1f);
        [SerializeField, Range(.002f, .03f)] private float shadowLift = .008f;
        [SerializeField, Tooltip("0 dùng footprint tự tính từ asset; lớn hơn0 dùng cỡ nhập (ô bãi).")] private Vector2 shadowSize = Vector2.zero;
        [SerializeField] private GameObject shadowPrefab = null;
        [SerializeField] private Material groundMaterial = null;
        [SerializeField] private MechanicCatalog mechanics = null;
        public event Action Changed;
        public Sample[] Samples => samples;
        public int SelectedSample => selectedSample;
        public GameObject SelectedPrefab => selectedSample >= 0 && selectedSample < samples.Length && samples[selectedSample] != null ? samples[selectedSample].prefab : null;
        public float LightIntensity => lightIntensity;
        public Vector3 LightAngles => lightAngles;
        public Color Ambient => ambient;
        public Vector3 ModelAngles => modelAngles;
        public float ModelScale => modelScale;
        public float ModelLift => modelLift;
        public Vector2 GroundSize => groundSize;
        public Color GroundColor => groundColor;
        public bool ShowShadow => showShadow;
        public float ShadowAlpha => shadowAlpha;
        public Vector2 ShadowOffset => shadowOffset;
        public float ShadowLift => shadowLift;
        public Vector2 ShadowSize => shadowSize;
        public GameObject ShadowPrefab => shadowPrefab;
        public Material GroundMaterial => groundMaterial;
        public MechanicCatalog Mechanics => mechanics;
        private void OnValidate()
        {
            selectedSample = Mathf.Clamp(selectedSample, 0, Mathf.Max(0, samples.Length - 1));
            lightIntensity = Clean(lightIntensity, 0, 2, 1);
            modelScale = Clean(modelScale, .1f, 3, 1);
            modelLift = Clean(modelLift, 0, 2, 0);
            shadowAlpha = Clean(shadowAlpha, 0, .5f, .28f);
            shadowLift = Clean(shadowLift, .002f, .03f, .008f);
            groundSize.x = Clean(groundSize.x, 2, 30, 6);
            groundSize.y = Clean(groundSize.y, 2, 30, 6);
            shadowSize.x = Clean(shadowSize.x, 0, 20, 0);
            shadowSize.y = Clean(shadowSize.y, 0, 20, 0);
            // Listener chỉ đánh dấu dirty; tạo/xóa object ở Editor tick chính, không làm trong OnValidate.
            Changed?.Invoke();
        }
        private static float Clean(float value, float min, float max, float fallback)
        { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max); }
    }
}
