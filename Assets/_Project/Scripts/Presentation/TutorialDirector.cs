using System;
using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Class thường do GameBootstrap sở hữu (như TableButtons): nối TutorialRunner với vị trí trên màn hình.
    public sealed class TutorialDirector
    {
        private readonly TutorialView view;
        private readonly NavigationView navigation;
        private readonly TableButtons tableButtons;
        private readonly List<Rect> holes = new List<Rect>(8);
        private readonly Vector3[] corners = new Vector3[4];
        private BoardView board;
        private Camera camera;
        private GameSession session;
        private TutorialRunner runner;
        private TutorialStep shown;
        private SolverMove hint;
        private float hintLeft;
        private bool hintShown;

        public event Action SkipRequested;
        // Không có view trong scene thì bỏ hẳn hướng dẫn, game chạy như trước.
        public bool Available => view != null;
        public bool Active => runner != null;
        public bool Finished => runner != null && runner.IsDone;
        public TutorialLesson Lesson => runner == null ? null : runner.Lesson;

        public TutorialDirector(TutorialView view, NavigationView navigation, TableButtons tableButtons)
        {
            this.view = view;
            this.navigation = navigation;
            this.tableButtons = tableButtons;
        }

        public void Connect()
        {
            if (view == null) { return; }
            view.Continue += Next;
            view.Skip += RequestSkip;
        }

        public void Disconnect()
        {
            if (view == null) { return; }
            view.Continue -= Next;
            view.Skip -= RequestSkip;
            Hide();
        }

        // Gọi mỗi lần nạp bãi; lesson = null là màn chơi thường.
        public void Begin(TutorialLesson lesson, BoardView boardView, Camera boardCamera, GameSession game)
        {
            End();
            board = boardView;
            camera = boardCamera;
            session = game;
            runner = lesson == null ? null : new TutorialRunner(lesson);
        }

        public void End()
        {
            runner = null;
            hintLeft = 0;
            Hide();
        }

        public void Hide()
        {
            shown = null;
            hintShown = false;
            if (view != null) { view.Hide(); }
        }

        // Bãi nhận lệnh khi không có bài, hoặc bước đang hiện cần người chơi thao tác.
        public bool AcceptsBoard => runner == null || (shown != null && shown.Action != TutorialAction.Next);

        public bool Allows(SolverMove command, bool swipe)
        {
            if (runner == null) { return true; }
            if (shown == null) { return false; }
            if (runner.Allows(command, swipe)) { return true; }
            view.Nudge();
            return false;
        }

        public bool AllowsUndo => runner == null || (shown != null && runner.AllowsUndo);
        public bool AllowsHint => runner == null || (shown != null && runner.AllowsHint);

        public void Moved()
        {
            Advance(() => runner.Moved());
        }

        public void Undone()
        {
            Advance(() => runner.Undone());
        }

        // Gợi ý hiện bằng bàn tay trên đúng xe/nút (số trên nóc xe đã tắt nên chữ "xe 3" không đọc được).
        public bool ShowHint(SolverMove move)
        {
            if (view == null) { return false; }
            hint = move;
            hintLeft = view.HintSeconds;
            hintShown = false;
            return true;
        }

        // Gọi trong LateUpdate, sau khi nút bàn xoay đã cập nhật vị trí. visible = đang chơi và không che bởi hộp thoại.
        public void Tick(bool visible, bool busy, float deltaTime)
        {
            if (view == null || board == null) { return; }
            if (hintLeft > 0 && visible) { hintLeft -= deltaTime; }
            // Bước tự chơi giữ nguyên dòng chữ khi xe đang chạy; các bước khác đợi xe dừng để chiếu đúng chỗ.
            bool freePlay = shown != null && shown.Action == TutorialAction.Win;
            if (!visible || (busy && !freePlay))
            {
                Hide();
                return;
            }

            if (runner != null && !runner.IsDone)
            {
                TickStep();
                return;
            }

            if (runner == null && hintLeft > 0)
            {
                if (!hintShown)
                {
                    hintShown = true;
                    view.ShowPointerOnly();
                }

                TrackHint(hint);
                return;
            }

            Hide();
        }

        private void TickStep()
        {
            var step = runner.Current;
            if (shown != step)
            {
                shown = step;
                hintShown = false;
                view.ShowStep(step.Text, step.Action == TutorialAction.Next, navigation.PauseRect);
            }

            if (step.Action == TutorialAction.Win && hintLeft > 0)
            {
                TrackHint(hint);
                return;
            }

            holes.Clear();
            for (int i = 0; i < step.Focus.Count; i++)
            {
                if (TryRect(step.Focus[i], out var rect)) { holes.Add(rect); }
            }

            // Bước tự chơi không phủ tối: người chơi cần nhìn cả bãi.
            view.Track(holes, step.Action != TutorialAction.Win, EdgeInset(), Guide(step));
        }

        private void TrackHint(SolverMove move)
        {
            holes.Clear();
            var guide = default(TutorialPointer);
            switch (move.Kind)
            {
                case SolverMoveKind.MoveCar:
                    holes.Add(CarRect(move.Index));
                    guide = DragGuide(move.Index, move.Sign);
                    break;
                case SolverMoveKind.RotateTable:
                    int table = TableOf(move.Index);
                    if (table >= 0 && tableButtons.TryGetButtonRect(table, out var button))
                    {
                        holes.Add(button);
                        guide = TutorialPointer.Tap(button.center);
                    }

                    break;
                default:
                    var pylon = PylonRect(move.Index);
                    holes.Add(pylon);
                    guide = TutorialPointer.Tap(Screen(board.PylonPosition(move.Index)));
                    break;
            }

            view.Track(holes, false, 0, guide);
        }

        private TutorialPointer Guide(TutorialStep step)
        {
            switch (step.Action)
            {
                case TutorialAction.Move:
                    int sign = step.Sign == 0 ? 1 : step.Sign;
                    return step.Gesture == TutorialGesture.Tap ? TutorialPointer.Tap(HalfPoint(step.Target, sign)) : DragGuide(step.Target, sign);
                case TutorialAction.RotateTable:
                    int table = TableOf(step.Target);
                    return table >= 0 && tableButtons.TryGetButtonRect(table, out var button) ? TutorialPointer.Tap(button.center) : default;
                case TutorialAction.RotatePylon: return TutorialPointer.Tap(Screen(board.PylonPosition(step.Target)));
                case TutorialAction.Undo: return TutorialPointer.Tap(RectOf(navigation.UndoRect).center);
                default: return default;
            }
        }

        // Điểm giữa của nửa xe nằm về phía sign.
        private Vector2 HalfPoint(int car, int sign)
        {
            var item = board.Car(car).transform;
            float reach = board.Car(car).Length * .25f;
            return Screen(item.position + board.Car(car).PositiveDirection * (sign * reach));
        }

        private TutorialPointer DragGuide(int car, int sign)
        {
            var item = board.Car(car).transform;
            var step = board.Car(car).PositiveDirection * sign;
            return TutorialPointer.Drag(Screen(item.position - step * .3f), Screen(item.position + step * 1.5f));
        }

        private bool TryRect(TutorialTarget target, out Rect rect)
        {
            switch (target.Kind)
            {
                case TutorialTargetKind.Car:
                    rect = CarRect(target.A);
                    return board.Car(target.A).gameObject.activeInHierarchy;
                case TutorialTargetKind.Cell:
                    rect = CellsRect(target.A - .5f, target.B - .5f, target.A + .5f, target.B + .5f, .15f);
                    return true;
                case TutorialTargetKind.Gate:
                    rect = GateRect(target.Edge, target.A);
                    return true;
                case TutorialTargetKind.Table:
                    var table = session.Level.Tables[target.A];
                    rect = CellsRect(table.R - .5f, table.C - .5f, table.R + 1.5f, table.C + 1.5f, .15f);
                    if (tableButtons.TryGetButtonRect(target.A, out var button)) { rect = Union(rect, button); }
                    return true;
                case TutorialTargetKind.Pylon:
                    rect = PylonRect(target.A);
                    return true;
                case TutorialTargetKind.Undo:
                    rect = RectOf(navigation.UndoRect);
                    return true;
                case TutorialTargetKind.Hint:
                    rect = RectOf(navigation.HintRect);
                    return true;
                default:
                    rect = RectOf(navigation.ClockRect);
                    return true;
            }
        }

        private Rect CarRect(int index)
        {
            var box = board.Car(index).GetComponent<BoxCollider>();
            var half = box.size * .5f;
            var rect = new Rect(Screen(box.transform.TransformPoint(box.center)), Vector2.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = box.center + Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                rect = Include(rect, Screen(box.transform.TransformPoint(corner)));
            }

            return rect;
        }

        // Khối chữ nhật theo ô bãi (hàng/cột thực), cao thêm height đơn vị.
        private Rect CellsRect(float top, float left, float bottom, float right, float height)
        {
            var first = BoardPoint(top, left, LotSurfaceView.Top);
            var rect = new Rect(first, Vector2.zero);
            for (int i = 1; i < 8; i++)
            {
                rect = Include(rect, BoardPoint((i & 1) == 0 ? top : bottom, (i & 2) == 0 ? left : right,
                    LotSurfaceView.Top + ((i & 4) == 0 ? 0 : height)));
            }

            return rect;
        }

        // Cổng nằm ngay ngoài mép bãi và cao hơn mặt bãi.
        private Rect GateRect(GateEdge edge, int at)
        {
            float cols = session.Level.Cols, rows = session.Level.Rows;
            const float inside = .35f, outside = .55f, height = .7f;
            switch (edge)
            {
                case GateEdge.Left: return CellsRect(at - .5f, -.5f - outside, at + .5f, -.5f + inside, height);
                case GateEdge.Right: return CellsRect(at - .5f, cols - .5f - inside, at + .5f, cols - .5f + outside, height);
                case GateEdge.Top: return CellsRect(-.5f - outside, at - .5f, -.5f + inside, at + .5f, height);
                default: return CellsRect(rows - .5f - inside, at - .5f, rows - .5f + outside, at + .5f, height);
            }
        }

        private Rect PylonRect(int index)
        {
            var pylon = session.Level.Pylons[index];
            return CellsRect(pylon.R - .5f, pylon.C - .5f, pylon.R + .5f, pylon.C + .5f, .9f);
        }

        private int TableOf(int car)
        {
            var under = Core.Rules.TableRule.TableUnder(session.Level, session.State.CarAt(car), session.Level.Cars[car].Len);
            if (!under.HasValue) { return -1; }
            for (int i = 0; i < session.Level.Tables.Count; i++)
            {
                if (session.Level.Tables[i].R == under.Value.R && session.Level.Tables[i].C == under.Value.C) { return i; }
            }

            return -1;
        }

        // Hình gồm cả huy hiệu và nhãn con của nút HUD.
        private Rect RectOf(RectTransform target)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(target);
            corners[0] = target.TransformPoint(bounds.min);
            corners[1] = target.TransformPoint(bounds.max);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[1].x, corners[1].y);
        }

        // Chừa mép khi viền đỏ đang nhấp nháy để lớp tối không làm mờ cảnh báo.
        private float EdgeInset() => navigation.StuckEdgeShowing ? navigation.StuckEdgeWidth : 0;

        private Vector2 BoardPoint(float row, float column, float height) => Screen(board.transform.TransformPoint(board.Cell(row, column, height)));
        private Vector2 Screen(Vector3 world) => camera.WorldToScreenPoint(world);

        private static Rect Include(Rect rect, Vector2 point) => Rect.MinMaxRect(Mathf.Min(rect.xMin, point.x), Mathf.Min(rect.yMin, point.y),
            Mathf.Max(rect.xMax, point.x), Mathf.Max(rect.yMax, point.y));

        private static Rect Union(Rect a, Rect b) => Include(Include(a, b.min), b.max);

        private void Advance(Action step)
        {
            hintLeft = 0;
            if (runner != null) { step(); }
            // Bước tự chơi không đổi sau một nước: không tắt rồi hiện lại.
            if (shown == null || runner == null || runner.Current != shown) { Hide(); }
        }

        private void Next() => Advance(() => runner.Next());
        private void RequestSkip() => SkipRequested?.Invoke();
    }
}
