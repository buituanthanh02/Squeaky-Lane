using System;
using System.Collections.Generic;

namespace SqueakyLane.Core.Model
{
    public sealed class MoveResult
    {
        public GameState State { get; }
        public IReadOnlyList<MoveEvent> Events { get; }
        public bool Moved { get; }
        public bool Exited { get; }

        public MoveResult(GameState state, IReadOnlyList<MoveEvent> events, bool moved, bool exited = false)
        { State = state; Events = ReadOnlyData.Freeze(events); Moved = moved; Exited = exited; }

        internal static MoveResult Unchanged(GameState state) =>
            new MoveResult(state, (List<MoveEvent>)null, false);

        internal MoveResult(GameState state, List<MoveEvent> events, bool moved, bool exited = false)
        {
            State = state; Moved = moved; Exited = exited;
            // List được luật tạo riêng và không sửa sau khi công bố; không cần chép mảng lần nữa.
            Events = events == null ? (IReadOnlyList<MoveEvent>)Array.Empty<MoveEvent>() : events.AsReadOnly();
        }
    }
}
