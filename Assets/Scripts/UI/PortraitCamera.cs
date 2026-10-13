using Fishing.Core;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// HUD 왼쪽 위 초상화. 내 캐릭터 얼굴만 비추는 카메라를 하나 더 두고 렌더 텍스처로 그린다.
    /// 캐릭터 모델은 <see cref="Layer"/> 레이어에 있고, 이 카메라와 전용 조명은 그 레이어만 본다.
    /// 그래서 옷·모자·머리를 바꾸면 초상화도 그대로 바뀌고, 대기 애니메이션으로 살짝 움직인다.
    /// </summary>
    public class PortraitCamera : MonoBehaviour
    {
        public const int Layer = 8;
        public static RenderTexture Texture { get; private set; }

        Camera _cam;
        Light _key, _rim;

        public static PortraitCamera Create()
        {
            var go = new GameObject("PortraitCamera");
            return go.AddComponent<PortraitCamera>();
        }

        void Awake()
        {
            Texture = new RenderTexture(256, 256, 16) { name = "Portrait", antiAliasing = 2 };
            _cam = gameObject.AddComponent<Camera>();
            _cam.cullingMask = 1 << Layer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.14f, 0.22f, 0.28f);
            _cam.fieldOfView = 24f;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 6f;
            _cam.targetTexture = Texture;
            _cam.depth = -10;
            _cam.allowHDR = false;

            _key = MakeLight("PortraitKey", new Color(1f, 0.95f, 0.88f), 1.6f);
            _rim = MakeLight("PortraitRim", new Color(0.55f, 0.75f, 1f), 1.1f);
        }

        Light MakeLight(string name, Color color, float intensity)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = 3f;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << Layer;
            return l;
        }

        void LateUpdate()
        {
            var player = Game.Player;
            if (player == null || player.Head == null) return;
            var head = player.Head.position + Vector3.up * 0.06f;
            var fwd = player.Model.forward;
            var right = player.Model.right;
            // 정면에서 살짝 오른쪽, 눈높이에서 비춘다
            var pos = head + fwd * 1.15f + right * 0.28f + Vector3.up * 0.04f;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(head - Vector3.up * 0.04f - pos));
            _key.transform.position = head + fwd * 0.9f + right * 0.5f + Vector3.up * 0.5f;
            _rim.transform.position = head - fwd * 0.6f - right * 0.5f + Vector3.up * 0.3f;
        }
    }
}
