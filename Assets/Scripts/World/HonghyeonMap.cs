using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 홍현 앞바다 (남해). 원작 지역 지도처럼 A~H열 × 1~8행 격자.
    ///  - 남쪽 해안: 다랭이논 비탈 아래 어촌, 갯바위 해안, 낚시점
    ///  - 방파제(E1~E2): 해안에서 50m 뻗은 방파제, 끝에 빨간 등대, 양옆 테트라포드
    ///  - 갯바위 섬 둘: 서쪽 B4 근처, 동쪽 G6 근처. 배로 건너가 내려서 낚시할 수 있다
    ///  - 3행 이후 먼바다는 배로만. 자이언트 그루퍼 어군은 저녁 C4, 아침 E3 (원작 공략 그대로)
    /// </summary>
    public class HonghyeonDef : FieldMapDef
    {
        public override string Id => "honghyeon";
        public override string MapName => "홍현 앞바다 · 남해";
        public override string ShortName => "홍현 앞바다";
        public override string ShopName => "홍현 낚시점";
        public override string UnionContact => "방파제 입구의 어촌계 정계장";
        public override bool IsSea => true;

        public const float ShoreZ = -40f;
        public static readonly Rect Breakwater = new Rect(27f, -40f, 6f, 50f);   // x, z, 너비, 길이
        public static readonly Vector3[] Islands = { new Vector3(-95f, 0f, 70f), new Vector3(85f, 0f, 140f) };
        public static readonly float[] IslandRadius = { 14f, 18f };

        public HonghyeonDef()
        {
            WaterY = 0.2f;
            LandY = 1.0f;
            GridMinX = -160f;
            GridMinZ = ShoreZ;
            CellW = 40f;
            CellH = 30f;
            Cols = 8;
            Rows = 8;
            MinX = -200f;
            MaxX = 200f;
            View = new Rect(-180f, -70f, 360f, 300f);
            Spawn = new Vector3(18f, LandY + 0.1f, -50f);
            ShopPos = new Vector3(-12f, LandY, -58f);
            ShopRadius = 10f;

            // [row, col] row 0 = 해안 쪽
            var S = Habitat.Shallow; var D = Habitat.Deep; var R = Habitat.Rocky; var W = Habitat.Weed;
            HabitatGrid = new[,]
            {
                { S, S, R, R, R, S, W, R },
                { S, W, R, S, R, S, W, R },
                { W, R, S, D, D, S, W, D },
                { R, R, D, D, D, D, D, D },
                { D, R, D, D, R, D, D, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
            };

            Mark(MapMark.Shape.Rect, 0f, (ShoreZ + 260f) * 0.5f, 400f, 260f - ShoreZ, new Color(0.09f, 0.24f, 0.36f));
            Mark(MapMark.Shape.Rect, Breakwater.center.x, Breakwater.center.y, Breakwater.width, Breakwater.height, new Color(0.75f, 0.75f, 0.72f), "방파제");
            Mark(MapMark.Shape.Circle, Breakwater.center.x, Breakwater.yMax, 5f, 5f, new Color(0.95f, 0.25f, 0.2f), "등대");
            for (int i = 0; i < Islands.Length; i++)
                Mark(MapMark.Shape.Circle, Islands[i].x, Islands[i].z, IslandRadius[i] * 2f, IslandRadius[i] * 2f, new Color(0.42f, 0.4f, 0.33f),
                    i == 0 ? "서쪽 갯바위" : "동쪽 갯바위");
            Mark(MapMark.Shape.Circle, ShopPos.x, ShopPos.z, 8f, 8f, new Color(1f, 0.82f, 0.35f), "낚시점");
        }

        static bool OnBreakwater(Vector3 p, float margin = 0f) =>
            p.x > Breakwater.xMin + margin && p.x < Breakwater.xMax - margin && p.z >= ShoreZ - 1f && p.z < Breakwater.yMax - margin;

        static int IslandAt(Vector3 p, float shrink = 0f)
        {
            for (int i = 0; i < Islands.Length; i++)
            {
                var d = new Vector2(p.x - Islands[i].x, p.z - Islands[i].z);
                if (d.magnitude < IslandRadius[i] - shrink) return i;
            }
            return -1;
        }

        public override bool IsWater(Vector3 p) =>
            p.x > MinX && p.x < MaxX && p.z > ShoreZ && p.z < 260f && !OnBreakwater(p) && IslandAt(p) < 0;

        public override bool IsWalkable(Vector3 p)
        {
            if (p.x < MinX + 2f || p.x > MaxX - 2f) return false;
            if (p.z < ShoreZ - 0.6f && p.z > -118f) return true;     // 해안·마을
            if (OnBreakwater(p, 0.5f)) return true;
            return IslandAt(p, 2.5f) >= 0;                          // 갯바위 섬 위
        }

        public override string AreaName(Vector3 p)
        {
            if (OnBreakwater(p)) return p.z > Breakwater.yMax - 8f ? "방파제 끝 등대" : "홍현 방파제";
            int island = IslandAt(p, -6f);
            if (island == 0) return "서쪽 갯바위";
            if (island == 1) return "동쪽 갯바위";
            if (p.z < ShoreZ) return p.x < 0f ? "홍현 마을 앞 갯바위" : "홍현 포구";
            return p.z > 110f ? "먼바다" : "홍현 앞바다";
        }

        public override List<FishSchool> CreateSchools() => new List<FishSchool>
        {
            new FishSchool { Name = "고등어 떼", SpeciesIds = new[] { "mackerel", "horsemackerel", "halfbeak" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Evening }, Col = 4, Row = 2, MaxRow = 6 },
            new FishSchool { Name = "감성돔 무리", SpeciesIds = new[] { "blackseabream", "opaleye" },
                PreferredTimes = new[] { TimeBand.Dawn, TimeBand.Evening }, Col = 2, Row = 1, MaxRow = 4 },
            new FishSchool { Name = "우럭·볼락", SpeciesIds = new[] { "jacopever", "rockfish", "scorpionfish" },
                PreferredTimes = new[] { TimeBand.Evening, TimeBand.Night }, Col = 6, Row = 4, MaxRow = 6 },
            new FishSchool { Name = "참돔 어군", SpeciesIds = new[] { "redseabream", "seabass" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Day }, Col = 3, Row = 5, MinRow = 3, MaxRow = 7 },
            // 원작 공략: 자이언트 그루퍼 어군은 저녁 C4, 아침 E3에만 나타난다
            new FishSchool { Name = "자이언트 그루퍼", SpeciesIds = new[] { "giantgrouper" }, Special = true,
                Schedule = new[] { (TimeBand.Evening, 2, 3), (TimeBand.Morning, 4, 2) } },
        };

        public override Transform BuildWorld(List<Light> lamps, List<Renderer> bulbs) => new SeaWorldBuilder(this).Build(lamps, bulbs);
    }

    /// <summary>홍현 앞바다 월드: 바다, 해안 갯바위, 다랭이논 비탈 마을, 방파제와 등대, 테트라포드, 갯바위 섬, 포구, 낚시점.</summary>
    public class SeaWorldBuilder
    {
        readonly HonghyeonDef _map;
        Transform _root;
        System.Random _rng;
        List<Light> _lamps;
        List<Renderer> _bulbs;

        static readonly Color Rock = new Color(0.36f, 0.34f, 0.31f);
        static readonly Color RockDark = new Color(0.26f, 0.25f, 0.24f);
        static readonly Color Concrete = new Color(0.7f, 0.7f, 0.68f);
        static readonly Color Grass = new Color(0.3f, 0.45f, 0.22f);
        static readonly Color Paddy = new Color(0.45f, 0.55f, 0.25f);
        static readonly Color Wood = new Color(0.55f, 0.4f, 0.26f);

        public SeaWorldBuilder(HonghyeonDef map) { _map = map; }

        float R(float min, float max) => (float)(min + _rng.NextDouble() * (max - min));

        public Transform Build(List<Light> lamps, List<Renderer> bulbs)
        {
            _root = new GameObject("World_Honghyeon").transform;
            _rng = new System.Random(20120412);
            _lamps = lamps;
            _bulbs = bulbs;

            BuildSea();
            BuildShore();
            BuildTerraces();
            BuildBreakwater();
            BuildIslands();
            BuildHarbor();
            BuildShop();
            BuildFarIslands();
            BuildNpcs();
            return _root;
        }

        void BuildSea()
        {
            WaterSurface.Create(_root, new Vector3(0f, _map.WaterY, 180f), new Vector2(900f, 460f), 4f);
            Mats.Prim(PrimitiveType.Cube, "SeaBed", _root, new Vector3(0f, _map.WaterY - 4f, 180f), new Vector3(900f, 1f, 460f),
                Mats.Lit(new Color(0.04f, 0.08f, 0.1f)), false);
        }

        void BuildShore()
        {
            // 해안 땅 (z < -40). 앞쪽은 갯바위, 뒤쪽은 마을 길
            float landH = 6f, depth = 200f;
            Mats.Prim(PrimitiveType.Cube, "Shore", _root, new Vector3(0f, _map.LandY - landH * 0.5f, HonghyeonDef.ShoreZ - depth * 0.5f),
                new Vector3(700f, landH, depth), Mats.Lit(Grass));
            Mats.Prim(PrimitiveType.Cube, "CoastRoad", _root, new Vector3(0f, _map.LandY + 0.01f, -62f), new Vector3(700f, 0.02f, 7f),
                Mats.Lit(new Color(0.34f, 0.34f, 0.36f)), false);
            Mats.Prim(PrimitiveType.Cube, "Seawall", _root, new Vector3(0f, _map.LandY - 0.3f, HonghyeonDef.ShoreZ - 0.4f),
                new Vector3(700f, 1.4f, 1f), Mats.Lit(Concrete));
            Mats.Prim(PrimitiveType.Cube, "HarborApron", _root, new Vector3(30f, _map.LandY + 0.02f, -47f), new Vector3(70f, 0.04f, 13f),
                Mats.Lit(new Color(0.62f, 0.62f, 0.6f)), false);

            // 해안 갯바위: 물가를 따라 크고 작은 바위
            var rock = Mats.Lit(Rock, 0.25f);
            var rockDark = Mats.Lit(RockDark, 0.2f);
            for (float x = -210f; x <= 210f; x += R(4f, 9f))
            {
                if (x > 0f && x < 62f) continue; // 포구 앞은 비워 둔다
                float s = R(2f, 5f);
                Mats.Prim(PrimitiveType.Sphere, "ShoreRock", _root, new Vector3(x, _map.WaterY + s * 0.15f, HonghyeonDef.ShoreZ + R(-0.5f, 2.5f)),
                    new Vector3(s * R(1f, 1.6f), s * 0.6f, s), _rng.NextDouble() < 0.5 ? rock : rockDark, false,
                    Quaternion.Euler(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)));
            }
        }

        void BuildTerraces()
        {
            // 다랭이논: 남쪽으로 올라가는 계단식 논두렁 비탈과 어촌 집들
            for (int i = 0; i < 8; i++)
            {
                float z = -78f - i * 12f;
                float h = 1.6f + i * 2.2f;
                var c = Color.Lerp(Paddy, new Color(0.62f, 0.6f, 0.3f), (i % 3) * 0.3f);
                Mats.Prim(PrimitiveType.Cube, "Terrace", _root, new Vector3(0f, _map.LandY + h * 0.5f - 0.5f, z), new Vector3(700f, h, 12f), Mats.Lit(c));
                Mats.Prim(PrimitiveType.Cube, "TerraceWall", _root, new Vector3(0f, _map.LandY + h - 0.45f, z + 5.8f), new Vector3(700f, 0.3f, 0.5f),
                    Mats.Lit(new Color(0.48f, 0.44f, 0.36f)), false);
                if (!KenneyProps.Available) continue;
                for (float x = -180f; x <= 180f; x += R(18f, 34f))
                {
                    if (_rng.NextDouble() < 0.45)
                        KenneyProps.Spawn("Kenney/City/building-" + "abcdeh"[_rng.Next(6)], _root,
                            new Vector3(x, _map.LandY + h - 0.5f, z + R(-2f, 2f)), 180f + R(-15f, 15f), R(5f, 8f));
                    else
                        KenneyProps.Spawn("Kenney/Nature/" + new[] { "tree_default", "tree_oak", "tree_pineRoundA", "tree_fat" }[_rng.Next(4)],
                            _root, new Vector3(x, _map.LandY + h - 0.5f, z + R(-3f, 3f)), R(0f, 360f), R(4f, 7f));
                }
            }
        }

        void BuildBreakwater()
        {
            var b = HonghyeonDef.Breakwater;
            var bw = new GameObject("Breakwater").transform;
            bw.SetParent(_root, false);
            float bottom = _map.WaterY - 4f;
            float h = _map.LandY - bottom;
            Mats.Prim(PrimitiveType.Cube, "Body", bw, new Vector3(b.center.x, bottom + h * 0.5f, b.center.y), new Vector3(b.width, h, b.height), Mats.Lit(Concrete));
            Mats.Prim(PrimitiveType.Cube, "Parapet", bw, new Vector3(b.xMin + 0.3f, _map.LandY + 0.6f, b.center.y), new Vector3(0.6f, 1.2f, b.height),
                Mats.Lit(new Color(0.64f, 0.64f, 0.62f)), false);

            // 테트라포드 (바깥쪽): 4방향 다리를 가진 콘크리트 블록 흉내
            var tetra = Mats.Lit(new Color(0.6f, 0.6f, 0.58f), 0.15f);
            for (float z = b.yMin + 3f; z < b.yMax + 4f; z += R(2.2f, 3f))
            for (int k = 0; k < 2; k++)
            {
                var p = new Vector3(b.xMin - R(1f, 3.5f) - k * 1.5f, _map.WaterY + R(-0.4f, 0.5f), z + R(-0.8f, 0.8f));
                var t = new GameObject("Tetrapod").transform;
                t.SetParent(bw, false);
                t.localPosition = p;
                t.localRotation = Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f));
                for (int leg = 0; leg < 4; leg++)
                {
                    var dir = leg == 0 ? Vector3.up : Quaternion.Euler(109.5f, leg * 120f, 0f) * Vector3.up;
                    Mats.Prim(PrimitiveType.Capsule, "Leg", t, dir * 0.55f, new Vector3(0.6f, 0.6f, 0.6f), tetra, false, Quaternion.FromToRotation(Vector3.up, dir));
                }
            }

            // 빨간 등대
            var lighthouse = new GameObject("Lighthouse").transform;
            lighthouse.SetParent(bw, false);
            lighthouse.localPosition = new Vector3(b.center.x, _map.LandY, b.yMax - 2.5f);
            var red = Mats.Lit(new Color(0.85f, 0.15f, 0.12f), 0.5f);
            var white = Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0.5f);
            Mats.Prim(PrimitiveType.Cylinder, "Base", lighthouse, new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.4f, 2.6f), white);
            Mats.Prim(PrimitiveType.Cylinder, "Tower", lighthouse, new Vector3(0f, 4.4f, 0f), new Vector3(1.7f, 3.6f, 1.7f), red);
            Mats.Prim(PrimitiveType.Cylinder, "Band", lighthouse, new Vector3(0f, 6f, 0f), new Vector3(1.75f, 0.35f, 1.75f), white, false);
            Mats.Prim(PrimitiveType.Cylinder, "Balcony", lighthouse, new Vector3(0f, 8.1f, 0f), new Vector3(2.2f, 0.12f, 2.2f), white, false);
            var lamp = Mats.Prim(PrimitiveType.Sphere, "Lamp", lighthouse, new Vector3(0f, 8.8f, 0f), Vector3.one * 1.1f, white, false);
            _bulbs.Add(lamp.GetComponent<Renderer>());
            Mats.Prim(PrimitiveType.Cylinder, "Cap", lighthouse, new Vector3(0f, 9.5f, 0f), new Vector3(1.4f, 0.3f, 1.4f), red, false);
            AddLamp(lighthouse.position + Vector3.up * 8.8f, new Color(1f, 0.55f, 0.45f), 26f, 2.2f);
        }

        void BuildIslands()
        {
            var rock = Mats.Lit(Rock, 0.2f);
            var top = Mats.Lit(new Color(0.42f, 0.4f, 0.35f), 0.15f);
            for (int i = 0; i < HonghyeonDef.Islands.Length; i++)
            {
                var c = HonghyeonDef.Islands[i];
                float r = HonghyeonDef.IslandRadius[i];
                var isl = new GameObject("Island").transform;
                isl.SetParent(_root, false);
                // 걸을 수 있는 평평한 윗면 + 둘레의 큰 바위
                Mats.Prim(PrimitiveType.Cylinder, "Top", isl, new Vector3(c.x, (_map.LandY + _map.WaterY - 4f) * 0.5f, c.z),
                    new Vector3(r * 2f - 2f, (_map.LandY - (_map.WaterY - 4f)) * 0.5f, r * 2f - 2f), top);
                for (int k = 0; k < 18; k++)
                {
                    float a = k / 18f * Mathf.PI * 2f + R(-0.1f, 0.1f);
                    float s = R(3f, 6f);
                    var p = new Vector3(c.x + Mathf.Cos(a) * (r - 1f), _map.WaterY + R(-0.5f, 1.2f), c.z + Mathf.Sin(a) * (r - 1f));
                    Mats.Prim(PrimitiveType.Sphere, "Rock", isl, p, new Vector3(s, s * R(0.6f, 1f), s * R(0.8f, 1.2f)), rock, false,
                        Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-20f, 20f)));
                }
                // 섬 꼭대기 바위 봉우리
                Mats.Prim(PrimitiveType.Sphere, "Peak", isl, new Vector3(c.x + r * 0.35f, _map.LandY + 2f, c.z - r * 0.3f),
                    new Vector3(r * 0.7f, 6f, r * 0.6f), rock, true);
            }
        }

        void BuildHarbor()
        {
            // 포구 안쪽(방파제 동쪽)에 정박한 어선과 부표
            if (KenneyProps.Available)
            {
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", _root, new Vector3(44f, _map.WaterY - 0.3f, -30f), 10f, 0f, 7f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", _root, new Vector3(54f, _map.WaterY - 0.3f, -24f), -15f, 0f, 7f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-row-small", _root, new Vector3(38f, _map.WaterY - 0.15f, -36f), 80f, 0f, 3.6f);
                for (int i = 0; i < 4; i++)
                    KenneyProps.Spawn("Kenney/Watercraft/buoy", _root, new Vector3(-60f + i * 50f, _map.WaterY - 0.6f, 30f + (i % 2) * 40f), 0f, 1.6f);
            }
            // 해안 가로등
            var pole = Mats.Lit(new Color(0.2f, 0.22f, 0.25f), 0.5f, 0.6f);
            for (float x = -120f; x <= 120f; x += 30f)
            {
                var basePos = new Vector3(x, _map.LandY, -58.5f);
                Vector3 head;
                if (KenneyProps.Available)
                {
                    const float lampH = 5.5f;
                    KenneyProps.Spawn("Kenney/Roads/light-curved", _root, basePos, 180f, lampH);
                    float s = lampH / 0.67f;
                    head = basePos + new Vector3(0f, 0.62f * s, 0.19f * s);
                }
                else
                {
                    Mats.Prim(PrimitiveType.Cylinder, "Pole", _root, basePos + Vector3.up * 2f, new Vector3(0.12f, 2f, 0.12f), pole, false);
                    head = basePos + Vector3.up * 4.1f;
                }
                var bulb = Mats.Prim(PrimitiveType.Sphere, "Bulb", _root, head, new Vector3(0.45f, 0.18f, 0.45f), pole, false);
                _bulbs.Add(bulb.GetComponent<Renderer>());
                AddLamp(head - Vector3.up * 0.2f, new Color(1f, 0.82f, 0.55f), 14f, 1.6f);
            }
        }

        void AddLamp(Vector3 pos, Color color, float range, float intensity)
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

        void BuildShop()
        {
            var p = _map.ShopPos;
            var shop = new GameObject("TackleShop").transform;
            shop.SetParent(_root, false);
            shop.localPosition = p;
            Mats.Prim(PrimitiveType.Cube, "Body", shop, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 4.5f), Mats.Lit(new Color(0.9f, 0.92f, 0.95f)));
            Mats.Prim(PrimitiveType.Cube, "Roof", shop, new Vector3(0f, 3.2f, 0f), new Vector3(7.6f, 0.4f, 5.2f), Mats.Lit(new Color(0.75f, 0.3f, 0.2f)));
            for (int i = 0; i < 6; i++)
            {
                var c = i % 2 == 0 ? new Color(0.2f, 0.45f, 0.8f) : new Color(0.97f, 0.97f, 0.95f);
                Mats.Prim(PrimitiveType.Cube, "Awning", shop, new Vector3(-3f + i * 1.2f, 2.6f, 2.75f), new Vector3(1.2f, 0.12f, 1.4f),
                    Mats.Lit(c), false, Quaternion.Euler(-18f, 0f, 0f));
            }
            Mats.Prim(PrimitiveType.Cube, "Counter", shop, new Vector3(0f, 0.55f, 3.5f), new Vector3(5f, 1.1f, 0.6f), Mats.Lit(Wood));
            var light = new GameObject("ShopLight").AddComponent<Light>();
            light.transform.SetParent(shop, false);
            light.transform.localPosition = new Vector3(0f, 2.4f, 3.4f);
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.shadows = LightShadows.None;

            var owner = NpcAngler.Spawn(shop, new Vector3(1.2f, 0f, 2.75f), 0f, new CharacterLook
            {
                Name = "홍현 낚시점 최사장", Title = "홍현 낚시점", Female = true, Hair = "Hair_Buns", HairColor = new Color(0.12f, 0.1f, 0.09f),
                Shirt = new Color(0.95f, 0.9f, 0.8f), VestColor = new Color(0.2f, 0.4f, 0.6f), Pants = new Color(0.25f, 0.25f, 0.3f), Hat = false
            }, NpcAngler.Role.Talker);
            owner.NpcId = "shop";
            WorldBuilder.AddSign(shop, "홍현 낚시점", new Vector3(0f, 3.9f, 2.3f), new Color(0.6f, 0.85f, 1f));
        }

        void BuildFarIslands()
        {
            // 남해 다도해: 수평선 가까이 겹겹이 보이는 섬들
            var near = Mats.Lit(new Color(0.2f, 0.32f, 0.24f));
            var far = Mats.Lit(new Color(0.32f, 0.42f, 0.42f));
            float[,] isl = { { -260f, 430f, 150f, 45f }, { 40f, 520f, 220f, 60f }, { 300f, 460f, 170f, 50f }, { -120f, 620f, 260f, 70f }, { 360f, 640f, 240f, 65f } };
            for (int i = 0; i < isl.GetLength(0); i++)
                Mats.Prim(PrimitiveType.Sphere, "FarIsland", _root, new Vector3(isl[i, 0], -isl[i, 3] * 0.35f, isl[i, 1]),
                    new Vector3(isl[i, 2], isl[i, 3], isl[i, 2] * 0.45f), i >= 3 ? far : near, false);
        }

        void BuildNpcs()
        {
            var parent = new GameObject("SeaNpcs").transform;
            parent.SetParent(_root, false);
            var b = HonghyeonDef.Breakwater;
            var union = NpcAngler.Spawn(parent, new Vector3(b.xMin - 3f, _map.LandY, HonghyeonDef.ShoreZ - 4f), 200f, new CharacterLook
            {
                Name = "어촌계 정계장", Title = "한국 낚시 조합 홍현 지부", Hair = "Hair_Buzzed", Beard = true, HairColor = new Color(0.25f, 0.25f, 0.25f),
                Shirt = new Color(0.3f, 0.35f, 0.45f), Vest = true, VestColor = new Color(0.9f, 0.55f, 0.15f), Pants = new Color(0.2f, 0.2f, 0.22f), Hat = true,
                HatColor = new Color(0.15f, 0.2f, 0.35f)
            }, NpcAngler.Role.Talker);
            union.NpcId = "union";

            // 방파제와 섬의 갯바위 꾼들
            NpcAngler.Spawn(parent, new Vector3(b.xMax - 1f, _map.LandY, b.yMin + 18f), 70f, new CharacterLook
            {
                Name = "감생이사냥꾼", Title = "5짜 감성돔 2회", Hair = "Hair_Buzzed", Shirt = new Color(0.15f, 0.2f, 0.3f),
                VestColor = new Color(0.95f, 0.5f, 0.1f), Pants = new Color(0.2f, 0.2f, 0.22f), HatColor = new Color(0.9f, 0.9f, 0.85f)
            }, NpcAngler.Role.Angler);
            NpcAngler.Spawn(parent, new Vector3(b.xMin + 1.2f, _map.LandY, b.yMax - 8f), -20f, new CharacterLook
            {
                Name = "밤볼락", Title = "볼락 루어 고수", Female = true, Hair = "Hair_SimpleParted", Shirt = new Color(0.85f, 0.3f, 0.35f),
                Vest = false, Pants = new Color(0.15f, 0.15f, 0.2f), Hat = false
            }, NpcAngler.Role.Angler);
            var isl = HonghyeonDef.Islands[0];
            NpcAngler.Spawn(parent, new Vector3(isl.x + 4f, _map.LandY, isl.z + 8f), 10f, new CharacterLook
            {
                Name = "홍현토박이", Title = "갯바위 40년", Hair = "Hair_Buzzed", Beard = true, HairColor = new Color(0.8f, 0.8f, 0.78f),
                Shirt = new Color(0.5f, 0.45f, 0.35f), VestColor = new Color(0.3f, 0.35f, 0.25f), Pants = new Color(0.3f, 0.28f, 0.25f)
            }, NpcAngler.Role.Angler);
        }
    }
}
