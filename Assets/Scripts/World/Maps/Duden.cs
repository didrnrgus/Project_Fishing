using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>듀덴 폭포 (Lv.36): 도시 폭포가 절벽에서 바다로 곧장 떨어지는 안탈리아. 뒤로 토로스 산맥. 미거·블루피시, 홍해에서 들어온 은빛뺨복어·쏠배감펭. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Duden() => new CoastSpec
        {
            Id = "duden", MapName = "듀덴 폭포 · 안탈리아", ShortName = "듀덴 폭포", ShopName = "라라 해안 낚시점", UnionContact = "폭포 공원의 메흐메트 지부장",
            LandName = "폭포 공원", SeaName = "안탈리아 만", Seed = 63, 
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
            ShopOwner = Local("라라 해안 낚시점 사장", "라라 해안 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("메흐메트 지부장", "한국 낚시 조합 안탈리아 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = DudenExtra,
        };

        /// <summary>듀덴 폭포만의 풍경 (임시: 비어 있음).</summary>
        static void DudenExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>듀덴 폭포 이야기 (Lv.36~). 첫 퀘스트는 그 낚시터 연락관(메흐메트 지부장)이 준다.</summary>
        static QuestDef[] DudenChapter() => new QuestDef[0];
    }
}
