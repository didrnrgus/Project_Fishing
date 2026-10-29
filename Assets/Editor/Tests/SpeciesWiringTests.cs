using System.Collections.Generic;
using System.Linq;
using Fishing.Core;
using Fishing.Data;
using NUnit.Framework;

namespace Fishing.EditorTools.Tests
{
    /// <summary>
    /// 어종이 게임 시스템에 빠짐없이 이어졌는지 (2026-10-30, 어종 224종): 도감 낚시터 · 찌/파이팅 패턴 · 퀘스트 · 해체/생미끼 규칙 · 도감 업적.
    /// 어종 표(FishDatabase*.cs)에 새 어종을 넣으면 이 테스트가 빠진 곳을 알려 준다.
    /// </summary>
    public class SpeciesWiringTests
    {
        static IEnumerable<QuestDef> AllQuests => QuestDatabase.Story.Concat(QuestDatabase.Side);

        static readonly QuestGoal[] SpeciesGoals = { QuestGoal.CatchSpecies, QuestGoal.CatchSpeciesInBand, QuestGoal.CatchMinLength, QuestGoal.DeliverFish };

        [Test]
        public void EverySpeciesLivesOnAnOpenMapAndHasPatterns()
        {
            var open = new HashSet<string>(FishingAreas.All.Where(a => a.Open).Select(a => a.MapId));
            foreach (var s in FishDatabase.DexSpecies)
            {
                Assert.IsNotNull(s.Maps, $"{s.Name}: 도감 어종은 사는 낚시터가 있어야 한다");
                Assert.IsNotEmpty(s.Maps, s.Name);
                foreach (var m in s.Maps)
                {
                    Assert.IsTrue(open.Contains(m), $"{s.Name}: 낚시터 목록에 없는 맵 {m}");
                    Assert.IsNotNull(Fishing.World.Maps.Get(m), $"{s.Name}: 맵 정의가 없다 {m}");
                }
                Assert.IsTrue(FishPatterns.Listed(s.Id), $"{s.Name}({s.Id}): FishPatterns에 찌·파이팅 패턴을 넣어야 한다");
            }
        }

        [Test]
        public void EveryMapHasDexQuestAndSpeciesQuests()
        {
            foreach (var area in FishingAreas.All.Where(a => a.Open))
            {
                var dex = QuestDatabase.Side.FirstOrDefault(q => q.Goal == QuestGoal.MapDex && q.MapId == area.MapId);
                Assert.IsNotNull(dex, $"{area.Name}: 낚시터 도감 퀘스트가 없다");
                int eligible = FishDatabase.DexOf(area.MapId).Count(s => !s.Boss && !s.SchoolOnly && !s.FarSeaOnly && s.Rarity <= 4);
                Assert.LessOrEqual(dex.Count, eligible, $"{area.Name}: 도감 퀘스트 목표가 흔한 어종 수보다 많다");
                int speciesQuests = AllQuests.Count(q => q.MapId == area.MapId && q.SpeciesId != null && SpeciesGoals.Contains(q.Goal));
                Assert.GreaterOrEqual(speciesQuests, 2, $"{area.Name}: 어종 퀘스트가 너무 적다");
            }
            // 대기실 문박사의 도감 퀘스트 (전체 도감 수)
            var steps = QuestDatabase.Side.Where(q => q.Goal == QuestGoal.DistinctSpecies && q.Giver == QuestNpcs.Doctor).ToList();
            Assert.GreaterOrEqual(steps.Count, 3);
            Assert.IsTrue(steps.All(q => q.Count <= FishDatabase.DexSpecies.Count()), "도감 퀘스트 목표가 어종 수보다 많다");
        }

        [Test]
        public void QuestsPointAtRealSpeciesWhereTheyLive()
        {
            var ids = new HashSet<string>();
            foreach (var q in AllQuests)
            {
                Assert.IsTrue(ids.Add(q.Id), $"퀘스트 id가 겹친다: {q.Id}");
                Assert.IsNotNull(QuestNpcs.Get(q.Giver), $"{q.Title}: 주는 NPC");
                Assert.IsNotNull(QuestNpcs.Get(q.Reporter), $"{q.Title}: 보고자");
                Assert.Greater(q.Count, 0, q.Title);
                if (q.SpeciesId == null) continue;
                var s = FishDatabase.Get(q.SpeciesId);
                Assert.IsNotNull(s, $"{q.Title}: 없는 어종 {q.SpeciesId}");
                if (q.MapId != null) Assert.IsTrue(s.LivesIn(q.MapId), $"{q.Title}: {s.Name}은(는) {q.MapId}에 살지 않는다");
                if (q.Goal == QuestGoal.CatchMinLength)
                    Assert.IsTrue(q.MinLen > s.MinLen && q.MinLen < s.MaxLen, $"{q.Title}: {q.MinLen}cm는 {s.Name} 길이 범위({s.MinLen}~{s.MaxLen}) 안이어야 한다");
                if (q.Goal == QuestGoal.CatchSpeciesInBand)
                    Assert.IsTrue(s.Times.Contains(q.Band), $"{q.Title}: {s.Name}은(는) {q.Band}에 물지 않는다");
            }
        }

        [Test]
        public void EverySpeciesTurnsIntoBait()
        {
            foreach (var s in FishDatabase.DexSpecies)
            {
                var bait = GearDatabase.DissectBaitFor(s);
                Assert.IsNotNull(bait, $"{s.Name}: 해체 미끼가 없다");
                Assert.AreEqual(s.IsCephalopod ? BaitKind.Squid : BaitKind.CutBait, bait.Kind, s.Name);
                Assert.Greater(PlayerProfile.DissectBaitCount(s, s.MaxLen), 1, s.Name);
                Assert.AreEqual(s.IsCephalopod, PlayerProfile.DissectScales(s, s.MaxLen) == 0, $"{s.Name}: 비늘은 물고기만");
                var small = new CaughtFish { speciesId = s.Id, length = 12f };
                Assert.AreEqual(!s.IsCephalopod, PlayerProfile.CanMakeBait(small), $"{s.Name}: 작은 물고기만 생미끼가 된다");
            }
            Assert.IsTrue(FishDatabase.Get("squid").IsCephalopod);
            Assert.IsTrue(FishDatabase.Get("giantoctopus").IsCephalopod);
            Assert.IsFalse(FishDatabase.Get("skate").IsCephalopod);
            Assert.IsNull(GearDatabase.DissectBaitFor(FishDatabase.Get("can")), "잡동사니는 해체하지 않는다");
        }

        [Test]
        public void MapDexQuestCountsSpeciesRecordedAnywhere()
        {
            var d = new SaveData { level = 5 };
            var p = new PlayerProfile(d);
            var qs = new QuestSystem(p);
            var q = QuestDatabase.Get("dex_tancheon");
            Assert.IsNotNull(q);
            Assert.IsTrue(qs.AvailableAt(q.Giver).Contains(q), "탄천 연락관이 도감 퀘스트를 준다");
            // 한강에서 낚은 피라미도 탄천 도감으로 친다 (같은 어종)
            foreach (var s in FishDatabase.DexOf("tancheon").Take(q.Count - 1)) p.RecordDex(s, s.MinLen, s.WeightGrams(s.MinLen), 1);
            Assert.IsTrue(qs.Accept(q));
            Assert.AreEqual(q.Count - 1, qs.ProgressOf(q));
            Assert.IsFalse(qs.IsReady(q));
            p.RecordDex(FishDatabase.DexOf("tancheon").Skip(q.Count - 1).First(), 10f, 10f, 1);
            qs.OnDexChanged();
            Assert.IsTrue(qs.IsReady(q), "통발 등으로 도감이 늘어도 목표를 채운다");
        }

        [Test]
        public void RegionDexAchievementsCoverEveryHub()
        {
            var p = new PlayerProfile(new SaveData());
            foreach (var id in new[] { "dexhan", "dextancheon", "dexsea", "dexwest", "dexeast", "dexkorea", "dexsoutheast", "dexmed", "dexnile", "dexworld" })
            {
                var a = AchievementDatabase.Get(id);
                Assert.IsNotNull(a, id);
                var (got, total) = a.Progress(p);
                Assert.AreEqual(0, got, id);
                Assert.Greater(total, 0, $"{id}: 어종이 없다");
                Assert.IsFalse(a.Done(p), id);
            }
            Assert.AreEqual(FishDatabase.DexSpecies.Count(), AchievementDatabase.Get("dexworld").Progress(p).Item2);
            // 모든 낚시터가 거점 도감 업적(국내·동남아·지중해·나일강) 어느 하나에 들어간다
            foreach (var area in FishingAreas.All.Where(a => a.Open))
                Assert.Contains(area.Hub, new[] { "korea", "sea", "med", "africa" }, $"{area.Name}: 도감 업적이 없는 거점");
        }
    }
}
