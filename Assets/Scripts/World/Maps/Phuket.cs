using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>푸켓 (Lv.30): 안다만해의 진주. 언덕 위 하얀 대불과 팡아만 석회암 섬, 롱테일 보트. 돛새치·청새치·황다랑어·GT. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Phuket() => new CoastSpec
        {
            Id = "phuket", MapName = "푸켓 · 안다만해", ShortName = "푸켓", ShopName = "찰롱 낚시점", UnionContact = "찰롱 부두의 솜차이 지부장",
            LandName = "찰롱 부두", SeaName = "안다만해", Seed = 53, 
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
            ShopOwner = Local("찰롱 낚시점 사장", "찰롱 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("솜차이 지부장", "한국 낚시 조합 푸켓 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = PhuketExtra,
        };

        /// <summary>푸켓만의 풍경 (임시: 비어 있음).</summary>
        static void PhuketExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>푸켓 이야기 (Lv.30~). 첫 퀘스트는 그 낚시터 연락관(솜차이 지부장)이 준다.</summary>
        static QuestDef[] PhuketChapter() => new QuestDef[0];
    }
}
