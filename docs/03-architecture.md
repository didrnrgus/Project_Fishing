# 03. 코드 구조

## 원칙

- **씬에는 `GameBootstrap` 하나만 둔다.** 월드, 플레이어, 카메라, UI를 모두 코드로 만든다. 씬에 없어도 `RuntimeInitializeOnLoadMethod`로 자동 생성된다.
- **아트 에셋이 없다.** 프리미티브, 런타임 머티리얼(`Mats`), 절차적 텍스처/스프라이트, 절차적 사운드(`SoundFx`)만 쓴다. 나중에 에셋으로 바꿀 때는 `WorldBuilder`, `PlayerController.BuildModel`, `FightRodView.Build`, `Bobber.Create`만 교체하면 된다.
- **게임 규칙은 MonoBehaviour와 분리한다.** `FishFight`, `BiteSelector`, `PlayerProfile`, `QuestSystem`은 순수 C#이라서 에디터에서 밸런스 시뮬레이션을 돌릴 수 있다.
- **데이터는 C# 정적 테이블이다** (`FishDatabase`, `GearDatabase`, `QuestDatabase`). 텍스트 diff로 리뷰하기 쉽다. 데이터가 많아지면 ScriptableObject나 JSON으로 옮길 수 있다.
- **입력은 구 Input Manager**(`UnityEngine.Input`)를 쓴다. 프로젝트 설정 `activeInputHandler: 0`.
- **렌더 파이프라인은 Built-in**이다. `Standard`, `Unlit/Color`, `Skybox/Procedural`, `Legacy Shaders/Particles/Alpha Blended`를 `Shader.Find`로 쓰므로, 셋업 도구가 Always Included Shaders에 등록한다.

## 폴더

```
Assets/
  Scenes/HanRiver.unity          GameBootstrap 하나만 있는 씬
  Scripts/
    Core/                        진입점, 전역 상태, 시간/날씨/조명, 저장, 퀘스트
      GameBootstrap.cs           모든 시스템 생성·연결, 자동 저장, 체력 자연 감소
      Game.cs                    전역 참조(서비스 로케이터) + Toast 이벤트 + Save()
      GameClock.cs               게임 시계 (1초 = 2분), 시간대
      WeatherSystem.cs           날씨 전환, 비 파티클
      DayNightCycle.cs           해/달, 스카이박스, 주변광, 안개, 가로등
      PlayerProfile.cs           레벨/경험치/골드/체력/아이템/장비/도감 규칙 + SaveSystem
      SaveData.cs                JsonUtility 직렬화용 데이터
      QuestSystem.cs             조합 퀘스트 진행
      Mats.cs                    런타임 머티리얼 캐시, 프리미티브 생성 헬퍼, 절차적 텍스처
    Data/                        정적 데이터 테이블
      Enums.cs                   Habitat, TimeBand, Weather, BaitKind + 한글 이름
      FishDatabase.cs            어종 16종 + 잡동사니
      GearDatabase.cs            낚싯대/릴/줄, 미끼, 김밥
      QuestDatabase.cs           스토리 퀘스트 8개
    World/
      HanRiverMap.cs             맵 좌표계, 격자, 서식지, 걸을 수 있는 곳/물 판정
      WorldBuilder.cs            프리미티브로 한강, 다리, 데크, 낚시점, 나무, 도시 생성
      FishSchoolManager.cs       어군 이동, 수면 파문
      Effects.cs                 물보라(공유 파티클), 파문
    Player/
      PlayerController.cs        3인칭 캐릭터 이동, 낚싯대 기울기
      CameraRig.cs               어깨 너머 3인칭 ↔ 파이팅 1인칭, 흔들림, 충돌 회피
    Fishing/
      FishingController.cs       낚시 상태 머신 (Idle→Charging→Casting→Waiting→Bite→Fighting→Result)
      FishFight.cs               파이팅 시뮬레이션 (순수 로직)
      BiteSelector.cs            어떤 고기가 몇 cm로 물지 결정
      Bobber.cs                  찌 비행/부유/예신/본신
      FightRodView.cs            1인칭 낚싯대, 낚싯줄, 물고기 위치·몸체
      FightBot.cs                자동 파이팅 입력 (밸런스 시뮬레이션·자동 스크린샷용)
    UI/
      GameUI.cs                  모든 UI 생성, 단축키
      UIFactory.cs               코드 UGUI 헬퍼, 한글 OS 폰트, 절차적 스프라이트
      UIPanel.cs                 창 공통 틀
      HudView.cs                 필드 HUD (플레이어, 시계, 미니맵, 퀘스트, 미끼 슬롯, 토스트)
      FightHudView.cs            파이팅 HUD
      ResultPanel.cs, KeepNetPanel.cs, DexPanel.cs, ShopPanel.cs, InfoPanels.cs(퀘스트/도움말)
    Audio/SoundFx.cs             절차적 효과음
    Dev/DevAutomation.cs         -autoshot 자동 연출 + 스크린샷
  Editor/
    ProjectTools.cs              Setup(씬·빌드 설정·셰이더), BuildWindows, DeleteSave
    BalanceSim.cs                파이팅 밸런스 시뮬레이션 → docs/balance-report.md
```

## 실행 흐름

```
GameBootstrap.Awake
 ├─ SaveSystem.Load → PlayerProfile, QuestSystem
 ├─ GameClock, WeatherSystem
 ├─ WorldBuilder.Build → DayNightCycle.Init(가로등)
 ├─ FishSchoolManager
 ├─ PlayerController (+ 모델), CameraRig (+ AudioListener, SoundFx)
 ├─ FishingController (Start에서 찌, 1인칭 낚싯대, 낚싯줄, 착지 링 생성)
 └─ GameUI (EventSystem, Canvas, HUD, 창들)
```

매 프레임:
- `FishingController.Update`가 상태 머신을 진행한다. 파이팅 중에는 `FishFight.Tick(dt, input, 체력계수, 내구도계수)`를 부른다.
- `GameUI.Update`가 `HudView.Update`와 `FightHudView.Update`를 부른다. 둘 다 상태를 **폴링해서** 그리고, 창은 `PlayerProfile.Changed` 이벤트가 오면 다시 그린다.
- `Game.Toast()`가 게임 어디서든 화면 위쪽에 메시지를 띄운다.

## 상태 머신 (`FishingController`)

| 상태 | 들어가는 조건 | 하는 일 | 나가는 조건 |
|---|---|---|---|
| Idle | 기본 | 이동 가능 | 좌클릭 → Charging (미끼·체력이 있어야 함) |
| Charging | | 게이지 왕복, 착지 링 | 떼기 → Casting (물이면) / Idle |
| Casting | | 찌 비행 | 착지 → Waiting |
| Waiting | | 어종 미리 결정, 예신 | 시간 끝 → Bite / 좌클릭 → 헛챔질(다시 Waiting) / R·이동 → Idle |
| Bite | | 찌 잠김, `!`, 두근두근 | 좌클릭 → Fighting / 시간 초과 → Idle |
| Fighting | | `FishFight.Tick`, 1인칭, 커서 잠금 | 결과 → Result(성공) / Idle(실패) |
| Result | | 결과 창 | 살림망·방생 선택 → Idle |

## 확장 방법

### 어종 추가
`FishDatabase.All`에 `FishSpecies`를 추가한다. 도감, 입질, 퀘스트 텍스트에 자동으로 반영된다. 추가한 뒤 `tools/balance.sh`로 난이도를 확인한다.

### 장비 추가
`GearDatabase.Gear`에 `GearDef`를 추가한다. 낚시점 목록은 `Slot` 순서대로 자동 정렬된다.

### 새 맵 추가 (예: 서천 앞바다)
지금은 `HanRiverMap`이 정적 클래스다. 맵을 늘리려면 다음 순서로 바꾼다.
1. `HanRiverMap`의 공개 API(`IsWater`, `IsWalkable`, `TryCell`, `HabitatAt`, `CellCenter`, `AreaName`, `Spawn`, `ShopPos`)를 `IMapDefinition` 인터페이스로 뽑는다.
2. `WorldBuilder`를 맵별 빌더로 나눈다.
3. `FishSpecies`에 `MapIds`를 추가하고, `BiteSelector`가 현재 맵의 어종만 고르게 한다.
4. 레벨/라이선스로 맵을 여는 이동 UI(조합 NPC 또는 지도)를 만든다.
5. `Game.Map`에 현재 맵을 두고, 지금 `HanRiverMap.`을 직접 부르는 곳(WorldBuilder 포함 약 70군데, 대부분 WaterY·LandY 상수)을 `Game.Map.`으로 바꾼다.

### 아트 교체
- 캐릭터: `PlayerController.BuildModel()`에서 프리팹을 Instantiate하고, `RodTip`만 낚싯대 끝 Transform으로 지정한다.
- 물: `WorldBuilder.BuildWater()`의 큐브를 물 셰이더 평면으로 바꾼다 (`HanRiverMap.WaterY` 기준).
- 1인칭 낚싯대: `FightRodView`의 `_segs`(9마디) 구조를 유지하면 휘는 연출을 그대로 쓸 수 있다.
