using System;
using SqueakyLane.Core.Solver;

namespace SqueakyLane.Game
{
    // Con trỏ bước của một bài tập: lệnh nào được phép, khi nào qua bước. Không biết UI hay phiên chơi.
    public sealed class TutorialRunner
    {
        public TutorialLesson Lesson { get; }
        public int Index { get; private set; }
        public bool IsDone => Index >= Lesson.Steps.Count;
        public TutorialStep Current => IsDone ? null : Lesson.Steps[Index];

        public TutorialRunner(TutorialLesson lesson)
        {
            Lesson = lesson ?? throw new ArgumentNullException(nameof(lesson));
        }

        // swipe chỉ có nghĩa với lệnh chạy xe (chạm nửa xe hay vuốt).
        public bool Allows(SolverMove command, bool swipe)
        {
            var step = Current;
            if (step == null)
            {
                return false;
            }

            switch (step.Action)
            {
                case TutorialAction.Win: return true;
                case TutorialAction.Move:
                    return command.Kind == SolverMoveKind.MoveCar && command.Index == step.Target
                        && (step.Sign == 0 || step.Sign == command.Sign)
                        && (step.Gesture == TutorialGesture.Any || (step.Gesture == TutorialGesture.Swipe) == swipe);
                case TutorialAction.RotateTable:
                    return command.Kind == SolverMoveKind.RotateTable && command.Index == step.Target;
                case TutorialAction.RotatePylon:
                    return command.Kind == SolverMoveKind.RotatePylon && command.Index == step.Target;
                default: return false;
            }
        }

        public bool AllowsUndo => Current != null && (Current.Action == TutorialAction.Undo || Current.Action == TutorialAction.Win);
        public bool AllowsHint => Current != null && Current.Action == TutorialAction.Win;

        // Gọi sau khi lệnh được phép đã làm bãi thay đổi.
        public void Moved()
        {
            var step = Current;
            if (step != null && (step.Action == TutorialAction.Move || step.Action == TutorialAction.RotateTable || step.Action == TutorialAction.RotatePylon))
            {
                Index++;
            }
        }

        public void Undone() => Advance(TutorialAction.Undo);
        public void Next() => Advance(TutorialAction.Next);
        // Thắng bãi tập hoặc bấm Bỏ qua đều kết thúc bài.
        public void Finish() => Index = Lesson.Steps.Count;

        private void Advance(TutorialAction action)
        {
            if (Current != null && Current.Action == action)
            {
                Index++;
            }
        }
    }
}
