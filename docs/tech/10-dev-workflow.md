# 10. 개발 흐름

[← 기술 문서 목차](README.md) · 코드: `Core/GameBootstrap.cs`, `Data/*.cs`, `Dev/DevAutomation.cs`, `Editor/Tests/`, `Core/SaveData.cs`, `tools/`

혼자, 그리고 AI와 함께 빠르게 고치고 확인하기 위해 고른 방식들이다.

## 1. 씬에는 오브젝트 하나: 모든 것을 코드로

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void EnsureBootstrap()
{
    if (FindFirstObjectByType<GameBootstrap>() == null)
        new GameObject("Game").AddComponent<GameBootstrap>();
}
```

- 씬 파일에는 `GameBootstrap` 하나만 있다. 없어도 위 코드가 만든다. **어느 씬에서 Play해도 게임이 뜬다.**
- 월드·플레이어·카메라·UI를 `Awake`에서 순서대로 만든다. 실행 흐름이 코드 한 곳에 보인다([03 문서](../03-architecture.md) "실행 흐름").
- 프리팹·씬 YAML 대신 C#이라 **변경이 텍스트 diff로 리뷰된다**. 충돌 해결도 쉽다.
- 대가: 에디터에서 끌어다 놓는 배치가 없다. 그래서 좌표를 문서·사양(`CoastSpec`)에 적고, 자동 촬영으로 결과를 본다.
- 전역 참조는 서비스 로케이터(`Game.Profile`, `Game.Fishing`, `Game.Clock` …) 하나. 싱글턴 여러 개보다 생성 순서를 통제하기 쉽다.

## 2. 데이터는 C# 정적 테이블

```csharp
new FishSpecies { Id = "carp", Name = "잉어", Rarity = 3, MinLen = 25, MaxLen = 105, Habitats = …, Baits = …, Times = …, Power = …, Exp = … },
```

- 어종 77, 장비, 퀘스트 45, 업적 36, 의상 25가 모두 C# 목록이다. **컴파일러가 오타를 잡는다**(존재하지 않는 enum, 잘못된 타입).
- 순수 C#이라 시뮬레이터([02](02-headless-balance.md))와 테스트가 그대로 읽는다.
- 문서 표도 데이터에서 만든다: `python tools/fish_table.py --write`가 `FishDatabase.cs`를 읽어 02 문서의 어종 표와 맵별 어종 수를 다시 쓴다. **문서와 코드가 어긋나지 않는다.**
- 데이터가 더 커지면 ScriptableObject나 JSON으로 옮길 수 있게 규칙 코드는 목록만 받는다.

## 3. 자동 촬영: 눈으로 보는 회귀 테스트

```bash
sh tools/build.sh                 # 배치모드 빌드
sh tools/autoshot.sh              # 빌드를 실행해 약 150장 촬영 후 종료, 맵별 평균 FPS
sh tools/autoshot.sh 06_fight     # 그 장면까지만 (-autoshotStop)
```

`ProjectFishing.exe -autoshot <폴더>`로 실행하면 `DevAutomation`이 코루틴으로 **대본대로** 게임을 진행한다.

```csharp
yield return new WaitUntil(() => intro.Current == IntroSequence.Stage.Company && intro.T >= 3.6f);
yield return Shot("00_intro_company");
…
yield return new WaitForSeconds(3f);
yield return Shot("00a_lobby");
```

- 대기실 → 낚시터 목록 → 한강 입장 → 캐스팅 → 챔질 → 파이팅(봇) → 카운터 → 랜딩 → 결과 → 각종 창 → 홍현·서천·해안·해외 맵 순회(`CoastTour`).
- 파이팅은 `FightBot`이 한다. 시간·날씨·물때는 디버그 함수로 정한다(`Game.Clock.Set`, `Game.Weather.Force`, `DebugForceBite`).
- **세이브를 읽지도 쓰지도 않는다**(`Game.SaveDisabled`). 늘 같은 출발점에서 찍는다.
- 로그에 장면마다 `(FPS, 맵, 정적 배칭 수)`, 파이팅 결과(`어종 82.0cm, 경과 21.7s, 결과 Landed`), 캐스팅 막힘 경고가 남는다.
- 스크린샷을 이전 것과 나란히 보면 **"뭔가 깨졌다"를 사람이 1분 안에 찾는다**. 문서 그림(`docs/images/`)도 여기서 복사한다.
- 클라우드 세션처럼 GPU가 없는 곳에서는 못 돈다. 그래서 규칙 검증은 시뮬레이터·테스트로, 화면 검증은 PC에서 자동 촬영으로 나눴다.

## 4. 단위 테스트 (EditMode, 49개)

```bash
sh tools/test.sh    # Logs/test-results.xml
```

| 묶음 | 지키는 것 |
|---|---|
| `FishFightTests` | 작은 붕어는 입문 장비로 잡힌다 · 지친 상태로도 대물 장비면 나일농어를 올린다 · 대물일수록 오래 걸린다 · 무작정 감으면 끊긴다 |
| `BiteSelectorTests` | 그 맵에 사는 고기만 · 잡동사니 13% 미만 · 그루퍼는 어군 안에서만 · 맞는 미끼가 유리 |
| `TournamentRulesTests` | 점수·동점 · 같은 날은 같은 AI 결과 · 실력 차이 |
| `RodUpgradeTests` | +10 = 힘 1.3배 · 확률 내림·비용 오름 · +7부터 하락 |
| `SeasonTests` | 달력 · 빙어는 겨울 얼음 구멍에서만 · 다금바리·돗돔은 먼바다에서만 |
| `CoastMapTests` | 물때(간조·평균·만조)마다 시작점·낚시점·연락관을 걸을 수 있다 · **모든 어종이 사는 낚시터에서 실제로 낚인다** |

- 테스트는 **사용자가 지적한 버그를 다시 못 생기게** 하는 데 쓴다. "깡통만 낚인다" → 잡동사니 13% 테스트, "지치면 못 끝낸다" → 지친 상태 테스트.
- `CoastMapTests`의 "모든 어종이 낚인다"는 데이터 실수를 잡는다: 어종의 서식지가 그 맵 격자에 없거나, 그 맵 낚시점에서 맞는 미끼를 안 팔면 실패한다.
- 게임 코드에 asmdef가 없어 테스트도 `Editor/` 아래(Assembly-CSharp-Editor)에 둔다.

## 5. 세이브 호환: 기본값으로 버틴다

```csharp
public static SaveData Load()
{
    try { if (File.Exists(PathFor)) return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor)) ?? new SaveData(); }
    catch (Exception e) { Debug.LogWarning($"[Save] 불러오기 실패, 새로 시작합니다: {e.Message}"); }
    return new SaveData();
}
```

- `JsonUtility`는 **JSON에 없는 필드를 클래스 기본값으로 둔다.** 그래서 새 필드에 알맞은 기본값만 주면 예전 세이브가 그대로 열린다. 버전 번호·마이그레이션 코드가 필요 없었다.
- 실패하면 던지지 않고 새로 시작한다(경고 로그).

### 따라잡기(catch-up) 패턴

새 기능이 "레벨 N에 도달하면 주는 것"이면, 이미 그 레벨을 넘은 예전 세이브는 받을 기회가 없다.

```csharp
foreach (var (lv, id, count, text) in LevelRewards.All)
{
    if (Data.level < lv || Data.levelRewards.Contains(lv)) continue;   // 받은 기록이 있으면 건너뜀
    Data.levelRewards.Add(lv);
    // 장비면 소유 목록에, 아이템이면 가방에
}
```

- "받았나"를 **기록 목록**으로 저장하고, 켤 때마다 "도달했는데 안 받은 것"을 준다. 레벨 업 순간과 게임 시작 때 같은 함수를 부른다.
- 같은 방식: 대회 정산(껐다가 다음 날 켜면 켤 때 정산), 출석(실제 날짜).

### 바꾸면 안 되는 값

저장 경로와 옵션(PlayerPrefs)은 `companyName`·`productName`을 따른다. 사업자 이름을 「제쓰로네 게임공방」으로 바꿀 때도 **이 두 값은 그대로 뒀다**. 바꾸면 예전 세이브가 안 보인다.

## 6. Unity 없이 컴파일만 확인

에디터가 열려 있으면 배치모드 빌드가 막힌다. Unity가 만든 `Library/Bee/artifacts/*/Assembly-CSharp.rsp`를 Unity 안의 Roslyn(`csc.dll`)으로 돌리면 **오류만** 빨리 본다(rsp의 소스 목록과 `-out`만 바꿔서).
클라우드에서는 `tools/fightsim`이 규칙 코드를 .NET으로 컴파일하므로, 규칙 코드의 문법·타입 오류는 거기서도 잡힌다.

## 7. 문서를 코드 옆에 둔다

- `docs/` 문서가 **번호 순서**(조사 → 디자인 → 구조 → 밸런스 → 개발 → 로드맵 …)로 있다.
- 값이 바뀌면 문서 표도 같은 커밋에서 고친다. 밸런스 조정은 차수별 표로 기록한다([02](02-headless-balance.md) 5장).
- 원작에서 가져온 것과 바꾼 것을 대조표로 남긴다(11 문서). "왜 원작과 다르지?"에 바로 답한다.
- `WORK-IN-PROGRESS.md`에 지금 상태와 **사람이 해야 할 일**(에셋 스토어 로그인 같은)을 적어 둔다. 세션이 바뀌어도 이어서 일한다.

## 옮겨 쓸 때 체크리스트

- 씬은 비우고 부트스트랩 하나 + `RuntimeInitializeOnLoadMethod`.
- 데이터는 컴파일되는 형태로, 문서 표는 데이터에서 생성.
- 화면은 대본 코루틴으로 자동 촬영, 세이브는 끄고, FPS·결과를 로그로.
- 사용자 지적은 테스트로 고정.
- 세이브는 "새 필드 = 기본값" + "받았나 기록 목록"으로 따라잡기.
