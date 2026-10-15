using System.Collections.Generic;

namespace Fishing.Data
{
    public enum QuestGoal { CatchAny, CatchSpecies, DistinctSpecies, CatchInHabitat, CatchSpeciesInBand, CatchMinLength }

    public sealed class QuestDef
    {
        public string Title;
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
                    _ => ""
                };
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
                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>한국 낚시 조합의 이야기 퀘스트. 순서대로 진행된다.</summary>
    public static class QuestDatabase
    {
        public const string Giver = "한국 낚시 조합";

        public static readonly List<QuestDef> Story = new List<QuestDef>
        {
            new QuestDef { Title = "첫 손맛", Desc = "조합에 온 걸 환영하네! 우선 한 마리 낚아서 손맛부터 보게.",
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
                RewardGold = 20000, RewardExp = 1000 },

            // ---------- 홍현 앞바다 (Lv.5, 배를 받으면 열린다) ----------
            new QuestDef { Title = "남해로", Desc = "한강을 졸업했으면 바다로 가 보게. 홍현 앞바다 고등어 떼부터 맛보게나. (대기실에서 홍현 앞바다 입장)",
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
                RewardKeepNet = 3, RewardCostume = "top_union" },

            // ---------- 서천 앞바다 (Lv.6, 서해) ----------
            new QuestDef { Title = "서해 노을", Desc = "서천 앞바다에 가 보게. 춘장대 갯벌 가에서 망둥어부터 낚아 보면 서해 맛을 알 걸세.",
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
                RewardKeepNet = 2 },

            // ---------- 영흥도 (Lv.9, 서해) ----------
            new QuestDef { Title = "해무 속의 섬", Desc = "영흥도는 흐린 날 해무가 짙게 깔리는 섬이라네. 진두항 방파제에서 우럭부터 낚아 보게. (Lv.9부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "jacopever", Count = 2, RewardGold = 5000, RewardExp = 700,
                RewardBaitId = "bait_minnow", RewardBaitCount = 10 },
            new QuestDef { Title = "목섬 가는 길", Desc = "썰물이면 목섬까지 모래톱이 드러난다네. 물골을 오가는 점농어를 생미끼로 노려 보게. 물이 들기 전에 돌아오고!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "spottedseabass", Count = 1, RewardGold = 10000, RewardExp = 1200 },

            // ---------- 태안반도 연안 (Lv.11, 서해) ----------
            new QuestDef { Title = "만리포의 원투", Desc = "만리포 긴 해변은 광어 밭이라네. 생미끼를 멀리 던져 모래 바닥을 노려 보게. (Lv.11부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "flounder", Count = 1, RewardGold = 7000, RewardExp = 1000 },
            new QuestDef { Title = "대광어", Desc = "70cm가 넘는 광어를 꾼들은 대광어라 부르지. 할미·할아비바위 노을을 보며 한 마리 올려 보게.",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "flounder", MinLen = 70f, Count = 1, RewardGold = 15000, RewardExp = 1600 },

            // ---------- 삼척 앞바다 (Lv.14, 동해) ----------
            new QuestDef { Title = "동해의 맛", Desc = "동해로 가 보게! 삼척 앞바다 갯바위에서 누런 몸에 검은 띠, 임연수어를 낚아 오게. (Lv.14부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "atkamackerel", Count = 3, RewardGold = 7000, RewardExp = 1200,
                RewardBaitId = "bait_egi", RewardBaitCount = 10 },
            new QuestDef { Title = "집어등 아래", Desc = "밤이 되면 수평선에 오징어배 불빛이 줄지어 켜진다네. 에기로 밤바다의 오징어를 노려 보게.",
                Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "squid", Band = TimeBand.Night, Count = 2, RewardGold = 12000, RewardExp = 1600 },

            // ---------- 동해 등대섬 (Lv.16) ----------
            new QuestDef { Title = "등대섬의 열기", Desc = "등대섬 물은 바닥이 보일 만큼 맑다네. 바위 아래 붉은 열기(불볼락)가 줄줄이 문다지. (Lv.16부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "redrockfish", Count = 4, RewardGold = 9000, RewardExp = 1500 },
            new QuestDef { Title = "방어 떼를 쫓아", Desc = "새벽과 저녁에 방어 떼가 섬을 돌아 나간다네. 배를 타고 먼 물에서 생미끼나 루어로 붙어 보게.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "yellowtail", Count = 1, RewardGold = 15000, RewardExp = 1900 },

            // ---------- 헌화로 (Lv.18, 동해) ----------
            new QuestDef { Title = "헌화가", Desc = "옛날 한 노인이 이 절벽의 진달래를 꺾어 수로부인에게 바쳤다지. 우리는 꽃 대신 새벽 갯바위의 감성돔을 바치세. (Lv.18부터 입장)",
                Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "blackseabream", Band = TimeBand.Dawn, Count = 1, RewardGold = 15000, RewardExp = 2000 },
            new QuestDef { Title = "바위굴의 거인", Desc = "헌화로 갯바위 굴에 대문어가 산다네. 새벽 D2, 저녁 G2에 굴 밖으로 나온다지. 바닥에 붙으면 버티기 싸움일세.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "giantoctopus", Count = 1, RewardGold = 25000, RewardExp = 2600 },

            // ---------- 욕지도 (Lv.19, 남해) ----------
            new QuestDef { Title = "가두리 손님", Desc = "욕지도 가두리 낚시터 둘레엔 참돔이 모여든다네. 통로를 걸으며 그물 칸 사이로 찌를 넣어 보게. (Lv.19부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "redseabream", Count = 2, RewardGold = 15000, RewardExp = 2200 },
            new QuestDef { Title = "은빛 칼", Desc = "밤이면 갈치가 몸을 세우고 수면 가까이 올라온다네. 해체 미끼나 오징어살로 노려 보게.",
                Goal = QuestGoal.CatchSpeciesInBand, SpeciesId = "hairtail", Band = TimeBand.Night, Count = 3, RewardGold = 18000, RewardExp = 2500 },

            // ---------- 제주 서귀포 (Lv.21) ----------
            new QuestDef { Title = "외돌개 아래", Desc = "제주에 왔으면 자리돔부터 맛봐야지! 외돌개 앞 갯바위 물속에 구름처럼 떼 지어 있다네. 크릴 한 줌이면 된다. (Lv.21부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "damselfish", Count = 3, RewardGold = 12000, RewardExp = 2400,
                RewardBaitId = "bait_egi", RewardBaitCount = 10 },
            new QuestDef { Title = "진상품 옥돔", Desc = "임금님께 올리던 옥돔이라네. 깊은 모래펄 바닥에 갯지렁이를 내려 보게. 해녀 할망이 옥돔국을 끓여 준다더군.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "tilefish", Count = 1, RewardGold = 22000, RewardExp = 2800 },

            // ---------- 청진포구 (Lv.22, 동해 북부) ----------
            new QuestDef { Title = "북쪽 끝 항구", Desc = "청진포구는 눈 덮인 큰 항구라네. 깊은 물에서 입 큰 대구부터 낚아 보게. (Lv.22부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "cod", Count = 1, RewardGold = 20000, RewardExp = 2800 },
            new QuestDef { Title = "명태의 고향", Desc = "국민 생선 명태가 여기서 왔다네. 새벽엔 B5, 밤엔 F6 어군에만 들어오지. 덕장 주인이 꼭 한 마리 보고 싶다더군.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "pollock", Count = 1, RewardGold = 60000, RewardExp = 4000, RewardKeepNet = 2 },

            // ---------- 거문도 (Lv.24, 남해 먼바다) ----------
            new QuestDef { Title = "거문도 등대", Desc = "1905년에 불을 밝힌 거문도 등대 아래 갯바위는 돌돔의 집이라네. 맑은 낮에 갯지렁이로 노려 보게. (Lv.24부터 입장)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "beakfish", Count = 1, RewardGold = 25000, RewardExp = 3000 },
            new QuestDef { Title = "미터급 부시리", Desc = "마지막 부탁일세. 거문도 아침 G4, 낮 C6에 몰려오는 부시리 무리에서 1m가 넘는 놈을 낚아 오게. 그럼 자네가 진짜 강태공이지!",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "amberjack", MinLen = 100f, Count = 1, RewardGold = 80000, RewardExp = 5000, RewardKeepNet = 3 },
        };
    }
}
