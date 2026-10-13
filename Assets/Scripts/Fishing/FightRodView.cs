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
        // 1인칭에서는 화면 안에 끝이 들어오도록 조금 짧게 보여 준다
        const float FirstPersonLength = 0.75f;

        Transform _root;
        TelescopicRod _rod;
        string _rodId, _reelId;
        Transform _leftHand;
        float _reelTurns, _lineOut;
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
            _root.localPosition = new Vector3(0.2f, -0.25f, 0.62f); // 릴과 손이 화면 아래쪽에 보이도록

            var skin = Mats.Lit(new Color(0.98f, 0.82f, 0.7f));
            var glove = Mats.Lit(new Color(0.95f, 0.95f, 0.95f));

            BuildRod();
            // 오른손은 릴 시트를 쥐고(릴 다리를 손가락 사이에 끼움), 왼손은 릴 손잡이를 돌린다
            Mats.Prim(PrimitiveType.Capsule, "HandRight", _root, new Vector3(0f, -0.07f, -0.01f), new Vector3(0.065f, 0.05f, 0.065f), glove, false);
            Mats.Prim(PrimitiveType.Capsule, "ArmR", _root, new Vector3(0.05f, -0.24f, -0.14f), new Vector3(0.08f, 0.15f, 0.08f), skin, false, Quaternion.Euler(40f, 0f, 20f));

            // 밤에도 손과 릴이 보이도록 아주 약한 보조광 (원작 1인칭도 손 주변이 밝다)
            var fill = new GameObject("HandFill").AddComponent<Light>();
            fill.transform.SetParent(_root, false);
            fill.transform.localPosition = new Vector3(-0.15f, 0.25f, -0.25f);
            fill.type = LightType.Point;
            fill.range = 1.2f;
            fill.intensity = 0.9f;
            fill.color = new Color(1f, 0.95f, 0.85f);
            fill.shadows = LightShadows.None;

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

        void BuildRod()
        {
            var p = Game.Profile;
            _rodId = p?.Rod.Id;
            _reelId = p?.Reel.Id;
            if (_rod == null) _rod = TelescopicRod.Create(_root, p?.Rod, p?.Reel, FirstPersonLength);
            else _rod.Rebuild(p?.Rod, p?.Reel, FirstPersonLength);

            if (_leftHand != null) Destroy(_leftHand.gameObject);
            var knob = _rod.HandleKnob;
            if (knob != null)
            {
                _leftHand = Mats.Prim(PrimitiveType.Sphere, "HandLeft", knob, new Vector3(-0.6f, 0f, 0f), new Vector3(3.2f, 2.6f, 3.2f),
                    Mats.Lit(new Color(0.95f, 0.95f, 0.95f)), false).transform;
            }
        }

        /// <summary>릴 회전: 감는 속도(초당 바퀴)와 줄이 풀리는 속도(m/s). FishingController가 매 프레임 넣어 준다.</summary>
        public void SetReel(float turnsPerSec, float lineOutSpeed)
        {
            _reelTurns = turnsPerSec;
            _lineOut = lineOutSpeed;
        }

        /// <summary>카운터 같은 순간에 낚싯대를 확 휘게 한다.</summary>
        public void Kick(float degPerSec) => _rod.Kick(degPerSec);

        public void Show(FishFight fight, Vector3 origin, Vector3 forward)
        {
            var p = Game.Profile;
            if (p != null && (p.Rod.Id != _rodId || p.Reel.Id != _reelId)) BuildRod();
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
            float flex = _rod.Look.Flex;
            float maxBend = Mathf.Lerp(50f, 85f, flex);
            float target = Mathf.Pow(Mathf.Clamp01(f.Tension * 1.05f), 0.8f) * maxBend;
            if (f.Tension > 1f) target += (f.Tension - 1f) * 40f;

            // 물고기가 날뛰면 초릿대가 떨린다
            float shake = f.Mood == FishMood.Burst ? 1f : f.Mood == FishMood.Run ? 0.5f : 0.15f;
            float jitter = (Mathf.PerlinNoise(Time.time * 11f, 0.3f) - 0.5f) * 2f * shake * (2f + Mathf.Clamp01(f.Tension) * 7f);
            _rod.Drive(target, fishPos - _rod.transform.position, jitter, Time.deltaTime);
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
            _rod.SpinReel(_reelTurns, _lineOut, Time.deltaTime);

            var tipPos = _rod.Tip.position;
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
