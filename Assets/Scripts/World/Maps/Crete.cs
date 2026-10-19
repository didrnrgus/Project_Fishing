using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>크레타 (Lv.30): 지중해에서 가장 큰 섬, 하니아의 베네치아 항구. 돌 등대와 파스텔 집, 둥근 지붕 모스크, 뒤로 하얀 산. 덴텍스·그루퍼·만새기. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Crete() => new CoastSpec
        {
            Id = "crete", MapName = "크레타 · 하니아", ShortName = "크레타", ShopName = "하니아 항구 낚시점", UnionContact = "베네치아 항구의 디미트리스 지부장",
            LandName = "베네치아 항구", SeaName = "크레타 바다", Seed = 61, 
            WaterDeep = new Color(0.03f, 0.2f, 0.28f), WaterShallow = new Color(0.1f, 0.5f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -40f, ShoreKind.Beach), new ShoreSeg(-40f, 40f, ShoreKind.Quay), new ShoreSeg(40f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.05f, FlatWidth = 30f, CliffHeight = 10f, CliffWidth = 40f, RoadOffset = 16f, VillageDensity = 0.8f,
            Houses = HouseStyle.Venetian, TreeKind = TreeStyle.Mediterranean,
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
            ShopOwner = Local("하니아 항구 낚시점 사장", "하니아 항구 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("디미트리스 지부장", "한국 낚시 조합 크레타 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = CreteExtra,
        };

        /// <summary>크레타만의 풍경 (임시: 비어 있음).</summary>
        static void CreteExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>크레타 이야기 (Lv.30~). 첫 퀘스트는 그 낚시터 연락관(디미트리스 지부장)이 준다.</summary>
        static QuestDef[] CreteChapter() => new QuestDef[0];
    }
}
