using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 유리 UI(<see cref="UIShape"/>, 셰이더 Fishing/UIShape) 뒤에 비치는 흐린 화면. 메인 카메라가 다 그린 화면을 1/4로 줄여
    /// 대각 4점 평균(Kawase)을 오프셋을 키워 가며 여러 번 거친 뒤 전역 텍스처 _UIBlurTex로 둔다. 화면 크기가 바뀌면 다시 만든다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UIBlurSource : MonoBehaviour
    {
        static readonly int BlurTex = Shader.PropertyToID("_UIBlurTex");
        static readonly int Offset = Shader.PropertyToID("_Offset");
        /// <summary>흐림 세기 (오프셋, 텍셀). 길수록 더 흐리다.</summary>
        static readonly float[] Passes = { 0f, 1f, 2f, 2f, 3f };

        Material _mat;
        RenderTexture _a, _b;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            Graphics.Blit(src, dst);
            if (_mat == null)
            {
                var shader = Shader.Find("Hidden/Fishing/UIBlur");
                if (shader == null) return;
                _mat = new Material(shader) { hideFlags = HideFlags.DontSave };
            }
            int w = Mathf.Max(16, src.width / 4), h = Mathf.Max(16, src.height / 4);
            Ensure(ref _a, w, h);
            Ensure(ref _b, w, h);
            var half = RenderTexture.GetTemporary(Mathf.Max(16, src.width / 2), Mathf.Max(16, src.height / 2), 0, RenderTextureFormat.ARGB32);
            _mat.SetFloat(Offset, 0f);
            Graphics.Blit(src, half, _mat, 0);
            Graphics.Blit(half, _a, _mat, 0);
            RenderTexture.ReleaseTemporary(half);
            var from = _a;
            var to = _b;
            foreach (var o in Passes)
            {
                _mat.SetFloat(Offset, o);
                Graphics.Blit(from, to, _mat, 0);
                (from, to) = (to, from);
            }
            Shader.SetGlobalTexture(BlurTex, from);
        }

        static void Ensure(ref RenderTexture rt, int w, int h)
        {
            if (rt != null && rt.width == w && rt.height == h) return;
            if (rt != null) rt.Release();
            rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "UIBlur", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            rt.Create();
        }

        void OnDestroy()
        {
            if (_a != null) _a.Release();
            if (_b != null) _b.Release();
            if (_mat != null) Destroy(_mat);
        }
    }
}
