#!/bin/sh
# 파이팅 밸런스 시뮬레이션 → docs/balance-report.md  (요약만: ./tools/balance.sh -simSummary)
. "$(dirname "$0")/unity.sh"
"$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod Fishing.EditorTools.BalanceSim.Run -logFile "$PROJECT/Logs/sim.log" "$@"
grep -E "error CS" "$PROJECT/Logs/sim.log"
echo "결과: docs/balance-report.md"
