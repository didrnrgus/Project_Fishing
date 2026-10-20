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
        /// 랑카위 (Lv.33): 쿠아 부두와 독수리 광장. 바다를 향해 날개를 펴는 적갈색 독수리 상(다타란 랑), 뒤로 맛친창 산과 산마루를 휘감는 스카이 브리지,
        /// 서쪽 갯벌의 맹그로브 숲(물 위로 뻗은 뿌리), 앞바다의 숲 덮인 섬들 (원작 2차 — 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec Langkawi() => new CoastSpec
        {
            Id = "langkawi", MapName = "랑카위 · 말레이시아", ShortName = "랑카위", ShopName = "쿠아 낚시점", UnionContact = "독수리 광장의 아지즈 지부장",
            LandName = "쿠아 부두", SeaName = "안다만 해협", Seed = 56,
            WaterDeep = new Color(0.03f, 0.2f, 0.24f), WaterShallow = new Color(0.14f, 0.44f, 0.38f),
            Shores = new[] { new ShoreSeg(-999f, -90f, ShoreKind.Mud), new ShoreSeg(-90f, 50f, ShoreKind.Quay), new ShoreSeg(50f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.05f, FlatWidth = 30f, CliffHeight = 22f, CliffWidth = 50f, RoadOffset = 16f, VillageDensity = 0.7f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Tropical, Features = CoastFeature.Mountains,
            Platforms = new[] { new PlatformSpec { Area = new Rect(14f, -41f, 6f, 40f), Kind = PlatformKind.Pier, Name = "쿠아 잔교" } },
            Islets = new[]
            {
                new IsletSpec { Center = new Vector3(110f, 0f, 150f), Radius = 30f, Height = 26f, Trees = true, Name = "다양 분팅 섬" },
                new IsletSpec { Center = new Vector3(-150f, 0f, 190f), Radius = 22f, Height = 18f, Trees = true, Name = "숲섬" },
            },
            Grid = new[,]
            {
                { W, W, W, S, S, S, S, S },
                { W, S, S, D, D, D, R, R },
                { S, D, D, D, D, D, R, R },
                { D, D, D, D, R, D, D, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(4f, 1.1f, -47f), ShopPos = new Vector3(30f, 1f, -54f),
            ShopRoof = new Color(0.78f, 0.36f, 0.22f),
            ShopOwner = Local("쿠아 낚시점 하리스", "쿠아 낚시점", new Color(0.25f, 0.45f, 0.65f), false, true, 1),
            Npcs = new[]
            {
                Npc(-6f, -47f, 160f, Union("아지즈 지부장", "한국 낚시 조합 랑카위 지부", false, true), NpcAngler.Role.Talker, "union"),
                Npc(17f, -6f, 0f, Local("바틱 셔츠 낚시꾼", "타폰 손맛에 빠졌다", new Color(0.75f, 0.45f, 0.2f), false, true, 1), NpcAngler.Role.Angler, y: 1f),
                Npc(-100f, -46f, 300f, Local("맹그로브 투어 가이드", "독수리 먹이 주기 투어", new Color(0.3f, 0.6f, 0.35f), true, false, 2), NpcAngler.Role.Talker),
                Npc(70f, -44f, 0f, Local("면세 쇼핑 온 여행객", "초콜릿 한 보따리", new Color(0.9f, 0.5f, 0.6f), true, false, -1), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("DATARAN LANG", new Vector3(-40f, 6.5f, -64f), SignGold) },
            Areas = new[] { (new Rect(-200f, -60f, 110f, 90f), "맹그로브 숲") },
            Reserved = new[] { new Rect(-58f, -66f, 34f, 26f) },
            Schools = () => new List<FishSchool>
            {
                School("물총고기 맹그로브", 0, 0, 1, MornDay, "archerfish", "mangrovejack"),
                School("레이디피시 얕은 물", 3, 0, 1, EveNight, "ladyfish", "threadfin"),
                School("타폰 물길", 1, 1, 2, DawnEve, "tarpon", "threadfin"),
                School("산호 바위", 6, 1, 3, DayLong, "coraltrout", "goldensnapper", "spangledemperor", "orangegrouper"),
                School("퀸피시 떼", 4, 2, 4, MornDay, "queenfish", "bluefintrevally", "cobia"),
                School("황소상어 골", 2, 4, 6, EveNight, "bullshark", "gianttrevally"),
                // 대왕바리: 저녁 E4, 밤 B5 (바위굴)
                Special("대왕바리", "daewangbari", (TimeBand.Evening, 4, 3), (TimeBand.Night, 1, 4)),
            },
            Extra = LangkawiExtra,
        };

        /// <summary>랑카위만의 풍경: 독수리 상과 광장, 맛친창 산과 스카이 브리지, 맹그로브 숲.</summary>
        static void LangkawiExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Langkawi").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, water = b.Map.BaseWaterY;

            // --- 독수리 광장: 둥근 돌 광장 + 계단 단 + 독수리 (바다·잔교 쪽을 본다) ---
            var plaza = new Vector3(-40f, land, -53f);
            var paving = Mats.Textured("concrete", new Color(0.78f, 0.72f, 0.64f), 2f, 0.2f);
            Mats.Prim(PrimitiveType.Cylinder, "EaglePlaza", root, plaza + new Vector3(0f, 0.05f, 0f), new Vector3(28f, 0.1f, 28f), paving, false);
            Mats.Prim(PrimitiveType.Cylinder, "EagleStep", root, plaza + new Vector3(0f, 0.5f, 0f), new Vector3(12f, 0.5f, 12f), paving, true);
            MapKit.Prop(root, "eagle", plaza + new Vector3(0f, 0.95f, 0f), 15f, 14f);
            // 광장 둘레 분수 둑 (낮은 원형 벽 + 물)
            Mats.Prim(PrimitiveType.Cylinder, "EaglePool", root, plaza + new Vector3(0f, 0.25f, 0f), new Vector3(20f, 0.25f, 20f), Mats.Lit(new Color(0.2f, 0.45f, 0.55f), 0.9f), false);

            // --- 맛친창 산 + 스카이 브리지 (산마루를 휘감는 굽은 다리, 기울어진 기둥 하나에 줄로 매단다) ---
            var hill = new Vector3(110f, land, -330f);
            var jungle = MapKit.FarRock(new Color(0.2f, 0.34f, 0.17f), "grass", 25f, 0.8f, 0.05f);
            var cliffs = MapKit.FarRock(new Color(0.46f, 0.46f, 0.4f), "cliff", 35f);
            // 능선이 들쭉날쭉한 산 (위쪽 서브메쉬 = 바위가 드러난 꼭대기)
            MapKit.Place(root, "MatCincang", MapKit.Mountain(240f, 120f, 200f, 56, 0.72f, 3), new[] { jungle, cliffs }, hill);
            MapKit.Place(root, "MatCincang2", MapKit.Mountain(170f, 90f, 140f, 57, 0.8f, 2), new[] { jungle, cliffs }, hill + new Vector3(-220f, 0f, 50f));
            var steel = MapKit.Far(new Color(0.8f, 0.8f, 0.78f), 0.4f, 0.4f);
            var bridgeC = hill + new Vector3(0f, 128f, 60f);
            Vector3 Deck(float t)
            {
                float a = Mathf.Lerp(-1.1f, 1.1f, t);
                return bridgeC + new Vector3(Mathf.Sin(a) * 60f, Mathf.Sin(t * Mathf.PI) * 4f, -Mathf.Cos(a) * 30f + 10f);
            }
            var pylonTop = bridgeC + new Vector3(10f, 46f, 30f);
            MapKit.Beam(root, bridgeC + new Vector3(4f, -30f, 20f), pylonTop, 2.2f, steel, false, "SkyPylon");
            for (int i = 0; i < 12; i++)
            {
                var p0 = Deck(i / 12f);
                var p1 = Deck((i + 1) / 12f);
                MapKit.Beam(root, p0, p1, 3f, steel, false, "SkyDeck");
                if (i % 2 == 0) MapKit.Beam(root, p0 + Vector3.up * 0.5f, pylonTop, 0.35f, steel, false, "SkyCable");
            }
            // 케이블카 줄 (산 아래에서 꼭대기 정류장으로)
            MapKit.Beam(root, hill + new Vector3(-80f, 10f, 160f), bridgeC + new Vector3(-50f, 8f, 20f), 0.4f, Mats.Lit(new Color(0.15f, 0.15f, 0.15f)), false, "SkyCableCar");

            // --- 맹그로브: 갯벌 위로 뻗은 아치 뿌리 다발 + 덤불 지붕 ---
            var roots = new List<Vector3[]>();
            var rng = new System.Random(56);
            float Rf(float a, float c) => a + (float)rng.NextDouble() * (c - a);
            for (int k = 0; k < 34; k++)
            {
                float x = Rf(-200f, -92f);
                float z = b.Map.ShoreAt(x) + Rf(-6f, 26f);
                float gy = Mathf.Max(b.Height(x, z), water - 1.2f);
                var trunkTop = new Vector3(x, Mathf.Max(gy, water) + 1.8f, z);
                for (int r = 0; r < 9; r++)
                {
                    float a = r / 9f * Mathf.PI * 2f + Rf(-0.3f, 0.3f);
                    float reach = Rf(1.4f, 2.8f);
                    var foot = new Vector3(x + Mathf.Cos(a) * reach, gy - 0.5f, z + Mathf.Sin(a) * reach);
                    var mid = Vector3.Lerp(trunkTop, foot, 0.5f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.6f + Vector3.up * 0.7f;
                    roots.Add(new[] { trunkTop, Vector3.Lerp(trunkTop, mid, 0.5f) + Vector3.up * 0.1f, mid, Vector3.Lerp(mid, foot, 0.6f), foot });
                }
                roots.Add(new[] { trunkTop, trunkTop + Vector3.up * 1.6f });
                KenneyProps.SpawnBush(root, trunkTop + Vector3.up * 1.2f, Rf(0f, 360f), Rf(3f, 4.5f));
            }
            MapKit.Place(root, "MangroveRoots", MapKit.Tubes(roots, u => 0.13f - 0.06f * u, 6), Mats.Lit(new Color(0.36f, 0.3f, 0.24f), 0.2f), Vector3.zero);

            // 쿠아 부두 롱테일·관광선
            MapKit.Prop(root, "longtail_a", new Vector3(23f, water - 0.25f, -20f), 92f, 0f, 9.5f);
            KenneyProps.Spawn("Kenney/Watercraft/boat-speed-a", root, new Vector3(9f, water - 0.2f, -14f), 85f, 0f, 8f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>랑카위 이야기 (Lv.33~). 첫 퀘스트는 그 낚시터 연락관(아지즈 지부장)이 준다.</summary>
        static QuestDef[] LangkawiChapter() => new[]
        {
            new QuestDef { Id = "langkawi_1", Title = "물총 쏘는 물고기", Desc = "슬라맛 다탕! 맹그로브 숲 물가엔 물총으로 벌레를 떨어뜨리는 물총고기가 살아요. 아침·낮에 세 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.33부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "archerfish", Count = 3, RewardGold = 55000, RewardExp = 6500 },
            new QuestDef { Id = "langkawi_2", Title = "은빛 왕의 점프", Desc = "인도양 타폰은 걸리면 물 위로 몇 번이고 뛰어오른다네. 새벽이나 저녁 물길에서 한 마리!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "tarpon", Count = 1, RewardGold = 75000, RewardExp = 8500 },
            new QuestDef { Id = "langkawi_3", Title = "바위굴의 대왕", Desc = "저녁 E4, 밤 B5 바위굴에 세상에서 가장 큰 바리, 대왕바리가 산다오. 물리면 굴로 처박으니 처음부터 힘껏 버티시오!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "daewangbari", Count = 1, RewardGold = 130000, RewardExp = 12000, RewardKeepNet = 3 },
        };
    }
}
