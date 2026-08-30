using Alpha.Item.Weapon.Melee;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 근접 무기가 시작한 Skill 자산의 AudioClip을 재생한다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponAudioView : WeaponView
    {
        [SerializeField]
        private MeleeWeapon _weapon;

        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        private void Awake()
        {
            ResolveDependencies();

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (_weapon == null)
                return;

            _weapon.OnSkillStarted -= HandleSkillStarted;
            _weapon.OnSkillStarted += HandleSkillStarted;
        }

        private void OnDisable()
        {
            if (_weapon != null)
                _weapon.OnSkillStarted -= HandleSkillStarted;
        }

        private void HandleSkillStarted(MeleeSkillDefinition p_skill)
        {
            IReadOnlyList<AudioClip> clips = p_skill?.AudioClips;

            if (_audioSource == null || clips == null || clips.Count == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Count)];

            if (clip != null)
                _audioSource.PlayOneShot(clip, _volume);
        }

        private void ResolveDependencies()
        {
            _weapon ??= GetComponentInParent<MeleeWeapon>();
            _audioSource ??= GetComponent<AudioSource>();
            _audioSource ??= GetComponentInChildren<AudioSource>(true);
        }

        private void OnValidate()
        {
            _volume = Mathf.Clamp01(_volume);
            ResolveDependencies();
        }
    }
}
