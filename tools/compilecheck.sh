#!/bin/sh
# Unity 없이 Assembly-CSharp(런타임 스크립트) 컴파일 오류만 빨리 본다 (에디터가 열려 있거나 다른 작업이 빌드 중일 때).
# Unity가 마지막 빌드에서 만든 rsp(참조·define)를 쓰고, 소스 목록은 지금 Assets 아래 .cs로 바꿔 Unity 안의 Roslyn으로 돌린다.
#   sh tools/compilecheck.sh        → 오류가 없으면 "compile OK"
# 새 asmdef를 만들거나 패키지를 바꾸면 한 번은 tools/build.sh로 rsp를 새로 만들어야 한다.
. "$(dirname "$0")/unity.sh"
EDITOR_DIR="$(dirname "$UNITY")"
RSP=$(ls "$PROJECT"/Library/Bee/artifacts/*/Assembly-CSharp.rsp 2>/dev/null | head -1)
[ -z "$RSP" ] && { echo "rsp 없음: 먼저 sh tools/build.sh"; exit 1; }
TMP="$PROJECT/Temp/compilecheck_$$"  # Windows 경로 (csc가 /tmp를 못 읽는다)
mkdir -p "$TMP"
# 출력 경로를 바꾸고, 소스 목록은 rsp에 있던 것 중 아직 있는 파일 + Assets/Scripts의 새 파일(Editor 제외)
grep -v '^"Assets/' "$RSP" | grep -v '^-out:\|^-refout:' > "$TMP/cc.rsp"
echo "-out:\"$TMP/cc.dll\"" >> "$TMP/cc.rsp"
cd "$PROJECT"
{ grep '^"Assets/' "$RSP" | tr -d '"' | while read -r f; do [ -f "$f" ] && echo "$f"; done
  find Assets/Scripts -name '*.cs' -not -path '*/Editor/*'; } | sort -u | sed 's/.*/"&"/' >> "$TMP/cc.rsp"
"$EDITOR_DIR/Data/NetCoreRuntime/dotnet.exe" "$EDITOR_DIR/Data/DotNetSdkRoslyn/csc.dll" "@$TMP/cc.rsp" > "$TMP/out.txt" 2>&1
grep -E "error CS" "$TMP/out.txt" | sort -u | head -40
if grep -q "error CS" "$TMP/out.txt"; then echo "compile FAILED"; rm -rf "$TMP"; exit 1; fi
echo "compile OK"
rm -rf "$TMP"
