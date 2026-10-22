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
        /// 크레타 (Lv.30): 하니아의 베네치아 항구. 동쪽에서 길게 휘어 들어오는 방파제 끝의 돌 등대(팔각 탑, 이집트 등대), 물가를 따라 선 파스텔 집과
        /// 둥근 지붕의 옛 모스크(야니체리 모스크), 돌로 쌓은 부두, 뒤로 봄까지 눈을 인 하얀 산(레프카 오리) (원작 지중해 — 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec Crete() => new CoastSpec
        {
            Id = "crete", MapName = "크레타 · 하니아", ShortName = "크레타", ShopName = "하니아 항구 낚시점", UnionContact = "베네치아 항구의 디미트리스 지부장",
            LandName = "베네치아 항구", SeaName = "크레타 바다", Seed = 61, SandTint = new Color(1.3f, 1.26f, 1.16f), StoneQuay = true, StoneTint = new Color(0.82f, 0.74f, 0.6f),
            WaterDeep = new Color(0.02f, 0.16f, 0.3f), WaterShallow = new Color(0.08f, 0.42f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -120f, ShoreKind.Rock), new ShoreSeg(-120f, 90f, ShoreKind.Quay), new ShoreSeg(90f, 999f, ShoreKind.Rock) },
            NearDepth = 4f, DepthSlope = 0.07f, FlatWidth = 26f, CliffHeight = 14f, CliffWidth = 50f, RoadOffset = 0f, VillageDensity = 1.5f,
            Houses = HouseStyle.Venetian, TreeKind = TreeStyle.Mediterranean,
            Breakwaters = new[] { new BreakwaterSpec { Name = "베네치아 방파제", Stem = new Rect(80f, -40f, 7f, 64f), Arm = new Rect(26f, 20f, 61f, 6f), Light = new Color(0f, 0f, 0f, 0f) } },
            Grid = new[,]
            {
                { W, S, S, S, S, S, R, R },
                { R, S, D, D, D, R, R, R },
                { R, D, D, D, D, D, R, D },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, R, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(6f, 1.1f, -46f), ShopPos = new Vector3(-24f, 1f, -50f),
            ShopRoof = new Color(0.72f, 0.32f, 0.2f),
            ShopOwner = Local("하니아 낚시점 코스타스", "하니아 항구 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -46f, 200f, Union("디미트리스 지부장", "한국 낚시 조합 크레타 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(40f, 23f, 270f, Local("등대 앞 낚시꾼 니코스", "덴텍스만 30년", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-60f, -46f, 160f, Local("타베르나 주인 엘레니", "오늘의 생선 요리", true), NpcAngler.Role.Talker),
                Npc(83f, -10f, 90f, Local("방파제 산책객", "해 질 녘 사진을 찍는다", true), NpcAngler.Role.Angler, y: 1f),
            },
            Signs = new[] { ("CHANIA · ΧΑΝΙΑ", new Vector3(-2f, 6.5f, -60f), SignBlue) },
            // 모스크 자리
            Reserved = new[] { new Rect(-96f, -62f, 32f, 22f) },
            Schools = () => new List<FishSchool>
            {
                School("도미 떼", 2, 0, 2, DayLong, "whiteseabream", "twobandedbream", "saddledbream"),
                School("살레마 수초", 0, 0, 1, MornDay, "salema", "paintedcomber", "rainbowwrasse"),
                School("붉은촉수 모래밭", 4, 1, 3, MornDay, "redmullet", "pandora", "bogue"),
                School("블루피시 떼", 3, 3, 5, DayLong, "bluefish", "atlanticbonito", "littletunny", "greateramberjack"),
                School("쏨뱅이 바위", 6, 1, 3, EveNight, "redscorpionfish", "europeanconger", "moray"),
                School("그루퍼 굴", 6, 4, 5, DawnEve, "duskygrouper", "redporgy"),
                // 덴텍스: 새벽 B3, 아침 A4 (등대 앞 바위)
                Special("덴텍스 어군", "dentex", (TimeBand.Dawn, 1, 2), (TimeBand.Morning, 0, 3)),
            },
            Extra = CreteExtra,
        };

        /// <summary>크레타만의 풍경: 방파제 끝 돌 등대, 둥근 지붕 모스크, 뒤로 눈 덮인 하얀 산.</summary>
        static void CreteExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Crete").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY;
            var stone = Mats.Textured("rock", new Color(0.86f, 0.78f, 0.64f), 0.5f, 0.12f);
            var stoneLight = Mats.Wall(new Color(0.86f, 0.8f, 0.68f), 0.15f);

            // --- 이집트 등대: 사각 기단 → 팔각 몸통(위로 좁아짐) → 둘레 난간 → 첨탑 같은 등롱 ---
            var lh = new Vector3(30f, land, 23f);
            Mats.Prim(PrimitiveType.Cube, "LighthouseBase", root, lh + new Vector3(0f, 2f, 0f), new Vector3(5.5f, 4f, 5.5f), stone, true);
            MapKit.Place(root, "LighthouseShaft", MapKit.Lathe(new[] { new Vector2(2.4f, 0f), new Vector2(2.1f, 9f), new Vector2(1.9f, 13f) }, 8, 2f, 1f, false), stoneLight, lh + new Vector3(0f, 4f, 0f), 22.5f);
            MapKit.Place(root, "LighthouseGallery", MapKit.Lathe(new[] { new Vector2(1.9f, 0f), new Vector2(2.6f, 0.4f), new Vector2(2.6f, 0.9f), new Vector2(1.6f, 1f) }, 8), stoneLight, lh + new Vector3(0f, 17f, 0f), 22.5f);
            MapKit.Place(root, "LighthouseLantern", MapKit.Lathe(new[] { new Vector2(1.4f, 0f), new Vector2(1.4f, 2.4f), new Vector2(1.6f, 2.6f), new Vector2(0.5f, 4f), new Vector2(0.05f, 5.2f) }, 8), stoneLight, lh + new Vector3(0f, 18f, 0f), 22.5f);
            var lamp = Mats.Emissive(new Color(1f, 0.9f, 0.6f), 2f);
            var bulb = Mats.Prim(PrimitiveType.Sphere, "LighthouseLamp", root, lh + new Vector3(0f, 19.4f, 0f), Vector3.one * 1.6f, lamp, false);
            b.AddBulb(bulb.GetComponent<Renderer>());
            b.AddLamp(lh + new Vector3(0f, 19.4f, 0f), new Color(1f, 0.9f, 0.6f), 30f, 2f);

            // --- 야니체리 모스크 (사용자 참고 사진): 황토빛 사암 네모 건물 + 붉은 테라코타 큰 돔 + 앞 회랑의 작은 테라코타 돔 다섯 + 야자수 둘 ---
            var mq = new Vector3(-80f, land, -52f);
            var sandstone = Mats.Carved(new Color(0.84f, 0.72f, 0.54f), 0.12f, 0.15f);
            var terracotta = Mats.Weathered("plaster", new Color(0.74f, 0.42f, 0.3f), 2f, 0.2f, 0f, 0.35f, 0.05f, null, 0.6f, 0f, null, 0.5f);
            stoneLight = sandstone;
            Mats.Prim(PrimitiveType.Cube, "MosqueHall", root, mq + new Vector3(0f, 4f, 0f), new Vector3(14f, 8f, 14f), stoneLight, true);
            Mats.Prim(PrimitiveType.Sphere, "MosqueDome", root, mq + new Vector3(0f, 8.4f, 0f), new Vector3(12.5f, 9f, 12.5f), terracotta, false);
            Mats.Prim(PrimitiveType.Cylinder, "MosqueDrum", root, mq + new Vector3(0f, 8.6f, 0f), new Vector3(12.8f, 0.6f, 12.8f), stone, false);
            Mats.Prim(PrimitiveType.Cube, "MosquePorch", root, mq + new Vector3(0f, 2.6f, 9.5f), new Vector3(16f, 5.2f, 5f), stoneLight, true);
            for (int k = -2; k <= 2; k++)
            {
                Mats.Prim(PrimitiveType.Sphere, "PorchDome", root, mq + new Vector3(k * 3.1f, 5.3f, 9.5f), new Vector3(2.8f, 2.2f, 2.8f), terracotta, false);
                Mats.Prim(PrimitiveType.Cube, "PorchArch", root, mq + new Vector3(k * 3.1f, 1.7f, 12.02f), new Vector3(1.8f, 3.2f, 0.05f), Mats.Lit(new Color(0.15f, 0.13f, 0.12f)), false);
            }
            foreach (float sx in new[] { -11f, 11f })
                KenneyProps.Spawn("Props/palm_a", root, mq + new Vector3(sx, 0f, 12f), b.Rand(0f, 360f), 9f);

            // --- 레프카 오리 (하얀 산): 마을 뒤로 눈을 인 산줄기 ---
            var mountain = MapKit.Mountain(620f, 140f, 210f, 61, 0.6f, 4);
            MapKit.Place(root, "LefkaOri", mountain, new[] { MapKit.FarRock(new Color(0.62f, 0.58f, 0.52f), "cliff", 45f), MapKit.FarRock(new Color(0.95f, 0.96f, 0.98f), "snow", 30f, 0.5f, 0.3f) }, new Vector3(-60f, land, -560f));
            MapKit.Place(root, "Foothills", HanRiverLandmarks.HillMesh(360f, 90f, 70f, 62), FarLand.Maquis(0.6f), new Vector3(120f, land, -330f));

            // 항구 안 작은 어선들
            b.PlaceBoats("Kenney/Watercraft/boat-fishing-small", 4, 7f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>크레타 이야기 (Lv.30~). 첫 퀘스트는 그 낚시터 연락관(디미트리스 지부장)이 준다.</summary>
        static QuestDef[] CreteChapter() => new[]
        {
            new QuestDef { Id = "crete_1", Title = "타베르나의 붉은 생선", Desc = "칼리메라! 엘레니네 타베르나에서 붉은촉수 튀김을 내려는데 고기가 모자라대요. 모래밭에서 갯지렁이로 다섯 마리! (낚시터 목록 [지중해] 탭, Lv.30부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "redmullet", Count = 5, RewardGold = 45000, RewardExp = 5500 },
            new QuestDef { Id = "crete_2", Title = "등대 끝의 덴텍스", Desc = "새벽 B3, 아침 A4, 등대 앞 바위에 덴텍스 무리가 든다네. 니코스 영감이 30년 동안 노린 고기일세. 한 마리!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "dentex", Count = 1, RewardGold = 70000, RewardExp = 8000 },
            new QuestDef { Id = "crete_3", Title = "바위굴의 갈색 그루퍼", Desc = "저녁·새벽 G5 바위굴엔 지중해의 왕, 갈색 그루퍼가 산다오. 1m가 넘는 놈을 낚으면 하니아 사람들이 이름을 기억할 걸세.",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "duskygrouper", MinLen = 100f, Count = 1, RewardGold = 120000, RewardExp = 11000, RewardKeepNet = 3 },
        };
    }
}
