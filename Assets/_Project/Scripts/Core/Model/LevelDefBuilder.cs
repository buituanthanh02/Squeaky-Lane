using System.Collections.Generic;

namespace SqueakyLane.Core.Model
{
    // Builder chỉ dùng lúc dựng dữ liệu; LevelDef sao chép lại để không giữ List có thể sửa.
    public sealed class LevelDefBuilder
    {
        public int Cols { get; }
        public int Rows { get; }
        public bool Splash { get; set; }
        public List<CarDef> Cars { get; } = new List<CarDef>();
        public List<GateDef> Gates { get; } = new List<GateDef>();
        public List<Cell> Cones { get; } = new List<Cell>();
        public List<Cell> Muds { get; } = new List<Cell>();
        public List<Cell> Sprays { get; } = new List<Cell>();
        public List<PaintDef> Paints { get; } = new List<PaintDef>();
        public List<LightDef> Lights { get; } = new List<LightDef>();
        public List<Cell> Tables { get; } = new List<Cell>();
        public List<BarrierDef> Barriers { get; } = new List<BarrierDef>();
        public List<SwitchDef> Switches { get; } = new List<SwitchDef>();
        public List<PortalDef> Portals { get; } = new List<PortalDef>();
        public List<PylonDef> Pylons { get; } = new List<PylonDef>();

        public LevelDefBuilder(int cols, int rows) { Cols = cols; Rows = rows; }
        public LevelDef Build() => new LevelDef(this);
    }
}
