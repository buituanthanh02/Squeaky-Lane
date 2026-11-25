using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Một prefab cho cả bãi; mỗi loại một ParticleSystem. Emit không sinh GameObject/material.
    public sealed class FeelEffects : MonoBehaviour
    {
        [SerializeField, Tooltip("Tia nước, circle_01.")] private ParticleSystem jets;
        [SerializeField, Tooltip("Hơi nước và bụi va, smoke_01 dùng chung.")] private ParticleSystem smoke;
        [SerializeField, Tooltip("Bọt cổng màu, circle_05.")] private ParticleSystem bubbles;
        [SerializeField, Tooltip("Lấp lánh, star_05.")] private ParticleSystem sparkles;
        [SerializeField, Tooltip("Pháo giấy dùng spark_05.")] private ParticleSystem confetti;
        private ParticleSystem[] systems;
        private ActionAnimator animator;
        private BoardView board;
        private Camera boardCamera;
        private FeelConfig feel;
        private VisualPalette palette;
        private Vector3 cameraOffset;
        private Vector3 appliedCameraPosition;
        private float shakeTime = float.MaxValue;
        public int LiveCount
        {
            get
            {
                int count = 0;
                if (systems == null) { return 0; }
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i] != null) { count += systems[i].particleCount; }
                }
                return count;
            }
        }

        public void ConfigurePrefab(ParticleSystem water, ParticleSystem mist, ParticleSystem foam,
            ParticleSystem stars, ParticleSystem paper)
        {
            jets = water;
            smoke = mist;
            bubbles = foam;
            sparkles = stars;
            confetti = paper;
        }

        public void Initialize(ActionAnimator animation, BoardView view, Camera camera, FeelConfig config, VisualPalette colors)
        {
            Disconnect();
            animator = animation;
            board = view;
            boardCamera = camera;
            feel = config;
            palette = colors;
            systems = new[] { jets, smoke, bubbles, sparkles, confetti };
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) { continue; }
                var main = systems[i].main;
                main.maxParticles = config.MaxLiveParticles;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = false;
                var emission = systems[i].emission;
                emission.enabled = true;
                emission.rateOverTime = 0;
                emission.rateOverDistance = 0;
                emission.burstCount = 0;
                // Simulate điều khiển thời gian và tự pause. Hạt dùng cùng đồng hồ với animator.
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            animator.CarEventStarted += OnCarEvent;
            animator.Cancelled += Clear;
            Clear();
        }

        private void OnCarEvent(CarView car, MoveEvent ev)
        {
            var direction = car.PositiveDirection * ev.Sign;
            var position = car.transform.position + direction * (car.Length * .5f);
            if ((ev.Kind == MoveEventKind.Water || ev.Kind == MoveEventKind.Spray) && feel.WaterParticlesEnabled)
            {
                if (ev.Kind == MoveEventKind.Spray) { position = car.transform.position; }
                int jetCount = Mathf.RoundToInt(feel.Water.Count * feel.WaterJetRatio);
                Emit(jets, feel.Water, position, feel.WaterColor, jetCount, true);
                Emit(smoke, feel.Water, position, feel.SparkleColor, feel.Water.Count - jetCount);
            }
            else if (ev.Kind == MoveEventKind.Exit && feel.GateParticlesEnabled)
            {
                Emit(bubbles, feel.Bubbles, position, palette.Car(car.Color), feel.Bubbles.Count);
                Emit(sparkles, feel.Sparkles, position, feel.SparkleColor, feel.Sparkles.Count);
            }
            else if (ev.Kind == MoveEventKind.Stop && IsImpact(ev.Reason))
            {
                if (feel.ImpactParticlesEnabled)
                {
                    Emit(smoke, feel.Impact, position, feel.ImpactColor, feel.Impact.Count);
                }
                if (feel.ShakeEnabled) { shakeTime = 0; }
            }
        }

        private static bool IsImpact(StopReason? reason)
        {
            return reason.HasValue && reason.Value != StopReason.Push;
        }

        public void Celebrate()
        {
            if (feel == null || board == null || board.Level == null || !feel.ConfettiEnabled) { return; }
            Vector3 center = board.transform.TransformPoint(new Vector3(0, feel.ConfettiHeight, 0));
            int count = Mathf.Min(feel.Confetti.Count, feel.MaxLiveParticles - LiveCount);
            for (int i = 0; i < count; i++)
            {
                var spread = new Vector3(Random.Range(-1f, 1f) * board.Level.Cols, 0,
                    Random.Range(-1f, 1f) * board.Level.Rows) * feel.ConfettiSpread;
                Emit(confetti, feel.Confetti, center + board.transform.TransformVector(spread),
                    palette.Car((TileColor)(i % (int)TileColor.Water)), 1);
            }
        }

        private void Emit(ParticleSystem system, FeelConfig.ParticleFeel settings, Vector3 position, Color color,
            int requested, bool waterTint = false)
        {
            if (system == null) { return; }
            int count = Mathf.Min(requested, feel.MaxLiveParticles - LiveCount);
            var main = system.main;
            main.gravityModifier = settings.Gravity;
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                startLifetime = settings.Lifetime,
                startSize = settings.Size,
                applyShapeToPosition = false
            };
            for (int i = 0; i < count; i++)
            {
                var horizontal = Random.insideUnitCircle * feel.ParticleSpread;
                emit.velocity = new Vector3(horizontal.x, 1, horizontal.y).normalized * settings.Speed;
                emit.startColor = waterTint && Random.value > feel.WaterTintRatio ? feel.SparkleColor : color;
                emit.rotation = Random.Range(0, 360f);
                system.Emit(emit, 1);
            }
        }

        // Xóa offset trước Update của bootstrap: FitCamera vẫn làm việc trên tư thế nền đúng.
        private void Update() { RestoreCamera(); }
        private void LateUpdate()
        {
            if (animator == null || feel == null) { return; }
            Advance(Time.unscaledDeltaTime);
        }
        public void Advance(float seconds)
        {
            RestoreCamera();
            if (animator == null || animator.Paused) { return; }
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].particleCount > 0) { systems[i].Simulate(seconds, false, false, false); }
            }
            shakeTime += seconds;
            if (boardCamera != null && feel.ShakeEnabled && shakeTime < feel.ShakeSeconds)
            {
                float t = shakeTime / feel.ShakeSeconds;
                float phase = t * feel.ShakeCycles * Mathf.PI * 2;
                cameraOffset = Vector3.ClampMagnitude(boardCamera.transform.right * Mathf.Sin(phase) + boardCamera.transform.up * Mathf.Sin(phase * 2), 1) *
                    (feel.ShakeDistance * (1 - t));
                boardCamera.transform.position += cameraOffset;
                appliedCameraPosition = boardCamera.transform.position;
            }
        }

        private void RestoreCamera()
        {
            // FitCamera có thể đã ghi tư thế mới trong Update cùng frame; không trừ offset cũ vào tư thế mới.
            if (boardCamera != null && boardCamera.transform.position == appliedCameraPosition)
            {
                boardCamera.transform.position -= cameraOffset;
            }
            cameraOffset = Vector3.zero;
        }
        public void Clear()
        {
            RestoreCamera();
            shakeTime = float.MaxValue;
            if (systems == null) { return; }
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                {
                    systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    // Reset bộ mô phỏng ở t=0 rồi pause trước khi Emit, kể cả pool vừa tạo trong EditMode.
                    systems[i].Simulate(0, false, true, false);
                }
            }
        }
        private void Disconnect()
        {
            if (animator != null)
            {
                animator.CarEventStarted -= OnCarEvent;
                animator.Cancelled -= Clear;
            }
            Clear();
            animator = null;
        }
        private void OnDisable() { Disconnect(); }
    }
}
