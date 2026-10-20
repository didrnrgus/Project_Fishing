using Fishing.Core;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 원작 1인칭 파이팅의 두 팔: 맨살 아래팔 + 손가락 없는 검정 반장갑(주황 손목띠).
    ///  - 오른손은 릴 시트 바로 아래 손잡이를 쥐고, 왼손은 릴 손잡이 노브를 쥔다 (감으면 노브를 따라 손이 원을 그린다)
    ///  - 팔꿈치는 화면 아래 바깥에 두고 손목까지 아래팔을 잇는다 → 낚싯대를 좌우로 틀면 팔이 따라 쓸려 간다
    ///  - 낚싯대 자식이 아니라서 장력이 높아 낚싯대가 금빛으로 달아올라도 팔·장갑은 그대로다 (원작과 같음)
    /// </summary>
    public class FirstPersonArms : MonoBehaviour
    {
        // 카메라 공간에서 손목 → 팔꿈치 방향 (화면 아래 바깥, 몸 쪽)
        static readonly Vector3 RightElbow = new Vector3(0.11f, -0.3f, -0.2f);
        static readonly Vector3 LeftElbow = new Vector3(-0.17f, -0.29f, -0.17f);
        const float HandLen = 0.07f;

        Arm _right, _left;

        class Arm
        {
            public Transform Forearm, Hand;
            public float Mirror;
        }

        public static FirstPersonArms Create(Transform parent)
        {
            var go = new GameObject("FirstPersonArms");
            go.transform.SetParent(parent, false);
            var arms = go.AddComponent<FirstPersonArms>();
            arms.Build();
            return arms;
        }

        void Build()
        {
            var skin = Mats.Lit(new Color(1f, 0.78f, 0.62f), 0.35f);
            var glove = Mats.Lit(new Color(0.09f, 0.09f, 0.11f), 0.35f);
            var band = Mats.Lit(new Color(0.95f, 0.55f, 0.15f), 0.3f);
            _right = BuildArm("Right", 1f, skin, glove, band);
            _left = BuildArm("Left", -1f, skin, glove, band);

            // 밤에도 팔이 회색으로 죽지 않도록 손 앞쪽에 따뜻한 보조광 (원작 야간 1인칭도 팔·장갑이 밝게 보인다)
            _light = new GameObject("ArmFill").AddComponent<Light>();
            _light.transform.SetParent(transform, false);
            _light.type = LightType.Point;
            _light.range = 1.3f;
            _light.intensity = 2f;
            _light.color = new Color(1f, 0.88f, 0.75f);
            _light.shadows = LightShadows.None;
        }

        Light _light;

        Arm BuildArm(string name, float mirror, Material skin, Material glove, Material band)
        {
            var arm = new Arm { Mirror = mirror };
            // 아래팔: 팔꿈치(굵음) → 손목(가늚), 단면은 납작한 타원. 길이는 매 프레임 Z 스케일로 맞춘다
            arm.Forearm = MeshObj(name + "Forearm", transform, Lathe(new[] { 0.041f, 0.047f, 0.048f, 0.045f, 0.04f, 0.034f, 0.03f, 0.028f }, 0.8f), skin).transform;

            // 손: 손목 원점, +Z = 손끝 방향, +Y = 쥔 막대(낚싯대·노브) 축
            arm.Hand = new GameObject(name + "Hand").transform;
            arm.Hand.SetParent(transform, false);
            // 손목띠와 장갑 손등(주먹 덩어리)
            var b = MeshObj("Band", arm.Hand, Lathe(new[] { 0.031f, 0.033f, 0.033f, 0.031f }, 0.82f), band);
            b.transform.localPosition = new Vector3(0f, 0f, -0.012f);
            b.transform.localScale = new Vector3(1f, 1f, 0.026f);
            var fist = MeshObj("Glove", arm.Hand, Lathe(new[] { 0.03f, 0.036f, 0.04f, 0.041f, 0.039f, 0.033f, 0.022f }, 0.7f), glove);
            fist.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            fist.transform.localScale = new Vector3(1f, 1.15f, HandLen + 0.01f);
            // 반장갑 밖으로 나온 손가락 네 개: 쥔 막대 앞쪽을 가로로 감싼다 (주먹 쥔 손을 뒤에서 본 모양)
            for (int i = 0; i < 4; i++)
            {
                float y = (i - 1.5f) * 0.0175f;
                float len = i == 0 || i == 3 ? 0.014f : 0.017f;
                Mats.Prim(PrimitiveType.Capsule, "Finger", arm.Hand, new Vector3(-0.004f * mirror, y, HandLen + 0.017f), new Vector3(0.016f, len, 0.016f),
                    skin, false, Quaternion.Euler(0f, 0f, 90f));
            }
            // 엄지: 막대 위를 따라 눌러 잡는다
            Mats.Prim(PrimitiveType.Capsule, "Thumb", arm.Hand, new Vector3(0.014f * mirror, 0.03f, HandLen - 0.016f), new Vector3(0.017f, 0.019f, 0.017f),
                skin, false, Quaternion.Euler(-12f, 0f, 0f));
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return arm;
        }

        /// <summary>
        /// 손이 쥘 곳(grip, 월드)과 쥔 막대의 축(axis, 월드)을 받아 손·아래팔을 놓는다.
        /// 손은 팔꿈치에서 쥘 곳을 향하고, 손끝 쪽이 막대를 감싼다.
        /// </summary>
        void Place(Arm arm, Vector3 grip, Vector3 axis, Vector3 elbowOffset)
        {
            var cam = transform.parent;
            var elbow = grip + cam.TransformVector(elbowOffset);
            var dir = (grip - elbow).normalized;
            // 손등이 막대 축을 따라 눕도록: 손끝(Z)은 팔 방향에서 막대 축 성분을 뺀 쪽
            var up = Vector3.ProjectOnPlane(axis, dir);
            if (up.sqrMagnitude < 1e-5f) up = cam.up;
            var wrist = grip - dir * HandLen;
            arm.Hand.SetPositionAndRotation(wrist, Quaternion.LookRotation(dir, up.normalized));
            var toWrist = wrist - elbow;
            arm.Forearm.SetPositionAndRotation(elbow, Quaternion.LookRotation(toWrist, cam.up));
            arm.Forearm.localScale = new Vector3(1f, 1f, toWrist.magnitude + 0.01f);
        }

        /// <summary>FightRodView.LateUpdate가 낚싯대 자세를 정한 뒤 부른다.</summary>
        public void Follow(TelescopicRod rod)
        {
            var rt = rod.transform;
            // 오른손: 릴 시트 바로 아래 손잡이 (릴 다리를 손가락 사이에 끼운다)
            Place(_right, rt.TransformPoint(new Vector3(0f, -0.045f, -0.005f)), rt.up, RightElbow);
            // 왼손: 릴 손잡이 노브. 감으면 노브를 따라 손이 돈다
            _light.transform.localPosition = transform.parent.InverseTransformPoint(rt.position) + new Vector3(0f, 0.12f, -0.22f);
            var knob = rod.HandleKnob;
            if (knob != null) Place(_left, knob.position, knob.up, LeftElbow);
        }

        static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>+Z 0~1을 따라 반지름이 바뀌는 회전체 (단면 타원: 가로 r, 세로 r×squash). 양 끝은 막는다.</summary>
        static Mesh Lathe(float[] radii, float squash, int seg = 16)
        {
            int rings = radii.Length;
            var v = new Vector3[rings * seg + 2];
            for (int i = 0; i < rings; i++)
            {
                float z = i / (float)(rings - 1);
                for (int s = 0; s < seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    v[i * seg + s] = new Vector3(Mathf.Cos(a) * radii[i], Mathf.Sin(a) * radii[i] * squash, z);
                }
            }
            int c0 = rings * seg, c1 = c0 + 1;
            v[c0] = new Vector3(0f, 0f, -0.01f);
            v[c1] = new Vector3(0f, 0f, 1.01f);
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < rings - 1; i++)
                for (int s = 0; s < seg; s++)
                {
                    int a = i * seg + s, b = i * seg + (s + 1) % seg, c = a + seg, d = b + seg;
                    tris.AddRange(new[] { a, b, c, b, d, c });
                }
            for (int s = 0; s < seg; s++)
            {
                tris.AddRange(new[] { c0, (s + 1) % seg, s });
                int top = (rings - 1) * seg;
                tris.AddRange(new[] { c1, top + s, top + (s + 1) % seg });
            }
            var m = new Mesh { vertices = v, triangles = tris.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
