using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fishing.Core
{
    /// <summary>
    /// 게임패드 진동 (원작 후기 요청 사항). 새 Input System의 Gamepad로만 진동을 준다 — 조작은 예전 Input Manager 그대로
    /// (Project Settings > Active Input Handling = Both). 패드가 없거나 옵션에서 끄면 아무것도 안 한다.
    ///  - 입질(본신): 짧게 톡, 챔질 성공: 세게 한 번, 질주·폭주: 길게 웅, 카운터: 강하게 짧게, 줄 끊김: 크게, 랜딩: 경쾌하게
    /// </summary>
    public class Rumble : MonoBehaviour
    {
        static Rumble _instance;
        float _until;
        float _low, _high;
        float _hold;   // 지속 진동 (장력 등): 매 프레임 갱신해야 유지된다
        float _holdUntil;

        public static void Ensure()
        {
            if (_instance != null) return;
            _instance = new GameObject("Rumble").AddComponent<Rumble>();
            DontDestroyOnLoad(_instance.gameObject);
        }

        /// <summary>짧은 진동. low = 저주파(묵직), high = 고주파(찌릿).</summary>
        public static void Pulse(float low, float high, float seconds)
        {
            if (_instance == null || !Settings.Vibration) return;
            _instance._low = Mathf.Max(_instance._low, low);
            _instance._high = Mathf.Max(_instance._high, high);
            _instance._until = Mathf.Max(_instance._until, Time.unscaledTime + seconds);
        }

        /// <summary>계속 떨림 (장력이 높을 때 등). 부르지 않으면 곧 멈춘다.</summary>
        public static void Hold(float amount)
        {
            if (_instance == null || !Settings.Vibration) return;
            _instance._hold = Mathf.Clamp01(amount);
            _instance._holdUntil = Time.unscaledTime + 0.15f;
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return;
            float t = Time.unscaledTime;
            float hold = t < _holdUntil ? _hold : 0f;
            float low = t < _until ? _low : 0f;
            float high = t < _until ? _high : 0f;
            if (t >= _until) { _low = 0f; _high = 0f; }
            low = Mathf.Max(low, hold * 0.35f);
            high = Mathf.Max(high, hold * 0.2f);
            if (!Settings.Vibration) low = high = 0f;
            pad.SetMotorSpeeds(low, high);
#endif
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
#endif
        }
    }
}
