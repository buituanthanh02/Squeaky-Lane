using UnityEngine;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // 014: vệt bánh và tia đèn của một xe. Class thường do CarView giữ; object con tạo MỘT lần lúc dựng xe,
    // không tạo material lúc chạy (material/prefab lấy từ FeelConfig). Gắn vào root xe, không gắn modelRoot,
    // để vệt và đèn không nhún/lắc theo thân.
    public sealed class CarRoadFx
    {
        private readonly Transform owner;
        private readonly FeelConfig feel;
        private readonly float planeY;
        private readonly float halfLength;
        private readonly TrailRenderer trail;
        private readonly Transform head, tail;
        private float drive, target;

        public CarRoadFx(Transform root, FeelConfig config, float bodyLength, float boardPlaneY)
        {
            owner = root;
            feel = config;
            planeY = boardPlaneY;
            halfLength = bodyLength * .5f;
            if (feel.TrailEnabled && feel.TireTrailMaterial != null)
            {
                var node = new GameObject("014 Tire Trail");
                node.transform.SetParent(owner, false);
                // TransformZ: mặt vệt vuông góc với trục Z của node; xoay Z lên trời để vệt nằm phẳng trên bãi.
                node.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                trail = node.AddComponent<TrailRenderer>();
                trail.sharedMaterial = feel.TireTrailMaterial;
                trail.alignment = LineAlignment.TransformZ;
                trail.textureMode = LineTextureMode.Stretch;
                trail.time = feel.TrailSeconds;
                trail.widthMultiplier = feel.TrailWidth;
                trail.minVertexDistance = .08f;
                trail.numCapVertices = 2;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(feel.TrailAlpha, 0), new GradientAlphaKey(0, 1) });
                trail.colorGradient = gradient;
            }
            if (feel.HeadlightsEnabled)
            {
                head = Spawn(feel.HeadlightPrefab, 0);
                tail = Spawn(feel.TaillightPrefab, 180);
            }
            Clear();
        }

        private Transform Spawn(GameObject prefab, float yaw)
        {
            if (prefab == null) { return null; }
            var beams = Object.Instantiate(prefab, owner, false).transform;
            beams.localRotation = Quaternion.Euler(0, yaw, 0);
            beams.gameObject.SetActive(false);
            return beams;
        }

        // Tốc độ có dấu, đã chuẩn hóa theo tốc độ tối đa: +1 chạy tới hết tốc, -1 lùi hết tốc, 0 đứng yên.
        // 014 bùn: xe dính bùn để lại vệt nâu, sạch thì vệt trắng. Đổi sharedMaterial có sẵn, không tạo material.
        // Cả vệt đang có đổi màu theo; chấp nhận vì vệt chỉ sống 0,6 s.
        public void SetMud(bool muddy)
        {
            if (trail == null) { return; }
            var wanted = muddy && feel.MudTrailMaterial != null ? feel.MudTrailMaterial : feel.TireTrailMaterial;
            if (trail.sharedMaterial != wanted) { trail.sharedMaterial = wanted; }
        }

        // Đèn bật ít nhất 55% chiều dài ngay khi xe lăn bánh (đọc được hướng chạy từ khung hình đầu), rồi dài thêm theo tốc độ.
        public void Drive(float signedSpeed)
        {
            float speed = Mathf.Clamp01(Mathf.Abs(signedSpeed));
            target = speed < .01f ? 0 : Mathf.Sign(signedSpeed) * Mathf.Lerp(MinBeam, 1, speed);
        }
        private const float MinBeam = .55f;

        public void Advance(float seconds)
        {
            if (head == null && tail == null) { return; }
            drive = Mathf.MoveTowards(drive, target, feel.LightResponse * seconds);
            SetBeam(head, Mathf.Max(0, drive));
            SetBeam(tail, Mathf.Max(0, -drive));
        }

        private static void SetBeam(Transform beams, float length)
        {
            if (beams == null) { return; }
            bool visible = length > .02f;
            if (beams.gameObject.activeSelf != visible) { beams.gameObject.SetActive(visible); }
            if (visible) { beams.localScale = new Vector3(length, 1, 1); }
        }

        // Gọi sau khi CarView đặt pose: giữ vệt và đèn trên mặt bãi kể cả khi xe đổi cỡ (ra cổng, portal).
        public void Project()
        {
            if (owner.parent == null || Mathf.Abs(owner.localScale.x) < .0001f) { return; }
            Place(trail != null ? trail.transform : null, 0);
            Place(head, halfLength);
            Place(tail, -halfLength);
        }

        private void Place(Transform node, float x)
        {
            if (node == null) { return; }
            var board = owner.parent.InverseTransformPoint(owner.TransformPoint(new Vector3(x, 0, 0)));
            board.y = planeY + feel.RoadFxLift;
            node.localPosition = owner.InverseTransformPoint(owner.parent.TransformPoint(board));
        }

        // Xe nhảy chỗ (Hoàn tác, Chơi lại, portal): bỏ vệt cũ để không kéo vạch ngang bãi; tắt đèn.
        public void Clear()
        {
            if (trail != null) { trail.Clear(); }
            drive = target = 0;
            SetBeam(head, 0);
            SetBeam(tail, 0);
        }
    }
}
