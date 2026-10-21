using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 둥근 상자 UI 도형 (셰이더 Fishing/UIShape). 모서리 반지름·가장자리 광택·안쪽 테두리와 바깥 빛·그림자를 한 장의 메쉬로 그리고,
    /// Glass면 뒤 화면을 흐리게 비춘다(<see cref="UIBlurSource"/>). Image를 이어받아 버튼 targetGraphic·마스크·레이캐스트가 그대로 된다.
    /// 그림자·빛이 들어갈 만큼 사각형 바깥으로 메쉬를 넓힌다. 2026-10-21까지는 Translucent Image + Paraform(에셋 스토어)이었다.
    /// </summary>
    public class UIShape : Image
    {
        float _radius = 5f, _bevel = 6f, _opacity = 0.62f;
        Color _borderColor = Color.clear;
        float _borderWidth, _borderGlow;
        Vector2 _shadowOffset;
        float _shadowSoftness, _shadowAlpha;
        Texture _raw;

        /// <summary>모서리 반지름 (크면 알약·원).</summary>
        public float Radius { get => _radius; set { _radius = value; SetVerticesDirty(); } }
        /// <summary>가장자리 광택 폭 (0이면 없음).</summary>
        public float Bevel { get => _bevel; set { _bevel = value; SetVerticesDirty(); } }
        /// <summary>유리 판: 판 색(color)이 흐린 배경을 덮는 진하기 0~1.</summary>
        public float foregroundOpacity { get => _opacity; set { _opacity = value; SetVerticesDirty(); } }
        /// <summary>true면 뒤 화면이 흐리게 비친다 (재질이 유리용이어야 한다 — UIFactory.Glass).</summary>
        public bool IsGlass { get; set; }

        /// <summary>텍스처 모드: 스프라이트 대신 이 텍스처를 사각형 전체에 (초상화·지도).</summary>
        public Texture rawTexture
        {
            get => _raw;
            set { _raw = value; SetMaterialDirty(); SetVerticesDirty(); }
        }

        public override Texture mainTexture => _raw != null ? _raw : base.mainTexture;

        public void SetBorder(Color color, float width, float glow)
        {
            _borderColor = color;
            _borderWidth = width;
            _borderGlow = glow;
            SetVerticesDirty();
        }

        public void SetShadow(float softness, Vector2 offset, float alpha)
        {
            _shadowSoftness = softness;
            _shadowOffset = offset;
            _shadowAlpha = alpha;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var hb = r.size * 0.5f;
            if (hb.x <= 0f || hb.y <= 0f) return;
            float shadowReach = _shadowAlpha > 0f ? _shadowSoftness + Mathf.Max(Mathf.Abs(_shadowOffset.x), Mathf.Abs(_shadowOffset.y)) : 0f;
            float margin = Mathf.Max(shadowReach, _borderColor.a > 0f ? _borderGlow : 0f) + 2f;
            var shape = new Vector4(0f, 0f, hb.x, hb.y);
            var p2 = new Vector4(Mathf.Min(_radius, Mathf.Min(hb.x, hb.y)), _bevel, _borderColor.a > 0f ? _borderWidth : 0f, _borderGlow);
            var p3 = new Vector4(_shadowOffset.x, _shadowOffset.y, _shadowSoftness, _shadowAlpha);
            var border = new Vector4(_borderColor.r, _borderColor.g, _borderColor.b, _borderColor.a);
            var glass = new Vector3(IsGlass ? _opacity : 0f, 0f, 0f);
            Color32 c = color;
            for (int k = 0; k < 4; k++)
            {
                float sx = k == 0 || k == 1 ? -1f : 1f;
                float sy = k == 0 || k == 3 ? -1f : 1f;
                var local = new Vector2(sx * (hb.x + margin), sy * (hb.y + margin));
                shape.x = local.x;
                shape.y = local.y;
                var v = new UIVertex
                {
                    position = r.center + local,
                    color = c,
                    uv0 = new Vector4(local.x / (hb.x * 2f) + 0.5f, local.y / (hb.y * 2f) + 0.5f, 0f, 0f),
                    uv1 = shape,
                    uv2 = p2,
                    uv3 = p3,
                    normal = glass,
                    tangent = border,
                };
                vh.AddVert(v);
            }
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
