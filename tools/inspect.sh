#!/bin/sh
# 모델 디테일 확인: 맵을 열고 이름으로 찾은 물체를 HUD 없이 가까이 찍는다 → Screenshots/i_맵_물체_번호.png
#   sh tools/inspect.sh "seocheon:Pavilion:200:15:1.6;sentosa:merlion:230:5:1.4"
#   항목 = 맵:물체이름:방위각:고도각:거리배수[:시각(분)]  (거리배수 = 경계 반지름의 배수)
. "$(dirname "$0")/unity.sh"
rm -rf "$PROJECT/Screenshots"
"$PROJECT/Builds/Windows/ProjectFishing.exe" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 \
  -autoshot "$PROJECT/Screenshots" -autoshotInspect "$1" -logFile "$PROJECT/Logs/player.log" > /dev/null
grep -E "\[Dev\]|Exception" "$PROJECT/Logs/player.log"
