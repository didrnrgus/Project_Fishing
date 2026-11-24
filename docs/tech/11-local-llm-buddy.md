# 11. 로컬 LLM 동료 AI (윤슬)

[← 기술 문서 목차](README.md) · 코드: `Assets/Scripts/Buddy/` (`BuddyBrain`·`BuddyBody`·`BuddyPath`·`BuddySpots`·`OllamaChat`·`OllamaSetup`·`GameKnowledge`·`BuddyMcpServer`), `UI/BuddyChatView.cs` · 플레이어 설명: [가이드 09](../guide/09-buddy.md)

낚시터에서 늘 따라다니는 친구 **윤슬**은 두 부분으로 나뉜다.

| 부분 | 하는 일 | AI가 필요한가 |
|---|---|---|
| **몸** (`BuddyBody`·`BuddyPath`) | 따라 걷기·뛰기, 길 안내(A*), 손으로 가리키기(IK), 배 같이 타기, 반응 동작 | 아니다 — 게임 데이터로 바로 움직인다 |
| **두뇌** (`BuddyBrain`·`OllamaChat`·`GameKnowledge`) | 정해진 반응 말 + 자유 대화(이 PC에서 도는 LLM) | 반응 말은 아니고, 자유 대화만 |

설계 원칙: **AI가 꺼져 있어도 친구 역할은 한다.** 길 안내·반응 말처럼 정답이 게임 데이터에 있는 일은 AI에게 맡기지 않는다. LLM은 "말로 풀어 주기"에만 쓴다.

## 1. 왜 로컬 LLM인가

| 후보 | 결과 |
|---|---|
| Claude/ChatGPT 구독 로그인을 게임에서 쓰기 | 막힘 — Anthropic은 2026-02 약관으로 다른 앱의 구독 로그인 금지, "Sign in with ChatGPT"는 제휴 파트너만 |
| 클라우드 API 키 | 플레이어마다 결제·키 관리가 필요하다. 솔로 게임에 맞지 않는다 |
| **Ollama (이 PC에서 도는 무료 LLM)** | 돈·로그인·인터넷이 필요 없고 대화가 PC 밖으로 나가지 않는다 → **채택** |

Ollama는 `http://127.0.0.1:11434/api/chat`에 HTTP로 붙는다. 게임은 `UnityWebRequest` + 코루틴으로 부르므로 대답을 기다리는 동안 게임이 멈추지 않는다.

### 플레이어가 명령 창을 열지 않게 (`OllamaSetup`)

설치만 하면 나머지는 게임이 한다.

- **켜기**: 대화창을 열 때 `/api/tags`가 안 되면, 기본 설치 자리(`%LOCALAPPDATA%\Programs\Ollama\ollama.exe`, 맥·리눅스 경로) → PATH 순으로 실행 파일을 찾는다. 설치 직후에는 게임이 받은 PATH에 아직 없어서 기본 자리를 먼저 본다. 찾으면 `ollama serve`를 창 없이 켜고 1초마다 `/api/tags`를 본다(25초까지, 실제로는 1초 안). 트레이 앱(`ollama app.exe`)을 켜지 않는 건, 새 버전 앱은 켜면 채팅 창이 떠서 게임을 가리기 때문이다. 주소가 이 PC(127.0.0.1·localhost)일 때만 켠다.
- **끄기**: 게임이 켠 Ollama만 `Application.quitting`에서 끈다. 윈도우는 `taskkill /T`로 모델을 돌리는 자식(runner)까지 끈다. 남겨 두면 그래픽 메모리를 쥐고, 나중에 플레이어가 트레이 앱을 켤 때 포트가 겹친다.
- **모델 받기**: `/api/pull`을 `stream: true`로 부르고 줄마다 오는 `{digest, total, completed}`를 조각별로 모아 전체 진행률을 낸다. 3GB라 `timeout = 0`. Ollama는 조각을 받기 시작할 때에야 크기를 알려 줘서 모은 합으로 나누면 진행률이 뒤로 간다(2.5GB→3.4GB로 늘어남) → 기본 모델 크기(3,350,272,403바이트)와 큰 쪽으로 나누고 뒤로 가지 않게 한다. 1초에 수십 줄이 와서 글자·퍼센트가 바뀔 때만 다시 그린다. 끊겨도 Ollama가 받은 조각을 남겨 두어 다시 누르면 이어 받는다.
- 대화창 가운데 버튼은 상황에 따라 [설치 안내] / [모델 받기 (3.4GB)] / [받는 중 42%]로, 오른쪽 버튼은 [다시 연결] / [Ollama 켜기]로 바뀐다.

## 2. 모델 고르기 — 재 보고 정했다

같은 질문 6개를 RTX 5060 8GB(게임 + Unity 에디터가 같이 그래픽 메모리를 씀)에서 돌려 비교했다.

| 모델 | 대답 시간 | 판단 |
|---|---|---|
| `qwen3:8b` | 10~66초 | 가장 똑똑하지만 6GB를 써서 게임과 나눠 쓰면 VRAM이 넘친다 (7.6/8.1GB, 초당 1~7토큰) |
| `qwen3:4b` | 15~30초 | 도구를 부른 뒤 영어로 1,400토큰 "생각"한다 (`think:false`·`/no_think`가 안 먹음) |
| `qwen3:4b-instruct` | 약 1초 | 도구는 쓰지만 "덜 끊긴다"를 "끊기 쉽다"로 거꾸로 읽고 존댓말이 섞인다 |
| `exaone3.5:2.4b` | 빠름 | 한국어는 좋지만 붙여 준 자료를 그대로 되읊는다 |
| **`gemma3:4b`** | **0.7~1.6초** | 한국어 반말이 가장 자연스럽다. 도구는 못 쓴다 → **기본**, 대신 3장의 "미리 찾기"로 보완 |

교훈: 게임 안 LLM은 **정확도보다 지연 시간과 VRAM 예산**이 먼저다. 게임이 이미 그래픽 메모리를 쓰고 있다는 걸 잊으면 벤치마크 숫자가 의미 없다.

- **미리 올리기(Warm)**: 모델을 처음 VRAM에 올리는 데 최대 30초. 게임을 켤 때 올리면 대화하지 않는 사람의 VRAM을 뺏으니, **대화창을 여는 순간** 빈 요청으로 올린다 → 질문을 타이핑하는 동안 끝난다.
- **스트리밍**: `stream: true`로 받아 한 줄(JSON 하나)씩 이어 붙여 말풍선에 바로 보인다(`DownloadHandlerScript.ReceiveData`). 빈 말풍선으로 기다리는 시간이 사라진다.

## 3. 작은 모델이 지어내지 않게: 질문 전에 사실을 붙인다 (Prefetch)

4B 모델은 도구를 안 부르고 그럴듯하게 지어내기 쉽다(실제로 없는 NPC "박주임"·없는 "낚시배"를 지어냈다). 그래서 **모델을 부르기 전에** 게임이 직접 자료를 찾아 질문 끝에 `[참고 자료]`로 붙인다 — 일종의 경량 RAG다.

```csharp
// GameKnowledge.Prefetch(question)
foreach (var s in FishDatabase.All.Where(s => Mentions(question, s.Name)).Take(2))
    parts.Add(FishInfo(s.Name));                            // 질문에 나온 물고기 → 도감 데이터 표
if (area != null) parts.Add(MapInfo(area.Name));            // 질문에 나온 낚시터 → 입장 레벨·어종
if (Has(q, "퀘스트", "할일", "뭐해야")) parts.Add(QuestInfo());
if (levelQuestion || Has(q, "골드", "장비", "내가")) parts.Add(GameState());   // 지금 내 레벨·장비·다음 낚시터
// 가이드 문서 검색: 조사 떼기("붕어는"→"붕어") + 끝 글자 뗀 줄기("끊겨"→"끊")로 낱말 매칭, 상위 5줄만
```

- **숫자는 데이터 표에서**: 입장 레벨·길이·가격은 문서보다 `FishDatabase`·`FishingAreas`가 정확하다. 문서 검색보다 표를 먼저 붙인다.
- **"다음에 열리는 낚시터"를 한 줄로 계산해서** 넣는다. 작은 모델에게 목록을 주고 레벨을 세게 하면 틀린다 — 셈은 코드가 한다.
- **낱말 경계**: 띄어쓰기를 지운 글에서 그냥 찾으면 "이 게임 물은 어떻게"가 "물은어떻게" → "은어"로 잡힌다. 이름이 낱말 첫머리에 올 때만 맞춘다(`Mentions`).
- **자료는 적게**: 작은 모델은 자료가 많으면 엉뚱한 줄을 고른다. 가이드 줄은 5줄, 개발 문서 줄은 6줄까지.
- **게임 밖 질문**: "이 PC 사양 어때?"면 CPU·RAM·GPU(`SystemInfo`)를, "물은 어떻게 만들었어?"면 개발 문서(`docs/*.md`·`docs/tech`, 빌드는 `StreamingAssets/DevDocs`)를 쉬운 말 → 문서 용어 사전(물 → 수면·물결·Water)으로 넓혀 찾는다.

도구를 쓸 수 있는 모델(qwen3)이면 같은 기능을 **도구 호출**로도 쓴다(아래). gemma3는 도구를 넣으면 400을 돌려주므로 그때는 도구를 빼고 다시 보낸다(`OllamaChat.NoTools`).

## 4. 도구 호출 루프와 MCP 서버 — 같은 도구를 두 곳에서

`GameKnowledge`에 도구 10개가 있다: `game_state` · `list_guides` · `read_guide` · `search_guides` · `fish_info` · `map_info` · `quest_info` · `system_info` · `search_dev_docs` · `read_dev_doc`.

```
질문 → /api/chat (tools 포함, stream)
       ├─ 대답 글 → 말풍선에 흘려 보이기
       └─ tool_calls → GameKnowledge.Call(name, args) → 결과를 role:"tool"로 붙여 다시 /api/chat
          (최대 4번, 그 뒤엔 도구 없이 대답하게)
```

- 도구 결과는 **이번 대답에만** 쓰고 대화 기록에 남기지 않는다. 기록이 길어져 문맥 창(6,144토큰)을 넘지 않게.
- 도구는 Unity 객체를 읽으므로 **메인 스레드에서만** 부른다. 모르는 도구·잘못된 인자도 예외 대신 "그런 도구는 없어" 같은 글로 돌려준다 — 모델이 스스로 고칠 수 있게.

같은 도구를 **MCP 서버**(`BuddyMcpServer`)로도 연다. Claude Code 같은 바깥 AI가 실행 중인 게임에 붙어 가이드·도감·지금 상태를 읽는다.

| 항목 | 구현 |
|---|---|
| 전송 | Streamable HTTP의 가장 단순한 꼴: POST 한 번 = JSON-RPC 요청 하나 → JSON 응답 하나 (세션·SSE 없음) |
| 메서드 | `initialize` · `tools/list` · `tools/call` |
| 보안 | `127.0.0.1:8770`에서만 받고, 다른 `Origin` 헤더는 403 (브라우저 페이지가 몰래 부르는 것 막기). 빌드는 실행 옵션 `-mcp`가 있어야 켜진다 |
| 스레드 | `HttpListener`는 작업 스레드에서 받고, 요청을 큐에 넣어 `Update`(메인 스레드)에서 도구를 돌린 뒤 응답 |
| JSON | 의존성 없이 작은 파서 `Json.cs` (사전·섞인 배열) |

```
claude mcp add --transport http project-fishing http://127.0.0.1:8770/mcp
```

## 5. 대답 다듬기 — 작은 모델의 버릇을 코드로 지운다

| 버릇 | 처리 (`BuddyBrain`) |
|---|---|
| 존댓말 끝("~요", "~습니다")이 섞임 | `Banmal`: 문장 끝을 반말로 (단, "필요"·"중요"처럼 "요"로 끝나는 낱말은 그대로) + 마지막 질문 뒤에 "반말로" 다짐을 한 번 더 |
| 그림 문자·`**`·`[파일.md › 장]`·자료 되읊기 | `Clean` |
| 너무 길다 | `Short`: 말풍선 4문장, 대화창 8문장 |
| 생각하는 모델의 `<think>…</think>`, 앞에 붙이는 "윤슬:" | 보여 주기 전에 지움 (아직 생각 중이면 "음…") |
| 말투 예시를 그대로 따라 말함 | 시스템 프롬프트의 예시를 두 줄로 줄이고 "거친 말 금지"만 남김 |

## 6. 길 안내: AI 없이 데이터로 판단, 1m 칸 A*

"내 퀘스트 좀 알려 줘", "붕어 어디서 잡아?"는 정답이 게임에 있다. LLM에게 물으면 지어낸다. 그래서 `BuddyBrain.TryGuide`가 **낱말로 의도를 먼저 가로챈다**.

1. **퀘스트·안내 낱말**이면 `QuestSystem.MarkFor`로 가장 급한 NPC(보고할 사람 > 이야기 퀘스트 > 받을 퀘스트 > 진행 중)를 고른다 → 정해진 말 + `BuddyBody.Lead(그 NPC)`. 바로 앞 대화가 퀘스트 얘기면 "너가 먼저 안내해 봐"도 안내로 읽는다.
2. **물고기 이름**이면 `BuddySpots`: 그 어종 서식지 칸의 물 → 가장 가까운 물가. 낚싯대가 안 닿으면(먼바다·섬 건너) 배 띄울 물가로.
3. 다른 낚시터·대기실이면 말로만 ("◀ 조합으로 가서…").

### 길 찾기 `BuddyPath`

맵이 모두 코드로 지어져 NavMesh를 굽지 않았다. 대신 **1m 격자 A\***를 쓰되 칸을 **처음 닿을 때만** 검사한다(게으른 격자).

```csharp
static float Probe(Vector2Int c, float near) {
    var top = new Vector3(c.x, near + 1.6f, c.y);              // 옆 칸 높이 조금 위에서 아래로 → 다리 위·아래를 섞지 않는다
    if (!Physics.Raycast(top, Vector3.down, out var hit, 3.5f, Mask)) return float.NaN;
    if (!FieldMap.IsWalkable(hit.point)) return float.NaN;      // 물·갯벌 판정은 맵 정의 함수 그대로
    if (Physics.CheckCapsule(hit.point + up * 0.6f, hit.point + up * 1.5f, 0.28f, Mask)) return float.NaN;  // 몸이 벽·난간에 걸리나
    return hit.point.y;
}
```

- 이웃 8방향, 높이 차 0.7m 넘으면 못 감(계단 OK, 방파제 벽 NG). **대각선은 옆 두 칸도 갈 수 있어야** — 모서리를 깎다 벽에 끼지 않게.
- 열린 목록은 `SortedSet<(f, seq, x, z)>` (seq로 같은 f값 구분). 15,000칸 넘게 열리면 포기하고 곧장 걷는다.
- **줄 당기기(string pulling)**: 찾은 칸 길에서 몸 캡슐이 안 걸리고 사이가 물이 아닌 가장 먼 점까지 건너뛰어 꺾는 점만 남긴다. 한강 낚시점까지 **7ms, 꺾는 점 5개**.
- 따라오기 연출: 플레이어가 뛰면 같이 뛰고, 8m 처지면 서서 돌아보며 부르고(세 번까지), 1.2초 반대로 가면 "아니야, 저기야!" 하고 **오른팔 IK로 가리킨다**(6초에 한 번). 닿으면 "여기야! F 눌러서 말 걸어 봐".

## 7. 결과

- 질문 → 첫 글자까지 **1.4~1.6초** (gemma3:4b, 처음 올릴 때만 8초) — 자동 촬영 `sh tools/autoshot.sh --buddy "질문"`으로 잰다.
- Ollama가 꺼져 있어도: 따라다니기·길 안내·반응 말은 그대로. 깔려 있으면 대화창을 열 때 게임이 켜고(1초 안), 없으면 빨강 ● "Ollama 없음"과 [설치 안내] 버튼. 모델이 없으면 [모델 받기]로 게임 안에서 받는다.
- 남은 과제: "줄이 자꾸 끊겨"처럼 넓은 질문에서 가이드 검색이 엉뚱한 줄을 고를 때가 있다 → 장 제목 가중치.
