using System;
using System.Collections.Generic;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Core.Solver
{
    public readonly struct StateKey : IEquatable<StateKey>
    {
        private readonly LevelDef level;
        private readonly byte[] bytes;
        private readonly int hash;
        public int ByteCount => bytes == null ? 0 : bytes.Length;

        internal StateKey(LevelDef level, byte[] bytes, int hash)
        { this.level = level; this.bytes = bytes; this.hash = hash; }

        public bool Equals(StateKey other)
        {
            if (!ReferenceEquals(level, other.level) || hash != other.hash) return false;
            if (ReferenceEquals(bytes, other.bytes)) return true;
            if (bytes == null || other.bytes == null || bytes.Length != other.bytes.Length) return false;
            // Hash 32 bit có thể trùng: chỉ coi cùng trạng thái khi toàn bộ byte đều bằng nhau.
            for (int i = 0; i < bytes.Length; i++) if (bytes[i] != other.bytes[i]) return false;
            return true;
        }
        public override bool Equals(object obj) => obj is StateKey other && Equals(other);
        public override int GetHashCode() => hash;
    }

    public sealed class StateKeyComparer : IEqualityComparer<StateKey>
    {
        public static readonly StateKeyComparer Instance = new StateKeyComparer();
        private StateKeyComparer() { }
        public bool Equals(StateKey x, StateKey y) => x.Equals(y);
        public int GetHashCode(StateKey obj) => obj.GetHashCode();
    }

    public sealed class StateKeyEncoder
    {
        private readonly LevelDef level;
        private readonly int rowBits, colBits, carBits, byteCount;
        private readonly bool includePhase;

        public StateKeyEncoder(LevelDef level)
        {
            this.level = level ?? throw new ArgumentNullException(nameof(level));
            rowBits = BitsFor(level.Rows - 1); colBits = BitsFor(level.Cols - 1);
            carBits = 1 + rowBits + colBits + 1 + 3 + 2;
            includePhase = level.Lights.Count > 0;
            int bitCount = checked(level.Cars.Count * carBits + level.Muds.Count + level.Pylons.Count * 2 + (includePhase ? 1 : 0));
            byteCount = checked((bitCount + 7) / 8);
        }

        public StateKey Encode(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.CarCount != level.Cars.Count || state.MudCount != level.Muds.Count || state.PylonCount != level.Pylons.Count)
                throw new ArgumentException("State không khớp số xe/bùn/trụ của màn.", nameof(state));
            var data = byteCount == 0 ? Array.Empty<byte>() : new byte[byteCount];
            var writer = new BitWriter(data);
            for (int i = 0; i < state.CarCount; i++)
            {
                var car = state.CarAt(i);
                if (!car.Alive)
                {
                    // Thuộc tính xe đã ra bãi không ảnh hưởng lượt sau, giống stateKey của prototype.
                    writer.Skip(carBits);
                    continue;
                }
                if (!level.Contains(car.R, car.C) || (car.Axis != Axis.Horizontal && car.Axis != Axis.Vertical) ||
                    car.Color < TileColor.Pink || car.Color >= TileColor.Water || car.Mud < 0 || car.Mud > 2)
                    throw new ArgumentException("Xe sống chứa dữ liệu ngoài miền mã hóa.", nameof(state));
                writer.Write(1, 1);
                writer.Write((uint)car.R, rowBits); writer.Write((uint)car.C, colBits);
                writer.Write((uint)car.Axis, 1); writer.Write((uint)car.Color, 3); writer.Write((uint)car.Mud, 2);
            }
            for (int i = 0; i < state.MudCount; i++) writer.Write(state.MudAt(i) ? 1u : 0u, 1);
            for (int i = 0; i < state.PylonCount; i++)
            {
                int turns = state.PylonTurnsAt(i);
                if (turns < 0 || turns > 3) throw new ArgumentException("Hướng trụ phải thuộc 0..3.", nameof(state));
                writer.Write((uint)turns, 2);
            }
            if (includePhase) writer.Write((uint)(state.Moves & 1), 1);
            uint hash = 2166136261u;
            for (int i = 0; i < data.Length; i++) hash = unchecked((hash ^ data[i]) * 16777619u);
            return new StateKey(level, data, unchecked((int)hash));
        }

        private static int BitsFor(int maximum)
        {
            int bits = 0;
            while (maximum > 0) { bits++; maximum >>= 1; }
            return bits;
        }

        private struct BitWriter
        {
            private readonly byte[] target;
            private int position;
            internal BitWriter(byte[] target) { this.target = target; position = 0; }
            internal void Skip(int bits) { position += bits; }
            internal void Write(uint value, int bits)
            {
                while (bits > 0)
                {
                    int offset = position & 7, take = Math.Min(bits, 8 - offset);
                    target[position >> 3] |= (byte)((value & ((1u << take) - 1)) << offset);
                    value >>= take; position += take; bits -= take;
                }
            }
        }
    }
}
