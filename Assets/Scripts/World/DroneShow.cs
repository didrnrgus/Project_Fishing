using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 밤하늘 드론쇼 (탄천종합운동장 위, 사용자 요청). 드론 수백 대가 강 쪽을 향한 세로 판에 모양을 그리고, 몇 초마다 다음 모양으로 날아가 바뀐다:
    /// 잉어 → 벚꽃 → 하트 → 태극 → 버들치 떼 → (다시 잉어). 드론 하나 = 파티클 하나 (Fishing/BioGlow 더하기 빛 점), 위치는 코드가 매 프레임 정한다.
    /// 밤(<c>_FishingNight</c>)에만 보이고, 날이 밝으면 아래로 내려앉아 꺼진다. 모양이 바뀔 땐 드론마다 조금씩 늦게 출발해 물결처럼 흐른다.
    /// </summary>
    public class DroneShow : MonoBehaviour
    {
        const int N = 420;
        const float Hold = 15f, Move = 4f;
        public float Width = 130f;
        public float Height = 112f;

        ParticleSystem _ps;
        ParticleSystem.Particle[] _parts;
        readonly List<(Vector2[] pts, Color[] cols)> _shapes = new List<(Vector2[], Color[])>();
        Vector3[] _from, _to, _cur;
        Color[] _colFrom, _colTo;
        float[] _delay;
        int _shape;
        float _t;
        float _on;

        public static DroneShow Create(Transform parent, Vector3 center, float yaw)
        {
            var go = new GameObject("DroneShow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.AddComponent<DroneShow>();
        }

        void Awake()
        {
            var rng = new System.Random(2026);
            BuildShapes(rng);
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = N;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 1e6f;
            main.startSpeed = 0f;
            var em = _ps.emission;
            em.enabled = false;
            var r = GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Fishing/BioGlow");
            r.sharedMaterial = shader != null ? new Material(shader) { mainTexture = ProceduralTextures.Star } : Fishing.Core.Mats.Particle(Color.white);
            if (shader != null) r.sharedMaterial.SetFloat("_Boost", 6f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _parts = new ParticleSystem.Particle[N];
            _from = new Vector3[N]; _to = new Vector3[N]; _cur = new Vector3[N];
            _colFrom = new Color[N]; _colTo = new Color[N];
            _delay = new float[N];
            for (int i = 0; i < N; i++)
            {
                _cur[i] = Ground(i);
                _delay[i] = (float)rng.NextDouble() * 1.2f;
            }
            SetTarget(0);
            for (int i = 0; i < N; i++) { _from[i] = _cur[i]; _colFrom[i] = _colTo[i]; }
            _ps.Emit(N);
            _ps.GetParticles(_parts);
            Apply(0f);
        }

        /// <summary>대기 자리: 판 아래 땅 근처 격자 (날이 밝으면 여기로 내려앉는다).</summary>
        Vector3 Ground(int i) => new Vector3((i % 30 - 15) * 2.2f, -Height + 2f, (i / 30 - 7) * 2.2f);

        void SetTarget(int s)
        {
            var (pts, cols) = _shapes[s % _shapes.Count];
            for (int i = 0; i < N; i++)
            {
                var p = pts[i];
                _to[i] = new Vector3(p.x * Width * 0.5f, p.y * Width * 0.5f, 0f);
                _colTo[i] = cols[i];
            }
        }

        void Update()
        {
            float night = Shader.GetGlobalFloat("_FishingNight");
            bool wasOff = _on <= 0.01f;
            _on = Mathf.MoveTowards(_on, night > 0.6f ? 1f : 0f, Time.deltaTime * 0.5f);
            var r = GetComponent<ParticleSystemRenderer>();
            r.enabled = _on > 0.01f;
            if (!r.enabled) return;
            if (wasOff)
            {
                // 켜질 때는 지금 모양을 갖춘 채 땅에서 떠오른다 (_on으로 대기 자리 → 판)
                _t = Move;
                for (int i = 0; i < N; i++) { _from[i] = _to[i]; _colFrom[i] = _colTo[i]; }
            }
            _t += Time.deltaTime;
            if (_t > Hold + Move)
            {
                _t = 0f;
                for (int i = 0; i < N; i++) { _from[i] = _cur[i]; _colFrom[i] = _colTo[i]; }
                _shape++;
                SetTarget(_shape);
            }
            Apply(_t);
        }

        void Apply(float t)
        {
            float flick = Time.time;
            for (int i = 0; i < N; i++)
            {
                float k = Mathf.Clamp01((t - _delay[i]) / (Move - 1.2f));
                k = k * k * (3f - 2f * k);
                // 옮겨 갈 때 살짝 앞으로(강 쪽) 부풀었다 돌아온다
                var p = Vector3.Lerp(_from[i], _to[i], k) + Vector3.back * Mathf.Sin(k * Mathf.PI) * 6f;
                // 떠 있는 드론의 미세한 흔들림
                p += new Vector3(Mathf.Sin(flick * 0.9f + i), Mathf.Sin(flick * 1.3f + i * 1.7f), 0f) * 0.15f;
                _cur[i] = p;
                var shown = Vector3.Lerp(Ground(i), p, _on);
                var c = Color.Lerp(_colFrom[i], _colTo[i], k);
                c.a = Mathf.Clamp01(_on * (0.85f + 0.15f * Mathf.Sin(flick * 3f + i * 0.37f)));
                _parts[i].position = shown;
                _parts[i].startColor = c;
                _parts[i].startSize = 2.6f;
                _parts[i].remainingLifetime = 1e5f;
                _parts[i].startLifetime = 1e6f;
            }
            _ps.SetParticles(_parts, N);
        }

        // ---------------------------------------------------------------- 모양 (판 좌표 -1~1, 위가 +y)

        void BuildShapes(System.Random rng)
        {
            float Rf() => (float)rng.NextDouble();
            // 잉어: 유선형 몸 윤곽 + 몸 속 비늘 점 + 갈라진 꼬리 + 등지느러미 + 눈 (주황·금빛)
            {
                var pts = new List<Vector2>();
                var cols = new List<Color>();
                var gold = new Color(1f, 0.62f, 0.15f);
                var light = new Color(1f, 0.85f, 0.45f);
                for (int i = 0; i < 150; i++)
                {
                    float a = i / 150f * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * 0.6f - 0.1f, y = Mathf.Sin(a) * 0.26f * (1f - 0.35f * Mathf.Max(0f, Mathf.Cos(a)));
                    pts.Add(new Vector2(x, y)); cols.Add(gold);
                }
                for (int i = 0; i < 70; i++)
                {
                    float t = i / 70f;
                    float side = i % 2 == 0 ? 1f : -1f;
                    pts.Add(new Vector2(-0.7f - t * 0.28f, side * t * 0.32f)); cols.Add(gold);
                }
                for (int i = 0; i < 30; i++) { float t = i / 30f; pts.Add(new Vector2(-0.98f, Mathf.Lerp(-0.32f, 0.32f, t))); cols.Add(gold); }
                for (int i = 0; i < 40; i++) { float t = i / 40f; pts.Add(new Vector2(Mathf.Lerp(0.05f, -0.4f, t), 0.25f + Mathf.Sin(t * Mathf.PI) * 0.16f)); cols.Add(light); }
                while (pts.Count < N - 6)
                {
                    var p = new Vector2(Rf() * 1.1f - 0.65f, Rf() * 0.4f - 0.2f);
                    float ex = (p.x + 0.1f) / 0.55f, ey = p.y / 0.22f;
                    if (ex * ex + ey * ey < 1f) { pts.Add(p); cols.Add(Color.Lerp(gold, light, Rf())); }
                }
                for (int i = 0; i < 6; i++) { pts.Add(new Vector2(0.33f + (i % 3) * 0.015f, 0.06f + (i / 3) * 0.015f)); cols.Add(Color.white); }
                _shapes.Add((pts.ToArray(), cols.ToArray()));
            }
            // 벚꽃: 끝이 오목한 꽃잎 다섯 장 + 노란 꽃술
            {
                var pts = new Vector2[N];
                var cols = new Color[N];
                for (int i = 0; i < N; i++)
                {
                    if (i < 40)
                    {
                        float a = Rf() * Mathf.PI * 2f, r = Mathf.Sqrt(Rf()) * 0.12f;
                        pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        cols[i] = new Color(1f, 0.9f, 0.35f);
                        continue;
                    }
                    float th = i / (float)(N - 40) * Mathf.PI * 2f;
                    float petal = Mathf.Abs(Mathf.Cos(th * 2.5f));
                    float notch = 1f - 0.15f * Mathf.Exp(-Mathf.Pow(Mathf.Repeat(th * 5f / (2f * Mathf.PI), 1f) - 0.5f, 2f) * 200f);
                    float rr = (0.35f + 0.6f * petal) * notch * (i % 3 == 0 ? Rf() * 0.9f + 0.1f : 1f);
                    pts[i] = new Vector2(Mathf.Cos(th + Mathf.PI * 0.5f), Mathf.Sin(th + Mathf.PI * 0.5f)) * rr;
                    cols[i] = Color.Lerp(new Color(1f, 0.55f, 0.75f), new Color(1f, 0.9f, 0.95f), rr);
                }
                _shapes.Add((pts, cols));
            }
            // 하트
            {
                var pts = new Vector2[N];
                var cols = new Color[N];
                for (int i = 0; i < N; i++)
                {
                    float t = i / (float)N * Mathf.PI * 2f;
                    float s = i % 4 == 0 ? Rf() * 0.9f + 0.05f : 1f;
                    float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                    float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                    pts[i] = new Vector2(x, y + 2f) / 18f * s;
                    cols[i] = Color.Lerp(new Color(1f, 0.15f, 0.3f), new Color(1f, 0.55f, 0.7f), s);
                }
                _shapes.Add((pts, cols));
            }
            // 태극: 원 안을 S자로 나눠 위 빨강·아래 파랑
            {
                var pts = new Vector2[N];
                var cols = new Color[N];
                for (int i = 0; i < N; i++)
                {
                    Vector2 p;
                    if (i < 120) { float a = i / 120f * Mathf.PI * 2f; p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.8f; }
                    else { float a = Rf() * Mathf.PI * 2f, r = Mathf.Sqrt(Rf()) * 0.78f; p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
                    // 태극 경계: 위 반원 + 오른쪽(빨강이 왼쪽 위로 휘감는) 작은 원 둘
                    bool red = p.y > 0f;
                    if (new Vector2(p.x - 0.4f, p.y).magnitude < 0.4f) red = false;
                    if (new Vector2(p.x + 0.4f, p.y).magnitude < 0.4f) red = true;
                    pts[i] = p;
                    cols[i] = red ? new Color(1f, 0.15f, 0.2f) : new Color(0.15f, 0.35f, 1f);
                }
                _shapes.Add((pts, cols));
            }
            // 버들치 떼: 작은 물고기 열두 마리가 비스듬히 무리 지어 헤엄친다 (은빛·하늘빛)
            {
                var pts = new Vector2[N];
                var cols = new Color[N];
                var centers = new Vector2[12];
                for (int f = 0; f < 12; f++) centers[f] = new Vector2(Rf() * 1.5f - 0.75f, (f / 12f) * 1.2f - 0.6f + (Rf() - 0.5f) * 0.15f);
                for (int i = 0; i < N; i++)
                {
                    var c = centers[i % 12];
                    int k = i / 12;
                    float a = k / (N / 12f) * Mathf.PI * 2f;
                    Vector2 p = k < 26 ? new Vector2(Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.045f) : new Vector2(-0.13f - (k - 26) * 0.012f, ((k % 2) * 2 - 1) * (k - 26) * 0.012f);
                    pts[i] = c + p;
                    cols[i] = Color.Lerp(new Color(0.75f, 0.9f, 1f), new Color(0.4f, 0.75f, 1f), Rf());
                }
                _shapes.Add((pts, cols));
            }
        }
    }
}
