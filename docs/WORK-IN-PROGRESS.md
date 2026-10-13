# 진행 중인 작업 (v0.3 "얼티밋" 원작화)

세션이 끊겨도 이 파일만 보고 이어서 작업할 수 있게 남기는 메모. 끝나면 로드맵·각 문서에 합친다.

## 사용자 요청 (2026-10-13)
- Gemini가 준 「청풍명월 사전」 7개 파일(Downloads/ChungPoongMyungWol_Data)을 참고하되, **더 찾아서 진짜 원작처럼 "얼티밋하게"**.
- 검증 결과: [07-original-data.md](07-original-data.md) — 확인된 것은 원작대로, 근거 없는 것(강화 실패 시 장비 파괴, 낚싯바늘 행동력 등)은 쓰지 않거나 "원작풍"으로만.
- 맵 범위: 한국만 (해외는 아직).

## 완료 (커밋됨)
1. [x] 원작 데이터 검증 문서 (07-original-data.md, 출처 포함)
2. [x] 맵 시스템 일반화(FieldMapDef/FieldMap/MapLoader) + **홍현 앞바다**(A~H×1~8, 방파제·등대·테트라포드·갯바위 섬·다랭이 마을·다도해), 바다 어종 17 + 자이언트 그루퍼(저녁 C4/아침 E3), 바다 미끼, 원작 잡동사니(통발·상자·생선 캔·로봇 물고기), Lv.5 고무보트 지급·홍현 입장, 헌화로 표기 수정, 바다 이야기 퀘스트 5개
3. [x] 해체(해체 미끼 5등급·반짝이는 비늘·별모래), 낚싯대·릴 +15 강화(원작 인터뷰 규칙), 가방(I)
4. [x] 의상(C) 25+종, 부위별 능력치, 전신 미리보기, 모자 3종·선글라스 모델
5. [x] 일일 의뢰·대물 도전·업적 17개·칭호, 소라고동, 소라고동 교환소
6. [x] 수족관 (대기실 수조에서 실제로 헤엄침, 먹이·배고픔·장식, 대기실 [수족관] 버튼)

## 캐릭터 퀄리티 (2026-10-13, main에 머지됨)
- 보통 체형 + 진짜 옷 메쉬 캐릭터 시제품, Unity 미확인. **[08-quality-plan.md](08-quality-plan.md) 2장부터** 데스크톱에서 확인.

## UI 전면 교체 · 연출 (2026-10-14, 커밋됨)
- 다크 유리 UI(Translucent Image + Paraform), EasyChart 차트(결과·도감·살림망·강화·의상), Jalnan2 TMP 글꼴.
- 원작 튜토리얼 영상(youtube sAL6RQI92T0)을 1초 단위로 보고 캐스팅·챔질·파이팅·랜딩 연출 추가 → [03-architecture.md](03-architecture.md) "연출" 표.
- 다음 후보: **지형을 Unity Terrain으로** (해변·섬·갯바위 경사, 모래/풀/바위 텍스처 칠하기, 바위 메쉬). 홍현 앞바다부터 추천 — 사용자에게 범위 확인 중.

## 아트 계획 (2026-10-14 기록, 아직 시작 안 함)
- 사용자가 **물고기 도감 자료를 보여 주면** 시작한다. 잉어 한 마리 시제품(모델·텍스처·헤엄)을 먼저 렌더해 보여 주고 방향을 확인받는다.
- 그다음 아이템 아이콘: 실물 사진을 검색해 참고 → 설계 시트 → Blender로 모델링·렌더 → 게임에 적용.
- 상세: [09-art-plan.md](09-art-plan.md)

## 남은 작업 (다음 세션, 순서대로)
7. [x] **대회** (완료: 02-game-design 13-1) (원작 확인됨: NPC로 참가, 하루~이틀마다 정해진 시간, 미션 합산 포인트 = 가장 많이·가장 길게·길이 합계, 누적 포인트로 계급 엠블럼, 1~3위 보상 "거거 낚시대"·"나팔고둥"(릴)).
   - 솔로판: 서천 앞바다 대신 지금 맵에서, AI 낚시꾼 7명 점수 시뮬레이션, 대회 시간 HUD 순위표, 엠블럼(브론즈~다이아), 거거 낚시대(Lv.10 rod)·나팔고둥 릴(Lv.10 reel)을 보상 전용 장비로 GearDatabase에 추가
8. [ ] 밸런스: `sh tools/balance.sh -simSummary`로 바다 어종·그루퍼 확인, 강화 +15·의상 데미지 반영 후 docs/04-balance.md 갱신
9. [ ] 문서 갱신: README(특징·조작 F/I/C/M/B·스크린샷), 02-game-design(맵 2개·해체·강화·의상·퀘스트·수족관·대회), 03-architecture(FieldMap, MapLoader, 새 패널·데이터 파일), 05-dev-guide(자동 스크린샷 13b~29), 06-roadmap
10. [ ] 그 밖에 보이는 것: 1인칭 팔·손 모델 다듬기, 밤 강물 다리 불빛 반사, 결과 화면 3D 물고기, 다른 한국 낚시터(서천 앞바다 = 대회 장소, 헌화로, 거문도 …)

## 참고
- 빌드: `sh tools/build.sh`, 확인: `sh tools/autoshot.sh` (Screenshots/ 33장, 홍현·수족관·의상·퀘스트 포함)
- 새 파일: World/FieldMap.cs, World/HonghyeonMap.cs, World/Aquarium.cs, World/KenneyProps.cs, Core/MapLoader.cs,
  Data/RodUpgrade.cs(장비 강화), Data/ItemDatabase.cs, Data/CostumeDatabase.cs, Data/AchievementDatabase.cs,
  UI/AreaMapPanel.cs, UI/DialogPanel.cs, UI/UpgradePanel.cs, UI/InventoryPanel.cs, UI/CostumePanel.cs, UI/ExchangePanel.cs, UI/AquariumPanel.cs, UI/PortraitCamera.cs
- 단축키: F 대화, I 가방, C 의상, M 지역 지도, B 배, L 퀘스트(탭 4개), Z 살림망(해체), P 도감(맵 탭)
