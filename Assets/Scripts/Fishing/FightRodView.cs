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
        const int Segments = 14;
        const float SegLen = 0.145f;

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

        // 휨: 스프링으로 따라가서 장력이 갑자기 바뀌면 출렁인다
        float _bend, _bendVel;
        Vector3 _bendAxis = Vector3.right;

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
                float thick = Mathf.Lerp(0.03f, 0.007f, i / (float)(Segments - 1));
                Mats.Prim(PrimitiveType.Cylinder, "Blank", seg, new Vector3(0f, SegLen * 0.5f, 0f), new Vector3(thick, SegLen * 0.5f, thick), rodMat, false);
                // 마디 사이가 벌어져 보이지 않게 이음매에 작은 구
                if (i > 0) Mats.Prim(PrimitiveType.Sphere, "Joint", seg, Vector3.zero, Vector3.one * thick, rodMat, false);
                // 가이드(줄 고리) 몇 개
                if (i % 3 == 2) Mats.Prim(PrimitiveType.Cylinder, "Guide", seg, new Vector3(0f, SegLen * 0.5f, thick * 0.9f),
                    new Vector3(thick * 1.6f, 0.002f, thick * 1.6f), Mats.Lit(new Color(0.8f, 0.8f, 0.82f), 0.8f, 0.8f), false,
                    Quaternion.Euler(90f, 0f, 0f));
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
            _fish.SetActive(false);
        }

        public void Show(FishFight fight, Vector3 origin, Vector3 forward)
        {
            _fight = fight;
            _origin = origin;
            _forward = new Vector3(forward.x, 0f, forward.z).normalized;
            if (_fishBody != null) Destroy(_fishBody.gameObject);
            _fishBody = FishModels.Create(fight.Species, fight.Length, _fish.transform).transform;
            gameObject.SetActive(true);
            _line.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _fight = null;
            gameObject.SetActive(false);
            _fish.SetActive(false);
        }

        /// <summary>
        /// 낚싯대를 낚싯줄 방향(물고기 쪽)으로 휜다.
        ///  - 휨 양: 장력^0.8 × 최대 휨각. 연조대(Flex 높음)는 최대 85°, 경조대는 50°
        ///  - 분포: 끝으로 갈수록 많이 휜다. 연조대는 손잡이 쪽까지 휘고(지수 1.3), 경조대는 끝만 휜다(지수 2.4)
        ///  - 스프링: 덜 감쇠된 스프링으로 따라가서 장력이 튀면 출렁이고, 질주할 때는 끝이 떨린다
        /// </summary>
        void BendRod(FishFight f, Vector3 fishPos)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float flex = Game.Profile != null ? Game.Profile.Rod.Flex : 0.6f;
            float maxBend = Mathf.Lerp(50f, 85f, flex);
            float target = Mathf.Pow(Mathf.Clamp01(f.Tension * 1.05f), 0.8f) * maxBend;
            if (f.Tension > 1f) target += (f.Tension - 1f) * 40f;

            const float stiffness = 140f;
            float damping = 2f * Mathf.Sqrt(stiffness) * 0.32f;
            _bendVel += ((target - _bend) * stiffness - _bendVel * damping) * dt;
            _bend += _bendVel * dt;

            // 물고기가 날뛰면 초릿대가 떨린다
            float shake = f.Mood == FishMood.Burst ? 1f : f.Mood == FishMood.Run ? 0.5f : 0.15f;
            float jitter = (Mathf.PerlinNoise(Time.time * 11f, 0.3f) - 0.5f) * 2f * shake * (2f + Mathf.Clamp01(f.Tension) * 7f);

            // 낚싯대 기준으로 줄이 당기는 방향 → 그 방향으로 기울어지는 회전축
            var local = _root.InverseTransformDirection(fishPos - _root.position);
            var pull = new Vector3(local.x, 0f, local.z);
            if (pull.sqrMagnitude < 1e-4f) pull = Vector3.forward;
            var axis = Vector3.Cross(Vector3.up, pull.normalized);
            _bendAxis = Vector3.Slerp(_bendAxis, axis, 1f - Mathf.Exp(-8f * dt));

            float exponent = Mathf.Lerp(2.4f, 1.3f, flex);
            float total = 0f;
            for (int i = 1; i < Segments; i++) total += Mathf.Pow(i / (float)(Segments - 1), exponent);
            float bend = Mathf.Max(0f, _bend + jitter);
            for (int i = 1; i < Segments; i++)
            {
                float w = Mathf.Pow(i / (float)(Segments - 1), exponent) / total;
                _segs[i].localRotation = Quaternion.AngleAxis(bend * w, _bendAxis);
            }
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

            float t = Mathf.Clamp01(f.Tension);
            _root.localRotation = Quaternion.Euler(40f + t * 8f, f.RodX * 28f, -f.RodX * 10f);
            var fishPos = FishWorldPos(f);
            BendRod(f, fishPos);

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
                _fish.transform.rotation = Quaternion.LookRotation(look.normalized) * Quaternion.Euler(0f, 90f + wiggle * 0.4f, 0f);
            }
        }
    }
}
