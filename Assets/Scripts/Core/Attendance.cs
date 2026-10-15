using System;
using Fishing.Data;

namespace Fishing.Core
{
    /// <summary>
    /// 출석 체크 (원작 2013 공략: 이벤트 기간 7일 출석하면 특수 배 「블랙스피어」).
    /// 솔로판은 <b>실제 날짜</b>로 하루 한 번, 게임을 켜면 도장을 찍는다. 7칸을 채우면 블랙스피어를 주고 다시 1칸부터.
    /// 하루를 건너뛰어도 끊기지 않는다 (모은 칸은 그대로).
    /// </summary>
    public static class Attendance
    {
        public static readonly (string id, int count, string text)[] Rewards =
        {
            ("food_kimbap", 3, "김밥 3개"),
            ("bait_worm", 30, "지렁이 30마리"),
            ("food_coffee", 2, "보온병 믹스커피 2개"),
            (RodUpgrade.ScaleId, 10, "반짝이는 비늘 10개"),
            ("food_dosirak", 2, "바다 도시락 2개"),
            (RodUpgrade.StarSandId, 1, "별모래 1개"),
            (BlackSpearId, 1, "특수 배 「블랙스피어」"),
        };

        public const string BlackSpearId = "boat_blackspear";

        public static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        public static bool CheckedToday => Game.Profile.Data.attendLast == Today;

        /// <summary>오늘 처음 켰으면 도장을 찍고 보상 글을 돌려준다 (이미 찍었으면 null).</summary>
        public static string CheckIn()
        {
            var d = Game.Profile.Data;
            if (d.attendLast == Today) return null;
            d.attendLast = Today;
            d.attendTotal++;
            int slot = d.attendStamp % Rewards.Length;
            d.attendStamp++;
            var (id, count, text) = Rewards[slot];
            var p = Game.Profile;
            if (id == BlackSpearId)
            {
                if (!p.Owns(id)) p.Data.ownedGear.Add(id);
                else { p.AddGold(15000); text = "15,000골드 (블랙스피어는 이미 있음)"; }
            }
            else p.AddItem(id, count);
            p.Log($"출석 {slot + 1}일째 — {text}");
            p.NotifyChanged();
            return $"출석 체크 {slot + 1}/7일째! 보상: {text}" + (slot + 1 < Rewards.Length ? $"  (7일째: 블랙스피어)" : "");
        }
    }
}
