using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // Range 무기의 발사 Audio 표현만 담당한다.
    [DisallowMultipleComponent]
    public sealed class RangeWeaponAudioView : WeaponView
    {
        [SerializeField]
        private RangeWeapon _weapon;

        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _fireClip;

        [SerializeField, Range(0f, 1f)]
        private float _fireVolume = 1f;

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
        }

        private void OnDisable()
        {
            if (_weapon != null)
                _weapon.OnFired -= HandleFired;
        }

        private void ResolveDependencies()
        {
            _weapon ??= GetComponentInParent<RangeWeapon>();
            _audioSource ??= GetComponent<AudioSource>();
            _audioSource ??= GetComponentInChildren<AudioSource>(true);

            if (_audioSource == null && _fireClip != null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            if (_audioSource != null)
                _audioSource.playOnAwake = false;
        }

        private void HandleFired(RangeAttackRequest p_request)
        {
            if (_audioSource != null && _fireClip != null)
                _audioSource.PlayOneShot(_fireClip, _fireVolume);
        }

        private void OnValidate()
        {
            _fireVolume = Mathf.Clamp01(_fireVolume);
        }
    }
}
