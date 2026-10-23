using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 한강(반포)에서 바라보는 서울 랜드마크와 먼 도시. 모두 코드로 만든 메쉬 + 코드로 그린 텍스처.
    ///  - 롯데월드타워(동쪽 잠실, 123층): 위로 갈수록 붓끝처럼 좁아지는 곡선 탑. 은빛 유리에 흰 층선, 네 면 가운데를 타고 오르는 이음매(seam),
    ///    꼭대기 107~123층과 첨탑은 'ㅅ'자 강관을 이은 다이아그리드 격자(구멍 뚫린 양면 재질)로 네 갈래가 벌어지며 모인다. 밤엔 창과 왕관이 하얗게
    ///  - 63빌딩(서쪽 여의도, 60층): 금빛 유리. 강 쪽으로 살짝 꺾인 평면을 여섯 조각으로 나눠, 조각마다 높이가 계단처럼 낮아지는 사선 지붕 + 안테나
    ///  - 남산서울타워(북쪽 남산 위): 숲 덮인 산(노이즈 돔) 위 흰 콘크리트 기둥, 두 겹 전망대 원통(창), 빨강·흰 줄무늬 송신탑. 밤엔 기둥이 푸르게
    ///  - 먼 도시: 강 건너 수백 m에 아파트·빌딩 상자 수백 채 (밤엔 창 불빛), 동서로 이어지는 강과 땅
    /// 실제 거리(롯데 약 9km)라면 점처럼 작아서, 거리를 줄이고 높이도 줄여 화면에서 실제처럼 크게 보이게 했다.
    /// 셰이더 Fishing/Landmark: 안개를 맵의 0.35배만 받아 수백 m 밖에서도 실루엣이 산다.
    /// </summary>
    public static class HanRiverLandmarks
    {
        // 반포 데크(0, -40) 기준 배치. +x 동쪽, +z 북쪽
        public static readonly Vector3 LottePos = new Vector3(470f, HanRiverMap.LandY, 560f);
        public static readonly Vector3 SixtyThreePos = new Vector3(-520f, HanRiverMap.LandY, 300f);
        public static readonly Vector3 NamsanPos = new Vector3(60f, HanRiverMap.LandY, 470f);
        const float LotteHeight = 300f;      // 실제 555m
        const float SixtyThreeHeight = 150f; // 실제 249m
        const float NamsanHill = 112f, NamsanTower = 150f; // 실제 산 262m + 탑 236m

        static System.Random _rng;
        static float R(float a, float b) => (float)(a + _rng.NextDouble() * (b - a));

        public static void Build(Transform parent)
        {
            _rng = new System.Random(555);
            var root = new GameObject("Landmarks").transform;
            root.SetParent(parent, false);
            FarLand(root);
            FarCity(root);
            Lotte(root);
            SixtyThree(root);
            Namsan(root);
        }

        // ---------- 재질 ----------

        internal static Material Mat(Color color, Texture2D tex, Texture2D night, Color nightColor, float smooth, float metal, Vector2 tiling, int nightTiles = 1)
        {
            var shader = Shader.Find("Fishing/Landmark") ?? Shader.Find("Standard");
            var m = new Material(shader) { color = color };
            if (tex != null) { m.mainTexture = tex; m.mainTextureScale = tiling; }
            if (night != null) m.SetTexture("_EmissionMap", night);
            m.SetFloat("_NightTiles", nightTiles);
            m.SetColor("_EmissionColor", nightColor);
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_FogScale", 0.22f);
            return m;
        }

        // ---------- 텍스처 (코드로) ----------

        /// <summary>
        /// 커튼월 한 장 = floors층 × bays칸. 유리(하늘이 비친 세로 그러데이션) + 층마다 흰/금 스팬드럴 띠 + 세로 멀리언.
        /// night: 창마다 켜짐·꺼짐(따뜻한 빛, 사무실 형광등빛 섞어서). nightTiles > 1이면 낮 한 장의 nightTiles × nightTiles배를 덮는
        /// 아틀라스로 만들어(재질에도 같은 값, Mat(nightTiles)) 건물이 커도 불빛 배치가 되풀이되지 않는다.
        /// </summary>
        internal static Texture2D Facade(int floors, int bays, Color glassTop, Color glassBottom, Color frame, float bandFrac, float mullionFrac, int seed, out Texture2D night, float litChance, int nightTiles = 1, int nightCellPx = 16)
        {
            const int W = 256, H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)W * bays, v = y / (float)H * floors;
                int bx = Mathf.Min(bays - 1, (int)u), fy = Mathf.Min(floors - 1, (int)v);
                float fu = u - bx, fv = v - fy;
                bool band = fv < bandFrac;
                bool mullion = fu < mullionFrac * 0.5f || fu > 1f - mullionFrac * 0.5f;
                // 유리: 위는 하늘빛, 아래는 짙게 + 패널마다 반사가 조금씩 다르다
                float jitter = (Mathf.PerlinNoise(bx * 0.73f + seed, fy * 0.61f) - 0.5f) * 0.12f;
                var glass = Color.Lerp(glassBottom, glassTop, Mathf.Clamp01(fv * 0.8f + 0.1f + jitter));
                Color c = band ? frame * (0.9f + 0.1f * fv / Mathf.Max(0.01f, bandFrac)) : mullion ? frame * 0.85f : glass;
                c.a = band || mullion ? 0.35f : 1f; // 알파 = 광택 (프레임은 덜 번들)
                px[y * W + x] = c;
            }
            tex.SetPixels(px); tex.Apply(true);

            // 밤 불빛: 창 한 칸에 nightCellPx 이상
            int nb = bays * nightTiles, nf = floors * nightTiles;
            int NW = Mathf.Max(256, nb * nightCellPx), NH = Mathf.Max(256, nf * nightCellPx);
            night = new Texture2D(NW, NH, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var npx = new Color[NW * NH];
            var lit = LitPattern(nf, nb, litChance, new System.Random(seed));
            for (int y = 0; y < NH; y++)
            for (int x = 0; x < NW; x++)
            {
                float u = x / (float)NW * nb, v = y / (float)NH * nf;
                int bx = Mathf.Min(nb - 1, (int)u), fy = Mathf.Min(nf - 1, (int)v);
                float fu = u - bx, fv = v - fy;
                bool framepx = fv < bandFrac || fu < mullionFrac * 0.5f || fu > 1f - mullionFrac * 0.5f;
                float l = lit[fy * nb + bx];
                npx[y * NW + x] = framepx || l == 0f ? Color.black
                    : (l > 0f ? new Color(1f, 0.82f, 0.55f) * l : new Color(0.8f, 0.9f, 1f) * -l) * (0.75f + 0.25f * fv);
            }
            night.SetPixels(npx); night.Apply(true);
            return tex;
        }

        /// <summary>
        /// 창마다 불빛 세기(0 꺼짐, + 따뜻한 빛, - 형광등빛). 한 칸씩 동전 던지기면 고르게 흩뿌린 무늬가 되니
        /// 집(1~3칸) 단위로 같이 켜고 끄고, 층마다 켜진 정도가 다르며 가끔 층 하나가 통째로 켜지거나(사무실·복도) 거의 다 꺼진다.
        /// </summary>
        static float[] LitPattern(int floors, int bays, float litChance, System.Random rng)
        {
            var lit = new float[floors * bays];
            if (litChance <= 0f) return lit;
            for (int f = 0; f < floors; f++)
            {
                double roll = rng.NextDouble();
                if (floors >= 3 && roll < 0.05)
                {
                    // 층 통째로 켜짐: 한 가지 빛
                    float b = (float)(0.8 + rng.NextDouble() * 0.2) * (rng.NextDouble() < 0.5 ? -1f : 1f);
                    for (int x = 0; x < bays; x++) lit[f * bays + x] = b * (float)(0.9 + rng.NextDouble() * 0.1);
                    continue;
                }
                // 층마다 켜진 정도: 거의 빈 층 ~ 꽤 켜진 층
                float chance = litChance * (floors >= 3 && roll < 0.2 ? 0.15f : (float)(0.4 + rng.NextDouble() * 0.9));
                for (int x = 0; x < bays;)
                {
                    int unit = 1 + rng.Next(3);
                    bool on = rng.NextDouble() < chance;
                    float b = (float)(0.45 + rng.NextDouble() * 0.55) * (rng.NextDouble() < 0.3 ? -1f : 1f);
                    for (int k = 0; k < unit && x < bays; k++, x++)
                        lit[f * bays + x] = on && rng.NextDouble() > 0.2 ? b * (float)(0.85 + rng.NextDouble() * 0.15) : 0f;
                }
            }
            return lit;
        }

        /// <summary>다이아그리드: 'ㅅ'자로 이은 흰 강관 격자. 칸은 비어(알파 0) 하늘이 보이고, 일부 칸엔 유리.</summary>
        static Texture2D Diagrid()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = x / (float)S, v = y / (float)S;
                // 마름모 격자 두 칸
                float d1 = Mathf.Abs(Mathf.Repeat(u * 2f + v * 2f, 1f) - 0.5f);
                float d2 = Mathf.Abs(Mathf.Repeat(u * 2f - v * 2f, 1f) - 0.5f);
                float tube = Mathf.Max(Mathf.SmoothStep(0.44f, 0.47f, d1), Mathf.SmoothStep(0.44f, 0.47f, d2));
                float ring = Mathf.SmoothStep(0.47f, 0.49f, Mathf.Abs(Mathf.Repeat(v * 4f, 1f) - 0.5f)) * 0.8f; // 층 링
                float solid = Mathf.Max(tube, ring);
                bool glassCell = Mathf.Repeat(Mathf.Floor(u * 2f + v * 2f) + Mathf.Floor(u * 2f - v * 2f), 3f) < 1f;
                var c = solid > 0.5f ? new Color(0.93f, 0.94f, 0.95f, 1f) : glassCell ? new Color(0.62f, 0.72f, 0.82f, 0.75f) : new Color(0, 0, 0, 0f);
                px[y * S + x] = c;
            }
            tex.SetPixels(px); tex.Apply(true);
            return tex;
        }

        static Texture2D Stripes(Color a, Color b, int bands)
        {
            var tex = new Texture2D(4, 64, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[4 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 4; x++)
                px[y * 4 + x] = (y * bands / 64) % 2 == 0 ? a : b;
            tex.SetPixels(px); tex.Apply(true);
            return tex;
        }

        // ---------- 메쉬 도우미 ----------

        /// <summary>
        /// 고리(단면)들을 위로 이어 붙인 로프트. rings[i]는 같은 개수의 점(시계 반대, 위에서 볼 때), ys[i]는 높이.
        /// UV: u = 둘레 길이 / tileU, v = 높이 / tileV. capTop이면 맨 위를 부채꼴로 닫는다.
        /// </summary>
        static Mesh Loft(List<Vector2[]> rings, List<float> ys, float tileU, float tileV, bool capTop, bool closed = true)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int n = rings[0].Length;
            int cols = closed ? n + 1 : n;
            for (int r = 0; r < rings.Count; r++)
            {
                float acc = 0f;
                for (int i = 0; i < cols; i++)
                {
                    var p = rings[r][i % n];
                    if (i > 0) acc += Vector2.Distance(rings[r][(i - 1) % n], p);
                    verts.Add(new Vector3(p.x, ys[r], p.y));
                    uvs.Add(new Vector2(acc / tileU, ys[r] / tileV));
                }
            }
            for (int r = 0; r < rings.Count - 1; r++)
            for (int i = 0; i < cols - 1; i++)
            {
                int a = r * cols + i, b = a + 1, c = a + cols, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
            if (capTop)
            {
                var top = rings[rings.Count - 1];
                var center = Vector2.zero;
                foreach (var p in top) center += p;
                center /= top.Length;
                int ci = verts.Count;
                verts.Add(new Vector3(center.x, ys[ys.Count - 1], center.y));
                uvs.Add(Vector2.zero);
                int baseIdx = verts.Count;
                for (int i = 0; i < n; i++) { verts.Add(new Vector3(top[i].x, ys[ys.Count - 1], top[i].y)); uvs.Add(Vector2.zero); }
                for (int i = 0; i < n; i++) tris.AddRange(new[] { ci, baseIdx + (i + 1) % n, baseIdx + i });
            }
            var mesh = new Mesh { name = "Loft", indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector2[] Circle(float r, int n)
        {
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return pts;
        }

        static GameObject Place(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 수백 m 밖이라 그림자 거리 밖
            return go;
        }

        // ---------- 롯데월드타워 ----------

        /// <summary>층 높이 비율 f(0~1)에서 한 변 반폭. 아래는 거의 곧고 위로 갈수록 붓끝처럼 빨리 좁아진다.</summary>
        static float LotteHalf(float f) => 21f * (1f - 0.64f * Mathf.Pow(f, 1.45f));

        /// <summary>둥근 네모(초타원 n=5) 단면 + 네 면 가운데 이음매 홈. gapDeg > 0이면 이음매가 그만큼 벌어진다(꼭대기 왕관).</summary>
        static List<float> LotteAngles()
        {
            var angles = new List<float>();
            for (int i = 0; i < 80; i++) angles.Add(i * 4.5f);
            for (int k = 0; k < 4; k++)
                foreach (float d in new[] { -1.6f, -0.7f, 0f, 0.7f, 1.6f }) angles.Add(k * 90f + d + (d < 0 ? 360f : 0f));
            for (int i = 0; i < angles.Count; i++) angles[i] = Mathf.Repeat(angles[i], 360f);
            angles.Sort();
            for (int i = angles.Count - 1; i > 0; i--) if (Mathf.Abs(angles[i] - angles[i - 1]) < 0.3f) angles.RemoveAt(i);
            return angles;
        }

        static Vector2 LottePoint(float deg, float half, float seamDepth)
        {
            float a = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            const float n = 5f;
            float r = Mathf.Pow(Mathf.Pow(Mathf.Abs(c), n) + Mathf.Pow(Mathf.Abs(s), n), -1f / n) * half;
            // 면 가운데(0·90·180·270도)의 이음매 홈
            float toSeam = Mathf.Abs(Mathf.DeltaAngle(deg, Mathf.Round(deg / 90f) * 90f));
            if (toSeam < 1.1f) r -= seamDepth;
            return new Vector2(c, s) * r;
        }

        static void Lotte(Transform root)
        {
            var t = new GameObject("LotteWorldTower").transform;
            t.SetParent(root, false);
            t.localPosition = LottePos;
            t.localRotation = Quaternion.Euler(0f, 18f, 0f); // 이음매 하나가 서북쪽(서울 도심)을 본다
            float H = LotteHeight, bodyTop = 0.855f;
            // 층선: 123층을 300m로 줄였으니 한 층 약 2.1m. 한 장 = 6층 × 10칸
            // 밤 불빛은 20 × 20장(120층 256m × 가로 280m, 아래 둘레 약 165m) 아틀라스 하나로 몸통 전체를 덮는다: 한 장을 되풀이하면 위로 좁아지며
            // 같은 무늬가 둘레를 따라 비스듬히 밀려 꽈배기처럼 보였다
            var glass = Facade(6, 10, new Color(0.78f, 0.84f, 0.9f), new Color(0.42f, 0.5f, 0.58f), new Color(0.93f, 0.94f, 0.95f), 0.16f, 0.07f, 11, out var night, 0.3f, 20, 10);
            var body = Mat(Color.white, glass, night, new Color(1f, 0.95f, 0.88f) * 1.7f, 0.85f, 0.35f, Vector2.one, 20);
            var angles = LotteAngles();
            var rings = new List<Vector2[]>();
            var ys = new List<float>();
            for (int r = 0; r <= 40; r++)
            {
                float f = r / 40f * bodyTop;
                float half = LotteHalf(f);
                var ring = new Vector2[angles.Count];
                for (int i = 0; i < angles.Count; i++) ring[i] = LottePoint(angles[i], half, 0.55f);
                rings.Add(ring);
                ys.Add(f * H);
            }
            // 한 장 = 6층(약 12.8m) × 10칸(약 14m)
            Place(t, "LotteBody", Loft(rings, ys, 14f, 12.8f, true), body, Vector3.zero);

            // 왕관: 이음매가 벌어지며 네 갈래 다이아그리드 꽃잎이 첨탑 끝으로 모인다 (양면, 칸은 뚫림)
            var crown = Mat(Color.white, Diagrid(), Texture2D.whiteTexture, new Color(0.95f, 0.97f, 1f) * 1.2f, 0.6f, 0.3f, Vector2.one);
            crown.SetFloat("_Cutoff", 0.4f);
            crown.SetFloat("_Cull", 0f);
            for (int k = 0; k < 4; k++)
            {
                var prings = new List<Vector2[]>();
                var pys = new List<float>();
                for (int r = 0; r <= 16; r++)
                {
                    float g = r / 16f;
                    float f = Mathf.Lerp(bodyTop - 0.015f, 1f, g);
                    float half = Mathf.Lerp(LotteHalf(bodyTop - 0.015f), 1.2f, Mathf.Pow(g, 1.15f));
                    float gap = Mathf.Lerp(1.5f, 13f, Mathf.Sin(g * Mathf.PI * 0.85f)); // 가운데쯤 가장 벌어졌다가 끝에서 모인다
                    var ring = new Vector2[14];
                    for (int i = 0; i < 14; i++)
                    {
                        float deg = k * 90f + gap + (90f - 2f * gap) * i / 13f;
                        ring[i] = LottePoint(deg, half, 0f);
                    }
                    prings.Add(ring);
                    pys.Add(f * H);
                }
                Place(t, "LotteCrown", Loft(prings, pys, 9f, 9f, false, false), crown, Vector3.zero);
            }
            // 전망대 지붕·첨탑 끝
            var steel = Mat(new Color(0.85f, 0.86f, 0.88f), null, Texture2D.whiteTexture, new Color(1f, 1f, 1f) * 0.25f, 0.5f, 0.6f, Vector2.one);
            Place(t, "LotteMast", Loft(new List<Vector2[]> { Circle(1.6f, 12), Circle(0.25f, 12) }, new List<float> { bodyTop * H, H + 6f }, 4f, 4f, false), steel, Vector3.zero);
            var beacon = Mat(new Color(1f, 0.3f, 0.3f), null, Texture2D.whiteTexture, new Color(1f, 0.15f, 0.1f) * 3f, 0.3f, 0f, Vector2.one);
            beacon.SetFloat("_DayEmission", 0.4f);
            Mats.Prim(PrimitiveType.Sphere, "LotteBeacon", t, new Vector3(0f, H + 6.3f, 0f), Vector3.one * 1.1f, beacon, false);
            // 저층부 롯데월드몰 (단 세 개)
            var mall = Mat(new Color(0.9f, 0.9f, 0.88f), Facade(2, 6, new Color(0.7f, 0.76f, 0.8f), new Color(0.35f, 0.4f, 0.45f), new Color(0.92f, 0.92f, 0.9f), 0.25f, 0.06f, 7, out var mallNight, 0.6f),
                mallNight, new Color(1f, 0.9f, 0.7f), 0.6f, 0.1f, new Vector2(8f, 2f));
            Mats.Prim(PrimitiveType.Cube, "LotteMall", t, new Vector3(60f, 14f, 30f), new Vector3(110f, 28f, 50f), mall, false);
            Mats.Prim(PrimitiveType.Cube, "LotteMall2", t, new Vector3(-55f, 10f, 40f), new Vector3(80f, 20f, 60f), mall, false);
        }

        // ---------- 63빌딩 ----------

        static void SixtyThree(Transform root)
        {
            var t = new GameObject("63Building").transform;
            t.SetParent(root, false);
            t.localPosition = SixtyThreePos;
            t.localRotation = Quaternion.Euler(0f, -59f, 0f); // 앞면이 강 건너 반포(남동쪽)를 본다
            float H = SixtyThreeHeight;
            // 금빛 유리: 60층 → 150m, 한 층 2.5m. 한 장 = 4층 × 6칸
            var gold = Facade(4, 6, new Color(1f, 0.86f, 0.5f), new Color(0.62f, 0.44f, 0.18f), new Color(0.78f, 0.6f, 0.3f), 0.12f, 0.1f, 63, out var night, 0.3f);
            var mat = Mat(new Color(1f, 0.92f, 0.75f), gold, night, new Color(1f, 0.85f, 0.55f) * 1.5f, 0.9f, 0.75f, Vector2.one);
            const float halfW = 26f, depth = 9f, bow = 0.12f; // 앞뒤 면이 가운데에서 바깥으로 살짝 꺾인다 (사람 인(人)자 평면)
            int slices = 7;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int s = 0; s < slices; s++)
            {
                float x0 = Mathf.Lerp(-halfW, halfW, s / (float)slices), x1 = Mathf.Lerp(-halfW, halfW, (s + 1) / (float)slices);
                // 서쪽 끝이 가장 높고 동쪽으로 갈수록 계단처럼 낮아지는 사선 지붕
                float h = H * Mathf.Lerp(1f, 0.83f, Mathf.Pow(s / (float)(slices - 1), 1.2f));
                float zf0 = -depth + Mathf.Abs(x0) * bow, zf1 = -depth + Mathf.Abs(x1) * bow;  // 앞(강 쪽, -z)
                float zb0 = depth - Mathf.Abs(x0) * bow, zb1 = depth - Mathf.Abs(x1) * bow;    // 뒤
                // 위에서 볼 때 앞(-z) → 오른쪽 → 뒤 → 왼쪽 순서로 네 면
                Quad(verts, uvs, tris, new Vector3(x0, 0, zf0), new Vector3(x1, 0, zf1), h, x0, x1);
                Quad(verts, uvs, tris, new Vector3(x1, 0, zb1), new Vector3(x0, 0, zb0), h, -x1, -x0);
                if (s == slices - 1) Quad(verts, uvs, tris, new Vector3(x1, 0, zf1), new Vector3(x1, 0, zb1), h, 0f, zb1 - zf1);
                else
                {
                    // 다음 조각보다 높으면 그 차이만큼 옆 벽
                    float hn = H * Mathf.Lerp(1f, 0.83f, Mathf.Pow((s + 1) / (float)(slices - 1), 1.2f));
                    QuadBand(verts, uvs, tris, new Vector3(x1, 0, zf1), new Vector3(x1, 0, zb1), hn, h);
                }
                if (s == 0) Quad(verts, uvs, tris, new Vector3(x0, 0, zb0), new Vector3(x0, 0, zf0), h, 0f, zb0 - zf0);
                // 지붕
                int i = verts.Count;
                verts.Add(new Vector3(x0, h, zf0)); verts.Add(new Vector3(x0, h, zb0)); verts.Add(new Vector3(x1, h, zb1)); verts.Add(new Vector3(x1, h, zf1));
                for (int k = 0; k < 4; k++) uvs.Add(new Vector2(0.02f, 0.05f));
                tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            var mesh = new Mesh { name = "63Building" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Place(t, "63Body", mesh, mat, Vector3.zero);
            // 사진(63빌딩): 넓은 앞뒤 면 가운데를 위아래로 가르는 짙은 세로 홈 두 줄 (강 쪽에서 보면 탑이 둘로 나뉜 듯)
            var groove = Mat(new Color(0.18f, 0.13f, 0.08f), null, null, Color.black, 0.85f, 0.6f, Vector2.one);
            float hCenter = H * Mathf.Lerp(1f, 0.83f, Mathf.Pow(3 / (float)(slices - 1), 1.2f));
            float bend = Mathf.Atan(bow) * Mathf.Rad2Deg;
            foreach (float fz in new[] { -1f, 1f })
            foreach (float gx in new[] { -1.6f, 1.6f })
            {
                float z = fz * (depth - Mathf.Abs(gx) * bow) + fz * 0.06f;
                float yaw = -Mathf.Sign(gx) * fz * bend;
                Mats.Prim(PrimitiveType.Cube, "63Groove", t, new Vector3(gx, hCenter * 0.5f, z), new Vector3(1.1f, hCenter, 0.14f), groove, false, Quaternion.Euler(0f, yaw, 0f));
            }
            // 옥상 안테나 + 항공 장애등
            var steel = Mat(new Color(0.75f, 0.75f, 0.76f), null, null, Color.black, 0.5f, 0.6f, Vector2.one);
            Mats.Prim(PrimitiveType.Cylinder, "63Antenna", t, new Vector3(-halfW + 6f, H + 9f, 0f), new Vector3(0.7f, 9f, 0.7f), steel, false);
            var beacon = Mat(new Color(1f, 0.3f, 0.3f), null, Texture2D.whiteTexture, new Color(1f, 0.15f, 0.1f) * 3f, 0.3f, 0f, Vector2.one);
            beacon.SetFloat("_DayEmission", 0.4f);
            Mats.Prim(PrimitiveType.Sphere, "63Beacon", t, new Vector3(-halfW + 6f, H + 18.3f, 0f), Vector3.one * 0.9f, beacon, false);
            // 저층부 (수족관·전시관)
            var podium = Mat(new Color(0.85f, 0.78f, 0.62f), null, null, Color.black, 0.5f, 0.3f, Vector2.one);
            Mats.Prim(PrimitiveType.Cube, "63Podium", t, new Vector3(0f, 6f, 0f), new Vector3(70f, 12f, 32f), podium, false);

            static void Quad(List<Vector3> v, List<Vector2> uv, List<int> tr, Vector3 a, Vector3 b, float h, float u0, float u1)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(b + Vector3.up * h); v.Add(a + Vector3.up * h);
                const float tileU = 13f, tileV = 10f; // 한 장 = 6칸 × 4층 (13m × 10m)
                uv.Add(new Vector2(u0 / tileU, 0f)); uv.Add(new Vector2(u1 / tileU, 0f)); uv.Add(new Vector2(u1 / tileU, h / tileV)); uv.Add(new Vector2(u0 / tileU, h / tileV));
                tr.AddRange(new[] { i, i + 3, i + 2, i, i + 2, i + 1 });
            }
            static void QuadBand(List<Vector3> v, List<Vector2> uv, List<int> tr, Vector3 a, Vector3 b, float y0, float y1)
            {
                int i = v.Count;
                v.Add(a + Vector3.up * y0); v.Add(b + Vector3.up * y0); v.Add(b + Vector3.up * y1); v.Add(a + Vector3.up * y1);
                for (int k = 0; k < 4; k++) uv.Add(new Vector2(0f, (k < 2 ? y0 : y1) / 10f));
                tr.AddRange(new[] { i, i + 3, i + 2, i, i + 2, i + 1 });
            }
        }

        // ---------- 남산서울타워 ----------

        static void Namsan(Transform root)
        {
            var t = new GameObject("Namsan").transform;
            t.SetParent(root, false);
            t.localPosition = NamsanPos;
            // 산: 노이즈로 울퉁불퉁한 돔. 숲 텍스처를 넓게
            var forest = Fishing.World.FarLand.Forest(Fishing.World.FarLand.KoreaDark * 1.3f, Fishing.World.FarLand.KoreaLight * 1.15f, "seacliff", null, 0.45f);
            Place(t, "NamsanHill", HillMesh(260f, 190f, NamsanHill, 7), forest, Vector3.zero);
            float baseY = NamsanHill - 2f;
            var concreteTex = Facade(8, 24, new Color(0.95f, 0.95f, 0.94f), new Color(0.82f, 0.82f, 0.8f), new Color(0.88f, 0.88f, 0.87f), 0.02f, 0.06f, 3, out _, 0f);
            // 밤엔 기둥 전체가 푸르스름하게 (조명)
            var shaftGlow = new Texture2D(2, 2);
            shaftGlow.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); shaftGlow.Apply();
            var shaft = Mat(new Color(0.95f, 0.95f, 0.95f), concreteTex, shaftGlow, new Color(0.35f, 0.45f, 1f) * 0.9f, 0.2f, 0f, Vector2.one);
            float T = NamsanTower;
            // 받침 건물
            var baseMat = Mat(new Color(0.85f, 0.85f, 0.83f), Facade(3, 12, new Color(0.65f, 0.72f, 0.78f), new Color(0.35f, 0.4f, 0.45f), new Color(0.9f, 0.9f, 0.88f), 0.3f, 0.08f, 9, out var baseNight, 0.6f),
                baseNight, new Color(1f, 0.85f, 0.6f), 0.4f, 0f, Vector2.one);
            Place(t, "TowerBase", Loft(new List<Vector2[]> { Circle(13f, 6), Circle(12f, 6) }, new List<float> { baseY - 4f, baseY + 7f }, 10f, 8f, true), baseMat, Vector3.zero);
            // 기둥: 위로 살짝 가늘어진다 (0 ~ 0.6T)
            float s0 = baseY + 7f, s1 = baseY + T * 0.6f;
            Place(t, "TowerShaft", Loft(new List<Vector2[]> { Circle(4.2f, 24), Circle(3.4f, 24) }, new List<float> { s0, s1 }, 4f, 30f, false), shaft, Vector3.zero);
            // 전망대: 아래 데크 → 큰 창 원통 → 지붕 → 작은 원통 → 지붕 (두 겹)
            var obs = Mat(new Color(0.9f, 0.9f, 0.9f), Facade(1, 36, new Color(0.55f, 0.66f, 0.78f), new Color(0.2f, 0.25f, 0.32f), new Color(0.93f, 0.93f, 0.92f), 0.18f, 0.08f, 5, out var obsNight, 0.95f),
                obsNight, new Color(1f, 0.85f, 0.6f) * 1.3f, 0.6f, 0.2f, Vector2.one);
            var white = Mat(new Color(0.93f, 0.93f, 0.92f), null, shaftGlow, new Color(0.35f, 0.45f, 1f) * 0.3f, 0.3f, 0f, Vector2.one);
            float y = s1;
            Place(t, "ObsDeckLow", Loft(new List<Vector2[]> { Circle(3.4f, 32), Circle(8f, 32), Circle(8f, 32) }, new List<float> { y - 3f, y, y + 1.2f }, 6f, 6f, false), white, Vector3.zero);
            y += 1.2f;
            Place(t, "ObsWindows", Loft(new List<Vector2[]> { Circle(8.6f, 48), Circle(9.4f, 48) }, new List<float> { y, y + 6f }, 1.6f, 6f, false), obs, Vector3.zero);
            y += 6f;
            Place(t, "ObsRoof", Loft(new List<Vector2[]> { Circle(9.4f, 48), Circle(10.2f, 48), Circle(7.6f, 48) }, new List<float> { y, y + 0.8f, y + 1.6f }, 6f, 6f, false), white, Vector3.zero);
            y += 1.6f;
            Place(t, "ObsWindows2", Loft(new List<Vector2[]> { Circle(7.2f, 40), Circle(7.6f, 40) }, new List<float> { y, y + 4f }, 1.4f, 4f, false), obs, Vector3.zero);
            y += 4f;
            Place(t, "ObsRoof2", Loft(new List<Vector2[]> { Circle(7.6f, 40), Circle(8.2f, 40), Circle(2.2f, 40) }, new List<float> { y, y + 0.6f, y + 3.2f }, 6f, 6f, true), white, Vector3.zero);
            y += 3.2f;
            // 송신탑: 빨강·흰 줄무늬, 위로 가늘어지며 중간에 작은 단 두 개
            float mastTop = baseY + T;
            var stripe = Mat(Color.white, Stripes(new Color(0.85f, 0.15f, 0.12f), new Color(0.95f, 0.95f, 0.95f), 10), null, Color.black, 0.4f, 0.2f, Vector2.one);
            Place(t, "Mast", Loft(new List<Vector2[]> { Circle(1.9f, 16), Circle(1.2f, 16), Circle(0.45f, 16) }, new List<float> { y, Mathf.Lerp(y, mastTop, 0.5f), mastTop }, 30f, mastTop - y, false), stripe, Vector3.zero);
            foreach (float k in new[] { 0.3f, 0.62f })
            {
                float py = Mathf.Lerp(y, mastTop, k);
                Place(t, "MastPlatform", Loft(new List<Vector2[]> { Circle(1.7f - k, 16), Circle(3f - k * 1.5f, 16), Circle(3f - k * 1.5f, 16), Circle(1.5f - k, 16) },
                    new List<float> { py - 0.4f, py, py + 0.5f, py + 0.6f }, 4f, 4f, false), white, Vector3.zero);
            }
            var beacon = Mat(new Color(1f, 0.3f, 0.3f), null, Texture2D.whiteTexture, new Color(1f, 0.15f, 0.1f) * 3f, 0.3f, 0f, Vector2.one);
            beacon.SetFloat("_DayEmission", 0.4f);
            Mats.Prim(PrimitiveType.Sphere, "MastBeacon", t, new Vector3(0f, mastTop + 0.4f, 0f), Vector3.one * 0.9f, beacon, false);

            // 사진(남산): 탑 옆 산등성이의 빨강·흰 철골 송신탑 두 기 (탑보다 낮다)
            var red = Mat(new Color(0.85f, 0.16f, 0.12f), null, null, Color.black, 0.3f, 0.3f, Vector2.one);
            var whiteSteel = Mat(new Color(0.94f, 0.94f, 0.93f), null, null, Color.black, 0.3f, 0.3f, Vector2.one);
            LatticeMast(t, new Vector3(-48f, NamsanHill - 22f, 18f), 82f, 6f, 0.8f, red, whiteSteel, beacon);
            LatticeMast(t, new Vector3(-30f, NamsanHill - 12f, -24f), 58f, 4.5f, 0.6f, red, whiteSteel, beacon);
        }

        /// <summary>철골 송신탑: 네 다리가 위로 모이고, 칸마다 X자 가새와 수평재. 높이를 7칸으로 나눠 빨강·흰 띠를 번갈아 칠한다.</summary>
        static void LatticeMast(Transform parent, Vector3 basePos, float height, float baseHalf, float topHalf, Material red, Material white, Material beacon)
        {
            var t = new GameObject("LatticeMast").transform;
            t.SetParent(parent, false);
            t.localPosition = basePos;
            const int bays = 14;
            Vector3 Corner(int k, float f)
            {
                float h = Mathf.Lerp(baseHalf, topHalf, Mathf.Pow(f, 0.8f));
                return new Vector3((k == 0 || k == 3) ? -h : h, f * height, (k < 2) ? -h : h);
            }
            void Bar(Vector3 a, Vector3 b, float thick, Material m)
            {
                var d = b - a;
                var go = Mats.Prim(PrimitiveType.Cube, "Bar", t, (a + b) * 0.5f, new Vector3(thick, thick, d.magnitude), m, false, Quaternion.LookRotation(d));
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < bays; i++)
            {
                float f0 = i / (float)bays, f1 = (i + 1) / (float)bays;
                var m = (i / 2) % 2 == 0 ? red : white;
                float thick = Mathf.Lerp(0.5f, 0.2f, f0);
                for (int k = 0; k < 4; k++)
                {
                    Vector3 a0 = Corner(k, f0), a1 = Corner(k, f1), b0 = Corner((k + 1) % 4, f0), b1 = Corner((k + 1) % 4, f1);
                    Bar(a0, a1, thick, m);              // 다리
                    Bar(a1, b1, thick * 0.6f, m);       // 수평재
                    Bar(a0, b1, thick * 0.45f, m);      // X 가새
                    Bar(b0, a1, thick * 0.45f, m);
                }
            }
            Mats.Prim(PrimitiveType.Cylinder, "MastTip", t, new Vector3(0f, height + 4f, 0f), new Vector3(0.35f, 4f, 0.35f), red, false);
            Mats.Prim(PrimitiveType.Sphere, "MastBeacon", t, new Vector3(0f, height + 8.3f, 0f), Vector3.one * 0.8f, beacon, false);
        }

        /// <summary>둥근 산: 가운데가 높고 가장자리로 흘러내리는 돔 + 능선 노이즈.</summary>
        internal static Mesh HillMesh(float rx, float rz, float height, int seed)
        {
            const int seg = 64, rings = 20;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float f = r / (float)rings; // 0 가장자리 ~ 1 꼭대기
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    float n = Mathf.PerlinNoise(seed + Mathf.Cos(a) * 1.7f + f * 2f, seed + Mathf.Sin(a) * 1.7f);
                    float rad = (1f - f) * (0.85f + n * 0.3f);
                    float h = height * Mathf.SmoothStep(0f, 1f, Mathf.Pow(f, 0.8f)) * (0.88f + n * 0.24f) - 6f;
                    var p = new Vector3(Mathf.Cos(a) * rx * rad, h, Mathf.Sin(a) * rz * rad);
                    verts.Add(p);
                    uvs.Add(new Vector2(p.x / rx * 0.5f + 0.5f, p.z / rz * 0.5f + 0.5f));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < seg; s++)
            {
                int i = r * (seg + 1) + s;
                tris.AddRange(new[] { i, i + seg + 1, i + 1, i + 1, i + seg + 1, i + seg + 2 });
            }
            var mesh = new Mesh { name = "Hill" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // ---------- 먼 땅·도시 ----------

        static void FarLand(Transform root)
        {
            float y = HanRiverMap.LandY - 0.05f;
            var ground = Mat(new Color(0.36f, 0.37f, 0.36f), null, null, Color.black, 0.1f, 0f, Vector2.one);
            var river = Mat(new Color(0.2f, 0.27f, 0.3f), null, null, Color.black, 0.92f, 0.1f, Vector2.one);
            // 한강 맵 땅(x ±300, z -166~166) 바깥: 북쪽 시가지, 동서로 이어지는 둔치와 강
            Mats.Prim(PrimitiveType.Cube, "FarNorth", root, new Vector3(0f, y - 0.5f, 660f), new Vector3(2400f, 1f, 1000f), ground, false);
            foreach (float side in new[] { -1f, 1f })
            {
                Mats.Prim(PrimitiveType.Cube, "FarRiver", root, new Vector3(side * 750f, HanRiverMap.WaterY - 0.6f, 0f), new Vector3(900f, 1f, 140f), river, false);
                Mats.Prim(PrimitiveType.Cube, "FarBankN", root, new Vector3(side * 750f, y - 0.5f, 115f), new Vector3(900f, 1f, 90f), ground, false);
                Mats.Prim(PrimitiveType.Cube, "FarBankS", root, new Vector3(side * 750f, y - 0.5f, -300f), new Vector3(900f, 1f, 460f), ground, false);
            }
        }

        /// <summary>강 건너 먼 시가지: 판상형 아파트(흰 벽 + 층 띠)와 빌딩 상자. 강변 가까운 쪽은 낮고 멀수록 높다.</summary>
        static void FarCity(Transform root)
        {
            var aptTex = Facade(5, 8, new Color(0.88f, 0.87f, 0.84f), new Color(0.74f, 0.73f, 0.7f), new Color(0.95f, 0.94f, 0.92f), 0.2f, 0.12f, 21, out var aptNight, 0.38f, NightTiles);
            var officeTex = Facade(5, 8, new Color(0.62f, 0.7f, 0.78f), new Color(0.3f, 0.36f, 0.42f), new Color(0.7f, 0.72f, 0.74f), 0.15f, 0.08f, 22, out var officeNight, 0.42f, NightTiles);
            var apt = Mat(Color.white, aptTex, aptNight, new Color(1f, 0.85f, 0.6f) * 0.8f, 0.2f, 0f, Vector2.one, NightTiles);
            var aptTint = Mat(new Color(0.95f, 0.9f, 0.82f), aptTex, aptNight, new Color(1f, 0.85f, 0.6f) * 0.8f, 0.2f, 0f, Vector2.one, NightTiles);
            var office = Mat(Color.white, officeTex, officeNight, new Color(0.9f, 0.95f, 1f) * 0.8f, 0.7f, 0.2f, Vector2.one, NightTiles);
            var mats = new[] { apt, aptTint, office };
            int placed = 0;
            for (int i = 0; i < 900 && placed < 330; i++)
            {
                float x = R(-1100f, 1100f), z = R(150f, 1050f);
                // 동서 먼 강변 쪽은 강(z -70~70) 바로 뒤부터
                if (Mathf.Abs(x) < 300f && z < 175f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(LottePos.x, LottePos.z)) < 90f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(SixtyThreePos.x, SixtyThreePos.z)) < 70f) continue;
                if (new Vector2((x - NamsanPos.x) / 250f, (z - NamsanPos.z) / 180f).magnitude < 1f) continue;
                float far = Mathf.InverseLerp(150f, 1050f, z);
                int kind = _rng.Next(3);
                float h = kind == 2 ? R(30f, 70f) + far * 40f : R(28f, 52f);
                float w = kind == 2 ? R(16f, 30f) : R(40f, 70f);
                float d = kind == 2 ? R(16f, 30f) : R(12f, 15f);
                var go = Mats.Prim(PrimitiveType.Cube, "FarBuilding", root, new Vector3(x, HanRiverMap.LandY + h * 0.5f, z), new Vector3(w, h, d), mats[kind], false,
                    Quaternion.Euler(0f, kind == 2 ? R(0f, 90f) : R(-12f, 12f), 0f));
                // 상자 면마다 UV가 0~1이라 크기에 맞춰 반복 (아파트 한 장 = 8칸 × 5층 ≈ 20m × 14m)
                var mf = go.GetComponent<MeshFilter>();
                mf.sharedMesh = TiledCube(mf.sharedMesh, w, h, d, placed * 7);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                placed++;
            }
        }

        static readonly Dictionary<(Vector3Int, int), Mesh> _cubes = new Dictionary<(Vector3Int, int), Mesh>();

        /// <summary>
        /// 크기 (w,h,d)인 상자 프리미티브(스케일로 늘림)에 맞춘 UV: 옆면 한 장 = 20m × 14m.
        /// variant(0~15)마다, 옆면마다 UV를 낮 텍스처 몇 장씩(정수) 밀어 둔다: 낮 무늬는 그대로고 밤 불빛 아틀라스(NightTiles장)에선
        /// 건물·면마다 다른 곳을 쓰게 되어 이웃 건물끼리 불 켜진 모양이 같지 않다.
        /// </summary>
        internal static Mesh TiledCube(Mesh src, float w, float h, float d, int variant = 0)
        {
            var size = new Vector3Int(Mathf.Max(1, Mathf.RoundToInt(w / 4f)), Mathf.Max(1, Mathf.RoundToInt(h / 4f)), Mathf.Max(1, Mathf.RoundToInt(d / 4f)));
            variant &= 15;
            if (_cubes.TryGetValue((size, variant), out var m)) return m;
            m = Object.Instantiate(src);
            var v = m.vertices;
            var n = m.normals;
            var uv = new Vector2[v.Length];
            var shift = new Vector2[4];
            var r = new System.Random(variant + 1);
            for (int f = 0; f < 4; f++) shift[f] = new Vector2(r.Next(NightTiles), r.Next(NightTiles));
            for (int i = 0; i < v.Length; i++)
            {
                var p = Vector3.Scale(v[i] + Vector3.one * 0.5f, new Vector3(size.x * 4f, size.y * 4f, size.z * 4f));
                if (Mathf.Abs(n[i].y) > 0.5f) uv[i] = new Vector2(0.01f, 0.01f); // 지붕: 프레임 색 한 점
                else if (Mathf.Abs(n[i].x) > 0.5f) uv[i] = new Vector2(p.z / 20f, p.y / 14f) + shift[n[i].x > 0f ? 0 : 1];
                else uv[i] = new Vector2(p.x / 20f, p.y / 14f) + shift[n[i].z > 0f ? 2 : 3];
            }
            m.uv = uv;
            _cubes[(size, variant)] = m;
            return m;
        }

        /// <summary>시가지 상자 건물의 밤 불빛 아틀라스 크기(낮 텍스처 NightTiles × NightTiles장 = 가로 160m 남짓 × 세로 112m).</summary>
        internal const int NightTiles = 8;

        // ---------- CU 편의점 (반포 한강공원) ----------

        /// <summary>
        /// 둔치 남쪽의 한강공원 편의점 (사용자 참고 사진: 요즘 한강 CU): 흰 타공 패널 벽, 위에 연두 띠 간판 "Nice to CU",
        /// 짙은 회색 틀의 통유리 앞면(가운데 자동문, 유리에 행사 포스터) 안으로 진열대·냉장고·계산대·한강라면 조리기,
        /// 붉은 보도블록 앞마당에 보라 팔각 파라솔(띠에 흰 CU) + 흰 정사각 탁자 + 보라 플라스틱 팔걸이 의자,
        /// 모퉁이 기둥 간판 "CU 24h Open", 분리수거함, 한강라면 X배너. 밤엔 가게 안과 간판이 밝다.
        /// </summary>
        public static void ConvenienceStore(Transform parent, Vector3 pos, float yaw)
        {
            var t = new GameObject("CU_Store").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            const float W = 12f, D = 8f, Ht = 4.6f, GlassTop = 3.1f;
            var purple = Mats.Lit(new Color(0.37f, 0.16f, 0.55f), 0.35f);
            var lime = Mats.Emissive(new Color(0.55f, 0.82f, 0.12f), 0.35f);
            var frame = Mats.Lit(new Color(0.2f, 0.21f, 0.23f), 0.5f, 0.4f);
            var floorMat = Mats.Wood(new Color(0.82f, 0.82f, 0.8f), 0.4f);
            var indoorLight = Mats.Emissive(new Color(1f, 0.98f, 0.92f), 1.6f);
            var coping = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.4f, 0.3f);
            // 패널 0.6m 한 장씩 (상자 면마다 UV 0~1 → 면 크기로 반복)
            Material Panel(float a, float b) => Mat(new Color(1f, 0.99f, 0.97f), PanelTex(), null, Color.black, 0.35f, 0f, new Vector2(a / 0.6f, b / 0.6f));

            // 바닥·벽·지붕 (+z가 강 쪽 앞면). 옆벽만 앞뒤 끝까지, 나머지는 옆벽 사이(Wi)에 넣어 면이 겹치지 않게 (겹치면 z-fighting으로 깜박인다)
            const float Wi = W - 0.62f;
            var granite = Mats.Weathered("granite", new Color(0.74f, 0.64f, 0.6f), 1.2f, 0.45f, 0f, 0.1f, 0f);
            Mats.Prim(PrimitiveType.Cube, "StoreFloor", t, new Vector3(0f, 0.08f, 0f), new Vector3(Wi, 0.16f, D - 0.2f), floorMat, false);
            Mats.Prim(PrimitiveType.Cube, "StoreBack", t, new Vector3(0f, Ht * 0.5f, -D * 0.5f + 0.15f), new Vector3(Wi, Ht, 0.3f), Panel(W, Ht));
            Mats.Prim(PrimitiveType.Cube, "StoreSideL", t, new Vector3(-W * 0.5f + 0.15f, Ht * 0.5f, 0f), new Vector3(0.3f, Ht, D), Panel(D, Ht));
            Mats.Prim(PrimitiveType.Cube, "StoreSideR", t, new Vector3(W * 0.5f - 0.15f, Ht * 0.5f, 0f), new Vector3(0.3f, Ht, D), Panel(D, Ht));
            Mats.Prim(PrimitiveType.Cube, "StoreHead", t, new Vector3(0f, (GlassTop + Ht) * 0.5f, D * 0.5f - 0.15f), new Vector3(Wi, Ht - GlassTop, 0.3f), Panel(W, Ht - GlassTop), false);
            // 입구 양옆 기둥: 분홍빛 화강석 붙임 (벽보다 4cm 앞으로)
            foreach (float sx in new[] { -1f, 1f })
                Mats.Prim(PrimitiveType.Cube, "StorePier", t, new Vector3(sx * 5.25f, GlassTop * 0.5f, D * 0.5f - 0.13f), new Vector3(0.9f, GlassTop, 0.34f), granite, false);
            Mats.Prim(PrimitiveType.Cube, "StoreRoof", t, new Vector3(0f, Ht - 0.1f, 0f), new Vector3(Wi, 0.2f, D - 0.62f), Mats.Wall(new Color(0.72f, 0.72f, 0.7f)), false);
            // 옥상 난간 덮개 (은회색 알루미늄): 앞뒤는 끝까지, 옆은 그 사이만
            Mats.Prim(PrimitiveType.Cube, "CopingF", t, new Vector3(0f, Ht + 0.04f, D * 0.5f - 0.15f), new Vector3(W + 0.06f, 0.08f, 0.36f), coping, false);
            Mats.Prim(PrimitiveType.Cube, "CopingB", t, new Vector3(0f, Ht + 0.04f, -D * 0.5f + 0.15f), new Vector3(W + 0.06f, 0.08f, 0.36f), coping, false);
            foreach (float sx in new[] { -1f, 1f })
                Mats.Prim(PrimitiveType.Cube, "CopingS", t, new Vector3(sx * (W * 0.5f - 0.15f), Ht + 0.04f, 0f), new Vector3(0.36f, 0.08f, D - 0.74f), coping, false);
            // 옥상 실외기
            for (int k = 0; k < 2; k++)
                Mats.Prim(PrimitiveType.Cube, "RoofAC", t, new Vector3(-3f + k * 1.3f, Ht + 0.35f, -2.5f), new Vector3(0.9f, 0.7f, 0.4f), Mats.Lit(new Color(0.85f, 0.85f, 0.83f), 0.3f), false);
            Mats.Prim(PrimitiveType.Cube, "Ceiling", t, new Vector3(0f, GlassTop + 0.05f, 0f), new Vector3(W - 0.64f, 0.06f, D - 0.64f), Mats.Lit(new Color(0.96f, 0.96f, 0.95f)), false);
            for (int k = 0; k < 3; k++)
                Mats.Prim(PrimitiveType.Cube, "CeilingLight", t, new Vector3(-3.5f + k * 3.5f, GlassTop, 0f), new Vector3(0.3f, 0.04f, D - 1.6f), indoorLight, false);

            // 앞면: 통유리 틀(짙은 회색, 가운데 미닫이 자동문)
            float fz = D * 0.5f - 0.1f, gw = W - 2.4f;
            // 유리판은 두지 않는다: 반투명 Standard 판이 빌드에서 안을 다 가려 회색 벽처럼 보였다. 틀만 두면 진열대·포스터가 보인다
            foreach (float x in new[] { -gw * 0.5f, -2.6f, -0.9f, 0f, 0.9f, 2.6f, gw * 0.5f })
                Mats.Prim(PrimitiveType.Cube, "Mullion", t, new Vector3(x, 1.6f, fz), new Vector3(x == 0f ? 0.04f : 0.08f, 3f, 0.12f), frame, false);
            Mats.Prim(PrimitiveType.Cube, "GlassSill", t, new Vector3(0f, 0.12f, fz), new Vector3(gw + 0.1f, 0.1f, 0.14f), frame, false);
            Mats.Prim(PrimitiveType.Cube, "GlassHead", t, new Vector3(0f, GlassTop, fz), new Vector3(gw + 0.1f, 0.12f, 0.14f), frame, false);
            Mats.Prim(PrimitiveType.Cube, "DoorTransom", t, new Vector3(0f, 2.3f, fz + 0.01f), new Vector3(1.8f, 0.08f, 0.12f), frame, false);
            Mats.Prim(PrimitiveType.Cube, "DoorSensor", t, new Vector3(0f, 2.93f, fz + 0.1f), new Vector3(0.42f, 0.1f, 0.1f), frame, false);
            foreach (float x in new[] { -0.12f, 0.12f })
                Mats.Prim(PrimitiveType.Cube, "DoorHandle", t, new Vector3(x, 1.05f, fz + 0.06f), new Vector3(0.03f, 0.5f, 0.03f), coping, false);
            Mats.Prim(PrimitiveType.Cube, "DoorStripe", t, new Vector3(0f, 1.45f, fz + 0.035f), new Vector3(1.7f, 0.08f, 0.01f), purple, false);
            // 유리에 붙인 것들 (판은 없어도 같은 면 fz에): "Nice to CU" 스티커, 행사·get커피·한강라면 포스터
            void Text(string s, Vector3 p, Color c, float size, bool bold = true)
            {
                var tx = UIFactory.WorldText(t, s, p, c, size, "StoreText");
                tx.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                if (bold) tx.fontStyle = TMPro.FontStyles.Bold;
                UIFactory.OneSided(tx);
            }
            var cuPurple = new Color(0.4f, 0.18f, 0.6f);
            Mats.Prim(PrimitiveType.Cube, "GlassSticker", t, new Vector3(1.75f, 2.05f, fz), new Vector3(1.4f, 0.42f, 0.01f), Mats.Lit(new Color(0.97f, 0.97f, 0.97f), 0.4f), false);
            Mats.Prim(PrimitiveType.Cube, "GlassStickerPill", t, new Vector3(2.08f, 2.05f, fz + 0.008f), new Vector3(0.62f, 0.24f, 0.01f), lime, false);
            Text("Nice to", new Vector3(2.08f, 2.05f, fz + 0.02f), cuPurple, 1.5f);
            Text("CU", new Vector3(1.4f, 2.04f, fz + 0.02f), cuPurple, 3.2f);
            void Poster(Vector3 p, Vector2 size, Color c) =>
                Mats.Prim(PrimitiveType.Cube, "Poster", t, p, new Vector3(size.x, size.y, 0.01f), Mats.Emissive(c, 0.45f), false);
            Poster(new Vector3(3.7f, 1.1f, fz), new Vector2(1.5f, 1.2f), new Color(0.22f, 0.3f, 0.62f));
            Text("1+1", new Vector3(3.7f, 1.25f, fz + 0.02f), new Color(1f, 0.92f, 0.3f), 6f);
            Text("행사 상품", new Vector3(3.7f, 0.75f, fz + 0.02f), Color.white, 1.8f);
            Poster(new Vector3(-1.75f, 1.0f, fz), new Vector2(0.7f, 1f), new Color(0.42f, 0.26f, 0.16f));
            Text("get\n커피", new Vector3(-1.75f, 1.05f, fz + 0.02f), new Color(1f, 0.95f, 0.85f), 2.2f);
            Poster(new Vector3(-3.7f, 1.1f, fz), new Vector2(0.8f, 1.1f), new Color(0.88f, 0.26f, 0.18f));
            Text("한강\n라면", new Vector3(-3.7f, 1.15f, fz + 0.02f), Color.white, 2.6f);
            // 문 앞 알루미늄 경사판 (가게 바닥 → 보도블록)
            Mats.Prim(PrimitiveType.Cube, "DoorRamp", t, new Vector3(0f, 0.11f, D * 0.5f + 0.48f), new Vector3(1.9f, 0.04f, 1f), Mats.Lit(new Color(0.7f, 0.72f, 0.74f), 0.55f, 0.85f), false, Quaternion.Euler(5.7f, 0f, 0f));
            // 오른쪽 기둥 도로명 주소판, 왼쪽 처마 밑 CCTV
            Mats.Prim(PrimitiveType.Cube, "AddressPlate", t, new Vector3(-5.25f, 2.45f, D * 0.5f + 0.05f), new Vector3(0.44f, 0.26f, 0.02f), Mats.Lit(new Color(0.1f, 0.28f, 0.58f), 0.4f), false);
            Text("신반포로11길", new Vector3(-5.25f, 2.52f, D * 0.5f + 0.07f), Color.white, 0.75f, false);
            Text("40", new Vector3(-5.25f, 2.4f, D * 0.5f + 0.07f), Color.white, 1.6f);
            Mats.Prim(PrimitiveType.Cube, "Cctv", t, new Vector3(5.5f, 3.17f, D * 0.5f + 0.32f), new Vector3(0.1f, 0.1f, 0.24f), Mats.Lit(new Color(0.92f, 0.92f, 0.9f), 0.4f), false, Quaternion.Euler(18f, -25f, 0f));
            Mats.Prim(PrimitiveType.Cube, "CctvMount", t, new Vector3(5.5f, 3.23f, D * 0.5f + 0.24f), new Vector3(0.04f, 0.06f, 0.04f), frame, false);
            // 문 앞 막이(들어가지 못하게): 투명 콜라이더
            var block = new GameObject("StoreBlock").AddComponent<BoxCollider>();
            block.transform.SetParent(t, false);
            block.center = new Vector3(0f, Ht * 0.5f, 0f);
            block.size = new Vector3(W, Ht, D);

            // 간판 (사용자 참고 사진): 앞으로 튀어나온 보라 간판 상자, 왼쪽 연두 알약 "Nice to" + 흰 입체 글자 "CU" + 오른쪽 끝까지 연두 띠.
            // 앞에서 보면 +x가 왼쪽. 아래 처마에 다운라이트
            var fascia = Mats.Emissive(new Color(0.36f, 0.15f, 0.55f), 0.18f);
            const float FasciaFront = D * 0.5f + 0.5f, by = 3.8f;
            Mats.Prim(PrimitiveType.Cube, "Fascia", t, new Vector3(0f, 3.8f, D * 0.5f + 0.225f), new Vector3(W + 0.3f, 1.1f, 0.55f), fascia, false);
            for (int k = 0; k < 4; k++)
                Mats.Prim(PrimitiveType.Cylinder, "Downlight", t, new Vector3(-4.5f + k * 3f, 3.245f, D * 0.5f + 0.25f), new Vector3(0.16f, 0.006f, 0.16f), Mats.Emissive(new Color(1f, 0.97f, 0.9f), 2f), false);
            float signZ = FasciaFront + 0.03f;
            void Pill(string name, float x0, float x1)
            {
                Mats.Prim(PrimitiveType.Cube, name, t, new Vector3((x0 + x1) * 0.5f, by, signZ), new Vector3(x1 - x0, 0.48f, 0.06f), lime, false);
                foreach (float x in new[] { x0, x1 })
                    Mats.Prim(PrimitiveType.Cylinder, name + "End", t, new Vector3(x, by, signZ - 0.003f), new Vector3(0.48f, 0.025f, 0.48f), lime, false, Quaternion.Euler(90f, 0f, 0f));
            }
            Pill("NicePill", 3.7f, 5.2f);
            Text("Nice to", new Vector3(4.45f, by - 0.01f, signZ + 0.04f), cuPurple, 3.6f);
            // 흰 CU: 뒤에 연보라 글자를 살짝 비켜 겹쳐 두께가 보이게 (채널 글자)
            var cuBack = Mats.Emissive(new Color(0.42f, 0.2f, 0.62f), 0.45f);
            Text("CU", new Vector3(2.83f, by - 0.02f, FasciaFront + 0.015f), new Color(0.7f, 0.64f, 0.86f), 9.5f);
            Text("CU", new Vector3(2.85f, by, FasciaFront + 0.035f), new Color(0.99f, 0.98f, 1f), 9.5f);
            Pill("SignBand", -5.5f, 1.9f);
            Text("한강공원 반포점", new Vector3(-3.9f, by - 0.01f, signZ + 0.04f), cuPurple, 2.4f);
            // 옆벽 둥근 보라 간판
            Mats.Prim(PrimitiveType.Cylinder, "SideSign", t, new Vector3(W * 0.5f + 0.03f, 3.8f, D * 0.5f - 1f), new Vector3(0.9f, 0.04f, 0.9f), cuBack, false, Quaternion.Euler(0f, 0f, 90f));
            var side = UIFactory.WorldText(t, "CU", new Vector3(W * 0.5f + 0.08f, 3.78f, D * 0.5f - 1f), Color.white, 4.5f, "CU_Side");
            side.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            side.fontStyle = TMPro.FontStyles.Bold;
            UIFactory.OneSided(side);

            // 기둥 앞 배송 상자 더미 (BGF로지스: 보라·회색·초록·파랑) + 쓰레기봉투
            Color[] crateCols = { new Color(0.36f, 0.2f, 0.55f), new Color(0.5f, 0.52f, 0.55f), new Color(0.15f, 0.48f, 0.3f), new Color(0.15f, 0.3f, 0.65f) };
            var stacks = new[] { (x: 5.1f, z: 4.38f, n: 5, yaw: 2f), (x: 5.72f, z: 4.42f, n: 3, yaw: -3f), (x: 5.35f, z: 4.95f, n: 2, yaw: 88f), (x: -5.2f, z: 4.38f, n: 4, yaw: -2f), (x: -5.8f, z: 4.42f, n: 2, yaw: 4f) };
            int ci = 0;
            foreach (var st in stacks)
                for (int k = 0; k < st.n; k++, ci++)
                {
                    var crate = KenneyProps.Spawn("Props/crate_cu", t, new Vector3(st.x, 0.06f + k * 0.285f, st.z), st.yaw + (ci * 37 % 7 - 3), 0f, 0f, false);
                    if (crate != null) KenneyProps.Tint(crate, "CrateBody", Mats.Lit(crateCols[(ci * 5 + k) % crateCols.Length], 0.35f));
                }
            Mats.Prim(PrimitiveType.Sphere, "TrashBag", t, new Vector3(-4.62f, 0.27f, 4.4f), new Vector3(0.46f, 0.5f, 0.42f), Mats.Lit(new Color(0.55f, 0.28f, 0.14f), 0.55f), false);
            Mats.Prim(PrimitiveType.Sphere, "TrashBag", t, new Vector3(4.5f, 0.24f, 4.45f), new Vector3(0.42f, 0.44f, 0.4f), Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0.6f), false);

            // 안: 진열대 세 줄(색색 상품), 냉장고 벽, 계산대, 한강라면 조리기
            var shelf = Mats.Lit(new Color(0.85f, 0.85f, 0.86f), 0.3f, 0.3f);
            var goods = new[]
            {
                Mats.Lit(new Color(0.9f, 0.25f, 0.2f)), Mats.Lit(new Color(0.95f, 0.75f, 0.2f)), Mats.Lit(new Color(0.25f, 0.5f, 0.85f)),
                Mats.Lit(new Color(0.3f, 0.7f, 0.35f)), Mats.Lit(new Color(0.95f, 0.95f, 0.9f)), Mats.Lit(new Color(0.85f, 0.4f, 0.65f)),
            };
            var rng = new System.Random(24);
            for (int row = 0; row < 3; row++)
            {
                float sz = -1.6f + row * 1.4f;
                Mats.Prim(PrimitiveType.Cube, "Shelf", t, new Vector3(-2f, 0.75f, sz), new Vector3(5f, 1.5f, 0.5f), shelf, false);
                for (int lv = 0; lv < 3; lv++)
                for (float x = -4.3f; x < 0.3f; x += 0.42f)
                    foreach (float face in new[] { -0.2f, 0.2f })
                    {
                        float gh = 0.2f + (float)rng.NextDouble() * 0.15f;
                        Mats.Prim(PrimitiveType.Cube, "Goods", t, new Vector3(x + 0.2f, 0.3f + lv * 0.45f + gh * 0.5f, sz + face), new Vector3(0.34f, gh, 0.12f), goods[rng.Next(goods.Length)], false);
                    }
            }
            // 뒤 벽 냉장고 (유리문 + 안쪽 불빛)
            Mats.Prim(PrimitiveType.Cube, "Fridge", t, new Vector3(-1.5f, 1.1f, -D * 0.5f + 0.7f), new Vector3(8f, 2.2f, 0.8f), Mats.Emissive(new Color(0.85f, 0.95f, 1f), 0.8f), false);
            for (float x = -5.2f; x <= 2.3f; x += 1f)
                Mats.Prim(PrimitiveType.Cube, "FridgeDoor", t, new Vector3(x, 1.1f, -D * 0.5f + 1.12f), new Vector3(0.06f, 2.2f, 0.06f), frame, false);
            // 계산대 + 포스기
            Mats.Prim(PrimitiveType.Cube, "Counter", t, new Vector3(3.8f, 0.5f, 0.6f), new Vector3(2.6f, 1f, 1f), Mats.Lit(new Color(0.55f, 0.35f, 0.65f)), false);
            Mats.Prim(PrimitiveType.Cube, "CounterTop", t, new Vector3(3.8f, 1.03f, 0.6f), new Vector3(2.7f, 0.06f, 1.1f), Mats.Lit(new Color(0.9f, 0.9f, 0.9f), 0.5f), false);
            Mats.Prim(PrimitiveType.Cube, "Pos", t, new Vector3(3.4f, 1.25f, 0.6f), new Vector3(0.4f, 0.35f, 0.3f), Mats.Lit(new Color(0.15f, 0.15f, 0.17f), 0.5f), false);
            // 한강라면 조리기 두 대 (은색 상자 + 빨간 숫자 창 + 검은 화구)
            var steel = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.7f, 0.8f);
            for (int k = 0; k < 2; k++)
            {
                float x = 4.6f - k * 0.0f;
                float z = -1.4f - k * 1.1f;
                Mats.Prim(PrimitiveType.Cube, "RamenTable", t, new Vector3(x, 0.45f, z), new Vector3(1.2f, 0.9f, 0.9f), frame, false);
                Mats.Prim(PrimitiveType.Cube, "RamenCooker", t, new Vector3(x, 1.25f, z), new Vector3(0.7f, 0.7f, 0.6f), steel, false);
                Mats.Prim(PrimitiveType.Cube, "RamenDisplay", t, new Vector3(x - 0.36f, 1.42f, z), new Vector3(0.02f, 0.12f, 0.3f), Mats.Emissive(new Color(1f, 0.15f, 0.1f), 1.5f), false);
                Mats.Prim(PrimitiveType.Cylinder, "RamenPlate", t, new Vector3(x - 0.05f, 1.02f, z), new Vector3(0.36f, 0.02f, 0.36f), Mats.Lit(new Color(0.1f, 0.1f, 0.1f)), false);
            }
            var label = UIFactory.WorldText(t, "한강라면", new Vector3(3.9f, 2.1f, -0.85f), new Color(1f, 0.85f, 0.3f), 2.2f, "RamenLabel");
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            UIFactory.OneSided(label);
            // 밤 불빛 (가게 안 → 앞마당)
            var lightGo = new GameObject("StoreLight");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 3f, D * 0.5f + 1.5f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.range = 12f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;

            // 앞마당: 붉은 보도블록 + 연석, 파라솔 탁자 넷 (Props/parasol_cu·table_cu·chair_cu)
            const float YardD = 6.5f;
            Mats.Prim(PrimitiveType.Cube, "Yard", t, new Vector3(0f, 0.03f, D * 0.5f + YardD * 0.5f), new Vector3(W + 3f, 0.06f, YardD),
                Mats.Weathered("greytile", new Color(0.66f, 0.42f, 0.36f), 0.8f, 0.15f, 0f, 0f, 0.05f), false);
            Mats.Prim(PrimitiveType.Cube, "YardCurb", t, new Vector3(0f, 0.06f, D * 0.5f + YardD), new Vector3(W + 3.1f, 0.12f, 0.15f), Mats.Lit(new Color(0.7f, 0.7f, 0.68f)), false);
            for (int k = 0; k < 4; k++)
            {
                var c = new Vector3(-4.5f + k * 3f, 0.06f, D * 0.5f + 2.4f + (k % 2) * 1.4f);
                float ty = (k * 23) % 30 - 15f;
                if (KenneyProps.Spawn("Props/table_cu", t, c, ty, 0f, 0f, false) == null) continue;
                KenneyProps.Spawn("Props/parasol_cu", t, c, ty + 22.5f, 0f, 0f, false);
                for (int ch = 0; ch < 4; ch++)
                {
                    if ((k + ch) % 5 == 4) continue; // 빈 자리 하나씩
                    float a = (ch * 90f + ty + 8f * Mathf.Sin(k * 3f + ch)) * Mathf.Deg2Rad;
                    var cp = c + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.72f;
                    // 의자 앞(+z)이 탁자를 보게, 조금씩 비뚤게 빼 놓았다
                    KenneyProps.Spawn("Props/chair_cu", t, cp, a * Mathf.Rad2Deg + 180f + 10f * Mathf.Cos(k + ch * 2f), 0f, 0f, false);
                }
            }

            // 모퉁이 기둥 간판 "CU 24h Open": 회색 기둥 위 흰 상자, 보라 테
            var pyl = new Vector3(W * 0.5f + 1.2f, 0f, D * 0.5f + YardD - 0.6f);
            Mats.Prim(PrimitiveType.Cube, "PylonPole", t, pyl + new Vector3(0f, 1.6f, 0f), new Vector3(0.18f, 3.2f, 0.18f), frame);
            Mats.Prim(PrimitiveType.Cube, "PylonFrame", t, pyl + new Vector3(0f, 3.6f, 0f), new Vector3(1.3f, 1.4f, 0.3f), purple, false);
            Mats.Prim(PrimitiveType.Cube, "PylonFace", t, pyl + new Vector3(0f, 3.62f, 0f), new Vector3(1.15f, 1.22f, 0.34f), Mats.Emissive(new Color(0.97f, 0.97f, 0.99f), 0.6f), false);
            foreach (float face in new[] { 1f, -1f })
            {
                var pc = UIFactory.WorldText(t, "CU", pyl + new Vector3(0f, 3.75f, face * 0.18f), new Color(0.42f, 0.2f, 0.62f), 8f, "PylonCU");
                pc.transform.localRotation = Quaternion.Euler(0f, face > 0f ? 180f : 0f, 0f); pc.fontStyle = TMPro.FontStyles.Bold; UIFactory.OneSided(pc);
                var po = UIFactory.WorldText(t, "24h Open", pyl + new Vector3(0f, 3.22f, face * 0.18f), new Color(0.3f, 0.3f, 0.32f), 2f, "PylonOpen");
                po.transform.localRotation = pc.transform.localRotation; UIFactory.OneSided(po);
            }

            // 분리수거함: 스테인리스 칸 셋 + 위 색 띠 이름표, 옆에 흰 비닐 통
            var steelBin = Mats.Lit(new Color(0.72f, 0.73f, 0.74f), 0.55f, 0.7f);
            var bin = new Vector3(-W * 0.5f - 1.3f, 0f, D * 0.5f + 1.2f);
            string[] binNames = { "일반쓰레기", "재활용", "캔·병" };
            Color[] binCols = { new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.8f), new Color(0.25f, 0.6f, 0.3f) };
            for (int k = 0; k < 3; k++)
            {
                var bp = bin + new Vector3(0f, 0f, k * 0.72f);
                Mats.Prim(PrimitiveType.Cube, "RecycleBin", t, bp + new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.68f), steelBin);
                Mats.Prim(PrimitiveType.Cube, "BinMouth", t, bp + new Vector3(0.31f, 0.8f, 0f), new Vector3(0.02f, 0.14f, 0.4f), Mats.Lit(new Color(0.08f, 0.08f, 0.09f)), false);
                Mats.Prim(PrimitiveType.Cube, "BinLabel", t, bp + new Vector3(0.31f, 0.95f, 0f), new Vector3(0.02f, 0.1f, 0.66f), Mats.Lit(binCols[k], 0.3f), false);
                var lb = UIFactory.WorldText(t, binNames[k], bp + new Vector3(0.33f, 0.945f, 0f), Color.white, 0.8f, "BinText");
                lb.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); UIFactory.OneSided(lb);
            }
            Mats.Prim(PrimitiveType.Cylinder, "BagBin", t, bin + new Vector3(0.1f, 0.42f, 2.3f), new Vector3(0.55f, 0.42f, 0.55f), Mats.Lit(new Color(0.94f, 0.94f, 0.95f), 0.5f));

            // 한강라면 세움 배너 (X배너)
            var xb = new Vector3(-4.1f, 0f, D * 0.5f + 1.05f);
            Mats.Prim(PrimitiveType.Cube, "XBanner", t, xb + new Vector3(0f, 1.05f, 0f), new Vector3(0.6f, 1.6f, 0.02f), purple, false);
            Mats.Prim(PrimitiveType.Cube, "XBannerFoot", t, xb + new Vector3(0f, 0.12f, -0.12f), new Vector3(0.05f, 0.24f, 0.4f), frame, false, Quaternion.Euler(30f, 0f, 0f));
            var xt = UIFactory.WorldText(t, "한강\n라면", xb + new Vector3(0f, 1.25f, 0.02f), new Color(1f, 0.9f, 0.3f), 2.6f, "XBannerText");
            xt.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); xt.fontStyle = TMPro.FontStyles.Bold; UIFactory.OneSided(xt);
        }

        /// <summary>편의점 외벽 패널 한 장 (흰 판 + 얕은 줄눈 + 판마다 작은 타공 점 격자). 알파 = 매끈함.</summary>
        static Texture2D PanelTex()
        {
            if (_panelTex != null) return _panelTex;
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float v = 1f;
                if (x < 2 || y < 2) v = 0.72f;                              // 줄눈
                else if (x < 4 || y < 4) v = 0.9f;
                else
                {
                    float gx = Mathf.Repeat((x - 4) / 12f, 1f) - 0.5f, gy = Mathf.Repeat((y - 4) / 12f, 1f) - 0.5f;
                    if (gx * gx + gy * gy < 0.03f) v = 0.9f;               // 타공 점
                }
                px[y * S + x] = new Color(v, v, v, v < 0.8f ? 0.2f : 0.7f);
            }
            tex.SetPixels(px); tex.Apply(true);
            return _panelTex = tex;
        }

        static Texture2D _panelTex;
    }
}
