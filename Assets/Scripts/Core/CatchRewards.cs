using System.Collections.Generic;
using System.Linq;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 원작 RESULT 오른쪽 말풍선 토스트·BONUS 보상, 재도전(복수혈전), 오늘의 이벤트 어종(EXP +200%), 조준 중 업적 3줄.
    /// 원작 관찰값 (clips docs 03·06·09·13):
    ///  - 토스트 노랑(보상형): "첫 월척 물고기" "5가지 물고기" "첫 고급 물고기(오늘)" "중급 물고기 10마리(오늘)" "복수혈전 - 방금 놓친 ○○를 낚으셨습니다"
    ///    → 각각 GOLD·EXP, 결과 창에 "BONUS 보상 460 GOLD 1910 EXP" 합산. Lv.17에서 업적 하나 955 EXP / 230 GOLD
    ///  - 토스트 하늘색(기록형): "연속 색깔 맞추기(같은 등급 연속)" "연속 공격!(한 파이팅 최다 카운터)" "최고 기록"
    ///  - 실패 뒤 "재도전 하시겠습니까?" + 오른쪽 위 "X2 재도전" 배지 (X5까지 관찰)
    ///  - 조준 중 거리 숫자 아래 업적 3줄: 월척급 이상 1,000마리 158/1000 · 랜딩으로 400회 240/400 · 카운터 1회 이상 성공 437/700
    ///  - 등급 메달: 초록(초급) &lt; 파랑(중급) &lt; 보라(고급)
    /// </summary>
    public static class CatchRewards
    {
        public sealed class Toast
        {
            public string Title, Body;
            public int Gold, Exp;
            /// <summary>true = 노랑 보상형, false = 하늘색 기록형.</summary>
            public bool Reward;
        }

        public sealed class Outcome
        {
            public readonly List<Toast> Toasts = new List<Toast>();
            public int BonusGold, BonusExp;
            /// <summary>오늘의 이벤트 어종이면 200 (EXP +200%).</summary>
            public int EventPct;
            public bool Revenge;
            public int RevengeMult;
            public float FightSeconds;
            public int Grade;
        }

        static RewardSave S => Game.Profile.Data.rewards;

        // ---------------- 등급 ----------------

        /// <summary>원작 메달 색 등급: 0 초급(초록) · 1 중급(파랑) · 2 고급(보라).</summary>
        public static int Grade(FishSpecies s) => s.Rarity <= 1 ? 0 : s.Rarity == 2 ? 1 : 2;
        public static string GradeName(int g) => g switch { 0 => "초급", 1 => "중급", _ => "고급" };
        public static Color GradeColor(int g) => g switch
        {
            0 => new Color(0.45f, 0.88f, 0.4f),
            1 => new Color(0.35f, 0.7f, 1f),
            _ => new Color(0.72f, 0.45f, 1f),
        };

        // ---------------- 오늘의 이벤트 어종 ----------------

        /// <summary>낚시터마다 게임 하루에 한 종: 잡으면 EXP +200% (원작 RESULT "EXP +200%" 엠블럼).</summary>
        public static FishSpecies EventSpecies(string mapId, int day)
        {
            var list = FishDatabase.DexOf(mapId).Where(s => !s.Boss && s.Rarity >= 1 && s.Rarity <= 3).ToList();
            if (list.Count == 0) return null;
            int h = (day * 7919 + mapId.GetHashCode() * 31) & 0x7fffffff;
            return list[h % list.Count];
        }

        public static FishSpecies TodayEvent => Game.Clock == null ? null : EventSpecies(Fishing.World.FieldMap.Id, Game.Clock.Day);

        // ---------------- 조준 업적 3줄 ----------------

        static readonly int[] TrophyTiers = { 10, 50, 100, 400, 1000 };
        static readonly int[] LandTiers = { 10, 50, 100, 400, 1000, 2000 };
        static readonly int[] CounterTiers = { 5, 20, 70, 200, 700 };

        static int Goal(int[] tiers, int tier) => tiers[Mathf.Min(tier, tiers.Length - 1)];

        /// <summary>조준 중 큰 거리 숫자 아래에 띄우는 업적 진행 3줄.</summary>
        public static string[] AimLines()
        {
            var s = S;
            int tg = Goal(TrophyTiers, s.trophyTier), lg = Goal(LandTiers, s.landedTier), cg = Goal(CounterTiers, s.counterTier);
            return new[]
            {
                $"월척급 이상 물고기 {tg:N0}마리 낚기 {Mathf.Min(s.trophyTotal, tg)}/{tg}",
                $"랜딩으로 물고기 {lg:N0}회 낚기 {Mathf.Min(s.landedTotal, lg)}/{lg}",
                $"파이팅 도중 카운터 1회 이상 성공해보기 {Mathf.Min(s.counterFights, cg)}/{cg}",
            };
        }

        static void CheckTier(int[] tiers, ref int tier, int value, string label, Outcome o)
        {
            while (tier < tiers.Length && value >= tiers[tier])
            {
                int k = tier + 1;
                o.Toasts.Add(RewardToast("[업적] " + string.Format(label, tiers[tier]), "도전과제를 달성했습니다.", k, 0));
                tier++;
            }
        }

        // ---------------- 보상 크기 ----------------

        /// <summary>업적 토스트 하나의 보상. 원작 Lv.17: 955 EXP / 230 GOLD (기본 337 EXP 고기).</summary>
        static Toast RewardToast(string title, string body, float k, int baseExp)
        {
            int lv = Game.Profile.Data.level;
            return new Toast
            {
                Title = title, Body = body, Reward = true,
                Gold = Mathf.RoundToInt((60f + 10f * lv) * k / 10f) * 10,
                Exp = Mathf.RoundToInt((40f * lv + baseExp * 0.8f) * k / 5f) * 5,
            };
        }

        static void RollDay()
        {
            int day = Game.Clock.Day;
            if (S.day == day) return;
            S.day = day;
            S.midToday = 0;
            S.highToday = false;
        }

        static readonly int[] DexTiers = { 5, 10, 20, 30, 50, 75, 100, 130, 165 };

        /// <summary>파이팅이 끝났다 (성공·실패 모두): 카운터를 1회 이상 친 파이팅을 센다.</summary>
        public static void OnFightEnded(int counters)
        {
            if (counters >= 1) S.counterFights++;
        }

        /// <summary>
        /// 고기를 낚았다. baseExp는 이미 계산한 기본 EXP. 이벤트·재도전 배율을 적용한 EXP를 돌려주고, 토스트·BONUS를 outcome에 담는다.
        /// BONUS는 여기서 바로 지급한다 (시스템 줄도 원작처럼 줄마다 나눠 찍는다).
        /// </summary>
        public static int OnCatch(FishSpecies sp, float length, int baseExp, int counters, bool record, float prevBest,
            float fightSeconds, out Outcome o)
        {
            o = new Outcome { FightSeconds = fightSeconds, Grade = Grade(sp) };
            if (sp.IsJunk) return baseExp;
            RollDay();
            var s = S;
            var p = Game.Profile;
            int exp = baseExp;

            var ev = TodayEvent;
            if (ev != null && ev.Id == sp.Id)
            {
                o.EventPct = 200;
                exp = baseExp * 3;
            }

            // 재도전(복수혈전)
            if (s.revengeArmed && s.revengeId == sp.Id && Mathf.Abs(s.revengeLen - length) < 0.05f)
            {
                o.Revenge = true;
                o.RevengeMult = s.revengeMult;
                var t = RewardToast("복수혈전", $"방금 놓친 {sp.Name}{QuestNpcs.Josa(sp.Name, "을", "를")} 낚으셨습니다.", 0.6f, baseExp);
                t.Exp += baseExp * (s.revengeMult - 1);
                o.Toasts.Add(t);
                ClearRevenge();
            }

            bool trophy = sp.IsTrophy(length);
            if (trophy && !s.firstTrophy.Contains(sp.Id))
            {
                s.firstTrophy.Add(sp.Id);
                o.Toasts.Add(RewardToast("첫 월척 물고기", $"처음으로 {sp.TrophyName}급 {sp.Name}{QuestNpcs.Josa(sp.Name, "을", "를")} 낚았습니다.", 1f, baseExp));
            }

            int dex = p.Data.dex.Count;
            while (s.dexTier < DexTiers.Length && dex >= DexTiers[s.dexTier])
            {
                o.Toasts.Add(RewardToast($"{DexTiers[s.dexTier]}가지 물고기", $"지금까지 {DexTiers[s.dexTier]}가지 물고기를 낚았습니다.", 1f + s.dexTier * 0.3f, baseExp));
                s.dexTier++;
            }

            int g = o.Grade;
            if (g == 2 && !s.highToday)
            {
                s.highToday = true;
                o.Toasts.Add(RewardToast("첫 고급 물고기", "이 물고기는 오늘 처음 낚은 고급 물고기입니다.", 1.6f, baseExp));
            }
            if (g == 1)
            {
                s.midToday++;
                if (s.midToday % 10 == 0)
                    o.Toasts.Add(RewardToast($"중급 물고기 {s.midToday}마리", $"오늘 중급 물고기를 {s.midToday}마리 낚았습니다. 다음 목표: {s.midToday + 10}", 1.6f, baseExp));
            }

            s.landedTotal++;
            if (trophy) s.trophyTotal++;
            CheckTier(LandTiers, ref s.landedTier, s.landedTotal, "랜딩으로 물고기 {0:N0}회 낚기", o);
            CheckTier(TrophyTiers, ref s.trophyTier, s.trophyTotal, "월척급 이상 물고기 {0:N0}마리 낚기", o);
            CheckTier(CounterTiers, ref s.counterTier, s.counterFights, "카운터를 친 파이팅 {0:N0}회", o);

            // 기록형 (하늘색)
            s.gradeStreak = s.lastGrade == g ? s.gradeStreak + 1 : 1;
            s.lastGrade = g;
            if (s.gradeStreak >= 2 && s.gradeStreak > s.bestGradeStreak)
            {
                s.bestGradeStreak = s.gradeStreak;
                o.Toasts.Add(new Toast { Title = "연속 색깔 맞추기", Body = $"연속 같은 등급 물고기 기록을 달성하셨습니다. 신규 기록: {s.gradeStreak}회" });
            }
            if (counters >= 2 && counters > s.bestCounters)
            {
                o.Toasts.Add(new Toast { Title = "연속 공격!", Body = $"가장 많은 카운터를 친 파이팅이었습니다. 신규 기록: {counters}회" + (s.bestCounters > 0 ? $" / 기존 기록: {s.bestCounters}회" : "") });
                s.bestCounters = counters;
            }
            if (record && prevBest > 0f)
                o.Toasts.Add(new Toast { Title = "최고 기록", Body = $"이전 최고 기록 {prevBest:0.00}cm를 {length:0.00}cm로 갱신했습니다." });

            foreach (var t in o.Toasts)
            {
                if (!t.Reward) continue;
                o.BonusGold += t.Gold;
                o.BonusExp += t.Exp;
            }
            return exp;
        }

        /// <summary>BONUS를 지급하고 시스템 줄에 원작처럼 한 줄씩 찍는다 (결과 창을 띄운 뒤).</summary>
        public static void Pay(Outcome o)
        {
            var p = Game.Profile;
            foreach (var t in o.Toasts)
            {
                if (!t.Reward) continue;
                if (t.Exp > 0) Game.SysLog($"{t.Exp:N0} EXP 를 획득했습니다.", QuestSystem.ExpColor);
                if (t.Gold > 0) Game.SysLog($"{t.Gold:N0} GOLD 를 획득했습니다.");
            }
            if (o.BonusGold > 0) p.AddGold(o.BonusGold);
            if (o.BonusExp > 0) p.AddExp(o.BonusExp);
        }

        // ---------------- 재도전 (복수혈전) ----------------

        static readonly int[] Mults = { 2, 3, 5 };

        /// <summary>지금 재도전을 걸어 둔 상태 (오른쪽 위 "X2 재도전" 배지).</summary>
        public static bool Armed => Game.Profile != null && S.revengeArmed && !string.IsNullOrEmpty(S.revengeId);
        public static int Mult => S.revengeMult;
        public static string RevengeName => FishDatabase.Get(S.revengeId)?.Name ?? "";

        /// <summary>고기를 놓쳤다: 재도전 후보로 기억한다. 재도전 중이던 고기를 또 놓치면 배율이 X2 → X3 → X5.</summary>
        public static bool OnLost(FishSpecies sp, float length)
        {
            if (sp == null || sp.IsJunk) return false;
            var s = S;
            bool again = s.revengeArmed && s.revengeId == sp.Id;
            int idx = again ? System.Array.IndexOf(Mults, s.revengeMult) + 1 : 0;
            s.revengeId = sp.Id;
            s.revengeLen = length;
            s.revengeMap = Fishing.World.FieldMap.Id;
            s.revengeMult = Mults[Mathf.Clamp(idx, 0, Mults.Length - 1)];
            s.revengeArmed = false;
            return true;
        }

        /// <summary>"재도전 하시겠습니까?"를 눌렀다: 이 낚시터에서 다음 입질은 놓친 그 고기.</summary>
        public static void Arm()
        {
            if (string.IsNullOrEmpty(S.revengeId)) return;
            S.revengeArmed = true;
            Game.SysLog($"[재도전] {RevengeName} X{S.revengeMult} — 다음 입질에 다시 옵니다.", "#7fd8ff");
        }

        /// <summary>재도전 제안을 그냥 넘겼다.</summary>
        public static void Decline()
        {
            if (!S.revengeArmed) ClearRevenge();
        }

        /// <summary>자동 촬영용: 재도전 상태를 지운다.</summary>
        public static void DebugClear() => ClearRevenge();

        static void ClearRevenge()
        {
            S.revengeId = "";
            S.revengeArmed = false;
            S.revengeMult = 0;
        }

        /// <summary>재도전 중이고 그 고기를 놓친 낚시터면 놓친 그 고기가 문다.</summary>
        public static bool TryRevengeBite(string mapId, out FishSpecies species, out float length)
        {
            species = null;
            length = 0f;
            if (!Armed || S.revengeMap != mapId) return false;
            species = FishDatabase.Get(S.revengeId);
            length = S.revengeLen;
            return species != null;
        }
    }
}
