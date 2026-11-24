# 포트폴리오 (노션용)

[PORTFOLIO.md](PORTFOLIO.md) — 청풍명월 팬메이드 포트폴리오 본문. 노션에 그대로 가져오도록 Markdown으로 썼다 (그림은 `images/`).

## 노션에 올리기

1. 그림·zip 만들기 (Python 3 + Pillow):
   ```
   python tools/portfolio_images.py --zip
   ```
   → `docs/portfolio/images/*.jpg` (긴 변 1600px, JPG 품질 88 — 노션 무료 요금제의 파일 하나 5MB 제한 안쪽) + `docs/portfolio/notion_import.zip`(PORTFOLIO.md + images/)
2. 노션 → 왼쪽 아래 **설정 → 가져오기(Import)** → **Text & Markdown** → `notion_import.zip`을 고른다. 그림이 페이지 안에 들어간 채로 새 페이지가 생긴다.
3. 다듬기 (노션에서 손으로):
   - 맨 위에 `/목차`(Table of contents) 블록을 넣는다.
   - "숫자로 보기" 표 아래, 그림 둘씩은 드래그로 **두 단 배치**하면 더 보기 좋다 (Markdown에는 단 배치가 없어 한 줄에 하나씩 넣었다).
   - 인용문(`>`)은 노션에서 **콜아웃**으로 바꾸면(블록 메뉴 → 전환) 눈에 띈다.
   - 링크를 걸 곳: 시연 영상(유튜브), 릴리즈 빌드 다운로드. 저장소는 비공개라 저장소 링크·문서 링크는 넣지 않았다.

## 고치는 법

- 글: `PORTFOLIO.md`를 고치고 다시 zip → 가져오기 (노션은 덮어쓰기가 없어서 새 페이지가 생긴다. 작은 수정은 노션에서 바로 고치는 게 낫다).
- 그림: `tools/portfolio_images.py`의 `PICKS`(포트폴리오 이름 → 스크린샷 이름). 새로 찍은 `Screenshots/`가 있으면 그것을, 없으면 `docs/images/`를 쓴다. 장면을 다시 찍으려면 `sh tools/build.sh && sh tools/autoshot.sh`.
- 기술 내용의 원본은 `docs/tech/` 13편이다. 숫자(맵·어종·테스트 수·FPS)는 [WORK-IN-PROGRESS](../WORK-IN-PROGRESS.md)의 "지금 상태"와 맞춘다.

## 쓰는 말

- 포트폴리오에서는 게임을 **"청풍명월 팬메이드"**라고 부르고 "짭"이라는 낱말은 쓰지 않는다 (2026-11-25 사용자). 그래서 "짭풍명월" 로고가 보이는 타이틀 화면은 그림에서 뺐다.
- 문장은 **격식체(~합니다/~입니다)**로 쓴다. "~요"(해요체)나 "~한다"(해라체)를 섞지 않는다. 그림 설명·표 칸은 명사로 끝내도 된다 (2026-11-25 사용자).

## 그림

- 35장. 경치·모델링 자랑 사진은 넣지 않고, 낚시터는 맵마다 가장 멋진 한 장씩만 (2026-11-25 사용자 "사진이 많다").

- 스크린샷은 1920×1080(2026-11-25 해상도 FHD 고정 뒤 새로 찍음). 다른 세션의 자동 촬영이 `Screenshots/`를 지우지 않게 빌드 사본으로 별도 폴더에 찍어 `--from`으로 넘겼다:
  ```
  Builds/Windows/ProjectFishing.exe -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -autoshot <폴더> -logFile <폴더>/full.log
  python tools/portfolio_images.py --from <폴더> --zip
  ```
- 동료 AI 장면(30번대)은 Ollama가 켜져 있어야 대답까지 찍힌다 (`sh tools/autoshot.sh --buddy "질문"`).
