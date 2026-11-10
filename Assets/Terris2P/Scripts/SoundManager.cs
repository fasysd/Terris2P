using UnityEngine;

namespace Terris2P
{
    /// <summary>
    /// Bộ tổng hợp âm thanh retro 8-bit theo thủ tục bằng mã C#.
    /// Tự động sinh toàn bộ hiệu ứng âm thanh mà không cần tệp âm thanh bên ngoài, tối ưu cho WebGL.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource audioSource;

        private AudioClip sfxMove;
        private AudioClip sfxRotate;
        private AudioClip sfxHardDrop;
        private AudioClip sfxLineClear;
        private AudioClip sfxHeartLost;
        private AudioClip sfxVictory;

        private void Awake()
        {
            Instance = this;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            GenerateClips();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void GenerateClips()
        {
            sfxMove = CreateToneClip("Move", 0.05f, 220f, 220f, WaveType.Square);
            sfxRotate = CreateToneClip("Rotate", 0.07f, 330f, 440f, WaveType.Square);
            sfxHardDrop = CreateToneClip("HardDrop", 0.12f, 150f, 60f, WaveType.Triangle);
            sfxLineClear = CreateArpeggioClip("LineClear", new[] { 440f, 554.37f, 659.25f, 880f }, 0.06f);
            sfxHeartLost = CreateNoiseClip("HeartLost", 0.35f);
            sfxVictory = CreateArpeggioClip("Victory", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.1f);
        }

        public void PlayMove() => Play(sfxMove, 0.4f);
        public void PlayRotate() => Play(sfxRotate, 0.5f);
        public void PlayHardDrop() => Play(sfxHardDrop, 0.7f);
        public void PlayLineClear(int lineCount) => Play(sfxLineClear, Mathf.Clamp01(0.6f + lineCount * 0.1f));
        public void PlayHeartLost() => Play(sfxHeartLost, 0.8f);
        public void PlayVictory() => Play(sfxVictory, 0.9f);

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, volume);
        }

        private enum WaveType { Sine, Square, Triangle, Sawtooth }

        private static AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq, WaveType type)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;

                float sample = 0f;
                switch (type)
                {
                    case WaveType.Sine:
                        sample = Mathf.Sin(phase);
                        break;
                    case WaveType.Square:
                        sample = Mathf.Sin(phase) >= 0f ? 0.7f : -0.7f;
                        break;
                    case WaveType.Triangle:
                        sample = Mathf.PingPong(phase / Mathf.PI, 2f) - 1f;
                        break;
                    case WaveType.Sawtooth:
                        sample = (float)((phase / (2f * Mathf.PI)) % 1f) * 2f - 1f;
                        break;
                }

                // Đường bao âm: tăng nhanh, giảm mượt dần về 0
                float envelope = 1f - t;
                samples[i] = sample * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateArpeggioClip(string name, float[] freqs, float noteDuration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * noteDuration * freqs.Length);
            float[] samples = new float[totalSamples];

            int noteSampleCount = Mathf.CeilToInt(sampleRate * noteDuration);
            for (int note = 0; note < freqs.Length; note++)
            {
                float freq = freqs[note];
                float phase = 0f;
                int start = note * noteSampleCount;
                int end = Mathf.Min(start + noteSampleCount, totalSamples);

                for (int i = start; i < end; i++)
                {
                    phase += 2f * Mathf.PI * freq / sampleRate;
                    float sample = Mathf.Sin(phase) >= 0f ? 0.6f : -0.6f;
                    float noteT = (float)(i - start) / noteSampleCount;
                    float env = 1f - (noteT * 0.5f);
                    samples[i] = sample * env * 0.4f;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseClip(string name, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float noise = Random.Range(-1f, 1f);
                float envelope = Mathf.Pow(1f - t, 1.5f);
                samples[i] = noise * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
