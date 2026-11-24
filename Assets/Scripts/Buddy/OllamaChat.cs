using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Fishing.Buddy
{
    /// <summary>
    /// 동료 AI 설정 (기기 설정이라 PlayerPrefs). 대화는 이 PC에서 도는 Ollama(무료·오프라인)에 묻는다.
    /// </summary>
    public static class BuddyConfig
    {
        // gemma3:4b: 8GB 그래픽카드를 게임과 나눠 써도 1초 안팎에 대답하고, 같은 크기 중 한국어(반말·자료 읽기)가 가장 자연스럽다
        // (qwen3:8b는 그래픽 메모리가 넘쳐 20~60초, qwen3:4b-instruct는 빠르지만 "덜 끊긴다"를 "끊기 쉽다"로 읽는 식으로 자주 틀린다).
        // 도구를 못 쓰는 모델이라 사실은 질문 전에 붙이는 자료(GameKnowledge.Prefetch)로 준다
        public const string DefaultModel = "gemma3:4b";
        /// <summary>[모델 받기] 버튼에 보이는 받을 크기와 실제 바이트 (/api/tags의 size, 진행률 분모).</summary>
        public const string DefaultModelSize = "3.4GB";
        public const long DefaultModelBytes = 3_350_272_403;
        public const string DownloadUrl = "https://ollama.com/download";

        public static string OllamaUrl
        {
            get => PlayerPrefs.GetString("buddy.url", "http://127.0.0.1:11434");
            set => PlayerPrefs.SetString("buddy.url", value);
        }

        public static string Model
        {
            // 키 "buddy.model2": 기본이 8b였을 때 저장된 고른 모델을 넘겨받지 않게 (느린 8b에 머물지 않게)
            get => PlayerPrefs.GetString("buddy.model2", DefaultModel);
            set => PlayerPrefs.SetString("buddy.model2", value);
        }

        /// <summary>MCP 서버 (바깥 AI가 게임 지식 도구를 쓰게). 기본은 에디터에서만 켜고, 빌드는 실행 옵션 -mcp로 켠다.</summary>
        public static bool McpEnabled
        {
            get => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mcp") >= 0 ||
                   PlayerPrefs.GetInt("buddy.mcp", Application.isEditor ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt("buddy.mcp", value ? 1 : 0);
        }

        public static int McpPort
        {
            get => PlayerPrefs.GetInt("buddy.mcpPort", 8770);
            set => PlayerPrefs.SetInt("buddy.mcpPort", value);
        }
    }

    /// <summary>
    /// Ollama /api/chat 대화 한 번. 모델이 도구를 부르면(<see cref="GameKnowledge"/>) 결과를 붙여 다시 묻고,
    /// 최대 <see cref="MaxToolRounds"/>번 뒤에는 도구 없이 대답하게 한다. 코루틴이라 게임이 멈추지 않는다.
    /// </summary>
    public static class OllamaChat
    {
        const int MaxToolRounds = 4;
        const int TimeoutSeconds = 180;
        static readonly HashSet<string> NoTools = new HashSet<string>();

        /// <summary>Ollama가 켜져 있으면 깔린 모델 이름 목록, 꺼져 있으면 null.</summary>
        public static IEnumerator ListModels(Action<List<string>> done)
        {
            using var req = UnityWebRequest.Get(BuddyConfig.OllamaUrl + "/api/tags");
            req.timeout = 3;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { done(null); yield break; }
            var models = Json.Arr(Json.Obj(Json.Parse(req.downloadHandler.text)), "models");
            done(models?.Select(m => Json.Str(Json.Obj(m), "name")).Where(n => n != null).ToList() ?? new List<string>());
        }

        /// <summary>
        /// 모델을 그래픽카드에 미리 올린다 (빈 요청). 처음 올리는 데 30초쯤 걸려서, 대화창을 열 때 불러 두면 질문을 쓰는 동안 끝난다.
        /// 게임을 켤 때 올리지 않는 건 대화하지 않는 사람의 그래픽 메모리(4b ≈ 3GB, 8b ≈ 6GB)를 뺏지 않으려고.
        /// </summary>
        public static IEnumerator Warm(string model)
        {
            var body = new Dictionary<string, object> { { "model", model }, { "keep_alive", "30m" }, { "options", new Dictionary<string, object> { { "num_ctx", 6144 } } } };
            using var req = new UnityWebRequest(BuddyConfig.OllamaUrl + "/api/generate", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Write(body)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = TimeoutSeconds;
            yield return req.SendWebRequest();
        }

        /// <summary>
        /// messages(system·user·assistant 차례)를 보내 대답을 받는다. 도구 호출·결과는 messages에 붙지 않고 이번 대화에만 쓴다.
        /// onTool: 도구를 부를 때마다 (말풍선에 "가이드 읽는 중…"을 띄우게). onPartial: 대답이 흘러나오는 대로 지금까지의 글
        /// (스트리밍: 그래픽카드를 게임과 나눠 쓰면 대답에 20초쯤 걸려서, 빈 말풍선 대신 말이 이어서 나오게).
        /// </summary>
        public static IEnumerator Ask(string model, List<Dictionary<string, object>> messages, Action<string> done, Action<string> fail,
            Action<string> onTool = null, Action<string> onPartial = null)
        {
            var convo = new List<object>(messages);
            var tools = GameKnowledge.Tools.Select(t => (object)new Dictionary<string, object>
            {
                { "type", "function" },
                { "function", new Dictionary<string, object> { { "name", t.Name }, { "description", t.Description }, { "parameters", t.Schema } } },
            }).ToList();
            bool think = true; // think:false를 모르는 모델이면 빼고 다시 보낸다
            bool useTools = !NoTools.Contains(model); // 도구를 못 쓰는 모델(gemma3)은 빼고 다시 보낸다: 질문에 붙인 미리 찾은 자료로만 대답한다
            for (int round = 0; round <= MaxToolRounds; round++)
            {
                var body = new Dictionary<string, object>
                {
                    { "model", model },
                    { "messages", convo },
                    { "stream", true },
                    { "keep_alive", "30m" },
                    // 문맥 6144: 8192면 8GB 그래픽카드에서 8b 모델이 넘쳐 몇 배 느려진다 (게임과 나눠 쓴다)
                    { "options", new Dictionary<string, object> { { "num_ctx", 6144 }, { "temperature", 0.3 } } },
                };
                if (round < MaxToolRounds && useTools) body["tools"] = tools;
                if (think) body["think"] = false;

                var stream = new StreamHandler(onPartial);
                using var req = new UnityWebRequest(BuddyConfig.OllamaUrl + "/api/chat", "POST");
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Write(body)));
                req.downloadHandler = stream;
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = TimeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success || stream.Error != null)
                {
                    string err = stream.Error ?? stream.Raw;
                    if (think && err != null && err.Contains("think")) { think = false; round--; continue; }
                    // 400 본문("does not support tools")은 스트림 받개에 안 들어올 때가 있어서, 본문이 없는 400도 도구를 빼고 한 번 더 보낸다
                    if (useTools && req.responseCode == 400 && (err == null || err.Contains("tools"))) { useTools = false; NoTools.Add(model); round--; continue; }
                    fail(req.result == UnityWebRequest.Result.ConnectionError ? null : $"{req.responseCode} {Json.Str(Json.Obj(SafeParse(err)), "error") ?? err ?? req.error}");
                    yield break;
                }

                var calls = stream.ToolCalls;
                if (calls.Count == 0)
                {
                    done(Clean(stream.Content.ToString()));
                    yield break;
                }
                convo.Add(new Dictionary<string, object> { { "role", "assistant" }, { "content", stream.Content.ToString() }, { "tool_calls", calls } });
                foreach (var c in calls)
                {
                    var fn = Json.Obj(Json.Obj(c), "function");
                    string name = Json.Str(fn, "name");
                    // arguments는 보통 객체지만, 글로 오는 모델도 있다
                    var args = Json.Obj(fn, "arguments") ?? Json.Obj(SafeParse(Json.Str(fn, "arguments"))) ?? new Dictionary<string, object>();
                    onTool?.Invoke(name);
                    string result = GameKnowledge.Call(name, args);
                    Debug.Log($"[Buddy] 도구 {name}({Json.Write(args)}) → {result.Length}자");
                    convo.Add(new Dictionary<string, object> { { "role", "tool" }, { "tool_name", name }, { "content", result } });
                }
            }
            fail("도구를 너무 많이 불렀습니다.");
        }

        static object SafeParse(string s)
        {
            try { return Json.Parse(s); }
            catch { return null; }
        }

        /// <summary>
        /// 보여 줄 대답: 생각하는 모델(qwen3 등)이 본문에 남긴 &lt;think&gt;…&lt;/think&gt;를 지우고(아직 생각 중이면 빈칸),
        /// 모델이 가끔 앞에 붙이는 "윤슬:"을 뗀다.
        /// </summary>
        public static string Clean(string s)
        {
            int end = s.IndexOf("</think>", StringComparison.Ordinal);
            if (end >= 0) s = s.Substring(end + 8);
            else if (s.TrimStart().StartsWith("<think>", StringComparison.Ordinal)) return "";
            s = s.Trim();
            foreach (var prefix in new[] { BuddyBrain.BuddyName + ":", BuddyBrain.BuddyName + " :", BuddyBrain.BuddyName + "：" })
                if (s.StartsWith(prefix, StringComparison.Ordinal)) s = s.Substring(prefix.Length).TrimStart();
            // 작은 모델이 붙이는 것들: 붙여 준 자료를 되읊기([참고 자료] 뒤는 버린다), 출처 "[README.md › 파이팅]", 굵게 **, 그림 문자(글꼴에 없어 네모로 보인다)
            int echo = s.IndexOf("[참고", StringComparison.Ordinal);
            if (echo > 0) s = s.Substring(0, echo);
            s = Regex.Replace(s, @"\[[^\]\n]*\.md[^\]\n]*\]\s*(에|에서|에도)?\s*", "");
            s = s.Replace("**", "");
            s = Regex.Replace(s, @"[\uD800-\uDFFF☀-➿️]", "");
            return Regex.Replace(s, @"[ \t]{2,}", " ").Trim();
        }

        /// <summary>
        /// Ollama 스트리밍 대답(한 줄에 JSON 하나)을 받는 대로 읽는다. 본문 조각은 이어 붙여 onPartial로 알리고, 도구 호출은 모은다.
        /// 유니티가 메인 스레드에서 ReceiveData를 부른다.
        /// </summary>
        sealed class StreamHandler : DownloadHandlerScript
        {
            readonly Action<string> _onPartial;
            readonly List<byte> _pending = new List<byte>();
            public readonly StringBuilder Content = new StringBuilder();
            public readonly List<object> ToolCalls = new List<object>();
            public string Error;
            /// <summary>JSON 줄이 아닌 응답 (오류 본문).</summary>
            public string Raw;

            public StreamHandler(Action<string> onPartial) : base(new byte[4096]) { _onPartial = onPartial; }

            protected override bool ReceiveData(byte[] data, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    if (data[i] != (byte)'\n') { _pending.Add(data[i]); continue; }
                    Line(Encoding.UTF8.GetString(_pending.ToArray()));
                    _pending.Clear();
                }
                return true;
            }

            protected override void CompleteContent()
            {
                if (_pending.Count > 0) Line(Encoding.UTF8.GetString(_pending.ToArray()));
                _pending.Clear();
            }

            void Line(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                var o = Json.Obj(SafeParse(line));
                if (o == null) { Raw = (Raw ?? "") + line; return; }
                if (Json.Str(o, "error") is string err) { Error = err; return; }
                var msg = Json.Obj(o, "message");
                if (Json.Arr(msg, "tool_calls") is List<object> calls) ToolCalls.AddRange(calls);
                var piece = Json.Str(msg, "content");
                if (string.IsNullOrEmpty(piece)) return;
                Content.Append(piece);
                if (ToolCalls.Count == 0) _onPartial?.Invoke(Clean(Content.ToString()));
            }
        }
    }
}
