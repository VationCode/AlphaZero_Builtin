using Alpha.Player.Animation;
using Alpha.Player.Locomotion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Audio
{
    // Animation Event Key와 재생할 AudioClip 후보를 연결한다.
    [Serializable]
    public sealed class PlayerAnimationAudioSetting
    {
        [SerializeField]
        private string _key;

        [SerializeField]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [NonSerialized]
        private int _lastClipIndex = -1;

        public float Volume => _volume;

        public bool Matches(string p_key)
        {
            return !string.IsNullOrWhiteSpace(_key) &&
                   string.Equals(
                       _key.Trim(),
                       p_key,
                       StringComparison.Ordinal);
        }

        public bool TryGetClip(out AudioClip p_clip)
        {
            p_clip = null;

            if (_clips == null || _clips.Length == 0)
                return false;

            int index = UnityEngine.Random.Range(0, _clips.Length);

            if (_clips.Length > 1 && index == _lastClipIndex)
            {
                index = (index + UnityEngine.Random.Range(
                    1,
                    _clips.Length)) % _clips.Length;
            }

            p_clip = _clips[index];

            if (p_clip == null)
                return false;

            _lastClipIndex = index;
            return true;
        }
    }

    // Player Locomotion의 상태음과 발소리 표현을 담당한다.
    public sealed class PlayerLocomotionAudioView : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        [Header("Animation Event")]
        [SerializeField]
        private PlayerAnimationAudioSetting[] _animationEvents;

        [Header("State Fallback")]
        [Tooltip("현재 Flight Animation View가 없어 Rising 상태로 재생한다.")]
        [SerializeField]
        private AudioClip _flyUpClip;

        [SerializeField, Range(0f, 1f)]
        private float _actionVolume = 1f;

        [Header("Footstep")]
        [SerializeField]
        private AudioClip[] _footstepClips;

        [SerializeField, Range(0f, 1f)]
        private float _footstepVolume = 0.65f;

        private LocomotionContext _context;
        private PlayerAnimationView _animationView;
        private readonly HashSet<string> _missingKeyWarnings = new();
        private int _lastFootstepIndex = -1;
        private bool _isSubscribed;

        private void Awake()
        {
            _audioSource ??= GetComponent<AudioSource>();

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        // PlayerCore가 Locomotion 상태와 Animation Event Key 발행 View를 연결한다.
        public void Bind(
            LocomotionContext p_context,
            PlayerAnimationView p_animationView)
        {
            if (ReferenceEquals(_context, p_context) &&
                ReferenceEquals(_animationView, p_animationView))
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            _context = p_context;
            _animationView = p_animationView;
            _missingKeyWarnings.Clear();
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _context = null;
            _animationView = null;
            _missingKeyWarnings.Clear();
        }

        private void Subscribe()
        {
            if (_isSubscribed || !isActiveAndEnabled)
                return;

            if (_context != null)
                _context.OnStateChanged += HandleStateChanged;

            if (_animationView != null)
            {
                _animationView.OnAudioKeyRequested +=
                    HandleAudioKeyRequested;
            }

            _isSubscribed = _context != null ||
                            _animationView != null;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            if (_context != null)
                _context.OnStateChanged -= HandleStateChanged;

            if (_animationView != null)
            {
                _animationView.OnAudioKeyRequested -=
                    HandleAudioKeyRequested;
            }

            _isSubscribed = false;
        }

        // Flight Animation View가 없는 Rising 상태만 기존 상태 기반으로 재생한다.
        private void HandleStateChanged(ELocomotionMode p_mode, ELocoStateType p_state)
        {
            if (p_mode == ELocomotionMode.Flight &&
                p_state == ELocoStateType.Rising)
            {
                PlayOneShot(_flyUpClip, _actionVolume);
            }
        }

        // 요청 Key와 일치하는 Audio 설정을 재생한다.
        private void HandleAudioKeyRequested(string p_key)
        {
            bool hasMatch = false;

            if (_animationEvents != null)
            {
                foreach (PlayerAnimationAudioSetting setting in
                         _animationEvents)
                {
                    if (setting == null || !setting.Matches(p_key))
                        continue;

                    hasMatch = true;

                    if (setting.TryGetClip(out AudioClip clip))
                        PlayOneShot(clip, setting.Volume);
                }
            }

            if (!hasMatch && _missingKeyWarnings.Add(p_key))
            {
                Debug.LogWarning(
                    $"Animation Audio Event Key 설정을 찾을 수 없습니다: {p_key}",
                    this);
            }
        }

        // AnimationView의 보행 주기 알림을 실제 발소리로 표현한다.
        public void PlayFootstep()
        {
            if (_context == null ||
                _context.CurrentMode != ELocomotionMode.Ground ||
                _context.CurrentState != ELocoStateType.Move ||
                !_context.IsGrounded ||
                _footstepClips == null ||
                _footstepClips.Length == 0)
            {
                return;
            }

            int index = GetNextFootstepIndex();
            AudioClip clip = _footstepClips[index];

            if (clip == null)
                return;

            _lastFootstepIndex = index;
            PlayOneShot(clip, _footstepVolume);
        }

        private int GetNextFootstepIndex()
        {
            if (_footstepClips.Length == 1)
                return 0;

            int index = UnityEngine.Random.Range(
                0,
                _footstepClips.Length);

            if (index == _lastFootstepIndex)
            {
                index = (index + UnityEngine.Random.Range(
                    1,
                    _footstepClips.Length)) % _footstepClips.Length;
            }

            return index;
        }

        private void PlayOneShot(AudioClip p_clip, float p_volume)
        {
            if (_audioSource == null || p_clip == null)
                return;

            _audioSource.PlayOneShot(p_clip, p_volume);
        }
    }
}
