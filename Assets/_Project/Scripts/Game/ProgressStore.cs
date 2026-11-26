using System;
using System.Collections.Generic;
using UnityEngine;

namespace SqueakyLane.Game
{
    public interface IProgressStorage
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class ProgressStore
    {
        private const string Prefix = "squeaky-lane.";
        private readonly IProgressStorage storage;
        private bool soundEnabled, hapticsEnabled;
        public ProgressStore(IProgressStorage storage = null)
        {
            this.storage = storage ?? new PlayerPrefsStorage();
            soundEnabled = this.storage.GetInt(Prefix + "sound", 1) != 0;
            hapticsEnabled = this.storage.GetInt(Prefix + "haptics", 1) != 0;
        }
        public bool SoundEnabled
        {
            get => soundEnabled;
            set { if (value == soundEnabled) return; soundEnabled = value; Write(Prefix + "sound", value ? 1 : 0); }
        }
        public bool HapticsEnabled
        {
            get => hapticsEnabled;
            set { if (value == hapticsEnabled) return; hapticsEnabled = value; Write(Prefix + "haptics", value ? 1 : 0); }
        }
        public int Stars(int levelId)
        {
            CheckId(levelId);
            return Math.Max(0, Math.Min(3, storage.GetInt(Prefix + "stars." + levelId, 0)));
        }
        public void RecordWin(int levelId, int stars)
        {
            CheckId(levelId);
            if (stars < 1 || stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));
            if (stars > Stars(levelId)) Write(Prefix + "stars." + levelId, stars);
        }
        // Combo = số xe ra cổng liên tiếp; từ 2 xe mới gọi là combo. Kỷ lục lưu riêng từng màn.
        public const int MinCombo = 2;
        public int BestCombo(int levelId)
        {
            CheckId(levelId);
            return Math.Max(0, storage.GetInt(Prefix + "combo." + levelId, 0));
        }
        // true khi vừa phá kỷ lục của màn (lần đầu đạt combo cũng tính).
        public bool RecordCombo(int levelId, int combo)
        {
            if (combo < MinCombo || combo <= BestCombo(levelId)) return false;
            Write(Prefix + "combo." + levelId, combo);
            return true;
        }
        public bool IsUnlocked(IReadOnlyList<int> order, int index, bool unlockAll)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (index < 0 || index >= order.Count) throw new ArgumentOutOfRangeException(nameof(index));
            // Khóa theo thứ tự danh sách, không suy từ id số học: order có thể là 21, 3, 17...
            return unlockAll || index == 0 || Stars(order[index - 1]) > 0;
        }
        private void Write(string key, int value) { storage.SetInt(key, value); storage.Save(); }
        private static void CheckId(int id) { if (id < 1) throw new ArgumentOutOfRangeException(nameof(id)); }
        // Adapter duy nhất chạm PlayerPrefs; test dùng bộ nhớ riêng, không xóa tiến trình của người làm.
        private sealed class PlayerPrefsStorage : IProgressStorage
        {
            public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
            public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
            public void Save() => PlayerPrefs.Save();
        }
    }
}
