using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fishing.Characters
{
    /// <summary>
    /// AnimatorController 에셋 없이 Playables로 클립을 섞어 재생한다.
    /// 클립은 Universal Animation Library(Resources/Animations/UAL1_Standard),
    /// 직접 만든 반응 동작(Resources/Animations/Reactions, tools/blender/build_reactions.py),
    /// Human Basic Motions(Resources/Animations/KevinIglesias, 남·녀 따로)에서 이름으로 찾는다.
    /// 낚싯대를 든 팔은 클립이 아니라 IK가 정한다 (PlayerController.UpdateRodArmRight, NpcAngler.UpdateRightHand).
    /// "이름_M"/"이름_F" 클립이 있으면 Female에 맞는 쪽을 먼저 쓴다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        public const string Idle = "Basic_Idle_Loop";
        public const string Walk = "Basic_Walk_Loop";
        /// <summary>Walk 클립이 원래 가는 속도 (m/s, 10 문서 7장). 다른 속도로 걸릴 때 배속 = 속도 / 이 값.</summary>
        public const float BasicWalkSpeed = 2f;
        public const string Jog = "Basic_Run_Loop";
        public const string Sprint = "Basic_Sprint_Loop";

        /// <summary>클립 원래 이동 속도(m/s). 게임 이동 속도 ÷ 이 값 = 재생 배속 (발 미끄러짐 최소)</summary>
        public const float WalkClipSpeed = 2f, JogClipSpeed = 4f, SprintClipSpeed = 6f;
        /// <summary>낚싯대를 들고 서 있을 때 몸 (UAL 대기). 두 팔은 IK가 낚싯대에 붙인다.</summary>
        public const string HoldRod = "Idle_Loop";
        public const string Cheer = "Cheer_ArmsUp";
        public const string Talk = "Basic_Talk_Loop";
        public const string Clap = "Clap_Loop";
        public const string Sit = "Sitting_Idle_Loop";
        public const string Interact = "Interact";
        public const string Dance = "Dance_Loop";
        public const string Drive = "Driving_Loop";
        public const string JumpStart = "Basic_JumpBegin";
        public const string JumpLoop = "Basic_Fall_Loop";
        public const string TurnLeft = "Basic_TurnLeft_Loop";
        public const string TurnRight = "Basic_TurnRight_Loop";
        public const string Excited = "Excited";
        public const string Wave = "Wave_Call";
        public const string Scared = "Scared";

        /// <summary>그 이름의 클립이 있는지 (받은 에셋을 지운 프로젝트에서는 없을 수 있다).</summary>
        public static bool Has(string clipName) => Clips.ContainsKey(clipName) || Clips.ContainsKey(clipName + "_M");

        static Dictionary<string, AnimationClip> _clips;

        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        readonly List<string> _names = new List<string>();
        readonly List<AnimationClipPlayable> _playables = new List<AnimationClipPlayable>();
        int _target = -1;
        float _fadeSpeed = 5f;
        string _loopState = Idle;
        float _loopSpeed = 1f;   // 한 번짜리 동작이 끝나고 반복 상태로 돌아갈 때 배속 (걷기 속도 맞춤 유지)
        float _oneShotUntil;

        /// <summary>여자 캐릭터면 "_F" 클립을 쓴다. CharacterFactory가 AddComponent 직후 정한다(첫 재생은 Start).</summary>
        public bool Female;

        public string Current => _target >= 0 ? _names[_target] : "";

        static Dictionary<string, AnimationClip> Clips
        {
            get
            {
                if (_clips != null) return _clips;
                _clips = new Dictionary<string, AnimationClip>();
                foreach (var folder in new[] { "Animations/UAL1_Standard", "Animations/Reactions", "Animations/KevinIglesias" })
                    foreach (var c in Resources.LoadAll<AnimationClip>(folder))
                    {
                        var name = c.name.Contains("|") ? c.name.Substring(c.name.LastIndexOf('|') + 1) : c.name;
                        if (!_clips.ContainsKey(name)) _clips[name] = c;
                    }
                return _clips;
            }
        }

        void Awake()
        {
            var animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            _graph = PlayableGraph.Create(name + "_Anim");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 0);
            var output = AnimationPlayableOutput.Create(_graph, "Output", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        void Start()
        {
            if (_target < 0) Play(_loopState, 0f);
        }

        void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        bool TryGetClip(string clipName, out AnimationClip clip) =>
            Clips.TryGetValue(clipName + (Female ? "_F" : "_M"), out clip) || Clips.TryGetValue(clipName, out clip);

        int IndexOf(string clipName)
        {
            int i = _names.IndexOf(clipName);
            if (i >= 0) return i;
            if (!TryGetClip(clipName, out var clip)) return -1;
            var p = AnimationClipPlayable.Create(_graph, clip);
            if (!clip.isLooping) p.SetDuration(clip.length);
            _mixer.SetInputCount(_names.Count + 1);
            _graph.Connect(p, 0, _mixer, _names.Count);
            _mixer.SetInputWeight(_names.Count, 0f);
            _names.Add(clipName);
            _playables.Add(p);
            return _names.Count - 1;
        }

        /// <summary>
        /// 반복 상태 전환 (대기, 걷기, 낚싯대 들기 …). 한 번짜리 동작 중이면 끝난 뒤 이 상태로 돌아간다.
        /// 반복하지 않는 클립을 주면 처음부터 한 번 재생하고 끝 자세에서 멈춰 있다.
        /// </summary>
        public void SetLoop(string clipName, float speed = 1f)
        {
            _loopState = clipName;
            _loopSpeed = speed;
            if (Time.time < _oneShotUntil) return;
            Play(clipName, 0.2f, speed);
        }

        /// <summary>캐스팅·챔질 같은 한 번짜리 동작. 끝나면 반복 상태로 돌아간다.</summary>
        /// <summary>PlayOnce로 튼 클립이 아직 도는 중 (그동안은 팔 IK를 끈다).</summary>
        public bool PlayingOnce => Time.time < _oneShotUntil;

        public void PlayOnce(string clipName, float speed = 1f, float cutAt = 1f)
        {
            if (!TryGetClip(clipName, out var clip)) return;
            Play(clipName, 0.08f, speed, restart: true);
            _oneShotUntil = Time.time + clip.length * cutAt / Mathf.Max(0.01f, speed);
        }

        void Play(string clipName, float fade, float speed = 1f, bool restart = false)
        {
            int i = IndexOf(clipName);
            if (i < 0) { Debug.LogWarning($"[Dev] 클립 없음: {clipName} ({name})"); return; }
            var p = _playables[i];
            p.SetSpeed(speed);
            // 반복하지 않는 클립은 다시 들어올 때마다 처음부터
            if (!restart && i != _target && !p.GetAnimationClip().isLooping) restart = true;
            if (restart || i != _target) p.SetTime(restart ? 0 : p.GetTime());
            if (restart) p.SetDone(false);
            _target = i;
            _fadeSpeed = fade <= 0f ? 1000f : 1f / fade;
            if (fade <= 0f)
                for (int k = 0; k < _names.Count; k++) _mixer.SetInputWeight(k, k == i ? 1f : 0f);
        }

        void Update()
        {
            if (_target < 0) return;
            if (_oneShotUntil > 0f && Time.time >= _oneShotUntil)
            {
                _oneShotUntil = 0f;
                Play(_loopState, 0.25f, _loopSpeed);
            }
            float step = Time.deltaTime * _fadeSpeed;
            for (int k = 0; k < _names.Count; k++)
            {
                float w = _mixer.GetInputWeight(k);
                _mixer.SetInputWeight(k, Mathf.MoveTowards(w, k == _target ? 1f : 0f, step));
            }
        }
    }
}
