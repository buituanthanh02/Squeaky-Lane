using System.Collections.Generic;
using UnityEngine;

namespace SqueakyLane.Presentation
{
    // 014: trạm rửa ở ô vòi xịt. Chỉ là hình: chổi lăn quay và hệ hạt bọt khi xe bò qua; luật vẫn do Core quyết.
    // Prefab do Codex dựng (Art), Claude gắn script này. Tìm phần tử theo tên để prefab không phải gán tay:
    //   "Roller N|S|E|W" có con "Spin" (quay quanh trục dọc cạnh: N/S theo X, E/W theo Z của trạm),
    //   các ParticleSystem con (Spray N|S|E|W) phun bọt vào tâm ô.
    public sealed class WashStationView : MonoBehaviour
    {
        private sealed class Roller
        {
            public Transform Spin;
            public Quaternion Rest;
            public Vector3 Axis;
        }

        private readonly List<Roller> rollers = new List<Roller>(4);
        private ParticleSystem[] sprays = System.Array.Empty<ParticleSystem>();
        private float remaining, speed, angle, spinTarget;
        private bool built;

        public bool IsActive => remaining > 0;
        public int RollerCount { get { Build(); return rollers.Count; } }

        private void Awake() { Build(); }

        // Tạo danh sách chổi và hệ hạt một lần; gọi lại không làm gì.
        private void Build()
        {
            if (built) { return; }
            built = true;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Spin" || t.parent == null || !t.parent.name.StartsWith("Roller ", System.StringComparison.Ordinal)) { continue; }
                string side = t.parent.name.Substring("Roller ".Length);
                var stationAxis = side == "E" || side == "W" ? Vector3.forward : Vector3.right;
                rollers.Add(new Roller
                {
                    Spin = t,
                    Rest = t.localRotation,
                    Axis = t.parent.InverseTransformDirection(transform.TransformDirection(stationAxis)).normalized
                });
            }
            sprays = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < sprays.Length; i++)
            {
                var main = sprays[i].main;
                main.playOnAwake = false;
                sprays[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // Xe bắt đầu bò qua ô: quay chổi và xịt trong `seconds` giây (hạt còn bay nốt sau đó).
        public void Begin(float seconds, float spinDegreesPerSecond)
        {
            Build();
            remaining = Mathf.Max(0, seconds);
            spinTarget = spinDegreesPerSecond;
            for (int i = 0; i < sprays.Length; i++) { sprays[i].Play(true); }
            enabled = true;
        }

        // Hoàn tác/Chơi lại: tắt ngay, xóa hạt, chổi về tư thế nghỉ.
        public void Cancel()
        {
            Build();
            remaining = 0;
            speed = 0;
            angle = 0;
            spinTarget = 0;
            for (int i = 0; i < sprays.Length; i++) { sprays[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            Apply();
        }

        // Dùng đồng hồ không scale như animator; chổi tăng/giảm tốc mềm để trông nặng tay, không giật.
        private void Update() { Advance(Time.unscaledDeltaTime); }

        public void Advance(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds)) { return; }
            Build();
            if (remaining > 0)
            {
                remaining -= seconds;
                if (remaining <= 0)
                {
                    remaining = 0;
                    for (int i = 0; i < sprays.Length; i++) { sprays[i].Stop(true, ParticleSystemStopBehavior.StopEmitting); }
                }
            }
            float goal = remaining > 0 ? spinTarget : 0;
            speed = Mathf.MoveTowards(speed, goal, Mathf.Max(1, spinTarget) * 6 * seconds);
            angle = Mathf.Repeat(angle + speed * seconds, 360);
            Apply();
            if (remaining <= 0 && speed <= 0.01f) { enabled = false; }
        }

        private void Apply()
        {
            for (int i = 0; i < rollers.Count; i++)
            {
                var roller = rollers[i];
                if (roller.Spin == null) { continue; }
                roller.Spin.localRotation = Quaternion.AngleAxis(angle, roller.Axis) * roller.Rest;
            }
        }

        private void OnDisable()
        {
            // Object bị tắt giữa lúc rửa (đổi màn/ẩn bãi): không để hạt phát tiếp.
            remaining = 0;
            speed = 0;
        }
    }
}
