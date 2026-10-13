using Fishing.Data;

namespace Fishing.Core
{
    public struct CatchInfo
    {
        public FishSpecies Species;
        public float Length;
        public Habitat Habitat;
        public TimeBand Band;
        public string MapId;
        public bool NewSpecies;
        public int Combo;
    }

    /// <summary>
    /// 한국 낚시 조합의 이야기 퀘스트를 순서대로 진행한다.
    /// 목표를 채우면 "보고 대기"가 되고, 낚시터의 조합 연락관(한강 박주임, 홍현 정계장)이나 대기실(한국 낚시 조합)에 보고해야 보상을 받는다.
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
            var d = _profile.Data;
            if (c.Combo > d.bestCombo) d.bestCombo = c.Combo;
            if (c.Species.IsJunk)
            {
                if ((c.Species.Id == "robotfish" || c.Species.Id == "fishcan") && !d.specialFinds.Contains(c.Species.Id)) d.specialFinds.Add(c.Species.Id);
                CheckAchievements();
                return;
            }
            // 원작: 도감을 채우면 소라고동
            if (c.NewSpecies) GiveConch(1, $"도감 등록: {c.Species.Name}");
            OnCatchDaily(c);
            OnCatchTrophy(c);
            OnCatchStory(c);
            CheckAchievements();
        }

        void OnCatchStory(CatchInfo c)
        {
            var q = Current;
            if (q == null || P.ready) return;

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
            Game.Toast($"퀘스트 목표 달성: {q.Title}! {Fishing.World.FieldMap.UnionContact}이나 한국 낚시 조합에 보고하세요.", ToastKind.Great);
            if (Game.Sound != null) Game.Sound.Coin();
        }

        /// <summary>보고하고 보상을 받는다 (조합 연락관과 대화하거나 대기실 퀘스트 창에서).</summary>
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
            if (q.RewardKeepNet > 0) _profile.Data.keepNetBonus += q.RewardKeepNet;
            if (!string.IsNullOrEmpty(q.RewardCostume)) _profile.GiveCostume(q.RewardCostume);

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
    
        // ================= 소라고동 =================

        public void GiveConch(int n, string why)
        {
            _profile.AddItem(AchievementDatabase.ConchId, n);
            Game.Toast($"{why}  소라고동 +{n}", ToastKind.Good);
        }

        // ================= 대물 도전 =================

        /// <summary>어종마다 대물 기준 길이를 처음 넘기면 소라고동 3개.</summary>
        void OnCatchTrophy(CatchInfo c)
        {
            var d = _profile.Data;
            if (!c.Species.IsTrophy(c.Length) || d.trophyDone.Contains(c.Species.Id)) return;
            d.trophyDone.Add(c.Species.Id);
            _profile.AddExp(c.Species.Exp * 2);
            GiveConch(3, $"대물 도전 달성! {c.Species.TrophyName} {c.Species.Name}");
        }

        // ================= 일일 의뢰 =================

        public System.Collections.Generic.List<DailyQuest> Daily
        {
            get
            {
                EnsureDaily();
                return _profile.Data.daily;
            }
        }

        /// <summary>게임 하루가 바뀌면 오늘의 조합 의뢰 3개를 새로 받는다 (가 본 낚시터에서).</summary>
        public void EnsureDaily()
        {
            var d = _profile.Data;
            int day = Game.Clock != null ? Game.Clock.Day : 1;
            if (d.dailyDay == day && d.daily.Count > 0) return;
            d.dailyDay = day;
            d.daily.Clear();
            var maps = new System.Collections.Generic.List<string> { "hangang" };
            foreach (var m in d.visitedMaps) if (!maps.Contains(m)) maps.Add(m);
            var rng = new System.Random(day * 7919 + d.level);
            for (int i = 0; i < 3; i++)
            {
                string map = maps[rng.Next(maps.Count)];
                var pool = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(FishDatabase.DexOf(map), s => s.Rarity <= 3 && !s.SchoolOnly));
                if (pool.Count == 0) continue;
                var sp = pool[rng.Next(pool.Count)];
                var dq = new DailyQuest { mapId = map, kind = i, speciesId = sp.Id };
                if (i == 0) { dq.count = sp.Rarity <= 1 ? 3 : 2; }
                else if (i == 1) { dq.habitat = (int)sp.Habitats[rng.Next(sp.Habitats.Length)]; dq.count = 3; dq.speciesId = null; }
                else { dq.count = 1; dq.minLen = UnityEngine.Mathf.Round(UnityEngine.Mathf.Lerp(sp.MinLen, sp.MaxLen, 0.45f)); }
                dq.gold = 1200 + sp.Rarity * 600 + i * 400;
                dq.exp = 80 + sp.Rarity * 40 + i * 30;
                d.daily.Add(dq);
            }
        }

        public static string DailyText(DailyQuest q)
        {
            var map = Fishing.World.Maps.Get(q.mapId);
            var sp = FishDatabase.Get(q.speciesId);
            return q.kind switch
            {
                0 => $"[{map.ShortName}] {sp?.Name} {q.count}마리",
                1 => $"[{map.ShortName}] {map.HabitatName((Habitat)q.habitat)}에서 {q.count}마리",
                _ => $"[{map.ShortName}] {q.minLen:0}cm 이상 {sp?.Name}",
            };
        }

        void OnCatchDaily(CatchInfo c)
        {
            foreach (var q in Daily)
            {
                if (q.done || q.mapId != c.MapId) continue;
                bool counts = q.kind switch
                {
                    0 => c.Species.Id == q.speciesId,
                    1 => (int)c.Habitat == q.habitat,
                    _ => c.Species.Id == q.speciesId && c.Length >= q.minLen,
                };
                if (!counts) continue;
                q.progress++;
                if (q.progress < q.count) { Game.Toast($"[일일 의뢰] {DailyText(q)} {q.progress}/{q.count}"); continue; }
                q.done = true;
                _profile.Data.dailyCompleted++;
                _profile.AddGold(q.gold);
                _profile.AddExp(q.exp);
                GiveConch(1, $"일일 의뢰 완료! {DailyText(q)} (+{q.gold:N0}골드, 경험치 {q.exp})");
            }
        }

        // ================= 업적 =================

        public void CheckAchievements()
        {
            var d = _profile.Data;
            foreach (var a in AchievementDatabase.All)
            {
                if (d.achievements.Contains(a.Id) || !a.Done(_profile)) continue;
                d.achievements.Add(a.Id);
                if (Game.Sound != null) Game.Sound.LevelUp();
                _profile.AddItem(AchievementDatabase.ConchId, a.Conch);
                Game.Toast($"업적 달성: {a.Name}!  소라고동 +{a.Conch}" + (a.Title != null ? $"  · 칭호 「{a.Title}」" : ""), ToastKind.Great);
            }
        }
}
}
