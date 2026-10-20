using Fishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 대기실 화면 UI (원작 cpmw_lobby_ship.mp4 0:00~0:10, clips/01 로비 — 1920×1080 기준 자리):
    ///  - 가운데 위 판 없이 흰 굵은 글씨 `한국 낚시 조합` (y≈70)
    ///  - 오른쪽 위 작은 검은 판: `대기실  한국 낚시 조합` 한 줄 + 큰 날짜·시계, 그 밑 양피지 놋쇠 판 `연속출석 N일` 도장 7칸 + `일일 도전과제` ? 3칸
    ///  - 오른쪽 아래 옅은 검은 판 안에 남색 금속 버튼 4개(300×56, 65px 간격) `낚시터 선택하기 / 퀘스트 / 납품 / 거점 이동하기`
    /// 솔로판에 더한 `수족관`·`옷 바꾸기`는 원작 4개 위에 반 폭 두 칸으로 둔다 (원작 4개 배치는 그대로).
    /// </summary>
    public class LobbyView
    {
        readonly RectTransform _root;
        readonly TMP_Text _info, _clock, _attend, _outfitLabel;
        readonly TMP_Text[] _dailyMarks = new TMP_Text[3];
        readonly Graphic[] _stamps = new Graphic[7], _dailyBg = new Graphic[3];
        readonly Button _outfit;
        bool _outfitBusy;

        static readonly Color Ink = new Color(0.24f, 0.15f, 0.07f);           // 양피지 판 글씨
        static readonly Color Parchment = new Color(0.8f, 0.72f, 0.56f, 0.97f);
        static readonly Color Brass = new Color(0.42f, 0.32f, 0.16f, 1f);

        public GameObject Root => _root.gameObject;

        public LobbyView(Transform canvas, System.Action openArea, System.Action openQuest, System.Action delivery, System.Action changeBase, System.Action openAquarium)
        {
            _root = UIFactory.Node("Lobby", canvas).Fill();

            // 가운데 위 제목: 원작은 판 없이 흰 글씨만 (어두운 실내 위라 외곽선으로 읽힌다)
            var title = UIFactory.Text(_root, "한국 낚시 조합", 34, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(600, 56));
            UIFactory.Outline(title, new Color(0f, 0f, 0f, 0.85f), 2f);

            var panel = UIFactory.Panel(_root, "Info", 20f).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, -12), new Vector2(390, 100));
            _info = UIFactory.Text(panel, "", 18, UIFactory.TextMain, TextAnchor.UpperLeft);
            _info.rectTransform.Fill(18, 14, 14, 10);

            // 오른쪽 위 시계 판: 위 줄 `대기실  한국 낚시 조합`(대기실은 하늘색), 아래 큰 시계
            var clockPanel = UIFactory.Shape(_root, new Color(0.02f, 0.03f, 0.05f, 0.72f), 3f, "Clock", 0f);
            UIFactory.AddBorder(clockPanel, new Color(0.5f, 0.6f, 0.7f, 0.35f), 1f);
            clockPanel.rectTransform.At(new Vector2(1, 1), new Vector2(-12, -8), new Vector2(330, 62));
            var where = UIFactory.Text(clockPanel.transform, "<color=#5cc8ff>대기실</color>   한국 낚시 조합", 14, UIFactory.TextMain, TextAnchor.MiddleLeft);
            where.rectTransform.At(new Vector2(0, 1), new Vector2(12, -4), new Vector2(310, 20));
            _clock = UIFactory.Text(clockPanel.transform, "", 21, UIFactory.TextMain, TextAnchor.MiddleRight, FontStyle.Bold);
            _clock.rectTransform.At(new Vector2(0, 1), new Vector2(10, -24), new Vector2(310, 34));

            // 원작 대기실 오른쪽 위 (2013 홍현 영상 0:00): 시계 밑 양피지 판(놋쇠 테·모서리 못)에 "연속출석 N일" 도장 7칸 + "일일 도전과제" 3칸
            // (끝낸 의뢰는 완, 남은 의뢰는 ?)
            var board = UIFactory.Shape(_root, Parchment, 4f, "AttendBoard", 3f);
            UIFactory.AddBorder(board, Brass, 3f, 1f);
            UIFactory.AddShadow(board, 14f, new Vector2(0, -4), 0.5f);
            board.rectTransform.At(new Vector2(1, 1), new Vector2(-22, -76), new Vector2(196, 216), new Vector2(1, 1));
            foreach (var corner in new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) })
            {
                var rivet = UIFactory.Image(board.transform, new Color(0.3f, 0.22f, 0.12f), UIFactory.Circle, "Rivet");
                rivet.rectTransform.At(corner, new Vector2(corner.x > 0 ? -10 : 10, corner.y > 0 ? -10 : 10), new Vector2(9, 9));
            }
            // 위 가운데 놋쇠 걸이 (원작: 판 윗변의 작은 손잡이)
            var hook = UIFactory.Shape(board.transform, Brass, UIFactory.Pill, "Hook", 2f);
            hook.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, 6), new Vector2(64, 10));
            _attend = UIFactory.Text(board.transform, "", 17, Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            _attend.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(180, 54));
            _attend.lineSpacing = -6f;
            for (int i = 0; i < 7; i++)
            {
                // 도장 칸: 7개를 두 줄(4 + 3)로. 7칸째는 블랙스피어(보석)
                int row = i < 4 ? 0 : 1, col = i < 4 ? i : i - 4;
                float x = row == 0 ? -63 + col * 42 : -42 + col * 42;
                var ring = UIFactory.Image(board.transform, new Color(0.3f, 0.2f, 0.08f, 0.55f), UIFactory.Circle, "StampRing");
                ring.rectTransform.At(new Vector2(0.5f, 1), new Vector2(x, -82 - row * 32), new Vector2(32, 32));
                _stamps[i] = UIFactory.Image(ring.transform, Color.white, UIFactory.Circle, "Stamp");
                _stamps[i].rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
            }
            var dailyTitle = UIFactory.Text(board.transform, "일일 도전과제", 15, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            dailyTitle.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(180, 20));
            for (int i = 0; i < 3; i++)
            {
                var ring = UIFactory.Image(board.transform, new Color(0.25f, 0.16f, 0.05f, 0.9f), UIFactory.Circle, "DailyRing");
                ring.rectTransform.At(new Vector2(0.5f, 1), new Vector2(-56 + i * 56, -184), new Vector2(46, 46));
                _dailyBg[i] = UIFactory.Image(ring.transform, UIFactory.Gold, UIFactory.Circle, "Daily");
                _dailyBg[i].rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38));
                _dailyMarks[i] = UIFactory.Text(_dailyBg[i].transform, "", 24, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                _dailyMarks[i].rectTransform.Fill();
            }

            // 오른쪽 아래 메뉴 (원작 1920×1080: 판 x 1535~1910, 버튼 300×56을 65px 간격으로, 마지막 버튼 아래 끝이 바닥에서 75px)
            (string ko, string en, System.Action act)[] menu =
            {
                ("낚시터 선택하기", "FISHING AREA", openArea),
                ("퀘스트", "QUEST", openQuest),
                ("납품", "DELIVERY", delivery),
                ("거점 이동하기", "BASE", changeBase),
            };
            const float pitch = 65f, btnH = 56f, extraH = 44f;
            float dockH = 14 + extraH + 12 + menu.Length * pitch - (pitch - btnH) + 14;
            var dock = UIFactory.Shape(_root, new Color(0f, 0f, 0f, 0.3f), 4f, "MenuDock", 0f);
            UIFactory.AddBorder(dock, new Color(1f, 1f, 1f, 0.1f), 1f);
            dock.rectTransform.At(new Vector2(1, 0), new Vector2(-10, 60), new Vector2(372, dockH), new Vector2(1, 0));
            for (int i = 0; i < menu.Length; i++)
            {
                var (ko, en, act) = menu[i];
                var b = UIFactory.ClassicButton(dock.transform, ko, en, () => act(), false, 23);
                ((RectTransform)b.transform).At(new Vector2(0.5f, 0), new Vector2(0, 14 + (menu.Length - 1 - i) * pitch), new Vector2(300, btnH), new Vector2(0.5f, 0));
            }
            // 솔로판에 더한 두 칸: 원작 4개 위에 반 폭으로 (수족관 · 옷 바꾸기)
            float extraY = 14 + menu.Length * pitch - (pitch - btnH) + 12;
            var aq = UIFactory.ClassicButton(dock.transform, "수족관", "AQUARIUM", () => openAquarium(), false, 18);
            ((RectTransform)aq.transform).At(new Vector2(0.5f, 0), new Vector2(-77, extraY), new Vector2(146, extraH), new Vector2(0.5f, 0));
            // [옷 바꾸기]: 다른 VRM의 옷을 무작위로 입힌다 (Characters/OutfitChanger, 웹에서 받는 동안은 "받는 중…")
            _outfit = UIFactory.ClassicButton(dock.transform, "옷 바꾸기", "OUTFIT", () => Fishing.Characters.OutfitChanger.Randomize(), false, 18);
            ((RectTransform)_outfit.transform).At(new Vector2(0.5f, 0), new Vector2(77, extraY), new Vector2(146, extraH), new Vector2(0.5f, 0));
            _outfitLabel = _outfit.GetComponentInChildren<TMP_Text>();

            var hint = UIFactory.Text(_root, "[낚시터 선택하기]에서 입장 · 수족관은 조합 안에 있어요", 15, UIFactory.TextDim, TextAnchor.MiddleLeft);
            hint.rectTransform.At(new Vector2(0, 0), new Vector2(22, 22), new Vector2(560, 28));
            UIFactory.Outline(hint, new Color(0f, 0f, 0f, 0.8f), 1.5f);
        }

        public void Update()
        {
            var d = Game.Profile.Data;
            _info.text = $"<b>Lv. {d.level}    강태공</b>  <size=14><color=#80c8ff>{Game.Profile.CurrentTitle}</color></size>\n" +
                         $"<color=#ffd060>{d.gold:N0} G</color>   ·   총 {d.totalCatches}마리   ·   도감 {d.dex.Count}종\n" +
                         $"<size=14><color=#a0b0c0>살림망 {d.keepNet.Count}/{Game.Profile.KeepNetSize}</color></size>";
            // 연속출석: 이번 7칸 중 찍은 칸 (7칸째를 받은 날은 7칸이 다 찬 채로 보인다)
            int filled = d.attendStamp == 0 ? 0 : (d.attendStamp - 1) % 7 + 1;
            _attend.text = $"연속출석\n<size=24>{filled} 일</size>";
            for (int i = 0; i < 7; i++)
            {
                // 찍은 칸은 금빛 도장, 7칸째(블랙스피어)는 원작처럼 보석 빛, 안 찍은 칸은 옅은 빈 자리
                _stamps[i].color = i == 6 ? (i < filled ? new Color(0.65f, 0.92f, 1f) : new Color(0.75f, 0.8f, 0.85f, 0.45f))
                                          : (i < filled ? UIFactory.Gold : new Color(0.55f, 0.45f, 0.3f, 0.45f));
            }
            for (int i = 0; i < 3; i++)
            {
                bool has = d.daily != null && i < d.daily.Count;
                bool done = has && d.daily[i].done;
                _dailyMarks[i].text = !has ? "-" : done ? "완" : "?";
                _dailyMarks[i].color = done ? Color.white : Ink;
                _dailyBg[i].color = done ? new Color(0.2f, 0.55f, 0.3f) : UIFactory.Gold;
            }
            var c = Game.Clock;
            _clock.text = $"<size=15>{c.Day}일차 {Fishing.Data.Seasons.MonthOf(c.Day)}월 {Fishing.Data.Seasons.Name(Fishing.Data.Seasons.Current)} · {Fishing.Data.Seasons.WeatherName(Game.Weather.Current)}</size>   {c.TimeText}";
            bool busy = Fishing.Characters.OutfitChanger.Busy;
            if (busy != _outfitBusy)
            {
                _outfitBusy = busy;
                _outfit.interactable = !busy;
                if (_outfitLabel != null) _outfitLabel.text = $"{(busy ? "받는 중…" : "옷 바꾸기")}\n<size=10><color=#e4f0f8>OUTFIT</color></size>";
            }
        }
    }
}
