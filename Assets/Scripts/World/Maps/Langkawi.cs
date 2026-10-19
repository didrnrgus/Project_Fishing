using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>랑카위 (Lv.33): 바다·늪·정글의 섬. 붉은 독수리 상과 산 위 스카이 브리지, 맹그로브 물길. 바라문디·맹그로브 잭·포핑거 스레드핀·GT. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Langkawi() => new CoastSpec
        {
            Id = "langkawi", MapName = "랑카위 · 말레이시아", ShortName = "랑카위", ShopName = "쿠아 낚시점", UnionContact = "독수리 광장의 아지즈 지부장",
            LandName = "쿠아 부두", SeaName = "안다만 해협", Seed = 56, 
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
            ShopOwner = Local("쿠아 낚시점 사장", "쿠아 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("아지즈 지부장", "한국 낚시 조합 랑카위 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = LangkawiExtra,
        };

        /// <summary>랑카위만의 풍경 (임시: 비어 있음).</summary>
        static void LangkawiExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>랑카위 이야기 (Lv.33~). 첫 퀘스트는 그 낚시터 연락관(아지즈 지부장)이 준다.</summary>
        static QuestDef[] LangkawiChapter() => new QuestDef[0];
    }
}
