using Alpha.Player.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Audio
{
    // Player Melee Combat이 시작한 Skill의 AudioClip을 재생한다.
    [DisallowMultipleComponent]
    public sealed class PlayerMeleeSkillAudioView : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        private CombatModule _combatModule;
        private bool _isSubscribed;

        public void Bind(CombatModule p_combatModule)
        {
            if (ReferenceEquals(_combatModule, p_combatModule))
            {
                Subscribe();
                return;
            }

            Unbind();
            _combatModule = p_combatModule;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _combatModule = null;
        }

        private void Awake()
        {
            ResolveAudioSource();

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                _combatModule == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _combatModule.OnMeleeSkillStarted += HandleSkillStarted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _combatModule == null)
                return;

            _combatModule.OnMeleeSkillStarted -= HandleSkillStarted;
            _isSubscribed = false;
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

        private void ResolveAudioSource()
        {
            _audioSource ??= GetComponent<AudioSource>();
            _audioSource ??= GetComponentInChildren<AudioSource>(true);
        }

        private void OnValidate()
        {
            _volume = Mathf.Clamp01(_volume);
            ResolveAudioSource();
        }
    }
}
