using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 밤하늘 드론 라이트쇼 (탄천종합운동장 위, 사용자 참고: 성남페스티벌 드론쇼). 드론 = 3D 공간의 빛 점 하나 (파티클 하나, Fishing/BioGlow 더하기 빛).
    /// 쇼는 "어디에 있나 · 켜졌나 · 무슨 색인가" 세 가지로만 그린다:
    ///  - 장면(<see cref="DroneArt"/>)마다 점 목록(위치·색·반짝임)이 있고, 윤곽은 촘촘한 점선, 면은 성긴 점 격자. 점보다 남는 드론은 불을 끄고 그 자리에 떠 있는다
    ///  - 한 장면을 <see cref="Hold"/>초 보여 준 뒤 <see cref="Move"/>초 동안 바뀐다 (트랜지션): 드론마다 새 장면에서 가장 가까운 빈 점으로 날아가고
    ///    (가까운 것부터 짝지어 무리가 흩어지지 않게), 왼쪽에서 오른쪽으로 물결처럼 차례로 출발하고, 3D로 앞뒤·위로 부푼 곡선을 그리며, 날아가는 동안 빛이 조금 어두워졌다
    ///    새 색으로 켜진다. 새로 쓰이는 드론은 꺼진 자리에서 켜지며 오고, 안 쓰이는 드론은 날아가며 꺼진다
    /// 밤(<c>_FishingNight</c>)에만 보이고, 날이 밝으면 아래 대기 자리로 내려앉아 꺼진다.
    /// 레퍼런스 사진의 넥슨·디즈니 캐릭터는 저작권 때문에 쓰지 않고, 구도만 따온 오리지널 장면이다 (공개 저장소, 17 문서).
    /// </summary>
    public class DroneShow : MonoBehaviour
    {
        public const int N = 1800;
        public const float Hold = 10f, Move = 5f;
        /// <summary>판 폭 (m). 장면 좌표 1 = 폭의 절반.</summary>
        public float Width = 150f;
        /// <summary>판 가운데가 대기 자리(땅)보다 높은 정도 (m).</summary>
        public float Height = 76f;
        public static DroneShow Instance { get; private set; }
        public int SceneCount => _scenes.Count;

        ParticleSystem _ps;
        ParticleSystemRenderer _renderer;
        ParticleSystem.Particle[] _parts;
        List<DroneArt> _scenes;
        Vector3[] _from, _to, _cur, _arc;
        Color[] _cFrom, _cTo, _cCur;
        float[] _delay, _twTo;
        int _scene;
        float _t, _on;

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
            Instance = this;
            _scenes = DroneArt.Scenes();
            for (int i = 0; i < _scenes.Count; i++) Debug.Log($"[DroneShow] 장면 {i} {_scenes[i].Name}: 점 {_scenes[i].Count}개");
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
            _renderer = GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Fishing/BioGlow");
            _renderer.sharedMaterial = shader != null ? new Material(shader) { mainTexture = ProceduralTextures.Star } : Fishing.Core.Mats.Particle(Color.white);
            if (shader != null) _renderer.sharedMaterial.SetFloat("_Boost", 4.5f);
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            _parts = new ParticleSystem.Particle[N];
            _from = new Vector3[N]; _to = new Vector3[N]; _cur = new Vector3[N]; _arc = new Vector3[N];
            _cFrom = new Color[N]; _cTo = new Color[N]; _cCur = new Color[N];
            _delay = new float[N]; _twTo = new float[N];
            var rng = new System.Random(2026);
            for (int i = 0; i < N; i++)
            {
                _cur[i] = Ground(i);
                _cCur[i] = Color.clear;
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                _arc[i] = new Vector3(((float)rng.NextDouble() - 0.5f) * 6f, 3f + (float)rng.NextDouble() * 5f, side * (6f + (float)rng.NextDouble() * 10f));
            }
            Jump(0);
            _ps.Emit(N);
            _ps.GetParticles(_parts);
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>대기 자리: 판 아래 땅 근처 격자 (날이 밝으면 여기로 내려앉는다).</summary>
        Vector3 Ground(int i) => new Vector3((i % 45 - 22) * 2f, -Height + 1.5f, (i / 45 - 20) * 2f);

        /// <summary>
        /// 장면 s로 짝짓기: 새 장면의 점을 무작위 순서로 돌며 아직 짝 없는 드론 중 지금 가장 가까운 것을 고른다.
        /// 짝 없는 드론은 지금 자리에서 불을 끈다. 출발 시각은 목표 x로 왼쪽부터 물결처럼.
        /// </summary>
        void Assign(int s)
        {
            var art = _scenes[s % _scenes.Count];
            float S = Width * 0.5f;
            int m = Mathf.Min(art.Count, N);
            var used = new bool[N];
            var order = new int[m];
            for (int k = 0; k < m; k++) order[k] = k;
            var rng = new System.Random(s * 977 + 3);
            for (int k = m - 1; k > 0; k--) { int j = rng.Next(k + 1); (order[k], order[j]) = (order[j], order[k]); }
            for (int i = 0; i < N; i++) { _from[i] = _cur[i]; _cFrom[i] = _cCur[i]; }
            foreach (int k in order)
            {
                var tp = art.P[k] * S;
                int best = -1;
                float bd = float.MaxValue;
                for (int i = 0; i < N; i++)
                {
                    if (used[i]) continue;
                    float dx = _cur[i].x - tp.x, dy = _cur[i].y - tp.y, dz = _cur[i].z - tp.z;
                    float d = dx * dx + dy * dy + dz * dz;
                    if (d < bd) { bd = d; best = i; }
                }
                used[best] = true;
                _to[best] = tp;
                _cTo[best] = art.C[k];
                _twTo[best] = art.Tw[k];
                _delay[best] = (art.P[k].x + 1f) * 0.75f + (float)rng.NextDouble() * 0.35f;
            }
            for (int i = 0; i < N; i++)
            {
                if (used[i]) continue;
                _to[i] = _cur[i];
                _cTo[i] = new Color(_cCur[i].r, _cCur[i].g, _cCur[i].b, 0f);
                _twTo[i] = 0f;
                _delay[i] = (_cur[i].x / S + 1f) * 0.75f;
            }
        }

        /// <summary>장면 s를 곧바로 다 그린 상태로 (시작할 때, 자동 촬영).</summary>
        public void Jump(int s)
        {
            _scene = s;
            Assign(s);
            for (int i = 0; i < N; i++) { _cur[i] = _from[i] = _to[i]; _cCur[i] = _cFrom[i] = _cTo[i]; }
            _t = Move;
        }

        /// <summary>자동 촬영: 장면 s에서 다음 장면으로 바뀌는 중간 (progress 0~1)에 멈춘 듯 맞춘다.</summary>
        public void JumpMid(int s, float progress)
        {
            Jump(s);
            _scene = s + 1;
            Assign(_scene);
            _t = Move * progress;
            _on = 1f;
        }

        void Update()
        {
            float night = Shader.GetGlobalFloat("_FishingNight");
            _on = Mathf.MoveTowards(_on, night > 0.6f ? 1f : 0f, Time.deltaTime * 0.5f);
            _renderer.enabled = _on > 0.01f;
            if (!_renderer.enabled) return;
            _t += Time.deltaTime;
            if (_t > Move + Hold)
            {
                _t = 0f;
                _scene++;
                Assign(_scene);
            }
            Apply();
        }

        void Apply()
        {
            float now = Time.time;
            float span = Move - 2f;   // 한 드론이 날아가는 시간 (출발은 0~1.85초 사이로 물결)
            for (int i = 0; i < N; i++)
            {
                float k = Mathf.Clamp01((_t - _delay[i]) / span);
                float e = k * k * (3f - 2f * k);
                float bump = Mathf.Sin(e * Mathf.PI);
                var p = Vector3.Lerp(_from[i], _to[i], e) + _arc[i] * bump;
                // 떠 있는 드론의 미세한 흔들림
                p += new Vector3(Mathf.Sin(now * 0.9f + i), Mathf.Sin(now * 1.3f + i * 1.7f), 0f) * 0.08f;
                _cur[i] = p;
                // 날아가는 동안은 조금 어둡게, 새 색으로 바뀐다
                var c = Color.Lerp(_cFrom[i], _cTo[i], Mathf.Clamp01(e * 1.4f - 0.2f));
                float dim = 1f - 0.45f * bump;
                if (_twTo[i] > 0f && k >= 1f) dim *= 0.55f + 0.45f * Mathf.Sin(now * 4.2f + i * 2.3f);
                _cCur[i] = c;
                var shown = Vector3.Lerp(Ground(i), p, _on);
                var col = new Color(c.r * dim, c.g * dim, c.b * dim, c.a * _on);
                _parts[i].position = shown;
                _parts[i].startColor = col;
                _parts[i].startSize = 1.15f;
                _parts[i].remainingLifetime = 1e5f;
                _parts[i].startLifetime = 1e6f;
            }
            _ps.SetParticles(_parts, N);
        }
    }

    /// <summary>
    /// 드론쇼 한 장면: 점(판 좌표, x -1~1 · y 위가 +, z 앞뒤 깊이) · 색 · 반짝임. 선은 일정한 간격(<see cref="Sp"/>)으로 점을 찍고, 면은 엇갈린 격자.
    /// 너무 가까운 점은 하나만 남긴다 (겹치면 빛이 두 배로 밝아진다).
    /// </summary>
    public sealed class DroneArt
    {
        const float Sp = 0.017f;
        public readonly string Name;
        public readonly List<Vector3> P = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<float> Tw = new List<float>();
        readonly HashSet<long> _cells = new HashSet<long>();
        float _rot, _scale = 1f, _z;
        Vector2 _off;
        public int Count => P.Count;

        DroneArt(string name) { Name = name; }

        // ---------------- 붓

        void Add(Vector2 p, Color c, float tw = 0f)
        {
            p = _off + (Vector2)(Quaternion.Euler(0f, 0f, _rot) * (p * _scale));
            const float cell = Sp * 0.72f;
            long key = ((long)Mathf.FloorToInt(p.x / cell) << 32) | (uint)Mathf.FloorToInt(p.y / cell);
            if (!_cells.Add(key)) return;
            P.Add(new Vector3(p.x, p.y, _z));
            C.Add(c);
            Tw.Add(tw);
        }

        void Curve(Func<float, Vector2> f, Color c, float tw = 0f, float sp = Sp)
        {
            var prev = f(0f);
            Add(prev, c, tw);
            float acc = 0f;
            const int steps = 700;
            for (int i = 1; i <= steps; i++)
            {
                var q = f(i / (float)steps);
                acc += Vector2.Distance(q, prev);
                if (acc >= sp / Mathf.Max(0.2f, _scale)) { Add(q, c, tw); acc = 0f; }
                prev = q;
            }
        }

        void Line(Vector2 a, Vector2 b, Color c, float tw = 0f) => Curve(t => Vector2.Lerp(a, b, t), c, tw);

        void Poly(Color c, bool closed, params Vector2[] pts)
        {
            for (int i = 0; i + 1 < pts.Length; i++) Line(pts[i], pts[i + 1], c);
            if (closed) Line(pts[pts.Length - 1], pts[0], c);
        }

        void Arc(Vector2 o, float rx, float ry, float a0, float a1, Color c, float tw = 0f) =>
            Curve(t => { float a = Mathf.Lerp(a0, a1, t) * Mathf.Deg2Rad; return o + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); }, c, tw);

        void Circle(Vector2 o, float r, Color c, float tw = 0f) => Arc(o, r, r, 0f, 360f, c, tw);
        void Ellipse(Vector2 o, float rx, float ry, Color c, float tw = 0f) => Arc(o, rx, ry, 0f, 360f, c, tw);

        void RoundRect(Vector2 o, float hx, float hy, float r, Color c)
        {
            Line(o + new Vector2(-hx + r, hy), o + new Vector2(hx - r, hy), c);
            Line(o + new Vector2(-hx + r, -hy), o + new Vector2(hx - r, -hy), c);
            Line(o + new Vector2(-hx, -hy + r), o + new Vector2(-hx, hy - r), c);
            Line(o + new Vector2(hx, -hy + r), o + new Vector2(hx, hy - r), c);
            Arc(o + new Vector2(hx - r, hy - r), r, r, 0f, 90f, c);
            Arc(o + new Vector2(-hx + r, hy - r), r, r, 90f, 180f, c);
            Arc(o + new Vector2(-hx + r, -hy + r), r, r, 180f, 270f, c);
            Arc(o + new Vector2(hx - r, -hy + r), r, r, 270f, 360f, c);
        }

        /// <summary>면 채우기: bbox 안 엇갈린 격자 중 inside가 참인 곳 (간격 = Sp × gap). 색은 위치로.</summary>
        void Fill(Vector2 min, Vector2 max, Func<Vector2, bool> inside, Func<Vector2, Color> col, float gap = 2.3f, float tw = 0f)
        {
            float s = Sp * gap / Mathf.Max(0.2f, _scale);
            int row = 0;
            for (float y = min.y; y <= max.y; y += s * 0.87f, row++)
                for (float x = min.x + (row % 2) * s * 0.5f; x <= max.x; x += s)
                {
                    var p = new Vector2(x, y);
                    if (inside(p)) Add(p, col(p), tw);
                }
        }

        void FillEllipse(Vector2 o, float rx, float ry, Func<Vector2, Color> col, float gap = 2.3f) =>
            Fill(o - new Vector2(rx, ry), o + new Vector2(rx, ry),
                p => { var d = new Vector2((p.x - o.x) / (rx - Sp), (p.y - o.y) / (ry - Sp)); return d.sqrMagnitude < 1f; }, col, gap);

        void FillPoly(Func<Vector2, Color> col, float gap, params Vector2[] poly)
        {
            var min = poly[0];
            var max = poly[0];
            foreach (var q in poly) { min = Vector2.Min(min, q); max = Vector2.Max(max, q); }
            Fill(min, max, p => InPoly(p, poly), col, gap);
        }

        static bool InPoly(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        /// <summary>반짝이 별 (네 갈래 + 짧은 대각선).</summary>
        void Sparkle(Vector2 o, float r, Color c)
        {
            Line(o - new Vector2(r, 0f), o + new Vector2(r, 0f), c, 1f);
            Line(o - new Vector2(0f, r), o + new Vector2(0f, r), c, 1f);
            Line(o - new Vector2(r, r) * 0.35f, o + new Vector2(r, r) * 0.35f, c, 1f);
            Line(o - new Vector2(r, -r) * 0.35f, o + new Vector2(r, -r) * 0.35f, c, 1f);
        }

        /// <summary>눈송이: 여섯 가지 + 가지마다 V자 잔가지 둘.</summary>
        void Snowflake(Vector2 o, float r, Color c)
        {
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60f * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Line(o, o + d * r, c, 1f);
                foreach (float f in new[] { 0.45f, 0.75f })
                {
                    var b = o + d * r * f;
                    foreach (float s in new[] { -1f, 1f })
                    {
                        float a2 = a + s * 45f * Mathf.Deg2Rad;
                        Line(b, b + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * r * 0.28f, c, 1f);
                    }
                }
            }
        }

        /// <summary>바퀴: 테 + 바퀴살 넷 + 가운데 점.</summary>
        void Wheel(Vector2 o, float r, Color c)
        {
            Circle(o, r, c);
            Circle(o, r * 0.45f, c);
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                Line(o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.45f, o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c);
            }
        }

        /// <summary>한글 두 글자 "탄천" (획을 선으로). o = 첫 글자 왼쪽 아래, size = 글자 한 칸.</summary>
        void Tancheon(Vector2 o, float size, Color c)
        {
            Vector2 Q(float x, float y, int ch) => o + new Vector2((x + ch * 1.08f) * size, y * size);
            void L(int ch, float x0, float y0, float x1, float y1)
            {
                // 두 줄로 굵게 (획에 수직으로 조금 비켜)
                var d = new Vector2(y1 - y0, x0 - x1).normalized * 0.055f;
                Line(Q(x0, y0, ch), Q(x1, y1, ch), c);
                Line(Q(x0 + d.x, y0 + d.y, ch), Q(x1 + d.x, y1 + d.y, ch), c);
            }
            // 탄
            L(0, 0.05f, 0.95f, 0.5f, 0.95f); L(0, 0.05f, 0.95f, 0.05f, 0.5f); L(0, 0.05f, 0.72f, 0.45f, 0.72f); L(0, 0.05f, 0.5f, 0.5f, 0.5f);
            L(0, 0.7f, 1.0f, 0.7f, 0.47f); L(0, 0.7f, 0.72f, 0.9f, 0.72f);
            L(0, 0.15f, 0.4f, 0.15f, 0.1f); L(0, 0.15f, 0.1f, 0.92f, 0.1f);
            // 천
            L(1, 0.27f, 1.0f, 0.27f, 0.9f); L(1, 0.06f, 0.88f, 0.48f, 0.88f); L(1, 0.27f, 0.88f, 0.04f, 0.48f); L(1, 0.27f, 0.8f, 0.5f, 0.5f);
            L(1, 0.76f, 1.0f, 0.76f, 0.47f); L(1, 0.56f, 0.72f, 0.76f, 0.72f);
            L(1, 0.15f, 0.4f, 0.15f, 0.1f); L(1, 0.15f, 0.1f, 0.92f, 0.1f);
        }

        // ---------------- 색

        static readonly Color White = new Color(0.95f, 0.95f, 1f), Pink = new Color(1f, 0.35f, 0.72f), Magenta = new Color(0.92f, 0.3f, 0.95f),
            Blue = new Color(0.25f, 0.45f, 1f), Sky = new Color(0.45f, 0.8f, 1f), Cyan = new Color(0.3f, 0.95f, 1f), Orange = new Color(1f, 0.55f, 0.15f),
            Red = new Color(1f, 0.22f, 0.2f), Gold = new Color(1f, 0.78f, 0.3f), Purple = new Color(0.62f, 0.35f, 1f), Green = new Color(0.35f, 1f, 0.5f),
            Peach = new Color(1f, 0.68f, 0.55f), Lilac = new Color(0.85f, 0.8f, 1f);

        static Func<Vector2, Color> Solid(Color c) => _ => c;

        // ---------------- 장면

        public static List<DroneArt> Scenes() => new List<DroneArt> { Racers(), Ufo(), Snowman(), Astronaut(), Angler(), Carp() };

        /// <summary>① 액자 속 카트 레이서 (참고: 2025 성남 넥슨 드론쇼의 액자 구도): 파란 수달과 주황 잉어가 카트를 몬다.</summary>
        static DroneArt Racers()
        {
            var a = new DroneArt("카트 레이서");
            a.RoundRect(new Vector2(-0.08f, 0f), 0.84f, 0.44f, 0.12f, Magenta);
            a.Circle(new Vector2(0.88f, -0.3f), 0.07f, Magenta);
            a.Line(new Vector2(0.76f, -0.18f), new Vector2(0.86f, -0.08f), Magenta);
            // 수달: 둥근 머리·귀·흰 주둥이·윙크한 눈·분홍 코·수염
            var h = new Vector2(-0.45f, 0.12f);
            a.Circle(h, 0.2f, Blue);
            a.Circle(h + new Vector2(-0.17f, 0.15f), 0.055f, Blue);
            a.Circle(h + new Vector2(0.17f, 0.15f), 0.055f, Blue);
            a.Ellipse(h + new Vector2(0f, -0.07f), 0.11f, 0.075f, White);
            a.FillEllipse(h + new Vector2(0f, -0.03f), 0.028f, 0.02f, Solid(Pink), 1.4f);
            a.Arc(h + new Vector2(-0.07f, 0.05f), 0.045f, 0.03f, 20f, 160f, White);
            a.FillEllipse(h + new Vector2(0.07f, 0.05f), 0.028f, 0.03f, Solid(White), 1.4f);
            a.Arc(h + new Vector2(0f, -0.09f), 0.04f, 0.03f, 200f, 340f, Pink);
            foreach (float s in new[] { -1f, 1f })
            {
                a.Line(h + new Vector2(s * 0.12f, -0.05f), h + new Vector2(s * 0.26f, -0.02f), White);
                a.Line(h + new Vector2(s * 0.12f, -0.08f), h + new Vector2(s * 0.26f, -0.1f), White);
            }
            // 파란 카트
            var kart = new[] { new Vector2(-0.74f, -0.12f), new Vector2(-0.24f, -0.12f), new Vector2(-0.12f, -0.2f), new Vector2(-0.14f, -0.3f), new Vector2(-0.76f, -0.3f) };
            a.Poly(Blue, true, kart);
            a.FillPoly(p => Color.Lerp(Blue, Sky, (p.y + 0.3f) * 4f), 2.3f, kart);
            a.Wheel(new Vector2(-0.66f, -0.32f), 0.075f, White);
            a.Wheel(new Vector2(-0.22f, -0.32f), 0.075f, White);
            a.Arc(new Vector2(-0.3f, -0.06f), 0.05f, 0.05f, 0f, 360f, White);
            // 잉어: 몸·꼬리·지느러미·눈·비늘
            var f = new Vector2(0.34f, 0.06f);
            a.Ellipse(f, 0.18f, 0.1f, Orange);
            a.Poly(Orange, true, f + new Vector2(-0.17f, 0f), f + new Vector2(-0.3f, 0.09f), f + new Vector2(-0.27f, 0f), f + new Vector2(-0.3f, -0.09f));
            a.Arc(f + new Vector2(-0.02f, 0.08f), 0.07f, 0.06f, 20f, 160f, Gold);
            a.FillEllipse(f + new Vector2(0.11f, 0.025f), 0.022f, 0.022f, Solid(White), 1.4f);
            a.FillEllipse(f + new Vector2(-0.02f, -0.01f), 0.12f, 0.065f, p => Color.Lerp(Orange, Gold, (p.y - f.y + 0.06f) * 6f), 2.2f);
            a.Arc(f + new Vector2(0.15f, -0.03f), 0.03f, 0.02f, 200f, 300f, Pink);
            // 분홍 카트
            var kart2 = new[] { new Vector2(0.1f, -0.1f), new Vector2(0.56f, -0.1f), new Vector2(0.64f, -0.2f), new Vector2(0.58f, -0.3f), new Vector2(0.1f, -0.3f) };
            a.Poly(Pink, true, kart2);
            a.FillPoly(Solid(Pink * 0.85f), 2.5f, kart2);
            a.Wheel(new Vector2(0.18f, -0.32f), 0.075f, White);
            a.Wheel(new Vector2(0.52f, -0.32f), 0.075f, White);
            // 속도선
            foreach (float y in new[] { -0.17f, -0.23f })
            {
                a.Line(new Vector2(-0.88f, y), new Vector2(-0.8f, y), White);
                a.Line(new Vector2(-0.05f, y), new Vector2(0.03f, y), White);
            }
            a.Sparkle(new Vector2(0.86f, 0.3f), 0.05f, Gold);
            a.Sparkle(new Vector2(0.62f, 0.32f), 0.03f, Gold);
            return a;
        }

        /// <summary>② UFO와 카트 (참고 사진: 점 격자로 채운 비행접시가 반짝이 빛줄기로 카트를 끌어올린다).</summary>
        static DroneArt Ufo()
        {
            var a = new DroneArt("UFO");
            var o = new Vector2(0f, 0.25f);
            a.Ellipse(o, 0.62f, 0.19f, Sky);
            a.FillEllipse(o, 0.62f, 0.19f, p => Color.Lerp(Blue, new Color(0.75f, 0.9f, 1f), (p.y - o.y + 0.19f) * 2.6f), 2.6f);
            a.Arc(o + new Vector2(0f, -0.04f), 0.44f, 0.22f, 200f, 340f, Sky);
            a.Arc(o + new Vector2(0f, 0.12f), 0.2f, 0.17f, 0f, 180f, White);
            a.FillEllipse(o + new Vector2(0f, 0.17f), 0.17f, 0.11f, Solid(new Color(0.7f, 0.85f, 1f)), 2f);
            foreach (var (x, y) in new[] { (-0.38f, 0.0f), (0.38f, 0.0f), (0f, -0.07f) })
            {
                a.Ellipse(o + new Vector2(x, y), 0.07f, 0.04f, Orange);
                a.FillEllipse(o + new Vector2(x, y), 0.07f, 0.04f, Solid(Gold), 1.4f);
            }
            for (int k = 0; k < 9; k++)
            {
                float x = (k - 4) * 0.13f;
                a.Add(o + new Vector2(x, -0.13f * (1f - Mathf.Abs(x) * 0.8f)), White, 1f);
            }
            // 빛줄기 반짝이
            foreach (var (x, y, r) in new[] { (-0.1f, -0.1f, 0.04f), (0.12f, -0.06f, 0.035f), (0.02f, -0.2f, 0.05f), (-0.17f, -0.32f, 0.035f), (0.18f, -0.3f, 0.04f), (0f, -0.36f, 0.02f) })
                a.Sparkle(new Vector2(x, y), r, Cyan);
            // 아래 카트에 탄 분홍 수달 (선글라스)
            var h = new Vector2(0f, -0.48f);
            a.Circle(h, 0.11f, Pink);
            a.Circle(h + new Vector2(-0.09f, 0.09f), 0.035f, Pink);
            a.Circle(h + new Vector2(0.09f, 0.09f), 0.035f, Pink);
            a.FillEllipse(h + new Vector2(-0.04f, 0.01f), 0.032f, 0.026f, Solid(White), 1.3f);
            a.FillEllipse(h + new Vector2(0.04f, 0.01f), 0.032f, 0.026f, Solid(White), 1.3f);
            a.Arc(h + new Vector2(0f, -0.04f), 0.04f, 0.025f, 200f, 340f, Pink);
            var k2 = new[] { new Vector2(-0.22f, -0.58f), new Vector2(0.2f, -0.58f), new Vector2(0.27f, -0.66f), new Vector2(0.2f, -0.72f), new Vector2(-0.24f, -0.72f) };
            a.Poly(Pink, true, k2);
            a.Wheel(new Vector2(-0.16f, -0.74f), 0.055f, White);
            a.Wheel(new Vector2(0.15f, -0.74f), 0.055f, White);
            return a;
        }

        /// <summary>③ 눈사람과 눈송이 (참고: 강 위 겨울 장면 — 흰 눈사람, 주황 당근 코, 붉은 나뭇가지 팔, 파란 눈송이, 펭귄 친구).</summary>
        static DroneArt Snowman()
        {
            var a = new DroneArt("눈사람");
            var lil = new Color(0.55f, 0.5f, 0.95f);
            foreach (var (y, r) in new[] { (-0.5f, 0.24f), (-0.13f, 0.17f), (0.19f, 0.14f) })
            {
                a.Circle(new Vector2(-0.12f, y), r, White);
                a.FillEllipse(new Vector2(-0.12f, y), r, r, p => Color.Lerp(lil, Lilac, (p.y - y + r) / (2f * r)), 3.2f);
            }
            foreach (float y in new[] { -0.08f, -0.18f, -0.42f })
                a.FillEllipse(new Vector2(-0.12f, y), 0.022f, 0.022f, Solid(Blue), 1.3f);
            a.FillEllipse(new Vector2(-0.17f, 0.23f), 0.02f, 0.025f, Solid(Blue), 1.3f);
            a.FillEllipse(new Vector2(-0.07f, 0.23f), 0.02f, 0.025f, Solid(Blue), 1.3f);
            var nose = new[] { new Vector2(-0.12f, 0.2f), new Vector2(0.1f, 0.16f), new Vector2(-0.12f, 0.15f) };
            a.Poly(Orange, true, nose);
            a.FillPoly(Solid(Orange), 1.4f, nose);
            a.Arc(new Vector2(-0.12f, 0.14f), 0.06f, 0.035f, 210f, 330f, White);
            var twig = new Color(1f, 0.38f, 0.28f);
            a.Line(new Vector2(-0.28f, -0.1f), new Vector2(-0.58f, 0.08f), twig);
            a.Line(new Vector2(-0.5f, 0.03f), new Vector2(-0.55f, 0.14f), twig);
            a.Line(new Vector2(-0.5f, 0.03f), new Vector2(-0.63f, 0.02f), twig);
            a.Line(new Vector2(0.04f, -0.1f), new Vector2(0.34f, 0.06f), twig);
            a.Line(new Vector2(0.27f, 0.02f), new Vector2(0.3f, 0.13f), twig);
            foreach (float x in new[] { -0.16f, -0.12f, -0.08f })
                a.Line(new Vector2(-0.12f, 0.33f), new Vector2(x * 2.2f + 0.14f, 0.42f), twig);
            // 펭귄
            var pg = new Vector2(0.55f, -0.45f);
            a.Ellipse(pg, 0.12f, 0.2f, Cyan);
            a.Ellipse(pg + new Vector2(0f, -0.04f), 0.07f, 0.13f, White);
            a.Circle(pg + new Vector2(0f, 0.25f), 0.08f, Cyan);
            a.FillEllipse(pg + new Vector2(0.03f, 0.27f), 0.015f, 0.015f, Solid(White), 1.2f);
            a.Poly(Orange, true, pg + new Vector2(0.07f, 0.25f), pg + new Vector2(0.14f, 0.23f), pg + new Vector2(0.07f, 0.21f));
            a.Line(pg + new Vector2(-0.11f, 0.05f), pg + new Vector2(-0.2f, -0.08f), Cyan);
            a.Line(pg + new Vector2(0.11f, 0.05f), pg + new Vector2(0.22f, 0.12f), Cyan);
            a.Snowflake(new Vector2(-0.72f, 0.45f), 0.11f, Sky);
            a.Snowflake(new Vector2(0.38f, 0.45f), 0.08f, Sky);
            a.Snowflake(new Vector2(-0.75f, -0.28f), 0.09f, Sky);
            a.Snowflake(new Vector2(0.72f, 0.05f), 0.07f, Sky);
            return a;
        }

        /// <summary>④ 날아가는 우주비행사 (참고: 비스듬히 하늘을 가르는 영웅 구도, 색색 선): 헬멧·보라 바이저·배낭·뻗은 팔·제트 불꽃.</summary>
        static DroneArt Astronaut()
        {
            var a = new DroneArt("우주비행사") { _rot = 28f, _off = new Vector2(0.05f, 0f), _scale = 1.15f };
            a.Circle(new Vector2(0.27f, 0.1f), 0.15f, White);
            a.Ellipse(new Vector2(0.3f, 0.1f), 0.1f, 0.08f, Purple);
            a.FillEllipse(new Vector2(0.3f, 0.1f), 0.1f, 0.08f, p => Color.Lerp(Purple, Pink, (p.x - 0.2f) * 5f), 1.9f);
            a.Arc(new Vector2(0.33f, 0.12f), 0.05f, 0.04f, 30f, 110f, White);
            a.RoundRect(new Vector2(-0.02f, 0f), 0.18f, 0.12f, 0.06f, White);
            a.FillPoly(Solid(new Color(0.4f, 0.4f, 1f)), 2.4f, new Vector2(-0.18f, -0.1f), new Vector2(0.14f, -0.1f), new Vector2(0.14f, 0.1f), new Vector2(-0.18f, 0.1f));
            a.RoundRect(new Vector2(-0.04f, 0.16f), 0.15f, 0.045f, 0.03f, Green);
            a.Circle(new Vector2(0.05f, 0f), 0.04f, Red);
            // 팔: 앞으로 뻗은 팔(장갑), 뒤로 젖힌 팔
            a.Line(new Vector2(0.14f, -0.06f), new Vector2(0.46f, -0.2f), White);
            a.Line(new Vector2(0.14f, -0.1f), new Vector2(0.44f, -0.24f), White);
            a.Circle(new Vector2(0.5f, -0.23f), 0.04f, Red);
            a.Line(new Vector2(0.0f, 0.12f), new Vector2(0.15f, 0.34f), White);
            a.Line(new Vector2(0.04f, 0.1f), new Vector2(0.19f, 0.32f), White);
            a.Circle(new Vector2(0.18f, 0.37f), 0.035f, Red);
            // 다리 + 부츠
            foreach (var (y0, y1) in new[] { (0.05f, 0.12f), (-0.07f, -0.2f) })
            {
                a.Line(new Vector2(-0.2f, y0), new Vector2(-0.48f, y1), Blue);
                a.Line(new Vector2(-0.2f, y0 - 0.05f), new Vector2(-0.48f, y1 - 0.05f), Blue);
                a.RoundRect(new Vector2(-0.53f, y1 - 0.025f), 0.05f, 0.04f, 0.015f, Red);
            }
            // 제트 불꽃 (반짝이며 뒤로 흩어진다)
            foreach (float y in new[] { 0.09f, -0.23f })
                a.Curve(t => new Vector2(-0.6f - t * 0.32f, y + Mathf.Sin(t * 18f) * 0.025f * t), Color.Lerp(Orange, Red, 0.4f), 1f);
            a.Curve(t => new Vector2(-0.22f - t * 0.6f, 0.16f + Mathf.Sin(t * 14f) * 0.02f), Purple, 1f);
            for (int k = 0; k < 6; k++) a.Add(new Vector2(-0.95f + k * 0.05f, 0.3f - k * 0.02f), Pink, 1f);
            return a;
        }

        /// <summary>⑤ 낚싯대를 든 소녀 초상 (참고: 큰 인물 초상 구도 — 올림머리, 감은 눈 미소, 파란 점 격자 드레스, 뻗은 팔 끝 반짝임).</summary>
        static DroneArt Angler()
        {
            var a = new DroneArt("낚시 소녀");
            var f = new Vector2(-0.25f, 0.3f);
            a.Ellipse(f, 0.14f, 0.18f, Peach);
            a.Arc(f + new Vector2(0f, 0.04f), 0.19f, 0.22f, 160f, 380f, Lilac);
            a.Circle(f + new Vector2(0f, 0.27f), 0.08f, Lilac);
            a.Arc(f + new Vector2(0.02f, 0.25f), 0.05f, 0.04f, 200f, 340f, Lilac);
            a.Curve(t => f + new Vector2(-0.18f - t * 0.1f, -t * 0.42f + Mathf.Sin(t * 6f) * 0.02f), Lilac);
            a.Curve(t => f + new Vector2(0.17f + t * 0.03f, -t * 0.25f), Lilac);
            a.Arc(f + new Vector2(-0.06f, 0.02f), 0.035f, 0.022f, 200f, 340f, Purple);
            a.Arc(f + new Vector2(0.06f, 0.02f), 0.035f, 0.022f, 200f, 340f, Purple);
            a.Ellipse(f + new Vector2(0f, -0.1f), 0.035f, 0.016f, Pink);
            a.Add(f + new Vector2(-0.09f, -0.05f), Pink); a.Add(f + new Vector2(0.09f, -0.05f), Pink);
            // 목·어깨·드레스
            a.Line(f + new Vector2(-0.05f, -0.18f), f + new Vector2(-0.05f, -0.27f), Peach);
            a.Line(f + new Vector2(0.05f, -0.18f), f + new Vector2(0.05f, -0.27f), Peach);
            a.Curve(t => f + new Vector2(Mathf.Lerp(-0.2f, 0.18f, t), -0.3f - Mathf.Sin(t * Mathf.PI) * 0.06f), Peach);
            var dress = new[] { new Vector2(-0.45f, -0.03f), new Vector2(-0.07f, -0.03f), new Vector2(-0.04f, -0.62f), new Vector2(-0.5f, -0.62f) };
            a.Poly(Blue, true, dress);
            a.FillPoly(p => Color.Lerp(Blue, Sky, (p.y + 0.62f) * 1.6f), 2.2f, dress);
            // 팔 → 낚싯대 → 줄 → 물고기, 반짝이
            a.Curve(t => new Vector2(Mathf.Lerp(-0.12f, 0.28f, t), -0.05f + Mathf.Sin(t * Mathf.PI) * 0.06f), Peach);
            a.Circle(new Vector2(0.3f, -0.05f), 0.03f, Peach);
            a.Line(new Vector2(0.3f, -0.05f), new Vector2(0.78f, 0.52f), Gold);
            a.Curve(t => new Vector2(0.78f - t * 0.04f, 0.52f - t * 0.7f + Mathf.Sin(t * Mathf.PI) * 0.03f), White);
            var fish = new Vector2(0.73f, -0.24f);
            a.Ellipse(fish, 0.035f, 0.07f, Orange);
            a.Poly(Orange, true, fish + new Vector2(0f, -0.07f), fish + new Vector2(-0.05f, -0.13f), fish + new Vector2(0.05f, -0.13f));
            a.Sparkle(new Vector2(0.62f, -0.18f), 0.04f, Cyan);
            a.Sparkle(new Vector2(0.86f, -0.1f), 0.05f, Cyan);
            a.Sparkle(new Vector2(0.82f, 0.62f), 0.04f, Gold);
            // 뒤로 흩날리는 리본
            for (int k = 0; k < 3; k++)
                a.Curve(t => new Vector2(-0.45f - t * (0.35f + k * 0.06f), -0.05f - t * (0.45f + k * 0.08f) + Mathf.Sin(t * 5f + k) * 0.04f), Color.Lerp(Pink, Orange, k * 0.4f), 1f);
            return a;
        }

        /// <summary>⑥ "탄천" 글씨와 물 위로 뛰어오르는 잉어 (물결과 물방울).</summary>
        static DroneArt Carp()
        {
            var a = new DroneArt("탄천 잉어");
            a.Tancheon(new Vector2(-0.97f, -0.2f), 0.44f, Magenta);
            // 잉어: 호를 그리는 등뼈를 따라 몸 윤곽, 머리 오른쪽
            var c = new Vector2(0.45f, -0.4f);
            const float R = 0.36f;
            Vector2 Spine(float t) { float th = Mathf.Lerp(165f, 25f, t) * Mathf.Deg2Rad; return c + new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * R; }
            Vector2 Norm(float t) { float th = Mathf.Lerp(165f, 25f, t) * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(th), Mathf.Sin(th)); }
            float W(float t) => 0.1f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.12f, 0.92f, t)), 0.7f);
            a.Curve(t => Spine(t) + Norm(t) * W(t), Gold);
            a.Curve(t => Spine(t) - Norm(t) * W(t), Gold);
            for (float t = 0.12f; t < 0.92f; t += 0.035f)
                for (float u = -0.7f; u <= 0.7f; u += 0.35f)
                    a.Add(Spine(t) + Norm(t) * W(t) * u, Color.Lerp(Orange, Gold, (u + 0.7f) / 1.4f));
            var tail = Spine(0f);
            a.Poly(Orange, false, tail + new Vector2(0.03f, 0.08f), tail + new Vector2(-0.1f, 0.16f), tail + new Vector2(-0.04f, 0f), tail + new Vector2(-0.12f, -0.1f), tail + new Vector2(0.02f, -0.06f));
            var head = Spine(1f);
            a.Curve(t => head + new Vector2(Mathf.Cos(t * Mathf.PI - Mathf.PI * 0.5f) * 0.07f, Mathf.Sin(t * Mathf.PI - Mathf.PI * 0.5f) * 0.075f), Gold);
            a.FillEllipse(Spine(0.9f) + new Vector2(0.02f, 0.03f), 0.018f, 0.018f, Solid(White), 1.2f);
            a.Arc(Spine(0.5f) + Norm(0.5f) * 0.1f, 0.08f, 0.06f, 30f, 150f, Orange);
            // 물결·물방울
            a.Curve(t => new Vector2(Mathf.Lerp(0.0f, 0.95f, t), -0.58f + Mathf.Sin(t * 18f) * 0.025f), Cyan);
            a.Curve(t => new Vector2(Mathf.Lerp(0.1f, 0.85f, t), -0.66f + Mathf.Sin(t * 16f + 1f) * 0.02f), Blue);
            foreach (var (x, y) in new[] { (0.1f, -0.45f), (0.05f, -0.38f), (0.82f, -0.42f), (0.88f, -0.34f), (0.76f, -0.5f), (0.16f, -0.52f) })
                a.Circle(new Vector2(x, y), 0.018f, Sky, 1f);
            a.Sparkle(new Vector2(0.48f, 0.12f), 0.05f, Gold);
            a.Sparkle(new Vector2(-0.2f, 0.4f), 0.04f, Pink);
            return a;
        }
    }
}
