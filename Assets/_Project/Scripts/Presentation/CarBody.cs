using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Chỉ sửa hình con. Root/collider vẫn hoàn toàn do ActionAnimator/CarView quản lý.
    public sealed class CarBody : MonoBehaviour
    {
        private FeelConfig feel;
        private Transform visual, dirt;
        private Vector3 visualPosition, visualScale, dirtPosition, dirtScale;
        private Quaternion visualRotation, dirtRotation;
        private Wheel[] wheels = System.Array.Empty<Wheel>();
        private float pitchTime, pitchDuration, pitchAmount, squashTime, mudTime, washTime, hitTime;
        private float exitSign, rollAngle;
        private float impactScale = 1;
        private Vector3 hitDirection;
        private bool braking, exiting;

        public void Bind(FeelConfig config, Transform model, Transform mud)
        {
            ResetPose();
            feel = config;
            visual = model;
            dirt = mud;
            visualPosition = visual.localPosition;
            visualScale = visual.localScale;
            visualRotation = visual.localRotation;
            dirtPosition = dirt.localPosition;
            dirtScale = dirt.localScale;
            dirtRotation = dirt.localRotation;
            var found = new List<Wheel>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.name.StartsWith("wheel-", System.StringComparison.Ordinal) || filter.sharedMesh == null)
                {
                    continue;
                }
                var node = filter.transform;
                var matrix = transform.worldToLocalMatrix * node.localToWorldMatrix;
                var extent = filter.sharedMesh.bounds.extents;
                float height = Mathf.Abs(matrix.MultiplyVector(Vector3.right * extent.x).y) +
                    Mathf.Abs(matrix.MultiplyVector(Vector3.up * extent.y).y) +
                    Mathf.Abs(matrix.MultiplyVector(Vector3.forward * extent.z).y);
                if (height <= Mathf.Epsilon)
                {
                    continue;
                }
                found.Add(new Wheel
                {
                    Node = node, Position = node.localPosition, Rotation = node.localRotation,
                    Center = filter.sharedMesh.bounds.center, Radius = height,
                    Axis = node.parent.InverseTransformDirection(transform.TransformDirection(Vector3.back)).normalized
                });
            }
            wheels = found.ToArray();
            ResetPose();
        }

        public void StartMotion(int sign)
        {
            if (feel == null) { return; }
            exiting = false;
            braking = false;
            pitchAmount = feel.LaunchPitch * sign;
            pitchTime = 0;
            pitchDuration = feel.LaunchSeconds;
        }

        // slideCells: số ô xe vừa trượt trước khi dừng; va mạnh hơn khi trượt dài (FeelConfig.ImpactScale).
        public void Play(MoveEvent ev, int slideCells = 0)
        {
            if (feel == null) { return; }
            switch (ev.Kind)
            {
                case MoveEventKind.Stop:
                    exiting = false;
                    braking = true;
                    impactScale = feel.ImpactScale(slideCells);
                    pitchAmount = feel.BrakePitch * ev.Sign * impactScale;
                    pitchTime = 0;
                    pitchDuration = feel.BrakeSeconds;
                    squashTime = 0;
                    break;
                case MoveEventKind.Exit:
                    exiting = true;
                    exitSign = ev.Sign;
                    break;
                case MoveEventKind.Mud:
                    mudTime = 0;
                    break;
                case MoveEventKind.Water:
                case MoveEventKind.Spray:
                    washTime = 0;
                    break;
            }
        }

        public void Hit(Vector3 worldDirection)
        {
            hitDirection = transform.InverseTransformDirection(worldDirection).normalized;
            hitTime = 0;
        }

        public void Roll(float signedDistance)
        {
            if (feel == null || !feel.WheelsEnabled) { return; }
            rollAngle += signedDistance;
        }

        public void Advance(float seconds)
        {
            if (feel == null || visual == null || dirt == null) { return; }
            pitchTime += seconds;
            squashTime += seconds;
            mudTime += seconds;
            washTime += seconds;
            hitTime += seconds;
            float pitch = 0;
            var scale = Vector3.one;
            var offset = Vector3.zero;
            float roll = 0;
            if (feel.BodyEnabled)
            {
                float pulse = braking ? BodySpring.Sample(pitchTime, pitchDuration,
                    feel.BrakePitch > 0 ? feel.BrakeRebound / feel.BrakePitch : 0) : EaseReturn(pitchTime, pitchDuration);
                pitch = exiting ? feel.ExitPitch * exitSign : pitchAmount * pulse;
                float squash = EaseReturn(squashTime, feel.SquashSeconds);
                // Độ ép nhân với hệ số va: xe trượt dài rồi dừng ép sâu hơn. Hệ số 1 cho kết quả như Lerp cũ.
                float depth = squash * impactScale;
                scale.x = exiting ? feel.ExitStretch : 1 + (feel.BrakeLength - 1) * depth;
                scale.y = 1 + (feel.BrakeHeight - 1) * depth;
                scale.z = 1 + (feel.BrakeWidth - 1) * depth;
                offset += hitDirection * (feel.HitDistance * Mathf.Sin(Mathf.Clamp01(hitTime / feel.HitSeconds) * Mathf.PI));
                if (feel.MudBodyEnabled)
                {
                    float sink = Mathf.Sin(Mathf.Clamp01(mudTime / feel.MudSeconds) * Mathf.PI);
                    offset.y = -feel.MudSink * sink;
                    scale.y *= Mathf.Lerp(1, feel.MudHeight, sink);
                    float wash = Mathf.Clamp01(washTime / feel.WashSeconds);
                    roll = Mathf.Sin(wash * feel.WashCycles * 2 * Mathf.PI) * feel.WashRoll * (1 - wash);
                }
            }
            // +X là đầu xe: pitch dương chúi xuống => quay âm quanh Z. Roll quanh X.
            var rotation = Quaternion.Euler(roll, 0, -pitch);
            visual.localPosition = offset + rotation * Vector3.Scale(visualPosition, scale);
            visual.localRotation = rotation * visualRotation;
            visual.localScale = Vector3.Scale(visualScale, scale);
            dirt.localPosition = offset + rotation * Vector3.Scale(dirtPosition, scale);
            dirt.localRotation = rotation * dirtRotation;
            dirt.localScale = Vector3.Scale(dirtScale, scale);
            for (int i = 0; i < wheels.Length; i++)
            {
                var wheel = wheels[i];
                if (wheel.Node == null) { continue; }
                float angle = feel.WheelsEnabled ? rollAngle / wheel.Radius * Mathf.Rad2Deg : 0;
                var turn = Quaternion.AngleAxis(angle, wheel.Axis);
                wheel.Node.localRotation = turn * wheel.Rotation;
                // FBX có thể đặt pivot ở gốc model. Giữ tâm bounds bánh đứng yên khi quay.
                var center = wheel.Rotation * Vector3.Scale(wheel.Center, wheel.Node.localScale);
                wheel.Node.localPosition = wheel.Position + center - turn * center;
            }
        }

        private static float EaseReturn(float elapsed, float duration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(Mathf.Epsilon, duration));
            return (1 - t) * (1 - t);
        }

        public void ResetPose()
        {
            pitchTime = squashTime = mudTime = washTime = hitTime = float.MaxValue;
            pitchDuration = 1;
            pitchAmount = rollAngle = 0;
            impactScale = 1;
            exiting = braking = false;
            if (visual != null)
            {
                visual.localPosition = visualPosition;
                visual.localRotation = visualRotation;
                visual.localScale = visualScale;
            }
            if (dirt != null)
            {
                dirt.localPosition = dirtPosition;
                dirt.localRotation = dirtRotation;
                dirt.localScale = dirtScale;
            }
            for (int i = 0; i < wheels.Length; i++)
            {
                var wheel = wheels[i];
                if (wheel.Node == null) { continue; }
                wheel.Node.localPosition = wheel.Position;
                wheel.Node.localRotation = wheel.Rotation;
            }
        }

        private void OnDisable() { ResetPose(); }
        private struct Wheel
        {
            public Transform Node;
            public Vector3 Position, Center, Axis;
            public Quaternion Rotation;
            public float Radius;
        }
    }
}
