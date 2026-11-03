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
            _history.Add(Message("user", question));
            // 길 안내는 AI 없이 게임 데이터로 바로 (Ollama가 꺼져 있어도, 이름·위치를 지어내지 않게)
            if (TryGuide(question)) return;
            _history.RemoveAt(_history.Count - 1);
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
                "\n\n(윤슬은 반말로 짧게. 게임 사실은 자료·도구에 있는 것만, 게임 밖 질문은 아는 대로 성실하게.)");
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
            Reply(Short(Banmal(answer), 8), null, true);
        }

        // 문장 수 자르기 (작은 모델은 "짧게"를 자주 넘긴다): 대화창은 8문장, 말풍선은 4문장(UI.BuddyChatView). 줄바꿈 문단도 한 줄로 잇는다
        static readonly System.Text.RegularExpressions.Regex SentenceEnd = new System.Text.RegularExpressions.Regex(@"(?<=[.!?~])\s+");

        public static string Short(string s, int sentences)
        {
            var list = SentenceEnd.Split(s.Replace("\r", "")).Select(x => x.Replace("\n", " ").Trim()).Where(x => x.Length > 0).ToList();
            return list.Count <= sentences ? string.Join(" ", list) : string.Join(" ", list.Take(sentences)) + " …";
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
길이: 보통은 2~4문장. 설명이 필요한 질문(개발·기술·사양)은 6문장까지. 마크다운 기호(#, *, |)는 쓰지 마.
무엇이든 물어봐도 대답해: 게임 얘기든, 이 PC 사양이든, 이 게임을 어떻게 만들었는지(코드·셰이더·AI·구조)든, 일상·공부·개발 질문이든. 낚시 얘기로 억지로 돌리지 마.
- 이 PC 사양·버전은 system_info, 이 게임의 구현·기술은 search_dev_docs/read_dev_doc, 일반 상식·개발 질문은 아는 대로 성실하게. 모르면 모른다고 해.
게임 질문(조작, 물고기, 낚시터, 장비, 미끼, 퀘스트, 레벨, 배, 대회)에는 먼저 도구로 확인하고 대답해:
- '어떻게 해?', '왜 안 돼?', 요령·조작·장비 고르기·문제(줄이 끊김, 입질이 없음, 바늘이 빠짐)는 search_guides로 핵심 낱말 한두 개를 찾아. 필요하면 read_guide로 그 장을 읽어.
- 물고기 하나는 fish_info, 낚시터는 map_info, 퀘스트는 quest_info, 플레이어 레벨·장비·있는 곳은 game_state.
- 숫자(레벨·길이·가격·시간)는 도구 결과에 있는 것만 써. 도구에도 없으면 지어내지 말고 '그건 나도 잘 모르겠어'라고 해.
질문 끝에 [참고 자료]가 붙어 있으면 그중 질문에 딱 맞는 한두 가지만 골라 쉬운 말로 풀어 대답해. 자료를 그대로 옮기거나 파일 이름을 말하지는 마.
말투 예시: (미끼는 뭐가 좋아?) → 붕어 노릴 거면 떡밥이 제일 무난해! 지렁이도 잘 먹으니까 둘 다 챙겨 가자. / (오늘 힘들다) → 그럴 땐 찌만 멍하니 봐도 좋잖아. 한 마리만 낚고 쉬자!
지금 상황: {where}, Lv.{p?.Data.level ?? 1}, {(Game.Clock != null ? $"{Game.Clock.TimeText} {Names.Of(Game.Clock.Band)}" : "")}, 날씨 {(Game.Weather != null ? Names.Of(Game.Weather.Current) : "?")}, 미끼 {p?.Bait.Name}{(q != null ? $", 이야기 퀘스트 「{q.Title}」({q.GoalText})" : "")}.";
        }

        // ───────────── 길 안내 ─────────────
        // "데려다줘", "누구한테 가야 해?", "퀘스트 알고 싶어"면 윤슬이 그 NPC에게 앞장서 걸어간다 (BuddyBody.Lead).
        // 갈 NPC: 질문에 이름·낚시점·조합이 나오면 그 사람, 아니면 이 낚시터에서 퀘스트 표시가 가장 급한 사람(보고 > 이야기 퀘스트 > 받을 퀘스트 > 진행 중).
        // 이 낚시터에 없으면(대기실·다른 낚시터) 어디로 가야 하는지 말로만.

        static readonly string[] GuideWords = { "안내", "데려", "데리고", "따라갈", "따라가", "길알려", "어디로가", "어디가야", "어딨", "어디있", "어디에있", "누구한테", "누구에게", "찾아가" };
        static readonly string[] QuestAsk = { "알고", "알려", "뭐해", "뭘해", "받", "할거", "할게", "어디", "누구", "있어?", "있나" };

        bool TryGuide(string question)
        {
            if (Game.Mode != GameMode.Field || Game.Quests == null) return false;
            string q = question.Replace(" ", "");
            bool guide = GuideWords.Any(q.Contains);
            bool quest = q.Contains("퀘스트") || q.Contains("임무") || q.Contains("의뢰");
            var named = NamedNpc(q);
            // "너가 먼저 안내해 봐"처럼 퀘스트라는 말이 없어도, 바로 앞 질문이 퀘스트 얘기였으면 퀘스트 안내로 본다
            bool context = _lastWasQuest;
            _lastWasQuest = quest;
            // 낚시 포인트: "붕어 어디서 잡아?", "참돔 포인트 데려가 줘"
            var fish = FishDatabase.All.Where(f => !f.IsJunk && GameKnowledge.Mentions(question, f.Name)).OrderByDescending(f => f.Name.Length).FirstOrDefault();
            if (fish != null && !quest && (guide || q.Contains("어디") || q.Contains("포인트") || q.Contains("자리")) && GuideToFish(fish)) return true;
            if (!(guide && (quest || named != null || context)) && !(quest && QuestAsk.Any(q.Contains))) return false;
            _lastWasQuest = true;

            var body = BuddyBody.Instance;
            if (named != null && (!quest || Game.Quests.MarkFor(KeyOf(named)) != QuestSystem.Mark.None))
            {
                Reply($"{named.Look.Name}{Honor(named.Look.Name)}? 저쪽이야. 따라와!", Fishing.Characters.CharacterAnimator.Wave, true);
                body?.Lead(NpcGoal(named));
                return true;
            }

            // 이 낚시터에서 퀘스트 표시가 가장 급한 NPC (같으면 가까운 쪽)
            Fishing.Characters.NpcAngler best = null;
            var bestMark = QuestSystem.Mark.None;
            float bestD = float.MaxValue;
            foreach (var n in Fishing.Characters.NpcAngler.Talkable)
            {
                if (n == null) continue;
                var mark = Game.Quests.MarkFor(KeyOf(n));
                if (mark == QuestSystem.Mark.None) continue;
                float d = (n.transform.position - Game.Player.transform.position).sqrMagnitude;
                if (Rank(mark) > Rank(bestMark) || (mark == bestMark && d < bestD)) { best = n; bestMark = mark; bestD = d; }
            }
            if (best != null)
            {
                string key = KeyOf(best), who = best.Look.Name + Honor(best.Look.Name);
                var ready = Game.Quests.Active().FirstOrDefault(x => (x.Reporter ?? x.Giver) == key && Game.Quests.IsReady(x));
                var avail = Game.Quests.AvailableAt(key);
                var working = Game.Quests.InProgressAt(key).FirstOrDefault();
                string line = bestMark switch
                {
                    QuestSystem.Mark.Ready when ready != null => $"「{ready.Title}」 다 했네! {who}한테 보고하러 가자. 따라와!",
                    QuestSystem.Mark.Ready => $"{who}한테 가 보자. 기다리고 있을 거야. 따라와!",
                    QuestSystem.Mark.AvailableStory or QuestSystem.Mark.Available when avail.Count > 0 =>
                        $"{who}한테 받을 퀘스트가 있어. 「{avail[0].Title}」{(avail.Count > 1 ? $" 말고도 {avail.Count - 1}개 더" : "")}! 내가 데려다줄게, 따라와!",
                    _ when working != null => $"「{working.Title}」은 {who} 일이야. 지금 목표는 {Game.Quests.ObjectiveLine(working)}. 같이 가 보자, 따라와!",
                    _ => $"{who}한테 가 보자. 따라와!",
                };
                Reply(line, Fishing.Characters.CharacterAnimator.Wave, true);
                if (body == null || !body.Lead(NpcGoal(best))) Reply($"{who}{QuestNpcs.Josa(best.Look.Name, "은", "는")} {FieldMap.AreaName(best.transform.position)} 쪽에 있어. 머리 위 표시를 찾아봐!", null, true);
                return true;
            }

            // 이 낚시터에는 없다: 이야기 퀘스트를 받을·보고할 사람이 어디 있는지 말로
            var s = Game.Quests.Current;
            if (s != null)
            {
                bool accepted = Game.Quests.IsActive(s);
                var npc = QuestNpcs.Get(!accepted ? s.Giver : Game.Quests.IsReady(s) ? s.Reporter ?? s.Giver : null);
                if (npc != null)
                {
                    string who = npc.Name + Honor(npc.Name);
                    Reply(npc.InLobby
                        ? $"「{s.Title}」은 대기실 조합의 {who}한테 가야 해. 오른쪽 위 ◀ 조합으로 돌아가서 [퀘스트]를 눌러 봐!"
                        : $"「{s.Title}」은 {npc.Place}의 {who}한테 가야 해. 대기실에서 {npc.Place}로 가자!", null, true);
                    return true;
                }
                if (accepted)
                {
                    Reply($"지금은 찾아갈 사람이 없어. 「{s.Title}」 목표부터 채우자: {Game.Quests.ObjectiveLine(s)}. 다 하면 같이 보고하러 가자!", null, true);
                    return true;
                }
            }
            if (!guide) return false; // 퀘스트 질문인데 갈 데가 없으면 AI가 대답
            Reply("지금은 받을 퀘스트가 없어. 레벨을 올리거나 다른 낚시터에 가 보자!", null, true);
            return true;
        }

        bool _lastWasQuest;

        static int Rank(QuestSystem.Mark m) => m switch
        {
            QuestSystem.Mark.Ready => 4,
            QuestSystem.Mark.AvailableStory => 3,
            QuestSystem.Mark.Available => 2,
            QuestSystem.Mark.Working => 1,
            _ => 0,
        };

        static string KeyOf(Fishing.Characters.NpcAngler n) => QuestNpcs.KeyFor(n.NpcId, FieldMap.Id);

        /// <summary>"박주임"·"정계장"(이름 끝 낱말)·"낚시점"·"조합"·"어부"로 이 낚시터의 NPC를 찾는다.</summary>
        static Fishing.Characters.NpcAngler NamedNpc(string q)
        {
            foreach (var n in Fishing.Characters.NpcAngler.Talkable)
            {
                if (n == null || n.Look == null || string.IsNullOrEmpty(n.Look.Name)) continue;
                string name = n.Look.Name.Replace(" ", "");
                string last = n.Look.Name.Split(' ').Last();
                if (q.Contains(name) || (last.Length >= 2 && q.Contains(last))) return n;
            }
            string id = q.Contains("낚시점") || q.Contains("상점") || q.Contains("가게") ? "shop"
                : q.Contains("조합") || q.Contains("연락관") || q.Contains("지부") ? "union"
                : q.Contains("어부") || q.Contains("통발") ? "fisher" : null;
            return id == null ? null : Fishing.Characters.NpcAngler.Talkable.FirstOrDefault(n => n != null && n.NpcId == id);
        }

        /// <summary>"박주임" → "님" ("이장님"처럼 이미 님이면 붙이지 않는다).</summary>
        static string Honor(string name) => name != null && name.EndsWith("님") ? "" : "님";

        BuddyBody.Goal NpcGoal(Fishing.Characters.NpcAngler npc)
        {
            string who = npc.Look.Name + Honor(npc.Look.Name);
            return new BuddyBody.Goal
            {
                Target = npc.transform,
                Name = who,
                Arrived = () => Say($"여기야! {who}한테 {Settings.KeyName(KeyAction.Talk)} 눌러서 말 걸어 봐.", Fishing.Characters.CharacterAnimator.Wave),
            };
        }

        /// <summary>그 고기가 사는 이 낚시터 포인트로 데려간다. 다른 낚시터 고기면 false (AI가 자료로 대답).</summary>
        bool GuideToFish(FishSpecies s)
        {
            var body = BuddyBody.Instance;
            if (body == null || (s.Maps != null && !s.Maps.Contains(FieldMap.Id))) return false;
            var p = Game.Profile;
            float reach = (p.Rod.CastRange + p.Costume.Cast) * 0.85f;
            bool onBoat = Game.Player.OnBoat;
            bool canBoat = onBoat || !string.IsNullOrEmpty(p.Data.boatId);
            var spot = BuddySpots.Find(s, Game.Player.transform.position, reach, canBoat);
            string name = s.Name + QuestNpcs.Josa(s.Name, "은", "는");
            if (!spot.HasValue)
            {
                if (!canBoat && BuddySpots.Find(s, Game.Player.transform.position, reach, true).HasValue)
                {
                    Reply($"{name} 여기선 배를 타야 닿는 데 살아. 낚시점에서 배부터 마련하자!", null, true);
                    return true;
                }
                return false;
            }
            var sp = spot.Value;
            string where = $"{sp.Cell} {sp.Habitat}";
            string boatKey = Settings.KeyName(KeyAction.Boat);
            var goal = new BuddyBody.Goal { Spot = sp.Shore, PointAt = sp.Water, Boat = sp.Boat || onBoat, Name = where };
            if (onBoat)
            {
                Reply($"{name} {where} 쪽이야. 저기! 내가 가리키는 쪽으로 몰아 줘.", null, true);
                goal.Reached = () => Say($"여기야, {where}! 여기서 던져 봐.", Fishing.Characters.CharacterAnimator.Excited);
            }
            else if (sp.Boat)
            {
                Reply($"{name} {where}에 있는데, 거긴 배를 타야 해. 배 띄울 물가로 데려갈게, 따라와!", Fishing.Characters.CharacterAnimator.Wave, true);
                goal.Arrived = () => Say($"여기서 배를 타야 해! {boatKey} 눌러서 배 꺼내. 포인트는 저기, {sp.Cell}이야. 나도 같이 탈게!", Fishing.Characters.CharacterAnimator.Wave);
                goal.Reached = () => Say($"여기야, {where}! 여기서 던져 봐.", Fishing.Characters.CharacterAnimator.Excited);
            }
            else
            {
                Reply($"{name} {where} 쪽이 좋아! 저기야, 따라와.", Fishing.Characters.CharacterAnimator.Wave, true);
                goal.Arrived = () => Say($"여기야! 저쪽 {sp.Habitat}{Euro(sp.Habitat)} 던져 봐.", Fishing.Characters.CharacterAnimator.Wave);
            }
            body.Lead(goal);
            return true;
        }

        /// <summary>"여울" → "로", "수초" → "로", "암초 지대" → "로", "갯바위" → "로", "깊은 곳" → "으로" (받침이 없거나 ㄹ이면 로).</summary>
        static string Euro(string word)
        {
            if (string.IsNullOrEmpty(word)) return "로";
            char c = word[word.Length - 1];
            if (c < '가' || c > '힣') return "로";
            int final = (c - '가') % 28;
            return final == 0 || final == 8 ? "로" : "으로";
        }

        /// <summary>안내하는데 플레이어가 처졌다 (BuddyBody).</summary>
        public void CallPlayer(string where) =>
            Say(Pick("이쪽이야! 따라와~", $"여기야 여기! {where} 가는 길이야.", "천천히 와도 돼, 기다릴게!"), Fishing.Characters.CharacterAnimator.Wave);

        /// <summary>안내하는데 플레이어가 반대로 간다 (BuddyBody: 손으로 가리키며).</summary>
        public void WrongWay(string where) =>
            Say(Pick("아니야, 저기야!", $"거기 아니야~ {where}는 저쪽!", "어디 가? 이쪽이야!"));

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
