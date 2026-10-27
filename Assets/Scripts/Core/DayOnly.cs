using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 낮에만 보이는 것 (센토사 열기구): DayNightCycle이 올리는 전역 _FishingNight가 0.5를 넘으면 자식 렌더러를 끈다.
    /// 밤에 불 꺼진 시커먼 풍선이 빌딩 불빛 앞에 떠 있으면 이상했다 (2026-10-27 사용자 지적).
    /// </summary>
    public class DayOnly : MonoBehaviour
    {
        static readonly int Night = Shader.PropertyToID("_FishingNight");
        Renderer[] _rs;
        bool _shown = true;
        float _next;

        void Start() => _rs = GetComponentsInChildren<Renderer>(true);

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 1f;
            bool show = Shader.GetGlobalFloat(Night) < 0.5f;
            if (show == _shown) return;
            _shown = show;
            foreach (var r in _rs) if (r != null) r.enabled = show;
        }
    }
}
