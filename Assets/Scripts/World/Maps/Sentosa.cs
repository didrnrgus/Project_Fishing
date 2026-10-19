using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>센토사 (Lv.35): 동남아 여행의 종착역, 해양 스포츠의 중심지. 거대한 머라이언, 바다 건너 마천루, 하늘의 열기구와 요트 마리나. GT·바라문디·스내퍼. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Sentosa() => new CoastSpec
        {
            Id = "sentosa", MapName = "센토사 · 싱가포르", ShortName = "센토사", ShopName = "센토사 마리나 낚시점", UnionContact = "머라이언 광장의 탄 지부장",
            LandName = "머라이언 광장", SeaName = "싱가포르 해협", Seed = 57, 
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
            ShopOwner = Local("센토사 마리나 낚시점 사장", "센토사 마리나 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("탄 지부장", "한국 낚시 조합 싱가포르 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = SentosaExtra,
        };

        /// <summary>센토사만의 풍경 (임시: 비어 있음).</summary>
        static void SentosaExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>센토사 이야기 (Lv.35~). 첫 퀘스트는 그 낚시터 연락관(탄 지부장)이 준다.</summary>
        static QuestDef[] SentosaChapter() => new QuestDef[0];
    }
}
