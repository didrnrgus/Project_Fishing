using Fishing.Data;

namespace Fishing.Core
{
    public struct CatchInfo
    {
        public FishSpecies Species;
        public float Length;
        public Habitat Habitat;
        public TimeBand Band;
    }

    /// <summary>한국 낚시 조합의 이야기 퀘스트를 순서대로 진행한다.</summary>
    public class QuestSystem
    {
        readonly PlayerProfile _profile;

        public QuestSystem(PlayerProfile profile) { _profile = profile; }

        QuestProgress P => _profile.Data.quest;

        public QuestDef Current => P.index < QuestDatabase.Story.Count ? QuestDatabase.Story[P.index] : null;
        public int CompletedCount => P.index;

        public int Progress
        {
            get
            {
                var q = Current;
                if (q == null) return 0;
                // 도감 종 수는 퀘스트를 받기 전에 잡은 것도 인정한다
                return q.Goal == QuestGoal.DistinctSpecies ? _profile.Data.dex.Count : P.progress;
            }
        }

        public void OnCatch(CatchInfo c)
        {
            var q = Current;
            if (q == null || c.Species.IsJunk) return;

            bool counts = q.Goal switch
            {
                QuestGoal.CatchAny => true,
                QuestGoal.CatchSpecies => c.Species.Id == q.SpeciesId,
                QuestGoal.CatchInHabitat => c.Habitat == q.Habitat,
                QuestGoal.CatchSpeciesInBand => c.Species.Id == q.SpeciesId && c.Band == q.Band,
                QuestGoal.CatchMinLength => c.Species.Id == q.SpeciesId && c.Length >= q.MinLen,
                _ => false
            };
            if (counts) P.progress++;

            if (Progress >= q.Count) Complete(q);
            else if (counts) Game.Toast($"[{q.Title}] {Progress}/{q.Count}");
            _profile.NotifyChanged();
        }

        void Complete(QuestDef q)
        {
            Game.Toast($"퀘스트 완료: {q.Title}  ({q.RewardText})", ToastKind.Great);
            if (Game.Sound != null) Game.Sound.LevelUp();
            if (q.RewardGold > 0) _profile.AddGold(q.RewardGold);
            if (!string.IsNullOrEmpty(q.RewardBaitId)) _profile.AddItem(q.RewardBaitId, q.RewardBaitCount);

            P.index++;
            P.progress = 0;
            P.distinct.Clear();
            if (q.RewardExp > 0) _profile.AddExp(q.RewardExp);

            var next = Current;
            if (next == null)
            {
                Game.Toast("조합의 모든 이야기 퀘스트를 마쳤습니다! 진정한 강태공이군요.", ToastKind.Great);
                return;
            }
            Game.Toast($"새 퀘스트: {next.Title} - {next.GoalText}", ToastKind.Info);
            // 도감 종 수처럼 이미 조건을 채운 퀘스트는 바로 완료
            if (Progress >= next.Count) Complete(next);
        }
    }
}
