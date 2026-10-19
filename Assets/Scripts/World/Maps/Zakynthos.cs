using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>자킨토스 (Lv.33): 이오니아해의 휴양섬. 하얀 절벽에 둘러싸인 난파선 해변 나바지오와 푸른 동굴. 참다랑어·청상아리·황새치. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Zakynthos() => new CoastSpec
        {
            Id = "zakynthos", MapName = "자킨토스 · 이오니아해", ShortName = "자킨토스", ShopName = "나바지오 낚시점", UnionContact = "포르토 브로미의 엘레나 지부장",
            LandName = "포르토 브로미", SeaName = "이오니아해", Seed = 62, 
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
            ShopOwner = Local("나바지오 낚시점 사장", "나바지오 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("엘레나 지부장", "한국 낚시 조합 자킨토스 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = ZakynthosExtra,
        };

        /// <summary>자킨토스만의 풍경 (임시: 비어 있음).</summary>
        static void ZakynthosExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>자킨토스 이야기 (Lv.33~). 첫 퀘스트는 그 낚시터 연락관(엘레나 지부장)이 준다.</summary>
        static QuestDef[] ZakynthosChapter() => new QuestDef[0];
    }
}
