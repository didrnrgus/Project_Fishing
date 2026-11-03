using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Buddy
{
    /// <summary>
    /// 따라다니는 낚시 친구 "윤슬"의 두뇌.
    ///  - 대화: 플레이어가 물으면 이 PC의 Ollama(무료·오프라인)에 성격·지금 상황·최근 대화를 붙여 묻는다. 모델은 게임 지식 도구로 가이드·데이터를 찾아 읽고 대답한다.
    ///  - 반응: 낚시 결과·레벨 업·퀘스트 완료·새 낚시터에 정해진 짧은 말을 한다 (AI 없이도 친구 역할, 바로 나온다).
    /// 몸(<see cref="BuddyBody"/>)은 <see cref="Said"/>를 받아 말풍선·동작을 보이고, 대화창(UI.BuddyChatView)은 <see cref="Log"/>를 그린다.
    /// </summary>
    public class BuddyBrain : MonoBehaviour
    {
        public const string BuddyName = "윤슬";
        const int MaxHistory = 16;

        public enum Status { Checking, Offline, NoModel, Ready, Thinking }

        public static BuddyBrain Instance { get; private set; }
        public Status State { get; private set; } = Status.Checking;
        public List<string> Models { get; private set; } = new List<string>();
        /// <summary>실제로 쓰는 모델 (설정한 모델이 없으면 깔린 것 중 첫째).</summary>
        public string Model { get; private set; }

        /// <summary>대화창에 보이는 줄 (fromPlayer, 글).</summary>
        public readonly List<(bool fromPlayer, string text)> Log = new List<(bool, string)>();
        public event Action Changed;
        /// <summary>윤슬이 말했다 (말풍선). 두 번째 값: 반응 동작 (CharacterAnimator 클립 이름, 없으면 null).</summary>
        public event Action<string, string> Said;
        /// <summary>플레이어가 윤슬에게 말했다 (플레이어 머리 위 말풍선).</summary>
        public event Action<string> Heard;
        /// <summary>대답을 기다리는 동안 말풍선에 띄울 글 ("가이드 읽는 중…"). null이면 없음.</summary>
        public string Working { get; private set; }
        /// <summary>흘러나오는 중인 대답 (스트리밍). 다 나오면 null.</summary>
        public string Partial { get; private set; }

        readonly List<Dictionary<string, object>> _history = new List<Dictionary<string, object>>();
        string _mapId;
        bool _greeted;

        public static BuddyBrain Ensure(GameObject host)
        {
            if (Instance == null) Instance = host.AddComponent<BuddyBrain>();
            return Instance;
        }

        void Start()
        {
            if (Game.Fishing != null) Game.Fishing.FightEnded += OnFightEnded;
            if (Game.Profile != null) Game.Profile.LeveledUp += OnLeveledUp;
            QuestSystem.QuestCompleted += OnQuestCompleted;
            CheckOllama();
        }

        void OnDestroy()
        {
            if (Game.Fishing != null) Game.Fishing.FightEnded -= OnFightEnded;
            if (Game.Profile != null) Game.Profile.LeveledUp -= OnLeveledUp;
            QuestSystem.QuestCompleted -= OnQuestCompleted;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            // 낚시터에 들어오면 한마디 (처음 오는 곳이면 소개, 아니면 짧게)
            if (Game.Mode != GameMode.Field) { _mapId = null; return; }
            if (_mapId == FieldMap.Id) return;
            bool first = _mapId == null && !_greeted;
            _mapId = FieldMap.Id;
            StartCoroutine(GreetLater(first));
        }

        IEnumerator GreetLater(bool first)
        {
            yield return new WaitForSeconds(2.5f);
            if (Game.Mode != GameMode.Field) yield break;
            string map = FieldMap.ShortName;
            if (first)
            {
                _greeted = true;
                Say(State == Status.Ready
                    ? $"안녕! 나는 {BuddyName}, 오늘도 같이 낚시하자. 궁금한 건 Enter 눌러서 물어봐. {map} 얘기든 물고기 얘기든 다 좋아!"
                    : $"안녕! 나는 {BuddyName}, 오늘도 같이 낚시하자. (Enter로 대화창을 열 수 있어. 나랑 이야기하려면 Ollama가 필요해!)", CharacterAnimatorWave);
            }
            else Say(Pick($"{map} 도착! 어디부터 던져 볼까?", $"{map}이다. 오늘은 뭐가 물까?", $"좋아, {map}! 바람 냄새부터 다르네."));
        }

        const string CharacterAnimatorWave = Fishing.Characters.CharacterAnimator.Wave;

        // ───────────── Ollama 연결 ─────────────

        /// <summary>Ollama가 켜져 있는지, 모델이 깔려 있는지 확인한다 (대화창을 열 때마다). warm이면 쓸 모델을 미리 그래픽카드에 올린다.</summary>
        public void CheckOllama(bool warm = false)
        {
            if (State == Status.Thinking) return;
            StartCoroutine(OllamaChat.ListModels(models =>
            {
                Models = models ?? new List<string>();
                // 확인하는 사이에 질문을 보냈으면 대답을 기다리는 상태를 덮어쓰지 않는다
                if (State == Status.Thinking) { Changed?.Invoke(); return; }
                if (models == null) { State = Status.Offline; Model = null; }
                else if (models.Count == 0) { State = Status.NoModel; Model = null; }
                else
                {
                    Model = models.Contains(BuddyConfig.Model) ? BuddyConfig.Model
                        : models.FirstOrDefault(m => m.StartsWith(BuddyConfig.Model + ":")) ?? models[0];
                    State = Status.Ready;
                    if (warm) StartCoroutine(OllamaChat.Warm(Model));
                }
                Changed?.Invoke();
            }));
        }

        /// <summary>깔린 모델 중 다음 것으로 바꾼다 (대화창 [모델] 버튼).</summary>
        public void NextModel()
        {
            if (Models.Count == 0) return;
            int i = Models.IndexOf(Model);
            Model = Models[(i + 1) % Models.Count];
            BuddyConfig.Model = Model;
            StartCoroutine(OllamaChat.Warm(Model));
            Changed?.Invoke();
        }

        // ───────────── 대화 ─────────────

        public void Ask(string question)
        {
            question = question?.Trim();
            if (string.IsNullOrEmpty(question) || State == Status.Thinking) return;
            Log.Add((true, question));
            Heard?.Invoke(question);
            if (State != Status.Ready)
            {
                Reply(State == Status.Offline
                    ? "앗, 지금은 머리가 안 돌아가… Ollama가 꺼져 있나 봐. 아래 [설치 안내]를 보고 Ollama를 켜 주면 뭐든 대답해 줄게!"
                    : $"Ollama는 켜져 있는데 모델이 없어. 명령 창에서 'ollama pull {BuddyConfig.DefaultModel}'를 한 번 해 줘!", null, false);
                return;
            }
            _history.Add(Message("user", question));
            StartCoroutine(AskRoutine());
        }

        IEnumerator AskRoutine()
        {
            State = Status.Thinking;
            Working = "음… 생각 중";
            Changed?.Invoke();
            var messages = new List<Dictionary<string, object>> { Message("system", SystemPrompt()) };
            messages.AddRange(_history);
            // 마지막 질문 끝에만 미리 찾은 자료와 짧은 다짐을 붙인다 (모델은 맨 앞 시스템 글보다 최근 말을 잘 따른다). 기록·대화창에는 원래 질문만 남는다
            var last = messages[messages.Count - 1];
            string question = (string)last["content"];
            string facts = GameKnowledge.Prefetch(question);
            Debug.Log($"[Buddy] 미리 찾은 자료 {facts?.Length ?? 0}자\n{facts}");
            messages[messages.Count - 1] = Message("user", question +
                (facts != null ? $"\n\n[참고 자료 — 게임에서 찾아 둔 사실. 숫자·장소는 이것만 믿어]\n{facts}" : "") +
                "\n\n(윤슬은 반말로 2~3문장만. 모자라면 도구로 더 찾고, 자료·도구에 없는 건 지어내지 않기.)");
            string answer = null, error = null;
            bool offline = false;
            float started = Time.realtimeSinceStartup;
            yield return OllamaChat.Ask(Model, messages,
                a => answer = a,
                e => { error = e; offline = e == null; },
                tool =>
                {
                    Working = tool switch
                    {
                        "read_guide" or "list_guides" or "search_guides" => "가이드 펼쳐 보는 중…",
                        "fish_info" => "도감 찾아보는 중…",
                        "map_info" => "낚시터 지도 보는 중…",
                        "quest_info" => "퀘스트 확인하는 중…",
                        _ => "지금 상황 보는 중…",
                    };
                    Changed?.Invoke();
                },
                partial =>
                {
                    if (partial.Length == 0) return;
                    Partial = Banmal(partial);
                    Changed?.Invoke();
                });
            Working = null;
            Partial = null;
            State = Status.Ready;
            Debug.Log($"[Buddy] 대답 {Time.realtimeSinceStartup - started:0.0}초 ({Model})");
            if (offline)
            {
                _history.RemoveAt(_history.Count - 1);
                State = Status.Offline;
                Reply("어라, Ollama랑 연결이 끊겼어. Ollama가 켜져 있는지 봐 줄래?", null, false);
                yield break;
            }
            if (error != null)
            {
                _history.RemoveAt(_history.Count - 1);
                Reply($"미안, 대답하다가 꼬였어… ({error})", null, false);
                yield break;
            }
            if (string.IsNullOrWhiteSpace(answer)) answer = "음… 뭐라고 해야 할지 모르겠어. 다르게 물어봐 줄래?";
            Reply(Short(Banmal(answer)), null, true);
        }

        // 말풍선에 들어가게 4문장까지 (작은 모델은 "2~3문장"을 자주 넘긴다). 줄바꿈 문단도 한 줄로 잇는다
        static readonly System.Text.RegularExpressions.Regex SentenceEnd = new System.Text.RegularExpressions.Regex(@"(?<=[.!?~])\s+");

        static string Short(string s)
        {
            var sentences = SentenceEnd.Split(s.Replace("\r", "")).Select(x => x.Replace("\n", " ").Trim()).Where(x => x.Length > 0).ToList();
            return string.Join(" ", sentences.Take(4));
        }

        // 작은 모델이 가끔 섞는 존댓말 끝("살아요", "좋아요", "이에요")을 반말로. "필요"·"중요"처럼 낱말 속 "요"는 앞 글자를 보고 건드리지 않는다
        static readonly System.Text.RegularExpressions.Regex PoliteEnd =
            new System.Text.RegularExpressions.Regex(@"(이에요|예요|에요|주세요|세요|([아어해여지네래게걸데군나까라봐줘워와돼가서자죠])요)(?=[\s.,!?~…)]|$)");

        static string Banmal(string s) => PoliteEnd.Replace(s, m => m.Value switch
        {
            "이에요" => "이야",
            "예요" or "에요" => "야",
            "주세요" => "줘",
            "세요" => "",
            _ => m.Groups[2].Value == "죠" ? "지" : m.Groups[2].Value,
        });

        /// <summary>정해진 반응 말. 대화 기록에도 남겨서, 뒤에 물어보면 AI가 앞뒤를 안다.</summary>
        public void Say(string line, string motion = null) => Reply(line, motion, true);

        void Reply(string text, string motion, bool remember)
        {
            Log.Add((false, text));
            while (Log.Count > 60) Log.RemoveAt(0);
            if (remember)
            {
                _history.Add(Message("assistant", text));
                while (_history.Count > MaxHistory) _history.RemoveAt(0);
                // 맨 앞이 대답이면 모델이 헷갈리지 않게 질문부터 시작하도록 자른다
                while (_history.Count > 0 && (string)_history[0]["role"] != "user" && _history.Count > MaxHistory / 2) _history.RemoveAt(0);
            }
            Said?.Invoke(text, motion);
            Changed?.Invoke();
        }

        static Dictionary<string, object> Message(string role, string content) =>
            new Dictionary<string, object> { { "role", role }, { "content", content } };

        string SystemPrompt()
        {
            var p = Game.Profile;
            string player = p != null ? p.BuildLook().Name : "플레이어";
            string where = Game.Mode == GameMode.Lobby ? "한국 낚시 조합(대기실)" : FieldMap.MapName;
            var q = Game.Quests?.Current;
            return
$@"너는 낚시 게임 「짭풍명월」에서 플레이어 곁을 따라다니는 낚시 친구 '{BuddyName}'이야. 플레이어 이름은 {player}.
말투: 오래된 친구처럼 반말로만, 밝고 다정하게 (-요, -습니다 같은 존댓말과 '젠장' 같은 거친 말은 쓰지 마). 한국어로만 말해.
길이: 말풍선에 들어가게 2~4문장. 목록이 꼭 필요하면 3줄까지만. 마크다운 기호(#, *, |)는 쓰지 마.
게임 질문(조작, 물고기, 낚시터, 장비, 미끼, 퀘스트, 레벨, 배, 대회)에는 먼저 도구로 확인하고 대답해:
- '어떻게 해?', '왜 안 돼?', 요령·조작·장비 고르기·문제(줄이 끊김, 입질이 없음, 바늘이 빠짐)는 search_guides로 핵심 낱말 한두 개를 찾아. 필요하면 read_guide로 그 장을 읽어.
- 물고기 하나는 fish_info, 낚시터는 map_info, 퀘스트는 quest_info, 플레이어 레벨·장비·있는 곳은 game_state.
- 숫자(레벨·길이·가격·시간)는 도구 결과에 있는 것만 써. 도구에도 없으면 지어내지 말고 '그건 나도 잘 모르겠어'라고 해.
질문 끝에 [참고 자료]가 붙어 있으면 그중 질문에 딱 맞는 한두 가지만 골라 쉬운 말로 풀어 대답해. 자료를 그대로 옮기거나 파일 이름을 말하지는 마.
말투 예시: (미끼는 뭐가 좋아?) → 붕어 노릴 거면 떡밥이 제일 무난해! 지렁이도 잘 먹으니까 둘 다 챙겨 가자. / (오늘 힘들다) → 그럴 땐 찌만 멍하니 봐도 좋잖아. 한 마리만 낚고 쉬자!
게임 밖 이야기도 가볍게 받아 주되, 자연스럽게 낚시 이야기로 돌아와.
지금 상황: {where}, Lv.{p?.Data.level ?? 1}, {(Game.Clock != null ? $"{Game.Clock.TimeText} {Names.Of(Game.Clock.Band)}" : "")}, 날씨 {(Game.Weather != null ? Names.Of(Game.Weather.Current) : "?")}, 미끼 {p?.Bait.Name}{(q != null ? $", 이야기 퀘스트 「{q.Title}」({q.GoalText})" : "")}.";
        }

        // ───────────── 반응 ─────────────

        void OnFightEnded(FightOutcome outcome)
        {
            var f = Game.Fishing.Fight;
            if (f == null || outcome == FightOutcome.None) return;
            var s = f.Species;
            switch (outcome)
            {
                case FightOutcome.Landed:
                {
                    // 결과 창이 뜨기 전(도감 기록 전)이라 이전 기록과 비교할 수 있다
                    var dex = Game.Profile.Dex(s.Id);
                    string len = $"{f.Length:0.#}cm";
                    if (s.IsJunk) Say(Pick($"{s.Name}…? 하하, 그것도 다 추억이야.", $"이번엔 {s.Name}이네. 다음엔 진짜 고기 낚자!"));
                    else if (dex == null) Say(Pick($"와, {s.Name}! 처음 잡아 보는 거지? 도감에 새로 올라가겠다!", $"{s.Name} 첫 기록! 오늘 운 좋은데?"), Fishing.Characters.CharacterAnimator.Cheer);
                    else if (s.IsTrophy(f.Length)) Say(Pick($"{len} 대물 {s.Name}! 대박, 손 떨리지 않아?", $"우와 {s.Name} {len}! 이건 자랑해야 돼!"), Fishing.Characters.CharacterAnimator.Cheer);
                    else if (f.Length > dex.maxLength) Say($"{s.Name} 최고 기록 갱신! 전에는 {dex.maxLength:0.#}cm였잖아.", Fishing.Characters.CharacterAnimator.Clap);
                    else if (UnityEngine.Random.value < 0.35f) Say(Pick($"{s.Name} {len}, 좋아 그 느낌이야.", $"나이스! {s.Name}이다.", $"{s.Name} 손맛 어때?"), Fishing.Characters.CharacterAnimator.Clap);
                    break;
                }
                case FightOutcome.LineBroken:
                    Say(Pick("아까워…! 장력이 빨간 칸까지 가면 줄이 끊겨. 세게 당길 땐 잠깐 쉬어 줘.", $"줄이 끊겼어… {s.Name}였는데. 장력 막대를 보면서 다시 해 보자."));
                    break;
                case FightOutcome.HookOut:
                    Say("바늘이 빠졌네. 줄을 너무 느슨하게 두면 빠지더라. 계속 조금씩 감아 줘.");
                    break;
                case FightOutcome.LineRunOut:
                    Say("줄이 다 풀렸어! 멀리 도망갈 땐 감아서 거리를 좁혀야 해.");
                    break;
            }
        }

        void OnLeveledUp(int level)
        {
            var areas = FishingAreas.All.Where(a => a.Open && a.Level == level && a.Level > 1).Select(a => a.Name).ToList();
            Say($"레벨 {level}! 축하해!" + (areas.Count > 0 ? $" 이제 {string.Join(", ", areas)}에도 갈 수 있어. 대기실에서 낚시터를 골라 봐!" : ""),
                Fishing.Characters.CharacterAnimator.Cheer);
        }

        void OnQuestCompleted(QuestDef q, int gold, int exp)
        {
            if (q.Type == QuestType.Story) Say($"「{q.Title}」 끝! 다음 이야기도 같이 가 보자.");
        }

        static string Pick(params string[] lines) => lines[UnityEngine.Random.Range(0, lines.Length)];
    }
}
