using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fishing.Data
{
    public enum CostumeSlot { Hat, Glasses, Top, Vest, Bottom, Shoes }

    /// <summary>
    /// 의상 한 벌. 원작은 의상이 100~200종이었고 일부는 능력치가 있었다(선글라스가 HP를 조금 올림, 퀘스트 보상 티셔츠는 능력치 없음).
    /// 솔로판은 부위별로 능력치를 나눴다.
    /// </summary>
    public sealed class CostumeDef
    {
        public string Id;
        public string Name;
        public CostumeSlot Slot;
        public Color Color = Color.white;
        /// <summary>모자 모양: 0 벙거지, 1 야구모자, 2 밀짚모자. -1이면 모자/조끼/안경을 벗는다.</summary>
        public int Style;
        public int Price;
        public int Level = 1;
        /// <summary>상점에서 팔지 않음 (퀘스트·대회 보상).</summary>
        public bool Reward;
        public string Desc = "";

        // 능력치
        public float Stamina;     // 최대 체력 +
        public float BigFish;     // 대물 확률 +(0.03 = 3%)
        public float Damage;      // 파이팅 데미지 +%
        public float Counter;     // 카운터 데미지 +%
        public float Cast;        // 비거리 +m
        public float Drain;       // 체력 소모 -%

        public string StatLine
        {
            get
            {
                var parts = new List<string>();
                if (Stamina > 0) parts.Add($"체력 +{Stamina:0}");
                if (BigFish > 0) parts.Add($"대물 +{BigFish * 100f:0}%");
                if (Damage > 0) parts.Add($"데미지 +{Damage * 100f:0}%");
                if (Counter > 0) parts.Add($"카운터 +{Counter * 100f:0}%");
                if (Cast > 0) parts.Add($"비거리 +{Cast:0.0}m");
                if (Drain > 0) parts.Add($"체력 소모 -{Drain * 100f:0}%");
                return parts.Count == 0 ? "능력치 없음" : string.Join(" · ", parts);
            }
        }
    }

    public struct CostumeStats
    {
        public float Stamina, BigFish, Damage, Counter, Cast, Drain;
    }

    public static class CostumeDatabase
    {
        static readonly Color Beige = new Color(0.9f, 0.84f, 0.62f);

        public static readonly List<CostumeDef> All = new List<CostumeDef>
        {
            // 모자
            new CostumeDef { Id = "hat_bucket", Name = "기본 벙거지", Slot = CostumeSlot.Hat, Color = Beige, Style = 0, Desc = "조합에서 처음 받은 벙거지" },
            new CostumeDef { Id = "hat_none", Name = "모자 벗기", Slot = CostumeSlot.Hat, Style = -1 },
            new CostumeDef { Id = "hat_straw", Name = "밀짚모자", Slot = CostumeSlot.Hat, Color = new Color(0.93f, 0.82f, 0.5f), Style = 2, Price = 2000, Level = 2, Stamina = 5 },
            new CostumeDef { Id = "hat_cap", Name = "조합 야구모자", Slot = CostumeSlot.Hat, Color = new Color(0.15f, 0.25f, 0.5f), Style = 1, Price = 3500, Level = 4, BigFish = 0.03f },
            new CostumeDef { Id = "hat_khaki", Name = "카키 벙거지", Slot = CostumeSlot.Hat, Color = new Color(0.5f, 0.52f, 0.35f), Style = 0, Price = 6000, Level = 8, Stamina = 8, BigFish = 0.02f },
            new CostumeDef { Id = "hat_red", Name = "갯바위 레드캡", Slot = CostumeSlot.Hat, Color = new Color(0.8f, 0.15f, 0.12f), Style = 1, Price = 14000, Level = 12, BigFish = 0.05f, Counter = 0.03f },
            new CostumeDef { Id = "hat_conch", Name = "소라고동 모자", Slot = CostumeSlot.Hat, Color = new Color(0.95f, 0.85f, 0.75f), Style = 2, Reward = true,
                Stamina = 6, BigFish = 0.04f, Desc = "소라고동 교환소" },
            // 안경
            new CostumeDef { Id = "glass_none", Name = "안경 벗기", Slot = CostumeSlot.Glasses, Style = -1 },
            new CostumeDef { Id = "glass_sun", Name = "편광 선글라스", Slot = CostumeSlot.Glasses, Color = new Color(0.08f, 0.08f, 0.1f), Price = 4000, Level = 3, Stamina = 6,
                Desc = "원작에서도 선글라스는 HP를 조금 올려 줬다" },
            new CostumeDef { Id = "glass_pro", Name = "프로 미러 선글라스", Slot = CostumeSlot.Glasses, Color = new Color(0.2f, 0.45f, 0.7f), Price = 15000, Level = 12, Stamina = 10, BigFish = 0.03f },
            // 상의 (셔츠)
            new CostumeDef { Id = "top_white", Name = "흰 반팔 티", Slot = CostumeSlot.Top, Color = new Color(0.92f, 0.92f, 0.94f) },
            new CostumeDef { Id = "top_check", Name = "체크 남방", Slot = CostumeSlot.Top, Color = new Color(0.65f, 0.25f, 0.22f), Price = 2500, Level = 2, Drain = 0.05f },
            new CostumeDef { Id = "top_blue", Name = "한강 블루 셔츠", Slot = CostumeSlot.Top, Color = new Color(0.3f, 0.5f, 0.85f), Price = 5000, Level = 5, BigFish = 0.03f },
            new CostumeDef { Id = "top_rash", Name = "래시가드", Slot = CostumeSlot.Top, Color = new Color(0.12f, 0.14f, 0.2f), Price = 9000, Level = 9, Stamina = 8, Drain = 0.05f },
            new CostumeDef { Id = "top_union", Name = "조합 기념 티셔츠", Slot = CostumeSlot.Top, Color = new Color(0.95f, 0.75f, 0.2f), Reward = true,
                Desc = "자이언트 그루퍼 퀘스트 보상. 원작처럼 능력치는 없다" },
            new CostumeDef { Id = "top_conch", Name = "소라 무늬 셔츠", Slot = CostumeSlot.Top, Color = new Color(0.95f, 0.6f, 0.55f), Reward = true,
                BigFish = 0.04f, Drain = 0.05f, Desc = "소라고동 교환소" },
            // 조끼
            new CostumeDef { Id = "vest_green", Name = "낚시 조끼", Slot = CostumeSlot.Vest, Color = new Color(0.25f, 0.4f, 0.3f) },
            new CostumeDef { Id = "vest_none", Name = "조끼 벗기", Slot = CostumeSlot.Vest, Style = -1 },
            new CostumeDef { Id = "vest_life", Name = "구명조끼", Slot = CostumeSlot.Vest, Color = new Color(0.95f, 0.45f, 0.1f), Price = 3000, Level = 5, Stamina = 10, Desc = "바다에선 안전이 먼저" },
            new CostumeDef { Id = "vest_pro", Name = "프로 낚시 조끼", Slot = CostumeSlot.Vest, Color = new Color(0.55f, 0.52f, 0.38f), Price = 12000, Level = 10, Damage = 0.05f, Counter = 0.05f },
            // 하의
            new CostumeDef { Id = "bottom_cargo", Name = "카고 바지", Slot = CostumeSlot.Bottom, Color = new Color(0.32f, 0.36f, 0.3f) },
            new CostumeDef { Id = "bottom_jeans", Name = "청바지", Slot = CostumeSlot.Bottom, Color = new Color(0.22f, 0.3f, 0.5f), Price = 2000, Level = 2, Damage = 0.02f },
            new CostumeDef { Id = "bottom_wader", Name = "방수 웨이더", Slot = CostumeSlot.Bottom, Color = new Color(0.2f, 0.28f, 0.22f), Price = 8000, Level = 7, Drain = 0.08f },
            new CostumeDef { Id = "bottom_tech", Name = "블랙 테크 팬츠", Slot = CostumeSlot.Bottom, Color = new Color(0.1f, 0.1f, 0.12f), Price = 16000, Level = 13, Damage = 0.05f, Counter = 0.05f },
            // 신발
            new CostumeDef { Id = "shoes_sneaker", Name = "운동화", Slot = CostumeSlot.Shoes, Color = new Color(0.2f, 0.2f, 0.22f) },
            new CostumeDef { Id = "shoes_boots", Name = "고무 장화", Slot = CostumeSlot.Shoes, Color = new Color(0.15f, 0.25f, 0.2f), Price = 2500, Level = 3, Cast = 0.5f },
            new CostumeDef { Id = "shoes_spike", Name = "갯바위 스파이크화", Slot = CostumeSlot.Shoes, Color = new Color(0.6f, 0.6f, 0.62f), Price = 10000, Level = 8, Cast = 1.2f, Stamina = 5 },
            new CostumeDef { Id = "shoes_gold", Name = "황금 낚시화", Slot = CostumeSlot.Shoes, Color = new Color(0.95f, 0.75f, 0.2f), Price = 30000, Level = 18, Cast = 2f, BigFish = 0.03f },
        };

        /// <summary>처음부터 가진 기본 의상 (부위 순서대로).</summary>
        public static readonly string[] Defaults = { "hat_bucket", "glass_none", "top_white", "vest_green", "bottom_cargo", "shoes_sneaker" };

        static Dictionary<string, CostumeDef> _byId;
        public static CostumeDef Get(string id)
        {
            _byId ??= All.ToDictionary(c => c.Id);
            return id != null && _byId.TryGetValue(id, out var c) ? c : null;
        }

        public static IEnumerable<CostumeDef> InSlot(CostumeSlot slot) => All.Where(c => c.Slot == slot);
        public static bool IsFree(CostumeDef c) => c.Price == 0 && !c.Reward;
    }
}
