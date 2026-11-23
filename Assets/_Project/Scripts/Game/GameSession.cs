using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Core.Solver;

namespace SqueakyLane.Game
{
    public sealed class GameSession
    {
        private readonly Stack<GameState> history = new Stack<GameState>();
        public LevelDef Level { get; }
        public GameState State { get; private set; }
        public int UndoCount => history.Count;
        public bool IsWin => GameRules.IsWin(State);
        public event Action Changed;

        public GameSession(LevelDef level)
        { Level = level ?? throw new ArgumentNullException(nameof(level)); State = GameState.Initial(level); }

        public MoveResult Apply(SolverMove command)
        {
            var result = command.Apply(Level, State);
            if (result.Moved)
            {
                history.Push(State); State = result.State; Changed?.Invoke();
            }
            return result;
        }
        public bool Undo()
        {
            if (history.Count == 0) return false;
            State = history.Pop(); Changed?.Invoke(); return true;
        }
        public void Restart()
        { history.Clear(); State = GameState.Initial(Level); Changed?.Invoke(); }
    }
}
