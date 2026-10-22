using System.Linq;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 마을 교환 (마을 어부). 상점에서 안 파는 이 마을 낚싯대·릴·줄을 나팔고동(한국)·황금거거(해외)로 바꾼다.
    /// 아래에는 같은 재화로 바꾸는 다른 마을 장비와 어디 가야 하는지를 보여 준다.
    /// </summary>
    public class VillageTradePanel : UIPanel
    {
        string _fisher;

        public VillageTradePanel(Transform canvas)
        {
            BuildFrame(canvas, "VillageTradePanel", UIFactory.Caption("마을 교환", "VILLAGE TRADE"), new Vector2(980, 880));
        }

        static PlayerProfile P => Game.Profile;

        public void OpenFrom(string fisherName)
        {
            _fisher = fisherName;
            Game.UI.OpenExclusive(this);
        }

        public override void Refresh()
        {
            string map = FieldMap.Id;
            string coin = VillageTrade.CurrencyFor(map);
            string coinName = VillageTrade.Name(coin);
            int have = P.Count(coin);
            string who = string.IsNullOrEmpty(_fisher) ? $"{VillageTrade.MapName(map)} 어부" : _fisher;
            Title.text = $"{UIFactory.Caption("마을 교환", "VILLAGE TRADE")}   <size=20><color=#9fd8ff>{who}</color>  ·  <color=#ffd060>{coinName} {have}개</color></size>";
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);

            Cell(rows.Next(28), VillageTrade.IsKorea(map)
                ? $"나팔고동: 한국 일일 의뢰 {VillageTrade.DailyTokens} · 한국 대물 도전 {VillageTrade.TrophyTokens} · 조합 낚시 대회 1~3위 {string.Join("/", VillageTrade.TournamentTokens)} · 빙고 3줄 {Bingo.BigNapal}"
                : $"황금거거: 해외 일일 의뢰 {VillageTrade.DailyTokens} · 해외 대물 도전 {VillageTrade.TrophyTokens} · 빙고 3줄 {Bingo.BigGoldGege} (Lv.25부터)",
                4, 900, 16, UIFactory.TextDim);
            Cell(rows.Next(24), "상점에서 골드로는 못 사는 마을 장비. 같은 레벨 상점 장비보다 능력치가 좋다.", 4, 900, 16, UIFactory.TextDim);

            UIFactory.SectionTitle(rows.Next(32, 4), $"{VillageTrade.MapName(map)} 마을 장비", 4, 18);
            foreach (var g in VillageTrade.OffersAt(map))
            {
                var row = rows.Next(86, 6);
                UIFactory.RowBg(row);
                bool owned = P.Owns(g.Id);
                bool levelOk = P.Data.level >= g.Level;
                float ix = ItemIcons.Place(row, g.Id, 8, 64, owned);
                Cell(row, $"<b>{g.Name}</b>  <size=15><color=#8fa3b5>{SlotName(g.Slot)} · Lv.{g.Level}</color></size>\n" +
                          $"<size=15><color=#a0d0a0>{g.StatLine}</color>  {Compare(g)}</size>\n<size=14><color=#a0b0c0>{g.Desc}</color></size>",
                    12 + ix, 680 - ix, 18, owned ? UIFactory.TextDim : UIFactory.TextMain);
                var gear = g;
                if (owned) RowButton(row, "교환 완료", null, 700, 210, null, false);
                else if (!levelOk) RowButton(row, $"Lv.{g.Level} 필요 · {coinName} {g.TradeCost}", null, 700, 210, null, false, 15);
                else RowButton(row, $"{coinName} {g.TradeCost}개", () => Trade(gear, coin), 700, 210, UIFactory.BtnGold, have >= g.TradeCost, 18);
            }

            rows.Next(6);
            UIFactory.SectionTitle(rows.Next(32, 4), $"다른 마을 장비 ({coinName})", 4, 18);
            foreach (var g in VillageTrade.OffersFor(coin).Where(x => x.TradeMap != map))
            {
                var row = rows.Next(44, 4);
                UIFactory.RowBg(row);
                bool owned = P.Owns(g.Id);
                float ix = ItemIcons.Place(row, g.Id, 6, 38, true);
                Cell(row, $"<b>{g.Name}</b>  <size=14><color=#8fa3b5>{SlotName(g.Slot)} · Lv.{g.Level} · {g.StatLine}</color></size>",
                    10 + ix, 690 - ix, 16, owned ? UIFactory.TextDim : UIFactory.TextMain);
                Cell(row, owned ? "<color=#70e070>가지고 있음</color>" : $"{VillageTrade.MapName(g.TradeMap)} 어부 · {coinName} {g.TradeCost}",
                    700, 210, 15, UIFactory.TextDim, TextAnchor.MiddleRight);
            }
        }

        static string SlotName(GearSlot s) => s switch { GearSlot.Rod => "낚싯대", GearSlot.Reel => "릴", GearSlot.Line => "원줄", _ => "배" };

        /// <summary>같은 레벨까지 살 수 있는 가장 좋은 상점 장비와 견준 한 줄.</summary>
        static string Compare(GearDef g)
        {
            var best = GearDatabase.Gear.Where(x => x.Slot == g.Slot && !x.NotSold && x.Level <= g.Level).OrderByDescending(Key).FirstOrDefault();
            if (best == null) return "";
            float d = Key(g) - Key(best);
            string stat = g.Slot switch { GearSlot.Rod => "힘", GearSlot.Reel => "감기", _ => "강도" };
            return d > 0f ? $"<color=#ffd060>(상점 {best.Name}보다 {stat} +{d:0.0#})</color>" : "";
        }

        static float Key(GearDef g) => g.Slot switch { GearSlot.Rod => g.Power, GearSlot.Reel => g.ReelSpeed, _ => g.Strength };

        void Trade(GearDef g, string coin)
        {
            if (P.Owns(g.Id) || !P.ConsumeItem(coin, g.TradeCost)) return;
            P.Data.ownedGear.Add(g.Id);
            Game.Sound.Coin();
            Game.Toast($"{VillageTrade.Name(coin)} {g.TradeCost}개로 {g.Name}을(를) 받았습니다. 가방(I)이나 낚시점에서 장착하세요.", ToastKind.Good);
            P.Log($"마을 교환 — {g.Name} ({VillageTrade.MapName(g.TradeMap)})");
            P.NotifyChanged();
        }
    }
}
