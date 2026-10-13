using System.Linq;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 어류 도감. 원작처럼 같은 종을 여러 번 잡아야 정보가 열린다.
    /// 1마리: 이름·성질·기록 / 3마리: 선호 미끼 / 5마리: 활동 시간·서식지
    /// 오른쪽 칸: 이 낚시터 수집률(도넛), 등급별 수집(도넛), 많이 낚은 어종 TOP 5(막대).
    /// 카드에는 물고기 아이콘(Icons/Fish, 못 낚은 어종은 검은 실루엣). 낚은 어종 카드를 누르면
    /// 3D 물고기가 천천히 헤엄치며 도는 미리보기(<see cref="ResultFishStage"/>)와 자세한 기록이 나온다.
    /// </summary>
    public class DexPanel : UIPanel
    {
        public const int BaitUnlock = 3;
        public const int HabitatUnlock = 5;

        const float SideW = 340f;
        const float FrameW = 1760f, FrameH = 860f;

        string _tab;
        RectTransform _grid;
        ChartHost _rateChart, _rarityChart, _topChart;
        TMP_Text _rateText, _topEmpty;
        // 3D 미리보기
        RectTransform _detail;
        UnityEngine.UI.RawImage _detailImg;
        TMP_Text _detailText;
        ResultFishStage _stage;
        FishSpecies _selected;
        float _openedAt;
        static readonly Vector3 StagePos = new Vector3(0f, -800f, 400f);

        static readonly string[] RarityNames = { "", "★ 흔함", "★★ 보통", "★★★ 귀함", "★★★★ 희귀", "★★★★★ 전설" };
        static readonly Color[] RarityColors =
        {
            Color.gray,
            new Color(0.62f, 0.69f, 0.76f),
            new Color(0.45f, 0.85f, 0.4f),
            new Color(0.42f, 0.66f, 1f),
            new Color(0.7f, 0.52f, 1f),
            new Color(1f, 0.78f, 0.32f),
        };

        public DexPanel(Transform canvas)
        {
            BuildFrame(canvas, "DexPanel", "어류 도감", new Vector2(FrameW, FrameH));
            _grid = UIFactory.Node("Grid", Body).Fill(0, SideW + 20, 0, 0);
            BuildSide();
            BuildDetail();
        }

        void BuildDetail()
        {
            _detail = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "Detail", 0f).rectTransform;
            _detail.Fill(0, SideW + 20, 0, 0);
            var well = UIFactory.Shape(_detail, new Color(0.04f, 0.09f, 0.13f, 0.75f), UIFactory.RadiusCard, "Stage", 0f).rectTransform;
            well.At(new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(820, 520), new Vector2(0, 0.5f));
            _detailImg = UIFactory.Node("Fish3D", well).Fill().gameObject.AddComponent<UnityEngine.UI.RawImage>();
            _detailImg.raycastTarget = false;
            _detailText = UIFactory.Text(_detail, "", 18, UIFactory.TextMain, TextAnchor.UpperLeft);
            _detailText.rectTransform.At(new Vector2(0, 1), new Vector2(860, -24), new Vector2(480, 640), new Vector2(0, 1));
            _detailText.lineSpacing = 8f;
            var back = UIFactory.Button(_detail, "◀ 목록으로", () => ShowDetail(null), UIFactory.BtnNeutral, 18);
            UIFactory.SetRadius(back.targetGraphic, UIFactory.Pill);
            ((RectTransform)back.transform).At(new Vector2(0, 0), new Vector2(860, 22), new Vector2(180, 44), Vector2.zero);
            _detail.gameObject.SetActive(false);
        }

        /// <summary>어종 미리보기를 연다 (null이면 목록으로).</summary>
        public void ShowDetail(FishSpecies s)
        {
            _selected = s;
            _grid.gameObject.SetActive(s == null);
            _detail.gameObject.SetActive(s != null);
            if (s == null)
            {
                if (_stage != null) _stage.Hide();
                return;
            }
            if (_stage == null) _stage = ResultFishStage.Create(StagePos, "DexFishStage");
            _detailImg.texture = _stage.Target;
            var e = Game.Profile.Dex(s.Id);
            float len = e != null ? e.maxLength : (s.MinLen + s.MaxLen) / 2f;
            if (_stage.Show(s, len)) Fishing.Angling.FishModels.Play(_stage.Fish, Fishing.Angling.FishModels.SwimSlow, 0.8f, 0f);
            _openedAt = Time.unscaledTime;
            _stage.Spin(0f);
            string Lock(int need, string text) => e != null && e.count >= need ? text : $"<color=#607080>{need}마리 낚으면 공개</color>";
            _detailText.text =
                $"<size=34><b>{s.Name}</b></size>  <color=#ffd060>{new string('★', s.Rarity)}</color>\n" +
                $"<i><color=#8fa3b5>{s.Latin}</color></i>\n" +
                $"<color=#a0b0c0>{s.Temper}</color>\n\n" +
                (e != null
                    ? $"낚은 수   <b>{e.count}</b>마리\n최대 기록   <b>{e.maxLength:0.0}cm</b> · {e.maxWeight / 1000f:0.00}kg" +
                      (s.IsTrophy(e.maxLength) ? $"  <color=#ff7090>{s.TrophyName}</color>" : "") + $"\n처음 낚은 날   {e.firstDay}일째\n"
                    : "<color=#607080>아직 낚지 못했습니다</color>\n") +
                $"길이   {s.MinLen:0}~{s.MaxLen:0}cm  ·  {s.TrophyName} {s.TrophyLen:0.#}cm 이상\n\n" +
                $"미끼   {Lock(BaitUnlock, string.Join(", ", s.Baits.Select(Names.Of)))}\n" +
                $"시간   {Lock(HabitatUnlock, string.Join(", ", s.Times.Select(Names.Of)))}\n" +
                $"장소   {Lock(HabitatUnlock, string.Join(", ", s.Habitats.Select(Names.Of)))}\n" +
                $"날씨   {Lock(HabitatUnlock, string.Join(", ", (s.Weathers ?? new Weather[0]).Select(Names.Of)))}";
        }

        /// <summary>미리보기 물고기를 돌린다 (GameUI.Update에서).</summary>
        public void Tick()
        {
            if (_selected != null && _stage != null) _stage.Spin(Time.unscaledTime - _openedAt);
        }

        public override void Close()
        {
            ShowDetail(null);
            base.Close();
        }

        /// <summary>오른쪽 통계 칸. 차트는 한 번만 만들고 Refresh에서 값만 바꾼다.</summary>
        void BuildSide()
        {
            var side = UIFactory.Node("Side", Body);
            side.anchorMin = new Vector2(1, 0);
            side.anchorMax = new Vector2(1, 1);
            side.pivot = new Vector2(1, 0.5f);
            side.anchoredPosition = Vector2.zero;
            side.sizeDelta = new Vector2(SideW, 0);

            // 수집률
            var rate = UIFactory.Shape(side, UIFactory.Surface, UIFactory.RadiusCard, "Rate", 0f).rectTransform
                .At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(SideW, 236));
            Header(rate, "수집률");
            _rateChart = UIChart.Donut(rate, new Vector2(190, 190), new[] { "발견", "미발견" }, new[] { 1f, 1f },
                new[] { UIFactory.Accent, new Color(1f, 1f, 1f, 0.1f) }, 0.72f, false);
            ((RectTransform)_rateChart.transform).At(new Vector2(0, 0), new Vector2(12, 8), new Vector2(190, 190), Vector2.zero);
            _rateText = UIFactory.Text(rate, "", 30, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _rateText.rectTransform.At(new Vector2(0, 0), new Vector2(107, 103), new Vector2(150, 70), new Vector2(0.5f, 0.5f));

            // 등급별 수집 (도넛, 범례에 종 수)
            var rarity = UIFactory.Shape(side, UIFactory.Surface, UIFactory.RadiusCard, "Rarity", 0f).rectTransform
                .At(new Vector2(0.5f, 1), new Vector2(0, -248), new Vector2(SideW, 220));
            Header(rarity, "등급별 발견");
            _rarityChart = UIChart.Donut(rarity, new Vector2(SideW - 16, 176), new[] { "-" }, new[] { 1f }, new[] { RarityColors[1] }, 0.55f, true, 13);
            ((RectTransform)_rarityChart.transform).At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(SideW - 16, 176), new Vector2(0.5f, 0));

            // 많이 낚은 어종 TOP 5
            var top = UIFactory.Shape(side, UIFactory.Surface, UIFactory.RadiusCard, "Top", 0f).rectTransform;
            top.anchorMin = new Vector2(0, 0);
            top.anchorMax = new Vector2(1, 1);
            top.offsetMin = Vector2.zero;
            top.offsetMax = new Vector2(0, -480);
            Header(top, "많이 낚은 어종 TOP 5");
            _topChart = UIChart.Bars(top, new Vector2(SideW - 16, 200), new[] { "-" }, new[] { 0f }, UIFactory.Accent, null, "0", 0, 12, true, 26f);
            ((RectTransform)_topChart.transform).At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(SideW - 16, 200), new Vector2(0.5f, 0));
            _topEmpty = UIFactory.Text(top, "아직 기록이 없습니다", 15, UIFactory.TextDim, TextAnchor.MiddleCenter);
            _topEmpty.rectTransform.Fill(0, 0, 40, 0);
        }

        static void Header(RectTransform card, string text)
        {
            var t = UIFactory.Text(card, text, 15, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(300, 24));
        }

        public override void Open()
        {
            _tab = Fishing.World.FieldMap.Id;
            base.Open();
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            _tab ??= Fishing.World.FieldMap.Id;
            foreach (Transform child in Frame)
                if (child.name == "DexTab") Object.Destroy(child.gameObject);
            var species = FishDatabase.DexOf(_tab).ToList();
            int known = species.Count(s => p.Dex(s.Id) != null);
            Title.text = $"어류 도감 · {Fishing.World.Maps.Get(_tab).ShortName}  <size=20><color=#8fa3b5>{known}/{species.Count}</color></size>";
            UIFactory.ClearChildren(_grid);

            // 낚시터 탭 (제목 줄 오른쪽, 닫기 버튼 옆)
            var tabs = Fishing.World.Maps.All;
            for (int t = 0; t < tabs.Length; t++)
            {
                var map = tabs[t];
                bool sel = map.Id == _tab;
                var b = UIFactory.Button(Frame, map.ShortName, () => { _tab = map.Id; Refresh(); }, sel ? UIFactory.TabOn : UIFactory.TabOff, 16);
                UIFactory.SetRadius(b.targetGraphic, UIFactory.Pill);
                ((RectTransform)b.transform).At(new Vector2(1, 1), new Vector2(-74 - (tabs.Length - 1 - t) * 134, -18), new Vector2(126, 36));
                b.gameObject.name = "DexTab";
            }

            BuildCards(p, species);
            if (_selected != null) ShowDetail(_selected);
            RefreshSide(p, species, known);
        }

        void BuildCards(PlayerProfile p, System.Collections.Generic.List<FishSpecies> species)
        {
            // 칸 수에 맞춰 5열 또는 6열, 높이는 창에 꽉 차게
            int cols = species.Count > 15 ? 6 : 5;
            int rows = Mathf.CeilToInt(species.Count / (float)cols);
            const float gap = 10f;
            float areaW = FrameW - 56f - SideW - 20f, areaH = FrameH - 106f;
            float w = (areaW - gap * (cols - 1)) / cols;
            float h = Mathf.Min(250f, (areaH - gap * (rows - 1)) / rows);
            int body = cols == 6 ? 13 : 14;

            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                var entry = p.Dex(s.Id);
                int col = i % cols, row = i / cols;
                var cardImg = UIFactory.Shape(_grid, entry != null ? UIFactory.SurfaceHi : UIFactory.Surface, UIFactory.RadiusCard, "Card", 0f);
                if (entry != null)
                {
                    var btn = cardImg.gameObject.AddComponent<UnityEngine.UI.Button>();
                    btn.transition = UnityEngine.UI.Selectable.Transition.None;
                    var picked = s;
                    btn.onClick.AddListener(() => ShowDetail(picked));
                }
                if (entry != null && s.IsTrophy(entry.maxLength)) UIFactory.AddBorder(cardImg, new Color(1f, 0.45f, 0.6f, 0.55f), 1.5f);
                var card = cardImg.rectTransform;
                card.At(new Vector2(0, 1), new Vector2(col * (w + gap), -row * (h + gap)), new Vector2(w, h));

                // 왼쪽 위 번호, 오른쪽 위 등급 점
                Cell(card, $"No.{i + 1:00}", 12, 70, 12, UIFactory.TextFaint, TextAnchor.UpperLeft).rectTransform.offsetMax = new Vector2(82, -10);
                UIFactory.Shape(card, RarityColors[Mathf.Clamp(s.Rarity, 0, 5)], UIFactory.Pill, "RarityDot", 0f).rectTransform
                    .At(new Vector2(1, 1), new Vector2(-14, -14), new Vector2(8, 8));
                // 물고기 아이콘 (못 낚은 어종은 검은 실루엣)
                var icon = Fishing.Angling.FishModels.Icon(s);
                if (icon != null)
                {
                    var img = UIFactory.Image(card, entry != null ? Color.white : new Color(0f, 0f, 0f, 0.55f), icon, "Icon");
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    if (entry != null) img.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(w - 24, 78), new Vector2(0.5f, 1));
                    else img.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(w - 20, h * 0.55f));
                }
                if (entry == null)
                {
                    var q = UIFactory.Text(card, "???", 30, new Color(1, 1, 1, 0.22f), TextAnchor.LowerCenter, FontStyle.Bold);
                    q.rectTransform.Fill(0, 0, 0, 32);
                    var rarity = UIFactory.Text(card, new string('★', s.Rarity), 13, new Color(1, 0.8f, 0.3f, 0.35f), TextAnchor.LowerCenter);
                    rarity.rectTransform.Fill(0, 0, 0, 12);
                    continue;
                }

                bool trophy = s.IsTrophy(entry.maxLength);
                var lines = $"<b><size={body + 7}>{s.Name}</size></b>  <color=#ffd060><size={body - 2}>{new string('★', s.Rarity)}</size></color>\n" +
                            $"<size={body - 3}><i><color=#8fa3b5>{s.Latin}</color></i></size>\n" +
                            $"<color=#a0b0c0>{s.Temper}</color>\n" +
                            $"낚은 수  {entry.count}마리\n" +
                            $"최대  {entry.maxLength:0.0}cm" + (trophy ? $" <color=#ff7090>{s.TrophyName}</color>" : "") + "\n" +
                            $"길이  {s.MinLen:0}~{s.MaxLen:0}cm\n";
                lines += entry.count >= BaitUnlock
                    ? $"미끼  {string.Join(", ", s.Baits.Select(Names.Of))}\n"
                    : $"<color=#607080>미끼  {BaitUnlock}마리 낚으면 공개</color>\n";
                lines += entry.count >= HabitatUnlock
                    ? $"시간  {string.Join(", ", s.Times.Select(Names.Of))}\n장소  {string.Join(", ", s.Habitats.Select(Names.Of))}"
                    : $"<color=#607080>시간/장소  {HabitatUnlock}마리 낚으면 공개</color>";

                var text = UIFactory.Text(card, lines, body, UIFactory.TextMain, TextAnchor.UpperLeft);
                text.rectTransform.Fill(14, 12, icon != null ? 80 : 28, 8);
                text.lineSpacing = 2f;
            }
        }

        void RefreshSide(PlayerProfile p, System.Collections.Generic.List<FishSpecies> species, int known)
        {
            // 수집률 도넛 + 가운데 숫자
            int total = Mathf.Max(1, species.Count);
            UIChart.SetSlices(_rateChart.Serie(), new[] { "발견", "미발견" },
                new[] { Mathf.Max(0.001f, known), Mathf.Max(0.001f, total - known) },
                new[] { UIFactory.Accent, new Color(1f, 1f, 1f, 0.1f) });
            _rateChart.Refresh();
            _rateText.text = $"{known * 100f / total:0}%\n<size=14><color=#8fa3b5>{known} / {species.Count}종</color></size>";

            // 등급별: 발견한 종 수를 등급 색으로
            var names = new System.Collections.Generic.List<string>();
            var values = new System.Collections.Generic.List<float>();
            var colors = new System.Collections.Generic.List<Color>();
            for (int r = 1; r <= 5; r++)
            {
                int all = species.Count(s => s.Rarity == r);
                if (all == 0) continue;
                int found = species.Count(s => s.Rarity == r && p.Dex(s.Id) != null);
                names.Add($"{RarityNames[r]}  {found}/{all}");
                values.Add(Mathf.Max(0.001f, found));
                colors.Add(found > 0 ? RarityColors[r] : new Color(RarityColors[r].r, RarityColors[r].g, RarityColors[r].b, 0.2f));
            }
            UIChart.SetSlices(_rarityChart.Serie(), names, values, colors);
            _rarityChart.Refresh();

            // 많이 낚은 어종 TOP 5
            var top = species.Select(s => (s, e: p.Dex(s.Id))).Where(x => x.e != null && x.e.count > 0)
                .OrderByDescending(x => x.e.count).Take(5).ToList();
            _topEmpty.gameObject.SetActive(top.Count == 0);
            _topChart.gameObject.SetActive(top.Count > 0);
            if (top.Count > 0)
            {
                UIChart.SetCategories(_topChart.Profile, top.Select(x => x.s.Name).ToList());
                UIChart.SetValues(_topChart.Serie(), top.Select(x => (float)x.e.count).ToList());
                _topChart.Refresh();
            }
        }
    }
}
