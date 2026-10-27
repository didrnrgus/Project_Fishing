using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 바닷가 맵이 함께 쓰는 소품: 등대, 테트라포드, 정자(팔작지붕), 가로등, 낚시점, 바위.
    /// 서천 앞바다(<see cref="SeocheonWorldBuilder"/>)와 해안 맵 틀(<see cref="CoastWorldBuilder"/>)이 쓴다.
    /// 밤에 켜지는 불빛은 lamps·bulbs에 모아 DayNightCycle이 켜고 끈다.
    /// </summary>
    public class CoastProps
    {
        readonly Transform _root;
        readonly System.Random _rng;
        readonly List<Light> _lamps;
        readonly List<Renderer> _bulbs;

        public CoastProps(Transform root, System.Random rng, List<Light> lamps, List<Renderer> bulbs)
        {
            _root = root;
            _rng = rng;
            _lamps = lamps;
            _bulbs = bulbs;
        }

        float R(float min, float max) => (float)(min + _rng.NextDouble() * (max - min));

        public void Tetrapod(Vector3 p, Material mat) => Tetrapod(_root, p, Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f)), R(0.9f, 1.1f), mat, true);

        /// <summary>테트라포드 한 개 (다리 끝까지 약 1.05m × scale). collider면 볼록 메쉬 콜라이더 (찌가 걸린다). 위 다리 끝에 녹슨 인양 고리(둘째 재질).</summary>
        public static void Tetrapod(Transform parent, Vector3 p, Quaternion rot, float scale, Material mat, bool collider)
        {
            var go = new GameObject("Tetrapod");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.transform.localRotation = rot;
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = TetrapodMesh();
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { mat, HookMat };
            if (collider) go.AddComponent<MeshCollider>().convex = true;
        }

        static Material _hookMat;
        static Material HookMat => _hookMat ??= Mats.Weathered("rust", new Color(0.5f, 0.3f, 0.18f), 0.5f, 0.3f, 1f, 0.2f, 0f, null, 0.5f);

        /// <summary>실물 테트라포드 크기 (다리 끝까지 약 1.5m, 높이 약 3m): 사진의 방파제 테트라포드는 사람 키를 훌쩍 넘는다.</summary>
        public const float TetraScale = 1.45f;

        public void TetrapodBank(Vector3 from, Vector3 to, Vector3 outward, float waterY, Material mat, int rows = 6) =>
            TetrapodBank(_root, from, to, outward, waterY, mat, rows);

        /// <summary>
        /// 방파제 바깥 테트라포드 더미 (2026-10-27 사용자 참고 사진 두 번째: 실제 방파제는 크고 마구 쌓였다):
        /// from→to 선을 따라 바깥(outward) 쪽으로 아래 켜 rows줄(2.05m 간격, 줄마다 반 칸 엇갈림, 바로 선 것·뒤집힌 것을 번갈아 경사를 따라 기울고 ±14° 흐트러짐) +
        /// 벽 쪽 세 줄 위에 한 켜 더(아래 켜 틈에 얹고 아무렇게나 돌아감) → 벽에 기댄 쪽은 파라펫 높이까지 쌓이고 바깥 줄일수록 물에 잠긴다.
        /// 배치 난수는 선 위치로 따로 (맵 난수 순서를 바꾸지 않는다).
        /// </summary>
        public static void TetrapodBank(Transform parent, Vector3 from, Vector3 to, Vector3 outward, float waterY, Material mat, int rows = 6)
        {
            const float step = 2.05f, rowGap = 1.75f;
            var rng = new System.Random(Mathf.RoundToInt(from.x * 31f + from.z * 17f + to.x * 7f));
            float R(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
            var along = to - from;
            float len = along.magnitude;
            if (len < 0.1f) return;
            along /= len;
            outward = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
            var lean = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, outward, 0.22f));
            var line = Quaternion.LookRotation(along, Vector3.up);
            for (int k = 0; k < rows; k++)
            {
                int i = 0;
                for (float s = (k % 2) * step * 0.5f; s < len; s += step, i++)
                {
                    var p = from + along * (s + R(-0.2f, 0.2f)) + outward * (1.2f + k * rowGap + R(-0.15f, 0.15f));
                    p.y = waterY + 0.9f - k * 0.55f + R(-0.12f, 0.12f);
                    bool flip = (i + k) % 2 == 1;
                    var rot = lean * line * Quaternion.Euler((flip ? 180f : 0f) + R(-14f, 14f), (flip ? 0f : 60f) + R(-25f, 25f), R(-14f, 14f));
                    Tetrapod(parent, p, rot, TetraScale * R(0.94f, 1.05f), mat, true);
                }
            }
            // 위 켜: 벽 쪽 세 줄 틈에 얹는다 (마구잡이 방향)
            for (int k = 0; k < Mathf.Min(3, rows - 1); k++)
                for (float s = step * 0.25f + (k % 2) * step * 0.5f; s < len; s += step * R(1f, 1.35f))
                {
                    var p = from + along * (s + R(-0.3f, 0.3f)) + outward * (1.5f + (k + 0.5f) * rowGap + R(-0.25f, 0.25f));
                    p.y = waterY + 2.1f - k * 0.6f + R(-0.15f, 0.15f);
                    Tetrapod(parent, p, Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f)), TetraScale * R(0.92f, 1.04f), mat, true);
                }
        }

        public void TetrapodHead(Vector3 tip, Vector3 outward, float halfWidth, float waterY, Material mat, int rings = 5) =>
            TetrapodHead(_root, tip, outward, halfWidth, waterY, mat, rings);

        /// <summary>
        /// 방파제 끝 둥근 테트라포드 더미 (사진: 등대 선 끝을 반원으로 감싼다). tip = 끝면 가운데(땅 높이 무시), outward = 방파제가 뻗은 방향, halfWidth = 몸통 반폭.
        /// 끝면에서 halfWidth 안쪽을 가운데로 링마다 ±115° 부채꼴, 안쪽 링이 높다. 위 켜 한 겹.
        /// </summary>
        public static void TetrapodHead(Transform parent, Vector3 tip, Vector3 outward, float halfWidth, float waterY, Material mat, int rings = 5)
        {
            var rng = new System.Random(Mathf.RoundToInt(tip.x * 13f + tip.z * 29f));
            float R(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
            outward = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
            var side = Vector3.Cross(Vector3.up, outward);
            var c = tip - outward * halfWidth;
            for (int k = 0; k < rings; k++)
            {
                float rr = halfWidth + 1.3f + k * 1.75f;
                int n = Mathf.Max(3, Mathf.RoundToInt(rr * Mathf.Deg2Rad * 230f / 2.05f));
                for (int i = 0; i <= n; i++)
                {
                    float th = Mathf.Lerp(-115f, 115f, (i + (k % 2) * 0.5f) / (n + 0.5f)) * Mathf.Deg2Rad;
                    var dir = outward * Mathf.Cos(th) + side * Mathf.Sin(th);
                    var p = c + dir * (rr + R(-0.2f, 0.2f));
                    p.y = waterY + 0.9f - k * 0.55f + R(-0.12f, 0.12f);
                    var lean = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, dir, 0.22f));
                    bool flip = (i + k) % 2 == 1;
                    Tetrapod(parent, p, lean * Quaternion.Euler((flip ? 180f : 0f) + R(-14f, 14f), R(0f, 360f), R(-14f, 14f)), TetraScale * R(0.94f, 1.05f), mat, true);
                    if (k < 2 && i % 2 == 0)
                    {
                        var q = c + dir * (rr + 0.9f) + Vector3.up * (waterY + 2.0f - k * 0.6f - c.y);
                        Tetrapod(parent, q, Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f)), TetraScale * R(0.92f, 1.04f), mat, true);
                    }
                }
            }
        }

        static Mesh _tetrapod;

        /// <summary>
        /// 테트라포드 메쉬: 정사면체 방향 다리 4개. 가운데가 굵고(반지름 0.42) 끝이 가는(0.24) 원뿔대이고,
        /// 끝은 평평하게 자르되 모서리를 둥글게 모따기(실물은 거푸집 모서리가 닳아 둥글다). 다리 뿌리는 가운데 공으로 메워 이음매가 보이지 않는다.
        /// 다리마다 링 7줄(뿌리 → 끝 모따기 3줄), 둘레 20칸 — 예전(링 2줄·12칸)보다 실루엣이 둥글다.
        /// </summary>
        public static Mesh TetrapodMesh()
        {
            if (_tetrapod != null) return _tetrapod;
            const int seg = 20;
            const float len = 1.05f, r0 = 0.42f, r1 = 0.24f, back = 0.15f, bevel = 0.07f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            // 링: (다리 방향 위치, 반지름). 끝 모따기는 1/4 원을 3칸으로
            var rings = new List<(float t, float r, bool cap)>();
            for (int i = 0; i <= 3; i++)
            {
                float f = i / 3f;
                rings.Add((Mathf.Lerp(-back, len - bevel, f), Mathf.Lerp(r0, r1 + bevel * 0.2f, f), false));
            }
            for (int i = 1; i <= 3; i++)
            {
                float a = i / 3f * Mathf.PI * 0.5f;
                rings.Add((len - bevel + Mathf.Sin(a) * bevel, r1 + bevel * 0.2f - (1f - Mathf.Cos(a)) * bevel * 1.2f, i == 3));
            }
            for (int leg = 0; leg < 4; leg++)
            {
                var d = leg == 0 ? Vector3.up : Quaternion.Euler(109.47f, leg * 120f, 0f) * Vector3.up;
                var q = Quaternion.FromToRotation(Vector3.up, d);
                var a = q * Vector3.right;
                var b = q * Vector3.forward;
                int start = v.Count;
                float slope = (r0 - r1) / (len + back);
                for (int k = 0; k < rings.Count; k++)
                {
                    var (t, r, _) = rings[k];
                    // 법선: 옆면은 원뿔대 경사, 모따기는 끝면 쪽으로 점점 기운다
                    float capW = k <= 3 ? 0f : (k - 3) / 3f;
                    for (int i = 0; i <= seg; i++)
                    {
                        float th = i / (float)seg * Mathf.PI * 2f;
                        var radial = Mathf.Cos(th) * a + Mathf.Sin(th) * b;
                        var side = (radial + d * slope).normalized;
                        v.Add(d * t + radial * r);
                        n.Add(Vector3.Slerp(side, d, capW * 0.9f).normalized);
                        uv.Add(new Vector2(i / (float)seg * 1.6f, (t + back) * 0.8f));
                    }
                }
                for (int k = 0; k < rings.Count - 1; k++)
                    for (int i = 0; i < seg; i++)
                    {
                        int b0 = start + k * (seg + 1) + i, b1 = b0 + 1, t0 = b0 + seg + 1, t1 = t0 + 1;
                        tri.AddRange(new[] { b0, t0, b1, b1, t0, t1 });
                    }
                // 끝면
                var (tc, rc, _) = rings[rings.Count - 1];
                int center = v.Count;
                v.Add(d * tc); n.Add(d); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= seg; i++)
                {
                    float th = i / (float)seg * Mathf.PI * 2f;
                    v.Add(d * tc + (Mathf.Cos(th) * a + Mathf.Sin(th) * b) * rc); n.Add(d);
                    uv.Add(new Vector2(0.5f + Mathf.Cos(th) * 0.25f, 0.5f + Mathf.Sin(th) * 0.25f));
                }
                for (int i = 0; i < seg; i++) tri.AddRange(new[] { center, center + 2 + i, center + 1 + i });
            }
            // 가운데 공: 네 다리 뿌리 사이 틈을 메운다
            const int ls = 10, lr = 16;
            const float cr = 0.43f;
            int c0 = v.Count;
            for (int i = 0; i <= ls; i++)
            {
                float ph = i / (float)ls * Mathf.PI;
                for (int j = 0; j <= lr; j++)
                {
                    float th = j / (float)lr * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(ph) * Mathf.Cos(th), Mathf.Cos(ph), Mathf.Sin(ph) * Mathf.Sin(th));
                    v.Add(dir * cr); n.Add(dir); uv.Add(new Vector2(j / (float)lr, i / (float)ls));
                }
            }
            for (int i = 0; i < ls; i++)
                for (int j = 0; j < lr; j++)
                {
                    int p0 = c0 + i * (lr + 1) + j, p1 = p0 + 1, p2 = p0 + lr + 1, p3 = p2 + 1;
                    tri.AddRange(new[] { p0, p1, p2, p1, p3, p2 });
                }
            // 인양 고리: 위 다리 끝면에 박힌 반원 쇠고리 (사진: 다리 끝마다 녹슨 고리)
            var hook = new List<int>();
            {
                const int hs = 10, hr = 6;
                const float R0 = 0.12f, r = 0.022f;
                int h0 = v.Count;
                for (int i = 0; i <= hs; i++)
                {
                    float ph = i / (float)hs * Mathf.PI;
                    var c = new Vector3(Mathf.Cos(ph) * R0, len + Mathf.Sin(ph) * R0 - 0.02f, 0f);
                    var radial = new Vector3(Mathf.Cos(ph), Mathf.Sin(ph), 0f);
                    for (int j = 0; j <= hr; j++)
                    {
                        float th = j / (float)hr * Mathf.PI * 2f;
                        var dir = radial * Mathf.Cos(th) + Vector3.forward * Mathf.Sin(th);
                        v.Add(c + dir * r); n.Add(dir); uv.Add(new Vector2(i / (float)hs, j / (float)hr));
                    }
                }
                for (int i = 0; i < hs; i++)
                    for (int j = 0; j < hr; j++)
                    {
                        int p0 = h0 + i * (hr + 1) + j, p1 = p0 + 1, p2 = p0 + hr + 1, p3 = p2 + 1;
                        hook.AddRange(new[] { p0, p2, p1, p1, p2, p3, p0, p1, p2, p1, p3, p2 });   // 양면 (감는 방향 걱정 없이)
                    }
            }
            _tetrapod = new Mesh { name = "Tetrapod", subMeshCount = 2 };
            _tetrapod.SetVertices(v);
            _tetrapod.SetNormals(n);
            _tetrapod.SetUVs(0, uv);
            _tetrapod.SetTriangles(tri, 0);
            _tetrapod.SetTriangles(hook, 1);
            _tetrapod.RecalculateTangents();
            _tetrapod.RecalculateBounds();
            return _tetrapod;
        }

        /// <summary>등대: 몸통 색, 밤 불빛 색 (항구 입구 오른쪽 빨강·왼쪽 흰색 — 실제 항로 표지 규칙). scale로 크기를 키운다. 모양은 <see cref="LighthouseBuilder"/>.</summary>
        public Transform Lighthouse(Vector3 at, Color body, Color lightColor, float scale = 1f)
        {
            var lh = new GameObject("Lighthouse").transform;
            lh.SetParent(_root, false);
            lh.localPosition = at;
            lh.localScale = Vector3.one * scale;
            _bulbs.Add(LighthouseBuilder.Small(lh, body));
            AddLamp(lh.position + Vector3.up * LighthouseBuilder.SmallLampY * scale, lightColor, 24f * scale, 2f);
            return lh;
        }

        /// <summary>
        /// 큰 유인 등대 (거문도 등대·동해 등대섬): 흰 팔각 탑, 붉은 등롱, 옆에 등대지기 숙소.
        /// </summary>
        public Transform GrandLighthouse(Vector3 at, float yaw)
        {
            var lh = new GameObject("GrandLighthouse").transform;
            lh.SetParent(_root, false);
            lh.localPosition = at;
            lh.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _bulbs.Add(LighthouseBuilder.Grand(lh));
            AddLamp(lh.position + Vector3.up * LighthouseBuilder.GrandLampY, new Color(1f, 0.95f, 0.75f), 60f, 2.6f);
            return lh;
        }

        /// <summary>묵호등대풍 큰 등대 (동해 등대섬). <see cref="LighthouseBuilder.Mukho"/>.</summary>
        public Transform MukhoLighthouse(Vector3 at, float yaw) => BigLighthouse("MukhoLighthouse", at, yaw, LighthouseBuilder.Mukho, LighthouseBuilder.MukhoLampY, 60f, 2.6f);

        /// <summary>거문도 등대 (새 등대 + 1905 옛 등대). <see cref="LighthouseBuilder.Geomun"/>.</summary>
        public Transform GeomunLighthouse(Vector3 at, float yaw) => BigLighthouse("GeomunLighthouse", at, yaw, LighthouseBuilder.Geomun, LighthouseBuilder.GeomunLampY, 70f, 2.8f);

        Transform BigLighthouse(string name, Vector3 at, float yaw, System.Func<Transform, Renderer> build, float lampY, float range, float intensity)
        {
            var lh = new GameObject(name).transform;
            lh.SetParent(_root, false);
            lh.localPosition = at;
            lh.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _bulbs.Add(build(lh));
            AddLamp(lh.position + Vector3.up * lampY, new Color(1f, 0.95f, 0.75f), range, intensity);
            return lh;
        }

        public void AddLamp(Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject("Lamp");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            _lamps.Add(l);
        }

        public void Streetlight(Vector3 basePos, float yaw = 180f)
        {
            Vector3 head;
            if (KenneyProps.Available)
            {
                const float lampH = 5.5f;
                KenneyProps.Spawn("Kenney/Roads/light-curved", _root, basePos, yaw, lampH);
                float s = lampH / 0.67f;
                head = basePos + Quaternion.Euler(0f, yaw - 180f, 0f) * new Vector3(0f, 0.62f * s, 0.19f * s);
            }
            else
            {
                Mats.Prim(PrimitiveType.Cylinder, "Pole", _root, basePos + Vector3.up * 2f, new Vector3(0.12f, 2f, 0.12f), Mats.Lit(new Color(0.2f, 0.22f, 0.25f)), false);
                head = basePos + Vector3.up * 4.1f;
            }
            var bulb = Mats.Prim(PrimitiveType.Sphere, "Bulb", _root, head, new Vector3(0.45f, 0.18f, 0.45f), Mats.Lit(new Color(0.2f, 0.22f, 0.25f)), false);
            _bulbs.Add(bulb.GetComponent<Renderer>());
            AddLamp(head - Vector3.up * 0.2f, new Color(1f, 0.82f, 0.55f), 14f, 1.6f);
        }

        /// <summary>팔작지붕 정자 (<see cref="Hanok.Pavilion"/>): 장대석 기단, 주춧돌, 석간주 기둥, 공포, 단청 창방, 계자 난간, 기와지붕.</summary>
        public Transform Pavilion(Vector3 at, float yaw) => Hanok.Pavilion(_root, at, yaw);

        /// <summary>낚시점 (<see cref="TackleShopKit"/>: 통유리 가게·간판·캐노피·냉동 쇼케이스) + 주인 NPC(NpcId "shop"). roof = 간판·캐노피 색.</summary>
        public NpcAngler TackleShop(Vector3 p, float yaw, string sign, CharacterLook owner, Color roof) =>
            TackleShopKit.Build(_root, p, yaw, sign, owner, roof, _bulbs);

        static readonly Color[] RoofColors =
        {
            new Color(0.2f, 0.38f, 0.68f), new Color(0.72f, 0.22f, 0.16f), new Color(0.22f, 0.5f, 0.36f),
            new Color(0.85f, 0.45f, 0.15f), new Color(0.35f, 0.55f, 0.75f), new Color(0.55f, 0.18f, 0.2f),
        };
        static readonly Color[] WallColors =
        {
            new Color(0.94f, 0.93f, 0.9f), new Color(0.92f, 0.88f, 0.78f), new Color(0.85f, 0.86f, 0.84f), new Color(0.9f, 0.84f, 0.74f),
        };
        static readonly string[] ShopSigns = { "횟집", "민박", "수산", "슈퍼", "식당", "낚시민박", "회센터", "카페" };
        static Mesh _gable;

        /// <summary>
        /// 바닷가 마을 집 (<see cref="HouseKit"/>): 단층 박공지붕 집(파랑·빨강·초록 칼라강판 골지붕, 가끔 시멘트 기와) 또는 2층 상가(평지붕 초록 방수칠, 옥탑, 옥상 물탱크, 1층 간판·셔터).
        /// 창·문은 벽을 비운 자리에 15cm 들어가 앉고, 기단·창턱·처마 판은 시멘트, 벽 밑동은 흙탕 그늘. 앞(+Z)이 길·바다 쪽. 몸 콜라이더를 붙인다.
        /// sign이면 1층 위에 간판 (바다 쪽에서 읽힌다). 맵 난수(_rng)는 예전과 같은 순서로만 쓰고 디테일은 집마다 따로 난수.
        /// </summary>
        public Transform KoreanHouse(Vector3 at, float yaw, bool sign)
        {
            var h = new GameObject("KoreanHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wallCol = WallColors[_rng.Next(WallColors.Length)];
            var roofCol = RoofColors[_rng.Next(RoofColors.Length)];
            bool twoStory = _rng.NextDouble() < 0.35;
            float w = R(7f, 11f), d = R(5.5f, 8f), floorH = 2.9f;
            float wallH = twoStory ? floorH * 2f : floorH;
            // 디테일 배치는 따로 난수 (맵의 다른 소품 배치가 바뀌지 않게)
            var rng = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            float Rd(float a0, float a1) => (float)(a0 + rng.NextDouble() * (a1 - a0));

            bool barrel = !twoStory && rng.NextDouble() < 0.2; // 시멘트 기와 지붕 (나머지는 칼라강판 골지붕)
            var p = new HouseKit.Parts
            {
                WallMat = Plaster(wallCol, w, wallH),
                TrimMat = Mats.Weathered("concrete", new Color(0.6f, 0.6f, 0.58f), 2f, 0.1f, 0f, 0.4f, 0.3f, null, 0.7f, 0f, "wallconcrete"),
                FrameMat = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.6f, 0.55f),   // 알루미늄 새시
                GlassMat = Mats.Lit(new Color(0.16f, 0.22f, 0.28f), 0.92f),
                Glass2Mat = Mats.Lit(new Color(0.34f, 0.3f, 0.26f), 0.85f),      // 커튼 친 유리
                DoorMat = Mats.Lit(DoorColors[rng.Next(DoorColors.Length)], 0.35f, 0.3f),
                RoofMat = barrel ? Mats.Tile(roofCol, 0.3f) : Mats.Weathered("rust", roofCol, 1.6f, 0.4f, 0f, 0.4f, 0.04f, null, 0.75f, 0.25f, null, 0.35f),
                UnderMat = Plaster(wallCol * 0.88f, w, 1f),
                MetalMat = Mats.Lit(new Color(0.2f, 0.21f, 0.22f), 0.4f, 0.4f),
                AccentMat = Mats.Weathered("concrete", new Color(0.3f, 0.55f, 0.38f), 3f, 0.55f, 0f, 0f, 0.05f, null, 0.9f, 0f, null, 0.4f), // 옥상 초록 방수칠
                PipeMat = Mats.Lit(new Color(0.76f, 0.76f, 0.73f), 0.4f),
            };
            const float t = 0.22f;
            var shade = HouseKit.WallShade(0f, wallH);

            // 기단: 비탈(다랭이 마을)에 서도 밑이 뜨지 않게 1.5m 아래부터 0.35m까지, 벽보다 3cm 내밀어 걸레받이 띠
            var gray = new Color(0.85f, 0.85f, 0.85f);
            p.Trim.Block(new Vector3(0f, -0.575f, 0f), new Vector3(w + 0.06f, 1.85f, d + 0.06f), 0.02f, Quaternion.identity, gray, gray * 0.6f);
            if (twoStory)
                p.Trim.Block(new Vector3(0f, floorH, 0f), new Vector3(w + 0.08f, 0.16f, d + 0.08f), 0.015f, Quaternion.identity, gray, gray * 0.8f);

            // 앞벽: 층마다 창, 1층 가운데 문 (간판 집은 1층이 가게 셔터)
            int windows = Mathf.Max(1, Mathf.RoundToInt(w / 3.2f));
            var front = new List<HouseKit.Opening>();
            for (int f = 0; f < (twoStory ? 2 : 1); f++)
            for (int i = 0; i < windows; i++)
            {
                float x = -w * 0.5f + (i + 0.5f) * w / windows;
                if (f == 0 && i == windows / 2) front.Add(new HouseKit.Opening(x, 0.35f, 1.1f, 2.15f, HouseKit.Hole.Door));
                else if (f == 0 && sign && windows > 1) front.Add(new HouseKit.Opening(x, 0.35f, Mathf.Min(2.4f, w / windows - 0.6f), 2.2f, HouseKit.Hole.Shop));
                else front.Add(new HouseKit.Opening(x, f * floorH + 1f, Mathf.Min(1.6f, w / windows - 0.7f), 1.2f));
            }
            var back = new List<HouseKit.Opening>();
            for (int f = 0; f < (twoStory ? 2 : 1); f++)
                back.Add(new HouseKit.Opening(Rd(-w * 0.25f, w * 0.25f), f * floorH + 1.3f, 1.0f, 0.9f));
            var side = new List<HouseKit.Opening>();
            if (rng.NextDouble() < 0.6) side.Add(new HouseKit.Opening(Rd(-d * 0.15f, d * 0.15f), 1.3f, 0.8f, 0.8f));
            HouseKit.Wall(p.Wall, new Vector3(0f, 0f, d * 0.5f), Quaternion.identity, w, -0.3f, wallH, t, front, shade);
            HouseKit.Wall(p.Wall, new Vector3(0f, 0f, -d * 0.5f), Quaternion.Euler(0f, 180f, 0f), w, -0.3f, wallH, t, back, shade);
            HouseKit.Wall(p.Wall, new Vector3(w * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), d - t * 2f, -0.3f, wallH, t, side, shade);
            HouseKit.Wall(p.Wall, new Vector3(-w * 0.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), d - t * 2f, -0.3f, wallH, t, null, shade);
            foreach (var o in front) HouseKit.Fill(p, new Vector3(0f, 0f, d * 0.5f), Quaternion.identity, o, t, rng);
            foreach (var o in back) HouseKit.Fill(p, new Vector3(0f, 0f, -d * 0.5f), Quaternion.Euler(0f, 180f, 0f), o, t, rng);
            foreach (var o in side) HouseKit.Fill(p, new Vector3(w * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), o, t, rng);
            // 방범창 (앞 창 몇 개): 창턱 위 세로 쇠창살
            foreach (var o in front)
                if (o.Kind == HouseKit.Hole.Window && rng.NextDouble() < 0.3)
                    for (float x = o.X - o.W * 0.5f + 0.15f; x < o.X + o.W * 0.5f - 0.1f; x += 0.18f)
                        p.Metal.Block(new Vector3(x, o.Y + o.H * 0.5f, d * 0.5f + 0.04f), new Vector3(0.022f, o.H + 0.1f, 0.022f), 0f, Quaternion.identity, Color.white, Color.white);
            // 문 위 차양 (칠한 철판, 비스듬히)
            foreach (var o in front)
                if (o.Kind == HouseKit.Hole.Door)
                    p.Roof.Block(new Vector3(o.X, o.Y + o.H + 0.3f, d * 0.5f + 0.45f), new Vector3(1.8f, 0.04f, 0.9f), 0.01f, Quaternion.Euler(12f, 0f, 0f), Color.white, Color.white);

            // 옆벽 살림: 에어컨 실외기(땅), 빗물 홈통, 전기 계량기, LPG 가스통
            float sideSign = rng.NextDouble() < 0.5 ? -1f : 1f;
            if (KenneyProps.Spawn("Props/house_ac", h, new Vector3(sideSign * (w * 0.5f + 0.2f), 0f, Rd(-d * 0.3f, d * 0.3f)), sideSign * 90f, 0f, 0f, false) == null)
                Mats.Prim(PrimitiveType.Cube, "AcUnit", h, new Vector3(sideSign * (w * 0.5f + 0.22f), 0.45f, 0f), new Vector3(0.4f, 0.62f, 0.85f), Mats.Lit(new Color(0.88f, 0.88f, 0.86f), 0.4f), false);
            p.Metal.Block(new Vector3(-sideSign * (w * 0.5f + 0.05f), 1.5f, 0f), new Vector3(0.08f, 0.4f, 0.3f), 0.01f, Quaternion.identity, Color.white, Color.white);
            // 모양 다양하게: 단층 셋 중 하나는 뒤로 꺾인 날개(ㄱ자), 넷 중 하나는 옆에 외쪽지붕 창고 (창고 쪽엔 가스통 대신)
            bool wing = !twoStory && rng.NextDouble() < 0.35;
            bool shed = rng.NextDouble() < 0.28;
            if (wing) KoreanWing(h, p, w, d, wallH, t, shade, rng, barrel);
            if (shed) KoreanShed(h, p, w, d, -sideSign, rng);
            else if (rng.NextDouble() < 0.6)
                KenneyProps.Spawn("Props/house_lpg", h, new Vector3(-sideSign * (w * 0.5f + 0.3f), 0f, -d * 0.2f), -sideSign * 90f, 0f, 0f, false);
            // 마당 살림: 현관 옆 화분, 스티로폼 상자 텃밭, 고무 대야
            float doorX = 0f;
            foreach (var o in front) if (o.Kind == HouseKit.Hole.Door) doorX = o.X;
            if (rng.NextDouble() < 0.55) KenneyProps.Spawn("Props/house_pots", h, new Vector3(doorX + 1.05f, 0f, d * 0.5f + 0.45f), Rd(0f, 360f), 0f, 0f, false);
            if (rng.NextDouble() < 0.35) KenneyProps.Spawn("Props/house_garden", h, new Vector3(Rd(-w * 0.45f, -w * 0.2f), 0f, d * 0.5f + 1.1f), Rd(-8f, 8f), 0f, 0f, false);
            if (rng.NextDouble() < 0.3) KenneyProps.Spawn("Props/house_basin", h, new Vector3(sideSign * (w * 0.5f + 0.6f), 0f, d * 0.35f), Rd(0f, 360f), 0f, 0f, false);
            if (rng.NextDouble() < 0.5)
                KenneyProps.Spawn("Props/house_dish", h, new Vector3(w * 0.5f - 0.5f, wallH - 0.75f, d * 0.5f + 0.12f), Rd(-20f, 20f), 0f, 0f, false);

            if (twoStory)
            {
                // 평지붕: 슬래브 + 초록 방수칠 + 난간 턱, 옥상 물탱크(다리 받침), 가끔 옥탑
                HouseKit.FlatRoof(p, wallH, w, d, 0.5f, rng.NextDouble() < 0.75, shade);
                bool tankRed = _rng.NextDouble() >= 0.5; // (예전 물탱크 색 난수 자리)
                var tp = new Vector3(R(-w * 0.25f, w * 0.25f), wallH + 0.2f, -d * 0.2f);
                if (KenneyProps.Spawn("Props/house_tank", h, tp + Vector3.up * 0.45f, Rd(0f, 360f), 0f, 0f, false) is GameObject tank)
                {
                    for (int k = 0; k < 4; k++)
                        p.Metal.Block(tp + new Vector3((k % 2 - 0.5f) * 0.9f, 0.22f, (k / 2 - 0.5f) * 0.9f), new Vector3(0.06f, 0.45f, 0.06f), 0f, Quaternion.identity, Color.white, Color.white);
                    p.Metal.Block(tp + Vector3.up * 0.44f, new Vector3(1.2f, 0.04f, 1.2f), 0f, Quaternion.identity, Color.white, Color.white);
                    if (tankRed) Tint(tank, new Color(0.8f, 0.25f, 0.2f));
                }
                if (rng.NextDouble() < 0.4)
                {
                    // 옥탑 (계단실): 작은 상자 + 문 + 슬래브
                    float ow = 2.6f, od = 2.4f, oh = 2.3f, top = wallH + 0.2f;
                    var ox = new Vector3(w * 0.5f - ow * 0.5f - 0.4f, top, -d * 0.5f + od * 0.5f + 0.4f);
                    var oshade = HouseKit.WallShade(top, top + oh);
                    var door = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0.1f, 0.9f, 1.95f, HouseKit.Hole.Door) };
                    HouseKit.Wall(p.Wall, ox + new Vector3(0f, 0f, od * 0.5f), Quaternion.identity, ow, 0f, oh, 0.18f, null, oshade);
                    HouseKit.Wall(p.Wall, ox + new Vector3(0f, 0f, -od * 0.5f), Quaternion.Euler(0f, 180f, 0f), ow, 0f, oh, 0.18f, null, oshade);
                    HouseKit.Wall(p.Wall, ox + new Vector3(-ow * 0.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), od - 0.36f, 0f, oh, 0.18f, door, oshade);
                    HouseKit.Wall(p.Wall, ox + new Vector3(ow * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), od - 0.36f, 0f, oh, 0.18f, null, oshade);
                    HouseKit.Fill(p, ox + new Vector3(-ow * 0.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), door[0], 0.18f, rng);
                    p.Trim.Block(ox + Vector3.up * (oh + 0.08f), new Vector3(ow + 0.3f, 0.16f, od + 0.3f), 0.02f, Quaternion.identity, Color.white, Color.white * 0.75f);
                }
            }
            else
            {
                // 박공지붕 (용마루가 앞뒤 방향 → 처마가 길·바다 쪽)
                float rh = R(1.6f, 2.3f) * 0.8f;
                HouseKit.GableRoof(p, wallH, w, d, rh, 0.4f, 0.5f, barrel ? HouseKit.Skin.Barrel : HouseKit.Skin.Corrugated, p.Wall, t, 0.1f, true, shade);
                // 홈통이 앞 모서리로 내려온다
                float gx = w * 0.5f + 0.25f, gz = d * 0.5f + 0.58f, gy = wallH + 0.1f - rh / (d * 0.5f) * 0.5f - 0.2f;
                p.Pipes.Add(new[] { new Vector3(gx, gy, gz), new Vector3(gx, gy - 0.25f, d * 0.5f + 0.08f), new Vector3(w * 0.5f - 0.12f, gy - 0.4f, d * 0.5f + 0.08f), new Vector3(w * 0.5f - 0.12f, 0.15f, d * 0.5f + 0.08f) });
            }
            p.Emit(h);
            KoreanHouseLife(h, w, d, wallH, twoStory, rng);
            if (sign)
            {
                string text = ShopSigns[_rng.Next(ShopSigns.Length)];
                var board = Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.3f);
                float sy = twoStory ? floorH + 0.1f : floorH - 0.35f;
                Mats.Prim(PrimitiveType.Cube, "SignBoard", h, new Vector3(0f, sy, d * 0.5f + 0.12f), new Vector3(Mathf.Min(w - 1f, 4.5f), 0.7f, 0.12f), board, false);
                Mats.Prim(PrimitiveType.Cube, "SignFrame", h, new Vector3(0f, sy, d * 0.5f + 0.1f), new Vector3(Mathf.Min(w - 1f, 4.5f) + 0.12f, 0.82f, 0.1f), Mats.Lit(roofCol * 0.8f, 0.4f), false);
                var tx = UIFactory.WorldText(h, text, new Vector3(0f, sy, d * 0.5f + 0.2f), new Color(0.75f, 0.12f, 0.1f), 5f, "Sign");
                UIFactory.OneSided(tx);
                tx.transform.localScale = Vector3.one * 0.85f;
                tx.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // 앞(+Z, 바다 쪽)에서 읽힌다
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, wallH * 0.5f, 0f);
            col.size = new Vector3(w, wallH + 1f, d);
            return h;
        }

        /// <summary>ㄱ자 날개: 본채 뒤 한쪽으로 꺾여 붙은 작은 칸 (용마루가 본채와 직각), 옆·뒤에 창 하나씩.</summary>
        static void KoreanWing(Transform h, HouseKit.Parts main, float w, float d, float wallH, float t, System.Func<Vector3, Color> shade, System.Random rng, bool barrel)
        {
            float Rd(float a0, float a1) => (float)(a0 + rng.NextDouble() * (a1 - a0));
            float w2 = Rd(3.5f, Mathf.Min(5f, w * 0.55f)), d2 = Rd(3.2f, 4.5f), sx = rng.NextDouble() < 0.5 ? -1f : 1f, top = wallH - 0.25f;
            var rig = HouseKit.Rig(h, new Vector3(sx * (w * 0.5f - w2 * 0.5f), 0f, -d * 0.5f - d2 * 0.5f + t), 0f, "Wing");
            HouseKit.Parts Same() => new HouseKit.Parts
            {
                WallMat = main.WallMat, TrimMat = main.TrimMat, FrameMat = main.FrameMat, GlassMat = main.GlassMat, Glass2Mat = main.Glass2Mat, DoorMat = main.DoorMat,
                RoofMat = main.RoofMat, UnderMat = main.UnderMat, MetalMat = main.MetalMat, PipeMat = main.PipeMat,
            };
            var wp = Same();
            var win = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 1.2f, 0.9f, 0.9f) };
            HouseKit.Box(wp, w2, d2, -0.3f, top, t, null, win, sx > 0f ? win : null, sx < 0f ? win : null, shade, rng);
            var gray = new Color(0.85f, 0.85f, 0.85f);
            wp.Trim.Block(new Vector3(0f, -0.575f, 0f), new Vector3(w2 + 0.06f, 1.85f, d2 + 0.06f), 0.02f, Quaternion.identity, gray, gray * 0.6f);
            wp.Emit(rig);
            var rr = HouseKit.Rig(rig, Vector3.zero, 90f);
            var rp = Same();
            HouseKit.GableRoof(rp, top, d2, w2, Rd(1.1f, 1.5f), 0.35f, 0.4f, barrel ? HouseKit.Skin.Barrel : HouseKit.Skin.Corrugated, rp.Wall, t, 0.1f, true, shade);
            rp.Emit(rr);
            var c = new GameObject("WingCol");
            c.transform.SetParent(rig, false);
            var bc = c.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, top * 0.5f, 0f);
            bc.size = new Vector3(w2, top + 1f, d2);
        }

        /// <summary>외쪽지붕 창고: 옆벽에 덧댄 시멘트 칸, 바깥쪽으로 낮아지는 골강판 한 면, 철문.</summary>
        static void KoreanShed(Transform h, HouseKit.Parts main, float w, float d, float side, System.Random rng)
        {
            float sd = 2.3f, sw = Mathf.Min(d * 0.65f, 4.2f);
            var rig = HouseKit.Rig(h, new Vector3(side * (w * 0.5f + sd * 0.5f), 0f, -d * 0.12f), side > 0f ? -90f : 90f, "Shed"); // 리그 +z = 본채 쪽
            var sp = new HouseKit.Parts
            {
                WallMat = main.TrimMat, TrimMat = main.TrimMat, FrameMat = main.MetalMat, GlassMat = main.GlassMat, DoorMat = Mats.Lit(new Color(0.32f, 0.36f, 0.38f), 0.4f, 0.5f),
                RoofMat = main.RoofMat, UnderMat = main.TrimMat, MetalMat = main.MetalMat, PipeMat = main.PipeMat,
            };
            var door = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0.05f, 1.1f, 1.9f, HouseKit.Hole.Door) };
            HouseKit.Box(sp, sw, sd, -0.3f, 2.15f, 0.18f, null, door, null, null, HouseKit.WallShade(0f, 2.15f), rng, 0f, 0.08f);
            HouseKit.ShedRoof(sp, sw, sd, 2.55f, 2.15f, 0.3f, sp.Wall, 0.18f);
            sp.Emit(rig);
            var c = new GameObject("ShedCol");
            c.transform.SetParent(rig, false);
            var bc = c.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.2f, 0f);
            bc.size = new Vector3(sw, 2.4f, sd);
        }

        /// <summary>
        /// 길가 전봇대 줄 (사진: 시골 바닷가 길): 콘크리트 전봇대(Props/house_pole)를 놓고 이웃끼리 전선 다섯 가닥을 처지게 잇는다. 자리 = (발밑, 길 방향 yaw).
        /// </summary>
        public void PowerLine(IList<(Vector3 pos, float yaw)> poles)
        {
            if (poles.Count == 0 || Resources.Load<GameObject>("Props/house_pole") == null) return;
            var tops = new List<Vector3[]>();
            foreach (var (pos, yaw) in poles)
            {
                KenneyProps.Spawn("Props/house_pole", _root, pos, yaw + 90f, 0f, 0f, true);
                var r = Quaternion.Euler(0f, yaw + 90f, 0f);
                tops.Add(new[] { pos + r * new Vector3(-0.81f, 9.69f, 0f), pos + r * new Vector3(0f, 9.69f, 0f), pos + r * new Vector3(0.81f, 9.69f, 0f),
                    pos + r * new Vector3(-0.63f, 8.79f, 0f), pos + r * new Vector3(0.63f, 8.79f, 0f) });
            }
            var wires = new List<Vector3[]>();
            for (int i = 0; i + 1 < tops.Count; i++)
            {
                if ((tops[i][0] - tops[i + 1][0]).magnitude > 45f) continue; // 건너뛴 자리(명소·비탈)는 잇지 않는다
                for (int k = 0; k < 5; k++)
                {
                    var a = tops[i][k];
                    var b = tops[i + 1][k];
                    var pts = new Vector3[9];
                    for (int j = 0; j < 9; j++)
                    {
                        float u = j / 8f;
                        pts[j] = Vector3.Lerp(a, b, u) + Vector3.down * (4f * u * (1f - u) * 0.45f);
                    }
                    wires.Add(pts);
                }
            }
            if (wires.Count == 0) return;
            var go = new GameObject("PowerWires");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = MapKit.Tubes(wires, _ => 0.012f, 4);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.3f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static readonly Color[] DoorColors =
        {
            new Color(0.42f, 0.32f, 0.24f), new Color(0.22f, 0.4f, 0.3f), new Color(0.25f, 0.34f, 0.55f), new Color(0.7f, 0.68f, 0.62f),
        };

        /// <summary>Blender 소품의 모든 재질 색을 바꾼다 (물탱크 파랑 → 빨강).</summary>
        static void Tint(GameObject go, Color c)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Mats.Lit(c * (mats[i] != null && mats[i].color.grayscale < 0.3f ? 0.8f : 1f), 0.45f);
                r.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// 한국 바닷가 집의 살림살이 (사진: 남해·통영 어촌): 장독대(옹기 항아리 무리), 옥상 태양열 온수기·빨래 건조대,
        /// 처마 홈통, 가끔 시멘트 블록 담장 + 철 대문. 배치는 집마다 정해진 난수(rng)라 맵 소품 배치가 바뀌지 않는다.
        /// </summary>
        static void KoreanHouseLife(Transform h, float w, float d, float wallH, bool flatRoof, System.Random rng)
        {
            float Rd(float a0, float a1) => (float)(a0 + rng.NextDouble() * (a1 - a0));
            var metal = Mats.Lit(new Color(0.82f, 0.83f, 0.84f), 0.55f, 0.5f);
            var dark = Mats.Lit(new Color(0.2f, 0.21f, 0.22f), 0.4f);
            // 장독대: 옥상(평지붕) 또는 뒷마당 낮은 단 위에 크고 작은 옹기
            Vector3 jarBase = flatRoof && rng.NextDouble() < 0.6 ? new Vector3(Rd(-w * 0.3f, w * 0.3f), wallH + 0.2f, d * 0.2f)
                : new Vector3(Rd(-w * 0.3f, w * 0.3f), 0f, -d * 0.5f - 1.4f);
            if (jarBase.y < 0.1f)
                Mats.Prim(PrimitiveType.Cube, "JangdokDae", h, jarBase + new Vector3(0f, 0.15f, 0f), new Vector3(2.4f, 0.3f, 1.4f), Mats.Stone(new Color(0.55f, 0.53f, 0.5f)), false);
            int jars = rng.Next(4, 8);
            for (int k = 0; k < jars; k++)
            {
                float s = Rd(0.45f, 0.85f);
                var p = jarBase + new Vector3((k % 4 - 1.5f) * 0.55f + Rd(-0.08f, 0.08f), jarBase.y < 0.1f ? 0.3f : 0f, (k / 4 - 0.3f) * 0.6f);
                if (KenneyProps.Spawn("Props/house_onggi", h, p, Rd(0f, 360f), s, 0f, false) == null)
                    Mats.Prim(PrimitiveType.Sphere, "Onggi", h, p + Vector3.up * s * 0.5f, new Vector3(s * 0.85f, s, s * 0.85f), Mats.Lit(new Color(0.32f, 0.2f, 0.12f), 0.75f), false);
            }
            if (flatRoof)
            {
                // 옥상 태양열 온수기 (비스듬한 판 + 위 원통 물통) · 빨래 건조대
                if (rng.NextDouble() < 0.4)
                {
                    var p = new Vector3(Rd(-w * 0.25f, w * 0.25f), wallH + 0.9f, -d * 0.3f);
                    Mats.Prim(PrimitiveType.Cube, "SolarPanel", h, p, new Vector3(1.8f, 0.06f, 1.4f), Mats.Lit(new Color(0.12f, 0.16f, 0.25f), 0.85f), false, Quaternion.Euler(-35f, 0f, 0f));
                    Mats.Prim(PrimitiveType.Cylinder, "SolarTank", h, p + new Vector3(0f, 0.55f, -0.5f), new Vector3(0.4f, 0.9f, 0.4f), metal, false, Quaternion.Euler(0f, 0f, 90f));
                }
                if (rng.NextDouble() < 0.5)
                {
                    var p = new Vector3(Rd(-w * 0.3f, w * 0.3f), wallH + 0.2f, d * 0.05f);
                    foreach (float sx in new[] { -1f, 1f })
                        Mats.Prim(PrimitiveType.Cylinder, "LaundryPole", h, p + new Vector3(sx * 1.2f, 0.7f, 0f), new Vector3(0.04f, 0.7f, 0.04f), metal, false);
                    Mats.Prim(PrimitiveType.Cube, "LaundryLine", h, p + new Vector3(0f, 1.35f, 0f), new Vector3(2.4f, 0.015f, 0.015f), metal, false);
                    var cloth = new[] { new Color(0.9f, 0.9f, 0.88f), new Color(0.3f, 0.45f, 0.75f), new Color(0.85f, 0.4f, 0.45f) };
                    for (int k = 0; k < 4; k++)
                        Mats.Prim(PrimitiveType.Cube, "Laundry", h, p + new Vector3(-0.9f + k * 0.6f, 1.05f, 0f), new Vector3(0.45f, 0.55f, 0.02f), Mats.Lit(cloth[rng.Next(cloth.Length)], 0.3f), false);
                }
            }
            // 시멘트 블록 담장 + 철 대문 (앞마당을 두른다, 사진: 시골집 회색 블록 담) — 절반 넘는 집
            if (rng.NextDouble() < 0.55)
            {
                var fp = new HouseKit.Parts
                {
                    AccentMat = Mats.Weathered("wallconcrete", new Color(0.66f, 0.65f, 0.62f), 1.2f, 0.1f, 0f, 0.6f, 0.3f, null, 0.7f),
                    TrimMat = Mats.Weathered("concrete", new Color(0.7f, 0.7f, 0.68f), 2f, 0.1f, 0f, 0.4f, 0.3f, null, 0.7f, 0f, "wallconcrete"),
                    UnderMat = dark, WallMat = dark,
                };
                float fz = d * 0.5f + 2f, fw = w + 2f, fh = 1.2f + (float)rng.Next(0, 3) * 0.2f;
                foreach (float sx in new[] { -1f, 1f })
                {
                    HouseKit.BlockWall(fp.Accent, fp.Under, fp.Trim, new Vector3(sx * 0.95f, 0f, fz), new Vector3(sx * fw * 0.5f, 0f, fz), fh, rng);
                    HouseKit.BlockWall(fp.Accent, fp.Under, fp.Trim, new Vector3(sx * fw * 0.5f, 0f, fz), new Vector3(sx * fw * 0.5f, 0f, d * 0.5f), fh, rng);
                    var g = new Color(0.9f, 0.9f, 0.9f);
                    fp.Trim.Block(new Vector3(sx * 0.8f, (fh + 0.3f) * 0.5f, fz), new Vector3(0.32f, fh + 0.3f, 0.32f), 0.02f, Quaternion.identity, g, g * 0.7f);
                }
                fp.Emit(h);
                var gate = Mats.Lit(rng.NextDouble() < 0.5 ? new Color(0.2f, 0.42f, 0.32f) : new Color(0.25f, 0.35f, 0.6f), 0.4f, 0.4f);
                Mats.Prim(PrimitiveType.Cube, "Gate", h, new Vector3(0f, 0.75f, fz), new Vector3(1.3f, 1.4f, 0.05f), gate, false);
                for (int k = 0; k < 6; k++)
                    Mats.Prim(PrimitiveType.Cube, "GateBar", h, new Vector3(-0.55f + k * 0.22f, 0.75f, fz + 0.04f), new Vector3(0.03f, 1.35f, 0.03f), dark, false);
            }
        }

        /// <summary>칠한 미장 벽: 거친 회벽 텍스처(Fishing/Weathered)에 칠 색, 처마 밑부터 흘러내린 빗물 자국. 한 장 = 2.5m.</summary>
        static Material Plaster(Color c, float w, float hgt) =>
            Mats.Wall(c);

        /// <summary>
        /// 콘크리트 텍스처 위에 칠한 색이 그대로 나오게 하는 곱 색: 텍스처 평균(0.47, 0.43, 0.36)으로 나눈다.
        /// (그냥 곱하면 흰 벽도 평균 0.45배 회색으로 어두워졌다)
        /// </summary>
        public static Color PaintTint(Color c) => new Color(c.r / 0.47f, c.g / 0.43f, c.b / 0.36f) * 0.93f;

        public static Mesh GableMesh() => Gable();

        /// <summary>박공지붕 단위 메쉬: 바닥 x -0.5~0.5, z -0.5~0.5, 용마루 높이 1 (용마루가 x 방향). 면마다 법선이 따로.</summary>
        static Mesh Gable()
        {
            if (_gable != null) return _gable;
            var a = new Vector3(-0.5f, 0f, -0.5f); var b = new Vector3(0.5f, 0f, -0.5f);
            var c = new Vector3(0.5f, 0f, 0.5f); var e = new Vector3(-0.5f, 0f, 0.5f);
            var r0 = new Vector3(-0.5f, 1f, 0f); var r1 = new Vector3(0.5f, 1f, 0f);
            var v = new[] { a, b, r1, r0,   e, r0, r1, c,   a, r0, e,   b, c, r1,   a, e, c, b };
            var t = new[] { 0, 2, 1, 0, 3, 2,   4, 6, 5, 4, 7, 6,   8, 10, 9,   11, 13, 12,   14, 15, 16, 14, 16, 17 };
            _gable = new Mesh { name = "GableRoof", vertices = v, triangles = t };
            _gable.RecalculateNormals();
            _gable.RecalculateBounds();
            return _gable;
        }

        /// <summary>갯바위 덩어리 (절차적 바위 메쉬를 눕힌다, <see cref="Rocks"/>).</summary>
        public void Boulder(Vector3 at, float size, Material mat)
        {
            Rocks.Prim("Boulder", _root, at, new Vector3(size * R(1f, 1.5f), size * R(0.55f, 0.8f), size * R(0.9f, 1.2f)), mat,
                Quaternion.Euler(R(-15f, 15f), R(0f, 360f), R(-15f, 15f)));
        }
    }
}
