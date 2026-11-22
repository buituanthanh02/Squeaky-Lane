using System;
using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Game
{
    public static class LevelJsonLoader
    {
        public static LevelDef Load(string json, string sourceName)
        {
            string source = string.IsNullOrWhiteSpace(sourceName) ? "JSON không tên" : sourceName;
            try
            {
                if (string.IsNullOrWhiteSpace(json)) throw Bad("json", "nội dung rỗng");
                var data = JsonUtility.FromJson<LevelData>(json);
                if (data == null || data.cols <= 0 || data.rows <= 0) throw Bad("cols/rows", "kích thước phải dương");
                var builder = new LevelDefBuilder(data.cols, data.rows) { Splash = data.splash };
                var cars = data.cars ?? Array.Empty<CarData>();
                for (int i = 0; i < cars.Length; i++)
                {
                    var car = Require(cars[i], $"cars[{i}]");
                    builder.Cars.Add(new CarDef(ReadAxis(car.axis, $"cars[{i}].axis"), car.r, car.c,
                        ReadColor(car.color, false, $"cars[{i}].color"), car.len, car.truck, car.mud));
                }
                var gates = data.gates ?? Array.Empty<GateData>();
                for (int i = 0; i < gates.Length; i++)
                {
                    var gate = Require(gates[i], $"gates[{i}]");
                    builder.Gates.Add(new GateDef(ReadEdge(gate.side, $"gates[{i}].side"), gate.i,
                        ReadColor(gate.color, true, $"gates[{i}].color")));
                }
                builder.Cones.AddRange(ReadCells(data.cones, data, "cones"));
                builder.Muds.AddRange(ReadCells(data.muds, data, "muds"));
                builder.Sprays.AddRange(ReadCells(data.sprays, data, "sprays"));
                builder.Tables.AddRange(ReadCells(data.tables, data, "tables"));
                var paints = data.paints ?? Array.Empty<PaintData>();
                for (int i = 0; i < paints.Length; i++)
                {
                    var paint = Require(paints[i], $"paints[{i}]");
                    CheckCell(paint.r, paint.c, data, $"paints[{i}]");
                    builder.Paints.Add(new PaintDef(paint.r, paint.c, ReadColor(paint.color, false, $"paints[{i}].color")));
                }
                var lights = data.lights ?? Array.Empty<LightData>();
                for (int i = 0; i < lights.Length; i++)
                {
                    var light = Require(lights[i], $"lights[{i}]");
                    CheckCell(light.r, light.c, data, $"lights[{i}]");
                    builder.Lights.Add(new LightDef(light.r, light.c, light.phase));
                }
                var barriers = data.barriers ?? Array.Empty<BarrierData>();
                for (int i = 0; i < barriers.Length; i++)
                {
                    var barrier = Require(barriers[i], $"barriers[{i}]");
                    CheckCell(barrier.r, barrier.c, data, $"barriers[{i}]");
                    if (barrier.need < 0) throw Bad($"barriers[{i}].need", "không được âm");
                    builder.Barriers.Add(new BarrierDef(barrier.r, barrier.c, barrier.need));
                }
                var portals = data.portals ?? Array.Empty<PortalData>();
                for (int i = 0; i < portals.Length; i++)
                {
                    var portal = Require(portals[i], $"portals[{i}]");
                    builder.Portals.Add(new PortalDef(ReadFace(portal.a, data, $"portals[{i}].a"),
                        ReadFace(portal.b, data, $"portals[{i}].b")));
                }
                var pylons = data.pylons ?? Array.Empty<PylonData>();
                for (int i = 0; i < pylons.Length; i++)
                {
                    var pylon = Require(pylons[i], $"pylons[{i}]");
                    CheckCell(pylon.r, pylon.c, data, $"pylons[{i}]");
                    if (pylon.to == null || pylon.to.Length != 2) throw Bad($"pylons[{i}].to", "cần đúng hai mặt cổng đích");
                    int turns = pylon.o == "h" ? 0 : pylon.o == "v" ? 1 : throw Bad($"pylons[{i}].o", "chỉ nhận h hoặc v");
                    builder.Pylons.Add(new PylonDef(pylon.r, pylon.c, turns, new[] {
                        ReadFace(pylon.to[0], data, $"pylons[{i}].to[0]"),
                        ReadFace(pylon.to[1], data, $"pylons[{i}].to[1]") }, pylon.@fixed));
                }
                var switches = data.switches ?? Array.Empty<SwitchData>();
                for (int i = 0; i < switches.Length; i++)
                {
                    var button = Require(switches[i], $"switches[{i}]");
                    CheckCell(button.r, button.c, data, $"switches[{i}]");
                    builder.Switches.Add(new SwitchDef(button.r, button.c,
                        ReadCells(button.a, data, $"switches[{i}].a"), ReadCells(button.b, data, $"switches[{i}].b")));
                }
                // Build kiểm cả thân xe/chồng xe, bảng 2×2 và giới hạn cổng; không lặp luật Core ở loader.
                return builder.Build();
            }
            catch (Exception error) when (error is ArgumentException || error is OverflowException ||
                error is InvalidOperationException || error is FormatException)
            {
                throw new FormatException($"Không nạp được màn '{source}': {error.Message}", error);
            }
        }

        private static Axis ReadAxis(string key, string field)
        {
            if (key == "h") return Axis.Horizontal;
            if (key == "v") return Axis.Vertical;
            throw Bad(field, $"trục lạ '{key}'");
        }

        private static GateEdge ReadEdge(string key, string field)
        {
            switch (key)
            {
                case "t": return GateEdge.Top;
                case "b": return GateEdge.Bottom;
                case "l": return GateEdge.Left;
                case "r": return GateEdge.Right;
                default: throw Bad(field, $"cạnh bãi lạ '{key}'");
            }
        }

        private static TileColor ReadColor(string key, bool waterAllowed, string field)
        {
            switch (key)
            {
                case "pink": return TileColor.Pink;
                case "blue": return TileColor.Blue;
                case "yellow": return TileColor.Yellow;
                case "green": return TileColor.Green;
                case "orange": return TileColor.Orange;
                case "purple": return TileColor.Purple;
                case "water": if (waterAllowed) return TileColor.Water; break;
            }
            throw Bad(field, $"màu không hợp lệ '{key}'");
        }

        private static PortalFace ReadFace(FaceData face, LevelData level, string field)
        {
            Require(face, field);
            CheckCell(face.r, face.c, level, field);
            Side side;
            switch (face.side)
            {
                case "N": side = Side.N; break;
                case "W": side = Side.W; break;
                case "S": side = Side.S; break;
                case "E": side = Side.E; break;
                default: throw Bad(field + ".side", $"cạnh ô lạ '{face.side}'");
            }
            return new PortalFace(face.r, face.c, side);
        }

        private static Cell[] ReadCells(CellData[] cells, LevelData level, string field)
        {
            // Không dựa vào constructor DTO: field/mảng vắng từ JsonUtility có thể là null.
            if (cells == null) return Array.Empty<Cell>();
            var result = new Cell[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = Require(cells[i], $"{field}[{i}]");
                CheckCell(cell.r, cell.c, level, $"{field}[{i}]");
                result[i] = new Cell(cell.r, cell.c);
            }
            return result;
        }

        private static void CheckCell(int r, int c, LevelData level, string field)
        {
            if (r < 0 || c < 0 || r >= level.rows || c >= level.cols) throw Bad(field, $"ô ({r},{c}) ngoài bãi");
        }
        private static T Require<T>(T value, string field) where T : class => value ?? throw Bad(field, "thiếu dữ liệu");
        private static ArgumentException Bad(string field, string reason) => new ArgumentException(field + ": " + reason);

        // DTO nằm cùng loader vì chỉ phục vụ định dạng JSON; Core không biết các kiểu này.
        // Field được gán mặc định tường minh; sau FromJson vẫn chuẩn hóa mảng null ở phía trên.
        [Serializable] private sealed class LevelData
        {
            public int cols = 0, rows = 0;
            public bool splash = false;
            public CarData[] cars = null;
            public GateData[] gates = null;
            public CellData[] cones = null, muds = null, sprays = null, tables = null;
            public PaintData[] paints = null;
            public LightData[] lights = null;
            public BarrierData[] barriers = null;
            public PortalData[] portals = null;
            public PylonData[] pylons = null;
            public SwitchData[] switches = null;
        }
        [Serializable] private sealed class CarData
        {
            public string axis = null, color = null;
            public int r = 0, c = 0, len = 0, mud = 0;
            public bool truck = false;
        }
        [Serializable] private sealed class GateData { public string side = null, color = null; public int i = 0; }
        [Serializable] private sealed class CellData { public int r = 0, c = 0; }
        [Serializable] private sealed class PaintData { public int r = 0, c = 0; public string color = null; }
        [Serializable] private sealed class LightData { public int r = 0, c = 0, phase = 0; }
        [Serializable] private sealed class BarrierData { public int r = 0, c = 0, need = 0; }
        [Serializable] private sealed class FaceData { public int r = 0, c = 0; public string side = null; }
        [Serializable] private sealed class PortalData { public FaceData a = null, b = null; }
        [Serializable] private sealed class PylonData
        {
            public int r = 0, c = 0;
            public string o = null;
            public bool @fixed = false;
            public FaceData[] to = null;
        }
        [Serializable] private sealed class SwitchData { public int r = 0, c = 0; public CellData[] a = null, b = null; }
    }
}
