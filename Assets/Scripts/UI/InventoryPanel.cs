using System.Linq;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>가방(I): 미끼·먹을거리·강화 재료와 가진 배. 미끼는 여기서 바로 장착, 김밥은 바로 먹는다.</summary>
    public class InventoryPanel : UIPanel
    {
        public InventoryPanel(Transform canvas)
        {
            BuildFrame(canvas, "InventoryPanel", "가방", new Vector2(900, 760));
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            Title.text = $"가방   <size=20><color=#ffd060>{p.Data.gold:N0} 골드</color></size>";
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);

            foreach (var cat in new[] { ItemCategory.Bait, ItemCategory.Food, ItemCategory.Material })
            {
                var items = p.Data.items.Where(i => i.count > 0 && ItemDatabase.Category(i.id) == cat).ToList();
                Cell(rows.Next(30), cat switch { ItemCategory.Bait => "미끼", ItemCategory.Food => "먹을거리", _ => "재료" }, 4, 300, 20,
                    UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                if (items.Count == 0)
                {
                    Cell(rows.Next(26), "없음", 14, 400, 16, UIFactory.TextDim);
                    continue;
                }
                foreach (var it in items)
                {
                    var row = rows.Next(36, 3);
                    UIFactory.Image(row, new Color(1, 1, 1, 0.04f), null, "Bg").rectTransform.Fill();
                    bool equipped = it.id == p.Data.baitId;
                    Cell(row, $"<b>{ItemDatabase.Name(it.id)}</b>  <color=#ffd060>x{it.count}</color>{(equipped ? "  <color=#70e070>장착 중</color>" : "")}", 10, 300, 18);
                    Cell(row, ItemDatabase.Desc(it.id), 310, 400, 14, UIFactory.TextDim);
                    if (cat == ItemCategory.Bait)
                    {
                        var b = GearDatabase.GetBait(it.id);
                        bool ok = b != null && b.Level <= p.Data.level && !equipped;
                        string id = it.id;
                        RowButton(row, b != null && b.Level > p.Data.level ? $"Lv.{b.Level}" : equipped ? "장착 중" : "장착", () => p.SelectBait(id),
                            720, 120, null, ok && Game.Fishing.State == Fishing.Angling.FishingState.Idle, 16);
                    }
                    else if (it.id == GearDatabase.KimbapId)
                    {
                        RowButton(row, "먹기", EatKimbap, 720, 120, new Color(0.25f, 0.45f, 0.25f), true, 16);
                    }
                }
                rows.Next(8);
            }

            // 배
            Cell(rows.Next(30), "배", 4, 300, 20, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            var boat = p.Boat;
            Cell(rows.Next(28), boat == null ? "없음 (Lv.5에 조합이 고무보트를 지급합니다)"
                : $"{boat.Name}  연료 {p.Data.boatFuel:0.0}/{boat.FuelCapacity:0}L  ·  물가에서 B로 꺼내 타기", 14, 800, 17, boat == null ? UIFactory.TextDim : UIFactory.TextMain);
        }

        void EatKimbap()
        {
            var p = Game.Profile;
            if (p.Data.stamina >= p.StaminaCap - 0.5f) { Game.Toast("배가 부릅니다.", ToastKind.Warn); return; }
            if (!p.ConsumeItem(GearDatabase.KimbapId)) return;
            p.ChangeStamina(GearDatabase.KimbapStamina);
            Game.Toast($"김밥을 먹었습니다. 체력 +{GearDatabase.KimbapStamina:0}", ToastKind.Good);
            p.NotifyChanged();
        }
    }
}
