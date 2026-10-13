using Fishing.Angling;
using Fishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 파이팅 HUD. 원작 스크린샷 배치를 따른다.
    ///  - 위: 파이팅 레벨 링(바늘 걸림) + 물고기 HP 바 + 거리
    ///  - 아래: LOW~HIGH 장력 바 + 카운터 콤보 구슬
    /// </summary>
    public class FightHudView
    {
        readonly RectTransform _root;
        Image _hookRing;
        Text _hookText;
        Image _hpFill;
        Text _nameText, _distText;
        Image _distFill;
        RectTransform _tensionBar;
        Image _tensionFill;
        RectTransform _tensionMarker;
        Text _highLabel;
        readonly Image[] _pips = new Image[5];
        Text _counterText, _warnText, _landText, _hintText, _staminaText;
        float _counterUntil;

        public FightHudView(Transform canvas)
        {
            _root = UIFactory.Node("FightHud", canvas).Fill();
            BuildTop();
            BuildBottom();
            BuildCenter();
            _root.gameObject.SetActive(false);
        }

        void BuildTop()
        {
            var top = UIFactory.Node("Top", _root).At(new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(860, 96));
            UIFactory.Image(top, new Color(0, 0, 0, 0.45f), null, "Back").rectTransform.Fill(40, 0, 10, 10);

            var ringBack = UIFactory.Image(top, new Color(0, 0, 0, 0.7f), UIFactory.Circle, "RingBack").rectTransform
                .At(new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(96, 96));
            _hookRing = UIFactory.Image(ringBack, UIFactory.Good, UIFactory.Ring, "Ring");
            _hookRing.rectTransform.Fill(4, 4, 4, 4);
            _hookRing.type = Image.Type.Filled;
            _hookRing.fillMethod = Image.FillMethod.Radial360;
            _hookRing.fillOrigin = (int)Image.Origin360.Top;
            _hookText = UIFactory.Text(ringBack, "", 18, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hookText.rectTransform.Fill();

            UIFactory.Text(top, "HP", 18, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(110, -14), new Vector2(40, 24));
            _nameText = UIFactory.Text(top, "", 18, UIFactory.TextMain, TextAnchor.MiddleRight);
            _nameText.rectTransform.At(new Vector2(1, 1), new Vector2(-16, -12), new Vector2(500, 24));

            _hpFill = UIFactory.Bar(top, new Color(0.15f, 0.05f, 0.05f, 0.9f), new Color(0.98f, 0.45f, 0.3f), "HP");
            ((RectTransform)_hpFill.transform.parent).At(new Vector2(0, 1), new Vector2(110, -40), new Vector2(730, 22));

            _distFill = UIFactory.Bar(top, new Color(0, 0, 0, 0.6f), new Color(0.75f, 0.85f, 0.95f, 0.8f), "Distance");
            ((RectTransform)_distFill.transform.parent).At(new Vector2(0, 1), new Vector2(110, -68), new Vector2(600, 8));
            _distText = UIFactory.Text(top, "", 20, UIFactory.TextMain, TextAnchor.MiddleRight, FontStyle.Bold);
            _distText.rectTransform.At(new Vector2(1, 1), new Vector2(-16, -60), new Vector2(120, 26));
        }

        void BuildBottom()
        {
            var bottom = UIFactory.Node("Bottom", _root).At(new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(980, 40));

            for (int i = 0; i < _pips.Length; i++)
            {
                var pip = UIFactory.Image(bottom, new Color(1, 1, 1, 0.2f), UIFactory.Circle, $"Pip{i}");
                pip.rectTransform.At(new Vector2(0, 0.5f), new Vector2(i * 26, 0), new Vector2(22, 22));
                _pips[i] = pip;
            }

            UIFactory.Text(bottom, "LOW", 16, new Color(0.6f, 0.8f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(50, 24));
            _tensionBar = UIFactory.Node("TensionBar", bottom).At(new Vector2(0, 0.5f), new Vector2(190, 0), new Vector2(680, 22));
            // 구간 색: 0~0.6 일반, 0.6~0.9 하이텐션, 0.9~1 위험
            AddZone(0f, FishFight.HighTension, new Color(0.15f, 0.35f, 0.6f, 0.85f));
            AddZone(FishFight.HighTension, FishFight.DangerTension, new Color(0.75f, 0.5f, 0.15f, 0.85f));
            AddZone(FishFight.DangerTension, 1f, new Color(0.75f, 0.15f, 0.12f, 0.9f));
            _tensionFill = UIFactory.Image(_tensionBar, new Color(1, 1, 1, 0.35f), null, "Fill");
            _tensionFill.rectTransform.Fill();
            _tensionFill.type = Image.Type.Filled;
            _tensionFill.fillMethod = Image.FillMethod.Horizontal;
            _tensionMarker = UIFactory.Text(_tensionBar, "◆", 30, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "Marker").rectTransform
                .At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(30, 30), new Vector2(0.5f, 0.5f));
            _highLabel = UIFactory.Text(bottom, "HIGH", 16, UIFactory.Warn, TextAnchor.MiddleLeft, FontStyle.Bold);
            _highLabel.rectTransform.At(new Vector2(0, 0.5f), new Vector2(880, 0), new Vector2(60, 24));

            _hintText = UIFactory.Text(_root, "좌클릭 감기 · 우클릭 풀기 · 마우스 좌우로 낚싯대 방향 · 물고기가 끝에 몰리면 반대로 휙! = 카운터", 17,
                UIFactory.TextDim, TextAnchor.MiddleCenter);
            _hintText.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(1100, 26));
        }

        void AddZone(float from, float to, Color color)
        {
            var img = UIFactory.Image(_tensionBar, color, null, "Zone");
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(from, 0);
            rt.anchorMax = new Vector2(to, 1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void BuildCenter()
        {
            _counterText = UIFactory.Text(_root, "COUNTER!", 96, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _counterText.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 140));
            var outline = _counterText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.15f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3, -3);
            _counterText.gameObject.SetActive(false);

            _warnText = UIFactory.Text(_root, "", 26, UIFactory.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            _warnText.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(900, 36));
            _landText = UIFactory.Text(_root, "[Space] 랜딩!", 44, UIFactory.Good, TextAnchor.MiddleCenter, FontStyle.Bold);
            _landText.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(600, 70));
            _staminaText = UIFactory.Text(_root, "", 18, UIFactory.Warn, TextAnchor.MiddleLeft);
            _staminaText.rectTransform.At(new Vector2(0, 0), new Vector2(24, 120), new Vector2(500, 26));
        }

        public void ShowCounter(int combo)
        {
            _counterUntil = Time.time + 0.9f;
            _counterText.text = combo > 1 ? $"COUNTER! x{combo}" : "COUNTER!";
            _counterText.gameObject.SetActive(true);
        }

        public void Update()
        {
            var fishing = Game.Fishing;
            var f = fishing.Fight;
            bool show = fishing.State == FishingState.Fighting && f != null;
            _root.gameObject.SetActive(show);
            if (!show) return;

            float hook = f.Hook / 100f;
            _hookRing.fillAmount = hook;
            _hookRing.color = hook > 0.6f ? UIFactory.Good : hook > 0.3f ? UIFactory.Warn : UIFactory.Danger;
            _hookText.text = $"{f.Hook:0}";

            _hpFill.fillAmount = f.HP / f.MaxHP;
            _hpFill.color = f.HpLow ? UIFactory.Good : new Color(0.98f, 0.45f, 0.3f);
            _nameText.text = fishing.BigBite ? "<color=#ff7090>♥ 두근두근 ♥</color>  ???" : "???";

            _distFill.fillAmount = Mathf.Clamp01(f.Distance / f.Reel.Capacity);
            _distFill.color = f.Distance > f.Reel.Capacity * 0.8f ? UIFactory.Danger : new Color(0.75f, 0.85f, 0.95f, 0.8f);
            _distText.text = $"{f.Distance:0.0} m";

            float tension = Mathf.Clamp01(f.Tension);
            _tensionFill.fillAmount = tension;
            _tensionMarker.anchorMin = _tensionMarker.anchorMax = new Vector2(tension, 0.5f);
            _tensionMarker.anchoredPosition = Vector2.zero;
            _tensionMarker.localScale = Vector3.one * (f.IsDanger ? 1.2f + Mathf.Sin(Time.time * 30f) * 0.15f : 1f);
            _highLabel.color = f.IsHighTension ? UIFactory.Warn : new Color(1, 1, 1, 0.35f);

            for (int i = 0; i < _pips.Length; i++)
                _pips[i].color = i < f.Combo ? UIFactory.Accent : new Color(1, 1, 1, 0.2f);

            if (_counterText.gameObject.activeSelf)
            {
                float left = _counterUntil - Time.time;
                _counterText.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, left - 0.6f) * 1.5f);
                if (left <= 0f) _counterText.gameObject.SetActive(false);
            }

            if (f.IsDanger) _warnText.text = "줄이 끊어지려 한다! 우클릭으로 풀어주세요";
            else if (f.Hook < 30f) _warnText.text = "바늘이 빠지려 한다! 줄을 감아 팽팽하게";
            else if (f.Distance > f.Reel.Capacity * 0.8f) _warnText.text = "줄이 얼마 남지 않았다!";
            else if (f.AtEdge && !f.HpLow) _warnText.text = "<color=#ffd060>지금 반대로 휙! 카운터 기회</color>";
            else _warnText.text = "";

            _landText.gameObject.SetActive(f.CanLand);
            if (f.CanLand) _landText.rectTransform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.1f);
            else if (f.HpLow) _warnText.text = string.IsNullOrEmpty(_warnText.text) ? $"지쳤다! {FishFight.LandDistance:0}m 안으로 끌어오면 랜딩" : _warnText.text;

            _staminaText.text = Game.Profile.Data.stamina < 30f ? $"체력 {Game.Profile.Data.stamina:0} · 지쳐서 힘이 빠진다..." : $"체력 {Game.Profile.Data.stamina:0}";
            _staminaText.color = Game.Profile.Data.stamina < 30f ? UIFactory.Danger : UIFactory.TextDim;
        }
    }
}
