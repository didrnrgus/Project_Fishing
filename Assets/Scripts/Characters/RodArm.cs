using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 낚싯대를 쥔 오른손 자리 (내 캐릭터 PlayerController와 NPC 낚시꾼 NpcAngler가 같이 쓴다).
    /// 낚싯대 각도가 먼저고 손이 따라간다: 2 = 던진 직후 앞으로 뻗음, 35 = 허리 앞 대기, 82 = 챔질(가슴 앞에 세움), 155 = 어깨 너머로 젖힘.
    /// </summary>
    public static class RodArm
    {
        /// <summary>낚싯대 기울기별 오른 손목 자리 (오른 어깨 기준, 모델 축: x 오른쪽, y 위, z 앞).</summary>
        static readonly (float pitch, Vector3 off)[] GripCurve =
        {
            (0f,   new Vector3(-0.06f, -0.20f, 0.44f)),
            (35f,  new Vector3(-0.03f, -0.38f, 0.27f)),
            (82f,  new Vector3(-0.08f, -0.10f, 0.33f)),
            (120f, new Vector3(-0.04f,  0.10f, 0.22f)),
            (155f, new Vector3( 0.05f,  0.22f, 0.06f)),
        };

        /// <summary>손 자리 표(GripCurve)를 맞춘 예전 기본 캐릭터의 오른팔 길이 (어깨~손목, m). VRM 팔 길이를 이것과 비교해 늘이거나 줄인다.</summary>
        const float RefArmLength = 0.453f;

        public static Vector3 GripOffset(float pitch)
        {
            if (pitch <= GripCurve[0].pitch) return GripCurve[0].off;
            for (int i = 1; i < GripCurve.Length; i++)
            {
                if (pitch > GripCurve[i].pitch) continue;
                var (p0, o0) = GripCurve[i - 1];
                var (p1, o1) = GripCurve[i];
                return Vector3.Lerp(o0, o1, Mathf.SmoothStep(0f, 1f, (pitch - p0) / (p1 - p0)));
            }
            return GripCurve[GripCurve.Length - 1].off;
        }

        /// <summary>오른 팔꿈치가 향할 쪽 (모델 축): 들고 있을 때는 아래 바깥, 어깨 너머로 젖힐수록 앞 바깥으로 든다.</summary>
        public static Vector3 ElbowPole(float pitch) =>
            Vector3.Lerp(new Vector3(0.7f, -0.7f, -0.1f), new Vector3(0.75f, 0.05f, 0.65f), Mathf.InverseLerp(70f, 155f, pitch));

        /// <summary>팔 길이 비율: 손 자리 표는 기본 캐릭터 팔에 맞춘 값이라, 팔이 짧은 캐릭터는 그만큼 줄인다.</summary>
        public static float ArmScale(Transform upper, Transform lower, Transform hand)
        {
            if (upper == null || lower == null || hand == null) return 1f;
            float arm = (lower.position - upper.position).magnitude + (hand.position - lower.position).magnitude;
            return arm > 0.1f ? Mathf.Clamp(arm / RefArmLength, 0.6f, 1.3f) : 1f;
        }
    }
}
