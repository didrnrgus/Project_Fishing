using System;
using System.IO;
using System.Linq;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>플레이어 진행 상황. SaveData를 감싸서 규칙(레벨업, 재화, 아이템)을 담당한다.</summary>
    public class PlayerProfile
    {
        public const int MaxLevel = 25;
        public const int KeepNetCapacity = 20;
        public const float MaxStamina = 100f;

        public SaveData Data { get; private set; }

        public event Action Changed;
        public event Action<int> LeveledUp;

        public PlayerProfile(SaveData data) { Data = data ?? new SaveData(); }

        /// <summary>장착한 낚싯대 (강화 단계가 반영된 성능).</summary>
        public GearDef Rod
        {
            get
            {
                var baseRod = GearDatabase.GetGear(Data.rodId) ?? GearDatabase.GetGear("rod_basic");
                int lv = RodLevel(baseRod.Id);
                if (_rodCache == null || _rodCacheId != baseRod.Id || _rodCacheLevel != lv)
                {
                    _rodCache = RodUpgrade.Apply(baseRod, lv);
                    _rodCacheId = baseRod.Id;
                    _rodCacheLevel = lv;
                }
                return _rodCache;
            }
        }
        GearDef _rodCache;
        string _rodCacheId;
        int _rodCacheLevel = -1;

        public int RodLevel(string rodId) => Data.rodLevels.FirstOrDefault(r => r.id == rodId)?.count ?? 0;

        void SetRodLevel(string rodId, int level)
        {
            var e = Data.rodLevels.FirstOrDefault(r => r.id == rodId);
            if (e == null) Data.rodLevels.Add(e = new ItemStack { id = rodId });
            e.count = Mathf.Clamp(level, 0, RodUpgrade.MaxLevel);
        }

        /// <summary>장착한 낚싯대를 한 번 강화한다 (골드 소모, 확률 성공).</summary>
        public UpgradeResult TryUpgradeRod()
        {
            var rod = GearDatabase.GetGear(Data.rodId);
            int lv = RodLevel(rod.Id);
            if (lv >= RodUpgrade.MaxLevel) return UpgradeResult.Max;
            int cost = RodUpgrade.Cost(rod, lv);
            if (!TrySpend(cost)) return UpgradeResult.NoGold;
            UpgradeResult result;
            if (UnityEngine.Random.value < RodUpgrade.SuccessRate(lv)) { SetRodLevel(rod.Id, lv + 1); result = UpgradeResult.Success; }
            else if (RodUpgrade.FailDowngrades(lv)) { SetRodLevel(rod.Id, lv - 1); result = UpgradeResult.Downgrade; }
            else result = UpgradeResult.Fail;
            NotifyChanged();
            return result;
        }
        public GearDef Reel => GearDatabase.GetGear(Data.reelId) ?? GearDatabase.GetGear("reel_basic");
        public GearDef Line => GearDatabase.GetGear(Data.lineId) ?? GearDatabase.GetGear("line_nylon");
        public GearDef Boat => string.IsNullOrEmpty(Data.boatId) ? null : GearDatabase.GetGear(Data.boatId);
        public BaitDef Bait => GearDatabase.GetBait(Data.baitId) ?? GearDatabase.Baits[0];

        public static int ExpToNext(int level) => Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f));

        /// <summary>체력이 30 미만이면 지쳐서 능력치가 떨어진다.</summary>
        public float StaminaFactor => Data.stamina < 30f ? 0.65f : 1f;
        public float DurabilityFactor => Data.rodDurability <= 0f ? 0.5f : 1f;

        public void NotifyChanged() => Changed?.Invoke();

        // ---- 경험치 / 재화 ----
        public void AddExp(int amount)
        {
            if (Data.level >= MaxLevel) { NotifyChanged(); return; }
            Data.exp += amount;
            while (Data.level < MaxLevel && Data.exp >= ExpToNext(Data.level))
            {
                Data.exp -= ExpToNext(Data.level);
                Data.level++;
                Data.stamina = MaxStamina;
                LeveledUp?.Invoke(Data.level);
            }
            if (Data.level >= MaxLevel) Data.exp = 0;
            NotifyChanged();
        }

        public void AddGold(int amount) { Data.gold += amount; NotifyChanged(); }

        public bool TrySpend(int amount)
        {
            if (Data.gold < amount) return false;
            Data.gold -= amount;
            NotifyChanged();
            return true;
        }

        public void ChangeStamina(float delta)
        {
            Data.stamina = Mathf.Clamp(Data.stamina + delta, 0f, MaxStamina);
        }

        // ---- 아이템 ----
        public int Count(string id) => Data.items.FirstOrDefault(i => i.id == id)?.count ?? 0;

        public void AddItem(string id, int count)
        {
            var stack = Data.items.FirstOrDefault(i => i.id == id);
            if (stack == null) Data.items.Add(stack = new ItemStack { id = id });
            stack.count += count;
            NotifyChanged();
        }

        public bool ConsumeItem(string id, int count = 1)
        {
            var stack = Data.items.FirstOrDefault(i => i.id == id);
            if (stack == null || stack.count < count) return false;
            stack.count -= count;
            NotifyChanged();
            return true;
        }

        // ---- 장비 ----
        public bool Owns(string gearId) => Data.ownedGear.Contains(gearId);

        public void Equip(GearDef gear)
        {
            switch (gear.Slot)
            {
                case GearSlot.Rod:
                    if (Data.rodId != gear.Id) Data.rodDurability = 100f;
                    Data.rodId = gear.Id;
                    break;
                case GearSlot.Reel: Data.reelId = gear.Id; break;
                case GearSlot.Line: Data.lineId = gear.Id; break;
                case GearSlot.Boat:
                    if (Data.boatId != gear.Id) Data.boatFuel = gear.FuelCapacity; // 새 배는 연료를 가득 채워 준다
                    Data.boatId = gear.Id;
                    break;
            }
            NotifyChanged();
        }

        public void SelectBait(string baitId) { Data.baitId = baitId; NotifyChanged(); }

        // ---- 살림망 / 도감 ----
        public bool KeepNetFull => Data.keepNet.Count >= KeepNetCapacity;

        public DexEntry Dex(string speciesId) => Data.dex.FirstOrDefault(d => d.speciesId == speciesId);

        /// <summary>도감에 기록하고, 신기록이면 true.</summary>
        public bool RecordDex(FishSpecies species, float length, float weight, int day)
        {
            if (species.IsJunk) return false;
            var entry = Dex(species.Id);
            if (entry == null)
            {
                Data.dex.Add(new DexEntry { speciesId = species.Id, count = 1, maxLength = length, maxWeight = weight, firstDay = day });
                return true;
            }
            entry.count++;
            bool record = length > entry.maxLength;
            if (record) entry.maxLength = length;
            if (weight > entry.maxWeight) entry.maxWeight = weight;
            return record;
        }

        public static int SellPrice(CaughtFish fish)
        {
            var s = FishDatabase.Get(fish.speciesId);
            if (s == null || s.Price <= 0) return 0;
            return Mathf.RoundToInt(s.Price * Mathf.Pow(fish.length / s.MinLen, 1.2f));
        }

        /// <summary>요리해서 먹으면 무게에 비례해 체력을 회복한다.</summary>
        /// <summary>미끼로 만들 수 있는 작은 고기인지 (20cm 미만, 잡동사니 제외).</summary>
        public static bool CanMakeBait(CaughtFish fish)
        {
            var s = FishDatabase.Get(fish.speciesId);
            return s != null && !s.IsJunk && fish.length < BaitFishMaxLength;
        }

        public const float BaitFishMaxLength = 20f;

        /// <summary>작은 고기 한 마리로 만드는 생미끼 개수: 10cm 미만 2개, 그 이상 3개.</summary>
        public static int BaitFromFish(CaughtFish fish) => fish.length < 10f ? 2 : 3;

        public static float CookStamina(CaughtFish fish) => Mathf.Clamp(fish.weight / 25f, 5f, 60f);
    }

    public static class SaveSystem
    {
        static string PathFor => Path.Combine(Application.persistentDataPath, "save.json");

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(PathFor))
                    return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor)) ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 불러오기 실패, 새로 시작합니다: {e.Message}");
            }
            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            try
            {
                File.WriteAllText(PathFor, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 저장 실패: {e.Message}");
            }
        }

        public static void Delete()
        {
            if (File.Exists(PathFor)) File.Delete(PathFor);
        }
    }
}
