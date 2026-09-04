using Alpha.Player.Animation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Animation Event Key와 재생할 ParticleSystem 설정을 연결한다.
    [Serializable]
    public sealed class PlayerActionEffectSetting
    {
        [SerializeField]
        private string _key;

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

        public bool Matches(string p_key)
        {
            return !string.IsNullOrWhiteSpace(_key) &&
                   string.Equals(
                       _key.Trim(),
                       p_key,
                       StringComparison.Ordinal);
        }
    }

    // Animation Event Key를 실제 ParticleSystem 재생으로 표현한다.
    public sealed class PlayerActionEffectView : MonoBehaviour
    {
        [SerializeField]
        private PlayerActionEffectSetting[] _effects;

        private readonly List<ParticleSystem> _activeEffects = new();
        private readonly HashSet<string> _missingKeyWarnings = new();
        private PlayerAnimationView _animationView;
        private bool _isSubscribed;

        // PlayerCore가 Animation Event Key 발행 View를 전달한다.
        public void Bind(PlayerAnimationView p_animationView)
        {
            if (ReferenceEquals(_animationView, p_animationView))
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            _animationView = p_animationView;
            _missingKeyWarnings.Clear();
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _animationView = null;
            _missingKeyWarnings.Clear();
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
                _animationView == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _animationView.OnEffectKeyRequested +=
                HandleEffectKeyRequested;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _animationView == null)
                return;

            _animationView.OnEffectKeyRequested -=
                HandleEffectKeyRequested;
            _isSubscribed = false;
        }

        // 요청 Key와 일치하는 모든 Particle 설정을 한 번 재생한다.
        private void HandleEffectKeyRequested(string p_key)
        {
            if (_effects == null)
                return;

            CleanupDestroyedEffects();
            bool hasMatch = false;

            foreach (PlayerActionEffectSetting setting in _effects)
            {
                if (setting == null ||
                    !setting.Matches(p_key))
                {
                    continue;
                }

                hasMatch = true;
                PlayEffect(setting);
            }

            if (!hasMatch && _missingKeyWarnings.Add(p_key))
            {
                Debug.LogWarning(
                    $"Animation Effect Event Key 설정을 찾을 수 없습니다: {p_key}",
                    this);
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
