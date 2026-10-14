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
        const float FirstPersonLength = 0.62f;
        // 1인칭에서는 휜 모양이 잘 보이도록 조금 더 휘게 보여 준다
        const float FirstPersonBend = 1.35f;

        Transform _root;
        TelescopicRod _rod;
        string _rodId, _reelId;
        Transform _leftHand, _rightHand;
        Transform _armL, _armR;
        // 팔뚝 위쪽은 입은 상의 색 소매 (반팔이면 팔꿈치 위까지, 긴팔이면 손목까지)
        Transform _sleeveL, _sleeveR;
        Material _sleeveMat, _foreMat;
        string _lookKey;
        // 카메라 기준 어깨 위치 (원작 1인칭: 팔이 화면 아래에서 올라온다)
        static readonly Vector3 ShoulderL = new Vector3(-0.5f, -0.72f, 0.2f);
        static readonly Vector3 ShoulderR = new Vector3(0.16f, -0.8f, 0.2f);
        float _reelTurns, _lineOut;
        LineRenderer _line;
        GameObject _fish;
        Transform _fishBody;
        string _fishClip;
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
            // 원작 1인칭처럼 낚싯대는 왼쪽 아래에서 올라오고, 큰 릴과 장갑 낀 손이 보인다
            _root.localPosition = new Vector3(-0.2f, -0.25f, 0.62f);

            _foreMat = Mats.Lit(new Color(0.98f, 0.82f, 0.7f));
            _sleeveMat = Mats.Lit(new Color(0.92f, 0.92f, 0.94f));

            BuildRod();
            // 오른손은 릴 시트를 쥐고(릴 다리를 손가락 사이에 끼움), 왼손은 릴 손잡이를 돌린다
            _armL = Mats.Prim(PrimitiveType.Capsule, "ArmL", transform, Vector3.zero, Vector3.one * 0.07f, _foreMat, false).transform;
            _armR = Mats.Prim(PrimitiveType.Capsule, "ArmR", transform, Vector3.zero, Vector3.one * 0.07f, _foreMat, false).transform;
            _sleeveL = Mats.Prim(PrimitiveType.Capsule, "SleeveL", transform, Vector3.zero, Vector3.one * 0.08f, _sleeveMat, false).transform;
            _sleeveR = Mats.Prim(PrimitiveType.Capsule, "SleeveR", transform, Vector3.zero, Vector3.one * 0.08f, _sleeveMat, false).transform;

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
            if (_rod != null) Destroy(_rod.gameObject);
            _rodRenderers = null;
            _rod = TelescopicRod.Create(_root, p?.Rod, p?.Reel, FirstPersonLength, 1.8f);

            if (_leftHand != null) Destroy(_leftHand.gameObject);
            if (_rightHand != null) Destroy(_rightHand.gameObject);
            var glove = Mats.Lit(new Color(0.12f, 0.12f, 0.14f), 0.3f);
            var cuff = Mats.Lit(new Color(0.95f, 0.55f, 0.15f), 0.4f);
            // 왼손: 릴 손잡이를 쥐고 같이 돈다 / 오른손: 릴 시트 위 앞손잡이를 쥔다
            var knob = _rod.HandleKnob;
            if (knob != null)
            {
                _leftHand = new GameObject("HandLeft").transform;
                _leftHand.SetParent(knob, false);
                _leftHand.localPosition = new Vector3(-0.5f, 0f, 0f);
                Mats.Prim(PrimitiveType.Sphere, "Glove", _leftHand, Vector3.zero, new Vector3(4.2f, 3.4f, 4.2f), glove, false);
                Mats.Prim(PrimitiveType.Cylinder, "Cuff", _leftHand, new Vector3(-2.2f, 0f, 0f), new Vector3(3.6f, 1.2f, 3.6f), cuff, false,
                    Quaternion.Euler(0f, 0f, 90f));
            }
            _rightHand = new GameObject("HandRight").transform;
            _rightHand.SetParent(_rod.transform, false);
            _rightHand.localPosition = new Vector3(0.012f, 0.1f, -0.02f);
            // 주먹: 손등 + 낚싯대를 감싼 손가락 네 개 + 엄지 (손가락 끝 없는 반장갑이라 끝마디는 살색)
            Mats.Prim(PrimitiveType.Sphere, "Glove", _rightHand, new Vector3(0.006f, 0f, -0.012f), new Vector3(0.07f, 0.088f, 0.058f), glove, false);
            var skinTip = Mats.Lit(new Color(0.98f, 0.82f, 0.7f));
            for (int i = 0; i < 4; i++)
            {
                float y = 0.03f - i * 0.02f;
                var finger = Mats.Prim(PrimitiveType.Capsule, "Finger" + i, _rightHand, new Vector3(-0.008f, y, 0.02f), new Vector3(0.021f, 0.022f, 0.021f), glove, false,
                    Quaternion.Euler(0f, 0f, 90f)).transform;
                Mats.Prim(PrimitiveType.Sphere, "Tip", finger, new Vector3(0f, 0.9f, 0f), new Vector3(0.95f, 0.6f, 0.95f), skinTip, false);
            }
            var thumb = Mats.Prim(PrimitiveType.Capsule, "Thumb", _rightHand, new Vector3(-0.026f, 0.035f, -0.004f), new Vector3(0.022f, 0.026f, 0.022f), glove, false,
                Quaternion.Euler(0f, 0f, -20f)).transform;
            Mats.Prim(PrimitiveType.Sphere, "Tip", thumb, new Vector3(0f, 0.9f, 0f), new Vector3(0.95f, 0.6f, 0.95f), skinTip, false);
            Mats.Prim(PrimitiveType.Cylinder, "Cuff", _rightHand, new Vector3(0.02f, -0.06f, -0.03f), new Vector3(0.07f, 0.02f, 0.07f), cuff, false,
                Quaternion.Euler(30f, 0f, 0f));
        }

        /// <summary>입은 상의 색으로 소매를 칠한다 (긴팔이면 아래팔까지 소매).</summary>
        void ApplyLook()
        {
            var look = Game.Profile?.BuildLook();
            if (look == null) return;
            bool longSleeve = look.TopModel != "Top_Tee";
            string key = look.Shirt + look.TopModel;
            if (key == _lookKey) return;
            _lookKey = key;
            _sleeveMat.color = look.Shirt;
            _foreMat.color = longSleeve ? look.Shirt * 0.95f : new Color(0.98f, 0.82f, 0.7f);
            _sleeveLength = longSleeve ? 0.85f : 0.42f;
        }

        float _sleeveLength = 0.42f;

        /// <summary>어깨에서 손까지: 위쪽은 소매(조금 굵게), 아래쪽은 아래팔(맨살 또는 긴소매).</summary>
        void Arm(Transform sleeve, Transform fore, Vector3 shoulder, Vector3 hand)
        {
            var mid = Vector3.Lerp(shoulder, hand, _sleeveLength);
            Limb(sleeve, shoulder, mid, 0.07f);
            Limb(fore, Vector3.Lerp(shoulder, hand, _sleeveLength - 0.06f), hand, 0.05f);
        }

        static void Limb(Transform arm, Vector3 from, Vector3 to, float width)
        {
            var d = to - from;
            arm.position = (from + to) * 0.5f;
            arm.rotation = Quaternion.FromToRotation(Vector3.up, d);
            arm.localScale = new Vector3(width, d.magnitude * 0.5f, width);
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
            ApplyLook();
            _fight = fight;
            _lateralK = 1f;
            _origin = origin;
            _forward = new Vector3(forward.x, 0f, forward.z).normalized;
            if (_fishBody != null) Destroy(_fishBody.gameObject);
            _fishBody = FishModels.Create(fight.Species, fight.Length, _fish.transform).transform;
            _fishClip = null;
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
            _rod.Drive(target * FirstPersonBend, fishPos - _rod.transform.position, jitter, Time.deltaTime);
        }

        Renderer[] _rodRenderers;
        MaterialPropertyBlock _glowBlock;
        float _glow;

        /// <summary>원작 0:35: 장력이 HIGH 쪽이면 낚싯대가 금빛으로 달아오른다 (데미지가 커지는 구간이라는 신호).</summary>
        void UpdateGlow(FishFight f)
        {
            if (_rodRenderers == null || _rodRenderers.Length == 0 || _rodRenderers[0] == null)
                _rodRenderers = System.Array.FindAll(_rod.GetComponentsInChildren<Renderer>(),
                    r => (_leftHand == null || !r.transform.IsChildOf(_leftHand)) && (_rightHand == null || !r.transform.IsChildOf(_rightHand)));
            _glowBlock ??= new MaterialPropertyBlock();
            float target = f.IsDanger ? 0.85f : f.IsHighTension ? 0.6f + Mathf.Sin(Time.time * 10f) * 0.15f : 0f;
            _glow = Mathf.MoveTowards(_glow, target, Time.deltaTime * 4f);
            var hot = f.IsDanger ? new Color(1f, 0.45f, 0.3f) : new Color(1f, 0.82f, 0.35f);
            foreach (var r in _rodRenderers)
            {
                if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_Color")) continue;
                if (_glow < 0.01f) { r.SetPropertyBlock(null); continue; }
                var baseColor = r.sharedMaterial.color;
                _glowBlock.SetColor("_Color", Color.Lerp(baseColor, hot * 1.6f, _glow));
                r.SetPropertyBlock(_glowBlock);
            }
        }

        /// <summary>
        /// 물고기의 월드 위치. 거리와 좌우 위치(FishX)로 계산한다.
        /// 좌우로 내달린 자리가 땅·방파제면 물 위에 머물도록 좌우 폭을 줄인다(_lateralK, 부드럽게).
        /// </summary>
        public Vector3 FishWorldPos(FishFight f) => FishPos(f, _lateralK);

        float _lateralK = 1f;

        Vector3 FishPos(FishFight f, float k)
        {
            var right = Vector3.Cross(Vector3.up, _forward);
            var p = _origin + _forward * f.Distance + right * (f.FishX * f.Distance * 0.45f * k);
            p.y = FieldMap.WaterY;
            return p;
        }

        void KeepFishInWater(FishFight f)
        {
            float want = 1f;
            for (int i = 0; i < 6 && f.Distance > 2.5f && !FieldMap.IsWater(FishPos(f, want)); i++) want *= 0.6f;
            _lateralK = Mathf.MoveTowards(_lateralK, want, Time.deltaTime * (want < _lateralK ? 5f : 1f));
        }

        void LateUpdate()
        {
            if (_fight == null) return;
            var f = _fight;
            KeepFishInWater(f);

            float t = Mathf.Clamp01(f.Tension);
            // 원작: 낚싯대가 물고기 쪽으로 크게 끌려가고, 감을 때는 들썩이며 펌핑한다
            bool reeling = _reelTurns > 0.1f;
            float pump = reeling ? Mathf.Sin(Time.time * 9f) * 3f : 0f;
            float tug = f.Mood == FishMood.Burst ? Mathf.Sin(Time.time * 23f) * 2.5f : 0f;
            _root.localRotation = Quaternion.Euler(44f + t * 10f + pump + tug, 10f + f.RodX * 28f + f.FishX * 16f, -8f - f.RodX * 10f - f.FishX * 6f);
            UpdateGlow(f);
            var fishPos = FishWorldPos(f);
            BendRod(f, fishPos);
            _rod.SpinReel(_reelTurns, _lineOut, Time.deltaTime);
            if (_leftHand != null) Arm(_sleeveL, _armL, transform.TransformPoint(ShoulderL), _leftHand.position);
            Arm(_sleeveR, _armR, transform.TransformPoint(ShoulderR), _rightHand.position);

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
                // 질주·폭주하면 몸부림, 지치면 천천히, 그 밖에는 빠르게 헤엄
                string clip = f.HpLow ? FishModels.SwimSlow : f.Mood == FishMood.Burst ? FishModels.Thrash : FishModels.SwimFast;
                if (clip != _fishClip && _fishBody != null)
                {
                    _fishClip = clip;
                    FishModels.Play(_fishBody.gameObject, clip, f.Mood == FishMood.Run ? 1.3f : 1f);
                }
            }
        }
    }
}
