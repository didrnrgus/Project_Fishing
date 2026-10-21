using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 선·막대·도넛·레이더 차트를 코드로 만든다. UGUI 메쉬(<see cref="ChartGraphic"/>)와 TMP 글자로 직접 그려서
    /// 창 순서·숨김·마스크가 다른 UI와 똑같이 동작한다 (2026-10-21까지는 EasyChart — 에셋 스토어라 공개 저장소에 둘 수 없어 바꿨다).
    /// 배경은 투명이라 유리 판 위에 그대로 얹는다. 데이터를 바꾼 뒤에는 <see cref="ChartHost.Refresh"/> (0.45초 동안 새 값으로 바뀐다).
    /// </summary>
    public static class UIChart
    {
        public static readonly Color[] Palette =
        {
            new Color(0.36f, 0.88f, 0.8f),   // 청록 (Accent)
            new Color(1f, 0.78f, 0.32f),     // 금색
            new Color(0.42f, 0.66f, 1f),     // 파랑
            new Color(1f, 0.47f, 0.6f),      // 분홍
            new Color(0.7f, 0.52f, 1f),      // 보라
            new Color(0.55f, 0.9f, 0.45f),   // 초록
            new Color(1f, 0.6f, 0.32f),      // 주황
            new Color(0.62f, 0.69f, 0.76f),  // 회색 (기타)
        };

        internal static readonly Color AxisLabel = new Color(0.78f, 0.84f, 0.9f, 0.9f);
        internal static readonly Color GridLine = new Color(1f, 1f, 1f, 0.1f);
        internal static readonly Color AxisLine = new Color(1f, 1f, 1f, 0.25f);

        public struct Series
        {
            public string Name;
            public float[] Values;
            public Color Color;
            public Series(string name, float[] values, Color color) { Name = name; Values = values; Color = color; }
        }

        static ChartHost Create(Transform parent, ChartProfile profile, Vector2 size, string name)
        {
            var rt = UIFactory.Node(name, parent);
            rt.sizeDelta = size;
            var host = rt.gameObject.AddComponent<ChartHost>();
            host.Init(profile);
            return host;
        }

        static ChartSeries NewSeries(string name, Color color, IList<float> values)
        {
            var s = new ChartSeries { name = name, color = color };
            if (values != null) s.values.AddRange(values);
            return s;
        }

        /// <summary>꺾은선 (부드러운 선 + 옅은 면). yMax를 주면 세로축 범위를 고정한다.</summary>
        public static ChartHost Line(Transform parent, Vector2 size, string[] categories, Series[] series, float? yMax = null,
            string yFormat = "0", int fontSize = 12, bool points = false, bool legend = false)
        {
            var p = new ChartProfile { kind = ChartKind.Line, yMax = yMax, yFormat = yFormat, fontSize = fontSize, points = points, legend = legend };
            p.categories.AddRange(categories);
            foreach (var s in series) p.series.Add(NewSeries(s.Name, s.Color, s.Values));
            if (legend) p.padding.y = 34;
            return Create(parent, p, size, "LineChart");
        }

        /// <summary>세로 막대. 막대 위에 값 표시(decimals 자릿수).</summary>
        public static ChartHost Bars(Transform parent, Vector2 size, string[] categories, float[] values, Color color, float? yMax = null,
            string yFormat = "0", int decimals = 0, int fontSize = 12, bool valueLabels = true, float barWidth = 22f)
        {
            var p = new ChartProfile
            {
                kind = ChartKind.Bars, yMax = yMax, yFormat = yFormat, fontSize = fontSize,
                valueLabels = valueLabels, decimals = decimals, barWidth = barWidth,
            };
            p.categories.AddRange(categories);
            p.series.Add(NewSeries("값", color, values));
            return Create(parent, p, size, "BarChart");
        }

        /// <summary>도넛. legend면 오른쪽에 범례(이름 — 값은 이름에 직접 넣는다). 값이 0 이하인 조각은 빠진다.</summary>
        public static ChartHost Donut(Transform parent, Vector2 size, string[] names, float[] values, Color[] colors,
            float inner = 0.62f, bool legend = true, int fontSize = 13)
        {
            var p = new ChartProfile { kind = ChartKind.Donut, inner = inner, legend = legend, fontSize = fontSize, padding = new Vector4(0, 6, 0, 6) };
            var s = new ChartSeries { name = "구성" };
            p.series.Add(s);
            SetSlices(s, names, values, colors);
            return Create(parent, p, size, "DonutChart");
        }

        /// <summary>레이더. 값은 0~100으로 정규화해서 넣는다(축마다 최대가 달라도 모양을 비교할 수 있게).</summary>
        public static ChartHost Radar(Transform parent, Vector2 size, string[] indicators, Series[] series, int fontSize = 13)
        {
            var p = new ChartProfile { kind = ChartKind.Radar, yMax = 100f, fontSize = fontSize, padding = new Vector4(8, 8, 8, 8) };
            p.categories.AddRange(indicators);
            foreach (var s in series) p.series.Add(NewSeries(s.Name, s.Color, s.Values));
            return Create(parent, p, size, "RadarChart");
        }

        // ---------------- 데이터 갱신 ----------------

        public static void SetValues(ChartSeries serie, IList<float> values)
        {
            serie.values.Clear();
            serie.values.AddRange(values);
        }

        public static void SetSlices(ChartSeries serie, IList<string> names, IList<float> values, IList<Color> colors)
        {
            serie.values.Clear();
            serie.names.Clear();
            serie.colors.Clear();
            for (int i = 0; i < values.Count; i++)
            {
                serie.values.Add(values[i]);
                serie.names.Add(names[i]);
                serie.colors.Add(colors != null && i < colors.Count ? colors[i] : Palette[i % Palette.Length]);
            }
        }

        public static void SetCategories(ChartProfile p, IList<string> categories)
        {
            p.categories.Clear();
            p.categories.AddRange(categories);
        }
    }

    public enum ChartKind { Line, Bars, Donut, Radar }

    /// <summary>계열 하나. 도넛은 조각마다 이름·색이 있다.</summary>
    public class ChartSeries
    {
        public string name;
        public Color color;
        public readonly List<float> values = new List<float>();
        public readonly List<string> names = new List<string>();
        public readonly List<Color> colors = new List<Color>();
    }

    /// <summary>차트 설정과 데이터.</summary>
    public class ChartProfile
    {
        public ChartKind kind;
        public readonly List<string> categories = new List<string>();
        public readonly List<ChartSeries> series = new List<ChartSeries>();
        public float? yMax;
        public string yFormat = "0";
        public int fontSize = 12;
        public bool points, legend, valueLabels;
        public int decimals;
        public float barWidth = 22f;
        public float inner = 0.62f;
        /// <summary>그림 둘레 여백: x 왼쪽(세로축 글자), y 위, z 오른쪽, w 아래(가로축 글자).</summary>
        public Vector4 padding = new Vector4(46, 14, 14, 30);
    }

    /// <summary>
    /// 차트 하나. 그림은 자식 <see cref="ChartGraphic"/>, 글자는 자식 TMP. 크기가 바뀌거나 Refresh하면 다시 그린다.
    /// </summary>
    public class ChartHost : MonoBehaviour
    {
        const float AnimTime = 0.45f;

        public ChartProfile Profile { get; private set; }

        ChartGraphic _graphic;
        RectTransform _labels;
        readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
        int _textUsed;
        float _anim = 1f;
        readonly List<List<float>> _from = new List<List<float>>();
        Vector2 _lastSize;
        bool _dirty = true;

        RectTransform Rt => (RectTransform)transform;

        public void Init(ChartProfile profile)
        {
            Profile = profile;
            var g = UIFactory.Node("Plot", transform);
            g.anchorMin = Vector2.zero;
            g.anchorMax = Vector2.one;
            g.offsetMin = g.offsetMax = Vector2.zero;
            _graphic = g.gameObject.AddComponent<ChartGraphic>();
            _graphic.Host = this;
            _graphic.raycastTarget = false;
            _labels = UIFactory.Node("Labels", transform);
            _labels.anchorMin = Vector2.zero;
            _labels.anchorMax = Vector2.one;
            _labels.offsetMin = _labels.offsetMax = Vector2.zero;
            Refresh();
        }

        public ChartSeries Serie(int index = 0) => Profile.series[index];

        /// <summary>Profile의 데이터를 바꾼 뒤 부른다. 지금 보이는 값에서 새 값으로 0.45초에 걸쳐 바뀐다.</summary>
        public void Refresh()
        {
            var shown = new List<List<float>>();
            for (int s = 0; s < Profile.series.Count; s++)
            {
                var row = new List<float>();
                int n = Profile.series[s].values.Count;
                for (int i = 0; i < n; i++) row.Add(_from.Count > 0 || _anim < 1f ? Shown(s, i) : 0f);
                shown.Add(row);
            }
            _from.Clear();
            _from.AddRange(shown);
            _anim = 0f;
            _dirty = true;
        }

        /// <summary>그리는 값 (Refresh 직후에는 이전 값에서 새 값으로 가는 중).</summary>
        internal float Shown(int s, int i)
        {
            float target = Profile.series[s].values[i];
            float from = s < _from.Count && i < _from[s].Count ? _from[s][i] : 0f;
            float t = 1f - (1f - _anim) * (1f - _anim) * (1f - _anim);
            return Mathf.Lerp(from, target, t);
        }

        void Update()
        {
            var size = Rt.rect.size;
            if (size != _lastSize)
            {
                _lastSize = size;
                _dirty = true;
            }
            if (_anim < 1f)
            {
                _anim = Mathf.MoveTowards(_anim, 1f, Time.unscaledDeltaTime / AnimTime);
                _graphic.SetVerticesDirty();
            }
            if (_dirty)
            {
                _dirty = false;
                RebuildLabels();
                _graphic.SetVerticesDirty();
            }
        }

        // ---------------- 배치 (왼쪽 아래가 원점인 차트 좌표) ----------------

        internal Rect PlotRect
        {
            get
            {
                var size = Rt.rect.size;
                var p = Profile.padding;
                return new Rect(p.x, p.w, Mathf.Max(1f, size.x - p.x - p.z), Mathf.Max(1f, size.y - p.y - p.w));
            }
        }

        /// <summary>세로축 최대: 고정값이 없으면 4칸에 맞는 깔끔한 수(1·2·2.5·5·10 × 10ⁿ).</summary>
        internal float YMax
        {
            get
            {
                if (Profile.yMax.HasValue) return Mathf.Max(1e-4f, Profile.yMax.Value);
                float max = 0f;
                foreach (var s in Profile.series)
                    foreach (var v in s.values) max = Mathf.Max(max, v);
                if (max <= 0f) return 1f;
                float raw = max / 4f;
                float mag = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw)));
                float n = raw / mag;
                float step = (n <= 1f ? 1f : n <= 2f ? 2f : n <= 2.5f ? 2.5f : n <= 5f ? 5f : 10f) * mag;
                return step * 4f;
            }
        }

        internal float CategoryX(int i)
        {
            var r = PlotRect;
            int n = Mathf.Max(1, Profile.categories.Count);
            if (Profile.kind == ChartKind.Bars) return r.xMin + (i + 0.5f) / n * r.width;
            return n == 1 ? r.center.x : r.xMin + i / (float)(n - 1) * r.width;
        }

        /// <summary>도넛 가운데와 바깥 반지름 (범례가 있으면 왼쪽으로 비켜 선다).</summary>
        internal (Vector2 center, float radius) DonutLayout()
        {
            var r = PlotRect;
            var size = Rt.rect.size;
            bool legend = Profile.legend;
            var c = new Vector2(size.x * 0.5f + (legend ? -size.x * 0.24f : 0f), r.center.y);
            float w = legend ? size.x * 0.5f : size.x;
            return (c, Mathf.Max(4f, Mathf.Min(w, r.height) * 0.5f - 6f));
        }

        internal (Vector2 center, float radius) RadarLayout()
        {
            var r = PlotRect;
            return (r.center, Mathf.Max(4f, Mathf.Min(r.width, r.height) * 0.5f - Profile.fontSize * 2.6f));
        }

        internal static Vector2 RadarDir(int i, int n)
        {
            float a = 2f * Mathf.PI * i / Mathf.Max(1, n);
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)); // 위에서 시작해 시계 방향
        }

        // ---------------- 글자 ----------------

        void RebuildLabels()
        {
            _textUsed = 0;
            var p = Profile;
            int fs = p.fontSize;
            var r = PlotRect;
            switch (p.kind)
            {
                case ChartKind.Line:
                case ChartKind.Bars:
                {
                    float ymax = YMax;
                    for (int k = 0; k <= 4; k++)
                    {
                        float y = r.yMin + k / 4f * r.height;
                        Label((ymax * k / 4f).ToString(p.yFormat), new Vector2(r.xMin - 6f, y), new Vector2(1f, 0.5f), fs - 1, UIChart.AxisLabel, TextAlignmentOptions.MidlineRight);
                    }
                    for (int i = 0; i < p.categories.Count; i++)
                        if (!string.IsNullOrEmpty(p.categories[i]))
                            Label(p.categories[i], new Vector2(CategoryX(i), r.yMin - 4f), new Vector2(0.5f, 1f), fs, UIChart.AxisLabel, TextAlignmentOptions.Top);
                    if (p.kind == ChartKind.Bars && p.valueLabels && p.series.Count > 0)
                    {
                        var s = p.series[0];
                        for (int i = 0; i < s.values.Count && i < p.categories.Count; i++)
                        {
                            float y = r.yMin + Mathf.Clamp01(s.values[i] / ymax) * r.height + 2f;
                            Label(s.values[i].ToString("F" + p.decimals), new Vector2(CategoryX(i), y), new Vector2(0.5f, 0f), fs - 1, UIFactory.TextMain, TextAlignmentOptions.Bottom);
                        }
                    }
                    if (p.legend)
                    {
                        float x = r.xMin;
                        foreach (var s in p.series)
                        {
                            var t = Label("● " + s.name, new Vector2(x, Rt.rect.height - 4f), new Vector2(0f, 1f), fs, s.color, TextAlignmentOptions.TopLeft);
                            x += t.preferredWidth + 14f;
                        }
                    }
                    break;
                }
                case ChartKind.Donut:
                {
                    if (!p.legend || p.series.Count == 0) break;
                    var s = p.series[0];
                    var (c, rad) = DonutLayout();
                    var items = new List<int>();
                    for (int i = 0; i < s.values.Count; i++) if (s.values[i] > 0f) items.Add(i);
                    float line = fs + 5f;
                    float top = c.y + (items.Count - 1) * line * 0.5f;
                    float x = c.x + rad + 18f;
                    for (int k = 0; k < items.Count; k++)
                        Label(s.names[items[k]], new Vector2(x + 14f, top - k * line), new Vector2(0f, 0.5f), fs, UIFactory.TextMain, TextAlignmentOptions.MidlineLeft);
                    break;
                }
                case ChartKind.Radar:
                {
                    var (c, rad) = RadarLayout();
                    int n = p.categories.Count;
                    for (int i = 0; i < n; i++)
                    {
                        var d = RadarDir(i, n);
                        var pos = c + d * (rad + fs * 1.3f);
                        var pivot = new Vector2(0.5f - d.x * 0.5f, 0.5f - d.y * 0.5f);
                        Label(p.categories[i], pos, pivot, fs, UIChart.AxisLabel, TextAlignmentOptions.Center);
                    }
                    break;
                }
            }
            for (int i = _textUsed; i < _texts.Count; i++) _texts[i].gameObject.SetActive(false);
        }

        TextMeshProUGUI Label(string text, Vector2 pos, Vector2 pivot, float size, Color color, TextAlignmentOptions align)
        {
            TextMeshProUGUI t;
            if (_textUsed < _texts.Count) t = _texts[_textUsed];
            else
            {
                t = UIFactory.Text(_labels, "", size, color);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                _texts.Add(t);
            }
            _textUsed++;
            t.gameObject.SetActive(true);
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(Mathf.Max(20f, t.GetPreferredValues(text).x + 4f), size * 1.4f);
            rt.anchoredPosition = pos;
            return t;
        }

        // ---------------- 그림 ----------------

        internal void Draw(VertexHelper vh, Rect graphicRect)
        {
            var p = Profile;
            var m = new ChartMesh(vh, graphicRect.min); // 차트 좌표(왼쪽 아래 원점) → 그래픽 로컬 좌표 (그래픽은 차트를 꽉 채운다)
            switch (p.kind)
            {
                case ChartKind.Line: DrawLine(m); break;
                case ChartKind.Bars: DrawBars(m); break;
                case ChartKind.Donut: DrawDonut(m); break;
                case ChartKind.Radar: DrawRadar(m); break;
            }
        }

        void DrawGrid(ChartMesh m)
        {
            var r = PlotRect;
            for (int k = 1; k <= 4; k++)
                m.Dashed(new Vector2(r.xMin, r.yMin + k / 4f * r.height), new Vector2(r.xMax, r.yMin + k / 4f * r.height), 1f, 4f, 3f, UIChart.GridLine);
            m.Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), 1f, UIChart.AxisLine);
        }

        void DrawLine(ChartMesh m)
        {
            DrawGrid(m);
            var r = PlotRect;
            float ymax = YMax;
            for (int s = 0; s < Profile.series.Count; s++)
            {
                var serie = Profile.series[s];
                int n = Mathf.Min(serie.values.Count, Mathf.Max(1, Profile.categories.Count));
                if (n == 0) continue;
                var pts = new List<Vector2>();
                for (int i = 0; i < n; i++)
                    pts.Add(new Vector2(CategoryX(i), r.yMin + Mathf.Clamp01(Shown(s, i) / ymax) * r.height));
                // 부드러운 선 (Catmull-Rom, 마디마다 6칸), 그림 칸 밖으로 넘치지 않게
                var smooth = new List<Vector2>();
                if (pts.Count == 1) smooth.Add(pts[0]);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var p0 = pts[Mathf.Max(0, i - 1)];
                    var p1 = pts[i];
                    var p2 = pts[i + 1];
                    var p3 = pts[Mathf.Min(pts.Count - 1, i + 2)];
                    for (int k = 0; k < 6; k++)
                    {
                        float t = k / 6f;
                        var q = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
                        q.y = Mathf.Clamp(q.y, r.yMin, r.yMax);
                        smooth.Add(q);
                    }
                }
                if (pts.Count > 1) smooth.Add(pts[pts.Count - 1]);
                var fill = new Color(serie.color.r, serie.color.g, serie.color.b, 0.14f);
                for (int i = 0; i < smooth.Count - 1; i++)
                    m.Quad(smooth[i], smooth[i + 1], new Vector2(smooth[i + 1].x, r.yMin), new Vector2(smooth[i].x, r.yMin), fill);
                for (int i = 0; i < smooth.Count - 1; i++) m.Line(smooth[i], smooth[i + 1], 2.5f, serie.color);
                for (int i = 1; i < smooth.Count - 1; i++) m.Disc(smooth[i], 1.25f, serie.color, 6); // 마디 이음
                if (Profile.points)
                    foreach (var q in pts) m.Disc(q, 3f, serie.color, 12);
            }
        }

        void DrawBars(ChartMesh m)
        {
            DrawGrid(m);
            if (Profile.series.Count == 0) return;
            var r = PlotRect;
            float ymax = YMax;
            var s = Profile.series[0];
            int n = Mathf.Max(1, Profile.categories.Count);
            float w = Mathf.Min(Profile.barWidth, r.width / n * 0.8f);
            for (int i = 0; i < s.values.Count && i < n; i++)
            {
                float x = CategoryX(i);
                float h = Mathf.Clamp01(Shown(0, i) / ymax) * r.height;
                if (h < 0.5f) continue;
                m.RoundedTopRect(x - w * 0.5f, x + w * 0.5f, r.yMin, r.yMin + h, Mathf.Min(6f, w * 0.3f), s.color);
            }
        }

        void DrawDonut(ChartMesh m)
        {
            if (Profile.series.Count == 0) return;
            var s = Profile.series[0];
            var (c, rad) = DonutLayout();
            float inner = rad * Mathf.Clamp01(Profile.inner);
            float total = 0f;
            for (int i = 0; i < s.values.Count; i++) total += Mathf.Max(0f, Shown(0, i));
            if (total > 0f)
            {
                float gap = 2f / rad; // 조각 사이 2px
                float a = 0f;
                for (int i = 0; i < s.values.Count; i++)
                {
                    float v = Mathf.Max(0f, Shown(0, i));
                    if (v <= 0f) continue;
                    float span = v / total * Mathf.PI * 2f;
                    float g = span > gap * 2f && CountPositive(s) > 1 ? gap * 0.5f : 0f;
                    m.Arc(c, inner, rad, a + g, a + span - g, s.colors[i]);
                    a += span;
                }
            }
            if (!Profile.legend) return;
            // 범례 점 (글자는 RebuildLabels)
            var items = new List<int>();
            for (int i = 0; i < s.values.Count; i++) if (s.values[i] > 0f) items.Add(i);
            float line = Profile.fontSize + 5f;
            float top = c.y + (items.Count - 1) * line * 0.5f;
            for (int k = 0; k < items.Count; k++)
                m.Disc(new Vector2(c.x + rad + 18f + 4f, top - k * line), 4f, s.colors[items[k]], 12);
        }

        static int CountPositive(ChartSeries s)
        {
            int n = 0;
            foreach (var v in s.values) if (v > 0f) n++;
            return n;
        }

        void DrawRadar(ChartMesh m)
        {
            var (c, rad) = RadarLayout();
            int n = Profile.categories.Count;
            if (n < 3) return;
            var ring = new Vector2[n];
            for (int i = 0; i < n; i++) ring[i] = c + RadarDir(i, n) * rad;
            m.Fan(c, ring, new Color(1f, 1f, 1f, 0.03f));
            for (int k = 1; k <= 4; k++)
                for (int i = 0; i < n; i++)
                {
                    var a = c + RadarDir(i, n) * rad * k / 4f;
                    var b = c + RadarDir((i + 1) % n, n) * rad * k / 4f;
                    m.Line(a, b, 1f, k == 4 ? new Color(1f, 1f, 1f, 0.32f) : new Color(1f, 1f, 1f, 0.1f));
                }
            for (int i = 0; i < n; i++) m.Line(c, ring[i], 1f, new Color(1f, 1f, 1f, 0.18f));
            float max = YMax;
            for (int s = 0; s < Profile.series.Count; s++)
            {
                var serie = Profile.series[s];
                var pts = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    float v = i < serie.values.Count ? Mathf.Clamp01(Shown(s, i) / max) : 0f;
                    pts[i] = c + RadarDir(i, n) * rad * v;
                }
                m.Fan(c, pts, new Color(serie.color.r, serie.color.g, serie.color.b, 0.28f));
                for (int i = 0; i < n; i++) m.Line(pts[i], pts[(i + 1) % n], 2f, serie.color);
                foreach (var q in pts) m.Disc(q, 3f, serie.color, 12);
            }
        }
    }

    /// <summary>차트 그림 (메쉬만, 텍스처 없음). 정점 색으로 칠한다.</summary>
    public class ChartGraphic : MaskableGraphic
    {
        internal ChartHost Host;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Host != null && Host.Profile != null) Host.Draw(vh, rectTransform.rect);
        }
    }

    /// <summary>VertexHelper에 도형을 쌓는다. 좌표는 차트 좌표(왼쪽 아래 원점), offset으로 그래픽 로컬로 옮긴다.</summary>
    readonly struct ChartMesh
    {
        readonly VertexHelper _vh;
        readonly Vector2 _o;

        public ChartMesh(VertexHelper vh, Vector2 offset) { _vh = vh; _o = offset; }

        int V(Vector2 p, Color c)
        {
            int i = _vh.currentVertCount;
            _vh.AddVert(new Vector3(p.x + _o.x, p.y + _o.y), c, Vector2.zero);
            return i;
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            int i0 = V(a, col), i1 = V(b, col), i2 = V(c, col), i3 = V(d, col);
            _vh.AddTriangle(i0, i1, i2);
            _vh.AddTriangle(i0, i2, i3);
        }

        public void Line(Vector2 a, Vector2 b, float width, Color col)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
            Quad(a - n, b - n, b + n, a + n, col);
        }

        public void Dashed(Vector2 a, Vector2 b, float width, float dash, float gap, Color col)
        {
            float len = Vector2.Distance(a, b);
            var dir = (b - a) / Mathf.Max(1e-4f, len);
            for (float t = 0f; t < len; t += dash + gap) Line(a + dir * t, a + dir * Mathf.Min(len, t + dash), width, col);
        }

        public void Disc(Vector2 c, float r, Color col, int seg)
        {
            int center = V(c, col);
            int first = -1, prev = -1;
            for (int k = 0; k <= seg; k++)
            {
                float a = k / (float)seg * Mathf.PI * 2f;
                int i = k == seg ? first : V(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col);
                if (k == 0) first = i;
                else _vh.AddTriangle(center, prev, i);
                prev = i;
            }
        }

        /// <summary>폴리곤을 가운데에서 부채꼴로 채운다 (볼록하거나 가운데에서 보이는 모양).</summary>
        public void Fan(Vector2 c, Vector2[] pts, Color col)
        {
            int center = V(c, col);
            int first = V(pts[0], col), prev = first;
            for (int k = 1; k <= pts.Length; k++)
            {
                int i = k == pts.Length ? first : V(pts[k], col);
                _vh.AddTriangle(center, prev, i);
                prev = i;
            }
        }

        /// <summary>도넛 조각: 각도는 위에서 시계 방향(라디안).</summary>
        public void Arc(Vector2 c, float r0, float r1, float a0, float a1, Color col)
        {
            int seg = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / (Mathf.PI * 2f) * 72f));
            int pi = -1, po = -1;
            for (int k = 0; k <= seg; k++)
            {
                float a = Mathf.Lerp(a0, a1, k / (float)seg);
                var d = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                int ii = V(c + d * r0, col), io = V(c + d * r1, col);
                if (k > 0)
                {
                    _vh.AddTriangle(pi, po, io);
                    _vh.AddTriangle(pi, io, ii);
                }
                pi = ii;
                po = io;
            }
        }

        /// <summary>윗모서리만 둥근 막대.</summary>
        public void RoundedTopRect(float x0, float x1, float y0, float y1, float r, Color col)
        {
            r = Mathf.Min(r, (y1 - y0), (x1 - x0) * 0.5f);
            Quad(new Vector2(x0, y0), new Vector2(x0, y1 - r), new Vector2(x1, y1 - r), new Vector2(x1, y0), col);
            if (r <= 0.01f) return;
            Quad(new Vector2(x0 + r, y1 - r), new Vector2(x0 + r, y1), new Vector2(x1 - r, y1), new Vector2(x1 - r, y1 - r), col);
            Corner(new Vector2(x0 + r, y1 - r), r, Mathf.PI * 0.5f, Mathf.PI, col);
            Corner(new Vector2(x1 - r, y1 - r), r, 0f, Mathf.PI * 0.5f, col);
        }

        void Corner(Vector2 c, float r, float a0, float a1, Color col)
        {
            int center = V(c, col), prev = -1;
            for (int k = 0; k <= 4; k++)
            {
                float a = Mathf.Lerp(a0, a1, k / 4f);
                int i = V(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col);
                if (k > 0) _vh.AddTriangle(center, prev, i);
                prev = i;
            }
        }
    }
}
