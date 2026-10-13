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
      Game.cs                    전역 참조(서비스 로케이터), GameMode(Lobby/Field), 채널, Toast 이벤트, Save()
      GameClock.cs               게임 시계 (1초 = 2분), 시간대
      WeatherSystem.cs           날씨 전환, 비 파티클
      DayNightCycle.cs           해/달, 스카이박스, 주변광, 안개, 가로등, 하늘 반사 프로브, SkyDecor 제어
      PlayerProfile.cs           레벨/경험치/골드/체력/아이템/장비/도감 규칙 + SaveSystem
      SaveData.cs                JsonUtility 직렬화용 데이터
      QuestSystem.cs             조합 퀘스트 진행
      Mats.cs                    런타임 머티리얼 캐시, 프리미티브 생성 헬퍼, 절차적 텍스처
    Data/                        정적 데이터 테이블
      Enums.cs                   Habitat, TimeBand, Weather, BaitKind + 한글 이름
      FishDatabase.cs            어종 16종 + 잡동사니
      GearDatabase.cs            낚싯대/릴/줄, 미끼, 김밥
      QuestDatabase.cs           스토리 퀘스트 8개
      FishingAreas.cs            낚시터 목록 11곳(경위도, 열림 여부 — 지금은 한강만)
    Characters/
      CharacterFactory.cs        Quaternius 휴머노이드 + 옷 버텍스 컬러 마스크 + 머리·모자·이름표 (CharacterLook)
      CharacterAnimator.cs       Playables 믹서로 클립 재생 (AnimatorController 없이 SetLoop/PlayOnce)
      NpcAngler.cs               배경 NPC (Angler/Talker/Sitter, Idle이면 다시 던지지 않음)
    World/
      HanRiverMap.cs             맵 좌표계, 격자, 서식지, 걸을 수 있는 곳/물 판정
      WorldBuilder.cs            프리미티브로 한강, 다리, 데크, 낚시점, 나무, 도시, NPC 생성
      LobbyRoom.cs               대기실(한국 낚시 조합) 실내, 박제 물고기, 고정 카메라 구도
      WaterSurface.cs            격자 수면 메쉬 + Fishing/Water 머티리얼
      SkyDecor.cs                달·별·구름 돔 (카메라를 따라다님)
      KenneyProps.cs             Kenney CC0 모델 배치: 높이/길이에 맞춰 키우기, 나무 색 다시 칠하기, 밤에 빌딩 은은한 발광
      ProceduralTextures.cs      물 노멀맵, 구름 노이즈, 달 텍스처 (주기적 value-noise fbm)
      FishSchoolManager.cs       어군 이동, 큰/작은 어군, 수면 파문
      Effects.cs                 물보라(공유 파티클), 파문
    Player/
      PlayerController.cs        3인칭 캐릭터 이동, 낚싯대 기울기
      BoatController.cs          배: 물가에서 띄우기·운전·연료·부딪힘·물보라 꼬리·내리기 (Game.Boat)
      CameraRig.cs               어깨 너머 3인칭 ↔ 파이팅 1인칭 ↔ 고정(대기실), 흔들림, 충돌 회피
    Fishing/
      FishingController.cs       낚시 상태 머신 (Idle→Charging→Casting→Waiting→Bite→Fighting→Result)
      FishFight.cs               파이팅 시뮬레이션 (순수 로직)
      BiteSelector.cs            어떤 고기가 몇 cm로 물지 결정
      Bobber.cs                  찌 비행/부유/예신/본신
      FightRodView.cs            1인칭 낚싯대, 낚싯줄, 물고기 위치·몸체
      TelescopicRod.cs           뽑기대(마디 펼치기·휨 스프링) + 스피닝 릴(장비별 모양, 손잡이·로터·스풀 회전). 1인칭·플레이어·NPC 공용
      FishModels.cs              Fish1~3 모델로 어종별 물고기 (BakeMesh로 원본 길이를 재서 실제 cm에 맞춤)
      FightBot.cs                자동 파이팅 입력 (밸런스 시뮬레이션·자동 스크린샷용)
    UI/
      GameUI.cs                  모든 UI 생성, 단축키
      UIFactory.cs               코드 UGUI 헬퍼, 한글 OS 폰트, 절차적 스프라이트
      UIPanel.cs                 창 공통 틀
      HudView.cs                 필드 HUD (플레이어, 시계, 미니맵, 퀘스트, 미끼 슬롯, 토스트)
      FightHudView.cs            파이팅 HUD (장력 위험 빨간 테두리 포함)
      LobbyView.cs               대기실 HUD (제목, 정보, 오른쪽 아래 큰 버튼)
      FishingAreaPanel.cs        낚시터 목록 (한국 지도, MAP/FISH INFO, 채널, 빠른입장)
      KoreaMap.cs                한반도 해안선 다각형 → 지도 텍스처, 경위도 → UV
      AreaMapPanel.cs            지역 지도(M): 격자·서식지·어군·내 위치
      PortraitCamera.cs          HUD 초상화: 캐릭터 레이어(8)만 비추는 카메라 → 렌더 텍스처
      ResultPanel.cs, KeepNetPanel.cs, DexPanel.cs, ShopPanel.cs, InfoPanels.cs(퀘스트/도움말)
    Audio/SoundFx.cs             절차적 효과음
    Dev/DevAutomation.cs         -autoshot 자동 연출 + 스크린샷
  Shaders/
    Clothed.shader               버텍스 컬러 마스크(R 셔츠, G 바지, B 신발, A 조끼)로 옷 색 입히기
    Water.shader                 버텍스 사인파 3개 + 흐르는 노멀맵 2장, 하늘 반사
    Sky.shader                   Mode 0 달·별, Mode 1 구름(덮임 정도·노을색)
  Resources/                     Quaternius CC0: Characters(+Hair, Textures), Animations/UAL1_Standard, Fish/Fish1~3
  ThirdParty/Quaternius/         라이선스 사본
  Editor/
    ProjectTools.cs              Setup(씬·빌드 설정·셰이더), BuildWindows(Always Included Shaders 갱신), DeleteSave
    AssetImportRules.cs          캐릭터·애니메이션 휴머노이드, 머리 Generic, 물고기 Legacy, Loop 클립 처리
    AssetInspector.cs            임포트된 에셋 정보 → Logs/asset-inspect.txt
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
 ├─ GameUI (EventSystem, Canvas, HUD, 대기실 HUD, 창들)
 └─ LobbyRoom.Build (한강에서 멀리 떨어진 (0,0,-420)에 실내를 짓는다)
GameBootstrap.Start → GameUI.EnterLobby (Mode=Lobby, 카메라 고정)
```

**배에 탄 플레이어**: `PlayerController.Mount(자리, 앉음)`이 CharacterController를 끄고, `BoatController.Update`가 배를 움직인 뒤 `SnapToMount()`로 플레이어를 자리(운전석/뱃머리)에 옮긴다. 플레이어 `Update`는 배 위에서 이동하지 않고 WASD 눌림만 `MovedThisFrame`으로 알린다(찌 회수용). `Dismount`는 `Teleport`로 둔치에 내려놓는다.

**게임 모드** (`Game.Mode`):
- `Lobby`: 플레이어 이동·낚시·비가 멈추고, 카메라는 `LobbyRoom.CameraPos/Rot`에 고정된다. 필드 HUD 대신 `LobbyView`가 보인다.
- `Field`: `GameUI.EnterField(channel)`로 들어온다. 카메라 고정을 풀고 3인칭으로 돌아간다. `↩ 조합` 또는 `EnterLobby()`로 나간다(파이팅·결과 중에는 막힘, 던진 찌는 `ForceIdle`로 거둠).

매 프레임:
- `FishingController.Update`가 상태 머신을 진행한다. 파이팅 중에는 `FishFight.Tick(dt, input, 체력계수, 내구도계수)`를 부른다.
- `GameUI.Update`가 `HudView.Update`와 `FightHudView.Update`를 부른다. 둘 다 상태를 **폴링해서** 그리고, 창은 `PlayerProfile.Changed` 이벤트가 오면 다시 그린다.
- `Game.Toast()`가 게임 어디서든 화면 위쪽에 메시지를 띄운다.

## 상태 머신 (`FishingController`)

| 상태 | 들어가는 조건 | 하는 일 | 나가는 조건 |
|---|---|---|---|
| Idle | 기본 | 이동 가능 | 좌클릭 → Charging (미끼·체력이 있어야 함) |
| Charging | | 당긴 양·민 속도 측정, 궤적 리본, 비거리 숫자 | 떼기 → Casting (물이면) / Idle |
| Casting | | 찌 비행 | 착지 → Waiting |
| Waiting | | 어종 미리 결정, 예신, 찌 게이지 | 시간 끝 → Bite / 흔들기 → 헛챔질(다시 Waiting) / R·이동 → Idle |
| Bite | | 찌가 1.1초에 걸쳐 잠김, 빨간 `!`, 두근두근 | 흔들기 → 등급(`HookGrade`) → Fighting 또는 BAD 헛챔질 / 시간 초과 → Idle |
| Fighting | | `FishFight.Tick`, 1인칭, 커서 잠금, 데미지 숫자. 랜딩 찬스면 연타 → `LandingTap` | 결과 → Result(성공) / Idle(실패) |
| Result | | 결과 창 | 살림망·방생 선택 → Idle |

## 확장 방법

### 어종 추가
`FishDatabase.All`에 `FishSpecies`를 추가한다. 도감, 입질, 퀘스트 텍스트에 자동으로 반영된다. 추가한 뒤 `tools/balance.sh`로 난이도를 확인한다.

### 장비 추가
`GearDatabase.Gear`에 `GearDef`를 추가한다. 상점(F12) 장비 목록은 `Slot` 순서대로 자동 정렬된다.

### 새 맵 추가 (예: 서천 앞바다)
지금은 `HanRiverMap`이 정적 클래스다. 맵을 늘리려면 다음 순서로 바꾼다.
1. `HanRiverMap`의 공개 API(`IsWater`, `IsWalkable`, `TryCell`, `HabitatAt`, `CellCenter`, `AreaName`, `Spawn`, `ShopPos`)를 `IMapDefinition` 인터페이스로 뽑는다.
2. `WorldBuilder`를 맵별 빌더로 나눈다.
3. `FishSpecies`에 `MapIds`를 추가하고, `BiteSelector`가 현재 맵의 어종만 고르게 한다.
4. 레벨/라이선스로 맵을 여는 이동 UI(조합 NPC 또는 지도)를 만든다.
5. `Game.Map`에 현재 맵을 두고, 지금 `HanRiverMap.`을 직접 부르는 곳(WorldBuilder 포함 약 70군데, 대부분 WaterY·LandY 상수)을 `Game.Map.`으로 바꾼다.

### 아트 교체
- 캐릭터: `CharacterFactory.Create(look, parent)`가 Quaternius 모델을 만든다. 다른 휴머노이드로 바꿀 때는 뼈 이름(`hand_r`, `Head`, `spine_01~03`, `thigh_*`, `foot_*`)과 옷 마스크 규칙(`ClothedMesh`)만 맞추면 된다. 에셋이 없으면 프리미티브 모델로 대신한다.
- 애니메이션: `CharacterAnimator`의 상수(Idle/Walk/HoldRod/Cast/Hook…)가 클립 이름이다. 다른 라이브러리를 쓰면 이름만 바꾼다.
- 물고기: `FishModels.Models`(경로)와 `ModelIndex`(어종 → 모델)만 바꾸면 된다. 크기는 BakeMesh로 재서 자동으로 맞춘다.
- 물: `WaterSurface` + `Fishing/Water` 셰이더 (`_WaveAmp`, `_WaveLen`, 노멀 타일·속도). 하늘 반사는 `DayNightCycle`의 리얼타임 반사 프로브(하늘만, 2초마다).
- 낚싯대·릴: `TelescopicRod.StyleFor`(낚싯대 색·길이·마디 수)와 `BuildReel`(릴 모양)에서 장비 id별로 고른다. 좌표 규약은 +Y가 낚싯대 방향, 원점이 릴 시트, +Z가 릴 쪽. 휨은 `Drive(목표각, 줄 방향, 떨림, dt)`, 펼치기는 `SetExtension(0~1)`, 릴은 `SpinReel(감기 바퀴/s, 줄 풀림 m/s, dt)`.
