using Fishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>대기실 화면 UI: 조합 이름, 캐릭터 정보, 오른쪽 아래 메뉴 버튼 4개 (원작 배치).</summary>
    public class LobbyView
    {
        readonly RectTransform _root;
        readonly Text _info, _clock;

        public GameObject Root => _root.gameObject;

        public LobbyView(Transform canvas, System.Action openArea, System.Action openQuest, System.Action delivery, System.Action changeBase, System.Action openAquarium)
        {
            _root = UIFactory.Node("Lobby", canvas).Fill();

            var title = UIFactory.Text(_root, "한국 낚시 조합", 34, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(700, 50));
            var sub = UIFactory.Text(_root, "KOREA FISHING UNION", 14, new Color(0.7f, 0.85f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
            sub.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(700, 20));

            var panel = UIFactory.Panel(_root, "Info", new Color(0.03f, 0.05f, 0.07f, 0.85f)).rectTransform
                .At(new Vector2(0, 1), new Vector2(10, -8), new Vector2(380, 92));
            _info = UIFactory.Text(panel, "", 18, UIFactory.TextMain, TextAnchor.UpperLeft);
            _info.rectTransform.Fill(14, 10, 10, 8);

            var clockPanel = UIFactory.Panel(_root, "Clock", new Color(0.03f, 0.05f, 0.07f, 0.85f)).rectTransform
                .At(new Vector2(1, 1), new Vector2(-10, -8), new Vector2(300, 62));
            _clock = UIFactory.Text(clockPanel, "", 18, UIFactory.TextMain, TextAnchor.MiddleLeft);
            _clock.rectTransform.Fill(14, 10, 6, 6);

            (string ko, string en, System.Action act)[] menu =
            {
                ("낚시터 선택하기", "FISHING AREA", openArea),
                ("퀘스트", "QUEST", openQuest),
                ("납품", "DELIVERY", delivery),
                ("수족관", "AQUARIUM", openAquarium),
                ("거점 이동하기", "BASE", changeBase),
            };
            for (int i = 0; i < menu.Length; i++)
            {
                var (ko, en, act) = menu[i];
                var b = UIFactory.Button(_root, $"<b>{ko}</b>\n<size=12><color=#9fc8e0>{en}</color></size>", () => act(),
                    new Color(0.08f, 0.2f, 0.3f, 0.95f), 21);
                ((RectTransform)b.transform).At(new Vector2(1, 0), new Vector2(-30, 40 + (menu.Length - 1 - i) * 70), new Vector2(320, 62));
                var outline = b.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.4f, 0.75f, 0.95f, 0.6f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            var hint = UIFactory.Text(_root, "[낚시터 선택하기]에서 입장 · 수족관은 조합 안에 있어요", 17, UIFactory.TextDim, TextAnchor.MiddleLeft);
            hint.rectTransform.At(new Vector2(0, 0), new Vector2(24, 30), new Vector2(700, 26));
        }

        public void Update()
        {
            var d = Game.Profile.Data;
            _info.text = $"<b>Lv. {d.level}    강태공</b>  <size=14><color=#80c8ff>{Game.Profile.CurrentTitle}</color></size>\n" +
                         $"<color=#ffd060>{d.gold:N0} G</color>   ·   총 {d.totalCatches}마리   ·   도감 {d.dex.Count}종\n" +
                         $"<size=14><color=#a0b0c0>살림망 {d.keepNet.Count}/{Game.Profile.KeepNetSize}</color></size>";
            var c = Game.Clock;
            _clock.text = $"{c.Day}일차  {c.TimeText}  ·  {Fishing.Data.Names.Of(Game.Weather.Current)}";
        }
    }
}
