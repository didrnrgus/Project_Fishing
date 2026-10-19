using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>돈콘 (Lv.28): 메콩강 4천 개 섬 지대의 수상 가옥 마을. 물탱크 탑과 나무 선착장, 여울과 바위섬. 메콩 대형 메기·줄리엔 잉어·자이언트 파항가시우스. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec DonKhone() => new CoastSpec
        {
            Id = "donkhone", MapName = "돈콘 · 라오스", ShortName = "돈콘", ShopName = "돈콘 수상 낚시점", UnionContact = "나무 선착장의 분미 지부장",
            LandName = "수상 마을", SeaName = "메콩강", Seed = 52, River = true, FarShoreZ = 150f, 
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
            ShopOwner = Local("돈콘 수상 낚시점 사장", "돈콘 수상 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("분미 지부장", "한국 낚시 조합 메콩 지부", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = DonKhoneExtra,
        };

        /// <summary>돈콘만의 풍경 (임시: 비어 있음).</summary>
        static void DonKhoneExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>돈콘 이야기 (Lv.28~). 첫 퀘스트는 그 낚시터 연락관(분미 지부장)이 준다.</summary>
        static QuestDef[] DonKhoneChapter() => new QuestDef[0];
    }
}
