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

    /// <summary>
    /// 한국 낚시 조합의 이야기 퀘스트를 순서대로 진행한다.
    /// 목표를 채우면 "보고 대기"가 되고, 조합 연락관 박주임(데크 입구)이나 대기실(한국 낚시 조합)에 보고해야 보상을 받는다.
    /// </summary>
    public class QuestSystem
    {
        readonly PlayerProfile _profile;

        public QuestSystem(PlayerProfile profile) { _profile = profile; }

        QuestProgress P => _profile.Data.quest;

        public QuestDef Current => P.index < QuestDatabase.Story.Count ? QuestDatabase.Story[P.index] : null;
        public int CompletedCount => P.index;
        /// <summary>목표를 채워 보고만 남은 상태.</summary>
        public bool Ready => Current != null && P.ready;

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
            if (q == null || c.Species.IsJunk || P.ready) return;

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

            if (Progress >= q.Count) MarkReady(q);
            else if (counts) Game.Toast($"[{q.Title}] {Progress}/{q.Count}");
            _profile.NotifyChanged();
        }

        void MarkReady(QuestDef q)
        {
            P.ready = true;
            Game.Toast($"퀘스트 목표 달성: {q.Title}! 데크 입구의 조합 연락관 박주임이나 한국 낚시 조합에 보고하세요.", ToastKind.Great);
            if (Game.Sound != null) Game.Sound.Coin();
        }

        /// <summary>보고하고 보상을 받는다 (박주임과 대화하거나 대기실 퀘스트 창에서).</summary>
        public void Claim()
        {
            var q = Current;
            if (q == null || !P.ready) return;
            P.ready = false;
            Complete(q);
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
            // 도감 종 수처럼 이미 조건을 채운 퀘스트는 바로 보고 대기
            if (Progress >= next.Count) MarkReady(next);
        }
    }
}
