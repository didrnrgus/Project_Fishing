using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>텔록 바텍 (Lv.36): 고즈넉한 동남아 어촌 마을. 해변 망루와 초가 오두막, 색칠한 롱테일 보트, 금빛 노을. 퀸피시·레이디피시·아처피시. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec TelokBatek() => new CoastSpec
        {
            Id = "telokbatek", MapName = "텔록 바텍 · 말레이시아", ShortName = "텔록 바텍", ShopName = "어촌 잔교 낚시점", UnionContact = "어촌 잔교의 로즈나 지부장",
            LandName = "어촌 마을", SeaName = "남중국해", Seed = 58, 
            WaterDeep = new Color(0.03f, 0.2f, 0.28f), WaterShallow = new Color(0.1f, 0.5f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -40f, ShoreKind.Beach), new ShoreSeg(-40f, 40f, ShoreKind.Quay), new ShoreSeg(40f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.05f, FlatWidth = 30f, CliffHeight = 10f, CliffWidth = 40f, RoadOffset = 16f, VillageDensity = 0.8f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Tropical,
            Grid = new[,]
            {
                { S, S, S, S, S, S, S, S },
                { S, D, D, D, D, D, D, S },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(4f, 1.1f, -47f), ShopPos = new Vector3(-16f, 1f, -52f),
            ShopOwner = Local("어촌 잔교 낚시점 사장", "어촌 잔교 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("로즈나 지부장", "한국 낚시 조합 텔록 바텍 연락소", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = TelokBatekExtra,
        };

        /// <summary>텔록 바텍만의 풍경 (임시: 비어 있음).</summary>
        static void TelokBatekExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>텔록 바텍 이야기 (Lv.36~). 첫 퀘스트는 그 낚시터 연락관(로즈나 지부장)이 준다.</summary>
        static QuestDef[] TelokBatekChapter() => new QuestDef[0];
    }
}
