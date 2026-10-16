using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 화면 전체를 잠깐 하얗게(또는 다른 색으로) 덮었다가 걷는다. 장면을 끊어 넘길 때 컷을 감춘다
    /// (원작: 랜딩 성공 물보라가 화면을 덮은 뒤 3인칭 옆모습으로 넘어간다).
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {
        static ScreenFlash _instance;
        Image _image;
        Color _color;
        float _start, _hold, _fade;

        static ScreenFlash Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = UIFactory.CreateCanvas("ScreenFlash", 900);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<ScreenFlash>();
                _instance._image = UIFactory.Image(canvas.transform, Color.clear, null, "Flash");
                _instance._image.rectTransform.Fill();
                _instance._image.raycastTarget = false;
                _instance.enabled = false;
                return _instance;
            }
        }

        /// <summary>color로 hold초 덮었다가 fade초에 걸쳐 걷는다 (실제 시간 기준).</summary>
        public static void Flash(Color color, float hold, float fade)
        {
            var f = Instance;
            f._color = color;
            f._start = Time.unscaledTime;
            f._hold = hold;
            f._fade = Mathf.Max(0.01f, fade);
            f._image.color = color;
            f.enabled = true;
        }

        void Update()
        {
            float t = Time.unscaledTime - _start - _hold;
            var c = _color;
            c.a *= t <= 0f ? 1f : Mathf.Clamp01(1f - t / _fade);
            _image.color = c;
            if (t >= _fade) enabled = false;
        }
    }
}
