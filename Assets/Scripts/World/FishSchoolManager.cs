using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public class FishSchool
    {
        public string Name;
        public string[] SpeciesIds;
        public TimeBand[] PreferredTimes;
        public int Col;
        public int Row;
        public float NextRippleAt;

        public bool Contains(string speciesId) => System.Array.IndexOf(SpeciesIds, speciesId) >= 0;
    }

    /// <summary>
    /// 어군. 2시간(게임 시간)마다 이웃 칸으로 이동하고, 선호 시간대에는 둔치 쪽으로 붙는다.
    /// 어군이 있는 칸에 찌를 던지면 입질이 빠르고 해당 어종이 잘 낚인다.
    /// </summary>
    public class FishSchoolManager : MonoBehaviour
    {
        public readonly List<FishSchool> Schools = new List<FishSchool>();

        void Start()
        {
            Schools.Add(new FishSchool { Name = "붕어 떼", SpeciesIds = new[] { "crucian", "bluegill", "zacco" },
                PreferredTimes = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Col = 4, Row = 0 });
            Schools.Add(new FishSchool { Name = "잉어 무리", SpeciesIds = new[] { "carp", "goldkoi" },
                PreferredTimes = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Col = 3, Row = 1 });
            Schools.Add(new FishSchool { Name = "끄리·강준치 떼", SpeciesIds = new[] { "kkeuri", "skygager", "mullet" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Day }, Col = 2, Row = 1 });
            Schools.Add(new FishSchool { Name = "메기·동자개", SpeciesIds = new[] { "catfish", "bagrid", "eel" },
                PreferredTimes = new[] { TimeBand.Evening, TimeBand.Night }, Col = 1, Row = 0 });

            Game.Clock.HourChanged += OnHourChanged;
        }

        void OnDestroy()
        {
            if (Game.Clock != null) Game.Clock.HourChanged -= OnHourChanged;
        }

        void OnHourChanged()
        {
            if (Game.Clock.Hour % 2 != 0) return;
            var band = Game.Clock.Band;
            foreach (var s in Schools)
            {
                bool active = System.Array.IndexOf(s.PreferredTimes, band) >= 0;
                s.Col = Mathf.Clamp(s.Col + Random.Range(-1, 2), 0, HanRiverMap.Cols - 1);
                // 활동 시간대엔 둔치 가까이(0~1행), 아니면 깊은 곳(1~2행)으로
                int targetRow = active ? Random.Range(0, 2) : Random.Range(1, 3);
                s.Row = Mathf.Clamp(s.Row + (int)Mathf.Sign(targetRow - s.Row), 0, 2);
            }
        }

        public FishSchool At(int col, int row)
        {
            foreach (var s in Schools)
                if (s.Col == col && s.Row == row) return s;
            return null;
        }

        void Update()
        {
            // 어군 위치에서 가끔 물고기가 튀어 올라 파문이 생긴다
            foreach (var s in Schools)
            {
                if (Time.time < s.NextRippleAt) continue;
                s.NextRippleAt = Time.time + Random.Range(2.5f, 7f);
                var center = HanRiverMap.CellCenter(s.Col, s.Row);
                var pos = center + new Vector3(Random.Range(-18f, 18f), 0f, Random.Range(-10f, 10f));
                if (!HanRiverMap.IsWater(pos)) continue;
                Effects.Ripple(pos, Random.Range(1.5f, 3f), 2f);
                Effects.Splash(pos, 6, 0.6f);
            }
        }
    }
}
