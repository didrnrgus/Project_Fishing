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

    /// <summary>수족관에 넣은 물고기.</summary>
    [Serializable]
    public class AquaFish
    {
        public string speciesId;
        public float length;
        public float fedAt;   // 마지막으로 먹이를 준 게임 시각 (TotalMinutes)
    }

    /// <summary>오늘의 조합 의뢰 (일일 퀘스트).</summary>
    [Serializable]
    public class DailyQuest
    {
        public int kind;          // 0 어종 N마리, 1 서식지 N마리, 2 어종 길이 이상
        public string mapId;
        public string speciesId;
        public int habitat;
        public float minLen;
        public int count;
        public int progress;
        public bool done;
        public int gold;
        public int exp;
    }

    /// <summary>오늘 대회 참가 기록과 누적 성적.</summary>
    [Serializable]
    public class TournamentSave
    {
        public int day = -1;          // 참가한 대회 날짜 (-1이면 없음)
        public string mapId;
        public bool settled;          // 결과 정산을 마쳤는지
        public int count;
        public float longest;
        public string longestFish;
        public float total;
        public int points;            // 누적 포인트 (엠블럼)
        public int played;
        public int wins;
        public int podiums;           // 1~3위 횟수
        public int bestRank;          // 최고 순위 (0이면 없음)
        public int lastRank;
        public int lastDay = -1;
    }

    [Serializable]
    public class QuestProgress
    {
        public int index;
        public int progress;
        public List<string> distinct = new List<string>();
        public bool ready; // 목표 달성, 보고 대기
    }

    /// <summary>낚시 일지 한 줄 (원작 "일지": 그날그날의 기록).</summary>
    [Serializable]
    public class JournalEntry
    {
        public int day;
        public float minutes;
        public string text;
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
        public List<string> costumes = new List<string>();
        public List<DailyQuest> daily = new List<DailyQuest>();
        public int dailyDay = -1;
        public int dailyCompleted;
        public List<string> trophyDone = new List<string>();   // 대물 도전 달성한 어종
        public List<string> achievements = new List<string>();
        public List<string> specialFinds = new List<string>(); // 로봇 물고기, 생선 캔 같은 특별한 것
        public int bestCombo;
        public float focusCooldown;          // 포커싱 다시 쓰기까지 남은 실제 시간(초)
        public float regenPerMin;            // 분당 회복형 음식: 게임 1분에 체력 +
        public float regenUntil;             // 그 효과가 끝나는 게임 시각 (TotalMinutes)
        public List<int> levelRewards = new List<int>(); // 받은 레벨 보상
        public BingoSave bingo = new BingoSave();
        public string attendLast = "";       // 마지막 출석 날짜 (실제 날짜 yyyy-MM-dd)
        public int attendStamp;              // 찍은 도장 수 (7칸마다 한 바퀴)
        public int attendTotal;
        public List<JournalEntry> journal = new List<JournalEntry>();
        public List<AquaFish> aquarium = new List<AquaFish>();
        public List<string> aquariumDecor = new List<string>();
        public string title = "";                              // 고른 칭호 (빈 문자열이면 레벨 칭호) // 마지막으로 있던 낚시터
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
        public TournamentSave tournament = new TournamentSave();

        public int clockDay = 1;
        public float clockMinutes = 6 * 60f;
        public float posX = -10f, posY = 1.1f, posZ = -70f; // 낚시점 앞 (HanRiverMap.Spawn)
        public float yaw;
    }
}
