using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>키로 하는 일. 옵션 창에서 키를 바꿀 수 있다.</summary>
    public enum KeyAction
    {
        MoveForward, MoveBack, MoveLeft, MoveRight, Run, Jump,
        Talk, Boat, Retrieve,
        Quest, Dex, KeepNet, Inventory, Costume, AreaMap, Shop, Tips, Help, Save, Options,
        Focus,
    }

    /// <summary>
    /// 기기 설정 (세이브 파일이 아니라 PlayerPrefs에 저장): 음량, 마우스 감도, 화면, 후처리, 패드 진동, 키 배치.
    /// 원작 후기 요청: 게임패드 진동, 키 바꾸기. 로드맵 "옵션 창".
    /// </summary>
    public static class Settings
    {
        public static float MasterVolume = 1f;
        public static float EffectsVolume = 1f;
        /// <summary>시점 돌리기 감도 (오른쪽 버튼 드래그).</summary>
        public static float LookSensitivity = 1f;
        /// <summary>캐스팅·챔질·파이팅 마우스 감도. 밸런스에 영향이 있어 0.6~1.6 사이만.</summary>
        public static float FightSensitivity = 1f;
        public static bool InvertY;
        public static bool PostFx = true;
        public static bool Vibration = true;
        public static int Shadows = 2;   // 0 끔, 1 가까이만, 2 보통, 3 멀리
        public static int ResolutionIndex = -1;
        public static bool Fullscreen;

        public static event Action Changed;

        static readonly Dictionary<KeyAction, KeyCode> Defaults = new Dictionary<KeyAction, KeyCode>
        {
            { KeyAction.MoveForward, KeyCode.W }, { KeyAction.MoveBack, KeyCode.S }, { KeyAction.MoveLeft, KeyCode.A }, { KeyAction.MoveRight, KeyCode.D },
            { KeyAction.Run, KeyCode.LeftShift }, { KeyAction.Jump, KeyCode.Space },
            { KeyAction.Talk, KeyCode.F }, { KeyAction.Boat, KeyCode.B }, { KeyAction.Retrieve, KeyCode.R },
            // 원작 단축키: 퀘스트 L, 도감 P, 살림망 Z, 상점 F12
            { KeyAction.Quest, KeyCode.L }, { KeyAction.Dex, KeyCode.P }, { KeyAction.KeepNet, KeyCode.Z }, { KeyAction.Inventory, KeyCode.I },
            { KeyAction.Costume, KeyCode.C }, { KeyAction.AreaMap, KeyCode.M }, { KeyAction.Shop, KeyCode.F12 }, { KeyAction.Tips, KeyCode.U },
            { KeyAction.Help, KeyCode.F1 }, { KeyAction.Save, KeyCode.F5 }, { KeyAction.Options, KeyCode.O },
            { KeyAction.Focus, KeyCode.Q },
        };

        static readonly Dictionary<KeyAction, KeyCode> Bindings = new Dictionary<KeyAction, KeyCode>(Defaults);

        public static readonly (KeyAction action, string name)[] ActionNames =
        {
            (KeyAction.MoveForward, "앞으로"), (KeyAction.MoveBack, "뒤로"), (KeyAction.MoveLeft, "왼쪽"), (KeyAction.MoveRight, "오른쪽"),
            (KeyAction.Run, "달리기"), (KeyAction.Jump, "점프"), (KeyAction.Talk, "대화"), (KeyAction.Boat, "배 꺼내기/내리기"),
            (KeyAction.Retrieve, "찌 회수"), (KeyAction.Quest, "퀘스트"), (KeyAction.Dex, "도감"), (KeyAction.KeepNet, "살림망"),
            (KeyAction.Inventory, "가방"), (KeyAction.Costume, "의상"), (KeyAction.AreaMap, "지역 지도"), (KeyAction.Shop, "상점"),
            (KeyAction.Tips, "설명 켜기/끄기"), (KeyAction.Help, "도움말"), (KeyAction.Save, "저장"), (KeyAction.Options, "옵션"),
            (KeyAction.Focus, "포커싱 (파이팅)"),
        };

        /// <summary>바뀔 때마다 오른다 (메뉴 독 글자 갱신용).</summary>
        public static int Version { get; private set; }

        public static KeyCode Key(KeyAction a) => Bindings.TryGetValue(a, out var k) ? k : Defaults[a];
        public static bool Down(KeyAction a) => Input.GetKeyDown(Key(a));
        public static bool Held(KeyAction a) => Input.GetKey(Key(a));

        /// <summary>메뉴·안내문에 쓰는 키 이름 (LeftShift → Shift, Alpha1 → 1).</summary>
        public static string KeyName(KeyAction a) => KeyName(Key(a));

        public static string KeyName(KeyCode k) => k switch
        {
            KeyCode.LeftShift or KeyCode.RightShift => "Shift",
            KeyCode.LeftControl or KeyCode.RightControl => "Ctrl",
            KeyCode.LeftAlt or KeyCode.RightAlt => "Alt",
            KeyCode.Space => "Space",
            KeyCode.Return => "Enter",
            >= KeyCode.Alpha0 and <= KeyCode.Alpha9 => ((int)(k - KeyCode.Alpha0)).ToString(),
            _ => k.ToString(),
        };

        /// <summary>키를 바꾼다. 다른 일에 이미 쓰는 키면 서로 맞바꾼다.</summary>
        public static void Rebind(KeyAction a, KeyCode key)
        {
            foreach (var other in new List<KeyAction>(Bindings.Keys))
                if (other != a && Bindings[other] == key) Bindings[other] = Key(a);
            Bindings[a] = key;
            Save();
        }

        /// <summary>바꾸면 안 되는 키 (Esc는 창 닫기, 숫자 1~8은 미끼, 마우스는 낚시).</summary>
        public static bool Reserved(KeyCode k) =>
            k == KeyCode.Escape || (k >= KeyCode.Alpha1 && k <= KeyCode.Alpha8) || (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) || k == KeyCode.None;

        public static void ResetKeys()
        {
            Bindings.Clear();
            foreach (var kv in Defaults) Bindings[kv.Key] = kv.Value;
            Save();
        }

        // ---------------- 저장 / 불러오기 ----------------

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat("opt.master", 1f);
            EffectsVolume = PlayerPrefs.GetFloat("opt.sfx", 1f);
            LookSensitivity = PlayerPrefs.GetFloat("opt.look", 1f);
            FightSensitivity = PlayerPrefs.GetFloat("opt.fight", 1f);
            InvertY = PlayerPrefs.GetInt("opt.invertY", 0) == 1;
            PostFx = PlayerPrefs.GetInt("opt.postfx", 1) == 1;
            Vibration = PlayerPrefs.GetInt("opt.vibration", 1) == 1;
            Shadows = PlayerPrefs.GetInt("opt.shadow", 2);
            ResolutionIndex = PlayerPrefs.GetInt("opt.res", -1);
            Fullscreen = PlayerPrefs.GetInt("opt.fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            foreach (var kv in Defaults)
            {
                int code = PlayerPrefs.GetInt("key." + kv.Key, (int)kv.Value);
                Bindings[kv.Key] = Enum.IsDefined(typeof(KeyCode), code) ? (KeyCode)code : kv.Value;
            }
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("opt.master", MasterVolume);
            PlayerPrefs.SetFloat("opt.sfx", EffectsVolume);
            PlayerPrefs.SetFloat("opt.look", LookSensitivity);
            PlayerPrefs.SetFloat("opt.fight", FightSensitivity);
            PlayerPrefs.SetInt("opt.invertY", InvertY ? 1 : 0);
            PlayerPrefs.SetInt("opt.postfx", PostFx ? 1 : 0);
            PlayerPrefs.SetInt("opt.vibration", Vibration ? 1 : 0);
            PlayerPrefs.SetInt("opt.shadow", Shadows);
            PlayerPrefs.SetInt("opt.res", ResolutionIndex);
            PlayerPrefs.SetInt("opt.fullscreen", Fullscreen ? 1 : 0);
            foreach (var kv in Bindings) PlayerPrefs.SetInt("key." + kv.Key, (int)kv.Value);
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>설정을 게임에 반영한다 (음량, 그림자, 후처리, 해상도).</summary>
        public static void Apply()
        {
            Version++;
            if (!Application.isPlaying) return; // 에디터 테스트에서는 프로젝트 품질 설정을 건드리지 않는다
            AudioListener.volume = Mathf.Clamp01(MasterVolume);
            QualitySettings.shadows = Shadows == 0 ? ShadowQuality.Disable : ShadowQuality.All;
            QualitySettings.shadowDistance = Shadows switch { 0 => 0f, 1 => 40f, 2 => 90f, _ => 160f };
            if (Fishing.Core.PostFx.Instance != null) Fishing.Core.PostFx.Instance.Enabled = PostFx;
            Changed?.Invoke();
        }

        /// <summary>고를 수 있는 해상도 (같은 크기는 하나만, 작은 것부터).</summary>
        public static List<Resolution> Resolutions()
        {
            var list = new List<Resolution>();
            foreach (var r in Screen.resolutions)
                if (r.width >= 1280 && !list.Exists(x => x.width == r.width && x.height == r.height)) list.Add(r);
            if (list.Count == 0) list.Add(new Resolution { width = Screen.width, height = Screen.height });
            return list;
        }

        public static void ApplyResolution()
        {
            var list = Resolutions();
            if (ResolutionIndex < 0 || ResolutionIndex >= list.Count) { Screen.fullScreen = Fullscreen; return; }
            var r = list[ResolutionIndex];
            Screen.SetResolution(r.width, r.height, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
