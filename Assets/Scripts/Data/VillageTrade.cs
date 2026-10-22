using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    /// <summary>
    /// 마을 교환 (원작: 골드로 사는 상점 장비와 따로, 마을 NPC에게서만 구하는 낚싯대·릴·줄이 있었다.
    /// 한국 낚시터에서는 나팔고동, 해외 낚시터에서는 황금거거를 내고 바꾼다. 같은 레벨 상점 장비보다 능력치가 좋다).
    /// 낚시터마다 마을 어부가 그 마을 장비 하나를 내준다 (<see cref="GearDef.TradeMap"/>).
    ///
    /// 재화 얻는 곳: 그 지역 일일 의뢰 1 · 그 지역 대물 도전 2 · 조합 낚시 대회 1~3위 5/3/2 (나팔고동) · 빙고 3줄 (Lv.25 전 나팔고동 8, 뒤 황금거거 6).
    /// </summary>
    public static class VillageTrade
    {
        public const string NapalId = "mat_napal";
        public const string GoldGegeId = "mat_goldgege";

        public const int DailyTokens = 1, TrophyTokens = 2;
        public static readonly int[] TournamentTokens = { 5, 3, 2 };

        /// <summary>그 낚시터에서 쓰는 교환 재화: 한국 거점이면 나팔고동, 해외면 황금거거.</summary>
        public static string CurrencyFor(string mapId) => IsKorea(mapId) ? NapalId : GoldGegeId;

        public static bool IsKorea(string mapId) => FishingAreas.Korea.Any(a => a.MapId == mapId) || mapId == null;

        public static string Name(string currencyId) => currencyId == GoldGegeId ? "황금거거" : "나팔고동";

        /// <summary>이 마을 어부가 내주는 장비.</summary>
        public static IEnumerable<GearDef> OffersAt(string mapId) => GearDatabase.Gear.Where(g => g.TradeMap == mapId);

        /// <summary>같은 재화로 바꾸는 모든 마을 장비 (입장 레벨 순).</summary>
        public static IEnumerable<GearDef> OffersFor(string currencyId) =>
            GearDatabase.Gear.Where(g => g.TradeMap != null && CurrencyFor(g.TradeMap) == currencyId).OrderBy(g => g.Level);

        public static string MapName(string mapId) => FishingAreas.All.FirstOrDefault(a => a.MapId == mapId)?.Name ?? mapId;
    }
}
