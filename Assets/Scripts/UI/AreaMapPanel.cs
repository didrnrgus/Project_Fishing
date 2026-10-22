using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 지역 지도(M). 원작의 "홍현 앞바다" 지도처럼 수면을 격자(한강 A~F×1~4, 홍현 A~H×1~8)로 보여 주고
    /// 칸마다 서식지 색, 어군(큰/작은), 내 위치와 찌 위치를 표시한다.
    /// 지형은 한 번만 그리고, 움직이는 표시는 <see cref="Tick"/>에서 갱신한다.
    /// </summary>
    public class AreaMapPanel : UIPanel
    {
        // 지도 칸 크기(최대). 맵의 View 범위를 이 안에 맞춰 축척을 정한다
        const float BoxW = 860f, BoxH = 520f;
        float ViewMinX, ViewMaxX, ViewMinZ, ViewMaxZ, Scale, MapW, MapH;
        string _builtFor;
        RectTransform _mapFrame, _side, _footer;

        RectTransform _map;
        RectTransform _player, _bobber, _cellHighlight;
        // 퀘스트 책 [힌트맵]: 갈 곳 핀 (원작 숫자 핀)
        RectTransform _hint;
        TMP_Text _hintLabel;
        Vector3? _hintPos;
        string _hintText;
        TMP_Text _cellInfo;
        readonly List<(RectTransform root, Image halo, TMP_Text label)> _schoolMarks = new List<(RectTransform, Image, TMP_Text)>();
        RectTransform _schoolList;
        float _nextListRefresh;

        public AreaMapPanel(Transform canvas)
        {
            BuildFrame(canvas, "AreaMapPanel", UIFactory.Caption("지역 지도", "AREA MAP"), new Vector2(1240, 720));
            Rebuild();
        }

        /// <summary>지금 낚시터에 맞춰 지도와 옆 패널을 새로 그린다.</summary>
        void Rebuild()
        {
            var map = FieldMap.Current;
            _builtFor = map.Id;
            if (_mapFrame != null) Object.Destroy(_mapFrame.gameObject);
            if (_side != null) Object.Destroy(_side.gameObject);
            if (_footer != null) Object.Destroy(_footer.gameObject);
            _schoolMarks.Clear();
            ViewMinX = map.View.xMin; ViewMaxX = map.View.xMax; ViewMinZ = map.View.yMin; ViewMaxZ = map.View.yMax;
            Scale = Mathf.Min(BoxW / map.View.width, BoxH / map.View.height);
            MapW = map.View.width * Scale;
            MapH = map.View.height * Scale;
            BuildMap();
            BuildSide();
            _footer = UIFactory.Text(Body, "[M] 또는 [ESC]로 닫기  ·  ▲ 나  ● 찌  ◆ 어군(크기 = 큰/작은 어군, 빨강 = 대물 어군)", 14, UIFactory.TextDim, TextAnchor.MiddleLeft)
                .rectTransform.At(new Vector2(0, 1), new Vector2(4, -(MapH + 20)), new Vector2(BoxW, 24), new Vector2(0, 1));
        }

        Vector2 ToMap(float x, float z) => new Vector2((x - ViewMinX) * Scale, (z - ViewMinZ) * Scale);

        static Color HabitatColor(Habitat h) => h switch
        {
            Habitat.Shallow => new Color(0.32f, 0.58f, 0.72f, 0.55f),
            Habitat.Deep => new Color(0.08f, 0.2f, 0.38f, 0.55f),
            Habitat.Rocky => new Color(0.45f, 0.45f, 0.48f, 0.55f),
            Habitat.Weed => new Color(0.28f, 0.52f, 0.3f, 0.55f),
            _ => new Color(0.2f, 0.3f, 0.4f, 0.5f)
        };

        RectTransform Place(RectTransform rt, float x, float z, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = ToMap(x, z);
            rt.sizeDelta = size;
            return rt;
        }

        void BuildMap()
        {
            var map = FieldMap.Current;
            _mapFrame = UIFactory.Shape(Body, new Color(0f, 0f, 0f, 0.35f), UIFactory.RadiusCard, "MapFrame", 0f).rectTransform
                .At(new Vector2(0, 1), Vector2.zero, new Vector2(MapW + 8, MapH + 8), new Vector2(0, 1));
            _map = UIFactory.Node("Map", _mapFrame).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapW, MapH));
            _map.gameObject.AddComponent<RectMask2D>();

            // 땅 + 물 (첫 표시가 물)
            UIFactory.Image(_map, new Color(0.3f, 0.36f, 0.24f), null, "Land").rectTransform.Fill();
            var marks = new List<MapMark>();
            for (int i = 0; i < map.Marks.Count; i++)
            {
                var m = map.Marks[i];
                if (i == 0)
                {
                    Place(UIFactory.Image(_map, m.Color, null, "Water").rectTransform, m.Area.center.x, m.Area.center.y,
                        new Vector2(m.Area.width * Scale, m.Area.height * Scale));
                    continue;
                }
                marks.Add(m);
            }

            // 격자 칸: 서식지 색 + 칸 이름 + 서식지 이름
            int fs = Mathf.Clamp(Mathf.RoundToInt(map.CellH * Scale * 0.22f), 11, 15);
            for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
            {
                var center = map.CellCenter(c, r);
                var h = map.HabitatAt(c, r);
                var cell = Place(UIFactory.Image(_map, HabitatColor(h), null, "Cell").rectTransform, center.x, center.z,
                    new Vector2(map.CellW * Scale - 3f, map.CellH * Scale - 3f));
                UIFactory.Text(cell, map.CellName(c, r), fs, new Color(1, 1, 1, 0.85f), TextAnchor.UpperLeft, FontStyle.Bold)
                    .rectTransform.Fill(5, 5, 3, 3);
                // 칸이 작으면(홍현 8x8) 서식지 이름은 빼고 색과 범례로만 보여 준다 (어군 라벨과 겹치지 않게)
                if (map.CellH * Scale >= 70f)
                    UIFactory.Text(cell, map.HabitatName(h), fs - 3, new Color(1, 1, 1, 0.45f), TextAnchor.LowerRight).rectTransform.Fill(5, 5, 3, 3);
            }

            // 열 머리글
            for (int c = 0; c < map.Cols; c++)
            {
                float x = map.GridMinX + (c + 0.5f) * map.CellW;
                Place(UIFactory.Text(_map, ((char)('A' + c)).ToString(), 16, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    x, map.GridMaxZ + 12f / Scale, new Vector2(40, 22));
            }

            // 지형 표시 (다리, 데크, 방파제, 등대, 섬, 낚시점) + 이름
            foreach (var m in marks)
            {
                var size = new Vector2(Mathf.Max(5f, m.Area.width * Scale), Mathf.Max(5f, m.Area.height * Scale));
                Place(UIFactory.Image(_map, m.Color, m.Kind == MapMark.Shape.Circle ? UIFactory.Circle : null, "Mark").rectTransform,
                    m.Area.center.x, m.Area.center.y, size);
                if (!string.IsNullOrEmpty(m.Label))
                {
                    var label = UIFactory.Text(_map, m.Label, 13, new Color(1, 0.92f, 0.75f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform;
                    Place(label, m.Area.xMax, m.Area.center.y, new Vector2(110, 20));
                    label.pivot = new Vector2(0f, 0.5f);
                    label.anchoredPosition += new Vector2(4f, 0f);
                }
            }
            var north = UIFactory.Text(_map, "N ▲", 14, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform;
            north.anchorMin = north.anchorMax = new Vector2(1, 1);
            north.pivot = new Vector2(1, 1);
            north.anchoredPosition = new Vector2(-6, -6);
            north.sizeDelta = new Vector2(50, 24);

            // 움직이는 표시: 찌가 있는 칸, 찌, 플레이어
            _cellHighlight = Place(UIFactory.Image(_map, new Color(1f, 0.9f, 0.4f, 0.22f), null, "CellHighlight").rectTransform, 0, 0,
                new Vector2(map.CellW * Scale, map.CellH * Scale));
            _bobber = Place(UIFactory.Image(_map, new Color(1f, 0.3f, 0.2f), UIFactory.Circle, "Bobber").rectTransform, 0, 0, new Vector2(12, 12));
            _hint = Place(UIFactory.Node("QuestHint", _map), 0, 0, new Vector2(10, 10));
            UIFactory.Image(_hint, new Color(1f, 0.8f, 0.2f, 0.35f), UIFactory.Circle, "Halo").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54, 54));
            var pin = UIFactory.Shape(_hint, new Color(1f, 0.78f, 0.15f), UIFactory.Pill, "Pin", 0f);
            pin.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
            UIFactory.Text(pin.transform, "!", 18, new Color(0.25f, 0.15f, 0f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
            UIFactory.Shape(_hint, new Color(0.02f, 0.04f, 0.06f, 0.8f), UIFactory.Pill, "LabelBg", 0f).rectTransform
                .At(new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(150, 20));
            _hintLabel = UIFactory.Text(_hint, "", 13, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold, "Label");
            _hintLabel.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(170, 20));
            _player = Place(UIFactory.Text(_map, "▲", 26, new Color(0.45f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, "Player").rectTransform,
                0, 0, new Vector2(30, 30));
        }

        void BuildSide()
        {
            var side = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "Side", 0f).rectTransform
                .At(new Vector2(1, 1), Vector2.zero, new Vector2(310, 620), new Vector2(1, 1));
            _side = side;

            var title = UIFactory.Text(side, FieldMap.MapName, 20, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0, 1), new Vector2(14, -8), new Vector2(290, 30), new Vector2(0, 1));
            _cellInfo = UIFactory.Text(side, "", 14, UIFactory.Accent, TextAnchor.UpperLeft);
            _cellInfo.rectTransform.At(new Vector2(0, 1), new Vector2(14, -40), new Vector2(290, 40), new Vector2(0, 1));

            // 범례
            float y = -86f;
            UIFactory.Text(side, "서식지", 13, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, y), new Vector2(200, 18), new Vector2(0, 1));
            y -= 22f;
            var habitats = new[] { Habitat.Shallow, Habitat.Deep, Habitat.Rocky, Habitat.Weed };
            for (int i = 0; i < habitats.Length; i++)
            {
                float x = 14f + (i % 2) * 145f;
                float yy = y - (i / 2) * 24f;
                var col = HabitatColor(habitats[i]);
                col.a = 1f;
                UIFactory.Shape(side, col, 5f, "Swatch", 0f).rectTransform.At(new Vector2(0, 1), new Vector2(x, yy), new Vector2(18, 18), new Vector2(0, 1));
                UIFactory.Text(side, FieldMap.HabitatName(habitats[i]), 14, UIFactory.TextMain, TextAnchor.MiddleLeft).rectTransform
                    .At(new Vector2(0, 1), new Vector2(x + 24, yy + 1), new Vector2(110, 20), new Vector2(0, 1));
            }
            y -= 58f;

            UIFactory.Text(side, "어군", 13, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, y), new Vector2(200, 18), new Vector2(0, 1));
            _schoolList = UIFactory.Node("SchoolList", side).At(new Vector2(0, 1), new Vector2(14, y - 22f), new Vector2(286, 270), new Vector2(0, 1));

            var tip = UIFactory.Text(side,
                "어군이 있는 칸에 찌를 던지면 입질이 빠르고 그 어군의 고기가 잘 낚입니다.\n" +
                $"큰 어군에서 {FishSchoolManager.CatchesToShrink}마리를 낚으면 작은 어군으로 흩어지고, " +
                $"{FishSchoolManager.RegroupMinutes / 60f:0}시간 뒤 다시 모입니다. 어군은 2시간마다 옆 칸으로 움직여요.",
                13, UIFactory.TextDim, TextAnchor.LowerLeft);
            tip.rectTransform.At(new Vector2(0, 0), new Vector2(14, 12), new Vector2(286, 110), new Vector2(0, 0));
        }

        public override void Refresh()
        {
            if (_builtFor != FieldMap.Current.Id) Rebuild();
            _nextListRefresh = 0f;
            Tick();
        }

        /// <summary>퀘스트 [힌트맵]: 지도에 갈 곳 핀을 꽂는다 (pos가 null이면 핀 없음). 창을 연 다음에 부른다 — 닫으면 지워진다.</summary>
        public void SetHint(Vector3? pos, string label)
        {
            _hintPos = pos;
            _hintText = label;
            if (pos.HasValue) Game.Toast($"[힌트맵] {label} — 지도의 노란 핀", ToastKind.Info);
        }

        public override void Close()
        {
            _hintPos = null;
            base.Close();
        }

        public void Tick()
        {
            if (_hint != null)
            {
                _hint.gameObject.SetActive(_hintPos.HasValue);
                if (_hintPos.HasValue)
                {
                    _hint.anchoredPosition = ToMap(_hintPos.Value.x, _hintPos.Value.z);
                    _hint.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f);
                    _hintLabel.text = _hintText;
                }
            }
            // 플레이어와 찌
            var playerPos = Game.Player.transform.position;
            _player.anchoredPosition = ToMap(playerPos.x, playerPos.z);
            _player.localRotation = Quaternion.Euler(0, 0, -Game.Player.Yaw);

            var state = Game.Fishing.State;
            bool showBobber = state == FishingState.Waiting || state == FishingState.Bite || state == FishingState.Casting ||
                              state == FishingState.Fighting;
            _bobber.gameObject.SetActive(showBobber);
            var lookPos = showBobber ? Game.Fishing.BobberPosition : playerPos;
            if (showBobber) _bobber.anchoredPosition = ToMap(lookPos.x, lookPos.z);

            bool inCell = FieldMap.TryCell(lookPos, out int col, out int row);
            _cellHighlight.gameObject.SetActive(inCell);
            if (inCell)
            {
                var c = FieldMap.CellCenter(col, row);
                _cellHighlight.anchoredPosition = ToMap(c.x, c.z);
                var school = Game.Schools.At(col, row);
                _cellInfo.text = $"{(showBobber ? "찌 위치" : "현재 위치")} : {FieldMap.CellName(col, row)} · {FieldMap.HabitatName(FieldMap.HabitatAt(col, row))}" +
                                 (school != null ? $"\n{school.Name} ({school.SizeLabel})" : "");
            }
            else
            {
                _cellInfo.text = $"현재 위치 : {FieldMap.AreaName(playerPos)}";
            }

            // 어군 표시
            var schools = Game.Schools.Schools;
            while (_schoolMarks.Count < schools.Count)
            {
                var root = UIFactory.Node("School", _map);
                Place(root, 0, 0, new Vector2(10, 10));
                var halo = UIFactory.Image(root, new Color(1f, 0.7f, 0.25f, 0.35f), UIFactory.Circle, "Halo");
                halo.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
                UIFactory.Text(root, "◆", 18, new Color(1f, 0.8f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold, "Diamond").rectTransform
                    .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
                // 칸 이름·서식지 글자와 겹쳐도 읽히도록 어두운 띠를 깐다
                UIFactory.Shape(root, new Color(0.02f, 0.04f, 0.06f, 0.75f), UIFactory.Pill, "LabelBg", 0f).rectTransform
                    .At(new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(118, 18));
                var label = UIFactory.Text(root, "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Label");
                label.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(150, 18));
                _schoolMarks.Add((root, halo, label));
            }
            for (int i = 0; i < schools.Count; i++)
            {
                var s = schools[i];
                var (root, halo, label) = _schoolMarks[i];
                // 같은 칸에 두 어군이 겹치면 살짝 비켜 그린다
                root.gameObject.SetActive(!s.Hidden);
                // 물속 그림자는 칸이 아니라 실제로 헤엄치는 자리에
                var c = s.Shadow ? s.Spot : FieldMap.CellCenter(s.Col, s.Row);
                var offset = s.Shadow ? Vector2.zero : new Vector2((i % 2 == 0 ? -1f : 1f) * 22f, (i / 2 == 0 ? 1f : -1f) * 6f);
                if (s.Shadow) root.gameObject.SetActive(!s.Hidden && !s.Hooked);
                root.anchoredPosition = ToMap(c.x, c.z) + offset;
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f + i) * 0.08f;
                halo.rectTransform.localScale = Vector3.one * (s.Special ? 1.4f : s.Big ? 1f : 0.55f) * pulse;
                halo.color = s.Shadow ? (s.Quest ? new Color(1f, 0.82f, 0.3f, 0.5f) : new Color(0.55f, 0.85f, 1f, 0.5f))
                    : s.Kind == SchoolKind.Hidden ? new Color(1f, 0.9f, 0.3f, 0.55f) : s.Kind == SchoolKind.Predator ? new Color(1f, 0.25f, 0.4f, 0.5f)
                    : s.Special ? new Color(1f, 0.3f, 0.25f, 0.45f) : new Color(1f, 0.7f, 0.25f, 0.35f);
                root.GetChild(1).localScale = Vector3.one * (s.Special ? 1.5f : s.Big ? 1.2f : 0.75f);
                root.GetChild(1).GetComponent<TMPro.TMP_Text>().color = s.Shadow ? new Color(0.04f, 0.07f, 0.1f) : new Color(1f, 0.8f, 0.3f);
                label.text = s.Kind != SchoolKind.Normal ? s.Name : $"{s.Name} · {s.ShortLabel}";
            }

            if (Time.unscaledTime >= _nextListRefresh)
            {
                _nextListRefresh = Time.unscaledTime + 1f;
                RefreshSchoolList();
            }
        }

        void RefreshSchoolList()
        {
            UIFactory.ClearChildren(_schoolList);
            var schools = Game.Schools.Schools;
            int line = 0;
            for (int i = 0; i < schools.Count; i++)
            {
                var s = schools[i];
                string state;
                if (s.Shadow)
                {
                    // 그림자는 나타났을 때만 적는다 (언제 나올지 모르는 게 재미, 목록도 아래 설명과 겹치지 않게)
                    if (s.Hidden) continue;
                    float left = Mathf.Max(0f, s.ExpiresAt - Game.Clock.TotalMinutes) / 60f;
                    state = $"<color=#7fd8ff>지금 출현!</color>  {Mathf.CeilToInt(left)}시간 뒤 사라짐 · 천천히 헤엄침";
                }
                else if (s.Kind != SchoolKind.Normal)
                {
                    float left = Mathf.Max(0f, s.ExpiresAt - Game.Clock.TotalMinutes) / 60f;
                    state = s.Hidden
                        ? (s.Kind == SchoolKind.Hidden ? "<color=#9ab0c0>숨어 있음 · 큰 어군이 모두 흩어지면 나타남</color>" : "<color=#9ab0c0>먼바다에 있음 · 가끔 나타나 고기떼를 흩음</color>")
                        : $"<color={(s.Kind == SchoolKind.Hidden ? "#ffe060" : "#ff7080")}>지금 출현!</color>  {Mathf.CeilToInt(left)}시간 뒤 사라짐 · 남은 {s.CatchesLeft}마리";
                }
                else if (s.Special)
                {
                    string when = s.Schedule == null ? "" : string.Join(" · ", System.Array.ConvertAll(s.Schedule, e => $"{Names.Of(e.band)} {FieldMap.CellName(e.col, e.row)}"));
                    state = s.Hidden ? $"<color=#9ab0c0>숨어 있음</color>  ({when})" : $"<color=#ff8070>지금 출현!</color>  ({when})";
                }
                else if (s.Big) state = $"<color=#ffd060>큰 어군</color>  남은 {s.CatchesLeft}마리";
                else
                {
                    float left = Mathf.Max(0f, FishSchoolManager.RegroupMinutes - (Game.Clock.TotalMinutes - s.ShrankAt)) / 60f;
                    state = $"<color=#9ab0c0>작은 어군</color>  {Mathf.CeilToInt(left)}시간 뒤 모임";
                }
                var names = new List<string>();
                foreach (var id in s.SpeciesIds)
                {
                    var sp = FishDatabase.Get(id);
                    if (sp != null) names.Add(s.Shadow && !s.Quest ? "???" : sp.Name);
                }
                var t = UIFactory.Text(_schoolList,
                    $"<b>{s.Name}</b>  <color=#80c8ff>{(s.Hidden ? "-" : FieldMap.CellName(s.Col, s.Row))}</color>  <size=11><color=#8a96a0>{string.Join(", ", names)}</color></size>\n{state}",
                    13, UIFactory.TextMain, TextAnchor.UpperLeft);
                t.rectTransform.At(new Vector2(0, 1), new Vector2(0, -line++ * 46f), new Vector2(286, 44), new Vector2(0, 1));
            }
        }
    }
}
