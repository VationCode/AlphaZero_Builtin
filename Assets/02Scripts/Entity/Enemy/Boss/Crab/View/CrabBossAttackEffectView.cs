using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // 공격 State 이름별로 이벤트에 연결할 여러 이펙트를 보관한다.
    [Serializable]
    public sealed class CrabBossAttackEffectTrack
    {
        [Tooltip("Animator State 이름입니다. 예: Attack1")]
        [SerializeField] private string _attackName;
        [SerializeField] private List<CrabBossAttackEffectTiming> _effects = new();

        public string AttackName => _attackName;
        public IReadOnlyList<CrabBossAttackEffectTiming> Effects =>
            _effects ??= new List<CrabBossAttackEffectTiming>();

        public void Validate()
        {
            _attackName = _attackName?.Trim();
            _effects ??= new List<CrabBossAttackEffectTiming>();
            foreach (CrabBossAttackEffectTiming effect in _effects)
                effect?.Validate();
        }
    }

    // 이벤트 이름과 프리팹, 생성 위치, 생성 후 유지 시간을 설정한다.
    [Serializable]
    public sealed class CrabBossAttackEffectTiming
    {
        [SerializeField] private GameObject _effectPrefab;
        [Tooltip("비어 있으면 Combat 오브젝트에서 생성합니다.")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private bool _followSpawnPoint;
        [Tooltip("고정된 공격 위치에서 생성합니다. Spawn Point와 Follow Spawn Point는 사용하지 않습니다.")]
        [SerializeField] private bool _spawnAtTargetPosition;
        [Tooltip("클립 이벤트의 Function 이름입니다. Effect1Start~Effect16Start를 사용할 수 있으며 같은 이름을 여러 이펙트에 지정할 수 있습니다.")]
        [SerializeField] private string _startEventName = "Effect1Start";
        [Tooltip("이펙트 생성 후 유지할 시간(초)입니다. 지나면 인스턴스를 제거합니다.")]
        [SerializeField, Min(0f)] private float _tailDuration = 1f;

        public GameObject EffectPrefab => _effectPrefab;
        public Transform SpawnPoint => _spawnPoint;
        public bool FollowSpawnPoint => _followSpawnPoint;
        public bool SpawnAtTargetPosition => _spawnAtTargetPosition;
        public string StartEventName => _startEventName;
        public float TailDuration => _tailDuration;
        public bool IsValid => _effectPrefab != null;

        public void Validate()
        {
            _startEventName = _startEventName?.Trim();
            _tailDuration = Mathf.Max(0f, _tailDuration);
        }
    }

    // 클립의 EffectNStart 이벤트로 공격별 이펙트를 재생한다.
    [DisallowMultipleComponent]
    public sealed class CrabBossAttackEffectView : MonoBehaviour
    {
        private readonly struct TimedEffect
        {
            public readonly GameObject Instance;
            public readonly float DestroyAt;

            public TimedEffect(GameObject instance, float destroyAt)
            {
                Instance = instance;
                DestroyAt = destroyAt;
            }
        }

        [SerializeField] private List<CrabBossAttackEffectTrack> _attackEffects = new();

        private CrabBossAnimationView _animation;
        private CrabBossAttackEffectTrack _activeTrack;
        private readonly List<TimedEffect> _liveEffects = new();
        private bool[] _started = Array.Empty<bool>();
        private int _activeStateHash;

        public void Bind(CrabBossAnimationView animation)
        {
            Unbind();
            _animation = animation;
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            StopActiveAttack(true);
            _animation = null;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            StopActiveAttack(true);
        }

        private void Subscribe()
        {
            if (_animation == null)
                return;

            _animation.AttackAnimationStarted -= HandleAttackStarted;
            _animation.AttackAnimationEnded -= HandleAttackEnded;
            _animation.EffectStarted -= HandleEffectStarted;
            _animation.AttackAnimationStarted += HandleAttackStarted;
            _animation.AttackAnimationEnded += HandleAttackEnded;
            _animation.EffectStarted += HandleEffectStarted;
        }

        private void Unsubscribe()
        {
            if (_animation == null)
                return;

            _animation.AttackAnimationStarted -= HandleAttackStarted;
            _animation.AttackAnimationEnded -= HandleAttackEnded;
            _animation.EffectStarted -= HandleEffectStarted;
        }

        private void HandleAttackStarted(int stateHash)
        {
            StopActiveAttack(false);
            _activeTrack = FindTrack(stateHash);
            if (_activeTrack == null)
                return;

            _activeStateHash = stateHash;
            int count = _activeTrack.Effects.Count;
            _started = new bool[count];
        }

        private void HandleAttackEnded(int stateHash)
        {
            // 공격 종료 후에도 생성된 이펙트는 각자의 TailDuration까지 유지한다.
            if (_activeStateHash == stateHash)
                StopActiveAttack(false);
        }

        private CrabBossAttackEffectTrack FindTrack(int stateHash)
        {
            if (_attackEffects == null)
                return null;

            foreach (CrabBossAttackEffectTrack track in _attackEffects)
            {
                if (track == null || string.IsNullOrWhiteSpace(track.AttackName))
                    continue;

                if (Animator.StringToHash(track.AttackName) == stateHash)
                    return track;
            }

            return null;
        }

        private void LateUpdate()
        {
            ExpireLiveEffects();
        }

        // 같은 이벤트 이름을 가진 이펙트를 모두 시작한다.
        private void HandleEffectStarted(string eventName)
        {
            if (_activeTrack == null || string.IsNullOrEmpty(eventName))
                return;

            Vector3? targetPosition = _animation.AttackTargetPosition;
            for (int index = 0; index < _started.Length; index++)
            {
                CrabBossAttackEffectTiming timing = _activeTrack.Effects[index];
                if (_started[index] || timing == null || !timing.IsValid ||
                    !string.Equals(timing.StartEventName, eventName, StringComparison.Ordinal) ||
                    (timing.SpawnAtTargetPosition && !targetPosition.HasValue))
                    continue;

                _started[index] = true;
                GameObject instance = PlayEffect(timing, targetPosition);
                if (timing.TailDuration <= 0f)
                    Destroy(instance);
                else
                    _liveEffects.Add(new TimedEffect(instance,
                        Time.time + timing.TailDuration));
            }
        }

        private GameObject PlayEffect(CrabBossAttackEffectTiming timing,
            Vector3? targetPosition)
        {
            Transform spawnPoint = timing.SpawnPoint != null
                ? timing.SpawnPoint : transform;
            Vector3 position = timing.SpawnAtTargetPosition
                ? targetPosition.Value : spawnPoint.position;
            Quaternion rotation = timing.SpawnAtTargetPosition
                ? Quaternion.identity : spawnPoint.rotation;
            GameObject instance = Instantiate(timing.EffectPrefab,
                position, rotation);
            instance.SetActive(false);
            if (timing.FollowSpawnPoint && !timing.SpawnAtTargetPosition)
                instance.transform.SetParent(spawnPoint, true);

            ParticleSystem[] particles =
                instance.GetComponentsInChildren<ParticleSystem>(true);
            HashSet<ParticleSystem> subEmitters = new();
            foreach (ParticleSystem particle in particles)
            {
                // 프리팹의 Play On Awake와 이전 시뮬레이션 상태를 제거한다.
                ParticleSystem.MainModule main = particle.main;
                main.playOnAwake = false;
                particle.Stop(false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Simulate(0f, false, true);

                ParticleSystem.SubEmittersModule module = particle.subEmitters;
                if (!module.enabled)
                    continue;

                for (int index = 0; index < module.subEmittersCount; index++)
                {
                    ParticleSystem subEmitter = module.GetSubEmitterSystem(index);
                    if (subEmitter != null)
                        subEmitters.Add(subEmitter);
                }
            }

            instance.SetActive(true);
            foreach (ParticleSystem particle in particles)
            {
                // 서브 이미터는 부모 파티클이 트리거할 때 시작한다.
                if (!particle.gameObject.activeInHierarchy || subEmitters.Contains(particle))
                    continue;

                particle.Play(false);
            }

            return instance;
        }

        private void StopActiveAttack(bool clearImmediately)
        {
            if (clearImmediately)
                ClearLiveEffects();

            _activeTrack = null;
            _activeStateHash = 0;
            _started = Array.Empty<bool>();
        }

        private void ExpireLiveEffects()
        {
            for (int index = _liveEffects.Count - 1; index >= 0; index--)
            {
                TimedEffect effect = _liveEffects[index];
                if (effect.Instance != null && Time.time < effect.DestroyAt)
                    continue;

                if (effect.Instance != null)
                {
                    effect.Instance.SetActive(false);
                    Destroy(effect.Instance);
                }
                _liveEffects.RemoveAt(index);
            }
        }

        private void ClearLiveEffects()
        {
            foreach (TimedEffect effect in _liveEffects)
            {
                if (effect.Instance != null)
                {
                    effect.Instance.SetActive(false);
                    Destroy(effect.Instance);
                }
            }

            _liveEffects.Clear();
        }

        private void OnValidate()
        {
            _attackEffects ??= new List<CrabBossAttackEffectTrack>();
            foreach (CrabBossAttackEffectTrack track in _attackEffects)
                track?.Validate();
        }
    }
}
