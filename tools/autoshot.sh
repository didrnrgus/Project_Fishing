#!/bin/sh
# 빌드된 게임을 자동 연출 모드로 실행해 Screenshots/에 주요 장면(약 150장)을 저장한다. (세이브 파일은 건드리지 않음)
# sh tools/autoshot.sh 06_fight  → 그 장면까지만 찍고 끝낸다 (한 장면 빨리 확인)
. "$(dirname "$0")/unity.sh"
rm -rf "$PROJECT/Screenshots"
"$PROJECT/Builds/Windows/ProjectFishing.exe" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 \
  -autoshot "$PROJECT/Screenshots" ${1:+-autoshotStop "$1"} -logFile "$PROJECT/Logs/player.log" > /dev/null
grep -E "\[Dev\]|Exception" "$PROJECT/Logs/player.log"
# 맵별 평균 FPS (스크린샷 로그의 "(NN FPS, 맵, 정적 배칭 N개)")
grep -oE "\([0-9]+ FPS, [a-z0-9_]+, 정적 배칭 [0-9]+개\)" "$PROJECT/Logs/player.log" | tr -d '()개,' |
  awk '{f[$3]+=$1; n[$3]++; b[$3]=$6} END {for (m in f) printf "FPS %-12s 평균 %5.0f (%d장, 배칭 %d개)\n", m, f[m]/n[m], n[m], b[m]}' | sort
