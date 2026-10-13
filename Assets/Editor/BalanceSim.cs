using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fishing.Angling;
using Fishing.Data;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 파이팅 밸런스 시뮬레이션. 어종 × 크기 × 장비 단계별로 FightBot이 싸웠을 때의 성공률과 소요 시간을 출력한다.
    /// 메뉴 Fishing/Balance Simulation 또는 -executeMethod Fishing.EditorTools.BalanceSim.Run
    /// </summary>
    public static class BalanceSim
    {
        const int Trials = 40;
        const float Dt = 1f / 60f;
        const float TimeLimit = 240f;
        const float ReactionTime = 0.25f; // 사람의 반응 지연: 감기/풀기 판단을 이 간격으로만 갱신한다
        const float CounterChance = 0.5f;  // 끝에 몰렸을 때 판단마다 카운터를 시도할 확률

        /// <summary>장비 단계. plus = 낚싯대·릴 강화 단계, dmg/counter = 의상 데미지·카운터 보너스.</summary>
        static readonly (string name, string rod, string reel, string line, int plus, float dmg, float counter)[] Tiers =
        {
            ("입문", "rod_basic", "reel_basic", "line_nylon", 0, 0f, 0f),
            ("중급", "rod_carbon", "reel_power", "line_carbon", 0, 0f, 0f),
            ("상급", "rod_bamboo", "reel_power", "line_pe", 0, 0f, 0f),
            ("대물", "rod_heavy", "reel_hispeed", "line_pe", 0, 0f, 0f),
            ("대회", "rod_gege", "reel_napal", "line_pe", 0, 0f, 0f),
            ("대물+10·의상", "rod_heavy", "reel_hispeed", "line_pe", 10, 0.12f, 0.13f),
            ("대물+15·의상", "rod_heavy", "reel_hispeed", "line_pe", 15, 0.12f, 0.13f),
        };

        public static bool Summary;

        [MenuItem("Fishing/Balance Simulation")]
        public static void Run()
        {
            Random.InitState(1234);
            Summary |= System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-simSummary") >= 0;
            var sb = new StringBuilder();
            sb.AppendLine("# 파이팅 밸런스 시뮬레이션 (FightBot, 어종별 " + Trials + "회)");
            sb.AppendLine();
            sb.AppendLine("| 어종 | 길이 | 장비 | 성공률 | 평균 시간(s) | 줄 끊김 | 바늘 빠짐 | 줄 다 풀림 | 시간 초과 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");

            foreach (var s in FishDatabase.All.Where(f => !f.IsJunk))
            {
                float[] lengths = { Mathf.Lerp(s.MinLen, s.MaxLen, 0.25f), s.TrophyLen, s.MaxLen };
                if (Summary) lengths = new[] { s.TrophyLen, s.MaxLen };
                foreach (var len in lengths)
                foreach (var tier in Tiers)
                {
                    var r = Simulate(s, len, tier);
                    sb.AppendLine($"| {s.Name} | {len:0.0} | {tier.name} | {r.win * 100f / Trials:0}% | {r.avgTime:0.0} | {r.broken} | {r.hookOut} | {r.runOut} | {r.timeout} |");
                }
            }

            var path = "docs/balance-report.md";
            System.IO.Directory.CreateDirectory("docs");
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log("[BalanceSim] 결과 저장: " + path + "\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static (int win, float avgTime, int broken, int hookOut, int runOut, int timeout) Simulate(
            FishSpecies s, float len, (string name, string rod, string reel, string line, int plus, float dmg, float counter) tier)
        {
            int win = 0, broken = 0, hookOut = 0, runOut = 0, timeout = 0;
            var times = new List<float>();
            for (int i = 0; i < Trials; i++)
            {
                var fight = new FishFight(s, len, 18f, 0.8f,
                    RodUpgrade.Apply(GearDatabase.GetGear(tier.rod), tier.plus), RodUpgrade.Apply(GearDatabase.GetGear(tier.reel), tier.plus),
                    GearDatabase.GetGear(tier.line));
                fight.CounterBonus = 1f + tier.counter;
                // 감기/풀기 판단은 ReactionTime마다만 바뀌고(반응 지연), 낚싯대 조준은 매 프레임 따라간다.
                var held = default(FightInput);
                float nextDecision = 0f;
                float nextTap = 0f;
                while (fight.Outcome == FightOutcome.None && fight.Elapsed < TimeLimit)
                {
                    var frame = FightBot.Input(fight, 0f);
                    if (fight.Elapsed >= nextDecision)
                    {
                        held = FightBot.Input(fight, CounterChance);
                        nextDecision = fight.Elapsed + ReactionTime;
                        frame.MouseDX = held.MouseDX; // 카운터 플릭은 판단 시점에만
                    }
                    frame.Reeling = held.Reeling;
                    frame.Releasing = held.Releasing;
                    fight.Tick(Dt, frame, 1f, 1f + tier.dmg);
                    if (fight.LandingChance) fight.StartLanding();
                    if (fight.Landing && fight.Elapsed >= nextTap)
                    {
                        fight.LandingTap();
                        nextTap = fight.Elapsed + 1f / FightBot.LandingTapsPerSecond;
                    }
                }
                switch (fight.Outcome)
                {
                    case FightOutcome.Landed: win++; times.Add(fight.Elapsed); break;
                    case FightOutcome.LineBroken: broken++; break;
                    case FightOutcome.HookOut: hookOut++; break;
                    case FightOutcome.LineRunOut: runOut++; break;
                    default: timeout++; break;
                }
            }
            return (win, times.Count > 0 ? times.Average() : 0f, broken, hookOut, runOut, timeout);
        }
    }
}
