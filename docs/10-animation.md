# 10. 캐릭터 애니메이션 (UAL + 직접 만든 반응 동작 + Human Basic Motions)

캐릭터 동작은 AnimatorController 없이 `CharacterAnimator`(Playables 믹서)가 클립 이름으로 재생한다. 클립은 세 곳에서 찾는다.

| 라이브러리 | 위치 | 쓰는 동작 |
|---|---|---|
| Quaternius Universal Animation Library (CC0) | `Resources/Animations/UAL1_Standard.fbx` | 낚싯대 들고 선 몸(`Idle_Loop`), 점프, 말하기, 앉기, 운전, 춤 |
| **반응 동작** (직접 만듦, `tools/blender/build_reactions.py`) | `Resources/Animations/Reactions.fbx` | 환호, 박수, 펄쩍 뛰기, 손짓, 움찔 |
| **Human Basic Motions FREE** (Kevin Iglesias, Asset Store) | `Resources/Animations/KevinIglesias/*.fbx` | 맨손 대기, 걷기, 뛰기, 질주, 말하기, 점프·낙하 (남·녀 따로, 7장) |

모두 휴머노이드로 임포트해서(`AssetImportRules`) 어떤 VRM 캐릭터(내 캐릭터·NPC, 16 문서)에도 그대로 붙는다.
낚싯대를 든 두 팔은 클립이 아니라 낚싯대 각도를 따라가는 IK다(5장). 그래서 낚시 전용 동작 클립(들기·던지기·챔질·릴 감기)은 없다.

## 1. 반응 동작 (2026-10-21, Mixamo 대신)

저장소를 공개하려고 Mixamo 동작을 모두 걷어냈다(6장). 쓰던 Mixamo 동작은 7개뿐이었고, 그중 낚시 동작(`Fishing_Idle_Loop`·`Fishing_Sequence` 등)은 팔 IK로 대신하고, 나머지 5개는 UAL1 뼈대 위에 직접 만들었다.

![반응 동작 앞모습 (위부터 환호·박수·펄쩍·손짓·움찔)](images/reactions_sheet.png)

| 클립 | 길이 | 반복 | 언제 | 코드 |
|---|---|---|---|---|
| `Cheer_ArmsUp` | 78프레임 (2.6초) | | 랜딩 환호: 두 팔을 V자로 들고 좌우로 흔들며 무릎이 들썩 | `PlayerController.PlayCheer` (×1.1) |
| `Clap_Loop` | 75프레임 (2.5초, 박수 5번) | ○ | 근처에서 대물(대물 기준 이상 또는 ★4 이상)을 낚으면 22m 안 대화 NPC가 돌아보며 약 3초 | `NpcAngler.Applaud` (`cutAt` 1.2) |
| `Excited` | 76프레임 (2.5초) | | 괴물(`Boss`)·희귀도 5 어종: 웅크렸다 펄쩍 뛰며 두 주먹을 치켜들고, 내려와 주먹을 두 번 당긴다 | `PlayerController.PlayExcited` (×1.1) |
| `Wave_Call` | 72프레임 (2.4초) | | 보고할 퀘스트가 있으면 조합 연락관이 오른손을 머리 위로 들어 세 번 흔든다 | `NpcAngler` `_nextRally` |
| `Scared` | 40프레임 (1.3초) | | 줄이 끊겼을 때: 두 손을 가슴 앞으로 홱 올리며 뒤로 젖히고 무릎을 굽혔다 풀린다 | `PlayerController.PlayScared` |

## 2. 만드는 방법 (`build_reactions.py`)

```bash
bpyenv/Scripts/python -I tools/blender/build_reactions.py --preview Screenshots/reactions   # 10초쯤
```

- 바탕은 UAL1 `Idle_Loop`(숨쉬기·무게 이동, 75프레임). 프레임마다 그 자세를 놓고 위에 얹는다:
  - 손·발 자리를 시간 함수로 정하고 **2관절 IK**(어깨-팔꿈치-손목, 엉덩이-무릎-발목)로 맞춘다. 팔꿈치·무릎이 굽는 쪽(pole)은 무게 `w`에 맞춰 원래 자세에서 섞어 튀지 않는다.
  - 손 방향(`orient_hand`: 손가락·손바닥이 볼 쪽), 주먹(`curl_fingers`), 골반 높이(`move_pelvis`, 뛰기·들썩임 — 발은 IK로 땅에 붙인다), 허리 숙임(`lean`).
  - 처음·끝에 0.3~0.5초씩 대기 자세에서 섞어 들어가고 나온다. 반복 클립(박수)은 대기 길이(75)와 같게 해 이음새가 없다.
- 결과를 키로 구워 뼈대만 FBX로 내보낸다(`Armature|이름` 테이크 → 클립 `이름`). 같은 뼈대(UAL1)라 `rotationOffset` 없이 UAL 클립과 같은 쪽을 본다.
- `--preview`: 마네킹으로 클립마다 앞·옆 6프레임씩(`<폴더>/<클립>.png`). 축: 캐릭터는 +Y를 보고 오른쪽 +X.
- 새 반응 동작: 함수 하나(`def 이름(f): ... return 길이`)를 만들어 `CLIPS`에 넣고 `CharacterAnimator`에 상수를 만든다. 이름에 `Loop`가 있으면 반복.
- 확인: `sh tools/autoshot.sh 02r_scared` → `02r_cheer`·`02r_excited`·`02r_excited_pump`·`02r_scared`(내 캐릭터 앞에서). NPC는 `sh tools/inspect.sh "lobby:NPC_이름@wave:180:5:1.4"`처럼 이름 뒤 `@wave`·`@clap`·`@cast`·`@reel`.

## 3. 임포트 설정과 함정

`AssetImportRules`가 매번 같은 설정을 넣는다(손으로 인스펙터를 고치면 다음 임포트에서 덮어써진다 → 코드를 고친다).

- 휴머노이드, 아바타는 각 파일에서 만든다(자동 매핑으로 유효). Kevin Iglesias만 패키지 아바타를 복사(7장).
- 루트 회전·높이·위치는 모두 포즈에 굽고(Bake Into Pose) 원본 기준. 그래서 `Excited`에서 골반이 올라가면 몸 전체가 뜬다.
- 포스트프로세서 코드만 바꾸면 Unity가 FBX를 다시 임포트하지 않는다. 규칙을 고친 뒤에는 에디터에서 해당 FBX를 Reimport하거나, `.meta`를 건드려 배치모드 빌드가 다시 임포트하게 한다.
- 클립 이름에 `Loop`가 있으면 반복(loopTime·loopPose).

`CharacterAnimator` 쪽:
- 반복 안 하는 클립을 `SetLoop`에 주면 한 번 재생하고 끝 자세에서 멈춘다. 다른 상태로 갔다가 다시 들어오면 처음부터 재생한다.
- `PlayOnce(클립, 배속, cutAt)`은 길이 × `cutAt`이 지나면 마지막 `SetLoop` 상태로 0.25초 동안 섞어 돌아간다. 도는 동안(`PlayingOnce`)은 낚싯대 팔 IK를 끈다(5장).

## 4. 예전 Mixamo 동작 (기록)

2026-10-21까지는 Mixamo(Adobe) X Bot 동작을 썼다: Fishing Idle(낚시 중 몸), Fishing Cast(들기·던지기·챔질·릴 감기로 쪼갬, NPC는 한 벌 통째로), Cheering, Clapping, Excited, Rallying, Scared + 받은 동작 약 45개(춤·좀비·취객·헤엄 등, 장식 NPC를 걷어낸 뒤로는 대물 환호 40% `JoyfulJump`·줄 끊김 35% `Angry`만 썼다). 쪼갠 구간·임포트 함정(`rotationOffset = 180`)은 git 기록(`AssetImportRules.MixamoClips`, 이 문서 이전 판)에 있다. Mixamo 약관은 원본 FBX 재배포를 막아서 공개 저장소에 둘 수 없다.

## 5. 낚싯대와 손 (팔 IK, 2026-10-20)

낚싯대 각도(`PlayerController.RodPitch`·`RodYaw`, `BodyLean`)는 코드가 정하고, **내 캐릭터의 두 팔은 그 각도를 따라가는 2관절 IK**다. 예전에는 젖히기·던지기·챔질을 Mixamo 클립(`Windup`·`Cast`·`Hook`)이 시간대로 재생하고 낚싯대 각도는 마우스를 따라 따로 움직여서, 팔과 낚싯대가 따로 놀았다(사용자 지적: 어색하다).

- 몸은 낚시 중 늘 `HoldRod`(UAL `Idle_Loop`, 2026-10-21까지 Mixamo Fishing Idle) + 상체 젖힘/숙임(`BodyLean`) + 낚싯대를 좌우로 틀면 가슴이 반쯤 따라 돈다.
- **오른손** (`UpdateRodArmRight`): 오른 어깨 기준 손목 자리를 낚싯대 각도 표 `RodArm.GripCurve`에서 고른다(NPC 낚시꾼도 같은 표) — 2° 던진 직후 앞으로 뻗음, 35° 허리 앞 대기, 82° 챔질(가슴 앞에 세움), 120°, 155° 어깨 너머로 젖힘. 사이는 부드럽게 섞는다. 팔꿈치 방향(pole)은 들고 있을 땐 아래 바깥, 젖힐수록 앞 바깥.
- 낚싯대는 오른손 손바닥 앞(`RodGrip.Grip`)에 붙는다.
- **왼손** (`UpdateRodArmLeft`): 릴 시트에서 0.24m 뒤(손잡이 끝 쪽)를 감싸 쥔다. 릴을 감을 때(`UpdateCrank`)와 고기를 들 때(`UpdateHold`)는 그 위에서 손잡이·고기로 옮겨 간다.
- 걷기·점프·운전·넣어 둠·고기 들어 보이기·한 번짜리 동작(환호·펄쩍·움찔, `PlayingOnce`) 중에는 0.25초에 걸쳐 IK를 풀고 애니메이션 그대로. (2026-10-21 전에는 결과 화면이 `Result` 상태라 IK가 켜진 채로 환호 팔을 덮어썼다.)
- 확인: autoshot `02a_windup`, `02s_rod_arms_2`·`_35`·`_82`·`_155`(옆에서 각도별), `04b_hook_raise`(챔질 순간).

- VRM 캐릭터(16 문서)도 같은 IK를 쓴다. 손 자리 표는 기본 캐릭터 팔(어깨~손목 0.453m)에 맞춘 값이라 캐릭터 팔 길이 비율(`RodArm.ArmScale`, 0.6~1.3)만큼 줄이거나 늘인다. 뼈 이름이 달라 손·머리·가슴은 휴머노이드 매핑으로 찾는다.

NPC 낚시꾼도 같은 방식이다(10장): 평소 40°, 20~45초마다 다시 던지기(`NpcAngler.CastPitch`: 1초에 걸쳐 155°로 젖힘 → 0.3초 멈춤 → 0.25초에 2°로 휘두름 → 앞으로 뻗은 채 → 1초에 걸쳐 40°), 오른손은 `UpdateRightHand`가 같은 표로 따라간다.

## 6. 라이선스

- UAL1(Quaternius): CC0. 반응 동작(`Reactions.fbx`, `build_reactions.py`): 직접 만든 것이라 프로젝트 라이선스.
- Mixamo: 2026-10-21에 모두 걷어냄(4장). 게임에 넣어 배포는 되지만 원본 FBX 재배포가 안 돼서 공개 저장소에 둘 수 없었다.
- Human Basic Motions FREE(Asset Store): 에셋 스토어 약관도 원본 재배포를 막는다. **공개 전에 바꿀 것** (7장).

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
- **임포트**: 휴머노이드, 아바타는 패키지 그대로 **Copy From Other**(HumanM/F_Model) — 우리 규칙이 `CreateFromThisModel`로 덮지 않게 경로를 따로 처리한다. 루트는 포즈에 굽고, +Z를 보므로(UAL은 -Z) `rotationOffset = 180`.
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

## 8. 상황 동작: 제자리 돌기 · 펄쩍 · 손짓 · 움찔 (2026-10-15, 10-21 직접 만든 동작으로)

| 동작 | 클립 | 언제 | 코드 |
|---|---|---|---|
| 제자리 돌기 | `Basic_TurnLeft_Loop` / `Basic_TurnRight_Loop` (Human Basic Motions Turn01, 남·녀) | NPC가 플레이어 쪽으로 25° 넘게 몸을 돌릴 때 발을 옮기며 돈다. 8° 안으로 들어오면 멈춤(사이는 이전 상태 유지 — 떨림 방지). 앉은 NPC는 그대로 | `NpcAngler` `_turning` |
| 펄쩍 | `Excited` (1장) | 괴물 어종(`Boss`)이나 희귀도 5 어종을 낚았을 때, 보통 환호 대신 | `FishingController` → `PlayerController.PlayExcited` |
| 손짓 | `Wave_Call` (1장) | 보고할 퀘스트가 있고 플레이어가 15m 안에 오면 조합 연락관(`union`)이 9~14초마다 | `NpcAngler` `_nextRally` |
| 움찔 | `Scared` (1장) | 줄이 끊어졌을 때 (낚싯대 자세를 돌린 뒤 한 번) | `FishingController` → `PlayerController.PlayScared` |

- 한 번짜리 동작은 `PlayOnce(클립, 배속, cutAt)`으로 틀고, 끝나면 원래 루프로 돌아간다.
- 몸 방향은 코드가 돌리고 Turn 클립은 발만 맞춘다(루트 모션 없음).

## 9. 배경 사람 걷기 (2026-10-16)

> 2026-10-20: 장식 NPC(`Wanderer`)를 모두 걷어내서 아래는 기록으로만 남긴다 (16 문서 4장). 걷기 배속 규칙(`CharacterAnimator.BasicWalkSpeed`)은 그대로다.

사용자 지적: 한강 둔치를 걷는 의사가 이상하다. 자동 촬영 `00o_doctor_walk`(0.25초 간격 두 장)로 보니 셋이 겹쳤다.

| 문제 | 원인 | 고침 |
|---|---|---|
| 허리를 숙이고 팔을 휘저으며 살금살금 걷는다 | Mixamo `Walking`(`Walk2_Loop`)이 그런 걸음이었다 | 의사·순찰(군인·특공대)은 Human Basic Motions 기본 걷기(`Basic_Walk_Loop`)로 |
| 발이 미끄러진다 | 클립은 그대로 1배속인데 이동은 1.2 m/s | `Wanderer.Spec.WalkAnimSpeed` 기본 0 = 기본 걷기면 `이동 속도 / CharacterAnimator.BasicWalkSpeed(2 m/s)`로 자동. 의사 1.5 m/s(×0.75), 순찰 1.4 m/s(×0.7) |
| 끝점에서 돌아설 때 뒷걸음질·옆걸음질 | 몸이 돌기 전에 새 방향으로 움직였다 | 앞과 갈 방향의 내적이 0.7 아래면 그 자리에서 빠르게(5) 돌고, 0.7~0.95 사이에서 서서히 걷기 시작 |

- 의사 길은 풀밭(z −69)에서 산책로(z −66.6)로 옮겼다.
- 좀비·취객·헤엄은 자기 클립 배속을 그대로 쓴다(`WalkAnimSpeed`를 직접 준다).

## 10. NPC 낚시꾼의 손 (2026-10-21)

사용자 지적: 다른 NPC를 보면 낚싯대와 릴 감는 손이 반대 같다.
NPC는 20~45초마다 Mixamo `Fishing_Sequence`(들기 → 던지기 → 기다리기 → 릴 감기)를 통째로 틀었는데, 뒤 절반의 릴 감기는 **낚싯대 쥔 오른손이** 원을 그렸다.

- 같은 날 Mixamo를 걷어내면서(1장) NPC도 내 캐릭터처럼 **두 팔 IK**가 됐다: 몸은 `HoldRod`(UAL 대기), 오른손은 낚싯대 각도(`CastPitch`)를 따라가는 `UpdateRightHand`(5장 표), 낚싯대는 오른손(`RodGrip`)에 붙는다.
- 던지고 2~6초 뒤 3~5초 동안 릴을 감는다: 왼손을 2관절 IK(`ArmIK`, 내 캐릭터와 같은 코드)로 릴 손잡이(`TelescopicRod.HandleKnob`)에 붙이고 `SpinReel`.
- 평소엔 왼손이 오른손 바로 아래 손잡이를 감싸 쥔다(내 캐릭터 `UpdateRodArmLeft`와 같다). 던지는 동안에도 쥔 채로, 한 번짜리 클립(손짓·박수, `CharacterAnimator.PlayingOnce`)이 도는 동안만 IK를 끈다.
- 확인: `sh tools/inspect.sh "hangang:NPC_새벽조사@reel:120:15:1.1"` — 이름 뒤 `@reel`이면 바로 릴 감기, `@cast`면 바로 다시 던지기(1.2초 뒤 찍으니 어깨 너머로 젖힌 끝). NPC는 몸(SkinnedMeshRenderer)만으로 경계를 잡는다(줄·찌까지 넣으면 카메라가 물 위를 봤다).

## 11. 왼손 잡는 방향 (2026-10-21)

사용자 지적(스크린샷): 왼손이 반대로 잡고 있고, 오른손은 그대로 두고 왼손을 더 오른손 쪽으로 올려 잡아야 한다. 왼손 그립이 잘못돼 왼팔이 몸 쪽으로 파고들었다.
- 원인: `RodGrip.AlignTo`는 새끼→검지 방향만 낚싯대 축에 맞춰서, 축 둘레 방향(손바닥이 어디를 보는지)은 애니메이션 그대로였다 → 손바닥이 바깥을 보고 손등이 낚싯대에 붙었고, 손바닥 앞 자리를 낚싯대에 맞추려고 팔이 몸 쪽으로 꺾였다.
- `RodGrip.RollPalm(axis, want)`: 축을 중심으로 손을 굴려 손바닥이 몸 오른쪽(낚싯대 쪽)을 보게 한 뒤 IK. 내 캐릭터(`UpdateRodArmLeft`)와 NPC(`UpdateLeftHand`, 릴을 감을 땐 끈다) 모두.
- 왼손 자리 `LeftGripBehind` 0.24 → 0.12m: 손잡이 끝이 아니라 오른손(릴 시트) 바로 아래.
- 확인 장면 `02t_left_hand_35`·`02t_left_hand_82` (왼쪽 앞에서 가까이 — 오른쪽에서 찍은 `02s_rod_arms`는 왼손이 몸에 가려 안 보였다).

