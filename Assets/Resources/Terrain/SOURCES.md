# 지형 텍스처 출처 (karst_*만 Asset Store 무료 에셋, 나머지는 CC0)

Poly Haven (https://polyhaven.com, CC0 1.0) 1K JPG, 디퓨즈(_diff) + OpenGL 노멀(_nor)

| 파일 | Poly Haven 에셋 | 작가 |
|---|---|---|
| sand_* | Coast Sand 01 (coast_sand_01) | Rob Tuytel |
| grass_* | Sparse Grass (sparse_grass)를 색상만 초록으로 옮긴 것 (2026-10-20, 원본은 갈색이라 언덕이 누렇게 보였다. 마른 풀 얼룩은 지형 셰이더가 월드 노이즈로) | Amal Kumar |
| rock_* | Aerial Rocks 02 (aerial_rocks_02) | Rob Tuytel |
| shore_* | Coast Sand Rocks 02 (coast_sand_rocks_02) | Rob Tuytel |
| deck_* | Wood Floor Deck (wood_floor_deck) | Dimitrios Savva |
| concrete_* | Concrete Floor 02 (concrete_floor_02) | Rob Tuytel |
| asphalt_* | Asphalt 02 (asphalt_02) | Rob Tuytel |

지형(SeaTerrain)과 한강·홍현의 데크·길·콘크리트(Mats.Textured)에 쓴다.
| mud_* | Coast Sand 01을 어둡고 회갈색으로 바꾼 갯벌 진흙 (내장 Terrain 셰이더는 층별 색 보정을 무시해서 텍스처로 만듦) | Rob Tuytel (원본) |
| snow_* | Coast Sand 01을 밝고 푸르스름하게 바꾼 눈 (청진포구) | Rob Tuytel (원본) |
| karst_* | Rocks and Boulders 2 (Manufactura K4, Unity Asset Store 무료) `Rockdetail6` — 지의류 낀 흰 석회암. 1024 JPG로 줄여 하롱베이 석회암 봉우리(`Mats.Karst`, `Fishing/Karst`)에 쓴다 | Manufactura K4 |
| karst2_* | 같은 팩 `Rockdetail2` — 층이 진 회색 석회암. karst_*와 큰 덩어리로 섞는다 | Manufactura K4 |

## 2026-10-20 추가 (Poly Haven CC0 1K, 풍화 셰이더 `Fishing/Weathered`·먼 산 `Fishing/Landmark` 결)

| 파일 | Poly Haven 에셋 | 작가 | 쓰는 곳 |
|---|---|---|---|
| aggregate_* | Gravel Concrete (gravel_concrete) | Dario Barresi, Charlotte Baglioni | 테트라포드 |
| wallconcrete_* | Concrete Wall 006 (concrete_wall_006) | Dario Barresi, Charlotte Baglioni | 호안·방파제 옆면 (거푸집 구멍) |
| plaster_* | White Plaster Rough 02 (white_plaster_rough_02) | Rob Tuytel | 회벽 집 (`Mats.Wall`), Kenney 도시 건물 |
| rooftile_* | Clay Roof Tiles 02 (clay_roof_tiles_02) | Amal Kumar | 점토 기와 (`Mats.Tile`) |
| greytile_* | Grey Roof Tiles (grey_roof_tiles) | Rob Tuytel | 한옥 기와, 한국 시골집 지붕 |
| sandstone_* | Old Sandstone 02 (old_sandstone_02) | Dimitrios Savva | 석상·사암 (`Mats.Carved`, 앙코르 사자상·금불 받침) |
| cliff_* | Rock Face 03 (rock_face_03) | Dario Barresi, Rico Cilliers | 먼 산 (`MapKit.FarRock`) |
| thatch_* | Thatch Roof Angled (thatch_roof_angled) | Rob Tuytel, Dimitrios Savva | 초가·짚 지붕 (`Mats.Thatch`) |
| planks_* | Weathered Planks (weathered_planks) | Dario Barresi, Dimitrios Savva | 나무 판·기둥 (`Mats.Wood`), 정자 마루, 배 |
| basalt_* | Rock Pitted Mossy (rock_pitted_mossy) | Dimitrios Savva, Rico Cilliers | 돌하르방 현무암 |
| whitestone_* | White Sandstone Bricks (white_sandstone_bricks) | Rob Tuytel | (예비) 흰 석재 |
| stonewall_* | Stone Wall 04 (stone_wall_04) | Amal Kumar | 돌 부두·기단 (`Mats.Stone`) |
| rust_* | Rusty Metal 02 (rusty_metal_02) | Rob Tuytel | 난파선, 가로등 철제 |
| marble_* | Marble 01 (marble_01) | Rob Tuytel | 푸켓 하얀 대불, 정자 주춧돌 |

새 텍스처를 넣으면 `Mats.TexAvg`에 평균 색(감마)을 더한다 — 셰이더가 평균으로 나눠 결만 남기므로, 넘기는 색이 그대로 칠한 색이 된다.
