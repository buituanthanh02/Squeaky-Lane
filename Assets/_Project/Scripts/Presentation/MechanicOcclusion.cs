using UnityEngine;

namespace SqueakyLane.Presentation
{
    // BoardView kiểm che xe từ camera chơi. Hai bộ material đều là asset dựng trước ở Editor.
    public sealed class MechanicOcclusion : MonoBehaviour
    {
        [SerializeField] private Renderer target;
        [SerializeField] private Material[] solidMaterials, fadedMaterials;
        private bool faded;
        public bool IsFaded => faded;
        public bool IsConfigured => target != null && solidMaterials != null && fadedMaterials != null &&
            solidMaterials.Length > 0 && solidMaterials.Length == fadedMaterials.Length;
        public Bounds VisualBounds => target.bounds;
        public void Configure(Renderer renderer, Material[] solid, Material[] transparent)
        {
            target = renderer;
            solidMaterials = solid;
            fadedMaterials = transparent;
        }
        public void SetFaded(bool value)
        {
            if (!IsConfigured || faded == value) { return; }
            faded = value;
            target.sharedMaterials = value ? fadedMaterials : solidMaterials;
        }
        private void OnDisable() { SetFaded(false); }

        public static bool CoversCar(Camera camera, Bounds obstacle, Bounds car, float padding)
        {
            obstacle.Expand(Mathf.Max(0, padding) * 2);
            if (!Project(camera, obstacle, out var obstacleRect, out float obstacleNear, out _) ||
                !Project(camera, car, out var carRect, out _, out float carFar)) { return false; }
            // Chỉ mờ vật ở trước xe trên hình chiếu, không mờ vật nằm hoàn toàn sau xe.
            return obstacleNear < carFar && obstacleRect.Overlaps(carRect);
        }
        private static bool Project(Camera camera, Bounds bounds, out Rect rect, out float near, out float far)
        {
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            near = float.PositiveInfinity;
            far = float.NegativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = camera.WorldToViewportPoint(corner);
                if (point.z <= camera.nearClipPlane) { rect = default; return false; }
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
                near = Mathf.Min(near, point.z);
                far = Mathf.Max(far, point.z);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }
    }
}
