using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fishing.Characters
{
    /// <summary>
    /// AnimatorController 에셋 없이 Playables로 클립을 섞어 재생한다.
    /// 클립은 Universal Animation Library(Resources/Animations/UAL1_Standard),
    /// Mixamo 낚시 동작(Resources/Animations/Mixamo, AssetImportRules.MixamoClips가 쪼갬),
    /// Human Basic Motions(Resources/Animations/KevinIglesias, 남·녀 따로)에서 이름으로 찾는다.
    /// "이름_M"/"이름_F" 클립이 있으면 Female에 맞는 쪽을 먼저 쓴다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        public const string Idle = "Basic_Idle_Loop";
        public const string Walk = "Basic_Walk_Loop";
        public const string Jog = "Basic_Run_Loop";
        public const string Sprint = "Basic_Sprint_Loop";

        /// <summary>클립 원래 이동 속도(m/s). 게임 이동 속도 ÷ 이 값 = 재생 배속 (발 미끄러짐 최소)</summary>
        public const float WalkClipSpeed = 2f, JogClipSpeed = 4f, SprintClipSpeed = 6f;
        public const string HoldRod = "Fishing_Idle_Loop";
        public const string Windup = "Fishing_Windup";
        public const string Cast = "Fishing_Cast";
        public const string Hook = "Fishing_Hook";
        public const string Reel = "Fishing_Reel_Loop";
        public const string FishingSequence = "Fishing_Sequence";
        public const string Cheer = "Cheer_ArmsUp";
        public const string Talk = "Basic_Talk_Loop";
        public const string Clap = "Clap_Loop";
        public const string Sit = "Sitting_Idle_Loop";
        public const string Interact = "Interact";
        public const string Dance = "Dance_Loop";
        public const string Drive = "Driving_Loop";
        public const string JumpStart = "Basic_JumpBegin";
        public const string JumpLoop = "Basic_Fall_Loop";

        static Dictionary<string, AnimationClip> _clips;

        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        readonly List<string> _names = new List<string>();
        readonly List<AnimationClipPlayable> _playables = new List<AnimationClipPlayable>();
        int _target = -1;
        float _fadeSpeed = 5f;
        string _loopState = Idle;
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
                foreach (var folder in new[] { "Animations/UAL1_Standard", "Animations/Mixamo", "Animations/KevinIglesias" })
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
        /// 반복하지 않는 클립(Windup)을 주면 처음부터 한 번 재생하고 끝 자세에서 멈춰 있다.
        /// </summary>
        public void SetLoop(string clipName, float speed = 1f)
        {
            _loopState = clipName;
            if (Time.time < _oneShotUntil) return;
            Play(clipName, 0.2f, speed);
        }

        /// <summary>캐스팅·챔질 같은 한 번짜리 동작. 끝나면 반복 상태로 돌아간다.</summary>
        public void PlayOnce(string clipName, float speed = 1f, float cutAt = 1f)
        {
            if (!TryGetClip(clipName, out var clip)) return;
            Play(clipName, 0.08f, speed, restart: true);
            _oneShotUntil = Time.time + clip.length * cutAt / Mathf.Max(0.01f, speed);
        }

        void Play(string clipName, float fade, float speed = 1f, bool restart = false)
        {
            int i = IndexOf(clipName);
            if (i < 0) return;
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
                Play(_loopState, 0.25f);
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
