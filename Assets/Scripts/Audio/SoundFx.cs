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
        AudioClip _click, _splash, _plop, _tick, _whoosh, _counter, _snap, _success, _levelUp, _coin, _heart, _rodSnap;

        // 계속 나는 소리: 릴 감기(드르르륵), 드랙(지이이익), 낚싯대 버티는 소리(끼익)
        AudioSource _reelLoop, _dragLoop, _strainLoop, _engineLoop;
        float _reelTarget, _reelPitch, _dragTarget, _dragPitch, _strainTarget, _engineTarget, _enginePitch;

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
            _counter = MakeCounter();
            _rodSnap = Make("rodsnap", 0.09f, t =>
                (Noise() * 0.6f + Mathf.Sin(2f * Mathf.PI * 2900f * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 1700f * t) * 0.5f) * Mathf.Exp(-t * 70f));

            _reelLoop = MakeLoop("reel_loop", ReelLoop);
            _dragLoop = MakeLoop("drag_loop", DragLoop);
            _strainLoop = MakeLoop("strain_loop", StrainLoop);
            _engineLoop = MakeLoop("engine_loop", EngineLoop);
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

        // ---------------- 손맛 소리 ----------------

        /// <summary>
        /// 카운터: 낚싯대를 휙 채는 바람 소리(0~0.1초) → 묵직한 타격(쿵) → 낚싯줄이 튕기는 소리(팅) → 드랙이 짧게 지익.
        /// </summary>
        static AudioClip MakeCounter()
        {
            float lp = 0f;
            return Make("counter", 0.75f, t =>
            {
                float n = Noise();
                lp += (n - lp) * Mathf.Lerp(0.05f, 0.6f, Mathf.Clamp01(t / 0.1f)); // 점점 밝아지는 휙
                float swish = t < 0.12f ? lp * Mathf.Sin(Mathf.PI * t / 0.12f) * 0.9f : 0f;
                float hitT = t - 0.09f;
                if (hitT < 0f) return swish;
                float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(95f, 45f, Mathf.Clamp01(hitT / 0.2f)) * hitT) * Mathf.Exp(-hitT * 9f) * 1.3f;
                float crack = n * Mathf.Exp(-hitT * 60f) * 0.9f;
                float twang = (Mathf.Sin(2f * Mathf.PI * 196f * hitT) + 0.5f * Mathf.Sin(2f * Mathf.PI * 392f * hitT)) * Mathf.Exp(-hitT * 5f) * 0.35f;
                float dragT = hitT - 0.12f;
                float drag = dragT > 0f && dragT < 0.3f ? Click(dragT, 70f, 3000f) * (1f - dragT / 0.3f) * 0.5f : 0f;
                return swish + thump + crack + twang + drag;
            });
        }

        /// <summary>freq Hz로 반복되는 짧은 금속 딸깍 (드랙 톱니, 릴 기어).</summary>
        static float Click(float t, float freq, float ring)
        {
            float local = t % (1f / freq);
            return (Noise() * 0.5f + Mathf.Sin(2f * Mathf.PI * ring * local)) * Mathf.Exp(-local * 900f);
        }

        /// <summary>릴 감기: 기어가 맞물리는 웅웅 소리 + 드르르륵 하는 잔 딸깍. 1초에 정수 주기라 끊김 없이 반복된다.</summary>
        static float ReelLoop(float t) =>
            Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.18f + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.08f +
            Click(t, 28f, 2200f) * 0.45f + Noise() * 0.04f;

        /// <summary>드랙: 스풀이 거꾸로 돌며 톱니가 빠르게 튕기는 지이이익.</summary>
        static float DragLoop(float t) => Click(t, 60f, 3400f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.1f;

        /// <summary>낚싯대가 버티는 끼익 (낮게 떨리는 마찰음).</summary>
        static float StrainLoop(float t)
        {
            float wobble = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4f * t);
            return (Mathf.Sin(2f * Mathf.PI * 140f * t + Mathf.Sin(2f * Mathf.PI * 9f * t) * 2f) * 0.35f + Noise() * 0.12f) * wobble;
        }

        /// <summary>선외기 엔진: 낮은 톱니파 웅웅 + 배기 퍼덕임.</summary>
        static float EngineLoop(float t)
        {
            float saw = (t * 55f) % 1f * 2f - 1f;
            float putt = Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 22f * t)) * 0.5f;
            return saw * 0.25f * (0.6f + putt) + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.12f + Noise() * 0.05f;
        }

        /// <summary>배 엔진. throttle 0~1(0이면 꺼짐), speed01: 최고 속도 대비.</summary>
        public void SetEngine(float throttle, float speed01)
        {
            _engineTarget = throttle > 0.01f ? 0.12f + throttle * 0.3f : 0f;
            _enginePitch = 0.55f + throttle * 0.35f + Mathf.Clamp01(speed01) * 0.6f;
        }

        AudioSource MakeLoop(string name, System.Func<float, float> f)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = true;
            src.volume = 0f;
            src.clip = Make(name, 1f, f);
            src.Play();
            return src;
        }

        /// <summary>릴을 감는 중. turnsPerSec: 손잡이 회전 속도, tension: 장력(높을수록 무겁고 높은 소리).</summary>
        public void SetReel(float turnsPerSec, float tension)
        {
            _reelTarget = turnsPerSec > 0.01f ? Mathf.Lerp(0.35f, 0.6f, Mathf.Clamp01(tension)) : 0f;
            _reelPitch = 0.7f + turnsPerSec * 0.09f + Mathf.Clamp01(tension) * 0.15f;
        }

        /// <summary>줄이 풀려 나가는 속도(m/s). 빠를수록 크고 높게 운다.</summary>
        public void SetDrag(float lineOutSpeed)
        {
            _dragTarget = Mathf.Clamp01(lineOutSpeed / 5f) * 0.7f;
            _dragPitch = 0.6f + Mathf.Clamp(lineOutSpeed, 0f, 10f) * 0.1f;
        }

        /// <summary>0~1. 장력이 HIGH를 넘으면 낚싯대가 끼익 버틴다.</summary>
        public void SetStrain(float amount) => _strainTarget = Mathf.Clamp01(amount) * 0.35f;

        void LateUpdate()
        {
            // 매 프레임 다시 넣어 주지 않으면 소리가 잦아든다
            float k = 1f - Mathf.Exp(-Time.deltaTime * 18f);
            _reelLoop.volume = Mathf.Lerp(_reelLoop.volume, _reelTarget, k);
            _reelLoop.pitch = Mathf.Lerp(_reelLoop.pitch, Mathf.Max(0.5f, _reelPitch), k);
            _dragLoop.volume = Mathf.Lerp(_dragLoop.volume, _dragTarget, k);
            _dragLoop.pitch = Mathf.Lerp(_dragLoop.pitch, Mathf.Max(0.5f, _dragPitch), k);
            _strainLoop.volume = Mathf.Lerp(_strainLoop.volume, _strainTarget, k * 0.5f);
            _engineLoop.volume = Mathf.Lerp(_engineLoop.volume, _engineTarget, k * 0.4f);
            _engineLoop.pitch = Mathf.Lerp(_engineLoop.pitch, Mathf.Max(0.4f, _enginePitch), k * 0.4f);
            _reelTarget = _dragTarget = _strainTarget = _engineTarget = 0f;
        }

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
        /// <summary>카운터 타격음. 콤보가 쌓일수록 조금씩 높아진다.</summary>
        public void Counter(int combo = 0) => Play(_counter, 1f, 1f + Mathf.Clamp(combo, 0, 5) * 0.04f);
        /// <summary>낚싯대 마디가 하나 뽑혀 고정되는 '착'. 끝 마디일수록 높다.</summary>
        public void RodSnap(int section) => Play(_rodSnap, 0.55f, 0.85f + section * 0.08f);
        public void Snap() => Play(_snap, 0.9f);
        public void Success() => Play(_success, 0.8f);
        public void LevelUp() => Play(_levelUp, 0.8f);
        public void Coin() => Play(_coin, 0.7f);
        public void Heartbeat() => Play(_heart, 1f);
    }
}
