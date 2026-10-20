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
        /// 듀덴 폭포 (Lv.36): 안탈리아 라라 해안. 서쪽 석회화(트래버틴) 절벽 끝에서 듀덴 강이 바다로 곧장 떨어지는 폭포(낙차 약 40m)와 물보라,
        /// 돌 포장 폭포 공원 산책로, 뒤로 눈 덮인 토로스 산맥. 홍해에서 수에즈 운하로 들어온 쏠배감펭·은빛뺨복어가 산다 (원작 지중해 — 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec Duden() => new CoastSpec
        {
            Id = "duden", MapName = "듀덴 폭포 · 안탈리아", ShortName = "듀덴 폭포", ShopName = "라라 해안 낚시점", UnionContact = "폭포 공원의 메흐메트 지부장",
            LandName = "폭포 공원", SeaName = "안탈리아 만", Seed = 63, StoneQuay = true, StoneTint = new Color(0.8f, 0.72f, 0.58f),
            WaterDeep = new Color(0.01f, 0.16f, 0.32f), WaterShallow = new Color(0.06f, 0.45f, 0.55f),
            Shores = new[] { new ShoreSeg(-999f, -60f, ShoreKind.Rock), new ShoreSeg(-60f, 70f, ShoreKind.Quay), new ShoreSeg(70f, 999f, ShoreKind.Rock) },
            NearDepth = 5f, DepthSlope = 0.08f, FlatWidth = 30f, CliffHeight = 26f, CliffWidth = 30f, RoadOffset = 16f, VillageDensity = 0.7f,
            Houses = HouseStyle.Venetian, TreeKind = TreeStyle.Mediterranean, Features = CoastFeature.Mountains,
            // 폭포가 떨어지는 절벽 곶 (동쪽 옆구리에서 물이 떨어진다)
            Capes = new[] { new CapeSpec { Base = new Vector2(-125f, -70f), Tip = new Vector2(-118f, 60f), WidthBase = 30f, WidthTip = 24f, Height = 74f, Name = "듀덴 절벽" } },
            Grid = new[,]
            {
                { R, R, S, S, S, S, W, R },
                { R, R, D, D, D, S, R, R },
                { R, D, D, D, D, D, D, R },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(6f, 1.1f, -47f), ShopPos = new Vector3(-22f, 1f, -54f),
            ShopRoof = new Color(0.72f, 0.32f, 0.2f),
            ShopOwner = Local("라라 해안 낚시점 아이셰", "라라 해안 낚시점", true),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("메흐메트 지부장", "한국 낚시 조합 안탈리아 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(-46f, -42f, 330f, Local("폭포 공원 차 장수", "차이 한 잔 하고 가요", false), NpcAngler.Role.Talker),
                Npc(60f, -43f, 10f, Local("쏠배감펭 사냥꾼 무랏", "침입종은 잡아서 먹는다", false), NpcAngler.Role.Angler),
                Npc(-20f, -43f, 350f, Local("폭포 보러 온 신혼부부", "물보라에 무지개가 떴다", true), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("DÜDEN ŞELALESİ", new Vector3(-40f, 6.5f, -62f), SignGold) },
            Areas = new[] { (new Rect(-110f, -40f, 40f, 70f), "폭포 아래") },
            Schools = () => new List<FishSchool>
            {
                School("쏠배감펭 바위", 1, 1, 2, DawnEve, "lionfish", "marbledspinefoot"),
                School("도미 떼", 3, 0, 2, DayLong, "twobandedbream", "saddledbream", "whiteseabream"),
                School("대롱고기 수초", 6, 0, 1, DayLong, "cornetfish", "bogue"),
                School("은빛뺨복어", 4, 2, 4, DayLong, "toadfish", "redmullet"),
                School("블루피시 떼", 5, 3, 5, DayLong, "bluefish", "europeanbarracuda", "atlanticbonito"),
                School("쏨뱅이 바위", 7, 1, 3, EveNight, "redscorpionfish", "europeanconger", "moray"),
                // 앰버잭: 새벽 B3, 아침 D4 (폭포 아래 물살)
                Special("폭포 아래 앰버잭", "greateramberjack", (TimeBand.Dawn, 1, 2), (TimeBand.Morning, 3, 3)),
            },
            Extra = DudenExtra,
        };

        /// <summary>듀덴 폭포만의 풍경: 트래버틴 절벽 면, 절벽 위 강물과 바다로 떨어지는 폭포, 물보라, 토로스 산맥.</summary>
        static void DudenExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Duden").transform;
            root.SetParent(b.Root, false);
            float water = b.Map.BaseWaterY;

            // --- 트래버틴 절벽: 곶 동쪽 옆구리를 따라 황갈색 석회화 바위 면 ---
            var trav = Mats.Karst(new Color(0.86f, 0.74f, 0.56f), 0.45f);
            var travWet = Mats.Karst(new Color(0.42f, 0.38f, 0.32f), 0.2f);
            var travTop = Mats.Karst(new Color(0.86f, 0.74f, 0.56f), 1f);
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                var pk = b.KarstPeak(new Vector3(Mathf.Lerp(-102f, -98f, t), 0f, Mathf.Lerp(-40f, 66f, t)), 14f - 2f * t, 62f - 8f * t, true);
                pk.GetComponent<MeshRenderer>().sharedMaterials = new[] { trav, travWet, travTop };
            }

            // --- 폭포: 절벽 위 물길 → 동쪽(바다·공원 쪽)으로 튀어나와 떨어지는 물줄기 ---
            float topY = Mathf.Min(b.Height(-112f, 12f), 52f) + 1f;
            var fallTop = new Vector3(-88f, topY, 12f);
            var riverMat = Mats.Lit(new Color(0.12f, 0.32f, 0.36f), 0.92f);
            Mats.Prim(PrimitiveType.Cube, "DudenRiver", root, new Vector3(-112f, topY - 0.3f, 12f), new Vector3(48f, 0.5f, 20f), riverMat, false);
            var fallMat = MapKit.FallMat();
            float drop = topY - water;
            // 넓은 물줄기 하나 + 앞으로 더 튀어나오는 가는 물줄기 둘 (겹쳐서 두께감)
            MapKit.Place(root, "DudenFall", MapKit.FallSheet(22f, drop, 6f), fallMat, fallTop, 90f);
            MapKit.Place(root, "DudenFall2", MapKit.FallSheet(10f, drop, 9f), fallMat, fallTop + new Vector3(0.5f, 0f, -5f), 90f);
            MapKit.Place(root, "DudenFall3", MapKit.FallSheet(8f, drop, 8f), fallMat, fallTop + new Vector3(0.5f, 0f, 6f), 90f);
            // 사진처럼 여러 갈래: 바깥 가는 물줄기 둘 (앞으로 덜 튀어나와 절벽을 타고 흐른다)
            var fallMat2 = MapKit.FallMat();
            MapKit.Place(root, "DudenFall4", MapKit.FallSheet(5f, drop, 3f), fallMat2, fallTop + new Vector3(-0.5f, 0f, -13f), 90f);
            MapKit.Place(root, "DudenFall5", MapKit.FallSheet(4f, drop, 2.5f), fallMat2, fallTop + new Vector3(-0.5f, 0f, 15f), 90f);
            // 물이 떨어지며 흐르게 (두 재질 속도를 달리해 겹친 줄기가 따로 움직인다)
            TextureScroll.On(root).Add(fallMat, new Vector2(0f, 0.55f)).Add(fallMat2, new Vector2(0f, 0.42f));
            // 떨어지는 곳의 물안개
            MapKit.Mist(root, new Vector3(-80f, water + 0.5f, 12f), 9f, 18f);
            // 물보라: 떨어지는 곳의 흰 거품 원판 + 뿌연 물안개 덩어리
            var foam = new List<Vector4>();
            var rng = new System.Random(63);
            for (int k = 0; k < 60; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI, d = 3f + (float)rng.NextDouble() * 16f;
                foam.Add(new Vector4(-80f + Mathf.Sin(a) * d, 12f + Mathf.Cos(a) * d * 1.3f, 1f + (float)rng.NextDouble() * 2.4f, water + 0.1f + (float)rng.NextDouble() * 0.05f));
            }
            MapKit.Place(root, "DudenFoam", MapKit.Discs(foam, 10), Mats.Transparent(new Color(0.97f, 0.98f, 0.98f, 0.7f), 0.5f), Vector3.zero);

            // --- 토로스 산맥: 뒤로 눈 덮인 높은 산줄기 ---
            var taurus = MapKit.Mountain(700f, 150f, 260f, 63, 0.58f, 5);
            MapKit.Place(root, "Taurus", taurus, new[] { MapKit.FarRock(new Color(0.58f, 0.54f, 0.48f), "cliff", 50f), MapKit.FarRock(new Color(0.96f, 0.97f, 0.99f), "snow", 30f, 0.5f, 0.3f) }, new Vector3(80f, b.Map.LandY, -600f));
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>듀덴 폭포 이야기 (Lv.36~). 첫 퀘스트는 그 낚시터 연락관(메흐메트 지부장)이 준다.</summary>
        static QuestDef[] DudenChapter() => new[]
        {
            new QuestDef { Id = "duden_1", Title = "홍해에서 온 불청객", Desc = "메르하바! 수에즈 운하로 들어온 쏠배감펭이 토박이 고기를 다 먹어 치워요. 새벽·저녁 바위에서 세 마리 잡아 주세요. 가시에 독이 있으니 장갑 끼고요! (낚시터 목록 [지중해] 탭, Lv.36부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "lionfish", Count = 3, RewardGold = 60000, RewardExp = 7000 },
            new QuestDef { Id = "duden_2", Title = "먹으면 안 되는 복어", Desc = "은빛뺨복어는 맹독이 있고 이빨로 낚싯줄을 끊는다네. 한 마리 잡아다 조합 연구소에 보내 주게. 절대 먹지 말고!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "toadfish", Count = 1, RewardGold = 70000, RewardExp = 8000 },
            new QuestDef { Id = "duden_3", Title = "폭포 아래의 앰버잭", Desc = "새벽 B3, 아침 D4, 폭포 물살 아래로 지중해 앰버잭 무리가 든다오. 1.2m가 넘는 놈을 낚으면 지중해의 강태공이라 불러 주겠소!",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "greateramberjack", MinLen = 120f, Count = 1, RewardGold = 140000, RewardExp = 13000, RewardKeepNet = 3 },
        };
    }
}
