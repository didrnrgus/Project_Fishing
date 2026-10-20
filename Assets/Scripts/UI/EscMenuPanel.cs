using Fishing.Core;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// ESC 메뉴 (원작 cpmw_lobby_ship.mp4 2:50, 1920×1080 기준으로 잰 자리):
    ///  - 뒤 게임 화면은 뿌옇게 흐려지고 조금 어두워진다 (HUD는 그대로 보인다).
    ///  - 화면 가운데 폭 256px 세로 띠가 위에서 아래까지: 검은 반투명, 위·아래가 더 진하고 양옆에 가는 회색 세로 선.
    ///  - 띠 안에 228×48 남색 금속 버튼(하늘색 빛 테두리, 흰 한글 + 작은 영문 대문자):
    ///    게임으로 돌아가기 RESUME GAME — 한 칸 띄우고 — 옵션 OPTION / 시작 위치로 가기 STARTING POINT / 낚시터 나가기 EXIT FISHING AREA (/ 서버선택)
    ///    가 6px 간격으로 붙어 있고, 그 아래 옅은 회색 글씨로 칭호·캐릭터 이름 두 줄, 맨 아래 주황 판 게임 종료 EXIT GAME.
    /// 원작의 서버선택은 솔로판에 없어서 뺐다. 열린 창이 있으면 ESC는 창을 닫고, 아무 창도 없을 때 이 메뉴를 연다.
    /// </summary>
    public class EscMenuPanel : UIPanel
    {
        readonly UnityEngine.UI.Button _start, _exit;
        readonly TMP_Text _who;

        public EscMenuPanel(Transform canvas, System.Action options, System.Action startPoint, System.Action exitField)
        {
            // 화면 전체: 흐림 + 살짝 어둡게 (클릭을 막는다)
            var root = UIFactory.BlurBackdrop(canvas, "EscMenu", 0.22f);
            SetRoot(root.gameObject);

            // 가운데 세로 띠 + 양옆 가는 선
            var band = UIFactory.Image(root.transform, Color.white, BandSprite, "Band");
            band.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            band.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            band.rectTransform.anchoredPosition = Vector2.zero;
            band.rectTransform.sizeDelta = new Vector2(256, 0);
            foreach (int side in new[] { -1, 1 })
            {
                var line = UIFactory.Image(root.transform, new Color(0.72f, 0.76f, 0.8f, 0.45f), null, "Edge");
                line.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                line.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                line.rectTransform.anchoredPosition = new Vector2(side * 128, 0);
                line.rectTransform.sizeDelta = new Vector2(1.5f, 0);
            }

            UnityEngine.UI.Button Btn(string ko, string en, float y, System.Action act, bool red = false)
            {
                var b = UIFactory.ClassicButton(root.transform, ko, en, () => act(), red, 20);
                ((RectTransform)b.transform).At(new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(228, 48));
                return b;
            }
            // 원작 간격: 돌아가기와 아래 묶음 사이 36px, 묶음 안 6px (버튼 48 + 6 = 54 간격)
            Btn("게임으로 돌아가기", "RESUME GAME", 164, Close);
            Btn("옵션", "OPTION", 80, () => { Close(); options(); });
            _start = Btn("시작 위치로 가기", "STARTING POINT", 26, () => { Close(); startPoint(); });
            _exit = Btn("낚시터 나가기", "EXIT FISHING AREA", -28, () => { Close(); exitField(); });

            // 원작: 버튼 묶음과 게임 종료 사이에 칭호·이름이 옅게 (예: "수중 탐사대원 / 이오리와코의계절")
            _who = UIFactory.Text(root.transform, "", 13, new Color(0.62f, 0.64f, 0.67f, 0.75f), TextAnchor.MiddleCenter);
            _who.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -71), new Vector2(240, 36));
            _who.lineSpacing = -8f;

            Btn("게임 종료", "EXIT GAME", -114, Quit, red: true);
            root.gameObject.SetActive(false);
        }

        public override void Refresh()
        {
            // 대기실에서는 시작 위치·낚시터 나가기가 필요 없다
            bool field = Game.Mode == GameMode.Field;
            _start.interactable = field;
            _exit.interactable = field;
            var p = Game.Profile;
            string name = p != null ? p.BuildLook().Name : "";
            _who.text = p == null ? "" : $"{p.CurrentTitle}\n<size=15>{name}</size>";
        }

        static Sprite _band;

        /// <summary>세로 띠: 검은 반투명, 위·아래 끝으로 갈수록 진하고 가운데(버튼 자리)는 조금 옅다. 양옆 몇 px은 더 진하다.</summary>
        static Sprite BandSprite
        {
            get
            {
                if (_band != null) return _band;
                const int w = 32, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f); // 0 = 가운데, 1 = 위·아래 끝
                    float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float a = Mathf.Lerp(0.5f, 0.82f, v * v) + (u > 0.88f ? 0.08f : 0f);
                    px[y * w + x] = new Color(0.02f, 0.025f, 0.03f, a);
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _band = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            }
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
