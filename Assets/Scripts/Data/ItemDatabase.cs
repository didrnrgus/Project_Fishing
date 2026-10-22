using System.Collections.Generic;

namespace Fishing.Data
{
    public enum ItemCategory { Bait, Food, Material }

    /// <summary>가방(I)에 들어가는 아이템 이름·설명. 미끼는 <see cref="GearDatabase.Baits"/>에서 가져온다.</summary>
    public static class ItemDatabase
    {
        public const string BigCoinId = "coin_bigfish";

        static readonly Dictionary<string, (string name, string desc, ItemCategory cat)> Items = new Dictionary<string, (string, string, ItemCategory)>
        {
            { BigCoinId, ("대물 코인", "괴물 어종(괴물잉어·괴물가물치·폭군삼치·황새치)을 낚으면 받는 금화. 조합 교환소에서 풍림화산 채비로 바꾼다.", ItemCategory.Material) },
            { GearDatabase.KimbapId, ("김밥", $"체력 +{GearDatabase.KimbapStamina:0}. 출조의 든든한 동반자.", ItemCategory.Food) },
            { RodUpgrade.ScaleId, ("반짝이는 비늘", "큰 고기를 해체하면 나온다. 낚싯대·릴 강화 재료.", ItemCategory.Material) },
            { RodUpgrade.StarSandId, ("별모래", "강화할 때 쓰면 성공 확률이 오른다. 귀한 고기를 해체하면 가끔 나온다.", ItemCategory.Material) },
            { VillageTrade.NapalId, ("나팔고동", "한국 낚시터 마을 어부에게 상점에 없는 낚싯대·릴·줄로 바꿀 수 있는 귀한 고동. 한국 일일 의뢰·대물 도전·조합 낚시 대회·빙고로 모은다.", ItemCategory.Material) },
            { VillageTrade.GoldGegeId, ("황금거거", "금빛으로 빛나는 대왕조개 껍데기. 해외 낚시터 마을 어부에게 상점에 없는 낚싯대·릴·줄로 바꿀 수 있다. 해외 일일 의뢰·대물 도전·빙고로 모은다.", ItemCategory.Material) },
            { AchievementDatabase.ConchId, ("소라고동", "도감·대물 도전·일일 의뢰·업적으로 모은다. 조합 연락관에게 귀한 물건으로 바꿀 수 있다 (원작: 도감을 채우면 소라고동).", ItemCategory.Material) },
        };

        public static string Name(string id)
        {
            var food = GearDatabase.GetFood(id);
            if (food != null) return food.Name;
            if (Items.TryGetValue(id, out var it)) return it.name;
            return GearDatabase.GetBait(id)?.Name ?? id;
        }

        public static string Desc(string id)
        {
            var food = GearDatabase.GetFood(id);
            if (food != null) return food.Desc;
            if (Items.TryGetValue(id, out var it)) return it.desc;
            var b = GearDatabase.GetBait(id);
            if (b == null) return "";
            return b.NotSold
                ? $"고기를 해체해 만든 미끼. Lv.{b.Level}부터 사용, 입질 {b.Potency * 100f - 100f:+0;-0}%"
                : $"{Names.Of(b.Kind)} · 상점 {b.Price:N0}골드 / {b.PackSize}개";
        }

        public static ItemCategory Category(string id)
        {
            if (GearDatabase.GetFood(id) != null) return ItemCategory.Food;
            if (Items.TryGetValue(id, out var it)) return it.cat;
            return ItemCategory.Bait;
        }
    }
}
