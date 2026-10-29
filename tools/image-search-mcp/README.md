# image-search MCP

키워드로 이미지를 검색해 로컬에 JPG로 받아 두고, 그 경로를 돌려주는 MCP 서버. Claude Code가 `Read`로 열어 실물 사진을 직접 본다. 물고기 모델을 실물 사진과 견줄 때 쓴다 ([09 문서 3-21장](../../docs/09-art-plan.md)).

- 도구: `search_images(query, count=5, max_size=1024)`
- 검색: `BRAVE_API_KEY`가 있으면 Brave Image Search API, 없으면 `ddgs`(키 불필요, 비공식). DuckDuckGo가 "malformed headers"로 자주 실패하지만 ddgs가 Bing으로 넘어가서 받아진다.
- 받은 그림: `~/.cache/image-search-mcp/<키워드>_<시각>/` (`IMAGE_SEARCH_DIR`로 바꿈). 자동으로 지우지 않는다.
- 검색 그림은 저작권이 있을 수 있다. 눈으로 보는 참고용으로만 쓰고 저장소·에셋에 넣지 않는다.

## 설치 (PC마다 한 번)

저장소 루트에서 (Windows, Git Bash):

```bash
python -m venv tools/image-search-mcp/.venv
tools/image-search-mcp/.venv/Scripts/pip install -r tools/image-search-mcp/requirements.txt
claude mcp add image-search --scope user -- "$(cygpath -w "$PWD/tools/image-search-mcp/.venv/Scripts/python.exe")" "$(cygpath -w "$PWD/tools/image-search-mcp/server.py")"
claude mcp list    # image-search: ✔ Connected
```

macOS·Linux는 `.venv/bin/python`, `cygpath` 없이 경로 그대로. Brave 키를 쓰려면 `claude mcp add ... -e BRAVE_API_KEY=키 -- ...`.

MCP 도구는 Claude Code 세션을 새로 열어야 보인다. 그 전에는(또는 MCP 없이) `imgsearch.py`로 바로 쓴다:

```bash
tools/image-search-mcp/.venv/Scripts/python tools/image-search-mcp/imgsearch.py "Siniperca scherzeri side view" 6 out.jpg   # 6장 받아 한 장 모음 out.jpg
```

한국어로 찾으면 엉뚱한 그림이 많다. 학명이나 영어 이름 + "side view"가 잘 나온다.
