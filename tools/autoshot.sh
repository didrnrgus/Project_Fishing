#!/bin/sh
# 빌드된 게임을 자동 연출 모드로 실행해 Screenshots/에 주요 장면 10장을 저장한다. (세이브 파일은 건드리지 않음)
. "$(dirname "$0")/unity.sh"
rm -rf "$PROJECT/Screenshots"
"$PROJECT/Builds/Windows/ProjectFishing.exe" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 \
  -autoshot "$PROJECT/Screenshots" -logFile "$PROJECT/Logs/player.log" > /dev/null
grep -E "\[Dev\]|Exception" "$PROJECT/Logs/player.log"
