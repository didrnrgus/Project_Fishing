using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fishing.EditorTools.Tests
{
    /// <summary>
    /// Starter Assets 로코모션(Resources/Animations/StarterAssets)을 실제 VRM에 재생해 다리 자세를 숫자로 잰다.
    /// 클립 FBX 임포트가 자기 아바타로 돌아가면 무릎이 뒤로 꺾이고 발끝이 옆을 보고, 반복이 꺼지면 달리다 굳는다 (StarterClipBake.FixImportSettings).
    /// </summary>
    public class LocomotionClipTests
    {
        const string Dir = "Assets/Resources/Animations/StarterAssets";
        static readonly string[] Locomotion = { "Idle", "Walk_N", "Run_N" };

        static AnimationClip Load(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Dir}/{name}.anim");

        [Test]
        public void StarterClipsLoopExceptJump()
        {
            foreach (var n in new[] { "Idle", "Walk_N", "Run_N", "InAir" })
                Assert.IsTrue(Load(n).isLooping, $"{n} 반복이 꺼져 있다 (한 바퀴 뒤 굳는다)");
            Assert.IsFalse(Load("Jump").isLooping);
        }

        [Test]
        public void StarterClipsKeepKneesAndToesForwardOnVrm()
        {
            var (go, _) = Fishing.Vrm.VrmLoader.Load(Path.GetFullPath("Assets/StreamingAssets/Characters/Base_Male.vrm"));
            try
            {
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // VrmCharacters.Create와 같다
                foreach (var name in Locomotion)
                {
                    var s = Sample(go, Load(name));
                    // 무릎: 사람은 앞으로만 굽는다. 다리를 쭉 편 순간 몇 도 넘는 정도는 체형 차이
                    Assert.Greater(s.kneeMin, -6f, $"{name} 무릎이 {s.kneeMin:F1}° 뒤로 꺾인다");
                    // 발끝: 옆으로 돌아가 있지 않다 (아바타 기준이 틀어지면 좌우 성분이 0.6~0.9)
                    Assert.Less(s.toeSide, 0.35f, $"{name} 발끝 좌우 성분 평균 {s.toeSide:F2}");
                    // 디딤발: T포즈 발목 높이에서 4cm 안 (발 IK를 끈 채라 뜨거나 묻히지 않는지)
                    Assert.Less(Mathf.Abs(s.groundMin), 0.04f, $"{name} 디딤발 높이 {s.groundMin * 100f:F1}cm");
                    // 대기에서 무릎을 계속 굽힌 채(엉거주춤) 서 있지 않다
                    if (name == "Idle") Assert.Less(s.kneeMedian, 15f, $"대기 무릎 중앙값 {s.kneeMedian:F1}°");
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>CharacterAnimator처럼 발 IK를 끄고 한 바퀴 재생하며 잰다. 무릎 굽힘(°)은 + 정상·− 과신전.</summary>
        static (float kneeMin, float kneeMedian, float toeSide, float groundMin) Sample(GameObject go, AnimationClip clip)
        {
            var anim = go.GetComponent<Animator>();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.Rebind();
            Transform B(HumanBodyBones b) => anim.GetBoneTransform(b);
            float bindAnkle = Mathf.Min(B(HumanBodyBones.LeftFoot).position.y, B(HumanBodyBones.RightFoot).position.y);

            var graph = PlayableGraph.Create("LocomotionClipTests");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var p = AnimationClipPlayable.Create(graph, clip);
                p.SetApplyFootIK(false);
                AnimationPlayableOutput.Create(graph, "o", anim).SetSourcePlayable(p);
                graph.Play();
                var knees = new List<float>();
                float toe = 0f, ground = 9f;
                int frames = Mathf.CeilToInt(clip.length * 30f);
                graph.Evaluate(0f);
                for (int f = 0; f < frames; f++)
                {
                    if (f > 0) graph.Evaluate(1f / 30f);
                    Vector3 right = (B(HumanBodyBones.RightUpperLeg).position - B(HumanBodyBones.LeftUpperLeg).position).normalized;
                    ground = Mathf.Min(ground, Mathf.Min(B(HumanBodyBones.LeftFoot).position.y, B(HumanBodyBones.RightFoot).position.y) - bindAnkle);
                    foreach (var (up, lo, ft, tp) in new[]
                             {
                                 (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes),
                                 (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes),
                             })
                    {
                        Vector3 thigh = B(lo).position - B(up).position, shin = B(ft).position - B(lo).position;
                        float ang = Vector3.Angle(thigh, shin);
                        knees.Add(Vector3.Dot(Vector3.Cross(thigh, shin), right) >= 0f ? ang : -ang);
                        toe += Mathf.Abs(Vector3.Dot((B(tp).position - B(ft).position).normalized, right));
                    }
                }
                knees.Sort();
                return (knees[0], knees[knees.Count / 2], toe / knees.Count, ground);
            }
            finally { graph.Destroy(); }
        }
    }
}
