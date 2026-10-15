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
                UIFactory.SectionTitle(rows.Next(30), cat switch { ItemCategory.Bait => "미끼", ItemCategory.Food => "먹을거리", _ => "재료" }, 4, 20);
                if (items.Count == 0)
                {
                    Cell(rows.Next(26), "없음", 14, 400, 16, UIFactory.TextDim);
                    continue;
                }
                foreach (var it in items)
                {
                    var row = rows.Next(46, 3);
                    UIFactory.RowBg(row);
                    bool equipped = it.id == p.Data.baitId;
                    float ix = ItemIcons.Place(row, it.id, 6, 40);
                    Cell(row, $"<b>{ItemDatabase.Name(it.id)}</b>  <color=#ffd060>x{it.count}</color>{(equipped ? "  <color=#70e070>장착 중</color>" : "")}", 10 + ix, 300 - ix, 18);
                    Cell(row, ItemDatabase.Desc(it.id), 310, 400, 14, UIFactory.TextDim);
                    if (cat == ItemCategory.Bait)
                    {
                        var b = GearDatabase.GetBait(it.id);
                        bool ok = b != null && b.Level <= p.Data.level && !equipped;
                        string id = it.id;
                        RowButton(row, b != null && b.Level > p.Data.level ? $"Lv.{b.Level}" : equipped ? "장착 중" : "장착", () => p.SelectBait(id),
                            720, 120, null, ok && Game.Fishing.State == Fishing.Angling.FishingState.Idle, 16);
                    }
                    else if (GearDatabase.GetFood(it.id) is { } food)
                    {
                        RowButton(row, "먹기", () => Eat(food), 720, 120, UIFactory.BtnGood, true, 16);
                    }
                }
                rows.Next(8);
            }

            // 배
            UIFactory.SectionTitle(rows.Next(30), "배", 4, 20);
            var boat = p.Boat;
            var boatRow = rows.Next(boat == null ? 28 : 52);
            float bx = boat != null ? ItemIcons.Place(boatRow, boat.Id, 6, 48) : 0f;
            Cell(boatRow, boat == null ? "없음 (Lv.5에 조합이 고무보트를 지급합니다)"
                : $"{boat.Name}  연료 {p.Data.boatFuel:0.0}/{boat.FuelCapacity:0}L  ·  물가에서 B로 꺼내 타기", 14 + bx, 800 - bx, 17, boat == null ? UIFactory.TextDim : UIFactory.TextMain);

            // 장신구 (레벨 보상: 늘 끼고 있는 셈)
            rows.Next(8);
            UIFactory.SectionTitle(rows.Next(30), "장신구", 4, 20);
            var accRow = rows.Next(46);
            float ax = p.HasCottonGloves ? ItemIcons.Place(accRow, PlayerProfile.CottonGlovesId, 6, 40) : 0f;
            Cell(accRow, p.HasCottonGloves ? "<b>미끄러지지 않는 목장갑</b>  <color=#70e070>착용 중</color>  <size=14><color=#a0b0c0>파이팅 힘 +5% · 릴 속도 +5%</color></size>"
                : "없음 (Lv.6 보상: 미끄러지지 않는 목장갑)", 14 + ax, 800 - ax, 17, p.HasCottonGloves ? UIFactory.TextMain : UIFactory.TextDim);

            // 분당 회복 중인 음식
            if (p.Regenerating(Game.Clock.TotalMinutes))
                Cell(rows.Next(30), $"음식 효과: 게임 1분마다 체력 +{p.Data.regenPerMin:0.#} (게임 {p.Data.regenUntil - Game.Clock.TotalMinutes:0}분 남음)", 14, 800, 15, new Color(0.5f, 0.9f, 0.55f));
        }

        void Eat(FoodDef food)
        {
            var p = Game.Profile;
            if (!p.Eat(food, Game.Clock.TotalMinutes)) { Game.Toast("배가 부릅니다.", ToastKind.Warn); return; }
            Game.Toast($"{food.Name}을(를) 먹었습니다. {food.EffectText}", ToastKind.Good);
        }
    }
}
