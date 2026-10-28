using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 밤하늘 드론 라이트쇼 (탄천종합운동장 위, 사용자 참고: 성남페스티벌 드론쇼 — 드론 3,000대). 드론 = 3D 공간의 빛 점 하나 (파티클 하나, Fishing/BioGlow 더하기 빛).
    /// 쇼는 "어디에 있나 · 켜졌나 · 무슨 색인가" 세 가지로만 그린다:
    ///  - 장면(<see cref="DroneArt"/>)은 사용자가 그림에서 뽑은 점 파일 (내 문서/짭풍명월/DroneShow/*.json, 이름 순서). 점보다 남는 드론은 불을 끄고 떠 있는다
    ///  - 한 장면을 <see cref="Hold"/>초 보여 준 뒤 <see cref="Move"/>초 동안 바뀐다 (트랜지션): 드론마다 새 장면에서 가장 가까운 빈 점으로 날아가고
    ///    (가까운 것부터 짝지어 무리가 흩어지지 않게), 왼쪽에서 오른쪽으로 물결처럼 차례로 출발하고, 3D로 앞뒤·위로 부푼 곡선을 그리며, 날아가는 동안 빛이 조금 어두워졌다
    ///    새 색으로 켜진다. 새로 쓰이는 드론은 꺼진 자리에서 켜지며 오고, 안 쓰이는 드론은 날아가며 꺼진다
    /// 점 파일에 효과 번호가 있으면 점마다 깜빡이고, 장면 끝엔 전체가 반짝인다. 밤(<c>_FishingNight</c>)에만 보이고, 날이 밝으면 아래 대기 자리로 내려앉아 꺼진다.
    /// 점 파일이 없으면 쇼를 하지 않는다 (예전 코드로 그린 여섯 장면은 사용자 요청으로 지웠다).
    /// </summary>
    public class DroneShow : MonoBehaviour
    {
        public const int N = 3000;
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
        float[] _delay, _phTo;
        byte[] _fxTo;
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
            if (_scenes.Count == 0) { Debug.Log("[DroneShow] 점 파일 없음 (" + DroneArt.UserFolder + ")"); enabled = false; return; }
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
            _delay = new float[N]; _phTo = new float[N]; _fxTo = new byte[N];
            var rng = new System.Random(2026);
            for (int i = 0; i < N; i++)
            {
                _cur[i] = Standby(i);
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
                _delay[best] = (art.P[k].x + 1f) * 0.75f + (float)rng.NextDouble() * 0.35f;
            }
            Debug.Log($"[DroneShow] {art.Name}: 점 {art.Count}개 중 {m}개에 드론 배정 (켜진 드론 {m}대 / {N}대)");
            for (int i = 0; i < N; i++)
            {
                if (used[i]) continue;
                _to[i] = _cur[i];
                _cTo[i] = new Color(_cCur[i].r, _cCur[i].g, _cCur[i].b, 0f);
                _fxTo[i] = 0;
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

        /// <summary>자동 촬영: 지금 장면을 끝내고 바로 다음 장면으로 넘어가기 시작한다 (실제 트랜지션을 연속으로 찍으려고).</summary>
        public void NextNow()
        {
            _on = 1f;
            _t = Move + Hold + 0.001f;
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
                if (k >= 1f) dim *= FxLevel(_fxTo[i], _phTo[i], now);
                // 장면 끝 1.2초: 전체가 반짝반짝 하다가 트랜지션으로
                if (_t > Move + Hold - 1.2f) dim *= 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(now * 17f + i * 1.93f));
                _cCur[i] = c;
                var shown = Vector3.Lerp(Ground(i), p, _on);
                var col = new Color(c.r * dim, c.g * dim, c.b * dim, c.a * _on);
                _parts[i].position = shown;
                _parts[i].startColor = col;
                _parts[i].startSize = 1.5f;
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
        public int Count => P.Count;

        DroneArt(string name) { Name = name; }

        public static string UserFolder =>
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "짭풍명월", "DroneShow");

        /// <summary>사용자 폴더의 점 파일들 (없으면 빈 목록 — 쇼를 하지 않는다).</summary>
        public static List<DroneArt> Scenes()
        {
            var list = new List<DroneArt>();
            try
            {
                if (!System.IO.Directory.Exists(UserFolder)) return list;
                var files = System.IO.Directory.GetFiles(UserFolder, "*.json");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var f in files)
                {
                    var art = FromJson(System.IO.Path.GetFileNameWithoutExtension(f), System.IO.File.ReadAllText(f));
                    if (art.Count > 0) list.Add(art);
                }
            }
            catch (Exception e) { Debug.LogWarning("[DroneShow] 점 파일을 못 읽었다: " + e.Message); }
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
            return art;
        }
    }
}
