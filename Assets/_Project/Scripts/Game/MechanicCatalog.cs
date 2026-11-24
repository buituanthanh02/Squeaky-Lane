using System;
using UnityEngine;

namespace SqueakyLane.Game
{
    public enum MechanicKind { Surface, ColorGate, WaterGate, Mud, Cone, Turntable, TrafficLight, Pylon, Portal }

    [CreateAssetMenu(menuName = "Squeaky Lane/Mechanic Catalog")]
    public sealed class MechanicCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public MechanicKind kind;
            [Min(0)] public int variant;
            public GameObject prefab;
        }

        [Serializable]
        public sealed class Design
        {
            [Header("Bãi: mặt trên giữ Y=-0.01 để khớp xe 010")]
            public Color asphalt = new Color32(58, 63, 71, 255);
            public Color markings = new Color(1, 1, 1, .6f);
            public Color curb = new Color32(201, 206, 214, 255);
            [Min(.005f)] public float lineWidth = .03f;
            [Min(.01f)] public float curbHeight = .15f, curbWidth = .18f;
            [Min(.001f)] public float surfaceThickness = .14f, overlayLift = .002f;
            [Header("Cổng: mở theo trục Z, ngang cổng là X")]
            [Min(.1f)] public float gateWidth = 1f, gateHeight = .9f;
            [Min(.01f)] public float gatePost = .12f, gateDepth = .14f, curtainDepth = .025f;
            [Range(3, 15)] public int curtainStripes = 7;
            [Range(0, 1)] public float curtainAlpha = .28f;
            [Range(.1f, 1)] public float curtainFill = .65f;
            [Min(.01f)] public float curtainBottom = .07f, nozzleSize = .08f;
            [Range(2, 3)] public int nozzles = 3;
            public Color water = new Color32(79, 195, 247, 255);
            [Header("Bùn / cọc / bàn xoay")]
            public Color mud = new Color32(91, 58, 30, 255);
            [Min(.01f)] public float mudRadius = .42f, mudHeight = .02f, coneHeight = .45f;
            [Range(8, 32)] public int roundSegments = 24;
            [Range(0, .2f)] public float puddleIrregularity = .07f;
            [Min(.1f)] public float puddleFrequency = 2.3f;
            public Color table = new Color32(138, 147, 163, 255);
            [Min(.01f)] public float tableDiameter = 1.9f, tableHeight = .035f, arrowWidth = .055f;
            [Range(.1f, .45f)] public float arrowRadiusFraction = .36f;
            [Min(1)] public float arrowTipLength = 3, arrowWingWidth = 2;
            [Header("Đèn / trụ / portal: chiều cao tối đa 0.9 ô")]
            public Color red = new Color32(229, 57, 53, 255), green = new Color32(67, 160, 71, 255);
            public Color dark = new Color32(35, 39, 46, 255), pylon = new Color32(249, 168, 37, 255);
            [Min(.01f)] public float stopWidth = .12f, stopSpan = .82f, poleHeight = .6f, poleWidth = .07f;
            [Min(.01f)] public float lampDiameter = .12f, lampBoxWidth = .18f, lampBoxHeight = .3f;
            public Vector3 poleOffset = new Vector3(.32f, 0, .32f);
            // Giữ tên field cũ để không mất dữ liệu serialize; hình trụ mới dùng nhóm tham số vòng tròn bên dưới.
            [HideInInspector] public float pylonHeight = .45f, pylonDiameter = .2f, armLength = .9f, armWidth = .12f;
            [HideInInspector] public int armStripes = 5;
            [Min(.01f)] public float portalWidth = .84f, portalHeight = .9f, portalRim = .055f;
            public Color[] portalColors = { new Color32(0, 229, 255, 255), new Color32(255, 64, 129, 255), new Color32(198, 255, 0, 255) };
            [Header("Sửa visual theo ảnh29/30: miệng trụ xoay và chống che xe")]
            [Min(.01f)] public float pylonRingDiameter = .9f, pylonDiscHeight = .035f, pylonRingWidth = .055f;
            [Min(.01f)] public float pylonEntryWidth = .9f, pylonEntryHeight = .3f, pylonEntryOffset = .5f;
            [Min(.01f)] public float pylonArrowLength = .18f, pylonArrowWidth = .2f, pylonLabelHeight = .34f;
            [Min(.1f), Tooltip("Vòng cửa tele dựng đứng; cả hai mặt A/B quay cùng nhau.")]
            public float pylonDoorHeight = .84f;
            [Min(.01f), Tooltip("Độ dày khung tele, vẫn thấy hình khi cửa quay ngang camera.")]
            public float pylonDoorDepth = .14f;
            public bool fadeOccluders = true;
            [Range(.02f, 1)] public float occludedAlpha = .18f;
            [Min(0)] public float occlusionPadding = .03f;
        }

        [SerializeField, Tooltip("Loại + biến thể → prefab. Cổng màu dùng chỉ số TileColor; portal dùng chỉ số cặp.")]
        private Entry[] entries = Array.Empty<Entry>();
        [SerializeField, Tooltip("Số visual dùng bởi menu dựng prefab và mặt bãi; không thay đổi lúc Play.")]
        private Design design = new Design();
        public Design Settings => design;
        public GameObject Prefab(MechanicKind kind, int variant = 0)
        {
            foreach (var entry in entries)
            {
                if (entry != null && entry.kind == kind && entry.variant == variant)
                {
                    return entry.prefab;
                }
            }
            return null;
        }

        private void OnValidate()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null) { continue; }
                for (int j = 0; j < i; j++)
                {
                    if (entries[j] != null && entries[i].kind == entries[j].kind && entries[i].variant == entries[j].variant)
                    {
                        Debug.LogError("MechanicCatalog trùng loại/biến thể: " + entries[i].kind + "/" + entries[i].variant, this);
                    }
                }
            }
            if (design.gateHeight > .9f || design.portalHeight > .9f || design.poleHeight + design.lampBoxHeight > .9001f)
            {
                Debug.LogError("MechanicCatalog: vật cao quá 0.9 ô có thể che xe.", this);
            }
            if (design.portalColors == null || design.portalColors.Length < 3)
            {
                Debug.LogError("MechanicCatalog cần ít nhất ba màu portal.", this);
            }
            if (design.gateWidth <= 2 * design.gatePost || design.gateHeight - design.gateWidth * .5f <= design.curtainBottom ||
                design.stopSpan <= 2 * design.stopWidth || design.portalWidth <= 4 * design.portalRim || design.portalHeight <= 2 * design.portalRim)
            {
                Debug.LogError("MechanicCatalog: kích thước không đủ chỗ cho cửa/rèm/vạch dừng/vòng portal.", this);
            }
            if (design.pylonRingDiameter <= 2 * design.pylonRingWidth || design.pylonEntryWidth <= 2 * design.pylonRingWidth ||
                design.pylonEntryHeight <= design.pylonRingWidth || design.pylonEntryHeight > .9f ||
                design.pylonDoorHeight <= 2 * design.pylonRingWidth || design.pylonDoorHeight + design.pylonDiscHeight > .9001f)
            {
                Debug.LogError("MechanicCatalog: vòng trụ/miệng nhận không đủ chỗ hoặc cao quá0.9 ô.", this);
            }
        }
    }
}
