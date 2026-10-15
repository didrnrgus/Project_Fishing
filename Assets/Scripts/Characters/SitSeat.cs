using Fishing.Core;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 앉은 동작(운전·앉아 쉬기)일 때 엉덩이 밑에 낚시꾼 아이스박스를 깔아 준다. 앉는 동작은 발 기준이라
    /// 그대로 두면 배 갑판·땅 위 허공에 앉아 보인다. 매 프레임 애니메이션이 끝난 뒤(LateUpdate) 골반 뼈 높이를 재서
    /// 상자 높이(발바닥 ~ 엉덩이 밑)와 앞뒤 자리를 맞춘다. 기준 frame은 캐릭터 모델 (모델의 발이 y=0).
    /// </summary>
    public class SitSeat : MonoBehaviour
    {
        /// <summary>켜면 상자가 나타나 엉덩이를 받친다.</summary>
        public bool Active;

        Transform _frame, _pelvis, _box;
        float _h = -1f;
        Vector3 _at;

        /// <summary>frame = 발이 y=0인 캐릭터 모델 루트. 골반 뼈(pelvis)가 없으면 붙이지 않는다.</summary>
        public static SitSeat Attach(Transform frame)
        {
            Transform pelvis = null;
            foreach (var t in frame.GetComponentsInChildren<Transform>(true))
                if (t.name == "pelvis") { pelvis = t; break; }
            if (pelvis == null) return null;
            var s = frame.gameObject.AddComponent<SitSeat>();
            s._frame = frame;
            s._pelvis = pelvis;
            s.Build();
            return s;
        }

        void Build()
        {
            // 흔한 낚시용 아이스박스: 흰 몸통 + 파란 뚜껑 + 손잡이. 높이 1 단위로 만들고 y 스케일로 늘린다
            _box = new GameObject("SeatCooler").transform;
            _box.SetParent(_frame, false);
            var white = Mats.Lit(new Color(0.92f, 0.93f, 0.9f), 0.45f);
            var blue = Mats.Lit(new Color(0.15f, 0.42f, 0.75f), 0.5f);
            var grey = Mats.Lit(new Color(0.35f, 0.37f, 0.4f), 0.3f);
            Mats.Prim(PrimitiveType.Cube, "Body", _box, new Vector3(0f, 0.43f, 0f), new Vector3(1f, 0.86f, 1f), white, false);
            Mats.Prim(PrimitiveType.Cube, "Lid", _box, new Vector3(0f, 0.93f, 0f), new Vector3(1.04f, 0.14f, 1.04f), blue, false);
            Mats.Prim(PrimitiveType.Cube, "Band", _box, new Vector3(0f, 0.2f, 0f), new Vector3(1.01f, 0.06f, 1.01f), blue, false);
            foreach (int side in new[] { -1, 1 })
                Mats.Prim(PrimitiveType.Cube, "Handle", _box, new Vector3(side * 0.53f, 0.75f, 0f), new Vector3(0.06f, 0.08f, 0.45f), grey, false);
            _box.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_box == null) return;
            if (!Active)
            {
                if (_box.gameObject.activeSelf) _box.gameObject.SetActive(false);
                _h = -1f;
                return;
            }
            // 골반 중심에서 엉덩이 살까지 약 0.12m. 너무 낮거나(서 있는 중 전환) 높으면 상자를 두지 않는다
            var p = _frame.InverseTransformPoint(_pelvis.position);
            float h = p.y - 0.12f;
            bool ok = h > 0.12f && h < 0.75f;
            if (_box.gameObject.activeSelf != ok) _box.gameObject.SetActive(ok);
            if (!ok) { _h = -1f; return; }
            // 숨 쉬는 들썩임에 상자가 따라 떨지 않게 천천히 맞춘다 (처음엔 바로)
            var at = new Vector3(0f, 0f, p.z - 0.04f);
            if (_h < 0f) { _h = h; _at = at; }
            else
            {
                float k = 1f - Mathf.Exp(-4f * Time.deltaTime);
                _h = Mathf.Lerp(_h, h, k);
                _at = Vector3.Lerp(_at, at, k);
            }
            _box.localPosition = _at;
            _box.localRotation = Quaternion.identity;
            _box.localScale = new Vector3(0.5f, _h, 0.36f);
        }
    }
}
