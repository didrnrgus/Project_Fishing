using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 오른손으로 낚싯대를 쥐는 모양. 예전에는 낚싯대 기준점을 손목 관절(hand_r)에 붙이고 손가락은 애니메이션 그대로 펴 두어서,
    /// 서 있을 때 낚싯대가 손 옆 8cm쯤 허공에 떠 있고 빈손처럼 보였다.
    /// - <see cref="Palm"/>: 손목과 가운뎃손가락 뿌리 사이(손바닥 가운데)에 낚싯대 릴 시트를 둔다.
    /// - <see cref="Wrap"/>: 애니메이션이 끝난 뒤(LateUpdate) 손가락 마디를 낚싯대 축을 중심으로 돌려, 끝이 손잡이 겉면(반지름 r)에 닿게 감싼다.
    ///   축 방향은 낚싯대 방향이라 뼈의 로컬 축 규칙과 상관없이 맞다. 말아 쥐는 쪽(부호)은 가운뎃손가락으로 한 번 정해 모든 손가락에 쓴다.
    /// </summary>
    public class RodGrip
    {
        readonly Transform _hand, _middle1;
        readonly Transform[][] _fingers;   // 검지·중지·약지·소지: 01, 02, 03
        readonly Transform[] _thumb;       // 01, 02, 03

        RodGrip(Transform hand, Transform middle1, Transform[][] fingers, Transform[] thumb)
        {
            _hand = hand;
            _middle1 = middle1;
            _fingers = fingers;
            _thumb = thumb;
        }

        /// <summary>캐릭터 뼈에서 오른손 손가락을 찾는다. 하나라도 없으면 null.</summary>
        public static RodGrip Find(Transform character)
        {
            var all = character.GetComponentsInChildren<Transform>(true);
            Transform Bone(string n) => System.Array.Find(all, t => t.name == n);
            var hand = Bone("hand_r");
            if (hand == null) return FindHumanoid(character);
            string[] names = { "index", "middle", "ring", "pinky" };
            var fingers = new Transform[4][];
            for (int f = 0; f < 4; f++)
            {
                fingers[f] = new[] { Bone(names[f] + "_01_r"), Bone(names[f] + "_02_r"), Bone(names[f] + "_03_r") };
                if (System.Array.Exists(fingers[f], b => b == null)) return null;
            }
            var thumb = new[] { Bone("thumb_01_r"), Bone("thumb_02_r"), Bone("thumb_03_r") };
            if (System.Array.Exists(thumb, b => b == null)) thumb = null;
            return new RodGrip(hand, fingers[1][0], fingers, thumb);
        }

        /// <summary>받은 휴머노이드 캐릭터 (뼈 이름이 다르다): Animator의 휴머노이드 매핑으로 오른손·손가락을 찾는다.</summary>
        static RodGrip FindHumanoid(Transform character)
        {
            var an = character.GetComponentInChildren<Animator>();
            if (an == null || !an.isHuman) return null;
            Transform B(HumanBodyBones b) => an.GetBoneTransform(b);
            var hand = B(HumanBodyBones.RightHand);
            var fingers = new[]
            {
                new[] { B(HumanBodyBones.RightIndexProximal), B(HumanBodyBones.RightIndexIntermediate), B(HumanBodyBones.RightIndexDistal) },
                new[] { B(HumanBodyBones.RightMiddleProximal), B(HumanBodyBones.RightMiddleIntermediate), B(HumanBodyBones.RightMiddleDistal) },
                new[] { B(HumanBodyBones.RightRingProximal), B(HumanBodyBones.RightRingIntermediate), B(HumanBodyBones.RightRingDistal) },
                new[] { B(HumanBodyBones.RightLittleProximal), B(HumanBodyBones.RightLittleIntermediate), B(HumanBodyBones.RightLittleDistal) },
            };
            if (hand == null || System.Array.Exists(fingers, f => System.Array.Exists(f, x => x == null))) return null;
            var thumb = new[] { B(HumanBodyBones.RightThumbProximal), B(HumanBodyBones.RightThumbIntermediate), B(HumanBodyBones.RightThumbDistal) };
            if (System.Array.Exists(thumb, x => x == null)) thumb = null;
            return new RodGrip(hand, fingers[1][0], fingers, thumb);
        }

        /// <summary>손바닥 가운데 (낚싯대를 쥐는 자리).</summary>
        public Vector3 Palm => Vector3.Lerp(_hand.position, _middle1.position, 0.62f);

        /// <summary>손가락을 낚싯대(점 p, 방향 dir)에 감는다. r = 손잡이 반지름 + 손가락 두께 절반.</summary>
        public void Wrap(Vector3 p, Vector3 dir, float r = 0.032f)
        {
            dir.Normalize();
            // 말아 쥐는 방향: 가운뎃손가락 첫 마디를 +/- 40° 돌려 끝이 축에 더 가까워지는 쪽
            var m = _fingers[1];
            var mTip = Tip(m);
            float dPlus = AxisDist(Rotate(mTip, m[0].position, dir, 40f), p, dir);
            float dMinus = AxisDist(Rotate(mTip, m[0].position, dir, -40f), p, dir);
            float sign = dPlus < dMinus ? 1f : -1f;
            foreach (var chain in _fingers) WrapChain(chain, p, dir, r, sign, 100f);
            if (_thumb != null) WrapChain(_thumb, p, dir, r, sign, 60f);
        }

        /// <summary>마디마다 0~maxDeg 사이에서 다음 마디(끝 마디면 손끝)가 축에서 r 거리에 가장 가까워지는 각도를 찾아 돌린다.</summary>
        static void WrapChain(Transform[] chain, Vector3 p, Vector3 dir, float r, float sign, float maxDeg)
        {
            for (int j = 0; j < chain.Length; j++)
            {
                var joint = chain[j].position;
                var end = j + 1 < chain.Length ? chain[j + 1].position : Tip(chain);
                float best = 0f, bestErr = Mathf.Abs(AxisDist(end, p, dir) - r);
                for (float a = 5f; a <= maxDeg; a += 5f)
                {
                    float err = Mathf.Abs(AxisDist(Rotate(end, joint, dir, a * sign), p, dir) - r) + a * 0.00005f;
                    if (err < bestErr) { bestErr = err; best = a; }
                }
                if (best > 0f) chain[j].rotation = Quaternion.AngleAxis(best * sign, dir) * chain[j].rotation;
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
