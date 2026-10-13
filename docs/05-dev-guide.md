# 05. 개발 가이드

## 환경

- **Unity 6000.3.17f1** (Unity Hub 기본 경로 `C:\Program Files\Unity\Hub\Editor\6000.3.17f1`)
- Windows Build Support 모듈
- 패키지: `com.unity.ugui 2.0.0` + 기본 모듈
- 외부 에셋: Quaternius CC0 모델만 (`Assets/Resources/Characters`, `Animations`, `Fish`). 임포트 설정은 `Assets/Editor/AssetImportRules.cs`가 자동으로 맞춘다.
- 렌더 파이프라인: Built-in, 입력: 구 Input Manager

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

## 명령줄 도구 (`tools/`, Git Bash)

```bash
sh tools/build.sh                 # 윈도우 빌드
sh tools/autoshot.sh              # 빌드 실행 → Screenshots/ 에 20장 자동 촬영 후 종료
sh tools/balance.sh               # 밸런스 리포트 (전체)
sh tools/balance.sh -simSummary   # 밸런스 리포트 (대물급만)
```

Unity가 다른 경로에 있으면 `UNITY="/d/Unity/6000.3.17f1/Editor/Unity.exe" sh tools/build.sh`처럼 덮어쓴다.
에디터에 같은 프로젝트가 열려 있으면 배치모드가 실패하니 먼저 닫는다.

## 자동 스크린샷 (`-autoshot`)

`ProjectFishing.exe -autoshot <폴더>`로 실행하면 `DevAutomation`이 아래 장면을 연출하고 찍은 뒤 종료한다. **세이브 파일은 읽지도 쓰지도 않는다.**

| 파일 | 장면 |
|---|---|
| 00a_lobby | 대기실(한국 낚시 조합), 오전 10시 |
| 00b_area_list | 낚시터 목록 (한국 지도, 채널) → 한강 CH.1 입장 |
| 01_field_morning | 시작 위치 (새벽) |
| 00c_rod_extend | 접힌 낚싯대를 끝 마디부터 펼치는 중간 |
| 00_character | 한낮 캐릭터 정면 클로즈업 (옷, 모자, 이름표, 뽑기대·릴) |
| 02_casting | 밤의 낚시 데크, 당겼다 미는 캐스팅 (비거리 숫자, 궤적) |
| 03_waiting | 찌를 던지고 기다림 (찌 클로즈업 게이지) |
| 04_bite_gauge | 본신: 찌가 잠기고 빨간 `!`, 두근두근 |
| 05_hook_perfect | 자동 조작으로 PERFECT 챔질 → 파이팅 시작 |
| 06_fight | 82cm 잉어 파이팅 |
| 06c_speedlines | 카운터 찬스 집중선을 강제로 띄워 모서리까지 덮이는지 확인 |
| 06b_tension_danger | 장력을 0.96으로 강제 → 빨간 화면 테두리 |
| 07_counter_chance | 카운터 찬스 (속도선, 파란 화살표) |
| 08_counter | 카운터 성공 |
| 09_landing | 랜딩 찬스 다이얼 |
| 10_result | RESULT 화면 |
| 11_dex ~ 14_shop | 도감, 살림망, 퀘스트, 상점 (샘플 데이터) |
| 16_help | 도움말 |
| 20_npc_dialog | 낚시점 김사장과 대화 (밤, 처마 조명) |
| 21_rod_upgrade | 낚싯대 강화 창 (+4 시도 후) |
| 18_boat_drive | 노을 무렵 데크 끝에서 고무보트를 띄워 달리기 (물보라 꼬리, 계기판) |
| 19_boat_stop | 멈춘 배의 뱃머리에 서서 낚시 준비 |
| 17_area_map | 지역 지도(M). 어군 하나를 3마리 낚은 것으로 처리해 작은 어군으로 보여 줌 |
| 15_night_rain | 비 오는 밤, 동작대교 아래 |

로그(`Logs/player.log`)에 파이팅 결과가 남는다. 예: `[Dev] 파이팅 종료: 어종=잉어 82.0cm, 상태=Result, 경과=21.7s, HP=54/382, 결과=Landed`

UI나 연출을 바꾼 뒤 `build.sh` → `autoshot.sh`로 실제 화면을 확인하는 흐름을 권장한다.

## 세이브

- 위치: `%USERPROFILE%\AppData\LocalLow\didrnrgus\Project Fishing - 청풍명월 솔로\save.json`
- 형식: `SaveData`를 `JsonUtility`로 저장한 JSON. 필드를 추가할 때는 기본값을 두면 이전 세이브와 호환된다.
- 에디터에서 Play해도 같은 파일을 쓴다. 초기화하려면 `Fishing > Delete Save`.

## 디버깅 팁

- 파이팅만 바로 시험하려면 `Game.Fishing.DebugStartFight("carp", 90f, 20f)`를 호출한다 (예: 임시 키 바인딩).
- 찌를 던진 뒤 `Game.Fishing.DebugForceBite("carp", 82f)`면 바로 본신이 온다.
- 시간을 바꾸려면 `Game.Clock.Set(day, minutes)`, 날씨는 `Game.Weather.Force(Weather.Rain)`.
- 시간을 빨리 돌리려면 `Game.Clock.Speed`.
- `FishingController.Autoplay = true`면 파이팅을 봇이 대신한다.

## Git

- `.gitignore`는 Unity 표준에 `Builds/`, `Screenshots/`, `Logs/`, `UserSettings/`를 더 뺀다.
- 문서용 스크린샷은 `docs/images/`에 복사해서 커밋한다.
- 원격: `https://github.com/didrnrgus/Project_Fishing.git` (비공개 저장소)

## 코딩 컨벤션

- 네임스페이스 `Fishing.*` (`Core`, `Data`, `World`, `PlayerSystems`, `Angling`, `UI`, `Audio`, `Dev`, `EditorTools`)
  - 낚시 로직 네임스페이스가 `Fishing.Fishing`이 되지 않도록 `Angling`으로 했다.
- 사용자에게 보이는 문자열과 주석은 한국어로 쓴다.
- 게임 규칙은 순수 C# 클래스에 둔다(시뮬레이션 가능하게). MonoBehaviour는 입력·표현만 맡는다.
