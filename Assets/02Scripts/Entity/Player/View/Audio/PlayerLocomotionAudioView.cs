using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player.Audio
{
    // Player Locomotion의 상태음과 발소리 표현을 담당한다.
    public sealed class PlayerLocomotionAudioView : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        [Header("Action")]
        [SerializeField]
        private AudioClip _jumpClip;

        [SerializeField]
        private AudioClip _landClip;

        [SerializeField]
        private AudioClip _dashClip;

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
        private int _lastFootstepIndex = -1;

        private void Awake()
        {
            _audioSource = GetComponentInParent<AudioSource>();

            if (_audioSource == null)
                _audioSource = gameObject.GetComponentInParent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        // PlayerCore가 Locomotion 상태 변경 알림을 연결한다.
        public void Bind(LocomotionContext p_context)
        {
            if (ReferenceEquals(_context, p_context))
                return;

            Unbind();
            _context = p_context;

            if (_context != null)
                _context.OnStateChanged += HandleStateChanged;
        }

        public void Unbind()
        {
            if (_context != null)
                _context.OnStateChanged -= HandleStateChanged;

            _context = null;
        }

        // 상태가 실제로 확정된 시점에 대응하는 이동 효과음을 한 번 재생한다.
        private void HandleStateChanged(ELocomotionMode p_mode, ELocoStateType p_state)
        {
            switch (p_state)
            {
                case ELocoStateType.Jump:
                    PlayOneShot(_jumpClip, _actionVolume);
                    break;

                case ELocoStateType.Land:
                    PlayOneShot(_landClip, _actionVolume);
                    break;

                case ELocoStateType.Dash:
                    PlayOneShot(_dashClip, _actionVolume);
                    break;

                case ELocoStateType.Rising
                    when p_mode == ELocomotionMode.Flight:
                    PlayOneShot(_flyUpClip, _actionVolume);
                    break;
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

            int index = Random.Range(0, _footstepClips.Length);

            if (index == _lastFootstepIndex)
            {
                index = (index + Random.Range(1, _footstepClips.Length)) % _footstepClips.Length;
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
