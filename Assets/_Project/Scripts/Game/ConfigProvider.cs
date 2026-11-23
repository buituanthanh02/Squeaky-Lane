using System;
using UnityEngine;
namespace SqueakyLane.Game
{
    public sealed class ConfigProvider
    {
        public GameConfig Game { get; }
        public FeelConfig Feel { get; }
        public CameraConfig Camera { get; }
        public ConfigProvider(GameConfig game, FeelConfig feel, CameraConfig camera)
        {
            if (game == null || feel == null || camera == null) throw new ArgumentException("Thiếu config trên GameRoot; chạy Tools/Squeaky Lane/Setup Play.");
            Game = game; Feel = feel; Camera = camera;
        }
        // Đây là điểm duy nhất đọc Resources; bootstrap không biết đường dẫn tài nguyên.
        public TextAsset LevelOrder() => LoadText("Levels/level_order");
        public TextAsset Level(int id) => LoadText("Levels/level_" + id.ToString("00"));
        private static TextAsset LoadText(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Thiếu " + path + "; chạy Sync Levels.");
            return asset;
        }
    }
}
