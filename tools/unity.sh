#!/bin/sh
# 공통: 이 프로젝트용 Unity 6000.3.17f1 경로. 다른 경로에 설치했다면 UNITY 환경변수로 덮어쓰세요.
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe}"
# Git Bash에서는 pwd -W가 C:/... 형식 경로를 돌려준다 (Unity 인자로 필요)
PROJECT="$(cd "$(dirname "$0")/.." && (pwd -W 2>/dev/null || pwd))"
