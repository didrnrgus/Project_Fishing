using System;
using System.Collections.Generic;
using Fishing.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fishing.Angling
{
    public enum FightOutcome { None, Landed, LineBroken, HookOut, LineRunOut }

    public enum FishMood { Run, Rest, Burst }

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

        public readonly FishSpecies Species;
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

        public const float LandingTapDistance = 0.32f;

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
        public bool AtEdge => Mathf.Abs(FishX) >= EdgeForCounter;

        public event Action Countered;
        public event Action Burst;
        public event Action EscapeRun;

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
            Length = length;
            Rod = rod;
            Reel = reel;
            Line = line;

            MaxHP = species.Stamina * Mathf.Pow(length / 10f, 1.5f) * 12.5f;
            HP = MaxHP;
            Distance = startDistance;
            Hook = Mathf.Lerp(50f, 100f, Mathf.Clamp01(hookQuality));
            _basePull = species.Power * (0.45f + 0.75f * length / species.MaxLen);
            _dir = Random.value < 0.5f ? -1 : 1;
            SetMood(FishMood.Run, Random.Range(1.2f, 2.5f));
        }

        /// <summary>staminaFactor: 체력이 떨어지면 감는 힘과 데미지가 준다. durability: 낚싯대 내구도 배율.</summary>
        public void Tick(float dt, FightInput input, float staminaFactor = 1f, float durability = 1f)
        {
            if (Outcome != FightOutcome.None) return;
            Elapsed += dt;
            if (Landing)
            {
                TickLanding(dt);
                return;
            }

            UpdateRod(dt, input.MouseDX);
            UpdateFish(dt);

            // 낚싯대를 물고기 반대쪽으로 버티면(측면 압박) 데미지가 크고 장력도 오른다
            float opposition = Mathf.Clamp01(0.5f - RodX * FishX * 0.9f);

            float reelForce = input.Reeling ? 0.33f * (Reel.ReelSpeed / 3f) : 0f;
            float raw = Pull * (0.55f + 0.45f * opposition) + reelForce;
            if (input.Releasing) raw *= 0.45f;
            raw *= 1f - 0.2f * Rod.Flex; // 연조대는 충격을 흡수한다
            float target = raw / Line.Strength;
            float response = Mathf.Lerp(7f, 4f, Rod.Flex); // 경조대일수록 장력이 빨리 튄다
            Tension = Mathf.Lerp(Tension, target, 1f - Mathf.Exp(-response * dt));

            // 거리: 줄을 풀거나 드랙이 미끄러지면 멀어지고, 감으면 가까워진다
            float takeLine = input.Releasing ? Pull * 3.5f + 1f : Mathf.Max(0f, Tension - 0.85f) * 10f;
            float reelIn = input.Reeling ? Reel.ReelSpeed * staminaFactor * Mathf.Clamp01(1.15f - Pull * 0.6f) : 0f;
            Distance = Mathf.Max(0.5f, Distance + (takeLine - reelIn) * dt);

            // 체력이 남은 물고기를 너무 가까이 끌어오면 놀라서 도망간다
            if (Distance < LandDistance && !HpLow)
            {
                if (!Escaping)
                {
                    Escaping = true;
                    _dir = Random.value < 0.5f ? -1 : 1;
                    SetMood(FishMood.Burst, 1.2f);
                    Tension += 0.25f;
                    EscapeRun?.Invoke();
                }
                Distance += 9f * dt;
            }
            else if (Distance > LandDistance + 3f) Escaping = false;

            // 바늘 걸림: 줄이 느슨하면 빠지고, 적당히 팽팽하면 단단해진다
            if (Tension < 0.12f) Hook -= 14f * dt;
            else if (Tension < DangerTension) Hook = Mathf.Min(100f, Hook + 3f * dt);
            if (Mood == FishMood.Burst && Tension > DangerTension) Hook -= 5f * dt;

            // 데미지: 하이텐션 구간에서 크게 들어간다
            float zone = Tension < 0.25f ? 0.25f : Tension < HighTension ? 0.75f : Tension < DangerTension ? 1.3f : 1.6f;
            float dmg = 8f * Rod.Power * zone * (0.6f + 0.8f * opposition) * staminaFactor * durability;
            float before = HP;
            HP = Mathf.Max(0f, HP - dmg * dt);
            _damagePool += before - HP;

            TryCounter(staminaFactor, durability);

            // 실패 판정
            if (Tension > 1f) _overTimer += dt;
            else _overTimer = Mathf.Max(0f, _overTimer - dt * 2f);
            if (_overTimer > 0.4f || Tension > 1.25f) Outcome = FightOutcome.LineBroken;
            else if (Hook <= 0f) Outcome = FightOutcome.HookOut;
            else if (Distance > Reel.Capacity) Outcome = FightOutcome.LineRunOut;
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
            float pullBack = 0.5f + _basePull * Mathf.Lerp(0.3f, 1f, HP / MaxHP) * 1.2f;
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
            _counterCooldown -= dt;
            if (_moodTimer <= 0f) PickMood();

            float exhaustion = HP <= 0f ? 0.12f : Mathf.Lerp(0.3f, 1f, HP / MaxHP);
            float moodPull = Mood switch { FishMood.Run => 1f, FishMood.Burst => 2.1f, _ => 0.3f };
            float wobble = 1f + Mathf.Sin(Elapsed * 7.3f) * 0.08f;
            Pull = _basePull * moodPull * exhaustion * wobble;

            float targetVX = Mood switch
            {
                FishMood.Run => _dir * Species.Speed * 0.45f,
                FishMood.Burst => _dir * Species.Speed * 0.9f,
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

        void PickMood()
        {
            float r = Random.value;
            if (HP <= 0f) SetMood(FishMood.Rest, 1f);
            else if (r < 0.25f * Species.Aggression)
            {
                SetMood(FishMood.Burst, 0.7f);
                Burst?.Invoke();
            }
            else if (r < 0.65f)
            {
                if (Random.value < 0.6f) _dir = -_dir;
                SetMood(FishMood.Run, Random.Range(1.5f, 3.5f) / Mathf.Max(0.4f, Species.Aggression));
            }
            else SetMood(FishMood.Rest, Random.Range(0.8f, 2f));
        }

        void SetMood(FishMood mood, float duration)
        {
            Mood = mood;
            _moodTimer = duration;
        }

        void TryCounter(float staminaFactor, float durability)
        {
            if (!CounterChance) return;
            float flick = 0f;
            foreach (var f in _flicks) flick += f.dx;
            // 화살표(물고기 반대) 방향으로 휙 틀어야 한다
            int want = CounterDirection;
            if (flick * want < FlickThreshold) return;

            float damage = MaxHP * (0.07f + 0.02f * Combo) * Rod.Power * staminaFactor * durability;
            LastCounterDamage = Mathf.Min(HP, damage);
            HP = Mathf.Max(0f, HP - damage);
            Combo = Mathf.Min(5, Combo + 1);
            _dir = want;
            FishX *= 0.7f;
            Tension *= 0.6f;
            _counterCooldown = 0.8f;
            _flicks.Clear();
            SetMood(FishMood.Rest, 1.3f);
            Countered?.Invoke();
        }
    }
}
