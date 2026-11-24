using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Fishing.Buddy
{
    /// <summary>
    /// 플레이어가 명령 창을 열지 않아도 되게: 깔려 있는데 꺼진 Ollama를 켜고(<see cref="Launch"/>), 모델을 게임 안에서 받는다(<see cref="Pull"/>).
    /// 켜는 건 이 PC 주소(127.0.0.1·localhost)일 때만. 게임이 켠 Ollama는 게임을 끌 때 같이 끈다 (그래픽 메모리·포트를 남기지 않게).
    /// </summary>
    public static class OllamaSetup
    {
        static System.Diagnostics.Process _started;
        static bool _quitHooked;

        /// <summary>이 PC의 ollama 실행 파일 (없으면 null). 기본 설치 자리 → PATH 차례로 찾는다.</summary>
        public static string FindExecutable()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            var candidates = new List<string>();
            string exe = Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor ? "ollama.exe" : "ollama";
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(local)) candidates.Add(Path.Combine(local, "Programs", "Ollama", exe)); // 윈도우 기본 설치
            candidates.Add("/Applications/Ollama.app/Contents/Resources/ollama"); // 맥
            candidates.Add("/usr/local/bin/ollama");
            candidates.Add("/opt/homebrew/bin/ollama");
            candidates.Add("/usr/bin/ollama");
            // 설치 직후엔 게임이 받은 PATH에 아직 없을 수 있어서 기본 자리를 먼저 본다
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                if (!string.IsNullOrWhiteSpace(dir)) candidates.Add(Path.Combine(dir.Trim('"'), exe));
            foreach (var c in candidates)
            {
                try { if (File.Exists(c)) return c; }
                catch { /* 이상한 PATH 항목 */ }
            }
#endif
            return null;
        }

        public static bool Installed => FindExecutable() != null;

        /// <summary>Ollama 주소가 이 PC인가 (다른 PC의 Ollama는 켜 줄 수 없다).</summary>
        public static bool IsLocal
        {
            get
            {
                try
                {
                    string host = new Uri(BuddyConfig.OllamaUrl).Host;
                    return host is "127.0.0.1" or "localhost" or "::1" or "[::1]";
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// 깔려 있는 Ollama를 창 없이 켠다 (<c>ollama serve</c>). 켜졌는지는 부른 쪽이 /api/tags로 확인한다.
        /// 트레이 앱(ollama app.exe)이 아니라 serve를 켜는 건, 새 버전 앱은 켜면 채팅 창이 떠서 게임 위를 가리기 때문.
        /// </summary>
        public static bool Launch()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (!IsLocal) return false;
            if (_started != null && !_started.HasExited) return true;
            string exe = FindExecutable();
            if (exe == null) return false;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo(exe, "serve")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(exe),
                };
                // 게임에 다른 주소를 넣었으면(포트만 다른 이 PC) 그 주소로 켠다
                var uri = new Uri(BuddyConfig.OllamaUrl);
                info.EnvironmentVariables["OLLAMA_HOST"] = $"127.0.0.1:{uri.Port}";
                _started = System.Diagnostics.Process.Start(info);
                if (!_quitHooked) { _quitHooked = true; Application.quitting += StopStarted; }
                Debug.Log($"[Buddy] Ollama 켬: {exe} serve (pid {_started?.Id})");
                return _started != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Buddy] Ollama를 켜지 못함: {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>게임이 켠 Ollama만 끈다 (플레이어가 직접 켠 건 건드리지 않는다). 윈도우는 모델을 돌리는 자식(runner)까지.</summary>
        static void StopStarted()
        {
            var p = _started;
            _started = null;
            if (p == null) return;
            try
            {
                if (p.HasExited) return;
                if (Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor)
                {
                    using var kill = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("taskkill", $"/PID {p.Id} /T /F")
                    { UseShellExecute = false, CreateNoWindow = true });
                    kill?.WaitForExit(3000);
                }
                else p.Kill();
                Debug.Log("[Buddy] 게임이 켠 Ollama를 끔");
            }
            catch (Exception e) { Debug.LogWarning($"[Buddy] Ollama 끄기 실패: {e.Message}"); }
            finally { p.Dispose(); }
        }

        /// <summary>
        /// 모델을 받는다 (/api/pull, 스트리밍). progress(글, 0~1 또는 모르면 -1)를 받는 대로 알리고, 끝나면 done(null) 또는 done(오류).
        /// 받다 끊겨도 Ollama가 받은 조각을 남겨 두어 다음에 이어 받는다.
        /// expectedBytes: 받을 전체 크기를 알면 (Ollama는 조각을 받기 시작할 때에야 그 크기를 알려 줘서, 모은 합으로 나누면 진행률이 뒤로 간다).
        /// </summary>
        public static IEnumerator Pull(string model, long expectedBytes, Action<string, float> progress, Action<string> done)
        {
            var handler = new PullHandler(progress, expectedBytes);
            using var req = new UnityWebRequest(BuddyConfig.OllamaUrl + "/api/pull", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Write(new Dictionary<string, object> { { "model", model }, { "stream", true } })));
            req.downloadHandler = handler;
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 0; // 3GB라 몇 분~몇십 분: 끊지 않는다
            yield return req.SendWebRequest();
            if (handler.Error != null) done(handler.Error);
            else if (req.result != UnityWebRequest.Result.Success) done(req.result == UnityWebRequest.Result.ConnectionError ? "Ollama와 연결이 끊겼어요" : $"{req.responseCode} {req.error}");
            else if (!handler.Succeeded) done("받기가 끝나지 않았어요");
            else done(null);
        }

        /// <summary>/api/pull 줄({"status","digest","total","completed"})을 읽어 조각별 크기를 모아 전체 진행률을 낸다.</summary>
        sealed class PullHandler : DownloadHandlerScript
        {
            readonly Action<string, float> _progress;
            readonly long _expected;
            float _shown; // 지금까지 보인 진행률 (뒤로 가지 않게)
            readonly List<byte> _pending = new List<byte>();
            readonly Dictionary<string, (long done, long total)> _layers = new Dictionary<string, (long, long)>();
            public string Error;
            public bool Succeeded;

            public PullHandler(Action<string, float> progress, long expected) : base(new byte[8192]) { _progress = progress; _expected = expected; }

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
                Dictionary<string, object> o;
                try { o = Json.Obj(Json.Parse(line)); }
                catch { o = null; }
                if (o == null) { Error ??= line.Trim(); return; }
                if (Json.Str(o, "error") is string err) { Error = err; return; }
                string status = Json.Str(o, "status") ?? "";
                if (status == "success") { Succeeded = true; _progress?.Invoke("다 받았어요", 1f); return; }
                string digest = Json.Str(o, "digest");
                long total = Long(o, "total"), completed = Long(o, "completed");
                if (digest != null && total > 0) _layers[digest] = (completed, total);
                long sumDone = _layers.Values.Sum(l => l.done);
                long sumTotal = Math.Max(_layers.Values.Sum(l => l.total), _expected);
                if (sumTotal > 0) _shown = Mathf.Max(_shown, Mathf.Clamp01((float)sumDone / sumTotal));
                if (status.StartsWith("pulling ", StringComparison.Ordinal) && sumTotal > 0)
                    _progress?.Invoke($"받는 중 {sumDone / 1e9:0.0}/{sumTotal / 1e9:0.0}GB", _shown);
                else
                    _progress?.Invoke(status switch
                    {
                        "pulling manifest" => "목록 받는 중…",
                        "verifying sha256 digest" => "받은 파일 확인 중…",
                        "writing manifest" => "정리하는 중…",
                        _ => status,
                    }, sumTotal > 0 ? _shown : -1f);
            }

            static long Long(Dictionary<string, object> o, string key) =>
                o != null && o.TryGetValue(key, out var v) && v != null ? Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture) : 0;
        }
    }
}
