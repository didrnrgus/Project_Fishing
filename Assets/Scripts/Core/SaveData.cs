using System;
using System.Collections.Generic;

namespace Fishing.Core
{
    [Serializable]
    public class CaughtFish
    {
        public string speciesId;
        public float length;
        public float weight;
        public int day;
        public string zone;
    }

    [Serializable]
    public class DexEntry
    {
        public string speciesId;
        public int count;
        public float maxLength;
        public float maxWeight;
        public int firstDay;
    }

    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class QuestProgress
    {
        public int index;
        public int progress;
        public List<string> distinct = new List<string>();
        public bool ready; // 목표 달성, 보고 대기
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int level = 1;
        public int exp;
        public int gold = 3000;
        public float stamina = 100f;
        public int totalCatches;
        public bool showTips = true; // [설명 끄기 : U]

        public string rodId = "rod_basic";
        public string reelId = "reel_basic";
        public string lineId = "line_nylon";
        public string baitId = "bait_worm";
        public float rodDurability = 100f;
        public string mapId = "hangang";
        public int keepNetBonus;         // 퀘스트로 늘어난 살림망 칸
        public List<string> visitedMaps = new List<string>();
        public List<string> ownedCostumes = new List<string>();
        /// <summary>입은 의상 (CostumeSlot 순서: 모자, 안경, 상의, 조끼, 하의, 신발).</summary>
        public List<string> costumes = new List<string>(); // 마지막으로 있던 낚시터
        public string boatId = "";      // 장착한 배 (없으면 빈 문자열)
        public float boatFuel;          // 남은 연료 L
        public List<ItemStack> rodLevels = new List<ItemStack>(); // 낚싯대 강화 단계 (id, count = +N)
        public List<string> ownedGear = new List<string> { "rod_basic", "reel_basic", "line_nylon" };
        public List<ItemStack> items = new List<ItemStack>
        {
            new ItemStack { id = "bait_worm", count = 30 },
            new ItemStack { id = "bait_paste", count = 10 },
            new ItemStack { id = "food_kimbap", count = 2 },
        };

        public List<CaughtFish> keepNet = new List<CaughtFish>();
        public List<DexEntry> dex = new List<DexEntry>();
        public QuestProgress quest = new QuestProgress();

        public int clockDay = 1;
        public float clockMinutes = 6 * 60f;
        public float posX = -10f, posY = 1.1f, posZ = -70f; // 낚시점 앞 (HanRiverMap.Spawn)
        public float yaw;
    }
}
