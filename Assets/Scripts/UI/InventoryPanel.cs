using System.Collections.Generic;
using System.Linq;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 가방(I) — 원작 인벤토리 (2013 홍현 영상 0:12): 화면 오른쪽에 붙은 창, 탭 [가방 · 살림망 · 배], 6칸 격자에 아이콘과 오른쪽 아래 개수(x50),
    /// 장착 중인 미끼는 초록 테두리, 마우스를 올리면 창 왼쪽에 툴팁(이름·요구 레벨·설명). 아래 [정렬] [수리]와 골드.
    /// 누르면: 미끼 = 장착, 음식 = 먹기, 배 = 탈 배로 정하기. 화면을 어둡게 덮지 않는다.
    /// </summary>
    public class InventoryPanel : UIPanel
    {
        const int Cols = 6, Rows = 7;
        const float Slot = 58f, Gap = 4f;

        enum Tab { Bag, KeepNet, Boat }
        Tab _tab;
        bool _sorted;
        readonly RectTransform _tabs, _grid, _bottom;
        readonly TMP_Text _count;
        readonly RectTransform _tip;
        readonly TMP_Text _tipText;
        readonly Image _tipIcon;

        public InventoryPanel(Transform canvas)
        {
            var size = new Vector2(430, 640);
            BuildFrame(canvas, "InventoryPanel", "인벤토리 <size=15><color=#9fc8e0>INVENTORY</color></size>", size);
            // 원작처럼 오른쪽에 붙이고 뒤를 어둡게 덮지 않는다 (클릭은 막는다)
            Root.GetComponent<Image>().color = Color.clear;
            Frame.At(new Vector2(1, 0.5f), new Vector2(-24, 30), size, new Vector2(1, 0.5f));

            _count = UIFactory.Text(Frame, "", 15, UIFactory.TextDim, TextAnchor.MiddleRight);
            _count.rectTransform.At(new Vector2(1, 1), new Vector2(-70, -26), new Vector2(160, 24), new Vector2(1, 1));

            _tabs = UIFactory.Node("Tabs", Body).At(new Vector2(0, 1), Vector2.zero, new Vector2(374, 38), new Vector2(0, 1));
            _grid = UIFactory.Node("Grid", Body).At(new Vector2(0, 1), new Vector2(0, -48), new Vector2(Cols * (Slot + Gap), Rows * (Slot + Gap)), new Vector2(0, 1));
            _bottom = UIFactory.Node("Bottom", Body).At(new Vector2(0, 0), Vector2.zero, new Vector2(374, 52), Vector2.zero);

            // 툴팁: 창 왼쪽에 뜬다
            var tipGlass = UIFactory.Glass(Root.transform, "Tooltip", UIFactory.RadiusCard, null, 0.85f);
            _tip = tipGlass.rectTransform;
            _tip.At(new Vector2(1, 0.5f), new Vector2(-24 - size.x - 10, 120), new Vector2(300, 260), new Vector2(1, 0.5f));
            tipGlass.raycastTarget = false;
            _tipIcon = UIFactory.Image(_tip, Color.white, null, "Icon");
            _tipIcon.preserveAspect = true;
            _tipIcon.raycastTarget = false;
            _tipIcon.rectTransform.At(new Vector2(0, 1), new Vector2(14, -14), new Vector2(64, 64), new Vector2(0, 1));
            _tipText = UIFactory.Text(_tip, "", 15, UIFactory.TextMain, TextAnchor.UpperLeft);
            _tipText.rectTransform.Fill(14, 14, 88, 12);
            _tipText.raycastTarget = false;
            _tip.gameObject.SetActive(false);
        }

        public override void Open()
        {
            base.Open();
            _tip.gameObject.SetActive(false);
        }

        /// <summary>격자 한 칸에 들어갈 것.</summary>
        sealed class Entry
        {
            public string Icon;          // 아이템 아이콘 id (없으면 Text)
            public Sprite Sprite;        // 물고기 아이콘처럼 직접 넣는 그림
            public string Text;          // 그림이 없을 때 칸 가운데 글씨
            public string Badge;         // 오른쪽 아래 (x50, 30cm)
            public string Corner;        // 왼쪽 위 (+7, 장착)
            public Color Border = new Color(1f, 1f, 1f, 0.14f);
            public string Tip;
            public System.Action Click;
            public int Sort;
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            UIFactory.ClearChildren(_tabs);
            UIFactory.ClearChildren(_grid);
            UIFactory.ClearChildren(_bottom);

            string[] names = { "가방", "살림망", "배" };
            for (int i = 0; i < names.Length; i++)
            {
                var t = (Tab)i;
                var b = UIFactory.Button(_tabs, names[i], () => { _tab = t; Refresh(); }, t == _tab ? UIFactory.TabOn : UIFactory.TabOff, 17);
                ((RectTransform)b.transform).At(new Vector2(0, 1), new Vector2(i * 126, 0), new Vector2(120, 36), new Vector2(0, 1));
            }

            var entries = _tab switch { Tab.KeepNet => KeepNetEntries(p), Tab.Boat => BoatEntries(p), _ => BagEntries(p) };
            if (_sorted) entries = entries.OrderBy(e => e.Sort).ThenBy(e => e.Text ?? e.Icon).ToList();
            int capacity = _tab == Tab.KeepNet ? p.KeepNetSize : Cols * Rows;
            _count.text = _tab == Tab.KeepNet ? $"{p.Data.keepNet.Count} / {p.KeepNetSize}" : $"{entries.Count} / {Cols * Rows}";

            for (int i = 0; i < Cols * Rows; i++)
            {
                var cell = UIFactory.Shape(_grid, new Color(0f, 0f, 0f, i < capacity ? 0.4f : 0.15f), 8f, "Slot", 0f);
                cell.rectTransform.At(new Vector2(0, 1), new Vector2((i % Cols) * (Slot + Gap), -(i / Cols) * (Slot + Gap)), new Vector2(Slot, Slot), new Vector2(0, 1));
                if (i >= entries.Count) continue;
                FillSlot(cell, entries[i]);
            }

            // 아래: 정렬 · 수리 · 골드
            var sort = UIFactory.Button(_bottom, "정렬", () => { _sorted = !_sorted; Refresh(); }, _sorted ? UIFactory.TabOn : UIFactory.ButtonBg, 16);
            ((RectTransform)sort.transform).At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(84, 36), new Vector2(0, 0.5f));
            int cost = p.RepairCost;
            var repair = UIFactory.Button(_bottom, cost > 0 ? $"수리 {cost:N0}G" : "수리", () => Repair(cost), UIFactory.ButtonBg, 16);
            ((RectTransform)repair.transform).At(new Vector2(0, 0.5f), new Vector2(92, 0), new Vector2(120, 36), new Vector2(0, 0.5f));
            repair.interactable = cost > 0 && p.Data.gold >= cost && Game.Fishing.State == Fishing.Angling.FishingState.Idle;
            var gold = UIFactory.Text(_bottom, $"<color=#ffd060>{p.Data.gold:N0} GOLD</color>", 18, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            gold.rectTransform.At(new Vector2(1, 0.5f), Vector2.zero, new Vector2(150, 30), new Vector2(1, 0.5f));
        }

        void FillSlot(UIShape cell, Entry e)
        {
            UIFactory.AddBorder(cell, e.Border, 1.5f);
            var rt = cell.rectTransform;
            var sprite = e.Sprite ?? ItemIcons.Get(e.Icon);
            if (sprite != null)
            {
                var img = UIFactory.Image(rt, Color.white, sprite, "Icon");
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.rectTransform.Fill(5, 5, 5, 5);
            }
            else
            {
                var t = UIFactory.Text(rt, e.Text ?? "", 12, UIFactory.TextMain, TextAnchor.MiddleCenter);
                t.rectTransform.Fill(3, 3, 3, 3);
                t.raycastTarget = false;
            }
            if (!string.IsNullOrEmpty(e.Badge))
            {
                var b = UIFactory.Text(rt, e.Badge, 13, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                b.rectTransform.Fill(2, 4, 2, 1);
                b.raycastTarget = false;
            }
            if (!string.IsNullOrEmpty(e.Corner))
            {
                var c = UIFactory.Text(rt, e.Corner, 12, UIFactory.Good, TextAnchor.UpperLeft, FontStyle.Bold);
                c.rectTransform.Fill(4, 2, 2, 2);
                c.raycastTarget = false;
            }
            cell.raycastTarget = true;
            var btn = cell.gameObject.AddComponent<Button>();
            btn.targetGraphic = cell;
            if (e.Click != null) btn.onClick.AddListener(() => { Game.Sound?.UiClick(); e.Click(); });
            var et = cell.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowTip(e, sprite));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _tip.gameObject.SetActive(false));
            et.triggers.Add(enter);
            et.triggers.Add(exit);
        }

        void ShowTip(Entry e, Sprite sprite)
        {
            _tip.gameObject.SetActive(true);
            _tip.SetAsLastSibling();
            _tipIcon.sprite = sprite;
            _tipIcon.enabled = sprite != null;
            _tipText.text = e.Tip;
            _tipText.rectTransform.Fill(14, 14, sprite != null ? 88 : 14, 12);
        }

        // ---------------- 탭별 칸 ----------------

        List<Entry> BagEntries(PlayerProfile p)
        {
            var list = new List<Entry>();
            bool idle = Game.Fishing.State == Fishing.Angling.FishingState.Idle;
            foreach (var it in p.Data.items.Where(i => i.count > 0))
            {
                var cat = ItemDatabase.Category(it.id);
                string id = it.id;
                var e = new Entry { Icon = id, Text = ItemDatabase.Name(id), Badge = $"x{it.count}", Sort = (int)cat };
                string head = $"<b><size=18>{ItemDatabase.Name(id)}</size></b>\n";
                if (cat == ItemCategory.Bait)
                {
                    var b = GearDatabase.GetBait(id);
                    bool equipped = id == p.Data.baitId;
                    if (equipped) { e.Border = new Color(0.4f, 0.95f, 0.45f, 0.95f); e.Corner = "장착"; }
                    e.Tip = head + (equipped ? "<color=#70e070>(장착중)</color>\n" : "") +
                            $"<color=#8fa3b5>최소요구레벨</color>  Lv.{b?.Level ?? 1}\n<color=#8fa3b5>개수</color>  {it.count}\n\n{ItemDatabase.Desc(id)}\n\n" +
                            (equipped ? "" : b != null && b.Level > p.Data.level ? "<color=#f05048>레벨이 모자랍니다</color>" : "<color=#9fd8ff>누르면 장착</color>");
                    e.Click = () =>
                    {
                        if (!idle) { Game.Toast("낚시 중에는 미끼를 바꿀 수 없습니다.", ToastKind.Warn); return; }
                        if (b != null && b.Level > p.Data.level) { Game.Toast($"Lv.{b.Level}부터 쓸 수 있습니다.", ToastKind.Warn); return; }
                        p.SelectBait(id);
                    };
                }
                else if (GearDatabase.GetFood(id) is { } food)
                {
                    e.Border = new Color(1f, 0.75f, 0.35f, 0.5f);
                    e.Tip = head + $"<color=#8fa3b5>개수</color>  {it.count}\n\n{food.Desc}\n<color=#a0d0a0>{food.EffectText}</color>\n\n<color=#9fd8ff>누르면 먹기</color>";
                    e.Click = () => Eat(food);
                }
                else
                {
                    e.Tip = head + $"<color=#8fa3b5>개수</color>  {it.count}\n\n{ItemDatabase.Desc(id)}";
                }
                list.Add(e);
            }
            if (p.HasCottonGloves)
                list.Add(new Entry
                {
                    Icon = PlayerProfile.CottonGlovesId, Text = "목장갑", Corner = "착용", Sort = 9, Border = new Color(0.4f, 0.95f, 0.45f, 0.95f),
                    Tip = "<b><size=18>미끄러지지 않는 목장갑</size></b>\n<color=#70e070>(착용중)</color>\n\n<color=#a0d0a0>파이팅 힘 +5% · 릴 속도 +5%</color>\nLv.6 보상. 늘 끼고 있다.",
                });
            return list;
        }

        List<Entry> KeepNetEntries(PlayerProfile p)
        {
            var list = new List<Entry>();
            foreach (var f in p.Data.keepNet)
            {
                var s = FishDatabase.Get(f.speciesId);
                if (s == null) continue;
                bool trophy = s.IsTrophy(f.length);
                list.Add(new Entry
                {
                    Sprite = Resources.Load<Sprite>("Icons/Fish/" + s.Id),
                    Text = s.Name,
                    Badge = $"{f.length:0}cm",
                    Corner = trophy ? s.TrophyName : null,
                    Border = trophy ? new Color(1f, 0.8f, 0.3f, 0.9f) : new Color(1f, 1f, 1f, 0.14f),
                    Sort = -s.Rarity,
                    Tip = $"<b><size=18>{s.Name}</size></b>\n{f.length:0.00} cm  {f.weight:0.00} kg\n" +
                          (trophy ? $"<color=#ffd060>{s.TrophyName}!</color>\n" : "") +
                          $"\n<color=#8fa3b5>납품가</color>  <color=#ffd060>{PlayerProfile.SellPrice(f):N0} GOLD</color>\n<color=#8fa3b5>낚은 곳</color>  {f.zone}",
                });
            }
            return list;
        }

        List<Entry> BoatEntries(PlayerProfile p)
        {
            var list = new List<Entry>();
            foreach (var id in p.Data.ownedGear)
            {
                var g = GearDatabase.GetGear(id);
                if (g == null || g.Slot != GearSlot.Boat) continue;
                bool riding = p.Data.boatId == id;
                list.Add(new Entry
                {
                    Icon = id, Text = g.Name, Corner = riding ? "사용" : null,
                    Border = riding ? new Color(0.4f, 0.95f, 0.45f, 0.95f) : new Color(1f, 1f, 1f, 0.14f),
                    Tip = $"<b><size=18>{g.Name}</size></b>\n<color=#8fa3b5>최소요구레벨</color>  Lv.{g.Level}\n<color=#a0d0a0>{g.StatLine}</color>\n" +
                          (riding ? $"<color=#8fa3b5>주유량</color>  {p.Data.boatFuel / Mathf.Max(1f, g.FuelCapacity) * 100f:0} %\n" : "") +
                          $"\n{g.Desc}\n\n" + (riding ? "<color=#70e070>물가에서 B로 꺼내 타기</color>" : "<color=#9fd8ff>누르면 이 배를 쓴다</color>"),
                    Click = riding ? null : (System.Action)(() =>
                    {
                        if (Game.Boat != null && Game.Boat.Riding) { Game.Toast("배에서 내린 뒤에 바꿀 수 있습니다.", ToastKind.Warn); return; }
                        p.Equip(g);
                        Game.Toast($"{g.Name}을(를) 쓰기로 했습니다.", ToastKind.Good);
                    }),
                });
            }
            return list;
        }

        void Eat(FoodDef food)
        {
            var p = Game.Profile;
            if (!p.Eat(food, Game.Clock.TotalMinutes)) { Game.Toast("배가 부릅니다.", ToastKind.Warn); return; }
            Game.Toast($"{food.Name}을(를) 먹었습니다. {food.EffectText}", ToastKind.Good);
        }

        void Repair(int cost)
        {
            var p = Game.Profile;
            if (cost <= 0 || !p.RepairAll()) return;
            Game.SysLog($"{cost:N0} GOLD 를 지불했습니다. 낚싯대·릴을 수리하고 줄을 새로 감았습니다.");
        }
    }
}
