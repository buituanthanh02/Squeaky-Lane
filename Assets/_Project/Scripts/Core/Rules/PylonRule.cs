using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Rules
{
    public static class PylonRule
    {
        public static void PylonFaces(PylonDef pylon, int turns, out PortalFace first, out PortalFace second)
        {
            // Mặt 0 đi N → W → S → E; cạnh cổng nhìn ngược lại về tâm trụ.
            // Tính từ state hiện tại, không cache vào dữ liệu màn dùng chung.
            switch (turns & 3)
            {
                case 0:
                    first = new PortalFace(pylon.R - 1, pylon.C, Side.S);
                    second = new PortalFace(pylon.R + 1, pylon.C, Side.N);
                    break;
                case 1:
                    first = new PortalFace(pylon.R, pylon.C - 1, Side.E);
                    second = new PortalFace(pylon.R, pylon.C + 1, Side.W);
                    break;
                case 2:
                    first = new PortalFace(pylon.R + 1, pylon.C, Side.N);
                    second = new PortalFace(pylon.R - 1, pylon.C, Side.S);
                    break;
                default:
                    first = new PortalFace(pylon.R, pylon.C + 1, Side.W);
                    second = new PortalFace(pylon.R, pylon.C - 1, Side.E);
                    break;
            }
        }

        public static MoveResult RotatePylon(LevelDef level, GameState state, int index, bool recordEvents = true)
        {
            // Chặn fixed tại Core để mọi bên gọi (input/solver) đều tuân cùng luật.
            if (index < 0 || index >= level.Pylons.Count || level.Pylons[index].Fixed)
                return MoveResult.Unchanged(state);
            var next = state.Copy();
            next.SetPylonTurns(index, (state.PylonTurnsAt(index) + 1) & 3);
            next.AdvanceMove();
            var events = recordEvents ? new List<MoveEvent> { new MoveEvent(MoveEventKind.Pyl, pylonIndex: index) } : null;
            return new MoveResult(next, events, true);
        }

        internal static bool IsPylonCell(LevelDef level, int r, int c)
        {
            for (int i = 0; i < level.Pylons.Count; i++)
                if (level.Pylons[i].R == r && level.Pylons[i].C == c) return true;
            return false;
        }
    }
}
