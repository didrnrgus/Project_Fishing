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

        public override float SeabedAt(float x, float z) => OnBreakwater(new Vector3(x, 0f, z)) ? LandY : SeaTerrain.HeightAt(x, z);
        public override Rect SeabedArea => new Rect(-205f, -70f, 410f, 330f);

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
            BuildStoneWeir();
            BuildWindbreak();
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
            // 땅·바다 밑·갯바위 섬은 지형(Unity Terrain)으로
            SeaTerrain.Build(_root, _map.LandY, _map.WaterY);
        }

        void BuildShore()
        {
            // 해안 땅은 지형(SeaTerrain). 마을 길과 포구 앞 호안(콘크리트 벽)만 따로
            Mats.Prim(PrimitiveType.Cube, "CoastRoad", _root, new Vector3(0f, _map.LandY + 0.03f, -62f), new Vector3(420f, 0.14f, 7f),
                Mats.TexturedBox("asphalt", Color.white, new Vector2(420f, 7f), 6f, 0.15f), false);
            HarborKit.QuayWall(_root, -15f, 75f, _ => HonghyeonDef.ShoreZ - 0.4f, _map.LandY + 0.4f, _map.LandY - 2.8f, _map.WaterY,
                Mats.Quay(new Color(0.85f, 0.85f, 0.83f)), _rng, true);
            // 앞마당 잡동사니 (어촌계 정계장 자리·방파제 뿌리는 비운다)
            foreach (float x in new[] { -8f, 6f, 44f, 58f })
                HarborKit.QuayClutter(_root, new Vector3(x + R(-2f, 2f), _map.LandY + 0.1f, HonghyeonDef.ShoreZ - R(2.8f, 5f)), R(0f, 360f), _rng);
            Mats.Prim(PrimitiveType.Cube, "HarborApron", _root, new Vector3(30f, _map.LandY + 0.03f, -47f), new Vector3(70f, 0.14f, 13f),
                Mats.TexturedBox("concrete", new Color(0.8f, 0.8f, 0.78f), new Vector2(70f, 13f), 4f, 0.2f), false);

            // 해안 갯바위: 물가를 따라 크고 작은 바위
            var rock = Mats.Textured("rock", new Color(0.85f, 0.83f, 0.8f), 1f, 0.25f);
            var rockDark = Mats.Textured("rock", new Color(0.62f, 0.6f, 0.58f), 1.4f, 0.3f);
            for (float x = -210f; x <= 210f; x += R(4f, 9f))
            {
                if (x > 0f && x < 62f) continue; // 포구 앞은 비워 둔다
                float s = R(2f, 5f);
                Rocks.Prim("ShoreRock", _root, new Vector3(x, _map.WaterY + s * 0.15f, HonghyeonDef.ShoreZ + R(-0.5f, 2.5f)),
                    new Vector3(s * R(1f, 1.6f), s * 0.6f, s), _rng.NextDouble() < 0.5 ? rock : rockDark,
                    Quaternion.Euler(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)));
            }
        }

        void BuildTerraces()
        {
            // 다랭이논: 남쪽으로 올라가는 계단식 논두렁 비탈과 어촌 집들
            // 계단 논 자체는 지형(SeaTerrain.HeightAt)이 만든다. 여기서는 논 위 어촌 집과 나무를 지형 높이에 올린다
            if (!KenneyProps.Available) return;
            var props = new CoastProps(_root, _rng, _lamps, _bulbs);
            for (int i = 0; i < 8; i++)
            {
                float z = -80f - i * 12f;
                for (float x = -180f; x <= 180f; x += R(18f, 34f))
                {
                    float zz = z + R(-2f, 2f);
                    float y = SeaTerrain.HeightAt(x, zz) - 0.05f;
                    if (_rng.NextDouble() < 0.45)
                        props.KoreanHouse(new Vector3(x, y, zz), R(-15f, 15f), false); // 다랭이 마을 집 (사진: 칠한 지붕의 단층·2층 시멘트 집)
                    else
                        KenneyProps.Spawn("Kenney/Nature/" + new[] { "tree_default", "tree_oak", "tree_pineRoundA", "tree_fat" }[_rng.Next(4)],
                            _root, new Vector3(x, y, zz), R(0f, 360f), R(4f, 7f));
                }
            }
        }

        void BuildBreakwater()
        {
            var b = HonghyeonDef.Breakwater;
            var bw = new GameObject("Breakwater").transform;
            bw.SetParent(_root, false);
            // HarborKit: 10m 타설 블록, 서쪽 1.4m 파라펫(디딤단 없이 안쪽 면이 곧아서 벽화가 붙는다), 동쪽 안쪽 시설 (연석·계선주·타이어·사다리)
            HarborKit.Breakwater(bw, "Body", new HarborKit.BreakwaterSpec
            {
                Rect = b, AlongZ = true, ParapetSide = -1, TopY = _map.LandY, BottomY = _map.WaterY - 4f, WaterY = _map.WaterY,
                ParapetH = 1.4f, ParapetW = 0.9f, Fittings = true, LifeRing = true, NoStep = true,
                Tint = new Color(0.92f, 0.92f, 0.9f), ParapetTint = new Color(0.78f, 0.78f, 0.76f),
            }, _rng);

            // 사진(홍현 방파제): 난간벽 안쪽에 파란 바다 벽화와 고래 그림
            var muralBlue = Mats.Lit(new Color(0.16f, 0.38f, 0.68f), 0.3f);
            var muralDeep = Mats.Lit(new Color(0.1f, 0.22f, 0.45f), 0.3f);
            var muralWhite = Mats.Lit(new Color(0.92f, 0.94f, 0.96f), 0.3f);
            float mx = b.xMin + 0.92f;
            Mats.Prim(PrimitiveType.Cube, "Mural", bw, new Vector3(mx, _map.LandY + 0.68f, b.center.y), new Vector3(0.04f, 1.2f, b.height - 4f), muralBlue, false);
            for (float z = b.yMin + 6f; z < b.yMax - 6f; z += 11f)
            {
                // 고래: 납작한 몸통 + 꼬리 두 갈래 + 흰 배
                var whale = new Vector3(mx + 0.03f, _map.LandY + 0.72f + R(-0.08f, 0.1f), z);
                Mats.Prim(PrimitiveType.Sphere, "Whale", bw, whale, new Vector3(0.05f, 0.42f, 2.1f), muralDeep, false);
                Mats.Prim(PrimitiveType.Sphere, "WhaleBelly", bw, whale + new Vector3(0.01f, -0.1f, 0.2f), new Vector3(0.05f, 0.17f, 1.3f), muralWhite, false);
                foreach (float s in new[] { -1f, 1f })
                    Mats.Prim(PrimitiveType.Sphere, "WhaleTail", bw, whale + new Vector3(0f, s * 0.16f, -1.2f), new Vector3(0.05f, 0.32f, 0.2f), muralDeep, false,
                        Quaternion.Euler(s * 35f, 0f, 0f));
                Mats.Prim(PrimitiveType.Sphere, "Wave", bw, new Vector3(mx + 0.03f, _map.LandY + 0.2f, z + 5f), new Vector3(0.05f, 0.14f, 1.6f), muralWhite, false);
            }

            // 테트라포드 (바깥쪽): 벽에 기댄 안쪽 줄이 가장 높고 바깥 줄일수록 잠긴다
            var tetra = Mats.Tetrapod(new Color(0.92f, 0.9f, 0.86f));
            for (int k = 0; k < 3; k++)
                for (float z = b.yMin + 3f + (k % 2); z < b.yMax + 4f; z += R(1.8f, 2.3f))
                {
                    var p = new Vector3(b.xMin - 0.6f - k * 1.7f + R(-0.3f, 0.3f), _map.WaterY + 0.35f - k * 0.55f + R(-0.25f, 0.25f), z);
                    CoastProps.Tetrapod(bw, p, Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f)), R(0.9f, 1.1f), tetra, false);
                }

            // 빨간 등대
            var lighthouse = new GameObject("Lighthouse").transform;
            lighthouse.SetParent(bw, false);
            lighthouse.localPosition = new Vector3(b.center.x, _map.LandY, b.yMax - 2.5f);
            _bulbs.Add(LighthouseBuilder.Small(lighthouse, new Color(0.85f, 0.15f, 0.12f)));
            AddLamp(lighthouse.position + Vector3.up * LighthouseBuilder.SmallLampY, new Color(1f, 0.55f, 0.45f), 26f, 2.2f);
        }

        /// <summary>
        /// 홍현리 석방렴 (사용자 참고 사진, 원시어로 시설): 물가에서 바다 쪽으로 말굽처럼 둥글게 쌓은 돌담.
        /// 밀물에 들어온 고기가 썰물에 갇힌다. 둥근 냇돌을 사다리꼴 단면으로 쌓아 윗단이 수면 위로 조금 드러난다.
        /// </summary>
        void BuildStoneWeir()
        {
            var c = new Vector3(-70f, 0f, HonghyeonDef.ShoreZ + 1f);
            const float radius = 17f;
            var path = new List<Vector3>();
            for (int i = 0; i <= 28; i++)
            {
                float a = Mathf.Lerp(-8f, 188f, i / 28f) * Mathf.Deg2Rad;
                // 바다 쪽(+z)으로 조금 길쭉한 말굽
                path.Add(new Vector3(c.x + Mathf.Cos(a) * radius, 0f, c.z + Mathf.Sin(a) * radius * 1.15f));
            }
            var go = new GameObject("StoneWeir");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = Rocks.StoneBand(path, 2.6f, _map.WaterY + 0.45f, (x, z) => SeaTerrain.HeightAt(x, z), 0.75f, 1129);
            go.AddComponent<MeshRenderer>().sharedMaterial = Mats.Textured("rock", new Color(0.62f, 0.6f, 0.56f), 1f, 0.35f);
            WorldBuilder.AddSign(_root, "홍현 석방렴", new Vector3(c.x, _map.LandY + 4.5f, HonghyeonDef.ShoreZ - 8f), new Color(1f, 0.88f, 0.6f));
        }

        /// <summary>
        /// 홍현마을 방풍림 (사용자 참고 사진): 태풍을 막는 늙은 해송이 해안 돌 축대 위로 줄지어 선다.
        /// 물가는 둥근 돌을 쌓은 낮은 축대, 그 뒤 해송 줄과 그늘 아래 벤치.
        /// </summary>
        void BuildWindbreak()
        {
            const float x0 = -195f, x1 = -26f;
            var wall = new List<Vector3> { new Vector3(x0, 0f, HonghyeonDef.ShoreZ - 3.2f), new Vector3(x1, 0f, HonghyeonDef.ShoreZ - 3.2f) };
            var go = new GameObject("StoneEmbankment");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = Rocks.StoneBand(wall, 1.4f, _map.LandY + 0.7f, (x, z) => SeaTerrain.HeightAt(x, z) - 0.3f, 0.6f, 2207);
            go.AddComponent<MeshRenderer>().sharedMaterial = Mats.Textured("rock", new Color(0.7f, 0.68f, 0.64f), 1f, 0.25f);
            if (!KenneyProps.Available) return;
            for (float x = x0 + 3f; x < x1; x += R(5f, 8.5f))
            {
                float z = R(-56f, -48f);
                KenneyProps.Spawn("Kenney/Nature/" + (_rng.NextDouble() < 0.5 ? "tree_pineRoundA" : "tree_pineTallA"), _root,
                    new Vector3(x, SeaTerrain.HeightAt(x, z) - 0.05f, z), R(0f, 360f), R(9f, 13f));
            }
            var bench = Mats.Wood(new Color(0.5f, 0.36f, 0.24f), 0.3f);
            for (float x = x0 + 20f; x < x1; x += 34f)
            {
                Mats.Prim(PrimitiveType.Cube, "Bench", _root, new Vector3(x, _map.LandY + 0.45f, -50f), new Vector3(1.8f, 0.08f, 0.45f), bench, false);
                foreach (float lx in new[] { -0.7f, 0.7f })
                    Mats.Prim(PrimitiveType.Cube, "BenchLeg", _root, new Vector3(x + lx, _map.LandY + 0.22f, -50f), new Vector3(0.08f, 0.45f, 0.4f), bench, false);
            }
        }

        void BuildIslands()
        {
            var rock = Mats.Textured("rock", new Color(0.78f, 0.76f, 0.72f), 1.2f, 0.3f);
            for (int i = 0; i < HonghyeonDef.Islands.Length; i++)
            {
                var c = HonghyeonDef.Islands[i];
                float r = HonghyeonDef.IslandRadius[i];
                var isl = new GameObject("Island").transform;
                isl.SetParent(_root, false);
                // 섬 몸통·윗면·봉우리는 지형(SeaTerrain). 물가에 굴러떨어진 큰 바위만 따로
                for (int k = 0; k < 12; k++)
                {
                    float a = k / 12f * Mathf.PI * 2f + R(-0.2f, 0.2f);
                    float s = R(3f, 6f);
                    var p = new Vector3(c.x + Mathf.Cos(a) * (r + 2f), _map.WaterY + R(-0.8f, 0.6f), c.z + Mathf.Sin(a) * (r + 2f));
                    Rocks.Prim("Rock", isl, p, new Vector3(s, s * R(0.6f, 1f), s * R(0.8f, 1.2f)), rock,
                        Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-20f, 20f)));
                }
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

            var owner = NpcAngler.Spawn(shop, new Vector3(1.2f, 0f, 2.75f), 0f, new CharacterLook { Name = "홍현 낚시점 최사장", Title = "홍현 낚시점", Female = true }, NpcAngler.Role.Talker);
            owner.NpcId = "shop";
            WorldBuilder.AddSign(shop, "홍현 낚시점", new Vector3(0f, 3.9f, 2.3f), new Color(0.6f, 0.85f, 1f));
        }

        void BuildFarIslands()
        {
            // 남해 다도해 (사진: 잔잔한 만 건너 산이 겹겹인 섬들): 숲 결 섬, 물가는 바위 절벽, 멀수록 푸른 안개빛 (FarLand)
            float[,] isl =
            {
                { -230f, 380f, 150f, 70f }, { 60f, 430f, 210f, 95f }, { 300f, 400f, 160f, 75f },
                { -120f, 560f, 260f, 110f }, { 360f, 600f, 240f, 100f }, { 120f, 720f, 380f, 130f },
            };
            for (int i = 0; i < isl.GetLength(0); i++)
            {
                FarLand.Place(_root, "FarIsland", new Vector3(isl[i, 0], _map.WaterY, isl[i, 1]), isl[i, 2], isl[i, 2] * 0.55f, isl[i, 3], 11 + i,
                    FarLand.KoreaForest(i < 3 ? 0.45f : i < 5 ? 0.6f : 0.75f));
            }
        }

        void BuildNpcs()
        {
            var parent = new GameObject("SeaNpcs").transform;
            parent.SetParent(_root, false);
            var b = HonghyeonDef.Breakwater;
            var union = NpcAngler.Spawn(parent, new Vector3(b.xMin - 3f, _map.LandY, HonghyeonDef.ShoreZ - 4f), 200f, new CharacterLook { Name = "어촌계 정계장", Title = "한국 낚시 조합 홍현 지부" }, NpcAngler.Role.Talker);
            union.NpcId = "union";

            // 방파제와 섬의 갯바위 꾼들
            NpcAngler.Spawn(parent, new Vector3(b.xMax - 1f, _map.LandY, b.yMin + 18f), 70f, new CharacterLook { Name = "감생이사냥꾼", Title = "5짜 감성돔 2회" }, NpcAngler.Role.Angler);
            NpcAngler.Spawn(parent, new Vector3(b.xMin + 1.5f, _map.LandY, b.yMax - 8f), -20f, new CharacterLook { Name = "밤볼락", Title = "볼락 루어 고수", Female = true }, NpcAngler.Role.Angler);
            var isl = HonghyeonDef.Islands[0];
            NpcAngler.Spawn(parent, new Vector3(isl.x + 4f, _map.LandY, isl.z + 8f), 10f, new CharacterLook { Name = "홍현토박이", Title = "갯바위 40년" }, NpcAngler.Role.Angler);
        }
    }
}
