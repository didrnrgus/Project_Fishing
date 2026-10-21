# 서드파티 구성 요소와 라이선스

이 저장소에서 직접 만든 것은 [LICENSE](LICENSE)(CC BY-NC-SA 4.0)를 따릅니다. 아래 구성 요소는 각자의 라이선스를 따르며, 라이선스 사본은 표의 위치에 있습니다.
에셋 스토어·Mixamo처럼 원본 재배포가 금지된 에셋은 2026-10-21에 모두 걷어내고 직접 만든 것이나 아래 CC0·MIT·OFL 자료로 바꿨습니다(예전 커밋 기록에는 남아 있어서 공개 저장소는 기록을 새로 시작했습니다).

| 구성 요소 | 쓰는 곳 | 라이선스 | 사본·출처 |
|---|---|---|---|
| [UniVRM](https://github.com/vrm-c/UniVRM) v0.131.4 (VRM 컨소시엄) | VRM 캐릭터 읽기 (Unity 패키지, `Packages/manifest.json`이 받는다) | MIT | 패키지 안 LICENSE |
| [ez-tree](https://github.com/dgreenheck/ez-tree) 1.1.0 (Daniel Greenheck) | 나무·덤불 형상, 잎 그림 (`tools/eztree`, `Assets/Vegetation`) | MIT | [Assets/Vegetation/SOURCES.md](Assets/Vegetation/SOURCES.md) |
| ez-tree에 든 껍질 텍스처: Poly Haven bark_brown_02, TextureCan 221·588 | 나무껍질 | CC0 | [Assets/Vegetation/SOURCES.md](Assets/Vegetation/SOURCES.md) |
| [Quaternius](https://quaternius.com) Universal Animation Library | 동작 바탕 뼈대·클립 (`Resources/Animations/UAL1_Standard.fbx`), 물고기 대체 모델 | CC0 | `Assets/ThirdParty/Quaternius/` |
| VRoid 프로젝트(pixiv) VRoid Studio 샘플 모델 11명 | 기본 캐릭터·NPC (`StreamingAssets/Characters`) | CC0 | [OpenGameArt 미러](https://opengameart.org/content/vroid-studio-cc0-models) |
| [Kenney](https://kenney.nl) City Kit Commercial·Roads, Nature Kit, Watercraft Kit, Interface·Impact Sounds | 건물·가로등·배 모델, UI·발소리 | CC0 | `Assets/ThirdParty/Kenney/`, `Resources/Audio/Kenney/SOURCES.md` |
| [Poly Haven](https://polyhaven.com) 텍스처·모델·HDRI | 지형·바닥 텍스처, 바위, 풀·들꽃 카드, 하늘 | CC0 | `Resources/Terrain/SOURCES.md`, `Assets/Vegetation/SOURCES.md`, `Resources/Sky/SOURCES.md` |
| 여기어때 잘난체 2 (㈜여기어때컴퍼니) | UI 글꼴 | 여기어때 잘난체 라이선스 (무료·번들 재배포 가능, 글꼴 단독 판매·개작 재배포 금지) | `Assets/TextMesh Pro/Fonts/Jalnan2-LICENSE.txt` |
| Liberation Sans | TextMesh Pro 기본 글꼴 | SIL OFL 1.1 | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` |
| Cinzel, East Sea Dokdo, Song Myung | 인트로 그림 만들기 (`tools/intro`) | SIL OFL 1.1 | `tools/intro/fonts/OFL-*.txt` |
| [Unity Starter Assets – Third Person](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526) (Unity Technologies) | 대기·걷기·뛰기·점프 동작 (원본 FBX `Assets/ThirdParty/StarterAssets`, 180° 돌려 구운 클립 `Resources/Animations/StarterAssets`) | [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) | [Assets/ThirdParty/StarterAssets/SOURCES.md](Assets/ThirdParty/StarterAssets/SOURCES.md) |
| TextMesh Pro 기본 리소스 (Unity) | `Assets/TextMesh Pro` | Unity Companion License | Unity 패키지 |

## 원작에 대해

이 프로젝트는 피망 「청풍명월」(개발 저스트나인, 서비스 네오위즈게임즈)을 혼자 즐기려고 다시 만든 비영리 팬 프로젝트이고, 원작과 관련이 없습니다.
원작 소스·리소스는 쓰지 않았고 공개된 기사·플레이 영상에서 게임 방식만 참고했습니다. 원작의 이름·상표·저작물에 대한 권리는 그 권리자에게 있습니다.
