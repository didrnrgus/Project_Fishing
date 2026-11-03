using System.Linq;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Buddy
{
    /// <summary>
    /// "붕어 어디서 잡아?"에 윤슬이 데려갈 낚시 포인트를 고른다.
    /// 이 낚시터 칸(A1…)마다 그 고기가 사는 포인트(수초·여울·암초…)인지 보고, 칸 안의 물에서 가장 가까운 물가(걸을 수 있는 땅)를 찾는다.
    /// 물가에서 낚싯대가 닿으면 걸어서, 안 닿으면(먼바다·섬 건너) 배를 타야 한다 — 그때 물가는 배를 띄울 자리가 된다.
    /// </summary>
    public static class BuddySpots
    {
        public struct Spot
        {
            /// <summary>설 물가 (배를 타야 하면 배를 띄울 물가).</summary>
            public Vector3 Shore;
            /// <summary>던질 물 (포인트).</summary>
            public Vector3 Water;
            public bool Boat;
            public string Cell, Habitat;
        }

        const int Mask = ~(1 << 2);

        /// <summary>reach: 물가에서 낚싯대가 닿는 거리. canBoat: 배가 있는지 (없으면 걸어서 갈 수 있는 곳만).</summary>
        public static Spot? Find(FishSpecies s, Vector3 from, float reach, bool canBoat)
        {
            var map = FieldMap.Current;
            Spot? best = null;
            float bestCost = float.MaxValue;
            for (int c = 0; c < map.Cols; c++)
                for (int r = 0; r < map.Rows; r++)
                {
                    var h = map.HabitatAt(c, r);
                    if (s.Habitats != null && s.Habitats.Length > 0 && !s.Habitats.Contains(h)) continue;
                    var water = NearestWater(map, c, r, from);
                    if (!water.HasValue) continue;
                    if (s.FarSeaOnly && !map.IsFarSea(water.Value)) continue;
                    var shore = Shore(map, water.Value, from, out float gap);
                    if (!shore.HasValue) continue;
                    bool boat = gap > reach;
                    if (boat && !canBoat) continue;
                    // 걸어서 가는 곳을 먼저 (배는 띄우고 몰고 가야 하니 80m 더 먼 셈)
                    float cost = Flat(shore.Value - from) + (boat ? 80f : 0f);
                    if (cost >= bestCost) continue;
                    bestCost = cost;
                    best = new Spot { Shore = shore.Value, Water = water.Value, Boat = boat, Cell = map.CellName(c, r), Habitat = map.HabitatName(h) };
                }
            return best;
        }

        /// <summary>칸 안의 물 중 플레이어에게 가장 가까운 점 (칸을 5×5로 본다).</summary>
        static Vector3? NearestWater(FieldMapDef map, int c, int r, Vector3 from)
        {
            Vector3? best = null;
            float bestD = float.MaxValue;
            var center = map.CellCenter(c, r);
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    var p = center + new Vector3((i - 2) * map.CellW * 0.19f, 0f, (j - 2) * map.CellH * 0.19f);
                    if (!map.IsWater(p)) continue;
                    float d = Flat(p - from);
                    if (d < bestD) { bestD = d; best = p; }
                }
            return best;
        }

        /// <summary>물에서 바깥으로 2m씩 둘레를 넓혀 처음 만나는 걸을 수 있는 땅 (그 둘레에서 플레이어에게 가까운 점). gap = 물에서 그 땅까지.</summary>
        static Vector3? Shore(FieldMapDef map, Vector3 water, Vector3 from, out float gap)
        {
            for (float radius = 2f; radius <= 60f; radius += 2f)
            {
                Vector3? best = null;
                float bestD = float.MaxValue;
                for (int k = 0; k < 24; k++)
                {
                    float a = k * Mathf.PI * 2f / 24f;
                    var p = water + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    if (!Physics.Raycast(new Vector3(p.x, water.y + 40f, p.z), Vector3.down, out var hit, 80f, Mask, QueryTriggerInteraction.Ignore)) continue;
                    if (map.IsWater(hit.point) || !map.IsWalkable(hit.point)) continue;
                    float d = Flat(hit.point - from);
                    if (d < bestD) { bestD = d; best = hit.point; }
                }
                if (best.HasValue) { gap = radius; return best; }
            }
            gap = float.MaxValue;
            return null;
        }

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
