using System.Collections.Generic;
using System.Linq;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using NUnit.Framework;
using UnityEngine;

namespace Fishing.EditorTools.Tests
{
    /// <summary>
    /// 게임 규칙 단위 테스트 (EditMode, 그래픽 없이). 실행: sh tools/test.sh 또는 Window > General > Test Runner.
    /// 순수 C# 규칙(FishFight, BiteSelector, TournamentRules, RodUpgrade, PlayerProfile, Settings)만 다룬다.
    /// </summary>
    public class FishFightTests
    {
        static GearDef G(string id) => GearDatabase.GetGear(id);

        /// <summary>FightBot으로 끝까지 싸운다 (BalanceSim과 같은 방식, 반응 지연 없이).</summary>
        static FishFight Run(string species, float len, string rod, string reel, string line, float limit = 240f,
            float staminaFactor = 1f, float reelFactor = -1f)
        {
            var f = new FishFight(FishDatabase.Get(species), len, 18f, 0.8f, G(rod), G(reel), G(line));
            float nextTap = 0f;
            while (f.Outcome == FightOutcome.None && f.Elapsed < limit)
            {
                f.Tick(1f / 60f, FightBot.Input(f, 0.3f), staminaFactor, 1f, reelFactor);
                if (f.LandingChance) f.StartLanding();
                if (f.Landing && f.Elapsed >= nextTap)
                {
                    f.LandingTap(staminaFactor);
                    nextTap = f.Elapsed + 1f / FightBot.LandingTapsPerSecond;
                }
            }
            return f;
        }

        [SetUp]
        public void Seed() => Random.InitState(1234);

        [Test]
        public void SmallFishIsLandedWithBasicGear()
        {
            int landed = 0;
            for (int i = 0; i < 10; i++)
                if (Run("crucian", 25f, "rod_basic", "reel_basic", "line_nylon").Outcome == FightOutcome.Landed) landed++;
            Assert.GreaterOrEqual(landed, 9, "작은 붕어는 입문 장비로 거의 다 잡혀야 한다");
        }

        [Test]
        public void TiredPlayerCanStillLandTrophyFish()
        {
            // 체력 20% 이하(지침: 데미지·연타 ×0.45, 릴 ×0.8)여도 대물을 끝내 올릴 수 있어야 한다.
            // 랜딩 중 물고기가 지치지 않으면 연타가 되밀림을 못 이겨 랜딩·파이팅만 끝없이 오갔다 (나일농어 158cm).
            int landed = 0;
            for (int i = 0; i < 10; i++)
                if (Run("nileperch", 158f, "rod_heavy", "reel_hispeed", "line_pe", 180f, 0.45f, 0.8f).Outcome == FightOutcome.Landed) landed++;
            Assert.GreaterOrEqual(landed, 7, "지친 상태로도 대물 장비면 나일농어 대물을 대부분 올려야 한다");
        }

        [Test]
        public void BiggerFishTakesLonger()
        {
            float small = 0f, big = 0f;
            for (int i = 0; i < 6; i++)
            {
                small += Run("carp", 30f, "rod_carbon", "reel_power", "line_carbon").Elapsed;
                big += Run("carp", 100f, "rod_carbon", "reel_power", "line_carbon").Elapsed;
            }
            Assert.Greater(big, small, "대물은 파이팅이 더 길어야 한다");
        }

        [Test]
        public void HoldingReelAtFullTensionBreaksLine()
        {
            var f = new FishFight(FishDatabase.Get("giantgrouper"), 130f, 18f, 1f, G("rod_basic"), G("reel_basic"), G("line_nylon"));
            for (int i = 0; i < 60 * 120 && f.Outcome == FightOutcome.None; i++)
                f.Tick(1f / 60f, new FightInput { Reeling = true, MouseDX = -f.FishSide * 30f });
            Assert.AreEqual(FightOutcome.LineBroken, f.Outcome, "그루퍼를 입문 장비로 무작정 감으면 줄이 끊어져야 한다");
        }

        [Test]
        public void DamageReducesHp()
        {
            var f = new FishFight(FishDatabase.Get("carp"), 60f, 18f, 0.8f, G("rod_basic"), G("reel_basic"), G("line_nylon"));
            float hp = f.HP;
            for (int i = 0; i < 60 * 5; i++) f.Tick(1f / 60f, FightBot.Input(f, 0f));
            Assert.Less(f.HP, hp);
        }

        [Test]
        public void StrongerRodDealsMoreDamage()
        {
            float Hp(string rod)
            {
                Random.InitState(7);
                var f = new FishFight(FishDatabase.Get("carp"), 80f, 18f, 0.8f, G(rod), G("reel_power"), G("line_pe"));
                for (int i = 0; i < 60 * 8 && f.Outcome == FightOutcome.None; i++) f.Tick(1f / 60f, FightBot.Input(f, 0f));
                return f.HP / f.MaxHP;
            }
            Assert.Less(Hp("rod_heavy"), Hp("rod_basic"), "헤비 원투대가 입문용 민물대보다 HP를 더 깎아야 한다");
        }
    }

    public class BiteSelectorTests
    {
        [SetUp]
        public void Seed() => Random.InitState(42);

        [Test]
        public void OnlySpeciesOfTheMapBite()
        {
            var ctx = new BiteContext { MapId = "honghyeon", Habitat = Habitat.Rocky, Bait = BaitKind.Krill, Band = TimeBand.Morning, Weather = Weather.Clear };
            for (int i = 0; i < 300; i++)
            {
                var b = BiteSelector.Pick(ctx);
                Assert.IsTrue(b.Species.LivesIn("honghyeon"), $"{b.Species.Name}는 홍현 앞바다에 살지 않는다");
            }
        }

        [Test]
        public void JunkIsRare()
        {
            // 예전: 무게를 그냥 더해서 맞는 고기가 적은 바다·해외 맵에서는 입질의 절반 이상이 잡동사니였다
            foreach (var map in new[] { "hangang", "santorini", "nile", "cheongjin" })
            foreach (var band in new[] { TimeBand.Morning, TimeBand.Night })
            {
                var ctx = new BiteContext { MapId = map, Habitat = Habitat.Shallow, Bait = BaitKind.Minnow, Band = band, Weather = Weather.Clear };
                int junk = 0;
                for (int i = 0; i < 2000; i++)
                    if (BiteSelector.Pick(ctx).Species.IsJunk) junk++;
                Assert.Less(junk / 2000f, 0.13f, $"{map} {band}: 잡동사니 {junk / 20f:0.0}%");
            }
        }

        [Test]
        public void SchoolOnlyFishNeedsItsSchool()
        {
            var grouper = FishDatabase.Get("giantgrouper");
            var ctx = new BiteContext { MapId = "honghyeon", Habitat = Habitat.Deep, Bait = BaitKind.Squid, Band = TimeBand.Evening, Weather = Weather.Clear };
            Assert.AreEqual(0f, BiteSelector.Weight(grouper, ctx), "어군 밖에서는 그루퍼가 물지 않는다");
        }

        [Test]
        public void MatchingBaitIsPreferred()
        {
            var crucian = FishDatabase.Get("crucian");
            var good = new BiteContext { MapId = "hangang", Habitat = crucian.Habitats[0], Bait = crucian.Baits[0], Band = crucian.Times[0], Weather = Weather.Clear };
            var bad = good;
            bad.Bait = BaitKind.Lure;
            Assert.Greater(BiteSelector.Weight(crucian, good), BiteSelector.Weight(crucian, bad));
        }

        [Test]
        public void LengthStaysInRange()
        {
            foreach (var s in FishDatabase.All.Where(f => !f.IsJunk))
                for (int i = 0; i < 40; i++)
                {
                    float len = BiteSelector.RollLength(s, new BiteContext { BaitPotency = 1.7f, BigFishBonus = 0.1f, Bait = s.Baits[0] });
                    Assert.That(len, Is.InRange(s.MinLen, s.MaxLen), s.Name);
                }
        }
    }

    public class TournamentRulesTests
    {
        [Test]
        public void ScoresThreeMissions()
        {
            var a = new TournamentEntry { Name = "A", Count = 5, Longest = 30f, TotalLength = 120f };   // 많이 1위, 길이 합계 1위
            var b = new TournamentEntry { Name = "B", Count = 2, Longest = 80f, TotalLength = 110f };   // 가장 긴 고기 1위
            var c = new TournamentEntry { Name = "C" };                                                  // 기록 없음
            var list = new List<TournamentEntry> { c, b, a };
            TournamentRules.Score(list);
            Assert.AreEqual(10 + 6 + 10, a.Points);
            Assert.AreEqual(6 + 10 + 6, b.Points);
            Assert.AreEqual(0, c.Points);
            Assert.AreEqual("A", list[0].Name);
            Assert.AreEqual(1, a.Rank);
            Assert.AreEqual(3, c.Rank);
        }

        [Test]
        public void TiesShareMissionRank()
        {
            var a = new TournamentEntry { Name = "A", Count = 3, Longest = 40f, TotalLength = 100f };
            var b = new TournamentEntry { Name = "B", Count = 3, Longest = 40f, TotalLength = 100f };
            TournamentRules.Score(new List<TournamentEntry> { a, b });
            Assert.AreEqual(a.Points, b.Points);
        }

        [Test]
        public void AiSimulationIsDeterministicPerDay()
        {
            var x = TournamentRules.SimulateAi(5, "hangang");
            var y = TournamentRules.SimulateAi(5, "hangang");
            Assert.AreEqual(x.Count, y.Count);
            Assert.IsTrue(x.Zip(y, (p, q) => p.SpeciesId == q.SpeciesId && Mathf.Approximately(p.Length, q.Length)).All(t => t));
            Assert.IsTrue(x.All(c => c.Minute >= 0f && c.Minute < TournamentRules.Duration));
            Assert.IsTrue(x.All(c => FishDatabase.Get(c.SpeciesId).LivesIn("hangang")));
        }

        [Test]
        public void SkilledAnglersCatchMore()
        {
            int best = 0, worst = 0;
            for (int day = 1; day <= 30; day++)
                foreach (var c in TournamentRules.SimulateAi(day, TournamentRules.MapOf(day)))
                {
                    if (c.Angler == 0) best++;
                    if (c.Angler == TournamentRules.Anglers.Length - 1) worst++;
                }
            Assert.Greater(best, worst, "실력 1.25인 한강도사가 0.65인 주말낚시꾼보다 많이 낚아야 한다");
        }

        [Test]
        public void PhasesAndEmblems()
        {
            Assert.AreEqual(TournamentRules.Phase.Before, TournamentRules.PhaseAt(7 * 60f));
            Assert.AreEqual(TournamentRules.Phase.Registration, TournamentRules.PhaseAt(8 * 60f + 30f));
            Assert.AreEqual(TournamentRules.Phase.Running, TournamentRules.PhaseAt(12 * 60f));
            Assert.AreEqual(TournamentRules.Phase.Finished, TournamentRules.PhaseAt(15 * 60f));
            Assert.AreEqual("브론즈", TournamentRules.EmblemName(0));
            Assert.AreEqual("골드", TournamentRules.EmblemName(80));
            Assert.AreEqual("다이아몬드", TournamentRules.EmblemName(1000));
            Assert.AreEqual(30, TournamentRules.ToNextEmblem(0));
            Assert.AreEqual("hangang", TournamentRules.MapOf(1));
            Assert.AreEqual("seocheon", TournamentRules.MapOf(2));
            Assert.AreEqual("honghyeon", TournamentRules.MapOf(3));
            // 지역 대회 (4~6일째): 입장할 수 있는 해안 낚시터만, 못 가면 원래 순서
            Assert.AreEqual(TournamentRules.MapOf(4), TournamentRules.MapOf(4, 5), "Lv.5는 해안 낚시터에 못 가니 원래 순서");
            Assert.AreEqual("yeongheung", TournamentRules.MapOf(4, 9));
            for (int day = 1; day <= 60; day++)
            {
                string id = TournamentRules.MapOf(day, 12);
                Assert.LessOrEqual(FishingAreas.EntryLevel(id), id == "honghyeon" || id == "seocheon" ? 25 : 12, $"{day}일째 {id}");
            }
            Assert.IsTrue(Enumerable.Range(1, 120).Select(d => TournamentRules.MapOf(d, 25)).Distinct().Count() == 12, "만렙이면 12곳 모두 대회가 열린다");
        }
    }

    public class RodUpgradeTests
    {
        [Test]
        public void UpgradeRaisesStatsWithoutTouchingOriginal()
        {
            var rod = GearDatabase.GetGear("rod_carbon");
            var up = RodUpgrade.Apply(rod, 10);
            Assert.AreEqual(rod.Power * 1.3f, up.Power, 1e-4f);
            Assert.AreEqual(rod.CastRange + 4f, up.CastRange, 1e-4f);
            Assert.AreEqual(1.4f, rod.Power, 1e-4f, "원본 장비는 그대로여야 한다");
        }

        [Test]
        public void SuccessRateFallsAndCostRises()
        {
            for (int lv = 1; lv < RodUpgrade.MaxLevel; lv++)
            {
                Assert.LessOrEqual(RodUpgrade.SuccessRate(lv), RodUpgrade.SuccessRate(lv - 1));
                Assert.Greater(RodUpgrade.Cost(GearDatabase.GetGear("rod_basic"), lv), RodUpgrade.Cost(GearDatabase.GetGear("rod_basic"), lv - 1));
            }
            Assert.AreEqual(0f, RodUpgrade.SuccessRate(RodUpgrade.MaxLevel));
            Assert.Greater(RodUpgrade.SuccessRate(5, true), RodUpgrade.SuccessRate(5));
            Assert.IsFalse(RodUpgrade.CanDowngrade(6));
            Assert.IsTrue(RodUpgrade.CanDowngrade(7));
        }
    }

    public class VillageTradeTests
    {
        [Test]
        public void EveryMapHasVillageGearForItsCurrency()
        {
            foreach (var area in FishingAreas.All)
                Assert.IsTrue(VillageTrade.OffersAt(area.MapId).Any(), $"{area.Name} 마을 어부가 내줄 장비가 없다");
            foreach (var g in GearDatabase.Gear.Where(x => x.TradeMap != null))
            {
                Assert.IsTrue(g.NotSold, $"{g.Name}: 마을 장비는 상점에서 팔지 않는다");
                Assert.Greater(g.TradeCost, 0, g.Name);
                Assert.GreaterOrEqual(g.Level, FishingAreas.EntryLevel(g.TradeMap), $"{g.Name}: 입장 레벨보다 낮은 장비");
            }
            Assert.AreEqual(VillageTrade.NapalId, VillageTrade.CurrencyFor("hangang"));
            Assert.AreEqual(VillageTrade.NapalId, VillageTrade.CurrencyFor("geomun"));
            Assert.AreEqual(VillageTrade.GoldGegeId, VillageTrade.CurrencyFor("halong"));
            Assert.AreEqual(VillageTrade.GoldGegeId, VillageTrade.CurrencyFor("nile"));
        }

        /// <summary>원작: 상점에서 못 구하고 NPC에게 구하는 장비는 같은 레벨 상점 장비보다 능력치가 좋다.</summary>
        [Test]
        public void VillageGearBeatsShopGearOfSameLevel()
        {
            foreach (var g in GearDatabase.Gear.Where(x => x.TradeMap != null))
            {
                foreach (var shop in GearDatabase.Gear.Where(x => x.Slot == g.Slot && !x.NotSold && x.Level <= g.Level))
                {
                    switch (g.Slot)
                    {
                        case GearSlot.Rod: Assert.Greater(g.Power, shop.Power, $"{g.Name} vs {shop.Name}"); break;
                        case GearSlot.Reel:
                            Assert.Greater(g.ReelSpeed, shop.ReelSpeed, $"{g.Name} vs {shop.Name}");
                            Assert.GreaterOrEqual(g.Capacity, shop.Capacity, $"{g.Name} vs {shop.Name}");
                            break;
                        case GearSlot.Line: Assert.Greater(g.Strength, shop.Strength, $"{g.Name} vs {shop.Name}"); break;
                    }
                }
            }
        }
    }

    public class PlayerProfileTests
    {
        [Test]
        public void ExpLevelsUp()
        {
            var p = new PlayerProfile(new SaveData());
            int levels = 0;
            p.LeveledUp += _ => levels++;
            p.AddExp(PlayerProfile.ExpToNext(1) + PlayerProfile.ExpToNext(2));
            Assert.AreEqual(3, p.Data.level);
            Assert.AreEqual(2, levels);
        }

        [Test]
        public void ItemsAddAndConsume()
        {
            var p = new PlayerProfile(new SaveData());
            p.AddItem("mat_scale", 5);
            Assert.IsTrue(p.ConsumeItem("mat_scale", 3));
            Assert.AreEqual(2, p.Count("mat_scale"));
            Assert.IsFalse(p.ConsumeItem("mat_scale", 3));
        }

        [Test]
        public void BiggerFishSellsForMore()
        {
            var small = new CaughtFish { speciesId = "carp", length = 30f };
            var big = new CaughtFish { speciesId = "carp", length = 90f };
            Assert.Greater(PlayerProfile.SellPrice(big), PlayerProfile.SellPrice(small));
            Assert.AreEqual(0, PlayerProfile.SellPrice(new CaughtFish { speciesId = "can", length = 12f }));
        }

        [Test]
        public void RecordDexTracksBest()
        {
            var p = new PlayerProfile(new SaveData());
            var carp = FishDatabase.Get("carp");
            Assert.IsTrue(p.RecordDex(carp, 50f, 2000f, 1));
            Assert.IsFalse(p.RecordDex(carp, 40f, 1500f, 2));
            Assert.IsTrue(p.RecordDex(carp, 70f, 5000f, 3));
            Assert.AreEqual(3, p.Dex("carp").count);
            Assert.AreEqual(70f, p.Dex("carp").maxLength);
        }
    }

    public class SeasonTests
    {
        [Test]
        public void CalendarAndSeasons()
        {
            Assert.AreEqual(4, Seasons.MonthOf(1));
            Assert.AreEqual(Season.Spring, Seasons.SeasonOf(1));
            Assert.AreEqual(5, Seasons.MonthOf(8));
            Assert.AreEqual(Season.Summer, Seasons.SeasonOf(Seasons.FirstDayOf(Season.Summer)));
            int winter = Seasons.FirstDayOf(Season.Winter);
            Assert.AreEqual(12, Seasons.MonthOf(winter));
            Assert.AreEqual(Season.Autumn, Seasons.SeasonOf(winter - 1));
            Assert.AreEqual(Season.Spring, Seasons.SeasonOf(Seasons.FirstDayOf(Season.Spring, winter)), "겨울 다음은 다시 봄");
        }

        [Test]
        public void DeepSeaLegendsOnlyAtFarSea()
        {
            var dotdom = FishDatabase.Get("dotdom");
            var ctx = new BiteContext { MapId = "honghyeon", Habitat = Habitat.Deep, Bait = BaitKind.Squid, Band = TimeBand.Night, Weather = Weather.Clear,
                UseSeason = true, Season = Season.Spring, FarSea = true };
            Assert.Greater(BiteSelector.Weight(dotdom, ctx), 0f);
            ctx.FarSea = false;
            Assert.AreEqual(0f, BiteSelector.Weight(dotdom, ctx), "먼바다가 아니면 돗돔은 안 문다");
            ctx.FarSea = true;
            ctx.MapId = "seocheon";
            Assert.AreEqual(0f, BiteSelector.Weight(dotdom, ctx), "서해에는 없다");
            Assert.AreEqual(0f, BiteSelector.Weight(FishDatabase.Get("dageumbari"), new BiteContext { MapId = "honghyeon", Habitat = Habitat.Rocky,
                Bait = BaitKind.Squid, Band = TimeBand.Night, Weather = Weather.Clear }));
        }

        [Test]
        public void SmeltOnlyThroughIceInWinter()
        {
            var smelt = FishDatabase.Get("smelt");
            var ctx = new BiteContext { MapId = "hangang", Habitat = Habitat.Shallow, Bait = BaitKind.Worm, Band = TimeBand.Day, Weather = Weather.Clear,
                UseSeason = true, Season = Season.Winter, IceFishing = true };
            Assert.Greater(BiteSelector.Weight(smelt, ctx), 0f);
            ctx.IceFishing = false;
            Assert.AreEqual(0f, BiteSelector.Weight(smelt, ctx), "얼음 구멍이 아니면 빙어는 안 문다");
            ctx.IceFishing = true;
            ctx.Season = Season.Summer;
            Assert.AreEqual(0f, BiteSelector.Weight(smelt, ctx), "여름엔 빙어가 없다");
        }

        [Test]
        public void InSeasonBitesMore()
        {
            var shad = FishDatabase.Get("shad");
            Assert.Greater(Seasons.BiteFactor(shad, Season.Autumn), Seasons.BiteFactor(shad, Season.Spring), "전어는 가을");
            Assert.AreEqual(1f, Seasons.BiteFactor(FishDatabase.Get("bluegill"), Season.Winter), "제철이 없는 어종은 그대로");
        }
    }

    public class SettingsTests
    {
        [Test]
        public void RebindSwapsConflictingKeys()
        {
            Settings.ResetKeys();
            Settings.Rebind(KeyAction.Quest, KeyCode.P);   // P는 도감 키였다
            Assert.AreEqual(KeyCode.P, Settings.Key(KeyAction.Quest));
            Assert.AreEqual(KeyCode.L, Settings.Key(KeyAction.Dex), "도감은 퀘스트의 옛 키(L)를 받는다");
            Settings.ResetKeys();
            Assert.AreEqual(KeyCode.L, Settings.Key(KeyAction.Quest));
            Assert.IsTrue(Settings.Reserved(KeyCode.Escape));
            Assert.IsTrue(Settings.Reserved(KeyCode.Alpha3));
            Assert.AreEqual("Shift", Settings.KeyName(KeyCode.LeftShift));
        }
    }

    /// <summary>퀘스트: 예전 저장 이전, NPC 수락 방식 (13 문서).</summary>
    public class QuestTests
    {
        [Test]
        public void StoryKeepsLegacyQuestsInOrder()
        {
            int last = -1;
            for (int i = 0; i < QuestDatabase.LegacyStoryCount; i++)
            {
                int at = QuestDatabase.StoryIndex($"st{i:00}");
                Assert.Greater(at, last, $"st{i:00}가 예전 순서대로 있어야 한다");
                last = at;
            }
            Assert.AreEqual("남해로", QuestDatabase.Story[QuestDatabase.MigrateIndex(8)].Title);
            Assert.AreEqual(QuestDatabase.Story.Count, QuestDatabase.MigrateIndex(45));
            foreach (var q in QuestDatabase.Story)
            {
                Assert.IsNotNull(QuestNpcs.Get(q.Giver), $"{q.Title}: 주는 NPC");
                Assert.IsNotNull(QuestNpcs.Get(q.Reporter), $"{q.Title}: 보고자");
            }
        }

        [Test]
        public void OldSaveKeepsCurrentStoryAccepted()
        {
            var d = new SaveData();
            d.quest.index = 8;      // 예전 목록의 "남해로"
            d.quest.progress = 2;
            var qs = new QuestSystem(new PlayerProfile(d));
            Assert.AreEqual(1, d.quest.ver);
            Assert.AreEqual("남해로", qs.Current.Title);
            Assert.IsTrue(qs.IsActive(qs.Current));
            Assert.AreEqual(2, qs.ProgressOf(qs.Current));
        }

        [Test]
        public void NewSaveAcceptsFirstStoryAtLobbyDesk()
        {
            var d = new SaveData();
            var qs = new QuestSystem(new PlayerProfile(d));
            var first = qs.Current;
            Assert.IsFalse(qs.IsActive(first), "새 저장은 안내원에게 수락해야 한다");
            Assert.AreEqual(QuestSystem.Mark.AvailableStory, qs.MarkFor(QuestNpcs.Desk));
            Assert.IsTrue(qs.Accept(first));
            Assert.IsTrue(qs.IsActive(first));
            Assert.IsFalse(qs.Accept(first), "두 번 수락하지 않는다");
            qs.DebugFill(first);
            Assert.IsTrue(qs.IsReady(first));
            Assert.AreEqual(QuestSystem.Mark.Ready, qs.MarkFor(first.Reporter));
        }
    }
}
