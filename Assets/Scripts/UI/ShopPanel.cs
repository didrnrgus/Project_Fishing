using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>낚시점: 장비(낚싯대·릴·줄·배) 구매/장착, 미끼·김밥 구매, 물고기 판매, 낚싯대 수리, 배 연료.</summary>
    public class ShopPanel : UIPanel
    {
        const int RepairCostPerPoint = 8;

        public ShopPanel(Transform canvas)
        {
            BuildFrame(canvas, "ShopPanel", "반포 낚시점", new Vector2(1500, 960));
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            Title.text = $"반포 낚시점   <size=22><color=#ffd060>{p.Data.gold:N0} 골드</color></size>";
            UIFactory.ClearChildren(Body);

            var gearCol = UIFactory.Node("Gear", Body);
            gearCol.anchorMin = new Vector2(0, 0); gearCol.anchorMax = new Vector2(0.56f, 1);
            gearCol.offsetMin = Vector2.zero; gearCol.offsetMax = new Vector2(-10, 0);
            var shopCol = UIFactory.Node("Items", Body);
            shopCol.anchorMin = new Vector2(0.56f, 0); shopCol.anchorMax = new Vector2(1, 1);
            shopCol.offsetMin = new Vector2(10, 0); shopCol.offsetMax = Vector2.zero;

            BuildGear(gearCol, p);
            BuildItems(shopCol, p);
        }

        void BuildGear(RectTransform col, PlayerProfile p)
        {
            var rows = new RowCursor(col);
            Cell(rows.Next(30), "장비", 4, 300, 22, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            GearSlot? lastSlot = null;
            foreach (var g in GearDatabase.Gear)
            {
                if (lastSlot != g.Slot)
                {
                    lastSlot = g.Slot;
                    string slotName = g.Slot switch { GearSlot.Rod => "낚싯대", GearSlot.Reel => "릴", GearSlot.Boat => "배 (물가에서 B로 꺼내 타기)", _ => "원줄" };
                    Cell(rows.Next(22, 2), slotName, 4, 200, 15, UIFactory.TextDim);
                }
                var row = rows.Next(50, 4);
                UIFactory.Image(row, new Color(1, 1, 1, 0.04f), null, "Bg").rectTransform.Fill();
                int plus = g.Slot == GearSlot.Rod ? p.RodLevel(g.Id) : 0;
                var shown = plus > 0 ? RodUpgrade.Apply(g, plus) : g;
                var name = Cell(row, $"<b>{g.Name}</b>{(plus > 0 ? $" <color=#ffd060>+{plus}</color>" : "")}\n<size=14><color=#a0b0c0>{shown.StatLine}</color></size>", 10, 470, 18);
                name.alignment = TextAnchor.MiddleLeft;

                bool owned = p.Owns(g.Id);
                bool equipped = g.Id == p.Data.rodId || g.Id == p.Data.reelId || g.Id == p.Data.lineId || g.Id == p.Data.boatId;
                bool levelOk = p.Data.level >= g.Level;
                if (equipped)
                    RowButton(row, "장착 중", null, 560, 220, new Color(0.15f, 0.45f, 0.42f), false);
                else if (owned)
                    RowButton(row, "장착", () => { p.Equip(g); Game.Toast($"{g.Name} 장착"); }, 560, 220);
                else if (!levelOk)
                    RowButton(row, $"Lv.{g.Level} 필요 · {g.Price:N0}골드", null, 560, 220, null, false, 15);
                else
                    RowButton(row, $"구매 {g.Price:N0}골드", () => BuyGear(g), 560, 220, new Color(0.45f, 0.35f, 0.12f), p.Data.gold >= g.Price);
            }
        }

        void BuildItems(RectTransform col, PlayerProfile p)
        {
            var rows = new RowCursor(col);
            Cell(rows.Next(30), "미끼 · 먹을거리", 4, 300, 22, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            foreach (var b in GearDatabase.Baits)
            {
                var row = rows.Next(40, 4);
                UIFactory.Image(row, new Color(1, 1, 1, 0.04f), null, "Bg").rectTransform.Fill();
                Cell(row, $"{b.Name}  <size=14><color=#a0b0c0>{b.PackSize}개 · 보유 {p.Count(b.Id)}</color></size>", 10, 380, 18);
                RowButton(row, $"{b.Price:N0}골드", () => BuyItem(b.Id, b.Name, b.PackSize, b.Price), 400, 200,
                    new Color(0.45f, 0.35f, 0.12f), p.Data.gold >= b.Price);
            }
            var kimbap = rows.Next(40, 4);
            UIFactory.Image(kimbap, new Color(1, 1, 1, 0.04f), null, "Bg").rectTransform.Fill();
            Cell(kimbap, $"김밥  <size=14><color=#a0b0c0>체력 +{GearDatabase.KimbapStamina:0} · 보유 {p.Count(GearDatabase.KimbapId)}</color></size>", 10, 380, 18);
            RowButton(kimbap, $"{GearDatabase.KimbapPrice:N0}골드", () => BuyItem(GearDatabase.KimbapId, "김밥", 1, GearDatabase.KimbapPrice), 400, 200,
                new Color(0.45f, 0.35f, 0.12f), p.Data.gold >= GearDatabase.KimbapPrice);

            rows.Next(10);
            Cell(rows.Next(30), "서비스", 4, 300, 22, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);

            int total = 0;
            foreach (var f in p.Data.keepNet) total += PlayerProfile.SellPrice(f);
            var sell = rows.Next(46, 4);
            Cell(sell, $"살림망 물고기 모두 판매  <size=14><color=#a0b0c0>{p.Data.keepNet.Count}마리</color></size>", 10, 380, 18);
            RowButton(sell, $"+{total:N0}골드", SellAll, 400, 200, new Color(0.15f, 0.45f, 0.42f), p.Data.keepNet.Count > 0);

            float dur = p.Data.rodDurability;
            int cost = Mathf.CeilToInt((100f - dur) * RepairCostPerPoint);
            var repair = rows.Next(46, 4);
            Cell(repair, $"낚싯대 수리  <size=14><color=#a0b0c0>내구도 {dur:0}/100</color></size>", 10, 380, 18);
            RowButton(repair, cost > 0 ? $"{cost:N0}골드" : "수리 불필요", () => Repair(cost), 400, 200, null, cost > 0 && p.Data.gold >= cost);

            var up = rows.Next(46, 4);
            int lvNow = p.RodLevel(p.Data.rodId);
            Cell(up, $"낚싯대 강화  <size=14><color=#a0b0c0>{GearDatabase.GetGear(p.Data.rodId).Name} +{lvNow}</color></size>", 10, 380, 18);
            RowButton(up, "강화하러 가기", () => Game.UI.OpenExclusive(Game.UI.Upgrade), 400, 200, new Color(0.55f, 0.35f, 0.1f));

            var boat = p.Boat;
            var fuelRow = rows.Next(46, 4);
            if (boat == null)
            {
                Cell(fuelRow, "배 연료 채우기  <size=14><color=#a0b0c0>배가 없습니다</color></size>", 10, 380, 18, UIFactory.TextDim);
            }
            else
            {
                float missing = Mathf.Max(0f, boat.FuelCapacity - p.Data.boatFuel);
                int fuelCost = Mathf.CeilToInt(missing * GearDatabase.FuelPricePerLiter);
                Cell(fuelRow, $"{boat.Name} 연료 채우기  <size=14><color=#a0b0c0>{p.Data.boatFuel:0.0}/{boat.FuelCapacity:0}L</color></size>", 10, 380, 18);
                RowButton(fuelRow, fuelCost > 0 ? $"{fuelCost:N0}골드" : "가득 참", () => Refuel(fuelCost), 400, 200, null,
                    fuelCost > 0 && p.Data.gold >= fuelCost);
            }
        }

        void BuyGear(GearDef g)
        {
            var p = Game.Profile;
            if (!p.TrySpend(g.Price)) { Game.Toast("골드가 부족합니다.", ToastKind.Warn); return; }
            p.Data.ownedGear.Add(g.Id);
            p.Equip(g);
            Game.Sound.Coin();
            Game.Toast($"{g.Name}을(를) 구매해 장착했습니다.", ToastKind.Good);
        }

        void BuyItem(string id, string name, int count, int price)
        {
            var p = Game.Profile;
            if (!p.TrySpend(price)) { Game.Toast("골드가 부족합니다.", ToastKind.Warn); return; }
            p.AddItem(id, count);
            Game.Sound.Coin();
            Game.Toast($"{name} {count}개 구매");
        }

        void SellAll()
        {
            var p = Game.Profile;
            int total = 0;
            foreach (var f in p.Data.keepNet) total += PlayerProfile.SellPrice(f);
            int n = p.Data.keepNet.Count;
            p.Data.keepNet.Clear();
            p.AddGold(total);
            Game.Sound.Coin();
            Game.Toast($"물고기 {n}마리를 {total:N0}골드에 팔았습니다.", ToastKind.Good);
        }

        void Refuel(int cost)
        {
            var p = Game.Profile;
            if (p.Boat == null || !p.TrySpend(cost)) return;
            p.Data.boatFuel = p.Boat.FuelCapacity;
            Game.Sound.Coin();
            Game.Toast("배에 연료를 가득 채웠습니다.", ToastKind.Good);
            p.NotifyChanged();
        }

        void Repair(int cost)
        {
            var p = Game.Profile;
            if (!p.TrySpend(cost)) return;
            p.Data.rodDurability = 100f;
            Game.Sound.Coin();
            Game.Toast("낚싯대를 수리했습니다.", ToastKind.Good);
            p.NotifyChanged();
        }
    }
}
