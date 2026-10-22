using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fishing.Angling;
using Fishing.Data;
using Random = UnityEngine.Random;

/// <summary>
/// Unity 없이 파이팅 밸런스를 재는 콘솔 도구. 게임의 FishFight·FightBot·어종/장비 DB 소스를 그대로 컴파일하고
/// UnityEngine은 Stubs.cs로 대신한다 (Mathf·Random만 쓰므로). 에디터의 BalanceSim과 같은 조건으로 돈다.
/// 난수 생성기가 Unity와 달라 숫자는 BalanceSim 결과와 조금 다르다.
///
///   dotnet run -- report                       docs/balance-report.md 다시 쓰기 (BalanceSim -simSummary와 같은 표)
///   dotnet run -- table [어종id,..] [길이,..] [체력배율 내구도배율 릴배율]
///                                              표를 콘솔에 (체력 페널티: 지침 0.45 1 0.8, 탈진 0.15 1 0.35)
///   dotnet run -- auto  [어종id,..] [길이,..] [체력배율 내구도배율 릴배율]
///                                              자동 촬영(Autoplay)과 같은 조건: 매 프레임 FightBot, 카운터 0.08, 거리 20m
/// </summary>
static class FightSim
{
    static readonly (string name, string rod, string reel, string line, int plus, float dmg, float counter)[] Tiers =
    {
        ("입문", "rod_basic", "reel_basic", "line_nylon", 0, 0f, 0f),
        ("중급", "rod_carbon", "reel_power", "line_carbon", 0, 0f, 0f),
        ("상급", "rod_bamboo", "reel_power", "line_pe", 0, 0f, 0f),
        ("대물", "rod_heavy", "reel_hispeed", "line_pe", 0, 0f, 0f),
        ("대회", "rod_gege", "reel_hispeed", "line_pe", 0, 0f, 0f),
        ("대물+10·의상", "rod_heavy", "reel_hispeed", "line_pe", 10, 0.12f, 0.13f),
        ("대물+15·의상", "rod_heavy", "reel_hispeed", "line_pe", 15, 0.12f, 0.13f),
    };

    const int Trials = 40;
    const float Dt = 1f / 60f;
    const float TimeLimit = 240f;
    // 사람 반응 시간 (감기/풀기 판단 간격). FIGHTSIM_REACTION=0.5 처럼 바꿔서 느린 사람 기준도 잰다
    static readonly float ReactionTime = float.TryParse(Environment.GetEnvironmentVariable("FIGHTSIM_REACTION"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rt) ? rt : 0.25f;
    const float CounterChance = 0.5f;

    // 플레이어 상태 배율 (PlayerProfile.StaminaFactor·FightPower·ReelFactor)
    static float _stamina = 1f, _durability = 1f, _reel = -1f;

    static (int win, float avg, float max, int broken, int hookOut, int runOut, int timeout) Simulate(
        FishSpecies s, float len, (string name, string rod, string reel, string line, int plus, float dmg, float counter) tier, bool auto)
    {
        int win = 0, broken = 0, hookOut = 0, runOut = 0, timeout = 0;
        var times = new List<float>();
        for (int i = 0; i < Trials; i++)
        {
            var f = new FishFight(s, len, auto ? 20f : 18f, auto ? 0.9f : 0.8f,
                RodUpgrade.Apply(GearDatabase.GetGear(tier.rod), tier.plus), RodUpgrade.Apply(GearDatabase.GetGear(tier.reel), tier.plus),
                GearDatabase.GetGear(tier.line));
            f.CounterBonus = 1f + tier.counter;
            var held = default(FightInput);
            float nextDecision = 0f, nextTap = 0f;
            while (f.Outcome == FightOutcome.None && f.Elapsed < TimeLimit)
            {
                FightInput frame;
                if (auto) frame = f.Landing ? default : FightBot.Input(f, 0.08f);
                else
                {
                    // BalanceSim과 같다: 감기/풀기 판단은 반응 지연마다, 낚싯대 조준은 매 프레임
                    frame = FightBot.Input(f, 0f);
                    if (f.Elapsed >= nextDecision)
                    {
                        held = FightBot.Input(f, CounterChance);
                        nextDecision = f.Elapsed + ReactionTime;
                        frame.MouseDX = held.MouseDX;
                    }
                    frame.Reeling = held.Reeling;
                    frame.Releasing = held.Releasing;
                }
                f.Tick(Dt, frame, _stamina, _durability * (1f + tier.dmg), _reel);
                if (f.LandingChance) f.StartLanding();
                if (f.Landing && f.Elapsed >= nextTap)
                {
                    f.LandingTap(_stamina);
                    nextTap = f.Elapsed + 1f / FightBot.LandingTapsPerSecond;
                }
            }
            switch (f.Outcome)
            {
                case FightOutcome.Landed: win++; times.Add(f.Elapsed); break;
                case FightOutcome.LineBroken: broken++; break;
                case FightOutcome.HookOut: hookOut++; break;
                case FightOutcome.LineRunOut: runOut++; break;
                default: timeout++; break;
            }
        }
        return (win, times.Count > 0 ? times.Average() : 0f, times.Count > 0 ? times.Max() : 0f, broken, hookOut, runOut, timeout);
    }

    /// <summary>
    /// 입질 분포: 낚시터·미끼마다 잡동사니가 걸릴 확률 (서식지 4 × 시간대 5 평균, 맑음, 어군 없음 / 큰 어군 안).
    ///   dotnet run -- bites [맵id]
    /// </summary>
    static int Bites(string onlyMap)
    {
        string[] maps = { "hangang", "honghyeon", "seocheon", "yeongheung", "taean", "samcheok", "deungdae", "heonhwa", "yokji", "jeju", "cheongjin", "geomun", "halong", "santorini", "nile" };
        Console.WriteLine("| 맵 | 미끼 | 잡동사니 (어군 밖) | 잡동사니 (어군 안) |");
        Console.WriteLine("|---|---|---|---|");
        foreach (var map in maps)
        {
            if (onlyMap != null && map != onlyMap) continue;
            foreach (BaitKind bait in Enum.GetValues(typeof(BaitKind)))
            {
                var fish = FishDatabase.All.Where(s => !s.IsJunk && s.LivesIn(map) && Array.IndexOf(s.Baits, bait) >= 0).ToList();
                if (fish.Count == 0) continue;
                var school = new Fishing.World.FishSchool { SpeciesIds = fish.Take(3).Select(s => s.Id).ToArray() };
                float Share(Fishing.World.FishSchool sc)
                {
                    float sum = 0f; int n = 0;
                    foreach (Habitat h in Enum.GetValues(typeof(Habitat)))
                    foreach (TimeBand band in Enum.GetValues(typeof(TimeBand)))
                    {
                        var ctx = new BiteContext { Habitat = h, Bait = bait, Band = band, Weather = Weather.Clear, MapId = map, School = sc };
                        float fishW = 0f;
                        foreach (var s in FishDatabase.All)
                            if (!s.IsJunk) fishW += BiteSelector.Weight(s, ctx);
                        sum += fishW > 0f ? BiteSelector.JunkChance(fishW, ctx) : 1f;
                        n++;
                    }
                    return n > 0 ? sum / n : 0f;
                }
                Console.WriteLine($"| {map} | {bait} | {Share(null) * 100f:0.0}% | {Share(school) * 100f:0.0}% |");
            }
        }
        return 0;
    }

    static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "report";
        string[] ids = args.Length > 1 && args[1] != "" && args[1] != "all" ? args[1].Split(',') : null;
        float[] lens = args.Length > 2 && args[2] != "" && args[2] != "-" ? args[2].Split(',').Select(float.Parse).ToArray() : null;
        if (args.Length > 5)
        {
            _stamina = float.Parse(args[3]);
            _durability = float.Parse(args[4]);
            _reel = float.Parse(args[5]);
        }

        Random.InitState(1234);
        if (mode == "bites") return Bites(args.Length > 1 ? args[1] : null);
        var sb = new StringBuilder();
        bool report = mode == "report";
        if (report)
        {
            sb.AppendLine("# 파이팅 밸런스 시뮬레이션 (FightBot, 어종별 " + Trials + "회)");
            sb.AppendLine();
            sb.AppendLine("| 어종 | 길이 | 장비 | 성공률 | 평균 시간(s) | 줄 끊김 | 바늘 빠짐 | 줄 다 풀림 | 시간 초과 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        }
        else sb.AppendLine("| 어종 | 길이 | 장비 | 성공률 | 평균(s) | 최대(s) | 줄 끊김 | 바늘 빠짐 | 줄 다 풀림 | 시간 초과 |");

        foreach (var s in FishDatabase.All.Where(f => !f.IsJunk && (ids == null || ids.Contains(f.Id))))
        foreach (var len in lens ?? new[] { s.TrophyLen, s.MaxLen })
        foreach (var tier in Tiers)
        {
            var r = Simulate(s, len, tier, mode == "auto");
            sb.AppendLine(report
                ? $"| {s.Name} | {len:0.0} | {tier.name} | {r.win * 100f / Trials:0}% | {r.avg:0.0} | {r.broken} | {r.hookOut} | {r.runOut} | {r.timeout} |"
                : $"| {s.Name} | {len:0} | {tier.name} | {r.win * 100f / Trials:0}% | {r.avg:0.0} | {r.max:0.0} | {r.broken} | {r.hookOut} | {r.runOut} | {r.timeout} |");
        }

        if (report)
        {
            var path = Path.Combine(Root(), "docs", "balance-report.md");
            File.WriteAllText(path, sb.ToString().Replace("\r\n", "\n"));
            Console.WriteLine("결과 저장: " + path);
        }
        else Console.Write(sb.ToString());
        return 0;
    }
}
