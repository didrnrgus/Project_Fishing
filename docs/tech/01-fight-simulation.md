# 01. 파이팅 시뮬레이션

[← 기술 문서 목차](README.md) · 코드: `Assets/Scripts/Fishing/FishFight.cs`

낚시 게임의 손맛은 거의 다 파이팅에서 나온다. 이 프로젝트는 파이팅을 **게이지 세 개(거리·HP·장력)를 가진 작은 물리 모형**으로 만들었다.
입력은 감기·풀기·마우스 좌우 세 가지뿐이다.

## 1. 구조: 매 프레임 `Tick(dt, input)` 하나

```
FishFight.Tick(dt, input, 체력계수, 내구도계수, 릴계수)
  ├ UpdateRod   : 마우스 → 낚싯대 방향(RodX), 플릭 기록
  ├ UpdateFish  : 물고기 기분(Run/Rest/Burst) → 당기는 힘(Pull), 좌우 위치(FishX)
  ├ 장력        : 목표 장력을 계산하고 지수 평활로 따라간다
  ├ 거리        : 풀린 줄 - 감은 줄
  ├ 바늘 걸림   : 느슨하면 빠지고 적당하면 단단해진다
  ├ 데미지      : 장력 구간 × 측면 압박
  ├ 카운터      : 플릭 판정
  └ 실패 판정   : 끊김 / 바늘 빠짐 / 줄 다 풀림
```

MonoBehaviour가 아닌 평범한 클래스다. 화면(`FightHudView`)과 연출(`FishingController`)은 이 객체를 **읽기만** 한다.
이 분리 덕에 같은 코드를 밸런스 시뮬레이터에서 그대로 돌린다([02](02-headless-balance.md)).

## 2. 장력: 1차 지연 (지수 평활)

장력을 "목표값"으로 바로 바꾸면 질주 한 번에 0 → 1로 튀어서 사람이 반응할 수 없다. 그래서 목표를 **시간 상수**를 두고 따라가게 했다.

```csharp
float target = Mathf.Max(0f, raw / Line.Strength + DragLoad * (1f - 0.25f * Rod.Flex));
float response = Mathf.Lerp(3.4f, 2.3f, Rod.Flex) * (Focused ? FocusTensionSlow : 1f);
Tension = Mathf.Lerp(Tension, target, 1f - Mathf.Exp(-response * dt));
```

- `Lerp(x, target, 1 - e^(-k·dt))`는 미분방정식 `dx/dt = k(target - x)`의 **정확한 해**다. 그래서 dt가 0.016이든 0.033이든 결과가 같다.
  `Lerp(x, target, k·dt)`로 쓰면 프레임률이 낮을수록 빨리 따라가고, k·dt > 1이면 튀어 넘는다.
- 시간 상수 τ = 1/k. 경조(k 3.4) ≈ 0.3초, 연조(k 2.3) ≈ 0.45초.
- **낚싯대 성격**을 숫자 하나로 표현한다: 경조대는 빨리 따라가고(장력이 튄다), 연조대는 느리게 따라가고 충격을 20% 흡수한다(`raw *= 1 - 0.2·Flex`).
- 포커싱 스킬은 k에 0.4를 곱할 뿐이다. 새 규칙 없이 "장력이 천천히 변한다"를 만든다.

> 조정 기록(7차): 예전 k는 7~4(0.15~0.25초)였다. 사람 반응(약 0.25~0.5초)보다 빨라서 "질주 한 번, 손 떨림 한 번에 끊긴다"는 지적을 받았다. k를 절반으로 줄이자 사람 기준 성공률이 크게 올랐다.

## 3. 목표 장력 만들기

```csharp
float opposition = Mathf.Clamp01(0.5f - RodX * FishX * 0.9f);  // 물고기 반대쪽으로 버틸수록 1
float reelForce  = input.Reeling ? 0.33f * (Reel.ReelSpeed / 3f) : 0f;
float raw = Pull * (0.55f + 0.45f * opposition) + reelForce;
if (input.Releasing) raw *= 0.45f;
raw *= 1f - 0.2f * Rod.Flex;
```

- `RodX × FishX`의 부호로 "같은 쪽인가, 반대쪽인가"를 한 번에 잰다. 반대쪽이면 곱이 음수 → opposition이 커진다.
- 같은 opposition이 **장력에도, 데미지에도** 들어간다. 반대쪽으로 버티면 세게 깎지만 위험해진다. 위험과 보상이 한 변수로 묶여서 선택이 생긴다.

## 4. 드래그 장력: 누설 적분기

"마우스를 물고기 반대쪽으로 계속 끌면 줄이 팽팽해진다"는 원작 감각을 넣었다.

```csharp
float drag = -input.MouseDX * FishSide;          // + = 물고기 반대쪽으로 당김
DragLoad = Mathf.Clamp(DragLoad + drag * DragGain, DragMin, DragMax);
DragLoad *= Mathf.Exp(-DragDecay * dt);          // 손을 멈추면 서서히 풀린다
```

- **누설 적분기(leaky integrator)**: 입력을 쌓되(적분) 매 순간 조금씩 새어 나가게(감쇠) 한다. 짧은 손떨림은 금방 사라지고, 꾸준히 끈 양만 남는다.
- 위아래로 자르기(clamp)해서 무한히 쌓이지 않는다. 음수(물고기 쪽으로 끌기)는 "줄을 늦추는" 조작이 된다.
- 카운터에 성공하면 `DragLoad = 0`. 큰 동작 뒤에 상태를 비워 주면 연속 동작이 깔끔하다.
- 같은 패턴을 챔질 흔들기 판정에도 쓴다([04](04-mouse-gestures.md)).

## 5. 데미지: 구간 계단 함수

```csharp
float zone = Tension < 0.25f ? 0.25f : Tension < HighTension ? 0.75f : Tension < DangerTension ? 1.3f : 1.6f;
float dmg = 8f * Rod.Power * zone * (0.6f + 0.8f * opposition) * staminaFactor * durability * (Focused ? FocusDamage : 1f);
```

- 연속 함수 대신 **계단**을 쓴 이유: 화면 장력 바의 색 구간(LOW / 주황 HIGH / 빨강)과 1:1로 맞아서 플레이어가 "주황에 두면 세다"를 바로 배운다.
- 빨강(1.6배)이 가장 세지만 1.0을 넘은 채 0.7초면 끊긴다. 최적점이 위험선 바로 아래에 있게 해서 긴장을 만든다.
- 곱셈 계수(체력·내구도·포커싱·의상)는 **곱하기만** 한다. 새 버프가 생겨도 식이 안 바뀐다.

## 6. 물고기 AI: 타이머로 도는 3상태

```
Run (좌우로 달림, 힘 ×1) ⇄ Rest (쉼, 가운데로, 힘 ×0.3) ⇄ Burst (질주, 힘 ×1.85)
```

- 기분마다 지속 시간을 정해 두고, 끝나면 `PickMood()`가 확률로 다음 기분을 고른다. Burst 확률은 `0.25 × 어종 공격성`.
- 좌우 속도도 지수 평활로 따라가서 방향 전환이 부드럽다.
- 화면 끝(|FishX| = 1)에 1.6초 붙어 있으면 방향을 바꾼다. 끝에 몰린 순간은 **카운터 찬스**가 된다.
- 지침: `Lerp(0.3, 1, HP/MaxHP)`를 힘과 속도에 곱한다. HP가 깎일수록 저절로 약해져서 "끝이 보이는" 흐름이 생긴다.
- 흔들림 `1 + sin(7.3t)·0.04`로 일정한 힘에 맥박을 준다. 사인 주파수를 어중간한 수로 두면 다른 주기와 맞물려 반복이 덜 보인다.

## 7. 규칙 하나로 플레이 패턴 만들기: 5m 도망

> 원작 안내문: "체력이 높은 물고기를 5m 안으로 당겨오면 물고기가 도망가게 됩니다."

```csharp
if (Distance < LandDistance && !HpLow) { /* 질주 + 장력 +0.12 + 초당 9m 멀어짐 */ }
else if (Distance > LandDistance + 3f) Escaping = false;   // 8m 밖으로 나가야 다시 도망
```

- "무작정 감기"를 막는 규칙 하나로 **깎기 → 끌어오기 → 랜딩**의 단계가 생긴다.
- 들어가는 선(5m)과 풀리는 선(8m)을 다르게 둔 **히스테리시스**다. 같은 선이면 경계에서 매 프레임 켜졌다 꺼진다.

## 8. 랜딩 피로: 끝나지 않는 루프 끊기

연타로 0m까지 당기는 랜딩에서, 체력이 낮은 플레이어(연타 ×0.45)는 대물이 되미는 힘을 영영 못 이겼다. 5m 밖으로 밀려나면 다시 파이팅 → HP는 이미 바닥이라 곧 다시 랜딩 → 무한 반복.

```csharp
_landingTime += dt;   // 랜딩에 머문 시간을 여러 번에 걸쳐 합산
float pullBack = (0.5f + _basePull * Mathf.Lerp(0.3f, 1f, HP / MaxHP) * 1.2f)
    * Mathf.Lerp(1f, LandingFatigueMin, _landingTime / LandingFatigueTime);  // 12초에 걸쳐 ×1 → ×0.25
```

- 버틴 시간을 **누적**하는 게 요점이다. 밀려났다 돌아와도 이어서 지친다.
- 정상 체력에서는 랜딩이 몇 초 안에 끝나서 거의 영향이 없다(평균 0.4초 단축). 문제 상황에서만 일하는 안전장치다.
- 결과: 지친 상태 평균 성공률 46% → 93%. 회귀 테스트 `FishFightTests.TiredPlayerCanStillLandTrophyFish`로 고정했다.

## 9. 실패 판정의 유예 시간

```csharp
if (Tension > 1f) _overTimer += dt;
else _overTimer = Mathf.Max(0f, _overTimer - dt * 2f);   // 내려가면 두 배 빨리 회복
if (_overTimer > 0.7f || Tension > 1.35f) Outcome = FightOutcome.LineBroken;
```

- 한계를 넘는 순간 바로 끊지 않고 **0.7초 유예**를 둔다. 사람이 "앗" 하고 풀기를 누를 시간이다.
- 대신 너무 크게 넘으면(1.35) 즉시 끊는다. 유예와 절대 한계를 같이 두면 관대하면서도 무모함은 벌한다.
- 타이머가 0으로 바로 리셋되지 않고 두 배 속도로 줄어서, 넘었다 내렸다를 반복하면 결국 끊긴다.

## 10. 화면용 데이터는 따로 모은다

```csharp
_damagePool += before - HP;      // Tick 안에서 쌓고
public float TakeDamagePool() { float d = _damagePool; _damagePool = 0f; return d; }  // HUD가 0.55초마다 가져간다
```

시뮬레이션은 매 프레임 조금씩 깎지만, 화면에는 0.55초마다 숫자 하나가 뜬다. **표현 주기와 시뮬레이션 주기를 분리**하는 작은 버퍼다.
카운터·질주·도망 같은 순간 사건은 C# `event`(`Countered`, `Burst`, `EscapeRun`)로 알린다. 소리·카메라·진동이 각자 구독한다.

## 옮겨 쓸 때 체크리스트

- 게이지는 "목표값 + 지수 평활"로. 시간 상수를 사람 반응(0.3초 이상)보다 길게.
- 위험과 보상을 같은 변수에 묶는다(opposition).
- 경계 판정엔 히스테리시스, 실패 판정엔 유예 + 절대 한계.
- 끝나지 않는 루프가 생길 수 있는 곳엔 누적 피로 같은 수렴 장치.
