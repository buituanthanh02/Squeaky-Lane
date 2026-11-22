using System;

namespace SqueakyLane.Core.Model
{
    public readonly struct CarDef
    {
        public Axis Axis { get; }
        public int R { get; }
        public int C { get; }
        public TileColor Color { get; }
        public int Len { get; }
        public bool Truck { get; }
        public int Mud { get; }

        public CarDef(Axis axis, int r, int c, TileColor color, int len = 2, bool truck = false, int mud = 0)
        {
            if (axis != Axis.Horizontal && axis != Axis.Vertical) throw new ArgumentOutOfRangeException(nameof(axis));
            if (color < TileColor.Pink || color >= TileColor.Water) throw new ArgumentOutOfRangeException(nameof(color));
            if (len != 2 && len != 3) throw new ArgumentOutOfRangeException(nameof(len));
            if (truck && len != 3) throw new ArgumentException("Xe tải phải dài 3 ô.", nameof(len));
            if (mud < 0 || mud > 2) throw new ArgumentOutOfRangeException(nameof(mud));
            Axis = axis; R = r; C = c; Color = color; Len = len; Truck = truck; Mud = mud;
        }
    }

    // Struct chỉ đọc: bên gọi không thể sửa xe bên trong một state đã nhận.
    public readonly struct CarState
    {
        public int R { get; }
        public int C { get; }
        public Axis Axis { get; }
        public TileColor Color { get; }
        public int Mud { get; }
        public bool Alive { get; }

        public CarState(int r, int c, Axis axis, TileColor color, int mud = 0, bool alive = true)
        { R = r; C = c; Axis = axis; Color = color; Mud = mud; Alive = alive; }

        public Cell CellAt(int offset) => Axis == Axis.Horizontal ? new Cell(R, C + offset) : new Cell(R + offset, C);
        public CarState At(int r, int c) => new CarState(r, c, Axis, Color, Mud, Alive);
        public CarState WithColor(TileColor color) => new CarState(R, C, Axis, color, Mud, Alive);
        public CarState WithMud(int mud) => new CarState(R, C, Axis, Color, mud, Alive);
        public CarState WithAlive(bool alive) => new CarState(R, C, Axis, Color, Mud, alive);
    }
}
