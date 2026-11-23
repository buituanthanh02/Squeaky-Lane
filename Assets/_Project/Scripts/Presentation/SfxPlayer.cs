using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
namespace SqueakyLane.Presentation
{
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SfxCatalog catalog;
        private FeelConfig feel;
        private ProgressStore progress;
        private AudioSource[] voices;
        private float[] nextCue;
        private int oldest;
        public void Configure(FeelConfig settings, ProgressStore store)
        {
            feel = settings; progress = store;
            if (voices != null) return;
            voices = new AudioSource[settings.SfxVoices]; nextCue = new float[System.Enum.GetValues(typeof(SoundCue)).Length];
            for (int i = 0; i < voices.Length; i++)
            { var go = new GameObject("SFX voice " + i); go.transform.SetParent(transform, false); var voice = go.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0; voices[i] = voice; }
        }
        public void Play(SoundCue cue)
        {
            if (voices == null || catalog == null || !progress.SoundEnabled || feel.SfxVolume <= 0) return;
            var entry = catalog.Find(cue); int index = (int)cue;
            if (entry == null || entry.clips == null || entry.clips.Length == 0 || Time.unscaledTime < nextCue[index]) return;
            var clip = entry.clips[Random.Range(0, entry.clips.Length)]; if (clip == null) return;
            nextCue[index] = Time.unscaledTime + entry.cooldown;
            int slot = -1; for (int i = 0; i < voices.Length; i++) if (!voices[i].isPlaying) { slot = i; break; }
            if (slot < 0) { slot = oldest; oldest = (oldest + 1) % voices.Length; }
            var voice = voices[slot]; voice.Stop(); voice.clip = clip; voice.volume = entry.volume * feel.SfxVolume;
            voice.pitch = Random.Range(entry.pitchMin, entry.pitchMax); voice.Play();
        }
        public void StopAll() { if (voices != null) for (int i = 0; i < voices.Length; i++) voices[i].Stop(); }
        public void PlayEvent(MoveEventKind kind, StopReason? reason)
        {
            switch (kind)
            {
                case MoveEventKind.Step: Play(SoundCue.Step); break;
                case MoveEventKind.Stop: Play(reason == StopReason.Car ? SoundCue.StopCar : SoundCue.StopWall); break;
                case MoveEventKind.Exit: Play(SoundCue.Exit); break; case MoveEventKind.Water: Play(SoundCue.Water); break;
                case MoveEventKind.Mud: Play(SoundCue.Mud); break; case MoveEventKind.Spray: Play(SoundCue.Spray); break;
                case MoveEventKind.Paint: Play(SoundCue.Paint); break; case MoveEventKind.Splash: Play(SoundCue.Splash); break;
                case MoveEventKind.Push: Play(SoundCue.Push); break; case MoveEventKind.Tele: Play(SoundCue.Tele); break;
                case MoveEventKind.Rotate: Play(SoundCue.Rotate); break; case MoveEventKind.Pyl: Play(SoundCue.Pylon); break;
            }
        }
        private void OnDisable() { StopAll(); }
    }
}
