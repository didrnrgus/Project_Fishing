using UnityEngine;

namespace Fishing.Data
{
    public enum UpgradeResult { Success, Fail, Downgrade, Max, NoGold, NoMaterial }

    /// <summary>
    /// 장비 강화 규칙 (낚싯대·릴, +1 ~ +15). 원작 개발실장 인터뷰 기준:
    ///  - 낚싯대와 릴을 강화한다. 재료는 낚시로 얻고(해체해서 나온 "반짝이는 비늘"), 낚시점 주인에게 맡긴다
    ///  - 최대 15강. 실패해도 장비는 깨지지 않고, 아주 낮은 확률로만 한 단계 떨어진다 (+7 이상에서 10%)
    ///  - 별모래를 쓰면 성공 확률이 오른다 (원작에선 캐시 아이템)
    /// 단계별 성능: 낚싯대 힘 +5%·비거리 +0.4m, 릴 감기 속도 +4%·권사량 +5m.
    /// </summary>
    public static class RodUpgrade
    {
        public const int MaxLevel = 15;
        public const int DowngradeFrom = 7;
        public const float DowngradeChance = 0.1f;
        public const float StarSandBonus = 0.15f;
        public const string ScaleId = "mat_scale";
        public const string StarSandId = "mat_starsand";

        static readonly float[] Rates = { 1f, 0.95f, 0.9f, 0.85f, 0.8f, 0.7f, 0.62f, 0.55f, 0.48f, 0.42f, 0.36f, 0.3f, 0.25f, 0.2f, 0.15f };

        /// <summary>현재 단계에서 다음 단계로 올라갈 확률 (별모래 포함).</summary>
        public static float SuccessRate(int level, bool starSand = false)
        {
            if (level >= MaxLevel) return 0f;
            float r = Rates[Mathf.Clamp(level, 0, Rates.Length - 1)];
            return starSand ? Mathf.Min(0.95f, r + StarSandBonus) : r;
        }

        public static int Cost(GearDef gear, int level)
        {
            float c = (150f + gear.Price * 0.05f) * Mathf.Pow(level + 1, 1.5f);
            return Mathf.RoundToInt(c / 10f) * 10;
        }

        /// <summary>필요한 반짝이는 비늘 수.</summary>
        public static int Scales(int level) => 1 + level / 2;

        public static bool CanDowngrade(int level) => level >= DowngradeFrom;

        /// <summary>강화 단계를 반영한 성능 (원본은 건드리지 않는다).</summary>
        public static GearDef Apply(GearDef gear, int level)
        {
            if (gear == null || level <= 0) return gear;
            var g = new GearDef
            {
                Id = gear.Id, Name = $"{gear.Name} +{level}", Slot = gear.Slot, Price = gear.Price, Level = gear.Level, Desc = gear.Desc,
                Power = gear.Power, Flex = gear.Flex, CastRange = gear.CastRange,
                ReelSpeed = gear.ReelSpeed, Capacity = gear.Capacity, Strength = gear.Strength,
                BoatSpeed = gear.BoatSpeed, FuelCapacity = gear.FuelCapacity, FuelPerKm = gear.FuelPerKm,
            };
            if (gear.Slot == GearSlot.Rod)
            {
                g.Power = gear.Power * (1f + 0.05f * level);
                g.CastRange = gear.CastRange + 0.4f * level;
            }
            else if (gear.Slot == GearSlot.Reel)
            {
                g.ReelSpeed = gear.ReelSpeed * (1f + 0.04f * level);
                g.Capacity = gear.Capacity + 5f * level;
            }
            return g;
        }

        public static bool Upgradable(GearSlot slot) => slot == GearSlot.Rod || slot == GearSlot.Reel;
    }
}
