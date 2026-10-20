using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>팔 2관절 IK (어깨·팔꿈치·손목). 내 캐릭터(PlayerController)와 NPC 낚시꾼(NpcAngler)이 같이 쓴다 — 10 문서 5장.</summary>
    public static class ArmIK
    {
        /// <summary>2관절 IK: 위팔·아래팔 길이는 그대로 두고 손이 target에 닿게 돌린다 (팔꿈치는 지금 굽은 면 그대로).</summary>
        public static void Solve(Transform a, Transform b, Transform c, Vector3 target)
        {
            var pa = a.position;
            var pb = b.position;
            var pc = c.position;
            float lab = (pb - pa).magnitude, lcb = (pc - pb).magnitude;
            float lat = Mathf.Clamp((target - pa).magnitude, 0.01f, lab + lcb - 0.002f);
            float acab0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((pc - pa).normalized, (pb - pa).normalized), -1f, 1f));
            float babc0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((pa - pb).normalized, (pc - pb).normalized), -1f, 1f));
            float acab1 = Mathf.Acos(Mathf.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2f * lab * lat), -1f, 1f));
            float babc1 = Mathf.Acos(Mathf.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2f * lab * lcb), -1f, 1f));
            var axis = Vector3.Cross(pc - pa, pb - pa);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(pc - pa, Vector3.up);
            axis.Normalize();
            a.rotation = Quaternion.AngleAxis((acab1 - acab0) * Mathf.Rad2Deg, axis) * a.rotation;
            b.rotation = Quaternion.AngleAxis((babc1 - babc0) * Mathf.Rad2Deg, axis) * b.rotation;
            a.rotation = Quaternion.FromToRotation(c.position - pa, target - pa) * a.rotation;
        }

        /// <summary>2관절 IK + 팔꿈치 방향: 풀고 나서 어깨→손 축을 중심으로 팔을 돌려 팔꿈치가 pole(방향) 쪽을 보게 한다 (weight만큼).</summary>
        public static void Solve(Transform a, Transform b, Transform c, Vector3 target, Vector3 pole, float weight)
        {
            Solve(a, b, c, target);
            var axis = c.position - a.position;
            if (axis.sqrMagnitude < 1e-6f) return;
            var elbow = Vector3.ProjectOnPlane(b.position - a.position, axis);
            var want = Vector3.ProjectOnPlane(pole, axis);
            if (elbow.sqrMagnitude < 1e-8f || want.sqrMagnitude < 1e-8f) return;
            float angle = Vector3.SignedAngle(elbow, want, axis) * Mathf.Clamp01(weight);
            a.rotation = Quaternion.AngleAxis(angle, axis.normalized) * a.rotation;
        }
    }
}
