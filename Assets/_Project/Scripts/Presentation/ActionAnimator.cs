using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class ActionAnimator : MonoBehaviour
    {
        private BoardView board;
        private FeelConfig feel;
        private Lane[] lanes;
        private Pose[] poses;
        private bool[] affected;
        private readonly List<Clip> draft = new List<Clip>(128);
        private readonly List<PylonClip> pylons = new List<PylonClip>();
        private float clock;
        public bool Paused { get; set; }
        public event System.Action<MoveEventKind, StopReason?> EventStarted;
        public event System.Action CarStarted;
        public event System.Action<CarView, MoveEvent> CarEventStarted;
        public event System.Action Cancelled;
        // Số ô xe vừa trượt trước khi dừng, đặt ngay trước khi bắn CarEventStarted/EventStarted của chính sự kiện đó.
        // Người nghe chỉ đọc trong callback; cách này giữ nguyên chữ ký sự kiện cũ.
        public int LastSlideCells { get; private set; }
        // Xe thứ mấy trong chuỗi ra cổng liên tiếp (1 = xe đầu). Dùng đồng hồ của animator nên Pause không làm chuỗi hết hạn.
        public int ExitCombo { get; private set; }
        private float lastExitClock = float.NegativeInfinity;
        public bool IsBusy
        {
            get { if (lanes == null) return false; for (int i = 0; i < lanes.Length; i++) if (lanes[i].Head < lanes[i].Clips.Count) return true; return pylons.Count > 0; }
        }
        public void Initialize(BoardView view, FeelConfig config, GameState state)
        {
            board = view; feel = config; clock = 0; draft.Clear(); pylons.Clear();
            lanes = new Lane[state.CarCount]; poses = new Pose[state.CarCount]; affected = new bool[state.CarCount];
            for (int i = 0; i < lanes.Length; i++) lanes[i] = new Lane();
            for (int i = 0; i < lanes.Length; i++) { board.Car(i).ConfigureFeel(config); }
            CancelAndSnap(state);
        }
        public void CancelAndSnap(GameState state)
        {
            if (lanes == null) return;
            draft.Clear(); pylons.Clear();
            ExitCombo = 0; LastSlideCells = 0; lastExitClock = float.NegativeInfinity;
            for (int i = 0; i < lanes.Length; i++) { lanes[i].Clips.Clear(); lanes[i].Head = 0; poses[i] = FromState(state.CarAt(i), i); lanes[i].End = clock; board.Car(i).ResetFeel(); }
            board.Snap(state);
            // Xe vừa nhảy về tư thế của Core: bỏ vệt cũ để không kéo vạch từ chỗ cũ tới chỗ mới.
            for (int i = 0; i < lanes.Length; i++) { board.Car(i).ClearTrail(); }
            Cancelled?.Invoke();
        }
        public void Enqueue(SolverMove command, GameState before, MoveResult result)
        {
            draft.Clear(); System.Array.Clear(affected, 0, affected.Length);
            for (int i = 0; i < poses.Length; i++) poses[i] = FromState(before.CarAt(i), i);
            if (command.Kind == SolverMoveKind.RotatePylon)
            {
                float begin = clock;
                for (int i = 0; i < pylons.Count; i++) if (pylons[i].Index == command.Index) begin = Mathf.Max(begin, pylons[i].End);
                pylons.Add(new PylonClip { Index = command.Index, Start = begin, End = begin + feel.RotateSeconds,
                    From = before.PylonTurnsAt(command.Index) * -90, To = before.PylonTurnsAt(command.Index) * -90 - 90 });
                return;
            }
            int selected = command.Index; float cursor = 0;
            int slid = 0; // số ô xe đã trượt trước sự kiện hiện tại; Stop dùng để biết va nhẹ hay mạnh
            var events = result.Events;
            for (int e = 0; e < events.Count; e++)
            {
                int firstClip = draft.Count;
                var ev = events[e]; var pose = poses[selected]; var to = pose;
                switch (ev.Kind)
                {
                    case MoveEventKind.Step:
                    {
                        // 014: bứt tốc: ô đầu chậm, nhanh dần tới tốc độ tối đa sau AccelCells ô (FeelConfig.TimeToCell).
                        float step = feel.StepDuration(slid);
                        to.Position = Position(ev.R, ev.C, pose.Yaw, selected);
                        Add(selected, cursor, step, to, true); cursor += step; slid++; break;
                    }
                    case MoveEventKind.Water:
                        to.Position += Direction(pose.Yaw) * (ev.Sign * feel.WaterDipCells); to.Mud = 0;
                        Add(selected, cursor, feel.ExitSeconds * .5f, to, true); cursor += feel.ExitSeconds * .5f;
                        pose.Mud = 0; Add(selected, cursor, feel.ExitSeconds * .5f, pose, true); cursor += feel.ExitSeconds * .5f; break;
                    case MoveEventKind.Exit:
                    {
                        to.Position += Direction(pose.Yaw) * (ev.Sign * (board.Car(selected).Length + 1)); to.Scale = 0; to.Alive = false;
                        Add(selected, cursor, feel.GatePassSeconds, to, true); cursor += feel.GatePassSeconds;
                        // Xe lướt xuyên cổng nhanh dần và giữ nguyên cỡ; chỉ thu nhỏ ở phần đuôi khi đã ra ngoài.
                        var exitClip = draft[draft.Count - 1];
                        exitClip.Accelerate = true; exitClip.ShrinkStart = 1 - feel.GateTailRatio;
                        draft[draft.Count - 1] = exitClip;
                        break;
                    }
                    case MoveEventKind.Stop:
                        // Va mạnh hơn khi xe đã trượt dài trước khi dừng.
                        to.Position += Direction(pose.Yaw) * (ev.Sign * feel.BumpDistance * feel.ImpactScale(slid));
                        Add(selected, cursor, feel.BumpSeconds * .5f, to); cursor += feel.BumpSeconds * .5f;
                        Add(selected, cursor, feel.BumpSeconds * .5f, pose); cursor += feel.BumpSeconds * .5f; break;
                    case MoveEventKind.Paint: case MoveEventKind.Mud: case MoveEventKind.Spray:
                        Effect(selected, ev, cursor); break;
                    case MoveEventKind.Splash:
                        if (ev.OtherCar >= 0) { var splash = poses[ev.OtherCar]; splash.Mud = 1; Add(ev.OtherCar, cursor, 0, splash); } break;
                    case MoveEventKind.Push:
                    {
                        // Hàng bị đẩy đi cùng nhịp với bước kế tiếp của xe tải (cùng chỉ số slid).
                        float pushStep = feel.StepDuration(slid);
                        for (int k = 0; k < ev.PushedCars.Count; k++)
                        {
                            var push = ev.PushedCars[k]; var pushed = poses[push.Car];
                            pushed.Position = Position(push.R, push.C, pushed.Yaw, push.Car); Add(push.Car, cursor, pushStep, pushed, true);
                            for (int fx = 0; fx < push.Effects.Count; fx++) Effect(push.Car, push.Effects[fx], cursor + pushStep);
                        }
                    }
                        // Step tiếp theo của xe tải dùng cùng cursor, để cả hàng chuyển động đồng thời.
                        break;
                    case MoveEventKind.Tele:
                        to.Scale = 0; Add(selected, cursor, feel.TeleSeconds * .5f, to); cursor += feel.TeleSeconds * .5f;
                        to.Yaw = BoardView.Yaw(ev.Axis.Value); to.Position = Position(ev.R, ev.C, to.Yaw, selected);
                        Add(selected, cursor, 0, to); to.Scale = 1; Add(selected, cursor, feel.TeleSeconds * .5f, to); cursor += feel.TeleSeconds * .5f; break;
                    case MoveEventKind.Rotate:
                        to = FromState(result.State.CarAt(selected), selected);
                        // Luật chỉ kiểm ô đích, không kiểm thân 3D quét lúc xoay: thu nhỏ giữa chừng để không quệt xe cạnh bàn.
                        var middle = pose; middle.Position = Vector3.Lerp(pose.Position, to.Position, .5f);
                        middle.Yaw = pose.Yaw + 45; middle.Scale = feel.RotateMidScale;
                        Add(selected, cursor, feel.RotateSeconds * .5f, middle); cursor += feel.RotateSeconds * .5f;
                        to.Yaw = pose.Yaw + 90; Add(selected, cursor, feel.RotateSeconds * .5f, to); cursor += feel.RotateSeconds * .5f; break;
                }
                if (draft.Count > firstClip)
                { var clip = draft[firstClip]; clip.HasEvent = true; clip.EventKind = ev.Kind; clip.Reason = ev.Reason; clip.EventData = ev; clip.Slide = slid; draft[firstClip] = clip; }
            }
            if (command.Kind == SolverMoveKind.MoveCar && result.Moved)
                for (int i = 0; i < draft.Count; i++) if (draft[i].Car == selected) { var clip = draft[i]; clip.StartsMove = true; clip.StartSign = command.Sign; draft[i] = clip; break; }
            // Tư thế cuối lấy từ Core; không tự suy luật dựa vào vị trí đang render.
            for (int i = 0; i < affected.Length; i++) if (affected[i]) Add(i, cursor, 0, FromState(result.State.CarAt(i), i));
            float start = clock;
            for (int i = 0; i < affected.Length; i++) if (affected[i]) start = Mathf.Max(start, lanes[i].End);
            // Đặt chỗ theo bao quét XZ và thời gian. Bảo thủ: đường giao nhau chờ; đường riêng chạy song song.
            // Chỉ dịch lịch về sau, mỗi xung đột làm vượt ít nhất một reservation cũ nên vòng lặp hữu hạn.
            bool changed;
            do
            {
                changed = false;
                for (int d = 0; d < draft.Count; d++)
                {
                    var next = draft[d]; if (next.Duration <= 0) continue;
                    for (int other = 0; other < lanes.Length; other++)
                    {
                        if (affected[other]) continue;
                        var lane = lanes[other]; float previousEnd = clock;
                        for (int k = lane.Head; k < lane.Clips.Count; k++)
                        {
                            var reserved = lane.Clips[k];
                            // Giữ chỗ cả khi xe đứng đợi giữa các đoạn, không chỉ lúc đang trượt.
                            if (start + next.Start < reserved.Start && start + next.End > previousEnd && next.Swept.Overlaps(Bounds(reserved.From, other)))
                            { start = reserved.Start - next.Start + .0001f; changed = true; }
                            previousEnd = Mathf.Max(previousEnd, reserved.End);
                            if (reserved.Duration <= 0 || start + next.Start >= reserved.End || start + next.End <= reserved.Start) continue;
                            if (!next.Swept.Overlaps(reserved.Swept)) continue;
                            start = reserved.End - next.Start + .0001f; changed = true;
                        }
                    }
                }
            } while (changed);
            for (int i = 0; i < draft.Count; i++)
            {
                var clip = draft[i]; clip.Start += start; lanes[clip.Car].Clips.Add(clip);
                lanes[clip.Car].End = Mathf.Max(lanes[clip.Car].End, clip.End);
            }
        }
        private void Effect(int car, MoveEvent ev, float time)
        {
            var to = poses[car];
            if (ev.Kind == MoveEventKind.Paint) to.Color = ev.Color.Value;
            if (ev.Kind == MoveEventKind.Mud) to.Mud = 1;
            if (ev.Kind == MoveEventKind.Spray) to.Mud = 0;
            Add(car, time, 0, to);
            var effect = draft[draft.Count - 1]; effect.HasEvent = true; effect.EventKind = ev.Kind; effect.EventData = ev; draft[draft.Count - 1] = effect;
            if (ev.Kind == MoveEventKind.Mud)
            { var clip = draft[draft.Count - 1]; clip.ConsumedMud = ev.MudIndex; draft[draft.Count - 1] = clip; }
        }
        private void Add(int car, float start, float duration, Pose to, bool rolling = false)
        {
            var from = poses[car];
            // Xe đang bị push/splash cũng được gom vào cùng lệnh; không tự chạy trước xe tải.
            var swept = Bounds(from, car); var end = Bounds(to, car);
            swept = Rect.MinMaxRect(Mathf.Min(swept.xMin, end.xMin), Mathf.Min(swept.yMin, end.yMin), Mathf.Max(swept.xMax, end.xMax), Mathf.Max(swept.yMax, end.yMax));
            draft.Add(new Clip { Car = car, Start = start, Duration = duration, From = from, To = to, Swept = swept, ConsumedMud = -1,
                RollDistance = rolling ? Vector3.Dot(to.Position - from.Position, Direction(from.Yaw)) : 0 });
            poses[car] = to; affected[car] = true;
        }
        private Rect Bounds(Pose pose, int car)
        {
            bool horizontal = Mathf.Abs(Mathf.Cos(pose.Yaw * Mathf.Deg2Rad)) > .5f;
            float x = horizontal ? board.Car(car).Length - .2f : .78f, z = horizontal ? .78f : board.Car(car).Length - .2f;
            x *= pose.Scale; z *= pose.Scale;
            return new Rect(pose.Position.x - x * .5f, pose.Position.z - z * .5f, x, z);
        }
        private Pose FromState(CarState state, int i) => new Pose { Position = board.Position(state, board.Car(i).Length), Yaw = BoardView.Yaw(state.Axis), Color = state.Color, Mud = state.Mud, Alive = state.Alive, Scale = state.Alive ? 1 : 0 };
        private Vector3 Position(int r, int c, float yaw, int i)
        {
            bool horizontal = Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad)) > .5f;
            return board.Cell(r + (horizontal ? 0 : (board.Car(i).Length - 1) * .5f), c + (horizontal ? (board.Car(i).Length - 1) * .5f : 0));
        }
        private static Vector3 Direction(float yaw) => Quaternion.Euler(0, yaw, 0) * Vector3.right;
        private void Update() => Advance(Time.unscaledDeltaTime);
        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new System.ArgumentOutOfRangeException(nameof(seconds));
            if (lanes == null || Paused) return;
            clock += seconds;
            for (int i = 0; i < lanes.Length; i++)
            {
                var lane = lanes[i];
                float drive = 0;
                while (lane.Head < lane.Clips.Count)
                {
                    var clip = lane.Clips[lane.Head]; if (clock < clip.Start) break;
                    if (!clip.Started)
                    {
                        clip.Started = true; lane.Clips[lane.Head] = clip;
                        if (clip.StartsMove)
                        {
                            board.Car(i).StartMotion(clip.StartSign);
                            CarStarted?.Invoke();
                        }
                        if (clip.HasEvent)
                        {
                            LastSlideCells = clip.Slide;
                            if (clip.EventData.Kind == MoveEventKind.Exit)
                            {
                                ExitCombo = clock - lastExitClock <= feel.ComboWindowSeconds ? ExitCombo + 1 : 1;
                                lastExitClock = clock;
                            }
                            board.Car(i).PlayFeel(clip.EventData, clip.Slide);
                            if (clip.EventData.Kind == MoveEventKind.Stop && clip.EventData.OtherCar >= 0)
                            {
                                var direction = board.transform.TransformDirection(Direction(clip.From.Yaw) * clip.EventData.Sign);
                                board.Car(clip.EventData.OtherCar).Hit(direction);
                            }
                            CarEventStarted?.Invoke(board.Car(i), clip.EventData);
                            EventStarted?.Invoke(clip.EventKind, clip.Reason);
                        }
                    }
                    float t = clip.Duration <= 0 ? 1 : Mathf.Clamp01((clock - clip.Start) / clip.Duration);
                    var from = clip.From; var to = clip.To;
                    // Ra cổng: vị trí theo t² (nhanh dần), cỡ chỉ đổi ở phần đuôi; các đoạn khác tuyến tính như cũ.
                    float travel = clip.Accelerate ? t * t : t;
                    float shrink = clip.ShrinkStart > 0 ? Mathf.Clamp01((t - clip.ShrinkStart) / Mathf.Max(.0001f, 1 - clip.ShrinkStart)) : t;
                    board.Car(i).Show(Vector3.Lerp(from.Position, to.Position, travel), Mathf.LerpAngle(from.Yaw, to.Yaw, t),
                        t >= 1 ? to.Color : from.Color, t >= 1 ? to.Mud : from.Mud, t >= 1 ? to.Alive : from.Alive, Mathf.Lerp(from.Scale, to.Scale, shrink));
                    board.Car(i).Roll(clip.RollDistance * (travel - clip.RollProgress));
                    clip.RollProgress = travel;
                    lane.Clips[lane.Head] = clip;
                    // Đoạn tức thời đổi chỗ (portal): bỏ vệt để không kéo vạch xuyên bãi.
                    if (clip.Duration <= 0 && (clip.To.Position - clip.From.Position).sqrMagnitude > .0001f) { board.Car(i).ClearTrail(); }
                    if (t < 1)
                    {
                        // Tốc độ đang chạy, chuẩn hóa theo tốc độ tối đa 1/StepSeconds; có dấu theo chiều thân xe.
                        if (clip.Duration > 0) { drive = clip.RollDistance / clip.Duration * feel.StepSeconds; }
                        break;
                    }
                    if (clip.ConsumedMud >= 0) board.ConsumeMud(clip.ConsumedMud);
                    lane.Head++;
                }
                if (lane.Head == lane.Clips.Count && lane.Head > 0) { lane.Head = 0; lane.Clips.Clear(); }
                board.Car(i).Drive(drive);
            }
            for (int i = 0; i < lanes.Length; i++)
            {
                if (board.Car(i).gameObject.activeSelf) { board.Car(i).AdvanceFeel(seconds); }
            }
            for (int i = 0; i < pylons.Count;)
            {
                var p = pylons[i]; if (clock < p.Start) { i++; continue; }
                if (!p.Started) { p.Started = true; pylons[i] = p; EventStarted?.Invoke(MoveEventKind.Pyl, null); }
                float t = Mathf.Clamp01((clock - p.Start) / (p.End - p.Start)); board.SetPylon(p.Index, Mathf.Lerp(p.From, p.To, t));
                if (t >= 1) pylons.RemoveAt(i); else i++;
            }
        }
        private void OnDisable()
        {
            if (lanes == null) { return; }
            for (int i = 0; i < lanes.Length; i++)
            {
                lanes[i].Clips.Clear(); lanes[i].Head = 0; lanes[i].End = clock;
                if (board != null && board.Car(i) != null) { board.Car(i).ResetFeel(); }
            }
            pylons.Clear();
            Cancelled?.Invoke();
        }
        private sealed class Lane { public readonly List<Clip> Clips = new List<Clip>(64); public int Head; public float End; }
        private struct Pose { public Vector3 Position; public float Yaw, Scale; public TileColor Color; public int Mud; public bool Alive; }
        private struct Clip { public int Car, ConsumedMud, StartSign, Slide; public bool Accelerate; public float ShrinkStart; public float Start, Duration, RollDistance, RollProgress; public float End => Start + Duration; public Pose From, To; public Rect Swept; public bool Started, HasEvent, StartsMove; public MoveEventKind EventKind; public StopReason? Reason; public MoveEvent EventData; }
        private struct PylonClip { public int Index; public float Start, End, From, To; public bool Started; }
    }
}
