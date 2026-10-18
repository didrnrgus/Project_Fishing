using Fishing.Core;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// ESC 메뉴 (원작 2013 홍현 영상 2:50): 화면 가운데 세로 띠에 버튼을 쌓는다.
    /// 게임으로 돌아가기 / 옵션 / 시작 위치로 가기 / 낚시터 나가기 / 게임 종료(붉은 버튼).
    /// 원작의 서버선택은 솔로판에 없어서 뺐다. 열린 창이 있으면 ESC는 창을 닫고, 아무 창도 없을 때 이 메뉴를 연다.
    /// </summary>
    public class EscMenuPanel : UIPanel
    {
        readonly UnityEngine.UI.Button _start, _exit;

        public EscMenuPanel(Transform canvas, System.Action options, System.Action startPoint, System.Action exitField)
        {
            var root = UIFactory.Dim(canvas, "EscMenu", 0.35f);
            SetRoot(root.gameObject);
            // 세로 띠: 가운데만 어둡게 (원작처럼 양옆은 화면이 비친다)
            var band = UIFactory.Image(root.transform, new Color(0f, 0f, 0f, 0.55f), null, "Band");
            band.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 2000));

            UnityEngine.UI.Button Btn(string ko, string en, float y, System.Action act, Color bg)
            {
                var b = UIFactory.Button(root.transform, $"<b>{ko}</b>\n<size=12><color=#cfe6f5>{en}</color></size>", () => act(), bg, 21);
                ((RectTransform)b.transform).At(new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(240, 56));
                return b;
            }
            Btn("게임으로 돌아가기", "RESUME GAME", 175, Close, UIFactory.BtnPrimary);
            Btn("옵션", "OPTION", 90, () => { Close(); options(); }, UIFactory.ButtonBg);
            _start = Btn("시작 위치로 가기", "STARTING POINT", 28, () => { Close(); startPoint(); }, UIFactory.ButtonBg);
            _exit = Btn("낚시터 나가기", "EXIT FISHING AREA", -34, () => { Close(); exitField(); }, UIFactory.ButtonBg);
            Btn("게임 종료", "EXIT GAME", -130, Quit, new Color(0.75f, 0.28f, 0.12f, 0.9f));
            root.gameObject.SetActive(false);
        }

        public override void Refresh()
        {
            // 대기실에서는 시작 위치·낚시터 나가기가 필요 없다
            bool field = Game.Mode == GameMode.Field;
            _start.interactable = field;
            _exit.interactable = field;
        }

        static void Quit()
        {
            Game.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
