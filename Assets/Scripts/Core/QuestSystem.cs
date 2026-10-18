using System;
using System.Collections.Generic;
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
    /// 원작 퀘스트 흐름 (13 문서): 이름 붙은 NPC에게 말을 걸어 [퀘스트 목록보기] → "수락가능한 퀘스트"에서 골라 [수락] →
    /// 목표를 채우면 [보고자] NPC에게 가서 [완료] → 가운데 완료 편지 카드 + 시스템 줄 "N EXP 를 획득했습니다.".
    /// 이야기 · 일일반복 · 가이드 · 일반 퀘스트를 동시에 여러 개 진행한다.
    /// 이야기 퀘스트는 순서대로 하나씩 열리고(P.index), 나머지는 조건이 되면 그 NPC에게서 받을 수 있다.
    /// 납품·통발·방생 같은 다른 시스템은 OnDelivered / OnTrapInstalled / OnReleased로 알려 준다.
    /// </summary>
    public class QuestSystem
    {
        /// <summary>NPC 머리 위 표시: 없음 / 진행 중(회색 ?) / 받을 수 있음(하늘색 !) / 이야기 받을 수 있음(노란 !) / 완료 가능(노란 ?).</summary>
        public enum Mark { None, Working, Available, AvailableStory, Ready }

        /// <summary>퀘스트 완료 (완료 편지 카드: 제목, GOLD, EXP).</summary>
        public static event Action<QuestDef, int, int> QuestCompleted;

        // 시스템 줄 색 (원작: 수락 줄은 이야기 보라, EXP 초록, GOLD 노랑)
        public const string ExpColor = "#8fe070", GoldColor = "#f0c040", ItemColor = "#e8e8e8";

        readonly PlayerProfile _profile;

        public QuestSystem(PlayerProfile profile)
        {
            _profile = profile;
            Migrate();
        }

        QuestProgress P => _profile.Data.quest;

        /// <summary>
        /// 예전 저장(ver 0): index는 예전 45개 이야기 목록 기준이고 지금 퀘스트를 자동으로 받은 상태였다.
        /// 같은 퀘스트(st번호)의 새 위치로 옮기고, 하던 이야기 퀘스트는 받은 채로(진행·보고 대기 그대로) 이어 간다.
        /// 받아 둔 일일 의뢰 중 진행이 있던 것은 수락한 것으로 친다.
        /// </summary>
        void Migrate()
        {
            var d = _profile.Data;
            if (d.quest == null) d.quest = new QuestProgress();
            var p = d.quest;
            if (p.active == null) p.active = new List<ActiveQuest>();
            if (p.done == null) p.done = new List<string>();
            if (p.distinct == null) p.distinct = new List<string>();
            if (d.daily == null) d.daily = new List<DailyQuest>();
            if (p.ver >= 1) return;

            int old = p.index;
            p.index = QuestDatabase.MigrateIndex(old);
            p.active.Clear();
            p.completedTotal = old + d.dailyCompleted;
            var cur = Current;
            // 새로 시작한 저장은 대기실 안내원에게서 첫 퀘스트를 받는다. 하던 저장은 이어서
            bool started = old > 0 || p.progress > 0 || p.ready || d.totalCatches > 0;
            if (cur != null && started)
                p.active.Add(new ActiveQuest { id = cur.Id, progress = Math.Min(p.progress, cur.Count), ready = p.ready });
            p.progress = 0;
            p.ready = false;
            foreach (var q in d.daily)
                if (q.progress > 0 || q.done) q.accepted = true;
            p.ver = 1;
        }

        // ================= 조회 =================

        /// <summary>다음에 할 이야기 퀘스트 (받았든 안 받았든). 다 끝냈으면 null.</summary>
        public QuestDef Current => P.index < QuestDatabase.Story.Count ? QuestDatabase.Story[P.index] : null;
        public int CompletedCount => P.index;
        /// <summary>이야기 퀘스트 목표를 채워 보고만 남은 상태.</summary>
        public bool Ready => Current != null && IsReady(Current);
        public int Progress => Current == null ? 0 : ProgressOf(Current);

        ActiveQuest Entry(string id) => P.active.Find(a => a.id == id);

        DailyQuest DailyOf(QuestDef q)
        {
            var list = _profile.Data.daily;
            return q.Type == QuestType.Repeat && q.DailyIndex >= 0 && q.DailyIndex < list.Count ? list[q.DailyIndex] : null;
        }

        public bool IsActive(QuestDef q)
        {
            if (q == null) return false;
            if (q.Type == QuestType.Repeat) { var d = DailyOf(q); return d != null && d.accepted && !d.done; }
            return Entry(q.Id) != null;
        }

        public bool IsReady(QuestDef q)
        {
            if (q == null) return false;
            if (q.Type == QuestType.Repeat) { var d = DailyOf(q); return d != null && d.accepted && d.ready && !d.done; }
            return Entry(q.Id)?.ready == true;
        }

        public bool IsDone(QuestDef q)
        {
            if (q == null) return false;
            return q.Type switch
            {
                QuestType.Story => QuestDatabase.StoryIndex(q.Id) < P.index,
                QuestType.Repeat => DailyOf(q)?.done == true,
                _ => P.done.Contains(q.Id),
            };
        }

        public int ProgressOf(QuestDef q)
        {
            if (q == null) return 0;
            int p;
            // 도감 종 수는 퀘스트를 받기 전에 잡은 것도 인정한다
            if (q.Goal == QuestGoal.DistinctSpecies) p = _profile.Data.dex.Count;
            else if (q.Type == QuestType.Repeat) p = DailyOf(q)?.progress ?? 0;
            else p = Entry(q.Id)?.progress ?? 0;
            return Math.Min(p, q.Count);
        }

        /// <summary>원작 목표 줄: "참돔 잡아서 가져오기 2 / 5".</summary>
        public string ObjectiveLine(QuestDef q) => $"{q.Objective} {ProgressOf(q)} / {q.Count}";

        /// <summary>진행 중인 퀘스트. 이야기가 먼저, 나머지는 Lv 높은 순 (원작 QUEST LIST·퀘스트 책 순서).</summary>
        public List<QuestDef> Active()
        {
            var list = new List<QuestDef>();
            var story = Current;
            if (story != null && Entry(story.Id) != null) list.Add(story);
            var rest = new List<QuestDef>();
            foreach (var a in P.active)
            {
                var q = QuestDatabase.Get(a.id);
                if (q != null && q.Type != QuestType.Story) rest.Add(q);
            }
            foreach (var q in DailyDefs) if (IsActive(q)) rest.Add(q);
            rest.Sort((a, b) => b.Lv.CompareTo(a.Lv));
            list.AddRange(rest);
            return list;
        }

        bool SideOpen(QuestDef q)
        {
            if (P.done.Contains(q.Id) || Entry(q.Id) != null) return false;
            if (_profile.Data.level + 2 < q.Lv) return false;
            if (q.AfterStory != null)
            {
                int i = QuestDatabase.StoryIndex(q.AfterStory);
                if (i < 0 || i >= P.index) return false;
            }
            return true;
        }

        /// <summary>그 NPC에게서 지금 받을 수 있는 퀘스트 (원작 "수락가능한 퀘스트").</summary>
        public List<QuestDef> AvailableAt(string npcKey)
        {
            var list = new List<QuestDef>();
            if (npcKey == null) return list;
            var s = Current;
            if (s != null && s.Giver == npcKey && Entry(s.Id) == null) list.Add(s);
            foreach (var q in QuestDatabase.Side)
                if (q.Giver == npcKey && SideOpen(q)) list.Add(q);
            foreach (var q in DailyDefs)
            {
                var d = DailyOf(q);
                if (q.Giver == npcKey && d != null && !d.accepted && !d.done) list.Add(q);
            }
            return list;
        }

        /// <summary>그 NPC가 주었거나 그 NPC에게 보고하는 진행 중 퀘스트 (원작 "진행중인 퀘스트").</summary>
        public List<QuestDef> InProgressAt(string npcKey)
        {
            var list = new List<QuestDef>();
            if (npcKey == null) return list;
            foreach (var q in Active())
                if (q.Giver == npcKey || q.Reporter == npcKey) list.Add(q);
            return list;
        }

        /// <summary>진행 중인 퀘스트가 아직 이 어종을 요구하는지 (납품 "퀘스트어종 비활성화", 통발 녹색 테두리).</summary>
        public bool IsQuestSpecies(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return false;
            foreach (var q in Active())
                if (q.SpeciesId == speciesId && q.Goal != QuestGoal.DeliverFish && !IsReady(q)) return true;
            return false;
        }

        public bool HasQuestsAt(string npcKey) => npcKey != null && (AvailableAt(npcKey).Count > 0 || InProgressAt(npcKey).Count > 0);

        public Mark MarkFor(string npcKey)
        {
            if (npcKey == null) return Mark.None;
            bool working = false;
            foreach (var q in Active())
            {
                if (q.Reporter == npcKey && (IsReady(q) || q.Goal == QuestGoal.Meet)) return Mark.Ready;
                if (q.Giver == npcKey || q.Reporter == npcKey) working = true;
            }
            var avail = AvailableAt(npcKey);
            if (avail.Exists(q => q.Type == QuestType.Story)) return Mark.AvailableStory;
            if (avail.Count > 0) return Mark.Available;
            return working ? Mark.Working : Mark.None;
        }

        /// <summary>목표를 채웠을 때 찾아갈 곳 ("[홍현 앞바다]의 어촌계 정계장에게 보고하기").</summary>
        public static string ReportLine(QuestDef q)
        {
            var n = QuestNpcs.Get(q.Reporter);
            return n == null ? "보고하기" : $"[{n.Place}]의 {n.Name}에게 보고하기";
        }

        /// <summary>그 낚시터에서 할 수 있는 퀘스트인지 (HUD "[현재 낚시터]에서 가능한 퀘스트만 보기").</summary>
        public bool DoableOn(QuestDef q, string mapId)
        {
            bool goToReporter = IsReady(q) || q.Goal == QuestGoal.Meet || q.Goal == QuestGoal.Deliver;
            if (goToReporter) return QuestNpcs.Get(q.Reporter)?.MapId == mapId;
            return q.MapId == null || q.MapId == mapId;
        }

        // ================= 수락 · 완료 · 포기 =================

        static string TypeColor(QuestDef q) => q.Type == QuestType.Normal ? "#f0f0f0" : q.TypeColor;

        /// <summary>원작 [수락]. 성공하면 true.</summary>
        public bool Accept(QuestDef q)
        {
            if (q == null || IsActive(q) || IsDone(q)) return false;
            switch (q.Type)
            {
                case QuestType.Story:
                    if (q != Current) return false;
                    P.active.Add(new ActiveQuest { id = q.Id });
                    break;
                case QuestType.Repeat:
                    var d = DailyOf(q);
                    if (d == null) return false;
                    d.accepted = true;
                    d.progress = 0;
                    d.ready = false;
                    break;
                default:
                    if (!SideOpen(q)) return false;
                    P.active.Add(new ActiveQuest { id = q.Id });
                    break;
            }
            Game.SysLog($"[{q.Title}] 퀘스트를 수락하셨습니다.", TypeColor(q));
            if (Game.Sound != null) Game.Sound.UiClick();
            // 전달 퀘스트: 물건을 받아 들고 간다 (원작 "거대한 등지느러미 가시 1개를 획득했습니다.")
            if (q.Goal == QuestGoal.Deliver)
            {
                var e = Entry(q.Id);
                e.progress = 1;
                e.ready = true;
                Game.SysLog($"{q.ItemName} 1개를 획득했습니다.", ItemColor);
                Game.Notice($"[퀘스트] {q.Objective} 1 / 1");
            }
            else if (q.Goal == QuestGoal.DistinctSpecies && ProgressOf(q) >= q.Count) MarkReady(q);
            _profile.NotifyChanged();
            return true;
        }

        /// <summary>원작 [포기]. 이야기 퀘스트는 다시 그 NPC에게서 받을 수 있다.</summary>
        public void Abandon(QuestDef q)
        {
            if (!IsActive(q)) return;
            if (q.Type == QuestType.Repeat)
            {
                var d = DailyOf(q);
                d.accepted = false;
                d.progress = 0;
                d.ready = false;
            }
            else P.active.RemoveAll(a => a.id == q.Id);
            Game.SysLog($"[{q.Title}] 퀘스트를 포기했습니다.", "#a0a8b0");
            _profile.NotifyChanged();
        }

        /// <summary>
        /// [완료]: 목표를 채운 퀘스트를 [보고자]에게 내고 보상을 받는다. atNpc가 null이면 어디서든 (개발용·예전 호출).
        /// </summary>
        public bool Finish(QuestDef q, string atNpc)
        {
            if (q == null || !IsActive(q) || !IsReady(q)) return false;
            if (atNpc != null && q.Reporter != atNpc) return false;

            switch (q.Type)
            {
                case QuestType.Story:
                    P.active.RemoveAll(a => a.id == q.Id);
                    if (q == Current) P.index++;
                    break;
                case QuestType.Repeat:
                    var d = DailyOf(q);
                    d.done = true;
                    d.ready = false;
                    _profile.Data.dailyCompleted++;
                    break;
                default:
                    P.active.RemoveAll(a => a.id == q.Id);
                    if (!P.done.Contains(q.Id)) P.done.Add(q.Id);
                    break;
            }
            P.completedTotal++;
            _profile.Log($"{(q.Type == QuestType.Story ? "이야기" : q.TypeTag.Length > 0 ? q.TypeTag : "일반")} 퀘스트 완료 — {q.Title}");
            if (Game.Sound != null) Game.Sound.LevelUp();

            // 보상: 원작 시스템 줄 "6000 EXP 를 획득했습니다." → "2880 GOLD 를 획득했습니다."
            if (q.RewardExp > 0) Game.SysLog($"{q.RewardExp} EXP 를 획득했습니다.", ExpColor);
            if (q.RewardGold > 0) Game.SysLog($"{q.RewardGold} GOLD 를 획득했습니다.", GoldColor);
            if (q.RewardGold > 0) _profile.AddGold(q.RewardGold);
            var bait = GearDatabase.GetBait(q.RewardBaitId);
            if (bait != null)
            {
                _profile.AddItem(q.RewardBaitId, q.RewardBaitCount);
                Game.SysLog($"{bait.Name} {q.RewardBaitCount}개를 획득했습니다.", ItemColor);
            }
            if (q.RewardKeepNet > 0) _profile.Data.keepNetBonus += q.RewardKeepNet;
            if (!string.IsNullOrEmpty(q.RewardCostume)) _profile.GiveCostume(q.RewardCostume);
            if (q.RewardConch > 0) _profile.AddItem(AchievementDatabase.ConchId, q.RewardConch);
            QuestCompleted?.Invoke(q, q.RewardGold, q.RewardExp);
            if (q.RewardExp > 0) _profile.AddExp(q.RewardExp);

            if (q.Type == QuestType.Story)
            {
                var next = Current;
                if (next == null) Game.Toast("조합의 모든 이야기 퀘스트를 마쳤습니다! 진정한 강태공이군요.", ToastKind.Great);
                else
                {
                    var giver = QuestNpcs.Get(next.Giver);
                    Game.Toast($"새 이야기 퀘스트 [{next.Title}] — {giver?.Where ?? "조합"}에게서 받을 수 있습니다." +
                               (giver != null && giver.InLobby ? " (대기실 [퀘스트])" : ""), ToastKind.Info);
                }
            }
            CheckAchievements();
            _profile.NotifyChanged();
            return true;
        }

        /// <summary>예전 호출: 지금 이야기 퀘스트를 어디서든 완료한다.</summary>
        public void Claim() => Finish(Current, null);

        /// <summary>NPC에게 말을 걸었다: 그 사람을 만나는 퀘스트는 바로 완료 (원작 "[퀘스트] 최아린을 만나기 1/1 [완 료]").</summary>
        public void Talked(string npcKey)
        {
            if (npcKey == null) return;
            foreach (var q in Active())
            {
                if (q.Goal != QuestGoal.Meet || q.Reporter != npcKey) continue;
                var e = Entry(q.Id);
                if (e == null) continue;
                e.progress = q.Count;
                e.ready = true;
                Game.Notice($"[퀘스트] {q.Objective} {q.Count} / {q.Count} [완 료]");
                Finish(q, npcKey);
            }
        }

        void MarkReady(QuestDef q)
        {
            if (q.Type == QuestType.Repeat) { var d = DailyOf(q); if (d != null) d.ready = true; }
            else { var e = Entry(q.Id); if (e == null) return; e.ready = true; }
            Game.Notice($"[퀘스트] {q.Objective} {q.Count} / {q.Count} [완 료]");
            var rep = QuestNpcs.Get(q.Reporter);
            Game.Toast($"퀘스트 목표 달성: {q.Title}! {rep?.Where ?? "조합"}에게 가면 완료할 수 있습니다.", ToastKind.Great);
            if (Game.Sound != null) Game.Sound.Coin();
        }

        void Advance(QuestDef q, int by = 1)
        {
            if (!IsActive(q) || IsReady(q)) return;
            int p;
            if (q.Type == QuestType.Repeat)
            {
                var d = DailyOf(q);
                d.progress = Math.Min(q.Count, d.progress + by);
                p = d.progress;
            }
            else
            {
                var e = Entry(q.Id);
                e.progress = Math.Min(q.Count, e.progress + by);
                p = e.progress;
            }
            if (p >= q.Count) MarkReady(q);
            else Game.Notice($"[퀘스트] {q.Objective} {p} / {q.Count}");
        }

        // ================= 훅 (다른 시스템이 부른다) =================

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
            if (c.Species.Boss)
            {
                _profile.AddItem(ItemDatabase.BigCoinId, c.Species.BossCoins);
                Game.Toast($"괴물 어종 {c.Species.Name}! 대물 코인 +{c.Species.BossCoins} (조합 교환소에서 풍림화산 채비로)", ToastKind.Great);
            }
            // 일지: 처음 낚은 어종 · 대물 · 괴물
            if (c.NewSpecies || c.Species.IsTrophy(c.Length) || c.Species.Boss || c.Species.Rarity >= 4)
                _profile.Log($"{Array.Find(Fishing.World.Maps.All, m => m.Id == c.MapId)?.ShortName ?? ""}에서 {c.Species.Name} {c.Length:0.0}cm" +
                             (c.NewSpecies ? " (첫 만남)" : "") + (c.Species.IsTrophy(c.Length) ? $" — {c.Species.TrophyName}!" : ""));
            Bingo.OnCatch(c);
            OnCatchTrophy(c);
            foreach (var q in Active())
            {
                if (IsReady(q)) continue;
                if (q.Goal == QuestGoal.DistinctSpecies)
                {
                    if (!c.NewSpecies) continue;
                    if (ProgressOf(q) >= q.Count) MarkReady(q);
                    else Game.Notice($"[퀘스트] {ObjectiveLine(q)}");
                    continue;
                }
                if (Counts(q, c)) Advance(q);
            }
            CheckAchievements();
            _profile.NotifyChanged();
        }

        static bool Counts(QuestDef q, CatchInfo c)
        {
            if (q.RequireMap && q.MapId != null && q.MapId != c.MapId) return false;
            return q.Goal switch
            {
                QuestGoal.CatchAny => true,
                QuestGoal.CatchSpecies => c.Species.Id == q.SpeciesId,
                QuestGoal.CatchInHabitat => c.Habitat == q.Habitat,
                QuestGoal.CatchSpeciesInBand => c.Species.Id == q.SpeciesId && c.Band == q.Band,
                QuestGoal.CatchMinLength => c.Species.Id == q.SpeciesId && c.Length >= q.MinLen,
                _ => false
            };
        }

        void AdvanceGoal(QuestGoal goal, int by = 1)
        {
            bool any = false;
            foreach (var q in Active())
                if (q.Goal == goal) { Advance(q, by); any = true; }
            if (any) _profile.NotifyChanged();
        }

        /// <summary>물고기를 방생했다 (FishingController 결과 창 [방생하기]).</summary>
        public void OnReleased() => AdvanceGoal(QuestGoal.Release);

        /// <summary>통발을 설치했다 (통발 시스템이 부른다).</summary>
        public void OnTrapInstalled() => AdvanceGoal(QuestGoal.InstallTrap);

        /// <summary>
        /// 납품소에서 납품했다 (납품 시스템이 어종마다 부른다). 어종이 정해진 가이드는 그 어종을 그 낚시터에서 납품해야 센다.
        /// </summary>
        public void OnDelivered(string speciesId, string mapId, int count = 1)
        {
            bool any = false;
            foreach (var q in Active())
            {
                if (q.Goal != QuestGoal.DeliverFish) continue;
                if (q.SpeciesId != null && (q.SpeciesId != speciesId || (q.MapId != null && q.MapId != mapId))) continue;
                Advance(q, count);
                any = true;
            }
            if (any) _profile.NotifyChanged();
        }

        // ================= 개발용 =================

        /// <summary>이야기 진행 위치를 바꾸고 그 퀘스트를 받은 상태로 (자동 스크린샷).</summary>
        public void DebugSetStory(int index)
        {
            P.index = Math.Max(0, Math.Min(index, QuestDatabase.Story.Count));
            P.active.RemoveAll(a => QuestDatabase.Get(a.id)?.Type == QuestType.Story);
            var cur = Current;
            if (cur != null)
            {
                P.active.Add(new ActiveQuest { id = cur.Id });
                if (cur.Goal == QuestGoal.DistinctSpecies && ProgressOf(cur) >= cur.Count) Entry(cur.Id).ready = true;
            }
            _profile.NotifyChanged();
        }

        /// <summary>목표를 다 채운 상태로 (자동 스크린샷).</summary>
        public void DebugFill(QuestDef q)
        {
            if (!IsActive(q) || IsReady(q)) return;
            Advance(q, q.Count);
            _profile.NotifyChanged();
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

        // ================= 일일반복 (원작 REPEAT) =================

        public List<DailyQuest> Daily
        {
            get
            {
                EnsureDaily();
                return _profile.Data.daily;
            }
        }

        List<QuestDef> _dailyDefs = new List<QuestDef>();
        int _dailyDefsDay = int.MinValue;
        int _dailyDefsCount = -1;

        /// <summary>오늘의 일일반복 퀘스트 (그 낚시터 조합 연락관이 준다).</summary>
        public List<QuestDef> DailyDefs
        {
            get
            {
                var list = Daily;
                int day = _profile.Data.dailyDay;
                if (_dailyDefsDay == day && _dailyDefsCount == list.Count) return _dailyDefs;
                _dailyDefsDay = day;
                _dailyDefsCount = list.Count;
                _dailyDefs = new List<QuestDef>();
                for (int i = 0; i < list.Count; i++) _dailyDefs.Add(MakeDailyDef(list[i], i));
                return _dailyDefs;
            }
        }

        static QuestDef MakeDailyDef(DailyQuest d, int i)
        {
            var map = Fishing.World.Maps.Get(d.mapId);
            var sp = FishDatabase.Get(d.speciesId);
            string union = QuestNpcs.UnionOf(d.mapId);
            var q = new QuestDef
            {
                Id = $"daily{i}", Type = QuestType.Repeat, DailyIndex = i, MapId = d.mapId, RequireMap = true,
                Giver = union, Reporter = union, Count = d.count, RewardGold = d.gold, RewardExp = d.exp, RewardConch = 1,
                Lv = FishingAreas.EntryLevel(d.mapId) + (sp?.Rarity ?? 1),
            };
            switch (d.kind)
            {
                case 0:
                    q.Goal = QuestGoal.CatchSpecies; q.SpeciesId = d.speciesId;
                    q.Title = $"맛이 좋아 찾는 이가 많은 {sp?.Name}";
                    q.Desc = $"요즘 {map.ShortName} 손님들이 {sp?.Name}만 찾는다네. 오늘 안에 {d.count}마리만 잡아다 주게. 날이 바뀌면 다른 부탁을 하겠네.";
                    break;
                case 1:
                    q.Goal = QuestGoal.CatchInHabitat; q.Habitat = (Habitat)d.habitat;
                    q.Title = $"{map.HabitatName((Habitat)d.habitat)}의 단골손님";
                    q.Desc = $"{map.HabitatName((Habitat)d.habitat)}에 고기가 얼마나 붙는지 조사 중이라네. 그곳에서 아무 고기나 {d.count}마리 낚아 보게.";
                    break;
                default:
                    q.Goal = QuestGoal.CatchMinLength; q.SpeciesId = d.speciesId; q.MinLen = d.minLen;
                    q.Title = $"연락관이 주의 깊게 관찰하는 {sp?.Name}";
                    q.Desc = $"{d.minLen:0}cm가 넘는 {sp?.Name}이 {map.ShortName}에 들어왔다는 소문이 있네. 한 마리 올려서 보여 주게.";
                    break;
            }
            return q;
        }

        /// <summary>게임 하루가 바뀌면 오늘의 조합 의뢰 3개를 새로 만든다 (가 본 낚시터에서).</summary>
        public void EnsureDaily()
        {
            var d = _profile.Data;
            int day = Game.Clock != null ? Game.Clock.Day : 1;
            if (d.dailyDay == day && d.daily.Count > 0) return;
            d.dailyDay = day;
            d.daily.Clear();
            var maps = new List<string> { "hangang" };
            foreach (var m in d.visitedMaps) if (!maps.Contains(m)) maps.Add(m);
            var rng = new Random(day * 7919 + d.level);
            for (int i = 0; i < 3; i++)
            {
                string map = maps[rng.Next(maps.Count)];
                var pool = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(FishDatabase.DexOf(map), s => s.Rarity <= 3 && !s.SchoolOnly));
                if (pool.Count == 0) continue;
                var sp = pool[rng.Next(pool.Count)];
                var dq = new DailyQuest { mapId = map, kind = i, speciesId = sp.Id };
                if (i == 0) { dq.count = sp.Rarity <= 1 ? 3 : 2; }
                else if (i == 1)
                {
                    // 그 낚시터 격자에 있는 서식지 중에서 고른다 (헌화로처럼 해초밭이 없는 맵에서 못 깨는 의뢰가 나오지 않게)
                    var def = Fishing.World.Maps.Get(map);
                    var present = new List<Habitat>();
                    for (int r = 0; r < def.Rows; r++)
                    for (int c = 0; c < def.Cols; c++)
                        if (Array.IndexOf(sp.Habitats, def.HabitatAt(c, r)) >= 0 && !present.Contains(def.HabitatAt(c, r))) present.Add(def.HabitatAt(c, r));
                    if (present.Count == 0) present.Add(def.HabitatAt(def.Cols / 2, 0));
                    dq.habitat = (int)present[rng.Next(present.Count)];
                    dq.count = 3;
                    dq.speciesId = null;
                }
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
