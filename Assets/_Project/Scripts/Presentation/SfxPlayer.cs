using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;
namespace SqueakyLane.Presentation
{
    // Phát tiếng hiệu ứng bằng một pool AudioSource. Mỗi loại tiếng chỉ giữ một bản đang kêu:
    // phát lại (hoặc hết kênh) thì bản cũ tắt dần trong SfxFadeSeconds, không chồng lên nhau và không cắt cụt.
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SfxCatalog catalog;
        private FeelConfig feel;
        private ProgressStore progress;
        private AudioSource[] voices;
        private SoundCue[] voiceCue;
        private float[] remaining, fadeRate, nextCue;
        public void Configure(FeelConfig settings, ProgressStore store)
        {
            feel = settings; progress = store;
            if (voices != null) return;
            // Nạp ba tiếng cần khớp chuyển động khi dựng GameRoot, trước lượt chạm đầu.
            Preload(SoundCue.Move);
            Preload(SoundCue.StopCar);
            Preload(SoundCue.StopWall);
            int count = settings.SfxVoices;
            voices = new AudioSource[count]; voiceCue = new SoundCue[count]; remaining = new float[count]; fadeRate = new float[count];
            nextCue = new float[System.Enum.GetValues(typeof(SoundCue)).Length];
            for (int i = 0; i < voices.Length; i++)
            { var go = new GameObject("SFX voice " + i); go.transform.SetParent(transform, false); var voice = go.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0; voices[i] = voice; }
        }
        private void Preload(SoundCue cue)
        {
            if (!Application.isPlaying || catalog == null) { return; }
            var entry = catalog.Find(cue);
            if (entry == null || entry.clips == null) { return; }
            foreach (var clip in entry.clips)
            {
                if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                {
                    clip.LoadAudioData();
                }
            }
        }
        // pitchScale > 1 nâng cao độ (chuỗi ra cổng liên tiếp); nhân với khoảng ngẫu nhiên của catalog.
        public void Play(SoundCue cue, float pitchScale = 1f, float volumeScale = 1f)
        {
            if (voices == null || catalog == null || !progress.SoundEnabled || feel.SfxVolume <= 0) return;
            var entry = catalog.Find(cue); int index = (int)cue;
            // Dòng không có clip (vd tiếng bước đã bỏ) nghĩa là cố ý im lặng.
            if (entry == null || entry.clips == null || entry.clips.Length == 0 || Time.unscaledTime < nextCue[index]) return;
            var clip = entry.clips[Random.Range(0, entry.clips.Length)]; if (clip == null) return;
            nextCue[index] = Time.unscaledTime + entry.cooldown;
            for (int i = 0; i < voices.Length; i++) { if (remaining[i] > 0 && fadeRate[i] <= 0 && voiceCue[i] == cue) { Fade(i); } }
            int slot = FreeSlot();
            var voice = voices[slot]; voice.Stop(); voice.clip = clip; voice.volume = Mathf.Clamp01(entry.volume * feel.SfxVolume * volumeScale);
            voice.pitch = Mathf.Clamp(Random.Range(entry.pitchMin, entry.pitchMax) * pitchScale, .5f, 3f); voice.Play();
            voiceCue[slot] = cue; fadeRate[slot] = 0; remaining[slot] = clip.length / voice.pitch;
        }
        // Xe ra cổng: tiếng cổng cao và to dần theo chuỗi; từ combo 2 chồng thêm lớp tiếng combo, càng dài càng "đã".
        public void PlayExit(int combo)
        {
            if (feel == null) return;
            Play(SoundCue.Exit, feel.ComboPitch(combo), feel.ComboGain(combo));
            if (combo < ProgressStore.MinCombo) return;
            Play(combo >= feel.ComboBigFrom ? SoundCue.ComboBig : SoundCue.ComboUp, feel.ComboLayerPitch(combo), feel.ComboLayerGain(combo));
        }
        public float LiveVolume(SoundCue cue)
        {
            if (voices != null) for (int i = 0; i < voices.Length; i++) if (remaining[i] > 0 && fadeRate[i] <= 0 && voiceCue[i] == cue) return voices[i].volume;
            return 0;
        }
        // Giọng rảnh; nếu hết thì giọng đang tắt dần nhỏ nhất; cuối cùng là giọng sắp kêu xong.
        private int FreeSlot()
        {
            int best = 0; float score = float.MaxValue;
            for (int i = 0; i < voices.Length; i++)
            {
                if (remaining[i] <= 0) { return i; }
                float value = fadeRate[i] > 0 ? voices[i].volume : 10 + remaining[i];
                if (value < score) { score = value; best = i; }
            }
            return best;
        }
        private void Fade(int i)
        {
            if (feel.SfxFadeSeconds <= 0) { Release(i); return; }
            fadeRate[i] = Mathf.Max(.001f, voices[i].volume) / feel.SfxFadeSeconds;
        }
        private void Release(int i)
        {
            voices[i].Stop(); voices[i].clip = null; remaining[i] = 0; fadeRate[i] = 0;
        }
        private void Update() { Advance(Time.unscaledDeltaTime); }
        // Tách khỏi Update để test được; không cấp phát.
        public void Advance(float seconds)
        {
            if (voices == null || seconds <= 0) return;
            for (int i = 0; i < voices.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] -= seconds;
                if (fadeRate[i] > 0) { voices[i].volume = Mathf.MoveTowards(voices[i].volume, 0, fadeRate[i] * seconds); }
                if (remaining[i] <= 0 || (fadeRate[i] > 0 && voices[i].volume <= 0)) { Release(i); }
            }
        }
        // Số bản đang kêu (không tính bản đang tắt dần) của một loại tiếng, và số bản đang tắt dần.
        public int LiveVoices(SoundCue cue)
        {
            int count = 0;
            if (voices != null) for (int i = 0; i < voices.Length; i++) if (remaining[i] > 0 && fadeRate[i] <= 0 && voiceCue[i] == cue) count++;
            return count;
        }
        public int FadingVoices
        {
            get { int count = 0; if (voices != null) for (int i = 0; i < voices.Length; i++) if (remaining[i] > 0 && fadeRate[i] > 0) count++; return count; }
        }
        public void StopAll() { if (voices != null) for (int i = 0; i < voices.Length; i++) Release(i); }
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
