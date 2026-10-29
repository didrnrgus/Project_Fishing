using System.Collections.Generic;
using System.Linq;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 퀘스트 창 "업적 · 칭호" 탭 (원작 기록(I) 책 업적 쪽, 2013 홍현 영상 2:32~2:36).
    ///  - 왼쪽: 분류 `물고기(낚시·납품·대물·방생·생태조사·특수어종) / 도구 / 달인 / 발자취` + 달성 %, 아래에 칭호 고르기.
    ///  - 오른쪽: 고른 분류의 업적. 생태조사는 낚시터마다 "홍현 생태조사 (16/23)" 묶음을 눌러 펼치면
    ///    "○○을 홍현 앞바다에서 납품하기 · 도전과제 완성 - 날짜" 목록, 완성한 것을 누르면 "다음의 보상을 획득하였습니다. 250 GOLD".
    /// 소라고동 업적(<see cref="AchievementDatabase"/>)과 GOLD 업적(<see cref="EcoSurvey"/>)을 같은 분류로 함께 보여 준다.
    /// </summary>
    public partial class QuestPanel
    {
        const float CatW = 230f;
        const string CatFishing = "낚시", CatTrophy = "대물", CatSpecial = "특수어종", CatTool = "도구", CatMaster = "달인", CatFootprint = "발자취";

        /// <summary>기록 책 분류: 큰 분류와 그 아래 작은 분류 (작은 분류가 없으면 큰 분류가 곧 목록).</summary>
        static readonly (string major, string[] subs)[] Book =
        {
            ("물고기", new[] { CatFishing, EcoSurvey.CatDelivery, CatTrophy, EcoSurvey.CatRelease, EcoSurvey.CatEco, CatSpecial }),
            (CatTool, null),
            (CatMaster, null),
            (CatFootprint, null),
        };

        /// <summary>소라고동 업적 → 분류 (목록에 없는 새 업적은 발자취).</summary>
        static readonly Dictionary<string, string> ConchCategory = new Dictionary<string, string>
        {
            { "first", CatFishing }, { "catch50", CatFishing }, { "catch200", CatFishing }, { "octopus10", CatFishing }, { "squid20", CatFishing }, { "smelt", CatFishing },
            { "wolcheok", CatTrophy }, { "carp80", CatTrophy }, { "bream50", CatTrophy }, { "amberjack", CatTrophy }, { "hairtail", CatTrophy },
            { "trophy10", CatTrophy }, { "giantoctopus", CatTrophy },
            { "goldkoi", CatSpecial }, { "grouper", CatSpecial }, { "croaker", CatSpecial }, { "deeplegend", CatSpecial }, { "robot", CatSpecial },
            { "fishcan", CatSpecial }, { "pollock", CatSpecial }, { "tamna", CatSpecial },
            { "upgrade10", CatTool },
            { "combo5", CatMaster }, { "dexhan", CatMaster }, { "dexsea", CatMaster }, { "dexwest", CatMaster }, { "dexeast", CatMaster },
            { "dexkorea", CatMaster }, { "level25", CatMaster },
            // 2026-10-30 새 어종·해외 도감
            { "gulbi", CatFishing }, { "mahseer", CatTrophy },
            { "native", CatSpecial }, { "gangwonwinter", CatSpecial }, { "electric", CatSpecial }, { "reefshark", CatSpecial },
            { "dextancheon", CatMaster }, { "dexsoutheast", CatMaster }, { "dexmed", CatMaster }, { "dexnile", CatMaster }, { "dexworld", CatMaster },
        };

        string _achCat = EcoSurvey.CatEco;
        string _ecoMap;

        /// <summary>자동 촬영·바로가기: 업적 탭의 분류(와 펼칠 생태조사 낚시터)를 고른다.</summary>
        public void ShowAchievements(string category, string ecoMapId = null)
        {
            _achCat = category;
            _ecoMap = ecoMapId;
            ShowTab(3);
        }

        static string CategoryOf(AchievementDef a) => ConchCategory.TryGetValue(a.Id, out var c) ? c : CatFootprint;

        static IEnumerable<string> Leaves(int major) => Book[major].subs ?? new[] { Book[major].major };

        static (int done, int total) Count(PlayerProfile p, string cat)
        {
            int done = 0, total = 0;
            foreach (var a in AchievementDatabase.All)
            {
                if (CategoryOf(a) != cat) continue;
                total++;
                if (p.Data.achievements.Contains(a.Id)) done++;
            }
            foreach (var g in EcoSurvey.OfCategory(cat))
            {
                total++;
                if (EcoSurvey.IsDone(p, g)) done++;
            }
            return (done, total);
        }

        static string Percent(int done, int total) => total > 0 ? $"{Mathf.FloorToInt(done * 100f / total)}%" : "-";

        void AchievementBook(RowCursor rows)
        {
            var p = Game.Profile;
            // RowCursor가 만든 스크롤 목록을 오른쪽으로 밀고, 왼쪽에 분류 칸을 둔다
            // (다시 그릴 때 지난 목록이 프레임 끝까지 남아 있으므로 맨 뒤의 "List"가 방금 만든 것)
            RectTransform list = null;
            for (int i = Body.childCount - 1; i >= 0 && list == null; i--)
                if (Body.GetChild(i).name == "List") list = (RectTransform)Body.GetChild(i);
            var scroll = list != null ? list.Find("Scroll") as RectTransform : null;
            if (scroll != null) scroll.offsetMin = new Vector2(CatW + 16f, scroll.offsetMin.y);
            if (list != null) BuildCategories(list, p);

            // 오른쪽 머리: 분류 이름 + 진행 막대
            var (done, total) = Count(p, _achCat);
            var head = rows.Next(34, 6);
            UIFactory.SectionTitle(head, $"{_achCat}  <size=14><color=#8fa3b5>{Percent(done, total)}</color></size>", 4, 20);
            ProgressBar(head, done, total, 330, 330);

            if (_achCat == EcoSurvey.CatEco) EcoList(rows, p);
            else
            {
                foreach (var a in AchievementDatabase.All.Where(x => CategoryOf(x) == _achCat)) ConchRow(rows, p, a);
                foreach (var g in EcoSurvey.OfCategory(_achCat)) GoldRow(rows, p, g, false);
                if (total == 0) Cell(rows.Next(30), "아직 이 분류의 업적이 없습니다.", 8, 600, 16, UIFactory.TextDim);
            }
        }

        // ---------------- 왼쪽 분류 ----------------

        void BuildCategories(RectTransform list, PlayerProfile p)
        {
            var col = UIFactory.Shape(list, UIFactory.Surface, UIFactory.RadiusCard, "Categories", 0f).rectTransform;
            col.anchorMin = new Vector2(0, 0); col.anchorMax = new Vector2(0, 1); col.pivot = new Vector2(0, 0.5f);
            col.anchoredPosition = Vector2.zero; col.sizeDelta = new Vector2(CatW, 0);

            float y = -8f;
            for (int m = 0; m < Book.Length; m++)
            {
                var (major, subs) = Book[m];
                int md = 0, mt = 0;
                foreach (var leaf in Leaves(m)) { var (d, t) = Count(p, leaf); md += d; mt += t; }
                bool leafMajor = subs == null;
                bool selMajor = leafMajor ? _achCat == major : subs.Contains(_achCat);
                string first = leafMajor ? major : subs[0];
                CatButton(col, $"<b>{major}</b>", Percent(md, mt), y, 44, 20, selMajor && leafMajor ? UIFactory.TabOn : selMajor ? new Color(0.1f, 0.46f, 0.45f, 0.45f) : UIFactory.TabOff,
                    () => { _achCat = first; _ecoMap = null; Refresh(); });
                y -= 50f;
                if (subs == null) continue;
                foreach (var sub in subs)
                {
                    var (sd, st) = Count(p, sub);
                    string leafName = sub;
                    // 받을 보상이 남은 분류는 노란 점
                    bool claimable = EcoSurvey.OfCategory(sub).Any(g => EcoSurvey.IsDone(p, g) && !EcoSurvey.IsClaimed(p, g));
                    CatButton(col, (claimable ? "<color=#ffd060>●</color> " : "") + sub, Percent(sd, st), y, 32, 16,
                        _achCat == sub ? UIFactory.TabOn : new Color(1f, 1f, 1f, 0.04f), () => { _achCat = leafName; _ecoMap = null; Refresh(); }, 22f);
                    y -= 36f;
                }
                y -= 6f;
            }

            // 칭호 고르기 (아래쪽)
            var titleHead = UIFactory.Text(col, $"지금 칭호\n<color=#7fd8ff><b>「{p.CurrentTitle}」</b></color>", 15, UIFactory.TextMain, TextAnchor.UpperLeft);
            titleHead.rectTransform.At(new Vector2(0, 1), new Vector2(14, y - 10), new Vector2(CatW - 28, 46));
            y -= 60f;
            var titles = p.Titles;
            for (int i = 0; i < titles.Count && i < 5; i++)
            {
                string t = titles[titles.Count - 1 - i];
                var b = UIFactory.Button(col, t, () => p.SetTitle(t), t == p.CurrentTitle ? UIFactory.TabOn : UIFactory.TabOff, 13);
                UIFactory.SetRadius(b.targetGraphic, UIFactory.Pill);
                ((RectTransform)b.transform).At(new Vector2(0, 1), new Vector2(14, y), new Vector2(CatW - 28, 28));
                y -= 32f;
            }
        }

        static Button CatButton(RectTransform col, string label, string percent, float y, float h, int size, Color bg, System.Action click, float indent = 8f)
        {
            var b = UIFactory.Button(col, "", click, bg, size);
            var rt = (RectTransform)b.transform;
            rt.At(new Vector2(0, 1), new Vector2(indent, y), new Vector2(CatW - indent - 8f, h));
            var label0 = b.GetComponentInChildren<TMPro.TMP_Text>();
            label0.text = label;
            label0.alignment = UIFactory.Align(TextAnchor.MiddleLeft);
            label0.rectTransform.Fill(12, 60, 0, 0);
            UIFactory.Text(rt, percent, size - 3, UIFactory.TextDim, TextAnchor.MiddleRight).rectTransform.Fill(0, 12, 0, 0);
            return b;
        }

        // ---------------- 오른쪽 목록 ----------------

        void EcoList(RowCursor rows, PlayerProfile p)
        {
            Cell(rows.Next(24, 4), "낚시터마다 사는 어종을 그 낚시터에서 한 마리씩 납품하면 완성 · 완성한 업적을 누르면 GOLD 보상", 8, 680, 14, UIFactory.TextDim);
            foreach (var map in EcoSurvey.SurveyMaps)
            {
                var (done, total) = EcoSurvey.MapProgress(p, map.Id);
                bool open = _ecoMap == map.Id;
                string mapId = map.Id;
                var row = rows.Next(46, 4);
                var bg = UIFactory.RowBg(row, open);
                bg.raycastTarget = true;
                var btn = bg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { if (Game.Sound != null) Game.Sound.UiClick(); _ecoMap = open ? null : mapId; Refresh(); });
                int unclaimed = EcoSurvey.OfMap(map.Id).Count(g => EcoSurvey.IsDone(p, g) && !EcoSurvey.IsClaimed(p, g));
                Cell(row, $"{(open ? "▼" : "▶")}  <b>{EcoSurvey.GroupName(map)}</b> ({done} / {total})" +
                          (unclaimed > 0 ? $"  <size=13><color=#ffd060>보상 {unclaimed}개</color></size>" : ""), 12, 380, 18,
                    done >= total ? UIFactory.Good : UIFactory.TextMain);
                ProgressBar(row, done, total, 400, 260);
                if (!open) continue;
                foreach (var g in EcoSurvey.OfMap(map.Id)) GoldRow(rows, p, g, true);
                rows.Next(6);
            }
        }

        /// <summary>소라고동 업적 한 줄 (예전 목록 모양).</summary>
        void ConchRow(RowCursor rows, PlayerProfile p, AchievementDef a)
        {
            bool done = p.Data.achievements.Contains(a.Id);
            string prog = !done && a.Progress != null ? $"  ({a.Progress(p).Item1}/{a.Progress(p).Item2})" : "";
            var row = rows.Next(46, 4);
            UIFactory.RowBg(row, done);
            Cell(row, $"{(done ? "<color=#70e070>●</color>" : "○")} <b>{a.Name}</b>  <size=13><color=#8fa3b5>{a.Desc}{prog}</color></size>\n" +
                      $"<size=13><color=#ffd060>소라고동 {a.Conch}</color>{(a.Title != null ? $" · 칭호 「{a.Title}」" : "")}</size>",
                12, 660, 16, done ? UIFactory.Good : UIFactory.TextMain);
        }

        /// <summary>GOLD 업적 한 줄: 아이콘 · 이름 · "도전과제 완성 - 날짜" · 보상 받기 버튼.</summary>
        void GoldRow(RowCursor rows, PlayerProfile p, GoldAch g, bool indent)
        {
            bool done = EcoSurvey.IsDone(p, g);
            bool claimed = EcoSurvey.IsClaimed(p, g);
            var row = rows.Next(58, 4);
            var bg = UIFactory.RowBg(row, done && !claimed);
            if (done && !claimed) UIFactory.AddBorder(bg, new Color(0.36f, 0.88f, 0.8f, 0.6f), 1.5f);
            float x = indent ? 24f : 8f;
            var sprite = g.Species != null ? Fishing.Angling.FishModels.Icon(g.Species) : null;
            if (sprite != null)
            {
                var img = UIFactory.Image(row, done ? Color.white : new Color(0f, 0f, 0f, 0.55f), sprite, "Icon");
                img.preserveAspect = true;
                img.rectTransform.At(new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(70, 48), new Vector2(0, 0.5f));
                x += 78f;
            }
            int value = Mathf.Min(g.Value(p), g.Target);
            string sub = done ? $"<color=#8fd8c8>도전과제 완성 - {EcoSurvey.DoneAt(p, g)}</color>" : $"<color=#8090a0>{value}/{g.Target}</color>";
            Cell(row, $"<b>{g.Name}</b>\n<size=13>{sub}</size>", x, 470 - x, 16, done ? UIFactory.TextMain : UIFactory.TextDim);
            if (done && !claimed)
            {
                var b = RowButton(row, $"보상 받기 {g.Gold:N0} GOLD", () => ClaimGold(g), 478, 200, UIFactory.BtnGold, true, 15);
                ((RectTransform)b.transform).sizeDelta = new Vector2(200, -14);
            }
            else Cell(row, claimed ? $"<color=#70e070>받음</color>  {g.Gold:N0} GOLD" : $"{g.Gold:N0} GOLD", 478, 200, 15,
                claimed ? UIFactory.TextDim : UIFactory.TextFaint, TextAnchor.MiddleRight);
        }

        void ClaimGold(GoldAch g)
        {
            int gold = EcoSurvey.Claim(g);
            if (gold <= 0) return;
            RewardBand(g.Name, gold);
            Refresh();
        }

        /// <summary>원작 보상 알림: 창 가운데를 가로지르는 어두운 띠 "○○ / 다음의 보상을 획득하였습니다. 250 GOLD" (2초).</summary>
        void RewardBand(string name, int gold)
        {
            var band = UIFactory.Image(Frame, new Color(0.01f, 0.03f, 0.05f, 0.88f), null, "RewardBand");
            band.raycastTarget = false;
            band.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Frame.rect.width + 400f, 120));
            UIFactory.Image(band.transform, new Color(0.36f, 0.88f, 0.8f, 0.7f), null, "Top").rectTransform
                .At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(Frame.rect.width + 400f, 2), new Vector2(0.5f, 1));
            UIFactory.Image(band.transform, new Color(0.36f, 0.88f, 0.8f, 0.7f), null, "Bottom").rectTransform
                .At(new Vector2(0.5f, 0), Vector2.zero, new Vector2(Frame.rect.width + 400f, 2), new Vector2(0.5f, 0));
            var t = UIFactory.Text(band.transform, $"<b><size=22>{name}</size></b>\n\n다음의 보상을 획득하였습니다.\n<color=#ffd060><b>{gold:N0} GOLD</b></color>",
                17, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.Fill();
            band.transform.SetAsLastSibling();
            Object.Destroy(band.gameObject, 2.2f);
        }
    }
}
