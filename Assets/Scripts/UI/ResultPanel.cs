using System;
using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 원작 RESULT 화면: 위쪽 기록 바(WR/MR), 물 위로 튀어 오르는 물고기, 오른쪽 아래 이름·EXP·cm·kg,
    /// 엠블럼, 대물 배지, [살림망에 넣기] [방생하기].
    /// </summary>
    public class ResultPanel : UIPanel
    {
        Text _recordBar, _name, _exp, _length, _weight, _badge, _newRecord, _zone;
        Image _fish, _emblem, _emblemFish;
        RectTransform _fishRoot;
        Button _keepBtn, _releaseBtn;
        Action<bool> _onChoose;
        float _shownAt;
        readonly List<(RectTransform rt, Vector2 vel)> _drops = new List<(RectTransform, Vector2)>();

        public ResultPanel(Transform canvas)
        {
            var dim = UIFactory.Panel(canvas, "ResultPanel", new Color(0.02f, 0.05f, 0.08f, 0.72f));
            dim.rectTransform.Fill();
            SetRoot(dim.gameObject);
            var root = dim.rectTransform;

            // 기록 바
            var bar = UIFactory.Image(root, new Color(0, 0, 0, 0.8f), null, "RecordBar");
            bar.rectTransform.anchorMin = new Vector2(0, 1);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(0.5f, 1);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            bar.rectTransform.sizeDelta = new Vector2(0, 34);
            _recordBar = UIFactory.Text(bar.transform, "", 17, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _recordBar.rectTransform.Fill();

            var title = UIFactory.Text(root, "RESULT", 120, new Color(0.95f, 0.95f, 0.95f), TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
            title.rectTransform.At(new Vector2(0, 1), new Vector2(60, -50), new Vector2(700, 150));
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(4, -4);
            UIFactory.Image(root, new Color(0, 0, 0, 0.5f), null, "TitleBrush").rectTransform
                .At(new Vector2(0, 1), new Vector2(40, -168), new Vector2(560, 14));

            // 튀어 오르는 물고기
            _fishRoot = UIFactory.Node("FishRoot", root).At(new Vector2(0.5f, 0.5f), new Vector2(-80, 40), new Vector2(10, 10));
            _fish = UIFactory.Image(_fishRoot, Color.white, UIFactory.Fish, "Fish");
            _fish.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 300));

            // 오른쪽 아래 정보 띠
            var info = UIFactory.Image(root, new Color(0, 0, 0, 0.65f), null, "InfoBand");
            info.rectTransform.At(new Vector2(1, 0), new Vector2(40, 70), new Vector2(820, 270));
            info.rectTransform.localRotation = Quaternion.Euler(0, 0, 6f);
            _emblem = UIFactory.Image(root, UIFactory.Good, UIFactory.Circle, "Emblem");
            _emblem.rectTransform.At(new Vector2(1, 0), new Vector2(-560, 110), new Vector2(150, 150));
            UIFactory.Image(_emblem.transform, new Color(0.05f, 0.08f, 0.06f), UIFactory.Circle, "Inner").rectTransform.Fill(10, 10, 10, 10);
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

            _badge = UIFactory.Text(root, "", 72, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _badge.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-330, 170), new Vector2(560, 180));
            _badge.rectTransform.localRotation = Quaternion.Euler(0, 0, -8f);
            var badgeOutline = _badge.gameObject.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.25f, 0.1f, 0f, 0.95f);
            badgeOutline.effectDistance = new Vector2(3, -3);

            _newRecord = UIFactory.Text(root, "", 30, new Color(1f, 0.5f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _newRecord.rectTransform.At(new Vector2(0, 1), new Vector2(70, -196), new Vector2(600, 40));

            _keepBtn = UIFactory.Button(root, "살림망에 넣기", () => Choose(true), new Color(0.12f, 0.2f, 0.28f, 0.95f), 20);
            ((RectTransform)_keepBtn.transform).At(new Vector2(1, 0), new Vector2(-260, 30), new Vector2(220, 48));
            _releaseBtn = UIFactory.Button(root, "방생하기", () => Choose(false), new Color(0.12f, 0.2f, 0.28f, 0.95f), 20);
            ((RectTransform)_releaseBtn.transform).At(new Vector2(1, 0), new Vector2(-30, 30), new Vector2(220, 48));

            Root.SetActive(false);
        }

        public void Show(CaughtFish fish, int exp, bool record, float prevBest, int prevBestDay, Action<bool> onChoose)
        {
            var s = FishDatabase.Get(fish.speciesId);
            _onChoose = onChoose;
            _shownAt = Time.unscaledTime;

            float best = Mathf.Max(prevBest, fish.length);
            int bestDay = fish.length >= prevBest ? fish.day : prevBestDay;
            _recordBar.text = s.IsJunk
                ? "<color=#8090a0>잡동사니는 기록되지 않습니다</color>"
                : $"<color=#ffb040>WR</color> 한강 최대  {s.MaxLen:0.00}cm        " +
                  $"<color=#70e070>TR</color> 대물 기준  {s.TrophyLen:0.00}cm ({s.TrophyName})        " +
                  $"<color=#50c8ff>MR</color> 개인기록  {best:0.00}cm ({bestDay}일차)";

            bool trophy = s.IsTrophy(fish.length);
            _name.text = s.Name;
            _exp.text = $"{exp}EXP";
            _length.text = s.IsJunk ? "" : $"{fish.length:0.00}cm";
            _weight.text = s.IsJunk ? "" : $"{fish.weight / 1000f:0.00}kg";
            _zone.text = s.IsJunk ? s.Temper : $"{s.Latin}  ·  {fish.zone}";
            _badge.text = trophy ? $"{s.TrophyName}\n<size=34>EXP+100%</size>" : "";
            _newRecord.text = record && !s.IsJunk ? "★ 개인 신기록!" : "";

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

            _fish.sprite = s.IsJunk ? UIFactory.Circle : UIFactory.Fish;
            _fish.color = s.Id == "goldkoi" ? new Color(1f, 0.78f, 0.25f) : s.IsJunk ? new Color(0.5f, 0.5f, 0.55f) : FishColor(s.Id);
            float size = Mathf.Lerp(380f, 900f, Mathf.Clamp01(fish.length / 100f));
            _fish.rectTransform.sizeDelta = s.IsJunk ? new Vector2(180, 180) : new Vector2(size, size * 0.5f);

            _keepBtn.gameObject.SetActive(!s.IsJunk);
            _keepBtn.GetComponentInChildren<Text>().text = $"살림망에 넣기 ({PlayerProfile.SellPrice(fish):N0}G)";
            _releaseBtn.GetComponentInChildren<Text>().text = s.IsJunk ? "버리기" : "방생하기 (EXP+30%)";

            SpawnDrops();
            Open();
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

        void SpawnDrops()
        {
            foreach (var d in _drops) UnityEngine.Object.Destroy(d.rt.gameObject);
            _drops.Clear();
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
            _fishRoot.localRotation = Quaternion.Euler(0, 0, 18f + Mathf.Sin(t * 3f) * 4f);

            for (int i = 0; i < _drops.Count; i++)
            {
                var (rt, vel) = _drops[i];
                vel.y -= 1400f * Time.unscaledDeltaTime;
                rt.anchoredPosition += vel * Time.unscaledDeltaTime;
                _drops[i] = (rt, vel);
            }
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
            if (Time.unscaledTime - _shownAt < 1.2f) return;
            Choose(keep);
        }

        public override void Refresh() { }
    }
}
