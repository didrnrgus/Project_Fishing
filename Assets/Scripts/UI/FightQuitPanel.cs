using Fishing.Core;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 파이팅 중 ESC: "파이팅을 그만두시겠습니까?" 확인 창 (2026-10-30 사용자 요청).
    ///  - ESC 메뉴처럼 뒤 화면이 흐려지고, 가운데를 가로지르는 어두운 띠에 물음 + [계속하기] [그만두기].
    ///  - 열려 있는 동안 파이팅은 멈춘다 (FishingController.FightPaused). 닫으면(계속하기·ESC) 그대로 이어진다.
    ///  - 그만두기: 물고기를 놓친 것과 같다 (FightOutcome.GaveUp, 줄은 안 잃고 낚싯대·릴 내구도만 조금).
    /// </summary>
    public class FightQuitPanel : UIPanel
    {
        CursorLockMode _cursorLock;

        public FightQuitPanel(Transform canvas)
        {
            var root = UIFactory.BlurBackdrop(canvas, "FightQuit", 0.22f);
            SetRoot(root.gameObject);

            var band = UIFactory.Image(root.transform, new Color(0.02f, 0.03f, 0.05f, 0.92f), null, "Band").rectTransform;
            band.anchorMin = new Vector2(0, 0.5f);
            band.anchorMax = new Vector2(1, 0.5f);
            band.pivot = new Vector2(0.5f, 0.5f);
            band.anchoredPosition = new Vector2(0, 30);
            band.sizeDelta = new Vector2(0, 200);
            foreach (float y in new[] { 100f, -100f })
                UIFactory.Image(band, new Color(1f, 1f, 1f, 0.18f), null, "Edge").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(1100, 1));
            UIFactory.Text(band, "파이팅을 그만두시겠습니까?", 26, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .At(new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(800, 40), new Vector2(0.5f, 1));
            UIFactory.Text(band, "그만두면 물고기를 놓칩니다.  낚싯대 내구도 -1%  ·  릴 내구도 -2%", 16, UIFactory.TextDim, TextAnchor.MiddleCenter).rectTransform
                .At(new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(800, 28), new Vector2(0.5f, 1));
            var keep = UIFactory.ClassicButton(band, "계속하기", "CONTINUE", Close, false, 20);
            ((RectTransform)keep.transform).At(new Vector2(0.5f, 0), new Vector2(-130, 24), new Vector2(228, 48), new Vector2(0.5f, 0));
            var quit = UIFactory.ClassicButton(band, "그만두기", "GIVE UP", GiveUp, true, 20);
            ((RectTransform)quit.transform).At(new Vector2(0.5f, 0), new Vector2(130, 24), new Vector2(228, 48), new Vector2(0.5f, 0));
            root.gameObject.SetActive(false);
        }

        public override void Refresh() { }

        public override void Open()
        {
            if (IsOpen) return;
            base.Open();
            Game.Fishing.FightPaused = true;
            // 파이팅은 마우스를 잠가 둔다 → 버튼을 누를 수 있게 풀어 두었다가 계속하면 되돌린다
            _cursorLock = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Close()
        {
            if (!IsOpen) return;
            base.Close();
            Game.Fishing.FightPaused = false;
            if (Game.Fishing.State == Angling.FishingState.Fighting)
            {
                Cursor.lockState = _cursorLock;
                Cursor.visible = _cursorLock != CursorLockMode.Locked;
            }
        }

        void GiveUp()
        {
            base.Close();
            Game.Fishing.FightPaused = false;
            Game.Fishing.GiveUpFight();
        }
    }
}
