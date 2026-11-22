using System;
using System.Collections.Generic;

namespace SqueakyLane.Core.Model
{
    public readonly struct Cell
    {
        public int R { get; }
        public int C { get; }
        public Cell(int r, int c) { R = r; C = c; }
    }

    public readonly struct GateDef
    {
        public GateEdge Edge { get; }
        public int Index { get; }
        public TileColor Color { get; }
        public GateDef(GateEdge edge, int index, TileColor color) { Edge = edge; Index = index; Color = color; }
    }

    public readonly struct PaintDef
    {
        public int R { get; }
        public int C { get; }
        public TileColor Color { get; }
        public PaintDef(int r, int c, TileColor color) { R = r; C = c; Color = color; }
    }

    public readonly struct LightDef
    {
        public int R { get; }
        public int C { get; }
        public int Phase { get; }
        public LightDef(int r, int c, int phase = 0) { R = r; C = c; Phase = phase; }
    }

    public readonly struct BarrierDef
    {
        public int R { get; }
        public int C { get; }
        public int Need { get; }
        public BarrierDef(int r, int c, int need) { R = r; C = c; Need = need; }
    }

    public sealed class SwitchDef
    {
        public int R { get; }
        public int C { get; }
        public IReadOnlyList<Cell> A { get; }
        public IReadOnlyList<Cell> B { get; }

        public SwitchDef(int r, int c, IReadOnlyList<Cell> a, IReadOnlyList<Cell> b)
        { R = r; C = c; A = ReadOnlyData.Freeze(a); B = ReadOnlyData.Freeze(b); }
    }

    public readonly struct PortalFace
    {
        public int R { get; }
        public int C { get; }
        public Side Side { get; }
        public PortalFace(int r, int c, Side side) { R = r; C = c; Side = side; }
    }

    public readonly struct PortalDef
    {
        public PortalFace A { get; }
        public PortalFace B { get; }
        public PortalDef(PortalFace a, PortalFace b) { A = a; B = b; }
    }

    public sealed class PylonDef
    {
        public int R { get; }
        public int C { get; }
        public int Turns { get; }
        public bool Fixed { get; }
        public IReadOnlyList<PortalFace> To { get; }

        public PylonDef(int r, int c, int turns, IReadOnlyList<PortalFace> to, bool fixedRotation = false)
        {
            if (to == null || to.Count != 2) throw new ArgumentException("Trụ cần đúng hai cổng đích.", nameof(to));
            R = r; C = c; Turns = ((turns % 4) + 4) % 4; Fixed = fixedRotation;
            To = ReadOnlyData.Freeze(to);
        }
    }
}
