using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Fishing.Buddy
{
    /// <summary>
    /// 게임 지식 도구(<see cref="GameKnowledge"/>)를 MCP(Model Context Protocol)로 연다: http://127.0.0.1:8770/mcp
    /// Claude Code 같은 바깥 AI가 실행 중인 게임에 붙어 가이드 문서·어종·낚시터·지금 상태를 읽는다
    /// (예: claude mcp add --transport http project-fishing http://127.0.0.1:8770/mcp).
    /// - Streamable HTTP의 가장 단순한 꼴: POST 한 번 = JSON-RPC 요청 하나 → JSON 응답 하나 (세션·스트림 없음).
    /// - 이 PC 안(127.0.0.1)에서만 받고, 브라우저 페이지가 몰래 부르지 못하게 다른 곳 Origin은 거절한다.
    /// - 요청은 작업 스레드가 받고, 도구는 유니티 객체를 읽으므로 메인 스레드(Update)에서 돌린다.
    /// </summary>
    public class BuddyMcpServer : MonoBehaviour
    {
        const string ProtocolVersion = "2025-06-18";

        HttpListener _listener;
        readonly ConcurrentQueue<(Dictionary<string, object> req, TaskCompletionSource<object> done)> _queue =
            new ConcurrentQueue<(Dictionary<string, object>, TaskCompletionSource<object>)>();

        public static BuddyMcpServer Instance { get; private set; }
        public bool Running => _listener != null && _listener.IsListening;
        public string Endpoint => $"http://127.0.0.1:{BuddyConfig.McpPort}/mcp";

        public static void Ensure(GameObject host)
        {
            if (!BuddyConfig.McpEnabled || Instance != null) return;
            Instance = host.AddComponent<BuddyMcpServer>();
        }

        void Start()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{BuddyConfig.McpPort}/");
                _listener.Start();
                Task.Run(Listen);
                Debug.Log($"[Buddy] MCP 서버 시작: {Endpoint}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Buddy] MCP 서버를 열지 못했습니다 ({Endpoint}): {e.Message}");
                _listener = null;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            try { _listener?.Close(); } catch { /* 이미 닫힘 */ }
            _listener = null;
        }

        async Task Listen()
        {
            while (_listener != null && _listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { return; } // 닫힘
                _ = Task.Run(() => Handle(ctx));
            }
        }

        async Task Handle(HttpListenerContext ctx)
        {
            var res = ctx.Response;
            try
            {
                var origin = ctx.Request.Headers["Origin"];
                if (!string.IsNullOrEmpty(origin) && !origin.StartsWith("http://127.0.0.1") && !origin.StartsWith("http://localhost"))
                {
                    res.StatusCode = 403;
                    return;
                }
                if (ctx.Request.Url.AbsolutePath.TrimEnd('/') != "/mcp") { res.StatusCode = 404; return; }
                if (ctx.Request.HttpMethod != "POST") { res.StatusCode = 405; res.AddHeader("Allow", "POST"); return; }

                string body;
                using (var reader = new System.IO.StreamReader(ctx.Request.InputStream, Encoding.UTF8)) body = await reader.ReadToEndAsync();
                Dictionary<string, object> req;
                try { req = Json.Obj(Json.Parse(body)); }
                catch (Exception e) { await Send(res, Error(null, -32700, "JSON을 읽지 못했습니다: " + e.Message)); return; }
                if (req == null) { await Send(res, Error(null, -32600, "JSON-RPC 요청 하나만 받습니다.")); return; }

                // 알림(id 없음)은 대답하지 않는다
                if (!req.ContainsKey("id")) { res.StatusCode = 202; return; }

                var done = new TaskCompletionSource<object>();
                _queue.Enqueue((req, done));
                var reply = await Task.WhenAny(done.Task, Task.Delay(15000)) == done.Task
                    ? done.Task.Result
                    : Error(req["id"], -32603, "게임이 대답하지 않습니다 (멈춰 있거나 로딩 중).");
                await Send(res, reply);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Buddy] MCP 요청 처리 실패: {e.Message}");
                res.StatusCode = 500;
            }
            finally
            {
                try { res.Close(); } catch { /* 연결 끊김 */ }
            }
        }

        static async Task Send(HttpListenerResponse res, object reply)
        {
            var bytes = Encoding.UTF8.GetBytes(Json.Write(reply));
            res.StatusCode = 200;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        void Update()
        {
            while (_queue.TryDequeue(out var item))
            {
                object reply;
                try { reply = Dispatch(item.req); }
                catch (Exception e) { reply = Error(item.req["id"], -32603, e.Message); }
                item.done.TrySetResult(reply);
            }
        }

        static object Dispatch(Dictionary<string, object> req)
        {
            var id = req["id"];
            var p = Json.Obj(req, "params");
            switch (Json.Str(req, "method"))
            {
                case "initialize":
                    return Result(id, new Dictionary<string, object>
                    {
                        { "protocolVersion", Json.Str(p, "protocolVersion") ?? ProtocolVersion },
                        { "capabilities", new Dictionary<string, object> { { "tools", new Dictionary<string, object>() } } },
                        { "serverInfo", new Dictionary<string, object> { { "name", "project-fishing" }, { "version", Application.version } } },
                        { "instructions", "짭풍명월(낚시 게임)이 실행 중일 때 쓰는 게임 지식 도구. 플레이 가이드 문서와 게임 데이터(어종·낚시터·퀘스트), 플레이어의 지금 상태를 읽는다." },
                    });
                case "ping":
                    return Result(id, new Dictionary<string, object>());
                case "tools/list":
                    return Result(id, new Dictionary<string, object>
                    {
                        { "tools", GameKnowledge.Tools.Select(t => (object)new Dictionary<string, object>
                            { { "name", t.Name }, { "description", t.Description }, { "inputSchema", t.Schema } }).ToList() },
                    });
                case "tools/call":
                {
                    string name = Json.Str(p, "name");
                    if (GameKnowledge.Tools.All(t => t.Name != name)) return Error(id, -32602, $"'{name}' 도구는 없습니다.");
                    string text = GameKnowledge.Call(name, Json.Obj(p, "arguments") ?? new Dictionary<string, object>());
                    return Result(id, new Dictionary<string, object>
                    {
                        { "content", new List<object> { new Dictionary<string, object> { { "type", "text" }, { "text", text } } } },
                        { "isError", false },
                    });
                }
                default:
                    return Error(id, -32601, $"'{Json.Str(req, "method")}'는 지원하지 않습니다.");
            }
        }

        static object Result(object id, object result) =>
            new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "result", result } };

        static object Error(object id, int code, string message) =>
            new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "error", new Dictionary<string, object> { { "code", code }, { "message", message } } } };
    }
}
