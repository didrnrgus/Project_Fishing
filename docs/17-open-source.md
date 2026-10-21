# 17. 공개 준비: 재배포 금지 에셋 걷어내기와 라이선스 (2026-10-21)

사용자 요청: 저장소를 공개해서 남이 포크해 쓸 수 있게 하되, 유료로 파는 건 막고 싶다.

에셋 스토어와 Mixamo 약관은 **게임에 넣어 배포하는 건 허락하지만 원본 파일을 남이 받을 수 있게 올리는 건 막는다**(무료 에셋도 같다).
그래서 공개 저장소에 둘 수 없는 에셋을 모두 직접 만든 것이나 CC0·MIT 자료로 바꿨다. 예전 커밋 기록에는 원본이 남아 있어서 공개용 저장소는 기록을 새로 시작한다(아래 5장).

## 1. 바꾼 것

| 예전 (재배포 금지) | 지금 | 다시 만들기 | 문서 |
|---|---|---|---|
| Mixamo 동작 7개 + 받은 동작 약 45개 | 직접 만든 반응 동작(환호·박수·펄쩍·손짓·움찔) + 낚싯대 팔 IK | `bpyenv/Scripts/python -I tools/blender/build_motions.py` | 10 문서 1~2장 |
| Human Basic Motions FREE (Kevin Iglesias) | 직접 만든 기본 동작(대기·걷기·뛰기·질주·말하기·점프·낙하·제자리 돌기·낚시꾼 자세), 바탕은 Quaternius UAL1(CC0). 대기·걷기·뛰기·점프는 같은 날 Unity Starter Assets(Unity Companion License)로 다시 바꿈 | 같은 스크립트, `StarterClipBake` | 10 문서 7장 |
| AllSky Free · Fantasy Skybox FREE | Poly Haven 땅 없는 하늘 사진(CC0), 맑은 날은 해를 지움 | `bpyenv/Scripts/python -I tools/sky/build_sky.py` | `Resources/Sky/SOURCES.md` |
| EasyChart | `UI/UIChart.cs`: UGUI 메쉬로 직접 그리는 선·막대·도넛·레이더 (같은 API) | — | 아래 2장 |
| Translucent Image + Paraform (Le Tai, 유료) | `UI/UIShape.cs` + 셰이더 `Fishing/UIShape`(둥근 도형·테두리·바깥 빛·그림자·가장자리 광택) + `UI/UIBlurSource.cs`(뒤 화면 흐림) | — | 아래 3장 |
| Conifers [BOTD] · Realistic Tree 9·10 · Nature Starter Kit 2 · BigRook Nature Pack | ez-tree(MIT)로 만든 침엽수 3·활엽수 4·작은 나무 4·덤불 6 | `bpyenv/Scripts/python -I tools/blender/build_vegetation.py` | 아래 4장 |
| Rock and Boulders 2 | Poly Haven 바위 9종(CC0), 눈 판은 셰이더 `Fishing/Rock` `_Snow` | 같은 스크립트 | 아래 4장 |
| Grass Flowers Pack Free | Poly Haven 풀 2·들꽃 5를 찍은 카드 그림(CC0) | 같은 스크립트 | 아래 4장 |

남은 서드파티는 모두 재배포 가능한 것(UniVRM MIT, ez-tree MIT, Quaternius·Kenney·Poly Haven·VRoid 샘플 CC0, 글꼴 OFL·잘난체 라이선스, TextMesh Pro) → [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).
로컬에만 있던 `Assets/MixamoCharacters/`(예전 장식 NPC용 Mixamo 사람, 안 씀)는 .gitignore에 넣었다 — 지워도 된다.

## 2. 차트 (`UIChart`)

- 예전 EasyChart는 UI Toolkit 차트를 렌더 텍스처에 그려 RawImage로 넣었다. 지금은 `ChartHost`(데이터·글자) + `ChartGraphic : MaskableGraphic`(메쉬)로 직접 그린다.
- 부르는 쪽 API는 그대로: `UIChart.Line/Bars/Donut/Radar`, `SetValues/SetSlices/SetCategories`, `ChartHost.Profile.series`·`Serie()`·`Refresh()`.
  세로축 글자 형식은 `Profile.yFormat`(예전 `axes[...].labelFormat`).
- 그리는 것: 4칸 점선 격자 + 세로축 깔끔한 최대(1·2·2.5·5·10 × 10ⁿ), 부드러운 선(Catmull-Rom) + 옅은 면, 윗모서리 둥근 막대 + 값, 조각 사이 2px 띄운 도넛 + 오른쪽 범례, 레이더 4겹 + 축 글자.
- `Refresh()`하면 지금 보이는 값에서 새 값으로 0.45초에 바뀐다.
- 함정: 그래픽 자식은 차트를 꽉 채우지만 기준점(pivot)이 차트와 달라서, 메쉬 원점을 그래픽 자기 사각형 왼쪽 아래(`rectTransform.rect.min`)로 둬야 한다(차트 pivot으로 했더니 살림망 도넛이 반 칸 내려가 글자와 겹쳤다).

## 3. 유리 판 (`UIShape`·`UIBlurSource`)

- `UIShape : Image`라서 버튼 `targetGraphic`·마스크·레이캐스트가 그대로 된다. 예전 `TranslucentImage`를 쓰던 곳은 이름만 바꿨다.
- 메쉬는 사각형 하나를 그림자·바깥 빛 폭만큼 넓히고, 셰이더가 둥근 상자 거리(SDF)로 채움·안쪽 테두리·바깥 빛·그림자·위쪽이 밝은 가장자리 광택을 한 번에 그린다.
  정점 채널: uv1 = 중심 기준 위치·반폭, uv2 = 반지름·광택·테두리·빛 폭, uv3 = 그림자, tangent = 테두리 색, normal.x = 유리 판 색 진하기 → 캔버스에 Normal·Tangent 채널을 켠다(`UIFactory.CreateCanvas`).
- 유리(`Glass`·`BlurBackdrop`)는 메인 카메라 `UIBlurSource.OnRenderImage`가 화면을 1/4로 줄여 Kawase 흐림 5번 → 전역 `_UIBlurTex`. 셰이더가 화면 좌표로 읽어 채도를 올리고 대비를 줄이고 조금 어둡게 한 뒤 판 색으로 덮는다.
- 테두리는 하나(다시 `AddBorder`하면 바뀐다), 그림자도 하나. 셰이더 `Fishing/UIShape`·`Hidden/Fishing/UIBlur`는 그래픽 설정 Always Included.

## 4. 나무·덤불·바위·풀 (`build_vegetation.py` → `VegetationBake`)

- **나무·덤불**: 사용자가 [ez-tree](https://github.com/dgreenheck/ez-tree)를 제안했다(가볍다). `tools/eztree/dump.mjs`가 Node로 프리셋 형상(가지 = 관, 잎 = 십자로 겹친 사각형)을 JSON으로 뽑는다(브라우저 없이 — 텍스처 로더가 만드는 `<img>`만 흉내).
  처음 한 번 `cd tools/eztree && npm ci`(빌드 스크립트가 없으면 알아서 한다). Blender가 메쉬로 바꾸고 잎 법선을 수관 가운데에서 바깥으로(부드러운 수관 음영), 가지는 덤불 3천·나무 6천 삼각형까지 줄인다.
  먼 거리 LOD1은 Blender로 앞에서 찍은 그림을 십자로 세운 판 두 장.
- Poly Haven 나무도 먼저 해 봤지만 하나에 170만~1,700만 삼각형(소나무 FBX 650MB)이라 잎을 사각형으로 바꿔도 무겁고 손이 많이 가서 ez-tree로 바꿨다. 사용자: 특별한 나무 몇 그루는 괜찮지만 너무 무겁게는 말 것.
- **바위**: Poly Haven 모델에 든 LOD 중 목표 삼각형(3천~5천)에 가까운 것 + 1/5로 줄인 LOD1. 법선 맵이 EXR이면 Blender로 PNG로.
- **풀·들꽃**: Poly Haven 모델은 크기가 다른 포기 여러 개가 한 줄로 들어 있어서 가장 큰 포기 하나만 찍는다(투명 배경 512px). 버뮤다 잔디는 알파 텍스처를 못 찾아 검은 판이 돼서 뺐다.
- 출력 `Assets/Vegetation/<종류>/<이름>.fbx` (오브젝트 `LOD0_bark`·`LOD0_leaves`·`LOD0_rock`·`LOD1_billboard`·`LOD1_rock`), 껍질·잎 텍스처는 종류마다 하나(`Textures/`).
- `Editor/VegetationBake`(빌드 전): 재질(껍질·바위 `Fishing/Rock`, 잎·십자 판 `Fishing/Foliage` — 양면·알파 자르기·투과·위쪽일수록 바람), LODGroup 프리팹(`Assets/Vegetation/Prefabs`), 눈 바위(`_snow`),
  덤불·작은 나무 사본(`Resources/Plants/bush01~06·tree01~04`, `KenneyProps`가 이름으로 찾는다), `Resources/VegetationLibrary.asset`.
- 무게: 침엽수 1만 3천~1만 6천, 활엽수 8천~1만 7천, 덤불 4천~8천, 바위 3천~5천 삼각형 (LOD0). 빌드 1738 → 1543MB.

## 5. 라이선스와 공개 저장소

- 직접 만든 것: **CC BY-NC-SA 4.0** ([LICENSE](../LICENSE), 법률 문서 전문 포함) — 포크·수정·공유 가능, 상업적 이용 금지, 고친 것도 같은 라이선스, 출처 표시.
  저작권자 표기: 제쓰로네 게임공방 (Jethro's Game Company, github.com/didrnrgus).
- 서드파티: [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md). 잘난체2는 "저작권 안내·라이선스 전문을 함께 넣으면 번들 재배포 가능, 글꼴 단독 판매·개작 재배포 금지" → 사본 `Assets/TextMesh Pro/Fonts/Jalnan2-LICENSE.txt`.
- 원작 「청풍명월」 IP는 이 라이선스와 별개(비영리 팬 프로젝트 문구).
- 기록: 지금 비공개 저장소의 예전 커밋에는 에셋 스토어·Mixamo 원본이 있다 → 공개는 기록을 새로 시작한 저장소로 한다(비공개 저장소는 그대로 백업).
- Git LFS: 그림·모델·VRM이 LFS라 포크·클론할 때 원 저장소의 LFS 대역폭을 쓴다.

## 6. 바뀐 점·남은 것

- 캐릭터 창의 걸음걸이 남/여: 예전에는 Human Basic Motions의 남·녀 클립을 골랐는데 지금 기본 동작(Unity Starter Assets)은 하나라 같은 걸음이다(설정은 그대로 남아 있다). 여자 걸음을 따로 만들면 `Basic_Walk_Loop_F`처럼 이름 끝 `_F`로 넣으면 된다(`CharacterAnimator.TryGetClip`).
- 확인: 자동 촬영 전체(`sh tools/autoshot.sh`) — 걷기·뛰기 `00d`·`00e`, 대기 `00f`, 나무 `00g_park_trees`, 반응 동작 `02r_*`, 낚싯대 팔 `02s`·`02t`·`02v`, 차트 `10_result`·`11_dex`·`12_keepnet`, 흐린 하늘 `15b_cloudy_day`, 눈 바위 청진 장면.
