using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 지역 지도(M). 원작의 "홍현 앞바다" 지도처럼 수면을 A~F열, 1~4행 격자로 보여 주고
    /// 칸마다 서식지 색, 어군(큰/작은), 내 위치와 찌 위치를 표시한다.
    /// 지형은 한 번만 그리고, 움직이는 표시는 <see cref="Tick"/>에서 갱신한다.
    /// </summary>
    public class AreaMapPanel : UIPanel
    {
        // 지도에 보이는 범위(월드 좌표, m)
        const float ViewMinX = -160f, ViewMaxX = 160f, ViewMinZ = -88f, ViewMaxZ = 88f;
        const float MapW = 860f;
        const float Scale = MapW / (ViewMaxX - ViewMinX);
        const float MapH = (ViewMaxZ - ViewMinZ) * Scale;

        RectTransform _map;
        RectTransform _player, _bobber, _cellHighlight;
        Text _cellInfo;
        readonly List<(RectTransform root, Image halo, Text label)> _schoolMarks = new List<(RectTransform, Image, Text)>();
        RectTransform _schoolList;
        float _nextListRefresh;

        public AreaMapPanel(Transform canvas)
        {
            BuildFrame(canvas, "AreaMapPanel", "지역 지도", new Vector2(1240, 700));
            BuildMap();
            BuildSide();
            UIFactory.Text(Body, "[M] 또는 [ESC]로 닫기  ·  ▲ 나  ● 찌  ◆ 어군(크기 = 큰/작은 어군)", 14, UIFactory.TextDim, TextAnchor.MiddleLeft)
                .rectTransform.At(new Vector2(0, 1), new Vector2(4, -(MapH + 20)), new Vector2(MapW, 24), new Vector2(0, 1));
        }

        static Vector2 ToMap(float x, float z) => new Vector2((x - ViewMinX) * Scale, (z - ViewMinZ) * Scale);

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
            var frame = UIFactory.Image(Body, new Color(0.02f, 0.04f, 0.06f, 1f), null, "MapFrame").rectTransform
                .At(new Vector2(0, 1), Vector2.zero, new Vector2(MapW + 8, MapH + 8), new Vector2(0, 1));
            _map = UIFactory.Node("Map", frame).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapW, MapH));
            _map.gameObject.AddComponent<RectMask2D>();

            // 둔치(땅), 산책로, 강
            UIFactory.Image(_map, new Color(0.3f, 0.36f, 0.24f), null, "Land").rectTransform.Fill();
            foreach (float z in new[] { -75f, 75f })
                Place(UIFactory.Image(_map, new Color(0.55f, 0.52f, 0.45f, 0.6f), null, "Path").rectTransform, 0f, z,
                    new Vector2(MapW, 3f * Scale));
            Place(UIFactory.Image(_map, new Color(0.1f, 0.22f, 0.33f), null, "River").rectTransform, 0f, 0f,
                new Vector2(MapW, (HanRiverMap.RiverMaxZ - HanRiverMap.RiverMinZ) * Scale));

            // 격자 칸: 서식지 색 + 칸 이름 + 서식지 이름
            for (int r = 0; r < HanRiverMap.Rows; r++)
            for (int c = 0; c < HanRiverMap.Cols; c++)
            {
                var center = HanRiverMap.CellCenter(c, r);
                var h = HanRiverMap.HabitatAt(c, r);
                var cell = Place(UIFactory.Image(_map, HabitatColor(h), null, "Cell").rectTransform, center.x, center.z,
                    new Vector2(HanRiverMap.CellW * Scale - 3f, HanRiverMap.CellH * Scale - 3f));
                UIFactory.Text(cell, HanRiverMap.CellName(c, r), 15, new Color(1, 1, 1, 0.85f), TextAnchor.UpperLeft, FontStyle.Bold)
                    .rectTransform.Fill(6, 6, 4, 4);
                UIFactory.Text(cell, Names.Of(h), 12, new Color(1, 1, 1, 0.45f), TextAnchor.LowerRight).rectTransform.Fill(6, 6, 4, 4);
            }

            // 열/행 머리글
            for (int c = 0; c < HanRiverMap.Cols; c++)
            {
                float x = HanRiverMap.GridMinX + (c + 0.5f) * HanRiverMap.CellW;
                Place(UIFactory.Text(_map, ((char)('A' + c)).ToString(), 16, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    x, HanRiverMap.RiverMaxZ + 4f, new Vector2(40, 22));
            }

            // 다리, 교각, 잔교, 낚시점
            Place(UIFactory.Image(_map, new Color(0.82f, 0.82f, 0.8f), null, "Bridge").rectTransform, HanRiverMap.BridgeX, 0f,
                new Vector2(4f * Scale, MapH));
            Place(UIFactory.Text(_map, "동작대교", 13, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter).rectTransform,
                HanRiverMap.BridgeX, -82f, new Vector2(90, 20));
            var pier = HanRiverMap.Pier;
            Place(UIFactory.Image(_map, new Color(0.72f, 0.52f, 0.3f), null, "Pier").rectTransform, pier.center.x, pier.center.y,
                new Vector2(pier.width * Scale, pier.height * Scale));
            Place(UIFactory.Text(_map, "낚시 데크", 12, new Color(1, 0.85f, 0.6f, 0.9f), TextAnchor.MiddleLeft).rectTransform,
                pier.xMax + 16f, HanRiverMap.RiverMinZ - 5f, new Vector2(80, 18));
            Place(UIFactory.Image(_map, UIFactory.Gold, UIFactory.Circle, "Shop").rectTransform,
                HanRiverMap.ShopPos.x, HanRiverMap.ShopPos.z, new Vector2(14, 14));
            Place(UIFactory.Text(_map, "낚시점", 13, UIFactory.Gold, TextAnchor.MiddleLeft).rectTransform,
                HanRiverMap.ShopPos.x + 16f, HanRiverMap.ShopPos.z, new Vector2(70, 20));
            Place(UIFactory.Text(_map, "N ▲", 14, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                ViewMaxX - 10f, ViewMaxZ - 8f, new Vector2(50, 24));

            // 움직이는 표시: 찌가 있는 칸, 찌, 플레이어
            _cellHighlight = Place(UIFactory.Image(_map, new Color(1f, 0.9f, 0.4f, 0.22f), null, "CellHighlight").rectTransform, 0, 0,
                new Vector2(HanRiverMap.CellW * Scale, HanRiverMap.CellH * Scale));
            _bobber = Place(UIFactory.Image(_map, new Color(1f, 0.3f, 0.2f), UIFactory.Circle, "Bobber").rectTransform, 0, 0, new Vector2(12, 12));
            _player = Place(UIFactory.Text(_map, "▲", 26, new Color(0.45f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, "Player").rectTransform,
                0, 0, new Vector2(30, 30));
        }

        void BuildSide()
        {
            var side = UIFactory.Image(Body, new Color(0.08f, 0.12f, 0.16f, 0.95f), null, "Side").rectTransform
                .At(new Vector2(1, 1), Vector2.zero, new Vector2(310, 600), new Vector2(1, 1));

            var title = UIFactory.Text(side, HanRiverMap.MapName, 20, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
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
                UIFactory.Image(side, col, null, "Swatch").rectTransform.At(new Vector2(0, 1), new Vector2(x, yy), new Vector2(18, 18), new Vector2(0, 1));
                UIFactory.Text(side, Names.Of(habitats[i]), 14, UIFactory.TextMain, TextAnchor.MiddleLeft).rectTransform
                    .At(new Vector2(0, 1), new Vector2(x + 24, yy + 1), new Vector2(110, 20), new Vector2(0, 1));
            }
            y -= 58f;

            UIFactory.Text(side, "어군", 13, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, y), new Vector2(200, 18), new Vector2(0, 1));
            _schoolList = UIFactory.Node("SchoolList", side).At(new Vector2(0, 1), new Vector2(14, y - 22f), new Vector2(286, 220), new Vector2(0, 1));

            var tip = UIFactory.Text(side,
                "어군이 있는 칸에 찌를 던지면 입질이 빠르고 그 어군의 고기가 잘 낚입니다.\n" +
                $"큰 어군에서 {FishSchoolManager.CatchesToShrink}마리를 낚으면 작은 어군으로 흩어지고, " +
                $"{FishSchoolManager.RegroupMinutes / 60f:0}시간 뒤 다시 모입니다. 어군은 2시간마다 옆 칸으로 움직여요.",
                13, UIFactory.TextDim, TextAnchor.LowerLeft);
            tip.rectTransform.At(new Vector2(0, 0), new Vector2(14, 12), new Vector2(286, 110), new Vector2(0, 0));
        }

        public override void Refresh()
        {
            _nextListRefresh = 0f;
            Tick();
        }

        public void Tick()
        {
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

            bool inCell = HanRiverMap.TryCell(lookPos, out int col, out int row);
            _cellHighlight.gameObject.SetActive(inCell);
            if (inCell)
            {
                var c = HanRiverMap.CellCenter(col, row);
                _cellHighlight.anchoredPosition = ToMap(c.x, c.z);
                var school = Game.Schools.At(col, row);
                _cellInfo.text = $"{(showBobber ? "찌 위치" : "현재 위치")} : {HanRiverMap.CellName(col, row)} · {Names.Of(HanRiverMap.HabitatAt(col, row))}" +
                                 (school != null ? $"\n{school.Name} ({school.SizeLabel})" : "");
            }
            else
            {
                _cellInfo.text = $"현재 위치 : {HanRiverMap.AreaName(playerPos)}";
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
                UIFactory.Image(root, new Color(0f, 0f, 0f, 0.6f), null, "LabelBg").rectTransform
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
                var c = HanRiverMap.CellCenter(s.Col, s.Row);
                var offset = new Vector2((i % 2 == 0 ? -1f : 1f) * 22f, (i / 2 == 0 ? 1f : -1f) * 6f);
                root.anchoredPosition = ToMap(c.x, c.z) + offset;
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f + i) * 0.08f;
                halo.rectTransform.localScale = Vector3.one * (s.Big ? 1f : 0.55f) * pulse;
                root.GetChild(1).localScale = Vector3.one * (s.Big ? 1.2f : 0.75f);
                label.text = $"{s.Name} · {(s.Big ? "大" : "小")}";
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
            for (int i = 0; i < schools.Count; i++)
            {
                var s = schools[i];
                string state;
                if (s.Big) state = $"<color=#ffd060>큰 어군</color>  남은 {s.CatchesLeft}마리";
                else
                {
                    float left = Mathf.Max(0f, FishSchoolManager.RegroupMinutes - (Game.Clock.TotalMinutes - s.ShrankAt)) / 60f;
                    state = $"<color=#9ab0c0>작은 어군</color>  {Mathf.CeilToInt(left)}시간 뒤 모임";
                }
                var names = new List<string>();
                foreach (var id in s.SpeciesIds)
                {
                    var sp = FishDatabase.Get(id);
                    if (sp != null) names.Add(sp.Name);
                }
                var t = UIFactory.Text(_schoolList,
                    $"<b>{s.Name}</b>  <color=#80c8ff>{HanRiverMap.CellName(s.Col, s.Row)}</color>\n{state}\n<size=11><color=#8a96a0>{string.Join(", ", names)}</color></size>",
                    14, UIFactory.TextMain, TextAnchor.UpperLeft);
                t.rectTransform.At(new Vector2(0, 1), new Vector2(0, -i * 56f), new Vector2(286, 54), new Vector2(0, 1));
            }
        }
    }
}
