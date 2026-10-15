using System.Collections.Generic;
using System.Linq;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 조합 낚시 대회 (솔로판). 규칙은 <see cref="TournamentRules"/>.
    ///  - 08:00부터 조합 연락관(또는 대기실 퀘스트 창 [대회] 탭)에서 참가 신청
    ///  - 09:00~15:00 그날 대회 맵에서 낚은 고기만 기록된다. 참가하면 시간이 천천히 흐른다
    ///  - 15:00에 정산: 순위 보상, 누적 포인트(엠블럼), 1위 거거 낚시대 · 2위 나팔고둥 릴
    /// AI 낚시꾼의 성적은 날짜로 정해지므로 저장하지 않는다.
    /// </summary>
    public class TournamentSystem : MonoBehaviour
    {
        PlayerProfile Profile => Game.Profile;
        TournamentSave S => Game.Profile.Data.tournament;

        List<AiCatch> _ai;
        int _aiDay = -1;
        TournamentRules.Phase _lastPhase;
        int _lastDay = -1;
        bool _slowed;

        public int Day => Game.Clock.Day;
        /// <summary>오늘 대회 맵. 하루 안에서는 레벨이 올라도 바뀌지 않게 그날 처음 정한 것을 쓴다.</summary>
        public string MapId
        {
            get
            {
                if (_mapDay != Day) { _mapDay = Day; _mapId = TournamentRules.MapOf(Day, Game.Profile != null ? Game.Profile.Data.level : 0); }
                return _mapId;
            }
        }
        int _mapDay = -1;
        string _mapId;
        public TournamentRules.Phase Phase => TournamentRules.PhaseAt(Game.Clock.Minutes);
        /// <summary>오늘 대회에 참가했는지.</summary>
        public bool Joined => S.day == Day;
        /// <summary>지금 내 낚시가 대회 기록에 들어가는지 (참가 · 진행 중 · 대회 맵).</summary>
        public bool Counting => Joined && !S.settled && Phase == TournamentRules.Phase.Running && Game.Mode == GameMode.Field
                                && World.FieldMap.Id == S.mapId;
        public float MinutesLeft => Mathf.Max(0f, TournamentRules.EndMinute - Game.Clock.Minutes);
        public float Elapsed => Mathf.Clamp(Game.Clock.Minutes - TournamentRules.StartMinute, 0f, TournamentRules.Duration);

        void Start()
        {
            _lastDay = Day;
            _lastPhase = Phase;
            // 지난 대회를 정산하지 못하고 껐다면 (저장 후 다음 날 켬) 지금 정산한다
            if (S.day >= 0 && !S.settled && (S.day < Day || Phase == TournamentRules.Phase.Finished)) Settle();
        }

        void Update()
        {
            if (Game.Clock == null) return;
            var phase = Phase;
            if (Day != _lastDay || phase != _lastPhase)
            {
                OnPhaseChanged(phase, Day != _lastDay);
                _lastDay = Day;
                _lastPhase = phase;
            }
            // 대회 중에는 시간이 천천히 흐른다 (참가했고 대회 맵에 있을 때만)
            bool slow = Counting;
            if (slow != _slowed)
            {
                _slowed = slow;
                Game.Clock.Speed = slow ? TournamentRules.ClockSpeed : 1f;
            }
        }

        void OnPhaseChanged(TournamentRules.Phase phase, bool newDay)
        {
            if (newDay && S.day >= 0 && S.day < Day && !S.settled) Settle();
            switch (phase)
            {
                case TournamentRules.Phase.Registration:
                    Game.Toast($"오늘 {TournamentRules.Title(Day, MapId)}가 09:00에 열립니다! {World.Maps.Get(MapId).UnionContact}이나 대기실에서 참가 신청하세요.", ToastKind.Great);
                    break;
                case TournamentRules.Phase.Running:
                    if (Joined)
                    {
                        Game.Toast($"{TournamentRules.Title(Day, MapId)} 시작! 15:00까지 많이 · 길게 · 길이 합계로 겨룹니다.", ToastKind.Great);
                        if (Game.Sound != null) Game.Sound.LevelUp();
                    }
                    break;
                case TournamentRules.Phase.Finished:
                    if (Joined && !S.settled) Settle();
                    break;
            }
        }

        /// <summary>참가 신청할 수 없는 이유 (가능하면 null).</summary>
        public string CannotJoinReason()
        {
            if (Joined) return "이미 오늘 대회에 참가했습니다.";
            var phase = Phase;
            if (phase == TournamentRules.Phase.Before) return "참가 신청은 08:00부터입니다.";
            if (phase == TournamentRules.Phase.Finished) return "오늘 대회는 끝났습니다. 내일 대회에 참가하세요.";
            int lv = FishingAreas.EntryLevel(MapId);
            if (Profile.Data.level < lv) return $"{TournamentRules.MapShortName(MapId)} 대회는 Lv.{lv}부터 참가할 수 있습니다.";
            return null;
        }

        public bool Join()
        {
            var why = CannotJoinReason();
            if (why != null) { Game.Toast(why, ToastKind.Warn); return false; }
            var s = S;
            s.day = Day;
            s.mapId = MapId;
            s.settled = false;
            s.count = 0;
            s.longest = 0f;
            s.longestFish = null;
            s.total = 0f;
            Game.Toast(Phase == TournamentRules.Phase.Running
                ? $"{TournamentRules.Title(Day, MapId)}에 참가했습니다! 이미 시작했으니 서두르세요 ({TournamentRules.MapShortName(MapId)})."
                : $"{TournamentRules.Title(Day, MapId)}에 참가 신청했습니다. 09:00에 {TournamentRules.MapShortName(MapId)}에서 시작합니다.", ToastKind.Great);
            Profile.NotifyChanged();
            return true;
        }

        /// <summary>낚을 때마다 FishingController가 부른다.</summary>
        public void OnCatch(FishSpecies species, float length)
        {
            if (!Counting || species.IsJunk) return;
            S.count++;
            S.total += length;
            if (length > S.longest) { S.longest = length; S.longestFish = species.Name; }
            var me = Entries().First(e => e.IsPlayer);
            Game.Toast($"[대회] {species.Name} {length:0.0}cm 기록!  지금 {me.Rank}위 · {me.Points}점", ToastKind.Good);
        }

        List<AiCatch> Ai(int day, string mapId)
        {
            if (_aiDay != day || _ai == null) { _ai = TournamentRules.SimulateAi(day, mapId); _aiDay = day; }
            return _ai;
        }

        /// <summary>지금까지의 순위표 (끝난 대회면 최종 순위).</summary>
        public List<TournamentEntry> Entries() => EntriesFor(Joined ? S.day : Day, Joined ? S.mapId : MapId,
            Joined && !S.settled ? Elapsed : TournamentRules.Duration, Joined);

        List<TournamentEntry> EntriesFor(int day, string mapId, float upTo, bool withPlayer)
        {
            var list = new List<TournamentEntry>();
            if (withPlayer)
                list.Add(new TournamentEntry { Name = "강태공 (나)", IsPlayer = true, Count = S.count, Longest = S.longest, LongestFish = S.longestFish, TotalLength = S.total });
            var ai = TournamentRules.Anglers.Select(a => new TournamentEntry { Name = a.name }).ToList();
            foreach (var c in Ai(day, mapId))
            {
                if (c.Minute > upTo) break;
                ai[c.Angler].Add(FishDatabase.Get(c.SpeciesId)?.Name, c.Length);
            }
            list.AddRange(ai);
            TournamentRules.Score(list);
            return list;
        }

        /// <summary>대회 결과를 정산하고 보상을 준다.</summary>
        void Settle()
        {
            var s = S;
            if (s.day < 0 || s.settled) return;
            var entries = EntriesFor(s.day, s.mapId, TournamentRules.Duration, true);
            var me = entries.First(e => e.IsPlayer);
            s.settled = true;
            s.played++;
            s.lastRank = me.Rank;
            s.lastDay = s.day;
            if (s.bestRank == 0 || me.Rank < s.bestRank) s.bestRank = me.Rank;
            int prevEmblem = TournamentRules.EmblemIndex(s.points);
            int gained = TournamentRules.ParticipationPoints + me.Points;
            s.points += gained;
            if (me.Rank == 1) s.wins++;
            if (me.Rank <= 3) s.podiums++;

            var (gold, conch) = TournamentRules.Reward(me.Rank);
            Profile.AddGold(gold);
            if (conch > 0) Profile.AddItem(AchievementDatabase.ConchId, conch);
            string extra = "";
            if (me.Rank == 1) extra = GiveGear(TournamentRules.RewardRodId) ?? GiveGear(TournamentRules.RewardReelId) ?? "";
            else if (me.Rank == 2) extra = GiveGear(TournamentRules.RewardReelId) ?? "";
            if (me.Rank <= 3 && string.IsNullOrEmpty(extra)) Profile.AddItem(RodUpgrade.StarSandId, 1);

            string winner = entries[0].IsPlayer ? "우승!" : $"우승은 {entries[0].Name}";
            Game.Toast($"{TournamentRules.Title(s.day, s.mapId)} 종료 — 나는 {me.Rank}위 ({me.Points}점, {me.Count}마리 · 최장 {me.Longest:0.0}cm). {winner}",
                me.Rank <= 3 ? ToastKind.Great : ToastKind.Info);
            Game.Toast($"대회 보상: {gold:N0}골드" + (conch > 0 ? $" · 소라고동 {conch}" : "") + extra + $" · 대회 포인트 +{gained}",
                ToastKind.Good);
            int nowEmblem = TournamentRules.EmblemIndex(s.points);
            if (nowEmblem > prevEmblem) Game.Toast($"계급 엠블럼 승급! {TournamentRules.Emblems[nowEmblem].name}", ToastKind.Great);
            if (Game.Sound != null) { if (me.Rank <= 3) Game.Sound.LevelUp(); else Game.Sound.Coin(); }
            Game.Quests?.CheckAchievements();
            Profile.NotifyChanged();
        }

        /// <summary>보상 장비를 준다 (이미 있으면 null).</summary>
        string GiveGear(string id)
        {
            if (Profile.Owns(id)) return null;
            Profile.Data.ownedGear.Add(id);
            var g = GearDatabase.GetGear(id);
            return $" · <color=#ffd060>{g.Name}</color>";
        }

        // ---------------- 개발용 ----------------

        /// <summary>개발용: 오늘 대회에 바로 참가하고 기록을 채운다.</summary>
        public void DebugJoin(int count, float longest, string fish, float total)
        {
            var s = S;
            s.day = Day;
            s.mapId = MapId;
            s.settled = false;
            s.count = count;
            s.longest = longest;
            s.longestFish = fish;
            s.total = total;
            Profile.NotifyChanged();
        }
    }
}
