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
        /// <summary>낚은 낚시터 (생태조사 업적: "○○을 홍현 앞바다에서 납품하기"). 예전 세이브는 빈 문자열.</summary>
        public string mapId = "";
    }

    /// <summary>낚시터·어종별 납품 마릿수 (원작 생태조사).</summary>
    [Serializable]
    public class DeliveryRecord
    {
        public string mapId;
        public string speciesId;
        public int count;
    }

    /// <summary>기록 책 업적의 완성 날짜와 보상 수령 여부 (생태조사·납품·방생 같은 GOLD 보상 업적).</summary>
    [Serializable]
    public class AchClaim
    {
        public string id;
        public string doneAt;   // "2026/10/19 23:26" (실제 날짜)
        public bool claimed;
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
        public bool accepted;     // 원작 일일반복: 그 낚시터 조합 연락관에게 "수락"해야 센다
        public bool ready;        // 목표 달성, 보고 대기
    }

    /// <summary>수락한 퀘스트 하나 (이야기·가이드·일반). 일일반복은 <see cref="DailyQuest"/>에 따로 적는다.</summary>
    [Serializable]
    public class ActiveQuest
    {
        public string id;
        public int progress;
        public bool ready;        // 목표 달성, [보고자]에게 가면 완료
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
        /// <summary>이야기 퀘스트 진행 위치 (QuestDatabase.Story에서 다음에 할 퀘스트 번호 = 끝낸 이야기 수).</summary>
        public int index;
        // 예전(ver 0) 저장: 지금 이야기 퀘스트의 진행·보고 대기. ver 1부터는 active에 적는다
        public int progress;
        public List<string> distinct = new List<string>();
        public bool ready; // 목표 달성, 보고 대기

        /// <summary>0 = 예전 저장(이야기 퀘스트를 자동으로 받던 방식, index가 예전 45개 목록 기준), 1 = NPC에게 수락하는 방식.</summary>
        public int ver;
        /// <summary>수락해서 진행 중인 이야기·가이드·일반 퀘스트.</summary>
        public List<ActiveQuest> active = new List<ActiveQuest>();
        /// <summary>끝낸 가이드·일반 퀘스트 id (한 번만 하는 퀘스트).</summary>
        public List<string> done = new List<string>();
        /// <summary>완료한 퀘스트 수 (이야기·가이드·일반·일일반복 모두).</summary>
        public int completedTotal;
        /// <summary>HUD QUEST LIST: [현재 낚시터]에서 가능한 퀘스트만 보기.</summary>
        public bool hereOnly;
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
        /// <summary>릴 내구도 (원작 실패 경고 "릴 내구도 감소 : 2.00 %"). 0이면 릴 속도가 떨어진다.</summary>
        public float reelDurability = 100f;
        /// <summary>줄이 끊겨 잃은 길이 m (원작 "줄 길이 감소 : 7.00m"). 수리하면 다시 감는다.</summary>
        public float lineLost;
        /// <summary>결과 화면 보상 토스트·재도전·조준 업적 (Core/CatchRewards.cs).</summary>
        public RewardSave rewards = new RewardSave();
        public string mapId = "hangang";
        public int keepNetBonus;         // 퀘스트로 늘어난 살림망 칸
        public List<string> visitedMaps = new List<string>();
        public List<string> ownedCostumes = new List<string>();
        /// <summary>입은 의상 (CostumeSlot 순서: 모자, 안경, 상의, 조끼, 하의, 신발).</summary>
        public List<string> costumes = new List<string>();
        /// <summary>내 캐릭터 모델: 비우면 기본 캐릭터(characters.json의 playerMale/playerFemale), 아니면 VRM id ("user/파일.vrm").</summary>
        public string vrm = "";
        /// <summary>여자: 기본 캐릭터면 여자 모델(characters.json playerFemale), 고른 VRM이면 여자 걸음걸이. 주인공은 여자가 기본 (2026-10-21 사용자 결정).</summary>
        public bool vrmFemale = true;
        /// <summary>1 = 주인공 기본을 여자로 바꾼 뒤의 세이브. 그 전 세이브는 불러올 때 한 번 여자로 바꾼다 (GameBootstrap).</summary>
        public int lookVersion;
        /// <summary>입은 옷: 다른 VRM의 옷(VRM id, VrmOutfit). "" = characters.json 기본(주인공은 흰 원피스), "own" = 모델 제 옷. 끄고 켜도 그대로 (2026-10-21).</summary>
        public string outfit = "";
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

        // 납품 (원작 납품소 DELIVERY): 낚시터·어종별 납품 기록, 누적 마릿수, 납품 게이지(0~1, 가득 차면 다음 납품 X2)
        public List<DeliveryRecord> deliveries = new List<DeliveryRecord>();
        public int deliveredTotal;
        public float deliveryGauge;
        public int releasedTotal;                                    // 방생한 마릿수 (업적 방생)
        public List<AchClaim> achClaims = new List<AchClaim>();      // GOLD 보상 업적 (생태조사·납품·방생)

        public List<CaughtFish> keepNet = new List<CaughtFish>();
        public List<TrapSave> traps = new List<TrapSave>();   // 설치한 통발 (Core/FishTraps.cs, 최대 2개)
        public List<DexEntry> dex = new List<DexEntry>();
        public QuestProgress quest = new QuestProgress();
        public TournamentSave tournament = new TournamentSave();

        public int clockDay = 1;
        public float clockMinutes = 6 * 60f;
        public float posX = -10f, posY = 1.1f, posZ = -70f; // 낚시점 앞 (HanRiverMap.Spawn)
        public float yaw;
    }

    /// <summary>원작 RESULT 오른쪽 토스트(오늘 단위 업적·기록)와 재도전(복수혈전), 조준 중 업적 3줄의 진행 상태.</summary>
    [Serializable]
    public class RewardSave
    {
        public int day = -1;                 // 오늘 카운터의 기준 게임 날짜
        public int midToday;                 // 오늘 낚은 중급(파랑) 물고기
        public bool highToday;               // 오늘 고급(보라 이상)을 낚았다
        public int trophyTotal, landedTotal, counterFights;   // 조준 업적 진행
        public int trophyTier, landedTier, counterTier;       // 받은 단계
        public int lastGrade = -1, gradeStreak, bestGradeStreak;
        public int bestCounters;             // 한 파이팅 최다 카운터
        public int dexTier;                  // "N가지 물고기" 받은 단계
        public List<string> firstTrophy = new List<string>();  // 첫 월척(대물)을 받은 어종
        // 재도전: 놓친 물고기 (빈 문자열이면 없음). mult = 다시 낚았을 때 EXP 배율, armed = 재도전을 눌렀다
        public string revengeId = "";
        public float revengeLen;
        public string revengeMap = "";
        public int revengeMult;
        public bool revengeArmed;
    }
}
