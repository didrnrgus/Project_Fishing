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
        readonly TMP_Text _info, _clock;

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
            var c = Game.Clock;
            _clock.text = $"{c.Day}일차  {Fishing.Data.Seasons.MonthOf(c.Day)}월 {Fishing.Data.Seasons.Name(Fishing.Data.Seasons.Current)}  {c.TimeText}  ·  {Fishing.Data.Seasons.WeatherName(Game.Weather.Current)}";
        }
    }
}
