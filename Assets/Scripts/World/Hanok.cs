using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 한옥 부재: 정자(동백정·해암정)와 바닷가 사당이 쓴다. 실물 사진(삼척 해암정, 서천 동백정, 경회루)을 보고 맞췄다.
    ///  - 지붕: 팔작지붕. 가운데 용마루가 있고 네 귀가 들리며(추녀), 처마 쪽으로 오목하게 휜다. 기와골(골기와 줄)이 메쉬 굴곡으로 들어가고
    ///    처마 끝은 두께가 있으며 아래로 서까래 끝이 촘촘히 나온다. 용마루·내림마루는 흰 줄눈을 넣은 두툼한 마루, 용마루 양끝은 치켜 올린 취두
    ///  - 몸: 낮은 장대석 기단 + 계단, 주춧돌, 석간주(붉은 갈색) 기둥, 기둥머리 공포(초록·붉은 단청), 창방에 단청 무늬 텍스처, 계자 난간
    /// </summary>
    public static class Hanok
    {
        static Texture2D _dancheong;
        static readonly Dictionary<string, Mesh> RoofCache = new Dictionary<string, Mesh>();

        /// <summary>
        /// 단청 무늬 (머리초): 뇌록 초록 바탕에 양끝은 붉은·남색·흰 띠가 겹친 꽃 무늬, 가운데는 긴 금문 띠. 가로 = 보 길이 방향.
        /// </summary>
        public static Texture2D Dancheong()
        {
            if (_dancheong != null) return _dancheong;
            const int W = 256, H = 32;
            _dancheong = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[W * H];
            var green = new Color(0.22f, 0.5f, 0.42f);
            var red = new Color(0.68f, 0.17f, 0.13f);
            var blue = new Color(0.16f, 0.24f, 0.5f);
            var white = new Color(0.93f, 0.9f, 0.82f);
            var yellow = new Color(0.9f, 0.72f, 0.25f);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1), v = (y + 0.5f) / H;
                float e = Mathf.Min(u, 1f - u) * 2f; // 0 = 끝, 1 = 가운데
                Color c = green;
                // 위아래 테두리: 흰 선 + 남색 선
                if (v < 0.08f || v > 0.92f) c = blue;
                else if (v < 0.13f || v > 0.87f) c = white;
                else if (e < 0.42f)
                {
                    // 머리초: 끝에서 안쪽으로 붉은 바탕 → 둥근 꽃(연화) 고리 → 흰·남 줄 띠
                    float k = e / 0.42f;
                    float cy = Mathf.Abs(v - 0.5f) * 2f;
                    float petal = Mathf.Repeat(k * 3f, 1f);
                    if (k < 0.15f) c = red;
                    else if (k < 0.7f)
                    {
                        float r = Mathf.Sqrt((petal - 0.5f) * (petal - 0.5f) * 4f + cy * cy);
                        c = r < 0.35f ? yellow : r < 0.55f ? red : r < 0.7f ? white : r < 0.85f ? blue : green;
                    }
                    else c = Mathf.Repeat(k * 14f, 1f) < 0.45f ? white : blue;
                }
                else if (Mathf.Abs(v - 0.5f) < 0.12f && Mathf.Repeat(u * 24f, 1f) < 0.6f) c = new Color(0.55f, 0.14f, 0.12f); // 가운데 금문 띠
                px[y * W + x] = c;
            }
            _dancheong.SetPixels(px);
            _dancheong.Apply(true);
            return _dancheong;
        }

        static Material DancheongMat()
        {
            var m = Mats.Lit(Color.white, 0.25f);
            if (m.mainTexture == null) m.mainTexture = Dancheong();
            return m;
        }

        /// <summary>
        /// 정자 한 채 (가로 width × 세로 depth, 처마 끝까지). 기단·주춧돌·기둥·공포·단청 창방·마루·계자 난간·팔작 기와지붕.
        /// 기둥은 앞뒤 colsX개 × 옆 colsZ개 둘레. parent 아래 at에 yaw로 놓는다.
        /// </summary>
        public static Transform Pavilion(Transform parent, Vector3 at, float yaw, float width = 6f, float depth = 6f, int colsX = 3, int colsZ = 3, float colH = 2.7f)
        {
            var p = new GameObject("Pavilion").transform;
            p.SetParent(parent, false);
            p.localPosition = at;
            p.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var stone = Mats.Weathered("stonewall", new Color(0.62f, 0.6f, 0.56f), 1.6f, 0.15f, 0f, 0.4f, 0.45f);
            var plinthStone = Mats.Weathered("marble", new Color(0.7f, 0.68f, 0.64f), 1.2f, 0.2f, 0f, 0.3f, 0.3f, null, 0.7f);
            var floor = Mats.Weathered("planks", new Color(0.45f, 0.34f, 0.24f), 2.2f, 0.25f, 0f, 0f, 0.05f);
            var pillar = Mats.Weathered("planks", new Color(0.44f, 0.16f, 0.11f), 1.5f, 0.3f, 0f, 0.15f, 0f, null, 0.7f);
            var bracketG = Mats.Lit(new Color(0.24f, 0.52f, 0.44f), 0.3f);
            var bracketR = Mats.Lit(new Color(0.66f, 0.18f, 0.13f), 0.3f);
            var rail = Mats.Weathered("planks", new Color(0.4f, 0.15f, 0.1f), 1.2f, 0.3f, 0f, 0f, 0f, null, 0.7f);
            float hw = width * 0.5f - 0.3f, hd = depth * 0.5f - 0.3f; // 기둥 줄
            float baseH = 0.75f, floorY = baseH + 0.45f;

            // 기단: 장대석 두 단 + 앞 계단 세 칸
            Mats.Prim(PrimitiveType.Cube, "Gidan", p, new Vector3(0f, baseH * 0.5f - 0.2f, 0f), new Vector3(width + 1.2f, baseH + 0.4f, depth + 1.2f), stone);
            Mats.Prim(PrimitiveType.Cube, "GidanCap", p, new Vector3(0f, baseH + 0.04f, 0f), new Vector3(width + 1.3f, 0.1f, depth + 1.3f), plinthStone, false);
            for (int s = 0; s < 3; s++)
                Mats.Prim(PrimitiveType.Cube, "Step", p, new Vector3(0f, (s + 0.5f) * baseH / 3f - 0.05f, -(depth * 0.5f + 0.6f) - (2 - s) * 0.35f - 0.2f),
                    new Vector3(1.8f, baseH / 3f * (s + 1), 0.4f), plinthStone);
            // 마루 (기둥 사이) + 마루 밑 동바리 기둥
            Mats.Prim(PrimitiveType.Cube, "Maru", p, new Vector3(0f, floorY, 0f), new Vector3(hw * 2f + 0.5f, 0.16f, hd * 2f + 0.5f), floor);
            Mats.Prim(PrimitiveType.Cube, "MaruBeam", p, new Vector3(0f, floorY - 0.18f, 0f), new Vector3(hw * 2f + 0.3f, 0.2f, hd * 2f + 0.3f), pillar, false);

            // 기둥 자리 (둘레)
            var posts = new List<Vector2>();
            for (int i = 0; i < colsX; i++)
            {
                float x = Mathf.Lerp(-hw, hw, i / (float)(colsX - 1));
                posts.Add(new Vector2(x, -hd));
                posts.Add(new Vector2(x, hd));
            }
            for (int j = 1; j < colsZ - 1; j++)
            {
                float z = Mathf.Lerp(-hd, hd, j / (float)(colsZ - 1));
                posts.Add(new Vector2(-hw, z));
                posts.Add(new Vector2(hw, z));
            }
            float topY = floorY + colH;
            foreach (var c in posts)
            {
                var b = new Vector3(c.x, baseH, c.y);
                // 주춧돌 (덤벙주초처럼 살짝 둥근 돌) + 기둥 (위가 약간 가는 민흘림)
                Mats.Prim(PrimitiveType.Cylinder, "Juchu", p, b + Vector3.up * 0.12f, new Vector3(0.62f, 0.12f, 0.62f), plinthStone, false);
                MapKit.Place(p, "Gidung", MapKit.Lathe(new[] { new Vector2(0.2f, 0f), new Vector2(0.19f, colH * 0.5f), new Vector2(0.17f, colH + 0.25f) }, 14), pillar, b + Vector3.up * 0.2f);
                // 공포: 주두(네모 받침) + 엇갈린 첨차 둘 (초록 바탕에 붉은 테)
                var head = new Vector3(c.x, topY + 0.2f, c.y);
                Mats.Prim(PrimitiveType.Cube, "Judu", p, head, new Vector3(0.48f, 0.22f, 0.48f), bracketG, false);
                Mats.Prim(PrimitiveType.Cube, "Cheomcha", p, head + Vector3.up * 0.2f, new Vector3(1.1f, 0.18f, 0.26f), bracketR, false);
                Mats.Prim(PrimitiveType.Cube, "Cheomcha2", p, head + Vector3.up * 0.2f, new Vector3(0.26f, 0.18f, 1.1f), bracketR, false);
                Mats.Prim(PrimitiveType.Cube, "Soro", p, head + Vector3.up * 0.36f, new Vector3(0.36f, 0.14f, 0.36f), bracketG, false);
            }
            // 창방 (기둥머리를 잇는 보, 단청 텍스처: 가로 = 보 길이)
            var dan = DancheongMat();
            float beamY = topY + 0.05f;
            foreach (var (pos, len, rot) in new[] { (new Vector3(0f, beamY, -hd), hw * 2f, 0f), (new Vector3(0f, beamY, hd), hw * 2f, 0f),
                         (new Vector3(-hw, beamY, 0f), hd * 2f, 90f), (new Vector3(hw, beamY, 0f), hd * 2f, 90f) })
            {
                Mats.Prim(PrimitiveType.Cube, "Changbang", p, pos, new Vector3(len, 0.42f, 0.3f), dan, false, Quaternion.Euler(0f, rot, 0f));
                // 위 평방 (가는 초록 보)
                Mats.Prim(PrimitiveType.Cube, "Pyeongbang", p, pos + Vector3.up * 0.42f, new Vector3(len + 0.3f, 0.2f, 0.26f), bracketG, false, Quaternion.Euler(0f, rot, 0f));
            }
            // 계자 난간: 마루 둘레(앞 가운데 칸은 드나드는 곳이라 비움). 아래 머름대 + 살대 + 위 둥근 돌란대
            float railY = floorY + 0.55f;
            void Railing(Vector3 a, Vector3 bb)
            {
                var d = bb - a;
                float L = d.magnitude;
                var q = Quaternion.LookRotation(d.normalized);
                Mats.Prim(PrimitiveType.Cube, "RailBottom", p, (a + bb) * 0.5f + Vector3.up * (floorY + 0.15f), new Vector3(0.08f, 0.14f, L), rail, false, q);
                Mats.Prim(PrimitiveType.Cube, "RailTop", p, (a + bb) * 0.5f + Vector3.up * railY, new Vector3(0.1f, 0.08f, L + 0.1f), rail, false, q);
                int n = Mathf.Max(2, Mathf.RoundToInt(L / 0.32f));
                for (int k = 1; k < n; k++)
                {
                    var at2 = Vector3.Lerp(a, bb, k / (float)n) + Vector3.up * (floorY + 0.36f);
                    Mats.Prim(PrimitiveType.Cube, "RailBar", p, at2, new Vector3(0.04f, 0.32f, 0.04f), rail, false, q);
                }
            }
            float rx = hw + 0.12f, rz = hd + 0.12f;
            Railing(new Vector3(-rx, 0f, rz), new Vector3(rx, 0f, rz));
            Railing(new Vector3(-rx, 0f, -rz), new Vector3(-rx, 0f, rz));
            Railing(new Vector3(rx, 0f, -rz), new Vector3(rx, 0f, rz));
            Railing(new Vector3(-rx, 0f, -rz), new Vector3(-0.8f, 0f, -rz));
            Railing(new Vector3(0.8f, 0f, -rz), new Vector3(rx, 0f, -rz));

            // 지붕
            float ew = width * 0.5f + 1.5f, ed = depth * 0.5f + 1.5f;
            var roof = new GameObject("Roof");
            roof.transform.SetParent(p, false);
            roof.transform.localPosition = new Vector3(0f, topY + 0.72f, 0f);
            roof.AddComponent<MeshFilter>().sharedMesh = Roof(ew, ed, Mathf.Max(ew, ed) * 0.55f, 0.55f);
            var mr = roof.AddComponent<MeshRenderer>();
            mr.sharedMaterials = RoofMaterials();
            roof.AddComponent<MeshCollider>();
            return p;
        }

        /// <summary>지붕 서브메쉬 순서: 기와, 처마 밑(서까래 사이 초록 단청), 마루(흰 줄눈 짙은 회색), 서까래 끝(붉은·흰 테).</summary>
        public static Material[] RoofMaterials() => new[]
        {
            Mats.Weathered("greytile", new Color(0.3f, 0.31f, 0.33f), 1.2f, 0.32f, 0f, 0.35f, 0.25f, new Color(0.3f, 0.33f, 0.22f), 0.85f),
            Mats.Lit(new Color(0.24f, 0.46f, 0.4f), 0.2f),
            Mats.Lit(new Color(0.26f, 0.27f, 0.3f), 0.35f),
            Mats.Lit(new Color(0.62f, 0.2f, 0.15f), 0.3f),
        };

        /// <summary>
        /// 팔작 기와지붕 메쉬. 처마 끝 반폭 hx(가로)·hz(세로), 높이 rise, 귀 들림 lift. 원점 = 처마 밑 가운데.
        ///  - 윗면: 처마→용마루로 오르는 오목한 곡면. 기와골을 따라 사인 굴곡(골 0.32m 간격)
        ///  - 처마 끝 두께 0.32m, 처마 밑면(초록), 처마 끝 아래 서까래 끝 (0.3m 간격 원기둥 머리)
        ///  - 용마루(양끝 취두가 들린다)와 네 귀의 내림마루
        /// </summary>
        public static Mesh Roof(float hx, float hz, float rise, float lift)
        {
            string key = $"{hx:F2}{hz:F2}{rise:F2}{lift:F2}";
            if (RoofCache.TryGetValue(key, out var cached)) return cached;
            var V = new List<Vector3>();
            var tiles = new List<int>();
            var under = new List<int>();
            var ridge = new List<int>();
            var rafter = new List<int>();
            float ridgeHalf = Mathf.Max(0.3f, hx - hz) + hz * 0.35f; // 용마루 반길이 (팔작이라 맞배 부분이 조금 있다)
            const float thick = 0.32f;

            // 높이: 바깥으로부터의 거리 m(0 = 처마 끝, 1 = 용마루)
            float Height(float x, float z, out float m)
            {
                float ax = Mathf.Abs(x), az = Mathf.Abs(z);
                float mz = 1f - az / hz;                                         // 앞뒤 면
                float mx = 1f - Mathf.Max(0f, ax - ridgeHalf) / Mathf.Max(0.01f, hx - ridgeHalf); // 옆 면 (합각 쪽)
                mx = Mathf.Lerp(mx, 1f - ax / hx, 0.35f);
                m = Mathf.Clamp01(Mathf.Min(mz, mx));
                float y = rise * (1f - Mathf.Pow(1f - m, 1.7f)) * 0.92f + rise * 0.08f * m;  // 오목: 처마 쪽은 완만, 위로 갈수록 가파름
                // 귀 들림 (추녀): 네 귀로 갈수록 올라간다, 처마 앞면 가운데도 아주 살짝(안허리곡)
                float cx = ax / hx, cz = az / hz;
                y += lift * Mathf.Pow(cx * cz, 2.2f) + lift * 0.25f * (Mathf.Pow(cx, 4f) + Mathf.Pow(cz, 4f)) * (1f - m);
                return y;
            }

            // 윗면 격자
            const int nx = 56, nz = 44;
            int top0 = V.Count;
            for (int iz = 0; iz <= nz; iz++)
            for (int ix = 0; ix <= nx; ix++)
            {
                float x = (ix / (float)nx * 2f - 1f) * hx, z = (iz / (float)nz * 2f - 1f) * hz;
                float y = Height(x, z, out float m);
                // 기와골: 면이 앞뒤로 기울면 x 방향으로, 옆으로 기울면 z 방향으로 물결 (골기와 줄)
                float ax = Mathf.Abs(x), az = Mathf.Abs(z);
                bool sideFace = (1f - az / hz) > (1f - Mathf.Max(0f, ax - ridgeHalf) / Mathf.Max(0.01f, hx - ridgeHalf));
                float along = sideFace ? z : x;
                float wave = Mathf.Abs(Mathf.Sin(along / 0.32f * Mathf.PI));
                // 처마 끝 0.5m는 굴곡을 줄인다 (끝은 수막새가 덮는다 — 그대로 두면 처마 판이 톱니처럼 보인다)
                y += (Mathf.Sqrt(wave) - 0.6f) * 0.07f * (1f - m * 0.6f) * Mathf.SmoothStep(0f, 1f, m / 0.08f);
                V.Add(new Vector3(x, y, z));
            }
            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                int a = top0 + iz * (nx + 1) + ix, b = a + 1, c = a + nx + 1, d = c + 1;
                tiles.AddRange(new[] { a, c, b, b, c, d });
            }

            // 처마 둘레: 윗면 가장자리 → 두께만큼 아래 → 안쪽으로 1.6m 들어간 밑면 (서까래가 받친 처마 밑)
            var ring = new List<Vector3>();
            for (int ix = 0; ix < nx; ix++) ring.Add(V[top0 + ix]);
            for (int iz = 0; iz < nz; iz++) ring.Add(V[top0 + iz * (nx + 1) + nx]);
            for (int ix = nx; ix > 0; ix--) ring.Add(V[top0 + nz * (nx + 1) + ix]);
            for (int iz = nz; iz > 0; iz--) ring.Add(V[top0 + iz * (nx + 1)]);
            int rn = ring.Count;
            int e0 = V.Count;
            foreach (var r in ring) V.Add(r);
            foreach (var r in ring) V.Add(r - Vector3.up * thick);
            foreach (var r in ring)
            {
                var inward = new Vector3(r.x * (1f - 1.5f / hx), 0f, r.z * (1f - 1.5f / hz));
                V.Add(new Vector3(inward.x, Height(inward.x, inward.z, out _) - thick - 0.15f, inward.z));
            }
            for (int i = 0; i < rn; i++)
            {
                int j = (i + 1) % rn;
                int a0 = e0 + i, a1 = e0 + j, b0 = e0 + rn + i, b1 = e0 + rn + j, c0 = e0 + 2 * rn + i, c1 = e0 + 2 * rn + j;
                // 링은 위에서 보아 시계 반대(+x → +z → -x → -z)라 바깥을 보게 감는다
                tiles.AddRange(new[] { a0, a1, b0, b0, a1, b1 });
                under.AddRange(new[] { b0, b1, c0, c0, b1, c1 });
            }
            // 처마 밑 안쪽 막음 (밑면 링 → 가운데)
            int mid = V.Count;
            V.Add(new Vector3(0f, rise * 0.3f, 0f));
            for (int i = 0; i < rn; i++) under.AddRange(new[] { e0 + 2 * rn + i, e0 + 2 * rn + (i + 1) % rn, mid });

            // 처마 끝: 수막새(둥근 끝 기와, 기와골마다) + 그 아래 서까래 끝 (처마 판 밑으로 0.12m 나온 네모 머리)
            for (int i = 0; i < rn; i++)
            {
                var r = ring[i];
                bool front = Mathf.Abs(r.z) > hz - 0.01f, sideE = Mathf.Abs(r.x) > hx - 0.01f;
                var outward = front ? new Vector3(0f, 0f, Mathf.Sign(r.z)) : sideE ? new Vector3(Mathf.Sign(r.x), 0f, 0f) : Vector3.zero;
                if (outward == Vector3.zero) continue;
                float along = front ? r.x : r.z;
                float edgeLen = front ? hx : hz;
                // 기와골(0.32m)마다 하나씩: 격자 점이 골 가운데에 가장 가까운 것만
                float phase = Mathf.Repeat(along / 0.32f + 0.5f, 1f);
                float step = (front ? hx : hz) * 2f / (front ? nx : nz) / 0.32f;
                if (phase < 0.5f - step * 0.5f || phase >= 0.5f + step * 0.5f || Mathf.Abs(along) > edgeLen - 0.15f) continue;
                AddCylinder(V, tiles, r + outward * 0.04f + Vector3.up * 0.02f, outward, 0.11f, 0.3f, 10);
                AddBox(V, rafter, r - Vector3.up * (thick + 0.07f) + outward * 0.02f, front ? new Vector3(0.13f, 0.13f, 0.4f) : new Vector3(0.4f, 0.13f, 0.13f), Quaternion.identity);
            }

            // 용마루: 두툼한 마루 + 양끝 들린 취두
            float ry = Height(0f, 0f, out _);
            AddBox(V, ridge, new Vector3(0f, ry + 0.18f, 0f), new Vector3(ridgeHalf * 2f, 0.42f, 0.42f), Quaternion.identity);
            AddBox(V, ridge, new Vector3(0f, ry + 0.42f, 0f), new Vector3(ridgeHalf * 2f - 0.2f, 0.12f, 0.3f), Quaternion.identity);
            foreach (float s in new[] { -1f, 1f })
                AddBox(V, ridge, new Vector3(s * (ridgeHalf + 0.05f), ry + 0.42f, 0f), new Vector3(0.36f, 0.7f, 0.44f), Quaternion.Euler(0f, 0f, -s * 18f));
            // 내림마루·추녀마루: 용마루 끝 → 네 귀, 표면을 따라 여러 토막
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                var a = new Vector3(sx * ridgeHalf, 0f, 0f);
                var b = new Vector3(sx * (hx - 0.25f), 0f, sz * (hz - 0.25f));
                const int segs = 8;
                for (int k = 0; k < segs; k++)
                {
                    var p0 = Vector3.Lerp(a, b, k / (float)segs);
                    var p1 = Vector3.Lerp(a, b, (k + 1) / (float)segs);
                    p0.y = Height(p0.x, p0.z, out _) + 0.12f;
                    p1.y = Height(p1.x, p1.z, out _) + 0.12f;
                    var d = p1 - p0;
                    AddBox(V, ridge, (p0 + p1) * 0.5f, new Vector3(0.3f, 0.26f, d.magnitude + 0.05f), Quaternion.LookRotation(d.normalized));
                }
            }
            // 합각(옆 삼각 벽): 용마루 끝 아래 작은 삼각 판
            foreach (float sx in new[] { -1f, 1f })
            {
                int t0 = V.Count;
                float gx = sx * (ridgeHalf - 0.05f);
                V.Add(new Vector3(gx, ry + 0.05f, 0f));
                V.Add(new Vector3(gx, Height(gx, hz * 0.4f, out _) - 0.05f, hz * 0.42f));
                V.Add(new Vector3(gx, Height(gx, -hz * 0.4f, out _) - 0.05f, -hz * 0.42f));
                if (sx > 0) under.AddRange(new[] { t0, t0 + 1, t0 + 2 }); else under.AddRange(new[] { t0, t0 + 2, t0 + 1 });
            }

            var mesh = new Mesh { name = "HanokRoof", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(V);
            mesh.subMeshCount = 4;
            mesh.SetTriangles(tiles, 0);
            mesh.SetTriangles(under, 1);
            mesh.SetTriangles(ridge, 2);
            mesh.SetTriangles(rafter, 3);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            RoofCache[key] = mesh;
            return mesh;
        }

        /// <summary>면마다 꼭짓점을 따로 둔 상자 (RecalculateNormals가 모서리를 뭉개지 않게).</summary>
        static void AddBox(List<Vector3> v, List<int> t, Vector3 c, Vector3 size, Quaternion q)
        {
            var h = size * 0.5f;
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            for (int ax = 0; ax < 3; ax++)
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                var n = axes[ax] * sgn;
                var u = axes[(ax + 1) % 3];
                var w = axes[(ax + 2) % 3];
                float hn = Vector3.Dot(h, axes[ax]), hu = Vector3.Dot(h, u), hw = Vector3.Dot(h, w);
                int s0 = v.Count;
                v.Add(c + q * (n * hn - u * hu - w * hw));
                v.Add(c + q * (n * hn + u * hu - w * hw));
                v.Add(c + q * (n * hn + u * hu + w * hw));
                v.Add(c + q * (n * hn - u * hu + w * hw));
                // 왼손 좌표계: n 쪽에서 보면 0-1-2가 시계 방향(= Unity 앞면). 반대쪽 면은 뒤집는다
                if (sgn > 0) t.AddRange(new[] { s0, s0 + 1, s0 + 2, s0, s0 + 2, s0 + 3 });
                else t.AddRange(new[] { s0, s0 + 2, s0 + 1, s0, s0 + 3, s0 + 2 });
            }
        }

        /// <summary>원기둥 (tip = 바깥 끝면 가운데, axis 방향 바깥, 안쪽으로 len). 끝면 꼭짓점은 따로.</summary>
        static void AddCylinder(List<Vector3> v, List<int> t, Vector3 tip, Vector3 axis, float r, float len, int seg)
        {
            var a = Vector3.Cross(axis, Vector3.up).normalized;
            if (a.sqrMagnitude < 0.01f) a = Vector3.right;
            var b = Vector3.Cross(axis, a);
            int s = v.Count;
            for (int i = 0; i < seg; i++)
            {
                float th = i / (float)seg * Mathf.PI * 2f;
                var o = (Mathf.Cos(th) * a + Mathf.Sin(th) * b) * r;
                v.Add(tip + o);
                v.Add(tip - axis * len + o);
            }
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                t.AddRange(new[] { s + i * 2, s + i * 2 + 1, s + j * 2, s + j * 2, s + i * 2 + 1, s + j * 2 + 1 });
            }
            int c = v.Count;
            v.Add(tip);
            for (int i = 0; i < seg; i++)
            {
                float th = i / (float)seg * Mathf.PI * 2f;
                v.Add(tip + (Mathf.Cos(th) * a + Mathf.Sin(th) * b) * r);
            }
            for (int i = 0; i < seg; i++) t.AddRange(new[] { c, c + 1 + i, c + 1 + (i + 1) % seg });
        }
    }
}
