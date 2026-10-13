using System.Collections.Generic;
using System.Linq;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 원작 "낚시터 목록 FISHINGAREA LIST" 화면.
    ///  왼쪽: KOREA 지도와 낚시터 이름 목록 / 오른쪽: MAP INFO(도감 달성도), FISH INFO(어종 넘겨보기, 개인 기록)
    ///  아래: 채널(방) 목록 + [빠른입장] [방만들기] [입장하기] [나가기]
    /// </summary>
    public class FishingAreaPanel : UIPanel
    {
        const float MapW = 440f, MapH = 440f * 560f / 420f;

        readonly List<(Button btn, Image ring, FishingArea area)> _markers = new List<(Button, Image, FishingArea)>();
        readonly List<(TMP_Text text, FishingArea area)> _listItems = new List<(TMP_Text, FishingArea)>();
        readonly List<(Image bg, int channel)> _channels = new List<(Image, int)>();
        TMP_Text _mapName, _dexRate, _fishName, _fishLatin, _seasonRecord, _records, _status;
        Image _dexFill, _fishImage;
        RectTransform _mapLabel;
        TMP_Text _mapLabelText;
        FishingArea _selected;
        int _fishIndex;
        int _channel = 1;

        /// <summary>입장 버튼을 눌렀을 때 (낚시터 id, 채널).</summary>
        public System.Action<FishingArea, int> Enter;

        public FishingAreaPanel(Transform canvas)
        {
            BuildFrame(canvas, "FishingAreaPanel", "낚시터 목록  <size=16><color=#80c8ff>FISHINGAREA LIST</color></size>", new Vector2(1240, 900));

            // ---- 지도 ----
            var mapFrame = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "MapFrame", 0f);
            mapFrame.rectTransform.At(new Vector2(0, 1), Vector2.zero, new Vector2(MapW + 230, MapH + 16));
            UIFactory.Text(mapFrame.transform, "KOREA", 30, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, -10), new Vector2(200, 40));
            var map = UIFactory.Node("Map", mapFrame.transform).At(new Vector2(1, 1), new Vector2(-8, -8), new Vector2(MapW, MapH));
            var raw = UIFactory.RoundedTexture(map, KoreaMap.Texture, 10f, "MapTexture");
            raw.rectTransform.Fill();

            float listY = -56f;
            foreach (var area in FishingAreas.Korea)
            {
                var a = area;
                // 왼쪽 이름 목록
                var item = UIFactory.Button(mapFrame.transform, "", () => Select(a), new Color(0, 0, 0, 0), 17);
                ((RectTransform)item.transform).At(new Vector2(0, 1), new Vector2(10, listY), new Vector2(200, 28));
                var t = item.GetComponentInChildren<TMP_Text>();
                t.alignment = UIFactory.Align(TextAnchor.MiddleLeft);
                t.text = a.Name;
                _listItems.Add((t, a));
                listY -= 30f;

                // 지도 마커
                var uv = KoreaMap.ToUV(a.Lon, a.Lat);
                var ring = UIFactory.Image(map, a.Open ? new Color(0.45f, 0.85f, 0.3f) : new Color(0.3f, 0.7f, 0.95f), UIFactory.Ring, "Marker");
                ring.raycastTarget = true;
                ring.rectTransform.At(new Vector2(0, 0), new Vector2(uv.x * MapW, uv.y * MapH), new Vector2(30, 30), new Vector2(0.5f, 0.5f));
                var dot = UIFactory.Image(ring.transform, new Color(0.05f, 0.1f, 0.15f, 0.9f), UIFactory.Circle, "Dot");
                dot.rectTransform.Fill(5, 5, 5, 5);
                UIFactory.Text(ring.transform, "↓", 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
                var btn = ring.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => Select(a));
                _markers.Add((btn, ring, a));
            }
            _mapLabel = UIFactory.Node("SelectedLabel", map).At(new Vector2(0, 0), Vector2.zero, new Vector2(130, 30), new Vector2(0.5f, 0f));
            var labelBg = UIFactory.Shape(_mapLabel, new Color(0.03f, 0.1f, 0.13f, 0.92f), UIFactory.Pill, "Bg", 4f);
            labelBg.rectTransform.Fill();
            UIFactory.AddBorder(labelBg, new Color(0.36f, 0.88f, 0.8f, 0.7f), 1.5f);
            _mapLabelText = UIFactory.Text(_mapLabel, "", 17, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _mapLabelText.rectTransform.Fill();

            // ---- MAP INFO ----
            float rightX = MapW + 250f;
            var info = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "MapInfo", 0f);
            info.rectTransform.At(new Vector2(0, 1), new Vector2(rightX, 0), new Vector2(460, 120));
            Header(info.transform, "MAP INFO");
            _mapName = UIFactory.Text(info.transform, "", 30, new Color(0.45f, 0.95f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _mapName.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(440, 40));
            UIFactory.Text(info.transform, "도감 달성도", 16, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform
                .At(new Vector2(0, 1), new Vector2(40, -80), new Vector2(110, 24));
            _dexFill = UIFactory.Bar(info.transform, new Color(0f, 0f, 0f, 0.4f), UIFactory.Accent, "Dex");
            ((RectTransform)_dexFill.transform.parent).At(new Vector2(0, 1), new Vector2(150, -86), new Vector2(200, 12));
            _dexRate = UIFactory.Text(info.transform, "", 16, UIFactory.TextMain, TextAnchor.MiddleRight);
            _dexRate.rectTransform.At(new Vector2(0, 1), new Vector2(360, -80), new Vector2(70, 24));

            // ---- FISH INFO ----
            var fish = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "FishInfo", 0f);
            fish.rectTransform.At(new Vector2(0, 1), new Vector2(rightX, -130), new Vector2(460, 340));
            Header(fish.transform, "FISH INFO");
            _fishName = UIFactory.Text(fish.transform, "", 26, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _fishName.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(440, 34));
            _fishLatin = UIFactory.Text(fish.transform, "", 15, UIFactory.TextDim, TextAnchor.MiddleCenter, FontStyle.Italic);
            _fishLatin.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(440, 22));
            _fishImage = UIFactory.Image(fish.transform, Color.white, UIFactory.Fish, "Fish");
            _fishImage.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(240, 110), new Vector2(0.5f, 0.5f));
            var prev = UIFactory.IconButton(fish.transform, "◀", () => Flip(-1), UIFactory.BtnGhost, 20);
            ((RectTransform)prev.transform).At(new Vector2(0, 1), new Vector2(16, -130), new Vector2(42, 42));
            var next = UIFactory.IconButton(fish.transform, "▶", () => Flip(1), UIFactory.BtnGhost, 20);
            ((RectTransform)next.transform).At(new Vector2(1, 1), new Vector2(-16, -130), new Vector2(42, 42));
            _seasonRecord = UIFactory.Text(fish.transform, "", 20, new Color(0.75f, 1f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _seasonRecord.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -222), new Vector2(440, 50));
            _records = UIFactory.Text(fish.transform, "", 15, UIFactory.TextDim, TextAnchor.MiddleCenter);
            _records.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -282), new Vector2(440, 50));

            // ---- 채널 목록 ----
            var rooms = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "Rooms", 0f);
            rooms.rectTransform.At(new Vector2(0, 0), Vector2.zero, new Vector2(MapW + 230, 180));
            var head = UIFactory.Node("Head", rooms.transform).At(new Vector2(0, 1), Vector2.zero, new Vector2(MapW + 230, 26));
            UIFactory.Text(head, "방번호", 14, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform.At(new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(80, 24));
            UIFactory.Text(head, "방제목", 14, UIFactory.TextDim, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(200, 24));
            UIFactory.Text(head, "방인원", 14, UIFactory.TextDim, TextAnchor.MiddleRight).rectTransform.At(new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(80, 24));
            for (int i = 0; i < 5; i++)
            {
                int ch = i + 1;
                var row = UIFactory.Button(rooms.transform, "", () => { _channel = ch; RefreshChannels(); }, UIFactory.TabOff, 17);
                ((RectTransform)row.transform).At(new Vector2(0, 1), new Vector2(8, -30 - i * 30), new Vector2(MapW + 214, 27));
                var label = row.GetComponentInChildren<TMP_Text>();
                label.alignment = UIFactory.Align(TextAnchor.MiddleLeft);
                label.richText = true;
                _channels.Add((row.GetComponent<Image>(), ch));
            }

            // ---- 버튼 ----
            var bx = new Vector2(rightX, 0);
            // 원작 순서: 위에서부터 빠른입장, 방만들기, 입장하기, 나가기
            BigButton("빠른입장", "QUICK JOIN", () => DoEnter(), new Vector2(0, 168), new Vector2(460, 64), rightX, true);
            BigButton("방만들기", "CREATE ROOM", null, new Vector2(0, 112), new Vector2(460, 50), rightX, false);
            BigButton("입장하기", "JOIN ROOM", () => DoEnter(), new Vector2(0, 56), new Vector2(460, 50), rightX, true);
            BigButton("나가기", "EXIT", Close, new Vector2(0, 0), new Vector2(460, 50), rightX, true);
            _status = UIFactory.Text(Body, "", 16, UIFactory.Warn, TextAnchor.MiddleCenter);
            _status.rectTransform.At(new Vector2(0, 0), new Vector2(rightX, 244), new Vector2(460, 26));

            Select(FishingAreas.Korea[0]);
        }

        static void Header(Transform parent, string text)
        {
            UIFactory.Text(parent, text, 13, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, "Header").rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -6), new Vector2(300, 22));
        }

        void BigButton(string ko, string en, System.Action onClick, Vector2 bottomOffset, Vector2 size, float x, bool enabled)
        {
            var b = UIFactory.Button(Body, $"<b>{ko}</b>\n<size=12>{en}</size>", onClick,
                enabled ? (en == "QUICK JOIN" ? UIFactory.BtnPrimary : UIFactory.BtnNeutral) : new Color(1f, 1f, 1f, 0.05f), 20);
            UIFactory.SetRadius(b.targetGraphic, 14f);
            ((RectTransform)b.transform).At(new Vector2(0, 0), new Vector2(x, bottomOffset.y), size);
            b.interactable = enabled;
        }

        void Select(FishingArea area)
        {
            _selected = area;
            _fishIndex = 0;
            Refresh();
        }

        void Flip(int dir)
        {
            var list = FishDatabase.DexOf(_selected?.MapId ?? "hangang").ToList();
            _fishIndex = (_fishIndex + dir + list.Count) % list.Count;
            Refresh();
        }

        void DoEnter()
        {
            if (_selected == null) return;
            if (!_selected.Open)
            {
                _status.text = $"{_selected.Name}은(는) 아직 준비 중입니다. 지금은 한강과 홍현 앞바다가 열려 있어요.";
                return;
            }
            if (Game.Profile.Data.level < _selected.Level)
            {
                _status.text = $"{_selected.Name}은(는) Lv.{_selected.Level}부터 입장할 수 있습니다.";
                return;
            }
            Close();
            Enter?.Invoke(_selected, _channel);
        }

        public override void Refresh()
        {
            if (_selected == null) return;
            var p = Game.Profile;
            _status.text = "";

            foreach (var (text, area) in _listItems)
            {
                bool sel = area == _selected;
                text.color = sel ? new Color(0.45f, 0.95f, 0.6f) : area.Open ? UIFactory.TextMain : UIFactory.TextDim;
                text.text = area.Open ? area.Name : $"{area.Name} <size=12><color=#607080>준비 중</color></size>";
            }
            foreach (var (btn, ring, area) in _markers)
                ring.rectTransform.localScale = Vector3.one * (area == _selected ? 1.45f : 1f);

            var uv = KoreaMap.ToUV(_selected.Lon, _selected.Lat);
            _mapLabel.anchoredPosition = new Vector2(uv.x * MapW, uv.y * MapH + 22f);
            _mapLabelText.text = _selected.Name;

            _mapName.text = _selected.Name;
            var species = FishDatabase.DexOf(_selected?.MapId ?? "hangang").ToList();
            float rate = _selected.Open ? p.Data.dex.Count / (float)species.Count : 0f;
            _dexFill.fillAmount = rate;
            _dexRate.text = $"{rate * 100f:0} %";

            if (_selected.Open)
            {
                var s = species[_fishIndex % species.Count];
                var entry = p.Dex(s.Id);
                bool known = entry != null;
                _fishName.text = known ? s.Name : "???";
                _fishLatin.text = known ? s.Latin : "아직 낚지 못한 어종";
                _fishImage.color = known ? Fishing.Angling.FishModels.BodyColor(s) * 1.25f : new Color(0.1f, 0.12f, 0.15f);
                _seasonRecord.text = known ? $"PERSONAL RECORD\n{entry.maxLength:0.00}cm  강태공" : "PERSONAL RECORD\n-";
                _records.text = $"[{_selected.Name} 최대] {s.MaxLen:0.00}cm\n[대물 기준] {s.TrophyLen:0.00}cm {s.TrophyName}";
            }
            else
            {
                _fishName.text = "준비 중";
                _fishLatin.text = _selected.Desc;
                _fishImage.color = new Color(0.1f, 0.12f, 0.15f);
                _seasonRecord.text = $"입장 레벨 Lv.{_selected.Level}";
                _records.text = "다음 업데이트에서 열릴 예정입니다";
            }
            RefreshChannels();
        }

        void RefreshChannels()
        {
            foreach (var (bg, ch) in _channels)
            {
                bool sel = ch == _channel;
                bg.color = sel ? UIFactory.TabOn : UIFactory.TabOff;
                var t = bg.GetComponentInChildren<TMP_Text>();
                string state = _selected != null && _selected.Open ? "<color=#60e060>원활</color>" : "<color=#607080>준비 중</color>";
                t.text = $"   No.{ch:000}        {(_selected?.Name ?? "")} CH.{ch}                                                      {state}";
            }
        }
    }
}
