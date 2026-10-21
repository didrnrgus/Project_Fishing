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
        /// 자킨토스 (Lv.33): 나바지오(난파선 해변). 깎아지른 하얀 석회암 절벽이 감싼 작은 만, 흰 자갈 해변에 기울어 박힌 녹슨 화물선,
        /// 하늘보다 파란 물, 절벽 밑 푸른 동굴 입구, 만으로 들어오는 관광선 (원작 지중해 — 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec Zakynthos() => new CoastSpec
        {
            Id = "zakynthos", MapName = "자킨토스 · 이오니아해", ShortName = "자킨토스", ShopName = "나바지오 낚시점", UnionContact = "나바지오 해변의 엘레나 지부장",
            LandName = "나바지오 해변", SeaName = "이오니아해", Seed = 62,
            WaterDeep = new Color(0.0f, 0.18f, 0.38f), WaterShallow = new Color(0.08f, 0.62f, 0.72f),
            Shores = new[] { new ShoreSeg(-999f, -55f, ShoreKind.Rock), new ShoreSeg(-55f, 55f, ShoreKind.Beach), new ShoreSeg(55f, 999f, ShoreKind.Rock) },
            CliffTexture = "limecliff", CliffTint = new Color(1.22f, 1.24f, 1.26f, 1f), SandTexture = "pebbles", SandTint = new Color(2.1f, 2.1f, 2.05f),
            NearDepth = 4f, DepthSlope = 0.09f, FlatWidth = 26f, CliffHeight = 60f, CliffWidth = 16f, RoadOffset = 0f, VillageDensity = 0.08f,
            Houses = HouseStyle.Venetian, TreeKind = TreeStyle.Mediterranean,
            // 만을 감싸는 두 절벽 곶
            Capes = new[]
            {
                new CapeSpec { Base = new Vector2(-95f, -60f), Tip = new Vector2(-80f, 110f), WidthBase = 34f, WidthTip = 22f, Height = 62f, Name = "나바지오 서쪽 절벽" },
                new CapeSpec { Base = new Vector2(95f, -60f), Tip = new Vector2(84f, 120f), WidthBase = 34f, WidthTip = 20f, Height = 58f, Name = "나바지오 동쪽 절벽" },
            },
            Platforms = new[] { new PlatformSpec { Area = new Rect(-30f, -41f, 4f, 26f), Kind = PlatformKind.Pier, Name = "관광선 선착장" } },
            Grid = new[,]
            {
                { R, R, S, S, S, S, R, R },
                { R, R, S, D, D, S, R, R },
                { R, D, D, D, D, D, D, R },
                { R, D, D, D, D, D, D, R },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(0f, 1.1f, -47f), ShopPos = new Vector3(-26f, 1f, -54f),
            ShopRoof = new Color(0.72f, 0.32f, 0.2f),
            ShopOwner = Local("나바지오 낚시점 스피로스", "나바지오 낚시점"),
            Npcs = new[]
            {
                Npc(20f, -47f, 200f, Union("엘레나 지부장", "한국 낚시 조합 자킨토스 지부", true), NpcAngler.Role.Talker, "union"),
                Npc(-28f, -18f, 0f, Local("관광선 선장 파블로스", "푸른 동굴 투어", false), NpcAngler.Role.Angler, y: 1f),
                Npc(36f, -48f, 220f, Local("난파선 사진 찍는 여행객", "절벽 위 전망대에서 내려왔다", true), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("NAVAGIO", new Vector3(-6f, 6f, -60f), SignBlue) },
            // 난파선 자리
            Reserved = new[] { new Rect(16f, -64f, 44f, 24f) },
            Schools = () => new List<FishSchool>
            {
                School("붉은촉수 모래밭", 2, 0, 1, MornDay, "redmullet", "pandora"),
                School("고등어 떼", 4, 1, 3, DayLong, "atlanticmackerel", "atlantichorsemackerel", "garfish"),
                School("가오리 모래톱", 3, 0, 2, EveNight, "commonstingray", "smoothhound"),
                School("블루피시 떼", 5, 2, 4, DayLong, "bluefish", "europeanbarracuda", "littletunny"),
                School("앰버잭 절벽", 6, 3, 5, MornDay, "greateramberjack", "dentex"),
                School("붕장어 동굴", 1, 2, 3, EveNight, "europeanconger", "moray"),
                // 미거: 저녁 D4, 밤 B5
                Special("미거 어군", "meagre", (TimeBand.Evening, 3, 3), (TimeBand.Night, 1, 4)),
            },
            Extra = ZakynthosExtra,
        };

        /// <summary>자킨토스만의 풍경: 하얀 석회암 절벽 벽, 해변의 난파선, 푸른 동굴 입구, 관광선.</summary>
        static void ZakynthosExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Zakynthos").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, water = b.Map.BaseWaterY;

            // --- 하얀 석회암 절벽: 해변 뒤와 두 곶을 따라 겹쳐 세운 봉우리 (이끼 적게) ---
            // 해를 등진 북향 절벽이라 그늘에서도 하얗게 보이게 색을 1보다 밝게 (HDR 틴트)
            // 사용자 참고 사진(나바지오): 크림빛 흰 석회암에 세로 결·금, 군데군데 회색·황토 빗물 자국, 꼭대기는 짙은 초록 관목.
            // 결은 세로 줄이 진 limecliff(Marble Cliff 04)를 주로, 흰 석회암 karst를 섞고 채도를 낮춰 하얗게 (예전 karst 한 장은 회색 반복 무늬로 보였다)
            var white = Mats.Karst(new Color(1.32f, 1.33f, 1.32f), 0.06f, 0f, 0.75f, "limecliff", "karst");
            var wet = Mats.Karst(new Color(0.66f, 0.68f, 0.66f), 0.04f, 0f, 0f, "limecliff", "karst");
            var top = Mats.Karst(new Color(1.2f, 1.2f, 1.16f), 0.75f, 0.35f, 0.4f, "limecliff", "karst");
            white.SetFloat("_Saturation", 0.42f);
            wet.SetFloat("_Saturation", 0.5f);
            top.SetFloat("_Saturation", 0.5f);
            top.SetColor("_MossColor", new Color(0.2f, 0.32f, 0.16f));
            white.SetColor("_StreakColor", new Color(0.62f, 0.6f, 0.55f));
            void Cliff(float x, float z, float r, float h)
            {
                var t = b.KarstPeak(new Vector3(x, 0f, z), r, h, true);
                t.GetComponent<MeshRenderer>().sharedMaterials = new[] { white, wet, top };
            }
            for (float x = -70f; x <= 70f; x += 22f) Cliff(x, -98f - Mathf.Abs(x) * 0.15f, 26f, 74f + b.Rand(-6f, 6f));
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                Cliff(Mathf.Lerp(-98f, -82f, t), Mathf.Lerp(-60f, 108f, t), 24f - 6f * t, 70f - 10f * t);
                Cliff(Mathf.Lerp(98f, 86f, t), Mathf.Lerp(-60f, 118f, t), 24f - 6f * t, 66f - 10f * t);
            }

            // --- 난파선: 흰 자갈 해변에 기울어 박힌 녹슨 화물선 ---
            var ship = MapKit.Prop(root, "shipwreck", new Vector3(36f, land - 1.4f, -50f), 0f, 0f, 30f);
            if (ship != null) ship.transform.localRotation = Quaternion.Euler(0f, 72f, 9f) * ship.transform.localRotation; // 모래에 박혀 9° 기울었다

            // --- 푸른 동굴 입구 (사진: 절벽 밑동이 파도에 패여 아치로 뚫리고, 안쪽 물은 빛을 받아 청록으로 빛난다) ---
            var cave = Mats.Lit(new Color(0.02f, 0.05f, 0.08f), 0.6f);
            var caveGlow = Mats.Emissive(new Color(0.1f, 0.7f, 0.85f), 0.7f);
            var rim = Mats.Karst(new Color(1.2f, 1.2f, 1.18f), 0.05f, 0f, 0.6f, "limecliff", "karst");
            foreach (var (x, z, yaw) in new[] { (-62f, 70f, 90f), (64f, 90f, -90f), (-58f, 20f, 90f) })
            {
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var c = new Vector3(x, water, z);
                Mats.Prim(PrimitiveType.Sphere, "BlueCave", root, c + Vector3.up * 0.5f, new Vector3(9f, 8f, 2.4f), cave, false, rot);
                Mats.Prim(PrimitiveType.Cylinder, "CaveWater", root, c + rot * new Vector3(0f, 0.08f, 0.6f), new Vector3(7.5f, 0.02f, 3f), caveGlow, false, rot);
                // 아치 테두리: 돌출된 바위 띠 (반원)
                for (int k = 0; k <= 8; k++)
                {
                    float a = Mathf.Lerp(0f, Mathf.PI, k / 8f);
                    var p = c + rot * new Vector3(Mathf.Cos(a) * 4.8f, 0.5f + Mathf.Sin(a) * 4.3f, -0.6f);
                    Rocks.Prim("CaveRim", root, p, new Vector3(2.2f, 1.8f, 2.4f), rim, rot * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg));
                }
            }

            // --- 관광선 ---
            KenneyProps.Spawn("Kenney/Watercraft/boat-speed-a", root, new Vector3(-24f, water - 0.2f, -24f), 90f, 0f, 9f);
            KenneyProps.Spawn("Kenney/Watercraft/boat-row-large", root, new Vector3(30f, water - 0.2f, 60f), 200f, 0f, 16f);
            KenneyProps.Spawn("Kenney/Watercraft/boat-speed-c", root, new Vector3(-20f, water - 0.2f, 120f), 160f, 0f, 9f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>자킨토스 이야기 (Lv.33~). 첫 퀘스트는 그 낚시터 연락관(엘레나 지부장)이 준다.</summary>
        static QuestDef[] ZakynthosChapter() => new[]
        {
            new QuestDef { Id = "zakynthos_1", Title = "파란 만의 고등어", Desc = "칼리스페라! 하늘보다 파란 이 만엔 고등어 떼가 들어와요. 크릴이나 루어로 다섯 마리 낚아 보세요. (낚시터 목록 [지중해] 탭, Lv.33부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "atlanticmackerel", Count = 5, RewardGold = 50000, RewardExp = 6000 },
            new QuestDef { Id = "zakynthos_2", Title = "우는 물고기", Desc = "미거는 물속에서 북 치듯 운다네. 저녁 D4, 밤 B5에 어군이 들어오니 미끼고기나 오징어로 한 마리!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "meagre", Count = 1, RewardGold = 80000, RewardExp = 9000 },
            new QuestDef { Id = "zakynthos_3", Title = "깊은 바다의 푸른 상어", Desc = "배를 타고 먼바다로 나가면 청상아리가 미끼를 노린다오. 새벽·저녁·밤에 오징어나 잘린 미끼로. 이빨을 조심하시게!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "blueshark", Count = 1, RewardGold = 130000, RewardExp = 12000, RewardKeepNet = 3 },
        };
    }
}
