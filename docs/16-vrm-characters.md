# 16. VRM 캐릭터 (모든 사람이 VRM)

2026-10-20 사용자 결정: 캐릭터를 **애니풍(서브컬처)** 으로 바꾸고, 플레이어가 직접 만든 캐릭터를 **빌드 없이** 넣어 플레이할 수 있게 한다.
처음엔 ReadyPlayerMe를 원했지만 Netflix 인수 뒤 **2026-01-31에 아바타 생성기·공개 API가 종료**되어 쓸 수 없다. 그래서 VRM(VRoid Studio 등에서 만드는 휴머노이드 아바타 표준 형식)으로 간다.

같은 날 2단계: **빌드에 박혀 있던 캐릭터 모델을 모두 걷어냈다.** 내 캐릭터·낚시점 사장·조합 연락관·낚시꾼 NPC·배 선장이 전부 VRM이다.
- 의상은 **능력치만** 남는다. 몸에 덧입히는 것은 없고, 몸에 붙여 보이는 장비는 **낚싯대 하나**다(사용자 결정).
- 퀘스트·정보와 상관없는 장식 NPC(산책하는 의사, 벤치 수다, 버스킹 아이돌 유니티짱, 밤의 좀비·귀신·광대·댄서, 코스프레 낚시꾼 등)는 모두 뺐다.
- 2026-10-21: **주인공은 여자가 기본** (사용자 결정, `SaveData.vrmFemale` 기본 true, 예전 세이브는 `lookVersion`으로 한 번 바꾼다). NPC는 머리·옷 색을 이름으로 바꿔 더 다양하게(`NpcLooks`), 낚시꾼 NPC는 릴을 왼손으로 감는다.
- 빌드한 뒤에도 파일만 바꿔 내 캐릭터·기본 캐릭터·NPC를 갈아입힐 수 있다 → 설명서 [가이드 07](guide/07-characters.md), 게임 폴더의 `StreamingAssets/Characters/읽어주세요.txt`.

| 실제 낚시 화면 (VRM, 젖힘) | 캐릭터 창 [캐릭터 모델] 탭 |
|---|---|
| ![vrm](images/02v_vrm_155.png) | ![panel](images/21e_character_model.png) |

## 1. 쓰는 법 (플레이어)

1. [VRoid Studio](https://vroid.com/studio)(무료)에서 캐릭터를 만들고 **VRM으로 내보내기** (0.x·1.0 둘 다 된다).
2. 캐릭터 창(C) → **[캐릭터 모델]** → **[폴더 열기]** → 열린 폴더에 `.vrm` 파일을 넣는다.
   - 폴더: `문서\짭풍명월\Characters` (Windows 문서 폴더. OneDrive로 옮겨 둔 PC는 그 아래)
3. **[새로고침]** → 목록에서 **[고르기]**. 필드·대기실·초상화가 바로 그 캐릭터로 바뀐다.
4. 아래 [남자]/[여자]: 기본 캐릭터면 남/녀 기본 모델, 고른 VRM이면 걸음걸이(대기·걷기 클립).

- 게임과 함께 온 모델 11개도 목록에 `기본 제공`으로 나온다 → 그대로 골라 쓸 수 있다.
- 고른 모델은 세이브(`SaveData.vrm`, `vrmFemale`)에 남는다. 파일이 없어지면 기본 캐릭터로 서 있다.

## 2. 기본 제공 캐릭터 (CC0)

`Assets/StreamingAssets/Characters/` (빌드: `ProjectFishing_Data/StreamingAssets/Characters/`). Git LFS(`*.vrm`).
VRoid 프로젝트(pixiv)가 VRoid Studio 알파 시절 샘플로 **CC0** 공개한 모델을 [OpenGameArt 미러](https://opengameart.org/content/vroid-studio-cc0-models)에서 받았다. 파일 안 VRM 메타도 "누구나 사용·재배포·상업 이용 허락, 표기 불필요"(VRoid Hub 조건 URL)다. 현재 VRoid Studio의 AvatarSample A~C는 조건이 달라 쓰지 않는다.

| 파일 | 역할 |
|---|---|
| `Sendagaya_Shino.vrm` (千駄ヶ谷篠) | 기본 캐릭터 (여자) — 주인공 기본 |
| `HairSample_Male.vrm` | 기본 캐릭터 (남자), 남자 NPC |
| `Sakurada_Fumiriya.vrm` (桜田史利矢), `Base_Male.vrm` | 남자 NPC |
| `AvatarSample_D`(千駄ヶ谷渋)·`E`(ビビ)·`F`(ヴィータ)·`G`(ヴィクトリア・ルービン), `HairSample_Female`, `Base_Female` | 여자 NPC |
| `AvatarSample_D_Darkness.vrm` | 목록에서 고를 수만 있다 (엘프 귀라 NPC 목록에선 뺐다) |

남자 모델은 셋뿐이라 같은 얼굴이 되풀이된다. 대신 NPC마다 이름으로 **머리색(9)·상의(10)·하의(6) 색**과 **키(±6%)** 를 바꾼다(`NpcLooks`, 아래). 모델을 더 넣으려면 `characters.json`의 `npcMale`/`npcFemale`에 파일을 더한다.

다른 후보도 찾아봤다: [madjin/vrm-samples](https://github.com/madjin/vrm-samples)는 같은 VRoid CC0 세트, Seed-san(VRM Public License, 재배포 허용·표기 필요)은 SF 슈트·배낭 차림이라 낚시꾼으로 어색해서, Orion(Polygonal Mind, CC0)은 로우폴리 화풍이 달라서 넣지 않았다.

## 3. 구조

| 파일 | 하는 일 |
|---|---|
| `Packages/manifest.json` | **UniVRM v0.131.4** (MIT, VRM 컨소시엄): `com.vrmc.gltf`(UniGLTF) + `com.vrmc.vrm`(VRM-1.0 로더, 0.x는 읽으면서 1.0으로 바꾼다) |
| `Scripts/Vrm/VrmLoader.cs` + `Fishing.Vrm.asmdef` | UniVRM을 감싼 얇은 어셈블리. `LoadAsync`(뒤에서), `Load`(ImmediateCaller로 이 프레임 안에), 메타(이름·만든 사람·썸네일·사용 허락) → `VrmMeta`, 눈 깜빡임 `VrmBlink` |
| `Scripts/Characters/VrmCharacters.cs` | 폴더 훑기(`List`), 바로 읽기(`LoadNow`), 뒤에서 읽기(`Request` → `Loaded`), 복제(`Create`). id는 `builtin/파일.vrm`(게임 폴더) · `user/파일.vrm`(문서 폴더) |
| `Scripts/Characters/CharacterRoster.cs` | `characters.json`을 읽어 누구에게 어떤 파일을 입힐지 정한다: 기본 캐릭터 남/녀(`PlayerDefault`), NPC(`ForNpc`: 이름 지정 → 성별 목록에서 이름 해시로 고르기). 문서 폴더 것이 게임 폴더 것을 덮는다 |
| `Scripts/Characters/NpcLooks.cs` + `Resources/Shaders/Recolor.shader` | NPC 머리·옷 색 바꾸기: VRoid 재질 이름(`_HAIR`, `_Tops_`·`_Onepice_`, `_Bottoms_` + `_CLOTH`)으로 부위를 찾아, 텍스처를 밝기만 남기고 색을 입힌 렌더 텍스처(최대 1024, 밉맵)로 바꾼다. 평균 밝기(16×16로 줄여 한 번 읽음)가 새 색이 되게 맞춰 어두운 머리도 금발이 된다. 그림자 색(`_ShadeColor`)은 색조를 빼 회색으로. 같은 텍스처 + 같은 색은 한 번만 만든다. 부위마다 1/6은 원래 색. 이름으로 정해 둔 사람(`characters.json` npc)과 내 캐릭터는 그대로 |
| `CharacterLook` → `CharacterFactory.Create` | 이름·칭호·성별·VRM id만 남았다. VRM id가 비면 `CharacterRoster`가 고르고, 처음 쓰는 파일은 `LoadNow`로 그 자리에서 읽는다. 못 읽으면 null → 플레이어는 도형 사람, NPC는 회색 마네킹(`CharacterFactory.Placeholder`) |
| `PlayerProfile.BuildLook` | 고른 VRM(파일이 있으면) 또는 기본 캐릭터(`vrmFemale`로 남/녀). 의상은 넣지 않는다 |
| `PlayerProfile.SetCharacterModel` | 모델·성별 저장 + `LookChanged` (의상 입기·칭호 바꾸기는 더 이상 캐릭터를 다시 세우지 않는다) |
| `GameUI` | 캐릭터 창이 뒤에서 읽던 VRM이 다 읽히면(`VrmCharacters.Loaded`) 필드·대기실 캐릭터를 다시 세운다 |
| `CharacterPanel` [캐릭터 모델] 탭 | 기본 캐릭터 + 폴더의 VRM 목록(이름·만든 사람·키·허락), 폴더 열기·새로고침(`characters.json`도 다시 읽음), 남/녀, 설정 경고(노란 글씨), 왼쪽 카드에 썸네일·사용 허락 |
| `ProjectTools.RuntimeShaders` | `VRM10/MToon10`, `UniGLTF/UniUnlit`을 Always Included에 (실행 중 `Shader.Find`) |

### 읽기·복제

- 한 파일은 **한 번만 읽어** 숨긴 선반(`VrmShelf`, 꺼짐·DontDestroyOnLoad)에 원본으로 두고, 필드·대기실·NPC가 **복제**해서 쓴다. 텍스처·메쉬는 원본이 가진다.
- 맵을 지을 때(로딩 화면 뒤) NPC·내 캐릭터가 바로 서 있어야 해서 **동기로 읽는다** (`VrmLoader.Load`, UniVRM `ImmediateCaller`). 한 파일에 수백 ms. 처음 그 모델이 나오는 맵에서만 걸리고 이후엔 복제만 한다.
- 캐릭터 창 목록은 문서 폴더 파일과 지금 쓰는 모델만 뒤에서 읽는다(`Request`). 기본 제공 11개를 다 읽으면 메모리가 커서다. `LoadNow`는 `Loaded`를 내지 않는다(부른 쪽이 바로 입히는 중이라 GameUI가 또 입히면 캐릭터가 둘 생긴다).
- 복제본은 `RuntimeGltfInstance`를 뗀다 → UniVRM이 "씬에 놓인 프리팹"으로 보고 지금 자세(원본의 T 자세)를 초기 자세로 잡는다. 떼지 않으면 복제본의 초기 자세 표가 비어 있다(직렬화되지 않는 필드). 원본이 꺼진 채 복제하니 복제본 `OnDestroy`가 원본 자원을 지우지 않는다.
- **컨트롤 리그 없이** (`ControlRigGenerationOption.None`): 휴머노이드 Animator가 뼈를 바로 움직인다 → 우리 동작 클립(UAL·Mixamo·Kevin Iglesias, 모두 휴머노이드)·팔 IK(10 문서 5장)·손가락 쥐기(`RodGrip`의 휴머노이드 경로)가 그대로 붙는다.
- VRM은 +Z를 보지만 우리 클립은 −Z에 맞춰 두어서 **180° 돌린다**.
- 키는 VRM 그대로(1.0~2.2m 밖이면 1.6m로 맞춤). `PlayerController`가 팔 길이(어깨~손목)를 재서 낚싯대 쥔 손 자리 표를 예전 기본 캐릭터 팔(0.453m) 대비로 줄이거나 늘인다(`_armScale`).
- 뼈는 모두 휴머노이드 매핑(`Animator.GetBoneTransform`)으로 찾는다 (예전 Quaternius 뼈 이름 `hand_r`·`spine_02` 경로는 지웠다).

### characters.json

```json
{
    "playerMale": "HairSample_Male.vrm",
    "playerFemale": "Sendagaya_Shino.vrm",
    "npcMale": ["Sakurada_Fumiriya.vrm", "Base_Male.vrm"],
    "npcFemale": ["AvatarSample_D.vrm", "..."],
    "npc": [{ "name": "김선장", "file": "Base_Male.vrm" }]
}
```

- 게임 폴더(`StreamingAssets/Characters/characters.json`)가 기본, 문서 폴더에 같은 이름 파일이 있으면 **적은 항목만** 덮는다 (`npc`는 합치고 같은 이름은 문서 폴더가 이긴다).
- 파일 이름 → id: 문서 폴더에 있으면 `user/`, 없으면 `builtin/`. 그래서 같은 이름의 `.vrm`을 문서 폴더에 넣기만 해도 기본 캐릭터가 바뀐다.
- NPC는 `npc` 이름 지정 → 성별 목록에서 이름의 FNV 해시로 고르기(실행마다 같다) → 다른 성별 목록 → 기본 캐릭터.
- JSON이 깨졌거나 적힌 파일이 없으면 `CharacterRoster.Warnings` → 캐릭터 창 맨 위 노란 글씨 + 로그.

### 옷 갈아입기 (VrmOutfit · OutfitChanger, 2026-10-21)

사용자 요청: 주인공은 교복 말고 데이트복·오피스룩 같은 옷 → 대기실에서 옷을 무작위로 바꾸고, 웹에서 받아 와 입히고, 끄고 켜도 그 옷.
- **VrmOutfit**: VRoid 모델은 몸 메쉬 하나(`Body.baked`)에 피부·상의·하의·신발이 재질별 조각(`…_SKIN`, `…_CLOTH`)으로 들어 있고 뼈 이름(`J_Bip_*`)이 같다. 입는 사람의 `_CLOTH` 조각은 메쉬 복사본에서 삼각형을 비워 숨기고, 옷 주인의 `_CLOTH` 조각만 떼어 새 SkinnedMeshRenderer로 입는 사람 뼈에 이름으로 붙인다. 바인드포즈는 옷 주인 것을 그대로 써서 정점이 각자 뼈 기준 자리를 지킨다(키·팔 길이가 달라도 소매·바짓단이 손목·발목을 따라간다). 입는 사람에게 없는 흔들림 뼈(후드·치마 `J_Sec_*`)는 옷 주인 뼈대의 가장 가까운 조상으로 붙이고 그 조상 기준 바인드포즈를 다시 계산한다(그 부분은 흔들리지 않는다). 피부 텍스처에 그려진 양말은 그대로 남는다.
- **주인공 기본 옷**: `characters.json`의 `playerFemaleOutfit` = `HairSample_Female.vrm`(흰 긴소매 원피스). 받은 여자 옷 중 오피스룩은 없어서(넥타이 셔츠는 교복) 데이트복으로 골랐다. 남자 옷(후드티)은 남자 몸이라 컸다.
- **세이브**: `SaveData.outfit` — `""` = characters.json 기본, `"own"` = 모델 제 옷, 그 밖 = 옷 주인 VRM id. `PlayerProfile.SetOutfit`. 끄고 켜도 그 옷이다.
- **대기실 [옷 바꾸기] / 캐릭터 창 [캐릭터 모델] 탭 옷 줄([랜덤] [기본] [제 옷])** → `OutfitChanger.Randomize()`:
  - `characters.json`의 `outfitUrls`에 아직 받지 않은 주소가 있으면 하나를 `UnityWebRequest`로 받아(`.vrm` 또는 `.vrm`이 든 `.zip`, 첫 바이트로 구분) `문서/짭풍명월/Characters/outfits/`에 두고 입힌다. 받은 옷은 다시 받지 않고 후보에 들어간다(`user/outfits/…`, 캐릭터 모델 목록에도 나온다).
  - 없거나 받기에 실패하면 가진 VRM(기본 11 + 문서 폴더) 중 지금 모델·지금 옷이 아니고 `_CLOTH`가 있는 것을 무작위로.
  - 게임과 함께 나가는 `outfitUrls`는 비워 두었다: 로그인 없이 받을 수 있는 공개 VRoid 옷 목록이 없다(VRoid Hub API는 pixiv 앱 등록 + 사용자 로그인 + 모델마다 내려받기 허락). 시험: OpenGameArt CC0 `avatarsample_g.zip` 주소를 넣고 받아 입는 것까지 확인(`00a2_outfit_random`).

### 사용 허락 (VRM 메타)

VRM 파일에는 만든 사람이 정한 허락이 들어 있다. 캐릭터 창이 보여 준다.
- **아바타 사용 허락**: 만든 사람만 / 따로 허락받은 사람만이면 노란 경고("직접 만든 모델일 때만 쓰세요"). 막지는 않는다(본인이 만든 모델일 수 있다).
- **만든 사람 표기 필요**면 그렇다고 알린다. 목록·카드에 만든 사람을 늘 보여 준다.
- 낚시는 폭력·성적 표현이 아니라 그 항목은 보지 않는다.

## 4. 걷어낸 것 (2026-10-20)

| 걷어낸 것 | 대신 |
|---|---|
| Quaternius 낚시꾼 `Resources/Characters/Angler_*`(몸 + 옷 메쉬 16종 + 머리카락), `tools/blender/build_characters.py`·`garments.py`, `Fishing/Skin`·`Fishing/Garment` 셰이더, `Universal Base Characters`, `ThirdParty/Quaternius/Source` | VRM |
| `CharacterLook`의 옷·머리·모자·피부 필드, `CostumeDef.Model`·`Pattern` | 의상은 능력치만 (색·모양은 목록 아이콘용으로 남김) |
| 받은 캐릭터 36명 `ExternalCharacters`·`CharacterLibrary`(+ `Editor/CharacterLibraryBake`): 유니티짱, Modern Zombie, Mixamo 사람 34종(3GB), 귀신 `Fishing/Ghost` 셰이더 | — |
| 장식 NPC `ExtraNpcs`(아이돌 `IdolRoutine`, 밤 손님 `NightVisitor`)·`Wanderer` | 퀘스트·대화 NPC(`NpcAngler`)만 남김. 한강 버스킹 무대는 빈 무대로 남았다 |
| 자동 촬영 `00h_idol_greeting`·`00i_idol_stage`·`00l_park_chat`·`00o_doctor_walk`·`00j_night_zombie`·`00k_night_ghost`·`00m_night_dancer`·`00n_night_clown` | — |

동작 클립(UAL, Mixamo·MixamoExtra, Kevin Iglesias)은 캐릭터 모델이 아니라 그대로 둔다(좀비·춤 클립은 지금 안 쓴다).

## 5. 남은 일

- 사용자가 만든 남·녀 기본 캐릭터로 바꾸기 (VRoid Studio — Claude는 VRoid GUI를 못 쓴다). `characters.json`의 `playerMale`/`playerFemale`만 바꾸면 된다.
- 남자 NPC 모델 더 넣기 (지금 2개).
- 텍스처 메모리: VRoid 텍스처는 압축 없이 올라온다. 한 맵에 모델이 많아지면 VRAM을 본다(`Texture2D.Compress` 후보).
- 나중에: VRoid Hub에서 바로 불러오기(pixiv에 앱 신청 필요), 표정(입질·환호 때 웃음).

## 6. 확인

- 자동 촬영: 목록 첫 VRM(`builtin/AvatarSample_D.vrm`)으로 갈아입고 `02v_vrm_35`·`_82`·`_155`·`02v_vrm_front`, 캐릭터 창 `21e_character_model`을 찍고 원래대로 돌린다. 나머지 장면의 내 캐릭터는 기본 캐릭터(남자).
- 시험용 모델: VRM 컨소시엄 샘플 [Seed-san](https://github.com/vrm-c/vrm-specification/tree/master/samples/Seed-san) (VRM Public License 1.0)도 문서 폴더에 넣어 시험할 수 있다. **저장소에는 넣지 않았다.**
