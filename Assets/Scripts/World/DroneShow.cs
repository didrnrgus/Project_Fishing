using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 밤하늘 드론 라이트쇼 (탄천종합운동장 위, 사용자 참고: 성남페스티벌 드론쇼 — 드론 3,000대). 드론 = 3D 공간의 빛 점 하나 (파티클 하나, Fishing/BioGlow 더하기 빛).
    /// 쇼는 "어디에 있나 · 켜졌나 · 무슨 색인가" 세 가지로만 그린다:
    ///  - 장면(<see cref="DroneArt"/>)은 점 파일 하나씩. 어느 폴더의 어떤 파일을 어떤 순서로 쓸지는 <see cref="DroneConfig"/> (droneshow.json — 빌드한 뒤에도 고친다,
    ///    docs/guide/08-drone-show.md). 기본: 게임 폴더의 타이틀 「청풍명월」 → 문서/짭풍명월/DroneShow/*.json. 점보다 남는 드론은 불을 끄고 떠 있는다
    ///  - 한 장면을 <see cref="Hold"/>초(기본 10) 보여 준 뒤 <see cref="Move"/>초(기본 5) 동안 바뀐다 (트랜지션): 드론마다 새 장면에서 가장 가까운 빈 점으로 날아가고
    ///    (가까운 것부터 짝지어 무리가 흩어지지 않게), 왼쪽에서 오른쪽으로 물결처럼 차례로 출발하고, 3D로 앞뒤·위로 부푼 곡선을 그리며, 날아가는 동안 빛이 조금 어두워졌다
    ///    새 색으로 켜진다. 새로 쓰이는 드론은 꺼진 자리에서 켜지며 오고, 안 쓰이는 드론은 날아가며 꺼진다
    /// 점 파일에 효과 번호가 있으면 점마다 깜빡이고, 장면 끝엔 전체가 반짝인다. 윤곽 점(<see cref="DroneArt.E"/>)은 조금 크고 밝으며,
    /// 장면을 보여 주는 동안 빛 줄기가 대각선으로 쫘르륵 훑고 지나가 윤곽이 하얗게 타오른다. 드론은 제자리에 박혀 있지 않고 살짝 둥둥 떠다닌다. 밤(<c>_FishingNight</c>)에만 보이고, 날이 밝으면 아래 대기 자리로 내려앉아 꺼진다.
    /// 점 파일이 하나도 없으면 쇼를 하지 않는다 (예전 코드로 그린 여섯 장면은 사용자 요청으로 지웠다).
    /// </summary>
    public class DroneShow : MonoBehaviour
    {
        public const int N = 3000;
        /// <summary>장면을 보여 주는 시간 · 트랜지션 시간 (초, droneshow.json의 holdSeconds · moveSeconds).</summary>
        public float Hold = 10f, Move = 5f;
        /// <summary>트랜지션 안에서 출발을 나누는 폭 (초, 트랜지션 5초면 1초). 드론마다 Move - Wave초 동안 난다.</summary>
        float Wave => Mathf.Min(1f, Move * 0.2f);
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
        float[] _delay, _phTo, _eTo, _bob;
        byte[] _fxTo;
        int _scene;
        /// <summary>공지: 밤이 됐는가 (null = 아직 처음 프레임 전 — 맵에 들어온 때가 밤이면 "펼쳐지고 있다", 저녁이 되면 "시작").</summary>
        bool? _night;
        float _noticeAt = -1e9f;
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
            var cfg = DroneConfig.Load();
            Hold = Mathf.Max(1f, cfg.holdSeconds);
            Move = Mathf.Max(1f, cfg.moveSeconds);
            _scenes = DroneArt.Scenes(cfg);
            if (_scenes.Count == 0) { Debug.Log("[DroneShow] 점 파일 없음 (" + string.Join(", ", cfg.folders) + ")"); enabled = false; return; }
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
            _delay = new float[N]; _phTo = new float[N]; _fxTo = new byte[N]; _eTo = new float[N]; _bob = new float[N];
            var rng = new System.Random(2026);
            for (int i = 0; i < N; i++)
            {
                _cur[i] = Standby(i);
                _cCur[i] = Color.clear;
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                _arc[i] = new Vector3(((float)rng.NextDouble() - 0.5f) * 6f, 3f + (float)rng.NextDouble() * 5f, side * (6f + (float)rng.NextDouble() * 10f));
                _bob[i] = (float)rng.NextDouble() * 100f;
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
        Vector3 Ground(int i) => new Vector3((i % 60 - 30) * 2f, -Height + 1.5f, (i / 60 - 25) * 2f);

        /// <summary>꺼진 채 기다리는 자리: 판 뒤 14m 하늘에 판만 한 격자 (3,000대가 다 켜지진 않는다 — 장면에 필요한 만큼만 불을 켜고 날아온다).</summary>
        Vector3 Standby(int i) => new Vector3(((i % 75) / 74f - 0.5f) * Width * 0.95f, ((i / 75) / 39f - 0.5f) * Width * 0.6f, 14f);

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
                _fxTo[best] = art.Fx[k];
                _phTo[best] = art.Ph[k];
                _eTo[best] = art.E[k];
                _delay[best] = Mathf.Clamp(((art.P[k].x + 1f) * 0.4f + (float)rng.NextDouble() * 0.2f) * Wave, 0f, Wave);
            }
            Debug.Log($"[DroneShow] {art.Name}: 점 {art.Count}개 중 {m}개에 드론 배정 (켜진 드론 {m}대 / {N}대)");
            for (int i = 0; i < N; i++)
            {
                if (used[i]) continue;
                _to[i] = _cur[i];
                _cTo[i] = new Color(_cCur[i].r, _cCur[i].g, _cCur[i].b, 0f);
                _fxTo[i] = 0;
                _eTo[i] = 0f;
                _delay[i] = Mathf.Clamp((_cur[i].x / S + 1f) * 0.4f * Wave, 0f, Wave);
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

        /// <summary>자동 촬영: 지금 장면을 끝내고 바로 다음 장면으로 넘어가기 시작한다 (실제 트랜지션을 연속으로 찍으려고).</summary>
        public void NextNow()
        {
            _on = 1f;
            _t = Move + Hold + 0.001f;
        }

        void Update()
        {
            float night = Shader.GetGlobalFloat("_FishingNight");
            bool isNight = _night == true ? night > 0.4f : night > 0.6f;   // 해 질 녘 경계에서 깜빡이지 않게 켜고 끄는 기준을 따로
            if (isNight && _night != true && Time.time > _noticeAt + 300f && Fishing.Core.Game.Mode == Fishing.Core.GameMode.Field)
            {
                _noticeAt = Time.time;
                // 저녁 공지 (사용자 요청): 저녁이 되면 쇼가 시작한다고, 밤에 들어오면 지금 하고 있다고
                string msg = _night == null
                    ? "탄천종합운동장 하늘에서 드론 라이트쇼가 펼쳐지고 있습니다. 하늘을 올려다보세요!"
                    : "저녁이 되었습니다. 곧 탄천종합운동장 하늘에서 드론 라이트쇼가 시작됩니다!";
                Fishing.Core.Game.Toast(msg, Fishing.Core.ToastKind.Great);
                Fishing.Core.Game.SysLog(msg, "#b8a0ff");
            }
            _night = isNight;
            _on = Mathf.MoveTowards(_on, isNight ? 1f : 0f, Time.deltaTime * 0.5f);
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

        /// <summary>
        /// 깜빡이 효과 (점마다, 다 날아온 뒤에만): 1 반짝임 · 2 전광판처럼 줄지어 달리는 불빛(ph = 선 길이) · 3 눈 깜빡임(3.4초마다 잠깐 꺼짐)
        /// · 4 일렁임(ph = x, 빛 물결이 옆으로 지나간다) · 5 배경 반짝이(대부분 꺼져 있다가 번쩍).
        /// </summary>
        static float FxLevel(byte fx, float ph, float t)
        {
            switch (fx)
            {
                case 1: return 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 2.6f + ph));
                case 2: return Mathf.Repeat(ph * 5f - t * 1.2f, 1f) < 0.45f ? 1f : 0.35f;
                case 3: return Mathf.Repeat(t, 3.4f) < 0.17f ? 0f : 1f;
                case 4: return 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(ph * 12f - t * 3.2f));
                case 5: return 0.05f + 0.95f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.15f + ph)), 14f);
                default: return 1f;
            }
        }

        /// <summary>
        /// 장면을 보여 주는 동안 왼쪽 위 → 오른쪽 아래로 판을 훑는 빛 줄기 (0~1). 앞은 날카롭고 뒤로 길게 꼬리를 끌어 쫘르륵 흐르는 것처럼.
        /// 다 날아온 뒤 0.6초부터, 3.4초에 한 번 판을 건넌다.
        /// </summary>
        float Sweep(Vector3 p)
        {
            float t = _t - Move - 0.6f;
            if (t < 0f || _t > Move + Hold - 1.2f) return 0f;
            float S = Width * 0.5f;
            float u = (p.x * 0.8f - p.y * 0.6f) / S;          // 대각선 방향 위치 (약 -1.3 ~ 1.3)
            float d = u - (Mathf.Repeat(t, 3.4f) * 1.05f - 1.6f);  // 줄기 앞에서 +, 지나간 뒤 -
            return d > 0f ? Mathf.Exp(-d * d / 0.0025f) : Mathf.Exp(d / 0.32f);
        }

        void Apply()
        {
            float now = Time.time;
            float span = Move - Wave;   // 한 드론이 날아가는 시간 (5초면 4초, 출발은 0~1초 사이로 물결) → 마지막 드론이 정확히 Move초에 자리 잡는다
            for (int i = 0; i < N; i++)
            {
                float k = Mathf.Clamp01((_t - _delay[i]) / span);
                float e = k * k * (3f - 2f * k);
                float bump = Mathf.Sin(e * Mathf.PI);
                var p = Vector3.Lerp(_from[i], _to[i], e) + _arc[i] * bump;
                _cur[i] = p;
                // 둥둥: 드론마다 제 박자로 위아래 0.4m · 옆 0.25m · 앞뒤 0.3m쯤, 판 전체엔 바람처럼 느린 물결 (점 간격 2m라 그림은 안 깨진다)
                float b = _bob[i];
                var bob = new Vector3(Mathf.Sin(now * 0.47f + b) * 0.25f,
                                      Mathf.Sin(now * 0.61f + b * 1.3f) * 0.4f + Mathf.Sin(now * 0.35f - p.x * 0.025f) * 0.3f,
                                      Mathf.Sin(now * 0.53f + b * 0.7f) * 0.3f);
                // 날아가는 동안은 조금 어둡게, 새 색으로 바뀐다
                var c = Color.Lerp(_cFrom[i], _cTo[i], Mathf.Clamp01(e * 1.4f - 0.2f));
                float dim = 1f - 0.45f * bump;
                if (k >= 1f) dim *= FxLevel(_fxTo[i], _phTo[i], now);
                // 장면 끝 1.2초: 전체가 반짝반짝 하다가 트랜지션으로
                if (_t > Move + Hold - 1.2f) dim *= 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(now * 17f + i * 1.93f));
                _cCur[i] = c;
                // 윤곽 빛: 다 날아온 만큼(e) 윤곽 점이 조금 크고 하얗게, 빛 줄기가 지나가면 확 타오른다 (안쪽 점도 살짝)
                float edge = _eTo[i] * e, glow = Sweep(p) * e;
                float lift = edge * (0.04f + 0.8f * glow) + 0.1f * glow;   // 평소엔 색 그대로 (윤곽이 40%쯤이라 늘 하얗게 하면 그림이 바랜다)
                c = Color.Lerp(c, new Color(1f, 1f, 1f, c.a), Mathf.Clamp01(lift));   // 알파는 그대로 (꺼진 드론은 꺼진 채)
                dim = Mathf.Min(1f, dim * (1f + 0.5f * lift));
                var shown = Vector3.Lerp(Ground(i), p + bob, _on);
                var col = new Color(c.r * dim, c.g * dim, c.b * dim, c.a * _on);
                _parts[i].position = shown;
                _parts[i].startColor = col;
                _parts[i].startSize = 1.5f * (1f + 0.18f * edge + 0.8f * edge * glow + 0.12f * glow);
                _parts[i].remainingLifetime = 1e5f;
                _parts[i].startLifetime = 1e6f;
            }
            _ps.SetParticles(_parts, N);
        }
    }

    /// <summary>
    /// 드론쇼 한 장면 = 점 목록 (판 좌표: x -1~1, y 위가 +, z 앞뒤) · 색 · 깜빡이 효과.
    /// 장면은 사용자가 뽑은 점 파일(<c>[[x, y, r, g, b], ...]</c>, x·y 0~1 · 긴 변 = 1 · y 아래가 +, 색 0~255)을
    /// 내 문서/짭풍명월/DroneShow/*.json 에서 이름 순서로 읽는다. 그림 저작권은 사용자 몫이라 점 파일은 저장소에 넣지 않는다 (17 문서).
    /// 여섯째 값이 있으면 깜빡이 효과 번호 (<see cref="DroneShow"/>.FxLevel).
    /// </summary>
    public sealed class DroneArt
    {
        public readonly string Name;
        public readonly List<Vector3> P = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<byte> Fx = new List<byte>();
        public readonly List<float> Ph = new List<float>();
        /// <summary>점마다 윤곽 정도 (0 안쪽 ~ 1 그림 테두리·색 경계). 읽을 때 이웃 점으로 잰다 (<see cref="MeasureEdges"/>).</summary>
        public readonly List<float> E = new List<float>();
        public int Count => P.Count;

        DroneArt(string name) { Name = name; }

        /// <summary>설정(droneshow.json) 순서대로 점 파일들 (없으면 빈 목록 — 쇼를 하지 않는다).</summary>
        public static List<DroneArt> Scenes(DroneConfig cfg)
        {
            var list = new List<DroneArt>();
            foreach (var f in cfg.SceneFiles())
            {
                try
                {
                    var art = FromJson(System.IO.Path.GetFileNameWithoutExtension(f), System.IO.File.ReadAllText(f));
                    if (art.Count > 0) list.Add(art);
                    else Debug.LogWarning("[DroneShow] 점이 없다: " + f);
                }
                catch (Exception e) { Debug.LogWarning($"[DroneShow] 점 파일을 못 읽었다 ({f}): {e.Message}"); }
            }
            return list;
        }

        /// <summary>
        /// <c>[[x, y, r, g, b(, fx)], ...]</c> → 판 좌표. 점들의 테두리 상자 가운데를 판 가운데로, 폭 2 · 높이 1.2 안에 들어가게 키우고 y를 뒤집는다.
        /// 드론 수(<see cref="DroneShow.N"/>)보다 많으면 앞에서부터 자른다.
        /// </summary>
        public static DroneArt FromJson(string name, string json)
        {
            var art = new DroneArt(name);
            var rows = new List<float[]>();
            var cur = new List<float>();
            int depth = 0, start = -1;
            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                bool num = (ch >= '0' && ch <= '9') || ch == '.' || ch == '-' || ch == 'e' || ch == 'E' || ch == '+';
                if (num) { if (start < 0) start = i; continue; }
                if (start >= 0)
                {
                    cur.Add(float.Parse(json.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture));
                    start = -1;
                }
                if (ch == '[') { depth++; if (depth == 2) cur.Clear(); }
                else if (ch == ']') { if (depth == 2 && cur.Count >= 5) rows.Add(cur.ToArray()); depth--; }
            }
            if (rows.Count == 0) return art;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var r in rows) { x0 = Mathf.Min(x0, r[0]); x1 = Mathf.Max(x1, r[0]); y0 = Mathf.Min(y0, r[1]); y1 = Mathf.Max(y1, r[1]); }
            float k = Mathf.Min(2f / Mathf.Max(1e-4f, x1 - x0), 1.2f / Mathf.Max(1e-4f, y1 - y0));   // 세로 그림도 화면에 들어오게 높이 1.2까지
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            int n = Mathf.Min(rows.Count, DroneShow.N);
            for (int i = 0; i < n; i++)
            {
                var r = rows[i];
                art.P.Add(new Vector3((r[0] - cx) * k, -(r[1] - cy) * k, 0f));
                art.C.Add(new Color(r[2] / 255f, r[3] / 255f, r[4] / 255f));
                art.Fx.Add(r.Length > 5 ? (byte)Mathf.Clamp(r[5], 0f, 5f) : (byte)0);
                art.Ph.Add(Mathf.Repeat(r[0] * 127.1f + r[1] * 311.7f, 6.283f));
            }
            art.MeasureEdges();
            return art;
        }

        /// <summary>
        /// 윤곽 정도: 점 간격(가장 가까운 이웃 거리의 중앙값) s의 2.6배 안 이웃들을 본다.
        /// 이웃들이 한쪽으로 쏠려 있으면 그림 테두리(구멍 가장자리 포함), 이웃과 색이 크게 다르면 색 경계 — 둘 중 큰 값.
        /// </summary>
        void MeasureEdges()
        {
            int n = P.Count;
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var q in P) { x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); y0 = Mathf.Min(y0, q.y); y1 = Mathf.Max(y1, q.y); }
            float h = Mathf.Max(1e-4f, Mathf.Sqrt((x1 - x0) * (y1 - y0) / n));
            var grid = Grid(h);
            var nn = new float[n];
            for (int i = 0; i < n; i++)
            {
                float best = float.MaxValue;
                foreach (int j in Near(grid, h, P[i]))
                    if (j != i) best = Mathf.Min(best, ((Vector2)(P[j] - P[i])).sqrMagnitude);
                nn[i] = best < float.MaxValue ? Mathf.Sqrt(best) : h;
            }
            var sorted = (float[])nn.Clone();
            Array.Sort(sorted);
            float r = 2.6f * sorted[n / 2];
            grid = Grid(r);
            for (int i = 0; i < n; i++)
            {
                Vector2 sum = Vector2.zero;
                int cnt = 0;
                float con = 0f;
                foreach (int j in Near(grid, r, P[i]))
                {
                    if (j == i) continue;
                    Vector2 d = P[j] - P[i];
                    if (d.sqrMagnitude > r * r) continue;
                    sum += d;
                    cnt++;
                    var dc = C[j] - C[i];
                    con = Mathf.Max(con, Mathf.Sqrt(dc.r * dc.r + dc.g * dc.g + dc.b * dc.b) / 1.732f);
                }
                float off = cnt > 0 ? sum.magnitude / (cnt * r) : 1f;
                E.Add(Mathf.Max(Mathf.Clamp01((off - 0.12f) * 3.5f), Mathf.Clamp01((con - 0.25f) * 2f)));
            }
        }

        Dictionary<Vector2Int, List<int>> Grid(float cell)
        {
            var g = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < P.Count; i++)
            {
                var key = new Vector2Int(Mathf.FloorToInt(P[i].x / cell), Mathf.FloorToInt(P[i].y / cell));
                if (!g.TryGetValue(key, out var list)) g[key] = list = new List<int>();
                list.Add(i);
            }
            return g;
        }

        static IEnumerable<int> Near(Dictionary<Vector2Int, List<int>> g, float cell, Vector3 p)
        {
            int gx = Mathf.FloorToInt(p.x / cell), gy = Mathf.FloorToInt(p.y / cell);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (g.TryGetValue(new Vector2Int(gx + dx, gy + dy), out var list))
                        foreach (int j in list) yield return j;
        }
    }

    /// <summary>
    /// 드론쇼 설정 droneshow.json — 빌드한 뒤에도 고친다 (설명: docs/guide/08-drone-show.md, 게임 폴더 StreamingAssets/DroneShow/읽어주세요.txt).
    /// - 게임 폴더 StreamingAssets/DroneShow/droneshow.json: 게임과 함께 나가는 기본 설정 (타이틀 장면 title.json과 같은 폴더)
    /// - 문서/짭풍명월/DroneShow/droneshow.json: 있으면 적은 항목만 위를 덮는다 (게임을 다시 깔아도 남는다)
    /// folders: 장면을 찾을 폴더 (차례대로, 폴더 안은 이름 순서 — 이름 속 숫자는 수 크기로). "{game}" = 게임 폴더, "{documents}" = 문서/짭풍명월/DroneShow,
    /// 그 밖엔 절대 경로 (%USERPROFILE% 같은 환경 변수도). scenes를 적으면 그 파일들만 그 순서로 (folders에서 찾는다, 절대 경로도 된다).
    /// </summary>
    [Serializable]
    public class DroneConfig
    {
        public const string FileName = "droneshow.json";
        public string[] folders;
        public string[] scenes;
        public float holdSeconds = -1f;
        public float moveSeconds = -1f;

        public static string GameFolder => System.IO.Path.Combine(Application.streamingAssetsPath, "DroneShow");
        public static string UserFolder =>
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "짭풍명월", "DroneShow");

        public static DroneConfig Load()
        {
            var cfg = Read(System.IO.Path.Combine(GameFolder, FileName)) ?? new DroneConfig();
            var user = Read(System.IO.Path.Combine(UserFolder, FileName));
            if (user != null)
            {
                if (user.folders != null && user.folders.Length > 0) cfg.folders = user.folders;
                if (user.scenes != null && user.scenes.Length > 0) cfg.scenes = user.scenes;
                if (user.holdSeconds > 0f) cfg.holdSeconds = user.holdSeconds;
                if (user.moveSeconds > 0f) cfg.moveSeconds = user.moveSeconds;
            }
            if (cfg.folders == null || cfg.folders.Length == 0) cfg.folders = new[] { "{game}", "{documents}" };
            if (cfg.holdSeconds <= 0f) cfg.holdSeconds = 10f;
            if (cfg.moveSeconds <= 0f) cfg.moveSeconds = 5f;
            bool listed = cfg.scenes != null && cfg.scenes.Length > 0;
            Debug.Log($"[DroneShow] 설정: 폴더 {string.Join(", ", cfg.folders)} · 장면 {(listed ? string.Join(", ", cfg.scenes) : "(폴더 안 전부)")} · {cfg.holdSeconds}초 보여 주고 {cfg.moveSeconds}초 트랜지션");
            return cfg;
        }

        static DroneConfig Read(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return null;
                return JsonUtility.FromJson<DroneConfig>(System.IO.File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DroneShow] {path}를 읽지 못했다 ({e.Message}). 쉼표·따옴표를 확인하세요.");
                return null;
            }
        }

        static string Resolve(string folder) =>
            folder == "{game}" ? GameFolder : folder == "{documents}" ? UserFolder : Environment.ExpandEnvironmentVariables(folder);

        /// <summary>쓸 점 파일 경로들 (순서대로, 설정 파일 droneshow.json은 빼고).</summary>
        public IEnumerable<string> SceneFiles()
        {
            var dirs = new List<string>();
            foreach (var f in folders)
                if (!string.IsNullOrWhiteSpace(f) && System.IO.Directory.Exists(Resolve(f))) dirs.Add(Resolve(f));
            if (scenes != null && scenes.Length > 0)
            {
                foreach (var name in scenes)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var file = Environment.ExpandEnvironmentVariables(name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? name : name + ".json");
                    string hit = System.IO.Path.IsPathRooted(file) && System.IO.File.Exists(file) ? file : null;
                    foreach (var d in dirs)
                        if (hit == null && System.IO.File.Exists(System.IO.Path.Combine(d, file))) hit = System.IO.Path.Combine(d, file);
                    if (hit != null) yield return hit;
                    else Debug.LogWarning($"[DroneShow] {FileName}에 적힌 장면 \"{name}\" 파일이 없다");
                }
                yield break;
            }
            // 이름 속 숫자는 수 크기로 (points_2 → points_10)
            string Key(string f) => System.Text.RegularExpressions.Regex.Replace(f, @"\d+", m => m.Value.PadLeft(8, '0'));
            foreach (var d in dirs)
            {
                var files = System.IO.Directory.GetFiles(d, "*.json");
                Array.Sort(files, (a, b) => string.CompareOrdinal(Key(a), Key(b)));
                foreach (var f in files)
                    if (!string.Equals(System.IO.Path.GetFileName(f), FileName, StringComparison.OrdinalIgnoreCase)) yield return f;
            }
        }
    }
}
