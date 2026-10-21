using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 새 해외 맵(World/Maps/*.cs)의 랜드마크를 짓는 공용 도구: 회전체(탑·돔·등대·대불 받침), 관 묶음(맹그로브 뿌리·밧줄),
    /// 원판 묶음(연잎), 눈 덮인 산, 폭포, 기울어진 들보(케이블·사다리), Blender 소품(Resources/Props) 놓기.
    /// 많은 조각은 메쉬 하나로 합쳐 렌더러 수를 줄인다 (한 맵 프리미티브 700개 안쪽).
    /// </summary>
    public static class MapKit
    {
        // ---------------- 메쉬 ----------------

        /// <summary>
        /// 회전체: prof = (반지름, 높이)를 y축으로 돌린다. notch &lt; 1이면 꼭짓점 하나 걸러 반지름을 줄여 각진 톱니 단면
        /// (크메르 프랑의 층층이 꺾인 모서리). UV: u = 둘레 m / tile, v = 높이 m / tile.
        /// </summary>
        public static Mesh Lathe(IList<Vector2> prof, int sides, float tile = 4f, float notch = 1f, bool capTop = true, float squashZ = 1f)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            int cols = sides + 1;
            float acc = 0f;
            for (int r = 0; r < prof.Count; r++)
            {
                if (r > 0) acc += Vector2.Distance(prof[r], prof[r - 1]);
                for (int k = 0; k <= sides; k++)
                {
                    float a = k / (float)sides * Mathf.PI * 2f;
                    float rad = prof[r].x * (k % 2 == 1 ? notch : 1f);
                    v.Add(new Vector3(Mathf.Cos(a) * rad, prof[r].y, Mathf.Sin(a) * rad * squashZ));
                    uv.Add(new Vector2(k / (float)sides * Mathf.PI * 2f * Mathf.Max(0.5f, prof[r].x) / tile, acc / tile));
                }
            }
            for (int r = 0; r < prof.Count - 1; r++)
            for (int k = 0; k < sides; k++)
            {
                int a = r * cols + k, b = a + 1, c = a + cols, d = c + 1;
                t.AddRange(new[] { a, c, b, b, c, d });
            }
            if (capTop && prof[prof.Count - 1].x > 0.001f)
            {
                int ci = v.Count;
                v.Add(new Vector3(0f, prof[prof.Count - 1].y, 0f));
                uv.Add(Vector2.zero);
                int top = (prof.Count - 1) * cols;
                for (int k = 0; k < sides; k++) t.AddRange(new[] { ci, top + k + 1, top + k });
            }
            return Finish("Lathe", v, uv, t);
        }

        /// <summary>점들을 따라가는 관 여러 개를 메쉬 하나로 (맹그로브 뿌리, 밧줄, 갈대). radius(u)로 굵기가 변한다.</summary>
        public static Mesh Tubes(IEnumerable<Vector3[]> paths, System.Func<float, float> radius, int sides = 6)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            foreach (var pts in paths)
            {
                int n = pts.Length, b0 = v.Count;
                Vector3 prevSide = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    var dir = (pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                    var side = i == 0 ? Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized
                        : (prevSide - dir * Vector3.Dot(prevSide, dir)).normalized;
                    prevSide = side;
                    var up = Vector3.Cross(side, dir);
                    float rr = radius(i / (float)(n - 1));
                    for (int k = 0; k <= sides; k++)
                    {
                        float a = k / (float)sides * Mathf.PI * 2f;
                        v.Add(pts[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rr);
                        uv.Add(new Vector2(k / (float)sides, i / (float)(n - 1)));
                    }
                }
                for (int i = 0; i < n - 1; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = b0 + i * (sides + 1) + k, b = a + 1, c = a + sides + 1, d = c + 1;
                    t.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            return Finish("Tubes", v, uv, t);
        }

        /// <summary>납작한 원판 여러 개 (x, z, 반지름, 높이) — 수면의 연잎, 화단 꽃. 한쪽에 쐐기 홈(연잎)을 낼 수 있다.</summary>
        public static Mesh Discs(IEnumerable<Vector4> discs, int sides = 10, bool notch = false)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            int idx = 0;
            foreach (var d in discs)
            {
                int c = v.Count;
                v.Add(new Vector3(d.x, d.w + 0.01f, d.y));
                uv.Add(new Vector2(0.5f, 0.5f));
                float rot = (idx++ * 2.39996f) % (Mathf.PI * 2f);
                int ring = notch ? sides - 1 : sides;
                for (int k = 0; k <= ring; k++)
                {
                    float a = rot + k / (float)sides * Mathf.PI * 2f + (notch ? 0.3f : 0f);
                    v.Add(new Vector3(d.x + Mathf.Cos(a) * d.z, d.w, d.y + Mathf.Sin(a) * d.z));
                    uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }
                for (int k = 0; k < ring; k++) t.AddRange(new[] { c, c + k + 2, c + k + 1 });
            }
            return Finish("Discs", v, uv, t);
        }

        /// <summary>
        /// 먼 산줄기 (rx × rz 타원 바닥, 높이 h). 꼭대기 쪽 삼각형은 둘째 서브메쉬(눈)로 나눈다 — snowLine(0~1 높이 비율)보다 높은 곳.
        /// 재질은 [바위, 눈] 두 개.
        /// </summary>
        public static Mesh Mountain(float rx, float rz, float h, int seed, float snowLine = 2f, int peaks = 3)
        {
            const int seg = 180, rings = 64;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var rnd = new System.Random(seed);
            var peakX = new float[peaks];
            var peakH = new float[peaks];
            for (int i = 0; i < peaks; i++) { peakX[i] = (float)(rnd.NextDouble() * 1.4 - 0.7); peakH[i] = (float)(0.6 + rnd.NextDouble() * 0.4); }
            for (int r = 0; r <= rings; r++)
            {
                float f = r / (float)rings;
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * (1f - f), z = Mathf.Sin(a) * (1f - f);
                    // 능선: 봉우리 몇 개를 가로로 늘어놓고 골짜기 노이즈
                    float ridge = 0f;
                    for (int i = 0; i < peaks; i++) ridge = Mathf.Max(ridge, peakH[i] * Mathf.Exp(-(x - peakX[i]) * (x - peakX[i]) * 6f));
                    float n = Mathf.PerlinNoise(seed + x * 3.1f, seed * 0.7f + z * 3.1f) - 0.5f;
                    float n2 = Mathf.PerlinNoise(seed + x * 9f, z * 9f) - 0.5f;
                    // 능선·골짜기 (사진: 레프카 오리·토로스 산맥은 날카로운 석회암 능선과 깊게 팬 골짜기): 리지 노이즈 세 옥타브.
                    // 산 위쪽일수록 세게 → 아래는 완만한 기슭, 위는 칼날 능선
                    float rdg = 0f, amp = 0.5f, fq = 5f;
                    for (int o = 0; o < 3; o++)
                    {
                        float q = Mathf.PerlinNoise(seed * 1.7f + x * fq + o * 13.1f, z * fq + o * 7.3f) * 2f - 1f;
                        rdg += (1f - Mathf.Abs(q)) * amp;
                        amp *= 0.5f; fq *= 2.3f;
                    }
                    float hh = h * Mathf.SmoothStep(0f, 1f, f) * (0.55f + 0.45f * ridge) * (1f + n * 0.5f + n2 * 0.12f);
                    hh *= 0.78f + 0.42f * rdg * Mathf.SmoothStep(0.15f, 0.8f, f);
                    v.Add(new Vector3(x * rx, hh - 4f, z * rz));
                    uv.Add(new Vector2(x * rx / 24f, (hh + z * rz) / 24f));
                }
            }
            var rock = new List<int>();
            var snow = new List<int>();
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < seg; s++)
            {
                int i = r * (seg + 1) + s;
                int[] tri1 = { i, i + seg + 1, i + 1 }, tri2 = { i + 1, i + seg + 1, i + seg + 2 };
                foreach (var tri in new[] { tri1, tri2 })
                {
                    float y = (v[tri[0]].y + v[tri[1]].y + v[tri[2]].y) / 3f;
                    float jag = (Mathf.PerlinNoise(v[tri[0]].x * 0.05f, v[tri[0]].z * 0.05f) - 0.5f) * 0.12f * h;
                    (y + jag > snowLine * h ? snow : rock).AddRange(tri);
                }
            }
            var m = new Mesh { name = "Mountain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.subMeshCount = 2;
            m.SetTriangles(rock, 0);
            m.SetTriangles(snow, 1);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// 폭포 물줄기 (사용자 참고 사진: 듀덴 폭포): 절벽 끝(원점)에서 앞(+Z)으로 뿜어져 나와 포물선으로 떨어지는 휜 물 커튼.
        /// 물은 절벽 끝을 수평 속도로 떠나 중력으로 떨어지므로 앞으로 나간 거리는 낙하 비율의 제곱근에 비례한다 (lip = 바닥까지 나간 거리).
        /// 위 끝은 둥글게 넘쳐흐르는 물마루, 아래로 갈수록 넓게 퍼지고, 가로로 물결이 굽이친다. seed마다 모양이 다르다.
        /// UV0: u = 가로 m/3, v = 낙차 m/14 (결 텍스처 반복), UV1: (가로 0~1, 세로 0 위~1 아래) — Fishing/Waterfall 셰이더가 가장자리·물보라에 쓴다.
        /// </summary>
        public static Mesh FallSheet(float width, float drop, float lip, int cols = 18, int rows = 24, int seed = 0)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var t = new List<int>();
            float sd = seed * 1.37f;
            for (int r = 0; r <= rows; r++)
            {
                // 위쪽에 줄을 더 촘촘히 (휘는 곳)
                float f = Mathf.Pow(r / (float)rows, 1.6f);
                for (int c = 0; c <= cols; c++)
                {
                    float u = c / (float)cols;
                    float x = (u - 0.5f) * width * (1f + 0.28f * f);
                    float y = -drop * f;
                    // 물마루: 절벽 끝 위로 둥글게 넘친다 (가운데가 조금 더 앞으로)
                    float crest = Mathf.Sin(u * Mathf.PI);
                    float z = lip * Mathf.Sqrt(f) + crest * 0.3f * (1f - Mathf.Clamp01(f * 6f));
                    if (f < 0.04f) y += crest * 0.35f * (1f - f / 0.04f);
                    // 가로 굽이 (물줄기 다발) + 아래로 갈수록 커지는 흔들림
                    z += (Mathf.PerlinNoise(u * 5f + sd, 0.3f) - 0.5f) * 1.2f * Mathf.Sqrt(f) + Mathf.Sin(u * 13f + sd + f * 4f) * 0.35f * f;
                    v.Add(new Vector3(x, y, z));
                    uv.Add(new Vector2(u * width / 3f, f * drop / 14f));
                    uv2.Add(new Vector2(u, f));
                }
            }
            // 양면: 뒷면은 꼭짓점을 따로 둔다 (같은 꼭짓점에 앞뒤 삼각형을 겹치면 법선이 0이 되어 검은 얼룩이 생겼다)
            int n = v.Count;
            for (int i = 0; i < n; i++) { v.Add(v[i]); uv.Add(uv[i]); uv2.Add(uv2[i]); }
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int a = r * (cols + 1) + c, b = a + 1, cc = a + cols + 1, d = cc + 1;
                t.AddRange(new[] { a, b, cc, b, d, cc });
                t.AddRange(new[] { n + a, n + cc, n + b, n + b, n + cc, n + d });
            }
            var m = Finish("Fall", v, uv, t);
            m.SetUVs(1, uv2);
            return m;
        }

        static Mesh Finish(string name, List<Vector3> v, List<Vector2> uv, List<int> t)
        {
            var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ---------------- 텍스처 ----------------

        static Texture2D _fallTex, _stripes;

        /// <summary>폭포 결: 세로로 흐르는 흰 줄과 반투명 틈 (알파 포함).</summary>
        public static Texture2D FallTexture()
        {
            if (_fallTex != null) return _fallTex;
            const int W = 128, H = 256;
            _fallTex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                // 사진(듀덴 폭포): 가는 세로 물줄기 결 + 떨어지며 뭉친 흰 거품 덩어리 + 줄기 사이로 비치는 틈. 가로·세로 모두 이어진다
                float u = x / (float)W, v = y / (float)H;
                float streak = Mathf.PerlinNoise(u * 58f, v * 5f) * 0.6f + Mathf.PerlinNoise(u * 160f, v * 15f) * 0.4f;
                float clump = Mathf.PerlinNoise(u * 14f + 3f, v * 9f) * Mathf.PerlinNoise(u * 31f, v * 22f + 5f);
                float gap = Mathf.SmoothStep(0.25f, 0.42f, Mathf.PerlinNoise(u * 22f + 9f, v * 2.5f));
                float a = Mathf.Clamp01((0.35f + streak * 0.75f) * (0.45f + 0.55f * gap) + clump * 0.8f);
                float w = Mathf.Clamp01(0.8f + 0.14f * streak + clump * 0.4f);
                px[y * W + x] = new Color(w, w * 1.01f, w * 1.03f, a);
            }
            _fallTex.SetPixels(px);
            _fallTex.Apply(true);
            return _fallTex;
        }

        /// <summary>폭포 밑 물안개: 천천히 피어올라 바람에 흩어지는 큰 흰 입자 (파티클). parent와 함께 지워진다.</summary>
        public static ParticleSystem Mist(Transform parent, Vector3 pos, float radius, float rate = 14f)
        {
            var go = new GameObject("Mist");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.5f, radius * 1.1f);
            main.startColor = new Color(1f, 1f, 1f, 0.22f);
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = radius;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Particle(new Color(1f, 1f, 1f, 0.5f));
            ps.Play();
            return ps;
        }

        /// <summary>가로 줄무늬 두 색 (기둥·차양).</summary>
        public static Texture2D Stripes()
        {
            if (_stripes != null) return _stripes;
            _stripes = new Texture2D(4, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var px = new Color[64];
            for (int i = 0; i < 64; i++) px[i] = (i / 4) % 8 < 4 ? new Color(0.85f, 0.15f, 0.15f) : Color.white;
            _stripes.SetPixels(px);
            _stripes.Apply();
            return _stripes;
        }

        // ---------------- 재질 ----------------

        /// <summary>먼 랜드마크 재질 (안개를 덜 받아 수백 m 밖에서도 보인다).</summary>
        public static Material Far(Color c, float smooth = 0.15f, float metal = 0f) => HanRiverLandmarks.Mat(c, null, null, Color.black, smooth, metal, Vector2.one);

        /// <summary>
        /// 먼 산·절벽 재질: Far와 같은 옅은 안개(Landmark 셰이더)에 실사 바위 결(Resources/Terrain/{tex})을 월드 트라이플래너로 (tile m마다).
        /// 매끈한 단색 산이 장난감처럼 보이던 것을 고친다. 같은 인자면 공유.
        /// </summary>
        public static Material FarRock(Color c, string tex = "cliff", float tile = 40f, float strength = 0.85f, float smooth = 0.08f)
        {
            string key = $"{c}{tex}{tile}{strength}{smooth}";
            if (FarRockCache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Far(c, smooth));
            var d = Resources.Load<Texture2D>("Terrain/" + tex + "_diff");
            if (d != null && m.HasProperty("_DetailTex"))
            {
                m.SetTexture("_DetailTex", d);
                m.SetColor("_DetailAvg", Mats.AvgOf(tex));
                m.SetFloat("_DetailTile", tile);
                m.SetFloat("_DetailStrength", strength);
            }
            FarRockCache[key] = m;
            return m;
        }
        static readonly System.Collections.Generic.Dictionary<string, Material> FarRockCache = new System.Collections.Generic.Dictionary<string, Material>();

        /// <summary>
        /// 흐르는 폭포 재질 (Fishing/Waterfall): 결 텍스처가 두 겹으로 흘러내리고(셰이더가 _Time으로 스크롤), 가장자리는 들쭉날쭉,
        /// 위 끝은 청록 맑은 물, 아래는 물보라로 부서진다. speed = 초당 UV. 셰이더가 없으면 예전 반투명 판.
        /// </summary>
        public static Material WaterfallMat(float speed = 0.55f, float opacity = 0.92f, Color? water = null)
        {
            var shader = Shader.Find("Fishing/Waterfall");
            if (shader == null) return FallMat();
            var m = new Material(shader) { mainTexture = FallTexture() };
            m.SetFloat("_Speed", speed);
            m.SetFloat("_Opacity", opacity);
            if (water.HasValue) m.SetColor("_WaterColor", water.Value);
            return m;
        }

        /// <summary>폭포가 떨어지는 곳의 물보라 (파티클): 수면에서 앞·옆으로 튀어 오르다 떨어지는 흰 물방울 덩어리. width = 물줄기 폭.</summary>
        public static ParticleSystem Splash(Transform parent, Vector3 pos, float width, float yaw, float rate = 60f)
        {
            var go = new GameObject("Splash");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(-90f, yaw, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
            main.startColor = new Color(1f, 1f, 1f, 0.55f);
            main.gravityModifier = 0.9f;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 38f;
            sh.radius = 1f;
            sh.scale = new Vector3(width, 2f, 1f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.8f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Particle(new Color(1f, 1f, 1f, 0.6f));
            ps.Play();
            return ps;
        }

        /// <summary>물줄기 앞면에서 바람에 날리는 엷은 물안개 (파티클). top = 절벽 끝 가운데, 아래로 drop m, yaw = 물이 떨어지는 쪽.</summary>
        public static ParticleSystem FallDrift(Transform parent, Vector3 top, float width, float drop, float yaw)
        {
            var go = new GameObject("FallDrift");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = top + Vector3.down * drop * 0.5f;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startColor = new Color(1f, 1f, 1f, 0.16f);
            main.gravityModifier = 0.25f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 24f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(width, drop * 0.9f, 3f);
            sh.position = new Vector3(0f, 0f, 4f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Particle(new Color(1f, 1f, 1f, 0.5f));
            ps.Play();
            return ps;
        }

        /// <summary>폭포 물줄기 재질: 반투명 흰 물결 텍스처 (셰이더가 없을 때 대비책).</summary>
        public static Material FallMat()
        {
            // 매끈하면 하늘이 비쳐 파란 얼룩이 된다 → 거친 물보라 면
            var m = Mats.Transparent(new Color(0.95f, 0.97f, 1f, 0.92f), 0.08f, false);
            m.mainTexture = FallTexture();
            return m;
        }

        // ---------------- 놓기 ----------------

        public static GameObject Place(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, float yaw = 0f, float scale = 1f, bool collider = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        public static GameObject Place(Transform parent, string name, Mesh mesh, Material[] mats, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var go = Place(parent, name, mesh, mats[0], pos, yaw, scale);
            go.GetComponent<MeshRenderer>().sharedMaterials = mats;
            return go;
        }

        /// <summary>a에서 b까지 이어진 상자 들보 (케이블, 사다리 다리, 버팀대). 단면 thick.</summary>
        public static GameObject Beam(Transform parent, Vector3 a, Vector3 b, float thick, Material mat, bool collider = false, string name = "Beam")
        {
            var d = b - a;
            return Mats.Prim(PrimitiveType.Cube, name, parent, (a + b) * 0.5f, new Vector3(thick, thick, d.magnitude), mat, collider, Quaternion.LookRotation(d.normalized, Mathf.Abs(d.normalized.y) > 0.99f ? Vector3.forward : Vector3.up));
        }

        /// <summary>Blender 소품 (Resources/Props/id). height 또는 length로 크기를 맞춘다.</summary>
        public static GameObject Prop(Transform parent, string id, Vector3 pos, float yaw, float height = 0f, float length = 0f, bool solid = true) =>
            KenneyProps.Spawn("Props/" + id, parent, pos, yaw, height, length, solid);

        /// <summary>from에서 to를 바라보는 yaw (도).</summary>
        public static float Face(Vector3 from, Vector3 to) => Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
    }
}
