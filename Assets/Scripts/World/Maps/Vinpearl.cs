using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>
        /// 빈펄 (Lv.27): 나트랑 앞바다 혼째 섬의 리조트 해변. 흰 모래밭에 초가 파라솔·선베드, 잔교 끝 바다 위 초가 정자,
        /// 바다 위 철탑을 건너 본토로 가는 케이블카, 멀리 작은 섬들 (원작 1차 영상 0:28·0:40·0:56).
        /// </summary>
        public static CoastSpec Vinpearl() => new CoastSpec
        {
            Id = "vinpearl", MapName = "빈펄 · 나트랑", ShortName = "빈펄", ShopName = "혼째 섬 낚시점", UnionContact = "빈펄 선착장의 레 티 마이 지부장",
            LandName = "빈펄 해변", SeaName = "나트랑 만", Seed = 51, SandTint = new Color(1.5f, 1.45f, 1.32f),
            WaterDeep = new Color(0.02f, 0.24f, 0.36f), WaterShallow = new Color(0.1f, 0.62f, 0.6f),
            Shores = new[] { new ShoreSeg(-999f, 999f, ShoreKind.Beach) },
            NearDepth = 2.5f, DepthSlope = 0.06f, FlatWidth = 34f, CliffHeight = 22f, CliffWidth = 50f, RoadOffset = 20f, VillageDensity = 0.6f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Palm,
            Platforms = new[]
            {
                new PlatformSpec { Area = new Rect(30f, -41f, 5f, 53f), Kind = PlatformKind.Pier, Name = "빈펄 잔교" },
                new PlatformSpec { Area = new Rect(24f, 11f, 17f, 13f), Kind = PlatformKind.Pier, Name = "바다 위 정자" },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(-112f, 0f, 74f), Radius = 14f, Height = 6f, Walkable = true, Trees = true, Name = "혼몬 바위섬" } },
            Stacks = new[] { new StackSpec { Pos = new Vector3(128f, 0f, 96f), Radius = 4f, Height = 9f, Pines = true, Name = "갈매기 바위" } },
            Grid = new[,]
            {
                { S, S, S, S, S, W, R, R },
                { W, S, S, D, D, S, R, R },
                { W, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(8f, 1.1f, -48f), ShopPos = new Vector3(-20f, 1f, -56f),
            ShopRoof = new Color(0.66f, 0.54f, 0.34f),
            ShopOwner = Local("혼째 섬 낚시점 반 사장", "혼째 섬 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -48f, 200f, Union("레 티 마이 지부장", "한국 낚시 조합 나트랑 지부", true), NpcAngler.Role.Talker, "union"),
                Npc(36f, 18f, 0f, Local("정자의 낚시꾼", "해 뜨면 여기 앉는다", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-34f, -50f, 150f, Local("논라 쓴 과일 장수", "망고 사세요!", true), NpcAngler.Role.Talker),
                Npc(-78f, -45f, 10f, Local("신혼여행 온 부부", "카약 빌리러 왔다", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("VINPEARL", new Vector3(-6f, 6.5f, -66f), SignGold) },
            FarIslands = new[] { new Vector4(-330f, 430f, 170f, 46f), new Vector4(260f, 470f, 110f, 34f), new Vector4(60f, 520f, 80f, 22f) },
            // 케이블카 정류장 언덕
            Reserved = new[] { new Rect(-185f, -110f, 50f, 60f), new Rect(52f, -150f, 120f, 92f) },
            Schools = () => new List<FishSchool>
            {
                School("폼파노 해변", 2, 0, 1, DayLong, "pompano", "silverpomfret"),
                School("독가시치 수초", 0, 1, 2, DayLong, "rabbitfish", "russellsnapper"),
                School("퀸피시 떼", 4, 1, 3, MornDay, "queenfish", "narrowbarredmackerel"),
                School("산호 바위", 6, 2, 4, DayLong, "coraltrout", "spangledemperor", "goldensnapper"),
                School("롱테일 참치 떼", 4, 4, 6, DayLong, "longtailtuna", "mahimahi"),
                // 코비아: 아침 F4, 낮 C6 (먼 쪽 깊은 물)
                Special("코비아 어군", "cobia", (TimeBand.Morning, 5, 3), (TimeBand.Day, 2, 5)),
            },
            Extra = VinpearlExtra,
        };

        static readonly Color Thatch = new Color(0.68f, 0.55f, 0.34f);

        /// <summary>빈펄만의 풍경: 바다 위 초가 정자, 해변 초가 파라솔·선베드, 바다를 건너는 케이블카.</summary>
        static void VinpearlExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Vinpearl").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, deck = land + 0.34f;
            var post = Mats.Wood(new Color(0.42f, 0.3f, 0.2f), 0.25f);
            var straw = Mats.Thatch(Thatch);

            // --- 바다 위 정자: 네 기둥 + 네모뿔 초가지붕 + 벤치 ---
            var gz = new Vector3(32.5f, deck, 17.5f);
            foreach (var (dx, dz) in new[] { (-3f, -3f), (3f, -3f), (-3f, 3f), (3f, 3f) })
                Mats.Prim(PrimitiveType.Cylinder, "GazeboPost", root, gz + new Vector3(dx, 1.4f, dz), new Vector3(0.28f, 1.4f, 0.28f), post, true);
            var roof = MapKit.Lathe(new[] { new Vector2(5.4f, 0f), new Vector2(3.2f, 1.3f), new Vector2(0.05f, 3.4f) }, 4, 2f);
            MapKit.Place(root, "GazeboRoof", roof, straw, gz + new Vector3(0f, 2.8f, 0f), 45f);
            Mats.Prim(PrimitiveType.Cube, "GazeboBench", root, gz + new Vector3(0f, 0.45f, 2.6f), new Vector3(4.5f, 0.12f, 0.6f), post, true);

            // --- 해변: 초가 파라솔과 선베드 (물가 6~16m 안쪽 모래밭) ---
            for (float x = -150f; x < 150f; x += b.Rand(13f, 19f))
            {
                if (x > -30f && x < 46f) continue; // 낚시점·잔교 앞은 비운다
                float z = b.Map.ShoreAt(x) - b.Rand(9f, 14f);
                float y = b.Height(x, z);
                if (y < land - 0.3f) continue;
                MapKit.Prop(root, "parasol_thatch", new Vector3(x, y - 0.05f, z), b.Rand(0f, 360f), 3.4f);
                for (int k = -1; k <= 1; k += 2)
                    MapKit.Prop(root, "sunbed", new Vector3(x + k * 1.3f, b.Height(x + k * 1.3f, z + 1.6f), z + 1.6f), 180f + b.Rand(-8f, 8f), 0f, 2f);
            }

            VinpearlHotel(b, root);

            // --- 케이블카: 섬 언덕 정류장 → 바다 위 철탑 셋 → 본토 쪽 (원작: 바다를 건너는 케이블카) ---
            var steel = Mats.Lit(new Color(0.75f, 0.76f, 0.78f), 0.5f, 0.6f);
            var cableMat = Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0.4f, 0.5f);
            float sx0 = -160f, sz0 = -80f;
            float sy0 = b.Height(sx0, sz0);
            var station = new Vector3(sx0, sy0, sz0);
            Mats.Prim(PrimitiveType.Cube, "CableStation", root, station + new Vector3(0f, 4f, 0f), new Vector3(16f, 8f, 12f), Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.2f), true);
            Mats.Prim(PrimitiveType.Cube, "CableStationRoof", root, station + new Vector3(0f, 8.4f, 0f), new Vector3(18f, 0.8f, 14f), Mats.Lit(new Color(0.75f, 0.2f, 0.18f), 0.3f), false);
            var end = new Vector3(-280f, 0f, 640f);
            var supports = new List<Vector3> { station + new Vector3(0f, 9f, 0f) };
            foreach (var (t, h) in new[] { (0.24f, 62f), (0.5f, 74f), (0.76f, 66f) })
            {
                var p = Vector3.Lerp(station, end, t);
                p.y = b.Map.BaseWaterY - 1f;
                CableTower(root, p, h, end - station, steel);
                supports.Add(p + new Vector3(0f, h, 0f));
            }
            supports.Add(new Vector3(end.x, 40f, end.z));
            var across = Vector3.Cross(Vector3.up, (end - station).normalized) * 3f;
            var gondola = Mats.Lit(new Color(0.85f, 0.2f, 0.18f), 0.4f);
            var glass = Mats.Lit(new Color(0.3f, 0.45f, 0.55f), 0.85f);
            for (int i = 0; i < supports.Count - 1; i++)
            {
                // 줄 처짐: 두 지지점 사이 가운데를 4m 낮춘 꺾인 줄 둘
                var a = supports[i];
                var c = supports[i + 1];
                var mid = (a + c) * 0.5f + Vector3.down * 4f;
                foreach (var off in new[] { across, -across })
                {
                    MapKit.Beam(root, a + off, mid + off, 0.12f, cableMat, false, "Cable");
                    MapKit.Beam(root, mid + off, c + off, 0.12f, cableMat, false, "Cable");
                }
                // 곤돌라 (갈 때·올 때 줄에 하나씩)
                foreach (var (f, side) in new[] { (0.3f, 1f), (0.68f, -1f) })
                {
                    var on = f < 0.5f ? Vector3.Lerp(a, mid, f * 2f) : Vector3.Lerp(mid, c, (f - 0.5f) * 2f);
                    on += across * side;
                    MapKit.Beam(root, on, on + Vector3.down * 3f, 0.1f, cableMat, false, "Hanger");
                    Mats.Prim(PrimitiveType.Cube, "Gondola", root, on + Vector3.down * 4.2f, new Vector3(2.2f, 2.3f, 2.2f), gondola, false, Quaternion.Euler(0f, MapKit.Face(station, end), 0f));
                    Mats.Prim(PrimitiveType.Cube, "GondolaGlass", root, on + Vector3.down * 4f, new Vector3(2.25f, 1f, 2.25f), glass, false, Quaternion.Euler(0f, MapKit.Face(station, end), 0f));
                }
            }
        }

        /// <summary>
        /// 빈펄 리조트 호텔 (사용자 참고 사진): 짙은 숲 산을 등진 넓은 흰 호텔. 가운데 높은 동 양옆으로 층이 계단처럼 낮아지는 날개,
        /// 촘촘한 흰 발코니 격자, 앞 정원의 야자수와 길쭉한 파란 수영장. 뒤로 혼째 섬의 초록 산.
        /// </summary>
        static void VinpearlHotel(CoastWorldBuilder b, Transform root)
        {
            float land = b.Map.LandY;
            var c = new Vector3(110f, 0f, -118f);
            var facade = HanRiverLandmarks.Facade(1, 4, new Color(0.62f, 0.72f, 0.78f), new Color(0.32f, 0.38f, 0.42f), new Color(0.98f, 0.98f, 0.96f), 0.42f, 0.2f, 951, out var night, 0.5f);
            var front = HanRiverLandmarks.Mat(Color.white, facade, night, new Color(1f, 0.86f, 0.62f), 0.4f, 0f, Vector2.one);
            var wall = Mats.Wall(new Color(0.97f, 0.97f, 0.95f), 0.2f);
            var roof = Mats.Tile(new Color(0.62f, 0.3f, 0.2f), 0.3f);
            // (가로 위치, 폭, 층수): 가운데가 가장 높다
            var blocks = new[] { (-46f, 18f, 4), (-30f, 16f, 6), (-14f, 16f, 8), (0f, 14f, 10), (14f, 16f, 8), (30f, 16f, 6), (46f, 18f, 4) };
            const float floorH = 3.2f, depth = 14f;
            foreach (var (dx, w, floors) in blocks)
            {
                float h = floors * floorH;
                float gy = b.Height(c.x + dx, c.z);
                var p = new Vector3(c.x + dx, gy, c.z + Mathf.Abs(dx) * 0.12f);
                Mats.Prim(PrimitiveType.Cube, "HotelBlock", root, p + Vector3.up * (h * 0.5f - 1.5f), new Vector3(w, h + 3f, depth), wall, true);
                var f = Mats.Prim(PrimitiveType.Quad, "HotelFront", root, p + new Vector3(0f, h * 0.5f, depth * 0.5f + 0.06f), new Vector3(w, h, 1f), front, false, Quaternion.Euler(0f, 180f, 0f));
                // 블록마다 층·칸 수가 달라 재질을 따로 (한 장 = 1층 × 4칸, 칸 하나 약 4m)
                f.GetComponent<MeshRenderer>().sharedMaterial = new Material(front) { mainTextureScale = new Vector2(w / 4f, floors) };
                for (int k = 1; k < floors; k++)
                    Mats.Prim(PrimitiveType.Cube, "Balcony", root, p + new Vector3(0f, k * floorH - 0.1f, depth * 0.5f + 0.6f), new Vector3(w, 0.18f, 1.2f), wall, false);
                MapKit.Place(root, "HotelRoof", CoastWorldBuilder.Gable(), roof, p + Vector3.up * h).transform.localScale = new Vector3(w + 1f, 2.6f, depth + 1.4f);
            }
            // 앞 정원: 길쭉한 수영장 + 야자수 줄
            var poolY = land + 0.05f;
            var poolZ = c.z + 22f;
            float py = b.Height(c.x, poolZ);
            Mats.Prim(PrimitiveType.Cube, "PoolDeck", root, new Vector3(c.x, py + 0.05f, poolZ), new Vector3(70f, 0.15f, 14f), Mats.TexturedBox("concrete", new Color(0.92f, 0.9f, 0.85f), new Vector2(70f, 14f), 3f, 0.3f), false);
            Mats.Prim(PrimitiveType.Cube, "Pool", root, new Vector3(c.x, py + 0.14f, poolZ), new Vector3(60f, 0.04f, 8f), Mats.Lit(new Color(0.12f, 0.62f, 0.82f), 0.95f), false);
            for (float x = c.x - 34f; x <= c.x + 34f; x += 8.5f)
                foreach (float zz in new[] { poolZ - 9f, poolZ + 9f })
                    KenneyProps.Spawn("Props/palm_a", root, new Vector3(x, b.Height(x, zz), zz), b.Rand(0f, 360f), b.Rand(8f, 11f));
            // 뒤 혼째 섬 산 (짙은 열대림)
            var jungle = MapKit.FarRock(new Color(0.18f, 0.32f, 0.16f), "grass", 22f, 0.8f, 0.05f);
            var rock = MapKit.FarRock(new Color(0.42f, 0.4f, 0.34f), "granite", 30f, 0.8f);
            MapKit.Place(root, "HonTre", MapKit.Mountain(420f, 140f, 190f, 51, 0.85f, 3), new[] { jungle, rock }, new Vector3(40f, land, -360f));
        }

        /// <summary>격자 철탑: 바닥 네모에서 꼭대기로 좁아지는 다리 넷, 층마다 가로 띠, 꼭대기 팔(줄을 거는 곳).</summary>
        static void CableTower(Transform root, Vector3 baseP, float h, Vector3 along, Material steel)
        {
            float yaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 Corner(float hb, int i)
            {
                float half = Mathf.Lerp(5f, 1.2f, hb / h);
                var local = new Vector3((i & 1) == 0 ? -half : half, hb, (i & 2) == 0 ? -half : half);
                return baseP + rot * local;
            }
            for (int i = 0; i < 4; i++) MapKit.Beam(root, Corner(0f, i), Corner(h, i), 0.6f, steel, false, "TowerLeg");
            foreach (float f in new[] { 0.22f, 0.45f, 0.68f, 0.9f })
            {
                float hb = h * f;
                MapKit.Beam(root, Corner(hb, 0), Corner(hb, 1), 0.3f, steel, false, "TowerBrace");
                MapKit.Beam(root, Corner(hb, 2), Corner(hb, 3), 0.3f, steel, false, "TowerBrace");
                MapKit.Beam(root, Corner(hb, 0), Corner(hb, 2), 0.3f, steel, false, "TowerBrace");
                MapKit.Beam(root, Corner(hb, 1), Corner(hb, 3), 0.3f, steel, false, "TowerBrace");
            }
            var top = baseP + Vector3.up * h;
            MapKit.Beam(root, top + rot * new Vector3(-4.5f, 0f, 0f), top + rot * new Vector3(4.5f, 0f, 0f), 0.8f, steel, false, "TowerArm");
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>빈펄 이야기 (Lv.27~). 첫 퀘스트는 그 낚시터 연락관(레 티 마이 지부장)이 준다.</summary>
        static QuestDef[] VinpearlChapter() => new[]
        {
            new QuestDef { Id = "vinpearl_1", Title = "흰 모래밭의 은빛 접시", Desc = "씬 짜오! 파도가 밀려드는 얕은 물에 폼파노가 몰려 다녀요. 갯지렁이나 새우로 세 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.27부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "pompano", Count = 3, RewardGold = 40000, RewardExp = 5000 },
            new QuestDef { Id = "vinpearl_2", Title = "산호 바위의 붉은 송어", Desc = "동쪽 갯바위 앞 산호엔 파란 점박이 붉은 고기, 코랄 트라우트가 산다네. 두 마리면 리조트 저녁 요리가 된다오.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "coraltrout", Count = 2, RewardGold = 45000, RewardExp = 5500 },
            new QuestDef { Id = "vinpearl_3", Title = "케이블카 아래의 흑상어?", Desc = "아침 F4, 낮 C6에 상어처럼 생긴 코비아 무리가 지나간다오. 1.2m가 넘는 놈을 걸면 잔교가 흔들릴 걸세!",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "cobia", MinLen = 120f, Count = 1, RewardGold = 90000, RewardExp = 9000 },
        };
    }
}
