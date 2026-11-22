using System;
using System.Collections.Generic;

namespace SqueakyLane.Core.Model
{
    public sealed class GameState
    {
        private readonly CarState[] cars;
        private readonly bool[] muds;
        private readonly int[] pyl;
        // Truy cập từng phần tử tránh tạo ba wrapper cho mỗi node solver.
        public int CarCount => cars.Length;
        public int MudCount => muds.Length;
        public int PylonCount => pyl.Length;
        public CarState CarAt(int index) => cars[index];
        public bool MudAt(int index) => muds[index];
        public int PylonTurnsAt(int index) => pyl[index];
        public int Moves { get; private set; }

        public GameState(IReadOnlyList<CarState> cars, IReadOnlyList<bool> muds, IReadOnlyList<int> pyl, int moves = 0)
            : this(ReadOnlyData.Copy(cars), ReadOnlyData.Copy(muds), ReadOnlyData.Copy(pyl), moves) { }

        private GameState(CarState[] cars, bool[] muds, int[] pyl, int moves)
        {
            if (moves < 0) throw new ArgumentOutOfRangeException(nameof(moves));
            this.cars = cars; this.muds = muds; this.pyl = pyl; Moves = moves;
        }

        public static GameState Initial(LevelDef level)
        {
            var cars = new CarState[level.Cars.Count];
            for (int i = 0; i < cars.Length; i++)
            { var c = level.Cars[i]; cars[i] = new CarState(c.R, c.C, c.Axis, c.Color, c.Mud); }
            var muds = new bool[level.Muds.Count];
            for (int i = 0; i < muds.Length; i++) muds[i] = true;
            var pyl = new int[level.Pylons.Count];
            for (int i = 0; i < pyl.Length; i++) pyl[i] = level.Pylons[i].Turns;
            return new GameState(cars, muds, pyl, 0);
        }

        // Chỉ luật trong Core được sửa bản sao chưa công bố; không sửa state đầu vào.
        internal GameState Copy() => new GameState((CarState[])cars.Clone(), (bool[])muds.Clone(), (int[])pyl.Clone(), Moves);
        internal void SetCar(int index, CarState car) { cars[index] = car; }
        internal void ConsumeMud(int index) { muds[index] = false; }
        internal void SetPylonTurns(int index, int turns) { pyl[index] = turns; }
        internal void AdvanceMove() { Moves++; }
    }
}
