using UnityEngine;

namespace Fishing.Audio
{
    /// <summary>
    /// 인트로 소리 (모두 절차적으로 만든다, 녹음 파일 없음).
    ///  - Stamp: 낙관을 찍는 묵직한 "쿵" + 종이 소리
    ///  - Chime: 맑은 종 (사업자 영어 이름이 뜰 때, 게임 시작)
    ///  - Brush: 붓이 종이를 스치는 "사아악" (타이틀 글씨가 써질 때)
    ///  - Gong: 징 (타이틀 붓글씨를 다 쓸 때)
    ///  - Theme: 가야금 같은 뜯는 소리(Karplus-Strong)로 평조(솔·라·도·레·미) 선율 + 낮은 울림 + 바람·물결. 36초, 반복 재생.
    /// 샘플 계산(특히 Theme)은 수십 ms가 걸려서 <see cref="BuildAsync"/>가 작업 스레드에서 하고, AudioClip은 메인 스레드에서 만든다.
    /// </summary>
    public static class IntroAudio
    {
        const int Rate = 44100;
        static System.Random _rng = new System.Random(5);

        /// <summary>계산만 끝난 소리 (스테레오 교차 샘플). <see cref="Raw.ToClip"/>는 메인 스레드에서.</summary>
        public sealed class Raw
        {
            public string Name;
            public float[] Data;
            public AudioClip ToClip()
            {
                var clip = AudioClip.Create(Name, Data.Length / 2, 2, Rate, false);
                clip.SetData(Data, 0);
                return clip;
            }
        }

        public sealed class Bank { public Raw Stamp, Chime, Brush, Gong, Theme; }

        public static System.Threading.Tasks.Task<Bank> BuildAsync() => System.Threading.Tasks.Task.Run(() => new Bank
        {
            Stamp = Stamp(), Chime = Chime(), Brush = Brush(), Gong = Gong(), Theme = Theme(),
        });

        static float Noise() => (float)(_rng.NextDouble() * 2 - 1);
        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        static Raw Clip(string name, float[] left, float[] right, float gain)
        {
            int n = left.Length;
            float peak = 1e-4f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(left[i]), Mathf.Abs(right[i]));
            float k = gain / peak;
            var data = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                data[i * 2] = left[i] * k;
                data[i * 2 + 1] = right[i] * k;
            }
            return new Raw { Name = name, Data = data };
        }

        /// <summary>짧은 잔향 (빗살 필터 몇 개). 작은 방 느낌.</summary>
        static void Room(float[] buf, float mix, float size)
        {
            int[] taps = { 1557, 1617, 1491, 1422 };
            var dry = (float[])buf.Clone();
            foreach (int tap in taps)
            {
                int d = (int)(tap * size);
                var comb = new float[buf.Length];
                for (int i = 0; i < buf.Length; i++)
                    comb[i] = dry[i] + (i >= d ? comb[i - d] * 0.72f : 0f);
                for (int i = 0; i < buf.Length; i++) buf[i] += comb[i] * mix / taps.Length;
            }
        }

        static Raw Stamp()
        {
            int n = (int)(Rate * 1.6f);
            var b = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(130f, 48f, Mathf.Clamp01(t / 0.18f));
                float thump = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 11f);
                lp += (Noise() - lp) * 0.25f;
                float slap = lp * Mathf.Exp(-t * 45f) * 0.9f;              // 종이에 닿는 탁
                float wood = Mathf.Sin(2f * Mathf.PI * 310f * t) * Mathf.Exp(-t * 30f) * 0.25f;   // 나무 도장 울림
                b[i] = thump + slap + wood;
            }
            Room(b, 0.35f, 1.0f);
            return Clip("intro_stamp", b, (float[])b.Clone(), 0.9f);
        }

        static Raw Chime()
        {
            int n = (int)(Rate * 3.2f);
            var l = new float[n];
            var r = new float[n];
            float[] freqs = { 1046.5f, 1568f, 2093f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float f = freqs[k];
                    float mod = Mathf.Sin(2f * Mathf.PI * f * 1.41f * t) * 1.2f * Mathf.Exp(-t * 3f);
                    s += Mathf.Sin(2f * Mathf.PI * f * t + mod) * Mathf.Exp(-t * (1.6f + k * 1.2f)) / (1 + k);
                }
                float attack = Mathf.Clamp01(t / 0.004f);
                l[i] = s * attack;
                r[i] = s * attack;
            }
            Echo(l, r, 0.31f, 0.43f, 0.35f, 0.3f);
            return Clip("intro_chime", l, r, 0.55f);
        }

        static Raw Brush()
        {
            int n = (int)(Rate * 1.7f);
            var b = new float[n];
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.6f)) * (0.7f + 0.3f * Mathf.Sin(t * 23f));
                float x = Noise();
                lp1 += (x - lp1) * 0.35f;
                lp2 += (lp1 - lp2) * 0.35f;
                b[i] = (lp1 - lp2 * 0.8f) * env;   // 띠 통과: 사아악
            }
            return Clip("intro_brush", b, (float[])b.Clone(), 0.5f);
        }

        /// <summary>징: 비조화 배음이 길게 울리고 천천히 맥놀이한다.</summary>
        static Raw Gong()
        {
            int n = (int)(Rate * 6f);
            var l = new float[n];
            var r = new float[n];
            float f0 = 98f;
            float[] ratio = { 1f, 1.52f, 2.03f, 2.61f, 3.37f, 4.18f };
            float[] amp = { 1f, 0.6f, 0.5f, 0.32f, 0.2f, 0.12f };
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < ratio.Length; k++)
                {
                    float f = f0 * ratio[k] * (1f + 0.004f * Mathf.Exp(-t * 2f));   // 치는 순간 살짝 높다가 내려앉음
                    s += Mathf.Sin(2f * Mathf.PI * f * t + k) * amp[k] * Mathf.Exp(-t * (0.55f + k * 0.35f));
                }
                s *= 1f + 0.25f * Mathf.Sin(2f * Mathf.PI * 2.1f * t);    // 맥놀이 (웅~웅~)
                lp += (Noise() - lp) * 0.08f;
                float hit = lp * Mathf.Exp(-t * 25f) * 2f;               // 채가 닿는 소리
                float attack = Mathf.Clamp01(t / 0.01f);
                l[i] = (s + hit) * attack;
                r[i] = (s * 0.95f + hit) * attack;
            }
            Echo(l, r, 0.41f, 0.57f, 0.25f, 0.25f);
            return Clip("intro_gong", l, r, 0.8f);
        }

        /// <summary>핑퐁 메아리 (왼쪽 dl초, 오른쪽 dr초).</summary>
        static void Echo(float[] l, float[] r, float dl, float dr, float feedback, float wet)
        {
            int a = (int)(dl * Rate), b = (int)(dr * Rate);
            var el = new float[l.Length];
            var er = new float[r.Length];
            for (int i = 0; i < l.Length; i++)
            {
                el[i] = (i >= a ? (r[i - a] + er[i - a] * feedback) : 0f);
                er[i] = (i >= b ? (l[i - b] + el[i - b] * feedback) : 0f);
            }
            for (int i = 0; i < l.Length; i++)
            {
                l[i] += el[i] * wet;
                r[i] += er[i] * wet;
            }
        }

        /// <summary>Karplus-Strong 뜯는 현: 비단 줄처럼 부드럽게 (시작 잡음을 낮게 거른다). pan -1~1.</summary>
        static void Pluck(float[] l, float[] r, float start, int midi, float vel, float pan, float sustain = 0.996f)
        {
            float f = Midi(midi);
            int len = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
            var buf = new float[len];
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                lp += (Noise() - lp) * 0.55f;
                buf[i] = lp;
            }
            int s0 = (int)(start * Rate);
            int dur = Mathf.Min(l.Length - s0, (int)(Rate * 4.5f));
            float gl = vel * Mathf.Sqrt(0.5f * (1 - pan)), gr = vel * Mathf.Sqrt(0.5f * (1 + pan));
            int idx = 0;
            // 낮은 음은 덜 빨리 줄어든다
            float decay = Mathf.Lerp(sustain, 0.9993f, Mathf.Clamp01((72 - midi) / 24f));
            for (int i = 0; i < dur; i++)
            {
                int next = (idx + 1) % len;
                float y = buf[idx];
                buf[idx] = decay * 0.5f * (buf[idx] + buf[next]);
                idx = next;
                float t = i / (float)Rate;
                float body = y * (1f - Mathf.Exp(-t * 400f));       // 뜯는 순간 딱딱함을 조금 줄인다
                l[s0 + i] += body * gl;
                r[s0 + i] += body * gr;
            }
        }

        /// <summary>타이틀 음악 (36초, 반복). 가야금 선율 + 낮은 울림 + 바람·물결.</summary>
        static Raw Theme()
        {
            const float Len = 36f;
            int n = (int)(Rate * Len);
            var l = new float[n];
            var r = new float[n];

            // (시간, 음, 세기) — 평조 G A C D E. 꾸밈음은 바로 앞에 짧게
            var notes = new (float t, int m, float v)[]
            {
                (0.4f, 55, 0.6f), (0.55f, 62, 0.5f), (0.7f, 67, 0.55f),
                (1.6f, 74, 0.8f), (2.2f, 76, 0.7f), (2.75f, 74, 0.6f), (3.5f, 72, 0.7f), (4.1f, 69, 0.6f), (5.0f, 67, 0.9f),
                (7.2f, 62, 0.5f), (7.7f, 64, 0.5f),
                (9.0f, 55, 0.6f), (9.0f, 67, 0.7f), (9.5f, 69, 0.6f), (10.0f, 72, 0.7f), (10.8f, 74, 0.75f), (11.6f, 76, 0.8f),
                (12.35f, 79, 0.9f), (13.2f, 76, 0.65f), (13.8f, 74, 0.6f), (14.85f, 74, 0.35f), (15.0f, 76, 0.75f), (16.0f, 74, 0.85f),
                (18.0f, 57, 0.5f), (18.0f, 69, 0.7f), (18.6f, 72, 0.6f), (19.2f, 69, 0.6f), (20.0f, 67, 0.75f), (20.8f, 64, 0.65f),
                (21.6f, 62, 0.7f), (22.75f, 62, 0.3f), (22.85f, 64, 0.6f), (23.5f, 67, 0.9f),
                (26.5f, 55, 0.7f), (26.5f, 62, 0.55f), (27.4f, 67, 0.6f), (28.2f, 69, 0.5f), (29.4f, 74, 0.55f), (30.6f, 72, 0.45f),
                (31.4f, 67, 0.8f), (31.4f, 55, 0.5f),
            };
            var pl = new float[n];
            var pr = new float[n];
            foreach (var (t, m, v) in notes)
                Pluck(pl, pr, t, m, v, Mathf.Clamp((m - 66) / 14f, -0.6f, 0.6f));
            Echo(pl, pr, 0.37f, 0.53f, 0.38f, 0.32f);

            // 낮은 울림 (G2·D3, 천천히 들어오고 끝에서 빠진다 → 반복 이음매가 자연스럽다)
            float lp = 0f, lp2 = 0f, wind = 0f;
            float nextLap = 1.3f, lapT = -10f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 4f) * Mathf.Clamp01((Len - t) / 4f);
                float drone = (Mathf.Sin(2f * Mathf.PI * 98f * t) + Mathf.Sin(2f * Mathf.PI * 98.4f * t) * 0.8f
                             + Mathf.Sin(2f * Mathf.PI * 146.8f * t) * 0.45f) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.07f * t));
                // 바람: 천천히 세졌다 약해지는 잡음
                float x = Noise();
                float cut = 0.02f + 0.03f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.09f * t + 1f));
                lp += (x - lp) * cut;
                lp2 += (lp - lp2) * cut;
                wind = lp2 * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.05f * t)) * env;
                // 물결: 뱃전에 찰싹 (가끔)
                if (t >= nextLap) { lapT = t; nextLap = t + 1.6f + (float)_rng.NextDouble() * 2.4f; }
                float lt = t - lapT;
                float lap = lt >= 0f && lt < 0.6f ? lp * Mathf.Sin(Mathf.PI * lt / 0.6f) * 0.8f : 0f;
                l[i] = pl[i] + drone * 0.035f * env + wind * 0.9f + lap * 0.5f;
                r[i] = pr[i] + drone * 0.035f * env + wind * 0.8f + lap * 0.45f;
            }
            return Clip("intro_theme", l, r, 0.75f);
        }
    }
}
