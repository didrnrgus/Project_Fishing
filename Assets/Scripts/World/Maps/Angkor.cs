using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>앙코르와트 (Lv.26): 천 년 사원의 해자와 톤레삽 호수. 이끼 낀 회랑과 연꽃 봉오리 탑, 메콩 대형 메기·자이언트 바브·거대 가오리. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Angkor() => new CoastSpec
        {
            Id = "angkor", MapName = "앙코르와트 · 캄보디아", ShortName = "앙코르와트", ShopName = "시엠립 낚시점", UnionContact = "해자 둑길의 속 찬나 지부장",
            LandName = "사원 둑길", SeaName = "앙코르 해자", Seed = 50, River = true, FarShoreZ = 150f, 
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
            ShopOwner = Local("시엠립 낚시점 사장", "시엠립 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("속 찬나 지부장", "한국 낚시 조합 앙코르 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = AngkorExtra,
        };

        /// <summary>앙코르와트만의 풍경 (임시: 비어 있음).</summary>
        static void AngkorExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>앙코르와트 이야기 (Lv.26~). 첫 퀘스트는 그 낚시터 연락관(속 찬나 지부장)이 준다.</summary>
        static QuestDef[] AngkorChapter() => new QuestDef[0];
    }
}
