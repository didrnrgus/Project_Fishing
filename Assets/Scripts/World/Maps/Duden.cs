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
            LandName = "폭포 공원", SeaName = "안탈리아 만", Seed = 63, CliffTexture = "canyon", CliffTint = new Color(1.25f, 1.15f, 1.05f, 1f), StoneQuay = true, StoneTint = new Color(0.8f, 0.72f, 0.58f),
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

            // --- 트래버틴 절벽 (사용자 참고 사진): 주황·황갈색 석회화 바위가 층층이 흘러내린 면, 젖은 곳은 짙고, 턱마다 초록 이끼·덤불 ---
            var trav = Mats.Karst(new Color(0.95f, 0.78f, 0.58f), 0.55f, 0f, 0.15f, "canyon", "cliff");
            var travWet = Mats.Karst(new Color(0.45f, 0.4f, 0.32f), 0.35f, 0f, 0f, "canyon", "cliff");
            var travTop = Mats.Karst(new Color(0.9f, 0.76f, 0.56f), 1f, 0.6f, 0f, "canyon", "cliff");
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                var pk = b.KarstPeak(new Vector3(Mathf.Lerp(-103f, -97f, t) + (i % 2) * 2f, 0f, Mathf.Lerp(-44f, 70f, t)), 13f - 1.5f * t, 62f - 8f * t, true);
                pk.GetComponent<MeshRenderer>().sharedMaterials = new[] { trav, travWet, travTop };
            }

            // --- 폭포 (사용자: 메쉬 디테일, 흐르는 물, 물보라·안개 파티클) ---
            // 절벽 위 강물 → 절벽 끝에서 여러 갈래로 나뉘어 동쪽(바다·공원 쪽)으로 뿜어져 떨어진다.
            // 갈래마다 폭·튀어나가는 거리·흐름 속도가 달라 따로 움직이고, 넓은 가운데 줄기 앞뒤로 가는 줄기를 겹쳐 두께감을 낸다
            float topY = Mathf.Min(b.Height(-112f, 12f), 52f) + 1f;
            const float lipX = -88f;
            var riverMat = Mats.Lit(new Color(0.12f, 0.34f, 0.36f), 0.92f);
            Mats.Prim(PrimitiveType.Cube, "DudenRiver", root, new Vector3(-112f, topY - 0.3f, 14f), new Vector3(48f, 0.5f, 56f), riverMat, false);
            float drop = topY - water;
            var falls = new (float z, float width, float lip, float speed, float opacity)[]
            {
                (12f, 20f, 7f, 0.55f, 0.95f),   // 가운데 큰 줄기
                (7f, 9f, 10f, 0.62f, 0.85f),    // 앞으로 더 뿜어지는 줄기
                (18f, 8f, 9f, 0.58f, 0.85f),
                (-6f, 7f, 5f, 0.5f, 0.9f),
                (-16f, 5f, 3.5f, 0.46f, 0.85f), // 바깥 가는 줄기: 절벽을 타고 흐른다
                (30f, 6f, 4f, 0.48f, 0.88f),
                (40f, 3.5f, 2.5f, 0.44f, 0.8f),
            };
            var lipFoam = new List<Vector4>();
            var rng = new System.Random(63);
            for (int k = 0; k < falls.Length; k++)
            {
                var f = falls[k];
                var top = new Vector3(lipX, topY, f.z);
                var mat = MapKit.WaterfallMat(f.speed, f.opacity, new Color(0.3f, 0.6f, 0.58f));
                MapKit.Place(root, "DudenFall", MapKit.FallSheet(f.width, drop, f.lip, seed: k + 1), mat, top, 90f);
                // 물이 수면에 닿는 곳: 튀어 오르는 물보라 (큰 줄기일수록 많이)
                MapKit.Splash(root, new Vector3(lipX + f.lip, water + 0.3f, f.z), f.width * 0.8f, 90f, 18f + f.width * 4f);
                if (f.width >= 8f) MapKit.FallDrift(root, top, f.width, drop, 90f);
                // 절벽 끝 물마루의 흰 거품
                for (float zz = -f.width * 0.5f; zz <= f.width * 0.5f; zz += 1.4f)
                    lipFoam.Add(new Vector4(lipX + 0.4f, f.z + zz, 0.7f + (float)rng.NextDouble() * 0.6f, topY + 0.25f));
            }
            MapKit.Place(root, "DudenLipFoam", MapKit.Discs(lipFoam, 8), Mats.Transparent(new Color(0.97f, 0.98f, 0.98f, 0.75f), 0.4f), Vector3.zero);
            // 절벽 위 강의 급류 거품 (물이 끝으로 몰려간다)
            var rapids = new List<Vector4>();
            for (int k = 0; k < 70; k++)
                rapids.Add(new Vector4(lipX - 2f - (float)rng.NextDouble() * 22f, -14f + (float)rng.NextDouble() * 56f, 0.5f + (float)rng.NextDouble() * 1.4f, topY - 0.02f + (float)rng.NextDouble() * 0.03f));
            MapKit.Place(root, "DudenRapids", MapKit.Discs(rapids, 8), Mats.Transparent(new Color(0.92f, 0.96f, 0.96f, 0.55f), 0.4f), Vector3.zero);
            // 떨어지는 곳의 큰 물안개 구름 (바다 쪽으로 피어오른다)
            MapKit.Mist(root, new Vector3(lipX + 9f, water + 1f, 12f), 14f, 26f);
            MapKit.Mist(root, new Vector3(lipX + 6f, water + 1f, 32f), 8f, 10f);
            // 물보라: 떨어지는 곳의 흰 거품 판 (반원으로 퍼진다)
            var foam = new List<Vector4>();
            for (int k = 0; k < 110; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI, d = 2f + (float)rng.NextDouble() * 22f;
                float zc = falls[rng.Next(falls.Length)].z;
                foam.Add(new Vector4(lipX + 6f + Mathf.Sin(a) * d, zc + Mathf.Cos(a) * d * 1.2f, 1f + (float)rng.NextDouble() * 2.8f, water + 0.1f + (float)rng.NextDouble() * 0.05f));
            }
            MapKit.Place(root, "DudenFoam", MapKit.Discs(foam, 10), Mats.Transparent(new Color(0.97f, 0.98f, 0.98f, 0.7f), 0.5f), Vector3.zero);
            // 절벽 끝을 따라 늘어진 덤불 (사진: 폭포 사이로 초록이 흘러내린다)
            for (float z = -40f; z < 66f; z += 4.5f)
            {
                bool inFall = false;
                foreach (var f in falls) if (Mathf.Abs(z - f.z) < f.width * 0.5f + 1f) inFall = true;
                if (inFall) continue;
                KenneyProps.SpawnBush(root, new Vector3(lipX - 1.5f, topY - 0.8f, z + (float)rng.NextDouble() * 2f), (float)rng.NextDouble() * 360f, 2.4f + (float)rng.NextDouble() * 1.6f);
            }

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
