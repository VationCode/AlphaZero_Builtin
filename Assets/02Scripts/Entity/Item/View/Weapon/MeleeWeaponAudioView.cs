using Alpha.Item.Weapon.Melee;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 근접 무기의 콤보 시작을 모션별 공격음으로 표현한다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponAudioView : MonoBehaviour
    {
        [SerializeField]
        private MeleeWeapon _weapon;

        [SerializeField]
        private AudioSource _audioSource;

        [Tooltip("Combo Clips와 같은 인덱스의 모션별 공격음")]
        [SerializeField]
        private AudioClip[] _comboClips;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        private void Awake()
        {
            _weapon ??= GetComponent<MeleeWeapon>();
            _audioSource ??= GetComponent<AudioSource>();

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            _weapon ??= GetComponent<MeleeWeapon>();

            if (_weapon == null)
                return;

            // 재활성화 시에도 중복 재생되지 않도록 구독을 한 번만 유지한다.
            _weapon.OnComboStarted -= HandleComboStarted;
            _weapon.OnComboStarted += HandleComboStarted;
        }

        private void OnDisable()
        {
            if (_weapon != null)
                _weapon.OnComboStarted -= HandleComboStarted;
        }

        private void HandleComboStarted(int p_comboIndex)
        {
            if (_audioSource == null ||
                _comboClips == null ||
                p_comboIndex < 0 ||
                p_comboIndex >= _comboClips.Length)
            {
                return;
            }

            AudioClip clip = _comboClips[p_comboIndex];

            if (clip == null)
                return;

            // 빠른 콤보에서도 앞 공격음을 끊지 않고 다음 공격음을 중첩한다.
            _audioSource.PlayOneShot(clip, _volume);
        }
    }
}
