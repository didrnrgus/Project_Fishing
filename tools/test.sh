#!/bin/sh
# 게임 규칙 단위 테스트 (EditMode) → Logs/test-results.xml. 실패가 있으면 0이 아닌 값으로 끝난다.
. "$(dirname "$0")/unity.sh"
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform EditMode \
  -testResults "$PROJECT/Logs/test-results.xml" -logFile "$PROJECT/Logs/test.log"
code=$?
grep -E "error CS" "$PROJECT/Logs/test.log"
# 요약: 전체/통과/실패
grep -o '<test-run [^>]*' "$PROJECT/Logs/test-results.xml" | grep -oE '(total|passed|failed)="[0-9]+"' | tr '\n' ' '
echo
grep -B1 -A3 'result="Failed"' "$PROJECT/Logs/test-results.xml" | grep -E 'name=|message' | head -40
exit $code
