using System;
using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    /// <summary>
    /// 어종 퀘스트 (2026-10-30, 어종 224종에 맞춰): 이야기 줄기 가운데에 끼우면 저장된 진행 번호(quest.index)가 밀리므로
    /// 모두 일반(Normal) 퀘스트로 <see cref="Side"/>에 붙인다.
    ///  - 낚시터 어종 퀘스트: 2026-10-29에 늘어난 어종(한국 20 · 동남아 16 · 나일 10 · 지중해 6)을 그 낚시터 조합 연락관·마을 어부·낚시점 주인이 부탁한다.
    ///  - 낚시터 도감 퀘스트: 낚시터마다 "○○ 생태 도감" — 그곳 어종을 N종 도감에 기록하기 (<see cref="QuestGoal.MapDex"/>). N은 어종 표에서 정한다.
    ///  - 문박사 도감 퀘스트: 대기실 문박사가 도감 25 · 50 · 100 · 150 · 200종마다.
    /// 보상은 낚시터 입장 레벨과 어종 등급으로 정한다 (<see cref="SideReward"/>) — 같은 레벨 이야기 퀘스트의 첫 부탁보다 조금 적게.
    /// </summary>
    public static partial class QuestDatabase
    {
        static List<QuestDef> WithSpeciesQuests(List<QuestDef> side)
        {
            side.AddRange(DexMilestones());
            side.AddRange(MapDexQuests());
            side.AddRange(SpeciesQuests());
            return side;
        }

        enum Who { Union, Fisher, Shop }

        static string KeyOf(Who who, string map) => who switch
        {
            Who.Fisher => QuestNpcs.FisherOf(map),
            Who.Shop => QuestNpcs.ShopOf(map),
            _ => QuestNpcs.UnionOf(map),
        };

        static bool IsOverseas(string map) => FishingAreas.Overseas.Exists(a => a.MapId == map);

        static int RoundTo(float v, int step) => Math.Max(step, (int)Math.Round(v / step) * step);

        /// <summary>
        /// 일반 퀘스트 보상 (골드, 경험치): 입장 레벨 L(3 미만은 3)에 한국 600L GOLD · 90L EXP, 해외 1,100L GOLD · 150L EXP를 기준으로
        /// 어종 등급(★1 0.6배 ~ ★5 1.4배) · 마릿수(1마리 더할 때마다 +10%, 1.4배까지) · 목표 배율(길이 1.25, 시간대 1.1, 도감 1.5)을 곱한다.
        /// 예) 탄천 Lv.3 ★1 3마리 1,300 GOLD / 190 EXP, 푸켓 Lv.30 ★3 1마리 33,000 GOLD / 4,500 EXP.
        /// </summary>
        static (int gold, int exp) SideReward(string map, int rarity, int count, float goalFactor)
        {
            int lv = Math.Max(3, FishingAreas.EntryLevel(map)); // 한강(Lv.1)은 Lv.5까지 머무르므로 Lv.3으로 친다
            bool abroad = IsOverseas(map);
            float f = (0.6f + 0.2f * (Math.Max(1, Math.Min(5, rarity)) - 1)) * Math.Min(1.4f, 1f + 0.1f * (count - 1)) * goalFactor;
            float gold = (abroad ? 1100f : 600f) * lv * f;
            float exp = (abroad ? 150f : 90f) * lv * f;
            return (RoundTo(gold, gold < 10000f ? 100 : 500), RoundTo(exp, exp < 1000f ? 10 : 50));
        }

        /// <summary>낚시터 어종 퀘스트 하나 (보상은 <see cref="SideReward"/>). 받는 NPC가 곧 보고자.</summary>
        static QuestDef Ask(string map, Who who, string id, string title, string desc, string species, int count = 1,
            QuestGoal goal = QuestGoal.CatchSpecies, float minLen = 0f, TimeBand band = TimeBand.Day, string bait = null, int baitCount = 0)
        {
            int rarity = FishDatabase.Get(species)?.Rarity ?? 1;
            float goalFactor = goal == QuestGoal.CatchMinLength ? 1.25f : goal == QuestGoal.CatchSpeciesInBand ? 1.1f : 1f;
            var (gold, exp) = SideReward(map, rarity, count, goalFactor);
            string key = KeyOf(who, map);
            return new QuestDef
            {
                Id = id, Type = QuestType.Normal, MapId = map, Lv = FishingAreas.EntryLevel(map) + (rarity >= 3 ? 1 : 0) + (rarity >= 4 ? 1 : 0),
                Giver = key, Reporter = key, Title = title, Desc = desc,
                Goal = goal, SpeciesId = species, Count = count, MinLen = minLen, Band = band,
                RewardGold = gold, RewardExp = exp, RewardBaitId = bait, RewardBaitCount = baitCount,
            };
        }

        // ================= 문박사 도감 퀘스트 (대기실) =================

        static IEnumerable<QuestDef> DexMilestones()
        {
            QuestDef Step(int n, int lv, int gold, int exp, int conch, string desc, int keepNet = 0, string bait = null, int baitCount = 0) => new QuestDef
            {
                Id = $"dex_{n}", Type = QuestType.Normal, Lv = lv, MapId = null, Giver = QuestNpcs.Doctor, Reporter = QuestNpcs.Doctor,
                Title = $"어류 도감 {n}종", Desc = desc + "\n<size=90%>지금까지 도감(P)에 기록한 어종을 모두 센다네. 통발로 거둔 고기도 기록되지.</size>",
                Goal = QuestGoal.DistinctSpecies, Count = n, RewardGold = gold, RewardExp = exp, RewardConch = conch,
                RewardKeepNet = keepNet, RewardBaitId = bait, RewardBaitCount = baitCount,
            };
            yield return Step(25, 5, 5000, 800, 3, "자네 도감을 보니 이제 막 첫 장을 넘겼군. 서로 다른 고기 25종을 기록해 오면 연구소 표본실을 구경시켜 주지.",
                bait: "bait_cut2", baitCount: 10);
            yield return Step(50, 12, 15000, 2000, 5, "50종이면 웬만한 낚시꾼 평생 기록이라네. 바다 셋(남해·서해·동해)을 고루 다녀 보게.", keepNet: 1);
            yield return Step(100, 22, 40000, 5000, 8, "100종! 이쯤 되면 조합 연구소 공동 연구자로 이름을 올려도 되겠어. 탄천 토종 고기와 동해 겨울 고기도 잊지 말게.",
                keepNet: 2);
            yield return Step(150, 30, 90000, 10000, 10, "150종을 넘기려면 바다 건너 거점까지 다녀야 하지. 동남아 강과 산호초, 지중해 모래밭엔 처음 보는 고기가 가득하다네.",
                bait: "bait_cut5", baitCount: 10);
            yield return Step(200, 36, 150000, 16000, 15, "200종… 내 연구 인생에서도 본 적 없는 기록일세. 나일강의 전기 물고기, 먼바다의 전설까지 모두 만나 보게.", keepNet: 3);
        }

        // ================= 낚시터 도감 퀘스트 (조합 연락관) =================

        /// <summary>낚시터 도감 퀘스트 목표 수: 그곳의 흔한~귀한 어종(괴물·어군 전용·먼바다 전용·★5 제외)의 60%, 적어도 3종.</summary>
        public static int MapDexTarget(string map)
        {
            int eligible = FishDatabase.DexOf(map).Count(s => !s.Boss && !s.SchoolOnly && !s.FarSeaOnly && s.Rarity <= 4);
            return Math.Max(3, (int)Math.Round(eligible * 0.6f));
        }

        static IEnumerable<QuestDef> MapDexQuests()
        {
            foreach (var area in FishingAreas.All)
            {
                if (!area.Open || !FishDatabase.DexOf(area.MapId).Any()) continue;
                int n = MapDexTarget(area.MapId);
                var (gold, exp) = SideReward(area.MapId, 3, 1, 1.5f);
                string key = QuestNpcs.UnionOf(area.MapId);
                yield return new QuestDef
                {
                    Id = $"dex_{area.MapId}", Type = QuestType.Normal, Lv = area.Level + 1, MapId = area.MapId, Giver = key, Reporter = key,
                    Title = $"{area.Name} 생태 도감",
                    Desc = $"조합 연구소 문박사님이 낚시터마다 어떤 고기가 사는지 도감으로 정리하고 계세요. {area.Name}에 사는 어종 {n}종을 도감(P)에 기록해 주세요.\n" +
                           "다른 낚시터에서 이미 기록한 어종도, 퀘스트를 받기 전에 기록한 어종도 모두 인정돼요. 도감 이 낚시터 탭에서 아직 못 만난 어종(실루엣)을 확인해 보세요.",
                    Goal = QuestGoal.MapDex, Count = n, RewardGold = gold, RewardExp = exp, RewardConch = 3,
                };
            }
        }

        // ================= 낚시터 어종 퀘스트 =================

        static List<QuestDef> SpeciesQuests() => new List<QuestDef>
        {
            // ---------- 한강 (Lv.1) ----------
            Ask("hangang", Who.Fisher, "nq_hangang_1", "모래 속 숨바꼭질",
                "모래무지는 모래 속에 몸을 묻고 눈만 내놓고 있다네. 얕은 여울 모래 바닥에 지렁이나 떡밥을 깔아 두면 주둥이로 쪽 빨아들이지. 두 마리만 보여 주게.",
                "pikegudgeon", 2),
            Ask("hangang", Who.Union, "nq_hangang_2", "돌 밑의 멍텅구리",
                "동사리는 돌 밑에 엎드려 꼼짝 않는다고 '멍텅구리'라 불러요. 그래도 밤이 되면 넓적한 입으로 지나가는 먹이를 덥석 문답니다. 밤에 교각 석축에서 한 마리 확인해 주세요.",
                "darksleeper", 1, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),
            Ask("hangang", Who.Shop, "nq_hangang_3", "추어탕 집 부탁",
                "건너편 추어탕 집 사장님이 토종 미꾸라지를 구한다고 난리야. 흐리거나 비 오는 날 수초 근처 진흙 바닥에 지렁이를 넣어 봐. 세 마리면 돼.",
                "pondloach", 3, bait: "bait_worm", baitCount: 20),

            // ---------- 탄천 (Lv.3, 성남 지부 생태조사) ----------
            Ask("tancheon", Who.Union, "nq_tancheon_1", "여울의 비단 줄무늬",
                "쉬리는 맑고 물살 빠른 여울에만 사는 한국 고유종이에요. 탄천에 쉬리가 돌아왔다는 건 정말 큰 뉴스죠! 맑은 아침·낮에 징검다리 여울에서 두 마리만 확인해 주세요. 플라이도 잘 들어요.",
                "splendiddace", 2, bait: "bait_fly", baitCount: 10),
            Ask("tancheon", Who.Fisher, "nq_tancheon_2", "바위 밑 텃세꾼",
                "꺽지는 제 바위 밑을 지키는 텃세꾼이야. 아가미 끝에 청록 테 두른 검은 점이 있지. 새벽이나 해 질 녘에 보 아래 돌 틈으로 작은 루어나 생미끼를 흘려 보게.",
                "auchaperch", 2),
            Ask("tancheon", Who.Union, "nq_tancheon_3", "토종 표본 기록",
                "생태조사 기록에 동사리 표본이 아직 없어요. 저녁·밤에 동사리를 낚아서 탄천 납품소에 한 마리 넣어 주세요. 문박사님 연구실로 바로 보내 드릴게요.",
                "darksleeper", 1, QuestGoal.DeliverFish),

            // ---------- 홍현 앞바다 (Lv.5, 남해) ----------
            Ask("honghyeon", Who.Shop, "nq_honghyeon_1", "미끼 도둑 졸복",
                "요즘 방파제에 졸복이 들끓어서 손님들 미끼가 남아나질 않아요. 세 마리만 잡아다 줄래요? 독이 있으니 절대 먹지 말고, 이빨에 줄 끊기지 않게 조심하고요!",
                "pantherpuffer", 3, bait: "bait_krill", baitCount: 30),
            Ask("honghyeon", Who.Fisher, "nq_honghyeon_2", "새끼 낳는 바다 붕어",
                "망상어는 알 대신 새끼를 낳는다네. 방파제 해초 사이에서 크릴만 보면 떼로 달려들지. 세 마리 잡아 오면 어린 손자 녀석한테 보여 주려네.",
                "surfperch", 3),
            Ask("honghyeon", Who.Union, "nq_honghyeon_3", "모래밭의 매복꾼",
                "양태는 넓적한 머리를 모래에 묻고 기다리다 덮치는 놈이야. 여름·가을 모래 여울에 생미끼를 바닥으로 끌어 보게. 걸리면 머리를 흔들어 대니 줄을 늦추지 말고.",
                "flathead", 1),

            // ---------- 서천 앞바다 (Lv.6, 서해) ----------
            Ask("seocheon", Who.Union, "nq_seocheon_1", "굴비의 고향",
                "말리면 굴비가 되는 참조기가 봄·가을이면 서해를 거슬러 올라와요. 저녁·밤 깊은 물에서 갯지렁이나 새우로 세 마리! 부레로 '구구' 우는 소리를 들어 보세요.",
                "yellowcroaker", 3, bait: "bait_seaworm", baitCount: 20),
            Ask("seocheon", Who.Fisher, "nq_seocheon_2", "박대묵 쑤는 날",
                "서천 사람들은 박대 껍질로 묵을 쑨다네. 갯벌 진흙 바닥에 납작 붙어 있으니 새벽이나 해 질 녘에 바닥을 노려 보게. 두 마리면 한 솥은 쑤겠어.",
                "tonguesole", 2),
            Ask("seocheon", Who.Fisher, "nq_seocheon_3", "바닥의 낚시꾼",
                "아귀는 머리 위 낚싯대로 고기를 꾀어 통째로 삼킨다네. 겨울·봄 깊은 바닥에 생미끼나 해체 미끼를 내려 두면 묵직하게 올라오지. 한 마리!",
                "anglerfish", 1),

            // ---------- 영흥도 (Lv.9, 서해) ----------
            Ask("yeongheung", Who.Fisher, "nq_yeongheung_1", "꾹꾹 우는 고기",
                "성대는 가슴지느러미 아래 다리 같은 줄기로 바닥을 걸어 다니고, 낚아 올리면 '꾹꾹' 운다네. 아침·낮 모래 바닥에서 두 마리 잡아 보게.",
                "gurnard", 2),
            Ask("yeongheung", Who.Union, "nq_yeongheung_2", "해무 속 참조기",
                "해무가 깔린 밤이면 진두항 앞 깊은 물에 참조기가 붙어요. 밤에 두 마리만 낚아 주세요. 제철(봄·가을)이면 훨씬 잘 물어요.",
                "yellowcroaker", 2, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),

            // ---------- 태안반도 연안 (Lv.11, 서해) ----------
            Ask("taean", Who.Union, "nq_taean_1", "판때기 홍어",
                "홍어는 걸리면 바닥에 들러붙어 판때기처럼 버틴다네. 겨울·봄 저녁이나 밤에 깊은 바닥으로 오징어살을 내려 보게. 한 마리!",
                "skate", 1),
            Ask("taean", Who.Fisher, "nq_taean_2", "만리포 여름 원투",
                "여름이면 만리포 모래 해변에 보리멸이 들어온다네. 갯지렁이를 멀리 던져 바닥을 살살 끌면 톡톡 쪼다 가져가지. 다섯 마리면 튀김 한 접시야.",
                "sillago", 5),
            Ask("taean", Who.Fisher, "nq_taean_3", "대양태를 찾아서",
                "60cm 가까운 양태는 꾼들도 대양태라 부르지. 모래 여울에서 생미끼나 루어로 58cm가 넘는 놈을 올려 보게.",
                "flathead", 1, QuestGoal.CatchMinLength, minLen: 58f),

            // ---------- 삼척 앞바다 (Lv.14, 동해) ----------
            Ask("samcheok", Who.Union, "nq_samcheok_1", "과메기의 원조",
                "과메기는 원래 청어로 만들었어요. 겨울·봄이면 청어 떼가 알을 낳으러 연안으로 몰려와요. 새벽·아침·저녁에 크릴이나 루어로 다섯 마리!",
                "pacificherring", 5),
            Ask("samcheok", Who.Fisher, "nq_samcheok_2", "집어등 아래 꽁치",
                "가을 밤 집어등 불빛 아래로 꽁치 떼가 모여든다네. 수면을 튀는 놈들을 크릴로 노려 보게. 밤에 세 마리!",
                "saury", 3, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),

            // ---------- 동해 등대섬 (Lv.16) ----------
            Ask("deungdae", Who.Fisher, "nq_deungdae_1", "선착장의 망상어",
                "선착장 해초 사이에 망상어가 바글바글하지. 크릴 한 줌이면 금방이야. 다섯 마리면 섬 사람들 저녁거리가 되겠어.",
                "surfperch", 5),
            Ask("deungdae", Who.Union, "nq_deungdae_2", "가을 꽁치 떼",
                "가을이 되면 찬 바다를 따라 꽁치가 섬 앞으로 내려온다오. 저녁이나 밤에 세 마리 낚아 주면 이장님이 구워 주신다더구먼.",
                "saury", 3),

            // ---------- 헌화로 (Lv.18, 동해) ----------
            Ask("heonhwa", Who.Union, "nq_heonhwa_1", "겨울 바위의 공",
                "도치는 공처럼 둥근 몸으로 배의 빨판을 바위에 붙이고 버틴대요. 한겨울 아침·낮에 바위나 해초 근처 바닥을 갯지렁이로 노려 보세요. 강릉 사람들은 도치 숙회를 최고로 쳐요.",
                "lumpsucker", 1),
            Ask("heonhwa", Who.Fisher, "nq_heonhwa_2", "삼숙이 매운탕",
                "울퉁불퉁 못생긴 삼세기를 여기선 삼숙이라 부르지. 겨울 새벽·저녁·밤에 바위 바닥에서 두 마리 잡아 오게. 매운탕 끓이면 국물이 끝내준다네.",
                "searaven", 2),

            // ---------- 욕지도 (Lv.19, 남해) ----------
            Ask("yokji", Who.Union, "nq_yokji_1", "가두리 밖의 붉은 보석",
                "가두리 둘레 암초 굴에 붉바리가 숨어 산대요. 여름·가을 새벽이나 저녁에 새우·오징어살로 노려 보세요. 걸리면 굴로 파고드니 처음부터 힘껏!",
                "redspottedgrouper", 1),
            Ask("yokji", Who.Fisher, "nq_yokji_2", "바닥을 걷는 고기",
                "성대는 날개 같은 청록 가슴지느러미를 펴면 참 곱지. 아침·낮 모래 바닥에서 두 마리 잡아 오게.",
                "gurnard", 2),
            Ask("yokji", Who.Fisher, "nq_yokji_3", "여름 해변의 보리멸",
                "여름 욕지도 모래 해변엔 보리멸이 떼로 들어온다네. 갯지렁이로 다섯 마리!",
                "sillago", 5),

            // ---------- 제주 서귀포 (Lv.21) ----------
            Ask("jeju", Who.Fisher, "nq_jeju_1", "해녀 할망의 붉바리",
                "물질하던 할망이 범섬 앞 바위굴에서 붉바리를 봤다고 하네. 다금바리만큼 귀한 고기야. 저녁·밤에 한 마리 올려 보게.",
                "redspottedgrouper", 1),
            Ask("jeju", Who.Union, "nq_jeju_2", "바다 위를 나는 고기",
                "여름 난류를 타고 날치 떼가 서귀포 앞바다로 몰려와요. 맑은 아침·낮에 크릴이나 루어로 두 마리! 물 위를 수십 미터 활공하는 모습이 장관이에요.",
                "flyingfish", 2),

            // ---------- 청진포구 (Lv.22, 동해 북부) ----------
            Ask("cheongjin", Who.Union, "nq_cheongjin_1", "포구의 대왕 아귀",
                "겨울 청진 앞바다 깊은 바닥엔 커다란 아귀가 산다오. 70cm가 넘는 놈을 낚아 오면 덕장 주인이 아귀찜을 해 준다더군.",
                "anglerfish", 1, QuestGoal.CatchMinLength, minLen: 70f),
            Ask("cheongjin", Who.Fisher, "nq_cheongjin_2", "새벽 청어 떼",
                "새벽 물안개가 걷힐 무렵 청어 떼가 포구로 들어온다우. 새벽에 세 마리 잡아 오시오.",
                "pacificherring", 3, QuestGoal.CatchSpeciesInBand, band: TimeBand.Dawn),
            Ask("cheongjin", Who.Fisher, "nq_cheongjin_3", "도치 알탕",
                "한겨울엔 도치가 알을 낳으러 얕은 바위로 올라온다우. 두 마리면 알탕 한 솥이오.",
                "lumpsucker", 2),

            // ---------- 거문도 (Lv.24, 남해 먼바다) ----------
            Ask("geomun", Who.Union, "nq_geomun_1", "왕붉바리",
                "거문도 바위굴의 붉바리는 크기가 남다르다네. 42cm가 넘는 놈을 낚아 오면 진짜 남해 갯바위 꾼이라 불러 주지.",
                "redspottedgrouper", 1, QuestGoal.CatchMinLength, minLen: 42f),
            Ask("geomun", Who.Fisher, "nq_geomun_2", "등대 앞 날치",
                "여름 아침이면 등대 앞으로 날치가 날아오른다네. 아침에 한 마리 잡아 보게.",
                "flyingfish", 1, QuestGoal.CatchSpeciesInBand, band: TimeBand.Morning),

            // ================= 해외 거점 =================
            // ---------- 하롱베이 (Lv.25) ----------
            Ask("halong", Who.Union, "nq_halong_1", "수상 마을 저녁 반찬",
                "수상 마을 집집마다 저녁 반찬은 인도고등어예요. 새벽·아침·저녁에 크릴이나 루어로 다섯 마리만 잡아 주세요.",
                "indianmackerel", 5),
            Ask("halong", Who.Fisher, "nq_halong_2", "호랑이 무늬 그루퍼",
                "봉우리 아래 바위굴에 누런 몸에 갈색 얼룩, 타이거 그루퍼가 산다네. 해 뜰 무렵이나 질 무렵 생미끼로! 물리면 굴로 처박으니 조심하게.",
                "tigergrouper", 1),
            Ask("halong", Who.Fisher, "nq_halong_3", "독가시 스캣",
                "점박이 스캣은 바다와 강을 오가며 뭐든 먹는다네. 등가시에 독이 있으니 조심해서 두 마리 잡아 오게.",
                "spottedscat", 2),

            // ---------- 앙코르와트 (Lv.26) ----------
            Ask("angkor", Who.Union, "nq_angkor_1", "은박지 물고기",
                "해자 물살 속에 은박지처럼 번쩍이는 틴포일 바브 떼가 있어요. 떡밥이나 옥수수로 다섯 마리 낚아 보세요.",
                "tinfoilbarb", 5),
            Ask("angkor", Who.Fisher, "nq_angkor_2", "쁠라 살릿 말리기",
                "뱀 껍질 무늬 스네이크스킨 구라미는 소금에 절여 말리면 별미라오. 수초 사이에서 세 마리 잡아 오시게.",
                "snakeskingourami", 3),
            Ask("angkor", Who.Fisher, "nq_angkor_3", "돌이 된 고기",
                "마블 고비는 강바닥 돌과 똑같이 생겨서 꼼짝 않으면 보이지도 않는다오. 밤이 되면 큰 입으로 사냥을 하지. 밤에 한 마리!",
                "marblegoby", 1, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),

            // ---------- 빈펄 (Lv.27) ----------
            Ask("vinpearl", Who.Union, "nq_vinpearl_1", "황금빛 입술",
                "골든 트레발리는 고무 같은 입술을 쭉 내밀어 모래 속 게를 빨아먹어요. 아침·낮 얕은 물에서 두 마리 낚아 보세요.",
                "goldentrevally", 2),
            Ask("vinpearl", Who.Fisher, "nq_vinpearl_2", "무지개 달리기",
                "동갈방어는 먼바다 부유물 밑에 떼로 산다네. 배를 타고 먼바다로 나가 루어를 끌어 보게. 한 마리면 충분하오.",
                "rainbowrunner", 1),
            Ask("vinpearl", Who.Fisher, "nq_vinpearl_3", "붉은 띠 세 줄",
                "분홍 몸에 붉은 띠 세 줄, 엠퍼러 스내퍼는 깊은 암초에서 밤에 올라온다오. 새벽·저녁·밤에 오징어살로 한 마리!",
                "emperorsnapper", 1),

            // ---------- 돈콘 (Lv.28) ----------
            Ask("donkhone", Who.Union, "nq_donkhone_1", "급류의 왕",
                "말레이 마시어는 숲에서 떨어진 열매를 받아먹고 산대요. 여울 바위 앞에 옥수수나 떡밥을 넣어 보세요. 걸리면 여울을 거슬러 쏜살같이 달아나요!",
                "mahseer", 1, bait: "bait_corn", baitCount: 30),
            Ask("donkhone", Who.Fisher, "nq_donkhone_2", "군인 같은 등지느러미",
                "솔저 리버 바브는 톱니 가시 등지느러미를 꼿꼿이 세우고 다닌다오. 아침·낮 큰 물살에서 세 마리!",
                "soldierriverbarb", 3),

            // ---------- 산토리니 (Lv.28) ----------
            Ask("santorini", Who.Union, "nq_santorini_1", "성 베드로의 물고기",
                "달고기 몸통의 검은 점은 성 베드로의 엄지 자국이래요. 새벽·아침·저녁 깊은 바위 근처에서 생미끼로 한 마리 낚아 보세요.",
                "johndory", 1),
            Ask("santorini", Who.Fisher, "nq_santorini_2", "하늘을 보는 물고기",
                "유럽통구멍은 모래에 몸을 묻고 하늘만 쳐다본다네. 어깨 가시에 독이 있으니 맨손으로 잡지 말게. 두 마리!",
                "stargazer", 2),

            // ---------- 푸켓 (Lv.30) ----------
            Ask("phuket", Who.Union, "nq_phuket_1", "무릎 깊이의 상어",
                "흑기흉상어는 무릎 깊이 산호초 여울을 등지느러미를 내놓고 돌아요. 해 질 녘에 해체 미끼나 오징어살로 한 마리! 손가락 조심하세요.",
                "blacktipreefshark", 1),
            Ask("phuket", Who.Fisher, "nq_phuket_2", "바늘 갉는 트리거",
                "타이탄 트리거피시는 바위 틈에 숨어 등가시를 빗장처럼 세운다네. 아침·낮에 두 마리. 산란철엔 사람도 쫓아와 문다니 조심하게!",
                "titantriggerfish", 2),
            Ask("phuket", Who.Fisher, "nq_phuket_3", "밤 산호의 줄무늬 입술",
                "오리엔탈 스위트립스는 낮엔 산호 그늘에서 쉬다 밤이면 모래 바닥으로 나온다오. 밤에 두 마리!",
                "orientalsweetlips", 2, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),

            // ---------- 크레타 (Lv.30) ----------
            Ask("crete", Who.Union, "nq_crete_1", "크레타의 이름",
                "지중해비늘돔의 학명(Sparisoma cretense)엔 크레타의 이름이 들어 있어요! 아침·낮 바위나 해초 근처에서 다섯 마리 낚아 보세요.",
                "medparrotfish", 5),
            Ask("crete", Who.Fisher, "nq_crete_2", "해변의 지뢰",
                "그레이터 위버는 모래 속에서 독가시만 내놓고 있다네. 밤이면 나와 돌아다니지. 밤에 두 마리 잡아 해수욕장 사람들을 안심시켜 주게.",
                "greaterweever", 2, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),
            Ask("crete", Who.Fisher, "nq_crete_3", "공작 날개",
                "유럽성대는 놀라면 공작 깃처럼 파란 날개를 활짝 편다네. 모래 바닥에서 두 마리!",
                "tubgurnard", 2),

            // ---------- 방콕 (Lv.31) ----------
            Ask("bangkok", Who.Union, "nq_bangkok_1", "순혹 요리",
                "마블 고비는 싱가포르 사람들이 '순혹'이라 부르는 귀한 생선이에요. 저녁·밤 수초 근처에서 두 마리 낚아 주세요.",
                "marblegoby", 2),
            Ask("bangkok", Who.Fisher, "nq_bangkok_2", "큰 강의 솔저 바브",
                "짜오프라야 큰 물살엔 큰 솔저 리버 바브가 산다오. 50cm가 넘는 놈을 낚아 보시게.",
                "soldierriverbarb", 1, QuestGoal.CatchMinLength, minLen: 50f),
            Ask("bangkok", Who.Fisher, "nq_bangkok_3", "강어귀의 스캣",
                "점박이 스캣이 강어귀까지 올라와 뭐든 먹는다오. 세 마리. 등가시 조심!",
                "spottedscat", 3),

            // ---------- 나일강 하류 (Lv.31) ----------
            Ask("nile", Who.Union, "nq_nile_1", "투덜대는 메기",
                "나일 시노돈티스는 낚아 올리면 '꾸룩꾸룩' 투덜거리는 뼈 투구 메기요. 새벽·저녁·밤에 지렁이로 다섯 마리! 가시에 찔리지 마시오.",
                "synodontis", 5),
            Ask("nile", Who.Fisher, "nq_nile_2", "수염 휘날리는 사냥꾼",
                "바야드 메기는 가슴지느러미까지 닿는 긴 수염을 휘날리며 저녁이면 작은 고기를 덮친다오. 저녁에 한 마리!",
                "bagrusbajad", 1, QuestGoal.CatchSpeciesInBand, band: TimeBand.Evening),
            Ask("nile", Who.Fisher, "nq_nile_3", "찌릿한 손님",
                "전기메기는 수백 볼트 전기를 품고 있다오. 고대 이집트 의사들은 그 찌릿함으로 관절통을 고쳤다지. 밤에 한 마리, 맨손으로 잡지 마시오!",
                "electriccatfish", 1, QuestGoal.CatchSpeciesInBand, band: TimeBand.Night),
            Ask("nile", Who.Union, "nq_nile_4", "살아 있는 화석",
                "등에 작은 깃발 지느러미를 줄줄이 세운 나일 비키르는 허파로 숨을 쉬는 살아 있는 화석이오. 저녁·밤 수초 사이에서 한 마리 낚아 연구소에 보여 주시오.",
                "bichir", 1),
            Ask("nile", Who.Union, "nq_nile_5", "전기로 보는 물고기",
                "짐나르쿠스는 눈이 거의 안 보이지만 전기로 미끼를 먼저 알아챈다오. 밤이나 새벽 수초·깊은 물에 생미끼를 넣어 보시오. 1.5m 넘게 자라는 나일의 전설이오!",
                "gymnarchus", 1),

            // ---------- 코사무이 (Lv.32) ----------
            Ask("kohsamui", Who.Union, "nq_kohsamui_1", "이칸 메라",
                "말라바 스내퍼는 '붉은 고기'라 불리는 바닥낚시의 꽃이에요. 밤이면 산호 둘레로 떼 지어 나와요. 오징어살로 두 마리!",
                "malabarsnapper", 2),
            Ask("kohsamui", Who.Fisher, "nq_kohsamui_2", "대불 섬 앞 엠퍼러",
                "황금 대불 섬 앞 깊은 암초에 큰 엠퍼러 스내퍼가 산다오. 75cm가 넘는 놈을 노려 보시게.",
                "emperorsnapper", 1, QuestGoal.CatchMinLength, minLen: 75f),
            Ask("kohsamui", Who.Fisher, "nq_kohsamui_3", "새벽 장 인도고등어",
                "보풋 새벽 장엔 인도고등어 '쁠라투'가 빠지면 안 된다오. 새벽에 세 마리!",
                "indianmackerel", 3, QuestGoal.CatchSpeciesInBand, band: TimeBand.Dawn),

            // ---------- 랑카위 (Lv.33) ----------
            Ask("langkawi", Who.Union, "nq_langkawi_1", "갯벌 위의 물고기",
                "자이언트 머드스키퍼는 머리 위로 튀어나온 눈을 굴리며 갯벌 위를 기어 다녀요. 맹그로브 물가에서 세 마리 낚아 보세요.",
                "giantmudskipper", 3),
            Ask("langkawi", Who.Fisher, "nq_langkawi_2", "해 질 녘 맹그로브 상어",
                "해 질 녘이면 흑기흉상어가 맹그로브 물길 어귀까지 사냥을 나온다네. 저녁에 한 마리!",
                "blacktipreefshark", 1, QuestGoal.CatchSpeciesInBand, band: TimeBand.Evening),

            // ---------- 자킨토스 (Lv.33) ----------
            Ask("zakynthos", Who.Union, "nq_zakynthos_1", "파도 속 움브리나",
                "움브리나는 파도가 부서지는 해변에서 해 질 녘 갯지렁이를 노려요. 턱 밑 짧은 수염이 귀여워요. 두 마리!",
                "shidrum", 2),
            Ask("zakynthos", Who.Fisher, "nq_zakynthos_2", "엄지 자국",
                "난파선 해변 깊은 데엔 큰 달고기가 산다네. 45cm가 넘는 놈을 올려 보게.",
                "johndory", 1, QuestGoal.CatchMinLength, minLen: 45f),

            // ---------- 센토사 (Lv.35) ----------
            Ask("sentosa", Who.Union, "nq_sentosa_1", "마리나의 붉은 고기",
                "요트 마리나 앞 깊은 물에 큰 말라바 스내퍼가 들어와요. 65cm가 넘는 놈으로 한 마리 부탁해요!",
                "malabarsnapper", 1, QuestGoal.CatchMinLength, minLen: 65f),
            Ask("sentosa", Who.Fisher, "nq_sentosa_2", "케이블카 아래 인도고등어",
                "머라이언 앞 물에 인도고등어 떼가 입을 쩍 벌리고 다닌다오. 다섯 마리면 호커 센터 사장님이 좋아할 거요.",
                "indianmackerel", 5),

            // ---------- 텔록 바텍 (Lv.36) ----------
            Ask("telokbatek", Who.Union, "nq_telokbatek_1", "롱테일 어부의 황금 고기",
                "롱테일 어부들은 75cm 넘는 골든 트레발리를 '바다의 금'이라 불러요. 아침·낮에 한 마리 올려 보세요.",
                "goldentrevally", 1, QuestGoal.CatchMinLength, minLen: 75f),
            Ask("telokbatek", Who.Fisher, "nq_telokbatek_2", "망루 아래 타이거 그루퍼",
                "망루 아래 바위에 타이거 그루퍼가 두 마리나 산다고 망루 아저씨가 그러더군. 두 마리 다 잡아 보게.",
                "tigergrouper", 2),

            // ---------- 듀덴 폭포 (Lv.36) ----------
            Ask("duden", Who.Union, "nq_duden_1", "폭포 아래 움브리나",
                "폭포 물이 바다와 만나는 곳엔 큰 움브리나가 살아요. 50cm가 넘는 놈으로 한 마리!",
                "shidrum", 1, QuestGoal.CatchMinLength, minLen: 50f),
            Ask("duden", Who.Fisher, "nq_duden_2", "꾸룩 우는 유럽성대",
                "유럽성대는 걸리면 '꾸룩' 하고 운다네. 모래 바닥에서 세 마리!",
                "tubgurnard", 3),

            // ---------- 플라우 티오만 (Lv.37) ----------
            Ask("tioman", Who.Union, "nq_tioman_1", "다이버를 쫓는 물고기",
                "산란철 타이탄 트리거피시는 다이버도 쫓아와 문대요. 다이빙 강사님이 50cm 넘는 놈을 치워 달래요!",
                "titantriggerfish", 1, QuestGoal.CatchMinLength, minLen: 50f),
            Ask("tioman", Who.Fisher, "nq_tioman_2", "동쪽 먼바다의 무지개",
                "배를 타고 동쪽 먼바다로 나가면 동갈방어 떼가 루어를 앞다투어 쫓는다오. 두 마리!",
                "rainbowrunner", 2),

            // ---------- 플라우 탱글 (Lv.39) ----------
            Ask("tenggol", Who.Union, "nq_tenggol_1", "산호초의 큰 상어",
                "별장 앞 산호초를 도는 흑기흉상어 중엔 1.2m가 넘는 놈도 있어요. 한 마리 올려 연구원에게 보여 주세요!",
                "blacktipreefshark", 1, QuestGoal.CatchMinLength, minLen: 120f),
            Ask("tenggol", Who.Fisher, "nq_tenggol_2", "퉁돔의 황제",
                "깊은 암초 바닥의 엠퍼러 스내퍼 두 마리. 새벽·저녁·밤에 오징어살로 노려 보시게.",
                "emperorsnapper", 2),
            Ask("tenggol", Who.Fisher, "nq_tenggol_3", "데크 아래 스위트립스",
                "별장 데크 아래 산호 그늘에 오리엔탈 스위트립스가 둥둥 떠 쉰다오. 저녁·밤에 세 마리!",
                "orientalsweetlips", 3),
        };
    }
}
