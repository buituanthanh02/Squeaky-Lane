using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.Tests.EditMode
{
    // 016: tiếng không được chồng nhau thành tiếng rè (lỗi người làm nghe 26/11) và tiếng rửa phát đúng lúc.
    public sealed class Audio016Tests
    {
        // Dữ liệu thật: tiếng bước đã bỏ; tiếng nào lặp dồn dập khi chơi thì clip phải ngắn.
        [Test]
        public void CatalogHasNoStepSoundAndRapidCuesAreShort()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>("Assets/_Project/Config/SfxCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            var step = catalog.Find(SoundCue.Step);
            Assert.That(step == null || step.clips.Length == 0, Is.True, "Tiếng bước phát mỗi ô gây rè, phải để trống");
            foreach (var cue in new[] { SoundCue.Ui, SoundCue.SelectCar, SoundCue.Move, SoundCue.StopCar, SoundCue.StopWall, SoundCue.Exit, SoundCue.Push, SoundCue.Countdown })
            {
                foreach (var clip in catalog.Find(cue).clips) { Assert.That(clip.length, Is.LessThan(.45f), cue + " quá dài: " + clip.name); }
            }
            foreach (SoundCue cue in System.Enum.GetValues(typeof(SoundCue)))
            {
                var entry = catalog.Find(cue);
                if (entry == null) { continue; }
                foreach (var clip in entry.clips) { Assert.That(clip, Is.Not.Null, cue.ToString()); Assert.That(clip.length, Is.LessThanOrEqualTo(1.25f), cue + " " + clip.name); }
            }
        }

        [Test]
        public void SameCueNeverStacksAndOldCopyFadesOut()
        {
            var host = new GameObject("sfx test");
            var feel = ScriptableObject.CreateInstance<FeelConfig>();
            var catalog = ScriptableObject.CreateInstance<SfxCatalog>();
            var clip = AudioClip.Create("beep", 44100, 1, 44100, false);
            try
            {
                var entry = new SfxCatalog.Entry { cue = SoundCue.Exit, clips = new[] { clip }, cooldown = 0 };
                Set(catalog, "entries", new[] { entry });
                var player = host.AddComponent<SfxPlayer>();
                Set(player, "catalog", catalog);
                player.Configure(feel, new ProgressStore(new Memory()));
                for (int i = 0; i < 5; i++) { player.Play(SoundCue.Exit); }
                Assert.That(player.LiveVoices(SoundCue.Exit), Is.EqualTo(1), "Chỉ một bản kêu, không chồng");
                Assert.That(player.FadingVoices, Is.GreaterThan(0), "Bản cũ tắt dần chứ không cắt cụt");
                player.Advance(feel.SfxFadeSeconds + .01f);
                Assert.That(player.FadingVoices, Is.Zero);
                Assert.That(player.LiveVoices(SoundCue.Exit), Is.EqualTo(1));
                player.Advance(3f);
                Assert.That(player.LiveVoices(SoundCue.Exit), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(host); Object.DestroyImmediate(feel);
                Object.DestroyImmediate(catalog); Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void WashSoundStartsWhenCarEntersStationAndIsNotRepeatedByTheSprayEvent()
        {
            var root = new GameObject("wash sound");
            var feel = ScriptableObject.CreateInstance<FeelConfig>();
            var palette = ScriptableObject.CreateInstance<VisualPalette>();
            var material = new Material(Shader.Find("Standard"));
            try
            {
                Set(palette, "material", material);
                var board = root.AddComponent<BoardView>();
                var animator = root.AddComponent<ActionAnimator>();
                var builder = new LevelDefBuilder(5, 2);
                builder.Cars.Add(new CarDef(Axis.Horizontal, 0, 0, TileColor.Pink, mud: 1));
                builder.Sprays.Add(new Cell(0, 2));
                var session = new GameSession(builder.Build());
                board.Build(session.Level, session.State, palette);
                animator.Initialize(board, feel, session.State);
                int washes = 0, sprays = 0; bool heard = false;
                animator.WashStarted += () => { washes++; Assert.That(sprays, Is.Zero, "Tiếng rửa phải phát trước sự kiện Spray"); };
                animator.EventStarted += (kind, reason) => { if (kind == MoveEventKind.Spray) { sprays++; heard = animator.SprayAlreadyHeard; } };
                var command = new SolverMove(SolverMoveKind.MoveCar, 0, 1);
                animator.Enqueue(command, session.State, session.Apply(command));
                for (int i = 0; i < 300; i++) { animator.Advance(.01f); }
                Assert.That(washes, Is.EqualTo(1));
                Assert.That(sprays, Is.EqualTo(1));
                Assert.That(heard, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(feel);
                Object.DestroyImmediate(palette); Object.DestroyImmediate(material);
            }
        }

        private sealed class Memory : IProgressStorage
        {
            private readonly System.Collections.Generic.Dictionary<string, int> values = new System.Collections.Generic.Dictionary<string, int>();
            public int GetInt(string key, int fallback) { return values.TryGetValue(key, out var value) ? value : fallback; }
            public void SetInt(string key, int value) { values[key] = value; }
            public void Save() { }
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
