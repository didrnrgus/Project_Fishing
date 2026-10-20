using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 파이팅 중 1인칭 시점의 낚싯대와 두 팔(<see cref="FirstPersonArms"/>). 장력에 따라 휘고, 마우스로 화면 끝에서 끝까지 좌우로 튼다.
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
        FirstPersonArms _arms;
        string _rodId, _reelId;
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
            // 낚싯대는 아래에서 올라오고 큰 릴이 보인다. 두 팔(FirstPersonArms)이 손잡이와 릴 노브를 쥔다
            _root.localPosition = new Vector3(-0.2f, -0.25f, 0.62f);

            BuildRod();
            _arms = FirstPersonArms.Create(transform);

            // 밤에도 낚싯대와 릴이 보이도록 아주 약한 보조광
            var fill = new GameObject("RodFill").AddComponent<Light>();
            fill.transform.SetParent(_root, false);
            fill.transform.localPosition = new Vector3(-0.15f, 0.25f, -0.25f);
            fill.type = LightType.Point;
            fill.range = 1.5f;
            fill.intensity = 1.3f;
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
            _rod = TelescopicRod.Create(_root, p?.Rod, p?.Reel, FirstPersonLength, 2.1f);
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
            _lateralK = 1f;
            _side = _sideVel = 0f;
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
            Lift = 0f;
            _landK = 0f;
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
            _rod.Drive(target * FirstPersonBend, BendPull(fishPos), jitter, Time.deltaTime);
        }

        /// <summary>
        /// 휘는 방향. 물고기는 20m 앞이라 실제 방향대로면 낚싯대가 화면에서 앞(위아래)으로만 휜다.
        /// 원작은 낚싯대를 화면 끝으로 틀면 초릿대가 물고기(화면 가운데 물보라) 쪽으로 옆으로 활처럼 휜다 (0:44 ↔ 0:48) →
        /// 카메라 공간에서 "낚싯대 → 물고기" 화면상 좌우 차이를 크게 키워 옆으로 휘게 한다.
        /// </summary>
        Vector3 BendPull(Vector3 fishPos)
        {
            var cam = transform;
            var fishLocal = cam.InverseTransformPoint(fishPos);
            var rodLocal = cam.InverseTransformPoint(_rod.transform.position);
            // 낚싯대 깊이에서 본 물고기의 화면상 좌우 위치와 낚싯대의 차이
            float fishX = fishLocal.x / Mathf.Max(1f, fishLocal.z) * rodLocal.z;
            float lateral = Mathf.Clamp((fishX - rodLocal.x) * 3.2f, -1.6f, 1.6f);
            return cam.TransformDirection(new Vector3(lateral, -0.25f, 0.75f));
        }

        Renderer[] _rodRenderers;
        Material[][] _rodMats;
        Material _goldMat;
        bool _goldOn;
        float _glow;

        /// <summary>
        /// 원작(2013 홍현 영상 0:52, 1:44~1:52): 장력이 HIGH 쪽이면 낚싯대와 릴 전체가 노랗게 달아올라 빛난다
        /// (데미지가 커지는 구간이라는 신호). 밤에도 빛나 보여야 해서 조명을 받지 않는 금색 머티리얼로 통째로 바꾼다
        /// (Standard의 Emission은 빌드에서 셰이더 변형이 빠져 켜지지 않았다). 장력 60%부터 차올라 80%면 완전히, 위험(90%~)이면 깜빡.
        /// </summary>
        void UpdateGlow(FishFight f)
        {
            if (_rodRenderers == null || _rodRenderers.Length == 0 || _rodRenderers[0] == null)
            {
                _rodRenderers = System.Array.FindAll(_rod.GetComponentsInChildren<Renderer>(), r => !(r is LineRenderer));
                _rodMats = System.Array.ConvertAll(_rodRenderers, r => r.sharedMaterials);
                _goldOn = false;
            }
            _goldMat ??= new Material(Shader.Find("Unlit/Color"));
            float heat = Mathf.InverseLerp(FishFight.HighTension, 0.8f, f.Tension);
            float target = f.IsDanger ? 0.8f + Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.2f : heat * (0.88f + Mathf.Sin(Time.time * 8f) * 0.08f);
            _glow = Mathf.MoveTowards(_glow, target, Time.deltaTime * 5f);
            bool on = _glow > 0.08f;
            if (on != _goldOn)
            {
                _goldOn = on;
                for (int i = 0; i < _rodRenderers.Length; i++)
                {
                    if (_rodRenderers[i] == null) continue;
                    if (on)
                    {
                        var mats = new Material[_rodMats[i].Length];
                        for (int k = 0; k < mats.Length; k++) mats[k] = _goldMat;
                        _rodRenderers[i].sharedMaterials = mats;
                    }
                    else _rodRenderers[i].sharedMaterials = _rodMats[i];
                }
            }
            if (on) _goldMat.color = Color.Lerp(new Color(0.6f, 0.45f, 0.12f), new Color(1f, 0.9f, 0.32f), _glow);
        }

        /// <summary>
        /// 낚싯대가 화면 어느 쪽에 있는지 (-1 왼쪽 ~ 1 오른쪽). 원작 1인칭은 낚싯대를 트는 쪽으로 낚싯대 전체가 화면 끝까지 옮겨 간다:
        /// 왼쪽으로 당기면 낚싯대·릴이 화면 왼쪽 아래, 초릿대는 물고기(오른쪽) 쪽으로 활처럼 휜다 (0:44 ↔ 0:48).
        /// </summary>
        float _side, _sideVel;
        float _landK;
        /// <summary>랜딩 뒤 들어 올리기 0~1 (FishingController가 올린다). 0이면 평소 파이팅.</summary>
        public float Lift;

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
            // 원작: 낚싯대는 거의 세워 들고(초릿대가 화면 위쪽까지), 마우스로 트는 만큼 낚싯대·릴·두 팔이 통째로
            // 화면 왼쪽 끝 ↔ 오른쪽 끝까지 쓸려 간다 (예전엔 오른쪽에 거의 붙어 있어 위아래로만 움직여 보였다).
            // 덜 감쇠된 스프링으로 따라가서 휙 틀면 살짝 넘쳤다 돌아오고, 쓸려 가는 동안 초릿대가 뒤로 처진다
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _sideVel += ((f.RodX - _side) * 90f - _sideVel * 2f * Mathf.Sqrt(90f) * 0.6f) * dt;
            _side += _sideVel * dt;
            float sweep = Mathf.Clamp(_sideVel, -4f, 4f);
            // 물고기가 끄는 쪽으로 낚싯대가 조금 끌려간다 (장력이 높을수록)
            float x = Mathf.Clamp(_side * 0.42f + f.FishX * 0.1f * t, -0.5f, 0.5f);
            float bob = reeling ? Mathf.Sin(Time.time * 9f) * 0.006f : 0f;
            var pos = new Vector3(x, -0.175f - Mathf.Abs(_side) * 0.03f + bob, 0.56f);
            // 화면 끝으로 갈수록 바깥으로 눕고(롤), 쓸려 가는 쪽 반대로 초릿대가 처진다
            var euler = new Vector3(24f + t * 8f + pump + tug, -_side * 12f + f.FishX * 6f, -_side * 24f + sweep * 3.5f - f.FishX * 4f);
            // 랜딩 찬스 (원작 1:52): 릴이 화면 가운데로 크게 다가온다
            _landK = Mathf.Lerp(_landK, f.Landing && Lift <= 0f ? 1f : 0f, 1f - Mathf.Exp(-6f * Time.deltaTime));
            pos = Vector3.Lerp(pos, new Vector3(-0.06f, -0.13f, 0.36f), _landK);
            euler = Vector3.Lerp(euler, new Vector3(16f + pump, 6f, -6f), _landK);
            // 랜딩 성공 (원작 1:53~1:54.8): 낚싯대를 세워 들어 올린다
            if (Lift > 0f)
            {
                float k = Mathf.SmoothStep(0f, 1f, Lift);
                pos = Vector3.Lerp(pos, new Vector3(0.12f, -0.2f, 0.42f), k);
                euler = Vector3.Lerp(euler, new Vector3(-6f, 4f, -10f), k);
            }
            _root.localPosition = pos;
            _root.localRotation = Quaternion.Euler(euler);
            UpdateGlow(f);
            var fishPos = FishWorldPos(f);
            BendRod(f, fishPos);
            _rod.SpinReel(_reelTurns, _lineOut, Time.deltaTime);
            _arms.Follow(_rod);

            var tipPos = _rod.Tip.position;
            var lineEnd = Lift > 0f ? Vector3.Lerp(fishPos, transform.position + transform.forward * 2.2f + Vector3.up * 0.9f, Mathf.SmoothStep(0f, 1f, Lift) * 0.85f) : fishPos;
            float sag = Mathf.Lerp(0.8f, 0.02f, Mathf.Clamp01(f.Tension * 1.5f));
            for (int i = 0; i < _line.positionCount; i++)
            {
                float u = i / (float)(_line.positionCount - 1);
                var lp = Vector3.Lerp(tipPos, lineEnd, u);
                lp.y -= Mathf.Sin(u * Mathf.PI) * sag;
                _line.SetPosition(i, lp);
            }
            // 원작: 평소 줄은 하늘색, HIGH 구간이면 분홍빛 붉은 줄
            _line.startColor = _line.endColor = f.IsDanger ? new Color(1f, 0.3f, 0.3f) : f.IsHighTension ? new Color(1f, 0.45f, 0.6f) : new Color(0.6f, 0.85f, 1f);

            // 원작: 물고기는 안 보이고, 그 자리에 하얀 물보라 기둥이 쉬지 않고 솟는다 (질주·폭주면 더 크게)
            if (Time.time >= _nextSplash && Lift <= 0f)
            {
                float intensity = f.Mood == FishMood.Burst ? 1.4f : f.Mood == FishMood.Run ? 1f : f.HpLow ? 0.35f : 0.6f;
                _nextSplash = Time.time + Mathf.Lerp(0.16f, 0.07f, intensity / 1.4f);
                Effects.FightSpray(fishPos, intensity);
                if (Random.value < 0.25f) Effects.Ripple(fishPos, 1.6f + intensity * 1.4f, 1.1f);
            }

            bool near = f.Distance < 9f;
            _fish.SetActive(near);
            if (near)
            {
                float surface = f.HpLow ? 0.05f : -0.12f;
                // 들어 올리면 물 밖으로 끌려 나와 카메라 앞으로 날아든다
                float lifted = Mathf.SmoothStep(0f, 1f, Lift);
                _fish.transform.position = Vector3.Lerp(fishPos + Vector3.up * surface, transform.position + transform.forward * 2.2f + Vector3.up * 0.9f, lifted * 0.85f);
                var look = (_origin - fishPos);
                look.y = 0f;
                float wiggle = Mathf.Sin(Time.time * (f.HpLow ? 6f : 14f)) * (f.HpLow ? 10f : 25f);
                _fish.transform.rotation = Quaternion.LookRotation(look.normalized) * Quaternion.Euler(0f, 90f + wiggle * 0.4f, 0f);
                // 질주·폭주하면 몸부림, 지치면 천천히, 그 밖에는 빠르게 헤엄
                string clip = Lift > 0f ? FishModels.Thrash : f.HpLow ? FishModels.SwimSlow : f.Mood == FishMood.Burst ? FishModels.Thrash : FishModels.SwimFast;
                if (clip != _fishClip && _fishBody != null)
                {
                    _fishClip = clip;
                    FishModels.Play(_fishBody.gameObject, clip, f.Mood == FishMood.Run ? 1.3f : 1f);
                }
            }
        }
    }
}
