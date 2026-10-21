using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 낚싯대를 쥔 오른손 자리 (내 캐릭터 PlayerController와 NPC 낚시꾼 NpcAngler가 같이 쓴다).
    /// 낚싯대 각도가 먼저고 손이 따라간다: 2 = 던진 직후 앞으로 뻗음, 35 = 허리 앞 대기, 82 = 챔질(가슴 앞에 세움), 155 = 어깨 너머로 젖힘.
    /// </summary>
    public static class RodArm
    {
        /// <summary>
        /// 낚싯대 기울기별 오른 손목 자리 (오른 어깨 기준, 모델 축: x 오른쪽, y 위, z 앞).
        /// 실제 낚시꾼처럼 (사용자 사진, 2026-10-21): 오른손은 릴 시트를 쥐고 명치~가슴 앞 25~30cm, 팔꿈치는 몸 옆 아래로 굽힌다.
        /// 어깨~손목 거리를 팔 길이(0.45m)보다 짧게 둬서 팔이 굽는다 — 예전 대기(35°) 자리는 어깨에서 0.47m 아래 앞(엉덩이 옆)이라
        /// 팔이 다 펴진 채 허리에 붙고, 그 뒤 손잡이를 쥐는 왼팔이 배를 파고들었다.
        /// </summary>
        static readonly (float pitch, Vector3 off)[] GripCurve =
        {
            (0f,   new Vector3(-0.05f, -0.14f, 0.40f)),
            (35f,  new Vector3( 0.00f, -0.24f, 0.29f)),
            (82f,  new Vector3(-0.04f, -0.12f, 0.32f)),
            (120f, new Vector3(-0.03f,  0.04f, 0.27f)),
            (155f, new Vector3( 0.05f,  0.20f, 0.08f)),
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
            Vector3.Lerp(new Vector3(0.45f, -0.88f, -0.15f), new Vector3(0.75f, 0.05f, 0.65f), Mathf.InverseLerp(90f, 155f, pitch));

        /// <summary>팔 길이 비율: 손 자리 표는 기본 캐릭터 팔에 맞춘 값이라, 팔이 짧은 캐릭터는 그만큼 줄인다.</summary>
        public static float ArmScale(Transform upper, Transform lower, Transform hand)
        {
            if (upper == null || lower == null || hand == null) return 1f;
            float arm = (lower.position - upper.position).magnitude + (hand.position - lower.position).magnitude;
            return arm > 0.1f ? Mathf.Clamp(arm / RefArmLength, 0.6f, 1.3f) : 1f;
        }
    }
}
