using System;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 소라고동 교환소 (조합 연락관). 도감·대물 도전·일일 의뢰·업적으로 모은 소라고동을 귀한 물건으로 바꾼다.
    /// </summary>
    public class ExchangePanel : UIPanel
    {
        struct Offer
        {
            public string Name, Desc, Icon;
            public int Cost;
            /// <summary>값을 치르는 아이템 (null이면 소라고동). 대물 코인 교환품은 <see cref="ItemDatabase.BigCoinId"/>.</summary>
            public string Coin;
            public string CoinName => Coin == ItemDatabase.BigCoinId ? "대물 코인" : "소라고동";
            public Func<bool> Can;
            public Action Give;
        }

        Offer[] _offers;

        public ExchangePanel(Transform canvas)
        {
            BuildFrame(canvas, "ExchangePanel", "소라고동 교환소", new Vector2(900, 860));
            _offers = new[]
            {
                new Offer { Name = "별모래 1개", Icon = RodUpgrade.StarSandId, Desc = "강화 성공 확률 +15%", Cost = 5, Give = () => P.AddItem(RodUpgrade.StarSandId, 1) },
                new Offer { Name = "반짝이는 비늘 10개", Icon = RodUpgrade.ScaleId, Desc = "강화 재료", Cost = 3, Give = () => P.AddItem(RodUpgrade.ScaleId, 10) },
                new Offer { Name = "촉촉한이리 5개", Icon = "bait_cut4", Desc = "Lv.12 해체 미끼 (입질 +50%)", Cost = 4, Give = () => P.AddItem("bait_cut4", 5) },
                new Offer { Name = "살림망 2칸 확장", Desc = "최대 3번까지", Cost = 15, Can = () => P.Data.keepNetBonus < 3 + 6,
                    Give = () => P.Data.keepNetBonus += 2 },
                new Offer { Name = "소라 무늬 셔츠", Desc = "의상 (상의) · 대물 +4% · 체력 소모 -5%", Cost = 12, Can = () => !P.Data.ownedCostumes.Contains("top_conch"),
                    Give = () => P.GiveCostume("top_conch") },
                new Offer { Name = "소라고동 모자", Desc = "의상 (모자) · 체력 +6 · 대물 +4%", Cost = 20, Can = () => !P.Data.ownedCostumes.Contains("hat_conch"),
                    Give = () => P.GiveCostume("hat_conch") },
                // 원작 이벤트: 괴물 어종 코인 → 풍림화산 채비
                new Offer { Name = "풍림화산 낚싯대", Icon = "rod_pungrim", Desc = "Lv.12 · 힘 2.3 · 경조 · 비거리 31m — 검은 옻칠에 붉은 띠", Cost = 12, Coin = ItemDatabase.BigCoinId,
                    Can = () => !P.Owns("rod_pungrim"), Give = () => P.Data.ownedGear.Add("rod_pungrim") },
                new Offer { Name = "풍림화산 릴", Icon = "reel_pungrim", Desc = "Lv.12 · 릴 속도 6.0 · 권사량 160m", Cost = 10, Coin = ItemDatabase.BigCoinId,
                    Can = () => !P.Owns("reel_pungrim"), Give = () => P.Data.ownedGear.Add("reel_pungrim") },
                new Offer { Name = "별모래 3개", Icon = RodUpgrade.StarSandId, Desc = "강화 성공 확률 +15% (하나당)", Cost = 2, Coin = ItemDatabase.BigCoinId,
                    Give = () => P.AddItem(RodUpgrade.StarSandId, 3) },
            };
        }

        static PlayerProfile P => Game.Profile;

        public override void Refresh()
        {
            int conch = P.Count(AchievementDatabase.ConchId);
            int coins = P.Count(ItemDatabase.BigCoinId);
            Title.text = $"소라고동 교환소   <size=20><color=#ffd060>소라고동 {conch}개</color>  ·  <color=#ffb040>대물 코인 {coins}개</color></size>";
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);
            Cell(rows.Next(28), "소라고동: 도감 등록 1 · 일일 의뢰 1 · 대물 도전 3 · 업적 1~10 · 빙고 한 줄 2", 4, 820, 16, UIFactory.TextDim);
            Cell(rows.Next(28), "대물 코인: 괴물잉어·괴물가물치(한강 비 오는 밤) 3 · 폭군삼치(남해 새벽) 4 · 황새치(먼바다 밤) 5", 4, 820, 16, UIFactory.TextDim);
            bool coinHeader = false;
            foreach (var o in _offers)
            {
                if (o.Coin != null && !coinHeader)
                {
                    coinHeader = true;
                    UIFactory.SectionTitle(rows.Next(32, 4), "대물 코인 교환 (풍림화산 채비)", 4, 18);
                }
                var row = rows.Next(58, 6);
                UIFactory.RowBg(row);
                bool can = o.Can == null || o.Can();
                float ix = ItemIcons.Place(row, o.Icon, 8, 48, !can);
                Cell(row, $"<b>{o.Name}</b>\n<size=14><color=#a0b0c0>{o.Desc}</color></size>", 12 + ix, 560 - ix, 19, can ? UIFactory.TextMain : UIFactory.TextDim);
                var offer = o;
                int have = o.Coin != null ? coins : conch;
                RowButton(row, can ? $"{o.CoinName} {o.Cost}개" : "교환 완료", () => Trade(offer), 600, 220, o.Coin != null ? UIFactory.BtnGold : UIFactory.BtnPurple, can && have >= o.Cost, 18);
            }
        }

        void Trade(Offer o)
        {
            if (!P.ConsumeItem(o.Coin ?? AchievementDatabase.ConchId, o.Cost)) return;
            o.Give();
            Game.Sound.Coin();
            Game.Toast($"{o.CoinName} {o.Cost}개로 {o.Name}을(를) 받았습니다.", ToastKind.Good);
            P.NotifyChanged();
        }
    }
}
