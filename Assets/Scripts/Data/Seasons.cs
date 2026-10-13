namespace Fishing.Data
{
    public enum Season { Spring, Summer, Autumn, Winter }

    /// <summary>
    /// 계절 (원작: 시간대·눈·비·장마에 따라 어종이 달라졌다). 솔로판은 계절을 체감할 수 있게 <b>한 달 = 게임 7일</b>.
    /// 1일째가 4월(봄)이고, 3~5월 봄 · 6~8월 여름 · 9~11월 가을 · 12~2월 겨울. 한 계절 = 21일 (현실 약 4시간).
    /// 어종마다 제철이 있으면(<see cref="FishSpecies.Seasons"/>) 제철 1.6배, 아니면 0.35배. 빙어는 겨울에만.
    /// </summary>
    public static class Seasons
    {
        public const int DaysPerMonth = 7;

        /// <summary>게임 날짜(1~)의 달 (1~12).</summary>
        public static int MonthOf(int day) => (3 + (day - 1) / DaysPerMonth) % 12 + 1;

        /// <summary>달 안의 날짜 표시 (7일을 1·5·9 … 25일로 펼친다).</summary>
        public static int DayOfMonth(int day) => 1 + ((day - 1) % DaysPerMonth) * 4;

        public static Season SeasonOf(int day)
        {
            int m = MonthOf(day);
            return m >= 3 && m <= 5 ? Season.Spring : m >= 6 && m <= 8 ? Season.Summer : m >= 9 && m <= 11 ? Season.Autumn : Season.Winter;
        }

        public static Season Current => SeasonOf(Core.Game.Clock != null ? Core.Game.Clock.Day : 1);
        public static bool IsWinter => Current == Season.Winter;

        /// <summary>그 계절이 처음 시작하는 날 (1일째 이후로).</summary>
        public static int FirstDayOf(Season s, int from = 1)
        {
            for (int d = from; d < from + 400; d++)
                if (SeasonOf(d) == s && (d == from || SeasonOf(d - 1) != s)) return d;
            return from;
        }

        public static string Name(Season s) => s switch { Season.Spring => "봄", Season.Summer => "여름", Season.Autumn => "가을", _ => "겨울" };

        /// <summary>겨울에는 비가 눈이다.</summary>
        public static string WeatherName(Weather w) => w == Weather.Rain && IsWinter ? "눈" : Names.Of(w);

        /// <summary>입질 가중치 배율 (제철 1.6, 철 아님 0.35, 빙어는 겨울이 아니면 0).</summary>
        public static float BiteFactor(FishSpecies s, Season now)
        {
            if (s.Seasons == null || s.Seasons.Length == 0) return 1f;
            bool inSeason = System.Array.IndexOf(s.Seasons, now) >= 0;
            if (inSeason) return 1.6f;
            return s.Id == "smelt" ? 0f : 0.35f;
        }
    }
}
