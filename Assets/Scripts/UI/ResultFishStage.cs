using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 결과 화면의 3D 물고기 (원작: 물 위로 튀어 오르는 실제 물고기).
    /// 월드 아래 멀리 떨어진 곳에 물고기 모델을 두고, 전용 레이어만 비추는 카메라로 투명 렌더 텍스처에 그린다.
    /// 메인 카메라는 이 레이어를 보지 않는다.
    /// </summary>
    public class ResultFishStage : MonoBehaviour
    {
        public const int Layer = 9;
        static readonly Vector3 StagePos = new Vector3(0f, -800f, 0f);
        const float ShownLength = 1.6f;   // 화면에 보일 몸길이(유닛). 실제 cm과 상관없이 크게

        public RenderTexture Target { get; private set; }
        /// <summary>지금 무대에 올린 물고기 (없으면 null).</summary>
        public GameObject Fish => _fish;
        Camera _cam;
        Transform _pivot;
        GameObject _fish;

        public static ResultFishStage Create() => Create(StagePos, "ResultFishStage");

        /// <summary>다른 곳(도감 미리보기)에서 쓸 무대. 서로 멀리 떨어뜨려야 카메라에 다른 무대가 안 비친다.</summary>
        public static ResultFishStage Create(Vector3 pos, string name)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            return go.AddComponent<ResultFishStage>();
        }

        /// <summary>도감: 옆모습을 보여 주며 천천히 돈다. t = 열린 뒤 시간(초).</summary>
        public void Spin(float t)
        {
            if (_fish == null) return;
            _pivot.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 8f, -90f + t * 24f, 0f);
        }

        void Awake()
        {
            Target = new RenderTexture(1024, 640, 16) { name = "ResultFish", antiAliasing = 4 };
            var camGo = new GameObject("ResultFishCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -2.6f);
            _cam = camGo.AddComponent<Camera>();
            _cam.cullingMask = 1 << Layer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.fieldOfView = 34f;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 10f;
            _cam.targetTexture = Target;
            _cam.allowHDR = false;
            _cam.depth = -9;
            _cam.enabled = false;

            Light(new Vector3(-1.2f, 1.6f, -1.8f), new Color(1f, 0.96f, 0.88f), 2.2f);
            Light(new Vector3(1.6f, 0.6f, 1.2f), new Color(0.5f, 0.75f, 1f), 1.6f);
            Light(new Vector3(0.3f, -1.4f, -1.2f), new Color(0.6f, 0.8f, 0.9f), 0.8f);

            _pivot = new GameObject("Pivot").transform;
            _pivot.SetParent(transform, false);

            // 메인 카메라는 이 무대를 보지 않는다
            if (Game.CameraRig != null) Game.CameraRig.Cam.cullingMask &= ~(1 << Layer);
        }

        void Light(Vector3 local, Color color, float intensity)
        {
            var l = new GameObject("StageLight").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = local;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = 6f;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << Layer;
        }

        /// <summary>물고기를 바꿔 끼우고 카메라를 켠다. 잡동사니면 false (2D 그림을 쓴다).</summary>
        public bool Show(FishSpecies species, float lengthCm)
        {
            if (_fish != null) Destroy(_fish);
            _fish = null;
            if (species == null || species.IsJunk) { _cam.enabled = false; return false; }

            _fish = FishModels.Create(species, lengthCm, _pivot);
            foreach (var t in _fish.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            FishModels.Play(_fish, FishModels.Flop, 0.9f, 0f); // 물 밖에서 펄떡
            // 길이를 재서 화면 크기에 맞춘다 (가장 긴 축 = 몸길이)
            var b = FishModels.BakedBounds(_fish);
            float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (longest > 1e-4f) _fish.transform.localScale *= ShownLength / longest;
            b = FishModels.BakedBounds(_fish);
            _fish.transform.position -= b.center - _pivot.position;
            _cam.enabled = true;
            return true;
        }

        public void Hide()
        {
            _cam.enabled = false;
            if (_fish != null) Destroy(_fish);
            _fish = null;
        }

        /// <summary>튀어 오르는 자세: 머리를 왼쪽 위로 들고 몸을 비튼다. t = 결과 화면이 뜬 뒤 시간(초).</summary>
        public void Pose(float t)
        {
            if (_fish == null) return;
            // 모델의 머리 방향은 +Z. 화면 왼쪽(-X)을 보게 돌리고 머리를 25° 든다
            float twist = Mathf.Sin(t * 3.1f) * 9f;
            float arch = Mathf.Sin(t * 2.2f) * 6f;
            _pivot.localRotation = Quaternion.Euler(0f, 0f, -24f + arch) * Quaternion.Euler(twist, -90f + Mathf.Sin(t * 1.7f) * 12f, 0f);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
