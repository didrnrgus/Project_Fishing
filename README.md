# 짭풍명월 — Project Fishing

제작: **제쓰로네 게임공방** (Jethro's Game Company)

피망 온라인 낚시 게임 **「청풍명월」**(2012~2021, 서비스 종료)의 손맛을 **혼자서** 즐기기 위해 Unity로 다시 만드는 프로젝트입니다.
원작 소스나 리소스는 쓰지 않았고, 공개된 기사·플레이 후기·플레이 영상에서 게임 메커니즘만 참고했습니다.

원작 낚시터 목록의 국내 낚시터 11곳(한강 · 홍현 앞바다 · 서천 앞바다 · 영흥도 · 태안반도 · 삼척 앞바다 · 동해 등대섬 · 헌화로 · 욕지도 · 청진포구 · 거문도)과 원작 이벤트 지역 **제주**에서 원작 한국 코스(Lv.1~25)를, 원작 해외 거점 전체 — **동남아 12곳**(하롱베이 · 앙코르와트 · 빈펄 · 돈콘 · 푸켓 · 방콕 · 코사무이 · 랑카위 · 센토사 · 텔록 바텍 · 플라우 티오만 · 플라우 탱글), **지중해 4곳**(산토리니 · 크레타 · 자킨토스 · 듀덴 폭포) + 솔로판의 **나일강 하류**에서 그 뒤(Lv.25~40)를 즐길 수 있습니다. 여기에 원작에 없는 **탄천**(성남 야탑, 도심 하천 · Lv.3)을 더해 모두 **30곳**, 어종 **224종**.
조작과 화면 구성은 원작 플레이 영상을 프레임 단위로 분석해서 맞췄습니다.

| 인트로: 제쓰로네 게임공방 | 타이틀: 짭풍명월 |
|---|---|
| ![company](docs/images/00_intro_company.png) | ![title](docs/images/00_intro_title.png) |
| **대기실 (한국 낚시 조합, 원작 방 그대로)** | **낚시터 목록** |
| ![lobby](docs/images/00a_lobby.png) | ![areas](docs/images/00b_area_list.png) |
| **캐릭터 (VRM, 기본 주인공 여자)** | **의상 (능력치만 — 몸에 보이는 장비는 낚싯대뿐)** |
| ![character](docs/images/00_character.png) | ![costume](docs/images/21c_costume.png) |
| **달리기 (Unity Starter Assets 동작)** | **점프** |
| ![run](docs/images/00e_run.png) | ![jump](docs/images/00e2_jump.png) |
| **다른 VRM으로 갈아입고 낚시 (기본 제공 11명 + 내가 만든 .vrm, 빌드 없이)** | **캐릭터 모델 고르기 (VRoid 등 .vrm, characters.json)** |
| ![vrm](docs/images/02v_vrm_155.png) | ![vrm_panel](docs/images/21e_character_model.png) |
| **홍현 앞바다 (남해)** | **서천 앞바다 (서해, 물때·갯벌)** |
| ![honghyeon](docs/images/22_honghyeon_day.png) | ![seocheon](docs/images/35_seocheon_lowtide.png) |
| **겨울 한강 얼음낚시** | **낚시 크루저 (먼바다)** |
| ![winter](docs/images/41_winter_ice.png) | ![cruiser](docs/images/40_seocheon_cruiser.png) |
| **삼척 밤바다 (오징어배 집어등)** | **욕지도 가두리 낚시터** |
| ![samcheok](docs/images/45b_samcheok_night.png) | ![yokji](docs/images/48_yokji_farm.png) |
| **동해 등대섬** | **청진포구 명태 덕장** |
| ![deungdae](docs/images/46_deungdae_lighthouse.png) | ![cheongjin](docs/images/49b_cheongjin_racks.png) |
| **제주 서귀포 (돌하르방·한라산)** | **제주 주상절리·외돌개** |
| ![jeju](docs/images/54_jeju_harbor.png) | ![columnar](docs/images/54b_jeju_oedolgae.png) |
| **낚시터 목록 (한국·동남아·지중해·아프리카 거점 탭)** | **하롱베이 (석회암 봉우리·수상 마을)** |
| ![areas_world](docs/images/53_area_list_all.png) | ![halong](docs/images/55_halong_bay.png) |
| **산토리니 (칼데라·방파제 테트라포드)** | **나일강 하류 (피라미드 노을)** |
| ![santorini](docs/images/56b_santorini_caldera.png) | ![nile](docs/images/57b_nile_sunset.png) |
| **앙코르와트 (해자 건너 연꽃 봉오리 탑·사자상)** | **방콕 (강 건너 새벽 사원 왓 아룬)** |
| ![angkor](docs/images/58b_angkor_lotus.png) | ![bangkok](docs/images/62_bangkok_watarun.png) |
| **빈펄 (바다 위 초가 정자·케이블카)** | **푸켓 (팡아만 석회암 섬·하얀 대불)** |
| ![vinpearl](docs/images/59b_vinpearl_gazebo.png) | ![phuket](docs/images/61b_phuket_buddha.png) |
| **랑카위 (독수리 광장)** | **센토사 (마리나 야경·열기구)** |
| ![langkawi](docs/images/64_langkawi_eagle.png) | ![sentosa](docs/images/65b_sentosa_skyline.png) |
| **크레타 하니아 (베네치아 항구·하얀 산)** | **듀덴 폭포 (바다로 떨어지는 폭포)** |
| ![crete](docs/images/69b_crete_mountains.png) | ![duden](docs/images/71_duden_falls.png) |
| **탄천 (보행교·탄천종합운동장·롯데월드타워)** | **탄천 밤 (등 축제·다리 LED)** |
| ![tancheon](docs/images/72h_tancheon_footbridge.png) | ![tancheon_festival](docs/images/72g_tancheon_festival.png) |
| **탄천 징검다리** | **탄천 야경** |
| ![tancheon_stones](docs/images/72c_tancheon_stones.png) | ![tancheon_night](docs/images/72e_tancheon_night.png) |
| **코사무이 (바다 위 섬의 황금 대불)** | **자킨토스 (나바지오 난파선 해변)** |
| ![kohsamui](docs/images/63_kohsamui_buddha.png) | ![zakynthos](docs/images/70_zakynthos_navagio.png) |
| **플라우 티오만 (곡선 리조트 호텔·보라 파라솔)** | **플라우 탱글 (바다 위 데크 별장)** |
| ![tioman](docs/images/67b_tioman_beach.png) | ![tenggol](docs/images/68_tenggol_deck.png) |
| **대기실 [옷 바꾸기] (다른 VRM의 옷을 무작위로, 웹에서 받아 입기도)** | **NPC도 VRM (사람마다 머리·옷 색이 다르다)** |
| ![outfit](docs/images/00a2_outfit_random.png) | ![npc](docs/images/20_npc_dialog.png) |
| **한강 둔치 공원 (실사 활엽수·소나무·풀)** | **삼척 바닷가 (실사 바위·풀)** |
| ![park](docs/images/00g_park_trees.png) | ![samcheok](docs/images/45_samcheok_chuam.png) |
| **거문도 등대** | **홍현 갯바위 섬 (반복 없는 지형 텍스처·실사 바위)** |
| ![geomun](docs/images/50_geomun_lighthouse.png) | ![islet](docs/images/22c_honghyeon_island.png) |
| **방파제 (10m 타설 블록·1.3m 파라펫·계선주·사다리·물웅덩이)** | **홍현 방파제 (고래 벽화·타이어 방충재)** |
| ![breakwater](docs/images/43c_yeongheung_breakwater.png) | ![honghyeon_bw](docs/images/22h_honghyeon_breakwater.png) |
| **바닷가 마을 (칼라강판·기와 지붕, 벽에 들어간 창, 블록 담장, 전봇대)** | **베네치아풍 집 (깊은 아치 문, 비늘살 덧창, 반원 기와)** |
| ![village](docs/images/43e_yeongheung_village.png) | ![venetian](docs/images/69d_house_venetian.png) |
| **낚시점 (통유리 가게·간판·철골 캐노피·크릴 냉동고·깃발 배너)** | **삼척 밤 야광충 (벽을 따라 겹겹이 밀려오는 빛, 실물 크기 테트라포드)** |
| ![shop](docs/images/43g_shop_yeongheung.png) | ![bio](docs/images/45d_samcheok_corner_night.png) |

| 캐스팅 (당겼다 밀기) | 챔질 (찌 게이지) |
|---|---|
| ![casting](docs/images/02_casting.png) | ![bite](docs/images/04_bite_gauge.png) |
| **1인칭 파이팅 (휘는 낚싯대·릴, 카운터 방향 표시)** | **카운터 찬스** |
| ![fight](docs/images/06_fight.png) | ![counter](docs/images/07_counter_chance.png) |
| **RESULT (3D 물고기, 파이팅 기록 차트)** | **도감 (어종별 3D 모델)** |
| ![result](docs/images/10_result.png) | ![dex](docs/images/11b_dex_detail.png) |
| **RESULT 말풍선 토스트 · BONUS 보상 · 이벤트 어종 EXP+200%** | **놓치면 WARNING(내구도·줄 길이) + 재도전 말풍선** |
| ![rewards](docs/images/10f_result_rewards.png) | ![warning](docs/images/10g_warning_retry.png) |
| **배 소환 (바다 쪽에서 달려와 선다)** | **엔진 START/STOP [Y] — 꺼야 낚시** |
| ![summon](docs/images/18a_boat_summon.png) | ![engine](docs/images/18b_boat_engine.png) |
| **조합 낚시 대회** | **수족관** |
| ![tournament](docs/images/30_tournament_hud.png) | ![aquarium](docs/images/28_aquarium.png) |
| **나일농어 158cm 파이팅** | **조합 퀘스트 (이야기 45개, 스크롤)** |
| ![nile_fight](docs/images/58_nile_fight.png) | ![quest](docs/images/13_quest.png) |
| **NPC 퀘스트 완료 카드** | **납품 (게이지가 차면 다음 납품 두 배)** |
| ![quest_complete](docs/images/13u_quest_complete_card.png) | ![delivery](docs/images/12b_delivery.png) |
| **통발 설치·수거** | **내 캐릭터 (장비 6칸 · 최종 능력치)** |
| ![trap](docs/images/22f_trap_harvest.png) | ![gear](docs/images/21d_character_gear.png) |
| **낚시터 로딩 화면** | **ESC 메뉴 (원작처럼 흐린 화면 위 검은 띠 + 남색 금속 버튼)** |
| ![loading](docs/images/21z_loading.png) | ![esc](docs/images/22a_esc_menu.png) |
| **한강에서 본 남산서울타워·롯데월드타워** | **밤 스카이라인 (창 불빛·별)** |
| ![skyline](docs/images/00q_skyline_namsan_lotte.png) | ![skyline_night](docs/images/00s_skyline_night.png) |
| **63빌딩과 동작대교** | **한강공원 CU 편의점** |
| ![63](docs/images/00r_skyline_63.png) | ![cu](docs/images/00t_cu_store.png) |
| **동작대교 야경 (황금빛 아치·교각 투광등·가로등)** | **강 건너에서 본 다리 야경** |
| ![bridge_night](docs/images/00w_bridge_night.png) | ![bridge_far](docs/images/00w2_bridge_night_far.png) |
| **63빌딩 (위로 좁아지는 금빛 유리 탑)** | **제주 주상절리대 (현무암 기둥 벽·너럭 단·전망 데크)** |
| ![63_close](docs/images/00r2_63_close.png) | ![columnar](docs/images/54c_jeju_columnar.png) |
| **제주 성산일출봉** | **제주 용두암 (검은 용암 너럭바위, 바위 줄기 끝 용머리)** |
| ![seongsan](docs/images/54e_jeju_seongsan.png) | ![yongduam](docs/images/54g_jeju_yongduam_rocks.png) |
| **주상절리 곶과 외돌개** | **홍현 갯바위 둔덕 (퇴적층 턱·틈)** |
| ![columnar_sea](docs/images/54d_jeju_columnar_sea.png) | ![outcrop](docs/images/22g_honghyeon_outcrop.png) |
| **산토리니 비탈 마을과 파란 돔 정자** | **산토리니 포석 산책로·파란 아치문** |
| ![santorini_pavilion](docs/images/56c_santorini_pavilion.png) | ![santorini_promenade](docs/images/56d_santorini_promenade.png) |
| **삼척 방파제 (실물 크기 테트라포드·끝 둥근 더미)** | **삼척 밤 항구 안 야광충** |
| ![samcheok_bw](docs/images/45c_samcheok_corner.png) | ![bio_harbor](docs/images/45b_samcheok_night.png) |
| **크레타 방파제 테트라포드** | **듀덴 폭포 트래버틴 절벽** |
| ![tetrapods](docs/images/69c_crete_tetrapods.png) | ![duden_cliff](docs/images/71b_duden_cliff.png) |
| **하롱베이 석회암 봉우리 (실사 석회암·빗물 얼룩·덤불)** | **청진포구 덕장 (코를 엮어 건 명태)** |
| ![karst](docs/images/55d_halong_karst_close.png) | ![pollock](docs/images/49e_cheongjin_pollock_close.png) |
| **낚싯대를 눕히면 줄이 처진다** | **세우면 팽팽하다** |
| ![slack](docs/images/03d_rod_low_slack.png) | ![taut](docs/images/03e_rod_high_taut.png) |

**물고기 224종** (Blender 스크립트로 만든 어종별 모델·텍스처 — 기와처럼 겹친 비늘, 젖은 점액 막 반사, 반투명 지느러미, 빨판 달린 오징어·문어)

![fish](docs/images/32_fish_models.png)

## 특징

- **원작 흐름**: **한국 낚시 조합(대기실)**에서 시작 → [낚시터 선택하기] → 거점 탭(한국·동남아·지중해·아프리카)과 지도에서 낚시터 → 채널 CH.1~5 입장. 필드 오른쪽 위 `↩ 조합`으로 돌아온다. 레벨로 맵이 열린다(한강 Lv.1, 홍현 Lv.5, 서천 Lv.6, 영흥도 Lv.9 … 제주 Lv.21 … 거문도 Lv.24, 하롱베이 Lv.25 · 앙코르와트 Lv.26 · 산토리니 Lv.28 · 나일강 Lv.31 … 플라우 탱글 Lv.39).
- **원작 조작 그대로**
  - **캐스팅**: 평소엔 낚싯대를 넣고 다니다가 던질 물을 클릭하면 꺼내 든다. 좌클릭을 누른 채 아래로 내려 낚싯대를 들고, 위로 쭉 올려 휘두르면 던진다. 휘두르는 속도가 비거리(5m 미만은 실패). 큰 비거리 숫자와 궤적. 찌를 기다리는 동안 마우스로 낚싯대를 상하좌우로 움직여 줄을 늦추거나 당기고(찌 자리는 그대로), 좌클릭을 꾹 누르면 릴이 느슨한 줄부터 감는다.
  - **챔질**: 왼쪽 버튼을 누른 채 좌우로 흔든다. 찌가 완전히 잠겼을 때 흔들면 **PERFECT**.
  - **파이팅(1인칭)**: 좌클릭 감기, 우클릭 풀기, 마우스로 낚싯대 방향. HP 바·거리 눈금자·파이팅 레벨 링·데미지 숫자. 물고기가 질주하면 반대로 휙 → **카운터** 콤보.
  - HP를 깎은 뒤 5m 안으로 끌어오면 **랜딩 찬스**, 연타로 0m까지 → **RESULT** (처음 낚은 어종은 FIRST GET).
  - **포커싱(Q)**: 8초 동안 장력이 천천히 변하고 데미지가 커지는 원작 파이팅 스킬 (20분에 한 번).
  - **RESULT 보상 연출**: 오른쪽 말풍선 토스트(첫 월척·N가지 물고기·오늘 첫 고급·중급 10마리·연속 공격!·연속 색깔 맞추기·최고 기록), `BONUS 보상 GOLD/EXP`, 파이팅 시간, 등급 색(초급·중급·고급), 낚시터마다 하루 한 종 **이벤트 어종 EXP +200%**.
  - **놓치면**: 원작 WARNING 카드(낚싯대 내구도 1%·릴 2%·줄 7m 감소) → **재도전** 말풍선 → `X2 재도전` 배지, 놓친 그 고기가 다시 와서 낚으면 **복수혈전** (또 놓치면 X3·X5).
  - 조준 중 업적 3줄(월척급·랜딩·카운터), 별 파티클 궤적. 채팅 줄에 같은 채널 다른 낚시꾼의 조과·잡담이 흐른다.
  - **체력**: 20% 아래면 지치고(힘·릴 감기 약해짐) 5% 아래면 탈진. 음식 5종으로 회복.
- **낚시법**: 찌낚시, **루어**(감기 속도·저킹 액션), **플라이**(털바늘·플라이대), **트롤링**(배를 몰며), 겨울 한강 **얼음 구멍 낚시**(빙어).
- **맵 29곳**
  - **한강**: 동작대교·낚시 데크·수초 만, 밤이면 다리가 실사 사진처럼 황금빛 투광등(아치·거더)·교각 올림 조명·상판 가로등으로 빛나고 물에 길게 비친다. 강 건너 **남산서울타워·롯데월드타워·63빌딩**과 먼 시가지(밤엔 집마다 띄엄띄엄, 가끔 층 통째로 켜진 창 불빛 — 롯데타워도 123층 전체가 되풀이 없이 불규칙하게), 둔치의 **CU 편의점**(한강라면 조리기, 파라솔 탁자).
  - **홍현 앞바다**(남해): 방파제·빨간 등대·테트라포드·갯바위 섬·다랭이 마을, 자이언트 그루퍼 어군, 먼바다의 전설 다금바리·돗돔.
  - **서천 앞바다**(서해): **물때**(밀물·썰물에 갯벌이 드러남), 홍원항 흰/빨간 등대, 동백정, 주꾸미·갑오징어(에기)·민어. 원작 대회 장소.
  - **해안 낚시터 9곳**(해안 맵 틀 `CoastMapDef`): 영흥도(썰물에 걸어 들어가는 목섬, 영흥대교, 해무), 태안(만리포·신두리 사구·할미할아비바위), 삼척(추암 촛대바위, 밤바다 집어등), 동해 등대섬(큰 등대), 헌화로(절벽 해안도로·바다부채길·진달래, 대문어 굴), 욕지도(가두리 낚시터·출렁다리), 청진포구(눈 덮인 항구·명태 덕장, 명태 어군), 제주(외돌개·현무암 기둥 주상절리대와 전망 데크·성산일출봉·용두암·한라산·돌하르방·귤밭, 다금바리 어군), 거문도(거문도 등대·백도, 부시리).
  - **탄천**(원작에 없는 새 맵, `World/Maps/Tancheon.cs`): 폭 60m 도심 하천, 갈대 물가·산책로·자전거도로·벚나무 제방, 높은 차도교(야탑교·사송교, 보라·초록 LED)·막돌 아치 보행교·지그재그 징검다리·보, 강 건너 탄천종합운동장·야구장, 등 축제, 파란 부력 큐브 카약 선착장과 물 위를 저어 다니는 카약들, 운동장 위 밤하늘 드론쇼(3,000대, 첫 장면은 타이틀 「청풍명월」, 이어서 `문서\짭풍명월\DroneShow`의 점 파일을 **빌드 없이** 읽어 그린다. 폴더·순서·시간은 `droneshow.json` → [드론쇼 장면 바꾸기](docs/guide/08-drone-show.md), 그림 한 장을 장면으로: `tools/drone/scene.py`), 수양버들, 한강 쪽으로 흐르는 물, 하류 너머 롯데월드타워. 토종 민물고기 7종(버들치·각시붕어 …).
  - **해외 17곳**(같은 틀 + 강·지역 양식 집·나무, 새 맵은 `World/Maps/<이름>.cs` 하나에 사양·랜드마크·이야기): 하롱베이(석회암 봉우리·정크선·수상 마을), 산토리니(비탈을 따라 계단식으로 쌓인 흰 집·파란 돔 교회, 포석 산책로·철제 가로등·파란 아치문·파라솔 카페, 돌 부두 끝 파란 돔 정자, 칼데라·풍차), 나일강 하류(펠루카·피라미드), 앙코르와트(해자·연꽃 봉오리 탑·사자상), 빈펄(초가 정자·케이블카), 돈콘(수상 가옥·여울), 푸켓(하얀 대불·팡아만), 방콕(왓 아룬), 코사무이(황금 대불), 랑카위(독수리 상·스카이 브리지·맹그로브), 센토사(머라이언·마리나 스카이라인·열기구), 텔록 바텍(망루·롱테일 보트), 티오만(곡선 리조트 호텔·보라 파라솔·나무 샬레·정글 산), 탱글(바다 위 데크 별장), 크레타(이집트 등대·모스크·하얀 산), 자킨토스(세로 결 흰 석회암 절벽·흰 조약돌 해변·난파선·푸른 동굴), 듀덴(일곱 갈래로 흘러내리는 폭포·물보라·토로스 산맥). 국내는 묵호등대·거문도 새/옛 등대, 서천 송림 스카이워크, 홍현 석방렴, 욕지도 가두리 낚시터. 청진포구 덕장엔 코를 엮어 건 명태가 줄지어 마른다.
- **생태**: 어종 **224종** + 잡동사니 6종. 서식지·미끼·시간대·날씨·**계절**(한 달 = 게임 7일, 제철 어종)에 따라 입질이 달라진다. 수면의 하얀 소용돌이 **어군**(큰/작은 어군), 고기떼를 모두 흩으면 나오는 **히든 어군**, 고기떼를 쫓는 **포식자 어군**, 지역 지도(M).
- **괴물 어종**: 괴물잉어·괴물가물치·폭군삼치·황새치를 낚으면 대물 코인 → 풍림화산 낚싯대·릴로 교환.
- **배 4종**: 고무보트·돛단배(바람)·고속보트·낚시 크루저. 원작처럼 B로 부르면 바다 쪽에서 달려와 서고, **Y로 엔진 START/STOP**(꺼야 낚시). 연료, 배마다 다른 흔들림(안정성 × 파도), 먼바다 포인트(대물 +8%).
- **성장·수집**: 레벨 1~40(레벨 보상 장비), 장비(낚싯대·릴·줄·배) 강화 +15, 미끼 + 해체 미끼 5등급, 도감, 살림망, 이야기 퀘스트 87개(낚시터마다), **일일 의뢰**, **대물 도전**, **업적**·칭호, **빙고 5×5**, **출석 체크**, 낚시 일지, 소라고동 교환소, **납품소**(살림망 고기를 팔면 납품 게이지가 차고, 가득 차면 다음 납품 두 배), **통발**(낚시터에 설치해 두고 20시간 뒤 수거), **내 캐릭터 창**(장착 장비 칸 + 최종 능력치 표 + 캐릭터 모델), **의상**(6부위 25벌, 부위별 능력치), 대기실 **수족관**.
- **조합 낚시 대회**: 매일 09~15시, AI 낚시꾼 7명과 마릿수·최장·길이 합계로 겨룬다. 계급 엠블럼, 우승 상품 거거 낚시대·나팔고둥 릴.
- **손맛**: 마디를 펼치는 뽑기대가 줄 방향으로 휘고 출렁인다. 장비별 릴이 돌고, 릴·드랙·카운터 소리, 게임패드 진동.
- **화면**: 원작풍 UI(짙은 남색 판·파란 빛 테두리·각진 모서리·파란 탭 — Translucent Image + Paraform 유리 판), 직접 그리는 차트(선·막대·도넛·레이더), 후처리(블룸·톤매핑·AO), 낮밤(하루 24분)·노을·해달별(은하수·반짝이는 별), 날씨(흐린 날 층구름·비·겨울 눈), 물결 셰이더(서천·삼척 밤바다 야광충: 밀려오는 푸른 빛과 흰 물마루, 방파제 앞까지 넓게 번지는 빛, 빛 알갱이 파티클), 흐르는 폭포 셰이더(`Fishing/Waterfall` + 물보라·물안개 파티클), 새벽 물안개, 공기 원근감(먼 섬이 지평선 하늘빛으로 바램). 먼 섬·산은 숲 셰이더(`Fishing/Forest`: 코드로 구운 수관 결 + 가파른 면·물가 바위)를 입힌 절차적 섬 메쉬. 바닥·지형은 반복 무늬를 깨는 셰이더(`Fishing/Ground`·`Fishing/Terrain`, 절벽은 옆 투영), 벽·호안·석상·집은 풍화 셰이더(`Fishing/Weathered`: 월드 트라이플래너 + 물 자국·빗물 줄·이끼), 테트라포드·등대·바위기둥·한옥 정자는 절차적 메쉬, 석상(머라이언·독수리·사자상·대불·돌하르방)은 Blender에서 한 덩어리로 깎은 조각, 배(한국 연안 어선·모터보트·고무보트·나무배·카이크·요트)는 Blender 로프트 선체, 나무·덤불은 [ez-tree](https://github.com/dgreenheck/ez-tree)(MIT)로 만든 실사풍 나무(잎 양면 셰이더 `Fishing/Foliage`, 먼 거리는 십자 판), 바위·풀·들꽃·하늘은 Poly Haven(CC0).
- **사람은 모두 VRM (애니풍)**: 내 캐릭터·낚시점 사장·조합 연락관·낚시꾼 NPC가 모두 VRM 파일이다. 기본 캐릭터(주인공은 여자 + 흰 원피스, 남자로 바꿀 수 있다)와 NPC는 VRoid CC0 샘플 11명(NPC는 사람마다 머리·옷 색·키를 바꿔 다양하게), [VRoid Studio](https://vroid.com/studio)로 만든 내 `.vrm`을 `문서\짭풍명월\Characters`에 넣으면 **빌드 없이** 캐릭터 창에서 골라 플레이한다. 기본 캐릭터·NPC도 `characters.json`만 고치면 빌드한 뒤에 갈아입힐 수 있다 → [캐릭터 바꾸기 설명서](docs/guide/07-characters.md). 의상은 능력치만 있고 몸에 보이는 장비는 낚싯대뿐. 대기실 **[옷 바꾸기]** 로 다른 VRoid 모델의 옷을 무작위로 입고(`outfitUrls`를 적으면 웹에서 받아 입기), 입은 옷은 저장된다.
- **물고기는 직접 만든 3D 모델**: headless Blender 스크립트(`tools/blender`)로 물고기 224종의 어종별 모델·텍스처·헤엄 애니메이션(오징어·문어·갑오징어는 다리마다 뼈와 빨판)을 만들고, 아이템 아이콘도 렌더.
- **인트로**: Unity 스플래시 → 사업자 로고 「제쓰로네 게임공방」(낙관이 쾅 찍히고 이름이 붓으로 써진다) → 타이틀 「짭풍명월」(달밤 수묵 산수화 겹 그림이 시차·물안개·배·꽃잎으로 움직이고, 원작 로고처럼 굵은 붓글씨·징·가야금풍 음악) → 아무 키 → 대기실. 아무 키로 건너뛸 수 있다. 그림은 `tools/intro/build_intro_art.py`, 소리는 절차적(`IntroAudio`).
- **낚시 친구 윤슬 (AI)**: 낚시터에서 늘 오른쪽에서 따라 걷고 뛰는 친구. 낚거나 놓치면·레벨이 오르면·새 낚시터에 들어서면 한마디(환호·박수 동작). **Enter로 무엇이든 물어보면** 이 PC에서 도는 무료 AI([Ollama](https://ollama.com), 기본 `gemma3:4b`, 로그인·인터넷 필요 없음)가 게임이 미리 찾아 붙여 준 플레이 가이드·어종·낚시터·퀘스트·내 상황으로 **1~2초 만에** 반말로 대답한다(내 말은 내 머리 위, 대답은 윤슬 머리 위 말풍선에). 게임 밖 질문(이 PC 사양, 이 게임을 어떻게 만들었는지)도 받는다. **길 안내**: "퀘스트 누구한테 받아?"·"붕어 어디서 잡아?" 하면 직접 앞장서 퀘스트 NPC·낚시 포인트 물가로 데려가 손으로 가리키고, 배를 타야 하면 배 띄울 곳으로 데려가 같이 탄다. 같은 "게임 지식 도구"를 **MCP 서버**(`-mcp`, `http://127.0.0.1:8770/mcp`)로도 열어 Claude Code 같은 바깥 AI가 실행 중인 게임을 읽는다 → [낚시 친구 윤슬](docs/guide/09-buddy.md)
- **편의**: 옵션(O) — 음량·감도·해상도·그림자·후처리·진동·**키 설정**.

## 실행 (포크해서 시작하기)

0. 저장소를 받는다. 그림·모델·소리·VRM은 **Git LFS**라서 먼저 `git lfs install`을 하고 clone한다 (이미 받았으면 `git lfs pull`).
1. **Unity 6000.3.17f1**로 이 폴더를 연다. 에셋 스토어 패키지는 필요 없다(모두 저장소 안에 있거나 Unity 패키지 관리자가 받는다).
2. `Assets/Scenes/HanRiver.unity`를 열고 Play한다.

명령줄로 빌드할 수도 있다 (Git Bash).
```bash
sh tools/build.sh      # Builds/Windows/ProjectFishing.exe
sh tools/autoshot.sh   # 빌드를 자동 연출해 Screenshots/ 에 약 150장 + 맵별 FPS
sh tools/test.sh       # 단위 테스트 35개
cd tools/fightsim && dotnet run -- report   # Unity 없이 파이팅 밸런스 (.NET 8)
```

## 조작

| 입력 | 동작 |
|---|---|
| WASD / Shift / Space | 이동 / 달리기 / 점프 |
| B | 물가에서 배 부르기·내리기 (배 위에서 W/S/A/D 운전) |
| Y | 배 엔진 START / STOP (꺼야 낚시) |
| 놓친 뒤 클릭 / Enter | 재도전 (놓친 그 고기가 다시 온다) |
| F | NPC와 대화 (상점·강화·퀘스트 보고·대회 신청·조황) |
| Enter | 따라다니는 낚시 친구 **윤슬**과 대화 (쓰고 Enter로 보내기, 빈칸 Enter·Esc로 닫기) |
| 오른쪽 드래그 · 휠 | 시점 · 줌 |
| 물 클릭 → 왼쪽 버튼 누른 채 아래로 내렸다가 위로 쭉 | 낚싯대 꺼내기 → 캐스팅 (5m 미만 실패. 겨울 한강 얼음 위에서는 클릭 → 누른 채 살짝 내리기 = 구멍 뚫기) |
| 왼쪽 버튼 누른 채 좌우로 흔들기 (찌가 잠겼을 때) | 챔질 |
| 왼쪽 버튼 누르고 있기 (던진 뒤) | 릴 감기 (루어는 감는 속도·멈춤이 액션) |
| R / 이동 | 찌 회수 |
| 1~8 | 미끼 선택 |
| **파이팅**: 왼쪽 / 오른쪽 버튼 / 마우스 좌우 | 감기 / 풀기 / 낚싯대 방향·카운터 |
| **랜딩 찬스**: Space 또는 왼쪽 버튼 연타 | 0m까지 끌어오기 |
| L / P / Z / M / F12 | 퀘스트 / 도감 / 살림망 / 지역 지도 / 상점 |
| I / C / O | 가방 / 내 캐릭터 (장비·능력치·의상) / 옵션 (키 설정) |
| **파이팅**: Q | 포커싱 (20분에 한 번) |
| U / F1 / F5 / Esc | 설명 켜기·끄기 / 도움말 / 저장 / 닫기 |

키는 옵션(O) → 키 설정에서 바꿀 수 있다.

## 문서

| 문서 | 내용 |
|---|---|
| [01-original-research.md](docs/01-original-research.md) | 원작 조사: 서비스 이력, 원작 시스템, **플레이 영상 분석**, 반영 현황, 출처 |
| [02-game-design.md](docs/02-game-design.md) | 게임 디자인: 루프, 입질, 낚시법, 파이팅 공식, 성장, 장비, 맵 29곳(해외 17곳 포함), 배, 계절, 어종표, 퀘스트·대회·업적·의상·수족관·빙고·출석 |
| [03-architecture.md](docs/03-architecture.md) | 코드 구조, 실행 흐름, 정적 배칭, 상태 머신, 확장 방법(어종/장비/맵/아트) |
| [04-balance.md](docs/04-balance.md) | 밸런스 시뮬레이션 방법(Unity 없이 `tools/fightsim`), 결과 요약, 해외·괴물 어종, 체력 페널티, 조정 이력 |
| [balance-report.md](docs/balance-report.md) | 시뮬레이터가 생성한 전체 결과표 |
| [05-dev-guide.md](docs/05-dev-guide.md) | 개발 환경, 에디터 메뉴, 명령줄 도구, 단위 테스트, Blender 파이프라인, 자동 스크린샷 |
| [06-roadmap.md](docs/06-roadmap.md) | 한 일과 다음 할 일 (v0.5 원작 자료집·해외 거점) |
| [07-original-data.md](docs/07-original-data.md) | 원작 데이터 검증 (확인된 것 / 근거 없는 것) |
| [08-quality-plan.md](docs/08-quality-plan.md) | (기록) 예전 Quaternius 캐릭터 퀄리티 계획 — 지금은 VRM(16 문서) |
| [09-art-plan.md](docs/09-art-plan.md) | 어종별 물고기 모델·도감 그림·아이템 아이콘·해외 소품 (검색 → 설계 → Blender → 적용) |
| [10-animation.md](docs/10-animation.md) | 캐릭터 애니메이션: Unity Starter Assets 대기·걷기·뛰기·점프(180° 돌려 굽기) + UAL + 직접 만든 반응 동작(환호·박수·펄쩍·손짓·움찔, Blender IK로 굽기) + 낚싯대 팔 IK |
| [11-original-crosscheck.md](docs/11-original-crosscheck.md) | 원작 자료집과 구현 대조표 (들어간 것 / 바꾼 것 / 안 넣은 것) |
| [12-original-fight-video.md](docs/12-original-fight-video.md) | 원작 낚시 영상 프레임 분석: 입질·챔질·파이팅·카운터·랜딩·결과 연출을 원작 시각별로 맞춘 기록 |
| [13-original-lobby-video.md](docs/13-original-lobby-video.md) | 원작 대기실·퀘스트·배 영상 프레임 분석: NPC 퀘스트, 납품·생태조사, 통발, 로딩·날씨 알림·ESC·인벤토리를 맞춘 기록 |
| [14-original-sea-video.md](docs/14-original-sea-video.md) | 원작 동남아 거점 영상 프레임 분석(앙코르와트·하롱베이·빈펄·돈콘·센토사·플라우 탱글·텔록 바텍·티오만)과 원작 전체 맵 목록 |
| [15-original-clip-library.md](docs/15-original-clip-library.md) | 원작 기능별 클립 라이브러리(814개·문서 17개) 대조: WARNING·재도전·복수혈전, RESULT 토스트·BONUS·이벤트 어종, 조준 업적, 배 소환·엔진, 원작풍 UI 팔레트 |
| [16-vrm-characters.md](docs/16-vrm-characters.md) | VRM 캐릭터: 모든 사람이 VRM, 실행 중 읽기·복제(UniVRM), characters.json(기본 캐릭터·NPC 배정, 빌드 뒤 교체), 걷어낸 옛 캐릭터 |
| [17-open-source.md](docs/17-open-source.md) | 공개 준비: 재배포 금지 에셋(에셋 스토어·Mixamo)을 직접 만든 것·CC0·MIT로 바꾼 기록(하늘·동작·차트·UI 유리·나무·바위·풀), 다시 만드는 명령, 라이선스 |
| [fish-db/](docs/fish-db/00_README_어종DB_가이드.md) | **맵별 서식 어종 DB**: 한국·동남아·지중해 약 145종 (실제 서식 어종 기준, 희귀도·파워·경계심·파이팅 유형·낚시터 매핑) |
| [WORK-IN-PROGRESS.md](docs/WORK-IN-PROGRESS.md) | 지금 상태, 다음 후보 |
| [guide/](docs/guide/README.md) | **레벨별 플레이 가이드**: Lv.1~40 구간마다 열리는 것, 장비 사는 순서, 낚시터·어군·퀘스트 요령, 기본기 |
| [tech/](docs/tech/README.md) | **기술 문서**: 만들면서 도움이 된 알고리즘·기법 (파이팅 모형, 헤드리스 밸런스, 가중치 확률, 제스처 인식, 스프링·IK, 절차적 지형, 셰이더, 성능, 절차적 콘텐츠, 개발 흐름) |

## 구조 한눈에

```
Assets/Scripts
  Core/       진입점(GameBootstrap), 시계·날씨·조명·물때·후처리, 저장, 퀘스트, 대회, 설정
  Data/       어종·장비·의상·업적·퀘스트·계절·대회 규칙 정적 테이블
  Characters/ VRM 캐릭터(VrmCharacters·CharacterRoster·CharacterFactory), Playables 애니메이션, NPC
  World/      맵 정의(FieldMapDef: 한강·홍현·서천 + 해안 맵 틀 CoastMapDef 국내 9곳·해외 17곳, World/Maps 새 해외 맵·MapKit), 월드·지형·정적 배칭, 대기실·수족관, 수면·하늘, 어군, 계절 효과
  Player/     3인칭 캐릭터, 배, 카메라(3인칭 ↔ 1인칭 ↔ 고정)
  Fishing/    낚시 상태 머신, 파이팅 시뮬레이션(FishFight), 입질 선택, 찌·루어, 물고기 모델
  UI/         코드로 만든 UGUI: 대기실, 낚시터 목록, 지역 지도, HUD, 파이팅 HUD, 각종 창
  Audio/      절차적 낚시 소리 + Kenney CC0 UI·발소리
  Dev/        자동 연출·스크린샷
Assets/Shaders  Fish, Water(물결), Sky(달·별·구름), Weathered(풍화), Terrain
Assets/Resources  물고기·아이콘(tools/blender로 만든 것), 동작 클립, Kenney·Quaternius 모델, Poly Haven 텍스처, 소리
Assets/StreamingAssets/Characters  기본 VRM 11명 + characters.json + 읽어주세요.txt (빌드한 뒤에도 바꿀 수 있다)
Assets/Editor  셋업·빌드 도구, 임포트 규칙, 밸런스 시뮬레이터, 단위 테스트
tools/        build·autoshot·test·balance 스크립트, fightsim/(Unity 없는 밸런스), fish_table.py(02 문서 어종 표), blender/(물고기·아이콘·해외 소품 생성)
```

씬에는 `GameBootstrap` 하나만 있고, 나머지는 모두 코드로 만듭니다.
에셋 스토어·Mixamo처럼 원본 재배포가 금지된 에셋은 쓰지 않습니다(2026-10-21에 모두 걷어내고 직접 만든 것과 CC0·MIT 자료로 바꿈 → [17 문서](docs/17-open-source.md)). 사람은 실행 중에 읽는 VRM(UniVRM), 물고기·아이콘은 `tools/blender`로 만든 모델, 건물·가로등·나무·배는 Kenney CC0 모델, 땅·물·다리·하늘은 프리미티브와 절차적 텍스처·셰이더(일부 바닥·지형은 Poly Haven CC0 텍스처)입니다. 낚시 소리는 절차적으로 만들고 UI·발소리만 Kenney CC0 녹음을 씁니다. 낚싯대를 든 두 팔(들기·던지기·챔질·릴 감기)은 낚싯대 각도를 따라가는 IK이고, 대기·걷기·뛰기·점프는 Unity가 무료로 내는 Starter Assets – Third Person(Unity Companion License) 동작, 말하기·제자리 돌기·낚시꾼 자세와 반응 동작(환호·박수·펄쩍 기뻐하기·손짓·움찔)은 `tools/blender/build_motions.py`로 Quaternius UAL(CC0) 뼈대 위에 직접 만들었습니다([10 문서](docs/10-animation.md)). 하늘 돔은 Poly Haven 하늘 사진(`tools/sky/build_sky.py`), 나무·덤불·바위·풀은 `tools/blender/build_vegetation.py`(ez-tree·Poly Haven)입니다. 실물 사진·다른 게임 그림은 참고만 하고 저장소에 넣지 않습니다.

## 크레딧

- 원작: 「청풍명월」 개발 저스트나인, 서비스 네오위즈게임즈(피망). 이 프로젝트는 원작과 관련이 없는 개인 팬 프로젝트입니다.
- 캐릭터: VRoid 프로젝트(pixiv)의 VRoid Studio 샘플 모델 11명 (CC0, [OpenGameArt 미러](https://opengameart.org/content/vroid-studio-cc0-models)) — 千駄ヶ谷渋·千駄ヶ谷篠·桜田史利矢·ビビ·ヴィータ·ヴィクトリア・ルービン 등. VRM 읽기: [UniVRM](https://github.com/vrm-c/UniVRM) v0.131.4 (MIT, VRM 컨소시엄).
- 3D 모델: [Quaternius](https://quaternius.com) — Universal Animation Library, Animated Fish Pack(어종 모델이 없을 때 대체) (모두 CC0). 라이선스 사본은 `Assets/ThirdParty/Quaternius/`.
- 동작: 대기·걷기·뛰기·점프 — [Unity Starter Assets – Third Person](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526) (Unity Technologies, Unity Companion License). 나머지는 직접 만듦 (`tools/blender/build_motions.py`, Quaternius UAL1 뼈대) — 말하기·제자리 돌기·낚시꾼 자세·환호·박수·펄쩍·손짓·움찔.
- 나무·덤불: [ez-tree](https://github.com/dgreenheck/ez-tree) 1.1.0 (Daniel Greenheck, MIT) — 프리셋 형상·잎 그림, 껍질은 Poly Haven·TextureCan(CC0) → `Assets/Vegetation/SOURCES.md`
- 바위·풀·들꽃·하늘: [Poly Haven](https://polyhaven.com) (CC0) → `Assets/Vegetation/SOURCES.md`, `Assets/Resources/Sky/SOURCES.md`
- 배경·배 모델: [Kenney](https://kenney.nl) — City Kit Commercial, City Kit Roads, Nature Kit, Watercraft Kit (모두 CC0). 라이선스 사본은 `Assets/ThirdParty/Kenney/`.
- 텍스처: [Poly Haven](https://polyhaven.com) (CC0) — 모래·풀·바위·해안·나무 데크·콘크리트·아스팔트(갯벌 진흙·눈은 모래를 바꿔 만듦), 2026-10-20 추가: 거친 콘크리트·벽 콘크리트·회벽·점토 기와·회색 기와·사암·암벽·초가·판재·현무암·흰 사암 벽돌·돌담·녹슨 철·대리석 → `Assets/Resources/Terrain/SOURCES.md`
- 소리: [Kenney](https://kenney.nl) Interface Sounds · Impact Sounds (CC0, UI 클릭·경고·발소리) → `Assets/Resources/Audio/Kenney/SOURCES.md`
- UI: 직접 만든 도형·흐림 셰이더와 차트, TextMesh Pro, 글꼴 여기어때 잘난체2(Jalnan2, ㈜여기어때컴퍼니 — 라이선스 사본 `Assets/TextMesh Pro/Fonts/Jalnan2-LICENSE.txt`).

## 라이선스

직접 만든 코드·그림·모델·동작·문서는 **[CC BY-NC-SA 4.0](LICENSE)** (저작자표시-비영리-동일조건변경허락)입니다. 포크해서 고치고 나눠도 되지만, **판매 등 상업적 이용은 안 되고**, 고친 것도 같은 라이선스로 공개해야 하며 출처(제쓰로네 게임공방, 이 저장소)를 밝혀야 합니다. 서드파티 구성 요소는 각자의 라이선스를 따릅니다 → [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). 원작 「청풍명월」의 이름·상표·저작물에 대한 권리는 원작 권리자에게 있으며 이 라이선스로 주어지지 않습니다.

