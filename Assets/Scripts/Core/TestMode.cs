using System.Linq;
using Fishing.Data;

namespace Fishing.Core
{
    /// <summary>
    /// 직접 해 보며 확인하는 시험 모드 (2026-10-27 사용자 요청: 레벨에 관계없이 맵을 다 열고 가장 좋은 장비로).
    /// 켜 두면 낚시터·거점·미끼의 레벨 제한을 건너뛰고, 시작할 때 가장 좋은 장비를 주고 끝까지 강화해 장착한다.
    /// 자동 촬영(-autoshot)에는 쓰지 않는다 — 스크린샷은 보통 게임 그대로. 끝나면 UnlockAll = false.
    /// </summary>
    public static class TestMode
    {
        public static bool UnlockAll = true;

        /// <summary>레벨 lv가 필요한 것을 지금 쓸 수 있는지 (시험 모드면 늘 true).</summary>
        public static bool LevelOk(int lv) => UnlockAll || Game.Profile.Data.level >= lv;

        /// <summary>장비를 모두 주고 칸마다 가장 좋은 것(낚싯대 힘·릴 감기·원줄 강도, 배는 크루저)을 +15로 장착, 미끼는 99개씩.</summary>
        public static void GrantBestGear(PlayerProfile p)
        {
            var d = p.Data;
            foreach (var g in GearDatabase.Gear)
                if (!d.ownedGear.Contains(g.Id)) d.ownedGear.Add(g.Id);

            var rod = GearDatabase.Gear.Where(g => g.Slot == GearSlot.Rod).OrderByDescending(g => g.Power).First();
            var reel = GearDatabase.Gear.Where(g => g.Slot == GearSlot.Reel).OrderByDescending(g => g.ReelSpeed).First();
            var line = GearDatabase.Gear.Where(g => g.Slot == GearSlot.Line).OrderByDescending(g => g.Strength).First();
            p.Equip(rod);
            p.Equip(reel);
            p.Equip(line);
            p.Equip(GearDatabase.GetGear("boat_cruiser"));
            d.rodDurability = d.reelDurability = 100f;
            d.lineLost = 0f;
            d.boatFuel = GearDatabase.GetGear("boat_cruiser").FuelCapacity;
            foreach (var id in new[] { rod.Id, reel.Id })
            {
                var e = d.rodLevels.FirstOrDefault(r => r.id == id);
                if (e == null) d.rodLevels.Add(e = new ItemStack { id = id });
                e.count = RodUpgrade.MaxLevel;
            }

            foreach (var b in GearDatabase.Baits)
            {
                var s = d.items.FirstOrDefault(i => i.id == b.Id);
                if (s == null) d.items.Add(s = new ItemStack { id = b.Id });
                if (s.count < 99) s.count = 99;
            }
            p.NotifyChanged();
        }
    }
}
