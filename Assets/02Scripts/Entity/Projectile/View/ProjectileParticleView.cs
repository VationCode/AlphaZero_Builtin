using System;
using UnityEngine;

namespace Alpha.Projectile.View
{
    // Projectile이 소유한 Particle 그룹의 반복 표현 방식을 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Projectile))]
    public sealed class ProjectileParticleView : MonoBehaviour
    {
        [Serializable]
        private sealed class ParticlePlaybackGroup
        {
            [Tooltip("반복 방식을 함께 적용할 ParticleSystem 계층의 Root입니다.")]
            [SerializeField]
            private Transform _particleRoot;

            [Tooltip("체크하면 Looping 대신 설정 주기마다 One-shot 효과를 처음부터 재생합니다.")]
            [SerializeField]
            private bool _oneShot = true;

            [Tooltip("One Shot 효과를 처음부터 다시 재생하는 주기입니다.")]
            [SerializeField, Min(0.01f)]
            private float _restartInterval = 2f;

            [NonSerialized]
            private ParticleSystem[] _particles =
                Array.Empty<ParticleSystem>();

            [NonSerialized]
            private float _elapsedTime;

            public void Begin(Transform p_fallbackRoot)
            {
                ResolveParticles(p_fallbackRoot);
                _elapsedTime = 0f;
                Restart();
            }

            public void Tick(float p_deltaTime)
            {
                if (!_oneShot || _particles.Length == 0)
                    return;

                _elapsedTime += Mathf.Max(0f, p_deltaTime);

                float interval = Mathf.Max(0.01f, _restartInterval);

                if (_elapsedTime < interval)
                    return;

                _elapsedTime %= interval;
                Restart();
            }

            public void Stop()
            {
                _elapsedTime = 0f;

                for (int index = 0; index < _particles.Length; index++)
                {
                    ParticleSystem particle = _particles[index];

                    if (particle != null)
                    {
                        particle.Stop(
                            false,
                            ParticleSystemStopBehavior
                                .StopEmittingAndClear);
                    }
                }
            }

            public void Validate()
            {
                _restartInterval = Mathf.Max(
                    0.01f,
                    _restartInterval);
            }

            private void ResolveParticles(Transform p_fallbackRoot)
            {
                Transform root = _particleRoot != null
                    ? _particleRoot
                    : p_fallbackRoot;

                _particles = root != null
                    ? root.GetComponentsInChildren<ParticleSystem>(true)
                    : Array.Empty<ParticleSystem>();
            }

            private void Restart()
            {
                bool shouldLoop = !_oneShot;

                // 모든 Particle을 먼저 정리한 뒤 같은 Frame에 함께 시작한다.
                for (int index = 0; index < _particles.Length; index++)
                {
                    ParticleSystem particle = _particles[index];

                    if (particle == null)
                        continue;

                    ParticleSystem.MainModule main = particle.main;
                    main.loop = shouldLoop;
                    particle.Stop(
                        false,
                        ParticleSystemStopBehavior
                            .StopEmittingAndClear);
                }

                for (int index = 0; index < _particles.Length; index++)
                {
                    ParticleSystem particle = _particles[index];

                    if (particle != null)
                        particle.Play(false);
                }
            }
        }

        [SerializeField]
        private ParticlePlaybackGroup[] _groups =
        {
            new()
        };

        private void OnEnable()
        {
            if (_groups == null)
                return;

            for (int index = 0; index < _groups.Length; index++)
                _groups[index]?.Begin(transform);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f || _groups == null)
                return;

            for (int index = 0; index < _groups.Length; index++)
                _groups[index]?.Tick(deltaTime);
        }

        private void OnDisable()
        {
            if (_groups == null)
                return;

            for (int index = 0; index < _groups.Length; index++)
                _groups[index]?.Stop();
        }

        private void OnValidate()
        {
            if (_groups == null || _groups.Length == 0)
            {
                _groups = new[]
                {
                    new ParticlePlaybackGroup()
                };
            }

            for (int index = 0; index < _groups.Length; index++)
            {
                _groups[index] ??=
                    new ParticlePlaybackGroup();
                _groups[index].Validate();
            }
        }
    }
}
