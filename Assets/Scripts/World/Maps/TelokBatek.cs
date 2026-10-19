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
        /// 텔록 바텍 (Lv.36): 고즈넉한 동남아 어촌 마을. 백사장의 나무 인명구조 망루, 야자수, 초가·함석 고상 가옥 마을, 모래에 끌어 올린 색칠한 롱테일 보트,
        /// 그물 말리는 대나무 시렁, 나무 잔교, 금빛 노을 (원작 3차 영상 0:30~0:36).
        /// </summary>
        public static CoastSpec TelokBatek() => new CoastSpec
        {
            Id = "telokbatek", MapName = "텔록 바텍 · 말레이시아", ShortName = "텔록 바텍", ShopName = "어촌 잔교 낚시점", UnionContact = "어촌 잔교의 로즈나 지부장",
            LandName = "어촌 마을", SeaName = "남중국해", Seed = 58,
            WaterDeep = new Color(0.03f, 0.22f, 0.3f), WaterShallow = new Color(0.12f, 0.5f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, 999f, ShoreKind.Beach) },
            NearDepth = 2.5f, DepthSlope = 0.05f, FlatWidth = 40f, CliffHeight = 14f, CliffWidth = 50f, RoadOffset = 24f, VillageDensity = 1.1f,
            Houses = HouseStyle.Stilt, TreeKind = TreeStyle.Palm, Features = CoastFeature.Mountains,
            Platforms = new[] { new PlatformSpec { Area = new Rect(-8f, -41f, 4f, 48f), Kind = PlatformKind.Pier, Name = "어촌 잔교" } },
            Stacks = new[] { new StackSpec { Pos = new Vector3(140f, 0f, 70f), Radius = 6f, Height = 10f, Pines = true, Name = "거북 바위" } },
            Grid = new[,]
            {
                { S, S, S, S, S, S, W, R },
                { S, S, D, D, D, S, R, R },
                { W, D, D, D, D, D, D, R },
                { D, D, D, D, D, D, D, D },
                { D, D, R, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(6f, 1.1f, -50f), ShopPos = new Vector3(-24f, 1f, -58f),
            ShopRoof = new Color(0.62f, 0.5f, 0.32f),
            ShopOwner = Local("어촌 잔교 낚시점 하산", "어촌 잔교 낚시점", new Color(0.3f, 0.5f, 0.35f), false, true, 2),
            Npcs = new[]
            {
                Npc(22f, -50f, 200f, Union("로즈나 지부장", "한국 낚시 조합 텔록 바텍 연락소", true, false), NpcAngler.Role.Talker, "union"),
                Npc(-6f, 2f, 0f, Local("송콕 쓴 어부", "롱테일로 새벽 조업", new Color(0.55f, 0.3f, 0.25f), false, true, 2), NpcAngler.Role.Angler, y: 1f),
                Npc(54f, -50f, 30f, Local("망루 아저씨", "고기 떼가 지나가는 게 보여", new Color(0.85f, 0.2f, 0.18f), false, true, 1), NpcAngler.Role.Talker),
                Npc(-70f, -46f, 340f, Local("그물 깁는 할머니", "바틱 두건", new Color(0.4f, 0.3f, 0.6f), true, false, -1), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("TELOK BATEK", new Vector3(14f, 6.5f, -68f), SignGold) },
            Schools = () => new List<FishSchool>
            {
                School("레이디피시 얕은 물", 2, 0, 1, EveNight, "ladyfish", "threadfin"),
                School("물총고기 수초", 0, 2, 2, MornDay, "archerfish", "rabbitfish"),
                School("퀸피시 떼", 4, 1, 3, MornDay, "queenfish", "gianttrevally"),
                School("스내퍼 바위", 6, 1, 3, EveNight, "spangledemperor", "goldensnapper", "mangrovejack", "orangegrouper"),
                School("타폰 물길", 1, 1, 2, DawnEve, "tarpon", "threadfin"),
                // 인도양 타폰 대물: 새벽 C3, 저녁 F4
                Special("인도양 타폰 무리", "tarpon", (TimeBand.Dawn, 2, 2), (TimeBand.Evening, 5, 3)),
            },
            Extra = TelokBatekExtra,
        };

        /// <summary>텔록 바텍만의 풍경: 인명구조 망루, 끌어 올린 롱테일 보트, 그물 말리는 시렁, 초가 파라솔.</summary>
        static void TelokBatekExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("TelokBatek").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, water = b.Map.BaseWaterY;

            // 망루 둘 (백사장 물가 가까이, 바다를 본다)
            foreach (float x in new[] { 50f, -110f })
            {
                float z = b.Map.ShoreAt(x) - 9f;
                MapKit.Prop(root, "lifeguard", new Vector3(x, b.Height(x, z) - 0.1f, z), 180f, 7.5f);
            }
            // 모래에 끌어 올린 롱테일 보트 (뱃머리가 바다 쪽)와 물에 뜬 것
            for (int i = 0; i < 9; i++)
            {
                float x = -150f + i * 17f + b.Rand(-3f, 3f);
                if (x > -20f && x < 20f) continue;
                bool beached = i % 3 != 0;
                float z = beached ? b.Map.ShoreAt(x) - b.Rand(1f, 5f) : b.Map.ShoreAt(x) + b.Rand(6f, 14f);
                float y = beached ? b.Height(x, z) + 0.15f : water - 0.25f;
                MapKit.Prop(root, i % 2 == 0 ? "longtail_a" : "longtail_b", new Vector3(x, y, z), 180f + b.Rand(-25f, 25f), 0f, 9f);
            }
            // 그물 말리는 대나무 시렁: 기둥 넷 + 가로대 + 늘어진 그물(반투명 짙은 판)
            var bamboo = Mats.Lit(new Color(0.7f, 0.6f, 0.38f), 0.3f);
            var net = Mats.Transparent(new Color(0.12f, 0.2f, 0.22f, 0.55f), 0.2f);
            foreach (float x in new[] { -60f, -80f, 80f })
            {
                float z = b.Map.ShoreAt(x) - 16f, y = b.Height(x, z);
                foreach (float dx in new[] { -3f, 3f })
                    Mats.Prim(PrimitiveType.Cylinder, "RackPole", root, new Vector3(x + dx, y + 1.2f, z), new Vector3(0.12f, 1.2f, 0.12f), bamboo, false);
                Mats.Prim(PrimitiveType.Cylinder, "RackBar", root, new Vector3(x, y + 2.3f, z), new Vector3(0.1f, 3.3f, 0.1f), bamboo, false, Quaternion.Euler(0f, 0f, 90f));
                Mats.Prim(PrimitiveType.Cube, "RackNet", root, new Vector3(x, y + 1.5f, z), new Vector3(6f, 1.6f, 0.04f), net, false);
            }
            // 초가 파라솔 몇 개 (잔교 옆 가게 앞)
            foreach (float x in new[] { -34f, -42f, 28f })
            {
                float z = b.Map.ShoreAt(x) - 12f;
                MapKit.Prop(root, "parasol_thatch", new Vector3(x, b.Height(x, z) - 0.05f, z), b.Rand(0f, 360f), 3.3f);
            }
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>텔록 바텍 이야기 (Lv.36~). 첫 퀘스트는 그 낚시터 연락관(로즈나 지부장)이 준다.</summary>
        static QuestDef[] TelokBatekChapter() => new[]
        {
            new QuestDef { Id = "telokbatek_1", Title = "해 질 녘의 레이디피시", Desc = "슬라맛 다탕! 해가 지면 레이디피시가 얕은 물에서 뛰어요. 루어로 세 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.36부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "ladyfish", Count = 3, RewardGold = 60000, RewardExp = 7000 },
            new QuestDef { Id = "telokbatek_2", Title = "망루 아저씨의 부탁", Desc = "밤 바위 앞엔 파란 줄무늬 엠퍼러가 모인다네. 세 마리 잡아다 주면 망루 아저씨가 고기 지나가는 자리를 알려 준다오.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "spangledemperor", Count = 3, RewardGold = 70000, RewardExp = 8000 },
            new QuestDef { Id = "telokbatek_3", Title = "금빛 노을의 은빛 왕", Desc = "새벽 C3, 저녁 F4에 인도양 타폰 무리가 마을 앞을 지난다오. 90cm가 넘는 놈을 낚으면 롱테일 어부들이 박수를 칠 걸세.",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "tarpon", MinLen = 90f, Count = 1, RewardGold = 130000, RewardExp = 12500 },
        };
    }
}
