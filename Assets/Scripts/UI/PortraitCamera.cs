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
        /// <summary>HUD 초상화(얼굴).</summary>
        public static RenderTexture Texture { get; private set; }
        /// <summary>의상 창 전신 미리보기.</summary>
        public static PortraitCamera FullBody { get; private set; }
        public RenderTexture Target { get; private set; }

        Camera _cam;
        Light _key, _rim;
        bool _full;
        /// <summary>미리보기를 빙글 돌려 보는 각도.</summary>
        public float Spin;

        public static PortraitCamera Create()
        {
            var go = new GameObject("PortraitCamera");
            return go.AddComponent<PortraitCamera>();
        }

        /// <summary>의상 창 전신 미리보기 카메라 (창이 열려 있을 때만 그린다).</summary>
        public static PortraitCamera CreateFullBody()
        {
            var go = new GameObject("FullBodyCamera");
            go.SetActive(false);
            var pc = go.AddComponent<PortraitCamera>();
            pc._full = true;
            go.SetActive(true);
            FullBody = pc;
            pc.SetActive(false);
            return pc;
        }

        public void SetActive(bool on) => _cam.enabled = on;

        void Awake()
        {
            Target = _full ? new RenderTexture(420, 640, 16) { name = "FullBody", antiAliasing = 2 }
                           : new RenderTexture(256, 256, 16) { name = "Portrait", antiAliasing = 2 };
            if (!_full) Texture = Target;
            _cam = gameObject.AddComponent<Camera>();
            _cam.cullingMask = 1 << Layer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.14f, 0.22f, 0.28f);
            _cam.fieldOfView = 24f;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 6f;
            _cam.targetTexture = Target;
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
            if (_full)
            {
                // 전신: 발끝부터 모자까지, 천천히 돌려 볼 수 있다
                var rot = Quaternion.Euler(0f, Spin, 0f);
                var center = player.Model.position + Vector3.up * 0.95f;
                var dir = rot * fwd;
                var camPos = center + dir * 3.3f + Vector3.up * 0.25f;
                transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(center - camPos));
                _cam.fieldOfView = 34f;
                _key.transform.position = center + dir * 2.5f + rot * right * 1.2f + Vector3.up * 1.5f;
                _rim.transform.position = center - dir * 1.5f + Vector3.up * 1.2f;
                _key.range = _rim.range = 6f;
                return;
            }
            // 정면에서 살짝 오른쪽, 눈높이에서 비춘다
            var pos = head + fwd * 1.15f + right * 0.28f + Vector3.up * 0.04f;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(head - Vector3.up * 0.04f - pos));
            _key.transform.position = head + fwd * 0.9f + right * 0.5f + Vector3.up * 0.5f;
            _rim.transform.position = head - fwd * 0.6f - right * 0.5f + Vector3.up * 0.3f;
        }
    }
}
