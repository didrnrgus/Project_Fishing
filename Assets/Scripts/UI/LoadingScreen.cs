using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 낚시터 입장 로딩 화면 (원작 2013 홍현 영상 0:18): 화면 가득 그 낚시터 풍경, 왼쪽 위 큰 붓글씨 이름 + 영어 이름 + 한 줄 소개,
    /// 맨 아래 팁 한 줄과 하늘색 진행 막대. 원작은 미리 그린 그림을 쓰지만 솔로판은 실제로 지은 월드를 경치 좋은 자리에서 비춘다.
    /// 순서: 검은 화면 + 글씨 → (한 프레임 그린 뒤) 월드 짓기 → 검정이 걷히며 풍경이 드러나고 막대가 찬다 → 낚시터 입장.
    /// </summary>
    public class LoadingScreen
    {
        readonly RectTransform _root;
        readonly Image _black, _bar;
        readonly TMP_Text _name, _english, _desc, _tip;

        public bool Showing => _root.gameObject.activeSelf;

        static readonly string[] Tips =
        {
            "상점에서 미끼를 살 수 있습니다.",
            "물고기마다 좋아하는 미끼·시간·날씨가 있습니다. 도감(P)에서 확인하세요.",
            "살림망이 가득 차면 납품소에서 물고기를 팔 수 있습니다.",
            "통발을 설치해 두면 낚시를 하지 않아도 물고기가 모입니다.",
            "날씨가 맑으면 챔질이 쉬워집니다.",
            "체력이 20% 아래로 떨어지면 능력치가 크게 줄어듭니다. 음식을 챙기세요.",
            "어군 한가운데에 찌를 던지면 입질이 빠릅니다.",
            "카운터 찬스에는 물고기가 도망가는 반대쪽으로 빠르게 당기세요.",
            "퀘스트 창(L)의 [힌트맵]으로 보고할 사람이 어디 있는지 볼 수 있습니다.",
        };

        public LoadingScreen(Transform canvas)
        {
            _root = UIFactory.Node("Loading", canvas).Fill();
            _black = UIFactory.Image(_root, Color.black, null, "Black");
            _black.rectTransform.Fill();
            _black.raycastTarget = true;

            // 위쪽을 살짝 어둡게 해서 흰 글씨가 하늘 위에서도 읽히게
            var shade = UIFactory.Image(_root, new Color(0f, 0f, 0f, 0.25f), null, "TopShade");
            shade.rectTransform.At(new Vector2(0, 1), Vector2.zero, new Vector2(0, 220), new Vector2(0, 1));
            shade.rectTransform.anchorMax = new Vector2(1, 1);
            shade.rectTransform.sizeDelta = new Vector2(0, 220);

            _name = UIFactory.Text(_root, "", 92, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            _name.rectTransform.At(new Vector2(0, 1), new Vector2(110, -36), new Vector2(560, 150), new Vector2(0, 1));
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Outline(_name, new Color(0.1f, 0.12f, 0.16f, 0.85f), 3f);
            _english = UIFactory.Text(_root, "", 40, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            _english.rectTransform.At(new Vector2(0, 1), new Vector2(400, -50), new Vector2(900, 56), new Vector2(0, 1));
            UIFactory.Outline(_english, new Color(0.1f, 0.12f, 0.16f, 0.8f), 2f);
            _desc = UIFactory.Text(_root, "", 22, Color.white, TextAnchor.UpperLeft);
            _desc.rectTransform.At(new Vector2(0, 1), new Vector2(400, -108), new Vector2(1100, 70), new Vector2(0, 1));
            UIFactory.Outline(_desc, new Color(0.1f, 0.12f, 0.16f, 0.8f), 1.6f);

            _tip = UIFactory.Text(_root, "", 16, Color.white, TextAnchor.MiddleCenter);
            _tip.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1200, 24), new Vector2(0.5f, 0));
            UIFactory.Outline(_tip, new Color(0, 0, 0, 0.7f), 1.2f);
            var track = UIFactory.Image(_root, new Color(0.05f, 0.08f, 0.12f, 0.85f), null, "BarTrack");
            track.rectTransform.At(new Vector2(0, 0), new Vector2(0, 6), new Vector2(0, 8), Vector2.zero);
            track.rectTransform.anchorMax = new Vector2(1, 0);
            track.rectTransform.sizeDelta = new Vector2(0, 8);
            _bar = UIFactory.Image(track.rectTransform, new Color(0.35f, 0.85f, 1f), null, "Bar");
            _bar.rectTransform.anchorMin = Vector2.zero;
            _bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            _bar.rectTransform.offsetMin = Vector2.zero;
            _bar.rectTransform.offsetMax = Vector2.zero;
            _root.gameObject.SetActive(false);
        }

        public void Begin(FishingArea area)
        {
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            var map = Fishing.World.Maps.Get(area.MapId);
            _name.text = map.ShortName;
            _english.text = Romanize(area.MapId);
            _desc.text = area.Desc;
            // 이름 길이에 맞춰 영어 이름·소개를 붙인다
            float w = Mathf.Min(_name.preferredWidth, 700f);
            _english.rectTransform.anchoredPosition = new Vector2(110 + w + 28, -50);
            _desc.rectTransform.anchoredPosition = new Vector2(110 + w + 28, -108);
            _tip.text = Tips[Random.Range(0, Tips.Length)];
            Set(0f, 1f);
        }

        /// <summary>progress 0~1 진행 막대, black 1이면 검은 화면, 0이면 풍경이 다 보인다.</summary>
        public void Set(float progress, float black)
        {
            _bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            _black.color = new Color(0f, 0f, 0f, black);
        }

        public void End() => _root.gameObject.SetActive(false);

        /// <summary>로딩 화면 영어 이름 (원작: Honghyeon).</summary>
        public static string Romanize(string mapId) => mapId switch
        {
            "hangang" => "Hangang",
            "heonhwa" => "Heonhwaro",
            "samcheok" => "Samcheok",
            "cheongjin" => "Cheongjinpogu",
            "deungdae" => "Deungdaeseom",
            "seocheon" => "Seocheon",
            "yeongheung" => "Yeongheungdo",
            "taean" => "Taean",
            "honghyeon" => "Honghyeon",
            "yokji" => "Yokjido",
            "jeju" => "Jeju Seogwipo",
            "geomun" => "Geomundo",
            "halong" => "Ha Long Bay",
            "santorini" => "Santorini",
            "nile" => "Lower Nile",
            "angkor" => "Angkor Wat",
            "vinpearl" => "Vinpearl",
            "donkhone" => "Don Khone",
            "phuket" => "Phuket",
            "bangkok" => "Bangkok",
            "kohsamui" => "Koh Samui",
            "langkawi" => "Langkawi",
            "sentosa" => "Sentosa",
            "telokbatek" => "Telok Batek",
            "tioman" => "Pulau Tioman",
            "tenggol" => "Pulau Tenggol",
            "crete" => "Crete",
            "zakynthos" => "Zakynthos",
            "duden" => "Düden Falls",
            _ => mapId,
        };
    }
}
