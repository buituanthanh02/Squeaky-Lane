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
        private float shakeScale = 1;
        // Chữ nổi "Sạch bóng!": pool nhỏ tạo một lần, tái dùng; không Instantiate theo sự kiện.
        private const int PopupPool = 4;
        private static readonly string[] Praise = { "Sạch bóng!", "Bóng loáng!", "Thơm tho!" };
        private static readonly string[] ComboText = { "", "", "Liên tiếp x2!", "Liên tiếp x3!", "Liên tiếp x4!", "Liên tiếp x5!", "Liên tiếp x6!", "Liên tiếp x7!", "Liên tiếp x8!" };
        private Popup[] popups;
        private Font popupFont;
        private int praiseIndex;
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
            else if (ev.Kind == MoveEventKind.Exit)
            {
                if (feel.GateParticlesEnabled)
                {
                    Emit(bubbles, feel.Bubbles, position, palette.Car(car.Color), feel.Bubbles.Count);
                    Emit(sparkles, feel.Sparkles, position, feel.SparkleColor, feel.Sparkles.Count);
                }
                if (feel.PopupTextEnabled) { ShowExitText(direction, position); }
            }
            else if (ev.Kind == MoveEventKind.Step && feel.MudDripsEnabled && car.HasMud)
            {
                // 014 bùn: xe bẩn nhỏ giọt bùn sau đuôi mỗi ô chạy qua (dùng chung pool hạt và trần MaxLiveParticles).
                var tail = car.transform.position - direction * (car.Length * .4f) + Vector3.up * .3f;
                Emit(smoke, feel.MudDrip, tail, palette.Mud, feel.MudDrip.Count);
            }
            else if (ev.Kind == MoveEventKind.Mud && feel.MudDripsEnabled)
            {
                // Lội vào vũng bùn: bùn bắn quanh thân.
                Emit(smoke, feel.MudSplash, car.transform.position + Vector3.up * .15f, palette.Mud, feel.MudSplash.Count);
            }
            else if (ev.Kind == MoveEventKind.Stop && IsImpact(ev.Reason))
            {
                // Va mạnh theo số ô xe vừa trượt; bị chặn ngay hoặc trượt 1 ô là mức nhẹ nhất.
                int slide = animator.LastSlideCells;
                float scale = feel.ImpactScale(slide);
                if (feel.ImpactParticlesEnabled)
                {
                    Emit(smoke, feel.Impact, position, feel.ImpactColor, Mathf.RoundToInt(feel.Impact.Count * scale));
                    int sparks = Mathf.RoundToInt(Mathf.Lerp(feel.ImpactSparksMin, feel.ImpactSparksMax, feel.ImpactPower(slide)));
                    Emit(sparkles, feel.Sparkles, position, feel.SparkleColor, sparks);
                }
                if (feel.ShakeEnabled && slide >= feel.ShakeMinCells)
                {
                    shakeTime = 0;
                    shakeScale = scale;
                }
            }
        }

        // Chữ phẳng trên mặt bãi (cùng kiểu nhãn 1A/2B), đặt lùi vào trong bãi từ cổng và kẹp trong mép bãi.
        private void ShowExitText(Vector3 exitDirection, Vector3 gatePoint)
        {
            if (popups == null || board == null || board.Level == null) { return; }
            Vector3 local = board.transform.InverseTransformPoint(gatePoint - exitDirection * .9f);
            float limitX = Mathf.Max(0, board.Level.Cols * .5f - 1.1f);
            float limitZ = Mathf.Max(0, board.Level.Rows * .5f - .6f);
            local.x = Mathf.Clamp(local.x, -limitX, limitX);
            local.z = Mathf.Clamp(local.z, -limitZ, limitZ);
            local.y = 1.1f;
            Pop(Praise[praiseIndex], local, Color.white);
            praiseIndex = (praiseIndex + 1) % Praise.Length;
            int combo = animator.ExitCombo;
            if (combo >= 2)
            {
                local.z = Mathf.Min(local.z + .55f, limitZ + .55f);
                Pop(ComboText[Mathf.Min(combo, ComboText.Length - 1)], local, new Color32(255, 229, 138, 255));
            }
        }

        private void Pop(string text, Vector3 localStart, Color color)
        {
            Popup chosen = null;
            for (int i = 0; i < popups.Length; i++)
            {
                if (!popups[i].Active) { chosen = popups[i]; break; }
                if (chosen == null || popups[i].Age > chosen.Age) { chosen = popups[i]; }
            }
            chosen.Active = true;
            chosen.Age = 0;
            chosen.Start = localStart;
            chosen.Color = color;
            chosen.Mesh.text = text;
            chosen.Mesh.color = color;
            chosen.Node.localPosition = localStart;
            chosen.Node.gameObject.SetActive(true);
        }

        private void AdvancePopups(float seconds)
        {
            if (popups == null) { return; }
            for (int i = 0; i < popups.Length; i++)
            {
                var popup = popups[i];
                if (!popup.Active) { continue; }
                popup.Age += seconds;
                float t = popup.Age / feel.PopupSeconds;
                if (t >= 1) { HidePopup(popup); continue; }
                float rise = 1 - (1 - t) * (1 - t);
                popup.Node.localPosition = popup.Start + new Vector3(0, 0, feel.PopupRise * rise);
                var color = popup.Color;
                color.a = t < .6f ? 1 : 1 - (t - .6f) / .4f;
                popup.Mesh.color = color;
            }
        }

        private static void HidePopup(Popup popup)
        {
            popup.Active = false;
            popup.Node.gameObject.SetActive(false);
        }

        // Tạo pool một lần theo font động của bãi và nạp sẵn ký tự có dấu để lần ra cổng đầu tiên không bị khựng.
        // Gọi mỗi lần nạp màn (qua Clear); sau lần đầu chỉ kiểm tra rồi thoát.
        private void EnsureText()
        {
            if (board == null || board.LabelFont == null || feel == null) { return; }
            var font = board.LabelFont;
            if (popups != null && popupFont == font) { return; }
            popupFont = font;
            font.RequestCharactersInTexture("Sạch bóng! Bóng loáng! Thơm tho! Liên tiếp x0123456789", 36, FontStyle.Normal);
            if (popups == null)
            {
                popups = new Popup[PopupPool];
                for (int i = 0; i < popups.Length; i++)
                {
                    var node = new GameObject("Popup " + i);
                    node.transform.SetParent(board.transform, false);
                    node.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var mesh = node.AddComponent<TextMesh>();
                    mesh.fontSize = 36;
                    mesh.anchor = TextAnchor.MiddleCenter;
                    node.SetActive(false);
                    popups[i] = new Popup { Node = node.transform, Mesh = mesh, Renderer = node.GetComponent<Renderer>() };
                }
            }
            for (int i = 0; i < popups.Length; i++)
            {
                popups[i].Mesh.font = font;
                popups[i].Mesh.characterSize = feel.PopupTextSize;
                popups[i].Renderer.sharedMaterial = font.material;
            }
        }

        private sealed class Popup
        {
            public Transform Node;
            public TextMesh Mesh;
            public Renderer Renderer;
            public Vector3 Start;
            public Color Color;
            public float Age;
            public bool Active;
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
            AdvancePopups(seconds);
            shakeTime += seconds;
            if (boardCamera != null && feel.ShakeEnabled && shakeTime < feel.ShakeSeconds)
            {
                float t = shakeTime / feel.ShakeSeconds;
                float phase = t * feel.ShakeCycles * Mathf.PI * 2;
                cameraOffset = Vector3.ClampMagnitude(boardCamera.transform.right * Mathf.Sin(phase) + boardCamera.transform.up * Mathf.Sin(phase * 2), 1) *
                    (feel.ShakeDistance * shakeScale * (1 - t));
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
            shakeScale = 1;
            if (popups != null) { for (int i = 0; i < popups.Length; i++) { HidePopup(popups[i]); } }
            EnsureText();
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
