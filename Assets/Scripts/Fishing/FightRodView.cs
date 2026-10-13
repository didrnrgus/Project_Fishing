using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 파이팅 중 1인칭 시점의 낚싯대와 손. 장력에 따라 휘고, 마우스로 좌우를 튼다.
    /// 물고기 위치에 물보라를 일으키고, 가까워지면 물고기 몸체가 수면에 보인다.
    /// </summary>
    public class FightRodView : MonoBehaviour
    {
        const int Segments = 9;
        const float SegLen = 0.2f;

        Transform _root;
        readonly Transform[] _segs = new Transform[Segments];
        Transform _tip;
        LineRenderer _line;
        GameObject _fish;
        Transform _fishBody;
        FishFight _fight;
        Vector3 _origin;
        Vector3 _forward;
        float _nextSplash;

        public static FightRodView Create(Camera cam)
        {
            var go = new GameObject("FightRodView");
            go.transform.SetParent(cam.transform, false);
            var view = go.AddComponent<FightRodView>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        void Build()
        {
            _root = new GameObject("RodRoot").transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0.28f, -0.42f, 0.55f);

            var rodMat = Mats.Lit(new Color(0.78f, 0.12f, 0.22f), 0.7f);
            var grip = Mats.Lit(new Color(0.15f, 0.15f, 0.15f), 0.3f);
            var skin = Mats.Lit(new Color(0.98f, 0.82f, 0.7f));
            var glove = Mats.Lit(new Color(0.95f, 0.95f, 0.95f));

            Mats.Prim(PrimitiveType.Cylinder, "Grip", _root, new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0.18f, 0.05f), grip, false);
            Mats.Prim(PrimitiveType.Cylinder, "Reel", _root, new Vector3(0.06f, 0f, 0f), new Vector3(0.11f, 0.03f, 0.11f),
                Mats.Lit(new Color(0.75f, 0.75f, 0.78f), 0.8f, 0.8f), false, Quaternion.Euler(0f, 0f, 90f));
            Mats.Prim(PrimitiveType.Sphere, "HandLow", _root, new Vector3(0f, -0.18f, -0.02f), new Vector3(0.11f, 0.12f, 0.11f), glove, false);
            Mats.Prim(PrimitiveType.Sphere, "HandHigh", _root, new Vector3(-0.02f, 0.05f, -0.03f), new Vector3(0.1f, 0.11f, 0.1f), glove, false);
            Mats.Prim(PrimitiveType.Capsule, "ArmL", _root, new Vector3(-0.12f, -0.3f, -0.2f), new Vector3(0.09f, 0.16f, 0.09f), skin, false, Quaternion.Euler(50f, 0f, -20f));
            Mats.Prim(PrimitiveType.Capsule, "ArmR", _root, new Vector3(0.1f, -0.38f, -0.15f), new Vector3(0.09f, 0.16f, 0.09f), skin, false, Quaternion.Euler(40f, 0f, 20f));

            Transform parent = _root;
            for (int i = 0; i < Segments; i++)
            {
                var seg = new GameObject($"Seg{i}").transform;
                seg.SetParent(parent, false);
                seg.localPosition = i == 0 ? Vector3.zero : new Vector3(0f, SegLen, 0f);
                float thick = Mathf.Lerp(0.028f, 0.008f, i / (float)(Segments - 1));
                Mats.Prim(PrimitiveType.Cylinder, "Blank", seg, new Vector3(0f, SegLen * 0.5f, 0f), new Vector3(thick, SegLen * 0.5f, thick), rodMat, false);
                _segs[i] = seg;
                parent = seg;
            }
            _tip = new GameObject("Tip").transform;
            _tip.SetParent(_segs[Segments - 1], false);
            _tip.localPosition = new Vector3(0f, SegLen, 0f);

            _line = new GameObject("Line").AddComponent<LineRenderer>();
            _line.transform.SetParent(transform, false);
            _line.useWorldSpace = true;
            _line.positionCount = 12;
            _line.widthMultiplier = 0.012f;
            _line.sharedMaterial = Mats.Unlit(new Color(0.85f, 0.95f, 0.85f));
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _fish = new GameObject("FightFish");
            _fishBody = Mats.Prim(PrimitiveType.Capsule, "Body", _fish.transform, Vector3.zero, Vector3.one,
                Mats.Lit(new Color(0.55f, 0.5f, 0.35f), 0.7f, 0.2f), false, Quaternion.Euler(0f, 0f, 90f)).transform;
            Mats.Prim(PrimitiveType.Cube, "Tail", _fishBody, new Vector3(0f, -0.62f, 0f), new Vector3(0.9f, 0.2f, 0.12f),
                Mats.Lit(new Color(0.5f, 0.45f, 0.3f)), false);
            _fish.SetActive(false);
        }

        public void Show(FishFight fight, Vector3 origin, Vector3 forward)
        {
            _fight = fight;
            _origin = origin;
            _forward = new Vector3(forward.x, 0f, forward.z).normalized;
            float s = Mathf.Clamp(fight.Length / 100f, 0.12f, 1.1f);
            _fishBody.localScale = new Vector3(s * 0.32f, s * 0.5f, s * 0.22f);
            gameObject.SetActive(true);
            _line.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _fight = null;
            gameObject.SetActive(false);
            _fish.SetActive(false);
        }

        /// <summary>물고기의 월드 위치. 거리와 좌우 위치(FishX)로 계산한다.</summary>
        public Vector3 FishWorldPos(FishFight f)
        {
            var right = Vector3.Cross(Vector3.up, _forward);
            var p = _origin + _forward * f.Distance + right * (f.FishX * f.Distance * 0.45f);
            p.y = HanRiverMap.WaterY;
            return p;
        }

        void LateUpdate()
        {
            if (_fight == null) return;
            var f = _fight;

            // 장력이 클수록 낚싯대가 앞으로 숙여지고 끝이 휜다
            float t = Mathf.Clamp01(f.Tension);
            _root.localRotation = Quaternion.Euler(42f + t * 18f, f.RodX * 28f, -f.RodX * 10f);
            float side = Mathf.Clamp(f.FishX - f.RodX * 0.5f, -1f, 1f);
            for (int i = 1; i < Segments; i++)
            {
                float k = i / (float)Segments;
                _segs[i].localRotation = Quaternion.Euler(t * 9f * k, 0f, -side * t * 5f * k);
            }

            var fishPos = FishWorldPos(f);
            var tipPos = _tip.position;
            float sag = Mathf.Lerp(0.8f, 0.02f, Mathf.Clamp01(f.Tension * 1.5f));
            for (int i = 0; i < _line.positionCount; i++)
            {
                float u = i / (float)(_line.positionCount - 1);
                var pos = Vector3.Lerp(tipPos, fishPos, u);
                pos.y -= Mathf.Sin(u * Mathf.PI) * sag;
                _line.SetPosition(i, pos);
            }
            _line.startColor = _line.endColor = f.IsDanger ? new Color(1f, 0.4f, 0.35f) : new Color(0.85f, 0.95f, 0.85f);

            if (Time.time >= _nextSplash)
            {
                float intensity = f.Mood == FishMood.Burst ? 1.4f : f.Mood == FishMood.Run ? 0.9f : 0.4f;
                _nextSplash = Time.time + Mathf.Lerp(0.5f, 0.12f, intensity / 1.4f);
                Effects.Splash(fishPos, Mathf.RoundToInt(4 + 8 * intensity), 0.5f + intensity * 0.5f);
                if (intensity > 0.8f) Effects.Ripple(fishPos, 1.2f + intensity, 0.9f);
            }

            bool near = f.Distance < 9f;
            _fish.SetActive(near);
            if (near)
            {
                float surface = f.HpLow ? 0.05f : -0.12f;
                _fish.transform.position = fishPos + Vector3.up * surface;
                var look = (_origin - fishPos);
                look.y = 0f;
                float wiggle = Mathf.Sin(Time.time * (f.HpLow ? 6f : 14f)) * (f.HpLow ? 10f : 25f);
                _fish.transform.rotation = Quaternion.LookRotation(look.normalized) * Quaternion.Euler(0f, 90f + wiggle, 0f);
            }
        }
    }
}
