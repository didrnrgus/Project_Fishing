# Project_Fishing — Claude Code 지침

개발 문서는 `docs/`에 있다 (시작은 [docs/05-dev-guide.md](docs/05-dev-guide.md), 진행 상황은 [docs/WORK-IN-PROGRESS.md](docs/WORK-IN-PROGRESS.md)).

## 커밋 메시지 (반드시 지킬 것)

제목 한 줄 + 빈 줄 + 세부작업 줄 목록. 쉼표·세미콜론으로 한 줄에 몰아 쓰지 않는다.

```
작업 - [메인 피처]

- 세부작업1
- 세부작업2
- 세부작업3
```

- 제목은 메인 피처만 짧게. 세부 변경·문서·스크린샷·빌드/테스트 결과는 `- ` 줄로 나눈다.
- 제목 다음 빈 줄을 빼먹지 않는다 (없으면 `git log --oneline`에 목록까지 한 줄로 붙는다).
- 자세한 예는 [docs/05-dev-guide.md](docs/05-dev-guide.md)의 Git 절.
