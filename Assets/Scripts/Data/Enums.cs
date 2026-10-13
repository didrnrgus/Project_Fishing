namespace Fishing.Data
{
    /// <summary>낚시 포인트의 지형 특성. 어종마다 선호하는 서식지가 다르다.</summary>
    public enum Habitat { Shallow, Deep, Rocky, Weed }

    /// <summary>게임 내 시간대. 어종별 활동 시간에 영향을 준다.</summary>
    public enum TimeBand { Dawn, Morning, Day, Evening, Night }

    public enum Weather { Clear, Cloudy, Rain }

    /// <summary>미끼 종류. Krill~CutBait는 바다 미끼(CutBait = 잡은 고기를 해체해 만든 살점·내장류).</summary>
    public enum BaitKind { Worm, Paste, Corn, Shrimp, Minnow, Lure, Krill, SeaWorm, Squid, CutBait, Egi }

    public static class Names
    {
        public static string Of(Habitat h) => h switch
        {
            Habitat.Shallow => "얕은 여울",
            Habitat.Deep => "깊은 물",
            Habitat.Rocky => "석축·교각",
            Habitat.Weed => "수초",
            _ => h.ToString()
        };

        /// <summary>바다 맵에서는 같은 서식지를 바다 지형 이름으로 부른다.</summary>
        public static string Of(Habitat h, bool sea) => !sea ? Of(h) : h switch
        {
            Habitat.Shallow => "모래 여울",
            Habitat.Deep => "먼바다",
            Habitat.Rocky => "갯바위·암초",
            Habitat.Weed => "해초밭",
            _ => h.ToString()
        };

        public static string Of(TimeBand t) => t switch
        {
            TimeBand.Dawn => "새벽",
            TimeBand.Morning => "아침",
            TimeBand.Day => "낮",
            TimeBand.Evening => "저녁",
            TimeBand.Night => "밤",
            _ => t.ToString()
        };

        public static string Of(Weather w) => w switch
        {
            Weather.Clear => "맑음",
            Weather.Cloudy => "흐림",
            Weather.Rain => "비",
            _ => w.ToString()
        };

        public static string Of(BaitKind b) => b switch
        {
            BaitKind.Worm => "지렁이",
            BaitKind.Paste => "떡밥",
            BaitKind.Corn => "옥수수",
            BaitKind.Shrimp => "새우",
            BaitKind.Minnow => "생미끼",
            BaitKind.Lure => "루어",
            BaitKind.Krill => "크릴",
            BaitKind.SeaWorm => "청갯지렁이",
            BaitKind.Squid => "오징어살",
            BaitKind.CutBait => "해체 미끼",
            BaitKind.Egi => "에기",
            _ => b.ToString()
        };
    }
}
