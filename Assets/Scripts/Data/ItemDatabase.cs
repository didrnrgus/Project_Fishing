using System.Collections.Generic;

namespace Fishing.Data
{
    public enum ItemCategory { Bait, Food, Material }

    /// <summary>가방(I)에 들어가는 아이템 이름·설명. 미끼는 <see cref="GearDatabase.Baits"/>에서 가져온다.</summary>
    public static class ItemDatabase
    {
        static readonly Dictionary<string, (string name, string desc, ItemCategory cat)> Items = new Dictionary<string, (string, string, ItemCategory)>
        {
            { GearDatabase.KimbapId, ("김밥", $"체력 +{GearDatabase.KimbapStamina:0}. 출조의 든든한 동반자.", ItemCategory.Food) },
            { RodUpgrade.ScaleId, ("반짝이는 비늘", "큰 고기를 해체하면 나온다. 낚싯대·릴 강화 재료.", ItemCategory.Material) },
            { RodUpgrade.StarSandId, ("별모래", "강화할 때 쓰면 성공 확률이 오른다. 귀한 고기를 해체하면 가끔 나온다.", ItemCategory.Material) },
        };

        public static string Name(string id)
        {
            if (Items.TryGetValue(id, out var it)) return it.name;
            return GearDatabase.GetBait(id)?.Name ?? id;
        }

        public static string Desc(string id)
        {
            if (Items.TryGetValue(id, out var it)) return it.desc;
            var b = GearDatabase.GetBait(id);
            if (b == null) return "";
            return b.NotSold
                ? $"고기를 해체해 만든 미끼. Lv.{b.Level}부터 사용, 입질 {b.Potency * 100f - 100f:+0;-0}%"
                : $"{Names.Of(b.Kind)} · 상점 {b.Price:N0}골드 / {b.PackSize}개";
        }

        public static ItemCategory Category(string id)
        {
            if (Items.TryGetValue(id, out var it)) return it.cat;
            return ItemCategory.Bait;
        }
    }
}
