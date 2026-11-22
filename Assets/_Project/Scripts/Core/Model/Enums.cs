namespace SqueakyLane.Core.Model
{
    // Thứ tự là hợp đồng dữ liệu; chỉ thêm giá trị mới ở cuối.
    public enum Axis
    {
        Horizontal = 0, Vertical = 1
    }

    // Thứ tự là hợp đồng dữ liệu; chỉ thêm giá trị mới ở cuối.
    public enum Side
    {
        N = 0, W = 1, S = 2, E = 3
    }

    // Thứ tự là hợp đồng dữ liệu; chỉ thêm giá trị mới ở cuối.
    public enum GateEdge
    {
        Top = 0, Bottom = 1, Left = 2, Right = 3
    }

    // Thứ tự là hợp đồng dữ liệu; chỉ thêm giá trị mới ở cuối.
    public enum TileColor
    {
        Pink = 0, Blue = 1, Yellow = 2, Green = 3, Orange = 4, Purple = 5, Water = 6
    }

    // Thứ tự là hợp đồng dữ liệu; chỉ thêm giá trị mới ở cuối.
    public enum MoveEventKind
    {
        Step = 0, Paint = 1, Mud = 2, Spray = 3, Water = 4, Exit = 5, Stop = 6, Splash = 7, Push = 8, Tele = 9, Rotate = 10, Pyl = 11
    }

    // Thứ tự là hợp đồng dữ liệu; chỉ thêm giá trị mới ở cuối.
    public enum StopReason
    {
        Edge = 0, WrongGate = 1, Muddy = 2, Car = 3, Wall = 4, Push = 5
    }
}
