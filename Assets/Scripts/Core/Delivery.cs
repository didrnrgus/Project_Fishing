using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 납품 (원작 납품소 DELIVERY, 2013 홍현 영상 1:36~2:04): 살림망 고기를 골라 조합에 넘기고 GOLD를 받는다.
    /// 값은 상점 판매가(<see cref="PlayerProfile.SellPrice"/>) 그대로.
    ///  - 납품 게이지: 납품한 금액만큼 찬다 (게이지 한 통 = <see cref="GaugeCapacity"/> GOLD).
    ///    가득 차면 다음 납품 한 번이 두 배(X2 납품게이지)이고, 그 납품 뒤 게이지는 0으로 돌아간다.
    ///  - 납품한 고기는 생태조사·납품 업적에 기록된다 (<see cref="EcoSurvey"/>).
    /// </summary>
    public static class Delivery
    {
        /// <summary>게이지 한 통에 드는 납품 금액. 레벨이 오르면 고기값도 오르니 함께 커진다.</summary>
        public static int GaugeCapacity => 20000 + (Game.Profile != null ? Game.Profile.Data.level : 1) * 1500;

        public static float Gauge => Game.Profile != null ? Mathf.Clamp01(Game.Profile.Data.deliveryGauge) : 0f;
        public static bool GaugeFull => Gauge >= 0.999f;

        /// <summary>진행 중인 퀘스트가 이 어종을 요구하는지 ("퀘스트어종 비활성화"로 잠근다).</summary>
        public static bool IsQuestSpecies(string speciesId) => Game.Quests != null && Game.Quests.IsQuestSpecies(speciesId);

        /// <summary>이 고기들을 납품하면 받을 기본 금액 (X2 전).</summary>
        public static int Price(IEnumerable<CaughtFish> fish)
        {
            int total = 0;
            foreach (var f in fish) total += PlayerProfile.SellPrice(f);
            return total;
        }

        /// <summary>
        /// 고른 고기를 납품한다. useX2이고 게이지가 가득이면 두 배를 받고 게이지를 비운다.
        /// 받은 GOLD를 돌려준다 (고른 것이 없으면 0).
        /// </summary>
        public static int Deliver(ICollection<CaughtFish> picked, bool useX2)
        {
            var p = Game.Profile;
            if (p == null || picked == null || picked.Count == 0) return 0;
            var fish = new List<CaughtFish>();
            foreach (var f in picked)
                if (p.Data.keepNet.Remove(f)) fish.Add(f);
            if (fish.Count == 0) return 0;

            int basePrice = Price(fish);
            bool x2 = useX2 && GaugeFull;
            int pay = x2 ? basePrice * 2 : basePrice;
            if (x2) p.Data.deliveryGauge = 0f;
            else p.Data.deliveryGauge = Mathf.Clamp01(p.Data.deliveryGauge + basePrice / (float)GaugeCapacity);

            EcoSurvey.OnDelivered(fish);
            // 퀘스트 "○○ 납품하기" 목표 (어종마다, 어느 낚시터 납품인지와 함께)
            if (Game.Quests != null)
                foreach (var f in fish) Game.Quests.OnDelivered(f.speciesId, EcoSurvey.CreditMap(f));
            p.AddGold(pay);   // NotifyChanged → 열린 창 갱신
            if (Game.Sound != null) Game.Sound.Coin();
            if (x2) Game.Notice($"X2 납품게이지! 물고기 {fish.Count}마리를 두 배 값에 납품했습니다.");
            Game.SysLog($"{pay} GOLD 를 획득했습니다.");
            p.Log($"물고기 {fish.Count}마리 납품 — {pay:N0} GOLD" + (x2 ? " (X2)" : ""));
            Game.Save();
            return pay;
        }
    }
}
