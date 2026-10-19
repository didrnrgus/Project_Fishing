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
        /// 플라우 탱글 (Lv.39): 나만의 특별한 해양 별장. 맑은 물 위 넓은 나무 데크와 기와지붕 별장 두 채(파라솔·선베드·화분), 정글 덮인 바위섬과
        /// 바위기둥, 물이 맑아 바닥 산호가 비치는 바다. 동남아 마지막 낚시터 (원작 3차 영상 0:24~0:30).
        /// </summary>
        public static CoastSpec Tenggol() => new CoastSpec
        {
            Id = "tenggol", MapName = "플라우 탱글 · 말레이시아", ShortName = "탱글", ShopName = "수상 별장 낚시점", UnionContact = "수상 별장의 아이샤 지부장",
            LandName = "탱글 섬", SeaName = "산호초 바다", Seed = 60,
            WaterDeep = new Color(0.02f, 0.26f, 0.34f), WaterShallow = new Color(0.1f, 0.7f, 0.62f),
            Shores = new[] { new ShoreSeg(-999f, -40f, ShoreKind.Rock), new ShoreSeg(-40f, 30f, ShoreKind.Beach), new ShoreSeg(30f, 999f, ShoreKind.Rock) },
            NearDepth = 3f, DepthSlope = 0.07f, FlatWidth = 24f, CliffHeight = 34f, CliffWidth = 40f, RoadOffset = 0f, VillageDensity = 0.15f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Tropical, Features = CoastFeature.Mountains,
            Platforms = new[]
            {
                new PlatformSpec { Area = new Rect(-6f, -41f, 5f, 40f), Kind = PlatformKind.Pier, Name = "별장 잔교" },
                new PlatformSpec { Area = new Rect(-34f, -4f, 62f, 26f), Kind = PlatformKind.Pier, Name = "바다 별장 데크" },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(120f, 0f, 120f), Radius = 26f, Height = 22f, Trees = true, Name = "정글 바위섬" } },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(-110f, 0f, 60f), Radius = 5f, Height = 12f, Pines = true, Name = "쌍둥이 바위" },
                new StackSpec { Pos = new Vector3(-98f, 0f, 70f), Radius = 3.5f, Height = 8f, Name = "쌍둥이 바위" },
                new StackSpec { Pos = new Vector3(70f, 0f, 40f), Radius = 4f, Height = 6f, Name = "산호초 바위" },
            },
            Grid = new[,]
            {
                { R, S, S, S, S, S, R, R },
                { R, S, S, R, S, S, R, R },
                { R, D, D, D, D, D, D, R },
                { D, D, R, D, D, D, D, D },
                { D, D, D, D, D, D, R, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(-2f, 1.1f, -47f), ShopPos = new Vector3(-28f, 1f, -54f),
            ShopRoof = new Color(0.6f, 0.45f, 0.3f),
            ShopOwner = Local("수상 별장 낚시점 이브라힘", "수상 별장 낚시점", new Color(0.35f, 0.55f, 0.4f), false, true, 1),
            Npcs = new[]
            {
                Npc(12f, -47f, 200f, Union("아이샤 지부장", "한국 낚시 조합 탱글 연락소", true, false), NpcAngler.Role.Talker, "union"),
                Npc(20f, 20f, 0f, Local("별장 주인 다툭", "이 데크가 내 자랑", new Color(0.2f, 0.3f, 0.5f), false, true, 1), NpcAngler.Role.Angler, y: 1f),
                Npc(-26f, 18f, 10f, Local("해양 연구원", "산호 백화를 조사 중", new Color(0.9f, 0.9f, 0.85f), true, false, 0), NpcAngler.Role.Talker, y: 1f),
            },
            Signs = new[] { ("PULAU TENGGOL", new Vector3(-12f, 6.5f, -60f), SignGold) },
            FarIslands = new[] { new Vector4(-340f, 420f, 160f, 70f), new Vector4(320f, 500f, 120f, 40f) },
            Schools = () => new List<FishSchool>
            {
                School("산호 바위", 0, 1, 3, DayLong, "coraltrout", "goldensnapper", "spangledemperor"),
                School("범프헤드 산호초", 3, 1, 2, MornDay, "bumpheadparrot", "bluefintrevally"),
                School("삼치 떼", 4, 3, 5, DayLong, "narrowbarredmackerel", "barracuda"),
                School("나폴레옹 바위", 6, 4, 5, MornDay, "napoleonwrasse", "coraltrout"),
                School("GT 물살", 2, 3, 5, DayLong, "gianttrevally", "bluefintrevally"),
                // 대왕바리: 새벽 H2, 저녁 C4
                Special("대왕바리", "daewangbari", (TimeBand.Dawn, 7, 1), (TimeBand.Evening, 2, 3)),
            },
            Extra = TenggolExtra,
        };

        /// <summary>탱글만의 풍경: 바다 위 데크의 기와지붕 별장 두 채, 데크 위 파라솔·선베드·화분·난간.</summary>
        static void TenggolExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Tenggol").transform;
            root.SetParent(b.Root, false);
            float deck = b.Map.LandY + 0.34f;
            var white = Mats.Lit(new Color(0.96f, 0.95f, 0.92f), 0.15f);
            var wood = Mats.Lit(new Color(0.5f, 0.34f, 0.2f), 0.3f);
            var tile = Mats.Lit(new Color(0.62f, 0.28f, 0.18f), 0.3f);
            var glass = Mats.Lit(new Color(0.3f, 0.45f, 0.52f), 0.85f, 0.1f);

            // 별장 두 채 (데크 뒤쪽, 바다를 본다): 흰 벽 + 통유리 + 겹 박공 기와지붕
            foreach (float x in new[] { -20f, 14f })
            {
                var h = new GameObject("WaterVilla").transform;
                h.SetParent(root, false);
                h.localPosition = new Vector3(x, deck, 4f);
                Mats.Prim(PrimitiveType.Cube, "VillaWall", h, new Vector3(0f, 1.7f, 0f), new Vector3(11f, 3.4f, 7f), white, true);
                Mats.Prim(PrimitiveType.Cube, "VillaGlass", h, new Vector3(0f, 1.5f, 3.53f), new Vector3(8.5f, 2.4f, 0.05f), glass, false);
                MapKit.Place(h, "VillaRoof", CoastWorldBuilder.Gable(), tile, new Vector3(0f, 3.4f, 0f)).transform.localScale = new Vector3(13f, 2.6f, 9.5f);
                MapKit.Place(h, "VillaRoof2", CoastWorldBuilder.Gable(), tile, new Vector3(0f, 3.4f, 0.5f), 90f).transform.localScale = new Vector3(7.5f, 2.5f, 6f);
                // 처마 아래 테라스 기둥
                foreach (float dx in new[] { -5f, 5f })
                    Mats.Prim(PrimitiveType.Cylinder, "VillaPost", h, new Vector3(dx, 1.7f, 4.6f), new Vector3(0.22f, 1.7f, 0.22f), wood, false);
            }
            // 데크 앞쪽: 파라솔·선베드·화분
            foreach (float x in new[] { -28f, -12f, 6f, 22f })
            {
                MapKit.Prop(root, "parasol_thatch", new Vector3(x, deck, 15f), b.Rand(0f, 360f), 3.3f);
                MapKit.Prop(root, "sunbed", new Vector3(x - 1.2f, deck, 16.5f), 180f, 0f, 2f);
                MapKit.Prop(root, "sunbed", new Vector3(x + 1.2f, deck, 16.5f), 180f, 0f, 2f);
            }
            var pot = Mats.Lit(new Color(0.7f, 0.42f, 0.28f), 0.2f);
            foreach (float x in new[] { -33f, -3f, 27f })
            {
                Mats.Prim(PrimitiveType.Cylinder, "Planter", root, new Vector3(x, deck + 0.4f, 9.5f), new Vector3(0.9f, 0.4f, 0.9f), pot, true);
                KenneyProps.SpawnBush(root, new Vector3(x, deck + 0.8f, 9.5f), b.Rand(0f, 360f), 1.3f);
            }
            // 데크 가장자리 낮은 난간 (바다 쪽은 비워서 던질 수 있게, 양옆만)
            foreach (float x in new[] { -33.8f, 27.8f })
                Mats.Prim(PrimitiveType.Cube, "DeckRail", root, new Vector3(x, deck + 0.5f, 9f), new Vector3(0.1f, 0.8f, 26f), wood, false);
            // 데크 밤 조명
            foreach (float x in new[] { -30f, -2f, 24f })
                b.AddLamp(new Vector3(x, deck + 3f, 8f), new Color(1f, 0.8f, 0.55f), 12f, 1.1f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>플라우 탱글 이야기 (Lv.39~). 첫 퀘스트는 그 낚시터 연락관(아이샤 지부장)이 준다.</summary>
        static QuestDef[] TenggolChapter() => new[]
        {
            new QuestDef { Id = "tenggol_1", Title = "별장 앞 산호초", Desc = "슬라맛 다탕! 데크 아래 산호초엔 큰 코랄 트라우트가 숨어 있어요. 70cm가 넘는 놈으로 한 마리 부탁해요. (낚시터 목록 [동남아] 탭, Lv.39부터)",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "coraltrout", MinLen = 70f, Count = 1, RewardGold = 70000, RewardExp = 8000 },
            new QuestDef { Id = "tenggol_2", Title = "산호초의 황제", Desc = "입술이 두툼하고 이마가 솟은 나폴레옹피시가 아침·낮 G5 바위에 나타난다네. 한 마리를 낚으면 연구원이 크기를 재고 싶다는군.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "napoleonwrasse", Count = 1, RewardGold = 110000, RewardExp = 11000 },
            new QuestDef { Id = "tenggol_3", Title = "산호초 너머의 송곳니", Desc = "배를 타고 먼바다로 나가 새벽이나 아침에 도그투스 참치를 노려 보시오. 동남아 바다의 마지막 시험이라네. 이걸 낚으면 자네는 동남아의 강태공일세!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "dogtoothtuna", Count = 1, RewardGold = 160000, RewardExp = 15000, RewardKeepNet = 3 },
        };
    }
}
