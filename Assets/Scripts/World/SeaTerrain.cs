using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 홍현 앞바다 지형 (Unity Terrain). 예전에는 상자·구 프리미티브였던 땅을 높이맵으로 만든다.
    ///  - 해안: 마을 길(LandY)에서 물가로 6m 내려가는 모래·자갈 비탈 (포구 앞은 방파제·호안이 덮는다)
    ///  - 다랭이논: 12m마다 2.2m씩 오르는 계단 논. 턱(논두렁 비탈)은 바위·흙, 논바닥은 풀
    ///  - 갯바위 섬: 윗면은 LandY 높이의 평평한 바위(내려서 낚시), 둘레는 물속으로 꺼지는 바위 비탈, 봉우리
    ///  - 바다 밑: 물가에서 멀어질수록 깊어진다 (물이 불투명이라 물가 근처만 보인다)
    /// 텍스처: Poly Haven CC0 (Resources/Terrain, SOURCES.md) — 모래, 풀, 바위, 자갈 해변.
    /// 걸을 수 있는 곳 판정은 그대로 HonghyeonDef.IsWalkable(평면)이고, 높이는 지형 콜라이더가 맡는다.
    /// </summary>
    public static class SeaTerrain
    {
        const float MinX = -210f, MinZ = -200f, SizeX = 420f, SizeZ = 470f;
        const float BaseY = -12f, Height = 44f;
        const int Res = 1025;     // 높이맵 (420m / 1024 ≈ 0.4m 간격 — 갯바위 둔덕의 턱·틈이 보이게)
        const int SplatRes = 512;

        static float _landY, _waterY;

        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>월드 좌표의 지형 높이 (건물·나무를 지형 위에 놓을 때도 쓴다).</summary>
        public static float HeightAt(float x, float z)
        {
            float shore = HonghyeonDef.ShoreZ;
            float y;
            if (z > shore + 2f)
            {
                // 바다 밑: 물가에서 멀어질수록 깊게, 살짝 울퉁불퉁
                y = _waterY - 1.2f - (z - shore - 2f) * 0.14f + Mathf.PerlinNoise(x * 0.03f, z * 0.03f) * 1.5f;
                y = Mathf.Max(y, BaseY + 1f);
            }
            else if (z > shore - 6f)
            {
                // 해변 비탈: 마을 길 높이 → 물속
                float t = Smooth(shore - 6f, shore + 2f, z);
                float bump = (Mathf.PerlinNoise(x * 0.15f, z * 0.15f) - 0.5f) * 0.5f;
                y = Mathf.Lerp(_landY, _waterY - 1.2f, t) + bump * t * (1f - t) * 4f;
            }
            else
            {
                y = _landY;
                // 다랭이논: z = -72부터 남쪽으로 12m마다 한 칸씩 오른다. 논둑 선은 등고선처럼 살짝 굽는다
                float zz = z + Mathf.Sin(x * 0.021f) * 3.5f + Mathf.Sin(x * 0.057f + 1.3f) * 1.2f;
                float start = -72f;
                if (zz < start)
                {
                    float steps = (start - zz) / 12f;
                    int n = Mathf.FloorToInt(steps);
                    float frac = steps - n;
                    n = Mathf.Min(n, 9);
                    float riser = Smooth(0f, 0.14f, frac);   // 칸의 앞 14%가 논두렁 비탈
                    y = _landY + (n + riser) * 2.2f;
                    // 맨 뒤는 산으로
                    if (zz < start - 120f) y += (start - 120f - zz) * 0.35f;
                }
            }

            // 갯바위 섬
            for (int i = 0; i < HonghyeonDef.Islands.Length; i++)
            {
                var c = HonghyeonDef.Islands[i];
                float r = HonghyeonDef.IslandRadius[i];
                float d = new Vector2(x - c.x, z - c.z).magnitude;
                if (d > r + 10f) continue;
                float rough = (Mathf.PerlinNoise(x * 0.18f + i * 10f, z * 0.18f) - 0.5f);
                float top = _landY + rough * 0.3f;
                float island;
                if (d < r - 2.5f) island = top;
                else
                {
                    float t = Smooth(r - 2.5f, r + 8f, d + rough * 3f);
                    island = Mathf.Lerp(top, _waterY - 6f, t);
                }
                // 봉우리: 길쭉한 바위 둔덕 + 어깨 둔덕 하나 (예전 가우스 언덕은 매끈한 원뿔로 보였다, 2026-10-27 사용자)
                var peak = new Vector2(c.x + r * 0.35f, c.z - r * 0.3f);
                float ang = 0.6f + i * 1.1f;
                var ax = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                island += Outcrop(x, z, peak, ax, r * 0.5f, r * 0.33f, 7.5f * (0.85f + rough * 0.3f), i * 13.7f);
                island += Outcrop(x, z, peak - ax * (r * 0.42f) + new Vector2(-ax.y, ax.x) * (r * 0.12f), ax, r * 0.28f, r * 0.2f, 3.2f, i * 13.7f + 5.3f);
                y = Mathf.Max(y, island);
            }
            return y;
        }

        /// <summary>
        /// 갯바위 둔덕 하나의 높이: 타원 바닥(ax 방향 반지름 lenR, 옆 widR), 옆이 가파른 돔에
        /// 기울어진 퇴적층 턱(1.1m마다 계단, 한쪽으로 기움), 갈라진 틈(ridged 노이즈), 잔 울퉁불퉁을 얹는다.
        /// </summary>
        static float Outcrop(float x, float z, Vector2 center, Vector2 ax, float lenR, float widR, float height, float seed)
        {
            var q = new Vector2(x - center.x, z - center.y);
            float u = Vector2.Dot(q, ax) / lenR, v = (q.y * ax.x - q.x * ax.y) / widR;
            float e = Mathf.Sqrt(u * u + v * v) + (Mathf.PerlinNoise(x * 0.12f + seed, z * 0.12f) - 0.5f) * 0.4f;
            if (e >= 1f) return 0f;
            float body = Mathf.Pow(1f - e, 0.6f);
            float raw = body * height;
            float inside = Smooth(0f, 0.2f, body);
            // 퇴적층 턱: 층이 ax 쪽으로 기울어 있어 한쪽은 계단, 반대쪽은 비스듬한 판
            float tilt = Vector2.Dot(q, ax) * 0.14f;
            float lc = (raw + tilt) / 1.1f;
            float fl = Mathf.Floor(lc);
            float stepped = (fl + Smooth(0.55f, 0.95f, lc - fl)) * 1.1f - tilt;
            raw = Mathf.Lerp(raw, stepped, 0.7f * inside);
            // 갈라진 틈
            float cr = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.21f + 3.1f + seed, z * 0.21f) * 2f - 1f);
            raw -= Mathf.Pow(cr, 10f) * 0.9f * inside;
            // 잔 울퉁불퉁
            raw += (Mathf.PerlinNoise(x * 0.75f + seed, z * 0.75f) - 0.5f) * 0.4f * inside;
            return Mathf.Max(0f, raw);
        }

        public static Terrain Build(Transform parent, float landY, float waterY)
        {
            _landY = landY;
            _waterY = waterY;
            var layers = new[]
            {
                new TerrainBuilder.LayerSpec("grass", 9f, 0.05f),
                new TerrainBuilder.LayerSpec("sand", 6f, 0.15f),
                new TerrainBuilder.LayerSpec("rock", 11f, 0.2f),
                new TerrainBuilder.LayerSpec("shore", 7f, 0.25f),
            };
            return TerrainBuilder.Build(parent, "SeaTerrain", new Rect(MinX, MinZ, SizeX, SizeZ), BaseY, Height, Res, HeightAt, layers,
                (x, z, y, steep, w) =>
                {
                    float n = Mathf.PerlinNoise(x * 0.08f, z * 0.08f);
                    bool island = false;
                    for (int i = 0; i < HonghyeonDef.Islands.Length; i++)
                        if (new Vector2(x - HonghyeonDef.Islands[i].x, z - HonghyeonDef.Islands[i].z).magnitude < HonghyeonDef.IslandRadius[i] + 10f) island = true;
                    float rock = Smooth(22f, 34f, steep);
                    float sand = 0f, shore = 0f;
                    if (!island)
                    {
                        // 해변: 물가 근처 높이대. 서쪽은 모래, 동쪽(포구 쪽)은 자갈
                        float beach = Smooth(landY + 0.4f, landY - 0.2f, y) * Smooth(waterY - 3f, waterY - 1f, y);
                        float west = Smooth(10f, -40f, x + (n - 0.5f) * 30f);
                        sand = beach * west;
                        shore = beach * (1f - west);
                        if (y < waterY - 1f) sand = 1f; // 물속은 모래
                    }
                    else
                    {
                        // 섬: 윗면은 바위, 물가는 젖은 자갈
                        rock = Mathf.Max(rock, 0.55f + n * 0.4f);
                        shore = Smooth(landY - 0.3f, waterY + 0.2f, y);
                    }
                    w[0] = Mathf.Max(0f, 1f - rock - sand - shore);
                    w[1] = sand;
                    w[2] = rock;
                    w[3] = shore;
                }, SplatRes);
        }
    }
}
