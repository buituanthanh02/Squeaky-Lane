using System;
using System.Diagnostics;
using SqueakyLane.Core.Solver;

namespace SqueakyLane.Game
{
    public enum StuckStatus { Unknown, Winnable, Stuck }

    public readonly struct SearchBudget
    {
        private readonly long deadline;
        private SearchBudget(long deadline) { this.deadline = deadline; }
        public bool Expired => Stopwatch.GetTimestamp() >= deadline;
        public static SearchBudget Start(double milliseconds)
        {
            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(milliseconds));
            return new SearchBudget(Stopwatch.GetTimestamp() + (long)(milliseconds * Stopwatch.Frequency / 1000.0));
        }
    }

    public sealed class StuckDetector : IDisposable
    {
        private readonly GameSession session;
        private readonly int maxStates;
        private WinSearch search;
        private bool started;
        public StuckStatus Status { get; private set; }
        public int Explored => search == null ? 0 : search.Explored;
        public StuckDetector(GameSession session, int maxStates)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (maxStates < 1) throw new ArgumentOutOfRangeException(nameof(maxStates));
            this.maxStates = maxStates; session.Changed += Cancel;
        }
        public StuckStatus Poll(SearchBudget budget)
        {
            if (budget.Expired || Status != StuckStatus.Unknown) return Status;
            if (!started) { search = new WinSearch(session.Level, session.State, maxStates); started = true; }
            while (!search.IsDone && !budget.Expired) search.Step(1);
            if (search.IsDone) Status = search.Win ? StuckStatus.Winnable : search.Exhausted ? StuckStatus.Stuck : StuckStatus.Unknown;
            return Status;
        }
        public void Cancel() { search = null; started = false; Status = StuckStatus.Unknown; }
        public void Dispose() { session.Changed -= Cancel; Cancel(); }
    }

    public sealed class HintProvider : IDisposable
    {
        private readonly GameSession session;
        private readonly LevelTimer timer;
        private readonly int maxStates;
        private PathSearch search;
        public int Remaining { get; private set; }
        public bool IsSearching => search != null && !search.IsDone;
        public bool HasFinished => search != null && search.IsDone;
        public bool HasHint => search != null && search.IsDone && search.Result.Moves != null && search.Result.Moves.Count > 0;
        public HintProvider(GameSession session, LevelTimer timer, int count, int maxStates)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
            if (count < -1 || maxStates < 1) throw new ArgumentOutOfRangeException();
            Remaining = count; this.maxStates = maxStates; session.Changed += Cancel;
        }
        public bool Request()
        {
            if (Remaining == 0 || timer.IsOver || session.IsWin) return false;
            // Tạo search trong Poll để việc khởi tạo cũng nằm trong vùng đo/ngân sách của frame.
            requested = true; return true;
        }
        private bool requested;
        public void Poll(SearchBudget budget)
        {
            if (!requested || budget.Expired) return;
            if (search == null) search = new PathSearch(session.Level, session.State, maxStates);
            while (!search.IsDone && !budget.Expired) search.Step(1);
        }
        public bool TryTake(out SolverMove move)
        {
            move = default;
            if (!HasHint || timer.IsOver) return false;
            move = search.Result.Moves[0]; if (Remaining > 0) Remaining--;
            timer.ChargeHint(); Cancel(); return true;
        }
        public void Cancel() { search = null; requested = false; }
        public void Dispose() { session.Changed -= Cancel; Cancel(); }
    }
}
