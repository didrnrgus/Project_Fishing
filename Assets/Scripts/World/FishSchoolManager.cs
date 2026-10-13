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

        /// <summary>큰 어군이면 true. 큰 어군에서 <see cref="FishSchoolManager.CatchesToShrink"/>마리를 낚으면 작은 어군이 된다.</summary>
        public bool Big = true;
        public int CatchesLeft = FishSchoolManager.CatchesToShrink;
        /// <summary>작은 어군이 된 게임 시각(TotalMinutes). 일정 시간이 지나면 다시 큰 어군으로 모인다.</summary>
        public float ShrankAt;

        public bool Contains(string speciesId) => System.Array.IndexOf(SpeciesIds, speciesId) >= 0;

        /// <summary>해당 어종의 입질 가중치 배율.</summary>
        public float BiteBoost => Big ? 4f : 2f;
        /// <summary>입질까지 기다리는 시간 배율.</summary>
        public float WaitFactor => Big ? 0.5f : 0.75f;
        /// <summary>길이 분포 지수 (작을수록 큰 고기가 잘 나온다).</summary>
        public float LengthSkew => Big ? 1.7f : 2f;
        public string SizeLabel => Big ? "큰 어군" : "작은 어군";
    }

    /// <summary>
    /// 어군. 2시간(게임 시간)마다 이웃 칸으로 이동하고, 선호 시간대에는 둔치 쪽으로 붙는다.
    /// 어군이 있는 칸에 찌를 던지면 입질이 빠르고 해당 어종이 잘 낚인다.
    /// 원작처럼 큰 어군에서 3마리를 낚으면 작은 어군으로 흩어지고, 6시간 뒤 다시 모인다.
    /// </summary>
    public class FishSchoolManager : MonoBehaviour
    {
        public const int CatchesToShrink = 3;
        public const float RegroupMinutes = 6 * 60f;

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
                if (!s.Big && Game.Clock.TotalMinutes - s.ShrankAt >= RegroupMinutes)
                {
                    s.Big = true;
                    s.CatchesLeft = CatchesToShrink;
                    if (Game.Mode == GameMode.Field) Game.Toast($"{s.Name}가 다시 큰 어군으로 모였습니다.", ToastKind.Info);
                }
                bool active = System.Array.IndexOf(s.PreferredTimes, band) >= 0;
                s.Col = Mathf.Clamp(s.Col + Random.Range(-1, 2), 0, HanRiverMap.Cols - 1);
                // 활동 시간대엔 둔치 가까이(0~1행), 아니면 깊은 곳(1~2행)으로
                int targetRow = active ? Random.Range(0, 2) : Random.Range(1, 3);
                s.Row = Mathf.Clamp(s.Row + (int)Mathf.Sign(targetRow - s.Row), 0, 2);
            }
        }

        /// <summary>어군 칸에서 그 어군의 어종을 낚았을 때. 큰 어군은 3마리째에 작은 어군이 된다.</summary>
        public void OnCatch(FishSchool school, string speciesId)
        {
            if (school == null || !school.Big || !school.Contains(speciesId)) return;
            school.CatchesLeft--;
            if (school.CatchesLeft > 0) return;
            school.Big = false;
            school.ShrankAt = Game.Clock.TotalMinutes;
            Game.Toast($"{school.Name}가 흩어져 작은 어군이 되었습니다. 6시간 뒤 다시 모입니다.", ToastKind.Warn);
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
                // 작은 어군은 덜 자주, 작게 튄다
                s.NextRippleAt = Time.time + (s.Big ? Random.Range(2.5f, 7f) : Random.Range(6f, 14f));
                var center = HanRiverMap.CellCenter(s.Col, s.Row);
                var pos = center + new Vector3(Random.Range(-18f, 18f), 0f, Random.Range(-10f, 10f));
                if (!HanRiverMap.IsWater(pos)) continue;
                Effects.Ripple(pos, s.Big ? Random.Range(1.5f, 3f) : Random.Range(0.8f, 1.6f), 2f);
                Effects.Splash(pos, s.Big ? 6 : 3, s.Big ? 0.6f : 0.4f);
            }
        }
    }
}
