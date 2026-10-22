using System;
using System.Collections.Generic;
using Fishing.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fishing.Angling
{
    public enum FightOutcome { None, Landed, LineBroken, HookOut, LineRunOut }

    /// <summary>
    /// 물고기 기분. Run·Rest·Burst는 모든 어종, 나머지는 어종 패턴(<see cref="FightStyle"/>) 전용:
    /// Dive 처박기 · Shake 바늘털이 · Jump 점프 · Hold 바닥에 붙기.
    /// </summary>
    public enum FishMood { Run, Rest, Burst, Dive, Shake, Jump, Hold }

    public struct FightInput
    {
        public bool Reeling;    // 좌클릭: 줄 감기
        public bool Releasing;  // 우클릭: 줄 풀기
        public float MouseDX;   // 마우스 좌우 이동 (낚싯대 방향, 카운터)
    }

    /// <summary>
    /// 파이팅 시뮬레이션. MonoBehaviour와 분리해서 에디터에서 밸런스 시뮬레이션도 돌릴 수 있다.
    ///
    /// 화면 게이지 3종(원작 기준):
    ///   거리(Distance) - 너무 멀어지면 줄이 다 풀려 놓친다
    ///   물고기 HP      - 0에 가까워지면 랜딩 가능
    ///   장력(Tension)  - 1.0을 넘으면 줄이 끊어진다, 너무 낮으면 바늘이 빠진다(Hook)
    /// 드래그 장력: 마우스를 물고기 반대쪽으로 끌면 줄을 당기는 것이라 장력이 쌓이고(계속 당기면 끊어진다),
    ///   물고기 쪽으로 끌면 줄이 늦춰진다. 낚싯대가 끝까지 가도 계속 끌면 장력은 계속 오른다.
    /// 카운터: 물고기가 질주하거나 한쪽 끝으로 몰렸을 때 반대 방향으로 마우스를 휙 틀면 큰 데미지.
    /// 랜딩: HP가 낮은 물고기를 5m 안으로 끌어오면 랜딩 찬스. 연타로 0m까지 끌어오면 성공.
    /// 원작 안내문: "체력이 높은 물고기를 5m안으로 당겨오면 물고기가 도망가게 됩니다."
    /// </summary>
    public class FishFight
    {
        public const float LandDistance = 5f;
        public const float LandHpRatio = 0.2f;
        public const float HighTension = 0.6f;
        public const float DangerTension = 0.9f;
        const float FlickWindow = 0.15f;
        const float FlickThreshold = 12f;
        const float EdgeForCounter = 0.78f;
        const float DragGain = 0.004f;   // 마우스 이동 1단위(Mouse X 축)당 장력 (0.006은 손이 조금만 떨려도 장력이 튀었다)
        const float DragDecay = 1.6f;    // 당긴 힘이 풀리는 속도(초당, 지수)
        const float DragMax = 0.6f;
        const float DragMin = -0.45f;

        public readonly FishSpecies Species;
        /// <summary>이 어종의 파이팅 패턴 (질주·처박기·바늘털이·점프·묵직·바닥 붙기·갈팡질팡).</summary>
        public readonly FightStyle Style;
        public readonly float Length;
        public readonly GearDef Rod;
        public readonly GearDef Reel;
        public readonly GearDef Line;

        public float MaxHP { get; }
        public float HP { get; private set; }
        public float Distance { get; private set; }
        public float Tension { get; private set; }   // 줄 강도 대비 비율. 1 이상이면 위험
        public float Hook { get; private set; }      // 파이팅 레벨(바늘 걸림) 0~100
        public float FishX { get; private set; }     // 화면 좌우 위치 -1~1
        public float RodX { get; private set; }      // 낚싯대 방향 -1~1
        public int Combo { get; private set; }
        public FishMood Mood { get; private set; }
        public FightOutcome Outcome { get; private set; }
        public float Elapsed { get; private set; }
        public float Pull { get; private set; }
        /// <summary>카운터 데미지 배율 (의상).</summary>
        public float CounterBonus = 1f;
        /// <summary>감겨 있는 줄 길이 m (릴 권사량 − 끊겨 잃은 줄). 물고기가 이보다 멀리 가면 다 풀린다.</summary>
        public float LineCapacity;
        /// <summary>이번 파이팅에서 성공한 카운터 수 (원작 토스트 "연속 공격! 가장 많은 카운터를 친 파이팅").</summary>
        public int CounterCount { get; private set; }

        /// <summary>
        /// 포커싱 (원작 파이팅 스킬): 켜져 있는 동안 장력이 천천히 변하고(물고기가 당겨도 덜 튄다) 데미지가 커진다.
        /// 원작: "일정 시간 텐션 이동 속도 감소 + 데미지 증가, 접속 20분마다 1회". 쿨타임은 PlayerProfile이 잰다.
        /// </summary>
        public const float FocusDuration = 8f, FocusTensionSlow = 0.4f, FocusDamage = 1.5f, FocusCounter = 1.3f;
        public float FocusLeft { get; private set; }
        public bool Focused => FocusLeft > 0f;
        public void Focus() => FocusLeft = FocusDuration;
        /// <summary>마우스로 당겨서(+) 또는 늦춰서(-) 더해진 장력.</summary>
        public float DragLoad { get; private set; }
        /// <summary>물고기가 있는(당기는) 쪽: -1 왼쪽, +1 오른쪽.</summary>
        public int FishSide => FishX > 0.05f ? 1 : FishX < -0.05f ? -1 : _dir;

        public const float LandingTapDistance = 0.32f;
        /// <summary>
        /// 랜딩에서 버틴 시간(여러 번 합산)이 이만큼 쌓이면 물고기가 끌고 가는 힘이 LandingFatigueMin까지 줄어든다.
        /// 체력 페널티(연타 ×0.45·×0.15)를 받아도 큰 물고기를 끝내 올릴 수 있게 한다 — 없으면 랜딩·파이팅만 끝없이 오간다.
        /// </summary>
        public const float LandingFatigueTime = 12f;
        public const float LandingFatigueMin = 0.25f;
        float _landingTime;

        public bool HpLow => HP <= MaxHP * LandHpRatio;
        public bool LandingChance => Outcome == FightOutcome.None && !Landing && HpLow && Distance <= LandDistance;
        public bool Landing { get; private set; }
        public float LandingDistance { get; private set; }
        public bool Escaping { get; private set; }

        /// <summary>카운터를 칠 수 있는 순간: 질주 중이거나 한쪽 끝에 몰렸을 때.</summary>
        public bool CounterChance => _counterCooldown <= 0f && HP > 0f && !Landing &&
                                     (AtEdge || (Mood == FishMood.Burst && Mathf.Abs(FishX) > 0.3f));
        /// <summary>카운터로 당겨야 하는 방향 (-1 왼쪽, +1 오른쪽). 물고기가 가는 반대쪽.</summary>
        public int CounterDirection => FishX > 0.05f ? -1 : FishX < -0.05f ? 1 : -_dir;
        /// <summary>물고기가 달려가는 방향 (화면 끝 화살표 표시용).</summary>
        public int RunDirection => _dir;
        public float LastCounterDamage { get; private set; }
        public bool IsHighTension => Tension >= HighTension;
        public bool IsDanger => Tension >= DangerTension;

        /// <summary>개발용(자동 스크린샷): 장력을 강제로 정한다.</summary>
        public void DebugSetTension(float value) => Tension = value;
        /// <summary>개발용(자동 스크린샷): 낚싯대 방향을 강제로 정한다.</summary>
        public void DebugSetRodX(float value) => RodX = Mathf.Clamp(value, -1f, 1f);
        public bool AtEdge => Mathf.Abs(FishX) >= EdgeForCounter;

        public event Action Countered;
        public event Action Burst;
        public event Action EscapeRun;
        /// <summary>패턴 기술을 시작할 때 (처박기·바늘털이·점프·바닥 붙기). 연출·안내용.</summary>
        public event Action<FishMood> Special;

        /// <summary>점프: 물 밖에 떠 있는 동안 (장력이 빠진다).</summary>
        public bool Airborne => Mood == FishMood.Jump && _moodAge < JumpAir;
        /// <summary>점프 진행 0~1 (물 밖에 뜬 동안).</summary>
        public float AirPhase => Airborne ? _moodAge / JumpAir : 0f;
        /// <summary>패턴 기술 중인지 (HUD 안내).</summary>
        public bool InSpecial => Mood >= FishMood.Dive;

        const float JumpAir = 0.7f, JumpTotal = 1.05f;
        float _moodAge;

        float _damagePool;

        readonly float _basePull;
        float _fishVX;
        int _dir;
        float _moodTimer;
        float _overTimer;
        float _edgeTimer;
        float _counterCooldown;
        readonly List<(float t, float dx)> _flicks = new List<(float, float)>();

        public FishFight(FishSpecies species, float length, float startDistance, float hookQuality,
            GearDef rod, GearDef reel, GearDef line, int startCombo = 0)
        {
            Combo = startCombo;
            Species = species;
            Style = FishPatterns.FightOf(species);
            Length = length;
            Rod = rod;
            Reel = reel;
            Line = line;
            LineCapacity = reel.Capacity;

            MaxHP = species.Stamina * Mathf.Pow(length / 10f, 1.5f) * 12.5f;
            HP = MaxHP;
            Distance = startDistance;
            Hook = Mathf.Lerp(50f, 100f, Mathf.Clamp01(hookQuality));
            _basePull = species.Power * (0.45f + 0.75f * length / species.MaxLen);
            _dir = Random.value < 0.5f ? -1 : 1;
            SetMood(FishMood.Run, Random.Range(1.2f, 2.5f));
        }

        /// <summary>staminaFactor: 체력이 떨어지면 감는 힘과 데미지가 준다. durability: 낚싯대 내구도 배율.</summary>
        public void Tick(float dt, FightInput input, float staminaFactor = 1f, float durability = 1f, float reelFactor = -1f)
        {
            if (Outcome != FightOutcome.None) return;
            Elapsed += dt;
            if (FocusLeft > 0f) FocusLeft = Mathf.Max(0f, FocusLeft - dt);
            if (Landing)
            {
                TickLanding(dt);
                return;
            }

            UpdateRod(dt, input.MouseDX);
            UpdateFish(dt);

            // 마우스를 물고기 반대쪽으로 끌면 줄을 당기고(+), 물고기 쪽으로 끌면 늦춘다(-)
            float drag = -input.MouseDX * FishSide;
            DragLoad = Mathf.Clamp(DragLoad + drag * DragGain, DragMin, DragMax);
            DragLoad *= Mathf.Exp(-DragDecay * dt);

            // 낚싯대를 물고기 반대쪽으로 버티면(측면 압박) 데미지가 크고 장력도 오른다
            float opposition = Mathf.Clamp01(0.5f - RodX * FishX * 0.9f);

            float reelForce = input.Reeling ? 0.33f * (Reel.ReelSpeed / 3f) : 0f;
            float raw = Pull * (0.55f + 0.45f * opposition) + reelForce;
            if (input.Releasing) raw *= 0.45f;
            raw *= 1f - 0.2f * Rod.Flex; // 연조대는 충격을 흡수한다
            float target = Mathf.Max(0f, raw / Line.Strength + DragLoad * (1f - 0.25f * Rod.Flex));
            // 장력이 목표를 따라가는 속도. 경조대일수록 빨리 튄다 (포커싱이면 더 느리게).
            // 예전 7~4(0.15~0.25초에 따라잡음)는 사람 반응보다 빨라 질주 한 번에 줄이 끊어졌다 → 0.3~0.45초
            float response = Mathf.Lerp(3.4f, 2.3f, Rod.Flex) * (Focused ? FocusTensionSlow : 1f);
            Tension = Mathf.Lerp(Tension, target, 1f - Mathf.Exp(-response * dt));

            // 거리: 줄을 풀거나 드랙이 미끄러지면 멀어지고, 감으면 가까워진다
            float takeLine = input.Releasing ? Pull * 3.5f + 1f : Mathf.Max(0f, Tension - 0.85f) * 10f;
            float reelIn = input.Reeling ? Reel.ReelSpeed * (reelFactor >= 0f ? reelFactor : staminaFactor) * Mathf.Clamp01(1.15f - Pull * 0.6f) : 0f;
            if (Mood == FishMood.Hold) reelIn *= 0.35f;         // 바닥에 붙으면 감아도 잘 안 끌려온다
            if (Airborne) reelIn *= 1.6f;                       // 뛰어오른 동안 늘어진 줄을 빨리 감아 들인다
            if (Mood == FishMood.Dive) takeLine += 2.5f;        // 처박으면 줄을 끌고 바닥으로
            Distance = Mathf.Max(0.5f, Distance + (takeLine - reelIn) * dt);

            // 체력이 남은 물고기를 너무 가까이 끌어오면 놀라서 도망간다
            if (Distance < LandDistance && !HpLow)
            {
                if (!Escaping)
                {
                    Escaping = true;
                    _dir = Random.value < 0.5f ? -1 : 1;
                    SetMood(FishMood.Burst, 1.2f);
                    Tension += 0.12f;
                    EscapeRun?.Invoke();
                }
                Distance += 9f * dt;
            }
            else if (Distance > LandDistance + 3f) Escaping = false;

            // 바늘 걸림: 줄이 느슨하면 빠지고, 적당히 팽팽하면 단단해진다
            if (Tension < 0.1f) Hook -= 9f * dt;
            else if (Tension < DangerTension) Hook = Mathf.Min(100f, Hook + 3f * dt);
            if (Mood == FishMood.Burst && Tension > DangerTension) Hook -= 5f * dt;
            // 바늘털이·점프: 줄이 늘어진 틈에 바늘을 턴다 → 감아서 팽팽하게 버텨야 한다
            if (Mood == FishMood.Shake && Tension < 0.3f) Hook -= 22f * dt;
            if (Airborne && !input.Reeling) Hook -= 30f * dt;

            // 데미지: 하이텐션 구간에서 크게 들어간다
            float zone = Tension < 0.25f ? 0.25f : Tension < HighTension ? 0.75f : Tension < DangerTension ? 1.3f : 1.6f;
            float dmg = 8f * Rod.Power * zone * (0.6f + 0.8f * opposition) * staminaFactor * durability * (Focused ? FocusDamage : 1f);
            float before = HP;
            HP = Mathf.Max(0f, HP - dmg * dt);
            _damagePool += before - HP;

            TryCounter(staminaFactor, durability);

            // 실패 판정
            if (Tension > 1f) _overTimer += dt;
            else _overTimer = Mathf.Max(0f, _overTimer - dt * 2f);
            // 1.0을 넘어도 0.7초까지는 버틴다 (풀기로 대응할 시간)
            if (_overTimer > 0.7f || Tension > 1.35f) Outcome = FightOutcome.LineBroken;
            else if (Hook <= 0f) Outcome = FightOutcome.HookOut;
            else if (Distance > LineCapacity) Outcome = FightOutcome.LineRunOut;
        }

        /// <summary>랜딩 찬스에 들어간다. 이후 LandingTap() 연타로 거리를 0m까지 줄인다.</summary>
        public bool StartLanding()
        {
            if (!LandingChance) return false;
            Landing = true;
            LandingDistance = Distance;
            return true;
        }

        /// <summary>Space 또는 좌클릭 연타 한 번.</summary>
        public void LandingTap(float staminaFactor = 1f)
        {
            if (!Landing || Outcome != FightOutcome.None) return;
            LandingDistance -= LandingTapDistance * staminaFactor;
            if (LandingDistance <= 0f)
            {
                LandingDistance = 0f;
                Outcome = FightOutcome.Landed;
            }
        }

        void TickLanding(float dt)
        {
            // 지친 물고기도 마지막까지 버틴다. 5m 밖으로 밀려나면 다시 파이팅
            _landingTime += dt;
            float pullBack = (0.5f + _basePull * Mathf.Lerp(0.3f, 1f, HP / MaxHP) * 1.2f)
                * Mathf.Lerp(1f, LandingFatigueMin, _landingTime / LandingFatigueTime);
            LandingDistance += pullBack * dt;
            Tension = Mathf.Lerp(Tension, 0.5f, dt * 4f);
            FishX = Mathf.Lerp(FishX, 0f, dt * 2f);
            if (LandingDistance > LandDistance + 0.5f)
            {
                Landing = false;
                Distance = LandingDistance;
            }
        }

        /// <summary>마지막 호출 이후 들어간 데미지 합 (데미지 숫자 표시용).</summary>
        public float TakeDamagePool()
        {
            float d = _damagePool;
            _damagePool = 0f;
            return d;
        }

        void UpdateRod(float dt, float mouseDX)
        {
            RodX = Mathf.Clamp(RodX + mouseDX * 0.02f, -1f, 1f);
            _flicks.Add((Elapsed, mouseDX));
            _flicks.RemoveAll(f => Elapsed - f.t > FlickWindow);
        }

        void UpdateFish(float dt)
        {
            _moodTimer -= dt;
            _moodAge += dt;
            _counterCooldown -= dt;
            if (_moodTimer <= 0f) PickMood();

            float exhaustion = HP <= 0f ? 0.12f : Mathf.Lerp(0.3f, 1f, HP / MaxHP);
            bool heavy = Style == FightStyle.Heavy;
            float moodPull = Mood switch
            {
                FishMood.Run => heavy ? 1.15f : 1f,
                FishMood.Burst => 1.85f,
                FishMood.Dive => _moodAge < 0.25f ? Mathf.Lerp(1f, 1.95f, _moodAge / 0.25f) : 1.95f,   // 쿡 처박는다
                FishMood.Shake => 1f + 0.85f * Mathf.Sin(_moodAge * Mathf.PI * 2f * 2.4f),           // 머리를 흔들 때마다 장력이 출렁
                FishMood.Jump => Airborne ? 0.05f : 1.9f,                                           // 물 밖이면 줄이 빠지고, 떨어지면 철썩
                FishMood.Hold => 1.1f,
                _ => heavy ? 0.6f : 0.3f
            };
            float wobble = 1f + Mathf.Sin(Elapsed * 7.3f) * 0.04f;
            Pull = _basePull * moodPull * exhaustion * wobble;

            float speed = Species.Speed * (Style == FightStyle.Runner ? 1.15f : 1f);
            float targetVX = Mood switch
            {
                FishMood.Run => _dir * speed * 0.45f,
                FishMood.Burst => _dir * speed * 0.9f,
                FishMood.Dive or FishMood.Hold => -FishX * 0.5f,
                FishMood.Shake => Mathf.Sin(_moodAge * 17f) * 0.8f,
                FishMood.Jump => _fishVX,
                _ => -FishX * 0.2f
            };
            _fishVX = Mathf.Lerp(_fishVX, targetVX * exhaustion, 1f - Mathf.Exp(-4f * dt));
            FishX += _fishVX * dt;

            if (Mathf.Abs(FishX) >= 1f)
            {
                FishX = Mathf.Sign(FishX);
                _edgeTimer += dt;
                if (_edgeTimer > 1.6f)
                {
                    _dir = -(int)Mathf.Sign(FishX);
                    _edgeTimer = 0f;
                }
            }
            else _edgeTimer = 0f;
        }

        /// <summary>
        /// 다음 기분을 고른다. 어종 패턴마다 기술(처박기·바늘털이·점프·바닥 붙기)을 쓸 확률, 질주 확률·길이, 방향 바꾸기가 다르다.
        /// 기술 바로 다음에는 같은 기술을 잇달아 쓰지 않는다(쉬거나 달린다).
        /// </summary>
        void PickMood()
        {
            if (HP <= 0f) { SetMood(FishMood.Rest, 1f); return; }
            var (special, burstK, run, runLen, flip) = Style switch
            {
                FightStyle.Runner => (FishMood.Rest, 1.3f, 0.75f, 1.6f, 0.5f),
                FightStyle.Diver => (FishMood.Dive, 0.5f, 0.6f, 1f, 0.6f),
                FightStyle.Shaker => (FishMood.Shake, 0.6f, 0.62f, 1f, 0.6f),
                FightStyle.Leaper => (FishMood.Jump, 0.6f, 0.65f, 1.1f, 0.6f),
                FightStyle.Heavy => (FishMood.Rest, 0.35f, 0.75f, 1.4f, 0.4f),
                FightStyle.Clinger => (FishMood.Hold, 0.25f, 0.55f, 1f, 0.5f),
                FightStyle.Darter => (FishMood.Rest, 0.6f, 0.8f, 0.45f, 0.85f),
                _ => (FishMood.Rest, 1f, 0.65f, 1f, 0.6f),
            };
            float specialChance = special != FishMood.Rest && Mood < FishMood.Dive ? 0.3f : 0f;
            float r = Random.value;
            if (r < specialChance)
            {
                switch (special)
                {
                    case FishMood.Dive: SetMood(special, Random.Range(0.7f, 1.1f)); break;
                    case FishMood.Shake: SetMood(special, Random.Range(1f, 1.5f)); break;
                    case FishMood.Jump: SetMood(special, JumpTotal); break;
                    default: SetMood(special, Random.Range(1.5f, 2.5f)); break;
                }
                Special?.Invoke(special);
                return;
            }
            r = Random.value;
            if (r < 0.25f * Species.Aggression * burstK)
            {
                SetMood(FishMood.Burst, 0.7f);
                Burst?.Invoke();
            }
            else if (r < run)
            {
                if (Random.value < flip) _dir = -_dir;
                SetMood(FishMood.Run, Random.Range(1.5f, 3.5f) * runLen / Mathf.Max(0.4f, Species.Aggression));
            }
            else SetMood(FishMood.Rest, Random.Range(0.8f, 2f));
        }

        void SetMood(FishMood mood, float duration)
        {
            Mood = mood;
            _moodTimer = duration;
            _moodAge = 0f;
        }

        void TryCounter(float staminaFactor, float durability)
        {
            if (!CounterChance) return;
            float flick = 0f;
            foreach (var f in _flicks) flick += f.dx;
            // 화살표(물고기 반대) 방향으로 휙 틀어야 한다
            int want = CounterDirection;
            if (flick * want < FlickThreshold) return;

            float damage = MaxHP * (0.07f + 0.02f * Combo) * Rod.Power * staminaFactor * durability * CounterBonus * (Focused ? FocusCounter : 1f);
            LastCounterDamage = Mathf.Min(HP, damage);
            HP = Mathf.Max(0f, HP - damage);
            Combo = Mathf.Min(5, Combo + 1);
            CounterCount++;
            _dir = want;
            FishX *= 0.7f;
            Tension *= 0.6f;
            DragLoad = 0f;
            _counterCooldown = 0.8f;
            _flicks.Clear();
            SetMood(FishMood.Rest, 1.3f);
            Countered?.Invoke();
        }
    }
}
