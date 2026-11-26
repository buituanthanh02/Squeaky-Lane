using System;
using UnityEngine;
namespace SqueakyLane.Game
{
    public sealed class ConfigProvider
    {
        public GameConfig Game { get; }
        public FeelConfig Feel { get; }
        public CameraConfig Camera { get; }
        public CarCatalog Cars { get; }
        public MechanicCatalog Mechanics { get; }
        public ConfigProvider(GameConfig game, FeelConfig feel, CameraConfig camera, CarCatalog cars = null, MechanicCatalog mechanics = null)
        {
            if (game == null || feel == null || camera == null) throw new ArgumentException("Thiếu config trên GameRoot; chạy Tools/Squeaky Lane/Setup Play.");
            Game = game; Feel = feel; Camera = camera;
            Cars = cars;
            Mechanics = mechanics;
        }
        // Đây là điểm duy nhất đọc Resources; bootstrap không biết đường dẫn tài nguyên.
        public TextAsset LevelOrder() => LoadText("Levels/level_order");
        public TextAsset Level(int id) => LoadText("Levels/level_" + id.ToString("00"));
        // Mọi file trong Resources/Tutorial là một bài tập; thứ tự nằm trong file.
        public TextAsset[] TutorialLessons() => Resources.LoadAll<TextAsset>("Tutorial");
        private static TextAsset LoadText(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Thiếu " + path + "; chạy Sync Levels.");
            return asset;
        }
    }
}
