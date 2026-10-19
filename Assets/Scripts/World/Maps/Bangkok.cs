using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>방콕 (Lv.31): 짜오프라야 강가와 낚시공원. 강 건너 새벽 사원 왓 아룬, 롱테일 보트와 수상 버스. 자이언트 스네이크헤드·피라루쿠·앨리게이터 가. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Bangkok() => new CoastSpec
        {
            Id = "bangkok", MapName = "방콕 · 짜오프라야강", ShortName = "방콕", ShopName = "짜오프라야 낚시점", UnionContact = "강변 선착장의 니란 지부장",
            LandName = "강변 선착장", SeaName = "짜오프라야강", Seed = 54, River = true, FarShoreZ = 150f, 
            WaterDeep = new Color(0.03f, 0.2f, 0.28f), WaterShallow = new Color(0.1f, 0.5f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -40f, ShoreKind.Beach), new ShoreSeg(-40f, 40f, ShoreKind.Quay), new ShoreSeg(40f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.05f, FlatWidth = 30f, CliffHeight = 10f, CliffWidth = 40f, RoadOffset = 16f, VillageDensity = 0.8f,
            Houses = HouseStyle.Stilt, TreeKind = TreeStyle.Tropical,
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
            ShopOwner = Local("짜오프라야 낚시점 사장", "짜오프라야 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("니란 지부장", "한국 낚시 조합 방콕 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = BangkokExtra,
        };

        /// <summary>방콕만의 풍경 (임시: 비어 있음).</summary>
        static void BangkokExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>방콕 이야기 (Lv.31~). 첫 퀘스트는 그 낚시터 연락관(니란 지부장)이 준다.</summary>
        static QuestDef[] BangkokChapter() => new QuestDef[0];
    }
}
