# 02. Unity 없이 밸런스 재기

[← 기술 문서 목차](README.md) · 코드: `tools/fightsim/`, `Assets/Editor/BalanceSim.cs`, `Assets/Scripts/Fishing/FightBot.cs`

"이 고기가 너무 어렵다/쉽다"를 손으로 수십 번 해 보고 판단하면 느리고 틀리기 쉽다.
이 프로젝트는 파이팅을 **봇이 수천 번 대신 하게** 해서 성공률·시간 표를 만든다. Unity가 없는 클라우드 컴퓨터에서도 돈다.

## 1. 전제: 규칙 코드에 엔진 의존을 두지 않는다

`FishFight`, `FightBot`, `BiteSelector`, 데이터 테이블(`FishDatabase`, `GearDatabase`, `RodUpgrade`, `Seasons`)은 `UnityEngine`에서 **`Mathf`와 `Random`만** 쓴다.
`Transform`·`GameObject`·`Time`은 쓰지 않는다. 시간은 인자 `dt`로 받는다.

이 규칙 하나 덕에 같은 소스 파일을 두 곳에서 컴파일한다.

| 어디서 | 어떻게 |
|---|---|
| Unity 에디터 | `BalanceSim.cs`(메뉴 `Fishing > Balance Simulation`, `tools/balance.sh`) |
| 아무 PC·클라우드 (.NET 8) | `tools/fightsim` 콘솔 앱 |

## 2. UnityEngine 스텁 (`tools/fightsim/Stubs.cs`)

게임 소스를 **복사하지 않고 그대로 링크**한다(`fightsim.csproj`의 `<Compile Include="../../Assets/Scripts/Fishing/FishFight.cs" />`).
빠진 엔진 타입은 같은 이름공간에 작게 흉내 낸다.

```csharp
namespace UnityEngine
{
    public static class Mathf
    {
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Exp(float v) => (float)System.Math.Exp(v);
        // … 게임 코드가 실제로 부르는 것만
    }
    public static class Random
    {
        static System.Random _r = new System.Random(1234);   // 시드 고정 → 결과 재현
        public static float value => (float)_r.NextDouble();
        public static float Range(float a, float b) => a + (b - a) * (float)_r.NextDouble();
    }
}
```

- 컴파일 오류가 곧 "규칙 코드에 엔진 의존이 새어 들어왔다"는 경보다. 의존이 늘면 스텁에 한 줄 더하거나 규칙 코드를 고친다.
- 난수기가 Unity와 달라서 숫자는 몇 % 다르지만 **경향은 같다**. 밸런스는 경향을 보는 일이라 충분하다.
- 데이터 소스가 참조하는 게임 쪽 타입(`GameClock`, `FishSchool`의 일부 필드)도 최소한만 스텁으로 둔다.

## 3. 봇: "적당히 잘하는 사람"

`FightBot.Input(fight)`이 매 틱 입력을 만든다.

```csharp
float targetRod = -f.FishSide * 0.8f;                 // 낚싯대는 물고기 반대쪽
float dx = (targetRod - f.RodX) * 8f;
if (f.Tension > 0.8f) dx = f.FishSide * 6f;           // 위험하면 물고기 쪽으로 늦추기
else if (f.Tension > 0.65f) dx *= 0.4f;               // 팽팽하면 천천히 겨누기
bool tooClose = !f.HpLow && f.Distance < FishFight.LandDistance + 1.5f;  // 5m 규칙을 안다
return new FightInput { Releasing = f.Tension > 0.9f, Reeling = f.Tension < 0.72f && !tooClose, MouseDX = dx };
```

- 장력을 0.72 아래면 감고 0.9 위면 푼다: 하이텐션 구간(0.6~0.9)을 지키는 **뱅뱅 제어 + 히스테리시스**.
- 카운터는 찬스마다 확률로 친다(완벽한 사람이 아니게).

### 반응 지연이 핵심

처음 봇은 매 프레임 판단해서 **모든 고기를 입문 장비로 100%** 잡았다. 사람은 그렇게 못 한다.

```csharp
// tools/fightsim/Program.cs — 감기/풀기 판단은 반응 시간마다, 조준은 매 프레임
frame = FightBot.Input(f, 0f);
if (f.Elapsed >= nextDecision)
{
    held = FightBot.Input(f, CounterChance);
    nextDecision = f.Elapsed + ReactionTime;   // 기본 0.25초, FIGHTSIM_REACTION=0.5로 "느린 사람"
}
frame.Reeling = held.Reeling;
frame.Releasing = held.Releasing;
```

- **판단(버튼)은 띄엄띄엄, 조준(마우스)은 연속**. 사람의 손 움직임과 비슷하다.
- 반응 시간을 환경 변수로 바꿔 "잘하는 사람(0.25초)"과 "보통 사람(0.5초)" 두 표를 만든다. 장비의 의미는 보통 사람 표에서 드러난다.

## 4. 몬테카를로 표

```
어종 × 길이 3종(하위 25%, 대물 기준, 최대) × 장비 7단계 × 40회
→ 성공률 · 평균 시간 · 최대 시간 · 실패 원인(끊김/바늘 빠짐/줄 다 풀림/시간 초과)
```

```bash
cd tools/fightsim
dotnet run -- report                         # docs/balance-report.md 다시 쓰기
dotnet run -- table nileperch,carp 150,190   # 골라서 콘솔에
dotnet run -- table all - 0.45 1 0.8         # 체력 20% 이하 상태로 전 어종
FIGHTSIM_REACTION=0.5 dotnet run -- table giantgrouper
dotnet run -- bites hangang                  # 미끼마다 잡동사니 확률
```

- **실패 원인을 따로 센다.** "성공률 60%"만으로는 고칠 곳을 모른다. 끊김이 많으면 장력 반응, 줄 다 풀림이 많으면 권사량·릴, 시간 초과면 HP·랜딩을 본다.
- **최대 시간**을 본다. 평균이 30초여도 최대가 240초면 어딘가에 무한 루프가 있다. 랜딩 피로([01](01-fight-simulation.md) 8장)가 이렇게 발견됐다.
- 플레이어 상태 배율(체력·내구도·릴)을 인자로 받아 "지친 상태" 같은 엣지 케이스도 표로 본다.

## 5. 조정 기록을 남긴다

`docs/04-balance.md`의 "조정 이력" 표에 **문제 → 조치 → 결과 숫자**를 차수별로 적었다.

| 차수 | 문제 | 조치 | 결과 |
|---|---|---|---|
| 1차 | 봇이 전부 100% | 반응 지연 0.25초, 질주·HP 강화 | 장비 차이가 생김 |
| 4차 | 드래그 장력 추가 | 봇이 늦추기를 쓰게 | 끊김 340 → 63회 |
| 6차 | 지친 상태에서 랜딩 무한 반복 | 랜딩 피로 | 46% → 93% |
| 7차 | 사람에게 너무 어려움 | 장력 반응 절반 | 사람 기준 그루퍼 130cm 15% → 63% |

다음 조정 때 "왜 이 숫자인가"를 다시 묻지 않게 된다. 같은 실수를 되돌리는 일도 막는다.

## 6. 같은 방식으로 잰 다른 것: 입질 분포

`dotnet run -- bites`는 `BiteSelector`를 그대로 컴파일해 **낚시터 × 미끼 × 서식지 4 × 시간대 5** 평균 잡동사니 확률을 낸다.
"바다에서 깡통만 낚인다"는 지적을 이걸로 재서 25~80%였던 것을 찾았고, 고친 뒤 4~10%가 된 것을 확인했다([03](03-weighted-random.md) 2장).

## 옮겨 쓸 때 체크리스트

- 규칙 코드는 `dt`를 인자로 받고, 엔진 API는 수학·난수만.
- 별도 프로젝트에서 소스를 **링크**(복사 X)하고, 빠진 타입만 스텁.
- 봇에 반응 지연을 넣고, 판단과 연속 조작의 주기를 나눈다.
- 성공률 + 실패 원인 + 최대 시간을 같이 본다.
- 조정은 표로 기록한다.
