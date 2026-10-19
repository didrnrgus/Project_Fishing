using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 한강(반포지구) 맵 정의. 강은 X축을 따라 흐르고, 남쪽 둔치(z &lt; -60)에서 시작한다.
    /// 수면은 50m x 30m 격자(A~F열, 1~4행)로 나뉘고, 칸마다 서식지가 정해져 있다.
    /// </summary>
    public static class HanRiverMap
    {
        public const string MapName = "한강 · 반포지구";

        public const float WaterY = 0.2f;
        public const float LandY = 1.0f;
        public const float RiverMinZ = -60f;
        public const float RiverMaxZ = 60f;
        public const float MinX = -160f;
        public const float MaxX = 160f;
        public const float LandFarZ = 110f;

        public const float GridMinX = -150f;
        public const float CellW = 50f;
        public const float CellH = 30f;
        public const int Cols = 6;
        public const int Rows = 4;

        public const float BridgeX = -75f;
        public static readonly float[] BridgePierZ = { -42f, -14f, 14f, 42f };

        // 낚시 데크: 남쪽 둔치에서 강 쪽으로 뻗은 잔교
        public static readonly Rect Pier = new Rect(23f, -62f, 4f, 36f);

        public static readonly Vector3 Spawn = new Vector3(-10f, LandY + 0.1f, -70f);
        /// <summary>공원 버스킹 무대 (낚시 아이돌 유니티짱). 무대 앞이 산책로·강 쪽(+z)을 본다.</summary>
        public static readonly Vector3 IdolStage = new Vector3(Pier.center.x - 14f, LandY, RiverMinZ - 11f);
        public const float IdolStageYaw = -20f;
        // 시작 위치 왼쪽 뒤편. 3인칭 카메라가 건물 안으로 들어가지 않도록 스폰 바로 뒤는 비워 둔다.
        public static readonly Vector3 ShopPos = new Vector3(-17f, LandY, -76f);
        public const float ShopRadius = 10f;
        /// <summary>CU 편의점 (자전거도로 남쪽, 앞면이 강 쪽 +z). 가게 8m + 앞마당 파라솔 탁자.</summary>
        public static readonly Vector3 StorePos = new Vector3(-52f, LandY, -100f);

        static readonly string[] ColNames = { "A", "B", "C", "D", "E", "F" };

        // [row, col], row 0이 남쪽 둔치에 가장 가깝다
        static readonly Habitat[,] HabitatGrid =
        {
            { Habitat.Shallow, Habitat.Rocky, Habitat.Shallow, Habitat.Deep, Habitat.Weed, Habitat.Weed },
            { Habitat.Deep, Habitat.Rocky, Habitat.Deep, Habitat.Deep, Habitat.Shallow, Habitat.Deep },
            { Habitat.Deep, Habitat.Rocky, Habitat.Deep, Habitat.Deep, Habitat.Deep, Habitat.Deep },
            { Habitat.Shallow, Habitat.Rocky, Habitat.Deep, Habitat.Deep, Habitat.Deep, Habitat.Shallow },
        };

        public static bool OnPier(Vector3 p, float margin = 0f)
        {
            return p.x > Pier.xMin + margin && p.x < Pier.xMax - margin && p.z > Pier.yMin && p.z < Pier.yMax - margin;
        }

        public static bool OnBridgePier(Vector3 p)
        {
            if (Mathf.Abs(p.x - BridgeX) > 3f) return false;
            foreach (var z in BridgePierZ)
                if (Mathf.Abs(p.z - z) < 4f) return true;
            return false;
        }

        public static bool IsWater(Vector3 p)
        {
            return p.x > MinX && p.x < MaxX && p.z > RiverMinZ && p.z < RiverMaxZ && !OnPier(p) && !OnBridgePier(p);
        }

        public static bool IsWalkable(Vector3 p)
        {
            if (p.x < MinX + 2f || p.x > MaxX - 2f) return false;
            if (p.z < RiverMinZ - 0.6f && p.z > -LandFarZ + 2f) return true;
            if (p.z > RiverMaxZ + 0.6f && p.z < LandFarZ - 2f) return true;
            return OnPier(p, 0.45f);
        }

        public static bool TryCell(Vector3 p, out int col, out int row)
        {
            col = Mathf.FloorToInt((p.x - GridMinX) / CellW);
            row = Mathf.FloorToInt((p.z - RiverMinZ) / CellH);
            return col >= 0 && col < Cols && row >= 0 && row < Rows && IsWater(p);
        }

        public static string CellName(int col, int row) => ColNames[col] + (row + 1);

        public static Habitat HabitatAt(int col, int row) => HabitatGrid[row, col];

        public static Vector3 CellCenter(int col, int row)
        {
            return new Vector3(GridMinX + (col + 0.5f) * CellW, WaterY, RiverMinZ + (row + 0.5f) * CellH);
        }

        public static string AreaName(Vector3 p)
        {
            if (OnPier(p)) return "반포 낚시 데크";
            if (p.x < -100f) return "서쪽 둔치";
            if (p.x < -50f) return "동작대교 아래";
            if (p.x < 0f) return "반포 한강공원";
            if (p.x < 50f) return "반포 낚시 데크 입구";
            if (p.x < 100f) return "서래섬 여울";
            return "동쪽 수초 만";
        }
    }
}
