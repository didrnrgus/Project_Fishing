# 06. 절차적 지형과 맵 판정

[← 기술 문서 목차](README.md) · 코드: `World/CoastMap.cs`, `World/KoreaCoasts.cs`, `World/TerrainBuilder.cs`, `Core/Tide.cs`, `Player/BoatController.cs`

바닷가 낚시터 12곳(국내 9 + 해외 3)을 **한 개의 맵 틀**로 만든다. 맵마다 코드를 새로 짜지 않고 사양(`CoastSpec`)만 바꾼다.
그 중심에는 **높이 함수 하나**가 있다.

## 1. 세계 = 높이 함수 `HeightAt(x, z)`

지형 메쉬, 걸을 수 있는 곳, 물, 물가 거품, 풀 심기가 모두 이 함수 하나에서 나온다.

```
HeightAt(x, z)
  1. 해안선 z = ShoreAt(x)              ← 가우시안 굴곡(곶·만)의 합
  2. 해안 모양 섞기                     ← 모래 해변 / 갯벌 / 부두 / 갯바위 프로필을 x 구간 무게로 평균
  3. 사구                               ← 영역 안에서만 사인 + Perlin 언덕
  4. 곶 (Capes)                         ← max(y, 곶 높이)
  5. 섬·모래톱 (Islets)                 ← max(y, 섬 높이)
  6. 바위기둥 (Stacks)                  ← max(y, …)
```

### 1-1. 해안선: 가우시안 혹의 합

```csharp
float z = S.ShoreZ;
foreach (var b in S.ShoreBumps) z += b.z * Mathf.Exp(-(x - b.x) * (x - b.x) / (2f * b.y * b.y));
```

곶(앞으로 튀어나옴)·만(들어감)을 `(위치, 폭, 깊이)` 세 숫자로 넣는다. 가우시안은 어디서나 매끄럽고, 여러 개를 더해도 매끄럽다.
섬 맵은 `LandHalfWidth` 밖에서 해안선을 크게 남쪽으로 꺾어 바다가 땅 옆을 감싸게 한다.

### 1-2. 해안 모양 섞기: 구간 무게

```csharp
float KindWeight(float x, ShoreKind kind)   // 그 종류 구간 안이면 1, 경계 ±8m에서 부드럽게 0
    => max over segments( Smooth(X0-8, X0+8, x) * Smooth(X1+8, X1-8, x) );

y = (wb·해변(d) + wm·갯벌(d) + wq·부두(d) + wr·갯바위(d)) / (wb + wm + wq + wr);   // d = z - 해안선
```

- `Smooth(a, b, x)`는 smoothstep(`t²(3 - 2t)`)이다. 두 개를 곱하면 "구간 안에서 1, 양 끝에서 부드럽게 0"인 **사다리꼴 창**이 된다.
- 프로필은 해안선에서의 거리 d만 받는 1차원 함수다. 해변은 완만하게, 부두는 벽처럼, 갯벌은 아주 얕고 길게.
- 무게로 평균 내니 해변에서 갯바위로 넘어가는 곳이 자연스럽게 이어진다. 경계에 이음매가 생기지 않는다.

### 1-3. 지형지물은 `max`로 합친다

```csharp
y = Mathf.Max(y, cape);   // 곶·섬·바위기둥
```

- 더하기 대신 **max**: 곶 위에 섬이 겹쳐도 높이가 두 배가 되지 않는다. 각 지물을 독립적으로 설계할 수 있다.
- 지물 가장자리는 거리의 거듭제곱(`(d/w)^2.2`)과 smoothstep으로 깎고, Perlin 노이즈로 경계선을 흔들어 둥근 도형이 티 나지 않게 한다.

### 1-4. 함수 하나라서 좋은 점

| 쓰는 곳 | 어떻게 |
|---|---|
| 지형 메쉬 | `TerrainBuilder`가 높이맵 해상도로 샘플링 → Unity Terrain |
| 칠하기 | 높이·경사·종류 무게로 모래/풀/바위/진흙 층 비율 |
| 걷기·물 판정 | 아래 2장 |
| 물가 거품 | 해저 높이 텍스처로 굽기([07](07-shaders.md)) |
| 단위 테스트 | `CoastMapTests`: 물때(간조·평균·만조)마다 시작점·낚시점·연락관 자리를 걸을 수 있는지 |

지형을 저장하지 않는다. 사양에서 언제든 다시 계산하므로 맵 데이터가 텍스트 diff로 리뷰된다.

## 2. 걷기·물 판정

```csharp
public override bool IsWater(Vector3 p)
{
    if (OnBreakwater(p) || OnPlatform(p) || InStack(p, 0.5f)) return false;   // 구조물 먼저
    return HeightAt(p.x, p.z) < WaterY - 0.12f;
}

public override bool IsWalkable(Vector3 p)
{
    if (OnBreakwater(p, 0.5f) || OnPlatform(p, 0.4f)) return true;
    float h = HeightAt(p.x, p.z);
    if (TideAmplitude > 0f && h > WaterY + 0.08f && (IsMudAt(p.x, p.z) || SandBarAt(p))) return true;  // 썰물 갯벌·모래톱
    return h > BaseWaterY + 0.3f && h > WaterY + 0.05f;   // 땅은 "평균" 수면 기준
}
```

- **구조물(방파제·가두리·잔교)은 높이 함수 밖**에서 따로 판정한다. 해석적 도형(사각형·선분 거리)이라 빠르고 정확하다.
- 물과 땅 사이에 **틈(0.12m, 0.3m)** 을 둔다. 경계에서 "물이면서 땅"인 곳이 생기지 않는다.
- 땅은 평균 수면(`BaseWaterY`) 기준이라 만조에도 마을·포구는 그대로 걷는다. 갯벌·모래톱만 **현재 수면** 기준이다. 물때가 의미 있는 곳에만 영향을 준다.

## 3. 물때: 사인파 하나

```csharp
public const float PeriodMinutes = 12f * 60f + 25f;          // 실제 반일주조 주기
public static float LevelAt(float totalMinutes) => Mathf.Sin(totalMinutes / PeriodMinutes * Mathf.PI * 2f);
// 매 프레임
Level  = LevelAt(t);
Rising = LevelAt(t + 1f) > Level;                            // 1분 뒤와 비교해 들물/날물
map.WaterY = map.BaseWaterY + Level * map.TideAmplitude;     // 물 메쉬도 같이 올린다
```

- 게임 시각의 **순수 함수**다. 저장할 상태가 없고, 시간을 건너뛰어도 맞다.
- 12시간 25분은 하루(24시간)와 맞지 않아서 날마다 만조 시각이 밀린다. 실제 물때표처럼 "오늘은 몇 시가 들물인가"를 보게 된다.
- `Rising`은 미분 대신 **1분 뒤 값과 비교**한다. 부호만 필요하면 이게 가장 간단하다.
- 물이 들어올 때 갯벌에 있던 플레이어는 남쪽으로 2m씩 훑어 처음 걸을 수 있는 땅으로 옮긴다(`SafeShore`).

## 4. 데이터 주도 맵 틀

새 바닷가 낚시터는 **`CoastSpec`을 돌려주는 함수 하나**다.

| 묶음 | 예 |
|---|---|
| 물 | 깊은/얕은 물 색, 물때 폭, 해무, 늘 눈 |
| 해안선 | 기본 z, 굴곡 목록, 구간별 해안 종류 |
| 구조물 | 방파제(몸통 + 꺾인 팔 + 등대 색), 곶, 섬(모래톱 여부), 바위기둥, 가두리·데크길·잔교 |
| 풍경 플래그 | 집어등, 덕장, 대교, 크레인, 사구, 한라산, 돌하르방, 석회암 봉우리, 피라미드 … |
| 낚시 | 8×8 서식지 격자, 어군(대물 어군은 시간대별 칸) |
| 사람 | 시작점, 낚시점, NPC(조합 연락관 필수) |

- 풍경은 **비트 플래그**(`CoastFeature`)로 켠다. 빌더는 플래그가 있을 때만 그 소품을 짓는다.
- 강 맵(나일강)도 `River = true`와 맞은편 둑 `FarShoreZ`(사인 두 개로 굽이침)만 더해 같은 틀에서 만든다.
- 사양이 다 찼는지(격자 크기, 연락관, 어군 수)를 테스트가 검사한다. 사람이 빠뜨린 것을 기계가 잡는다.

## 5. 공간 탐색: 배를 띄울 자리 찾기

B를 누르면 "바라보는 쪽에서 가장 가까운, 배가 들어갈 물"을 찾는다.

```csharp
for (float r = _halfWidth + 1.2f; r <= 9f; r += 0.5f)                    // 가까운 곳부터
for (int k = 0; k <= 12; k++)
{
    float yaw = faceYaw + (k % 2 == 0 ? 1 : -1) * ((k + 1) / 2) * 15f;   // 0, +15, -15, +30, -30 … ±90
    var c = origin + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * r;
    if (HullClear(c, yaw)) { Launch(c, yaw, origin); return; }
}
```

- **거리 바깥 고리 × 각도 안쪽 고리, 각도는 정면에서 좌우 번갈아** 넓혀 간다. 플레이어가 기대한 방향에 가장 가까운 답이 먼저 나온다.
- `HullClear`: 배 몸통 6점(앞·가운데·뒤 × 양옆) + 뱃머리가 모두 물인지, 그리고 `Physics.OverlapBox`로 물 위 물체(정박한 배·가두리·바위기둥)와 겹치지 않는지.
  운전 중에도 같은 함수로 다음 위치를 검사한다. 막히면 튕긴다.

## 6. 캐스팅 막힘: 포물선을 선분으로 쪼개 레이캐스트

```csharp
float arc = dist * 0.2f + 1f;                        // 찌가 그리는 포물선 높이
for (int i = 1; i <= 14; i++)
{
    float t = i / 14f;
    var p = Vector3.Lerp(from, to, t);
    p.y += Mathf.Sin(t * Mathf.PI) * arc;            // 사인 반주기로 포물선 근사
    var seg = p - prev;
    foreach (var hit in Physics.RaycastAll(prev, seg.normalized, seg.magnitude))
    {
        if (Ignore(hit.collider) || Vector3.Distance(hit.point, from) < 1.5f) continue;
        what = Describe(hit.collider);
        return true;
    }
    prev = p;
}
```

- 곡선 경로 충돌은 **선분 14개**로 충분하다. 찌는 작고 장애물은 크다.
- 내 몸·내 배·찌, 낚싯대 바로 앞 1.5m는 뺀다.
- 막은 물체의 이름을 부모를 타고 올라가며 찾아 "**등대**에 걸려서 던질 수 없습니다"처럼 알려 준다. 실패 이유를 말해 주면 플레이어가 덜 답답하다.
- 조준 중에도 매 프레임 같은 검사를 해서 링을 빨갛게 바꾼다. 던지기 전에 안다.

## 7. 돛단배: 극곡선(폴라)

```csharp
float rel = Mathf.Abs(Mathf.DeltaAngle(_yaw, Game.Weather.WindYaw));   // 0 = 뒤바람, 180 = 맞바람
float shape = rel < 90f  ? Mathf.Lerp(0.75f, 1f, rel / 90f)            // 뒤바람 → 옆바람: 빨라짐
            : rel < 135f ? Mathf.Lerp(1f, 0.55f, (rel - 90f) / 45f)    // 옆바람이 최고
            :              Mathf.Lerp(0.55f, 0.2f, (rel - 135f) / 45f); // 맞바람: 거의 못 감
float factor = shape * Mathf.Lerp(0.6f, 1.15f, Mathf.InverseLerp(2f, 9f, WindSpeed));
```

실제 요트의 속도 극곡선(옆바람이 가장 빠르고 맞바람 45° 안은 못 감)을 **선형 구간 세 개**로 흉내 냈다. 지그재그로 바람을 거슬러 가는 요령이 저절로 생긴다.

## 옮겨 쓸 때 체크리스트

- 지형은 `HeightAt(x, z)` 하나로. 기본 모양은 무게 평균, 지물은 max.
- 구간 경계는 smoothstep 두 개를 곱한 사다리꼴 창.
- 구조물 판정은 높이 함수 밖에서 해석적으로.
- 판정 경계 사이에 틈을 둔다.
- 시간에 따라 변하는 환경(물때)은 시각의 순수 함수로.
- 자리 찾기는 "가까운 거리부터, 정면에서 좌우 번갈아".
