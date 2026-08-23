using Alpha.Item.Weapon.Melee;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    [Serializable]
    public struct MeleeComboAudioBinding
    {
        [SerializeField]
        private string _comboName;

        [SerializeField]
        private AudioClip _clip;

        public string ComboName => _comboName;
        public AudioClip Clip => _clip;
    }

    // 근접 무기의 콤보 시작을 모션별 공격음으로 표현한다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponAudioView : MonoBehaviour
    {
        [SerializeField]
        private MeleeWeapon _weapon;

        [SerializeField]
        private AudioSource _audioSource;

        [Tooltip("Combo Name과 재생할 공격음을 연결합니다.")]
        [SerializeField]
        private MeleeComboAudioBinding[] _comboAudioBindings;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        private readonly Dictionary<string, AudioClip> _clipByComboName =
            new(StringComparer.Ordinal);

        private void Awake()
        {
            _weapon ??= GetComponent<MeleeWeapon>();
            _audioSource ??= GetComponent<AudioSource>();

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            RebuildAudioMap(true);
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

        private void HandleComboStarted(string p_comboName)
        {
            if (_audioSource == null ||
                string.IsNullOrWhiteSpace(p_comboName) ||
                !_clipByComboName.TryGetValue(
                    p_comboName,
                    out AudioClip clip))
            {
                return;
            }

            // 빠른 콤보에서도 앞 공격음을 끊지 않고 다음 공격음을 중첩한다.
            _audioSource.PlayOneShot(clip, _volume);
        }

        private void RebuildAudioMap(bool p_logWarnings)
        {
            _clipByComboName.Clear();

            if (_comboAudioBindings == null)
                return;

            foreach (MeleeComboAudioBinding binding in _comboAudioBindings)
            {
                string comboName = binding.ComboName?.Trim();

                if (string.IsNullOrWhiteSpace(comboName) ||
                    binding.Clip == null)
                {
                    continue;
                }

                if (_clipByComboName.ContainsKey(comboName))
                {
                    if (p_logWarnings)
                    {
                        Debug.LogWarning(
                            $"Melee Audio의 Combo Name이 중복되었습니다: {comboName}",
                            this);
                    }

                    continue;
                }

                _clipByComboName.Add(comboName, binding.Clip);
            }
        }

        private void OnValidate()
        {
            _volume = Mathf.Clamp01(_volume);
            RebuildAudioMap(false);
        }
    }
}
