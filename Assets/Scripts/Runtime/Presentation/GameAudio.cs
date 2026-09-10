using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    internal enum GameCue { Click, Card, Cast, Impact, Damage, Reward }

    /// <summary>Original synthesized cues, built once and released with the owning view.</summary>
    internal sealed class GameAudio : System.IDisposable
    {
        private readonly AudioSource source;
        private readonly AudioClip[] clips = new AudioClip[6];
        private readonly float originalListenerVolume;
        public GameAudio(GameObject owner)
        {
            originalListenerVolume = AudioListener.volume;
            source = owner.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
            source.ignoreListenerPause = true;
            float[] frequencies = { 660, 880, 220, 440, 110, 1046.5f };
            for (int cue = 0; cue < clips.Length; cue++)
            {
                int count = cue == 0 ? 2205 : cue == 5 ? 17640 : 8820;
                var samples = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = i / 44100f, phase = 2 * Mathf.PI * frequencies[cue] * t;
                    float envelope = Mathf.Min(1, i / 200f) * Mathf.Pow(1 - i / (float)count, 2);
                    samples[i] = (Mathf.Sin(phase) + .22f * Mathf.Sin(phase * 1.5f)) * envelope * .18f;
                }
                clips[cue] = AudioClip.Create("Graphaclysm cue " + cue, count, 1, 44100, false);
                clips[cue].SetData(samples, 0);
            }
        }
        public void Apply(int master, int effects) { AudioListener.volume = master / 100f; source.volume = effects / 100f; }
        public void Play(GameCue cue) { if (source != null) source.PlayOneShot(clips[(int)cue]); }
        public void Dispose()
        {
            AudioListener.volume = originalListenerVolume;
            if (source != null) Object.Destroy(source);
            for (int i = 0; i < clips.Length; i++) if (clips[i] != null) Object.Destroy(clips[i]);
        }
    }
}
