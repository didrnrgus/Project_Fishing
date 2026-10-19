using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>코사무이 (Lv.32): 숲으로 둘러싸인 해변의 섬. 바다 위 섬에 앉은 황금 대불, 할아버지·할머니 바위, 야자수 해변. 와후·만새기·코비아. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec KohSamui() => new CoastSpec
        {
            Id = "kohsamui", MapName = "코사무이 · 타이만", ShortName = "코사무이", ShopName = "보풋 낚시점", UnionContact = "보풋 어촌의 찻차이 지부장",
            LandName = "보풋 해변", SeaName = "타이만", Seed = 55, 
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
            ShopOwner = Local("보풋 낚시점 사장", "보풋 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("찻차이 지부장", "한국 낚시 조합 코사무이 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = KohSamuiExtra,
        };

        /// <summary>코사무이만의 풍경 (임시: 비어 있음).</summary>
        static void KohSamuiExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>코사무이 이야기 (Lv.32~). 첫 퀘스트는 그 낚시터 연락관(찻차이 지부장)이 준다.</summary>
        static QuestDef[] KohSamuiChapter() => new QuestDef[0];
    }
}
