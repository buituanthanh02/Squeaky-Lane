using System;
using UnityEngine;
namespace SqueakyLane.Presentation
{
    public enum SoundCue { Ui, SelectCar, Move, Step, StopCar, StopWall, Exit, Water, Mud, Spray, Paint, Splash, Push, Tele, Rotate, Pylon, Undo, Hint, Won, Lost, Stuck, Countdown, RecordWhoosh, RecordSlam }
    [CreateAssetMenu(menuName = "Squeaky Lane/Sfx Catalog")]
    public sealed class SfxCatalog : ScriptableObject
    {
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        public Entry Find(SoundCue cue) { for (int i = 0; i < entries.Length; i++) if (entries[i] != null && entries[i].cue == cue) return entries[i]; return null; }
        private void OnValidate()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i]; if (e == null) continue;
                e.volume = Mathf.Clamp01(e.volume); e.cooldown = Mathf.Max(0, e.cooldown);
                e.pitchMin = Mathf.Clamp(e.pitchMin, .5f, 2); e.pitchMax = Mathf.Clamp(e.pitchMax, e.pitchMin, 2);
                for (int j = 0; j < i; j++) if (entries[j] != null && entries[j].cue == e.cue) Debug.LogWarning("SfxCatalog trùng cue " + e.cue + "; chỉ dùng dòng đầu.", this);
            }
        }
        [Serializable] public sealed class Entry
        {
            public SoundCue cue;
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0, 1)] public float volume = .6f;
            [Range(.5f, 2)] public float pitchMin = .95f, pitchMax = 1.05f;
            [Min(0), Tooltip("Giới hạn tiếng cùng loại khi nhiều xe phát cùng lúc.")] public float cooldown = .08f;
        }
    }
}
