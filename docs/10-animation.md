# 10. 캐릭터 애니메이션 (UAL + Mixamo 낚시 동작)

캐릭터 동작은 AnimatorController 없이 `CharacterAnimator`(Playables 믹서)가 클립 이름으로 재생한다. 클립은 두 곳에서 찾는다.

| 라이브러리 | 위치 | 쓰는 동작 |
|---|---|---|
| Quaternius Universal Animation Library (CC0) | `Resources/Animations/UAL1_Standard.fbx` | 대기·걷기·뛰기·점프·말하기·앉기·운전·춤 |
| **Mixamo** (Adobe) | `Resources/Animations/Mixamo/*.fbx` | 낚싯대 들고 대기, 들어 올리기, 던지기, 챔질, 릴 감기, 환호, NPC 낚시 한 벌 |

두 FBX 모두 휴머노이드로 임포트해서(`AssetImportRules`) 어떤 캐릭터(Angler_Male·Female, NPC)에도 그대로 붙는다.

## 1. Mixamo 파일

받은 원본은 `Assets/MixamoAnimation/`에 있다(X Bot, 30fps). **쓰는 것만** `Resources/Animations/Mixamo/`로 옮겨 이름을 바꿨다(`.meta` GUID 유지). 나머지는 빌드에 안 들어간다.

| 파일 (Resources/Animations/Mixamo) | 원본 | 길이 |
|---|---|---|
| `FishingIdle.fbx` | X Bot@Fishing Idle | 106프레임 (3.5초) |
| `FishingCast.fbx` | X Bot@Fishing Cast | 264프레임 (8.8초) |
| `Cheering.fbx` | X Bot@Cheering | 88프레임 (2.9초) |

`MixamoAnimation/`에 남은 것(지금 안 씀): Cheering (1)(주먹 불끈 — 고개를 숙여 환호 클로즈업에서 얼굴이 모자에 가려 뺐다), Clapping, Excited, Rallying, Scared, Sitting·Sitting Idle 여러 개. 대회 시상·관중·앉은 NPC 등에 쓰고 싶으면 같은 방법으로 옮긴다(아래 4장).

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
| `Fishing_Windup` | FishingCast | 0~45 | | 캐스팅하려고 당기는 동안(`Charging`) 1.5배속. 끝 자세에서 멈춰 있다 |
| `Fishing_Cast` | FishingCast | 45~95 | | 버튼을 놓는 순간 `PlayCast()` 1.5배속 → 끝나면 `HoldRod` |
| `Fishing_Hook` | FishingCast | 0~30 | | 챔질 `PlayHook()` 2배속 (홱 들어 올리기) |
| `Fishing_Reel_Loop` | FishingCast | 207~238 | ○ | 대기 중 좌클릭으로 찌·루어를 감는 동안 (`FishingController.ReelingIn`) |
| `Fishing_Sequence` | FishingCast | 0~263 | | NPC 낚시꾼이 20~45초마다 한 벌 통째로 (끝 자세 = `HoldRod` 첫 자세라 이음새가 없다) |
| `Cheer_ArmsUp` | Cheering | 0~87 | | 랜딩 환호 `PlayCheer()` (두 팔 들고 흔들기, 얼굴 클로즈업) |

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

## 5. 낚싯대와 손

낚싯대는 손뼈(`hand_r`) **위치만** 따라가고 각도는 코드(`PlayerController.RodPitch`·`RodYaw`, `BodyLean`)가 정한다. 그래서 던지기·챔질에서 낚싯대 각도와 팔 동작이 따로 움직인다. NPC 낚싯대는 -40° 고정이라 `Fishing_Sequence`에서 손을 머리 위로 들 때 낚싯대가 기울지 않는다. 더 자연스럽게 하려면 손뼈 회전을 낚싯대에 섞는 것이 다음 단계다(뼈 축이 리그마다 달라서 오프셋을 맞춰야 한다).

## 6. 라이선스

Mixamo 동작은 Adobe 계정으로 받아 게임에 넣어 배포할 수 있지만, **원본 FBX를 그대로 재배포하는 것은 허용되지 않는다.** 이 저장소는 공개라 원래 계획(08 문서 5장)은 `.gitignore`된 폴더에 두는 것이었는데, 2026-10-14에 에셋 스토어 패키지와 함께 `Assets/MixamoAnimation/`을 커밋했다(사용자 결정). 저장소를 비공개로 돌리거나 이 파일들을 LFS 기록에서 빼는 것은 사용자가 정한다.
