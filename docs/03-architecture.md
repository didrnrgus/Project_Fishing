# 03. 코드 구조

## 원칙

- **씬에는 `GameBootstrap` 하나만 둔다.** 월드, 플레이어, 카메라, UI를 모두 코드로 만든다. 씬에 없어도 `RuntimeInitializeOnLoadMethod`로 자동 생성된다.
- **월드 아트는 코드로 만든다.** 프리미티브, 런타임 머티리얼(`Mats`), 절차적 텍스처/스프라이트, 절차적 사운드(`SoundFx`) + CC0 에셋(Quaternius·Kenney 모델, Poly Haven 텍스처, Kenney 소리). 캐릭터 옷·물고기·아이콘은 `tools/blender`의 headless Blender 스크립트로 만들어 FBX/PNG로 커밋한다. 나중에 에셋으로 바꿀 때는 `WorldBuilder`, `PlayerController.BuildModel`, `FightRodView.Build`, `Bobber.Create`만 교체하면 된다.
- **UI도 코드로 만들지만 외부 에셋을 쓴다**: Translucent Image + Paraform(유리·둥근 도형), EasyChart(차트), TextMeshPro(Jalnan2 글꼴). 프리팹 없이 `UIFactory`/`UIStyle`/`UIChart`로 조립한다.
- **게임 규칙은 MonoBehaviour와 분리한다.** `FishFight`, `BiteSelector`, `PlayerProfile`, `QuestSystem`은 순수 C#이라서 에디터에서 밸런스 시뮬레이션을 돌릴 수 있다.
- **데이터는 C# 정적 테이블이다** (`FishDatabase`, `GearDatabase`, `QuestDatabase`, `CostumeDatabase`, `AchievementDatabase` …). 텍스트 diff로 리뷰하기 쉽다. 데이터가 많아지면 ScriptableObject나 JSON으로 옮길 수 있다.
- **입력은 구 Input Manager**(`UnityEngine.Input`, 키는 `Settings`로 바꿀 수 있음)를 쓴다. 패드 진동만 새 Input System(`Rumble`) — 프로젝트 설정 `activeInputHandler: 2`(둘 다).
- **렌더 파이프라인은 Built-in**이다. `Standard`, `Unlit/Color`, `Skybox/Procedural`, `Legacy Shaders/Particles/Alpha Blended`를 `Shader.Find`로 쓰므로, 셋업 도구가 Always Included Shaders에 등록한다.

## 폴더

```
Assets/
  Scenes/HanRiver.unity          GameBootstrap 하나만 있는 씬
  Scripts/
    Core/                        진입점, 전역 상태, 시간·날씨·조명, 저장, 퀘스트, 대회, 설정
      GameBootstrap.cs           모든 시스템 생성·연결, 자동 저장, 체력 자연 감소
      Game.cs                    전역 참조(서비스 로케이터), GameMode(Lobby/Field), 채널, Toast 이벤트, Save()
      GameClock.cs               게임 시계 (1초 = 2분, 하루 12분), 시간대
      WeatherSystem.cs           날씨 전환, 비 파티클(겨울엔 눈), 바람(돛단배)·파도
      DayNightCycle.cs           해/달, 스카이박스, 주변광, 안개, 가로등, 하늘 반사 프로브, SkyDecor(구름 양) 제어
      PostFx.cs, PostFxConfig.cs 후처리 (Post Processing Stack v2: 블룸, Neutral 톤매핑, AO, SMAA, 시간대별 그레이딩)
      MapLoader.cs               낚시터 바꾸기 (월드·가로등·어군 다시 만들기, 시작 위치)
      Tide.cs                    물때 (서해처럼 TideAmplitude가 있는 맵에서 수면 높이가 오르내림)
      TournamentSystem.cs        조합 낚시 대회 진행 (참가·집계·AI 낚시꾼·시상)
      PlayerProfile.cs           레벨/경험치/골드/체력/아이템/장비/의상/도감 규칙 + SaveSystem
      SaveData.cs                JsonUtility 직렬화용 데이터 (수족관·대회·일일 의뢰 포함)
      QuestSystem.cs             이야기 퀘스트, 대물 도전, 일일 의뢰, 업적 판정, 소라고동
      Settings.cs                옵션(소리·감도·화면·진동)과 키 설정 (PlayerPrefs)
      Rumble.cs                  게임패드 진동 (새 Input System의 Gamepad만, 조작은 Input Manager)
      Mats.cs                    런타임 머티리얼 캐시, 프리미티브·텍스처 상자 헬퍼, 절차적 텍스처
    Data/                        정적 데이터 테이블 (순수 C#)
      Enums.cs                   Habitat, TimeBand, Weather, BaitKind + 한글 이름
      FishDatabase.cs            어종 41종 + 잡동사니 6종 (맵·제철·어군 전용·먼바다 전용)
      GearDatabase.cs            낚싯대/릴/줄/배, 미끼(해체 미끼 5등급 포함), 김밥
      ItemDatabase.cs            가방 아이템 이름·설명
      CostumeDatabase.cs         의상 6부위 25벌 + 능력치
      AchievementDatabase.cs     업적 26개, 레벨 칭호
      QuestDatabase.cs           이야기 퀘스트 18개 (한강·홍현·서천)
      RodUpgrade.cs              장비 강화 규칙 (+1~+15, 확률·비용·재료·성능)
      Seasons.cs                 계절 (한 달 = 게임 7일, 1일째 = 4월), 제철 입질 배율
      TournamentRules.cs         대회 규칙 (미션 점수, 엠블럼, 대회 맵 순서)
      FishingAreas.cs            낚시터 목록 (경위도, 열림 여부)
    Characters/
      CharacterFactory.cs        Angler FBX(몸 + 옷 메쉬)에서 입은 옷만 남기고 색 입히기, 머리카락·이름표 (CharacterLook)
      CharacterAnimator.cs       Playables 믹서로 클립 재생 (AnimatorController 없이 SetLoop/PlayOnce)
      NpcAngler.cs               NPC (Angler/Talker/Sitter). NpcId로 대화 종류, 대화 상대 바라보기
    World/
      FieldMap.cs                FieldMapDef(맵 정의: 격자·서식지·물/땅 판정·물때·먼바다·수면 색) + 정적 FieldMap(현재 맵) + Maps.All
      HanRiverMap.cs             한강 반포지구 정의 (A~F × 1~4)
      HonghyeonMap.cs            홍현 앞바다 정의·월드 (A~H × 1~8, 방파제·등대·갯바위 섬·다랭이 마을)
      SeocheonMap.cs             서천 앞바다 정의·지형·월드 (갯벌·홍원항 등대·동백정 정자)
      WorldBuilder.cs            한강 월드 (다리, 데크, 낚시점, 나무, 도시, NPC)
      TerrainBuilder.cs          높이 함수 + 칠하기 함수 → Unity Terrain (SeaTerrain = 홍현, SeocheonTerrain = 서천)
      SeaTerrain.cs              홍현 지형 (해변 비탈, 다랭이논 계단, 갯바위 섬)
      SeasonFx.cs                계절이 보이는 것 (겨울 한강 얼음판·얼음 구멍, 눈 덮인 둔치)
      LobbyRoom.cs               대기실(한국 낚시 조합) 실내, 박제 물고기, 고정 카메라 구도
      Aquarium.cs                대기실 수족관 (헤엄치는 물고기, 장식, 배고픔)
      WaterSurface.cs            격자 수면 메쉬 + Fishing/Water 머티리얼 (맵마다 색)
      WaterGlints.cs             밤 물 위에 길게 비치는 불빛 (가로등·다리·도시)
      SkyDecor.cs                달·별·구름 돔 (카메라를 따라다님)
      KenneyProps.cs             Kenney CC0 모델 배치 헬퍼
      ProceduralTextures.cs      물 노멀맵, 구름 노이즈, 달 텍스처
      FishSchoolManager.cs       어군 자리·이동, 큰/작은 어군, 소용돌이·파문
      Effects.cs                 물보라(공유 파티클), 파문
    Player/
      PlayerController.cs        3인칭 캐릭터 이동, 발소리, 낚싯대 기울기, 배 탑승(Mount)
      BoatController.cs          배: 띄우기·운전·연료·흔들림(안정성 × 파도)·돛(바람)·먼바다 제한·뱃머리 낚시·트롤링
      CameraRig.cs               3인칭 ↔ 파이팅 1인칭 ↔ 고정(대기실), 연출 구도(Cast/Bite/Cheer), 흔들림·펀치
    Fishing/
      FishingController.cs       낚시 상태 머신 + 루어 감기·저킹, 플라이, 트롤링, 얼음 구멍 낚시
      FishFight.cs               파이팅 시뮬레이션 (순수 로직)
      BiteSelector.cs            어떤 고기가 몇 cm로 물지 결정 (맵·서식지·미끼·시간·날씨·어군·계절·얼음·먼바다)
      Bobber.cs                  찌 / 루어 모양, 비행·부유·예신·본신
      FightRodView.cs            1인칭 낚싯대·팔(상의 색 소매)·반장갑 손, 낚싯줄, 물고기 위치·몸체
      TelescopicRod.cs           뽑기대(마디 펼치기·휨 스프링) + 스피닝 릴
      FishModels.cs              어종별 물고기 모델 (Resources/Fish/Species, 길이 = 실제 cm), 잡동사니 모델, 아이콘
      FightBot.cs                자동 파이팅 입력 (밸런스 시뮬레이션·자동 스크린샷용)
    UI/
      GameUI.cs                  모든 UI 생성, 단축키
      UIFactory.cs, UIStyle.cs   코드 UGUI 헬퍼(버튼 클릭음 포함), 다크 유리 스타일
      UIChart.cs                 EasyChart를 코드로: Line/Bars/Donut/Radar
      ItemIcons.cs               아이템 아이콘 (Resources/Icons/Items, Blender 렌더)
      HudView.cs, FightHudView.cs, LobbyView.cs    필드 HUD(대회 순위 포함) / 파이팅 HUD / 대기실 HUD
      ResultPanel.cs, ResultFishStage.cs           결과 창, 3D 물고기(전용 카메라 → 투명 렌더 텍스처)
      DexPanel.cs                도감 (아이콘·실루엣, 3D 상세 무대)
      KeepNetPanel.cs, ShopPanel.cs, InventoryPanel.cs, UpgradePanel.cs, CostumePanel.cs, ExchangePanel.cs, AquariumPanel.cs
      InfoPanels.cs              퀘스트 창 (이야기·일일 의뢰·업적·대회 탭), 도움말
      OptionsPanel.cs            옵션 (O)
      FishingAreaPanel.cs, KoreaMap.cs, AreaMapPanel.cs, DialogPanel.cs, PortraitCamera.cs, UIPanel.cs
    Audio/SoundFx.cs             절차적 낚시 소리 + Kenney CC0 UI·발소리
    Dev/DevAutomation.cs         -autoshot 자동 연출 + 스크린샷
  Shaders/
    Skin.shader                  몸: 입은 옷 속 피부를 버텍스 컬러 마스크(R 상의, G 하의, B 신발, A 긴소매)로 잘라냄
    Garment.shader               옷: 트림(R)·음영(G)·보조 원단(B), 무늬(체크·데님·줄무늬·밀짚 엮음), 장화 속 바지 자르기
    Fish.shader                  물고기 (알파 컷 지느러미: clip(), 렌더 텍스처에서도 알파 유지)
    Water.shader                 버텍스 파도 + 흐르는 노멀맵, 하늘 반사, 밤 불빛 반짝임(WaterGlints)
    Sky.shader                   Mode 0 달·별, Mode 1 구름(덮임 정도·노을색, 흐림·비 층구름)
  Resources/
    Characters/                  Angler_Male·Female.fbx (tools/blender/build_characters.py), 머리카락, 텍스처
    Animations/                  Quaternius UAL1_Standard
    Fish/Species, Fish/Textures  어종별 물고기 FBX·텍스처 (tools/blender/build_fish.py)
    Icons/Fish, Icons/Items, Icons/Junk, Junk/   아이콘·잡동사니 모델 (build_fish.py, build_icons.py)
    Terrain/                     Poly Haven CC0 바닥 텍스처 (SOURCES.md)
    Audio/Kenney/                Kenney CC0 UI·발소리 (SOURCES.md)
    Kenney/                      Kenney CC0 모델 (도시·도로·자연·배)
    UI/, Fonts/                  Paraform 재질, Jalnan2 SDF (TMP)
    PostFxConfig.asset           후처리 리소스 참조
  EasyChart/, Le Tai's Asset/, TextMesh Pro/   외부 UI 에셋 (데모 폴더 포함 그대로)
  ThirdParty/Quaternius/, ThirdParty/Kenney/   라이선스 사본, 원본 FBX(빌드에 안 들어감)
  Editor/
    ProjectTools.cs              Setup(씬·빌드 설정·셰이더·후처리), BuildWindows, DeleteSave
    AssetImportRules.cs          캐릭터·애니메이션 휴머노이드, 머리 Generic, 물고기, 텍스처 크기, Loop 클립
    AssetInspector.cs            임포트된 에셋 정보 → Logs/asset-inspect.txt
    FishModelCheck.cs            물고기 모델 확인 → Logs/fish-check.txt, 강제 재임포트
    FontSetup.cs                 Jalnan2 TMP 폰트 에셋 생성
    BalanceSim.cs                파이팅 밸런스 시뮬레이션 → docs/balance-report.md
    Tests/GameRulesTests.cs      EditMode 단위 테스트 25개 (sh tools/test.sh)
tools/
  build.sh, autoshot.sh, test.sh, balance.sh    배치모드 빌드 / 자동 스크린샷 / 테스트 / 밸런스
  blender/                       headless Blender(bpy) 파이프라인: 캐릭터 옷(garments.py), 물고기(build_fish.py), 아이콘(build_icons.py)
```

## 실행 흐름

```
GameBootstrap.Awake
 ├─ SaveSystem.Load → PlayerProfile, QuestSystem
 ├─ GameClock, WeatherSystem, Tide, SeasonFx, TournamentSystem, Settings.Load, PostFx
 ├─ MapLoader → (한강) WorldBuilder.Build / (홍현·서천) 맵 월드 빌더 → DayNightCycle.Init(가로등)
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

## 연출 (원작 튜토리얼 영상 분석)

`FishingController.UpdateCameraShot()`이 매 프레임 `CameraRig.Shot`을 정하고, 각 단계에서 캐릭터 자세(`PlayerController.RodPitch/RodYaw/RodPitchSpeed/BodyLean`)를 넣는다.

| 단계 | 카메라 | 캐릭터·효과 |
|---|---|---|
| Charging | Cast: 오른쪽 어깨 뒤 근접 | 당긴 만큼 낚싯대를 머리 뒤로(최대 155°), 상체 젖힘 |
| 놓는 순간 | 흔들림·화각 펀치 | 낚싯대를 빠르게(RodPitchSpeed 40) 앞으로, 초릿대 Kick |
| Casting | Cast: 날아가는 찌를 본다 | 큰 비거리 숫자가 `FlyingDistance`를 따라 올라감 |
| Bite | Bite: 옆모습 클로즈업 | 마우스 좌우대로 낚싯대 흔들림(`SwayRod`) |
| 챔질 성공 | 흔들림·펀치·히트 스톱(0.12초) | 0.35초 동안 홱 채기 → 파이팅, HIT 문자 |
| Fighting | 1인칭, 물고기 쪽 시선·기울기, 감기 펌핑 | Burst 물보라·흔들림, HIGH 장력 금빛 낚싯대, 카운터 히트 스톱 |
| 랜딩 | Cheer: 얼굴 앞 1.1초 | 환호 → `ResultPanel.Show` |

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

### 새 맵 추가
맵은 `FieldMapDef`(World/FieldMap.cs) 하나다. 서천 앞바다(`SeocheonMap.cs`)가 가장 최근 예시.
1. `FieldMapDef`를 상속해 격자(열·행·칸 크기), `HabitatAt`, `IsWater`/`IsWalkable`, 시작·상점 위치, 수면 색, (바다면) `IsSea`·`TideAmplitude`를 정한다.
2. 월드 빌더(지형은 `TerrainBuilder`에 높이·칠하기 함수만 넘기면 된다)와 NPC를 만든다.
3. `Maps.All`에 등록하고 `FishingAreas`에서 낚시터 목록에 연다(레벨 조건).
4. 어종은 `FishSpecies.Maps`에 맵 id를 넣는다. 도감·일일 의뢰·업적(맵 도감 완성)은 자동.
5. 자동 스크린샷(`DevAutomation`)에 장면을 추가하고 문서 02(맵 절)·06을 갱신한다.

### 아트 교체
- 캐릭터: `CharacterFactory.Create(look, parent)`가 Angler FBX(몸 `Body` + 옷 메쉬, `tools/blender/build_characters.py`)를 만든다. 옷을 고치거나 더하려면 `garments.py`에 함수를 추가하고 `build()`에 등록 → `CharacterFactory.Garments`·`GarmentMaterial`에 이름을 넣는다(08 문서 3장). 몸은 `Fishing/Skin`, 옷은 `Fishing/Garment` 셰이더.
- 애니메이션: `CharacterAnimator`의 상수(Idle/Walk/HoldRod/Cast/Hook…)가 클립 이름이다. 다른 라이브러리를 쓰면 이름만 바꾼다.
- 물고기: `tools/blender/fish_species.py`에 어종(체형 템플릿 A~H·두족류 O/Q, 색·무늬·지느러미)을 넣고 `build_fish.py --only <id>` → `Resources/Fish/Species/<id>.fbx`·텍스처·아이콘. `FishModels.CreateSpecies`가 길이(cm/100)로 키운다(09 문서).
- 물: `WaterSurface` + `Fishing/Water` 셰이더 (`_WaveAmp`, `_WaveLen`, 노멀 타일·속도). 하늘 반사는 `DayNightCycle`의 리얼타임 반사 프로브(하늘만, 2초마다).
- 낚싯대·릴: `TelescopicRod.StyleFor`(낚싯대 색·길이·마디 수)와 `BuildReel`(릴 모양)에서 장비 id별로 고른다. 좌표 규약은 +Y가 낚싯대 방향, 원점이 릴 시트, +Z가 릴 쪽. 휨은 `Drive(목표각, 줄 방향, 떨림, dt)`, 펼치기는 `SetExtension(0~1)`, 릴은 `SpinReel(감기 바퀴/s, 줄 풀림 m/s, dt)`.
