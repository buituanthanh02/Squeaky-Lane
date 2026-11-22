using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;

namespace SqueakyLane.Core.Solver
{
    public sealed class WinSearch
    {
        private readonly SearchEngine search;
        public WinSearch(LevelDef level, GameState state, int maxStates) { search = new SearchEngine(level, state, maxStates, false, false); }
        public bool IsDone => search.IsDone;
        public bool Win => search.Win;
        public bool Exhausted => search.Exhausted;
        public int Explored => search.Explored;
        public int Expanded => search.Expanded;
        public bool Step(int budget) => search.Step(budget);
    }

    public sealed class PathSearch
    {
        private readonly SearchEngine search;
        public PathSearch(LevelDef level, GameState state, int maxStates) { search = new SearchEngine(level, state, maxStates, false, true); }
        public bool IsDone => search.IsDone;
        public int Explored => search.Explored;
        public int Expanded => search.Expanded;
        // Result=null khi đang chạy, khác với Result.Moves=null khi đã kết thúc nhưng chưa tìm thấy đường.
        public SolveResult Result => search.Result;
        public bool Step(int budget) => search.Step(budget);
    }

    internal sealed class SearchEngine
    {
        private readonly LevelDef level;
        private readonly StateKeyEncoder encoder;
        private readonly int maxStates;
        private readonly bool shortest, keepPath;
        private HashSet<StateKey> seen;
        private List<Node> nodes;
        private Queue<int> queue;
        private Stack<int>[] buckets;
        private int lowestBucket, pending;
        public bool IsDone { get; private set; }
        public bool Win { get; private set; }
        public bool Exhausted { get; private set; }
        public int Explored { get; private set; }
        public int Expanded { get; private set; }
        public SolveResult Result { get; private set; }

        internal SearchEngine(LevelDef level, GameState initial, int maxStates, bool shortest, bool keepPath)
        {
            this.level = level ?? throw new ArgumentNullException(nameof(level));
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (maxStates < 1) throw new ArgumentOutOfRangeException(nameof(maxStates));
            this.maxStates = maxStates; this.shortest = shortest; this.keepPath = keepPath;
            encoder = new StateKeyEncoder(level);
            var key = encoder.Encode(initial);
            if (GameRules.IsWin(initial))
            {
                IsDone = Win = Exhausted = true;
                Result = new SolveResult(keepPath ? Array.Empty<SolverMove>() : null, true, 0, 0);
                return;
            }
            // Không cấp phát toàn bộ maxStates ngay khi chỉ cần tìm một gợi ý nhỏ.
            int capacity = Math.Min(maxStates, shortest ? 1024 : 64);
            seen = new HashSet<StateKey>(capacity, StateKeyComparer.Instance); seen.Add(key);
            nodes = new List<Node>(capacity);
            if (shortest) queue = new Queue<int>(capacity);
            else { buckets = new Stack<int>[initial.CarCount + 1]; lowestBucket = initial.CarCount; }
            int alive = initial.CarCount - GameRules.WashedCount(initial);
            nodes.Add(new Node(initial, -1, default, alive)); Push(0, alive);
        }

        internal bool Step(int budget)
        {
            if (budget < 1) throw new ArgumentOutOfRangeException(nameof(budget));
            if (IsDone) return true;
            // Budget là số node, không phải ms; một node vẫn phải xét nhiều lệnh của Core.
            for (int expanded = 0; expanded < budget && pending > 0; expanded++)
            {
                int index = Pop(); var node = nodes[index];
                if (GameRules.IsWin(node.State)) { Finish(index, true); return true; }
                Expanded++;
                var cursor = new MoveCursor(level, node.State);
                while (cursor.TryNext(out var move, out var result))
                {
                    var key = encoder.Encode(result.State);
                    if (!seen.Add(key)) continue;
                    if (Explored >= maxStates) { Finish(-1, false); return true; }
                    Explored++;
                    int alive = node.Alive - (result.Exited ? 1 : 0);
                    nodes.Add(new Node(result.State, index, move, alive));
                    Push(nodes.Count - 1, alive);
                }
                // Parent chỉ cần index/lệnh; không giữ mảng GameState của node đã mở rộng.
                node.State = null; nodes[index] = node;
            }
            if (pending == 0) Finish(-1, true);
            return IsDone;
        }

        private void Push(int index, int alive)
        {
            if (shortest) queue.Enqueue(index);
            else
            {
                if (buckets[alive] == null) buckets[alive] = new Stack<int>();
                buckets[alive].Push(index);
                if (alive < lowestBucket) lowestBucket = alive;
            }
            pending++;
        }
        private int Pop()
        {
            pending--;
            if (shortest) return queue.Dequeue();
            while (buckets[lowestBucket] == null || buckets[lowestBucket].Count == 0) lowestBucket++;
            return buckets[lowestBucket].Pop();
        }
        private void Finish(int winner, bool exhausted)
        {
            Win = winner >= 0; Exhausted = exhausted; IsDone = true;
            IReadOnlyList<SolverMove> path = null;
            if (Win && keepPath)
            {
                int count = 0;
                for (int i = winner; nodes[i].Parent >= 0; i = nodes[i].Parent) count++;
                var moves = new SolverMove[count];
                for (int i = winner; nodes[i].Parent >= 0; i = nodes[i].Parent) moves[--count] = nodes[i].Move;
                path = Array.AsReadOnly(moves);
            }
            Result = new SolveResult(path, exhausted, Explored, Expanded);
            // Caller có thể giữ Result sau khi xong mà không giữ cả đồ thị tìm kiếm.
            seen = null; nodes = null; queue = null; buckets = null; pending = 0;
        }

        private struct Node
        {
            internal GameState State;
            internal readonly int Parent, Alive;
            internal readonly SolverMove Move;
            internal Node(GameState state, int parent, SolverMove move, int alive)
            { State = state; Parent = parent; Move = move; Alive = alive; }
        }

        private struct MoveCursor
        {
            private readonly LevelDef level;
            private readonly GameState state;
            private int car, action, pylon;
            internal MoveCursor(LevelDef level, GameState state)
            { this.level = level; this.state = state; car = action = pylon = 0; }
            internal bool TryNext(out SolverMove move, out MoveResult result)
            {
                // Giữ thứ tự nextMoves: mỗi xe +, -, bàn xoay; sau đó các trụ không fixed.
                while (car < state.CarCount)
                {
                    if (!state.CarAt(car).Alive || action >= 3) { car++; action = 0; continue; }
                    int current = action++;
                    if (current == 2 && level.Tables.Count == 0) continue;
                    move = current == 2 ? new SolverMove(SolverMoveKind.RotateTable, car) :
                        new SolverMove(SolverMoveKind.MoveCar, car, current == 0 ? 1 : -1);
                    result = move.Apply(level, state, false);
                    if (result.Moved) return true;
                }
                while (pylon < state.PylonCount)
                {
                    int index = pylon++;
                    if (level.Pylons[index].Fixed) continue;
                    move = new SolverMove(SolverMoveKind.RotatePylon, index);
                    result = move.Apply(level, state, false);
                    if (result.Moved) return true;
                }
                move = default; result = null; return false;
            }
        }
    }
}
