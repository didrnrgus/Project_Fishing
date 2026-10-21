using System;
using Fishing.Core;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 옵션 (O): 소리 · 마우스 · 화면 · 패드 진동 · 키 설정. 기기 설정이라 세이브가 아니라 PlayerPrefs에 저장된다(<see cref="Settings"/>).
    /// 키 설정: 바꿀 일을 누르고 새 키를 누른다 (Esc 취소). 이미 쓰는 키면 서로 맞바꾼다.
    /// </summary>
    public class OptionsPanel : UIPanel
    {
        /// <summary>새 키를 기다리는 중인 일 (없으면 null).</summary>
        public KeyAction? Waiting { get; private set; }

        public OptionsPanel(Transform canvas)
        {
            BuildFrame(canvas, "OptionsPanel", UIFactory.Caption("옵션", "OPTION"), new Vector2(1300, 820));
        }

        public override void Close()
        {
            Waiting = null;
            base.Close();
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(Body);
            var left = UIFactory.Node("Left", Body);
            left.anchorMin = new Vector2(0, 0);
            left.anchorMax = new Vector2(0.48f, 1);
            left.offsetMin = left.offsetMax = Vector2.zero;
            var right = UIFactory.Node("Right", Body);
            right.anchorMin = new Vector2(0.52f, 0);
            right.anchorMax = new Vector2(1, 1);
            right.offsetMin = right.offsetMax = Vector2.zero;

            var rows = new RowCursor(left);
            UIFactory.SectionTitle(rows.Next(30), "소리", 4, 20);
            Stepper(rows, "전체 음량", () => $"{Settings.MasterVolume * 100f:0}%", d => Settings.MasterVolume = Mathf.Clamp01(Settings.MasterVolume + d * 0.1f));
            Stepper(rows, "효과음", () => $"{Settings.EffectsVolume * 100f:0}%", d => Settings.EffectsVolume = Mathf.Clamp01(Settings.EffectsVolume + d * 0.1f));

            rows.Next(6);
            UIFactory.SectionTitle(rows.Next(30), "마우스", 4, 20);
            Stepper(rows, "시점 돌리기 감도", () => $"{Settings.LookSensitivity:0.0}", d => Settings.LookSensitivity = Mathf.Clamp(Settings.LookSensitivity + d * 0.1f, 0.3f, 3f));
            Stepper(rows, "낚시 조작 감도", () => $"{Settings.FightSensitivity:0.0}", d => Settings.FightSensitivity = Mathf.Clamp(Settings.FightSensitivity + d * 0.1f, 0.6f, 1.6f),
                "캐스팅·챔질·파이팅의 마우스 움직임");
            Toggle(rows, "시점 위아래 반전", () => Settings.InvertY, v => Settings.InvertY = v);

            rows.Next(6);
            UIFactory.SectionTitle(rows.Next(30), "화면", 4, 20);
            var resList = Settings.Resolutions();
            Stepper(rows, "해상도", () =>
            {
                if (Settings.ResolutionIndex < 0 || Settings.ResolutionIndex >= resList.Count) return $"{Screen.width}×{Screen.height}";
                var r = resList[Settings.ResolutionIndex];
                return $"{r.width}×{r.height}";
            }, d =>
            {
                int i = Settings.ResolutionIndex < 0 ? resList.FindIndex(r => r.width == Screen.width && r.height == Screen.height) : Settings.ResolutionIndex;
                Settings.ResolutionIndex = Mathf.Clamp((i < 0 ? 0 : i) + d, 0, resList.Count - 1);
                Settings.ApplyResolution();
            });
            Toggle(rows, "전체 화면", () => Settings.Fullscreen, v => { Settings.Fullscreen = v; Settings.ApplyResolution(); });
            Toggle(rows, "후처리 (블룸·색보정·AO)", () => Settings.PostFx, v => Settings.PostFx = v);
            Stepper(rows, "그림자", () => Settings.Shadows switch { 0 => "끔", 1 => "가까이만", 2 => "보통", _ => "멀리까지" },
                d => Settings.Shadows = Mathf.Clamp(Settings.Shadows + d, 0, 3));

            rows.Next(6);
            UIFactory.SectionTitle(rows.Next(30), "게임패드", 4, 20);
            Toggle(rows, "진동", () => Settings.Vibration, v => { Settings.Vibration = v; if (v) Rumble.Pulse(0.5f, 0.3f, 0.25f); },
                "입질·챔질·질주·카운터·줄 끊김·랜딩");

            // 키 설정
            var keys = new RowCursor(right);
            var head = keys.Next(30);
            UIFactory.SectionTitle(head, "키 설정", 4, 20);
            RowButton(head, "기본값으로", () => { Settings.ResetKeys(); Waiting = null; Refresh(); }, 440, 150, UIFactory.BtnNeutral, true, 15);
            Cell(keys.Next(24), Waiting != null ? "<color=#ffd060>새 키를 누르세요 (Esc 취소)</color>" : "바꿀 일을 누른 뒤 새 키를 누르세요. 마우스·숫자 1~6·Esc는 바꿀 수 없어요.",
                4, 600, 14, UIFactory.TextDim);
            var list = Settings.ActionNames;
            int half = (list.Length + 1) / 2;
            for (int i = 0; i < half; i++)
            {
                var row = keys.Next(32, 4);
                for (int c = 0; c < 2; c++)
                {
                    int k = i + c * half;
                    if (k >= list.Length) continue;
                    var (action, name) = list[k];
                    float x = c * 300f;
                    Cell(row, name, x + 4, 150, 16);
                    bool waiting = Waiting == action;
                    var act = action;
                    RowButton(row, waiting ? "…" : Settings.KeyName(action), () => { Waiting = act; Refresh(); }, x + 156, 130,
                        waiting ? UIFactory.BtnGold : UIFactory.BtnNeutral, true, 16);
                }
            }
        }

        /// <summary>키를 기다리는 중이면 눌린 키로 바꾼다 (GameUI.Update에서).</summary>
        public void Tick()
        {
            if (Waiting == null || !Input.anyKeyDown) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Waiting = null; Refresh(); return; }
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(k) || Settings.Reserved(k) || k >= KeyCode.JoystickButton0) continue;
                Settings.Rebind(Waiting.Value, k);
                Game.Toast($"키 변경: {Array.Find(Settings.ActionNames, a => a.action == Waiting.Value).name} = {Settings.KeyName(k)}", ToastKind.Good);
                Waiting = null;
                Refresh();
                return;
            }
        }

        void Stepper(RowCursor rows, string label, Func<string> value, Action<int> change, string hint = null)
        {
            var row = rows.Next(40, 4);
            UIFactory.RowBg(row);
            Cell(row, hint == null ? label : $"{label}  <size=13><color=#8090a0>{hint}</color></size>", 12, 330, 17);
            RowButton(row, "◀", () => { change(-1); Settings.Save(); Refresh(); }, 360, 46, UIFactory.BtnNeutral, true, 16);
            Cell(row, value(), 410, 120, 17, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            RowButton(row, "▶", () => { change(1); Settings.Save(); Refresh(); }, 534, 46, UIFactory.BtnNeutral, true, 16);
        }

        void Toggle(RowCursor rows, string label, Func<bool> get, Action<bool> set, string hint = null)
        {
            var row = rows.Next(40, 4);
            UIFactory.RowBg(row);
            Cell(row, hint == null ? label : $"{label}  <size=13><color=#8090a0>{hint}</color></size>", 12, 330, 17);
            bool on = get();
            RowButton(row, on ? "켜짐" : "꺼짐", () => { set(!get()); Settings.Save(); Refresh(); }, 360, 220, on ? UIFactory.BtnGood : UIFactory.BtnNeutral, true, 16);
        }
    }
}
