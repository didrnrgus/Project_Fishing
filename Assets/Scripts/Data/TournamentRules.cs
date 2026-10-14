using System;
using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    /// <summary>대회 참가자 한 명의 성적 (미션 세 가지의 재료).</summary>
    public sealed class TournamentEntry
    {
        public string Name;
        public bool IsPlayer;
        public int Count;          // 미션 1: 가장 많이
        public float Longest;      // 미션 2: 가장 길게
        public string LongestFish; // 가장 긴 고기 이름
        public float TotalLength;  // 미션 3: 길이 합계
        public int Points;         // 미션 순위 점수 합
        public int Rank;           // 1부터

        public void Add(string fishName, float length)
        {
            Count++;
            TotalLength += length;
            if (length > Longest) { Longest = length; LongestFish = fishName; }
        }
    }

    /// <summary>AI 낚시꾼이 대회 중에 낚은 고기 한 마리 (게임 시각 순서).</summary>
    public struct AiCatch
    {
        public int Angler;
        public float Minute;   // 대회 시작 후 경과한 게임 분
        public string SpeciesId;
        public float Length;
    }

    /// <summary>
    /// 낚시 대회 규칙 (순수 C#, 시뮬레이션·테스트 가능).
    /// 원작(게임메카 2012-08 「대회 시스템」): NPC를 통해 참가, 하루~이틀마다 정해진 시간,
    /// 미션(가장 많이 · 가장 길게 · 길이 합계) 포인트 합산으로 순위, 참가만 해도 포인트, 누적 포인트로 계급 엠블럼,
    /// 1~3위 보상 「거거 낚시대」·「나팔고둥」.
    /// 솔로판에서는 AI 낚시꾼 7명과 겨룬다. 날마다 한강 → 서천 앞바다 → 홍현 앞바다 순서로 열린다.
    /// </summary>
    public static class TournamentRules
    {
        public const float RegisterFrom = 8 * 60f;   // 참가 신청 시작 08:00
        public const float StartMinute = 9 * 60f;    // 대회 09:00 ~ 15:00
        public const float EndMinute = 15 * 60f;
        public const float Duration = EndMinute - StartMinute;
        /// <summary>대회에 참가하면 시간이 천천히 흐른다 (게임 6시간 = 현실 6분).</summary>
        public const float ClockSpeed = 0.5f;
        public const int ParticipationPoints = 3;
        public const int AiCount = 7;

        public const string RewardRodId = "rod_gege";
        public const string RewardReelId = "reel_napal";

        /// <summary>미션 순위별 점수 (1~4위, 5위 이하는 한 마리라도 낚았으면 1점).</summary>
        static readonly int[] MissionPoints = { 10, 6, 4, 2 };

        /// <summary>AI 낚시꾼 (이름, 실력). 실력이 높을수록 자주, 크게 낚는다.</summary>
        public static readonly (string name, float skill)[] Anglers =
        {
            ("한강도사", 1.25f), ("감생이사냥꾼", 1.15f), ("새벽조사", 1.0f), ("붕어왕 김씨", 0.95f),
            ("찌맨", 0.85f), ("루어소녀", 0.8f), ("주말낚시꾼", 0.65f),
        };

        /// <summary>계급 엠블럼 (누적 포인트 기준).</summary>
        public static readonly (int points, string name, string color)[] Emblems =
        {
            (0, "브론즈", "#c8895a"), (30, "실버", "#c8d0d8"), (80, "골드", "#ffd060"),
            (160, "플래티넘", "#7fe8d8"), (300, "다이아몬드", "#9fd0ff"),
        };

        public static int EmblemIndex(int points)
        {
            int idx = 0;
            for (int i = 0; i < Emblems.Length; i++) if (points >= Emblems[i].points) idx = i;
            return idx;
        }

        public static string EmblemName(int points) => Emblems[EmblemIndex(points)].name;
        public static string EmblemColored(int points)
        {
            var e = Emblems[EmblemIndex(points)];
            return $"<color={e.color}>◆ {e.name}</color>";
        }

        /// <summary>다음 엠블럼까지 남은 포인트 (최고 단계면 0).</summary>
        public static int ToNextEmblem(int points)
        {
            int i = EmblemIndex(points);
            return i + 1 < Emblems.Length ? Emblems[i + 1].points - points : 0;
        }

        /// <summary>그날 대회가 열리는 맵: 한강 → 서천 앞바다(원작 대회 장소) → 홍현 앞바다 순서로 돈다.</summary>
        public static string MapOf(int day) => (day % 3) switch { 1 => "hangang", 2 => "seocheon", _ => "honghyeon" };

        public static string MapShortName(string mapId) => Fishing.World.Maps.Get(mapId).ShortName;

        public static string Title(int day) => $"제{day}회 {MapShortName(MapOf(day))} 조합 낚시 대회";

        public static string TimeText => "09:00 ~ 15:00 (참가 신청 08:00부터)";

        public enum Phase { Before, Registration, Running, Finished }

        public static Phase PhaseAt(float minutes) =>
            minutes < RegisterFrom ? Phase.Before :
            minutes < StartMinute ? Phase.Registration :
            minutes < EndMinute ? Phase.Running : Phase.Finished;

        /// <summary>
        /// AI 낚시꾼들이 대회 동안 낚을 고기를 미리 정한다 (같은 날이면 늘 같은 결과).
        /// 실력 1.0이 게임 약 45분에 한 마리, 큰 고기일수록 드물다.
        /// </summary>
        public static List<AiCatch> SimulateAi(int day, string mapId)
        {
            var rng = new Random(day * 7349 + (mapId == "honghyeon" ? 17 : mapId == "seocheon" ? 29 : 3));
            var pool = FishDatabase.DexOf(mapId).Where(s => !s.IsJunk && !s.SchoolOnly).ToList();
            var weights = pool.Select(s => 1.0 / Math.Pow(s.Rarity, 1.6)).ToArray();
            double wsum = weights.Sum();
            var list = new List<AiCatch>();
            for (int a = 0; a < Anglers.Length; a++)
            {
                float skill = Anglers[a].skill;
                double rate = skill / 45.0; // 분당 마릿수
                double t = 0;
                while (true)
                {
                    t += -Math.Log(1.0 - rng.NextDouble()) / rate;
                    if (t >= Duration) break;
                    double pick = rng.NextDouble() * wsum;
                    int k = 0;
                    while (k < pool.Count - 1 && (pick -= weights[k]) > 0) k++;
                    var s = pool[k];
                    // 실력이 높을수록 큰 고기 쪽으로 (지수가 작아진다)
                    double u = Math.Pow(rng.NextDouble(), 1.9 / skill);
                    float len = (float)Math.Round(s.MinLen + (s.MaxLen - s.MinLen) * u * 0.92, 1);
                    list.Add(new AiCatch { Angler = a, Minute = (float)t, SpeciesId = s.Id, Length = len });
                }
            }
            list.Sort((x, y) => x.Minute.CompareTo(y.Minute));
            return list;
        }

        /// <summary>세 미션의 순위 점수를 매기고 종합 순위를 정한다 (동점은 같은 미션 순위).</summary>
        public static void Score(List<TournamentEntry> entries)
        {
            foreach (var e in entries) e.Points = 0;
            Mission(entries, e => e.Count);
            Mission(entries, e => e.Longest);
            Mission(entries, e => e.TotalLength);
            var ordered = entries.OrderByDescending(e => e.Points).ThenByDescending(e => e.TotalLength).ThenBy(e => e.IsPlayer ? 0 : 1).ToList();
            for (int i = 0; i < ordered.Count; i++) ordered[i].Rank = i + 1;
            entries.Sort((x, y) => x.Rank.CompareTo(y.Rank));
        }

        static void Mission(List<TournamentEntry> entries, Func<TournamentEntry, float> value)
        {
            foreach (var e in entries)
            {
                float v = value(e);
                if (v <= 0f) continue;
                int rank = entries.Count(o => value(o) > v); // 나보다 잘한 사람 수
                e.Points += rank < MissionPoints.Length ? MissionPoints[rank] : 1;
            }
        }

        /// <summary>미션 하나에서 몇 등인지 (0이면 기록 없음).</summary>
        public static int MissionRank(List<TournamentEntry> entries, TournamentEntry e, int mission)
        {
            Func<TournamentEntry, float> value = mission switch { 0 => x => x.Count, 1 => x => x.Longest, _ => x => x.TotalLength };
            float v = value(e);
            return v <= 0f ? 0 : 1 + entries.Count(o => value(o) > v);
        }

        /// <summary>순위 보상 골드와 소라고동.</summary>
        public static (int gold, int conch) Reward(int rank) => rank switch
        {
            1 => (8000, 5),
            2 => (5000, 3),
            3 => (3000, 2),
            _ => (500, 0),
        };
    }
}
