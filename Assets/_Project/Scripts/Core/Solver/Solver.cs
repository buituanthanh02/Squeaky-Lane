using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;

namespace SqueakyLane.Core.Solver
{
    public enum SolverMoveKind { MoveCar = 0, RotateTable = 1, RotatePylon = 2 }

    public readonly struct SolverMove
    {
        public SolverMoveKind Kind { get; }
        public int Index { get; }
        public int Sign { get; }
        public SolverMove(SolverMoveKind kind, int index, int sign = 0)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (kind < SolverMoveKind.MoveCar || kind > SolverMoveKind.RotatePylon) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == SolverMoveKind.MoveCar ? (sign != -1 && sign != 1) : sign != 0)
                throw new ArgumentOutOfRangeException(nameof(sign));
            Kind = kind; Index = index; Sign = sign;
        }
        public MoveResult Apply(LevelDef level, GameState state, bool recordEvents = true)
        {
            switch (Kind)
            {
                case SolverMoveKind.MoveCar: return MoveResolver.Move(level, state, Index, Sign, recordEvents);
                case SolverMoveKind.RotateTable: return TableRule.Rotate(level, state, Index, recordEvents);
                case SolverMoveKind.RotatePylon: return PylonRule.RotatePylon(level, state, Index, recordEvents);
                default: throw new ArgumentOutOfRangeException(nameof(Kind));
            }
        }
    }

    public sealed class SolveResult
    {
        // Moves=null: không tìm được trong lần chạy này. Phải đọc Exhausted để phân biệt kẹt/hết ngân sách.
        public IReadOnlyList<SolverMove> Moves { get; }
        public bool Exhausted { get; }
        public int Explored { get; }
        public int Expanded { get; }
        internal SolveResult(IReadOnlyList<SolverMove> moves, bool exhausted, int explored, int expanded)
        { Moves = moves; Exhausted = exhausted; Explored = explored; Expanded = expanded; }
    }

    public static class Solver
    {
        public static SolveResult Solve(LevelDef level, GameState state, int maxStates)
        {
            // BFS chặn luồng gọi: dùng kiểm màn/offline; runtime tìm gợi ý/kẹt dùng StepSearch.
            var search = new SearchEngine(level, state, maxStates, true, true);
            while (!search.Step(int.MaxValue)) { }
            return search.Result;
        }
    }
}
