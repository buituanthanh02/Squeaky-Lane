using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class MoveResolver
    {
        private const int MaxIterations = 80; // Giữ hàng rào hữu hạn giống logic.js.

        public static MoveResult Move(LevelDef level, GameState state, int car, int sign, bool recordEvents = true)
        {
            if (sign != -1 && sign != 1) throw new ArgumentOutOfRangeException(nameof(sign));
            if (!state.CarAt(car).Alive) return MoveResult.Unchanged(state);
            var definition = level.Cars[car];
            var next = state.Copy();
            // Cổng nước đảo hướng nhưng bản đồ xe cản giữ từ đầu lệnh như prototype.
            var map = Occupancy.Build(level, state, car);
            var events = recordEvents ? new List<MoveEvent>() : null;
            int dir = sign, steps = 0;
            bool bounced = false, exited = false;

            for (int guard = 0; guard < MaxIterations; guard++)
            {
                var moving = next.CarAt(car);
                int nr = moving.Axis == Axis.Horizontal ? moving.R : dir > 0 ? moving.R + definition.Len : moving.R - 1;
                int nc = moving.Axis == Axis.Vertical ? moving.C : dir > 0 ? moving.C + definition.Len : moving.C - 1;
                if (!level.Contains(nr, nc))
                {
                    var edge = GateRule.EdgeOf(moving.Axis, dir);
                    var gate = level.GateAt(edge, moving.Axis == Axis.Horizontal ? moving.R : moving.C);
                    if (GateRule.CanBounce(gate, bounced))
                    {
                        events?.Add(new MoveEvent(MoveEventKind.Water, sign: dir, gate: gate, hadMud: moving.Mud));
                        next.SetCar(car, moving.WithMud(0));
                        bounced = true; dir = -dir;
                        continue;
                    }
                    if (GateRule.CanExit(gate, moving))
                    {
                        events?.Add(new MoveEvent(MoveEventKind.Exit, sign: dir, gate: gate));
                        next.SetCar(car, moving.WithAlive(false)); exited = true;
                        break;
                    }
                    events?.Add(new MoveEvent(MoveEventKind.Stop, sign: dir, gate: gate, reason: GateRule.StopAt(gate, moving)));
                    break;
                }

                int obstacle = map[nr * level.Cols + nc];
                bool pushedNow = false;
                if (obstacle >= 0)
                {
                    if (definition.Truck && PushRule.TryChain(level, next, car, dir, obstacle, out var chain, out int count))
                    {
                        PushRule.Apply(level, next, car, dir, chain, count, events);
                        // Khi đẩy thành công, splash không cần steps > 0 (khác đâm thường).
                        Splash(level, next, car, obstacle, dir, events);
                        pushedNow = true;
                    }
                    else
                    {
                        if (steps > 0) Splash(level, next, car, obstacle, dir, events);
                        events?.Add(new MoveEvent(MoveEventKind.Stop, sign: dir, reason: StopReason.Car, otherCar: obstacle));
                        break;
                    }
                }
                if (obstacle == Occupancy.Obstacle)
                {
                    events?.Add(new MoveEvent(MoveEventKind.Stop, sign: dir, reason: StopReason.Wall));
                    break;
                }

                next.SetCar(car, moving.At(moving.R + (moving.Axis == Axis.Vertical ? dir : 0),
                    moving.C + (moving.Axis == Axis.Horizontal ? dir : 0)));
                steps++;
                var position = next.CarAt(car);
                events?.Add(new MoveEvent(MoveEventKind.Step, r: position.R, c: position.C, sign: dir));
                TileEffects.Apply(level, next, car, nr, nc, events);
                if (pushedNow)
                {
                    events?.Add(new MoveEvent(MoveEventKind.Stop, sign: dir, reason: StopReason.Push));
                    break;
                }
            }

            bool moved = steps > 0 || bounced || exited;
            if (!moved) return new MoveResult(state, events, false);
            next.AdvanceMove();
            return new MoveResult(next, events, true, exited);
        }

        private static void Splash(LevelDef level, GameState state, int car, int target, int sign, List<MoveEvent> events)
        {
            if (!level.Splash || state.CarAt(car).Mud <= 0 || state.CarAt(target).Mud != 0) return;
            state.SetCar(target, state.CarAt(target).WithMud(1));
            events?.Add(new MoveEvent(MoveEventKind.Splash, sign: sign, otherCar: target));
        }
    }
}
