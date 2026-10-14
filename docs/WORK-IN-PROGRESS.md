# 진행 상황 메모

세션이 끊겨도 이 파일만 보고 이어서 작업할 수 있게 남기는 메모. 끝난 일은 로드맵·각 문서에 합쳤다.

## 지금 상태 (2026-10-14)

문서(06 로드맵, 08 퀄리티 계획, 09 아트 계획, 이 파일)에 적어 둔 할 일은 아래 "남은 것"을 빼고 모두 끝났다. 기능 단위로 커밋·푸시했다.

| 묶음 | 내용 | 문서 |
|---|---|---|
| 맵 3개 | 한강(Lv.1), 홍현 앞바다(남해, Lv.5), 서천 앞바다(서해, Lv.6, 물때·갯벌) | 02 9장·9-6·9-7 |
| 어종 41종 + 잡동사니 6종 | 어종별 Blender 모델·텍스처·아이콘, 제철, 어군 전용(그루퍼·민어), 먼바다 전용(다금바리·돗돔), 겨울 빙어 | 02 11장, 09 3장 |
| 낚시법 | 찌, 루어(감기·저킹), 플라이, 트롤링, 겨울 얼음 구멍 | 02 4-1, 10-1 |
| 배 4종 | 고무보트, 돛단배(바람), 고속보트, 낚시 크루저, 배마다 흔들림, 먼바다 | 02 9-2 |
| 성장·수집 | 강화 +15, 해체 미끼, 의상 25벌, 도감, 이야기 퀘스트 18개, 일일 의뢰, 대물 도전, 업적 26개, 소라고동 교환소, 수족관 | 02 7·8·13장 |
| 대회 | 조합 낚시 대회 (AI 7명, 미션 점수, 엠블럼, 거거 낚시대·나팔고둥 릴) | 02 13-1 |
| 계절 | 한 달 = 게임 7일, 눈, 겨울 한강 얼음 | 02 10-1 |
| 화면 | 진짜 옷 메쉬 캐릭터(반장갑·운동화 골·남방 깃), 후처리, 밤 물빛, 흐림 층구름, 1인칭 팔·장갑, 아이템 아이콘 | 08, 09 |
| 소리 | 절차적 낚시 소리 + Kenney CC0 UI 클릭·경고·발소리 | 03 |
| 편의 | 옵션(O)·키 설정, 패드 진동, 단위 테스트 25개, 밸런스 시뮬레이터 | 04, 05 |

## 남은 것

### 사용자가 해야 할 일 (계정 로그인이 필요해서 Claude가 못 한 것)

공개 저장소라 에셋 스토어·Mixamo 에셋은 받아도 `.gitignore`된 폴더(예: `Assets/AssetStore/`)에 두고 커밋하지 않는다(08 문서 5장).

| 에셋 | 어디서 | 넣으면 좋아지는 것 | 받은 뒤 할 일 |
|---|---|---|---|
| **Human Basic Motions FREE** (Kevin Iglesias) | [Asset Store](https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271) | 지금 주먹 쥔 슈퍼히어로 대기 자세 → 자연스러운 대기·걷기·뛰기 | `CharacterAnimator`가 두 번째 클립 라이브러리에서 Idle/Walk/Run을 찾게 (08 4장 1) |
| **AllSky Free** | [Asset Store](https://assetstore.unity.com/packages/p/allsky-free-10-sky-skybox-set-146014) | 흐린 날 하늘 큐브맵 (지금은 절차적 층구름으로 대신함) | 흐림·비일 때 `DayNightCycle`에서 큐브맵 섞기 |
| **Stylized Hand Painted Nature Pack Free** | [Asset Store](https://assetstore.unity.com/packages/3d/vegetation/trees/stylized-hand-painted-nature-pack-free-177775) | 한강 둔치·홍현 나무·풀 (지금 Kenney 나무는 단순) | `KenneyProps`의 나무 배치를 교체 |
| 국가유산청 전통 배 등 무료 에셋 | [국가유산청 에셋 예시(운현궁)](https://assetstore.unity.com/packages/3d/props/khs-unhyeongungcollect-273241) → 판매자 이름 눌러 무료 목록에서 배 찾기 | 서해·남해 배경 배 | 배경 소품 |
| **Mixamo** 동작 (Adobe 계정) | [mixamo.com](https://www.mixamo.com/) | 던지기·당기기·앉아 있기 낚시 동작 | FBX for Unity, Without Skin → 휴머노이드 리타깃 |
| (선택) **VRoid Studio** 시험 모델 | [VRoid Studio](https://vroid.com/en/studio), [UniVRM](https://github.com/vrm-c/UniVRM/releases) | 원작 '청'·'명' 같은 한국 MMO풍 얼굴 | UniVRM으로 임포트해 지금 캐릭터와 비교 (08 5장) |

- 그 밖에: 예전 스크립트 실수로 생긴 빈 폴더 `C:\Screenshots\fish`는 지워도 된다(보안 규칙 때문에 Claude가 못 지움).

### 다음 개발 후보

- **제주**(원작 이벤트 지역) · 헌화로 · 거문도 같은 국내 맵 (큰 작업: `FieldMapDef` + 지형 + 어종 + 퀘스트, 03 문서 "새 맵 추가")
- 다리·데크를 모델로 (지금은 프리미티브 + 텍스처)
- 피부 텍스처 팔 근육 흐리기 (가까이 보는 화면이 생기면, 08 4장 8)

## 참고
- 빌드 `sh tools/build.sh` → 확인 `sh tools/autoshot.sh` (Screenshots/ 61장) → 테스트 `sh tools/test.sh` → 밸런스 `sh tools/balance.sh -simSummary`
- Blender 파이프라인: `bpyenv/Scripts/python -I tools/blender/<build_characters|build_fish|build_icons>.py` (05 문서)
- 단축키: F 대화, I 가방, C 의상, M 지역 지도, B 배, O 옵션, L 퀘스트(탭 4개), Z 살림망(해체), P 도감(맵 탭)
