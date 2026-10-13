using UnityEngine;

namespace Fishing.Data
{
    public enum UpgradeResult { Success, Fail, Downgrade, Max, NoGold }

    /// <summary>
    /// 낚싯대 강화 규칙 (+1 ~ +10). 낚시점 김사장에게 골드를 내고 장착한 낚싯대를 강화한다.
    ///  - 단계마다 힘 +6%, 비거리 +0.6m
    ///  - 성공률: +1 100% → +10 20%
    ///  - +5 이상에서 실패하면 한 단계 떨어진다 (그 아래는 골드만 잃는다)
    /// </summary>
    public static class RodUpgrade
    {
        public const int MaxLevel = 10;
        const float PowerPerLevel = 0.06f;
        const float RangePerLevel = 0.6f;
        public const int DowngradeFrom = 5;

        static readonly float[] Rates = { 1f, 0.95f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f };

        /// <summary>현재 단계에서 다음 단계로 올라갈 확률.</summary>
        public static float SuccessRate(int level) => level >= MaxLevel ? 0f : Rates[Mathf.Clamp(level, 0, Rates.Length - 1)];

        public static int Cost(GearDef rod, int level)
        {
            float c = (200f + rod.Price * 0.08f) * Mathf.Pow(level + 1, 1.5f);
            return Mathf.RoundToInt(c / 10f) * 10;
        }

        public static bool FailDowngrades(int level) => level >= DowngradeFrom;

        /// <summary>강화 단계를 반영한 낚싯대 성능 (원본은 건드리지 않는다).</summary>
        public static GearDef Apply(GearDef rod, int level)
        {
            if (rod == null || level <= 0) return rod;
            return new GearDef
            {
                Id = rod.Id, Name = $"{rod.Name} +{level}", Slot = rod.Slot, Price = rod.Price, Level = rod.Level, Desc = rod.Desc,
                Power = rod.Power * (1f + PowerPerLevel * level), Flex = rod.Flex, CastRange = rod.CastRange + RangePerLevel * level,
                ReelSpeed = rod.ReelSpeed, Capacity = rod.Capacity, Strength = rod.Strength,
            };
        }
    }
}
