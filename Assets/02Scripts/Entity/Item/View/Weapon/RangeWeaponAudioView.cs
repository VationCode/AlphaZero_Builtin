using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // RangeWeapon의 Audio 표현만 담당한다.
    public sealed class RangeWeaponAudioView : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _fireClip;

        [SerializeField, Range(0f, 1f)]
        private float _fireVolume = 1f;

        private void Awake()
        {
            _audioSource ??= GetComponentInChildren<AudioSource>(true);

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        public void PlayFire()
        {
            if (_audioSource == null || _fireClip == null)
                return;

            // 연사 중에도 이전 발사음을 끊지 않고 중첩한다.
            _audioSource.PlayOneShot(
                _fireClip,
                _fireVolume);
        }
    }
}
