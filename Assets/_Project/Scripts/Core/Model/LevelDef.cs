using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SqueakyLane.Core.Model
{
    public sealed class LevelDef
    {
        public int Cols { get; }
        public int Rows { get; }
        public bool Splash { get; }
        public IReadOnlyList<CarDef> Cars { get; }
        public IReadOnlyList<GateDef> Gates { get; }
        public IReadOnlyList<Cell> Cones { get; }
        public IReadOnlyList<Cell> Muds { get; }
        public IReadOnlyList<Cell> Sprays { get; }
        public IReadOnlyList<PaintDef> Paints { get; }
        public IReadOnlyList<LightDef> Lights { get; }
        public IReadOnlyList<Cell> Tables { get; }
        public IReadOnlyList<BarrierDef> Barriers { get; }
        public IReadOnlyList<SwitchDef> Switches { get; }
        public IReadOnlyList<PortalDef> Portals { get; }
        public IReadOnlyList<PylonDef> Pylons { get; }

        private readonly int[] mudMap;
        private readonly TileColor?[] paintMap;
        private readonly bool[] sprayMap;
        private readonly GateDef?[] gateMap;

        internal LevelDef(LevelDefBuilder builder)
        {
            if (builder.Cols <= 0 || builder.Rows <= 0) throw new ArgumentException("Kích thước bãi phải dương.");
            Cols = builder.Cols; Rows = builder.Rows; Splash = builder.Splash;
            Cars = ReadOnlyData.Freeze(builder.Cars); Gates = ReadOnlyData.Freeze(builder.Gates);
            Cones = ReadOnlyData.Freeze(builder.Cones); Muds = ReadOnlyData.Freeze(builder.Muds);
            Sprays = ReadOnlyData.Freeze(builder.Sprays); Paints = ReadOnlyData.Freeze(builder.Paints);
            Lights = ReadOnlyData.Freeze(builder.Lights); Tables = ReadOnlyData.Freeze(builder.Tables);
            Barriers = ReadOnlyData.Freeze(builder.Barriers); Switches = ReadOnlyData.Freeze(builder.Switches);
            Portals = ReadOnlyData.Freeze(builder.Portals); Pylons = ReadOnlyData.Freeze(builder.Pylons);

            int cellCount = checked(Cols * Rows);
            mudMap = new int[cellCount]; paintMap = new TileColor?[cellCount]; sprayMap = new bool[cellCount];
            gateMap = new GateDef?[checked(2 * (Cols + Rows))];
            for (int i = 0; i < cellCount; i++) mudMap[i] = -1;
            var occupied = new bool[cellCount];
            for (int i = 0; i < Cars.Count; i++)
            {
                var car = Cars[i];
                var state = new CarState(car.R, car.C, car.Axis, car.Color, car.Mud);
                for (int k = 0; k < car.Len; k++)
                {
                    var cell = state.CellAt(k);
                    int index = CheckedCell(cell.R, cell.C);
                    if (occupied[index]) throw new ArgumentException("Xe chồng nhau trong màn.");
                    occupied[index] = true;
                }
            }
            for (int i = 0; i < Cones.Count; i++) CheckedCell(Cones[i].R, Cones[i].C);
            for (int i = 0; i < Muds.Count; i++) mudMap[CheckedCell(Muds[i].R, Muds[i].C)] = i;
            for (int i = 0; i < Sprays.Count; i++) sprayMap[CheckedCell(Sprays[i].R, Sprays[i].C)] = true;
            for (int i = 0; i < Paints.Count; i++)
            {
                var paint = Paints[i];
                if (paint.Color < TileColor.Pink || paint.Color >= TileColor.Water) throw new ArgumentException("Sơn cần màu xe.");
                paintMap[CheckedCell(paint.R, paint.C)] = paint.Color;
            }
            for (int i = 0; i < Lights.Count; i++)
            {
                CheckedCell(Lights[i].R, Lights[i].C);
                if (Lights[i].Phase != 0 && Lights[i].Phase != 1) throw new ArgumentException("Phase phải là 0 hoặc 1.");
            }
            for (int i = 0; i < Tables.Count; i++)
            { CheckedCell(Tables[i].R, Tables[i].C); CheckedCell(Tables[i].R + 1, Tables[i].C + 1); }
            for (int i = 0; i < Gates.Count; i++)
            {
                var gate = Gates[i];
                if (gate.Edge < GateEdge.Top || gate.Edge > GateEdge.Right ||
                    gate.Index < 0 || gate.Index >= (gate.Edge <= GateEdge.Bottom ? Cols : Rows) ||
                    gate.Color < TileColor.Pink || gate.Color > TileColor.Water)
                    throw new ArgumentException("Cổng không hợp lệ.");
                gateMap[GateIndex(gate.Edge, gate.Index)] = gate;
            }
        }

        public bool Contains(int r, int c) => r >= 0 && c >= 0 && r < Rows && c < Cols;
        public GateDef? GateAt(GateEdge edge, int index) => gateMap[GateIndex(edge, index)];
        internal int MudAt(int r, int c) => mudMap[r * Cols + c];
        internal TileColor? PaintAt(int r, int c) => paintMap[r * Cols + c];
        internal bool SprayAt(int r, int c) => sprayMap[r * Cols + c];

        private int GateIndex(GateEdge edge, int index)
        {
            switch (edge)
            {
                case GateEdge.Top: return index;
                case GateEdge.Bottom: return Cols + index;
                case GateEdge.Left: return 2 * Cols + index;
                case GateEdge.Right: return 2 * Cols + Rows + index;
                default: throw new ArgumentOutOfRangeException(nameof(edge));
            }
        }

        private int CheckedCell(int r, int c)
        {
            if (!Contains(r, c)) throw new ArgumentException("Ô nằm ngoài bãi.");
            return r * Cols + c;
        }
    }

    internal static class ReadOnlyData
    {
        internal static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
            return copy;
        }

        internal static ReadOnlyCollection<T> Freeze<T>(IReadOnlyList<T> source)
        {
            // Sao chép cả khi nguồn là List: sửa builder không được làm đổi màn đã dựng.
            return Array.AsReadOnly(Copy(source));
        }
    }
}
