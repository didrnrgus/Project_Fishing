using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 해상도를 FHD(1920×1080) 이하로 묶어 둔다. 시작할 때 저장된 해상도를 반영하고,
    /// Alt+Enter·창 최대화 따위로 그리는 크기가 FHD를 넘으면 다시 FHD로 돌린다
    /// (전체 화면이면 FHD로 그려서 모니터에 맞게 늘린다).
    /// </summary>
    public class ResolutionGuard : MonoBehaviour
    {
        float _nextCheck;

        void Start()
        {
            if (Application.isEditor) return; // 에디터 Game 뷰 크기는 건드리지 않는다
            Settings.ApplyResolution();
        }

        void Update()
        {
            if (Application.isEditor || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;
            if (!Settings.OverMax) return;
            Settings.Fullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
            Settings.ApplyResolution();
        }
    }
}
