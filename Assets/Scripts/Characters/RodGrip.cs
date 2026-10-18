using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 손으로 무언가(낚싯대, 고기 입)를 쥐는 모양. 예전에는 낚싯대 기준점을 손목 관절(hand_r)에 붙이고 손가락은 애니메이션 그대로 펴 두어서,
    /// 서 있을 때 낚싯대가 손 옆 8cm쯤 허공에 떠 있고 빈손처럼 보였다.
    /// - <see cref="AlignTo"/>: 손을 돌려 새끼→검지 방향(주먹 쥐었을 때 쥔 막대가 지나가는 방향)을 쥘 것의 축에 맞춘다.
    ///   (예전에는 손은 애니메이션 그대로, 낚싯대는 따로 기울여서 축이 손가락과 나란해지면 손가락이 비틀려 기괴했다)
    /// - <see cref="Grip"/>: 손바닥 앞(법선 쪽 2cm)에 축을 두는 자리.
    /// - <see cref="Wrap"/>: 애니메이션이 끝난 뒤(LateUpdate) 손가락마다 제 굽힘 축(손가락 방향 × 손바닥 법선)으로만 마디를 굽혀,
    ///   끝이 쥔 것의 겉면(반지름 r)에 닿게 감싼다. 손가락이 옆으로 꺾이지 않는다.
    /// </summary>
    public class RodGrip
    {
        readonly Transform _hand, _index1, _middle1, _pinky1;
        readonly Transform[][] _fingers;   // 검지·중지·약지·소지: 01, 02, 03
        readonly Transform[] _thumb;       // 01, 02, 03
        readonly bool _left;

        RodGrip(Transform hand, Transform[][] fingers, Transform[] thumb, bool left)
        {
            _hand = hand;
            _index1 = fingers[0][0];
            _middle1 = fingers[1][0];
            _pinky1 = fingers[3][0];
            _fingers = fingers;
            _thumb = thumb;
            _left = left;
        }

        /// <summary>캐릭터 뼈에서 오른손(left면 왼손) 손가락을 찾는다. 하나라도 없으면 null.</summary>
        public static RodGrip Find(Transform character, bool left = false)
        {
            var all = character.GetComponentsInChildren<Transform>(true);
            Transform Bone(string n) => System.Array.Find(all, t => t.name == n);
            string s = left ? "_l" : "_r";
            var hand = Bone("hand" + s);
            if (hand == null) return FindHumanoid(character, left);
            string[] names = { "index", "middle", "ring", "pinky" };
            var fingers = new Transform[4][];
            for (int f = 0; f < 4; f++)
            {
                fingers[f] = new[] { Bone(names[f] + "_01" + s), Bone(names[f] + "_02" + s), Bone(names[f] + "_03" + s) };
                if (System.Array.Exists(fingers[f], b => b == null)) return null;
            }
            var thumb = new[] { Bone("thumb_01" + s), Bone("thumb_02" + s), Bone("thumb_03" + s) };
            if (System.Array.Exists(thumb, b => b == null)) thumb = null;
            return new RodGrip(hand, fingers, thumb, left);
        }

        /// <summary>받은 휴머노이드 캐릭터 (뼈 이름이 다르다): Animator의 휴머노이드 매핑으로 손·손가락을 찾는다.</summary>
        static RodGrip FindHumanoid(Transform character, bool left)
        {
            var an = character.GetComponentInChildren<Animator>();
            if (an == null || !an.isHuman) return null;
            Transform B(HumanBodyBones r, HumanBodyBones l) => an.GetBoneTransform(left ? l : r);
            var hand = B(HumanBodyBones.RightHand, HumanBodyBones.LeftHand);
            var fingers = new[]
            {
                new[] { B(HumanBodyBones.RightIndexProximal, HumanBodyBones.LeftIndexProximal), B(HumanBodyBones.RightIndexIntermediate, HumanBodyBones.LeftIndexIntermediate), B(HumanBodyBones.RightIndexDistal, HumanBodyBones.LeftIndexDistal) },
                new[] { B(HumanBodyBones.RightMiddleProximal, HumanBodyBones.LeftMiddleProximal), B(HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.LeftMiddleIntermediate), B(HumanBodyBones.RightMiddleDistal, HumanBodyBones.LeftMiddleDistal) },
                new[] { B(HumanBodyBones.RightRingProximal, HumanBodyBones.LeftRingProximal), B(HumanBodyBones.RightRingIntermediate, HumanBodyBones.LeftRingIntermediate), B(HumanBodyBones.RightRingDistal, HumanBodyBones.LeftRingDistal) },
                new[] { B(HumanBodyBones.RightLittleProximal, HumanBodyBones.LeftLittleProximal), B(HumanBodyBones.RightLittleIntermediate, HumanBodyBones.LeftLittleIntermediate), B(HumanBodyBones.RightLittleDistal, HumanBodyBones.LeftLittleDistal) },
            };
            if (hand == null || System.Array.Exists(fingers, f => System.Array.Exists(f, x => x == null))) return null;
            var thumb = new[] { B(HumanBodyBones.RightThumbProximal, HumanBodyBones.LeftThumbProximal), B(HumanBodyBones.RightThumbIntermediate, HumanBodyBones.LeftThumbIntermediate), B(HumanBodyBones.RightThumbDistal, HumanBodyBones.LeftThumbDistal) };
            if (System.Array.Exists(thumb, x => x == null)) thumb = null;
            return new RodGrip(hand, fingers, thumb, left);
        }

        /// <summary>손바닥 가운데.</summary>
        public Vector3 Palm => Vector3.Lerp(_hand.position, _middle1.position, 0.62f);

        /// <summary>손목 → 가운뎃손가락 뿌리 방향.</summary>
        public Vector3 FingerDir => (_middle1.position - _hand.position).normalized;

        /// <summary>새끼 → 검지 뿌리 방향 (주먹 쥐면 쥔 막대가 이 방향으로 지나간다).</summary>
        public Vector3 Across => (_index1.position - _pinky1.position).normalized;

        /// <summary>
        /// 손바닥이 보는 쪽. 오른손 손바닥을 아래로 앞으로 뻗으면 엄지(검지)는 왼쪽이라 손가락 방향 × (새끼→검지) = 아래,
        /// 왼손은 엄지가 오른쪽이라 순서를 바꾼다. 뼈 위치만 쓰니 뼈 로컬 축 규칙과 상관없다.
        /// </summary>
        public Vector3 PalmNormal
        {
            get
            {
                var dir = _middle1.position - _hand.position;
                var across = _index1.position - _pinky1.position;
                return (_left ? Vector3.Cross(across, dir) : Vector3.Cross(dir, across)).normalized;
            }
        }

        /// <summary>쥔 것의 축이 지나갈 자리: 손바닥 가운데에서 손바닥 앞으로 2cm (예전에는 손바닥 속을 지나 손가락이 손에 박혔다).</summary>
        public Vector3 Grip => Palm + PalmNormal * 0.02f;

        /// <summary>손을 돌려 새끼→검지 방향을 axis에 맞춘다 (weight 0~1). 손목만 돈다.</summary>
        public void AlignTo(Vector3 axis, float weight = 1f)
        {
            if (weight <= 0f) return;
            var q = Quaternion.FromToRotation(Across, axis.normalized);
            _hand.rotation = Quaternion.Slerp(Quaternion.identity, q, Mathf.Clamp01(weight)) * _hand.rotation;
        }

        /// <summary>손을 돌려 손바닥이 normal 쪽을 보게 한다 (작은 고기를 손바닥에 올려 보일 때).</summary>
        public void AlignPalm(Vector3 normal, float weight = 1f)
        {
            if (weight <= 0f) return;
            var q = Quaternion.FromToRotation(PalmNormal, normal.normalized);
            _hand.rotation = Quaternion.Slerp(Quaternion.identity, q, Mathf.Clamp01(weight)) * _hand.rotation;
        }

        /// <summary>손가락을 편다 (애니메이션이 말아 쥔 손가락을 손바닥과 나란히): 마디마다 다음 마디가 앞 마디 방향을 잇게 돌린다.</summary>
        public void Open(float weight = 1f)
        {
            if (weight <= 0f) return;
            var palmDir = (_middle1.position - _hand.position).normalized;
            foreach (var chain in _fingers)
            {
                var prev = palmDir;
                for (int j = 0; j < chain.Length; j++)
                {
                    var end = j + 1 < chain.Length ? chain[j + 1].position : Tip(chain);
                    var cur = end - chain[j].position;
                    var q = Quaternion.FromToRotation(cur, prev);
                    chain[j].rotation = Quaternion.Slerp(Quaternion.identity, q, Mathf.Clamp01(weight)) * chain[j].rotation;
                    var next = j + 1 < chain.Length ? chain[j + 1].position : Tip(chain);
                    prev = (next - chain[j].position).normalized;
                }
            }
        }

        /// <summary>손가락을 축(점 p, 방향 dir)에 감는다. r = 쥔 것의 반지름 + 손가락 두께 절반.</summary>
        public void Wrap(Vector3 p, Vector3 dir, float r = 0.032f)
        {
            dir.Normalize();
            var n = PalmNormal;
            foreach (var chain in _fingers) Curl(chain, p, dir, r, n, 105f);
            if (_thumb != null) Curl(_thumb, p, dir, r, n, 60f);
        }

        /// <summary>
        /// 손가락 하나를 제 굽힘 축으로 굽힌다. 축 = 첫 마디 방향 × 손바닥 법선 (이 축으로 +로 돌면 손가락이 손바닥 쪽으로 굽는다).
        /// 마디마다 -15~maxDeg 사이에서 다음 마디(끝 마디면 손끝)가 쥔 것의 축에서 r 거리에 가장 가까워지는 각도를 고른다.
        /// </summary>
        static void Curl(Transform[] chain, Vector3 p, Vector3 dir, float r, Vector3 n, float maxDeg)
        {
            var hinge = Vector3.Cross(chain[1].position - chain[0].position, n);
            if (hinge.sqrMagnitude < 1e-10f) return;
            hinge.Normalize();
            for (int j = 0; j < chain.Length; j++)
            {
                var joint = chain[j].position;
                var end = j + 1 < chain.Length ? chain[j + 1].position : Tip(chain);
                float best = 0f, bestErr = Mathf.Abs(AxisDist(end, p, dir) - r);
                for (float a = -15f; a <= maxDeg; a += 5f)
                {
                    float err = Mathf.Abs(AxisDist(Rotate(end, joint, hinge, a), p, dir) - r) + Mathf.Abs(a) * 0.00005f;
                    if (err < bestErr) { bestErr = err; best = a; }
                }
                if (best != 0f) chain[j].rotation = Quaternion.AngleAxis(best, hinge) * chain[j].rotation;
            }
        }

        /// <summary>끝 마디 끝 (03 마디를 같은 방향으로 0.8배 연장).</summary>
        static Vector3 Tip(Transform[] chain)
        {
            var a = chain[chain.Length - 2].position;
            var b = chain[chain.Length - 1].position;
            return b + (b - a) * 0.8f;
        }

        static Vector3 Rotate(Vector3 pt, Vector3 pivot, Vector3 axis, float deg) => pivot + Quaternion.AngleAxis(deg, axis) * (pt - pivot);

        static float AxisDist(Vector3 pt, Vector3 p, Vector3 dir) => Vector3.ProjectOnPlane(pt - p, dir).magnitude;
    }
}
