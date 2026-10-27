# 03. 코드 구조

## 원칙

- **씬에는 `GameBootstrap` 하나만 둔다.** 월드, 플레이어, 카메라, UI를 모두 코드로 만든다. 씬에 없어도 `RuntimeInitializeOnLoadMethod`로 자동 생성된다.
- **월드 아트는 코드로 만든다.** 프리미티브, 런타임 머티리얼(`Mats`), 절차적 텍스처/스프라이트, 절차적 사운드(`SoundFx`) + CC0 에셋(Quaternius·Kenney 모델, Poly Haven 텍스처, Kenney 소리). 사람은 모두 실행 중에 읽는 VRM(`StreamingAssets/Characters` + 문서 폴더, 16 문서), 물고기·아이콘은 `tools/blender`의 headless Blender 스크립트로 만들어 FBX/PNG로 커밋한다. 나중에 에셋으로 바꿀 때는 `WorldBuilder`, `PlayerController.BuildModel`, `FightRodView.Build`, `Bobber.Create`만 교체하면 된다.
- **UI도 코드로 만든다**: 둥근 유리 도형·뒤 화면 흐림은 Translucent Image + Paraform(`UIStyle`), 차트 `UIChart`(메쉬로 직접), TextMeshPro(Jalnan2 글꼴). 프리팹 없이 `UIFactory`/`UIStyle`/`UIChart`로 조립한다.
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
      GameBootstrap.cs           모든 시스템 생성·연결, 자동 저장, 체력 자연 감소·음식 분당 회복·포커싱 쿨타임, 레벨 보상·출석 체크
      Game.cs                    전역 참조(서비스 로케이터), GameMode(Lobby/Field), 채널, Toast 이벤트, Save()
      GameClock.cs               게임 시계 (1초 = 1분, 하루 24분), 시간대
      WeatherSystem.cs           날씨 전환, 비 파티클(겨울엔 눈), 바람(돛단배)·파도
      DayNightCycle.cs           해/달, 스카이박스, 주변광, 안개, 가로등, 하늘 반사 프로브, SkyDecor(구름 양) 제어
      PostFx.cs, PostFxConfig.cs 후처리 (Post Processing Stack v2: 블룸, Neutral 톤매핑, AO, SMAA, 시간대별 그레이딩)
      MapLoader.cs               낚시터 바꾸기 (월드·가로등·어군 다시 만들기, 시작 위치)
      Tide.cs                    물때 (서해처럼 TideAmplitude가 있는 맵에서 수면 높이가 오르내림), 물이 들면 갯벌·모래톱에서 해안으로 구조
      TournamentSystem.cs        조합 낚시 대회 진행 (참가·집계·AI 낚시꾼·시상)
      PlayerProfile.cs           레벨/경험치/골드/체력(2단계 지침)/음식/포커싱/레벨 보상/목장갑/일지/아이템/장비/의상/도감 규칙 + SaveSystem
      SaveData.cs                JsonUtility 직렬화용 데이터 (수족관·대회·일일 의뢰·릴 내구도·잃은 줄·RewardSave 포함)
      QuestSystem.cs             이야기 퀘스트, 대물 도전, 일일 의뢰, 업적 판정, 소라고동, 대물 코인, 일지 기록
      CatchRewards.cs            원작 RESULT 말풍선 토스트·BONUS·등급(초급/중급/고급)·오늘의 이벤트 어종(EXP +200%)·재도전(복수혈전 X2/X3/X5)·조준 업적 3줄 (RewardSave)
      AnglerChatter.cs           같은 채널 다른 낚시꾼의 조과·잡담 시스템 줄 (솔로판 분위기용, 규칙 영향 없음)
      Bingo.cs                   빙고 5×5 (칸 만들기·채우기·줄 보상, BingoSave)
      Attendance.cs              출석 체크 (실제 날짜, 7일째 블랙스피어)
      Settings.cs                옵션(소리·감도·화면·진동)과 키 설정 (PlayerPrefs)
      Rumble.cs                  게임패드 진동 (새 Input System의 Gamepad만, 조작은 Input Manager)
      Mats.cs                    런타임 머티리얼 캐시, 프리미티브·텍스처 상자 헬퍼, 절차적 텍스처
    Data/                        정적 데이터 테이블 (순수 C#)
      Enums.cs                   Habitat, TimeBand, Weather, BaitKind + 한글 이름
      FishDatabase.cs            어종 77종(한국·첫 해외 거점) + 잡동사니 6종 (맵·제철·어군 전용·먼바다 전용·괴물 어종 BossCoins). 바다 묶음 SouthSea·WestSea·EastSea·AllSeas, 미끼 판매 SeaMaps·FreshMaps.
                                 정적 생성자가 아래 지역별 partial의 …Species 목록과 …MapAdds(이미 있는 어종이 새 맵에도 산다)를 합친다 → 모두 165종
      FishDatabase.Korea2.cs     한국 7종 (황복·웅어·참다랑어 등, docs/fish-db 01)
      FishDatabase.SeaAsiaFresh.cs  동남아 민물 20종 (메콩 대형 메기·자이언트 바브·피라루쿠…, 앙코르·돈콘·방콕)
      FishDatabase.SeaAsia.cs    동남아 바다 27종 (돛새치·청새치·대왕바리·나폴레옹피시…)
      FishDatabase.Mediterranean.cs / .Mediterranean2.cs  지중해 34종 (덴텍스·갈색 그루퍼·미거·쏠배감펭…)
      GearDatabase.cs            낚싯대/릴/줄/배, 미끼(해체 미끼 5등급 포함), 음식 5종(FoodDef), 레벨 보상(LevelRewards)
      ItemDatabase.cs            가방 아이템 이름·설명
      CostumeDatabase.cs         의상 6부위 25벌 + 능력치
      AchievementDatabase.cs     업적 35개, 레벨 칭호
      QuestDatabase.cs           이야기 퀘스트 87개 (낚시터마다, 해외 51개 — 새 해외 맵 14곳의 이야기는 그 맵 파일의 <이름>Chapter())
      RodUpgrade.cs              장비 강화 규칙 (+1~+15, 확률·비용·재료·성능)
      Seasons.cs                 계절 (한 달 = 게임 7일, 1일째 = 4월), 제철 입질 배율
      TournamentRules.cs         대회 규칙 (미션 점수, 엠블럼, 대회 맵 순서 — 6일 주기, 해안 낚시터 지역 대회)
      FishingAreas.cs            낚시터 목록 (한국 12곳 + 해외 17곳), 거점 4개(Hubs), 경위도, 입장 레벨, 맵 id
    Characters/
      CharacterFactory.cs        CharacterLook(이름·칭호·성별·VRM id) → VRM 복제 + 동작, 못 읽으면 마네킹, 이름표
      CharacterRoster.cs         characters.json: 기본 캐릭터 남/녀, NPC 모델 고르기(이름 지정 → 성별 목록), 문서 폴더가 게임 폴더를 덮음
      VrmCharacters.cs           VRM 폴더 훑기·바로 읽기(LoadNow)·뒤에서 읽기(Request)·원본 선반에서 복제 (16 문서)
      CharacterAnimator.cs       Playables 믹서로 클립 재생 (AnimatorController 없이 SetLoop/PlayOnce). "이름_M/_F" 클립은 성별로 고른다
      NpcAngler.cs               NPC (Angler/Talker/Sitter). NpcId로 대화 종류, 대화 상대 바라보기, 조합 연락관 머리 위 퀘스트 표시(? !)
    World/
      FieldMap.cs                FieldMapDef(맵 정의: 격자·서식지·물/땅 판정·물때·먼바다·수면 색) + 정적 FieldMap(현재 맵) + Maps.All
      HanRiverMap.cs             한강 반포지구 정의 (A~F × 1~4)
      HonghyeonMap.cs            홍현 앞바다 정의·월드 (A~H × 1~8, 방파제·등대·갯바위 섬·다랭이 마을)
      SeocheonMap.cs             서천 앞바다 정의·지형·월드 (갯벌·홍원항 등대·동백정 정자)
      CoastMap.cs                해안 맵 틀: CoastSpec(사양) → CoastMapDef(지형 높이·판정) + CoastWorldBuilder(월드)
      KoreaCoasts.cs             해안 낚시터 9곳 사양 (영흥도·태안·삼척·등대섬·헌화로·욕지도·제주·청진포구·거문도)
      OverseasCoasts.cs          해외 거점 3곳 사양 (하롱베이·산토리니·나일강 하류) + 해외 맵 공용 도우미(School·Special·Local·Union·Npc)
      CoastWorldApi.cs           CoastSpec.Extra(맵 고유 풍경)가 쓰는 CoastWorldBuilder 공개 도우미(Root·Height·Rand·PlaceHouse·PlaceTree·PlaceBoats·AddLamp·KarstPeak·Gable·Sign)
                                 + 새 집 양식 4가지 (고상 가옥·리조트 빌라·타이 겹지붕 집·베네치아풍 회벽 집)
      Maps/<이름>.cs             새 해외 맵 14곳 — 파일 하나에 사양(OverseasCoasts.<이름>()) · 랜드마크(<이름>Extra) · 이야기(QuestDatabase.<이름>Chapter())
      Maps/MapKit.cs             랜드마크 공용 도구: 회전체 Lathe(톱니 단면 프랑), 관 묶음 Tubes, 원판 묶음 Discs, 눈 덮인 산 Mountain, 폭포 FallSheet·WaterfallMat·물보라 Splash·FallDrift, 들보 Beam, Blender 소품 Prop
      CoastWorldOverseas.cs      CoastWorldBuilder의 해외 부분(partial): 지역 양식 집(키클라데스·이집트·베트남)·나무(야자수·열대·지중해), 강 맞은편 둑·사막 판, 석회암 봉우리·정크선·수상 마을·칼데라·풍차·펠루카·피라미드
      HarborKit.cs               포구 콘크리트 구조물 (방파제·호안): 모따기 블록·돌출 단면 메쉬 버퍼(꼭짓점 AO), 10m 타설 블록·파라펫·연석·계선주·타이어·사다리 (해안 맵 틀·서천·홍현)
      CoastProps.cs              바닷가 공용 소품 (등대·큰 등대·테트라포드·정자·가로등·낚시점·바위·한국 바닷가 집)
      Hanok.cs                   한옥 부재: 팔작 기와지붕 메쉬(기와골·수막새·서까래·용마루·취두), 정자(기단·주춧돌·공포·단청 창방·계자 난간), 단청 텍스처
      TextureScroll.cs           재질 텍스처를 흐르게 (폭포 물줄기)
      (배 모델은 tools/blender/boats.py → Resources/Props/boat_*.fbx·yacht.fbx, KenneyProps.BoatSwap이 Kenney 배 경로를 바꿔 끼운다)
      WorldBuilder.cs            한강 월드 (다리, 데크, 낚시점, 나무, 도시, NPC)
      HanRiverLandmarks.cs       한강에서 보이는 롯데월드타워·63빌딩·남산서울타워(로프트 메쉬 + 코드 텍스처), 먼 시가지, CU 편의점
      DriedPollock.cs            덕장에 거는 명태 한 쌍 메쉬·텍스처 (청진포구)
      TerrainBuilder.cs          높이 함수 + 칠하기 함수 → Unity Terrain (SeaTerrain = 홍현, SeocheonTerrain = 서천)
      SeaTerrain.cs              홍현 지형 (해변 비탈, 다랭이논 계단, 갯바위 섬)
      SeasonFx.cs                계절이 보이는 것 (겨울 한강 얼음판·얼음 구멍, 눈 덮인 둔치)
      LobbyRoom.cs               대기실(한국 낚시 조합) 실내, 박제 물고기, 고정 카메라 구도
      Aquarium.cs                대기실 수족관 (헤엄치는 물고기, 장식, 배고픔)
      WaterSurface.cs            격자 수면 메쉬 + Fishing/Water 머티리얼 (맵마다 색), 해저 높이 텍스처 굽기(SetSeabed → 물가 거품)
      WaterGlints.cs             밤 물 위에 길게 비치는 불빛 (가로등·다리·도시)
      WorldBatcher.cs            맵을 지은 뒤 움직이지 않는 Standard·월드 좌표 셰이더 소품을 정적 배칭으로 묶기 (아래 "정적 배칭")
      SkyDecor.cs                해 원판(번지는 빛)·달·별(번지는 사각형 2,200개, 은하수 띠, 반짝임)·구름 돔 (카메라를 따라다님). 해·달 길은 DayNightCycle(고도 최대 30°, 물 쪽)
      KenneyProps.cs             Kenney CC0 모델 배치 헬퍼
      ProceduralTextures.cs      물 노멀맵, 구름 노이즈, 달 텍스처, 어군 소용돌이, 위에서 본 물고기 그림자
      FishSchoolManager.cs       어군 자리·이동, 큰/작은 어군, 히든·포식자 어군·물속 그림자(SchoolKind), 소용돌이(색)·파문, 그림자 헤엄
      FishShadowFx.cs            물속 그림자 실루엣: 수면 바로 위 납작한 띠를 S자로 휘어 꼬리짓 (Fishing/FishShadow)
      Effects.cs                 물보라(공유 파티클), 파문
    Player/
      PlayerController.cs        3인칭 캐릭터 이동, 발소리, 낚싯대 기울기, 배 탑승(Mount)
      BoatController.cs          배: 소환 연출(바다 쪽에서 달려와 돌아 선다)·엔진 START/STOP(Y)·운전·연료·흔들림(안정성 × 파도)·돛(바람)·먼바다 제한·뱃머리 낚시·트롤링
      CameraRig.cs               3인칭 ↔ 파이팅 1인칭 ↔ 고정(대기실), 연출 구도(Cast/Bite/Cheer), 흔들림·펀치
    Fishing/
      FishingController.cs       낚시 상태 머신 + 루어 감기·저킹, 플라이, 트롤링, 얼음 구멍 낚시
      FishFight.cs               파이팅 시뮬레이션 (순수 로직)
      BiteSelector.cs            어떤 고기가 몇 cm로 물지 결정 (맵·서식지·미끼·시간·날씨·어군·계절·얼음·먼바다)
      Bobber.cs                  찌 / 루어 모양, 비행·부유·예신·본신
      FightRodView.cs            1인칭 낚싯대(화면 좌우 끝까지 쓸림·옆으로 휨), 낚싯줄, 물고기 위치·몸체
      TelescopicRod.cs           뽑기대(마디 펼치기·휨 스프링) + 스피닝 릴
      FishModels.cs              어종별 물고기 모델 (Resources/Fish/Species, 길이 = 실제 cm), 잡동사니 모델, 아이콘
      FightBot.cs                자동 파이팅 입력 (밸런스 시뮬레이션·자동 스크린샷용)
    UI/
      GameUI.cs                  모든 UI 생성, 단축키
      UIFactory.cs, UIStyle.cs   코드 UGUI 헬퍼(버튼 클릭음 포함), 다크 유리 스타일
      UIChart.cs                 EasyChart를 코드로: Line/Bars/Donut/Radar
      ItemIcons.cs               아이템 아이콘 (Resources/Icons/Items, Blender 렌더)
      HudView.cs, FightHudView.cs, LobbyView.cs    필드 HUD(대회 순위·WARNING 카드·재도전 말풍선/배지·조준 업적 3줄·엔진 버튼 포함) / 파이팅 HUD / 대기실 HUD
      IntroSequence.cs           인트로: 사업자 로고 「제쓰로네 게임공방」 → 타이틀 「짭풍명월」(겹 그림 시차·물안개·배·꽃잎) → 아무 키 → 대기실 (아래 "인트로")
      ResultPanel.cs, ResultFishStage.cs           결과 창, 3D 물고기(전용 카메라 → 투명 렌더 텍스처). 자세: 물고기는 옆모습으로 튀어 오름,
                                                   장어처럼 긴 고기(몸길이/몸높이 > 7)는 60° 굴려 S자 꿈틀이 보이게, 잡동사니는 비스듬히 내려다보며 천천히 돈다
      DexPanel.cs                도감 (아이콘·실루엣, 3D 상세 무대)
      KeepNetPanel.cs, ShopPanel.cs, InventoryPanel.cs, UpgradePanel.cs, CharacterPanel.cs(내 캐릭터: 장비 칸·최종 능력치 / 의상 탭), ScreenFlash.cs(장면 컷을 가리는 화면 번쩍임), ExchangePanel.cs, AquariumPanel.cs
      InfoPanels.cs              퀘스트 창 (이야기·일일 의뢰·대물 도전·빙고·출석·업적·대회·일지 탭), 도움말
      OptionsPanel.cs            옵션 (O)
      FishingAreaPanel.cs        낚시터 목록 (거점 탭, 거점 지도, MAP/FISH INFO)
      KoreaMap.cs, RegionMap.cs  거점 지도 텍스처 (한국 / 동남아·지중해·아프리카: 해안선 다각형·강 선)
      AreaMapPanel.cs, DialogPanel.cs, PortraitCamera.cs, UIPanel.cs
    Audio/SoundFx.cs             절차적 낚시 소리 + Kenney CC0 UI·발소리
    Audio/IntroAudio.cs          인트로 소리 (절차적): 낙관 쿵, 종, 붓, 징, 가야금풍 타이틀 음악(Karplus-Strong 평조 선율 + 바람·물결) — 작업 스레드에서 계산
    Dev/DevAutomation.cs         -autoshot 자동 연출 + 스크린샷
  Shaders/
    Fish.shader                  물고기 (알파 컷 지느러미: clip(), 렌더 텍스처에서도 알파 유지)
    UIInkReveal.shader           인트로 UI: 붓으로 쓰듯 왼쪽부터 번지며 드러남(_Progress), 지나가는 빛줄기(_Shine), 더하기 섞기(물에 비친 달빛)
    Water.shader                 버텍스 파도 + 흐르는 노멀맵, 하늘 반사, 밤 불빛 반짝임(WaterGlints), 물가 거품·얕은 물 (해저 높이 텍스처)
    Sky.shader                   Mode 0 달·별, Mode 1 구름(덮임 정도·노을색, 흐림·비 층구름)
    SkyCube.shader               흐림·비 하늘 큐브맵 돔 (AllSky "Overcast Low", 스카이박스 뒤 Transparent-300에 그린다)
  Resources/
    Animations/                  Quaternius UAL1_Standard, Motions.fbx(직접 만든 기본 동작·낚시꾼 자세·반응 동작, tools/blender/build_motions.py),
                                 StarterAssets/*.anim(Unity Starter Assets 대기·걷기·뛰기·점프, StarterClipBake가 180° 돌려 구움) → 10 문서
    Sky/                         하늘 돔 큐브맵 Overcast·Fair01~03 (Poly Haven CC0 하늘 사진, tools/sky/build_sky.py, SOURCES.md)
    Plants/                      ez-tree 덤불 6·작은 나무 4 LOD 프리팹 (tools/blender/build_vegetation.py → Editor/VegetationBake)
    Fish/Species, Fish/Textures  어종별 물고기 FBX·텍스처 (tools/blender/build_fish.py)
    Icons/Fish, Icons/Items, Icons/Junk, Junk/   아이콘·잡동사니 모델 (build_fish.py, build_icons.py)
    Props/                       해외 소품 FBX: 야자수 둘·펠루카·정크선 (build_props.py, 실제 미터)
    Terrain/                     Poly Haven CC0 바닥 텍스처 (SOURCES.md)
    Audio/Kenney/                Kenney CC0 UI·발소리 (SOURCES.md)
    Kenney/                      Kenney CC0 모델 (도시·도로·자연·배)
    Fonts/                       Jalnan2 SDF (TMP)
    Plants/                      덤불·작은 나무 프리팹 (VegetationBake, KenneyProps가 이름으로)
    Sky/                         하늘 돔 (Poly Haven, tools/sky/build_sky.py)
    PostFxConfig.asset           후처리 리소스 참조
  Vegetation/                  나무·덤불(ez-tree)·바위·풀 카드(Poly Haven) — tools/blender/build_vegetation.py, 재질·LOD 프리팹은 Editor/VegetationBake (17 문서)
  TextMesh Pro/                TMP 기본 리소스 + Jalnan2 글꼴(라이선스 사본)
  ThirdParty/Quaternius/, ThirdParty/Kenney/   라이선스 사본 (UAL 동작·Kenney)
  ThirdParty/StarterAssets/    Unity Starter Assets 동작 FBX 원본·아바타(Armature.fbx), SOURCES.md
  StreamingAssets/Characters/   기본 VRM 11개(VRoid CC0 샘플, Git LFS) + characters.json + 읽어주세요.txt — 빌드 뒤에도 바꿀 수 있다 (가이드 07)
  Editor/
    ProjectTools.cs              Setup(씬·빌드 설정·셰이더·후처리), BuildWindows, DeleteSave
    AssetImportRules.cs          애니메이션 휴머노이드, 물고기, 텍스처 크기, Loop 클립
    AssetInspector.cs            임포트된 에셋 정보 → Logs/asset-inspect.txt
    FishModelCheck.cs            물고기 모델 확인 → Logs/fish-check.txt, 강제 재임포트
    FontSetup.cs                 Jalnan2 TMP 폰트 에셋 생성
    NatureKitBake.cs             Tree Creator 나무 → 일반 메쉬 프리팹 굽기 (빌드 전 자동, 메뉴 Fishing > Bake Nature Kit Trees)
    BalanceSim.cs                파이팅 밸런스 시뮬레이션 → docs/balance-report.md
    Tests/GameRulesTests.cs      EditMode 단위 테스트 26개 (sh tools/test.sh)
    Tests/CoastMapTests.cs       낚시터 정의 검사 5개 (+ GameRulesTests의 대회 지역 순환) (물때마다 걸을 수 있는 곳, 어종이 사는 곳에서 낚이는지, 가두리 통로, 모래톱, 강 맵 맞은편 둑)
tools/
  build.sh, autoshot.sh, test.sh, balance.sh    배치모드 빌드 / 자동 스크린샷 / 테스트 / 밸런스
  fish_table.py                  FishDatabase*.cs(지역별 partial·이름 붙은 맵 배열·MapAdds 포함) → 02 문서 11장 어종 표 (--write)
  fightsim/                      Unity 없이 파이팅 밸런스 재기 (.NET 8, 게임 소스 + UnityEngine 대체 Stubs.cs) — 04 문서
  blender/                       headless Blender(bpy) 파이프라인: 물고기(build_fish.py, 어종 설계는 fish_species.py + 지역별 species_*.py), 아이콘(build_icons.py), 해외 소품(build_props.py + props_sea.py)
  intro/build_intro_art.py       인트로 그림 → Resources/Intro (numpy·PIL로 그린 달밤 수묵 산수화 겹, 붓글씨 타이틀, 낙관). 글꼴 intro/fonts (SIL OFL)
```

## 실행 흐름

```
GameBootstrap.Awake
 ├─ SaveSystem.Load → PlayerProfile, QuestSystem
 ├─ GameClock, WeatherSystem, Tide, SeasonFx, TournamentSystem, Settings.Load, PostFx
 ├─ MapLoader → (한강) WorldBuilder.Build / (홍현·서천) 맵 월드 빌더 / (해안·해외 12곳) CoastWorldBuilder → WorldBatcher.Batch → DayNightCycle.Init(가로등)
 ├─ FishSchoolManager
 ├─ PlayerController (+ 모델), CameraRig (+ AudioListener, SoundFx)
 ├─ FishingController (Start에서 찌, 1인칭 낚싯대, 낚싯줄, 착지 링 생성)
 ├─ GameUI (EventSystem, Canvas, HUD, 대기실 HUD, 창들)
 └─ LobbyRoom.Build (한강에서 멀리 떨어진 (0,0,-420)에 실내를 짓는다)
GameBootstrap.Awake 끝 → IntroSequence.Play(Welcome)  (Unity 스플래시 뒤 사업자 → 타이틀, 정렬 500 캔버스가 화면을 덮는다)
GameBootstrap.Start → GameUI.EnterLobby (Mode=Lobby, 카메라 고정) — 인트로 뒤에서 대기실이 미리 그려져 셰이더가 데워진다
IntroSequence 끝 → Welcome (환영·퀘스트·레벨 보상·출석 알림)
```

**배에 탄 플레이어**: `PlayerController.Mount(자리, 앉음)`이 CharacterController를 끄고, `BoatController.Update`가 배를 움직인 뒤 `SnapToMount()`로 플레이어를 자리(운전석/뱃머리)에 옮긴다. 플레이어 `Update`는 배 위에서 이동하지 않고 WASD 눌림만 `MovedThisFrame`으로 알린다(찌 회수용). `Dismount`는 `Teleport`로 둔치에 내려놓는다.

**게임 모드** (`Game.Mode`):
- `Lobby`: 플레이어 이동·낚시·비가 멈추고, 카메라는 `LobbyRoom.CameraPos/Rot`에 고정된다. 필드 HUD 대신 `LobbyView`가 보인다.
- `Field`: `GameUI.EnterField(channel)`로 들어온다. 카메라 고정을 풀고 3인칭으로 돌아간다. `↩ 조합` 또는 `EnterLobby()`로 나간다(파이팅·결과 중에는 막힘, 던진 찌는 `ForceIdle`로 거둠).

매 프레임:
- `FishingController.Update`가 상태 머신을 진행한다. 파이팅 중에는 `FishFight.Tick(dt, input, 체력계수, 힘계수(내구도×의상×목장갑), 릴계수)`를 부르고, Q를 누르면 `FishFight.Focus()`(포커싱).
- `GameUI.Update`가 `HudView.Update`와 `FightHudView.Update`를 부른다. 둘 다 상태를 **폴링해서** 그리고, 창은 `PlayerProfile.Changed` 이벤트가 오면 다시 그린다.
- `Game.Toast()`가 게임 어디서든 화면 위쪽에 메시지를 띄운다.

## 인트로 (`IntroSequence`, 2026-10-16)

게임을 켜면 **Unity 스플래시(검은 바탕) → 사업자 「제쓰로네 게임공방」 → 타이틀 「짭풍명월」 → 아무 키 → 대기실**.

| 장면 | 연출 (초) |
|---|---|
| 사업자 (5.4초) | 0~0.9 검은 막이 걷힘 · 0.45~0.7 붉은 낙관이 커졌다가 **쾅**(흔들림·붉은 빛·인주 튀김, 도장 소리) · 1.1~2.3 「제쓰로네 게임공방」이 붓으로 쓰듯 왼쪽부터(붓 소리) · 2.0 금빛 선이 펼쳐지며 영어 이름(종) · 2.9 빛줄기 · 4.6 어두워짐 |
| 타이틀 | 0~2.2 밝아지며 카메라가 천천히 물러남(11초) · 1.6~3.5 「짭풍명월」 붓글씨(붓 소리) · 3.72 「짭」 낙관 쾅(징) · 3.9 영어 · 4.4 「바람 맑고 달 밝은 밤, 낚싯대 하나」 · 5.6 「아무 키나 눌러 시작」 깜빡임. 음악은 가야금풍 36초 반복 |
| 나가기 | 종 소리, 1.1초 동안 어두워지며 음악이 빠짐 → `Welcome` → 0.9초 동안 대기실이 밝아짐 |

- **건너뛰기**: 사업자 화면에서 아무 키 → 바로 어두워짐. 타이틀이 써지는 중에 아무 키 → 다 쓴 상태로(붓 소리 끊고 징 생략), 다시 아무 키 → 게임.
- 모든 연출은 장면 시간 t의 함수(`CompanyPose(t)`, `TitlePose(t)`)라서 건너뛰기는 t를 옮기기만 한다. 첫 프레임처럼 오래 걸린 프레임 때문에 연출이 튀지 않게 dt는 0.05초로 자른다.
- 무대는 1920×1080(가운데 원점). 화면 비율이 달라도 덮도록(cover) 키운다. 배경 겹은 화면보다 6% 크게 그려서 마우스 시차·천천히 흐르는 흔들림의 여유가 있다.
- 타이틀 겹(뒤→앞, 시차 계수): 하늘 0.1 · 달 0.12(숨쉬듯 밝아짐) · 엷은 구름 0.18(가로 흐름) · 먼 산 0.3 · 물 0.45 · 달빛 길 0.45(더하기 섞기, 좌우 뒤집은 두 장을 번갈아 → 반짝임) · 물안개 0.5 · 가까운 산·소나무 0.65 · 배와 그림자 0.6(흔들림) · 앞 물안개 0.7 · 꽃잎 22장(바람, 뒤집히며 날림) · 갈대 1.0(흔들림) · 비네트 · 글씨 0.15.
- 그림 다시 만들기: `bpyenv/Scripts/python.exe -I tools/intro/build_intro_art.py --preview <폴더>` (미리보기 두 장도 만든다). 배치 좌표는 미리보기 그림 좌표(`P(x, y)`)를 그대로 쓴다. 임포트 규칙은 `AssetImportRules`의 `Resources/Intro/` (하늘·물·바탕은 무압축 — 압축하면 그러데이션에 띠가 진다).
- 게임 단축키는 `IntroSequence.Playing` 동안 막힌다(`GameUI`). 자동 촬영(-autoshot)은 `00_intro_company`·`00_intro_title`을 찍고 `Continue()`로 넘어간다.
- Unity 스플래시 설정(`ProjectTools.Setup`): 검은 바탕, 흰 로고(Light on Dark). 사업자 화면도 검은 막에서 시작해 이어진다.
- 저장 경로·옵션(PlayerPrefs)이 `companyName`·`productName`을 따르므로 두 값은 바꾸지 않았다(바꾸면 예전 저장이 안 보인다).

## 정적 배칭 (`WorldBatcher`, 2026-10-15)

맵 빌더는 프리미티브 수백~수천 개로 월드를 짓는다(다리 아치·데크 장선·방파제 이음매까지). 같은 재질도 하나씩 그리면 드로우 콜이 많아서, `MapLoader.Load`가 `BuildWorld` 직후 `WorldBatcher.Batch(Game.World)`로 움직이지 않는 것을 `StaticBatchingUtility.Combine`한다.

| 묶는 것 | 안 묶는 것 (이유) |
|---|---|
| Standard·`Fishing/Ground`·`Karst`·`Landmark`·`Weathered`(2026-10-27부터, 월드 트라이플래너라 굳혀도 같다) MeshRenderer, 읽을 수 있는 메쉬(프리미티브·절차적 메쉬), 켜져 있는 것 | 스크립트(MonoBehaviour)·Animator·Rigidbody가 자기나 조상에 있는 것 — NPC 낚시꾼, 어군, 수족관, 잔물결, 배처럼 런타임에 움직인다. 묶인 메쉬는 월드 좌표로 굳는다 |
| | Standard가 아닌 셰이더 — 물(`Fishing/Water`), 하늘, 물고기, 파티클은 오브젝트 좌표·버텍스 애니메이션을 써서 묶으면 깨진다 |
| | 읽을 수 없는 메쉬 (Read/Write를 끈 FBX) — Combine이 못 읽는다 |

- 재질 색을 나중에 바꾸는 것(`SeasonFx`의 눈 덮인 둔치, 밤 가로등 전구)은 묶여도 동작한다. 나중에 생기는 것(얼음판·얼음 구멍)은 안 묶인다.
- **새 소품을 움직이게 만들 때**: 스크립트를 그 물체(또는 부모)에 붙이면 자동으로 빠진다. 스크립트 없이 다른 곳에서 `transform`을 움직이면 안 된다.
- 자동 촬영 로그에 스크린샷마다 `(NN FPS, 맵, 정적 배칭 N개)`가 찍히고, `tools/autoshot.sh`가 맵별 평균 FPS를 출력한다.

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
`FishDatabase.All`에 `FishSpecies`를 추가한다(`Maps`에 사는 낚시터 id, 바다 지역은 `SouthSea`·`WestSea`·`EastSea`·`AllSeas` 또는 `M("id", …)`). 도감, 입질, 퀘스트 텍스트에 자동으로 반영된다. 모델은 `tools/blender/fish_species.py`에 넣고 `build_fish.py --only <id>`. 추가한 뒤 `tools/balance.sh`로 난이도, `tools/test.sh`(CoastMapTests: 사는 곳 격자에 서식지가 있고 그곳에서 파는 미끼로 무는지), `python tools/fish_table.py --write`로 02 문서 표를 갱신한다.

### 장비 추가
`GearDatabase.Gear`에 `GearDef`를 추가한다. 상점(F12) 장비 목록은 `Slot` 순서대로 자동 정렬된다.

### 새 맵 추가
맵은 `FieldMapDef`(World/FieldMap.cs) 하나다.

**바닷가 낚시터는 해안 맵 틀로 만든다** (코드 없이 사양만): `KoreaCoasts.cs`에 `CoastSpec`을 돌려주는 함수를 하나 더하고 `Maps`에 `new CoastMapDef(KoreaCoasts.X())`로 등록한다. 사양에 넣는 것:

| 항목 | 내용 |
|---|---|
| 이름 | `Id`(낚시터 id), `MapName`, `ShortName`, `ShopName`, `UnionContact`(퀘스트 보고 안내), `LandName`·`SeaName`(기본 지역 이름) |
| 물 | `WaterDeep/Shallow` 색, `Tide`(물때 폭, 서해), `FogBoost`(해무), `Snow`(늘 눈) |
| 해안선 | `ShoreZ`(-40), `ShoreBumps`(x, 폭, z 이동: 곶·만), `LandHalfWidth`(섬: 양옆이 바다), `Shores`(x 구간별 `Beach`/`Rock`/`Quay`/`Mud`) |
| 지형 | `NearDepth`·`DepthSlope`(수심), `FlatWidth`(해안 평지), `CliffHeight`·`CliffWidth`(뒤 언덕·절벽), `RoadOffset`(해안도로) |
| 구조물 | `Breakwaters`(몸통 Rect + 꺾인 팔 + 등대 색), `Capes`(등뼈·폭·높이·숲·`Landmark`), `Islets`(반지름·높이·걷기·`SandBar`), `Stacks`(바위기둥), `Platforms`(`FishFarm` 가두리 / `Boardwalk` 데크길 / `Pier` 잔교) |
| 풍경 | `Features` 플래그: `SquidBoats`(집어등), `DryingRacks`(덕장), `SeaBridge`(대교), `Cranes`(큰 항구), `FarRocks`(먼 바위섬), `SuspensionBridge`(출렁다리), `Dunes`(+`DuneArea`), `Mountains`, `Azaleas`, `IceFloes`, `Hallasan`(큰 화산·오름), `DolHareubang`, `Tangerines`(돌담 귤밭), `Columnar`(주상절리 육각 기둥), 해외: `Karst`(수평선 석회암 봉우리), `Junks`(정크선), `FloatingVillage`(수상 마을), `Caldera`(칼데라 테·화산섬), `Windmills`(풍차), `Pyramids`, `Feluccas`(펠루카). `FarIslands`, `VillageDensity`, `NorthPort`(회색 항구 건물), `DarkRock`(검은 현무암) |
| 지역 양식 | `Houses`(`Korean`/`Cyclades` 하얀 집·파란 돔, 절벽 위 마을과 풍차 / `Egypt` 흙벽돌·비둘기탑 / `Vietnam` 좁고 높은 색깔 집), `TreeKind`(`Korean`/`Palm`/`Tropical`/`Mediterranean`), `SandLand`(강가 45m 밖은 사막) |
| 강 | `River`(민물: `IsSea` 거짓 → 먼바다 없음, 민물 미끼), `FarShoreZ`(맞은편 둑이 시작하는 z, `FarShoreAt(x)`로 굽이친다). 지형 밖으로도 양쪽 둑이 이어지게 사막·들판 판을 깐다 |
| 낚시 | `Grid`(8×8 서식지, 0행 = 해안), `Schools`(어군, 대물 어군은 `Special` + 시간대별 칸) |
| 사람 | `Spawn`, `ShopPos`, `ShopOwner`, `Npcs`(Id "union" 하나 필수 = 조합 연락관), `Signs`, `Areas`(이름 붙은 자리) |

- 높이는 `CoastMapDef.HeightAt(x, z)` 하나(해안 모양을 x로 섞고 곶·섬·바위기둥·모래톱을 max로 합친다). 걸을 수 있는 곳 = 구조물 위이거나 땅 높이 > 평균 수면 + 0.3 (썰물에 드러난 갯벌·모래톱은 물때 수면 기준). 물 = 높이 < 수면 - 0.12이고 구조물·바위기둥이 아닌 곳.
- 해안도로·가로등·건물은 지형을 따라 놓고, 언덕(곶)이나 비탈은 건너뛴다.
- 그 밖에: `FishingAreas`(국내는 `Korea`, 해외는 `Overseas` + `Hub`)에 `Open = true, MapId`를 넣고, 어종 `Maps`에 id를 더하고, 바다 미끼를 팔려면 `FishSpecies.SeaMaps`(민물이면 `FreshMaps`)에도 id를 넣고, `DevAutomation.CoastTour`에 장면을 추가, `sh tools/test.sh`(CoastMapTests가 물때마다 시작점·낚시점·연락관 자리, 어군 수, 바다/강 칸을 검사한다).
- 새 거점: `FishingAreas.Hubs`에 거점을 더하고 `UI/RegionMap.cs`의 `Regions`에 지도 범위(경위도)·해안선 다각형(`Sea`면 바다 다각형)·섬 타원·강 선을 넣는다.
- 해외 소품 모델: `tools/blender/build_props.py`(야자수·펠루카·정크선) + `props_sea.py`(대불·머라이언·독수리·롱테일 보트·열기구·파라솔·선베드·망루·사자상·난파선), `Resources/Props`. `KenneyProps.Spawn("Props/<id>", …)`(또는 `MapKit.Prop`)으로 놓으면 키·길이를 맞추고 콜라이더(야자수 줄기, 배는 상자)를 붙인다.
  - Blender FBX는 Z-up이라 루트에 축 변환 회전(-90° X)이 있다. `Spawn`은 그 회전 위에 yaw만 더하고 크기는 부모 기준 경계로 잰다 (예전엔 회전을 지워 정크선·펠루카가 세로로 서 있었다).
  - 내보낼 때 기준점을 원점(바닥 가운데)으로 굳힌다(`transform_apply(location=True)`). 안 그러면 합친 첫 물체의 원점이 기준이 되어 땅에 묻힌다.
  - 조각상처럼 한 덩어리여야 하는 것은 메타볼 → 메쉬 → 구름 노이즈 깎기 → 면별 이끼 재질(`props_sea._eroded`, 앙코르 사자상).
- **새 해외 맵 (2026-10-20 방식)**: `World/Maps/<이름>.cs` 하나에 `OverseasCoasts.<이름>()` 사양 + `Extra = <이름>Extra`(랜드마크, `MapKit`·`CoastWorldApi` 도우미, 맵 하나에 프리미티브 700개 안쪽·스크립트 없이) + `QuestDatabase.<이름>Chapter()`(id `<맵>_1..3`). `Reserved`(마을 집·나무를 놓지 않을 자리, 강 건너편 둑에도 적용), `StoneQuay`/`StoneTint`(돌 부두). 잔교·데크 위 NPC는 상판 높이에 저절로 선다.
- 빨리 보기: `sh tools/autoshot.sh --maps angkor,vinpearl`(둘러보기에서 그 맵 장면만, 약 35초), Unity 없이 컴파일만: `sh tools/compilecheck.sh`.

**모양이 아주 다른 맵**(강·호수 등)은 `FieldMapDef`를 직접 상속한다 (한강 `HanRiverMap`, 홍현 `HonghyeonMap`, 서천 `SeocheonMap`):
1. 격자(열·행·칸 크기), `HabitatAt`, `IsWater`/`IsWalkable`, 시작·상점 위치, 수면 색, (바다면) `IsSea`·`TideAmplitude`를 정한다.
2. 월드 빌더(지형은 `TerrainBuilder`에 높이·칠하기 함수만 넘기면 된다, 소품은 `CoastProps`)와 NPC를 만든다.
3. `Maps.All`에 등록하고 `FishingAreas`에서 낚시터 목록에 연다(레벨 조건, 레벨 업 알림은 자동).
4. 어종은 `FishSpecies.Maps`에 맵 id를 넣는다. 도감·일일 의뢰(그 맵 격자에 있는 서식지만)·업적(맵 도감 완성)은 자동.
5. 자동 스크린샷(`DevAutomation`)에 장면을 추가하고 문서 02(맵 절)·06을 갱신한다.

### 아트 교체
- 캐릭터: 모든 사람이 VRM이다. `CharacterFactory.Create(look, parent)` → `CharacterRoster`가 고른(또는 `look.Vrm`) 파일을 `VrmCharacters`가 읽어 복제한다. 바꾸려면 코드가 아니라 `characters.json`·`.vrm`을 바꾼다(가이드 07, 16 문서). 의상은 능력치만이고 몸에 입히지 않는다.
- 애니메이션: `CharacterAnimator`의 상수(Idle/Walk/Jog/Sprint/HoldRod/Cheer/Clap/Excited/Wave/Scared…)가 클립 이름이다. 클립은 `Resources/Animations/Motions`, `UAL1_Standard`에서 찾고(대기·걷기·뛰기·점프는 `StarterAssets`가 있으면 그것 — `StarterLocomotion` 표), `이름_M`/`이름_F`가 있으면 캐릭터 성별(`CharacterAnimator.Female`)로 고른다. FBX 안의 `Armature|이름` 테이크가 클립 이름이고(10 문서). 낚시 중 몸은 늘 HoldRod(UAL 대기)이고 두 팔은 낚싯대 각도를 따라가는 IK다(10 문서 5장, 손 자리 표 `Characters/RodArm` — NPC 낚시꾼도 같다). 이동 중 클립과 배속은 `UpdateAnimation()`이 속도로 고른다.
- UniVRM은 `Scripts/Vrm`(asmdef `Fishing.Vrm`)만 직접 참조한다.
- 물고기: `tools/blender/fish_species.py`에 어종(체형 템플릿 A~H·두족류 O 문어/Q 갑오징어/S 오징어, 색·무늬·지느러미)을 넣고 `build_fish.py --only <id>` → `Resources/Fish/Species/<id>.fbx`·텍스처·아이콘. `FishModels.CreateSpecies`가 길이(cm/100)로 키운다(09 문서).
- 물: `WaterSurface` + `Fishing/Water` 셰이더 (`_WaveAmp`, `_WaveLen`, 노멀 타일·속도). 하늘 반사는 `DayNightCycle`의 리얼타임 반사 프로브(하늘만, 2초마다).
  - **물가 거품**: 맵을 불러올 때 `FieldMapDef.SeabedAt(x, z)`(물 밑 땅 높이, 방파제는 땅 높이)를 `SeabedArea`에 1m 간격(최대 512칸) RFloat 텍스처로 굽는다(`WaterSurface.SetSeabed`). 셰이더는 `수심 = 수면 높이(파도 포함) − 해저`, 이웃 텍셀로 해저 경사를 구해 `물가까지 거리 ≈ 수심 / 경사`를 어림하고, 그 거리 4.5m 안에 노이즈로 끊긴 흰 띠가 밀려오게 그린다. 기둥 위에 뜬 잔교(한강 낚시 데크)는 `SeabedAt`에서 깊은 물로 돌려 둘레에 해변 같은 거품 띠가 생기지 않게 한다. 수심 2.5m보다 얕으면 바닥이 비치게 밝힌다. 깊이 텍스처를 쓰지 않아 그림자·AO와 부딪치지 않는다. 새 맵은 `SeabedAt`만 지형 높이 함수로 바꿔 주면 된다(기본값은 물이면 수심 3m).
- **z-fighting 막기**: 카메라 near는 평소 0.2m(1인칭 파이팅만 0.05m — 눈앞 낚싯대), 지형 `heightmapPixelError` 2, 땅 위에 까는 판(길·산책로·앞마당)은 5cm 넘게 띄우고, 같은 높이로 겹치는 토막(해안도로 이음매·가두리 통로 교차·수련잎)은 높이를 엇갈린다. 한강 데크는 둔치에 2m 걸치므로 윗면을 둔치보다 6cm 높이고(`WorldBuilder.DeckTop`) 둔치 쪽 끝에 비스듬한 문턱을 덮는다. 강가 석축은 데크 자리에서 끊는다(예전에는 데크 위로 20cm 턱이 가로질렀다).
- **월드 글자**: 간판은 `UIFactory.OneSided`로 단면(뒤에서 거울 글씨가 안 보이게), 길가 간판은 `WorldBuilder.AddSignBothSides`(앞뒤 두 장). 내 이름표는 NPC 곁에서 숨긴다(NPC 이름표와 겹친다). 모든 머리 위 이름표에는 `NameTagFade`(`CharacterFactory.AddNameTag`가 붙임)가 있어 화면과 나란하게 서고(카메라 위치를 보게 돌리면 화면 가장자리에서 비스듬히 기울어 커 보였다), 카메라 3~6.5m 안에서는 흐려지다 사라진다.
- **부딪히는 몸**: `KenneyProps.Spawn`이 건물·바위·배경 배는 상자, 나무는 줄기, 가로등은 기둥 콜라이더를 붙인다(`solid: false`면 안 붙임 — 내 배). NPC는 캡슐. 캐스팅 궤적 검사(`FishingController.CastBlocked`)와 배 이동(`BoatController.HullClear`)이 이 콜라이더를 본다. 자동 스크린샷은 낚시 장면마다 `CheckClear`로 앞이 막혔는지 로그(`[Dev] 캐스팅 막힘`)를 남긴다.
- 낚싯대·릴: `TelescopicRod.StyleFor`(낚싯대 색·길이·마디 수)와 `BuildReel`(릴 모양)에서 장비 id별로 고른다. 좌표 규약은 +Y가 낚싯대 방향, 원점이 릴 시트, +Z가 릴 쪽. 휨은 `Drive(목표각, 줄 방향, 떨림, dt)`, 펼치기는 `SetExtension(0~1)`, 릴은 `SpinReel(감기 바퀴/s, 줄 풀림 m/s, dt)`.

## 화면 퀄리티 도구 (2026-10-15)

| 파일 | 하는 일 |
|---|---|
| `Shaders/Weathered.shader` (`Fishing/Weathered`) | 풍화 표면: 월드 트라이플래너(`_Tile` m) + 윗면/옆면 텍스처 따로 + 만조선(전역 `_FishingWetY`) 아래 물 자국·물때 + 빗물 줄 + 이끼 + 팔레트 모드(Kenney) + 꼭짓점 색 AO(`_VertexAO`, `Mats.VertexAO`, HarborKit) + 바닥 때(`_Grime`: 윗면 금·얼룩·물웅덩이, `Mats.Quay`). 보간기 10개 한도라 메쉬 UV와 꼭짓점 색(밝기·r−b)을 `float4 meshAO` 한 칸에 묶는다 (Input 필드 이름이 `uv`로 시작하면 서피스 셰이더가 텍스처 좌표로 여긴다). 텍스처를 평균 색으로 나눠 결만 남긴다. `Mats.Weathered`·`Wall`·`Wood`·`Tile`·`Thatch`·`Stone`·`Carved`·`Quay`·`Tetrapod`, Blender 소품은 `Mats.Weatherize` ([09 8장](09-art-plan.md)) |
| `Shaders/Ground.shader` (`Fishing/Ground`) | `Mats.Textured`가 쓰는 바닥·바위·콘크리트·데크 셰이더. 같은 텍스처를 회전·0.73배로 한 번 더 읽어 노이즈로 섞고, 월드 좌표 큰 얼룩으로 밝기·채도·광택을 흔들어 반복 무늬를 없앤다. 월드 좌표라 정적 배칭해도 같다 (`WorldBatcher`가 Standard와 함께 묶는다) |
| `Shaders/Terrain.shader` + `Terrain-AddPass.shader` (`FishingTerrain.cginc`, `FishingNoise.cginc`) | Unity Terrain(홍현·서천·해안 맵 틀) 셰이더. 내장 지형과 같은 엔진 속성을 받아 층마다 같은 방법으로 반복을 깬다. 5번째 층부터는 추가 패스. `TerrainBuilder`가 `materialTemplate`로 쓰고 인스턴싱은 끈다 |
| `Shaders/FishFin.shader` (`Fishing/FishFin`) | 물고기 지느러미 반투명 막 (09 문서 3-16) |
| `Shaders/FishShadow.shader` (`Fishing/FishShadow`) | 물속 그림자 실루엣. 물이 불투명이라 수면 바로 위(+0.16m)에 그리고, 어군 소용돌이보다 먼저(`Transparent-10`) 그려 거품이 위에 얹힌다. 물결처럼 uv를 일렁이고 안개를 받는다 (`FishShadowFx`) |
| `World/Rocks.cs` | 절차적 바위 메쉬 8종 (`Rocks.Prim`이 `Mats.Prim(Sphere)` 자리) |
| `World/LighthouseBuilder.cs` | 방파제 등대·큰 유인 등대 회전체 메쉬 (`CoastProps.Lighthouse`·`GrandLighthouse`, 홍현) |
| `CoastProps.TetrapodMesh`·`TetrapodBank` | 테트라포드 메쉬와 방파제 바깥 3줄 더미 |
| `CoastMap.StackMesh` | 바위기둥·석회암 봉우리 (서브메쉬: 절벽 / 젖은 아랫단 / 위를 보는 턱) |
| `Characters/SitSeat.cs` | 앉은 동작일 때 골반 높이에 맞춘 아이스박스 의자 |
| `KenneyProps.SpawnBush` | `Resources/Plants` 덤불·작은 나무(ez-tree) + 잎 겉면 꽃·열매 (동백·귤·진달래·당산나무). 꽃 없는 나무는 `VegetationLibrary` 활엽수 |
| `World/VegetationLibrary.cs` + `Editor/VegetationBake.cs` | 직접 만든 침엽수·활엽수(ez-tree)·바위·풀(Poly Haven) 참조 묶음 (`Resources/VegetationLibrary.asset`, 빌드 때 채움). `KenneyProps.Spawn`(나무)·`Rocks.Prim`(바위, 눈 맵은 `Rocks.Snowy`)이 있으면 먼저 쓴다 |
| `World/GrassField.cs` + `Shaders/Grass.shader` | 풀·들꽃 포기: 지형 풀 층·풀 텍스처 바닥을 레이캐스트로 훑어 심고 48m 칸 메쉬로 굽는다, 바람 흔들림, 130m 밖 칸은 끔(`GrassCuller`). `MapLoader`가 월드를 지은 직후 부른다 |
| `Characters/RodGrip.cs` | 3인칭 오른손이 낚싯대를 쥔다: 릴 시트를 손바닥 가운데에, 손가락 마디를 낚싯대 축으로 감는다 (플레이어·NPC 낚시꾼) |
| `Characters/VrmCharacters.cs` · `CharacterRoster.cs` · `Vrm/VrmLoader.cs`(`Fishing.Vrm` asmdef) | 모든 사람이 VRM (게임 폴더 `StreamingAssets/Characters` + 문서/짭풍명월/Characters, 빌드 없이): 폴더 훑기·실행 중 읽기(UniVRM, 맵을 지을 땐 동기)·원본 선반에서 복제·메타(사용 허락)·눈 깜빡임, characters.json으로 누구에게 어떤 모델 (16 문서, 가이드 07) |
| `UIPanel.RowCursor` | 창 목록을 쌓으면 자동으로 잘라내기 + 휠 스크롤 + 스크롤바 (key로 스크롤 위치 기억) |

새 셰이더는 코드에서 `Shader.Find`로 찾으므로 `ProjectSettings/GraphicsSettings.asset` Always Included와 `ProjectTools.RuntimeShaders`에 넣었다 (meta의 guid를 고정해 두었다).
