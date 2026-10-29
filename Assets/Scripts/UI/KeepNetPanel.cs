using System.Collections.Generic;
using System.Linq;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 살림망: 보관한 물고기 목록. 요리해서 먹으면 체력을 회복하고, 작은 고기(20cm 미만)는 생미끼로 만들 수 있다.
    /// 오른쪽 칸: 어종 구성(도넛)과 합계 판매가·체력.
    /// </summary>
    public class KeepNetPanel : UIPanel
    {
        const float SideW = 330f;
        RectTransform _list;
        ChartHost _mixChart;
        TMP_Text _summary, _mixEmpty;

        public KeepNetPanel(Transform canvas)
        {
            BuildFrame(canvas, "KeepNetPanel", UIFactory.Caption("살림망", "KEEP NET"), new Vector2(1240, 820));
            _list = UIFactory.Node("List", Body).Fill(0, SideW + 20, 0, 0);

            var side = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "Side", 0f).rectTransform;
            side.anchorMin = new Vector2(1, 0);
            side.anchorMax = new Vector2(1, 1);
            side.pivot = new Vector2(1, 0.5f);
            side.anchoredPosition = Vector2.zero;
            side.sizeDelta = new Vector2(SideW, 0);
            UIFactory.Text(side, "어종 구성", 15, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -10), new Vector2(300, 24));
            _mixChart = UIChart.Donut(side, new Vector2(SideW - 16, 330), new[] { "-" }, new[] { 1f }, null, 0.58f, false);
            ((RectTransform)_mixChart.transform).At(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(SideW - 16, 330), new Vector2(0.5f, 1));
            _mixEmpty = UIFactory.Text(side, "비어 있음", 18, UIFactory.TextFaint, TextAnchor.MiddleCenter);
            _mixEmpty.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -205), new Vector2(200, 40), new Vector2(0.5f, 0.5f));
            _summary = UIFactory.Text(side, "", 15, UIFactory.TextMain, TextAnchor.UpperLeft);
            _summary.rectTransform.At(new Vector2(0, 1), new Vector2(18, -384), new Vector2(SideW - 36, 320), new Vector2(0, 1));
            _summary.lineSpacing = 12f;
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var net = p.Data.keepNet;
            Title.text = $"{UIFactory.Caption("살림망", "KEEP NET")}  <size=20><color=#8fa3b5>{net.Count}/{Game.Profile.KeepNetSize}</color></size>";
            UIFactory.ClearChildren(_list);

            var rows = new RowCursor(_list);
            var header = rows.Next(24);
            Cell(header, "어종", 14, 190, 14, UIFactory.TextFaint);
            Cell(header, "길이", 196, 90, 14, UIFactory.TextFaint);
            Cell(header, "무게", 282, 100, 14, UIFactory.TextFaint);
            Cell(header, "판매가", 380, 90, 14, UIFactory.TextFaint);

            if (net.Count == 0)
                Cell(rows.Next(60), "비어 있습니다. 물고기를 낚아 보관해 보세요.", 14, 680, 18, UIFactory.TextDim);

            int total = 0;
            float cook = 0f;
            // 살림망이 커지면 행을 얇게 해서 창 안에 들어가게 한다
            float rowH = net.Count > 22 ? 22f : net.Count > 16 ? 25f : 32f;
            for (int i = 0; i < net.Count; i++)
            {
                var fish = net[i];
                var s = FishDatabase.Get(fish.speciesId);
                int price = PlayerProfile.SellPrice(fish);
                total += price;
                cook += PlayerProfile.CookStamina(fish);
                var row = rows.Next(rowH, 3);
                bool trophy = s.IsTrophy(fish.length);
                UIFactory.RowBg(row, trophy, 10f);
                Cell(row, (trophy ? "★ " : "") + s.Name, 14, 180, 17, trophy ? UIFactory.Gold : UIFactory.TextMain);
                Cell(row, $"{fish.length:0.0}cm", 196, 90, 16);
                Cell(row, fish.weight >= 1000 ? $"{fish.weight / 1000f:0.00}kg" : $"{fish.weight:0}g", 282, 100, 16);
                Cell(row, $"{price:N0}", 380, 90, 16, UIFactory.Gold);
                int index = i;
                RowButton(row, $"요리 +{PlayerProfile.CookStamina(fish):0}", () => Cook(index), 470, 130, UIFactory.BtnGold, true, 14);
                if (PlayerProfile.CanMakeBait(fish))
                    RowButton(row, $"생미끼 +{PlayerProfile.BaitFromFish(fish)}", () => MakeBait(index), 606, 150, UIFactory.BtnBlue, true, 14);
                else if (!s.IsJunk)
                    RowButton(row, "해체", () => Dissect(index), 606, 150, UIFactory.BtnDanger, true, 14);
            }

            var footer = rows.Next(52, 14);
            // 가진 음식 중 첫 번째를 먹는다 (가방에서 골라 먹을 수도 있다)
            var food = GearDatabase.Foods.FirstOrDefault(fd => p.Count(fd.Id) > 0);
            RowButton(footer, food != null ? $"{food.Name} 먹기 ({p.Count(food.Id)}개)" : "음식 없음", () => Eat(food), 0, 240, UIFactory.BtnGood, food != null, 17);
            Cell(rows.Next(40), $"{PlayerProfile.BaitFishMaxLength:0}cm 미만은 생미끼로, 그보다 큰 고기는 해체해서 해체 미끼·반짝이는 비늘(강화 재료)·별모래를 얻습니다. 오징어·문어는 해체하면 오징어살.",
                4, 840, 13, UIFactory.TextDim);

            RefreshSide(net, total, cook);
        }

        void RefreshSide(List<CaughtFish> net, int total, float cook)
        {
            // 어종별 마릿수 (많은 순 6종 + 나머지는 "기타")
            var groups = net.GroupBy(f => f.speciesId).Select(g => (s: FishDatabase.Get(g.Key), n: g.Count()))
                .OrderByDescending(x => x.n).ToList();
            var names = new List<string>();
            var values = new List<float>();
            for (int i = 0; i < groups.Count && i < 6; i++)
            {
                names.Add($"{groups[i].s?.Name}  {groups[i].n}");
                values.Add(groups[i].n);
            }
            int rest = groups.Skip(6).Sum(x => x.n);
            if (rest > 0) { names.Add($"기타  {rest}"); values.Add(rest); }
            _mixEmpty.gameObject.SetActive(net.Count == 0);
            _mixChart.gameObject.SetActive(net.Count > 0);
            if (net.Count > 0)
            {
                var colors = new List<Color>();
                for (int i = 0; i < values.Count; i++) colors.Add(i == 6 ? UIChart.Palette[7] : UIChart.Palette[i]);
                UIChart.SetSlices(_mixChart.Serie(), names, values, colors);
                _mixChart.Refresh();
            }

            float biggest = net.Count > 0 ? net.Max(f => f.length) : 0f;
            int trophies = net.Count(f => FishDatabase.Get(f.speciesId)?.IsTrophy(f.length) == true);
            var lines = new System.Text.StringBuilder();
            foreach (var n in names) lines.Append($"<color=#a0b0c0>·</color> {n}마리\n");
            lines.Append($"\n<color=#8fa3b5>합계 판매가</color>  <color=#ffd060><b>{total:N0} G</b></color>\n");
            lines.Append($"<color=#8fa3b5>요리하면</color>  체력 +{cook:0}\n");
            lines.Append($"<color=#8fa3b5>가장 큰 고기</color>  {biggest:0.0}cm");
            if (trophies > 0) lines.Append($"\n<color=#ff7090>대물 {trophies}마리</color>");
            lines.Append("\n\n<size=13><color=#8fa3b5>판매는 납품소(필드) · 대기실 납품 버튼에서</color></size>");
            _summary.text = lines.ToString();
        }

        void Cook(int index)
        {
            var p = Game.Profile;
            if (index >= p.Data.keepNet.Count) return;
            var fish = p.Data.keepNet[index];
            if (p.Data.stamina >= p.StaminaCap - 0.5f)
            {
                Game.Toast("배가 부릅니다.", ToastKind.Warn);
                return;
            }
            float amount = PlayerProfile.CookStamina(fish);
            p.Data.keepNet.RemoveAt(index);
            p.ChangeStamina(amount);
            var name = FishDatabase.Get(fish.speciesId)?.Name;
            Game.Toast($"{name} 요리를 먹었습니다. 체력 +{amount:0}", ToastKind.Good);
            p.NotifyChanged();
        }

        void Dissect(int index)
        {
            var msg = Game.Profile.Dissect(index);
            if (msg != null) Game.Toast(msg, ToastKind.Good);
        }

        void MakeBait(int index)
        {
            var p = Game.Profile;
            if (index >= p.Data.keepNet.Count) return;
            var fish = p.Data.keepNet[index];
            if (!PlayerProfile.CanMakeBait(fish)) return;
            int count = PlayerProfile.BaitFromFish(fish);
            p.Data.keepNet.RemoveAt(index);
            p.AddItem(MinnowBaitId, count);
            var name = FishDatabase.Get(fish.speciesId)?.Name;
            Game.Toast($"{name}로 생미끼 {count}개를 만들었습니다.", ToastKind.Good);
            p.NotifyChanged();
        }

        const string MinnowBaitId = "bait_minnow";

        void Eat(FoodDef food)
        {
            if (food == null) return;
            if (!Game.Profile.Eat(food, Game.Clock.TotalMinutes)) { Game.Toast("배가 부릅니다.", ToastKind.Warn); return; }
            Game.Toast($"{food.Name}을(를) 먹었습니다. {food.EffectText}", ToastKind.Good);
        }
    }
}
