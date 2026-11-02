using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Buddy
{
    /// <summary>
    /// 동료 AI가 쓰는 "게임 지식 도구". 같은 도구를 두 곳에서 쓴다:
    ///  - 게임 안 대화(<see cref="OllamaChat"/>): 모델이 도구를 불러 가이드 문서·어종·낚시터·지금 상태를 직접 찾아 읽는다.
    ///  - MCP 서버(<see cref="BuddyMcpServer"/>): Claude Code 같은 바깥 AI가 실행 중인 게임에 붙어 같은 도구를 쓴다.
    /// 문서는 플레이어용 가이드(docs/guide)만 읽힌다. 개발 문서(작업 기록·밸런스 리포트·원작 조사)는 스포일러·개발 용어가 많고 너무 길다.
    /// 숫자(입장 레벨·길이·가격)는 문서보다 게임 데이터 표가 정확하니 fish_info·map_info를 먼저 쓰게 설명에 적는다.
    /// 유니티 객체를 읽으므로 메인 스레드에서만 부른다.
    /// </summary>
    public static class GameKnowledge
    {
        const int MaxDocChars = 3000; // 한글은 거의 한 글자 = 한 토큰. 대화 문맥(6144)에 질문·도구 설명과 같이 들어가야 한다

        public sealed class Tool
        {
            public string Name, Description;
            public Dictionary<string, object> Schema;
        }

        public static readonly Tool[] Tools =
        {
            new Tool
            {
                Name = "game_state",
                Description = "플레이어의 지금 상태: 있는 곳(낚시터·구역·대기실·배), 시간·날씨·계절, 레벨·골드·체력, 장비·미끼, 진행 중인 이야기 퀘스트, 다음에 열리는 낚시터.",
                Schema = Schema(),
            },
            new Tool
            {
                Name = "list_guides",
                Description = "플레이 가이드 문서 목록과 각 문서의 장(제목) 목록. 어떤 문서를 읽을지 고를 때 쓴다.",
                Schema = Schema(),
            },
            new Tool
            {
                Name = "read_guide",
                Description = "플레이 가이드 문서를 읽는다. file은 파일 이름 일부(예: hangang, first-sea, README), section을 주면 그 장만 읽는다(제목 일부, 예: 노릴 고기).",
                Schema = Schema(("file", "문서 파일 이름 일부", true), ("section", "읽을 장 제목 일부 (없으면 문서 전체)", false)),
            },
            new Tool
            {
                Name = "search_guides",
                Description = "플레이 가이드 전체에서 낱말을 찾아 맞는 줄과 그 줄이 속한 문서·장을 돌려준다.",
                Schema = Schema(("query", "찾을 낱말 (띄어 쓰면 여러 낱말)", true)),
            },
            new Tool
            {
                Name = "fish_info",
                Description = "물고기 한 종의 게임 데이터: 사는 낚시터·포인트, 잘 무는 시간·날씨·계절·미끼, 길이, 대물 기준, 가격, 플레이어 도감 기록. 숫자는 이 도구가 가장 정확하다.",
                Schema = Schema(("name", "물고기 이름 (예: 붕어, 참돔)", true)),
            },
            new Tool
            {
                Name = "map_info",
                Description = "낚시터 정보: 입장 레벨, 거점, 설명, 사는 어종, 플레이어가 와 봤는지. name이 없으면 모든 낚시터 목록.",
                Schema = Schema(("name", "낚시터 이름 일부 (예: 한강, 홍현, 제주)", false)),
            },
            new Tool
            {
                Name = "quest_info",
                Description = "진행 중인 퀘스트(이야기·일반·일일)와 목표·진행도·보상, 누구에게 받는지.",
                Schema = Schema(),
            },
        };

        static Dictionary<string, object> Schema(params (string name, string desc, bool required)[] props)
        {
            var p = new Dictionary<string, object>();
            var req = new List<object>();
            foreach (var (name, desc, required) in props)
            {
                p[name] = new Dictionary<string, object> { { "type", "string" }, { "description", desc } };
                if (required) req.Add(name);
            }
            var s = new Dictionary<string, object> { { "type", "object" }, { "properties", p } };
            if (req.Count > 0) s["required"] = req;
            return s;
        }

        /// <summary>도구를 부른다. 결과는 모델이 읽을 글. 모르는 도구·잘못된 인자도 예외 대신 글로 돌려준다.</summary>
        public static string Call(string name, Dictionary<string, object> args)
        {
            try
            {
                string Arg(string key) => Json.Str(args, key)?.Trim();
                return name switch
                {
                    "game_state" => GameState(),
                    "list_guides" => ListGuides(),
                    "read_guide" => ReadGuide(Arg("file"), Arg("section")),
                    "search_guides" => SearchGuides(Arg("query")),
                    "fish_info" => FishInfo(Arg("name")),
                    "map_info" => MapInfo(Arg("name")),
                    "quest_info" => QuestInfo(),
                    _ => $"'{name}' 도구는 없습니다. 쓸 수 있는 도구: {string.Join(", ", Tools.Select(t => t.Name))}",
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Buddy] 도구 {name} 실패: {e}");
                return $"도구 {name}를 쓰다가 문제가 생겼습니다: {e.Message}";
            }
        }

        // ───────────── 가이드 문서 ─────────────

        /// <summary>가이드 폴더: 에디터는 저장소의 docs/guide, 빌드는 StreamingAssets/Guide (빌드할 때 복사, ProjectTools.BuildWindows).</summary>
        public static string GuideFolder =>
            Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "guide")) : Path.Combine(Application.streamingAssetsPath, "Guide");

        static List<(string file, string text)> _guides;

        static List<(string file, string text)> Guides
        {
            get
            {
                if (_guides != null) return _guides;
                _guides = new List<(string, string)>();
                if (Directory.Exists(GuideFolder))
                    foreach (var path in Directory.GetFiles(GuideFolder, "*.md").OrderBy(p => p, StringComparer.Ordinal))
                        _guides.Add((Path.GetFileName(path), Clean(File.ReadAllText(path))));
                return _guides;
            }
        }

        /// <summary>그림 줄은 빼고, 링크는 글자만 남긴다 (모델이 읽을 글자 수를 줄인다).</summary>
        static string Clean(string md)
        {
            var sb = new StringBuilder();
            foreach (var raw in md.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.TrimStart().StartsWith("![")) continue;
                line = System.Text.RegularExpressions.Regex.Replace(line, @"\[([^\]]+)\]\([^)]*\)", "$1");
                sb.Append(line).Append('\n');
            }
            return sb.ToString();
        }

        static string TitleOf(string text)
        {
            foreach (var line in text.Split('\n'))
                if (line.StartsWith("# ")) return line.Substring(2).Trim();
            return "";
        }

        static IEnumerable<string> Headings(string text) =>
            text.Split('\n').Where(l => l.StartsWith("## ")).Select(l => l.Substring(3).Trim());

        static string ListGuides()
        {
            if (Guides.Count == 0) return $"가이드 문서를 찾지 못했습니다 ({GuideFolder}).";
            var sb = new StringBuilder();
            foreach (var (file, text) in Guides)
                sb.Append($"- {file}: {TitleOf(text)} — 장: {string.Join(" / ", Headings(text))}\n");
            return sb.ToString();
        }

        static (string file, string text)? FindGuide(string query)
        {
            if (string.IsNullOrEmpty(query)) return null;
            var q = query.ToLowerInvariant().Replace(".md", "");
            foreach (var g in Guides)
                if (g.file.ToLowerInvariant().Contains(q) || TitleOf(g.text).ToLowerInvariant().Contains(q)) return g;
            return null;
        }

        static string ReadGuide(string file, string section)
        {
            var g = FindGuide(file);
            if (g == null) return $"'{file}' 문서가 없습니다. 목록:\n{ListGuides()}";
            var (name, text) = g.Value;
            if (!string.IsNullOrEmpty(section))
            {
                var part = Section(text, section);
                if (part == null) return $"{name}에 '{section}' 장이 없습니다. 장: {string.Join(" / ", Headings(text))}";
                text = part;
            }
            if (text.Length > MaxDocChars)
                text = text.Substring(0, MaxDocChars) + $"\n…(길어서 잘림. section으로 장을 골라 읽으세요: {string.Join(" / ", Headings(g.Value.text))})";
            return $"[{name}]\n{text}";
        }

        /// <summary>제목에 query가 들어간 "## 장"부터 다음 "## 장" 앞까지.</summary>
        static string Section(string text, string query)
        {
            var lines = text.Split('\n');
            int start = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("#")) continue;
                if (start >= 0 && lines[i].StartsWith("## ")) return string.Join("\n", lines, start, i - start);
                if (start < 0 && lines[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) start = i;
            }
            return start >= 0 ? string.Join("\n", lines, start, lines.Length - start) : null;
        }

        static string SearchGuides(string query)
        {
            if (string.IsNullOrEmpty(query)) return "query가 비었습니다.";
            var words = query.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var hits = new List<(int score, string text)>();
            foreach (var (file, text) in Guides)
            {
                string heading = TitleOf(text);
                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("#")) heading = line.TrimStart('#', ' ');
                    int score = words.Count(w => line.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (score == 0 || line.Trim().Length < 4) continue;
                    hits.Add((score, $"[{file} › {heading}] {line.Trim()}"));
                }
            }
            if (hits.Count == 0) return $"가이드에서 '{query}'를 찾지 못했습니다. fish_info나 map_info로 게임 데이터를 확인해 보세요.";
            return string.Join("\n", hits.OrderByDescending(h => h.score).Take(14).Select(h => h.text));
        }

        // ───────────── 게임 데이터 ─────────────

        static string GameState()
        {
            var p = Game.Profile;
            if (p == null) return "게임이 아직 시작되지 않았습니다.";
            var d = p.Data;
            var sb = new StringBuilder();
            var look = p.BuildLook();
            sb.Append($"플레이어: {look.Name} ({p.CurrentTitle}), Lv.{d.level} (다음 레벨까지 EXP {d.exp}/{PlayerProfile.ExpToNext(d.level)}), 골드 {d.gold:N0}, 체력 {p.StaminaRatio * 100f:0}%\n");
            if (Game.Mode == GameMode.Lobby) sb.Append("있는 곳: 한국 낚시 조합(대기실). 낚시터 목록에서 낚시터를 골라 들어간다.\n");
            else
            {
                string where = FieldMap.MapName;
                if (Game.Player != null)
                {
                    var pos = Game.Player.transform.position;
                    if (FieldMap.TryCell(pos, out int c, out int r))
                        where += $", 구역 {FieldMap.CellName(c, r)} ({FieldMap.HabitatName(FieldMap.HabitatAt(c, r))})";
                    if (Game.Player.OnBoat) where += ", 배 위";
                }
                sb.Append($"있는 곳: {where}\n");
                if (Game.Fishing != null) sb.Append($"낚시 상태: {Game.Fishing.State}\n");
            }
            if (Game.Clock != null)
                sb.Append($"시간: {Game.Clock.Day}일째 {Game.Clock.TimeText} ({Names.Of(Game.Clock.Band)}), 계절 {Seasons.Name(Seasons.Current)}, 날씨 {(Game.Weather != null ? Names.Of(Game.Weather.Current) : "?")}\n");
            sb.Append($"장비: {p.Rod.Name} / {p.Reel.Name} / {p.Line.Name}, 미끼 {p.Bait.Name} {p.Count(p.Bait.Id)}개, 배 {(p.Boat != null ? p.Boat.Name : "없음")}, 낚싯대 내구도 {d.rodDurability:0}% · 릴 {d.reelDurability:0}%\n");
            sb.Append($"지금까지 {d.totalCatches}마리, 도감 {d.dex.Count(e => FishDatabase.Get(e.speciesId) is FishSpecies s && !s.IsJunk)}종, 살림망 {d.keepNet?.Count ?? 0}/{p.KeepNetSize}\n");
            var q = Game.Quests?.Current;
            if (q != null) sb.Append($"이야기 퀘스트: 「{q.Title}」 {q.GoalText} {Game.Quests.Progress}/{q.Count}{(Game.Quests.Ready ? " (완료! 보고하러 가면 됨)" : "")}\n");
            var next = FishingAreas.All.Where(a => a.Open && a.Level > d.level).OrderBy(a => a.Level).FirstOrDefault();
            if (next != null) sb.Append($"다음에 열리는 낚시터: {next.Name} (Lv.{next.Level})\n");
            return sb.ToString();
        }

        static FishSpecies FindFish(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = FishDatabase.All;
            return all.FirstOrDefault(s => s.Name == name || s.Id == name)
                ?? all.FirstOrDefault(s => s.Name.Replace(" ", "") == name.Replace(" ", ""))
                ?? all.FirstOrDefault(s => s.Name.Contains(name))
                ?? all.FirstOrDefault(s => name.Contains(s.Name));
        }

        static string MapName(string mapId)
        {
            var area = FishingAreas.All.FirstOrDefault(a => a.MapId == mapId);
            return area != null ? area.Name : mapId;
        }

        static string FishInfo(string name)
        {
            var s = FindFish(name);
            // 모델이 도구를 헷갈려 낚시터 이름을 넣으면 낚시터 정보를 대신 준다
            if (s == null && FindArea(name) != null) return $"'{name}'는 물고기가 아니라 낚시터라서 낚시터 정보를 줄게.\n" + MapInfo(name);
            if (s == null) return $"'{name}'라는 물고기는 게임에 없습니다.";
            var sb = new StringBuilder();
            sb.Append($"{s.Name} ({s.Latin}) — {(s.IsJunk ? "잡동사니" : $"희귀도 {s.Rarity}/5")}{(s.Boss ? ", 괴물 어종" : "")}\n");
            if (!string.IsNullOrEmpty(s.Temper)) sb.Append($"성질: {s.Temper}\n");
            sb.Append($"길이 {s.MinLen:0.#}~{s.MaxLen:0.#}cm, 대물 {s.TrophyLen:0.#}cm 이상{(string.IsNullOrEmpty(s.TrophyName) ? "" : $" ({s.TrophyName})")}\n");
            sb.Append($"사는 곳: {(s.Maps == null ? "모든 낚시터" : string.Join(", ", s.Maps.Select(MapName)))}{(s.FarSeaOnly ? " (먼바다에서만)" : "")}{(s.SchoolOnly ? " (어군에서만)" : "")}\n");
            if (s.Habitats != null && s.Habitats.Length > 0) sb.Append($"포인트: {string.Join(", ", s.Habitats.Select(h => Names.Of(h)))}\n");
            if (s.Times != null && s.Times.Length > 0) sb.Append($"시간: {string.Join(", ", s.Times.Select(t => Names.Of(t)))}\n");
            if (s.Weathers != null && s.Weathers.Length > 0) sb.Append($"날씨: {string.Join(", ", s.Weathers.Select(w => Names.Of(w)))}\n");
            if (s.Seasons != null && s.Seasons.Length > 0) sb.Append($"제철: {string.Join(", ", s.Seasons.Select(Seasons.Name))}\n");
            if (s.Baits != null && s.Baits.Length > 0) sb.Append($"미끼 종류: {string.Join(", ", s.Baits.Select(b => Names.Of(b)))}\n");
            sb.Append($"힘 {s.Power:0.#}, 속도 {s.Speed:0.#}, 경험치 {s.Exp}, 값 {s.Price:N0}골드\n");
            var dex = Game.Profile?.Dex(s.Id);
            sb.Append(dex != null ? $"플레이어 기록: {dex.count}마리, 최대 {dex.maxLength:0.##}cm\n" : "플레이어 기록: 아직 못 잡음\n");
            return sb.ToString();
        }

        static string MapInfo(string name)
        {
            int level = Game.Profile?.Data.level ?? 1;
            var visited = Game.Profile?.Data.visitedMaps;
            if (string.IsNullOrEmpty(name))
            {
                var sb = new StringBuilder("낚시터 목록 (입장 레벨 순):\n");
                foreach (var a in FishingAreas.All.Where(a => a.Open).OrderBy(a => a.Level))
                    sb.Append($"- {a.Name} Lv.{a.Level} ({HubName(a.Hub)}){(a.Level <= level ? "" : " 아직 못 감")}\n");
                return sb.ToString();
            }
            var area = FindArea(name);
            if (area == null && FindFish(name) != null) return $"'{name}'는 낚시터가 아니라 물고기라서 물고기 정보를 줄게.\n" + FishInfo(name);
            if (area == null) return $"'{name}' 낚시터가 없습니다. name 없이 부르면 목록이 나옵니다.";
            var info = new StringBuilder();
            info.Append($"{area.Name}: 입장 Lv.{area.Level}, 거점 {HubName(area.Hub)}{(area.Open ? "" : ", 아직 안 열림")}\n");
            if (!string.IsNullOrEmpty(area.Desc)) info.Append($"{area.Desc}\n");
            info.Append(level >= area.Level ? "플레이어는 들어갈 수 있다" : $"플레이어는 Lv.{area.Level}부터 들어갈 수 있다 (지금 Lv.{level})");
            info.Append(visited != null && area.MapId != null && visited.Contains(area.MapId) ? ", 와 본 적 있음\n" : ", 아직 안 와 봄\n");
            if (area.MapId != null)
            {
                var fish = FishDatabase.DexOf(area.MapId).OrderBy(s => s.Rarity).ToList();
                info.Append($"사는 어종 {fish.Count}종: {string.Join(", ", fish.Select(s => s.Name + (Game.Profile?.Dex(s.Id) != null ? "" : "(미기록)")))}\n");
            }
            return info.ToString();
        }

        static FishingArea FindArea(string name) =>
            string.IsNullOrEmpty(name) ? null : FishingAreas.All.FirstOrDefault(a => a.Name.Contains(name) || a.MapId == name || (a.MapId != null && name.Contains(a.Name)));

        static string HubName(string id) => FishingAreas.Hubs.FirstOrDefault(h => h.Id == id)?.Name ?? id;

        static string QuestInfo()
        {
            var qs = Game.Quests;
            if (qs == null) return "퀘스트 정보가 없습니다.";
            var sb = new StringBuilder();
            foreach (var q in qs.Active().Take(12))
            {
                sb.Append($"- [{q.TypeTag}] 「{q.Title}」 {q.GoalText} {qs.ProgressOf(q)}/{q.Count}");
                if (!string.IsNullOrEmpty(q.Giver)) sb.Append($", 의뢰인 {q.Giver}");
                if (q.MapId != null) sb.Append($", 낚시터 {MapName(q.MapId)}");
                if (q.RewardGold > 0 || q.RewardExp > 0) sb.Append($", 보상 {q.RewardGold:N0}골드·{q.RewardExp:N0}EXP");
                sb.Append('\n');
                if (!string.IsNullOrEmpty(q.Desc)) sb.Append($"  {q.Desc}\n");
            }
            return sb.Length > 0 ? sb.ToString() : "진행 중인 퀘스트가 없습니다. 머리 위에 ! 표시가 있는 NPC에게 말을 걸면 받을 수 있다.";
        }
    }
}
