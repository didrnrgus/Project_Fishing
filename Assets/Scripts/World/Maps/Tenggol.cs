using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>플라우 탱글 (Lv.39): 나만의 특별한 해양 별장. 바다 위 나무 데크 별장과 산호초, 정글 바위섬. 도그투스 참치·범프헤드 패럿피시·코랄 트라우트. (임시 틀 — 지형·랜드마크·어군은 아직)</summary>
        public static CoastSpec Tenggol() => new CoastSpec
        {
            Id = "tenggol", MapName = "플라우 탱글 · 말레이시아", ShortName = "탱글", ShopName = "수상 별장 낚시점", UnionContact = "수상 별장의 아이샤 지부장",
            LandName = "수상 별장", SeaName = "산호초 바다", Seed = 60, 
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
            ShopOwner = Local("수상 별장 낚시점 사장", "수상 별장 낚시점", new Color(0.9f, 0.85f, 0.6f), hat: 1),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("아이샤 지부장", "한국 낚시 조합 탱글 연락소", false, false), NpcAngler.Role.Talker, "union"),
            },
            Schools = () => new List<FishSchool>(),
            Extra = TenggolExtra,
        };

        /// <summary>플라우 탱글만의 풍경 (임시: 비어 있음).</summary>
        static void TenggolExtra(CoastWorldBuilder b)
        {
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>플라우 탱글 이야기 (Lv.39~). 첫 퀘스트는 그 낚시터 연락관(아이샤 지부장)이 준다.</summary>
        static QuestDef[] TenggolChapter() => new QuestDef[0];
    }
}
