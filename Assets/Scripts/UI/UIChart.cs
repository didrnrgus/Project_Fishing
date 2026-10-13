using System.Collections.Generic;
using EasyChart;
using EasyChart.Layers;
using EasyChart.UGUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fishing.UI
{
    /// <summary>
    /// EasyChart(무료판: 선·막대·도넛·레이더)를 코드로 만든다. 차트는 UI Toolkit으로 렌더 텍스처에 그려져
    /// UGUI 안에 RawImage로 들어가므로, 창 순서·숨김·마스크가 다른 UI와 똑같이 동작한다.
    /// 배경은 투명이라 유리 판 위에 그대로 얹는다. 데이터를 바꾼 뒤에는 <see cref="ChartHost.Refresh"/>.
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

        static readonly Color AxisLabel = new Color(0.78f, 0.84f, 0.9f, 0.9f);
        static readonly Color GridLine = new Color(1f, 1f, 1f, 0.1f);

        static ChartTheme _theme;

        /// <summary>
        /// 차트 글꼴(한글). UI와 같은 Jalnan2(TMP 폰트 에셋의 원본 TTF)를 UI Toolkit용 TextCore 폰트로 만든다.
        /// 안 되면 시스템 맑은 고딕, 그것도 안 되면 OS 동적 폰트.
        /// </summary>
        static void EnsureTheme()
        {
            if (_theme != null) return;
            _theme = ScriptableObject.CreateInstance<ChartTheme>();
            _theme.hideFlags = HideFlags.DontSave;
            Object font = null;
            try
            {
                var source = UIFactory.Font != null ? UIFactory.Font.sourceFontFile : null;
                if (source != null) font = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(source);
                if (font == null)
                    foreach (var family in new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "NanumGothic" })
                    {
                        var fa = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(family, "Regular");
                        if (fa != null) { font = fa; break; }
                    }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIChart] 시스템 폰트를 TextCore 폰트로 만들지 못했습니다: {e.Message}");
            }
            _theme.primaryFont = font != null ? font : Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 32);
            _theme.fontScale = 1f;
            ChartThemeRegistry.GlobalTheme = _theme;
        }

        /// <summary>차트 칸을 만든다. size는 UI 좌표(기준 1920x1080) 크기이고 차트 설계 크기도 같게 둔다.</summary>
        public static ChartHost Create(Transform parent, ChartProfile profile, Vector2 size, string name = "Chart")
        {
            EnsureTheme();
            profile.chartWidth = size.x;
            profile.chartHeight = size.y;
            var rt = UIFactory.Node(name, parent);
            rt.sizeDelta = size;
            var go = rt.gameObject;
            go.SetActive(false); // 설정을 다 넣은 뒤 켠다 (브리지가 OnEnable 다음 LateUpdate에서 만든다)
            var bridge = go.AddComponent<UGUIChartBridge>();
            bridge.RenderMode = ChartRenderMode.WorldSpace;
            bridge.Profile = profile;
            var host = go.AddComponent<ChartHost>();
            host.Init(bridge, profile);
            go.SetActive(true);
            return host;
        }

        // ---------------- 프로필 ----------------

        static ChartProfile NewProfile(CoordinateSystemType cs)
        {
            var p = ScriptableObject.CreateInstance<ChartProfile>();
            p.hideFlags = HideFlags.DontSave;
            p.name = p.chartName = "RuntimeChart";
            p.chartId = System.Guid.NewGuid().ToString("N");
            p.coordinateSystem = cs;
            p.padding = new Vector4(46, 14, 14, 30);
            p.paddingInitialized = true;
            p.gridSettingsInitialized = true;
            p.hoverSettingsInitialized = true;
            p.axisSelectionInitialized = true;
            p.axisTypeSelectionInitialized = true;
            p.xAxisId = AxisId.XBottom;
            p.yAxisId = AxisId.YLeft;
            p.cartesian.xAxisId = AxisId.XBottom;
            p.cartesian.yAxisId = AxisId.YLeft;
            p.background.show = false;
            p.backgroundFX.enabled = false;
            p.plotSettings.overrideTheme = false;
            p.animationDuration = 0.45f;
            p.legendSettings.enabled = false;
            p.hover.cursorLineColor = new Color(1, 1, 1, 0.25f);
            p.hover.cursorLineWidth = 1f;
            return p;
        }

        static LabelStyleSettings LabelStyle(Color c, int size, bool on = true) =>
            new LabelStyleSettings { enabled = on, fontSize = size, color = c, position = LabelPosition.Outside };

        static AxisConfig Axis(AxisId id, AxisType type, Color line, int fontSize, bool labels = true) => new AxisConfig
        {
            id = id,
            axisType = type,
            labels = new List<string>(),
            visible = true,
            color = line,
            width = 1f,
            labelStyle = LabelStyle(AxisLabel, fontSize, labels),
            unitLabelStyle = LabelStyle(AxisLabel, fontSize, false),
            labelPlacement = type == AxisType.Category ? CategoryLabelPlacement.CellCenter : CategoryLabelPlacement.Tick,
            autoTicks = true,
            splitCount = 4,
            autoRangeRounding = AutoRangeRoundingMode.NiceNumbers,
        };

        static ChartProfile Cartesian(string[] categories, float? yMax, string yFormat, int fontSize, bool barLayout)
        {
            var p = NewProfile(CoordinateSystemType.Cartesian2D);
            var x = Axis(AxisId.XBottom, AxisType.Category, new Color(1, 1, 1, 0.25f), fontSize);
            x.labels.AddRange(categories);
            x.labelPlacement = barLayout ? CategoryLabelPlacement.CellCenter : CategoryLabelPlacement.Tick;
            var y = Axis(AxisId.YLeft, AxisType.Value, Color.clear, fontSize - 1);
            y.labelFormat = yFormat;
            y.autoRangeMin = false;
            y.minValue = 0f;
            if (yMax.HasValue)
            {
                y.autoRangeMax = false;
                y.maxValue = yMax.Value;
            }
            p.axes.Add(x);
            p.axes.Add(y);
            p.cartesianGrid.xGridColor = Color.clear;
            p.cartesianGrid.yGridColor = GridLine;
            p.cartesianGrid.yGridLineWidth = 1f;
            p.cartesianGrid.yGridDashed = true;
            return p;
        }

        public struct Series
        {
            public string Name;
            public float[] Values;
            public Color Color;
            public Series(string name, float[] values, Color color) { Name = name; Values = values; Color = color; }
        }

        /// <summary>꺾은선 (부드러운 선 + 옅은 면). yMax를 주면 세로축 범위를 고정한다.</summary>
        public static ChartHost Line(Transform parent, Vector2 size, string[] categories, Series[] series, float? yMax = null,
            string yFormat = "0", int fontSize = 12, bool points = false, bool legend = false)
        {
            var p = Cartesian(categories, yMax, yFormat, fontSize, false);
            foreach (var s in series)
            {
                var ls = new LineSettings();
                ls.stroke.lineType = LineType.Smooth;
                ls.stroke.width = 2.5f;
                ls.stroke.color = s.Color;
                ls.point.show = points;
                ls.point.size = 6f;
                ls.point.textureFill.color = s.Color;
                ls.area.show = true;
                ls.area.textureFill.color = new Color(s.Color.r, s.Color.g, s.Color.b, 0.14f);
                ls.hover.enabled = false;
                ls.stacked = false;
                var serie = new Serie { name = s.Name, type = SerieType.Line, visible = true, settings = ls };
                serie.labelSettings.decimalPlaces = 0;
                SetValues(serie, s.Values);
                p.series.Add(serie);
            }
            if (legend)
            {
                p.legendSettings.enabled = true;
                p.legendSettings.position = LegendPosition.Top;
                p.legendSettings.fontSize = fontSize;
                p.legendSettings.color = AxisLabel;
                p.legendSettings.offset = new Vector2(0, 2);
                p.padding.y = 34;
            }
            return Create(parent, p, size, "LineChart");
        }

        /// <summary>세로 막대. 막대 위에 값 표시(valueFormat 자릿수).</summary>
        public static ChartHost Bars(Transform parent, Vector2 size, string[] categories, float[] values, Color color, float? yMax = null,
            string yFormat = "0", int decimals = 0, int fontSize = 12, bool valueLabels = true, float barWidth = 22f)
        {
            var p = Cartesian(categories, yMax, yFormat, fontSize, true);
            var bs = new BarSettings
            {
                barWidth = barWidth, barGap = 4f, categoryGap = 0f,
                cornerRadius = Mathf.Min(6f, barWidth * 0.3f), cornerSegments = 4,
                stacked = false, stackGroup = "",
            };
            bs.textureFill.color = color;
            bs.border.width = 0f;
            bs.background.show = false;
            bs.hover.enabled = false;
            var serie = new Serie { name = "값", type = SerieType.Bar, visible = true, settings = bs };
            serie.labelSettings.enabled = valueLabels;
            serie.labelSettings.position = LabelPosition.Outside;
            serie.labelSettings.color = UIFactory.TextMain;
            serie.labelSettings.fontSize = fontSize - 1;
            serie.labelSettings.decimalPlaces = decimals;
            SetValues(serie, values);
            p.series.Add(serie);
            return Create(parent, p, size, "BarChart");
        }

        /// <summary>도넛. 오른쪽에 범례(이름 + 값). 값이 0 이하인 조각은 빠진다.</summary>
        public static ChartHost Donut(Transform parent, Vector2 size, string[] names, float[] values, Color[] colors,
            float inner = 0.62f, bool legend = true, int fontSize = 13)
        {
            var p = NewProfile(CoordinateSystemType.None);
            p.padding = new Vector4(0, 6, 0, 6);
            var ps = new PieSettings();
            ps.layout.innerRadius = inner;
            ps.layout.outerRadius = 0f;
            ps.layout.startAngleDeg = -90f;
            ps.layout.clockwise = true;
            ps.layout.angleRangeDeg = 360f;
            ps.layout.sliceGapPx = 2f;
            ps.layout.sliceGapType = SliceGapType.Radial;
            ps.layout.cornerRadius = 2f;
            ps.layout.innerRadiusColor = Color.clear;
            ps.layout.plot.padding = 6f;
            ps.layout.plot.centerOffset = legend ? new Vector2(-size.x * 0.24f, 0) : Vector2.zero;
            ps.hover.enabled = false;
            ps.sortByValue = false;
            ps.aggregation.enabled = false;
            ps.legend.enabled = legend;
            ps.legend.position = LegendPosition.Right;
            ps.legend.fontSize = fontSize;
            ps.legend.color = UIFactory.TextMain;
            ps.legend.itemSpacing = 5f;
            ps.legend.offset = new Vector2(-8, 0);
            ps.legend.valueDisplayStyle = ValueDisplayStyle.None; // 값은 이름에 직접 넣는다
            ps.legend.source = PieLegendSource.Slice;
            var serie = new Serie { name = "구성", type = SerieType.Pie, visible = true, settings = ps };
            serie.labelSettings.enabled = false;
            serie.labelSettings.decimalPlaces = 0;
            SetSlices(serie, names, values, colors);
            p.series.Add(serie);
            return Create(parent, p, size, "DonutChart");
        }

        /// <summary>레이더. 값은 0~100으로 정규화해서 넣는다(축마다 최대가 달라도 모양을 비교할 수 있게).</summary>
        public static ChartHost Radar(Transform parent, Vector2 size, string[] indicators, Series[] series, int fontSize = 13)
        {
            var p = NewProfile(CoordinateSystemType.Polar2D);
            p.padding = new Vector4(8, 8, 8, 8);
            var ang = p.polarAxes.angleAxis;
            var rad = p.polarAxes.radiusAxis;
            ang.labels = new List<string>(indicators);
            ang.visible = true;
            ang.color = new Color(1, 1, 1, 0.18f);
            ang.width = 1f;
            ang.labelStyle = LabelStyle(AxisLabel, fontSize);
            rad.visible = true;
            rad.color = new Color(1, 1, 1, 0.1f);
            rad.edgeColor = new Color(1, 1, 1, 0.32f);
            rad.width = 1f;
            rad.splitCount = 4;
            rad.labelStyle = LabelStyle(AxisLabel, 10, false);
            rad.autoRangeMin = false;
            rad.minValue = 0f;
            rad.autoRangeMax = false;
            rad.maxValue = 100f;
            rad.labelFormat = "0";
            foreach (var s in series)
            {
                var rs = new RadarSettings();
                rs.radar.startAngleDeg = -90f;
                rs.radar.clockwise = true;
                rs.radar.outerRadius = 0f;
                rs.radar.plot.padding = fontSize * 2.6f;
                rs.radar.textureFill.color = new Color(1, 1, 1, 0.03f);
                rs.stroke.color = s.Color;
                rs.stroke.width = 2f;
                rs.area.show = true;
                rs.area.textureFill.color = new Color(s.Color.r, s.Color.g, s.Color.b, 0.28f);
                rs.point.show = true;
                rs.point.size = 6f;
                rs.point.textureFill.color = s.Color;
                var serie = new Serie { name = s.Name, type = SerieType.Radar, visible = true, settings = rs };
                serie.labelSettings.enabled = false;
                SetValues(serie, s.Values);
                p.series.Add(serie);
            }
            return Create(parent, p, size, "RadarChart");
        }

        // ---------------- 데이터 갱신 ----------------

        public static void SetValues(Serie serie, IList<float> values)
        {
            serie.seriesData.Clear();
            for (int i = 0; i < values.Count; i++) serie.seriesData.Add(new SeriesData { x = i, value = values[i] });
            serie.EnsureIntegrity();
        }

        public static void SetSlices(Serie serie, IList<string> names, IList<float> values, IList<Color> colors)
        {
            serie.seriesData.Clear();
            for (int i = 0; i < values.Count; i++)
                serie.seriesData.Add(new SeriesData
                {
                    x = i, value = values[i], name = names[i],
                    useColor = true, color = colors != null && i < colors.Count ? colors[i] : Palette[i % Palette.Length],
                });
            serie.EnsureIntegrity();
        }

        public static void SetCategories(ChartProfile p, IList<string> categories)
        {
            var x = p.axes.Find(a => a.id == p.xAxisId);
            if (x == null) return;
            x.labels.Clear();
            x.labels.AddRange(categories);
        }
    }

    /// <summary>
    /// 차트 하나를 붙잡고 있는 컴포넌트. 브리지가 켜질 때마다 ChartElement를 새로 만들므로
    /// 마우스 상호작용(툴팁·범례 클릭)을 끄는 정책을 매번 다시 넣는다.
    /// </summary>
    public class ChartHost : MonoBehaviour
    {
        UGUIChartBridge _bridge;
        ChartElement _applied;
        bool _dirty;

        public ChartProfile Profile { get; private set; }

        public void Init(UGUIChartBridge bridge, ChartProfile profile)
        {
            _bridge = bridge;
            Profile = profile;
        }

        /// <summary>Profile의 데이터를 바꾼 뒤 부른다. 다음 프레임에 다시 그린다.</summary>
        public void Refresh() => _dirty = true;

        public Serie Serie(int index = 0) => Profile.series[index];

        void LateUpdate()
        {
            var chart = _bridge != null ? _bridge.ChartElement : null;
            if (chart == null) return;
            if (chart != _applied)
            {
                _applied = chart;
                chart.InteractionPolicy = NoInteraction.Instance;
                chart.LegendTogglePolicy = NoLegendToggle.Instance;
            }
            if (_dirty)
            {
                _dirty = false;
                chart.ForceRefreshProfile();
            }
        }

        void OnDestroy()
        {
            if (Profile != null) Destroy(Profile);
        }

        sealed class NoInteraction : IChartInteractionPolicy
        {
            public static readonly NoInteraction Instance = new NoInteraction();

            public void OnPointerMove(IChartTooltipRuntime tooltip, VisualElement host, Vector2 pointerWorldPos, ChartData data,
                IList<BaseSeriesRenderer> renderers, VisualElement chartArea, Vector4 padding, bool isTransposed,
                bool isCategorySmoothTranslating, Label tooltipLabel, VisualElement cursorLine) { }

            public void OnPointerLeave(IChartTooltipRuntime tooltip, IList<BaseSeriesRenderer> renderers, Label tooltipLabel,
                VisualElement cursorLine) { }
        }

        sealed class NoLegendToggle : IChartLegendTogglePolicy
        {
            public static readonly NoLegendToggle Instance = new NoLegendToggle();
            public bool GetNextVisibleState(bool isVisible, PointerDownEvent evt) => isVisible;
        }
    }
}
