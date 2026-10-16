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
        Transform _leftHand, _rightHand, _rightWrist, _leftWrist;
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
            // 팔: 캡슐(통나무 같았다) 대신 손목 쪽이 가늘어지는 팔 메쉬
            _armL = LimbPart("ArmL", _foreMat, 0.62f);
            _armR = LimbPart("ArmR", _foreMat, 0.62f);
            _sleeveL = LimbPart("SleeveL", _sleeveMat, 0.85f);
            _sleeveR = LimbPart("SleeveR", _sleeveMat, 0.85f);

            // 밤에도 손과 릴이 보이도록 아주 약한 보조광 (원작 1인칭도 손 주변이 밝다)
            var fill = new GameObject("HandFill").AddComponent<Light>();
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

            if (_leftHand != null) Destroy(_leftHand.gameObject);
            if (_rightHand != null) Destroy(_rightHand.gameObject);
            // 손가락 끝이 트인 낚시용 반장갑 (짙은 회색 천 + 손등 고무 덧댐), 끝마디는 맨살.
            // 예전 손목의 주황 원통(장갑 밴드)은 릴 크기로 커져 정체 모를 노란 원통처럼 보여 없앴다
            var glove = Mats.Lit(new Color(0.34f, 0.38f, 0.44f), 0.15f);   // 회청색 천 (짙은 검정은 밤에 덩어리로만 보였다)
            var pad = Mats.Lit(new Color(0.16f, 0.17f, 0.19f), 0.4f);      // 손등·마디 고무 덧댐
            var skin = Mats.Lit(new Color(0.93f, 0.76f, 0.64f), 0.3f);
            // 왼손: 릴 손잡이(노브)를 감싸 쥐고 손잡이와 같이 돈다 (자리는 LateUpdate에서 노브를 따라간다)
            // 오른손: 릴 다리를 손가락 사이에 끼우고 손잡이를 쥔다
            if (_rod.HandleKnob != null)
                _leftHand = FpHand.BuildLeft(transform, 0.012f, glove, pad, skin, out _leftWrist);
            // 오른손: 손잡이를 실제로 감아 쥔 손가락 메쉬 (FpHand)
            _rightHand = FpHand.BuildRight(_rod.transform, glove, pad, skin, out _rightWrist);
        }

        static readonly System.Collections.Generic.Dictionary<float, Mesh> LimbMeshes = new System.Collections.Generic.Dictionary<float, Mesh>();

        /// <summary>
        /// 팔 토막: 캡슐 프리미티브와 같은 크기 규칙(지름 1, 높이 2, Y축)인데 +Y 끝이 taper배로 가늘다 (손목·팔꿈치 쪽).
        /// 단면은 살짝 납작한 타원이라 통나무처럼 안 보인다.
        /// </summary>
        Transform LimbPart(string name, Material mat, float taper)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            if (!LimbMeshes.TryGetValue(taper, out var mesh))
                LimbMeshes[taper] = mesh = TaperedCapsule(taper);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Mesh TaperedCapsule(float taper)
        {
            const int seg = 20, rings = 28;
            var v = new System.Collections.Generic.List<Vector3>();
            var tri = new System.Collections.Generic.List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float rad = Mathf.Lerp(0.5f, 0.5f * taper, t);
                bool sleeve = taper > 0.8f;
                if (sleeve)
                    rad *= 1f + 0.13f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 0.86f, t)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.95f, 0.99f, t))) // 접어 올린 소매 끝
                         + 0.03f * Mathf.Sin(t * 37f) * (1f - t);                                                                                                 // 천 주름
                else
                    rad *= 1f + 0.14f * Mathf.Exp(-Mathf.Pow((t - 0.28f) / 0.18f, 2f)) - 0.06f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 0.95f, t)); // 팔꿈치 쪽 근육, 손목은 잘록
                float cap = Mathf.Min(Mathf.Clamp01(t / 0.12f), Mathf.Clamp01((1f - t) / 0.12f));
                rad *= Mathf.Sqrt(1f - (1f - cap) * (1f - cap));                      // 양 끝은 둥글게 닫힌다
                for (int k = 0; k <= seg; k++)
                {
                    float a = k / (float)seg * Mathf.PI * 2f;
                    float flat = taper > 0.8f ? 0.9f : Mathf.Lerp(0.88f, 0.7f, t); // 아래팔은 손목으로 갈수록 납작
                    v.Add(new Vector3(Mathf.Cos(a) * rad, -1f + 2f * t, Mathf.Sin(a) * rad * flat));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int k = 0; k < seg; k++)
            {
                int a = r * (seg + 1) + k, b = a + seg + 1;
                tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            var m = new Mesh { name = "Limb" };
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        float _rightTwist = float.NaN;

        /// <summary>
        /// 오른손을 손잡이 축으로 돌려 손등(설계상 −42°)이 카메라 쪽을 보게 한다 (조금 위·엄지 쪽으로 치우치게).
        /// 그래야 1인칭에서 손등·엄지가 보이고 손가락은 손잡이 아래로 숨는다. 고정 각도로 두면 낚싯대가 비틀릴 때
        /// 손끝이 빗살처럼 옆으로 튀어나와 보였다.
        /// </summary>
        void PoseRightHand()
        {
            if (_rightHand == null) return;
            var c = _rod.transform.InverseTransformPoint(transform.position);
            float camDeg = Mathf.Atan2(c.z, c.x) * Mathf.Rad2Deg;
            float target = camDeg + Mathf.DeltaAngle(camDeg, -90f) * 0.2f; // 엄지 쪽(위, −Z)으로 조금
            float twist = -42f - target;                                     // Y축으로 α 돌리면 각도 φ → φ − α
            _rightTwist = float.IsNaN(_rightTwist) ? twist : Mathf.LerpAngle(_rightTwist, twist, 1f - Mathf.Exp(-10f * Time.deltaTime));
            _rightHand.localRotation = Quaternion.AngleAxis(_rightTwist, Vector3.up);
        }

        /// <summary>왼손을 릴 노브에 맞춘다: 손 원점 = 노브, 로컬 +Y = 노브 축, 손등은 릴 몸통 반대쪽(바깥)을 본다.</summary>
        void PoseLeftHand()
        {
            var knob = _rod.HandleKnob;
            if (_leftHand == null || knob == null) return;
            var axis = knob.up; // 캡슐 노브의 길이 방향
            var outward = Vector3.ProjectOnPlane(knob.position - _rod.Reel.position, axis);
            if (outward.sqrMagnitude < 1e-6f) outward = -transform.right;
            outward.Normalize();
            // 거울(X −1) 안의 +X(손등)가 바깥을 보려면 손 원점의 right = −outward
            var right = -outward;
            var fwd = Vector3.Cross(right, axis);
            _leftHand.SetPositionAndRotation(knob.position, Quaternion.LookRotation(fwd, axis));
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
            _rod.Drive(target * FirstPersonBend, fishPos - _rod.transform.position, jitter, Time.deltaTime);
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
                _rodRenderers = System.Array.FindAll(_rod.GetComponentsInChildren<Renderer>(),
                    r => !(r is LineRenderer) && (_leftHand == null || !r.transform.IsChildOf(_leftHand)) && (_rightHand == null || !r.transform.IsChildOf(_rightHand)));
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
        float _side;
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
            // 원작: 낚싯대는 거의 세워 들고(초릿대가 화면 위쪽까지), 트는 쪽 화면 끝으로 통째로 옮겨 간다
            float wantSide = Mathf.Abs(f.RodX) > 0.12f ? Mathf.Sign(f.RodX) * Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(0.12f, 0.7f, Mathf.Abs(f.RodX))) : _side * 0.98f;
            _side = Mathf.Lerp(_side, wantSide, 1f - Mathf.Exp(-4f * Time.deltaTime));
            var pos = new Vector3(-0.02f + _side * 0.36f, -0.2f, 0.56f);
            var euler = new Vector3(26f + t * 8f + pump + tug, _side * -14f + f.FishX * 10f, -_side * 18f - f.FishX * 5f);
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
            PoseRightHand();
            PoseLeftHand();
            // 팔은 손목(손 뒤쪽)까지
            if (_leftHand != null) Arm(_sleeveL, _armL, transform.TransformPoint(ShoulderL), _leftWrist.position);
            Arm(_sleeveR, _armR, transform.TransformPoint(ShoulderR), _rightWrist.position);

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
