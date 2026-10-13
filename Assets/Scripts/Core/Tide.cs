using Fishing.World;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 물때 (밀물·썰물). 서해(서천 앞바다)처럼 <see cref="FieldMapDef.TideAmplitude"/>가 있는 맵에서만 움직인다.
    ///  - 게임 시각으로 12시간 25분마다 한 번 들고 난다 (실제 반일주조와 같은 주기)
    ///  - 수면 높이(FieldMap.WaterY)와 물 메쉬가 함께 오르내린다. 썰물이면 갯벌이 드러나 걸어 들어갈 수 있고,
    ///    물이 들어오면 갯벌 위에 있던 사람은 둔치로 돌아온다
    ///  - 들물(물이 들어올 때) 입질이 빠르고, 물이 멈춘 만조·간조(정조)에는 뜸하다 — 서해 꾼들이 물때표를 보는 이유
    /// </summary>
    public class Tide : MonoBehaviour
    {
        public const float PeriodMinutes = 12f * 60f + 25f;

        /// <summary>-1(간조) ~ 1(만조).</summary>
        public static float Level { get; private set; }
        /// <summary>물이 들어오는 중이면 true.</summary>
        public static bool Rising { get; private set; }
        public static bool Active => FieldMap.Current != null && FieldMap.Current.TideAmplitude > 0f;

        float _floodCheckAt;

        /// <summary>게임 시각(분)의 물 높이 -1~1.</summary>
        public static float LevelAt(float totalMinutes) => Mathf.Sin(totalMinutes / PeriodMinutes * Mathf.PI * 2f);

        /// <summary>HUD 표시: 들물 ▲ / 날물 ▼ / 만조 / 간조.</summary>
        public static string Name
        {
            get
            {
                if (!Active) return "";
                if (Level > 0.85f) return "만조";
                if (Level < -0.85f) return "간조";
                return Rising ? "들물 ▲" : "날물 ▼";
            }
        }

        /// <summary>입질 대기 시간 배율: 들물 0.75, 날물 1, 정조(만조·간조) 1.25. 물때가 없는 맵은 1.</summary>
        public static float WaitFactor
        {
            get
            {
                if (!Active) return 1f;
                if (Mathf.Abs(Level) > 0.85f) return 1.25f;
                return Rising ? 0.75f : 1f;
            }
        }

        void Update()
        {
            var map = FieldMap.Current;
            if (map == null || Game.Clock == null || map.TideAmplitude <= 0f) return;
            float t = Game.Clock.TotalMinutes;
            Level = LevelAt(t);
            Rising = LevelAt(t + 1f) > Level;
            map.WaterY = map.BaseWaterY + Level * map.TideAmplitude;
            if (WaterSurface.Current != null)
            {
                var p = WaterSurface.Current.localPosition;
                WaterSurface.Current.localPosition = new Vector3(p.x, map.WaterY, p.z);
            }

            // 갯벌에 서 있었는데 물이 들어왔으면 둔치로
            if (Time.time >= _floodCheckAt && Game.Player != null && Game.Mode == GameMode.Field && (Game.Boat == null || !Game.Boat.Riding))
            {
                _floodCheckAt = Time.time + 0.5f;
                var pos = Game.Player.transform.position;
                if (!map.IsWalkable(pos) && map.IsWater(pos))
                {
                    Game.Player.Teleport(new Vector3(pos.x, map.LandY + 0.1f, map.GridMinZ - 3f), Game.Player.Yaw);
                    Game.Toast("물이 들어와서 둔치로 피했습니다. 서해는 물때를 꼭 보세요!", ToastKind.Warn);
                }
            }
        }
    }
}
