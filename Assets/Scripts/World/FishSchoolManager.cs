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
        public TimeBand[] PreferredTimes = new TimeBand[0];
        public int Col;
        public int Row;
        /// <summary>돌아다닐 수 있는 행 범위.</summary>
        public int MinRow;
        public int MaxRow = 2;
        public float NextRippleAt;

        /// <summary>
        /// 정해진 시간에만 정해진 칸에 나타나는 특별 어군 (원작 자이언트 그루퍼: 저녁 C4, 아침 E3).
        /// 해당 시간대가 아니면 숨는다(<see cref="Hidden"/>).
        /// </summary>
        public (TimeBand band, int col, int row)[] Schedule;
        public bool Special;
        public bool Hidden;

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
        public string SizeLabel => Special ? "대물 어군" : Big ? "큰 어군" : "작은 어군";
    }

    /// <summary>
    /// 어군. 맵마다 다르다(<see cref="FieldMapDef.CreateSchools"/>).
    /// 2시간(게임 시간)마다 이웃 칸으로 이동하고, 선호 시간대에는 해안·둔치 쪽으로 붙는다.
    /// 어군이 있는 칸에 찌를 던지면 입질이 빠르고 해당 어종이 잘 낚인다.
    /// 원작처럼 큰 어군에서 3마리를 낚으면 작은 어군으로 흩어지고, 6시간 뒤 다시 모인다.
    /// 시간표가 있는 특별 어군은 정해진 시간·칸에만 나타난다.
    /// </summary>
    public class FishSchoolManager : MonoBehaviour
    {
        public const int CatchesToShrink = 3;
        public const float RegroupMinutes = 6 * 60f;

        public readonly List<FishSchool> Schools = new List<FishSchool>();
        TimeBand _lastBand;

        /// <summary>낚시터가 바뀌면 그 맵의 어군으로 바꾼다.</summary>
        public void Reset(List<FishSchool> schools)
        {
            Schools.Clear();
            Schools.AddRange(schools);
            if (Game.Clock != null) ApplySchedules(Game.Clock.Band);
        }

        void Start()
        {
            if (Schools.Count == 0) Reset(FieldMap.Current.CreateSchools());
            Game.Clock.HourChanged += OnHourChanged;
        }

        void OnDestroy()
        {
            if (Game.Clock != null) Game.Clock.HourChanged -= OnHourChanged;
        }

        void ApplySchedules(TimeBand band)
        {
            _lastBand = band;
            foreach (var s in Schools)
            {
                if (s.Schedule == null) continue;
                s.Hidden = true;
                foreach (var (b, col, row) in s.Schedule)
                    if (b == band) { s.Col = col; s.Row = row; s.Hidden = false; }
            }
        }

        void OnHourChanged()
        {
            var band = Game.Clock.Band;
            if (band != _lastBand)
            {
                ApplySchedules(band);
                foreach (var s in Schools)
                    if (s.Schedule != null && !s.Hidden && Game.Mode == GameMode.Field)
                        Game.Toast($"{s.Name} 어군이 {FieldMap.CellName(s.Col, s.Row)} 근처에 나타났다는 소문이 돈다!", ToastKind.Great);
            }
            if (Game.Clock.Hour % 2 != 0) return;
            foreach (var s in Schools)
            {
                if (!s.Big && Game.Clock.TotalMinutes - s.ShrankAt >= RegroupMinutes)
                {
                    s.Big = true;
                    s.CatchesLeft = CatchesToShrink;
                    if (Game.Mode == GameMode.Field) Game.Toast($"{s.Name}가 다시 큰 어군으로 모였습니다.", ToastKind.Info);
                }
                if (s.Schedule != null) continue;
                bool active = System.Array.IndexOf(s.PreferredTimes, band) >= 0;
                s.Col = Mathf.Clamp(s.Col + Random.Range(-1, 2), 0, FieldMap.Cols - 1);
                // 활동 시간대엔 해안 쪽, 아니면 깊은 쪽으로
                int span = Mathf.Max(1, s.MaxRow - s.MinRow);
                int targetRow = active ? s.MinRow + Random.Range(0, Mathf.Max(1, span / 2 + 1)) : s.MinRow + span / 2 + Random.Range(0, span / 2 + 1);
                s.Row = Mathf.Clamp(s.Row + (int)Mathf.Sign(targetRow - s.Row), s.MinRow, s.MaxRow);
            }
        }

        /// <summary>어군 칸에서 그 어군의 어종을 낚았을 때. 큰 어군은 3마리째에 작은 어군이 된다.</summary>
        public void OnCatch(FishSchool school, string speciesId)
        {
            if (school == null || school.Special || !school.Big || !school.Contains(speciesId)) return;
            school.CatchesLeft--;
            if (school.CatchesLeft > 0) return;
            school.Big = false;
            school.ShrankAt = Game.Clock.TotalMinutes;
            Game.Toast($"{school.Name}가 흩어져 작은 어군이 되었습니다. 6시간 뒤 다시 모입니다.", ToastKind.Warn);
        }

        public FishSchool At(int col, int row)
        {
            foreach (var s in Schools)
                if (!s.Hidden && s.Col == col && s.Row == row) return s;
            return null;
        }

        void Update()
        {
            if (Game.Clock != null && Game.Clock.Band != _lastBand) ApplySchedules(Game.Clock.Band);
            // 어군 위치에서 가끔 물고기가 튀어 올라 파문이 생긴다
            foreach (var s in Schools)
            {
                if (s.Hidden || Time.time < s.NextRippleAt) continue;
                // 작은 어군은 덜 자주, 작게 튄다
                s.NextRippleAt = Time.time + (s.Big ? Random.Range(2.5f, 7f) : Random.Range(6f, 14f));
                var center = FieldMap.CellCenter(s.Col, s.Row);
                var pos = center + new Vector3(Random.Range(-FieldMap.CellW * 0.36f, FieldMap.CellW * 0.36f), 0f,
                    Random.Range(-FieldMap.CellH * 0.33f, FieldMap.CellH * 0.33f));
                if (!FieldMap.IsWater(pos)) continue;
                float big = s.Special ? 1.6f : 1f;
                Effects.Ripple(pos, (s.Big ? Random.Range(1.5f, 3f) : Random.Range(0.8f, 1.6f)) * big, 2f);
                Effects.Splash(pos, s.Big ? 6 : 3, (s.Big ? 0.6f : 0.4f) * big);
            }
        }
    }
}
