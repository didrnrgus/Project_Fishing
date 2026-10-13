using System;
using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    /// <summary>업적 하나. 이루면 소라고동과 (있으면) 칭호를 준다.</summary>
    public sealed class AchievementDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public string Title;   // 얻는 칭호 (없으면 null)
        public int Conch;      // 소라고동 보상
        public Func<Core.PlayerProfile, bool> Done;
        /// <summary>진행도 표시 (현재, 목표). 없으면 null.</summary>
        public Func<Core.PlayerProfile, (int, int)> Progress;
    }

    /// <summary>
    /// 업적과 칭호. 원작에는 업적·도전과제가 있었고, 도감을 채우면 소라고동을 받았다.
    /// 칭호는 이름표에 붙는다 (원작 이름표: "칭호 + 이름").
    /// </summary>
    public static class AchievementDatabase
    {
        public const string ConchId = "mat_conch";

        /// <summary>레벨로 얻는 칭호.</summary>
        public static readonly (int level, string title)[] LevelTitles =
        {
            (1, "초보 조사"), (5, "견습 조사"), (10, "중급 조사"), (15, "고수 조사"), (20, "명인"), (25, "강태공"),
        };

        static bool Dex(Core.PlayerProfile p, string id, float minLen) => p.Dex(id) is { } d && d.maxLength >= minLen;
        static int DexCount(Core.PlayerProfile p, string map) => FishDatabase.DexOf(map).Count(s => p.Dex(s.Id) != null);
        static int DexTotal(string map) => FishDatabase.DexOf(map).Count();

        public static readonly List<AchievementDef> All = new List<AchievementDef>
        {
            new AchievementDef { Id = "first", Name = "첫 손맛", Desc = "물고기를 처음 낚았다", Conch = 1,
                Done = p => p.Data.totalCatches >= 1 },
            new AchievementDef { Id = "catch50", Name = "꾼의 길", Desc = "물고기 50마리 낚기", Title = "꾼", Conch = 2,
                Done = p => p.Data.totalCatches >= 50, Progress = p => (p.Data.totalCatches, 50) },
            new AchievementDef { Id = "catch200", Name = "베테랑", Desc = "물고기 200마리 낚기", Title = "베테랑 조사", Conch = 5,
                Done = p => p.Data.totalCatches >= 200, Progress = p => (p.Data.totalCatches, 200) },
            new AchievementDef { Id = "wolcheok", Name = "월척", Desc = "30.3cm가 넘는 붕어 낚기", Title = "월척꾼", Conch = 3,
                Done = p => Dex(p, "crucian", 30.3f) },
            new AchievementDef { Id = "carp80", Name = "한강의 대물 잉어", Desc = "80cm가 넘는 잉어 낚기", Conch = 3,
                Done = p => Dex(p, "carp", 80f) },
            new AchievementDef { Id = "goldkoi", Name = "전설", Desc = "황금 비단잉어 낚기", Title = "전설을 낚은 자", Conch = 5,
                Done = p => p.Dex("goldkoi") != null },
            new AchievementDef { Id = "bream50", Name = "5짜 감성돔", Desc = "50cm가 넘는 감성돔 낚기", Title = "감생이 사냥꾼", Conch = 5,
                Done = p => Dex(p, "blackseabream", 50f) },
            new AchievementDef { Id = "grouper", Name = "바다의 주인", Desc = "자이언트 그루퍼 낚기", Title = "바다의 주인", Conch = 10,
                Done = p => p.Dex("giantgrouper") != null },
            new AchievementDef { Id = "dexhan", Name = "한강 도감 완성", Desc = "한강의 모든 어종을 도감에 기록", Title = "한강 박사", Conch = 10,
                Done = p => DexCount(p, "hangang") >= DexTotal("hangang"), Progress = p => (DexCount(p, "hangang"), DexTotal("hangang")) },
            new AchievementDef { Id = "dexsea", Name = "홍현 도감 완성", Desc = "홍현 앞바다의 모든 어종을 도감에 기록", Title = "남해 박사", Conch = 10,
                Done = p => DexCount(p, "honghyeon") >= DexTotal("honghyeon"), Progress = p => (DexCount(p, "honghyeon"), DexTotal("honghyeon")) },
            new AchievementDef { Id = "dexwest", Name = "서천 도감 완성", Desc = "서천 앞바다의 모든 어종을 도감에 기록", Title = "서해 박사", Conch = 10,
                Done = p => DexCount(p, "seocheon") >= DexTotal("seocheon"), Progress = p => (DexCount(p, "seocheon"), DexTotal("seocheon")) },
            new AchievementDef { Id = "croaker", Name = "서해의 왕", Desc = "민어 낚기", Title = "서해의 왕", Conch = 10,
                Done = p => p.Dex("croaker") != null },
            new AchievementDef { Id = "octopus10", Name = "주꾸미 박사", Desc = "주꾸미 10마리 낚기", Conch = 3,
                Done = p => (p.Dex("octopus")?.count ?? 0) >= 10, Progress = p => (p.Dex("octopus")?.count ?? 0, 10) },
            new AchievementDef { Id = "smelt", Name = "얼음 위의 겨울", Desc = "겨울 한강 얼음 구멍에서 빙어 낚기", Conch = 2,
                Done = p => p.Dex("smelt") != null },
            new AchievementDef { Id = "deeplegend", Name = "전설의 심해어", Desc = "홍현 먼바다에서 다금바리나 돗돔 낚기", Title = "심해 사냥꾼", Conch = 5,
                Done = p => p.Dex("dageumbari") != null || p.Dex("dotdom") != null },
            new AchievementDef { Id = "combo5", Name = "카운터 장인", Desc = "한 번의 파이팅에서 카운터 콤보 5", Title = "카운터 장인", Conch = 3,
                Done = p => p.Data.bestCombo >= 5, Progress = p => (p.Data.bestCombo, 5) },
            new AchievementDef { Id = "trophy10", Name = "대물 사냥꾼", Desc = "대물 도전 10종 달성", Title = "대물 사냥꾼", Conch = 5,
                Done = p => p.Data.trophyDone.Count >= 10, Progress = p => (p.Data.trophyDone.Count, 10) },
            new AchievementDef { Id = "upgrade10", Name = "대장장이", Desc = "장비를 +10까지 강화", Title = "대장장이", Conch = 5,
                Done = p => p.Data.rodLevels.Any(r => r.count >= 10) },
            new AchievementDef { Id = "robot", Name = "한강의 비밀", Desc = "한강에서 로봇 물고기 낚기", Title = "한강 탐정", Conch = 3,
                Done = p => p.Data.specialFinds.Contains("robotfish") },
            new AchievementDef { Id = "fishcan", Name = "생선 캔", Desc = "전설의 CBT 이벤트 아이템, 생선 캔 낚기", Conch = 2,
                Done = p => p.Data.specialFinds.Contains("fishcan") },
            new AchievementDef { Id = "daily10", Name = "조합의 일꾼", Desc = "일일 의뢰 10개 완료", Title = "조합의 일꾼", Conch = 3,
                Done = p => p.Data.dailyCompleted >= 10, Progress = p => (p.Data.dailyCompleted, 10) },
            new AchievementDef { Id = "tour1", Name = "첫 대회", Desc = "조합 낚시 대회에 참가해 끝까지 치르기", Conch = 1,
                Done = p => p.Data.tournament.played >= 1 },
            new AchievementDef { Id = "tourpodium", Name = "시상대", Desc = "조합 낚시 대회 3위 안에 들기", Title = "입상 조사", Conch = 3,
                Done = p => p.Data.tournament.podiums >= 1 },
            new AchievementDef { Id = "tourwin", Name = "대회 챔피언", Desc = "조합 낚시 대회 우승", Title = "대회 챔피언", Conch = 5,
                Done = p => p.Data.tournament.wins >= 1 },
            new AchievementDef { Id = "tourgold", Name = "골드 엠블럼", Desc = "대회 누적 포인트 80 (골드 엠블럼)", Conch = 5,
                Done = p => p.Data.tournament.points >= 80, Progress = p => (p.Data.tournament.points, 80) },
            new AchievementDef { Id = "level25", Name = "만렙", Desc = "레벨 25 달성", Conch = 10,
                Done = p => p.Data.level >= 25, Progress = p => (p.Data.level, 25) },
        };

        public static AchievementDef Get(string id) => All.FirstOrDefault(a => a.Id == id);
    }
}
