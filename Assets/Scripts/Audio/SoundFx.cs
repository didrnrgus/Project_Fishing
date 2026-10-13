using UnityEngine;

namespace Fishing.Audio
{
    /// <summary>
    /// 사운드 에셋 없이 절차적으로 만든 효과음. 원작에서 손맛의 핵심이던
    /// 릴 감는 소리, 물보라, 줄 끊어지는 소리 등을 간단히 흉내 낸다.
    /// </summary>
    public class SoundFx : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource _src;
        AudioClip _click, _splash, _plop, _tick, _whoosh, _counter, _snap, _success, _levelUp, _coin, _heart;

        void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;

            _click = Make("click", 0.025f, t => Noise() * Mathf.Exp(-t * 260f));
            _tick = Make("tick", 0.06f, t => Mathf.Sin(t * 2f * Mathf.PI * 900f) * Mathf.Exp(-t * 90f));
            _splash = Make("splash", 0.45f, t => Noise() * Mathf.Exp(-t * 9f) * (0.6f + 0.4f * Mathf.Sin(t * 120f)));
            _plop = Make("plop", 0.16f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 120f, t / 0.16f) * t) * Mathf.Exp(-t * 18f));
            _whoosh = Make("whoosh", 0.35f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.35f) * 0.5f);
            _counter = Make("counter", 0.4f, t => (Noise() * Mathf.Exp(-t * 14f) + Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 8f)) * 0.8f);
            _snap = Make("snap", 0.5f, t => Noise() * Mathf.Exp(-t * 70f) + Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-t * 10f) * 0.4f);
            _success = Make("success", 0.7f, t => Arpeggio(t, new[] { 523f, 659f, 784f, 1047f }, 0.12f));
            _levelUp = Make("levelup", 0.9f, t => Arpeggio(t, new[] { 392f, 523f, 659f, 784f, 1047f }, 0.1f));
            _coin = Make("coin", 0.25f, t => Mathf.Sin(2f * Mathf.PI * (t < 0.07f ? 988f : 1319f) * t) * Mathf.Exp(-t * 12f));
            _heart = Make("heart", 0.6f, t =>
            {
                float beat = t < 0.15f ? t : t - 0.22f;
                return beat < 0f ? 0f : Mathf.Sin(2f * Mathf.PI * 55f * beat) * Mathf.Exp(-beat * 25f);
            });
        }

        static float Noise() => Random.value * 2f - 1f;

        static float Arpeggio(float t, float[] notes, float step)
        {
            int i = Mathf.Min(notes.Length - 1, (int)(t / step));
            float local = t - i * step;
            return Mathf.Sin(2f * Mathf.PI * notes[i] * t) * Mathf.Exp(-local * 6f) * 0.5f;
        }

        static AudioClip Make(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f) * 0.6f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            _src.pitch = pitch;
            _src.PlayOneShot(clip, volume);
        }

        public void ReelClick(float tension) => Play(_click, 0.35f, Mathf.Lerp(0.9f, 1.4f, Mathf.Clamp01(tension)));
        public void Tick() => Play(_tick, 0.5f);
        public void Splash(float strength) => Play(_splash, Mathf.Clamp01(0.4f + strength * 0.4f), Random.Range(0.85f, 1.15f));
        public void Plop() => Play(_plop, 0.9f);
        public void Whoosh() => Play(_whoosh, 0.6f);
        public void Counter() => Play(_counter, 1f);
        public void Snap() => Play(_snap, 0.9f);
        public void Success() => Play(_success, 0.8f);
        public void LevelUp() => Play(_levelUp, 0.8f);
        public void Coin() => Play(_coin, 0.7f);
        public void Heartbeat() => Play(_heart, 1f);
    }
}
