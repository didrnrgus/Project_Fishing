# 진행 중인 작업 (v0.2 다듬기)

세션이 끊겨도 이 파일만 보고 이어서 작업할 수 있게 남기는 메모. 끝나면 지우거나 로드맵에 합친다.

## 사용자 요청 (2026-10-13)
- 원작 참고 자료로 더 다듬기: 물결, 낮/해 뜨고 지기/달, 사람(에셋 활용), 낚시터 목록(한국 지도), 대기실
- 참고 영상
  - 9lCHAypXClQ (20일차 플레이 2시간 41분): 낮·노을·밤 바다, 보트, 홍현 앞바다 지역 지도(격자 A~H/1~8), 의상 상점, 장력 위험 시 빨간 화면 테두리
  - Asl68YV0z2M (앙코르와트), jHCA07QMYO0 (산토리니): 보트 물보라 꼬리, 밤바다 물결
  - NDxZf10CZAY (그래픽): 서천 앞바다 일몰, 한강의 밤(다리 조명이 물에 길게 비침), 흐린 날 영흥도(회색 안개), 태안 앞바다 맑은 날(뭉게구름, 청록 바다)
  - 2Mvir9jZ-28: 큰 어군에서 3마리 잡으면 사라지고 작은 어군이 나옴, 잡은 고기로 미끼 만들기
  - fw1GiQ2KFmQ (대기실): "한국 낚시 조합" 실내(목재, 박제 물고기, 지도, 책장, 지구본), 캐릭터 가운데, 오른쪽 아래 버튼 [낚시터 선택하기][퀘스트][납품][거점 이동하기]
  - 사용자 제공 이미지: 낚시터 목록(KOREA 지도, 한강/현화로/삼척 앞바다/청진포구/동해 등대섬/서천 앞바다/영흥도/태안반도 연안/홍련 앞바다/욕지도/거문도, MAP INFO 도감 달성도, FISH INFO 시즌 기록, 방 목록 CH.1~, 빠른입장)

## 에셋 (완료: 받아서 프로젝트에 넣음, 아직 코드에서 안 씀)
- 출처: Quaternius itch.io 무료(Standard) 버전, CC0 — 사용자 허락 받음 (구글 드라이브는 한도 초과로 실패)
- `Assets/Resources/Characters/Superhero_Male_FullBody.fbx`, `Superhero_Female_FullBody.fbx` (휴머노이드, 키 약 1.9m, 렌더러 3개, 뼈 이름 Unreal식: pelvis, spine_01~03, neck_01, Head, hand_r ...)
  - 속옷 차림 베이스 → 옷은 코드로 입힌다(뼈 가중치로 버텍스 컬러 마스크를 만들고 전용 셰이더로 셔츠/바지/신발 색)
  - 텍스처: `Characters/Textures/T_Superhero_Male_Ligh.png`, `_Dark`, 여성 Light/Dark, `T_Eye_Brown.png`
- 머리: `Characters/Hair/*.fbx` (Head 뼈에 리깅된 SkinnedMesh, 같은 65개 뼈 → 이름으로 다시 연결), 텍스처 `T_Hair_1/2_BaseColor.png`
- 애니메이션: `Resources/Animations/UAL1_Standard.fbx` 43클립 (Idle_Loop, Walk_Loop, Jog_Fwd_Loop, Sprint_Loop, Idle_Torch_Loop, Sword_Attack, Spell_Simple_Shoot, Interact, PickUp_Table, Sitting_Idle_Loop, Idle_Talking_Loop, Driving_Loop ...)
- 물고기: `Resources/Fish/Fish1~3.fbx` (레거시, Swim 루프, 단위가 cm라 0.01배, 머티리얼 3개)
- 임포트 규칙: `Assets/Editor/AssetImportRules.cs`, 확인 도구: `Fishing > Inspect Imported Assets` → `Logs/asset-inspect.txt`
- 라이선스: `Assets/ThirdParty/Quaternius/*.txt`

## 남은 작업 (순서대로)
1. [x] 캐릭터: Quaternius 모델 + 옷 셰이더(셔츠·조끼·바지·신발) + 머리·모자·이름표/칭호, Playables 애니메이션(대기·걷기·조깅·낚싯대 들기·캐스팅·챔질·환호), 낚싯대는 hand_r 추종
2. [x] NPC: 뗏목 2명, 둔치 3명, 낚시점 김사장 (NpcAngler)
3. [x] 파이팅 물고기: Fish1~3 모델 + 어종 색 (FishModels). 결과 화면은 아직 2D
4. [x] 물: Fishing/Water 셰이더(버텍스 파도 + 흐르는 절차적 노멀맵 2장), 격자 수면 메쉬
5. [x] 하늘: 구름 돔(날씨별 양, 노을색), 달(태양 반대편), 별(포인트 메쉬). 밤 강물에 비치는 도시 불빛은 미구현
6. [x] 대기실(한국 낚시 조합) + 낚시터 목록(한국 지도, 한강만 열림) + 채널 목록/빠른입장, 필드에서 대기실로 나가기. 박제 물고기 크기 버그(FishModels 길이 100배) 수정
7. [x] 지역 지도(M, AreaMapPanel), 장력 위험 빨간 테두리(FightHudView.UpdateVignette)
8. [x] 큰 어군/작은 어군(3마리 잡으면 작아짐, 6시간 뒤 회복), 작은 고기(20cm 미만)로 생미끼 만들기
9. [x] 빌드 → 자동 스크린샷 확인 → 문서 갱신 → 커밋/푸시

남은 것은 [06-roadmap.md](06-roadmap.md)의 v0.1.2 미완료 항목(밤 불빛 반사, RESULT 3D 물고기)으로 옮겼다.
