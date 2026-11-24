# 12. 실행 중 VRM 캐릭터 읽기·복제·옷 갈아입히기

[← 기술 문서 목차](README.md) · 코드: `Assets/Scripts/Vrm/VrmLoader.cs`(`Fishing.Vrm` asmdef), `Characters/VrmCharacters.cs`·`CharacterRoster.cs`·`NpcLooks.cs`·`VrmOutfit.cs`·`CharacterPack.cs` · 자세한 기록: [16 문서](../16-vrm-characters.md) · 플레이어 설명: [가이드 07](../guide/07-characters.md)

이 게임의 사람(내 캐릭터·낚시점 사장·조합 연락관·NPC 낚시꾼·동료 AI 윤슬)은 **모두 VRM 파일**이다. 빌드에 박힌 캐릭터 모델은 없다. 플레이어가 VRoid Studio로 만든 `.vrm`을 폴더에 넣으면 **빌드 없이** 그 캐릭터로 낚시한다.

| 해결한 문제 | 기법 |
|---|---|
| 빌드 없이 캐릭터 교체 | 실행 중 glTF/VRM 읽기(UniVRM) + JSON 배정표 |
| 한 모델을 NPC 여럿이 쓰면 느리고 무겁다 | 원본 한 번 읽기 → 숨긴 선반 → 복제 |
| 남자 모델 3개로 NPC 수십 명 | 이름 해시로 머리·옷 색·키 바꾸기 (밝기 보존 재채색) |
| 다른 모델의 옷 입히기 | 서브메쉬 단위 옷 떼기 + 뼈 이름으로 다시 스키닝 |
| 남이 만든 모델의 이용 조건 | VRM 메타의 허락 정보를 읽어 표시 |

## 1. 어셈블리 경계: UniVRM은 한 곳에서만 참조

UniVRM(v0.131.4, MIT)의 일부 어셈블리(`UniGLTF.Utils`)는 자동 참조되지 않아 게임 코드(Assembly-CSharp)에서 바로 부를 수 없다. 그래서 얇은 어셈블리 `Fishing.Vrm`만 UniVRM을 참조하고, 게임 코드는 `GameObject`와 `VrmMeta`(이름·만든 사람·썸네일·허락)만 주고받는다.

- 장점: UniVRM 버전을 올릴 때 고칠 곳이 `VrmLoader.cs` 하나다. 게임 코드는 "VRM을 읽는 법"을 모른다.
- VRM 0.x는 읽으면서 1.0으로 바꿔 한 가지 경로로 다룬다.
- 런타임에 `Shader.Find`로 찾는 `VRM10/MToon10`·`UniGLTF/UniUnlit`은 빌드 도구가 Always Included Shaders에 넣는다.

## 2. 한 번 읽고 여러 번 복제 — 원본 선반

```
VrmCharacters.LoadNow("builtin/Base_Male.vrm")
  └─ 처음이면 VrmLoader.Load (동기, 수백 ms) → 원본을 숨긴 선반(VrmShelf: 꺼짐 + DontDestroyOnLoad)에
CharacterFactory.Create(look)
  └─ Instantiate(원본) → RuntimeGltfInstance 떼기 → 180° 돌리기 → 키 맞추기 → CharacterAnimator(Playables)
```

- **텍스처·메쉬는 원본만 가진다.** NPC 다섯이 같은 모델이면 GPU 메모리는 한 벌.
- 원본이 **꺼진 채** 복제하므로 복제본이 지워져도(`OnDestroy`) 원본 자원은 남는다.
- **복제본에서 `RuntimeGltfInstance`를 떼는 이유**: 붙어 있으면 UniVRM이 복제본의 초기 자세 표(직렬화되지 않는 필드)를 비워 둔 채로 쓴다. 떼면 "씬에 놓인 프리팹"으로 보고 지금 자세(원본의 T 자세)를 초기 자세로 잡는다. — 문서화되지 않은 동작이라 복제본이 구겨지는 버그를 추적해 찾았다.
- **동기 vs 비동기**: 맵을 지을 때(로딩 화면 뒤) NPC가 바로 서 있어야 하므로 `ImmediateCaller`로 그 프레임 안에 읽는다. 캐릭터 창 목록은 뒤에서(`Request` → `Loaded` 이벤트) 읽는다. 기본 제공 11개를 다 읽으면 메모리가 커서, 목록은 정보만 보이고 **고를 때만** 읽는다(처음엔 15개를 한꺼번에 읽어 멈춘 듯했다).

## 3. 컨트롤 리그 없이 휴머노이드 그대로

`ControlRigGenerationOption.None`으로 읽으면 휴머노이드 Animator가 뼈를 바로 움직인다. 그래서 프로젝트의 모든 휴머노이드 동작(Unity Starter Assets 대기·걷기·뛰기·점프, Quaternius UAL, Blender로 직접 만든 반응 동작)과 **팔 2관절 IK**·**손가락 쥐기**(`RodGrip`)가 모델에 상관없이 붙는다.

- 뼈는 이름이 아니라 휴머노이드 매핑(`Animator.GetBoneTransform`)으로 찾는다.
- VRM은 +Z를 보지만 동작 클립은 −Z 기준이라 180° 돌린다.
- 키는 VRM 그대로(1.0~2.2m 밖이면 1.6m). 팔 길이(어깨~손목)를 재서 낚싯대 쥔 손 자리 표를 기준 팔(0.453m) 대비로 늘이거나 줄인다 — 키 큰 모델도 낚싯대가 손에서 떨어지지 않는다.

## 4. 데이터로 배정: `characters.json` 덮어쓰기 규칙

```json
{
  "playerMale": "HairSample_Male.vrm",
  "playerFemale": "Sendagaya_Shino.vrm",
  "npcMale": ["Sakurada_Fumiriya.vrm", "Base_Male.vrm"],
  "npcFemale": ["AvatarSample_D.vrm", "..."],
  "npc": [{ "name": "윤슬", "file": "AvatarSample_F.vrm" }]
}
```

- 게임 폴더(`StreamingAssets/Characters`)의 파일이 기본, 문서 폴더(`문서/짭풍명월/Characters`)에 같은 파일이 있으면 **적은 항목만 덮는다** (`npc`는 합치고 같은 이름은 문서 폴더가 이긴다). → 게임을 다시 깔아도 내 설정이 남는다.
- id 규칙: `builtin/파일.vrm`(게임 폴더) · `user/파일.vrm`(문서 폴더) · `pack/파일.vrm`(exe 옆 추가 캐릭터 폴더). 같은 이름의 `.vrm`을 문서 폴더에 넣기만 해도 기본 캐릭터가 바뀐다.
- NPC 고르기: 이름 지정 → 성별 목록에서 **이름의 FNV 해시**로 고르기(실행마다 같다) → 다른 성별 → 기본 캐릭터. 깨진 JSON·없는 파일은 예외 대신 캐릭터 창 노란 경고.

## 5. 같은 얼굴이 덜 반복되게: 밝기 보존 재채색 (`NpcLooks`)

남자 모델이 셋뿐이라 NPC가 쌍둥이처럼 보였다. 이름 해시로 **머리색 9 · 상의 10 · 하의 6 · 키 ±6%**를 고른다.

1. VRoid 재질 이름 규칙(`…_HAIR`, `…_Tops_…_CLOTH`, `…_Onepice_…`, `…_Bottoms_…`)으로 부위를 찾는다.
2. 원본 텍스처를 `Recolor` 셰이더로 렌더 텍스처(최대 1024, 밉맵)에 다시 그린다: **휘도만 남기고 새 색을 곱한다** → 명암·머리결·옷 주름은 그대로.
3. 원본의 평균 밝기(16×16로 줄여 한 번만 GPU→CPU로 읽음)가 새 색이 되게 맞춘다 → 원래 검은 머리도 밝은 색으로 바뀐다.

## 6. 다른 VRM의 옷 입히기 (`VrmOutfit`)

VRoid 모델은 몸 메쉬 하나(`Body`)에 피부·상의·하의·신발이 **재질별 서브메쉬**(`…_SKIN`, `…_CLOTH`)로 들어 있고, 뼈 이름(`J_Bip_*`)이 모델끼리 같다. 이걸 이용한다.

```
입는 사람: Body 메쉬 복사본에서 _CLOTH 서브메쉬의 삼각형을 비워 숨긴다
옷 주인:   _CLOTH 서브메쉬만 떼어 새 SkinnedMeshRenderer로
          bones[] = 옷 주인 뼈 이름 → 입는 사람의 같은 이름 뼈
          bindposes = 옷 주인 것 그대로
```

- 바인드포즈를 옷 주인 것으로 두면 정점이 **각자 뼈 기준 자리**를 지킨다 → 키·팔 길이가 달라도 소매·바짓단이 손목·발목을 따라간다(리타기팅 없이).
- 대기실 [옷 바꾸기]는 가진 VRM 중 무작위, 또는 `characters.json`의 `outfitUrls`에서 웹으로 받아(`.vrm` 또는 `.vrm`이 든 `.zip` — 첫 바이트로 구분) 입힌다. 고른 옷은 세이브에 남는다.

## 7. 남이 만든 모델의 이용 조건

- VRM 메타의 **아바타 사용 허락**(만든 사람만/허락받은 사람만/누구나)·**표기 필요**를 캐릭터 창에 보여 준다. 막지는 않는다 — 본인이 만든 모델일 수 있다.
- 추가 캐릭터 폴더(exe 옆 `Characters`)는 `파일.vrm` + 같은 이름 `파일.json`(이름·성별·만든 사람·출처·라이선스)이 **둘 다** 있어야 목록에 나온다. 재배포 허락이 애매한 모델을 저장소·빌드 밖에서만 쓰게 하는 장치다.
- 모델 밝기 배수(`brightness`)로 흰 머리·흰 옷 모델이 밤에 혼자 하얗게 뜨는 것을 맞춘다(MToon `_Color`·`_ShadeColor`·`_EmissionColor`·`_MatcapColor`에 곱함).

## 8. 확인 도구

- `sh tools/showcase.sh [pack|builtin|user|all]`: VRM을 하나씩 갈아입히며 서기·걷기·달리기·캐스팅·파이팅·물고기 들기·환호를 찍고, `tools/showcase_sheet.py`가 캐릭터마다 한 장으로 모은다.
- 자동 촬영 `02v_vrm_35`·`_82`·`_155`·`02v_vrm_front`(낚싯대 각도별 팔 IK), `21e_character_model`(캐릭터 창).
