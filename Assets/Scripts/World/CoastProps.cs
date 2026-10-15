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

        public void Tetrapod(Vector3 p, Material mat)
        {
            var t = new GameObject("Tetrapod").transform;
            t.SetParent(_root, false);
            t.localPosition = p;
            t.localRotation = Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f));
            for (int leg = 0; leg < 4; leg++)
            {
                var dir = leg == 0 ? Vector3.up : Quaternion.Euler(109.5f, leg * 120f, 0f) * Vector3.up;
                Mats.Prim(PrimitiveType.Capsule, "Leg", t, dir * 0.55f, new Vector3(0.6f, 0.6f, 0.6f), mat, false, Quaternion.FromToRotation(Vector3.up, dir));
            }
        }

        /// <summary>등대: 몸통 색, 밤 불빛 색 (항구 입구 오른쪽 빨강·왼쪽 흰색 — 실제 항로 표지 규칙). scale로 크기를 키운다.</summary>
        public Transform Lighthouse(Vector3 at, Color body, Color lightColor, float scale = 1f)
        {
            var lh = new GameObject("Lighthouse").transform;
            lh.SetParent(_root, false);
            lh.localPosition = at;
            lh.localScale = Vector3.one * scale;
            var bodyMat = Mats.Lit(body, 0.5f);
            var white = Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0.5f);
            var dark = Mats.Lit(new Color(0.2f, 0.22f, 0.25f), 0.4f, 0.4f);
            Mats.Prim(PrimitiveType.Cylinder, "Base", lh, new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.4f, 2.6f), white);
            Mats.Prim(PrimitiveType.Cylinder, "Tower", lh, new Vector3(0f, 4.2f, 0f), new Vector3(1.6f, 3.4f, 1.6f), bodyMat);
            Mats.Prim(PrimitiveType.Cylinder, "Band", lh, new Vector3(0f, 5.6f, 0f), new Vector3(1.65f, 0.3f, 1.65f), body.r > 0.9f && body.g > 0.9f ? dark : white, false);
            Mats.Prim(PrimitiveType.Cylinder, "Balcony", lh, new Vector3(0f, 7.7f, 0f), new Vector3(2.2f, 0.12f, 2.2f), dark, false);
            var lamp = Mats.Prim(PrimitiveType.Sphere, "Lamp", lh, new Vector3(0f, 8.4f, 0f), Vector3.one * 1.05f, white, false);
            _bulbs.Add(lamp.GetComponent<Renderer>());
            Mats.Prim(PrimitiveType.Cylinder, "Cap", lh, new Vector3(0f, 9.1f, 0f), new Vector3(1.3f, 0.3f, 1.3f), bodyMat, false);
            AddLamp(lh.position + Vector3.up * 8.4f * scale, lightColor, 24f * scale, 2f);
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
            var white = Mats.Lit(new Color(0.96f, 0.96f, 0.94f), 0.45f);
            var red = Mats.Lit(new Color(0.75f, 0.14f, 0.12f), 0.5f);
            var dark = Mats.Lit(new Color(0.18f, 0.2f, 0.22f), 0.4f, 0.5f);
            var glass = Mats.Lit(new Color(0.75f, 0.85f, 0.9f), 0.9f, 0.2f);
            Mats.Prim(PrimitiveType.Cylinder, "Base", lh, new Vector3(0f, 0.5f, 0f), new Vector3(6f, 0.5f, 6f), Mats.Textured("concrete", new Color(0.85f, 0.84f, 0.8f), 1f, 0.2f));
            Mats.Prim(PrimitiveType.Cylinder, "Tower", lh, new Vector3(0f, 8f, 0f), new Vector3(3.2f, 7f, 3.2f), white);
            Mats.Prim(PrimitiveType.Cylinder, "Gallery", lh, new Vector3(0f, 15.2f, 0f), new Vector3(4.4f, 0.25f, 4.4f), dark, false);
            Mats.Prim(PrimitiveType.Cylinder, "Lantern", lh, new Vector3(0f, 16.4f, 0f), new Vector3(2.2f, 1f, 2.2f), glass, false);
            var lamp = Mats.Prim(PrimitiveType.Sphere, "Lamp", lh, new Vector3(0f, 16.4f, 0f), Vector3.one * 1.5f, white, false);
            _bulbs.Add(lamp.GetComponent<Renderer>());
            Mats.Prim(PrimitiveType.Sphere, "Dome", lh, new Vector3(0f, 17.5f, 0f), new Vector3(2.5f, 1.4f, 2.5f), red, false);
            Mats.Prim(PrimitiveType.Cylinder, "Vane", lh, new Vector3(0f, 18.6f, 0f), new Vector3(0.12f, 0.6f, 0.12f), dark, false);
            // 등대지기 숙소
            Mats.Prim(PrimitiveType.Cube, "Keeper", lh, new Vector3(6.5f, 1.8f, 0f), new Vector3(7f, 3.2f, 5f), white);
            Mats.Prim(PrimitiveType.Cube, "KeeperRoof", lh, new Vector3(6.5f, 3.6f, 0f), new Vector3(7.6f, 0.4f, 5.6f), red, false);
            AddLamp(lh.position + Vector3.up * 16.4f, new Color(1f, 0.95f, 0.75f), 60f, 2.6f);
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
            Mats.Prim(PrimitiveType.Cube, "Body", shop, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 4.5f), Mats.Lit(new Color(0.93f, 0.9f, 0.84f)));
            Mats.Prim(PrimitiveType.Cube, "Roof", shop, new Vector3(0f, 3.2f, 0f), new Vector3(7.6f, 0.4f, 5.2f), Mats.Lit(roof));
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
            var wall = Mats.Lit(WallColors[_rng.Next(WallColors.Length)], 0.15f);
            var roof = Mats.Lit(RoofColors[_rng.Next(RoofColors.Length)], 0.35f);
            var glass = Mats.Lit(new Color(0.16f, 0.22f, 0.28f), 0.8f);
            var frame = Mats.Lit(new Color(0.75f, 0.75f, 0.73f), 0.3f);
            var door = Mats.Lit(new Color(0.42f, 0.32f, 0.24f), 0.3f);
            bool twoStory = _rng.NextDouble() < 0.35;
            float w = R(7f, 11f), d = R(5.5f, 8f), floorH = 2.9f;
            float wallH = twoStory ? floorH * 2f : floorH;
            Mats.Prim(PrimitiveType.Cube, "Wall", h, new Vector3(0f, wallH * 0.5f - 0.2f, 0f), new Vector3(w, wallH + 0.4f, d), wall, false);
            // 창문·문 (앞면)
            int windows = Mathf.Max(1, Mathf.RoundToInt(w / 3.2f));
            for (int f = 0; f < (twoStory ? 2 : 1); f++)
            for (int i = 0; i < windows; i++)
            {
                float x = -w * 0.5f + (i + 0.5f) * w / windows;
                bool isDoor = f == 0 && i == windows / 2;
                var size = isDoor ? new Vector3(1.1f, 2.1f, 0.06f) : new Vector3(1.4f, 1.1f, 0.06f);
                float y = f * floorH + (isDoor ? 1.05f : 1.55f);
                Mats.Prim(PrimitiveType.Cube, "Frame", h, new Vector3(x, y, d * 0.5f + 0.02f), size + new Vector3(0.16f, 0.16f, -0.02f), frame, false);
                Mats.Prim(PrimitiveType.Cube, isDoor ? "Door" : "Window", h, new Vector3(x, y, d * 0.5f + 0.05f), size, isDoor ? door : glass, false);
            }
            if (twoStory)
            {
                // 평지붕 + 난간 턱 + 옥상 물탱크 (한국 바닷가 상가)
                Mats.Prim(PrimitiveType.Cube, "RoofSlab", h, new Vector3(0f, wallH + 0.1f, 0f), new Vector3(w + 0.3f, 0.2f, d + 0.3f), frame, false);
                Mats.Prim(PrimitiveType.Cube, "Parapet", h, new Vector3(0f, wallH + 0.45f, d * 0.5f), new Vector3(w + 0.3f, 0.5f, 0.2f), wall, false);
                var tank = Mats.Lit(_rng.NextDouble() < 0.5 ? new Color(0.25f, 0.45f, 0.8f) : new Color(0.8f, 0.25f, 0.2f), 0.4f);
                Mats.Prim(PrimitiveType.Cylinder, "WaterTank", h, new Vector3(R(-w * 0.25f, w * 0.25f), wallH + 1f, -d * 0.2f), new Vector3(1.4f, 0.75f, 1.4f), tank, false);
            }
            else
            {
                // 박공지붕 (용마루가 앞뒤 방향 → 처마가 길·바다 쪽)
                var go = new GameObject("Roof");
                go.transform.SetParent(h, false);
                go.transform.localPosition = new Vector3(0f, wallH, 0f);
                go.transform.localScale = new Vector3(w + 0.8f, R(1.6f, 2.3f), d + 0.9f);
                go.AddComponent<MeshFilter>().sharedMesh = Gable();
                go.AddComponent<MeshRenderer>().sharedMaterial = roof;
            }
            if (sign)
            {
                string text = ShopSigns[_rng.Next(ShopSigns.Length)];
                var board = Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.3f);
                float sy = twoStory ? floorH + 0.1f : floorH - 0.35f;
                Mats.Prim(PrimitiveType.Cube, "SignBoard", h, new Vector3(0f, sy, d * 0.5f + 0.12f), new Vector3(Mathf.Min(w - 1f, 4.5f), 0.7f, 0.12f), board, false);
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

        /// <summary>갯바위 덩어리 (텍스처 바위 구를 찌그러뜨려 눕힌다).</summary>
        public void Boulder(Vector3 at, float size, Material mat)
        {
            Mats.Prim(PrimitiveType.Sphere, "Boulder", _root, at, new Vector3(size * R(1f, 1.5f), size * R(0.55f, 0.8f), size * R(0.9f, 1.2f)), mat, false,
                Quaternion.Euler(R(-15f, 15f), R(0f, 360f), R(-15f, 15f)));
        }
    }
}
