using Alpha.Player.Locomotion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // 하나의 Locomotion 행동과 재생할 ParticleSystem 설정을 연결한다.
    [Serializable]
    public sealed class PlayerActionEffectSetting
    {
        [SerializeField]
        private ELocoStateType _state;

        [SerializeField]
        private bool _useModeFilter;

        [SerializeField]
        private ELocomotionMode _mode;

        [SerializeField]
        private ParticleSystem _particlePrefab;

        [SerializeField]
        private Transform _spawnPoint;

        [SerializeField]
        private bool _followPlayer = true;

        [Tooltip("0이면 ParticleSystem 설정에서 유지 시간을 계산한다.")]
        [SerializeField, Min(0f)]
        private float _lifetime;

        public ParticleSystem ParticlePrefab => _particlePrefab;
        public Transform SpawnPoint => _spawnPoint;
        public bool FollowPlayer => _followPlayer;
        public float Lifetime => _lifetime;

        public bool Matches(
            ELocomotionMode p_mode,
            ELocoStateType p_state)
        {
            return _state == p_state &&
                   (!_useModeFilter || _mode == p_mode);
        }
    }

    // Player 행동 상태를 실제 ParticleSystem 재생으로 표현한다.
    public sealed class PlayerActionEffectView : MonoBehaviour
    {
        [SerializeField]
        private PlayerActionEffectSetting[] _effects;

        private readonly List<ParticleSystem> _activeEffects = new();
        private LocomotionContext _context;
        private bool _isSubscribed;

        // PlayerCore가 Player Locomotion 상태를 전달한다.
        public void Bind(LocomotionContext p_context)
        {
            if (ReferenceEquals(_context, p_context))
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            _context = p_context;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _context = null;
            StopAllEffects();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAllEffects();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                _context == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _context.OnStateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _context == null)
                return;

            _context.OnStateChanged -= HandleStateChanged;
            _isSubscribed = false;
        }

        // 확정된 행동과 일치하는 모든 Particle 설정을 한 번 재생한다.
        private void HandleStateChanged(
            ELocomotionMode p_mode,
            ELocoStateType p_state)
        {
            if (_effects == null)
                return;

            CleanupDestroyedEffects();

            foreach (PlayerActionEffectSetting setting in _effects)
            {
                if (setting == null ||
                    !setting.Matches(p_mode, p_state))
                {
                    continue;
                }

                PlayEffect(setting);
            }
        }

        private void PlayEffect(PlayerActionEffectSetting p_setting)
        {
            ParticleSystem prefab = p_setting.ParticlePrefab;

            if (prefab == null)
                return;

            Transform spawnPoint = p_setting.SpawnPoint != null
                ? p_setting.SpawnPoint
                : transform;

            ParticleSystem effect = Instantiate(
                prefab,
                spawnPoint,
                false);

            // 월드 고정 Effect는 Prefab의 Local Offset을 적용한 뒤 부모에서 분리한다.
            if (!p_setting.FollowPlayer)
                effect.transform.SetParent(null, true);

            effect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);

            _activeEffects.Add(effect);

            float lifetime = p_setting.Lifetime > 0f
                ? p_setting.Lifetime
                : CalculateLifetime(effect);

            Destroy(effect.gameObject, lifetime);
        }

        private static float CalculateLifetime(ParticleSystem p_effect)
        {
            float lifetime = 0.1f;
            ParticleSystem[] systems =
                p_effect.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule main = system.main;
                float systemLifetime =
                    main.startDelay.constantMax +
                    main.duration +
                    main.startLifetime.constantMax;

                lifetime = Mathf.Max(lifetime, systemLifetime);
            }

            return lifetime;
        }

        private void CleanupDestroyedEffects()
        {
            _activeEffects.RemoveAll(effect => effect == null);
        }

        private void StopAllEffects()
        {
            foreach (ParticleSystem effect in _activeEffects)
            {
                if (effect != null)
                    Destroy(effect.gameObject);
            }

            _activeEffects.Clear();
        }
    }
}
