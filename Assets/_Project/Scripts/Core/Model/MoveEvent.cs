using System;
using System.Collections.Generic;

namespace SqueakyLane.Core.Model
{
    public readonly struct MoveEvent
    {
        public MoveEventKind Kind { get; }
        public int R { get; }
        public int C { get; }
        public int Sign { get; }
        public TileColor? Color { get; }
        public int MudIndex { get; }
        public int HadMud { get; }
        public int OtherCar { get; }
        public int PylonIndex { get; }
        public StopReason? Reason { get; }
        public GateDef? Gate { get; }
        public PortalFace? FromFace { get; }
        public PortalFace? ToFace { get; }
        public Axis? Axis { get; }
        public IReadOnlyList<PushedCarEvent> PushedCars { get; }

        public MoveEvent(MoveEventKind kind, int r = 0, int c = 0, int sign = 0,
            TileColor? color = null, int mudIndex = -1, int hadMud = 0, int otherCar = -1,
            StopReason? reason = null, GateDef? gate = null, IReadOnlyList<PushedCarEvent> pushedCars = null,
            PortalFace? fromFace = null, PortalFace? toFace = null, Axis? axis = null, int pylonIndex = -1)
        {
            Kind = kind; R = r; C = c; Sign = sign; Color = color; MudIndex = mudIndex;
            HadMud = hadMud; OtherCar = otherCar; Reason = reason; Gate = gate;
            FromFace = fromFace; ToFace = toFace; Axis = axis; PylonIndex = pylonIndex;
            // Chỉ Push có danh sách con; Step/Paint/... không tạo collection rỗng mới.
            PushedCars = pushedCars == null ? Array.Empty<PushedCarEvent>() : ReadOnlyData.Freeze(pushedCars);
        }
    }

    public readonly struct PushedCarEvent
    {
        public int Car { get; }
        public int R { get; }
        public int C { get; }
        public IReadOnlyList<MoveEvent> Effects { get; }

        public PushedCarEvent(int car, int r, int c, IReadOnlyList<MoveEvent> effects)
        { Car = car; R = r; C = c; Effects = ReadOnlyData.Freeze(effects); }
    }
}
