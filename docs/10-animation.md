# 10. 캐릭터 애니메이션 (UAL + Mixamo 낚시·상황 동작 + Human Basic Motions)

캐릭터 동작은 AnimatorController 없이 `CharacterAnimator`(Playables 믹서)가 클립 이름으로 재생한다. 클립은 두 곳에서 찾는다.

| 라이브러리 | 위치 | 쓰는 동작 |
|---|---|---|
| Quaternius Universal Animation Library (CC0) | `Resources/Animations/UAL1_Standard.fbx` | 대기·걷기·뛰기·점프·말하기·앉기·운전·춤 |
| **Mixamo** (Adobe) | `Resources/Animations/Mixamo/*.fbx` | 낚싯대 들고 대기, 들어 올리기, 던지기, 챔질, 릴 감기, 환호, NPC 낚시 한 벌 |
| **Human Basic Motions FREE** (Kevin Iglesias, Asset Store) | `Resources/Animations/KevinIglesias/*.fbx` | 맨손 대기, 걷기, 뛰기, 질주, 말하기, 점프·낙하 (남·녀 따로, 7장) |

두 FBX 모두 휴머노이드로 임포트해서(`AssetImportRules`) 어떤 캐릭터(Angler_Male·Female, NPC)에도 그대로 붙는다.

## 1. Mixamo 파일

받은 원본은 `Assets/MixamoAnimation/`에 있다(X Bot, 30fps). **쓰는 것만** `Resources/Animations/Mixamo/`로 옮겨 이름을 바꿨다(`.meta` GUID 유지). 나머지는 빌드에 안 들어간다.

| 파일 (Resources/Animations/Mixamo) | 원본 | 길이 |
|---|---|---|
| `FishingIdle.fbx` | X Bot@Fishing Idle | 106프레임 (3.5초) |
| `FishingCast.fbx` | X Bot@Fishing Cast | 264프레임 (8.8초) |
| `Cheering.fbx` | X Bot@Cheering | 88프레임 (2.9초) |
| `Clapping.fbx` | X Bot@Clapping | 클립 `Clap_Loop` 0~35프레임 (근처에서 대물을 낚으면 NPC가 돌아보며 박수) |
| `Excited.fbx` | X Bot@Excited | 끝까지 (괴물·전설 어종을 낚았을 때, 8장) |
| `Rallying.fbx` | X Bot@Rallying | 끝까지 (조합 연락관 손짓, 8장) |
| `Scared.fbx` | X Bot@Scared | 끝까지, 재생은 앞부분만 (줄 끊김, 8장) |

`MixamoAnimation/`에 남은 것(지금 안 씀): Cheering (1)(주먹 불끈 — 고개를 숙여 환호 클로즈업에서 얼굴이 모자에 가려 뺐다), Sitting·Sitting Idle 여러 개. 대회 시상·앉은 NPC 등에 쓰고 싶으면 같은 방법으로 옮긴다(아래 4장).

## 2. Fishing Cast 쪼개기

`Fishing Cast` 하나 안에 낚시 동작이 순서대로 이어져 있다. 막대 인형 시트(아래 숫자 = Unity 프레임, 빨강 = 오른쪽, 파랑 = 왼쪽, 칸마다 앞·옆):

![Fishing Cast 시트](images/mixamo_fishing_cast.png)

| Unity 프레임 | 시간 | 내용 |
|---|---|---|
| 0 | 0.0s | 낚싯대를 허리 앞에 든 자세 (= Fishing Idle 첫 프레임, 마지막 프레임과도 같음) |
| 0 → 45 | 0.0~1.5s | 두 손을 어깨 너머 뒤로 들어 올린다 (45에서 잠깐 멈춤) |
| 45 → 70 | 1.5~2.3s | 앞으로 던진다 (54 근처가 가장 빠름) |
| 70 → 118 | 2.3~3.9s | 팔을 앞으로 뻗은 채 천천히 내린다 (찌 기다리기) |
| 118 → 130 | 3.9~4.3s | 잠깐 멈춤 |
| 130 → 250 | 4.3~8.3s | 릴을 감는다 (오른손이 약 0.4초 주기로 돌고, 두 손이 점점 몸 쪽으로) |
| 250 → 263 | 8.3~8.8s | 처음 자세로 돌아온다 |

이 표대로 `AssetImportRules.MixamoClips`에서 클립을 만든다.

| 클립 | 파일 | 프레임 | 반복 | 어디서 |
|---|---|---|---|---|
| `Fishing_Idle_Loop` | FishingIdle | 0~105 | ○ | 낚시 중 기본 자세 `HoldRod` (내 캐릭터 대기·NPC 낚시꾼) |
| `Fishing_Windup` | FishingCast | 0~45 | | (내 캐릭터는 안 씀, 2026-10-20) 시간대로 움직여 마우스로 젖히는 낚싯대와 따로 놀았다. 5장 팔 IK로 바꿈 |
| `Fishing_Cast` | FishingCast | 45~95 | | (내 캐릭터는 안 씀, 2026-10-20) 5장 팔 IK |
| `Fishing_Hook` | FishingCast | 0~30 | | (내 캐릭터는 안 씀, 2026-10-20) 5장 팔 IK |
| `Fishing_Reel_Loop` | FishingCast | 207~238 | ○ | (안 씀, 2026-10-19) 오른손이 원을 그려 낚싯대 쥔 손이 돌았다. 감는 동안은 `HoldRod` + 왼손 IK로 3인칭 릴 손잡이를 돌린다 (`PlayerController.UpdateCrank`) |
| `Fishing_Sequence` | FishingCast | 0~263 | | NPC 낚시꾼이 20~45초마다 한 벌 통째로 (끝 자세 = `HoldRod` 첫 자세라 이음새가 없다) |
| `Cheer_ArmsUp` | Cheering | 0~87 | | 랜딩 환호 `PlayCheer()` (두 팔 들고 흔들기, 얼굴 클로즈업) |
| `Clap_Loop` | Clapping | 0~35 | ○ | 근처에서 대물(대물 기준 이상 또는 ★4 이상)을 낚으면 22m 안 대화 NPC가 돌아보며 약 3초 박수 (`NpcAngler.Applaud`) |

릴 감기 루프 207~238(1.03초)은 130~250 안에서 "시작·끝 포즈 차 + 속도 차"가 가장 작은 구간을 골랐다(포즈 차 약 2cm). 임포트할 때 Loop Pose를 켜서 그 차이도 섞는다.

파이팅 중에는 화면이 1인칭(`FightRodView`)이라 3인칭 캐릭터가 숨겨진다. 그래서 파이팅에는 Mixamo 동작을 쓰지 않는다.

## 3. 임포트 설정과 함정

`AssetImportRules.PreprocessMixamo`가 매번 같은 설정을 넣는다(손으로 인스펙터를 고치면 다음 임포트에서 덮어써진다 → 표를 고친다).

- 휴머노이드, 아바타는 각 파일에서 만든다(`FishingCastAvatar` 등, 자동 매핑으로 유효).
- 루트 회전·높이·위치는 모두 포즈에 굽고(Bake Into Pose) 원본 기준.
- **`rotationOffset = 180`**: UAL 클립은 -Z를 보고(그래서 `CharacterFactory`가 모델을 180° 돌려 둔다) Mixamo는 +Z를 본다. 이걸 빼면 Mixamo 동작 동안 캐릭터가 뒤돌아 선다(캐스팅 화면에서 얼굴이 카메라를 보고, 환호 화면에서 등이 보임). "Body Orientation" 기준으로 바꿔도 해결되지 않는다.
- 포스트프로세서 코드만 바꾸면 Unity가 FBX를 다시 임포트하지 않는다. 표를 고친 뒤에는 에디터에서 해당 FBX를 Reimport하거나, `.meta`를 건드려 배치모드 빌드가 다시 임포트하게 한다.
- 클립 이름에 `Loop`가 있으면 반복(loopTime·loopPose).

`CharacterAnimator` 쪽:
- 반복 안 하는 클립을 `SetLoop`에 주면(예: `Windup`) 한 번 재생하고 끝 자세에서 멈춘다. 다른 상태로 갔다가 다시 들어오면 처음부터 재생한다.
- `PlayOnce`는 끝나면 마지막 `SetLoop` 상태로 0.25초 동안 섞어 돌아간다.

## 4. 구간 다시 찾기 / 새 Mixamo 동작 넣기

1. Mixamo에서 받은 FBX를 `Resources/Animations/Mixamo/`로 옮긴다(짧은 영어 이름, `.meta`도 같이).
2. 구간을 찾는다 (bpyenv는 05 문서 "Blender 파이프라인"):
   ```bash
   bpyenv/Scripts/python -I tools/blender/mixamo_probe.py Assets/Resources/Animations/Mixamo/FishingCast.fbx --out Screenshots/probe --step 8 --loop 134-247:30-75
   ```
   - `_sheet.png`: 프레임별 막대 인형(Unity 프레임 번호), `_curve.txt`: 프레임별 움직임 양·오른손 높이/앞뒤 → 움직임이 0에 가까운 곳이 동작 경계
   - `--loop A-B:MIN-MAX`: A~B 안에서 길이 MIN~MAX 프레임인 루프 후보
   - 블렌더는 FBX를 1프레임부터 읽는다. 툴은 Unity 기준(0부터)으로 바꿔 출력한다.
3. `AssetImportRules.MixamoClips`에 `(파일, 클립 이름, 첫 프레임, 끝 프레임)`을 더하고 `CharacterAnimator`에 상수를 만든다.
4. `sh tools/build.sh` → 메뉴 **Fishing > Inspect Imported Assets**(또는 `-executeMethod Fishing.EditorTools.AssetInspector.Run`)로 `Logs/asset-inspect.txt`에서 클립 길이·아바타 확인 → `sh tools/autoshot.sh`로 `02_casting`·`03_waiting`·`09b_cheer`·`22_honghyeon_day`(NPC) 확인.

## 5. 낚싯대와 손 (팔 IK, 2026-10-20)

낚싯대 각도(`PlayerController.RodPitch`·`RodYaw`, `BodyLean`)는 코드가 정하고, **내 캐릭터의 두 팔은 그 각도를 따라가는 2관절 IK**다. 예전에는 젖히기·던지기·챔질을 Mixamo 클립(`Windup`·`Cast`·`Hook`)이 시간대로 재생하고 낚싯대 각도는 마우스를 따라 따로 움직여서, 팔과 낚싯대가 따로 놀았다(사용자 지적: 어색하다).

- 몸은 낚시 중 늘 `HoldRod`(Fishing Idle) + 상체 젖힘/숙임(`BodyLean`) + 낚싯대를 좌우로 틀면 가슴이 반쯤 따라 돈다.
- **오른손** (`UpdateRodArmRight`): 오른 어깨 기준 손목 자리를 낚싯대 각도 표 `GripCurve`에서 고른다 — 2° 던진 직후 앞으로 뻗음, 35° 허리 앞 대기, 82° 챔질(가슴 앞에 세움), 120°, 155° 어깨 너머로 젖힘. 사이는 부드럽게 섞는다. 팔꿈치 방향(pole)은 들고 있을 땐 아래 바깥, 젖힐수록 앞 바깥.
- 낚싯대는 오른손 손바닥 앞(`RodGrip.Grip`)에 붙는다.
- **왼손** (`UpdateRodArmLeft`): 릴 시트에서 0.24m 뒤(손잡이 끝 쪽)를 감싸 쥔다. 릴을 감을 때(`UpdateCrank`)와 고기를 들 때(`UpdateHold`)는 그 위에서 손잡이·고기로 옮겨 간다.
- 걷기·점프·운전·넣어 둠·고기 들어 보이기 중에는 0.25초에 걸쳐 IK를 풀고 애니메이션 그대로.
- 확인: autoshot `02a_windup`, `02s_rod_arms_2`·`_35`·`_82`·`_155`(옆에서 각도별), `04b_hook_raise`(챔질 순간).

NPC 낚싯대는 -40° 고정이고 `Fishing_Sequence` 클립 그대로다.

## 6. 라이선스

Mixamo 동작은 Adobe 계정으로 받아 게임에 넣어 배포할 수 있지만, **원본 FBX를 그대로 재배포하는 것은 허용되지 않는다.** 2026-10-14에 에셋 스토어 패키지와 함께 `Assets/MixamoAnimation/`을 커밋했다(사용자 결정). 저장소는 **비공개**다(2026-10-15 확인). 에셋 스토어·Mixamo 약관이 막는 것은 판매가 아니라 원본 파일을 남이 받을 수 있게 올리는 것이라, 비공개로 두면 그대로 커밋해도 된다. **공개로 바꾸려면** 먼저 `Assets/Kevin Iglesias`·`AllSkyFree`·`Fantasy Skybox FREE`·`NatureStarterKit2`·`BigRookGames`·`MixamoAnimation`과 Resources로 옮긴 그 파일들(`Resources/Animations/Mixamo`·`KevinIglesias`, `Resources/Sky`, `Resources/NatureKit`)을 기록에서 빼야 한다.

## 7. 대기·걷기·뛰기 (Human Basic Motions FREE)

UAL의 `Idle_Loop`는 주먹 쥔 슈퍼히어로 자세라서, 에셋 스토어 무료 패키지 **Human Basic Motions FREE**(Kevin Iglesias, `Assets/Kevin Iglesias`)에서 4동작 × 남·녀 8개 FBX를 `Resources/Animations/KevinIglesias/`로 옮겨 이름을 바꿨다(`.meta` GUID 유지 → 패키지 데모 씬도 그대로 동작).

| 파일 | 원본 | 클립 (`AssetImportRules.KevinClips`) | 원래 이동 속도 |
|---|---|---|---|
| `Idle_M/F.fbx` | HumanM/F@Idle01 | `Basic_Idle_Loop_M/_F` | — |
| `Walk_M/F.fbx` | Walk01_Forward | `Basic_Walk_Loop_M/_F` | 2.0 m/s (0.8초에 두 걸음) |
| `Run_M/F.fbx` | Run01_Forward | `Basic_Run_Loop_M/_F` | 4.0 m/s (0.6초) |
| `Sprint_M/F.fbx` | Sprint01_Forward | `Basic_Sprint_Loop_M/_F` | 6.0 m/s (0.52초) |
| `Talk_M/F.fbx` | Talk01 | `Basic_Talk_Loop_M/_F` | — (NPC 대화 상대, 예전 UAL Idle_Talking) |
| `JumpBegin_M/F.fbx` | Jump01 - Begin | `Basic_JumpBegin_M/_F` | — (Space 점프 시작, 한 번) |
| `Fall_M/F.fbx` | Fall01 | `Basic_Fall_Loop_M/_F` | — (공중) |

- 원래 속도는 같은 패키지의 루트 모션(`[RM]`) 판을 Blender로 열어 엉덩이가 한 바퀴에 간 거리로 쟀다.
- **임포트**: 휴머노이드, 아바타는 패키지 그대로 **Copy From Other**(HumanM/F_Model) — 우리 규칙이 `CreateFromThisModel`로 덮지 않게 경로를 따로 처리한다. 루트는 포즈에 굽고, Mixamo처럼 +Z를 보므로 `rotationOffset = 180`.
- **성별**: `CharacterFactory`가 `CharacterAnimator.Female`을 정하면, 클립을 찾을 때 `이름_F`/`이름_M`을 먼저 찾고 없으면 `이름`을 쓴다. 첫 재생은 `Start`(성별이 정해진 뒤).
- **속도에 맞춘 클립·배속** (`PlayerController.UpdateAnimation`): 게임 걷기 키는 4.5 m/s, 달리기 7.5 m/s라서
  - 달리기 → `Sprint` × (속도 / 6) ≈ 1.25배
  - 2.6 m/s 넘게 걷기 → `Run`(뛰는 걸음) × (속도 / 4) ≈ 1.1배
  - 더 느릴 때(벽에 미끄러지며 등) → `Walk` × (속도 / 2)
  발이 땅에서 거의 미끄러지지 않는다. 발소리도 클립 한 걸음 길이(뛰기 1.2m, 질주 1.56m)마다 낸다.
- 확인: autoshot `00d_walk`(걷기 키), `00e_run`(달리기), `00f_idle`.

| 걷기 키 (Run 클립) | 달리기 (Sprint 클립) |
|---|---|
| ![걷기](images/00d_walk.png) | ![달리기](images/00e_run.png) |

## 8. 상황 동작: 제자리 돌기 · Excited · Rallying · Scared (2026-10-15)

받아 두고 안 쓰던 동작을 상황에 붙였다. 클립 목록은 `AssetImportRules`(Mixamo는 `(파일, 클립, 시작, 끝)`이고 끝 -1 = 마지막 프레임), 이름 상수는 `CharacterAnimator`.

| 동작 | 클립 | 언제 | 코드 |
|---|---|---|---|
| 제자리 돌기 | `Basic_TurnLeft_Loop` / `Basic_TurnRight_Loop` (Human Basic Motions Turn01, 남·녀) | NPC가 플레이어 쪽으로 25° 넘게 몸을 돌릴 때 발을 옮기며 돈다. 8° 안으로 들어오면 멈춤(사이는 이전 상태 유지 — 떨림 방지). 앉은 NPC는 그대로 | `NpcAngler` `_turning` |
| Excited | `Excited` (Mixamo) | 괴물 어종(`Boss`)이나 희귀도 5 어종을 낚았을 때, 보통 환호 대신 | `FishingController` → `PlayerController.PlayExcited` (×1.2배속) |
| Rallying | `Rallying` (Mixamo) | 보고할 퀘스트가 있고 플레이어가 15m 안에 오면 조합 연락관(`union`)이 9~14초마다 손짓 | `NpcAngler` `_nextRally` |
| Scared | `Scared` (Mixamo) | 줄이 끊어졌을 때 움찔 (낚싯대 자세를 돌린 뒤 한 번) | `FishingController` → `PlayerController.PlayScared` (×1.3배속) |

- 한 번짜리 동작은 `PlayOnce(클립, 배속, 섞기 시간)`으로 틀고, 끝나면 원래 루프로 돌아간다.
- 몸 방향은 코드가 돌리고 Turn 클립은 발만 맞춘다(루트 모션 없음).
- **아직 화면 확인 전**: 자동 촬영에 이 네 동작이 나오는 장면을 넣어 어색하지 않은지 볼 것 ([WORK-IN-PROGRESS.md](WORK-IN-PROGRESS.md)).

## 9. 배경 사람 걷기 (2026-10-16)

사용자 지적: 한강 둔치를 걷는 의사가 이상하다. 자동 촬영 `00o_doctor_walk`(0.25초 간격 두 장)로 보니 셋이 겹쳤다.

| 문제 | 원인 | 고침 |
|---|---|---|
| 허리를 숙이고 팔을 휘저으며 살금살금 걷는다 | Mixamo `Walking`(`Walk2_Loop`)이 그런 걸음이었다 | 의사·순찰(군인·특공대)은 Human Basic Motions 기본 걷기(`Basic_Walk_Loop`)로 |
| 발이 미끄러진다 | 클립은 그대로 1배속인데 이동은 1.2 m/s | `Wanderer.Spec.WalkAnimSpeed` 기본 0 = 기본 걷기면 `이동 속도 / CharacterAnimator.BasicWalkSpeed(2 m/s)`로 자동. 의사 1.5 m/s(×0.75), 순찰 1.4 m/s(×0.7) |
| 끝점에서 돌아설 때 뒷걸음질·옆걸음질 | 몸이 돌기 전에 새 방향으로 움직였다 | 앞과 갈 방향의 내적이 0.7 아래면 그 자리에서 빠르게(5) 돌고, 0.7~0.95 사이에서 서서히 걷기 시작 |

- 의사 길은 풀밭(z −69)에서 산책로(z −66.6)로 옮겼다.
- 좀비·취객·헤엄은 자기 클립 배속을 그대로 쓴다(`WalkAnimSpeed`를 직접 준다).

