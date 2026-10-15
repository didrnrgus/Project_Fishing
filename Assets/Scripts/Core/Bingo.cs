using System;
using System.Collections.Generic;
using System.Linq;
using Fishing.Data;

namespace Fishing.Core
{
    [Serializable]
    public class BingoSave
    {
        public int card;                                   // 몇 번째 빙고판 (0 = 아직 없음)
        public List<string> cells = new List<string>();    // 25칸: "sp:<어종>" · "len:<cm>" · "trophy" · "free"
        public List<bool> done = new List<bool>();
        public int linesPaid;                              // 보상을 준 줄 수
        public bool bigPaid;                               // 3줄 큰 보상을 줬는지
        public int cardsCleared;                           // 3줄을 채운 빙고판 수
    }

    /// <summary>
    /// 빙고 (원작: 25칸 미션 빙고판 — "랜딩으로 낚기 · 수족관에 먹이 주기 · 물고기 방생하기" 같은 미션, 3줄을 채우면 나팔고둥·황금거거).
    /// 칸 대부분은 "어느 어종 낚기", 가운데는 빈칸(처음부터 채워짐). 나머지는 방생 · 수족관 먹이 · 콤보 · 길이 · 대물 미션.
    /// 어종은 지금 레벨로 갈 수 있는 낚시터에서 고른다.
    /// 줄(가로·세로·대각선 12줄) 하나마다 소라고동 2 + 1,000골드, 3줄이면 큰 보상을 준다.
    /// </summary>
    public static class Bingo
    {
        public const int Size = 5, LinesForBig = 3;
        public const int LineConch = 2, LineGold = 1000;

        public static BingoSave S => Game.Profile.Data.bingo;

        /// <summary>3줄 큰 보상: 첫 판 나팔고둥 릴 → 둘째 판 황금거거 낚싯대 → 그다음은 골드·소라고동.</summary>
        public static string BigRewardText(BingoSave s)
        {
            if (!Game.Profile.Owns(TournamentRules.RewardReelId)) return "나팔고둥 릴";
            if (!Game.Profile.Owns(GoldGegeId)) return "황금거거 낚싯대";
            return "10,000골드 · 소라고동 5";
        }

        public const string GoldGegeId = "rod_goldgege";

        public static void EnsureCard()
        {
            if (S.card == 0 || S.cells.Count != Size * Size) NewCard();
        }

        public static void NewCard()
        {
            var p = Game.Profile;
            var s = S;
            s.card++;
            s.cells.Clear();
            s.done.Clear();
            s.linesPaid = 0;
            s.bigPaid = false;
            var rng = new System.Random(s.card * 7919 + p.Data.level * 31);
            var pool = new List<string>();
            foreach (var area in FishingAreas.Korea)
            {
                if (!area.Open || area.Level > p.Data.level || area.MapId == null) continue;
                foreach (var sp in FishDatabase.DexOf(area.MapId))
                    if (sp.Rarity >= 1 && sp.Rarity <= 3 && !sp.SchoolOnly && !sp.FarSeaOnly && !sp.Boss && sp.Id != "smelt" && !pool.Contains(sp.Id))
                        pool.Add(sp.Id);
            }
            pool = pool.OrderBy(_ => rng.Next()).ToList();
            var cells = new List<string>();
            foreach (var id in pool.Take(17)) cells.Add("sp:" + id);
            cells.Add("release");
            cells.Add("combo:3");
            if (p.Data.level >= 5) cells.Add("feed");   // 수족관은 Lv.5부터
            int[] lens = { 30, 50, 20, 40, 60 };
            int k = 0;
            while (cells.Count < 22) cells.Add("len:" + lens[k++ % lens.Length]);
            while (cells.Count < 24) cells.Add("trophy");
            cells = cells.OrderBy(_ => rng.Next()).ToList();
            cells.Insert(12, "free");
            s.cells.AddRange(cells);
            for (int i = 0; i < Size * Size; i++) s.done.Add(i == 12);
            p.NotifyChanged();
        }

        public static string CellText(string cell)
        {
            if (cell == "free") return "FREE";
            if (cell == "trophy") return "아무 대물\n1마리";
            if (cell == "release") return "물고기\n방생하기";
            if (cell == "feed") return "수족관에\n먹이 주기";
            if (cell.StartsWith("combo:")) return $"콤보 {cell.Substring(6)} 이상으로\n낚기";
            if (cell.StartsWith("len:")) return $"아무 고기\n{cell.Substring(4)}cm 이상";
            var sp = FishDatabase.Get(cell.Substring(3));
            return sp != null ? sp.Name : cell;
        }

        /// <summary>어느 낚시터에서 낚을 수 있는지 (칸 아래 작게).</summary>
        public static string CellHint(string cell)
        {
            if (!cell.StartsWith("sp:")) return "";
            var sp = FishDatabase.Get(cell.Substring(3));
            if (sp?.Maps == null || sp.Maps.Length == 0) return "";
            var map = Fishing.World.Maps.All.FirstOrDefault(m => m.Id == sp.Maps[0]);
            return map != null ? map.ShortName + (sp.Maps.Length > 1 ? " 외" : "") : "";
        }

        static bool Matches(string cell, CatchInfo c)
        {
            if (cell == "trophy") return c.Species.IsTrophy(c.Length);
            if (cell.StartsWith("len:")) return c.Length >= float.Parse(cell.Substring(4));
            if (cell.StartsWith("combo:")) return c.Combo >= int.Parse(cell.Substring(6));
            if (cell == "release" || cell == "feed") return false;
            return cell == "sp:" + c.Species.Id;
        }

        /// <summary>낚을 때마다: 맞는 칸을 하나 채운다 (같은 고기로 여러 칸을 한 번에 채우지는 않는다. 어종 칸 먼저).</summary>
        public static void OnCatch(CatchInfo c)
        {
            if (c.Species.IsJunk) return;
            EnsureCard();
            var s = S;
            int hit = -1;
            for (int pass = 0; pass < 2 && hit < 0; pass++)
                for (int i = 0; i < s.cells.Count; i++)
                    if (!s.done[i] && (pass == 0 ? s.cells[i].StartsWith("sp:") : !s.cells[i].StartsWith("sp:")) && Matches(s.cells[i], c)) { hit = i; break; }
            if (hit < 0) return;
            s.done[hit] = true;
            Game.Toast($"빙고 칸 채움: {CellText(s.cells[hit]).Replace('\n', ' ')}", ToastKind.Good);
            PayLines();
        }

        /// <summary>낚기가 아닌 미션 (방생 "release" · 수족관 먹이 "feed").</summary>
        public static void OnAction(string cell)
        {
            if (Game.Profile == null) return;
            EnsureCard();
            var s = S;
            int i = s.cells.IndexOf(cell);
            if (i < 0 || s.done[i]) return;
            s.done[i] = true;
            Game.Toast($"빙고 칸 채움: {CellText(cell).Replace('\n', ' ')}", ToastKind.Good);
            PayLines();
        }

        public static int Lines()
        {
            var d = S.done;
            if (d.Count != Size * Size) return 0;
            int n = 0;
            for (int r = 0; r < Size; r++)
            {
                bool row = true, col = true;
                for (int k = 0; k < Size; k++) { row &= d[r * Size + k]; col &= d[k * Size + r]; }
                if (row) n++;
                if (col) n++;
            }
            bool d1 = true, d2 = true;
            for (int k = 0; k < Size; k++) { d1 &= d[k * Size + k]; d2 &= d[k * Size + (Size - 1 - k)]; }
            if (d1) n++;
            if (d2) n++;
            return n;
        }

        static void PayLines()
        {
            var p = Game.Profile;
            var s = S;
            int lines = Lines();
            while (s.linesPaid < lines)
            {
                s.linesPaid++;
                p.AddItem(AchievementDatabase.ConchId, LineConch);
                p.AddGold(LineGold);
                Game.Toast($"빙고! {s.linesPaid}줄 완성  소라고동 +{LineConch} · {LineGold:N0}골드", ToastKind.Great);
                if (Game.Sound != null) Game.Sound.LevelUp();
            }
            if (!s.bigPaid && lines >= LinesForBig)
            {
                s.bigPaid = true;
                s.cardsCleared++;
                string what = BigRewardText(s);
                if (!p.Owns(TournamentRules.RewardReelId)) p.Data.ownedGear.Add(TournamentRules.RewardReelId);
                else if (!p.Owns(GoldGegeId)) p.Data.ownedGear.Add(GoldGegeId);
                else { p.AddGold(10000); p.AddItem(AchievementDatabase.ConchId, 5); }
                Game.Toast($"빙고 {LinesForBig}줄 달성! 보상: {what}. 퀘스트 창 [빙고]에서 새 빙고판을 받을 수 있습니다.", ToastKind.Great);
                p.Log($"빙고 {LinesForBig}줄 달성 — {what}");
            }
            p.NotifyChanged();
        }
    }
}
