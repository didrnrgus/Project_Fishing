# 05. 스프링 · IK · 애니메이션

[← 기술 문서 목차](README.md) · 코드: `Fishing/TelescopicRod.cs`, `Player/PlayerController.cs`, `Characters/CharacterAnimator.cs`

손맛의 절반은 눈으로 본다. 휘청이는 낚싯대, 고기를 들어 올리는 손, 끊김 없는 동작 전환을 만든 방법이다.

## 1. 낚싯대 휨: 덜 감쇠된 스프링

장력을 휨 각도로 바로 바꾸면 낚싯대가 딱딱하게 꺾인다. 실제 낚싯대는 **출렁인다**. 그래서 휨 각도를 스프링으로 따라가게 했다.

```csharp
public void Drive(float targetDeg, Vector3 pullWorld, float jitterDeg, float dt)
{
    dt = Mathf.Min(dt, 0.05f);                                     // 긴 프레임에 폭주하지 않게
    const float stiffness = 140f;                                  // k
    float damping = 2f * Mathf.Sqrt(stiffness) * 0.32f;            // c = 2√k · ζ,  ζ = 0.32
    _bendVel += ((targetDeg - _bend) * stiffness - _bendVel * damping) * dt;   // 가속도 → 속도
    _bend += _bendVel * dt;                                                     // 새 속도 → 위치
    …
}
```

- **반암시적(symplectic) 오일러**: 속도를 먼저 갱신하고, 그 **새 속도**로 위치를 갱신한다. 순서 하나 차이로 명시적 오일러보다 훨씬 안정적이다(에너지가 쌓여 폭주하지 않는다).
- **감쇠비 ζ로 생각한다**: `c = 2√k·ζ`. ζ = 1이면 출렁임 없이 딱 멈추고(임계 감쇠), ζ < 1이면 넘쳤다 돌아온다. 0.32는 "두세 번 출렁이고 선다".
  k와 c를 따로 만지면 감이 안 오지만, k(빠르기)와 ζ(출렁임)는 따로 상상할 수 있다.
- 고유 진동수 √140 ≈ 11.8 rad/s ≈ 1.9Hz. 낚싯대가 1초에 두 번쯤 떤다.
- `dt`를 0.05초로 자른다. 스프링은 dt가 크면 불안정해진다(대략 `dt > 2/√k`). 로딩 뒤 첫 프레임 같은 긴 프레임에서 터지지 않는다.

### 충격: 속도에 바로 더하기

```csharp
public void Kick(float degPerSec) => _bendVel += degPerSec;
```

캐스팅(-260)·카운터·챔질 때 **위치가 아니라 속도에** 충격을 준다. 스프링이 알아서 휘었다 튕겨 돌아온다. 애니메이션 커브를 따로 만들 필요가 없다.

## 2. 마디별 휨 분배: 경조 vs 연조

낚싯대는 마디(관절) 여러 개다. 전체 휨 각도를 마디마다 **거듭제곱 무게**로 나눈다.

```csharp
float exponent = Mathf.Lerp(2.4f, 1.3f, Look.Flex);     // 경조 2.4, 연조 1.3
foreach (var (_, u) in _joints) total += Mathf.Pow(u, exponent);          // u = 손잡이 0 → 끝 1
foreach (var (joint, u) in _joints)
    joint.localRotation = Quaternion.AngleAxis(bend * Mathf.Pow(u, exponent) / total, _bendAxis);
```

- 지수가 크면 무게가 끝 마디에 몰린다 → **경조대는 끝만 휜다.** 지수가 작으면 고르게 → **연조대는 손잡이까지 휜다.**
- 무게 합으로 나눠서(정규화) 전체 휨 각도는 지수와 상관없이 같다.
- 숫자 하나(`Flex`)가 파이팅 규칙([01](01-fight-simulation.md): 장력 반응 속도·충격 흡수)과 **모양**(휨 분포)에 같이 들어간다. 규칙과 보이는 것이 어긋나지 않는다.

### 휨 방향

```csharp
var local = transform.InverseTransformDirection(pullWorld);   // 줄이 당기는 방향
var axis = Vector3.Cross(Vector3.up, new Vector3(local.x, 0f, local.z).normalized);
_bendAxis = Vector3.Slerp(_bendAxis, axis, 1f - Mathf.Exp(-8f * dt));
```

줄 방향과 낚싯대 축의 외적이 회전축이다. 축도 지수 평활로 돌려서 물고기가 좌우로 달릴 때 휨 방향이 부드럽게 따라간다.

### 휨 각도 자체

`휨각 = 장력^0.8 × Lerp(50°, 85°, Flex)`. 지수 0.8(< 1)이라 낮은 장력에서도 눈에 띄게 휘고, 높은 장력에서는 덜 늘어난다. 장력 1을 넘으면 더 꺾인다.

## 3. 2관절 IK: 코사인 법칙

작은 고기를 왼손으로 들어 보이는 장면(`PlayerController.HoldFish`). 손을 목표점에 갖다 대려면 어깨·팔꿈치 각도를 구해야 한다.

```csharp
static void SolveTwoBone(Transform a, Transform b, Transform c, Vector3 target)   // 어깨, 팔꿈치, 손
{
    float lab = |b - a|, lcb = |c - b|;                                  // 위팔, 아래팔 길이
    float lat = Clamp(|target - a|, 0.01, lab + lcb - 0.002);            // 다 펴도 닿을 만큼만
    // 지금 각도
    float acab0 = acos(dot(norm(c - a), norm(b - a)));                   // 어깨에서 (손, 팔꿈치) 사이각
    float babc0 = acos(dot(norm(a - b), norm(c - b)));                   // 팔꿈치 각
    // 원하는 각도 (코사인 법칙)
    float acab1 = acos((lcb² - lab² - lat²) / (-2·lab·lat));
    float babc1 = acos((lat² - lab² - lcb²) / (-2·lab·lcb));
    var axis = normalize(cross(c - a, b - a));                           // 지금 팔이 굽은 면
    a.rotation = AngleAxis(acab1 - acab0, axis) * a.rotation;            // 1) 어깨 각 맞추기
    b.rotation = AngleAxis(babc1 - babc0, axis) * b.rotation;            // 2) 팔꿈치 각 맞추기
    a.rotation = FromToRotation(c.position - a, target - a) * a.rotation;   // 3) 팔 전체를 목표 쪽으로
}
```

- 삼각형 세 변(위팔, 아래팔, 어깨~목표 거리)을 알면 **코사인 법칙**으로 두 각을 바로 구한다. 반복 계산이 없다.
- 회전축을 **지금 팔이 굽어 있는 면**으로 잡는다. 애니메이션이 만든 팔꿈치 방향을 그대로 살려서 팔이 뒤로 꺾이지 않는다(폴 벡터 없이도).
- 마지막에 `FromToRotation`으로 팔 전체를 목표 쪽으로 돌린다. 길이는 1·2단계에서 맞췄으니 방향만 맞추면 손이 닿는다.
- `lab + lcb - 0.002`로 자르는 이유: 다 펴진 팔은 acos 입력이 정확히 ±1이 되어 축이 불안정해진다.
- IK는 애니메이션 **뒤에**(`LateUpdate` 쪽) 덮어쓴다. 그리고 가중치를 `MoveTowards`로 0→1 올려서(0.4초) 손이 스르륵 올라간다.

## 4. Playables 크로스페이드 (`CharacterAnimator`)

AnimatorController(상태 그래프)를 만들지 않고 **코드로** 클립을 섞는다.

```csharp
_graph = PlayableGraph.Create(name + "_Anim");
_mixer = AnimationMixerPlayable.Create(_graph, 0);
AnimationPlayableOutput.Create(_graph, "Output", animator).SetSourcePlayable(_mixer);

// 클립을 처음 부를 때 믹서 입력을 하나 늘려 연결 (지연 등록)
var p = AnimationClipPlayable.Create(_graph, clip);
_mixer.SetInputCount(_names.Count + 1);
_graph.Connect(p, 0, _mixer, _names.Count);

// 매 프레임: 목표 클립 가중치는 1로, 나머지는 0으로 같은 속도로
_mixer.SetInputWeight(k, Mathf.MoveTowards(w, k == _target ? 1f : 0f, Time.deltaTime * _fadeSpeed));
```

- API는 두 개뿐이다: `SetLoop(이름)`(대기·걷기·릴 감기 같은 반복 상태)과 `PlayOnce(이름)`(캐스팅·챔질 같은 한 번짜리, 끝나면 반복 상태로 돌아감).
- **모든 가중치를 같은 속도로 움직이면** 합이 대략 1로 유지된다. 전환 도중 또 전환해도 튀지 않는다.
- 한 번짜리 동작 중에 `SetLoop`이 오면 기억만 해 두고, 끝난 뒤 그 상태로 간다. 낚시 상태 머신이 매 프레임 `SetLoop`을 불러도 캐스팅 동작이 끊기지 않는다.
- `cutAt`(0~1)으로 클립 앞부분만 쓴다. Mixamo의 긴 "Fishing Cast" 한 개를 들기·던지기·챔질·릴 감기로 쪼개 쓴다([10 문서](../10-animation.md)).
- `이름_M` / `이름_F`가 있으면 성별로 고른다. 호출하는 쪽은 성별을 모른다.
- 장점: 클립 추가가 코드 한 줄, 상태 그래프 에셋이 없어 diff가 깔끔하다. 단점: 블렌드 트리·IK 레이어 같은 고급 기능은 직접 만들어야 한다.

## 5. 이동 속도에 맞춘 걸음 배속

배경 NPC(`Wanderer`)는 걷기 클립 배속을 `이동 속도 / 클립 기준 속도(2m/s)`로 맞춘다. 발이 미끄러지지 않는다.
돌아설 때는 먼저 제자리에서 몸을 돌린 뒤 걷는다. 걸으면서 도는 것보다 사람처럼 보인다.

## 옮겨 쓸 때 체크리스트

- 출렁임이 필요한 값은 스프링으로. k(빠르기)와 ζ(출렁임)로 정하고, 반암시적 오일러 + dt 상한.
- 충격은 속도에 더한다.
- 여러 관절에 나눌 땐 `u^지수 / Σ` 정규화 무게.
- 팔·다리 IK는 코사인 법칙 + 지금 굽은 면을 회전축으로.
- 코드 크로스페이드는 "모든 가중치를 같은 속도로 목표(1 또는 0)로".
