# 08. 성능

[← 기술 문서 목차](README.md) · 코드: `World/WorldBatcher.cs`, `World/GrassField.cs`, `Dev/DevAutomation.cs`

맵을 전부 코드로 짓다 보니 다리 아치, 데크 장선, 방파제 이음매까지 **프리미티브 수백~수천 개**가 된다. 하나씩 그리면 드로우 콜이 폭증한다.

## 1. 런타임 정적 배칭: 무엇을 묶을지 고르기

`MapLoader.Load`가 월드를 지은 직후 한 번 부른다.

```csharp
public static void Batch(Transform root)
{
    var gos = new List<GameObject>();
    foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(false))
        if (CanBatch(mr, root)) gos.Add(mr.gameObject);
    if (gos.Count >= 2) StaticBatchingUtility.Combine(gos.ToArray(), root.gameObject);
}
```

`StaticBatchingUtility.Combine`은 같은 재질 메쉬를 **월드 좌표로 합쳐 굳힌다**. 그래서 묶이면 안 되는 것을 걸러 내는 게 전부다.

| 조건 | 이유 |
|---|---|
| 렌더러가 켜져 있다 | 꺼진 것을 묶으면 나중에 켤 수 없다 |
| 메쉬가 읽을 수 있다(`isReadable`) | Combine이 버텍스를 읽어야 한다. Read/Write를 끈 FBX는 제외 |
| 셰이더가 `Standard` 또는 `Fishing/Ground` | 물·하늘·물고기·파티클 셰이더는 **오브젝트 좌표**·버텍스 애니메이션을 써서 합치면 깨진다 |
| 자기와 조상(루트 제외)에 `MonoBehaviour`·`Animator`·`Rigidbody`가 없다 | 스크립트가 붙은 것은 **런타임에 움직일 수 있다**(NPC, 어군, 수족관, 배, 잔물결) |

```csharp
for (var t = mr.transform; t != null && t != root; t = t.parent)
    if (t.GetComponent<MonoBehaviour>() != null || t.GetComponent<Animator>() != null || t.GetComponent<Rigidbody>() != null)
        return false;
```

- **"스크립트가 있으면 움직인다"는 보수적 규칙**이 핵심이다. 하나하나 표시(`isStatic`)하는 대신 구조로 판단하니, 새 소품을 만들 때 신경 쓸 게 한 가지뿐이다: *움직일 거면 스크립트를 그 물체나 부모에 붙인다.*
- 재질 **색**을 나중에 바꾸는 것(겨울 눈 덮인 둔치, 밤 가로등 전구)은 묶여도 동작한다. 바뀌는 게 버텍스가 아니라 머티리얼이라서.
- 나중에 생기는 것(얼음판·얼음 구멍)은 자연히 안 묶인다.
- 바닥 셰이더를 월드 좌표로 만든 이유도 이것이다([07](07-shaders.md) 2장).

## 2. 풀: 칸 메쉬 + 거리 컬링

풀 포기는 맵마다 최대 42,000개를 심는다. 포기마다 GameObject를 만들면 감당이 안 된다.

```
GrassField.Plant(world, waterY, seed):
  1.15m 간격으로 훑으며 위에서 레이캐스트 → 처음 맞은 게 "풀 텍스처 바닥"일 때만 심는다
     (길·건물·데크 위, 물가, 가파른 곳은 자동으로 빠진다)
  포기 = 교차한 판 두 장 (무작위 방향·크기·색)
  48m × 48m 칸마다 메쉬 하나로 굽는다 (그림 종류마다 서브메쉬)
GrassCuller: 카메라에서 130m 밖 칸은 끈다
```

- **레이캐스트로 심을 곳 고르기**: "어디가 풀밭인가"를 따로 데이터로 두지 않고, 이미 지은 월드에 물어본다. 맵이 바뀌어도 코드는 같다.
- **칸 단위 메쉬**: 드로우 콜은 칸 수 × 그림 수로 줄고, 칸이 컬링 단위가 된다. 칸이 너무 작으면 콜이 늘고, 너무 크면 컬링이 거칠다. 지형이 약 420m 폭이라 48m 칸이면 한 줄에 9칸쯤이다.
- 난수 시드를 맵 id로 만들어(`map.Id.GetHashCode()`) 같은 맵에는 같은 풀이 난다.
- 흔들림은 버텍스 셰이더에서([07](07-shaders.md) 4장). CPU는 아무것도 안 한다.

## 3. 측정을 자동화한다

자동 촬영(`-autoshot`, [10](10-dev-workflow.md))이 스크린샷마다 로그를 남긴다.

```
(NN FPS, 맵, 정적 배칭 N개)
```

`tools/autoshot.sh`가 끝나면 **맵별 평균 FPS**를 출력한다. 소품을 많이 더한 뒤 어느 맵이 느려졌는지 한눈에 본다. 성능은 "느낌"이 아니라 숫자로 회귀를 잡는다.

## 4. z-fighting 막기 규칙

코드로 판을 겹쳐 쌓다 보면 같은 높이 면이 깜빡인다. 규칙으로 정해 두었다.

| 규칙 | 값 |
|---|---|
| 카메라 near | 평소 0.2m, 1인칭 파이팅만 0.05m(눈앞 낚싯대) |
| 지형 정밀도 | `heightmapPixelError` 2 |
| 땅 위에 까는 판(길·산책로·앞마당) | 5cm 넘게 띄운다 |
| 같은 높이로 겹치는 토막(도로 이음매·가두리 통로 교차·수련잎) | 높이를 엇갈린다 |
| 한강 데크가 둔치에 걸치는 곳 | 윗면을 6cm 높이고 끝에 비스듬한 문턱 |

near를 0.05로 계속 두면 먼 곳 깊이 정밀도가 떨어져 멀리서 깜빡인다. **필요한 장면에서만** 줄인다.

## 5. 그리는 크기를 FHD로 묶기 (`ResolutionGuard`, 2026-11-25)

창을 끌어 키우거나 4K 모니터에서 전체 화면으로 켜면 Unity는 그만큼 큰 화면을 그린다. 픽셀 수가 1920×1080의 4배가 되면 셰이더·후처리 비용도 그만큼 늘어 중급 그래픽카드에서 프레임이 떨어졌다. 그래서 **그리는 크기의 상한을 FHD로** 둔다.

| 곳 | 하는 일 |
|---|---|
| 플레이어 설정 | `resizableWindow` 끔(창 끌어서 키우기·최대화 막음), `defaultIsNativeResolution` 끔(모니터 원래 해상도로 시작하지 않음). `ProjectTools.Setup`이 같은 값을 쓴다 |
| `Settings.Resolutions()` | 고를 수 있는 해상도는 FHD 이하만, 고른 적 없으면 1920×1080 |
| 전체 화면 | `FullScreenWindow`로 고른 해상도에 그리고 모니터에 맞게 늘린다(그리는 비용은 FHD 그대로) |
| 창 모드 | 모니터보다 작은 크기까지만 |
| `ResolutionGuard` | 시작할 때 저장된 해상도를 반영하고, 0.5초마다 FHD를 넘었는지(Alt+Enter 등 OS가 바꾼 경우) 보고 되돌린다. 에디터 Game 뷰는 건드리지 않는다 |

- 매 프레임이 아니라 **0.5초마다** 확인한다. 해상도가 바뀌는 일은 드물고, 늦게 잡아도 한 번 늘었다 줄 뿐이다.
- UI는 캔버스 스케일러가 기준 해상도로 맞추므로 FHD로 그려 늘려도 배치가 그대로다.

## 6. 그 밖에

| 기법 | 어디 |
|---|---|
| 머티리얼 캐시 | `Mats`가 (셰이더, 색, 텍스처) 조합마다 한 번만 만든다. 같은 재질이어야 배칭된다 |
| 나무 굽기 | ez-tree로 만든 나무·덤불을 빌드 전에 공유 재질(`Fishing/Foliage`)과 LOD 프리팹으로 굽는다(`Editor/VegetationBake`). 런타임 나무 생성 비용을 없애고 같은 재질끼리 배칭된다 |
| 무거운 계산은 작업 스레드 | 인트로 음악 샘플 계산(수십 ms)을 `Task.Run`에서, `AudioClip` 생성만 메인 스레드에서([09](09-procedural-content.md)) |
| 반사 프로브 | 하늘만, 2초마다 |
| 셰이더 데우기 | 인트로가 화면을 덮는 동안 뒤에서 대기실을 미리 그린다. 첫 화면에서 셰이더 컴파일로 끊기지 않는다 |

## 옮겨 쓸 때 체크리스트

- 런타임 배칭은 "움직일 수 있는 것"을 **구조로** 걸러 낸다(스크립트·Animator·Rigidbody가 위에 있나).
- 배칭 대상 셰이더는 월드 좌표 기반으로.
- 대량 오브젝트는 칸 메쉬로 굽고, 칸 단위로 거리 컬링.
- FPS를 자동 촬영 로그로 맵별 숫자로 남긴다.
