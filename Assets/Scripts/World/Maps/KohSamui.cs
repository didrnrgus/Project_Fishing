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
        /// 코사무이 (Lv.32): 보풋 어촌 해변. 동쪽 바다 위 작은 섬(코 판)에 앉은 황금 대불과 둑길, 하얀 기단과 붉은 계단,
        /// 서쪽 물가의 할아버지·할머니 바위(힌 따·힌 야이), 야자수 해변과 리조트 (원작 2차 — 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec KohSamui() => new CoastSpec
        {
            Id = "kohsamui", MapName = "코사무이 · 타이만", ShortName = "코사무이", ShopName = "보풋 낚시점", UnionContact = "보풋 어촌의 찻차이 지부장",
            LandName = "보풋 해변", SeaName = "타이만", Seed = 55, SandTint = new Color(1.5f, 1.45f, 1.32f),
            WaterDeep = new Color(0.02f, 0.22f, 0.32f), WaterShallow = new Color(0.1f, 0.56f, 0.55f),
            Shores = new[] { new ShoreSeg(-999f, -20f, ShoreKind.Beach), new ShoreSeg(-20f, 30f, ShoreKind.Quay), new ShoreSeg(30f, 999f, ShoreKind.Beach) },
            NearDepth = 3f, DepthSlope = 0.05f, FlatWidth = 32f, CliffHeight = 18f, CliffWidth = 50f, RoadOffset = 18f, VillageDensity = 0.8f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Palm,
            Platforms = new[]
            {
                new PlatformSpec { Area = new Rect(-4f, -41f, 5f, 34f), Kind = PlatformKind.Pier, Name = "보풋 잔교" },
                new PlatformSpec { Area = new Rect(117f, -41f, 6f, 46f), Kind = PlatformKind.Pier, Name = "빅부다 둑길" },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(120f, 0f, 22f), Radius = 20f, Height = 3f, Walkable = true, Trees = true, Name = "코 판 (빅부다 섬)" } },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(-128f, 0f, -30f), Radius = 3.2f, Height = 6f, Name = "힌 따 (할아버지 바위)" },
                new StackSpec { Pos = new Vector3(-118f, 0f, -26f), Radius = 4.5f, Height = 3.5f, Name = "힌 야이 (할머니 바위)" },
            },
            Grid = new[,]
            {
                { S, W, S, S, S, S, R, R },
                { R, S, D, D, D, D, R, R },
                { R, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(8f, 1.1f, -47f), ShopPos = new Vector3(-24f, 1f, -54f),
            ShopRoof = new Color(0.15f, 0.48f, 0.5f),
            ShopOwner = Local("보풋 낚시점 쁘라섯", "보풋 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("찻차이 지부장", "한국 낚시 조합 코사무이 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(-1.5f, -12f, 0f, Local("보풋 어부 할머니", "새벽 장에 고기를 낸다", true), NpcAngler.Role.Angler, y: 1f),
                Npc(110f, -46f, 60f, Local("탁발 나온 스님", "대불 섬에서 왔다", false), NpcAngler.Role.Talker),
                Npc(-96f, -44f, 20f, Local("코코넛 줍는 원숭이 주인", "원숭이 이름은 롱", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("BOPHUT", new Vector3(4f, 6.5f, -62f), SignBlue), ("BIG BUDDHA", new Vector3(118f, 6f, -50f), SignGold) },
            FarIslands = new[] { new Vector4(-220f, 460f, 150f, 50f), new Vector4(240f, 520f, 200f, 60f) },
            Schools = () => new List<FishSchool>
            {
                School("독가시치 수초", 1, 0, 1, DayLong, "rabbitfish"),
                School("퀸피시 떼", 3, 1, 3, MornDay, "queenfish", "bluefintrevally"),
                School("산호 바위", 6, 1, 3, DayLong, "coraltrout", "goldensnapper", "spangledemperor"),
                School("삼치 떼", 2, 3, 5, DayLong, "narrowbarredmackerel", "mahimahi"),
                School("코비아 그늘", 5, 4, 6, DayLong, "cobia", "barracuda"),
                // 롱테일 참치: 새벽 E6, 저녁 B7
                Special("롱테일 참치 떼", "longtailtuna", (TimeBand.Dawn, 4, 5), (TimeBand.Evening, 1, 6)),
            },
            Extra = KohSamuiExtra,
        };

        /// <summary>코사무이만의 풍경: 섬 위 황금 대불(하얀 2단 기단, 붉은 계단, 나가 난간), 롱테일 보트.</summary>
        static void KohSamuiExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("KohSamui").transform;
            root.SetParent(b.Root, false);
            float water = b.Map.BaseWaterY;
            var white = Mats.Wall(new Color(0.94f, 0.92f, 0.88f), 0.25f);
            var red = Mats.Lit(new Color(0.72f, 0.18f, 0.14f), 0.3f);
            var gold = Mats.Lit(new Color(0.88f, 0.68f, 0.26f), 0.6f, 0.7f);

            // 섬 꼭대기에 2단 하얀 기단 → 황금 대불 (둑길·낚시터 쪽을 본다)
            var c = new Vector3(120f, 0f, 26f);
            c.y = b.Height(c.x, c.z) - 0.2f;
            Mats.Prim(PrimitiveType.Cube, "BuddhaBase1", root, c + new Vector3(0f, 1.5f, 0f), new Vector3(20f, 3f, 20f), white, true);
            Mats.Prim(PrimitiveType.Cube, "BuddhaBase2", root, c + new Vector3(0f, 4.2f, 0f), new Vector3(13f, 2.4f, 13f), white, true);
            foreach (float y in new[] { 3f, 5.4f })
                Mats.Prim(PrimitiveType.Cube, "BuddhaTrim", root, c + new Vector3(0f, y, 0f), new Vector3(y < 4f ? 20.3f : 13.3f, 0.25f, y < 4f ? 20.3f : 13.3f), red, false);
            // 앞 계단 (붉은 바닥, 양옆 금빛 나가 난간)
            Mats.Prim(PrimitiveType.Cube, "BuddhaStair", root, c + new Vector3(0f, 2.4f, -12f), new Vector3(5f, 0.4f, 9f), red, false, Quaternion.Euler(-28f, 0f, 0f));
            foreach (float sx in new[] { -2.8f, 2.8f })
            {
                MapKit.Beam(root, c + new Vector3(sx, 0.9f, -16.5f), c + new Vector3(sx, 5.6f, -7f), 0.45f, gold, false, "NagaRail");
                Mats.Prim(PrimitiveType.Sphere, "NagaHead", root, c + new Vector3(sx, 1.4f, -16.8f), new Vector3(0.9f, 1.4f, 0.9f), gold, false);
            }
            var at = c + new Vector3(0f, 5.4f, 0f);
            MapKit.Prop(root, "buddha_gold", at, MapKit.Face(at, new Vector3(20f, 0f, -45f)), 20f);

            // --- 사용자 참고 사진(코사무이): 앙통 해상공원의 숲 덮인 섬들 + 모래톱으로 이어진 낭유안 세 섬 ---
            var jungle = FarLand.Jungle(0.7f);
            var granite = MapKit.FarRock(new Color(0.55f, 0.5f, 0.44f), "granite", 20f, 0.8f);
            foreach (var (x, z, rx, h, seed) in new[] { (-260f, 420f, 90f, 60f, 1), (-140f, 470f, 60f, 48f, 2), (-330f, 520f, 120f, 75f, 3), (240f, 460f, 70f, 52f, 4), (330f, 380f, 50f, 34f, 5) })
            {
                FarLand.Place(root, "AngThong", new Vector3(x, water, z), rx, rx * 0.7f, h, seed + 40, jungle, 0f, 0.2f);
            }
            // 낭유안: 봉긋한 세 섬 사이를 흰 모래톱이 Y자로 잇는다
            var ny = new Vector3(60f, water, 330f);
            foreach (var (dx, dz, rx, h) in new[] { (0f, 0f, 34f, 30f), (-58f, 40f, 26f, 22f), (52f, 46f, 22f, 18f) })
                FarLand.Place(root, "NangYuan", ny + new Vector3(dx, 0f, dz), rx, rx * 0.8f, h, (int)(dx + 77), FarLand.Jungle(0.5f), 0f, 0.18f);
            var sand = Mats.Textured("sand", new Color(1f, 0.97f, 0.9f), 6f, 0.1f);
            foreach (var (tx, tz) in new[] { (-58f, 40f), (52f, 46f) })
            {
                var to = ny + new Vector3(tx, 0f, tz);
                var mid = (ny + to) * 0.5f;
                Mats.Prim(PrimitiveType.Cube, "SandBar", root, mid + Vector3.up * 0.05f, new Vector3(12f, 0.3f, (to - ny).magnitude), sand, false, Quaternion.Euler(0f, MapKit.Face(ny, to), 0f));
            }
            // 바위 해안 화강암 무더기 (사진: 둥근 화강암이 물가에 쌓였다)
            var graniteNear = Mats.Weathered("granite", new Color(0.66f, 0.58f, 0.5f), 4f, 0.25f, 1f, 0.3f, 0.2f);
            var rng = new System.Random(5555);
            for (int k = 0; k < 18; k++)
            {
                float x = 92f + (float)rng.NextDouble() * 50f, z = b.Map.ShoreAt(x) + (float)(rng.NextDouble() - 0.4) * 16f;
                float s = 2f + (float)rng.NextDouble() * 5f;
                var go = new GameObject("Granite");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, Mathf.Max(b.Height(x, z), water - 1.2f) + s * 0.2f, z);
                go.transform.localScale = new Vector3(s * 1.3f, s * 0.8f, s);
                go.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = Rocks.Mesh(rng.Next(8));
                go.AddComponent<MeshRenderer>().sharedMaterial = graniteNear;
            }

            // 롱테일 보트·어선
            foreach (var (x, z, yaw, id) in new[] { (5f, -20f, 95f, "longtail_a"), (-10f, -14f, 80f, "longtail_b"), (-60f, -33f, 40f, "longtail_a"), (60f, -34f, 140f, "longtail_b") })
                MapKit.Prop(root, id, new Vector3(x, water - 0.25f, z), yaw, 0f, 9.5f);
            b.PlaceBoats("Kenney/Watercraft/boat-fishing-small", 3, 9f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>코사무이 이야기 (Lv.32~). 첫 퀘스트는 그 낚시터 연락관(찻차이 지부장)이 준다.</summary>
        static QuestDef[] KohSamuiChapter() => new[]
        {
            new QuestDef { Id = "kohsamui_1", Title = "새벽 장의 독가시치", Desc = "싸왓디 크랍! 보풋 할머니가 새벽 장에 낼 독가시치가 모자라대요. 수초 근처에서 크릴로 다섯 마리! 등지느러미 가시는 조심하세요. (낚시터 목록 [동남아] 탭, Lv.32부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "rabbitfish", Count = 5, RewardGold = 55000, RewardExp = 6500 },
            new QuestDef { Id = "kohsamui_2", Title = "황금 대불 앞 참치 떼", Desc = "새벽 E6, 저녁 B7에 롱테일 참치 떼가 대불 섬 앞을 지나간다네. 한 마리면 충분하오.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "longtailtuna", Count = 1, RewardGold = 70000, RewardExp = 8000 },
            new QuestDef { Id = "kohsamui_3", Title = "먼바다의 바다꼬치", Desc = "새벽에 배를 타고 먼바다로 나가면 와후가 루어를 쫓아온다오. 시속 80km로 달리는 물고기를 잡아 보시게!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "wahoo", Count = 1, RewardGold = 110000, RewardExp = 10500 },
        };
    }
}
