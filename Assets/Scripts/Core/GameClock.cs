using System;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>게임 내 시계. 현실 1초 = 게임 2분 (하루 12분).</summary>
    public class GameClock : MonoBehaviour
    {
        public const float GameMinutesPerSecond = 2f;
        public const float MinutesPerDay = 24 * 60f;

        public int Day { get; private set; } = 1;
        public float Minutes { get; private set; } = 6 * 60f;
        public float Speed = 1f;

        public event Action HourChanged;

        public int Hour => (int)(Minutes / 60f);
        public int Minute => (int)(Minutes % 60f);
        public float TotalMinutes => Day * MinutesPerDay + Minutes;
        public TimeBand Band => BandOf(Minutes);
        public string TimeText => $"{Hour:00}:{Minute:00}";

        public void Set(int day, float minutes)
        {
            Day = Mathf.Max(1, day);
            Minutes = Mathf.Repeat(minutes, MinutesPerDay);
            HourChanged?.Invoke();
        }

        void Update()
        {
            int prevHour = Hour;
            Minutes += Time.deltaTime * GameMinutesPerSecond * Speed;
            while (Minutes >= MinutesPerDay)
            {
                Minutes -= MinutesPerDay;
                Day++;
            }
            if (Hour != prevHour) HourChanged?.Invoke();
        }

        public static TimeBand BandOf(float minutes)
        {
            float h = minutes / 60f;
            if (h >= 4f && h < 7f) return TimeBand.Dawn;
            if (h >= 7f && h < 11f) return TimeBand.Morning;
            if (h >= 11f && h < 17f) return TimeBand.Day;
            if (h >= 17f && h < 20f) return TimeBand.Evening;
            return TimeBand.Night;
        }
    }
}
