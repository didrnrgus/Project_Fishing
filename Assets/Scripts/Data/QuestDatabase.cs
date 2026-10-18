using System.Collections.Generic;

namespace Fishing.Data
{
    public enum QuestGoal
    {
        CatchAny, CatchSpecies, DistinctSpecies, CatchInHabitat, CatchSpeciesInBand, CatchMinLength,
        /// <summary>[보고자] NPC를 만나기 (말을 걸면 바로 완료).</summary>
        Meet,
        /// <summary>퀘스트 물건을 받아 [보고자]에게 전달하기 (수락하면 물건을 받는다).</summary>
        Deliver,
        /// <summary>물고기 방생해보기.</summary>
        Release,
        /// <summary>납품소에서 납품하기 (SpeciesId·MapId가 있으면 그 어종을 그 낚시터에서).</summary>
        DeliverFish,
        /// <summary>통발 설치해보기.</summary>
        InstallTrap,
    }

    /// <summary>원작 퀘스트 종류: 이야기(STORY, 보라) · 일일반복(REPEAT, 파랑) · 가이드(GUIDE, 연보라) · 일반(흰색).</summary>
    public enum QuestType { Story, Repeat, Guide, Normal }

    public sealed class QuestDef
    {
        /// <summary>저장에 쓰는 고유 이름. 예전 이야기 퀘스트는 st00~st44 (예전 목록 순서), 새로 넣은 건 이름으로.</summary>
        public string Id;
        public QuestType Type = QuestType.Story;
        /// <summary>원작 Lv.N 표시 (권장 레벨).</summary>
        public int Lv = 1;
        /// <summary>퀘스트를 하는 낚시터 (HUD "[현재 낚시터]에서 가능한 퀘스트만 보기", 힌트맵). null이면 대기실.</summary>
        public string MapId;
        /// <summary>true면 MapId 낚시터에서 낚은 것만 센다 (일일반복).</summary>
        public bool RequireMap;
        /// <summary>퀘스트를 주는 NPC (<see cref="QuestNpcs"/> 키).</summary>
        public string Giver;
        /// <summary>[보고자]: 목표를 채우고 찾아가서 완료하는 NPC.</summary>
        public string Reporter;
        /// <summary>전달할 퀘스트 물건 이름 (Deliver).</summary>
        public string ItemName;
        /// <summary>가이드·일반 퀘스트: 이 이야기 퀘스트(id)를 끝내야 받을 수 있다.</summary>
        public string AfterStory;

        public string Title;
        /// <summary>NPC가 들려주는 이야기 글 (퀘스트 상세 화면).</summary>
        public string Desc;
        public QuestGoal Goal;
        public string SpeciesId;
        public Habitat Habitat;
        public TimeBand Band;
        public float MinLen;
        public int Count = 1;
        public int RewardGold;
        public int RewardExp;
        public string RewardBaitId;
        public int RewardBaitCount;
        /// <summary>살림망 칸 확장 보상 (원작 자이언트 그루퍼 퀘스트: 3칸).</summary>
        public int RewardKeepNet;
        public string RewardCostume;
        /// <summary>일일반복 보상 소라고동.</summary>
        public int RewardConch;
        /// <summary>일일반복: SaveData.daily 몇 번째인지.</summary>
        public int DailyIndex = -1;

        /// <summary>원작 목록 꼬리표: (이야기) (일일반복) (가이드), 일반은 없음.</summary>
        public string TypeTag => Type switch { QuestType.Story => "이야기", QuestType.Repeat => "일일반복", QuestType.Guide => "가이드", _ => "" };
        /// <summary>퀘스트 책 배지 (STORY / REPEAT / GUIDE).</summary>
        public string Badge => Type switch { QuestType.Story => "STORY", QuestType.Repeat => "REPEAT", QuestType.Guide => "GUIDE", _ => "" };
        /// <summary>종류 색 (html).</summary>
        public string TypeColor => Type switch { QuestType.Story => "#c49aff", QuestType.Repeat => "#7fc4ff", QuestType.Guide => "#e6a8ff", _ => "#eef2f6" };

        public string GoalText
        {
            get
            {
                var fish = FishDatabase.Get(SpeciesId)?.Name ?? "";
                return Goal switch
                {
                    QuestGoal.CatchAny => $"아무 물고기 {Count}마리 낚기",
                    QuestGoal.CatchSpecies => $"{fish} {Count}마리 낚기",
                    QuestGoal.DistinctSpecies => $"서로 다른 어종 {Count}종 낚기",
                    QuestGoal.CatchInHabitat => $"{Names.Of(Habitat)} 포인트에서 {Count}마리 낚기",
                    QuestGoal.CatchSpeciesInBand => $"{Names.Of(Band)}에 {fish} {Count}마리 낚기",
                    QuestGoal.CatchMinLength => $"{MinLen:0.#}cm 이상 {fish} 낚기",
                    _ => Objective
                };
            }
        }

        /// <summary>원작 목표 줄 형식 ("참돔 잡아서 가져오기", "최아린을 만나기") — 뒤에 " 0/1"을 붙여 쓴다.</summary>
        public string Objective
        {
            get
            {
                var fish = FishDatabase.Get(SpeciesId)?.Name ?? "";
                return Goal switch
                {
                    QuestGoal.CatchAny => "물고기 잡아서 가져오기",
                    QuestGoal.CatchSpecies => $"{fish} 잡아서 가져오기",
                    QuestGoal.DistinctSpecies => "서로 다른 어종 낚아보기",
                    QuestGoal.CatchInHabitat => RequireMap && MapId != null
                        ? $"{Fishing.World.Maps.Get(MapId).HabitatName(Habitat)}에서 낚아보기"
                        : $"{Names.Of(Habitat)} 포인트에서 낚아보기",
                    QuestGoal.CatchSpeciesInBand => $"{Names.Of(Band)}에 {fish} 낚아보기",
                    QuestGoal.CatchMinLength => $"{MinLen:0.#}cm 이상 {fish} 낚아보기",
                    QuestGoal.Meet => $"{QuestNpcs.Get(Reporter)?.Name}{QuestNpcs.Josa(QuestNpcs.Get(Reporter)?.Name, "을", "를")} 만나기",
                    QuestGoal.Deliver => $"{ItemName} 전달하기",
                    QuestGoal.Release => "물고기 방생해보기",
                    QuestGoal.DeliverFish => SpeciesId != null
                        ? $"{fish}{QuestNpcs.Josa(fish, "을", "를")} {Fishing.World.Maps.Get(MapId).ShortName}에서 납품하기"
                        : "물고기 납품해보기",
                    QuestGoal.InstallTrap => "통발 설치해보기",
                    _ => ""
                };
            }
        }

        /// <summary>원작 "[퀘스트 완료 보상] 6000EXP / 2880GOLD".</summary>
        public string RewardLine => $"{RewardExp}EXP / {RewardGold}GOLD";

        /// <summary>보상 칸에 넣을 아이템 (미끼·살림망·의상·소라고동).</summary>
        public List<string> RewardItems
        {
            get
            {
                var parts = new List<string>();
                var bait = GearDatabase.GetBait(RewardBaitId);
                if (bait != null) parts.Add($"{bait.Name}\nx{RewardBaitCount}");
                if (RewardKeepNet > 0) parts.Add($"살림망\n+{RewardKeepNet}칸");
                var costume = CostumeDatabase.Get(RewardCostume);
                if (costume != null) parts.Add(costume.Name);
                if (RewardConch > 0) parts.Add($"소라고동\nx{RewardConch}");
                return parts;
            }
        }

        public string RewardText
        {
            get
            {
                var parts = new List<string>();
                if (RewardGold > 0) parts.Add($"{RewardGold:N0}골드");
                if (RewardExp > 0) parts.Add($"경험치 {RewardExp}");
                var bait = GearDatabase.GetBait(RewardBaitId);
                if (bait != null) parts.Add($"{bait.Name} {RewardBaitCount}개");
                if (RewardKeepNet > 0) parts.Add($"살림망 {RewardKeepNet}칸 확장");
                var costume = CostumeDatabase.Get(RewardCostume);
                if (costume != null) parts.Add(costume.Name);
                if (RewardConch > 0) parts.Add($"소라고동 {RewardConch}개");
                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>퀘스트를 주고받는 NPC. 원작처럼 이름 + &lt;별칭&gt;이 있고, 낚시터(또는 대기실)에 있다.</summary>
    public sealed class QuestNpc
    {
        public string Key;
        public string Name;
        public string Epithet;
        /// <summary>있는 낚시터. null이면 대기실(한국 낚시 조합).</summary>
        public string MapId;
        /// <summary>말을 걸면 하는 인사 (대화창 첫 줄).</summary>
        public string Greeting;
        /// <summary>대기실 이름 카드 색.</summary>
        public UnityEngine.Color Color = new UnityEngine.Color(0.3f, 0.5f, 0.7f);

        public bool InLobby => MapId == null;
        /// <summary>"홍현 앞바다" / "한국 낚시 조합".</summary>
        public string Place => MapId == null ? "한국 낚시 조합" : Fishing.World.Maps.Get(MapId).ShortName;
        /// <summary>원작 "[보고자] 홍현 앞바다의 출장나온 강대표".</summary>
        public string Where => $"{Place}의 {Name}";
    }

    /// <summary>
    /// 퀘스트 NPC 목록. 키:
    ///  - 대기실 조합 직원 "lobby:desk" 등 (원작 대기실 퀘스트 버튼 → 초상화 4명. 솔로판 직원은 우리 인물)
    ///  - 낚시터 조합 연락관 "union@맵id" (NpcAngler.NpcId "union")
    ///  - 낚시점 주인 "shop@맵id" (NpcId "shop"), 마을 어부 "fisher@맵id" (NpcId "fisher", 통발)
    /// </summary>
    public static class QuestNpcs
    {
        public const string Desk = "lobby:desk", Chief = "lobby:chief", Doctor = "lobby:doctor", Manager = "lobby:manager";

        /// <summary>대기실 조합 직원 (원작 대기실 오른쪽 아래 초상화 줄 순서).</summary>
        public static readonly List<QuestNpc> Lobby = new List<QuestNpc>
        {
            new QuestNpc { Key = Manager, Name = "오실장", Epithet = "조합 사무장", Color = new UnityEngine.Color(0.36f, 0.42f, 0.62f),
                Greeting = "지부마다 일손이 모자라요. 레벨이 오르면 새 낚시터 지부를 소개해 드릴게요." },
            new QuestNpc { Key = Chief, Name = "서대표", Epithet = "조합 대표", Color = new UnityEngine.Color(0.55f, 0.4f, 0.28f),
                Greeting = "전국 바다를 다 돌아본 낚시꾼이 우리 조합의 자랑이지. 자네도 곧 그렇게 될 걸세." },
            new QuestNpc { Key = Doctor, Name = "문박사", Epithet = "어류 연구원", Color = new UnityEngine.Color(0.3f, 0.55f, 0.45f),
                Greeting = "물고기 생태는 알면 알수록 재미있다네. 자네가 채운 도감이 연구에 큰 도움이 되지." },
            new QuestNpc { Key = Desk, Name = "한서윤", Epithet = "조합 안내원", Color = new UnityEngine.Color(0.62f, 0.36f, 0.5f),
                Greeting = "어머, 무슨 일이세요? 낚시터 소식이나 조합 일은 저한테 물어보세요!" },
        };

        // 낚시터 조합 연락관 (월드에서 NpcId "union"으로 세운 NPC와 같은 이름)
        static readonly (string map, string name, string epithet, string greet)[] UnionTable =
        {
            ("hangang", "박주임", "한국 낚시 조합 연락관", "한국 낚시 조합 연락관 박주임입니다. 한강에서 하실 일은 저한테 받아 가세요."),
            ("honghyeon", "어촌계 정계장", "한국 낚시 조합 홍현 지부", "홍현에 참 별일이 다 생긴다니까. 바다가 예전 같지 않아."),
            ("seocheon", "어촌계 이계장", "한국 낚시 조합 서천 지부", "서해는 물때가 전부예요. 물 들어올 때 노 저으라는 말, 낚시도 똑같아요."),
            ("yeongheung", "어촌계 최계장", "한국 낚시 조합 영흥 지부", "해무가 끼면 한 치 앞도 안 보여요. 조심해서 다니세요."),
            ("taean", "어촌계 한계장", "한국 낚시 조합 태안 지부", "만리포 모래밭은 광어 밭이에요. 원투 한번 시원하게 해 보세요."),
            ("samcheok", "수협 강과장", "한국 낚시 조합 삼척 지부", "동해는 물이 맑고 깊어요. 고기도 힘이 장사지요."),
            ("deungdae", "등대섬 이장님", "한국 낚시 조합 등대섬 연락소", "섬에 손님이 오니 반갑구먼. 천천히 둘러보고 가."),
            ("heonhwa", "어촌계 김계장", "한국 낚시 조합 강릉 지부", "헌화로 바위 절벽, 경치 하나는 끝내주죠?"),
            ("cheongjin", "어로반장 박씨", "한국 낚시 조합 청진 연락소", "북쪽 바다는 춥다우. 장갑 단단히 끼시오."),
            ("yokji", "어촌계 서계장", "한국 낚시 조합 통영 지부", "욕지도 가두리는 참돔 천지예요. 통로에서 미끄러지지 마세요."),
            ("jeju", "어촌계 고계장", "한국 낚시 조합 제주 지부", "혼저 옵서예! 제주 바다는 물빛부터 다르지요."),
            ("geomun", "어촌계 문계장", "한국 낚시 조합 거문도 지부", "여기까지 오셨으면 한국 바다는 거의 다 보신 거예요."),
            ("halong", "응우옌 지부장", "한국 낚시 조합 하롱베이 지부", "씬 짜오! 한국에서 오신 손님이군요. 봉우리 사이 물길을 안내해 드리죠."),
            ("santorini", "야니스 지부장", "한국 낚시 조합 산토리니 지부", "칼리메라! 에게해의 바람은 낚싯줄도 노래하게 만들지요."),
            ("nile", "하산 지부장", "한국 낚시 조합 이집트 지부", "앗살람 알라이쿰! 나일강은 오천 년 동안 사람을 먹여 살린 강이오."),
        };

        static readonly (string map, string name, string epithet, string greet)[] ShopTable =
        {
            ("hangang", "낚시점 김사장", "반포 낚시점", "어서 와요. 미끼, 장비, 수리, 다 돼."),
            ("honghyeon", "홍현 낚시점 최사장", "홍현 낚시점", "바다 미끼는 여기서 다 팔아요. 필요한 거 있으면 말해요."),
        };

        static Dictionary<string, QuestNpc> _all;

        static Dictionary<string, QuestNpc> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new Dictionary<string, QuestNpc>();
                foreach (var n in Lobby) _all[n.Key] = n;
                foreach (var (map, name, ep, greet) in UnionTable)
                    _all[UnionOf(map)] = new QuestNpc { Key = UnionOf(map), Name = name, Epithet = ep, MapId = map, Greeting = greet };
                foreach (var (map, name, ep, greet) in ShopTable)
                    _all[ShopOf(map)] = new QuestNpc { Key = ShopOf(map), Name = name, Epithet = ep, MapId = map, Greeting = greet };
                // 마을 어부 (통발, 원작 이지훈 <홍현마을 어부>): 맵마다 연락관 옆에 선다 (FishTraps.SpawnFisher)
                foreach (var (map, _, _, _) in UnionTable)
                    _all[FisherOf(map)] = new QuestNpc { Key = FisherOf(map), Name = Fishing.Core.FishTraps.FisherName(map), Epithet = Fishing.Core.FishTraps.FisherTitle(map),
                        MapId = map, Greeting = "이 바다는 내 손바닥이지. 통발 놓는 법이라면 뭐든 물어보게.", Color = new UnityEngine.Color(0.4f, 0.45f, 0.3f) };
                return _all;
            }
        }

        public static string UnionOf(string mapId) => "union@" + mapId;
        public static string ShopOf(string mapId) => "shop@" + mapId;
        public static string FisherOf(string mapId) => "fisher@" + mapId;

        public static QuestNpc Get(string key) => key != null && All.TryGetValue(key, out var n) ? n : null;

        /// <summary>월드 NPC(NpcAngler.NpcId)의 퀘스트 키. 퀘스트 NPC가 아니면 null.</summary>
        public static string KeyFor(string npcId, string mapId)
        {
            string key = npcId == "union" ? UnionOf(mapId) : npcId == "shop" ? ShopOf(mapId) : npcId == "fisher" ? FisherOf(mapId) : null;
            return key != null && All.ContainsKey(key) ? key : null;
        }

        /// <summary>한국어 조사: 받침이 있으면 withFinal(을/과/은), 없으면 withoutFinal(를/와/는).</summary>
        public static string Josa(string word, string withFinal, string withoutFinal)
        {
            if (string.IsNullOrEmpty(word)) return withoutFinal;
            char c = word[word.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return withoutFinal;
            return (c - 0xAC00) % 28 != 0 ? withFinal : withoutFinal;
        }
    }

    /// <summary>
    /// 한국 낚시 조합의 퀘스트.
    ///  - 이야기(Story): 순서대로 이어지는 큰 줄기. NPC에게 수락하고, [보고자]에게 가서 완료한다. 장(낚시터)이 바뀔 때 대기실 직원 ↔ 낚시터 연락관으로 넘어간다
    ///    (원작: 강대표 → "최아린을 만나기" → 최아린 → "거대한 등지느러미 가시 전달하기" → 홍현의 강대표).
    ///  - 가이드(Guide): 방생·납품·통발 같은 시스템을 한 번씩 해 보게 하는 퀘스트.
    ///  - 일반(Normal): 낚시점 주인 같은 NPC의 한 번짜리 부탁.
    ///  - 일일반복(Repeat): QuestSystem이 게임 하루마다 3개 만든다 (그 낚시터 연락관이 준다).
    /// </summary>
    public static class QuestDatabase
    {
        public const string Giver = "한국 낚시 조합";

        /// <summary>예전 저장(ver 0)의 이야기 퀘스트 수. index 0~44 = st00~st44.</summary>
        public const int LegacyStoryCount = 45;

        public static readonly List<QuestDef> Story = BuildStory();

        static List<QuestDef> BuildStory()
        {
            var list = new List<QuestDef>();
            int legacy = 0;
            // 한 장(낚시터) 묶음: 첫 퀘스트는 opener(대기실 직원 등)가 주고, 나머지는 그 낚시터 연락관이 준다. 보고는 모두 그 낚시터 연락관
            void Chapter(string map, string opener, params QuestDef[] qs)
            {
                int entry = FishingAreas.EntryLevel(map);
                for (int i = 0; i < qs.Length; i++)
                {
                    var q = qs[i];
                    q.Type = QuestType.Story;
                    q.Id ??= $"st{legacy++:00}";
                    // 대기실 사람을 만나러 가는 퀘스트는 낚시터가 없다 (대기실)
                    bool toLobby = q.Goal == QuestGoal.Meet && QuestNpcs.Get(q.Reporter)?.InLobby == true;
                    if (q.MapId == null && !toLobby) q.MapId = map;
                    if (q.Lv <= 1) q.Lv = entry + i / 2;
                    q.Reporter ??= QuestNpcs.UnionOf(map);
                    q.Giver ??= i == 0 && opener != null ? opener : QuestNpcs.UnionOf(map);
                    list.Add(q);
                }
            }
            string U(string map) => QuestNpcs.UnionOf(map);

            // ---------- 한강 (Lv.1) ----------
            Chapter("hangang", QuestNpcs.Desk,
                new QuestDef { Title = "첫 손맛", Desc = "조합에 온 걸 환영하네! 우선 한 마리 낚아서 손맛부터 보게.\n한강 데크 입구의 박주임이 첫 일을 봐 줄 거예요. [낚시터 선택하기]에서 한강에 들어가 보세요.",
                    Goal = QuestGoal.CatchAny, Count = 1, RewardGold = 1000, RewardExp = 50 },
                new QuestDef { Title = "붕어를 찾아서", Desc = "민물낚시의 기본은 붕어지. 얕은 여울이나 수초 근처를 노려보게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "crucian", Count = 3, RewardGold = 1500, RewardExp = 80,
                    RewardBaitId = "bait_paste", RewardBaitCount = 20 },
                new QuestDef { Title = "도감 채우기", Desc = "한강엔 생각보다 많은 물고기가 산다네. 도감(P)을 채워보게.",
                    Goal = QuestGoal.DistinctSpecies, Count = 4, RewardGold = 2000, RewardExp = 120 },
                new QuestDef { Title = "수초 포인트", Desc = "동쪽 만곡부 수초 지대는 고기들의 은신처지.",
                    Goal = QuestGoal.CatchInHabitat, Habitat = Habitat.Weed, Count = 3, RewardGold = 1500, RewardExp = 120,
                    RewardBaitId = "bait_corn", RewardBaitCount = 30 },
                new QuestDef { Title = "밤낚시", Desc = "해가 지면 메기가 깨어난다네. 깊은 물이나 수초 근처를 노려보게.",
                    Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "catfish", Band = TimeBand.Night, Count = 1,
                    RewardGold = 3000, RewardExp = 200 },
                new QuestDef { Title = "교각 아래의 사냥꾼", Desc = "다리 교각 석축에 쏘가리가 숨어 있다지. 생미끼나 루어가 좋을 걸세.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "mandarin", Count = 1, RewardGold = 5000, RewardExp = 300,
                    RewardBaitId = "bait_minnow", RewardBaitCount = 10 },
                new QuestDef { Title = "월척의 꿈", Desc = "한 자(30.3cm)가 넘는 붕어, 낚시꾼이라면 누구나 꿈꾸지.",
                    Goal = QuestGoal.CatchMinLength, SpeciesId = "crucian", MinLen = 30.3f, Count = 1,
                    RewardGold = 8000, RewardExp = 400 },
                new QuestDef { Title = "한강의 주인", Desc = "80cm가 넘는 잉어를 낚아 온다면 자네를 진정한 강태공으로 인정하지.",
                    Goal = QuestGoal.CatchMinLength, SpeciesId = "carp", MinLen = 80f, Count = 1,
                    RewardGold = 20000, RewardExp = 1000 });

            // 넘겨주기: 한강 연락관 → 대기실 서대표 (만나기) → 서대표가 홍현 지부로 물건 전달
            Chapter("hangang", null,
                new QuestDef { Id = "hand_chief", Title = "대표님의 호출", MapId = null, Lv = 4, Giver = U("hangang"), Reporter = QuestNpcs.Chief,
                    Desc = "한강의 주인을 낚았다는 소식이 벌써 조합에 퍼졌어요! 서대표님이 직접 만나 보고 싶다고 하십니다.\n" +
                           "◀ 조합 버튼으로 대기실에 돌아가서 [퀘스트]에서 서대표님을 찾아가 보세요.",
                    Goal = QuestGoal.Meet, Count = 1, RewardGold = 1000, RewardExp = 300 });
            Chapter("honghyeon", null,
                new QuestDef { Id = "hand_parcel", Title = "홍현 지부로 가는 꾸러미", Lv = 5, Giver = QuestNpcs.Chief, ItemName = "조합 공문 꾸러미",
                    Desc = "자네 같은 사람을 기다렸네. 이제 강을 졸업하고 바다로 갈 때가 됐지.\n" +
                           "남해 홍현 지부에 보낼 조합 공문 꾸러미가 있네. 배를 받았으면 홍현 앞바다로 가서 방파제 입구의 어촌계 정계장에게 전해 주게. " +
                           "요즘 홍현 바다가 심상치 않다는 보고가 올라오는데, 가는 김에 그곳 사정도 좀 들어 보고. (Lv.5부터 입장)",
                    Goal = QuestGoal.Deliver, Count = 1, RewardGold = 1500, RewardExp = 400 });

            // ---------- 홍현 앞바다 (Lv.5, 배를 받으면 열린다) ----------
            Chapter("honghyeon", null,
                new QuestDef { Title = "남해로", Desc = "서대표님 꾸러미 잘 받았어요. 먼 길 왔으니 홍현 앞바다 고등어 떼부터 맛보게나.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "mackerel", Count = 3, RewardGold = 3000, RewardExp = 400,
                    RewardBaitId = "bait_krill", RewardBaitCount = 30 },
                new QuestDef { Title = "방파제의 밤", Desc = "해가 지면 방파제 테트라포드 사이로 볼락이 올라온다네.",
                    Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "rockfish", Band = TimeBand.Night, Count = 2,
                    RewardGold = 4000, RewardExp = 500, RewardBaitId = "bait_seaworm", RewardBaitCount = 30 },
                new QuestDef { Title = "감성돔 손맛", Desc = "홍현 앞바다 꾼들이 제일 아끼는 게 감성돔이지. 흐리거나 비 오는 새벽·저녁 갯바위를 노리게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "blackseabream", Count = 1, RewardGold = 8000, RewardExp = 800 },
                new QuestDef { Title = "먼바다의 여왕", Desc = "배를 타고 먼바다로 나가 참돔을 낚아 오게. 크릴이나 오징어살이 좋다네.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "redseabream", Count = 1, RewardGold = 12000, RewardExp = 1200,
                    RewardBaitId = "bait_squid", RewardBaitCount = 20 },
                new QuestDef { Title = "자이언트 그루퍼", Desc = "암초의 주인 자이언트 그루퍼! 저녁엔 C4, 아침엔 E3 어군에 나타난다지. 헤비대와 강한 줄을 챙기게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "giantgrouper", Count = 1, RewardGold = 50000, RewardExp = 3000,
                    RewardKeepNet = 3, RewardCostume = "top_union" });

            // 넘겨주기: 홍현 정계장 → 대기실 문박사 (만나기) → 문박사가 서천으로
            Chapter("honghyeon", null,
                new QuestDef { Id = "hand_doctor", Title = "그루퍼의 비늘", MapId = null, Lv = 7, Reporter = QuestNpcs.Doctor,
                    Desc = "자이언트 그루퍼를 올렸다는 소문이 조합까지 났어! 문박사님이 그 녀석 이야기를 직접 듣고 싶어 하시네.\n" +
                           "요즘 남해에 큰 고기가 줄고 서해엔 큰 고기만 잡힌다는데, 박사님이라면 까닭을 아실지도 몰라. 대기실로 가 보게.",
                    Goal = QuestGoal.Meet, Count = 1, RewardGold = 2000, RewardExp = 600 });

            // ---------- 서천 앞바다 (Lv.6, 서해) ----------
            Chapter("seocheon", QuestNpcs.Doctor,
                new QuestDef { Title = "서해 노을", Desc = "그루퍼 비늘을 보니 확실해졌네. 남해 고기들이 서해 쪽으로 올라가고 있어.\n서천 앞바다에 가 보게. 춘장대 갯벌 가에서 망둥어부터 낚아 보면 서해 맛을 알 걸세. 홍원항의 이계장이 도와줄 거야.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "goby", Count = 3, RewardGold = 3000, RewardExp = 500,
                    RewardBaitId = "bait_egi", RewardBaitCount = 5 },
                new QuestDef { Title = "주꾸미 철", Desc = "서천 하면 주꾸미지! 에기를 바닥에 붙여 살살 끌어 보게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "octopus", Count = 3, RewardGold = 6000, RewardExp = 700 },
                new QuestDef { Title = "물때를 읽어라", Desc = "서해는 물이 들고 나는 바다라네. 물이 들어올 때(들물) 고기가 붙지. 해 질 녘에 백조기를 노려 보게.",
                    Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "whitecroaker", Band = TimeBand.Evening, Count = 2,
                    RewardGold = 8000, RewardExp = 900, RewardBaitId = "bait_shrimp", RewardBaitCount = 20 },
                new QuestDef { Title = "먹물 조심", Desc = "갑오징어는 먹물을 뿜으며 뒤로 내뺀다네. 바위 근처에서 에기로 노려 보게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "cuttlefish", Count = 1, RewardGold = 10000, RewardExp = 1200 },
                new QuestDef { Title = "서해의 왕", Desc = "여름 서해의 왕 민어! 저녁엔 C5, 밤엔 F6 어군에 들어온다지. 새우 미끼와 튼튼한 줄을 챙기게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "croaker", Count = 1, RewardGold = 50000, RewardExp = 3000,
                    RewardKeepNet = 2 });

            // ---------- 영흥도 (Lv.9, 서해) ----------
            Chapter("yeongheung", QuestNpcs.Manager,
                new QuestDef { Title = "해무 속의 섬", Desc = "영흥도는 흐린 날 해무가 짙게 깔리는 섬이라네. 진두항 방파제에서 우럭부터 낚아 보게. 진두항 입구의 최계장에게 보고하고요. (Lv.9부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "jacopever", Count = 2, RewardGold = 5000, RewardExp = 700,
                    RewardBaitId = "bait_minnow", RewardBaitCount = 10 },
                new QuestDef { Title = "목섬 가는 길", Desc = "썰물이면 목섬까지 모래톱이 드러난다네. 물골을 오가는 점농어를 생미끼로 노려 보게. 물이 들기 전에 돌아오고!",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "spottedseabass", Count = 1, RewardGold = 10000, RewardExp = 1200 });

            // ---------- 태안반도 연안 (Lv.11, 서해) ----------
            Chapter("taean", QuestNpcs.Manager,
                new QuestDef { Title = "만리포의 원투", Desc = "만리포 긴 해변은 광어 밭이라네. 생미끼를 멀리 던져 모래 바닥을 노려 보게. 만리포항의 한계장이 기다려요. (Lv.11부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "flounder", Count = 1, RewardGold = 7000, RewardExp = 1000 },
                new QuestDef { Title = "대광어", Desc = "70cm가 넘는 광어를 꾼들은 대광어라 부르지. 할미·할아비바위 노을을 보며 한 마리 올려 보게.",
                    Goal = QuestGoal.CatchMinLength, SpeciesId = "flounder", MinLen = 70f, Count = 1, RewardGold = 15000, RewardExp = 1600 });

            // ---------- 삼척 앞바다 (Lv.14, 동해) ----------
            Chapter("samcheok", QuestNpcs.Chief,
                new QuestDef { Title = "동해의 맛", Desc = "동해로 가 보게! 삼척 앞바다 갯바위에서 누런 몸에 검은 띠, 임연수어를 낚아 오게. 삼척항의 강과장에게 안부도 전하고. (Lv.14부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "atkamackerel", Count = 3, RewardGold = 7000, RewardExp = 1200,
                    RewardBaitId = "bait_egi", RewardBaitCount = 10 },
                new QuestDef { Title = "집어등 아래", Desc = "밤이 되면 수평선에 오징어배 불빛이 줄지어 켜진다네. 에기로 밤바다의 오징어를 노려 보게.",
                    Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "squid", Band = TimeBand.Night, Count = 2, RewardGold = 12000, RewardExp = 1600 });

            // ---------- 동해 등대섬 (Lv.16) ----------
            Chapter("deungdae", QuestNpcs.Doctor,
                new QuestDef { Title = "등대섬의 열기", Desc = "등대섬 물은 바닥이 보일 만큼 맑다네. 바위 아래 붉은 열기(불볼락)가 줄줄이 문다지. 선착장 이장님께 보고하게. (Lv.16부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "redrockfish", Count = 4, RewardGold = 9000, RewardExp = 1500 },
                new QuestDef { Title = "방어 떼를 쫓아", Desc = "새벽과 저녁에 방어 떼가 섬을 돌아 나간다네. 배를 타고 먼 물에서 생미끼나 루어로 붙어 보게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "yellowtail", Count = 1, RewardGold = 15000, RewardExp = 1900 });

            // ---------- 헌화로 (Lv.18, 동해) ----------
            Chapter("heonhwa", QuestNpcs.Chief,
                new QuestDef { Title = "헌화가", Desc = "옛날 한 노인이 이 절벽의 진달래를 꺾어 수로부인에게 바쳤다지. 우리는 꽃 대신 새벽 갯바위의 감성돔을 바치세. 심곡항 김계장이 기다리네. (Lv.18부터 입장)",
                    Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "blackseabream", Band = TimeBand.Dawn, Count = 1, RewardGold = 15000, RewardExp = 2000 },
                new QuestDef { Title = "바위굴의 거인", Desc = "헌화로 갯바위 굴에 대문어가 산다네. 새벽 D2, 저녁 G2에 굴 밖으로 나온다지. 바닥에 붙으면 버티기 싸움일세.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "giantoctopus", Count = 1, RewardGold = 25000, RewardExp = 2600 });

            // ---------- 욕지도 (Lv.19, 남해) ----------
            Chapter("yokji", QuestNpcs.Manager,
                new QuestDef { Title = "가두리 손님", Desc = "욕지도 가두리 낚시터 둘레엔 참돔이 모여든다네. 통로를 걸으며 그물 칸 사이로 찌를 넣어 보게. 욕지항 서계장에게 보고하세요. (Lv.19부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "redseabream", Count = 2, RewardGold = 15000, RewardExp = 2200 },
                new QuestDef { Title = "은빛 칼", Desc = "밤이면 갈치가 몸을 세우고 수면 가까이 올라온다네. 해체 미끼나 오징어살로 노려 보게.",
                    Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "hairtail", Band = TimeBand.Night, Count = 3, RewardGold = 18000, RewardExp = 2500 });

            // ---------- 제주 서귀포 (Lv.21) ----------
            Chapter("jeju", QuestNpcs.Desk,
                new QuestDef { Title = "외돌개 아래", Desc = "제주에 왔으면 자리돔부터 맛봐야지! 외돌개 앞 갯바위 물속에 구름처럼 떼 지어 있다네. 크릴 한 줌이면 된다. 서귀포항 고계장님께 보고하세요. (Lv.21부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "damselfish", Count = 3, RewardGold = 12000, RewardExp = 2400,
                    RewardBaitId = "bait_egi", RewardBaitCount = 10 },
                new QuestDef { Title = "진상품 옥돔", Desc = "임금님께 올리던 옥돔이라네. 깊은 모래펄 바닥에 갯지렁이를 내려 보게. 해녀 할망이 옥돔국을 끓여 준다더군.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "tilefish", Count = 1, RewardGold = 22000, RewardExp = 2800 });

            // ---------- 청진포구 (Lv.22, 동해 북부) ----------
            Chapter("cheongjin", QuestNpcs.Doctor,
                new QuestDef { Title = "북쪽 끝 항구", Desc = "청진포구는 눈 덮인 큰 항구라네. 깊은 물에서 입 큰 대구부터 낚아 보게. 포구의 박씨가 안내해 줄 걸세. (Lv.22부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "cod", Count = 1, RewardGold = 20000, RewardExp = 2800 },
                new QuestDef { Title = "명태의 고향", Desc = "국민 생선 명태가 여기서 왔다네. 새벽엔 B5, 밤엔 F6 어군에만 들어오지. 덕장 주인이 꼭 한 마리 보고 싶다더군.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "pollock", Count = 1, RewardGold = 60000, RewardExp = 4000, RewardKeepNet = 2 });

            // ---------- 거문도 (Lv.24, 남해 먼바다) ----------
            Chapter("geomun", QuestNpcs.Chief,
                new QuestDef { Title = "거문도 등대", Desc = "1905년에 불을 밝힌 거문도 등대 아래 갯바위는 돌돔의 집이라네. 맑은 낮에 갯지렁이로 노려 보게. 거문항 문계장에게 보고하게. (Lv.24부터 입장)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "beakfish", Count = 1, RewardGold = 25000, RewardExp = 3000 },
                new QuestDef { Title = "미터급 부시리", Desc = "한국 바다 마지막 부탁일세. 거문도 아침 G4, 낮 C6에 몰려오는 부시리 무리에서 1m가 넘는 놈을 낚아 오게. 그럼 자네가 진짜 강태공이지!",
                    Goal = QuestGoal.CatchMinLength, SpeciesId = "amberjack", MinLen = 100f, Count = 1, RewardGold = 80000, RewardExp = 5000, RewardKeepNet = 3 });

            // 넘겨주기: 서대표가 하롱베이 지부로 소개장 전달
            Chapter("halong", null,
                new QuestDef { Id = "hand_letter", Title = "바다 건너 보내는 소개장", Lv = 25, Giver = QuestNpcs.Chief, ItemName = "조합 대표 소개장",
                    Desc = "한국 바다는 다 돌아봤군! 이제 자네를 세계에 내보낼 차례일세.\n" +
                           "조합 하롱베이 지부 응우옌 지부장 앞으로 소개장을 써 두었네. 낚시터 목록 [동남아] 탭에서 하롱베이로 가서 전해 주게. (Lv.25부터 입장)",
                    Goal = QuestGoal.Deliver, Count = 1, RewardGold = 5000, RewardExp = 2000 });

            // ================= 해외 거점 (원작: 동남아 → 지중해 → 나일강) =================
            // ---------- 하롱베이 (Lv.25) ----------
            Chapter("halong", null,
                new QuestDef { Title = "바다 건너 하롱베이", Desc = "서대표님 소개장이군요! 반갑습니다. 석회암 봉우리 사이 바라문디부터 인사하세요.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "barramundi", Count = 1, RewardGold = 30000, RewardExp = 4000 },
                new QuestDef { Title = "봉우리 밑 바위굴", Desc = "봉우리 아래 바위굴엔 얼룩바리가 산다네. 물리면 굴로 처박으니 장력을 늦추지 말게. 두 마리!",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "orangegrouper", Count = 2, RewardGold = 30000, RewardExp = 4500 },
                new QuestDef { Title = "GT의 비명", Desc = "새벽 F5, 저녁 C7 물살에 자이언트 트레발리가 몰려온다네. 릴이 비명을 지르는 소리를 들어 보게.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "gianttrevally", Count = 1, RewardGold = 50000, RewardExp = 6000 });
            // ---------- 산토리니 (Lv.28) ----------
            Chapter("santorini", QuestNpcs.Manager,
                new QuestDef { Title = "하얀 마을 아래 항구", Desc = "산토리니 이아 절벽 아래 암모우디 항구라네. 깊은 칼데라 바다의 유럽대구를 한 마리 낚아 보게. 야니스 지부장에게 보고하세요. (낚시터 목록 [지중해] 탭, Lv.28부터)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "hake", Count = 1, RewardGold = 40000, RewardExp = 5000 },
                new QuestDef { Title = "금빛 왕관", Desc = "눈 사이에 금빛 띠를 두른 귀족도미 세 마리. 타베르나 요르고스가 저녁 요리에 쓰겠다더군.",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "giltheadbream", Count = 3, RewardGold = 40000, RewardExp = 5500 },
                new QuestDef { Title = "에게해의 탄환", Desc = "먼바다(배로 D7·G8)에 참다랑어 떼가 지나간다네. 새벽과 아침에만. 배를 끌고 다닐 만큼 세니 각오하게!",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "bluefintuna", Count = 1, RewardGold = 90000, RewardExp = 9000 });
            // ---------- 나일강 하류 (Lv.31) ----------
            Chapter("nile", QuestNpcs.Doctor,
                new QuestDef { Title = "나일강의 저녁거리", Desc = "나일강 하류일세. 강변 사람들 저녁거리인 틸라피아를 다섯 마리 낚아 하산 지부장에게 갖다 주게. 지렁이·떡밥이 잘 듣는다네. (낚시터 목록 [아프리카] 탭, Lv.31부터)",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "tilapia", Count = 5, RewardGold = 50000, RewardExp = 6000 },
                new QuestDef { Title = "강의 호랑이", Desc = "송곳니를 드러낸 타이거피시는 바늘을 잘 뱉는다네. 맑은 낮에 루어로 두 마리!",
                    Goal = QuestGoal.CatchSpecies, SpeciesId = "tigerfish", Count = 2, RewardGold = 60000, RewardExp = 7000 },
                new QuestDef { Title = "나일의 거인", Desc = "벽화 속 그 고기, 나일농어. 새벽 B4·저녁 G5 깊은 골에 어군이 든다네. 1.5m가 넘는 놈을 낚으면 자네는 세계의 강태공일세!",
                    Goal = QuestGoal.CatchMinLength, SpeciesId = "nileperch", MinLen = 150f, Count = 1, RewardGold = 150000, RewardExp = 15000, RewardKeepNet = 3 });

            return list;
        }

        /// <summary>가이드(시스템 익히기)와 일반(한 번짜리 부탁) 퀘스트.</summary>
        public static readonly List<QuestDef> Side = new List<QuestDef>
        {
            // ---------- 가이드 ----------
            new QuestDef { Id = "g_release", Type = QuestType.Guide, Lv = 2, MapId = "hangang", AfterStory = "st00",
                Giver = QuestNpcs.UnionOf("hangang"), Reporter = QuestNpcs.UnionOf("hangang"), Title = "놓아주는 즐거움",
                Desc = "작은 고기는 놓아줘야 내년에도 또 만나지요. 낚은 뒤 결과 창에서 [방생하기]를 눌러 세 마리를 돌려보내 보세요.\n방생하면 경험치를 30% 더 받고, 빙고·업적 칸도 채워져요.",
                Goal = QuestGoal.Release, Count = 3, RewardGold = 800, RewardExp = 200 },
            new QuestDef { Id = "g_delivery", Type = QuestType.Guide, Lv = 3, MapId = null, AfterStory = "st01",
                Giver = QuestNpcs.Desk, Reporter = QuestNpcs.Desk, Title = "첫 납품",
                Desc = "낚은 고기는 납품소에서 조합에 넘길 수 있어요. 대기실 [납품]이나 낚시터 납품소에서 아무 고기나 한 마리 납품해 보세요.\n납품 게이지가 가득 차면 다음 납품은 두 배로 쳐 드려요!",
                Goal = QuestGoal.DeliverFish, Count = 1, RewardGold = 1000, RewardExp = 300, RewardBaitId = "bait_paste", RewardBaitCount = 10 },
            new QuestDef { Id = "g_trap", Type = QuestType.Guide, Lv = 6, MapId = "honghyeon", AfterStory = "st08",
                Giver = QuestNpcs.FisherOf("honghyeon"), Reporter = QuestNpcs.FisherOf("honghyeon"), Title = "통발 놓기",
                Desc = "통발을 바다에 놓아두면 낚시하는 동안에도 고기가 들어온다네. 시간이 지나 수거하면 그만큼 덤이지.\n나한테 [통발 설치하기]로 하나 설치해 보게.",
                Goal = QuestGoal.InstallTrap, Count = 1, RewardGold = 2000, RewardExp = 500 },
            new QuestDef { Id = "g_eco", Type = QuestType.Guide, Lv = 7, MapId = "honghyeon", AfterStory = "st08",
                Giver = QuestNpcs.UnionOf("honghyeon"), Reporter = QuestNpcs.UnionOf("honghyeon"), Title = "홍현 생태조사",
                Desc = "문박사님이 홍현 바다 생태조사를 부탁하셨어. 낚시터마다 어떤 고기가 납품되는지 기록하는 거지.\n고등어를 홍현 앞바다 납품소에서 한 마리 납품해 주게. 기록(I) 책 업적에도 남는다네.",
                Goal = QuestGoal.DeliverFish, SpeciesId = "mackerel", Count = 1, RewardGold = 2500, RewardExp = 600 },

            // ---------- 일반 ----------
            new QuestDef { Id = "n_bluegill", Type = QuestType.Normal, Lv = 2, MapId = "hangang", AfterStory = "st00",
                Giver = QuestNpcs.ShopOf("hangang"), Reporter = QuestNpcs.ShopOf("hangang"), Title = "불청객 블루길",
                Desc = "외래종 블루길이 붕어 새끼를 죄다 먹어 치운다고. 다섯 마리만 잡아다 줘. 튀김이라도 해 먹게.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "bluegill", Count = 5, RewardGold = 1500, RewardExp = 300, RewardBaitId = "bait_corn", RewardBaitCount = 20 },
            new QuestDef { Id = "n_scorpion", Type = QuestType.Normal, Lv = 6, MapId = "honghyeon", AfterStory = "st08",
                Giver = QuestNpcs.ShopOf("honghyeon"), Reporter = QuestNpcs.ShopOf("honghyeon"), Title = "가시 돋친 붉은 손님",
                Desc = "쏨뱅이 매운탕 찾는 손님이 많아요. 저녁이나 밤에 바위 쪽에서 세 마리만 잡아다 줄래요? 가시 조심하고!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "scorpionfish", Count = 3, RewardGold = 3000, RewardExp = 700, RewardBaitId = "bait_seaworm", RewardBaitCount = 20 },
            new QuestDef { Id = "n_horse", Type = QuestType.Normal, Lv = 7, MapId = "honghyeon", AfterStory = "st09",
                Giver = QuestNpcs.ShopOf("honghyeon"), Reporter = QuestNpcs.ShopOf("honghyeon"), Title = "해 질 녘 전갱이",
                Desc = "해 질 녘이면 전갱이가 방파제로 몰려와요. 구이용으로 세 마리만 부탁해요.",
                Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "horsemackerel", Band = TimeBand.Evening, Count = 3, RewardGold = 3500, RewardExp = 800 },
        };

        public static int StoryIndex(string id) => Story.FindIndex(q => q.Id == id);

        /// <summary>예전 저장의 이야기 index(예전 45개 목록 기준)를 지금 목록 위치로 바꾼다.</summary>
        public static int MigrateIndex(int oldIndex)
        {
            if (oldIndex <= 0) return 0;
            if (oldIndex >= LegacyStoryCount) return Story.Count;
            int i = StoryIndex($"st{oldIndex:00}");
            return i >= 0 ? i : Story.Count;
        }

        public static QuestDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var q in Story) if (q.Id == id) return q;
            foreach (var q in Side) if (q.Id == id) return q;
            return null;
        }
    }
}
