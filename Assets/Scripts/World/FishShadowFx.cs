using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 물속 그림자 실루엣 (<see cref="SchoolKind.Shadow"/>). 물이 불투명이라 수면 바로 위에 납작한 띠를 깔고
    /// 위에서 본 물고기 모양(가장자리가 흐릿한 텍스처)을 검게 그린다. 띠는 머리 → 꼬리로 갈수록 크게 S자로 휘어
    /// 헤엄치는 꼬리짓이 되고, 빨리 갈수록 꼬리를 빨리 친다. 나타나고 사라질 때는 2초에 걸쳐 짙어지고 옅어진다.
    /// </summary>
    public class FishShadowFx : MonoBehaviour
    {
        const int Segments = 16;
        const float FadeSeconds = 2f, Lift = 0.16f;

        Mesh _mesh;
        Material _mat;
        Vector3[] _verts;
        bool _visible;
        float _alpha, _length = 1.5f, _phase, _speed;

        /// <summary>그림자 길이(m): 그 어종 최대 길이보다 크게 (물에 번져 보이고, 비스듬히 내려다봐도 멀리서 눈에 띄게). 2.2~4.5m.</summary>
        public static float LengthFor(FishSpecies s) => Mathf.Clamp(s.MaxLen / 100f * 2f, 2.2f, 4.5f);

        public static FishShadowFx Create()
        {
            var fx = new GameObject("FishShadow").AddComponent<FishShadowFx>();
            fx._mesh = new Mesh { name = "FishShadow" };
            fx._mesh.MarkDynamic();
            fx._verts = new Vector3[(Segments + 1) * 2];
            var uv = new Vector2[fx._verts.Length];
            var tris = new int[Segments * 6];
            for (int i = 0; i <= Segments; i++)
            {
                float v = i / (float)Segments; // 0 꼬리 끝 ~ 1 머리 끝
                uv[i * 2] = new Vector2(0f, v);
                uv[i * 2 + 1] = new Vector2(1f, v);
                if (i == Segments) continue;
                int t = i * 6, a = i * 2;
                tris[t] = a; tris[t + 1] = a + 2; tris[t + 2] = a + 1;
                tris[t + 3] = a + 1; tris[t + 4] = a + 2; tris[t + 5] = a + 3;
            }
            fx.Bend(0f);
            fx._mesh.uv = uv;
            fx._mesh.triangles = tris;
            fx._mesh.bounds = new Bounds(Vector3.zero, new Vector3(8f, 1f, 8f));
            fx.gameObject.AddComponent<MeshFilter>().sharedMesh = fx._mesh;
            var r = fx.gameObject.AddComponent<MeshRenderer>();
            fx._mat = new Material(Shader.Find("Fishing/FishShadow")) { mainTexture = ProceduralTextures.FishShadow };
            r.sharedMaterial = fx._mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            fx._phase = Random.value * 10f;
            return fx;
        }

        /// <summary>
        /// spot이 null이면 사라진다. heading은 헤엄치는 방향, speed(m/s)로 꼬리짓 빠르기를 정한다.
        /// 숨어 있다가 다시 나타나면 옛 자리에서 미끄러져 오지 않고 새 자리에서 떠오른다.
        /// </summary>
        public void Show(Vector3? spot, Vector3 heading, float speed, float length)
        {
            _visible = spot.HasValue;
            _speed = speed;
            _length = length;
            if (!_visible) return;
            transform.position = new Vector3(spot.Value.x, FieldMap.WaterY + Lift, spot.Value.z);
            if (heading.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(new Vector3(heading.x, 0f, heading.z));
        }

        /// <summary>띠를 S자로 휜다: 머리는 거의 그대로, 꼬리로 갈수록 크게 좌우로 (물고기 꼬리짓).</summary>
        void Bend(float swing)
        {
            float width = _length * 0.36f;
            for (int i = 0; i <= Segments; i++)
            {
                float v = i / (float)Segments;
                float tail = 1f - v;
                float side = swing * tail * tail * _length * 0.16f * Mathf.Sin(_phase - v * 4.2f);
                float z = (v - 0.5f) * _length;
                _verts[i * 2] = new Vector3(side - width * 0.5f, 0f, z);
                _verts[i * 2 + 1] = new Vector3(side + width * 0.5f, 0f, z);
            }
            _mesh.vertices = _verts;
        }

        void Update()
        {
            _alpha = Mathf.MoveTowards(_alpha, _visible ? 1f : 0f, Time.deltaTime / FadeSeconds);
            var r = GetComponent<MeshRenderer>();
            r.enabled = _alpha > 0f;
            if (!r.enabled) return;
            // 느릴 땐 천천히 살랑, 빠를 땐 힘차게 (0.55m/s에서 초당 1.6번쯤)
            _phase += Time.deltaTime * (2.2f + _speed * 14f);
            Bend(0.55f + Mathf.Clamp01(_speed / 0.55f) * 0.45f);
            // 숨 쉬듯 조금 짙어졌다 옅어진다 (깊이가 바뀌는 느낌)
            float depth = 0.88f + 0.12f * Mathf.Sin(Time.time * 0.6f + _phase * 0.05f);
            _mat.SetColor("_Color", new Color(0f, 0.015f, 0.025f, 0.86f * _alpha * depth));
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
