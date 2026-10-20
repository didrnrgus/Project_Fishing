# 16. VRM 캐릭터 (내가 만든 캐릭터로 플레이)

2026-10-20 사용자 결정: 캐릭터를 **애니풍(서브컬처)** 으로 바꾸고, 플레이어가 직접 만든 캐릭터를 **빌드 없이** 넣어 플레이할 수 있게 한다.
처음엔 ReadyPlayerMe를 원했지만 Netflix 인수 뒤 **2026-01-31에 아바타 생성기·공개 API가 종료**되어 쓸 수 없다. 그래서 VRM(VRoid Studio 등에서 만드는 휴머노이드 아바타 표준 형식)으로 간다.

| 실제 낚시 화면 (Seed-san, 젖힘) | 캐릭터 창 [캐릭터 모델] 탭 |
|---|---|
| ![vrm](images/02v_vrm_155.png) | ![panel](images/21e_character_model.png) |

## 1. 쓰는 법 (플레이어)

1. [VRoid Studio](https://vroid.com/studio)(무료)에서 캐릭터를 만들고 **VRM으로 내보내기** (0.x·1.0 둘 다 된다).
2. 캐릭터 창(C) → **[캐릭터 모델]** → **[폴더 열기]** → 열린 폴더에 `.vrm` 파일을 넣는다.
   - 폴더: `문서\짭풍명월\Characters` (Windows 문서 폴더. OneDrive로 옮겨 둔 PC는 그 아래)
3. **[새로고침]** → 목록에서 **[고르기]**. 필드·대기실·초상화가 바로 그 캐릭터로 바뀐다.
4. 걸음걸이(남자/여자 대기·걷기 클립)를 고른다.

- 의상 능력치는 모델과 상관없이 그대로 붙는다(아바타 슬롯에서 입은 옷). VRM 캐릭터에는 옷이 보이지 않는다 → 3장 남은 일.
- 고른 모델은 세이브(`SaveData.vrm`, `vrmFemale`)에 남는다. 파일이 없어지면 기본 캐릭터로 서 있다.

## 2. 구조

| 파일 | 하는 일 |
|---|---|
| `Packages/manifest.json` | **UniVRM v0.131.4** (MIT, VRM 컨소시엄): `com.vrmc.gltf`(UniGLTF) + `com.vrmc.vrm`(VRM-1.0 로더, 0.x는 읽으면서 1.0으로 바꾼다) |
| `Scripts/Vrm/VrmLoader.cs` + `Fishing.Vrm.asmdef` | UniVRM을 감싼 얇은 어셈블리. UniVRM의 `UniGLTF.Utils`가 자동 참조되지 않아 게임 코드(Assembly-CSharp)에서 `Vrm10.LoadPathAsync`를 바로 부를 수 없어서 따로 뺐다. 메타(이름·만든 사람·썸네일·사용 허락)는 `VrmMeta`로 넘긴다. 눈 깜빡임 `VrmBlink` |
| `Scripts/Characters/VrmCharacters.cs` | 폴더 훑기(`List`), 읽기(`Request` → `Loaded` 이벤트), 복제(`Create`). id는 `user/파일.vrm`(문서 폴더) · `builtin/파일.vrm`(`StreamingAssets/Characters`, 게임에 넣어 배포할 기본 캐릭터) |
| `CharacterLook.Vrm`·`VrmFemale` → `CharacterFactory.Create` | VRM이 읽혀 있으면 그 복제본, 아니면 읽기 시작하고 기본 캐릭터 |
| `GameUI` | `VrmCharacters.Loaded`를 듣고 고른 모델이면 필드·대기실 캐릭터를 다시 입힌다 |
| `PlayerProfile.SetCharacterModel` | 모델·걸음걸이 저장 + `LookChanged` |
| `CharacterPanel` [캐릭터 모델] 탭 | 기본 캐릭터 / 폴더의 VRM 목록(이름·만든 사람·키·허락), 폴더 열기·새로고침, 걸음걸이, 왼쪽 카드에 썸네일·사용 허락 |
| `ProjectTools.RuntimeShaders` | `VRM10/MToon10`, `UniGLTF/UniUnlit`을 Always Included에 (실행 중 `Shader.Find`) |

### 읽기·복제

- 한 파일은 **한 번만 읽어** 숨긴 선반(`VrmShelf`, 꺼짐·DontDestroyOnLoad)에 원본으로 두고, 필드·대기실이 **복제**해서 쓴다. 텍스처·메쉬는 원본이 가진다.
- 복제본은 `RuntimeGltfInstance`를 뗀다 → UniVRM이 "씬에 놓인 프리팹"으로 보고 지금 자세(원본의 T 자세)를 초기 자세로 잡는다. 떼지 않으면 복제본의 초기 자세 표가 비어 있다(직렬화되지 않는 필드). 원본이 꺼진 채 복제하니 복제본 `OnDestroy`가 원본 자원을 지우지 않는다.
- **컨트롤 리그 없이** (`ControlRigGenerationOption.None`): 휴머노이드 Animator가 뼈를 바로 움직인다 → 우리 동작 클립(UAL·Mixamo·Kevin Iglesias, 모두 휴머노이드)·팔 IK(10 문서 5장)·손가락 쥐기(`RodGrip`의 휴머노이드 경로)가 그대로 붙는다.
- VRM은 +Z를 보지만 우리 클립은 −Z에 맞춰 두어서 받은 캐릭터(`ExternalCharacters`)처럼 **180° 돌린다**.
- 키는 VRM 그대로(1.0~2.2m 밖이면 1.6m로 맞춤). `PlayerController`가 팔 길이(어깨~손목)를 재서 낚싯대 쥔 손 자리 표를 기본 캐릭터 팔(0.453m) 대비로 줄이거나 늘인다(`_armScale`).
- 뼈 이름이 달라서 `PlayerController`는 손·머리·가슴을 이름으로 못 찾으면 휴머노이드 매핑(`Animator.GetBoneTransform`)으로 찾는다.

### 사용 허락 (VRM 메타)

VRM 파일에는 만든 사람이 정한 허락이 들어 있다. 캐릭터 창이 보여 준다.
- **아바타 사용 허락**: 만든 사람만 / 따로 허락받은 사람만이면 노란 경고("직접 만든 모델일 때만 쓰세요"). 막지는 않는다(본인이 만든 모델일 수 있다).
- **만든 사람 표기 필요**면 그렇다고 알린다. 목록·카드에 만든 사람을 늘 보여 준다.
- 낚시는 폭력·성적 표현이 아니라 그 항목은 보지 않는다.

## 3. 남은 일 (순서대로)

1. **기본 캐릭터를 VRM으로**: 남·녀 기본 캐릭터(와 의상별 변형)를 VRoid Studio로 만들어 `StreamingAssets/Characters/`에 넣는다(사용자 작업 — Claude는 VRoid GUI를 못 쓴다). 넣으면 처음 시작할 때 고를 수 있게 한다.
2. **의상 능력치 → 덧입히는 장비**: VRM은 옷까지 한 덩어리라, 모자·선글라스·조끼처럼 위에 덧입힐 수 있는 것만 VRM 뼈(머리·가슴)에 붙이고, 상의·하의·신발 능력치는 장비/장신구로 옮긴다(사용자 동의, 2026-10-20).
3. **Quaternius 기본 캐릭터 걷어내기**: 1·2가 끝나면 `Resources/Characters/Angler_*`, `garments.py`, `Fishing/Skin`·`Fishing/Garment`, `CharacterFactory` 옷 조합을 지운다.
4. **NPC도 VRM으로**, 장식 NPC 정리, 유니티짱에 역할 주기 (WORK-IN-PROGRESS 참고).
- 나중에: VRoid Hub에서 바로 불러오기(pixiv에 앱 신청 필요), 표정(입질·환호 때 웃음).

## 4. 확인

- 자동 촬영: 폴더(`문서\짭풍명월\Characters`)에 `.vrm`이 있으면 첫 번째로 갈아입고 `02v_vrm_35`·`_82`·`_155`·`02v_vrm_front`, 캐릭터 창 `21e_character_model`을 찍고 원래대로 돌린다. 없으면 건너뛴다(창은 빈 목록 안내).
- 시험용 모델: VRM 컨소시엄 샘플 [Seed-san](https://github.com/vrm-c/vrm-specification/tree/master/samples/Seed-san) (VRM Public License 1.0). **저장소에는 넣지 않았다** — 받아서 문서 폴더에 넣어 시험한다.
