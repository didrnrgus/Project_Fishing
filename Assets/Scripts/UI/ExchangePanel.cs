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
            public string Name, Desc;
            public int Cost;
            public Func<bool> Can;
            public Action Give;
        }

        Offer[] _offers;

        public ExchangePanel(Transform canvas)
        {
            BuildFrame(canvas, "ExchangePanel", "소라고동 교환소", new Vector2(900, 640));
            _offers = new[]
            {
                new Offer { Name = "별모래 1개", Desc = "강화 성공 확률 +15%", Cost = 5, Give = () => P.AddItem(RodUpgrade.StarSandId, 1) },
                new Offer { Name = "반짝이는 비늘 10개", Desc = "강화 재료", Cost = 3, Give = () => P.AddItem(RodUpgrade.ScaleId, 10) },
                new Offer { Name = "촉촉한이리 5개", Desc = "Lv.12 해체 미끼 (입질 +50%)", Cost = 4, Give = () => P.AddItem("bait_cut4", 5) },
                new Offer { Name = "살림망 2칸 확장", Desc = "최대 3번까지", Cost = 15, Can = () => P.Data.keepNetBonus < 3 + 6,
                    Give = () => P.Data.keepNetBonus += 2 },
                new Offer { Name = "소라 무늬 셔츠", Desc = "의상 (상의) · 대물 +4% · 체력 소모 -5%", Cost = 12, Can = () => !P.Data.ownedCostumes.Contains("top_conch"),
                    Give = () => P.GiveCostume("top_conch") },
                new Offer { Name = "소라고동 모자", Desc = "의상 (모자) · 체력 +6 · 대물 +4%", Cost = 20, Can = () => !P.Data.ownedCostumes.Contains("hat_conch"),
                    Give = () => P.GiveCostume("hat_conch") },
            };
        }

        static PlayerProfile P => Game.Profile;

        public override void Refresh()
        {
            int conch = P.Count(AchievementDatabase.ConchId);
            Title.text = $"소라고동 교환소   <size=20><color=#ffd060>소라고동 {conch}개</color></size>";
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);
            Cell(rows.Next(28), "도감 등록 1 · 일일 의뢰 1 · 대물 도전 3 · 업적 1~10개씩 모입니다.", 4, 820, 16, UIFactory.TextDim);
            foreach (var o in _offers)
            {
                var row = rows.Next(58, 6);
                UIFactory.RowBg(row);
                bool can = o.Can == null || o.Can();
                Cell(row, $"<b>{o.Name}</b>\n<size=14><color=#a0b0c0>{o.Desc}</color></size>", 12, 560, 19, can ? UIFactory.TextMain : UIFactory.TextDim);
                var offer = o;
                RowButton(row, can ? $"소라고동 {o.Cost}개" : "교환 완료", () => Trade(offer), 600, 220, UIFactory.BtnPurple, can && conch >= o.Cost, 18);
            }
        }

        void Trade(Offer o)
        {
            if (!P.ConsumeItem(AchievementDatabase.ConchId, o.Cost)) return;
            o.Give();
            Game.Sound.Coin();
            Game.Toast($"소라고동 {o.Cost}개로 {o.Name}을(를) 받았습니다.", ToastKind.Good);
            P.NotifyChanged();
        }
    }
}
