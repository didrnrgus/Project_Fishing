using System.Collections.Generic;

namespace Fishing.Data
{
    public enum QuestGoal { CatchAny, CatchSpecies, DistinctSpecies, CatchInHabitat, CatchSpeciesInBand, CatchMinLength }

    public sealed class QuestDef
    {
        public string Title;
        public string Desc;
        public QuestGoal Goal;
        public string SpeciesId;
        public Habitat Habitat;
        public TimeBand Band;
        public float MinLen;
        public int Count = 1;
        public int RewardGold;
        public int RewardExp;
        public string RewardBaitId;
        public int RewardBaitCount;

        public string GoalText
        {
            get
            {
                var fish = FishDatabase.Get(SpeciesId)?.Name ?? "";
                return Goal switch
                {
                    QuestGoal.CatchAny => $"아무 물고기 {Count}마리 낚기",
                    QuestGoal.CatchSpecies => $"{fish} {Count}마리 낚기",
                    QuestGoal.DistinctSpecies => $"서로 다른 어종 {Count}종 낚기",
                    QuestGoal.CatchInHabitat => $"{Names.Of(Habitat)} 포인트에서 {Count}마리 낚기",
                    QuestGoal.CatchSpeciesInBand => $"{Names.Of(Band)}에 {fish} {Count}마리 낚기",
                    QuestGoal.CatchMinLength => $"{MinLen:0.#}cm 이상 {fish} 낚기",
                    _ => ""
                };
            }
        }

        public string RewardText
        {
            get
            {
                var parts = new List<string>();
                if (RewardGold > 0) parts.Add($"{RewardGold:N0}골드");
                if (RewardExp > 0) parts.Add($"경험치 {RewardExp}");
                var bait = GearDatabase.GetBait(RewardBaitId);
                if (bait != null) parts.Add($"{bait.Name} {RewardBaitCount}개");
                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>한국 낚시 조합의 이야기 퀘스트. 순서대로 진행된다.</summary>
    public static class QuestDatabase
    {
        public const string Giver = "한국 낚시 조합";

        public static readonly List<QuestDef> Story = new List<QuestDef>
        {
            new QuestDef { Title = "첫 손맛", Desc = "조합에 온 걸 환영하네! 우선 한 마리 낚아서 손맛부터 보게.",
                Goal = QuestGoal.CatchAny, Count = 1, RewardGold = 1000, RewardExp = 50 },
            new QuestDef { Title = "붕어를 찾아서", Desc = "민물낚시의 기본은 붕어지. 얕은 여울이나 수초 근처를 노려보게.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "crucian", Count = 3, RewardGold = 1500, RewardExp = 80,
                RewardBaitId = "bait_paste", RewardBaitCount = 20 },
            new QuestDef { Title = "도감 채우기", Desc = "한강엔 생각보다 많은 물고기가 산다네. 도감(B)을 채워보게.",
                Goal = QuestGoal.DistinctSpecies, Count = 4, RewardGold = 2000, RewardExp = 120 },
            new QuestDef { Title = "수초 포인트", Desc = "동쪽 만곡부 수초 지대는 고기들의 은신처지.",
                Goal = QuestGoal.CatchInHabitat, Habitat = Habitat.Weed, Count = 3, RewardGold = 1500, RewardExp = 120,
                RewardBaitId = "bait_corn", RewardBaitCount = 30 },
            new QuestDef { Title = "밤낚시", Desc = "해가 지면 메기가 깨어난다네. 깊은 물이나 수초 근처를 노려보게.",
                Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "catfish", Band = TimeBand.Night, Count = 1,
                RewardGold = 3000, RewardExp = 200 },
            new QuestDef { Title = "교각 아래의 사냥꾼", Desc = "다리 교각 석축에 쏘가리가 숨어 있다지. 생미끼나 루어가 좋을 걸세.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "mandarin", Count = 1, RewardGold = 5000, RewardExp = 300,
                RewardBaitId = "bait_minnow", RewardBaitCount = 10 },
            new QuestDef { Title = "월척의 꿈", Desc = "한 자(30.3cm)가 넘는 붕어, 낚시꾼이라면 누구나 꿈꾸지.",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "crucian", MinLen = 30.3f, Count = 1,
                RewardGold = 8000, RewardExp = 400 },
            new QuestDef { Title = "한강의 주인", Desc = "80cm가 넘는 잉어를 낚아 온다면 자네를 진정한 강태공으로 인정하지.",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "carp", MinLen = 80f, Count = 1,
                RewardGold = 20000, RewardExp = 1000 },
        };
    }
}
