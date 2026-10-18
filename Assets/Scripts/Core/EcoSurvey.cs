using System;
using System.Collections.Generic;
using System.Linq;
using Fishing.Data;
using Fishing.World;

namespace Fishing.Core
{
    /// <summary>
    /// GOLD 보상 업적 하나 (원작 기록(I) 책의 업적: 완성하면 날짜가 찍히고, 눌러서 보상을 받는다).
    /// 생태조사("동갈자돔을 홍현 앞바다에서 납품하기"), 납품 마릿수, 방생 마릿수.
    /// </summary>
    public sealed class GoldAch
    {
        public string Id;
        public string Name;
        /// <summary>업적 분류 이름 (기록 책 왼쪽 목록): 납품 · 방생 · 생태조사.</summary>
        public string Category;
        /// <summary>생태조사: 낚시터 id와 어종 (아니면 null).</summary>
        public string MapId;
        public FishSpecies Species;
        public int Target = 1;
        public int Gold;
        public Func<PlayerProfile, int> Value;
    }

    /// <summary>
    /// 원작 생태조사·납품 업적 (2013 홍현 영상 1:36~2:36).
    ///  - 낚시터마다 사는 어종 하나하나가 "○○을 ○○에서 납품하기" 업적 (1마리). 묶음 이름은 "홍현 생태조사 (16/23)".
    ///  - "물고기 N마리를 납품하기" 100 / 400 / 1600, "물고기 N마리를 방생하기" 10 / 50 / 200.
    /// 완성하면 화면 가운데 "[업적] … 1/1 [완 료]", 보상(GOLD)은 퀘스트 창 업적 탭에서 눌러 받는다.
    /// 업적 확인은 QuestSystem.CheckAchievements(소라고동 업적)와 따로, 납품·방생할 때 여기서 한다.
    /// </summary>
    public static class EcoSurvey
    {
        public const string CatDelivery = "납품", CatRelease = "방생", CatEco = "생태조사";

        public static readonly int[] DeliverTiers = { 100, 400, 1600 };
        static readonly int[] DeliverGold = { 1000, 3000, 10000 };
        public static readonly int[] ReleaseTiers = { 10, 50, 200 };
        static readonly int[] ReleaseGold = { 500, 1500, 5000 };
        /// <summary>생태조사 보상: 등급이 높을수록 많이 (원작 250 · 420 GOLD).</summary>
        static readonly int[] EcoGold = { 100, 150, 250, 420, 700, 1200 };

        static List<GoldAch> _all;

        /// <summary>모든 GOLD 보상 업적 (처음 부를 때 낚시터·어종 목록으로 만든다).</summary>
        public static List<GoldAch> All => _all ??= Build();

        static List<GoldAch> Build()
        {
            var list = new List<GoldAch>();
            for (int i = 0; i < DeliverTiers.Length; i++)
            {
                int n = DeliverTiers[i];
                list.Add(new GoldAch { Id = "deliver" + n, Name = $"물고기 {n}마리를 납품하기", Category = CatDelivery, Target = n, Gold = DeliverGold[i],
                    Value = p => p.Data.deliveredTotal });
            }
            for (int i = 0; i < ReleaseTiers.Length; i++)
            {
                int n = ReleaseTiers[i];
                list.Add(new GoldAch { Id = "release" + n, Name = $"물고기 {n}마리를 방생하기", Category = CatRelease, Target = n, Gold = ReleaseGold[i],
                    Value = p => p.Data.releasedTotal });
            }
            foreach (var map in SurveyMaps)
            foreach (var s in FishDatabase.DexOf(map.Id))
            {
                string mapId = map.Id, spId = s.Id;
                list.Add(new GoldAch
                {
                    Id = EcoId(mapId, spId), Name = EntryName(s, map), Category = CatEco, MapId = mapId, Species = s,
                    Target = 1, Gold = EcoGold[UnityEngine.Mathf.Clamp(s.Rarity, 0, EcoGold.Length - 1)],
                    Value = p => Delivered(p, mapId, spId),
                });
            }
            return list;
        }

        /// <summary>생태조사가 있는 낚시터 (도감 어종이 있는 곳, 도감 탭 순서).</summary>
        public static IEnumerable<FieldMapDef> SurveyMaps => Maps.All.Where(m => FishDatabase.DexOf(m.Id).Any());

        public static string EcoId(string mapId, string speciesId) => $"eco:{mapId}:{speciesId}";

        /// <summary>"동갈자돔을 홍현 앞바다에서 납품하기".</summary>
        public static string EntryName(FishSpecies s, FieldMapDef map) =>
            $"{s.Name}{Fishing.UI.GameUI.Josa(s.Name, "을", "를")} {map.ShortName}에서 납품하기";

        /// <summary>"홍현 생태조사" (원작 묶음 이름은 '앞바다'를 뗀 짧은 이름).</summary>
        public static string GroupName(FieldMapDef map) => $"{map.ShortName.Replace(" 앞바다", "")} 생태조사";

        public static IEnumerable<GoldAch> OfMap(string mapId) => All.Where(a => a.MapId == mapId);
        public static IEnumerable<GoldAch> OfCategory(string cat) => All.Where(a => a.Category == cat);

        public static GoldAch Get(string id) => All.FirstOrDefault(a => a.Id == id);

        public static int Delivered(PlayerProfile p, string mapId, string speciesId) =>
            p.Data.deliveries.FirstOrDefault(r => r.mapId == mapId && r.speciesId == speciesId)?.count ?? 0;

        static AchClaim Record(PlayerProfile p, string id) => p.Data.achClaims.FirstOrDefault(c => c.id == id);

        public static bool IsDone(PlayerProfile p, GoldAch a) => Record(p, a.Id) != null;
        public static bool IsClaimed(PlayerProfile p, GoldAch a) => Record(p, a.Id)?.claimed == true;
        public static string DoneAt(PlayerProfile p, GoldAch a) => Record(p, a.Id)?.doneAt;
        /// <summary>완성했지만 아직 보상을 받지 않은 업적 수.</summary>
        public static int Unclaimed(PlayerProfile p) => p.Data.achClaims.Count(c => !c.claimed);

        /// <summary>한 낚시터 생태조사 진행도 (완성, 전체).</summary>
        public static (int done, int total) MapProgress(PlayerProfile p, string mapId)
        {
            int done = 0, total = 0;
            foreach (var a in OfMap(mapId))
            {
                total++;
                if (IsDone(p, a)) done++;
            }
            return (done, total);
        }

        /// <summary>
        /// 이 고기를 어느 낚시터 납품으로 칠지: 낚은 낚시터 → 지금 낚시터(거기 사는 어종이면) → 그 어종이 사는 첫 낚시터.
        /// </summary>
        public static string CreditMap(CaughtFish f)
        {
            var s = FishDatabase.Get(f.speciesId);
            if (s == null) return FieldMap.Id;
            if (!string.IsNullOrEmpty(f.mapId) && s.LivesIn(f.mapId)) return f.mapId;
            if (s.Maps == null || s.LivesIn(FieldMap.Id)) return FieldMap.Id;
            return s.Maps.Length > 0 ? s.Maps[0] : FieldMap.Id;
        }

        /// <summary>납품한 고기를 기록하고, 완성된 업적을 알린다 (Delivery.Deliver가 부른다).</summary>
        public static void OnDelivered(IList<CaughtFish> fish)
        {
            var p = Game.Profile;
            if (p == null || fish.Count == 0) return;
            foreach (var f in fish)
            {
                string map = CreditMap(f);
                var rec = p.Data.deliveries.FirstOrDefault(r => r.mapId == map && r.speciesId == f.speciesId);
                if (rec == null) p.Data.deliveries.Add(rec = new DeliveryRecord { mapId = map, speciesId = f.speciesId });
                rec.count++;
            }
            p.Data.deliveredTotal += fish.Count;

            // 생태조사: 새로 완성한 것 (한 번에 여러 개면 3개까지 알리고 나머지는 한 줄로)
            var fresh = Complete(p, CatEco);
            for (int i = 0; i < fresh.Count; i++)
            {
                var a = fresh[i];
                if (i < 3) Game.Notice($"[업적] {a.Name} 1/1 [완 료]");
                Game.SysLog($"[{GroupName(Maps.Get(a.MapId))}] 업적 [{a.Name}] 도전과제 완성!", "#e8a040");
            }
            if (fresh.Count > 3) Game.Notice($"[업적] 생태조사 {fresh.Count - 3}개 더 완성");
            CounterNotice(p, CatDelivery, p.Data.deliveredTotal, "물고기 {0}마리를 납품 하기");
        }

        /// <summary>방생할 때 (FishingController 결과 화면의 방생).</summary>
        public static void OnRelease()
        {
            var p = Game.Profile;
            if (p == null) return;
            CounterNotice(p, CatRelease, p.Data.releasedTotal, "물고기 {0}마리를 방생 하기");
        }

        /// <summary>마릿수 업적: 이번에 넘은 단계는 [완 료], 아니면 다음 단계 진행도 (원작 "[업적] 물고기 1600마리를 납품 하기 831/1600").</summary>
        static void CounterNotice(PlayerProfile p, string cat, int value, string format)
        {
            var fresh = Complete(p, cat);
            foreach (var a in fresh)
            {
                Game.Notice($"[업적] {string.Format(format, a.Target)} {a.Target}/{a.Target} [완 료]");
                Game.SysLog($"[{cat}] 업적 [{a.Name}] 도전과제 완성!", "#e8a040");
            }
            var next = OfCategory(cat).Where(a => !IsDone(p, a)).OrderBy(a => a.Target).FirstOrDefault();
            if (next != null && fresh.Count == 0) Game.Notice($"[업적] {string.Format(format, next.Target)} {value}/{next.Target}");
        }

        /// <summary>조건을 채웠는데 기록이 없는 업적에 완성 날짜를 찍는다. 새로 완성한 것을 돌려준다.</summary>
        static List<GoldAch> Complete(PlayerProfile p, string cat)
        {
            var fresh = new List<GoldAch>();
            foreach (var a in OfCategory(cat))
            {
                if (IsDone(p, a) || a.Value(p) < a.Target) continue;
                p.Data.achClaims.Add(new AchClaim { id = a.Id, doneAt = DateTime.Now.ToString("yyyy/M/d HH:mm"), claimed = false });
                fresh.Add(a);
            }
            if (fresh.Count > 0 && Game.Sound != null) Game.Sound.LevelUp();
            return fresh;
        }

        /// <summary>보상을 받는다 (퀘스트 창 업적 탭에서 누르면). 받은 GOLD, 못 받으면 0.</summary>
        public static int Claim(GoldAch a)
        {
            var p = Game.Profile;
            var rec = Record(p, a.Id);
            if (rec == null || rec.claimed) return 0;
            rec.claimed = true;
            p.AddGold(a.Gold);
            if (Game.Sound != null) Game.Sound.Coin();
            Game.SysLog($"{a.Gold} GOLD 를 획득했습니다.");
            return a.Gold;
        }
    }
}
