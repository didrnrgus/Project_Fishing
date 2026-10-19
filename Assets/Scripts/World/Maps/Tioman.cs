using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>플라우 티오만 (Lv.37): 한적하고 평화로운 리조트. 언덕의 둥근 리조트 호텔, 보라 파라솔 백사장, 용의 뿔 봉우리. 돛새치·와후·나폴레옹피시. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Tioman() => new CoastSpec
        {
            Id = "tioman", MapName = "플라우 티오만 · 남중국해", ShortName = "티오만", ShopName = "티오만 리조트 낚시점", UnionContact = "리조트 부두의 파리다 지부장",
            LandName = "리조트 해변", SeaName = "티오만 앞바다", Seed = 59, 
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
            ShopOwner = Local("티오만 리조트 낚시점 사장", "티오만 리조트 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("파리다 지부장", "한국 낚시 조합 티오만 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = TiomanExtra,
        };

        /// <summary>플라우 티오만만의 풍경 (임시: 비어 있음).</summary>
        static void TiomanExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>플라우 티오만 이야기 (Lv.37~). 첫 퀘스트는 그 낚시터 연락관(파리다 지부장)이 준다.</summary>
        static QuestDef[] TiomanChapter() => new QuestDef[0];
    }
}
