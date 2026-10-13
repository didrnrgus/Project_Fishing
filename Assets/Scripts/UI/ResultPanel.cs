using System;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>랜딩 성공 후 결과 창. 살림망에 넣을지 방생할지 고른다.</summary>
    public class ResultPanel : UIPanel
    {
        Text _headline, _name, _latin, _size, _extra;
        Image _fishIcon;
        Button _keepBtn, _releaseBtn;
        Action<bool> _onChoose;
        float _shownAt;

        public ResultPanel(Transform canvas)
        {
            BuildFrame(canvas, "ResultPanel", "", new Vector2(640, 500), closable: false);
            _headline = UIFactory.Text(Frame, "", 44, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _headline.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(580, 60));

            _fishIcon = UIFactory.Image(Frame, new Color(0.6f, 0.55f, 0.4f), UIFactory.Fish, "FishIcon");
            _fishIcon.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(200, 80), new Vector2(0.5f, 0.5f));

            _name = UIFactory.Text(Frame, "", 34, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _name.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(580, 44));
            _latin = UIFactory.Text(Frame, "", 16, UIFactory.TextDim, TextAnchor.MiddleCenter, FontStyle.Italic);
            _latin.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(580, 22));
            _size = UIFactory.Text(Frame, "", 26, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _size.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -274), new Vector2(580, 36));
            _extra = UIFactory.Text(Frame, "", 18, UIFactory.Accent, TextAnchor.MiddleCenter);
            _extra.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -318), new Vector2(580, 50));

            _keepBtn = UIFactory.Button(Frame, "", () => Choose(true), new Color(0.15f, 0.45f, 0.42f), 20);
            ((RectTransform)_keepBtn.transform).At(new Vector2(0.5f, 0), new Vector2(-148, 28), new Vector2(280, 56));
            _releaseBtn = UIFactory.Button(Frame, "", () => Choose(false), UIFactory.ButtonBg, 20);
            ((RectTransform)_releaseBtn.transform).At(new Vector2(0.5f, 0), new Vector2(148, 28), new Vector2(280, 56));
        }

        public void Show(CaughtFish fish, int exp, bool record, Action<bool> onChoose)
        {
            var s = FishDatabase.Get(fish.speciesId);
            _onChoose = onChoose;
            _shownAt = Time.unscaledTime;

            bool trophy = s.IsTrophy(fish.length);
            _headline.text = s.IsJunk ? "어라...?" : trophy ? $"{s.TrophyName}!" : "낚았다!";
            _headline.color = s.IsJunk ? UIFactory.TextDim : trophy ? new Color(1f, 0.45f, 0.55f) : UIFactory.Gold;
            _name.text = s.Name;
            _latin.text = s.IsJunk ? s.Temper : $"{s.Latin}  ·  {s.Temper}";
            _size.text = s.IsJunk ? "" : $"{fish.length:0.0} cm   ·   {FormatWeight(fish.weight)}";
            _extra.text = (record ? "★ 개인 신기록! ★\n" : "") + $"경험치 +{exp}   ·   {fish.zone}";

            float len = Mathf.Clamp(fish.length / 100f, 0.15f, 1f);
            _fishIcon.sprite = s.IsJunk ? UIFactory.Circle : UIFactory.Fish;
            float w = 90 + 170 * len; // 아이콘 영역(높이 약 110px) 안에서 크기로 대물 느낌을 준다
            _fishIcon.rectTransform.sizeDelta = s.IsJunk ? new Vector2(60, 60) : new Vector2(w, w * 0.42f);
            _fishIcon.color = s.Id == "goldkoi" ? new Color(1f, 0.75f, 0.2f) : s.IsJunk ? new Color(0.5f, 0.5f, 0.55f) : new Color(0.62f, 0.58f, 0.42f);

            if (s.IsJunk)
            {
                _keepBtn.gameObject.SetActive(false);
                _releaseBtn.GetComponentInChildren<Text>().text = "버리기";
            }
            else
            {
                _keepBtn.gameObject.SetActive(true);
                _keepBtn.GetComponentInChildren<Text>().text = $"살림망에 넣기  ({PlayerProfile.SellPrice(fish):N0}골드)";
                _releaseBtn.GetComponentInChildren<Text>().text = "방생하기  (경험치 +30%)";
            }
            Open();
        }

        static string FormatWeight(float grams) => grams >= 1000f ? $"{grams / 1000f:0.00} kg" : $"{grams:0} g";

        void Choose(bool keep)
        {
            if (Time.unscaledTime - _shownAt < 0.3f) return; // 파이팅 중 연타로 바로 닫히지 않게
            Close();
            var cb = _onChoose;
            _onChoose = null;
            cb?.Invoke(keep);
        }

        public void ChooseByKey(bool keep) => Choose(keep);

        public override void Refresh() { }
    }
}
