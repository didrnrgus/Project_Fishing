using Fishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>대기실 화면 UI: 조합 이름, 캐릭터 정보, 오른쪽 아래 메뉴 버튼 4개 (원작 배치).</summary>
    public class LobbyView
    {
        readonly RectTransform _root;
        readonly TMP_Text _info, _clock, _attend;
        readonly TMP_Text[] _dailyMarks = new TMP_Text[3];
        readonly Graphic[] _stamps = new Graphic[7], _dailyBg = new Graphic[3];

        public GameObject Root => _root.gameObject;

        public LobbyView(Transform canvas, System.Action openArea, System.Action openQuest, System.Action delivery, System.Action changeBase, System.Action openAquarium)
        {
            _root = UIFactory.Node("Lobby", canvas).Fill();

            // 가운데 위 제목: 유리 알약 위에
            var titlePill = UIFactory.Panel(_root, "TitlePill", UIFactory.Pill, 0.5f).rectTransform
                .At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(420, 76));
            var title = UIFactory.Text(titlePill, "한국 낚시 조합", 32, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(420, 44));
            var sub = UIFactory.Text(titlePill, "KOREA FISHING UNION", 12, new Color(0.55f, 0.88f, 0.85f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
            sub.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(420, 18));

            var panel = UIFactory.Panel(_root, "Info", 20f).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, -12), new Vector2(390, 100));
            _info = UIFactory.Text(panel, "", 18, UIFactory.TextMain, TextAnchor.UpperLeft);
            _info.rectTransform.Fill(18, 14, 14, 10);

            var clockPanel = UIFactory.Panel(_root, "Clock", UIFactory.Pill, 0.55f).rectTransform
                .At(new Vector2(1, 1), new Vector2(-14, -12), new Vector2(310, 48));
            _clock = UIFactory.Text(clockPanel, "", 18, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _clock.rectTransform.Fill(14, 14, 0, 0);

            // 원작 대기실 오른쪽 위 (2013 홍현 영상 0:00): 시계 밑 판에 "연속출석 N일" 도장 7칸 + "일일 도전과제" 3칸 (끝낸 의뢰는 체크, 남은 의뢰는 ?)
            var board = UIFactory.Panel(_root, "AttendBoard", 18f, 0.6f).rectTransform
                .At(new Vector2(1, 1), new Vector2(-14, -68), new Vector2(310, 176), new Vector2(1, 1));
            _attend = UIFactory.Text(board, "", 19, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _attend.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(290, 26));
            for (int i = 0; i < 7; i++)
            {
                _stamps[i] = UIFactory.Shape(board, Color.white, UIFactory.Pill, "Stamp", 0f);
                _stamps[i].rectTransform.At(new Vector2(0.5f, 1), new Vector2(-117 + i * 39, -50), new Vector2(30, 30));
            }
            var dailyTitle = UIFactory.Text(board, "일일 도전과제", 16, UIFactory.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            dailyTitle.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(290, 22));
            for (int i = 0; i < 3; i++)
            {
                _dailyBg[i] = UIFactory.Shape(board, new Color(0.35f, 0.25f, 0.05f, 0.9f), UIFactory.Pill, "Daily", 0f);
                _dailyBg[i].rectTransform.At(new Vector2(0.5f, 1), new Vector2(-70 + i * 70, -110), new Vector2(50, 50));
                _dailyMarks[i] = UIFactory.Text(_dailyBg[i].transform, "", 26, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                _dailyMarks[i].rectTransform.Fill();
            }

            (string ko, string en, System.Action act)[] menu =
            {
                ("낚시터 선택하기", "FISHING AREA", openArea),
                ("퀘스트", "QUEST", openQuest),
                ("납품", "DELIVERY", delivery),
                ("수족관", "AQUARIUM", openAquarium),
                ("거점 이동하기", "BASE", changeBase),
            };
            // 오른쪽 아래 유리 메뉴판: 첫 버튼(낚시터 선택)을 강조한다
            var dock = UIFactory.Panel(_root, "MenuDock", UIFactory.RadiusPanel, 0.55f).rectTransform
                .At(new Vector2(1, 0), new Vector2(-24, 24), new Vector2(352, menu.Length * 70 + 18), new Vector2(1, 0));
            for (int i = 0; i < menu.Length; i++)
            {
                var (ko, en, act) = menu[i];
                bool primary = i == 0;
                var b = UIFactory.Button(dock, $"<b>{ko}</b>\n<size=12><color={(primary ? "#d8fff8" : "#9fc8e0")}>{en}</color></size>", () => act(),
                    primary ? UIFactory.BtnPrimary : new Color(1f, 1f, 1f, 0.08f), 21);
                UIFactory.SetRadius(b.targetGraphic, 14f);
                ((RectTransform)b.transform).At(new Vector2(0.5f, 1), new Vector2(0, -12 - i * 70), new Vector2(326, 62), new Vector2(0.5f, 1));
                if (primary) UIFactory.AddBorder((LeTai.Asset.TranslucentImage.TranslucentImage)b.targetGraphic, new Color(0.6f, 1f, 0.95f, 0.5f), 1.5f, 6f);
            }

            var hintPill = UIFactory.Panel(_root, "HintPill", UIFactory.Pill, 0.45f).rectTransform
                .At(new Vector2(0, 0), new Vector2(14, 24), new Vector2(560, 36));
            var hint = UIFactory.Text(hintPill, "[낚시터 선택하기]에서 입장 · 수족관은 조합 안에 있어요", 16, UIFactory.TextDim, TextAnchor.MiddleCenter);
            hint.rectTransform.Fill();
        }

        public void Update()
        {
            var d = Game.Profile.Data;
            _info.text = $"<b>Lv. {d.level}    강태공</b>  <size=14><color=#80c8ff>{Game.Profile.CurrentTitle}</color></size>\n" +
                         $"<color=#ffd060>{d.gold:N0} G</color>   ·   총 {d.totalCatches}마리   ·   도감 {d.dex.Count}종\n" +
                         $"<size=14><color=#a0b0c0>살림망 {d.keepNet.Count}/{Game.Profile.KeepNetSize}</color></size>";
            // 연속출석: 이번 7칸 중 찍은 칸 (7칸째를 받은 날은 7칸이 다 찬 채로 보인다)
            int filled = d.attendStamp == 0 ? 0 : (d.attendStamp - 1) % 7 + 1;
            _attend.text = $"연속출석  <size=24><color=#ffd060>{filled}</color></size> 일";
            for (int i = 0; i < 7; i++)
            {
                // 7칸째(블랙스피어)는 원작처럼 보석 빛
                _stamps[i].color = i == 6 ? (i < filled ? new Color(0.6f, 0.9f, 1f) : new Color(0.6f, 0.75f, 0.85f, 0.35f)) : (i < filled ? UIFactory.Gold : new Color(1f, 1f, 1f, 0.18f));
            }
            for (int i = 0; i < 3; i++)
            {
                bool has = d.daily != null && i < d.daily.Count;
                bool done = has && d.daily[i].done;
                _dailyMarks[i].text = !has ? "-" : done ? "완" : "?";
                _dailyMarks[i].color = done ? Color.white : UIFactory.Gold;
                _dailyBg[i].color = done ? new Color(0.15f, 0.55f, 0.3f, 0.95f) : new Color(0.35f, 0.25f, 0.05f, 0.9f);
            }
            var c = Game.Clock;
            _clock.text = $"{c.Day}일차  {Fishing.Data.Seasons.MonthOf(c.Day)}월 {Fishing.Data.Seasons.Name(Fishing.Data.Seasons.Current)}  {c.TimeText}  ·  {Fishing.Data.Seasons.WeatherName(Game.Weather.Current)}";
        }
    }
}
