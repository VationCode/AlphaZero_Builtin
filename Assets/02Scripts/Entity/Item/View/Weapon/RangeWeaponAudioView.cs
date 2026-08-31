using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    public enum ERangeWeaponAudioPlaybackMode
    {
        OneShot,
        LoopWhilePrimary
    }

    // Range 무기의 발사 Audio 표현만 담당한다.
    [DisallowMultipleComponent]
    public sealed class RangeWeaponAudioView : WeaponView
    {
        [SerializeField]
        private RangeWeapon _weapon;

        [SerializeField]
        private AudioSource _audioSource;

        [Tooltip("OneShot은 발사마다 재생하고, LoopWhilePrimary는 공격 입력 동안 반복합니다.")]
        [SerializeField]
        private ERangeWeaponAudioPlaybackMode _playbackMode =
            ERangeWeaponAudioPlaybackMode.OneShot;

        [SerializeField]
        private AudioClip _fireClip;

        [SerializeField, Range(0f, 1f)]
        private float _fireVolume = 1f;

        private float _baseSourceVolume = 1f;
        private bool _hasBaseSourceVolume;
        private bool _isLoopPlaying;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (_weapon == null)
                return;

            _weapon.OnFired -= HandleFired;
            _weapon.OnFired += HandleFired;
            _weapon.OnActionStopped -= HandleActionStopped;
            _weapon.OnActionStopped += HandleActionStopped;
        }

        private void OnDisable()
        {
            if (_weapon != null)
            {
                _weapon.OnFired -= HandleFired;
                _weapon.OnActionStopped -= HandleActionStopped;
            }

            StopLoop();
        }

        private void ResolveDependencies()
        {
            _weapon ??= GetComponentInParent<RangeWeapon>();
            _audioSource ??= GetComponent<AudioSource>();
            _audioSource ??= GetComponentInChildren<AudioSource>(true);

            if (_audioSource == null && _fireClip != null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;

                if (!_hasBaseSourceVolume)
                {
                    _baseSourceVolume = _audioSource.volume;
                    _hasBaseSourceVolume = true;
                }
            }
        }

        private void HandleFired(RangeAttackRequest p_request)
        {
            if (_audioSource == null || _fireClip == null)
                return;

            if (_playbackMode ==
                ERangeWeaponAudioPlaybackMode.LoopWhilePrimary)
            {
                PlayLoop();
                return;
            }

            if (_isLoopPlaying)
                StopLoop();

            _audioSource.PlayOneShot(_fireClip, _fireVolume);
        }

        private void PlayLoop()
        {
            if (_isLoopPlaying)
                return;

            _audioSource.clip = _fireClip;
            _audioSource.loop = true;
            _audioSource.volume =
                _baseSourceVolume * _fireVolume;
            _audioSource.Play();
            _isLoopPlaying = true;
        }

        private void HandleActionStopped(EWeaponActionType p_actionType)
        {
            if (p_actionType == EWeaponActionType.Primary)
                StopLoop();
        }

        private void StopLoop()
        {
            if (!_isLoopPlaying || _audioSource == null)
                return;

            _audioSource.Stop();
            _audioSource.loop = false;

            if (_audioSource.clip == _fireClip)
                _audioSource.clip = null;

            _audioSource.volume = _baseSourceVolume;
            _isLoopPlaying = false;
        }

        private void OnValidate()
        {
            _fireVolume = Mathf.Clamp01(_fireVolume);
        }
    }
}
