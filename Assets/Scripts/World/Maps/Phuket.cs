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
        /// 푸켓 (Lv.30): 찰롱 부두. 서쪽 나깟 언덕 위 하얀 대불(높이 45m), 바다엔 팡아만처럼 솟은 석회암 섬과 바늘 같은 바위(제임스 본드 섬),
        /// 뱃머리에 꽃줄을 단 색색의 롱테일 보트. 먼바다엔 돛새치·청새치 (원작 2차 — 영상이 없어 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec Phuket() => new CoastSpec
        {
            Id = "phuket", MapName = "푸켓 · 안다만해", ShortName = "푸켓", ShopName = "찰롱 낚시점", UnionContact = "찰롱 부두의 솜차이 지부장",
            LandName = "찰롱 부두", SeaName = "안다만해", Seed = 53,
            WaterDeep = new Color(0.02f, 0.2f, 0.3f), WaterShallow = new Color(0.08f, 0.52f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -50f, ShoreKind.Beach), new ShoreSeg(-50f, 40f, ShoreKind.Quay), new ShoreSeg(40f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.06f, FlatWidth = 28f, CliffHeight = 26f, CliffWidth = 45f, RoadOffset = 16f, VillageDensity = 0.9f,
            Houses = HouseStyle.Thai, TreeKind = TreeStyle.Tropical, Features = CoastFeature.Karst,
            Platforms = new[] { new PlatformSpec { Area = new Rect(-8f, -41f, 6f, 58f), Kind = PlatformKind.Pier, Name = "찰롱 잔교" } },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(96f, 0f, 108f), Radius = 4f, Height = 22f, Pines = true, Name = "제임스 본드 섬" },
                new StackSpec { Pos = new Vector3(-132f, 0f, 92f), Radius = 6f, Height = 16f, Pines = true, Name = "코 판닛 바위" },
                new StackSpec { Pos = new Vector3(-60f, 0f, 196f), Radius = 20f, Height = 58f, Pines = true, Name = "팡아만 석회암 섬" },
                new StackSpec { Pos = new Vector3(150f, 0f, 176f), Radius = 24f, Height = 70f, Pines = true, Name = "팡아만 석회암 섬" },
                new StackSpec { Pos = new Vector3(-176f, 0f, 214f), Radius = 22f, Height = 62f, Pines = true, Name = "팡아만 석회암 섬" },
            },
            Grid = new[,]
            {
                { S, S, S, W, S, S, S, S },
                { R, S, D, D, D, D, S, R },
                { R, D, D, D, D, D, D, R },
                { D, D, R, D, D, D, R, D },
                { D, D, D, D, D, D, R, D },
                { R, D, D, D, D, D, D, D },
                { R, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(8f, 1.1f, -47f), ShopPos = new Vector3(-24f, 1f, -54f),
            ShopRoof = new Color(0.82f, 0.38f, 0.16f),
            ShopOwner = Local("찰롱 낚시점 위라 사장", "찰롱 낚시점", new Color(0.4f, 0.65f, 0.85f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("솜차이 지부장", "한국 낚시 조합 푸켓 지부", false, false), NpcAngler.Role.Talker, "union"),
                Npc(-5f, 12f, 0f, Local("롱테일 선장 분", "먼바다 돛새치 투어", new Color(0.95f, 0.65f, 0.2f), false, true, 1), NpcAngler.Role.Angler, y: 1f),
                Npc(-36f, -48f, 140f, Local("팟타이 노점 아주머니", "매운맛 조절 가능", new Color(0.85f, 0.3f, 0.35f), true, false, -1), NpcAngler.Role.Talker),
                Npc(70f, -44f, 0f, Local("게임 낚시 유튜버", "구독과 좋아요", new Color(0.2f, 0.2f, 0.25f), false, false, 1), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("CHALONG PIER", new Vector3(10f, 6.5f, -62f), SignBlue) },
            FarIslands = new[] { new Vector4(-280f, 420f, 120f, 40f), new Vector4(300f, 460f, 140f, 44f) },
            // 대불 언덕
            Reserved = new[] { new Rect(-150f, -150f, 60f, 60f) },
            Schools = () => new List<FishSchool>
            {
                School("산호 바위", 0, 1, 3, DayLong, "coraltrout", "spangledemperor", "goldensnapper"),
                School("퀸피시 떼", 3, 1, 3, MornDay, "queenfish", "bluefintrevally"),
                School("삼치 떼", 4, 3, 5, DayLong, "narrowbarredmackerel", "mahimahi"),
                School("코비아 바위 그늘", 6, 3, 5, DayLong, "cobia", "gianttrevally"),
                School("맹그로브잭 굴", 7, 1, 2, EveNight, "mangrovejack", "barracuda"),
                // 황소상어: 저녁 B6, 밤 F2 (얕은 물까지 들어온다)
                Special("황소상어", "bullshark", (TimeBand.Evening, 1, 5), (TimeBand.Night, 5, 1)),
            },
            Extra = PhuketExtra,
        };

        /// <summary>푸켓만의 풍경: 언덕 위 하얀 대불, 팡아만 석회암 섬, 롱테일 보트.</summary>
        static void PhuketExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Phuket").transform;
            root.SetParent(b.Root, false);
            float water = b.Map.BaseWaterY;

            // --- 나깟 언덕 위 하얀 대불: 낚시터를 내려다보게 돌린다 ---
            var buddhaAt = new Vector3(-120f, 0f, -122f);
            buddhaAt.y = b.Height(buddhaAt.x, buddhaAt.z) - 0.3f;
            MapKit.Prop(root, "buddha_white", buddhaAt, MapKit.Face(buddhaAt, new Vector3(10f, 0f, 40f)), 40f);
            // 대불 앞 하얀 대리석 마당
            Mats.Prim(PrimitiveType.Cylinder, "BuddhaTerrace", root, buddhaAt + new Vector3(0f, -0.6f, 0f), new Vector3(42f, 1.2f, 42f), Mats.Lit(new Color(0.9f, 0.88f, 0.84f), 0.3f), true);

            // 팡아만 석회암 섬: 가까운 것은 Stacks, 수평선은 CoastFeature.Karst

            // --- 롱테일 보트: 잔교 옆에 매어 둔 것과 물가, 먼 바다 ---
            foreach (var (x, z, yaw, id) in new[] { (1.5f, -12f, 98f, "longtail_a"), (-12f, 0f, 82f, "longtail_b"), (1.5f, 4f, 94f, "longtail_b"), (-80f, -34f, 30f, "longtail_a"), (-100f, -33f, 50f, "longtail_b"), (90f, -34f, 150f, "longtail_a") })
                MapKit.Prop(root, id, new Vector3(x, water - 0.25f, z), yaw, 0f, 9.5f);
            b.PlaceBoats("Props/longtail_a", 3, 9.5f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>푸켓 이야기 (Lv.30~). 첫 퀘스트는 그 낚시터 연락관(솜차이 지부장)이 준다.</summary>
        static QuestDef[] PhuketChapter() => new[]
        {
            new QuestDef { Id = "phuket_1", Title = "안다만의 삼치", Desc = "싸왓디 크랍! 언덕 위 대불님께 인사는 하셨지요? 안다만 바다 삼치는 1m가 우습답니다. 두 마리 부탁해요. (낚시터 목록 [동남아] 탭, Lv.30부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "narrowbarredmackerel", Count = 2, RewardGold = 50000, RewardExp = 6000 },
            new QuestDef { Id = "phuket_2", Title = "저녁의 회색 그림자", Desc = "저녁 B6, 밤 F2에 황소상어가 얕은 물까지 들어온다네. 잘린 미끼나 오징어로 한 마리! 손가락 조심하게.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "bullshark", Count = 1, RewardGold = 80000, RewardExp = 8000 },
            new QuestDef { Id = "phuket_3", Title = "돛새치의 춤", Desc = "분 선장이 먼바다에서 돛새치 떼를 봤다는군. 배를 타고 먼바다로 나가 아침이나 낮에 루어를 끌어 보게. 물 위로 춤추듯 뛰어오를 걸세!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "sailfish", Count = 1, RewardGold = 130000, RewardExp = 12000 },
        };
    }
}
