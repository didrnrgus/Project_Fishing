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

        /// <summary>테트라포드 한 개 (다리 끝까지 약 1.05m × scale). collider면 볼록 메쉬 콜라이더 (찌가 걸린다).</summary>
        public static void Tetrapod(Transform parent, Vector3 p, Quaternion rot, float scale, Material mat, bool collider)
        {
            var go = new GameObject("Tetrapod");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.transform.localRotation = rot;
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = TetrapodMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().convex = true;
        }

        /// <summary>
        /// 방파제 바깥 테트라포드 더미: from→to 선을 따라 바깥(outward) 쪽으로 rows줄, 줄마다 엇갈리고 바깥 줄일수록 낮게 잠긴다
        /// (실제 방파제처럼 벽에 기댄 안쪽 줄이 가장 높다).
        /// </summary>
        public void TetrapodBank(Vector3 from, Vector3 to, Vector3 outward, float waterY, Material mat, int rows = 3)
        {
            var along = to - from;
            float len = along.magnitude;
            if (len < 0.1f) return;
            along /= len;
            outward = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
            for (int k = 0; k < rows; k++)
                for (float s = (k % 2) * 1f; s < len; s += R(1.8f, 2.3f))
                {
                    var p = from + along * s + outward * (0.6f + k * 1.7f + R(-0.3f, 0.3f));
                    p.y = waterY + 0.35f - k * 0.55f + R(-0.25f, 0.25f);
                    Tetrapod(p, mat);
                }
        }

        static Mesh _tetrapod;

        /// <summary>
        /// 테트라포드 메쉬: 정사면체 방향 다리 4개, 가운데가 굵고(반지름 0.42) 끝이 가는(0.24) 원뿔대, 끝은 평평하게 자른 면.
        /// 다리마다 옆면 법선을 원뿔대 경사에 맞춰 부드럽게, 끝면은 따로.
        /// </summary>
        public static Mesh TetrapodMesh()
        {
            if (_tetrapod != null) return _tetrapod;
            const int seg = 12;
            const float len = 1.05f, r0 = 0.42f, r1 = 0.24f, back = 0.15f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (int leg = 0; leg < 4; leg++)
            {
                var d = leg == 0 ? Vector3.up : Quaternion.Euler(109.47f, leg * 120f, 0f) * Vector3.up;
                var q = Quaternion.FromToRotation(Vector3.up, d);
                var a = q * Vector3.right;
                var b = q * Vector3.forward;
                // 옆면
                int side = v.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float th = i / (float)seg * Mathf.PI * 2f;
                    var radial = Mathf.Cos(th) * a + Mathf.Sin(th) * b;
                    var normal = (radial * (len + back) + d * (r0 - r1)).normalized;
                    v.Add(-d * back + radial * r0); n.Add(normal); uv.Add(new Vector2(i / (float)seg * 1.2f, 0f));
                    v.Add(d * len + radial * r1); n.Add(normal); uv.Add(new Vector2(i / (float)seg * 1.2f, 0.6f));
                }
                for (int i = 0; i < seg; i++)
                {
                    int b0 = side + i * 2, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
                    tri.AddRange(new[] { b0, t0, b1, b1, t0, t1 });
                }
                // 끝면 (바깥) + 밑면 (가운데 안쪽, 다른 다리 틈으로 안이 보이지 않게)
                for (int cap = 0; cap < 2; cap++)
                {
                    var c = cap == 0 ? d * len : -d * back;
                    float r = cap == 0 ? r1 : r0;
                    var cn = cap == 0 ? d : -d;
                    int center = v.Count;
                    v.Add(c); n.Add(cn); uv.Add(new Vector2(0.5f, 0.5f));
                    for (int i = 0; i <= seg; i++)
                    {
                        float th = i / (float)seg * Mathf.PI * 2f;
                        v.Add(c + (Mathf.Cos(th) * a + Mathf.Sin(th) * b) * r); n.Add(cn);
                        uv.Add(new Vector2(0.5f + Mathf.Cos(th) * 0.25f, 0.5f + Mathf.Sin(th) * 0.25f));
                    }
                    for (int i = 0; i < seg; i++)
                    {
                        int p0 = center + 1 + i, p1 = p0 + 1;
                        if (cap == 0) tri.AddRange(new[] { center, p1, p0 });
                        else tri.AddRange(new[] { center, p0, p1 });
                    }
                }
            }
            _tetrapod = new Mesh { name = "Tetrapod" };
            _tetrapod.SetVertices(v);
            _tetrapod.SetNormals(n);
            _tetrapod.SetUVs(0, uv);
            _tetrapod.SetTriangles(tri, 0);
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

        /// <summary>팔작지붕 정자: 돌 기단, 붉은 기둥 8개, 단청 보, 처마 끝이 들린 기와지붕.</summary>
        public Transform Pavilion(Vector3 at, float yaw)
        {
            var p = new GameObject("Pavilion").transform;
            p.SetParent(_root, false);
            p.localPosition = at;
            p.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var stone = Mats.Textured("concrete", new Color(0.75f, 0.73f, 0.68f), 1f, 0.15f);
            var wood = Mats.Textured("deck", new Color(0.85f, 0.72f, 0.58f), 2f, 0.3f);
            var pillar = Mats.Lit(new Color(0.55f, 0.16f, 0.12f), 0.35f);
            var beam = Mats.Lit(new Color(0.18f, 0.48f, 0.42f), 0.3f);
            var beamRed = Mats.Lit(new Color(0.7f, 0.2f, 0.15f), 0.3f);
            Mats.Prim(PrimitiveType.Cube, "Base", p, new Vector3(0f, 0.25f, 0f), new Vector3(7f, 1.1f, 7f), stone);
            Mats.Prim(PrimitiveType.Cube, "Floor", p, new Vector3(0f, 0.95f, 0f), new Vector3(6f, 0.3f, 6f), wood);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.7f;
                if (i % 2 == 1) pos *= 1.41f * 0.98f; // 모서리
                pos = new Vector3(Mathf.Clamp(pos.x, -2.7f, 2.7f), 2.4f, Mathf.Clamp(pos.z, -2.7f, 2.7f));
                Mats.Prim(PrimitiveType.Cylinder, "Pillar", p, pos, new Vector3(0.32f, 1.35f, 0.32f), pillar);
            }
            // 보 (단청: 초록 + 붉은 띠)
            foreach (var (pos, size) in new[] { (new Vector3(0f, 3.75f, 2.7f), new Vector3(5.8f, 0.35f, 0.35f)), (new Vector3(0f, 3.75f, -2.7f), new Vector3(5.8f, 0.35f, 0.35f)),
                         (new Vector3(2.7f, 3.75f, 0f), new Vector3(0.35f, 0.35f, 5.8f)), (new Vector3(-2.7f, 3.75f, 0f), new Vector3(0.35f, 0.35f, 5.8f)) })
            {
                Mats.Prim(PrimitiveType.Cube, "Beam", p, pos, size, beam, false);
                Mats.Prim(PrimitiveType.Cube, "BeamBand", p, pos + Vector3.up * 0.22f, Vector3.Scale(size, new Vector3(1f, 0.25f, 1f)) + new Vector3(0.02f, 0f, 0.02f), beamRed, false);
            }
            // 난간
            var rail = Mats.Lit(new Color(0.5f, 0.2f, 0.15f), 0.3f);
            foreach (var (pos, size) in new[] { (new Vector3(0f, 1.6f, 2.85f), new Vector3(5.6f, 0.1f, 0.1f)), (new Vector3(0f, 1.6f, -2.85f), new Vector3(5.6f, 0.1f, 0.1f)),
                         (new Vector3(2.85f, 1.6f, 0f), new Vector3(0.1f, 0.1f, 5.6f)), (new Vector3(-2.85f, 1.6f, 0f), new Vector3(0.1f, 0.1f, 5.6f)) })
                Mats.Prim(PrimitiveType.Cube, "Rail", p, pos, size, rail, false);

            var roof = new GameObject("Roof");
            roof.transform.SetParent(p, false);
            roof.transform.localPosition = new Vector3(0f, 3.95f, 0f);
            roof.AddComponent<MeshFilter>().sharedMesh = CurvedRoof(4.6f, 2.3f, 0.75f);
            var mr = roof.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { Mats.Lit(new Color(0.22f, 0.24f, 0.28f), 0.35f), Mats.Lit(new Color(0.55f, 0.22f, 0.16f), 0.3f) };
            Mats.Prim(PrimitiveType.Sphere, "Finial", p, new Vector3(0f, 6.3f, 0f), new Vector3(0.5f, 0.6f, 0.5f), Mats.Lit(new Color(0.25f, 0.27f, 0.3f), 0.3f), false);
            return p;
        }

        /// <summary>
        /// 기와지붕 메쉬: 정사각형 바닥(반폭 hw)에서 꼭대기까지 오르고, 처마 쪽은 오목하게, 네 귀퉁이 끝은 위로 들린다.
        /// 서브메쉬 0 = 윗면(기와), 1 = 아랫면(서까래, 붉은색).
        /// </summary>
        public static Mesh CurvedRoof(float hw, float rise, float cornerLift)
        {
            const int n = 24;
            var verts = new List<Vector3>();
            var up = new List<int>();
            var down = new List<int>();
            for (int iz = 0; iz <= n; iz++)
            for (int ix = 0; ix <= n; ix++)
            {
                float u = ix / (float)n * 2f - 1f, v = iz / (float)n * 2f - 1f;
                float m = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                float y = rise * (1f - Mathf.Pow(m, 0.75f));               // 오목한 경사 (가운데가 높다)
                y += cornerLift * Mathf.Pow(Mathf.Abs(u * v), 3f);           // 귀퉁이 들림
                verts.Add(new Vector3(u * hw, y, v * hw));
            }
            int count = verts.Count;
            for (int i = 0; i < count; i++) verts.Add(verts[i] - new Vector3(0f, 0.12f, 0f));
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                int a = iz * (n + 1) + ix, b = a + 1, c = a + n + 1, d = c + 1;
                up.AddRange(new[] { a, c, b, b, c, d });
                down.AddRange(new[] { a + count, b + count, c + count, b + count, d + count, c + count });
            }
            var mesh = new Mesh { name = "PavilionRoof" };
            mesh.SetVertices(verts);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(up, 0);
            mesh.SetTriangles(down, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>낚시점 건물 (줄무늬 차양·카운터·불빛) + 주인 NPC(NpcId "shop").</summary>
        public NpcAngler TackleShop(Vector3 p, float yaw, string sign, CharacterLook owner, Color roof)
        {
            var shop = new GameObject("TackleShop").transform;
            shop.SetParent(_root, false);
            shop.localPosition = p;
            shop.localRotation = Quaternion.Euler(0f, yaw, 0f);
            // 몸통: 칠한 시멘트 벽(얼룩 셰이더) + 짙은 기단, 지붕 슬래브 + 처마 테두리
            Mats.Prim(PrimitiveType.Cube, "Body", shop, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 4.5f), Plaster(new Color(0.93f, 0.9f, 0.84f), 7f, 3f));
            Mats.Prim(PrimitiveType.Cube, "Plinth", shop, new Vector3(0f, 0.2f, 0f), new Vector3(7.1f, 0.4f, 4.6f), Mats.Textured("concrete", new Color(0.55f, 0.55f, 0.53f), new Vector2(4f, 1f), 0.1f), false);
            Mats.Prim(PrimitiveType.Cube, "Roof", shop, new Vector3(0f, 3.2f, 0f), new Vector3(7.6f, 0.4f, 5.2f), Mats.Lit(roof));
            Mats.Prim(PrimitiveType.Cube, "Fascia", shop, new Vector3(0f, 3.05f, 2.62f), new Vector3(7.7f, 0.18f, 0.06f), Mats.Lit(roof * 0.7f), false);
            // 옆 창: 창틀·유리·창턱, 유리에 낚시 용품 포스터
            var alu = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.6f, 0.6f);
            var glass = Mats.Lit(new Color(0.2f, 0.28f, 0.34f), 0.92f);
            foreach (float sx in new[] { -1f, 1f })
            {
                Window(shop, new Vector3(sx * 3.52f, 1.7f, 0.2f), 90f * sx, new Vector2(1.6f, 1.1f), alu, glass);
                Mats.Prim(PrimitiveType.Cube, "Poster", shop, new Vector3(sx * 3.56f, 1.7f, 0.55f), new Vector3(0.01f, 0.5f, 0.36f),
                    Mats.Lit(sx < 0 ? new Color(0.9f, 0.75f, 0.2f) : new Color(0.25f, 0.55f, 0.85f), 0.4f), false);
            }
            // 가게 앞: 낚싯대 거치대(여러 색 낚싯대가 비스듬히), 아이스박스 더미, 미끼 냉장고, 플라스틱 의자
            var rack = Mats.Lit(new Color(0.3f, 0.3f, 0.32f), 0.4f, 0.4f);
            Mats.Prim(PrimitiveType.Cube, "RodRack", shop, new Vector3(-3.1f, 0.35f, 3.1f), new Vector3(0.9f, 0.1f, 0.35f), rack, false);
            Color[] rodCols = { new Color(0.8f, 0.15f, 0.15f), new Color(0.1f, 0.15f, 0.35f), new Color(0.85f, 0.7f, 0.3f), new Color(0.15f, 0.15f, 0.15f), new Color(0.2f, 0.5f, 0.3f) };
            for (int i = 0; i < 5; i++)
                Mats.Prim(PrimitiveType.Cylinder, "RackRod", shop, new Vector3(-3.45f + i * 0.17f, 1.45f, 3.0f), new Vector3(0.025f, 1.25f, 0.025f), Mats.Lit(rodCols[i], 0.7f), false,
                    Quaternion.Euler(-12f, 0f, (i - 2) * 2.5f));
            var coolerW = Mats.Lit(new Color(0.93f, 0.94f, 0.92f), 0.4f);
            var coolerB = Mats.Lit(new Color(0.16f, 0.42f, 0.76f), 0.45f);
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector3(3.05f, 0.24f + i * 0.46f, 3.05f + (i == 2 ? 0.05f : 0f));
                Mats.Prim(PrimitiveType.Cube, "Cooler", shop, c, new Vector3(0.62f, 0.4f, 0.42f), coolerW, false);
                Mats.Prim(PrimitiveType.Cube, "CoolerLid", shop, c + Vector3.up * 0.22f, new Vector3(0.64f, 0.06f, 0.44f), coolerB, false);
            }
            Mats.Prim(PrimitiveType.Cube, "BaitFridge", shop, new Vector3(2.6f, 0.75f, 2.05f), new Vector3(0.9f, 1.5f, 0.6f), coolerW, false);
            Mats.Prim(PrimitiveType.Cube, "FridgeGlass", shop, new Vector3(2.6f, 0.85f, 2.36f), new Vector3(0.76f, 1.1f, 0.02f), glass, false);
            Mats.Prim(PrimitiveType.Cube, "FridgeLabel", shop, new Vector3(2.6f, 1.45f, 2.36f), new Vector3(0.7f, 0.16f, 0.02f), Mats.Lit(new Color(0.85f, 0.2f, 0.15f), 0.4f), false);
            var stool = Mats.Lit(new Color(0.85f, 0.25f, 0.2f), 0.3f);
            foreach (float sx in new[] { -1.9f, 1.9f })
            {
                Mats.Prim(PrimitiveType.Cylinder, "Stool", shop, new Vector3(sx, 0.22f, 4.4f), new Vector3(0.36f, 0.22f, 0.36f), stool, false);
                Mats.Prim(PrimitiveType.Cylinder, "StoolTop", shop, new Vector3(sx, 0.45f, 4.4f), new Vector3(0.4f, 0.02f, 0.4f), stool, false);
            }
            for (int i = 0; i < 6; i++)
            {
                var c = i % 2 == 0 ? new Color(0.85f, 0.25f, 0.2f) : new Color(0.97f, 0.97f, 0.95f);
                Mats.Prim(PrimitiveType.Cube, "Awning", shop, new Vector3(-3f + i * 1.2f, 2.6f, 2.75f), new Vector3(1.2f, 0.12f, 1.4f),
                    Mats.Lit(c), false, Quaternion.Euler(-18f, 0f, 0f));
            }
            Mats.Prim(PrimitiveType.Cube, "Counter", shop, new Vector3(0f, 0.55f, 3.5f), new Vector3(5f, 1.1f, 0.6f), Mats.Lit(new Color(0.55f, 0.4f, 0.26f)));
            var light = new GameObject("ShopLight").AddComponent<Light>();
            light.transform.SetParent(shop, false);
            light.transform.localPosition = new Vector3(0f, 2.4f, 3.4f);
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.shadows = LightShadows.None;
            var npc = NpcAngler.Spawn(shop, new Vector3(1.2f, 0f, 2.75f), 0f, owner, NpcAngler.Role.Talker);
            npc.NpcId = "shop";
            WorldBuilder.AddSign(shop, sign, new Vector3(0f, 3.9f, 2.3f), new Color(0.6f, 0.85f, 1f));
            return npc;
        }

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
        /// 바닷가 마을 집: 단층 박공지붕 집(파랑·빨강·초록 지붕) 또는 2층 상가(평지붕, 옥상 물탱크, 1층 간판).
        /// 앞(+Z)이 길·바다 쪽. 몸 콜라이더를 붙인다. sign이면 1층 위에 간판 (바다 쪽에서 읽힌다).
        /// </summary>
        public Transform KoreanHouse(Vector3 at, float yaw, bool sign)
        {
            var h = new GameObject("KoreanHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wallCol = WallColors[_rng.Next(WallColors.Length)];
            var roofCol = RoofColors[_rng.Next(RoofColors.Length)];
            var roof = Mats.Lit(roofCol, 0.35f);
            var glass = Mats.Lit(new Color(0.18f, 0.25f, 0.31f), 0.92f);
            var frame = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.6f, 0.55f);   // 알루미늄 새시
            var door = Mats.Lit(new Color(0.42f, 0.32f, 0.24f), 0.3f);
            bool twoStory = _rng.NextDouble() < 0.35;
            float w = R(7f, 11f), d = R(5.5f, 8f), floorH = 2.9f;
            float wallH = twoStory ? floorH * 2f : floorH;
            // 디테일 배치는 따로 난수 (맵의 다른 소품 배치가 바뀌지 않게)
            var rng = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            float Rd(float a0, float a1) => (float)(a0 + rng.NextDouble() * (a1 - a0));

            // 벽: 칠한 미장(얼룩 셰이더) + 짙은 기단 띠 + 층 사이 띠
            Mats.Prim(PrimitiveType.Cube, "Wall", h, new Vector3(0f, wallH * 0.5f - 0.2f, 0f), new Vector3(w, wallH + 0.4f, d), Plaster(wallCol, w, wallH), false);
            var plinth = Mats.Textured("concrete", new Color(0.52f, 0.52f, 0.5f), new Vector2(Mathf.Round(w / 2f), 1f), 0.1f);
            Mats.Prim(PrimitiveType.Cube, "Plinth", h, new Vector3(0f, 0.15f, 0f), new Vector3(w + 0.1f, 0.5f, d + 0.1f), plinth, false);
            if (twoStory)
                Mats.Prim(PrimitiveType.Cube, "FloorBand", h, new Vector3(0f, floorH, 0f), new Vector3(w + 0.08f, 0.16f, d + 0.08f), Mats.Lit(wallCol * 0.82f, 0.2f), false);

            // 창문·문 (앞면): 새시 + 유리 + 가운데 창살 + 창턱, 문 위 차양과 계단
            int windows = Mathf.Max(1, Mathf.RoundToInt(w / 3.2f));
            float fz = d * 0.5f;
            for (int f = 0; f < (twoStory ? 2 : 1); f++)
            for (int i = 0; i < windows; i++)
            {
                float x = -w * 0.5f + (i + 0.5f) * w / windows;
                bool isDoor = f == 0 && i == windows / 2;
                if (isDoor)
                {
                    Mats.Prim(PrimitiveType.Cube, "DoorFrame", h, new Vector3(x, 1.05f, fz + 0.02f), new Vector3(1.26f, 2.26f, 0.04f), frame, false);
                    Mats.Prim(PrimitiveType.Cube, "Door", h, new Vector3(x, 1.02f, fz + 0.05f), new Vector3(1.1f, 2.1f, 0.05f), door, false);
                    Mats.Prim(PrimitiveType.Cube, "DoorGlass", h, new Vector3(x, 1.5f, fz + 0.08f), new Vector3(0.8f, 0.7f, 0.02f), glass, false);
                    Mats.Prim(PrimitiveType.Sphere, "Knob", h, new Vector3(x + 0.4f, 1.0f, fz + 0.1f), Vector3.one * 0.07f, frame, false);
                    Mats.Prim(PrimitiveType.Cube, "Canopy", h, new Vector3(x, 2.45f, fz + 0.45f), new Vector3(1.8f, 0.06f, 0.9f), Mats.Lit(roofCol * 0.9f, 0.4f), false,
                        Quaternion.Euler(12f, 0f, 0f));
                    Mats.Prim(PrimitiveType.Cube, "Step", h, new Vector3(x, 0.08f, fz + 0.35f), new Vector3(1.6f, 0.16f, 0.6f), plinth, false);
                }
                else
                {
                    float y = f * floorH + 1.55f;
                    var c = new Vector3(x, y, fz);
                    Mats.Prim(PrimitiveType.Cube, "Frame", h, c + new Vector3(0f, 0f, 0.02f), new Vector3(1.56f, 1.26f, 0.05f), frame, false);
                    Mats.Prim(PrimitiveType.Cube, "Window", h, c + new Vector3(0f, 0f, 0.045f), new Vector3(1.4f, 1.1f, 0.03f), glass, false);
                    Mats.Prim(PrimitiveType.Cube, "Mullion", h, c + new Vector3(0f, 0f, 0.07f), new Vector3(0.05f, 1.12f, 0.03f), frame, false);
                    Mats.Prim(PrimitiveType.Cube, "Sill", h, c + new Vector3(0f, -0.66f, 0.09f), new Vector3(1.7f, 0.07f, 0.18f), plinth, false);
                    if (rng.NextDouble() < 0.3) // 방범창
                        for (int k = 0; k < 5; k++)
                            Mats.Prim(PrimitiveType.Cube, "Grille", h, c + new Vector3(-0.56f + k * 0.28f, 0f, 0.11f), new Vector3(0.025f, 1.15f, 0.025f), frame, false);
                }
            }

            // 옆벽: 에어컨 실외기, 빗물 홈통, 전기 계량기
            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
            var acPos = new Vector3(side * (w * 0.5f + 0.22f), 0.45f, Rd(-d * 0.3f, d * 0.3f));
            var ac = Mats.Lit(new Color(0.88f, 0.88f, 0.86f), 0.4f);
            Mats.Prim(PrimitiveType.Cube, "AcUnit", h, acPos, new Vector3(0.4f, 0.62f, 0.85f), ac, false);
            Mats.Prim(PrimitiveType.Cylinder, "AcFan", h, acPos + new Vector3(side * 0.205f, 0.02f, 0.08f), new Vector3(0.48f, 0.01f, 0.48f), Mats.Lit(new Color(0.25f, 0.26f, 0.28f), 0.3f), false,
                Quaternion.Euler(0f, 0f, 90f));
            var pipe = Mats.Lit(new Color(0.7f, 0.7f, 0.68f), 0.4f);
            Mats.Prim(PrimitiveType.Cylinder, "Downpipe", h, new Vector3(-side * (w * 0.5f + 0.06f), wallH * 0.5f, fz - 0.15f), new Vector3(0.09f, wallH * 0.5f, 0.09f), pipe, false);
            Mats.Prim(PrimitiveType.Cube, "Meter", h, new Vector3(-side * (w * 0.5f + 0.05f), 1.5f, 0f), new Vector3(0.08f, 0.4f, 0.3f), Mats.Lit(new Color(0.85f, 0.85f, 0.82f), 0.3f), false);

            if (twoStory)
            {
                // 평지붕 + 사방 난간 턱 + 다리 달린 옥상 물탱크
                Mats.Prim(PrimitiveType.Cube, "RoofSlab", h, new Vector3(0f, wallH + 0.1f, 0f), new Vector3(w + 0.3f, 0.2f, d + 0.3f), plinth, false);
                foreach (var (pos, size) in new[] { (new Vector3(0f, 0f, d * 0.5f), new Vector3(w + 0.3f, 0.5f, 0.2f)), (new Vector3(0f, 0f, -d * 0.5f), new Vector3(w + 0.3f, 0.5f, 0.2f)),
                             (new Vector3(w * 0.5f, 0f, 0f), new Vector3(0.2f, 0.5f, d + 0.3f)), (new Vector3(-w * 0.5f, 0f, 0f), new Vector3(0.2f, 0.5f, d + 0.3f)) })
                    Mats.Prim(PrimitiveType.Cube, "Parapet", h, pos + Vector3.up * (wallH + 0.45f), size, Plaster(wallCol, w, 0.5f), false);
                var tank = Mats.Lit(_rng.NextDouble() < 0.5 ? new Color(0.25f, 0.45f, 0.8f) : new Color(0.8f, 0.25f, 0.2f), 0.4f);
                var tp = new Vector3(R(-w * 0.25f, w * 0.25f), wallH + 1.25f, -d * 0.2f);
                Mats.Prim(PrimitiveType.Cylinder, "WaterTank", h, tp, new Vector3(1.4f, 0.65f, 1.4f), tank, false);
                Mats.Prim(PrimitiveType.Cylinder, "TankLid", h, tp + Vector3.up * 0.68f, new Vector3(0.5f, 0.05f, 0.5f), tank, false);
                for (int k = 0; k < 4; k++)
                    Mats.Prim(PrimitiveType.Cube, "TankLeg", h, tp + new Vector3((k % 2 - 0.5f) * 0.9f, -0.85f, (k / 2 - 0.5f) * 0.9f), new Vector3(0.08f, 0.4f, 0.08f), pipe, false);
            }
            else
            {
                // 박공지붕 (용마루가 앞뒤 방향 → 처마가 길·바다 쪽) + 처마 테두리 + 용마루
                float rh = R(1.6f, 2.3f);
                var go = new GameObject("Roof");
                go.transform.SetParent(h, false);
                go.transform.localPosition = new Vector3(0f, wallH, 0f);
                go.transform.localScale = new Vector3(w + 0.8f, rh, d + 0.9f);
                go.AddComponent<MeshFilter>().sharedMesh = Gable();
                go.AddComponent<MeshRenderer>().sharedMaterial = roof;
                var fascia = Mats.Lit(roofCol * 0.65f, 0.3f);
                foreach (float sz in new[] { -1f, 1f })
                    Mats.Prim(PrimitiveType.Cube, "Fascia", h, new Vector3(0f, wallH - 0.05f, sz * (d + 0.9f) * 0.5f), new Vector3(w + 0.85f, 0.16f, 0.08f), fascia, false);
                Mats.Prim(PrimitiveType.Cube, "Ridge", h, new Vector3(0f, wallH + rh + 0.03f, 0f), new Vector3(w + 0.85f, 0.12f, 0.24f), fascia, false);
            }
            if (sign)
            {
                string text = ShopSigns[_rng.Next(ShopSigns.Length)];
                var board = Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.3f);
                float sy = twoStory ? floorH + 0.1f : floorH - 0.35f;
                Mats.Prim(PrimitiveType.Cube, "SignBoard", h, new Vector3(0f, sy, d * 0.5f + 0.12f), new Vector3(Mathf.Min(w - 1f, 4.5f), 0.7f, 0.12f), board, false);
                Mats.Prim(PrimitiveType.Cube, "SignFrame", h, new Vector3(0f, sy, d * 0.5f + 0.1f), new Vector3(Mathf.Min(w - 1f, 4.5f) + 0.12f, 0.82f, 0.1f), Mats.Lit(roofCol * 0.8f, 0.4f), false);
                var t = UIFactory.WorldText(h, text, new Vector3(0f, sy, d * 0.5f + 0.2f), new Color(0.75f, 0.12f, 0.1f), 5f, "Sign");
                UIFactory.OneSided(t);
                t.transform.localScale = Vector3.one * 0.85f;
                t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // 앞(+Z, 바다 쪽)에서 읽힌다
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, wallH * 0.5f, 0f);
            col.size = new Vector3(w, wallH + 1f, d);
            return h;
        }

        /// <summary>칠한 미장 벽: 콘크리트 텍스처(반복 깨기 셰이더)에 칠 색을 곱한다. 벽 크기에 맞춰 한 장 = 3m.</summary>
        static Material Plaster(Color c, float w, float hgt) =>
            Mats.Textured("concrete", PaintTint(c), new Vector2(Mathf.Max(1f, Mathf.Round(w / 3f)), Mathf.Max(1f, Mathf.Round(hgt / 3f))), 0.12f);

        /// <summary>
        /// 콘크리트 텍스처 위에 칠한 색이 그대로 나오게 하는 곱 색: 텍스처 평균(0.47, 0.43, 0.36)으로 나눈다.
        /// (그냥 곱하면 흰 벽도 평균 0.45배 회색으로 어두워졌다)
        /// </summary>
        public static Color PaintTint(Color c) => new Color(c.r / 0.47f, c.g / 0.43f, c.b / 0.36f) * 0.93f;

        /// <summary>벽에 붙은 창 (yaw 방향을 바라본다): 새시 + 유리 + 창턱.</summary>
        static void Window(Transform parent, Vector3 at, float yaw, Vector2 size, Material frame, Material glass)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            Mats.Prim(PrimitiveType.Cube, "WinFrame", parent, at, new Vector3(size.x + 0.16f, size.y + 0.16f, 0.05f), frame, false, q);
            Mats.Prim(PrimitiveType.Cube, "WinGlass", parent, at + q * new Vector3(0f, 0f, 0.025f), new Vector3(size.x, size.y, 0.03f), glass, false, q);
            Mats.Prim(PrimitiveType.Cube, "WinSill", parent, at + q * new Vector3(0f, -size.y * 0.5f - 0.1f, 0.07f), new Vector3(size.x + 0.3f, 0.07f, 0.18f), frame, false, q);
        }

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
