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
        /// 돈콘 (Lv.28): 메콩강 4천 개 섬 지대(시판돈)의 수상 가옥 마을. 물가에 기둥을 박은 낡은 회색 판잣집, 콘크리트 물탱크 탑, 나무 선착장과 쪽배,
        /// 건너편 초록 산, 동쪽엔 바위 사이로 흰 물살이 이는 콘파펭 여울 (원작 1차 영상 0:30~0:38).
        /// </summary>
        public static CoastSpec DonKhone() => new CoastSpec
        {
            Id = "donkhone", MapName = "돈콘 · 라오스", ShortName = "돈콘", ShopName = "돈콘 수상 낚시점", UnionContact = "나무 선착장의 분미 지부장",
            LandName = "수상 마을", SeaName = "메콩강", Seed = 52, River = true, FarShoreZ = 160f,
            WaterDeep = new Color(0.12f, 0.17f, 0.1f), WaterShallow = new Color(0.36f, 0.34f, 0.2f),
            Shores = new[] { new ShoreSeg(-999f, -30f, ShoreKind.Beach), new ShoreSeg(-30f, 30f, ShoreKind.Quay), new ShoreSeg(30f, 999f, ShoreKind.Beach) },
            NearDepth = 2.5f, DepthSlope = 0.04f, FlatWidth = 30f, CliffHeight = 8f, CliffWidth = 50f, RoadOffset = 18f, VillageDensity = 1f,
            Houses = HouseStyle.Stilt, TreeKind = TreeStyle.Tropical, Features = CoastFeature.Mountains,
            Platforms = new[] { new PlatformSpec { Area = new Rect(-12f, -41f, 4f, 30f), Kind = PlatformKind.Pier, Name = "나무 선착장" } },
            Islets = new[]
            {
                new IsletSpec { Center = new Vector3(-118f, 0f, 70f), Radius = 22f, Height = 3f, Walkable = true, Trees = true, Name = "돈뎃 작은 섬" },
                new IsletSpec { Center = new Vector3(70f, 0f, 120f), Radius = 12f, Height = 2.5f, Trees = true, Name = "야자 섬" },
            },
            // 콘파펭 여울: 강물이 바위 사이로 쏟아진다
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(130f, 0f, 40f), Radius = 3.5f, Height = 2.5f, Name = "콘파펭 여울 바위" },
                new StackSpec { Pos = new Vector3(146f, 0f, 62f), Radius = 4.5f, Height = 3f, Name = "콘파펭 여울 바위" },
                new StackSpec { Pos = new Vector3(118f, 0f, 78f), Radius = 3f, Height = 2f, Name = "콘파펭 여울 바위" },
                new StackSpec { Pos = new Vector3(160f, 0f, 92f), Radius = 5f, Height = 3.5f, Name = "콘파펭 여울 바위" },
                new StackSpec { Pos = new Vector3(138f, 0f, 110f), Radius = 3.5f, Height = 2.2f, Name = "콘파펭 여울 바위" },
            },
            Areas = new[] { (new Rect(100f, 20f, 100f, 110f), "콘파펭 여울") },
            Grid = new[,]
            {
                { W, W, S, S, S, S, R, R },
                { W, D, D, D, D, S, R, R },
                { S, D, D, D, D, D, R, R },
                { D, D, D, D, D, D, R, R },
                { D, D, D, D, D, D, D, R },
                { S, W, S, S, S, W, S, S },
                { W, W, W, W, W, W, W, W },
                { W, W, W, W, W, W, W, W },
            },
            Spawn = new Vector3(4f, 1.1f, -47f), ShopPos = new Vector3(-22f, 1f, -54f),
            ShopRoof = new Color(0.55f, 0.6f, 0.63f),
            ShopOwner = Local("돈콘 수상 낚시점 캄펑", "돈콘 수상 낚시점"),
            Npcs = new[]
            {
                Npc(20f, -47f, 200f, Union("분미 지부장", "한국 낚시 조합 메콩 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(-10f, -14f, 10f, Local("삿갓 쓴 할아버지", "투망 60년", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-40f, -48f, 120f, Local("사롱 입은 아주머니", "쌀국수 가게 주인", true), NpcAngler.Role.Talker),
                Npc(-118f, 70f, 90f, Local("배낭여행객", "해먹에서 막 일어났다", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("DON KHONE · 돈콘", new Vector3(18f, 6.5f, -62f), SignGold) },
            Reserved = new[] { new Rect(40f, -75f, 24f, 24f) },
            Schools = () => new List<FishSchool>
            {
                School("바브 떼", 2, 0, 2, DayLong, "hampalabarb", "silverbarb"),
                School("가물치 수초", 0, 0, 1, MornDay, "stripedsnakehead", "giantsnakehead"),
                School("월라고 웅덩이", 3, 2, 4, EveNight, "wallago", "redtailcatfish"),
                School("황금 잉어 여울목", 5, 1, 3, DayLong, "julliencarp", "giantbarb"),
                School("메콩의 거인", 2, 3, 4, EveNight, "mekonggiantcatfish", "giantpangasius"),
                // 구치: 저녁 G3, 밤 H4 (여울 바위 밑)
                Special("구치 어군", "goonch", (TimeBand.Evening, 6, 2), (TimeBand.Night, 7, 3)),
            },
            Extra = DonKhoneExtra,
        };

        /// <summary>돈콘만의 풍경: 물에 기둥을 박은 수상 가옥 줄, 물탱크 탑, 쪽배, 여울의 흰 물살.</summary>
        static void DonKhoneExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("DonKhone").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, water = b.Map.BaseWaterY;

            // --- 수상 가옥: 서쪽 물가에 물로 나앉은 판잣집 다섯 채 (기둥이 물에 잠긴다) ---
            for (int i = 0; i < 5; i++)
            {
                float x = -40f - i * b.Rand(14f, 18f);
                float z = b.Map.ShoreAt(x) + b.Rand(1f, 4f);
                b.PlaceHouse(new Vector3(x, water + 0.2f, z), b.Rand(-8f, 8f));
            }
            // 동쪽 물가에도 셋
            for (int i = 0; i < 3; i++)
            {
                float x = 50f + i * b.Rand(15f, 19f);
                float z = b.Map.ShoreAt(x) + b.Rand(1f, 3f);
                b.PlaceHouse(new Vector3(x, water + 0.2f, z), b.Rand(-8f, 8f));
            }

            // --- 물탱크 탑: 콘크리트 기둥 넷 + 가로보 + 원통 물탱크 (하늘색 띠) ---
            var tankAt = new Vector3(52f, b.Height(52f, -63f), -63f);
            var concrete = Mats.Textured("concrete", new Color(0.82f, 0.8f, 0.76f), 1f, 0.15f);
            var stain = Mats.Lit(new Color(0.48f, 0.47f, 0.43f), 0.1f);
            const float th = 13f;
            foreach (var (dx, dz) in new[] { (-2.2f, -2.2f), (2.2f, -2.2f), (-2.2f, 2.2f), (2.2f, 2.2f) })
                Mats.Prim(PrimitiveType.Cube, "TankLeg", root, tankAt + new Vector3(dx, th * 0.5f, dz), new Vector3(0.45f, th, 0.45f), concrete, true);
            foreach (float y in new[] { 4.5f, 9f })
            {
                Mats.Prim(PrimitiveType.Cube, "TankBrace", root, tankAt + new Vector3(0f, y, -2.2f), new Vector3(4.8f, 0.3f, 0.3f), concrete, false);
                Mats.Prim(PrimitiveType.Cube, "TankBrace", root, tankAt + new Vector3(0f, y, 2.2f), new Vector3(4.8f, 0.3f, 0.3f), concrete, false);
            }
            Mats.Prim(PrimitiveType.Cylinder, "Tank", root, tankAt + new Vector3(0f, th + 2f, 0f), new Vector3(5.6f, 2f, 5.6f), concrete, false);
            Mats.Prim(PrimitiveType.Cylinder, "TankBand", root, tankAt + new Vector3(0f, th + 2.9f, 0f), new Vector3(5.66f, 0.35f, 5.66f), Mats.Lit(new Color(0.35f, 0.62f, 0.78f), 0.3f), false);
            Mats.Prim(PrimitiveType.Cylinder, "TankStain", root, tankAt + new Vector3(0f, th + 0.4f, 0f), new Vector3(5.62f, 0.4f, 5.62f), stain, false);
            Mats.Prim(PrimitiveType.Cylinder, "TankLid", root, tankAt + new Vector3(0f, th + 4.1f, 0f), new Vector3(5.8f, 0.12f, 5.8f), stain, false);
            for (int k = 0; k < 9; k++) // 사다리
                Mats.Prim(PrimitiveType.Cube, "TankRung", root, tankAt + new Vector3(2.45f, 1.2f + k * 1.3f, 0f), new Vector3(0.06f, 0.06f, 0.7f), stain, false);

            // --- 쪽배: 선착장 옆과 물가 (Kenney 나룻배) ---
            foreach (var (x, z, yaw) in new[] { (-16f, -20f, 85f), (-6f, -30f, 100f), (-70f, -34f, 60f), (32f, -36f, 120f) })
                KenneyProps.Spawn("Kenney/Watercraft/boat-row-small", root, new Vector3(x, water - 0.2f, z), yaw, 0f, 5f);
            MapKit.Prop(root, "longtail_b", new Vector3(-95f, water - 0.25f, -28f), 75f, 0f, 9f);

            // --- 여울: 바위 둘레·사이 흰 물살 (반투명 원판 겹) ---
            var foam = new List<Vector4>();
            var rng = new System.Random(52);
            float Rf(float a, float c) => a + (float)rng.NextDouble() * (c - a);
            foreach (var st in new[] { (130f, 40f, 3.5f), (146f, 62f, 4.5f), (118f, 78f, 3f), (160f, 92f, 5f), (138f, 110f, 3.5f) })
            {
                for (int k = 0; k < 9; k++)
                {
                    float a = Rf(0f, Mathf.PI * 2f), d = st.Item3 + Rf(0.5f, 4f);
                    foam.Add(new Vector4(st.Item1 + Mathf.Cos(a) * d, st.Item2 + Mathf.Sin(a) * d - Rf(0f, 3f), Rf(0.8f, 2.4f), water + 0.1f));
                }
            }
            for (int k = 0; k < 90; k++) foam.Add(new Vector4(Rf(104f, 200f), Rf(26f, 128f), Rf(0.8f, 2.6f), water + 0.08f + Rf(0f, 0.04f)));
            MapKit.Place(root, "RapidFoam", MapKit.Discs(foam, 9), Mats.Transparent(new Color(0.96f, 0.98f, 0.97f, 0.75f), 0.6f), Vector3.zero);
            // 여울 바위: 물살 사이로 고개를 내민 검은 바위 덩어리들
            var wetRock = Mats.Textured("rock", new Color(0.42f, 0.4f, 0.37f), 1f, 0.55f);
            for (int k = 0; k < 26; k++)
                b.Props.Boulder(new Vector3(Rf(104f, 200f), water + Rf(-0.6f, 0.5f), Rf(26f, 128f)), Rf(1.6f, 4.2f), wetRock);

            // 물가 해먹 (야자 사이)
            var cloth = Mats.Lit(new Color(0.75f, 0.25f, 0.2f), 0.2f);
            Mats.Prim(PrimitiveType.Cube, "Hammock", root, new Vector3(-112f, b.Height(-112f, 66f) + 0.9f, 66f), new Vector3(2.4f, 0.08f, 0.9f), cloth, false, Quaternion.Euler(0f, 30f, 0f));
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>돈콘 이야기 (Lv.28~). 첫 퀘스트는 그 낚시터 연락관(분미 지부장)이 준다.</summary>
        static QuestDef[] DonKhoneChapter() => new[]
        {
            new QuestDef { Id = "donkhone_1", Title = "메콩의 붉은 지느러미", Desc = "싸바이디! 하팔라 바브는 지느러미가 붉고 성질이 급해요. 낮에 루어로 세 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.28부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "hampalabarb", Count = 3, RewardGold = 40000, RewardExp = 5000 },
            new QuestDef { Id = "donkhone_2", Title = "여울목의 황금 잉어", Desc = "줄리엔 황금 잉어는 메콩에서도 귀한 손님이라오. 동쪽 여울목에 옥수수나 떡밥을 넣어 보시오.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "julliencarp", Count = 1, RewardGold = 55000, RewardExp = 6500 },
            new QuestDef { Id = "donkhone_3", Title = "메콩의 왕", Desc = "이빨도 없이 물이끼를 먹는 세상에서 가장 큰 민물고기, 메콩 대형 메기. 저녁과 밤에 D4 깊은 골로 올라온다오. 배째로 끌려가지 마시게!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "mekonggiantcatfish", Count = 1, RewardGold = 110000, RewardExp = 10000, RewardKeepNet = 3 },
        };
    }
}
