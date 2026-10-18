using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;

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

        // 생성과 공격 예고가 같은 위치·회전을 사용한다.
        public bool TryGetSpawnPose(Transform fallback, Vector3? targetPosition,
            out Pose pose)
        {
            pose = default;
            if (_spawnAtTargetPosition && !targetPosition.HasValue)
                return false;

            Transform point = _spawnPoint != null ? _spawnPoint : fallback;
            if (point == null)
                return false;

            pose = _spawnAtTargetPosition
                ? new Pose(targetPosition.Value, Quaternion.identity)
                : new Pose(point.position, point.rotation);
            return true;
        }

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
        private static readonly int Attack5Name = Animator.StringToHash("Attack5");

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
        private CrabBossAttackModule _attack;
        private TargetDetectionModule _targetDetection;
        private Transform _owner;
        private Transform _attackTarget;
        private CrabBossAttackEffectTrack _activeTrack;
        private readonly List<TimedEffect> _liveEffects = new();
        private bool[] _started = Array.Empty<bool>();
        private int _activeStateHash;

        public void Bind(CrabBossAnimationView animation,
            CrabBossAttackModule attack, TargetDetectionModule targetDetection,
            Transform owner)
        {
            Unbind();
            _animation = animation;
            _attack = attack;
            _targetDetection = targetDetection;
            _owner = owner;
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            StopActiveAttack(true);
            _animation = null;
            _attack = null;
            _targetDetection = null;
            _owner = null;
        }

        public int Attack5ProjectileCount => GetAttack5ProjectileCount(null);

        public int GetAttack5ProjectileCount(string eventName)
        {
            CrabBossAttackEffectTrack track = FindTrack(Attack5Name);
            if (track == null)
                return 0;

            int count = 0;
            foreach (CrabBossAttackEffectTiming timing in track.Effects)
                if (TryGetProjectile(timing, out _) &&
                    (string.IsNullOrEmpty(eventName) ||
                     string.Equals(timing.StartEventName, eventName,
                         StringComparison.Ordinal)))
                    count++;
            return count;
        }

        public bool IsAttack5Ready
        {
            get
            {
                CrabBossAttackEffectTrack track = FindTrack(Attack5Name);
                if (track == null || Attack5ProjectileCount == 0)
                    return false;

                foreach (CrabBossAttackEffectTiming timing in track.Effects)
                {
                    if (!TryGetProjectile(timing, out ProjectileEntity projectile))
                        continue;
                    if (!projectile.IsPrefabDamageValid || timing.SpawnPoint == null ||
                        timing.SpawnAtTargetPosition || timing.FollowSpawnPoint ||
                        !timing.TryGetSpawnPose(transform, null, out _))
                        return false;
                }
                return true;
            }
        }

        public bool TryGetAttack5ProjectilePose(int index, out Pose pose,
            string eventName = null)
        {
            pose = default;
            CrabBossAttackEffectTrack track = FindTrack(Attack5Name);
            if (track == null || index < 0)
                return false;

            foreach (CrabBossAttackEffectTiming timing in track.Effects)
            {
                if (!TryGetProjectile(timing, out _) ||
                    (!string.IsNullOrEmpty(eventName) &&
                     !string.Equals(timing.StartEventName, eventName,
                         StringComparison.Ordinal)))
                    continue;
                if (index-- == 0)
                    return timing.TryGetSpawnPose(transform, null, out pose);
            }
            return false;
        }

        private static bool TryGetProjectile(CrabBossAttackEffectTiming timing,
            out ProjectileEntity projectile)
        {
            projectile = null;
            return timing != null && timing.IsValid &&
                timing.EffectPrefab.TryGetComponent(out projectile);
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
            _attackTarget = null;
            if (stateHash == Attack5Name && _targetDetection != null)
                _targetDetection.TryGetTarget(out _attackTarget);
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
            bool repeatForAttack5 = _activeStateHash == Attack5Name;
            for (int index = 0; index < _started.Length; index++)
            {
                CrabBossAttackEffectTiming timing = _activeTrack.Effects[index];
                if ((!repeatForAttack5 && _started[index]) || timing == null ||
                    !timing.IsValid ||
                    !string.Equals(timing.StartEventName, eventName, StringComparison.Ordinal) ||
                    (timing.SpawnAtTargetPosition && !targetPosition.HasValue))
                    continue;

                if (!repeatForAttack5)
                    _started[index] = true;
                GameObject instance = PlayEffect(timing, targetPosition,
                    out bool selfManaged);
                if (instance == null || selfManaged)
                    continue;
                if (timing.TailDuration <= 0f)
                    Destroy(instance);
                else
                    _liveEffects.Add(new TimedEffect(instance,
                        Time.time + timing.TailDuration));
            }
        }

        private GameObject PlayEffect(CrabBossAttackEffectTiming timing,
            Vector3? targetPosition, out bool selfManaged)
        {
            selfManaged = false;
            if (!timing.TryGetSpawnPose(transform, targetPosition, out Pose pose))
                return null;

            Transform spawnPoint = timing.SpawnPoint != null
                ? timing.SpawnPoint : transform;
            GameObject instance = Instantiate(timing.EffectPrefab,
                pose.position, pose.rotation);
            instance.SetActive(false);
            if (timing.FollowSpawnPoint && !timing.SpawnAtTargetPosition)
                instance.transform.SetParent(spawnPoint, true);

            ParticleSystem[] particles =
                instance.GetComponentsInChildren<ParticleSystem>(true);
            HashSet<ParticleSystem> subEmitters = new();
            foreach (ParticleSystem particle in particles)
            {
                // 자동 재생을 끄고, 수동 재생 전에 생성된 입자만 비운다.
                ParticleSystem.MainModule main = particle.main;
                main.playOnAwake = false;
                particle.Stop(false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);

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

            if (_activeStateHash == Attack5Name &&
                instance.TryGetComponent(out ProjectileEntity projectile))
            {
                if (_attack == null || _owner == null ||
                    _targetDetection == null ||
                    _attackTarget == null ||
                    !_attackTarget.CompareTag("Player") ||
                    !_attack.IsAttack5AreaConfigured ||
                    !projectile.InitializeFromPrefab(_owner, _attackTarget, pose.position,
                        pose.rotation * Vector3.forward, _attack.Attack5Range))
                {
                    Destroy(instance);
                    return null;
                }
                selfManaged = true;
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
            _attackTarget = null;
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
