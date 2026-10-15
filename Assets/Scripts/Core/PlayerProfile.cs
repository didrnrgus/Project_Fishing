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
        /// <summary>만렙 (원작: 오픈 25 → 동남아 30·35 → 40, 2020년 55). 솔로판은 해외 거점을 열면서 40.</summary>
        public const int MaxLevel = 40;
        public const int KeepNetBase = 20;
        /// <summary>살림망 칸 수 (기본 20 + 퀘스트 확장).</summary>
        public int KeepNetSize => KeepNetBase + Data.keepNetBonus;
        public const float MaxStamina = 100f;

        public SaveData Data { get; private set; }

        public event Action Changed;
        public event Action<int> LeveledUp;

        public PlayerProfile(SaveData data) { Data = data ?? new SaveData(); }

        /// <summary>장착한 낚싯대 (강화 단계가 반영된 성능).</summary>
        public GearDef Rod => Upgraded(GearDatabase.GetGear(Data.rodId) ?? GearDatabase.GetGear("rod_basic"), ref _rodCache, ref _rodCacheLv);
        /// <summary>장착한 릴 (강화 단계가 반영된 성능).</summary>
        public GearDef Reel => Upgraded(GearDatabase.GetGear(Data.reelId) ?? GearDatabase.GetGear("reel_basic"), ref _reelCache, ref _reelCacheLv);
        GearDef _rodCache, _reelCache;
        int _rodCacheLv = -1, _reelCacheLv = -1;

        GearDef Upgraded(GearDef baseGear, ref GearDef cache, ref int cacheLv)
        {
            int lv = GearLevel(baseGear.Id);
            if (cache == null || cache.Id != baseGear.Id || cacheLv != lv)
            {
                cache = RodUpgrade.Apply(baseGear, lv);
                cacheLv = lv;
            }
            return cache;
        }

        /// <summary>장비 강화 단계 (+0 ~ +15). 장비마다 따로 저장된다.</summary>
        public int GearLevel(string gearId) => Data.rodLevels.FirstOrDefault(r => r.id == gearId)?.count ?? 0;
        public int RodLevel(string rodId) => GearLevel(rodId);

        void SetGearLevel(string gearId, int level)
        {
            var e = Data.rodLevels.FirstOrDefault(r => r.id == gearId);
            if (e == null) Data.rodLevels.Add(e = new ItemStack { id = gearId });
            e.count = Mathf.Clamp(level, 0, RodUpgrade.MaxLevel);
        }

        public GearDef EquippedBase(GearSlot slot) => GearDatabase.GetGear(slot == GearSlot.Reel ? Data.reelId : Data.rodId);

        /// <summary>장착한 낚싯대나 릴을 한 번 강화한다 (골드·비늘 소모, 별모래는 선택).</summary>
        public UpgradeResult TryUpgrade(GearSlot slot, bool useStarSand)
        {
            var gear = EquippedBase(slot);
            int lv = GearLevel(gear.Id);
            if (lv >= RodUpgrade.MaxLevel) return UpgradeResult.Max;
            int cost = RodUpgrade.Cost(gear, lv);
            int scales = RodUpgrade.Scales(lv);
            if (Data.gold < cost) return UpgradeResult.NoGold;
            if (Count(RodUpgrade.ScaleId) < scales) return UpgradeResult.NoMaterial;
            bool star = useStarSand && Count(RodUpgrade.StarSandId) > 0;
            TrySpend(cost);
            ConsumeItem(RodUpgrade.ScaleId, scales);
            if (star) ConsumeItem(RodUpgrade.StarSandId);
            UpgradeResult result;
            if (UnityEngine.Random.value < RodUpgrade.SuccessRate(lv, star)) { SetGearLevel(gear.Id, lv + 1); result = UpgradeResult.Success; }
            else if (RodUpgrade.CanDowngrade(lv) && UnityEngine.Random.value < RodUpgrade.DowngradeChance) { SetGearLevel(gear.Id, lv - 1); result = UpgradeResult.Downgrade; }
            else result = UpgradeResult.Fail;
            NotifyChanged();
            return result;
        }

        /// <summary>개발용: 낚싯대를 확실히 강화 (재료 무시).</summary>
        public void DebugUpgrade(GearSlot slot, int levels)
        {
            var gear = EquippedBase(slot);
            SetGearLevel(gear.Id, GearLevel(gear.Id) + levels);
            NotifyChanged();
        }

        /// <summary>
        /// 살림망의 고기를 해체한다 (20cm 이상). 등급에 맞는 해체 미끼, 반짝이는 비늘, 가끔 별모래.
        /// 돌려주는 값: 결과 문장.
        /// </summary>
        public string Dissect(int index)
        {
            if (index < 0 || index >= Data.keepNet.Count) return null;
            var fish = Data.keepNet[index];
            var s = FishDatabase.Get(fish.speciesId);
            if (s == null || s.IsJunk) return null;
            Data.keepNet.RemoveAt(index);
            var bait = GearDatabase.CutBaitFor(s.Rarity);
            int baits = 2 + Mathf.FloorToInt(fish.length / 20f);
            int scales = Mathf.Max(1, Mathf.RoundToInt((1 + fish.length / 15f) * (1f + s.Rarity * 0.3f)));
            float starChance = 0.03f * s.Rarity + (s.IsTrophy(fish.length) ? 0.15f : 0f);
            bool star = UnityEngine.Random.value < starChance;
            AddItem(bait.Id, baits);
            AddItem(RodUpgrade.ScaleId, scales);
            if (star) AddItem(RodUpgrade.StarSandId, 1);
            NotifyChanged();
            return $"{s.Name} 해체: {bait.Name} {baits}개, 반짝이는 비늘 {scales}개" + (star ? ", <color=#ffd060>별모래 1개!</color>" : "");
        }

        public GearDef Line => GearDatabase.GetGear(Data.lineId) ?? GearDatabase.GetGear("line_nylon");
        public GearDef Boat => string.IsNullOrEmpty(Data.boatId) ? null : GearDatabase.GetGear(Data.boatId);
        public BaitDef Bait => GearDatabase.GetBait(Data.baitId) ?? GearDatabase.Baits[0];

        public static int ExpToNext(int level) => Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f));

        /// <summary>체력 비율 0~1.</summary>
        public float StaminaRatio => StaminaCap > 0f ? Data.stamina / StaminaCap : 0f;
        /// <summary>
        /// 체력 단계 (원작 2013 공략): 0 = 괜찮음, 1 = 20% 이하(지침), 2 = 5% 이하(탈진).
        /// </summary>
        public int StaminaTier => StaminaRatio <= 0.05f ? 2 : StaminaRatio <= 0.2f ? 1 : 0;
        /// <summary>
        /// 능력치(데미지·카운터·랜딩) 배율. 원작은 20% 이하 70% 감소, 5% 이하 95% 감소로 아주 가혹해서
        /// 솔로판은 조금 누그러뜨렸다: 20% 이하 0.45, 5% 이하 0.15.
        /// </summary>
        public float StaminaFactor => StaminaTier switch { 2 => 0.15f, 1 => 0.45f, _ => 1f };
        /// <summary>릴 감는 속도 배율 (원작: 20% 이하 20% 감소, 5% 이하 대폭 감소).</summary>
        public float ReelFactor => StaminaTier switch { 2 => 0.35f, 1 => 0.8f, _ => 1f } * (HasCottonGloves ? 1.05f : 1f);
        /// <summary>파이팅 데미지 배율 (내구도 × 의상 × 목장갑).</summary>
        public float FightPower => DurabilityFactor * (1f + Costume.Damage) * (HasCottonGloves ? 1.05f : 1f);

        /// <summary>
        /// 아직 안 받은 레벨 보상을 준다 (레벨 업 때, 그리고 예전 세이브를 불러올 때 따라잡기).
        /// 받은 것들의 글을 돌려준다.
        /// </summary>
        public System.Collections.Generic.List<string> GrantLevelRewards()
        {
            var got = new System.Collections.Generic.List<string>();
            foreach (var (lv, id, count, text) in LevelRewards.All)
            {
                if (Data.level < lv || Data.levelRewards.Contains(lv)) continue;
                Data.levelRewards.Add(lv);
                if (id == CottonGlovesId) { if (!Data.specialFinds.Contains(id)) Data.specialFinds.Add(id); }
                else if (GearDatabase.GetGear(id) != null) { if (!Owns(id)) Data.ownedGear.Add(id); }
                else AddItem(id, count);
                got.Add($"Lv.{lv} 보상: {text}");
            }
            if (got.Count > 0) NotifyChanged();
            return got;
        }

        // ---- 낚시 일지 ----
        public const int JournalMax = 300;
        /// <summary>일지에 한 줄 남긴다 (게임 날짜·시각과 함께). 오래된 것부터 지운다.</summary>
        public void Log(string text)
        {
            Data.journal.Add(new JournalEntry { day = Game.Clock != null ? Game.Clock.Day : Data.clockDay, minutes = Game.Clock != null ? Game.Clock.Minutes : 0f, text = text });
            if (Data.journal.Count > JournalMax) Data.journal.RemoveRange(0, Data.journal.Count - JournalMax);
        }

        // ---- 포커싱 (파이팅 스킬) ----
        /// <summary>원작: 접속 유지 20분마다 1회.</summary>
        public const float FocusCooldownSeconds = 20f * 60f;
        public bool FocusReady => Data.focusCooldown <= 0f;
        public void UseFocus() => Data.focusCooldown = FocusCooldownSeconds;
        /// <summary>실제 시간이 흐르면 쿨타임이 준다 (GameBootstrap이 매 프레임).</summary>
        public void TickFocus(float realSeconds) { if (Data.focusCooldown > 0f) Data.focusCooldown = Mathf.Max(0f, Data.focusCooldown - realSeconds); }

        // ---- 음식 ----
        /// <summary>음식을 먹는다: 즉시 회복 + (분당 회복형이면) 게임 시간 동안 서서히 회복. 배부르면 false.</summary>
        public bool Eat(FoodDef food, float nowMinutes)
        {
            bool full = Data.stamina >= StaminaCap - 0.5f;
            if (full && food.PerMinute <= 0f) return false;
            if (!ConsumeItem(food.Id)) return false;
            ChangeStamina(food.Instant);
            if (food.PerMinute > 0f)
            {
                // 남은 분당 회복이 더 크면 그대로, 아니면 새 음식으로 바꾼다
                float left = Mathf.Max(0f, Data.regenUntil - nowMinutes) * Data.regenPerMin;
                if (food.PerMinute * food.Minutes >= left)
                {
                    Data.regenPerMin = food.PerMinute;
                    Data.regenUntil = nowMinutes + food.Minutes;
                }
            }
            NotifyChanged();
            return true;
        }

        public bool Regenerating(float nowMinutes) => nowMinutes < Data.regenUntil && Data.regenPerMin > 0f;

        /// <summary>분당 회복형 음식 효과 (GameBootstrap이 게임 시간 흐름만큼).</summary>
        public void TickRegen(float nowMinutes, float gameMinutes)
        {
            if (Regenerating(nowMinutes)) ChangeStamina(Data.regenPerMin * gameMinutes);
        }

        /// <summary>목장갑 (원작 Lv.6 보상 "미끄러지지 않는 목장갑": 힘·속도 상승).</summary>
        public const string CottonGlovesId = "acc_cottongloves";
        public bool HasCottonGloves => Data.specialFinds.Contains(CottonGlovesId);
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
                Data.stamina = StaminaCap;
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
            Data.stamina = Mathf.Clamp(Data.stamina + delta, 0f, StaminaCap);
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

        // ---- 의상 ----
        public event Action LookChanged;

        public string Worn(CostumeSlot slot)
        {
            int i = (int)slot;
            while (Data.costumes.Count <= i) Data.costumes.Add(CostumeDatabase.Defaults[Data.costumes.Count]);
            return Data.costumes[i];
        }

        public bool OwnsCostume(CostumeDef c) => CostumeDatabase.IsFree(c) || Data.ownedCostumes.Contains(c.Id);

        public void Wear(CostumeDef c)
        {
            Worn(c.Slot);
            Data.costumes[(int)c.Slot] = c.Id;
            Stamina(0f);
            NotifyChanged();
            LookChanged?.Invoke();
        }

        public void GiveCostume(string id)
        {
            if (!Data.ownedCostumes.Contains(id)) Data.ownedCostumes.Add(id);
        }

        /// <summary>입은 의상 능력치 합계.</summary>
        public CostumeStats Costume
        {
            get
            {
                var st = new CostumeStats();
                for (int i = 0; i < 6; i++)
                {
                    var c = CostumeDatabase.Get(Worn((CostumeSlot)i));
                    if (c == null) continue;
                    st.Stamina += c.Stamina; st.BigFish += c.BigFish; st.Damage += c.Damage;
                    st.Counter += c.Counter; st.Cast += c.Cast; st.Drain += c.Drain;
                }
                return st;
            }
        }

        /// <summary>최대 체력 (기본 100 + 의상).</summary>
        public float StaminaCap => MaxStamina + Costume.Stamina;

        void Stamina(float delta) => Data.stamina = Mathf.Clamp(Data.stamina + delta, 0f, StaminaCap);

        /// <summary>쓸 수 있는 칭호 (레벨 칭호 + 업적 칭호).</summary>
        public System.Collections.Generic.List<string> Titles
        {
            get
            {
                var list = new System.Collections.Generic.List<string>();
                foreach (var (lv, t) in AchievementDatabase.LevelTitles) if (Data.level >= lv) list.Add(t);
                foreach (var a in AchievementDatabase.All)
                    if (a.Title != null && Data.achievements.Contains(a.Id)) list.Add(a.Title);
                return list;
            }
        }

        /// <summary>이름표에 붙는 칭호.</summary>
        public string CurrentTitle
        {
            get
            {
                var titles = Titles;
                if (!string.IsNullOrEmpty(Data.title) && titles.Contains(Data.title)) return Data.title;
                string best = "초보 조사";
                foreach (var (lv, t) in AchievementDatabase.LevelTitles) if (Data.level >= lv) best = t;
                return best;
            }
        }

        public void SetTitle(string title)
        {
            Data.title = title;
            NotifyChanged();
            LookChanged?.Invoke();
        }

        /// <summary>입은 의상을 반영한 내 캐릭터 외형.</summary>
        public Fishing.Characters.CharacterLook BuildLook()
        {
            var look = Fishing.Characters.CharacterLook.Player;
            look.Title = CurrentTitle;
            var hat = CostumeDatabase.Get(Worn(CostumeSlot.Hat));
            look.Hat = hat != null && hat.Style >= 0;
            if (look.Hat) { look.HatColor = hat.Color; look.HatStyle = hat.Style; }
            var glass = CostumeDatabase.Get(Worn(CostumeSlot.Glasses));
            look.Glasses = glass != null && glass.Style >= 0;
            if (look.Glasses) look.GlassesColor = glass.Color;
            var top = CostumeDatabase.Get(Worn(CostumeSlot.Top));
            if (top != null) { look.Shirt = top.Color; look.TopModel = top.Model ?? look.TopModel; look.TopPattern = top.Pattern; }
            var vest = CostumeDatabase.Get(Worn(CostumeSlot.Vest));
            look.Vest = vest != null && vest.Style >= 0;
            if (look.Vest) { look.VestColor = vest.Color; look.VestModel = vest.Model ?? look.VestModel; }
            var bottom = CostumeDatabase.Get(Worn(CostumeSlot.Bottom));
            if (bottom != null) { look.Pants = bottom.Color; look.BottomModel = bottom.Model ?? look.BottomModel; look.BottomPattern = bottom.Pattern; }
            var shoes = CostumeDatabase.Get(Worn(CostumeSlot.Shoes));
            if (shoes != null) { look.Shoes = shoes.Color; look.ShoeModel = shoes.Model ?? look.ShoeModel; }
            return look;
        }

        public void SelectBait(string baitId)
        {
            var b = GearDatabase.GetBait(baitId);
            if (b != null && b.Level > Data.level)
            {
                Game.Toast($"{b.Name}은(는) Lv.{b.Level}부터 쓸 수 있습니다.", ToastKind.Warn);
                return;
            }
            Data.baitId = baitId;
            NotifyChanged();
        }

        // ---- 살림망 / 도감 ----
        public bool KeepNetFull => Data.keepNet.Count >= KeepNetSize;

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
