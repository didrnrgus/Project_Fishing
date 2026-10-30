#!/bin/sh
# 캐릭터 쇼케이스: VRM을 하나씩 갈아입히며 서 있기·걷기·달리기·캐스팅·감기·파이팅·고기 들기·반응 동작을 게임 창에서 돌려 보고 Screenshots/showcase에 찍는다.
# sh tools/showcase.sh          → 추가 캐릭터 폴더(Builds/Windows/Characters, 정보 .json 있는 것)
# sh tools/showcase.sh all      → 기본 제공·추가·내 폴더 전부 (builtin / pack / user 하나만도 된다)
# 끝나면 캐릭터마다 한 장으로 모은 시트(Screenshots/showcase/sheet_*.png)를 만든다 (tools/showcase_sheet.py).
. "$(dirname "$0")/unity.sh"
OUT="$PROJECT/Screenshots/showcase"
rm -rf "$OUT"
"$PROJECT/Builds/Windows/ProjectFishing.exe" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 \
  -autoshot "$OUT" -autoshotShowcase "${1:-pack}" -logFile "$PROJECT/Logs/player.log" > /dev/null
grep -E "\[Dev\] (쇼케이스|VRM 읽기 실패)|Exception" "$PROJECT/Logs/player.log"
python "$(dirname "$0")/showcase_sheet.py" "$OUT"
