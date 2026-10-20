# 05. 개발 가이드

## 환경

- **Unity 6000.3.17f1** (Unity Hub 기본 경로 `C:\Program Files\Unity\Hub\Editor\6000.3.17f1`)
- Windows Build Support 모듈
- 패키지: `com.unity.ugui 2.0.0`, `com.unity.burst`, `com.unity.collections`(Translucent Image가 씀), `com.unity.postprocessing 3.4.0`(후처리), `com.unity.inputsystem 1.14.2`(패드 진동만), `com.unity.test-framework 1.5.1` + 기본 모듈
- 외부 에셋
  - 동작 클립(`Assets/Resources/Animations`: Quaternius UAL CC0, Mixamo, Kevin Iglesias). 임포트 설정은 `Assets/Editor/AssetImportRules.cs`가 자동으로 맞춘다.
  - 사람은 모두 VRM: `Assets/StreamingAssets/Characters`(기본 11개, Git LFS — `git lfs pull`로 받는다) + `characters.json`. 바꾸는 법은 [가이드 07](guide/07-characters.md).
  - **Mixamo** 낚시 동작 (`Assets/MixamoAnimation` 원본, 쓰는 3개만 `Resources/Animations/Mixamo`): Fishing Cast를 들기·던지기·챔질·릴 감기 클립으로 쪼갠다 → [10 문서](10-animation.md).
  - 에셋 스토어 패키지(2026-10-14 커밋, 아직 게임에 연결 안 함): `Kevin Iglesias`(Human Basic Motions FREE), `AllSkyFree`, `Fantasy Skybox FREE`, `NatureStarterKit2`, `BigRookGames`.
  - Kenney CC0 모델(`Resources/Kenney`)·소리(`Resources/Audio/Kenney`), Poly Haven CC0 텍스처(`Resources/Terrain`). 출처는 각 폴더의 SOURCES.md·라이선스 사본.
  - 캐릭터 옷·물고기·아이콘은 직접 만든 것: `tools/blender`(아래 "Blender 파이프라인").
  - **Translucent Image 7.0.3 + Paraform 1.5.0** (`Assets/Le Tai's Asset`): 유리 UI. 스크립팅 심볼 `LETAI_PARAFORM`이 있어야 둥근 도형이 나온다(ProjectSettings에 등록됨, 에디터가 열릴 때도 자동 추가).
  - **EasyChart** 무료판 (`Assets/EasyChart`): 선·막대·파이(도넛)·레이더만. 가로 막대·게이지 등은 Pro 전용이라 쓰면 에러 로그가 난다.
  - **TextMesh Pro** + Jalnan2 글꼴: `Assets/Editor/FontSetup.cs`가 `Resources/Fonts/Jalnan2 SDF`를 만든다(없을 때만). 다시 만들려면 메뉴 **Fishing > Rebuild Jalnan2 TMP Font**.
- 렌더 파이프라인: Built-in, 입력: 구 Input Manager + 새 Input System(진동만, `activeInputHandler: 2`)

## 처음 열기

1. Unity Hub → Add → `Project_Fishing` 폴더를 추가한다 (버전 6000.3.17f1).
2. `Assets/Scenes/HanRiver.unity`를 열고 Play를 누른다.
   - 다른 씬에서 Play해도 `GameBootstrap`이 자동으로 생성된다.
3. 씬이나 빌드 설정이 깨졌다면 메뉴 **Fishing > Setup Project**를 실행한다.

## 메뉴 (Fishing)

| 메뉴 | 하는 일 |
|---|---|
| Setup Project | `HanRiver.unity` 생성, 빌드 씬 등록, Always Included Shaders 등록, 플레이어 설정 |
| Build Windows | `Builds/Windows/ProjectFishing.exe` |
| Balance Simulation | `docs/balance-report.md` 생성 |
| Delete Save | 세이브 파일 삭제 |
| Inspect Imported Assets | 캐릭터·애니메이션·물고기 임포트 결과(뼈, 클립, 크기) → `Logs/asset-inspect.txt` |
| Check Fish Models | 어종별 물고기 모델 크기·방향·뼈·클립 → `Logs/fish-check.txt` |
| Reimport Fish Assets | Blender로 다시 만든 물고기 FBX·텍스처를 강제 재임포트 (텍스처 크기 규칙 적용) |
| Rebuild Jalnan2 TMP Font | TMP 글꼴 에셋 다시 만들기 |

## 명령줄 도구 (`tools/`, Git Bash)

```bash
sh tools/build.sh                 # 윈도우 빌드
sh tools/autoshot.sh              # 빌드 실행 → Screenshots/ 에 약 150장 자동 촬영 후 종료, 맵별 평균 FPS 출력
sh tools/autoshot.sh 06_fight     # 그 장면까지만 찍고 끝낸다 (한 장면 빨리 확인, -autoshotStop)
sh tools/inspect.sh "seocheon:Pavilion:200:12:1.3;sentosa:merlion:210:5:1.0"   # 맵:물체이름:방위각:고도각:거리배수[:시각] — HUD 없이 물체 가까이 → Screenshots/i_*.png
sh tools/inspect.sh "hangang:NPC_새벽조사@reel:120:15:1.1"   # NPC는 몸만으로 거리를 잡는다, 이름 뒤 @reel = 바로 릴 감기 (10 문서 10장)
bpyenv/Scripts/python -I tools/blender/build_props.py --only merlion --preview3 Screenshots/props   # 소품 앞·3/4·옆 미리보기
sh tools/balance.sh               # 밸런스 리포트 (전체)
sh tools/balance.sh -simSummary   # 밸런스 리포트 (대물급만)
sh tools/test.sh                  # 게임 규칙 단위 테스트 (EditMode, Logs/test-results.xml)
```

Unity 없이 (.NET 8 SDK만 있으면, 클라우드 세션에서도):

```bash
cd tools/fightsim && dotnet run -- report    # docs/balance-report.md 다시 쓰기
dotnet run -- table nileperch 158 0.45 1 0.8 # 어종·길이·체력 상태를 골라 콘솔에 (04 문서)
dotnet run -- bites hangang                  # 미끼마다 잡동사니 확률 (BiteSelector를 그대로 컴파일)
```

## 단위 테스트 (`Assets/Editor/Tests`)

`com.unity.test-framework`의 EditMode 테스트. 게임 코드는 asmdef 없이 Assembly-CSharp에 있어서 테스트도 Editor 폴더(Assembly-CSharp-Editor)에 둔다.

| 묶음 | 확인하는 것 |
|---|---|
| `FishFightTests` | 작은 붕어는 입문 장비로 잡힌다, 체력 20% 이하(지침)여도 대물 장비면 나일농어 대물을 올린다(랜딩 피로), 대물일수록 오래 걸린다, 그루퍼를 입문 장비로 무작정 감으면 줄이 끊어진다, 센 낚싯대가 HP를 더 깎는다 |
| `BiteSelectorTests` | 그 맵에 사는 고기만 문다, **잡동사니는 13% 미만**(한강·산토리니·나일강·청진, 아침·밤), 그루퍼는 어군 안에서만, 맞는 미끼가 유리하다, 길이는 최소~최대 안 |
| `TournamentRulesTests` | 세 미션 점수·동점, AI 시뮬레이션은 날짜마다 같다, 실력 차이, 시간대·엠블럼·대회 맵 |
| `RodUpgradeTests` | 강화 성능(+10 = 힘 1.3배), 확률은 내려가고 비용은 오른다, +7부터 하락 |
| `PlayerProfileTests` | 레벨업, 아이템, 판매가, 도감 최고 기록 |
| `SettingsTests` | 키를 겹치게 바꾸면 맞바꾼다, 바꿀 수 없는 키(Esc·숫자 1~8·마우스) |
| `SeasonTests` | 달력·계절, 빙어는 겨울 얼음 구멍에서만, 제철 배율, 다금바리·돗돔은 먼바다에서만 |
| `CoastMapTests` | 해안 맵마다 물때(간조·평균·만조) 어느 때나 시작점·낚시점·조합 연락관 자리를 걷는다, 사양이 다 찼다(8×8 격자·연락관·어군·바다 칸), **모든 어종이 사는 낚시터에서 낚인다**(격자에 서식지·어군 전용은 어군, 그곳에서 파는 미끼), 가두리 통로/그물 칸, 목섬 모래톱은 간조에만 |

에디터에서는 Window > General > Test Runner > EditMode. 지금 35개 (2026-10-19 `sh tools/test.sh` 35/35 통과, 퀘스트 `QuestTests` 3개 추가).

어종 표: `python tools/fish_table.py --write`가 `FishDatabase.cs`를 읽어 02 문서 11장 표와 맵별 어종 수를 다시 쓴다.

## Blender 파이프라인 (`tools/blender`, Blender 설치 불필요)

PyPI의 `bpy`(headless Blender)로 돌린다. 한 번만:
```bash
py -3.13 -m venv bpyenv                    # bpy 5.x는 Python 3.13
bpyenv/Scripts/pip install bpy pillow numpy
```
| 명령 | 결과 |
|---|---|
| `... build_fish.py [--only carp,dotdom]` | `Resources/Fish/Species/*.fbx`, `Fish/Textures`, `Icons/Fish` (09 문서) |
| `... build_fish.py --preview --only carp --out Screenshots/fish` | 물고기 미리보기 |
| `... build_icons.py` | `Resources/Icons/Items`, `Icons/Junk`, `Resources/Junk` |
| `... mixamo_probe.py <fbx> --out <폴더> [--loop A-B:MIN-MAX]` | Mixamo FBX 막대 인형 시트·움직임 곡선·루프 후보 (구간 나누기, 10 문서) |
| `bpyenv/Scripts/python -I tools/intro/build_intro_art.py [--preview <폴더>]` | `Resources/Intro/*.png` 인트로 그림(타이틀 배경 겹·붓글씨·낙관·사업자 로고) + 미리보기 두 장. bpy 없이 numpy·PIL만 쓴다 (03 문서 "인트로") |

- 인트로 글자는 PIL `stroke_width`로 굵게 하지 않는다: 윤곽이 겹친 글리프(송명 '쓰')에서 획 속이 지워진다(ㅡ가 사라짐). 그냥 그린 뒤 `ImageFilter.MaxFilter`로 넓힌다.
- 스크립트 끝은 `sys.stdout.flush(); os._exit(0)` (bpy 종료 시 충돌 회피). `bpyenv/`는 .gitignore.
- 물고기를 전부 다시 만들면 바뀌지 않은 어종도 FBX·PNG가 바이트 단위로 달라진다 → 새로 만든 것만 커밋하고 나머지는 `git checkout --`.
- 만든 뒤 Unity에서 `Fishing > Reimport Fish Assets`(또는 배치모드 빌드가 자동 임포트).

Unity가 다른 경로에 있으면 `UNITY="/d/Unity/6000.3.17f1/Editor/Unity.exe" sh tools/build.sh`처럼 덮어쓴다.
에디터에 같은 프로젝트가 열려 있으면 배치모드가 실패하니 먼저 닫는다.

## 자동 스크린샷 (`-autoshot`)

`ProjectFishing.exe -autoshot <폴더>`로 실행하면 `DevAutomation`이 아래 장면을 연출하고 찍은 뒤 종료한다. **세이브 파일은 읽지도 쓰지도 않는다.**

| 파일 | 장면 |
|---|---|
| 00a_lobby, 00b_area_list | 대기실(한국 낚시 조합) → 낚시터 목록 → 한강 CH.1 입장 |
| 01_field_morning, 00c_rod_extend, 00_character | 시작 위치(새벽), 낚싯대 펼치기, 한낮 캐릭터 정면(기본 캐릭터 VRM) |
| 01b_fish_school | 데크 앞 수면의 어군 소용돌이 |
| 02_casting, 02b_cast_flight, 03_waiting, 04a_bite_alert, 04_bite_gauge | 밤 데크 캐스팅, 날아가는 찌, 찌 대기(원작 구도), 본신 머리 위 `!`, 어깨 뒤 챔질 클로즈업 |
| 05_hook_perfect | PERFECT 챔질 → HIT → PERFECT!!! |
| 06_fight, 06b_tension_danger, 06c_speedlines | 82cm 잉어 파이팅(1인칭 팔·장갑), 장력 위험 테두리, 카운터 집중선 |
| 06c_fight_rod_left, 06d_fight_rod_right | 낚싯대를 화면 왼쪽·오른쪽 끝으로 튼 모습 (초릿대가 물고기 쪽으로 옆으로 휨) |
| 07_counter_chance, 08_counter, 09_landing, 09c_lift, 09d_catch, 10_result | 카운터 찬스·성공(먹 글씨 COUNTER), 랜딩 찬스(릴 줌), 1인칭 들어 올리기, 3인칭 옆모습 줄 끝 고기, RESULT(82cm 잉어: 튀어 오르는 고기) |
| 09e_catch_small, 10b_result_held | 30cm 붕어: 왼손으로 잡기, RESULT에서 고기를 들어 보인다 |
| 10c_result_eel, 10d_result_shoe, 10e_result_trap | 긴 고기(뱀장어, S자 꿈틀)·잡동사니(운동화·통발, 비스듬히 돈다) RESULT — 결과 창을 바로 연다 |
| 00p_pier_entrance | 데크와 둔치·석축 경계 |
| 02v_vrm_35·82·155, 02v_vrm_front, 21e_character_model | 목록 첫 VRM으로 갈아입은 낚싯대 자세·정면, 캐릭터 창 [캐릭터 모델] 탭 (16 문서) |
| 11_dex, 11b_dex_detail, 12_keepnet | 도감(아이콘), 도감 상세(3D 물고기), 살림망 |
| 13_quest, 13b_daily, 13c_achievements, 13d_exchange, 14_shop | 퀘스트, 일일 의뢰, 업적, 소라고동 교환소, 상점 |
| 16_help, 16b_options | 도움말, 옵션(키 설정) |
| 05c_lure_action | 루어 감기·저킹 액션 표시 |
| 30_tournament_hud, 31_tournament_rank | 대회 진행 HUD 순위표, 대회 탭 |
| 20_npc_dialog, 21_rod_upgrade, 21b_inventory, 21c_costume | NPC 대화, 강화, 가방, 내 캐릭터 아바타 슬롯(의상 능력치) |
| 17_area_map | 지역 지도(M), 작은 어군 |
| 18_boat_drive, 19_boat_stop | 노을 고무보트 운전, 멈춘 배 뱃머리 |
| 15b_cloudy_day, 15_night_rain | 흐린 낮(층구름), 비 오는 밤 |
| 28_aquarium, 29_lobby_aquarium | 수족관 창, 대기실 수조 |
| 22_honghyeon_day ~ 27_sea_result | 홍현 앞바다: 낮, 다랭이 마을, 갯바위 섬, 노을, 지도, 배, 바다 파이팅, 결과 |
| 34_seocheon_harbor ~ 40_seocheon_cruiser | 서천 앞바다: 홍원항, 썰물 갯벌, 동백정, 노을, 파이팅, 결과, 돛단배, 낚시 크루저 |
| 41_winter_ice, 42_winter_smelt | 겨울 한강 얼음판·얼음 구멍, 빙어 |

로그(`Logs/player.log`)에 파이팅 결과가 남는다. 예: `[Dev] 파이팅 종료: 어종=잉어 82.0cm, 상태=Result, 경과=21.7s, HP=54/382, 결과=Landed`

UI나 연출을 바꾼 뒤 `build.sh` → `autoshot.sh`로 실제 화면을 확인하는 흐름을 권장한다.

## 세이브

- 위치: `%USERPROFILE%\AppData\LocalLow\didrnrgus\Project Fishing - 청풍명월 솔로\save.json`
- 형식: `SaveData`를 `JsonUtility`로 저장한 JSON. 필드를 추가할 때는 기본값을 두면 이전 세이브와 호환된다.
- 에디터에서 Play해도 같은 파일을 쓴다. 초기화하려면 `Fishing > Delete Save`.

## 에디터를 닫지 않고 컴파일만 확인하기

에디터가 열려 있으면 배치모드 빌드가 막힌다. 그럴 때는 Unity가 만든 `Library/Bee/artifacts/*/Assembly-CSharp.rsp`를 Unity에 들어 있는 Roslyn(`Editor/Data/DotNetSdkRoslyn/csc.dll`, `Editor/Data/NetCoreRuntime/dotnet.exe`)으로 돌려 오류만 볼 수 있다 (rsp의 소스 목록과 `-out`만 바꿔서).

## 디버깅 팁

- 파이팅만 바로 시험하려면 `Game.Fishing.DebugStartFight("carp", 90f, 20f)`를 호출한다 (예: 임시 키 바인딩).
- 찌를 던진 뒤 `Game.Fishing.DebugForceBite("carp", 82f)`면 바로 본신이 온다.
- 시간을 바꾸려면 `Game.Clock.Set(day, minutes)`, 날씨는 `Game.Weather.Force(Weather.Rain)`.
- 시간을 빨리 돌리려면 `Game.Clock.Speed`.
- `FishingController.Autoplay = true`면 파이팅을 봇이 대신한다.

## Git

- `.gitignore`는 Unity 표준에 `Builds/`, `Screenshots/`, `Logs/`, `UserSettings/`, `bpyenv/`를 더 뺀다.
- 문서용 스크린샷은 `docs/images/`에 복사해서 커밋한다.
- 원격: `https://github.com/didrnrgus/Project_Fishing.git`
- 바이너리(이미지·모델·소리·글꼴)는 Git LFS(`.gitattributes`). 처음 받을 때 `git lfs install` 후 clone/pull.
- 에셋 스토어·Mixamo 원본: 저장소는 **비공개**다(2026-10-15 확인). 에셋 스토어·Mixamo 약관이 막는 것은 판매가 아니라 원본 파일을 남이 받을 수 있게 올리는 것이라, 비공개로 두면 그대로 커밋해도 된다. **공개로 바꾸려면** 먼저 `Assets/Kevin Iglesias`·`AllSkyFree`·`Fantasy Skybox FREE`·`NatureStarterKit2`·`BigRookGames`·`MixamoAnimation`과 Resources로 옮긴 그 파일들(`Resources/Animations/Mixamo`·`KevinIglesias`, `Resources/Sky`, `Resources/NatureKit`)을 기록에서 빼야 한다.

## 코딩 컨벤션

- 네임스페이스 `Fishing.*` (`Core`, `Data`, `World`, `PlayerSystems`, `Angling`, `UI`, `Audio`, `Dev`, `EditorTools`)
  - 낚시 로직 네임스페이스가 `Fishing.Fishing`이 되지 않도록 `Angling`으로 했다.
- 사용자에게 보이는 문자열과 주석은 한국어로 쓴다.
- 게임 규칙은 순수 C# 클래스에 둔다(시뮬레이션 가능하게). MonoBehaviour는 입력·표현만 맡는다.
