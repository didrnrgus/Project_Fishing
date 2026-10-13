#!/bin/sh
# 윈도우 빌드: Builds/Windows/ProjectFishing.exe
. "$(dirname "$0")/unity.sh"
"$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod Fishing.EditorTools.ProjectTools.BuildWindows -logFile "$PROJECT/Logs/build.log"
grep -E "error CS|\[Build\]" "$PROJECT/Logs/build.log"
