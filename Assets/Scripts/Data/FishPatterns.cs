using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Data
{
    /// <summary>입질 때 찌가 움직이는 모양.</summary>
    public enum BiteStyle { Steady, Rise, Peck, Bounce, Snap, Drag }

    /// <summary>파이팅 때 물고기가 쓰는 수 (FishFight가 기분을 고를 때 쓴다).</summary>
    public enum FightStyle { Balanced, Runner, Diver, Shaker, Leaper, Heavy, Clinger, Darter }

    /// <summary>입질 패턴의 한 순간. Dip: 찌가 잠긴 정도(1 = 다 잠김, 음수 = 솟아오름), Side: 옆으로 끌려간 거리(m), Quality: 지금 챔질하면 몇 점(0~1).</summary>
    public struct BiteFrame
    {
        public float Dip, Side, Quality;
    }

    /// <summary>
    /// 어종마다 다른 입질(찌)·파이팅 패턴. 도감 성질 글(처박는다·바늘털이·뛰어오른다·바닥에 들러붙는다 …)과 실제 낚시에서 알려진 입질을 따랐다.
    /// 목록에 없는 어종은 힘·속도·성질·사는 곳으로 정한다 (<see cref="BiteOf"/>, <see cref="FightOf"/>).
    ///
    /// 입질 시간표는 순수 함수라 테스트·시뮬레이션에서도 쓴다. 챔질 등급은 Quality로 매긴다 (FishingController.GradeFor).
    /// </summary>
    public static class FishPatterns
    {
        static readonly Dictionary<string, BiteStyle> Bites = new Dictionary<string, BiteStyle>();
        static readonly Dictionary<string, FightStyle> Fights = new Dictionary<string, FightStyle>();

        static void Bite(BiteStyle s, string ids)
        {
            foreach (var id in ids.Split(' ')) if (id.Length > 0) Bites[id] = s;
        }

        static void Fight(FightStyle s, string ids)
        {
            foreach (var id in ids.Split(' ')) if (id.Length > 0) Fights[id] = s;
        }

        static FishPatterns()
        {
            // ---- 찌 ----
            // 찌올림: 붕어·잉어 무리 (바닥 미끼를 흡입하며 머리를 들어 찌가 솟는다)
            Bite(BiteStyle.Rise, "crucian gengorobuna carp goldkoi monstercarp tilapia silverbarb giantbarb julliencarp pacu giantgourami moonlightgourami");
            // 톡톡 쪼다가 쭉: 입이 작거나 예민한 도미·놀래기·쥐치, 미끼 도둑
            Bite(BiteStyle.Peck, "blackseabream filefish whiteseabream rockfish redrockfish halfbeak giltheadbream twobandedbream saddledbream bogue pandora " +
                                 "salema damselfish rabbitfish marbledspinefoot archerfish silverpomfret spangledemperor yellowfinseabream russellsnapper " +
                                 "redporgy sheepsheadwrasse beakfish redmullet paintedcomber rainbowwrasse sandfish shad smelt obscurepuffer toadfish bluegill " +
                                 "bumpheadparrot napoleonwrasse");
            // 오르락내리락: 바닥 고기·장어류·갈치 (깜빡깜빡 몇 번 하다 가져간다)
            Bite(BiteStyle.Bounce, "jacopever scorpionfish redscorpionfish greenling goby marbledflounder flatfish flounder bagrid eel conger europeanconger moray " +
                                   "catfish africancatfish redtailcatfish hairtail whitecroaker tilefish cod hake croaker meagre dotdom octopus giantoctopus " +
                                   "goldensnapper mangrovejack atkamackerel nuchi threadfin lionfish");
            // 확 사라짐: 떼로 덤비는 등푸른 고기·포식어
            Bite(BiteStyle.Snap, "opaleye mackerel horsemackerel atlanticmackerel atlantichorsemackerel zacco kkeuri skygager sweetfish mandarin yellowmandarin " +
                                 "seabass spottedseabass europeanseabass bass snakehead monstersnakehead giantsnakehead stripedsnakehead hampalabarb " +
                                 "amberjack yellowtail spanishmackerel tyrantmackerel barracuda europeanbarracuda bluefish leerfish garfish littletunny " +
                                 "atlanticbonito narrowbarredmackerel wahoo tigerfish ladyfish tarpon queenfish arowana squid bigfinsquid cuttlefish");
            // 끌고 감: 큰 고기가 미끼를 물고 유유히 간다 (숭어·대형 메기·가오리·상어·그루퍼)
            Bite(BiteStyle.Drag, "mullet mekonggiantcatfish giantpangasius stripedcatfish giantstingray commonstingray smoothhound blueshark bullshark thresher mako " +
                                 "arapaima nileperch clownknifefish wallago goonch alligatorgar giantgrouper daewangbari duskygrouper dageumbari orangegrouper " +
                                 "coraltrout cobia japanesedace chumsalmon grenadieranchovy pollock");
            // 쭉 (기본): 참돔처럼 예신 뒤 시원하게 빨려 든다
            Bite(BiteStyle.Steady, "redseabream");

            // ---- 파이팅 ----
            Fight(FightStyle.Runner, "pacificbluefin albacore littletunny atlanticbonito leerfish bluefish europeanbarracuda blueshark thresher wahoo " +
                                     "narrowbarredmackerel longtailtuna bluefintrevally yellowtail amberjack spanishmackerel tyrantmackerel gianttrevally " +
                                     "barracuda bluefintuna julliencarp skygager giantpangasius stripedcatfish pompano threadfin squid bigfinsquid cuttlefish " +
                                     "bullshark japanesedace grenadieranchovy");
            Fight(FightStyle.Diver, "greateramberjack dentex sheepsheadwrasse redporgy whiteseabream dogtoothtuna yellowfintuna cobia coraltrout goldensnapper " +
                                    "orangegrouper mangrovejack blackseabream opaleye beakfish giantgrouper dageumbari redseabream jacopever mandarin yellowmandarin " +
                                    "spangledemperor russellsnapper yellowfinseabream giltheadbream scorpionfish redscorpionfish rockfish redrockfish greenling " +
                                    "napoleonwrasse twobandedbream");
            Fight(FightStyle.Shaker, "seabass spottedseabass europeanseabass snakehead monstersnakehead giantsnakehead stripedsnakehead bass kkeuri wallago " +
                                     "tigerfish hairtail toadfish alligatorgar clownknifefish hampalabarb conger eel");
            Fight(FightStyle.Leaper, "sailfish blackmarlin mako swordfish ladyfish tarpon queenfish mahimahi arowana chumsalmon mullet barramundi garfish halfbeak");
            Fight(FightStyle.Heavy, "carp monstercarp goldkoi catfish africancatfish mekonggiantcatfish giantbarb arapaima redtailcatfish cod hake dotdom croaker " +
                                    "meagre nileperch nuchi bagrid smoothhound bumpheadparrot pacu giantgourami gengorobuna tilefish pollock obscurepuffer");
            Fight(FightStyle.Clinger, "commonstingray giantstingray giantoctopus octopus flounder marbledflounder flatfish moray europeanconger duskygrouper goonch daewangbari");
            Fight(FightStyle.Darter, "crucian zacco bluegill sweetfish smelt mackerel horsemackerel atlanticmackerel atlantichorsemackerel bogue saddledbream pandora " +
                                     "damselfish shad sandfish goby filefish rainbowwrasse paintedcomber silverbarb moonlightgourami tilapia salema marbledspinefoot " +
                                     "rabbitfish archerfish redmullet whitecroaker silverpomfret cornetfish lionfish atkamackerel");
        }

        public static BiteStyle BiteOf(FishSpecies s)
        {
            if (s == null) return BiteStyle.Steady;
            if (Bites.TryGetValue(s.Id, out var b)) return b;
            if (s.IsJunk) return BiteStyle.Steady;
            if (s.Aggression >= 0.8f) return BiteStyle.Snap;
            if (s.Power >= 1.3f) return BiteStyle.Drag;
            if (s.Power <= 0.5f) return BiteStyle.Peck;
            return BiteStyle.Steady;
        }

        public static FightStyle FightOf(FishSpecies s)
        {
            if (s == null) return FightStyle.Balanced;
            if (Fights.TryGetValue(s.Id, out var f)) return f;
            if (s.IsJunk) return FightStyle.Heavy;   // 깡통·통발·운동화: 날뛰지 않고 그냥 무겁다
            if (s.Speed >= 1.5f && s.Power >= 0.9f) return FightStyle.Runner;
            if (s.Power <= 0.5f) return FightStyle.Darter;
            if (s.Power >= 1.3f && s.Speed <= 0.9f) return FightStyle.Heavy;
            if (s.Habitats != null && System.Array.IndexOf(s.Habitats, Habitat.Rocky) >= 0 && s.Speed <= 1.1f) return FightStyle.Diver;
            return FightStyle.Balanced;
        }

        public static string Name(BiteStyle s) => s switch
        {
            BiteStyle.Rise => "찌올림", BiteStyle.Peck => "톡톡 쪼다 쭉", BiteStyle.Bounce => "오르락내리락",
            BiteStyle.Snap => "확 사라짐", BiteStyle.Drag => "끌고 감", _ => "쭉 빨림",
        };

        public static string Hint(BiteStyle s) => s switch
        {
            BiteStyle.Rise => "찌가 쑥 솟아오른다. 끝까지 올라와 멈춘 순간이 챔질 타이밍, 다시 잠기면 늦다",
            BiteStyle.Peck => "톡톡 쪼는 예신에 속지 말 것. 쭉 끌고 들어갈 때 챔질",
            BiteStyle.Bounce => "찌가 오르락내리락한다. 깊이 내려간 순간에 챔질",
            BiteStyle.Snap => "눈 깜짝할 새 확 잠긴다. 바로 챔질하지 않으면 뱉는다",
            BiteStyle.Drag => "찌가 옆으로 끌려가며 잠긴다. 멀리 끌고 갈수록 깊게 문 것",
            _ => "찌가 천천히 잠긴다. 다 잠기는 순간 챔질",
        };

        public static string Name(FightStyle s) => s switch
        {
            FightStyle.Runner => "질주형", FightStyle.Diver => "처박기형", FightStyle.Shaker => "바늘털이형", FightStyle.Leaper => "점프형",
            FightStyle.Heavy => "묵직형", FightStyle.Clinger => "바닥 붙기형", FightStyle.Darter => "갈팡질팡", _ => "무난",
        };

        public static string Hint(FightStyle s) => s switch
        {
            FightStyle.Runner => "길고 빠르게 내달린다. 권사량과 카운터가 중요",
            FightStyle.Diver => "갑자기 바닥으로 처박는다. 장력이 튈 때 줄을 풀어 버티고, 멈추면 바로 감는다",
            FightStyle.Shaker => "머리를 흔들어 바늘을 턴다. 줄이 느슨하면 빠지니 계속 감아 팽팽하게",
            FightStyle.Leaper => "물 밖으로 뛰어오른다. 떠 있는 동안 줄을 감고, 떨어질 때 튀는 장력은 풀어 받는다",
            FightStyle.Heavy => "날뛰지는 않지만 꾸준히 무겁게 당기며 오래 버틴다",
            FightStyle.Clinger => "바닥에 들러붙으면 감아도 잘 안 끌려온다. 버티며 지치게 한다",
            FightStyle.Darter => "작은 몸으로 이리저리 방향을 자주 바꾼다",
            _ => "질주와 휴식을 섞는다",
        };

        // ================= 찌 시간표 =================

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>패턴 안에서 쓰는 횟수 (톡톡 몇 번, 오르내림 몇 번) — 입질마다 seed로 다르게.</summary>
        static int Pecks(int seed) => 2 + (seed & 0x7fffffff) % 3;
        static int Bounces(int seed) => 2 + (seed & 0x7fffffff) % 2;
        const float PeckPeriod = 0.42f, BouncePeriod = 0.8f;

        /// <summary>이 시각이 지나면 고기가 미끼를 뱉는다 (늦었다).</summary>
        public static float BiteEnd(BiteStyle s, int seed) => s switch
        {
            BiteStyle.Snap => 0.7f,
            BiteStyle.Rise => 2.95f,
            BiteStyle.Peck => Pecks(seed) * PeckPeriod + 0.8f,
            BiteStyle.Bounce => Bounces(seed) * BouncePeriod + 0.85f,
            BiteStyle.Drag => 2.4f,
            _ => 1.7f,
        };

        /// <summary>입질 t초 뒤 찌 모양과 챔질 점수.</summary>
        public static BiteFrame BiteAt(BiteStyle s, float t, int seed)
        {
            var f = new BiteFrame();
            switch (s)
            {
                case BiteStyle.Snap:
                    f.Quality = Mathf.Clamp01(t / 0.28f);
                    f.Dip = Ease(f.Quality);
                    break;

                case BiteStyle.Rise:
                    if (t < 0.18f) { f.Dip = 0.12f * Mathf.Sin(Mathf.PI * t / 0.18f); f.Quality = 0f; }       // 살짝 깜빡
                    else if (t < 1.9f) { float u = (t - 0.18f) / 1.72f; f.Dip = -0.7f * Ease(u); f.Quality = u; } // 쑥 솟는다
                    else if (t < 2.35f) { f.Dip = -0.7f; f.Quality = 1f; }                                       // 꼭대기에서 멈칫
                    else { f.Dip = Mathf.Lerp(-0.7f, 1f, Ease((t - 2.35f) / 0.35f)); f.Quality = 0.72f; }         // 끌고 들어간다
                    break;

                case BiteStyle.Peck:
                {
                    float t0 = Pecks(seed) * PeckPeriod;
                    if (t < t0)
                    {
                        float ph = t % PeckPeriod;
                        f.Dip = ph < 0.14f ? 0.32f * Mathf.Sin(Mathf.PI * ph / 0.14f) : 0f;
                        f.Quality = ph < 0.14f ? 0.12f : 0f;
                    }
                    else { f.Quality = Mathf.Clamp01((t - t0) / 0.3f); f.Dip = Ease(f.Quality); }
                    break;
                }

                case BiteStyle.Bounce:
                {
                    int n = Bounces(seed);
                    float t0 = n * BouncePeriod;
                    if (t < t0)
                    {
                        int i = Mathf.Min(n - 1, (int)(t / BouncePeriod));
                        float amp = 0.35f + 0.25f * i;
                        f.Dip = amp * Mathf.Sin(Mathf.PI * (t - i * BouncePeriod) / BouncePeriod);
                        f.Quality = f.Dip;
                    }
                    else { f.Quality = Mathf.Clamp01((t - t0) / 0.35f); f.Dip = Ease(f.Quality); }
                    break;
                }

                case BiteStyle.Drag:
                {
                    float u = Mathf.Clamp01(t / 1.6f);
                    f.Quality = u;
                    f.Dip = 0.55f * Ease(u) + 0.45f * Ease((t - 1.6f) / 0.3f);
                    f.Side = (1.8f * Ease(u) + 0.6f * Mathf.Clamp01((t - 1.6f) / 0.8f)) * ((seed & 1) == 0 ? 1f : -1f);
                    break;
                }

                default:
                    f.Quality = Mathf.Clamp01(t / 1.1f);
                    f.Dip = f.Quality;
                    break;
            }
            return f;
        }

        /// <summary>본신 전 예신(깔짝) 횟수 범위 [min, max).</summary>
        public static (int min, int max) Nibbles(BiteStyle s) => s switch
        {
            BiteStyle.Snap => (0, 2),
            BiteStyle.Rise => (0, 2),
            BiteStyle.Peck => (1, 4),
            BiteStyle.Bounce => (1, 3),
            BiteStyle.Drag => (0, 2),
            _ => (0, 4),
        };
    }
}
