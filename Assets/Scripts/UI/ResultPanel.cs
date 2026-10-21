using System;
using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 원작 RESULT 화면 (2013 홍현 영상 1:55.5~1:58, 5:24, 7:48): 화면을 어둡게 덮지 않고 3D 장면 위에 글자만 얹는다.
    /// 왼쪽 위 RESULT, 오른쪽에 동그란 월척·대물 도장(EXP+%), 오른쪽 아래 이름·cm·kg·EXP, [살림망에 넣기] [방생하기].
    /// 작은 고기는 캐릭터가 손에 들어 보이고(FishingController.LandedSequence), 큰 고기는 물 위로 튀어 오르는 3D 물고기.
    /// </summary>
    public class ResultPanel : UIPanel
    {
        TMP_Text _recordBar, _name, _exp, _length, _weight, _badge, _newRecord, _zone, _fightTime, _bonus, _grade, _eventBadge;
        RectTransform _stamp, _eventStamp, _toastCol;
        readonly List<(RectTransform rt, float delay)> _toastCards = new List<(RectTransform, float)>();
        bool _held;
        Image _fish, _emblem, _emblemFish;
        RectTransform _fishRoot;
        ResultFishStage _stage;
        RawImage _fish3d;
        Button _keepBtn, _releaseBtn;
        Action<bool> _onChoose;
        float _shownAt;
        readonly List<(RectTransform rt, Vector2 vel)> _drops = new List<(RectTransform, Vector2)>();

        public ResultPanel(Transform canvas)
        {
            // 원작: 화면을 어둡게 덮지 않는다 (3D 장면이 그대로 보인다). 아래쪽 글자만 읽히게 옅은 그늘
            var dim = UIFactory.Dim(canvas, "ResultPanel", 0f);
            dim.rectTransform.Fill();
            SetRoot(dim.gameObject);
            var root = dim.rectTransform;

            // 기록 바
            var bar = UIFactory.Glass(root, "RecordBar", UIFactory.Pill, null, 0.62f, true, false);
            bar.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(1180, 40));
            _recordBar = UIFactory.Text(bar.transform, "", 17, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _recordBar.rectTransform.Fill();

            // 아래쪽 그늘: 오른쪽 아래 글자가 밝은 장면 위에서도 읽히게
            var shade = UIFactory.Image(root, new Color(0f, 0f, 0f, 0.55f), UIFactory.Vignette, "BottomShade");
            shade.rectTransform.Fill(-200, -200, -200, -200);
            // 붓글씨처럼 비스듬한 흰 RESULT (원작은 붓으로 쓴 로고)
            var title = UIFactory.Text(root, "RESULT", 104, new Color(0.97f, 0.97f, 0.97f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
            title.rectTransform.At(new Vector2(0, 1), new Vector2(40, -40), new Vector2(700, 140));
            title.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);
            title.characterSpacing = -6f;
            UIFactory.Outline(title, new Color(0.05f, 0.05f, 0.07f, 0.95f), 3.5f);
            UIFactory.Shape(root, new Color(0.05f, 0.05f, 0.07f, 0.85f), UIFactory.Pill, "TitleBrush", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(46, -150), new Vector2(380, 9));

            // 튀어 오르는 물고기
            _fishRoot = UIFactory.Node("FishRoot", root).At(new Vector2(0.5f, 0.5f), new Vector2(-80, 40), new Vector2(10, 10));
            _fish = UIFactory.Image(_fishRoot, Color.white, UIFactory.Fish, "Fish");
            _fish.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 300));
            // 3D 물고기 (잡동사니가 아니면 이걸 쓴다)
            _stage = ResultFishStage.Create();
            _fish3d = UIFactory.Node("Fish3D", _fishRoot).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 625))
                .gameObject.AddComponent<RawImage>();
            _fish3d.texture = _stage.Target;
            _fish3d.raycastTarget = false;

            // 오른쪽 아래 정보 띠
            var info = UIFactory.Glass(root, "InfoBand", 36f, null, 0.62f, true, false);
            info.rectTransform.At(new Vector2(1, 0), new Vector2(60, 76), new Vector2(840, 280));
            info.rectTransform.localRotation = Quaternion.Euler(0, 0, 6f);
            var emblem = UIFactory.Shape(root, UIFactory.Good, UIFactory.Pill, "Emblem", 10f);
            UIFactory.AddShadow(emblem, 24f, new Vector2(0, -6), 0.5f);
            _emblem = emblem;
            _emblem.rectTransform.At(new Vector2(1, 0), new Vector2(-560, 110), new Vector2(150, 150));
            UIFactory.Shape(_emblem.transform, new Color(0.04f, 0.06f, 0.08f), UIFactory.Pill, "Inner", 4f).rectTransform.Fill(10, 10, 10, 10);
            _emblemFish = UIFactory.Image(_emblem.transform, UIFactory.Good, UIFactory.Fish, "EmblemFish");
            _emblemFish.rectTransform.Fill(24, 24, 44, 44);

            _zone = UIFactory.Text(root, "", 16, UIFactory.TextDim, TextAnchor.MiddleRight);
            _zone.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 356), new Vector2(560, 22));
            _name = UIFactory.Text(root, "", 44, new Color(0.6f, 0.95f, 0.4f), TextAnchor.MiddleRight, FontStyle.Bold);
            _name.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 296), new Vector2(560, 56));
            _exp = UIFactory.Text(root, "", 56, new Color(0.75f, 1f, 0.35f), TextAnchor.MiddleRight, FontStyle.BoldAndItalic);
            _exp.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 232), new Vector2(560, 70));
            _length = UIFactory.Text(root, "", 64, new Color(0.5f, 0.9f, 1f), TextAnchor.MiddleRight, FontStyle.BoldAndItalic);
            _length.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 162), new Vector2(560, 80));
            _weight = UIFactory.Text(root, "", 36, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleRight, FontStyle.BoldAndItalic);
            _weight.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 116), new Vector2(560, 46));
            // 원작 RESULT: 파이팅 시간 · BONUS 보상 GOLD/EXP · 등급(메달 색)
            _fightTime = UIFactory.Text(root, "", 20, new Color(0.8f, 0.88f, 0.95f), TextAnchor.MiddleRight, FontStyle.Bold);
            _fightTime.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 88), new Vector2(560, 26));
            _bonus = UIFactory.Text(root, "", 22, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.BoldAndItalic);
            _bonus.rectTransform.At(new Vector2(1, 0), new Vector2(-50, 384), new Vector2(700, 30));
            UIFactory.Outline(_bonus, new Color(0.2f, 0.1f, 0f, 0.9f), 0.25f);
            _grade = UIFactory.Chip(root, "", new Color(0.04f, 0.07f, 0.09f, 0.9f), Color.white, 16, "GradeChip");
            ((RectTransform)_grade.transform.parent).At(new Vector2(1, 0), new Vector2(-50, 334), new Vector2(70, 26), new Vector2(1, 0.5f));

            // 오른쪽 위 말풍선 토스트 (원작: 노랑 보상형 · 하늘색 기록형, 위에서부터 차례로 뜬다)
            _toastCol = UIFactory.Node("Toasts", root).At(new Vector2(1, 1), new Vector2(-40, -80), new Vector2(460, 600), new Vector2(1, 1));

            // 원작 월척·대물 도장: 오른쪽에 동그란 구릿빛 도장이 굴러 들어와 찍힌다
            _stamp = UIFactory.Shape(root, new Color(0.16f, 0.1f, 0.06f, 0.95f), UIFactory.Pill, "Stamp", 6f).rectTransform;
            _stamp.At(new Vector2(1, 0.5f), new Vector2(-150, 70), new Vector2(190, 190));
            UIFactory.AddBorder((UIShape)_stamp.GetComponent<Image>(), new Color(0.85f, 0.6f, 0.3f, 0.9f), 5f);
            UIFactory.AddShadow((UIShape)_stamp.GetComponent<Image>(), 24f, new Vector2(0, -6), 0.6f);
            _badge = UIFactory.Text(_stamp, "", 58, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _badge.rectTransform.Fill(8, 8, 8, 8);
            _badge.rectTransform.localRotation = Quaternion.Euler(0, 0, -10f);
            UIFactory.Outline(_badge, new Color(0.25f, 0.1f, 0f, 0.9f), 2f);

            // 원작 "EXP +200%" 이벤트 어종 도장 (월척 도장 위)
            _eventStamp = UIFactory.Shape(root, new Color(0.08f, 0.12f, 0.2f, 0.95f), UIFactory.Pill, "EventStamp", 6f).rectTransform;
            _eventStamp.At(new Vector2(1, 0.5f), new Vector2(-370, 70), new Vector2(160, 160));
            UIFactory.AddBorder((UIShape)_eventStamp.GetComponent<Image>(), new Color(0.35f, 0.8f, 1f, 0.95f), 5f);
            _eventBadge = UIFactory.Text(_eventStamp, "", 40, new Color(0.55f, 0.92f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _eventBadge.rectTransform.Fill(8, 8, 8, 8);
            _eventBadge.rectTransform.localRotation = Quaternion.Euler(0, 0, 8f);
            UIFactory.Outline(_eventBadge, new Color(0f, 0.1f, 0.25f, 0.9f), 0.25f);

            _newRecord = UIFactory.Text(root, "", 30, new Color(1f, 0.5f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _newRecord.rectTransform.At(new Vector2(0, 1), new Vector2(70, -196), new Vector2(600, 40));

            _keepBtn = UIFactory.Button(root, "살림망에 넣기", () => Choose(true), UIFactory.BtnPrimary, 20);
            UIFactory.SetRadius(_keepBtn.targetGraphic, UIFactory.Pill);
            ((RectTransform)_keepBtn.transform).At(new Vector2(1, 0), new Vector2(-276, 28), new Vector2(240, 52));
            _releaseBtn = UIFactory.Button(root, "방생하기", () => Choose(false), UIFactory.BtnNeutral, 20);
            UIFactory.SetRadius(_releaseBtn.targetGraphic, UIFactory.Pill);
            ((RectTransform)_releaseBtn.transform).At(new Vector2(1, 0), new Vector2(-28, 28), new Vector2(240, 52));

            BuildFightReport(root);

            Root.SetActive(false);
        }

        // ---------------- 파이팅 기록 차트 ----------------

        RectTransform _report;
        ChartHost _reportChart;
        TMP_Text _reportTitle, _reportStats;

        /// <summary>왼쪽 아래: 장력(%)과 물고기 HP(%)가 시간에 따라 어떻게 움직였는지 꺾은선으로 보여 준다.</summary>
        void BuildFightReport(RectTransform root)
        {
            _report = UIFactory.Glass(root, "FightReport", UIFactory.RadiusPanel, null, 0.66f, true, false).rectTransform
                .At(new Vector2(0, 0), new Vector2(40, 40), new Vector2(560, 300));
            _reportTitle = UIFactory.Text(_report, "파이팅 기록", 19, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _reportTitle.rectTransform.At(new Vector2(0, 1), new Vector2(22, -12), new Vector2(300, 28));
            // 범례는 직접 그린다 (게임 글꼴·자리 맞춤)
            LegendDot(_report, new Vector2(-190, -26), UIChart.Palette[1], "장력");
            LegendDot(_report, new Vector2(-104, -26), UIChart.Palette[0], "물고기 HP");
            _reportChart = UIChart.Line(_report, new Vector2(530, 210), new[] { "" }, new[]
            {
                new UIChart.Series("장력", new[] { 0f }, UIChart.Palette[1]),
                new UIChart.Series("물고기 HP", new[] { 0f }, UIChart.Palette[0]),
            }, 100f, "0", 12);
            ((RectTransform)_reportChart.transform).At(new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(530, 210));
            _reportStats = UIFactory.Text(_report, "", 14, UIFactory.TextDim, TextAnchor.MiddleLeft);
            _reportStats.rectTransform.At(new Vector2(0, 0), new Vector2(22, 10), new Vector2(520, 24));
        }

        static void LegendDot(RectTransform parent, Vector2 pos, Color color, string label)
        {
            UIFactory.Shape(parent, color, UIFactory.Pill, "Dot", 0f).rectTransform.At(new Vector2(1, 1), pos, new Vector2(10, 10), new Vector2(0.5f, 0.5f));
            UIFactory.Text(parent, label, 13, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform
                .At(new Vector2(1, 1), pos + new Vector2(10, 0), new Vector2(90, 20), new Vector2(0, 0.5f));
        }

        void UpdateFightReport()
        {
            var tension = FightHudView.TensionLog;
            var hp = FightHudView.HpLog;
            bool show = tension.Count >= 4;
            _report.gameObject.SetActive(show);
            if (!show) return;
            // 점이 너무 많으면 고르게 솎아 낸다 (최대 48개)
            int n = Mathf.Min(48, tension.Count);
            var t = new float[n];
            var h = new float[n];
            var cats = new string[n];
            int lastLabel = -99;
            for (int i = 0; i < n; i++)
            {
                int src = Mathf.RoundToInt(i * (tension.Count - 1) / (float)Mathf.Max(1, n - 1));
                t[i] = Mathf.Min(100f, tension[src]);
                h[i] = hp[src];
                // 아래 눈금은 5칸 정도만 초를 적는다
                bool label = i == 0 || i - lastLabel >= Mathf.Max(1, n / 5);
                cats[i] = label ? $"{src * FightHudView.LogInterval:0}초" : "";
                if (label) lastLabel = i;
            }
            var p = _reportChart.Profile;
            UIChart.SetCategories(p, cats);
            UIChart.SetValues(p.series[0], t);
            UIChart.SetValues(p.series[1], h);
            _reportChart.Refresh();

            float peak = 0f;
            int danger = 0;
            foreach (var v in tension)
            {
                peak = Mathf.Max(peak, v);
                if (v >= Fishing.Angling.FishFight.DangerTension * 100f) danger++;
            }
            _reportTitle.text = $"파이팅 기록  <size=14><color=#8fa3b5>{FightHudView.LastFightSeconds:0.0}초</color></size>";
            _reportStats.text = $"최고 장력 <color=#ffc850>{peak:0}%</color>   ·   위험 구간 {danger * FightHudView.LogInterval:0.0}초";
        }

        /// <param name="held">캐릭터가 고기를 손에 들어 보이는 중: 3D·그림 물고기와 물방울을 띄우지 않는다.</param>
        public void Show(CaughtFish fish, int exp, bool record, float prevBest, int prevBestDay, bool firstGet, bool held,
            CatchRewards.Outcome extra, Action<bool> onChoose)
        {
            _held = held;
            var s = FishDatabase.Get(fish.speciesId);
            _onChoose = onChoose;
            _shownAt = Time.unscaledTime;

            float best = Mathf.Max(prevBest, fish.length);
            int bestDay = fish.length >= prevBest ? fish.day : prevBestDay;
            _recordBar.text = s.IsJunk
                ? "<color=#8090a0>잡동사니는 기록되지 않습니다</color>"
                : $"<color=#ffb040>WR</color> {Fishing.World.FieldMap.ShortName} 최대  {s.MaxLen:0.00}cm        " +
                  $"<color=#70e070>TR</color> 대물 기준  {s.TrophyLen:0.00}cm ({s.TrophyName})        " +
                  $"<color=#50c8ff>MR</color> 개인기록  {best:0.00}cm ({bestDay}일차)";

            bool trophy = s.IsTrophy(fish.length);
            _name.text = s.Name;
            _exp.text = $"{exp}EXP";
            _length.text = s.IsJunk ? "" : $"{fish.length:0.00}cm";
            _weight.text = s.IsJunk ? "" : $"{fish.weight / 1000f:0.00}kg";
            _zone.text = s.IsJunk ? s.Temper : $"{s.Latin}  ·  {fish.zone}";
            _badge.text = trophy ? $"{s.TrophyName}\n<size=40%>EXP+100%</size>" : "";
            _stamp.gameObject.SetActive(trophy);
            // 원작: 처음 낚은 어종이면 결과 화면에 "First Get" 표시
            _newRecord.text = s.IsJunk ? "" : firstGet ? "<color=#7fe8ff>★ FIRST GET!</color>  <size=20>도감에 새로 올랐습니다</size>"
                : record ? "★ 개인 신기록!" : "";

            var rarityColor = s.Rarity switch
            {
                0 => new Color(0.5f, 0.5f, 0.55f),
                1 => new Color(0.45f, 0.85f, 0.4f),
                2 => new Color(0.35f, 0.7f, 1f),
                3 => new Color(0.7f, 0.45f, 1f),
                4 => new Color(1f, 0.45f, 0.6f),
                _ => new Color(1f, 0.8f, 0.3f)
            };
            _emblem.color = rarityColor;
            _emblemFish.color = rarityColor;
            // 동그라미 엠블럼 안: 어종 아이콘이 있으면 그 그림
            var emblemIcon = s.IsJunk ? ItemIcons.Get(s.Id) : Fishing.Angling.FishModels.Icon(s);
            _emblemFish.sprite = emblemIcon != null ? emblemIcon : UIFactory.Fish;
            _emblemFish.preserveAspect = emblemIcon != null;
            if (emblemIcon != null) _emblemFish.color = Color.white;

            _fish.sprite = s.IsJunk ? (ItemIcons.Get(s.Id) ?? UIFactory.Circle) : UIFactory.Fish;
            _fish.preserveAspect = s.IsJunk;
            _fish.color = s.Id == "goldkoi" ? new Color(1f, 0.78f, 0.25f) : s.IsJunk ? (ItemIcons.Get(s.Id) != null ? Color.white : new Color(0.5f, 0.5f, 0.55f)) : FishColor(s.Id);
            float size = Mathf.Lerp(380f, 900f, Mathf.Clamp01(fish.length / 100f));
            _fish.rectTransform.sizeDelta = s.IsJunk ? new Vector2(360, 360) : new Vector2(size, size * 0.5f);
            bool model = !held && _stage.Show(s, fish.length);
            if (held) _stage.Hide();
            _fish.enabled = !model && !held;
            _fish3d.enabled = model;

            _keepBtn.gameObject.SetActive(!s.IsJunk);
            _keepBtn.GetComponentInChildren<TMP_Text>().text = $"살림망에 넣기 ({PlayerProfile.SellPrice(fish):N0}G)";
            _releaseBtn.GetComponentInChildren<TMP_Text>().text = s.IsJunk ? "버리기" : "방생하기 (EXP+30%)";

            ShowExtra(s, extra);
            if (!held) SpawnDrops();
            else ClearDrops();
            // 원작 결과 화면에는 파이팅 기록 차트가 없다
            _report.gameObject.SetActive(false);
            Open();
        }

        void ShowExtra(FishSpecies s, CatchRewards.Outcome extra)
        {
            foreach (var (rt, _) in _toastCards) UnityEngine.Object.Destroy(rt.gameObject);
            _toastCards.Clear();
            bool fishy = extra != null && !s.IsJunk;
            _fightTime.text = fishy && extra.FightSeconds > 0f ? $"파이팅 시간  {FormatTime(extra.FightSeconds)}" : "";
            _bonus.text = fishy && (extra.BonusGold > 0 || extra.BonusExp > 0)
                ? $"<size=16><color=#fff2c0>BONUS 보상</color></size>  {extra.BonusGold:N0} GOLD  <color=#b8ff70>{extra.BonusExp:N0} EXP</color>" : "";
            int g = s.IsJunk ? -1 : CatchRewards.Grade(s);
            _grade.transform.parent.gameObject.SetActive(g >= 0);
            if (g >= 0)
            {
                _grade.text = CatchRewards.GradeName(g);
                _grade.color = CatchRewards.GradeColor(g);
                _name.color = CatchRewards.GradeColor(g);
                ((RectTransform)_grade.transform.parent).anchoredPosition = new Vector2(-50f - _name.preferredWidth - 14f, 324f);
            }
            int pct = fishy ? extra.EventPct : 0;
            _eventStamp.gameObject.SetActive(pct > 0 || (fishy && extra.Revenge));
            _eventBadge.text = pct > 0 ? $"이벤트\n<size=50%>EXP+{pct}%</size>" : fishy && extra.Revenge ? $"복수\n<size=50%>EXP X{extra.RevengeMult}</size>" : "";
            if (!fishy) return;
            // 말풍선 토스트: 최대 3개 (아래 월척·이벤트 도장과 안 겹치게), 0.25초 간격으로 위에서부터
            float y = 0f;
            int n = 0;
            foreach (var t in extra.Toasts)
            {
                if (n++ >= 3) break;
                var col = t.Reward ? new Color(1f, 0.82f, 0.3f) : new Color(0.45f, 0.85f, 1f);
                var card = UIFactory.Shape(_toastCol, new Color(0.05f, 0.08f, 0.14f, 0.9f), 12f, "Toast", 3f);
                UIFactory.AddBorder(card, new Color(col.r, col.g, col.b, 0.9f), 2f);
                UIFactory.AddShadow(card, 16f, new Vector2(0, -4), 0.5f);
                var rt = card.rectTransform.At(new Vector2(1, 1), new Vector2(0, -y), new Vector2(440, 84), new Vector2(1, 1));
                UIFactory.Shape(rt, col, UIFactory.Pill, "Bar", 0f).rectTransform.At(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(6, 58));
                var title = UIFactory.Text(rt, t.Title, 19, col, TextAnchor.MiddleLeft, FontStyle.Bold);
                title.rectTransform.At(new Vector2(0, 1), new Vector2(28, -8), new Vector2(400, 26));
                var body = UIFactory.Text(rt, t.Body, 14, UIFactory.TextMain, TextAnchor.UpperLeft);
                body.rectTransform.At(new Vector2(0, 1), new Vector2(28, -34), new Vector2(400, 30));
                if (t.Reward && (t.Gold > 0 || t.Exp > 0))
                {
                    var rw = UIFactory.Text(rt, $"{t.Gold:N0} G / <color=#b8ff70>{t.Exp:N0} EXP</color>", 15, UIFactory.Gold, TextAnchor.LowerRight, FontStyle.Bold);
                    rw.rectTransform.At(new Vector2(1, 0), new Vector2(-14, 6), new Vector2(260, 22), new Vector2(1, 0));
                }
                rt.gameObject.SetActive(false);
                _toastCards.Add((rt, 0.55f + 0.25f * (n - 1)));
                y += 92f;
            }
        }

        static string FormatTime(float sec)
        {
            int t = Mathf.RoundToInt(sec);
            return t >= 60 ? $"{t / 60}분 {t % 60}초" : $"{t}초";
        }

        static Color FishColor(string id) => id switch
        {
            "crucian" => new Color(0.72f, 0.66f, 0.42f),
            "carp" => new Color(0.78f, 0.62f, 0.35f),
            "catfish" => new Color(0.4f, 0.38f, 0.32f),
            "mandarin" => new Color(0.75f, 0.68f, 0.35f),
            "snakehead" => new Color(0.38f, 0.36f, 0.3f),
            "eel" => new Color(0.42f, 0.45f, 0.35f),
            "bass" => new Color(0.55f, 0.62f, 0.4f),
            "bluegill" => new Color(0.5f, 0.6f, 0.62f),
            "skygager" or "mullet" or "zacco" or "kkeuri" => new Color(0.78f, 0.82f, 0.86f),
            _ => new Color(0.65f, 0.6f, 0.5f)
        };

        void ClearDrops()
        {
            foreach (var d in _drops) UnityEngine.Object.Destroy(d.rt.gameObject);
            _drops.Clear();
        }

        void SpawnDrops()
        {
            ClearDrops();
            for (int i = 0; i < 70; i++)
            {
                var img = UIFactory.Image(Root.transform, new Color(0.85f, 0.93f, 1f, 0.8f), UIFactory.Circle, "Drop");
                float sz = UnityEngine.Random.Range(6f, 22f);
                img.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-80 + UnityEngine.Random.Range(-260f, 260f), -150), new Vector2(sz, sz));
                img.transform.SetSiblingIndex(3);
                _drops.Add((img.rectTransform, new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(300f, 900f))));
            }
        }

        /// <summary>물고기가 튀어 오르는 연출과 물방울. GameUI가 매 프레임 부른다.</summary>
        public void Animate()
        {
            if (!IsOpen) return;
            float t = Time.unscaledTime - _shownAt;
            float rise = Mathf.Clamp01(t / 0.45f);
            float y = Mathf.Lerp(-260f, 40f, 1f - (1f - rise) * (1f - rise)) + Mathf.Sin(t * 2.2f) * 8f;
            _fishRoot.anchoredPosition = new Vector2(-80f, y);
            _fishRoot.localRotation = _fish3d.enabled ? Quaternion.identity : Quaternion.Euler(0, 0, 18f + Mathf.Sin(t * 3f) * 4f);
            _stage.Pose(t);
            // 도장: 오른쪽에서 굴러 들어와 쾅 (0.35초)
            if (_stamp.gameObject.activeSelf)
            {
                float k = Mathf.Clamp01((t - 0.15f) / 0.35f);
                _stamp.anchoredPosition = new Vector2(Mathf.Lerp(260f, -150f, 1f - (1f - k) * (1f - k)), 70f);
                _stamp.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-200f, 0f, k));
                _stamp.localScale = Vector3.one * (k < 1f ? 1f : 1f + Mathf.Max(0f, 0.65f - t) * 0.6f);
            }

            // 말풍선 토스트: 오른쪽에서 미끄러져 들어온다 (뜰 때마다 딸깍)
            foreach (var (rt, delay) in _toastCards)
            {
                float k = Mathf.Clamp01((t - delay) / 0.22f);
                if (k <= 0f) continue;
                if (!rt.gameObject.activeSelf)
                {
                    rt.gameObject.SetActive(true);
                    Game.Sound.Coin();
                }
                float e = 1f - (1f - k) * (1f - k);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(480f, 0f, e), rt.anchoredPosition.y);
                rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
            }
            if (_eventStamp.gameObject.activeSelf)
            {
                float k = Mathf.Clamp01((t - 0.3f) / 0.3f);
                _eventStamp.localScale = Vector3.one * (k < 1f ? Mathf.Lerp(2.2f, 1f, k) : 1f);
            }

            for (int i = 0; i < _drops.Count; i++)
            {
                var (rt, vel) = _drops[i];
                vel.y -= 1400f * Time.unscaledDeltaTime;
                rt.anchoredPosition += vel * Time.unscaledDeltaTime;
                _drops[i] = (rt, vel);
            }
        }

        public override void Close()
        {
            base.Close();
            if (_stage != null) _stage.Hide();
        }

        void Choose(bool keep)
        {
            if (Time.unscaledTime - _shownAt < 0.4f) return; // 파이팅 중 연타로 바로 닫히지 않게
            Close();
            var cb = _onChoose;
            _onChoose = null;
            cb?.Invoke(keep);
        }

        /// <summary>키보드 선택. 랜딩 연타(Space)가 그대로 눌리지 않도록 조금 더 기다린다.</summary>
        public void ChooseByKey(bool keep)
        {
            if (Time.unscaledTime - _shownAt < 0.5f) return;
            Choose(keep);
        }

        public override void Refresh() { }
    }
}
