using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 숲 덮인 먼 섬·산 (사용자 요청 "배경 모델 완전 실사 느낌으로", 2026-10-22).
    /// 예전 먼 섬은 공 프리미티브나 매끈한 돔(<see cref="HanRiverLandmarks.HillMesh"/>)에 풀 텍스처를 늘여 붙여 초록 원뿔처럼 보였다.
    /// 사진 속 남해·서해 섬은 ① 물가가 회색 바위 절벽으로 끊기고 ② 능선에서 골짜기가 방사형으로 갈라지며 ③ 나무 머리가 오돌토돌한 숲 결로 덮였다.
    ///  - <see cref="Island"/>: 물가 절벽 띠 + 봉우리 몇 개 + 리지 노이즈 능선·골짜기 메쉬
    ///  - <see cref="Forest"/>: Fishing/Forest 재질 (수관 텍스처 트라이플래너, 가파른 면·물가는 바위, 공기 원근감 안개)
    ///  - <see cref="Canopy"/>: 수관 높이(G)·수관별 색(R) 텍스처와 그 법선 (코드로 한 번 굽는다)
    /// </summary>
    public static class FarLand
    {
        // ---------------- 재질 ----------------

        public static readonly Color KoreaDark = new Color(0.07f, 0.13f, 0.065f), KoreaLight = new Color(0.25f, 0.34f, 0.13f);
        public static readonly Color TropicDark = new Color(0.045f, 0.12f, 0.05f), TropicLight = new Color(0.18f, 0.35f, 0.09f);

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>
        /// 숲 재질. dark·light = 수관 초록 범위, rock = 바위 결 텍스처(Resources/Terrain), fog = 맵 안개를 받는 배율(멀수록 크게),
        /// sparse > 0이면 나무 사이 땅(ground)이 드러난다(덤불 산 0.5, 눈 산 0.45 + snow).
        /// </summary>
        public static Material Forest(Color dark, Color light, string rock = "seacliff", Color? rockTint = null, float fog = 0.6f,
            float sparse = 0f, Color? ground = null, float snow = 0f, float rockSlope = 0.62f, float shoreBand = 3f)
        {
            var rt = rockTint ?? new Color(0.46f, 0.45f, 0.42f);
            var gc = ground ?? new Color(0.36f, 0.32f, 0.24f);
            string key = $"{dark}{light}{rock}{rt}{fog}{sparse}{gc}{snow}{rockSlope}{shoreBand}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var shader = Shader.Find("Fishing/Forest");
            if (shader == null)
                return Cache[key] = HanRiverLandmarks.Mat(Color.Lerp(dark, light, 0.5f), Resources.Load<Texture2D>("Terrain/grass_diff"), null, Color.black, 0.05f, 0f, new Vector2(14f, 14f));
            m = new Material(shader) { name = "Forest" };
            m.SetTexture("_Canopy", Canopy);
            m.SetTexture("_CanopyNormal", CanopyNormal);
            m.SetColor("_Dark", dark);
            m.SetColor("_Light", light);
            m.SetColor("_Ground", gc);
            m.SetFloat("_Sparse", sparse);
            m.SetFloat("_Snow", snow);
            m.SetTexture("_RockTex", Resources.Load<Texture2D>("Terrain/" + rock + "_diff"));
            var nor = Resources.Load<Texture2D>("Terrain/" + rock + "_nor");
            if (nor != null) m.SetTexture("_RockNor", nor);
            m.SetColor("_RockAvg", Mats.AvgOf(rock));
            m.SetColor("_RockTint", rt);
            m.SetFloat("_RockSlope", rockSlope);
            m.SetFloat("_ShoreBand", shoreBand);
            m.SetFloat("_FogScale", fog);
            return Cache[key] = m;
        }

        /// <summary>한국 바닷가 섬의 숲 (봄: 소나무 짙은 초록에 활엽수 연두 얼룩, 물가는 회색 바위).</summary>
        public static Material KoreaForest(float fog = 0.6f) => Forest(KoreaDark, KoreaLight, "seacliff", null, fog);

        /// <summary>열대 밀림 (더 짙고 푸른 초록, 바위는 화강암).</summary>
        public static Material Jungle(float fog = 0.6f, string rock = "granite") => Forest(TropicDark, TropicLight, rock, new Color(0.5f, 0.47f, 0.42f), fog);

        /// <summary>지중해 덤불 산 (마키: 올리브빛 덤불 사이로 황토 땅과 흰 석회암).</summary>
        public static Material Maquis(float fog = 0.6f) =>
            Forest(new Color(0.1f, 0.12f, 0.06f), new Color(0.24f, 0.25f, 0.13f), "limecliff", new Color(0.62f, 0.6f, 0.55f), fog, 0.5f, new Color(0.5f, 0.43f, 0.32f), 0f, 0.42f);

        /// <summary>눈 덮인 침엽수 산 (청진포구).</summary>
        public static Material SnowForest(float fog = 0.6f) =>
            Forest(new Color(0.04f, 0.07f, 0.05f), new Color(0.09f, 0.13f, 0.09f), "seacliff", new Color(0.5f, 0.51f, 0.53f), fog, 0.42f, new Color(0.84f, 0.87f, 0.92f), 1f);

        // ---------------- 메쉬 ----------------

        /// <summary>
        /// 바다 위 섬: 바닥(원점)은 물속 -5m에서 시작해 물가 절벽 띠로 가파르게 오르고, 봉우리 몇 개와 리지 노이즈 능선을 지나 꼭대기 h.
        /// rx·rz = 반지름(가로·세로), cliff = 물가 절벽 높이 비율(h의). 원점은 물높이에 놓는다.
        /// </summary>
        public static Mesh Island(float rx, float rz, float h, int seed, float cliff = 0.12f, int peakCount = 0)
        {
            const int seg = 128, rings = 40;
            var rnd = new System.Random(seed * 7919 + 17);
            float Rf(float a, float b) => (float)(a + rnd.NextDouble() * (b - a));
            int peaks = peakCount > 0 ? peakCount : rx > 160f ? 3 : rx > 70f ? 2 : 1;
            var pk = new Vector3[peaks]; // x, z (정규화), 높이 비율
            for (int i = 0; i < peaks; i++)
                pk[i] = peaks == 1 ? new Vector3(Rf(-0.15f, 0.15f), Rf(-0.15f, 0.15f), 1f) : new Vector3(Rf(-0.5f, 0.5f), Rf(-0.25f, 0.25f), i == 0 ? 1f : Rf(0.6f, 0.9f));
            float sx = seed * 1.37f % 97f, sz = seed * 2.11f % 89f;
            float cliffH = Mathf.Clamp(h * cliff, 2.5f, Mathf.Max(14f, h * 0.8f));

            var v = new List<Vector3>((seg + 1) * (rings + 1));
            var uv = new List<Vector2>((seg + 1) * (rings + 1));
            for (int r = 0; r <= rings; r++)
            {
                float f = r / (float)rings; // 0 물가 ~ 1 가운데
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    // 들쭉날쭉한 해안선: 각도 노이즈 두 옥타브 (작은 곶과 만)
                    float outline = 0.78f + 0.34f * Mathf.PerlinNoise(sx + ca * 1.4f, sz + sa * 1.4f) + 0.1f * Mathf.PerlinNoise(sx + ca * 5f + 3f, sz + sa * 5f);
                    float rad = (1f - f) * outline;
                    float x = ca * rad, z = sa * rad;
                    // 물가 절벽 띠: 가장자리 6%에서 물속 -5m → 절벽 위
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f / 0.06f));
                    float fi = Mathf.Clamp01((f - 0.06f) / 0.94f);
                    // 봉우리들 + 기본 돔
                    float dome = 0f;
                    foreach (var p in pk)
                    {
                        float d2 = (x - p.x) * (x - p.x) + (z - p.y) * (z - p.y);
                        dome = Mathf.Max(dome, p.z * Mathf.Exp(-d2 * 3.2f));
                    }
                    float body = Mathf.SmoothStep(0f, 1f, Mathf.Pow(fi, 0.75f));
                    float hh = Mathf.Lerp(body, dome, 0.55f) * Mathf.Clamp01(fi * 3f);
                    // 능선·골짜기: 리지 노이즈 세 옥타브 (위로 갈수록 세게). 숲 덮인 섬은 둥글둥글해서 세게 하면 바위산처럼 뾰족해진다
                    float rdg = 0f, amp = 0.5f, fq = 2.4f;
                    for (int o = 0; o < 3; o++)
                    {
                        float q = Mathf.PerlinNoise(sx + x * fq + o * 13.1f, sz + z * fq + o * 7.3f) * 2f - 1f;
                        rdg += (1f - Mathf.Abs(q)) * amp;
                        amp *= 0.45f; fq *= 2.1f;
                    }
                    hh *= 0.8f + 0.32f * rdg * Mathf.SmoothStep(0.05f, 0.6f, fi);
                    // 절벽 위 높이를 둘레마다 흔들되, 안쪽은 그 높이에서부터 쌓는다 (예전엔 둘레가 안쪽보다 높아 꼭대기가 분화구처럼 팼다)
                    float cTop = Mathf.Min(cliffH * (0.75f + 0.5f * Mathf.PerlinNoise(sx + ca * 3f, sz + sa * 3f)), h * 0.9f);
                    float y = Mathf.Lerp(-5f, cTop, edge) + (h - cTop) * hh;
                    var pos = new Vector3(x * rx, y, z * rz);
                    v.Add(pos);
                    uv.Add(new Vector2(pos.x / 20f, pos.z / 20f));
                }
            }
            var t = new List<int>(seg * rings * 6);
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < seg; s++)
            {
                int i = r * (seg + 1) + s;
                t.Add(i); t.Add(i + seg + 1); t.Add(i + 1);
                t.Add(i + 1); t.Add(i + seg + 1); t.Add(i + seg + 2);
            }
            var m = new Mesh { name = "FarIsland" };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>섬 하나를 놓는다 (그림자는 던지지 않음 — 수백 m 밖).</summary>
        public static GameObject Place(Transform parent, string name, Vector3 pos, float rx, float rz, float h, int seed, Material mat, float yaw = 0f, float cliff = 0.12f, int peaks = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = Island(rx, rz, h, seed, cliff, peaks);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ---------------- 수관 텍스처 ----------------

        static Texture2D _canopy, _canopyNormal;
        const int CanopySize = 512;

        /// <summary>R = 나무마다 다른 색 비율, G = 수관 높이(0 틈 ~ 1 꼭대기). 이음매 없이 반복.</summary>
        public static Texture2D Canopy { get { if (_canopy == null) BakeCanopy(); return _canopy; } }
        public static Texture2D CanopyNormal { get { if (_canopyNormal == null) BakeCanopy(); return _canopyNormal; } }

        static void BakeCanopy()
        {
            const int n = CanopySize;
            var h = new float[n * n];
            var tint = new float[n * n];
            // 잎 뭉치: 수관 안의 잔 굴곡 (주기 노이즈라 이음매 없음)
            var lumps = new float[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                lumps[y * n + x] = ProceduralTextures.Fbm(x / (float)n, y / (float)n, 64, 2, 7);
            var rnd = new System.Random(4242);
            float Rf(float a, float b) => (float)(a + rnd.NextDouble() * (b - a));
            // 큰 나무를 격자에 흩뿌린 뒤 작은 나무로 틈을 채운다. 나중 나무가 높으면 위에 덮인다
            const int cells = 20;
            float cell = n / (float)cells;
            for (int pass = 0; pass < 2; pass++)
            for (int cy = 0; cy < cells; cy++)
            for (int cx = 0; cx < cells; cx++)
            {
                float px = (cx + Rf(0f, 1f)) * cell, py = (cy + Rf(0f, 1f)) * cell;
                float rad = pass == 0 ? Rf(0.5f, 0.85f) * cell : Rf(0.3f, 0.5f) * cell;
                float top = pass == 0 ? Rf(0.75f, 1f) : Rf(0.55f, 0.8f);
                float tn = Rf(0f, 1f);
                int r = Mathf.CeilToInt(rad);
                for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / rad;
                    if (d >= 1f) continue;
                    int ix = ((Mathf.FloorToInt(px) + dx) % n + n) % n, iy = ((Mathf.FloorToInt(py) + dy) % n + n) % n;
                    int k = iy * n + ix;
                    float z = top * Mathf.Pow(1f - d * d, 0.6f) * (0.82f + 0.3f * lumps[k]);
                    if (z > h[k]) { h[k] = z; tint[k] = tn; }
                }
            }
            var px32 = new Color32[n * n];
            var nx32 = new Color32[n * n];
            const float strength = 5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int k = y * n + x;
                px32[k] = new Color(tint[k], h[k], 0f, 1f);
                float ddx = (h[y * n + (x + 1) % n] - h[y * n + (x - 1 + n) % n]) * strength;
                float ddy = (h[((y + 1) % n) * n + x] - h[((y - 1 + n) % n) * n + x]) * strength;
                var nv = new Vector3(-ddx, -ddy, 1f).normalized;
                nx32[k] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);
            }
            _canopy = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = "Canopy", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            _canopy.SetPixels32(px32);
            _canopy.Apply(true, true);
            _canopyNormal = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = "CanopyNormal", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            _canopyNormal.SetPixels32(nx32);
            _canopyNormal.Apply(true, true);
        }
    }
}
