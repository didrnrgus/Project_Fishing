using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>살림망: 보관한 물고기 목록. 요리해서 먹으면 체력을 회복한다.</summary>
    public class KeepNetPanel : UIPanel
    {
        public KeepNetPanel(Transform canvas)
        {
            BuildFrame(canvas, "KeepNetPanel", "살림망", new Vector2(760, 800));
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var net = p.Data.keepNet;
            Title.text = $"살림망  ({net.Count}/{PlayerProfile.KeepNetCapacity})";
            UIFactory.ClearChildren(Body);

            var rows = new RowCursor(Body);
            var header = rows.Next(24);
            Cell(header, "어종", 8, 200, 15, UIFactory.TextDim);
            Cell(header, "길이", 210, 100, 15, UIFactory.TextDim);
            Cell(header, "무게", 310, 110, 15, UIFactory.TextDim);
            Cell(header, "판매가", 420, 110, 15, UIFactory.TextDim);

            if (net.Count == 0)
            {
                Cell(rows.Next(60), "비어 있습니다. 물고기를 낚아 보관해 보세요.", 8, 680, 18, UIFactory.TextDim);
            }

            int total = 0;
            for (int i = 0; i < net.Count; i++)
            {
                var fish = net[i];
                var s = FishDatabase.Get(fish.speciesId);
                int price = PlayerProfile.SellPrice(fish);
                total += price;
                var row = rows.Next(26, 3);
                UIFactory.Image(row, new Color(1, 1, 1, i % 2 == 0 ? 0.04f : 0f), null, "Stripe").rectTransform.Fill();
                bool trophy = s.IsTrophy(fish.length);
                Cell(row, (trophy ? "★ " : "") + s.Name, 8, 200, 18, trophy ? UIFactory.Gold : UIFactory.TextMain);
                Cell(row, $"{fish.length:0.0}cm", 210, 100);
                Cell(row, fish.weight >= 1000 ? $"{fish.weight / 1000f:0.00}kg" : $"{fish.weight:0}g", 310, 110);
                Cell(row, $"{price:N0}", 420, 110, 18, UIFactory.Gold);
                int index = i;
                RowButton(row, $"요리 (+{PlayerProfile.CookStamina(fish):0})", () => Cook(index), 540, 160, new Color(0.45f, 0.3f, 0.15f));
            }

            var footer = rows.Next(50, 14);
            Cell(footer, $"합계 {total:N0}골드  ·  판매는 낚시점에서", 8, 360, 18, UIFactory.TextDim);
            int kimbap = p.Count(GearDatabase.KimbapId);
            RowButton(footer, $"김밥 먹기 ({kimbap}개)", EatKimbap, 450, 250, new Color(0.25f, 0.45f, 0.25f), kimbap > 0, 19);
        }

        void Cook(int index)
        {
            var p = Game.Profile;
            if (index >= p.Data.keepNet.Count) return;
            var fish = p.Data.keepNet[index];
            if (p.Data.stamina >= PlayerProfile.MaxStamina - 0.5f)
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

        void EatKimbap()
        {
            var p = Game.Profile;
            if (p.Data.stamina >= PlayerProfile.MaxStamina - 0.5f)
            {
                Game.Toast("배가 부릅니다.", ToastKind.Warn);
                return;
            }
            if (!p.ConsumeItem(GearDatabase.KimbapId)) return;
            p.ChangeStamina(GearDatabase.KimbapStamina);
            Game.Toast($"김밥을 먹었습니다. 체력 +{GearDatabase.KimbapStamina:0}", ToastKind.Good);
            p.NotifyChanged();
        }
    }
}
