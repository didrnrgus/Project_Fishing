# 04. 마우스 제스처 인식

[← 기술 문서 목차](README.md) · 코드: `Assets/Scripts/Fishing/FishingController.cs`, `FishFight.cs`

원작은 버튼을 누르는 게임이 아니라 **마우스를 휘두르는** 게임이다. 캐스팅(당겼다 밀기), 챔질(좌우로 흔들기), 카운터(한쪽으로 휙) 세 가지 제스처를 아래 기법으로 읽는다.

모든 입력은 `Input.GetAxis("Mouse X/Y") × 감도`(옵션에서 0.6~1.6)로 받는다. 픽셀이 아니라 축 단위라서 해상도에 덜 민감하다(대략 1단위 ≈ 10px).

## 1. 캐스팅: 누적 + 슬라이딩 윈도우

> 원작 안내문: "왼쪽 버튼을 누르고 당긴 다음 빠르게 밀면서 버튼을 놓으면 캐스팅 됩니다."

두 가지를 따로 잰다.

| 무엇 | 방법 | 의미 |
|---|---|---|
| 당긴 양 `PullBack` | 아래로 움직인 양을 **누적** | 얼마나 젖혔나 (준비) |
| 민 양 `PushSum` | **최근 0.15초** 동안 위로 움직인 양의 합 | 얼마나 빠르게 휘둘렀나 (속도) |

```csharp
float dy = Input.GetAxis("Mouse Y") * Settings.FightSensitivity;
if (dy < 0f && PushSum(now) <= 1f) PullBack += -dy;            // 밀기 시작하면 당긴 양은 그만 센다
_pushSamples.Add((now, Mathf.Max(0f, dy)));
_pushSamples.RemoveAll(s => now - s.t > PushWindow);             // 0.15초 넘은 샘플 버리기

float pullFactor = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(PullBack / PullFull));   // 안 당겨도 55%
CastPower = Mathf.Clamp01(PushSum(now) / PushFull) * pullFactor;
```

- **슬라이딩 윈도우 합 = 속도**다. "놓는 순간의 한 프레임 속도"는 프레임률·손떨림에 흔들린다. 0.15초 창의 합이면 안정적이다.
- 창을 시간(초)으로 정의해서 프레임률이 달라도 같은 동작이 같은 값이 된다(샘플 수가 아니라 시각으로 자른다).
- 당긴 양은 **곱셈 계수**(0.55~1)로만 들어간다. 안 당겨도 던질 수 있지만, 당기면 보상이 있다. 두 요소를 곱해야 "둘 다 해야 최대"가 된다.
- 놓는 순간 `CastPower`를 확정한다. 누르는 동안엔 **낼 수 있는 최대 비거리를 미리 보여 준다**(`preview`). 입력을 배우기 쉬워진다.

### 몸짓을 같은 값에 묶기

```csharp
Game.Player.RodPitch = Mathf.Lerp(70f, 155f, pull) - CastPower * 110f;   // 당길수록 뒤로 젖히고, 밀면 앞으로
Game.Player.BodyLean = Mathf.Lerp(4f, 20f, pull) - CastPower * 26f;
```

입력 값을 그대로 캐릭터 자세에 넣는다. 손의 움직임과 화면 속 낚싯대가 같이 움직여서 "내가 던졌다"는 느낌이 난다.

## 2. 챔질 흔들기: 누설 적분기 + 래치

> 원작 안내문: "왼쪽 버튼을 누른 상태로 좌우로 흔들어 챔질하세요."

```csharp
if (_shakeSpent) return false;                                     // 이미 한 번 판정했으면 무시
_shake = _shake * Mathf.Exp(-6f * Time.deltaTime)                  // 초당 e^-6로 새어 나감
       + Mathf.Abs(Input.GetAxis("Mouse X") * Settings.FightSensitivity);
if (_shake < ShakeThreshold) return false;                         // 14를 넘으면 흔든 것
_shakeSpent = true;                                                // 래치: 버튼을 뗄 때까지 다시 판정 안 함
_shake = 0f;
```

- **`|dx|`를 쌓는다**: 왼쪽·오른쪽 방향은 상관없이 "얼마나 격렬하게 움직였나"만 본다. 좌우 왕복이 자연히 큰 값이 된다.
- **새어 나가게(감쇠)** 해서 천천히 오래 움직인 것은 쌓이지 않는다. 짧은 시간 안에 빠르게 흔들어야 문턱을 넘는다.
  감쇠 계수 6이면 시간 상수 약 0.17초 — 사람이 "휙휙" 흔드는 리듬이다.
- 감쇠를 `exp(-k·dt)`로 써서 프레임률에 상관없다. `_shake *= 0.9` 같은 고정 계수는 144fps에서 훨씬 빨리 샌다.
- **래치(`_shakeSpent`)**: 한 번 판정하면 버튼을 뗐다 눌러야 다시 판정한다. 계속 흔들면 매 프레임 챔질되는 문제를 막는다.
  상태가 바뀔 때 `_shakeSpent = Input.GetMouseButton(0)`로 초기화해서, **버튼을 누른 채로 다음 단계에 들어오면** 손을 한 번 떼야 한다.

### 판정 시점의 값으로 등급 매기기

흔들기가 문턱을 넘은 **그 순간**의 찌 잠김(`Bobber.SinkAmount`)으로 BAD~PERFECT를 정한다. 판정(언제)과 평가(얼마나 잘)를 나누면 타이밍 게임이 된다.

## 3. 카운터 플릭: 부호 있는 윈도우 합

> 원작 안내문: "화살표 방향으로 낚시대를 빠르게 당겨주세요!"

```csharp
// UpdateRod: 매 틱 (시각, dx)를 기록하고 0.15초 넘은 것은 버린다
_flicks.Add((Elapsed, mouseDX));
_flicks.RemoveAll(f => Elapsed - f.t > FlickWindow);

// TryCounter: 찬스일 때만
float flick = 0f;
foreach (var f in _flicks) flick += f.dx;           // 부호 있는 합
if (flick * CounterDirection < FlickThreshold) return;   // 원하는 방향으로 12 이상
```

- 챔질과 달리 **부호를 살린 합**이다. 반대 방향 움직임은 상쇄된다. "한쪽으로 확"만 통과한다.
- `flick × 원하는방향`으로 방향 검사와 크기 검사를 한 번에 한다.
- 찬스가 아닐 때의 움직임도 기록은 한다. 찬스가 열리는 순간 이미 휘두르던 중이면 바로 인정된다(반응이 빨라 보인다).
- 성공하면 `_flicks.Clear()` + 쿨다운 0.8초. 한 번 휘두름이 두 번 카운터로 세이지 않는다.

## 4. 연속 입력: 위치 vs 속도

파이팅 중 마우스 좌우는 **두 가지 뜻**을 가진다.

| 해석 | 계산 | 쓰임 |
|---|---|---|
| 위치 (적분) | `RodX = clamp(RodX + dx·0.02, -1, 1)` | 낚싯대 방향 |
| 누적 힘 (누설 적분) | `DragLoad += -dx·FishSide·0.004`, `×= e^(-1.6dt)` | 드래그 장력 |
| 속도 (윈도우 합) | 최근 0.15초 합 | 카운터 |

같은 입력에서 **시간 척도가 다른 세 신호**를 뽑는다. 천천히 끌면 방향과 장력이, 빠르게 휙 하면 카운터가 된다.

## 5. 감도와 접근성

- 감도는 모든 제스처 입력에 같은 곱으로 들어간다(`Settings.FightSensitivity`). 문턱을 낮추는 대신 입력을 키운다.
- 상하 반전 옵션은 `Mouse Y`에만.
- 랜딩 연타는 Space와 왼쪽 버튼 **둘 다** 받는다. 마우스를 계속 누르기 힘든 사람도 할 수 있다.
- 결과 창은 Enter/Space를 **1.2초 뒤부터** 받는다. 연타하던 손이 실수로 버튼을 누르지 않게.

## 옮겨 쓸 때 체크리스트

- 속도는 한 프레임 값이 아니라 **시간 창(초) 합**으로.
- "격렬함"은 `|dx|` 누설 적분, "방향 있는 휘두름"은 부호 있는 창 합.
- 감쇠는 `exp(-k·dt)`.
- 한 번 판정 후에는 래치로 막고, 버튼을 떼야 풀리게.
- 입력 값을 캐릭터 자세에 그대로 연결해 피드백을 준다.
