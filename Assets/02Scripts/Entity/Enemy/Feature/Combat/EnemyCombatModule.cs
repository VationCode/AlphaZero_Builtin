using System;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Enemy
{
    // Pattern Settings를 참조하고 거리 규칙에 따라 선택된 공격을 실행한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAttackPatternSettings))]
    public sealed class EnemyCombatModule : MonoBehaviour
    {
        public const int MinimumPatternCount = 1;
        public const int MinimumDistancePatternCount = 1;

        [SerializeField]
        private Transform _owner;

        [SerializeField]
        private EnemyAttackPatternSettings _patternSettings;

        // 기존 Scene·Prefab의 _attackPatterns 데이터를 한 번만 이전한다.
        [FormerlySerializedAs("_attackPatterns")]
        [SerializeField, HideInInspector]
        private EnemyAttackPatternSetting[] _legacyAttackPatterns;

        [SerializeField]
        private EnemyDistancePatternSetting[] _distancePatterns =
        {
            new()
        };

        private readonly EnemyMeleeAttackModule _meleeAttack = new();
        private readonly EnemyRangeAttackModule _rangeAttack = new();
        private readonly EnemyRushAttackModule _rushAttack = new();
        private readonly Dictionary<Collider, int>
            _activeTimingColliderCounts = new();
        private readonly Dictionary<Collider, EnemyAttackColliderSource>
            _activeTimingColliderSources = new();

        private Transform _currentTarget;
        private EnemyAttackPatternSetting _currentPattern;
        private bool _didActivateAttack;
        private bool[] _startedAttackTimings = Array.Empty<bool>();
        private bool[] _completedAttackTimings = Array.Empty<bool>();
        private float _lastAttackNormalizedTime = -1f;
        private int _nextAttackId = 1;

        public int PatternCount => _patternSettings?.Count ?? 0;
        public int DistancePatternCount =>
            _distancePatterns?.Length ?? 0;
        public Transform Owner => ResolveOwner();
        public bool IsAttacking => _currentPattern != null;
        public bool IsAttackActivated =>
            IsAttacking && _didActivateAttack;

        public EnemyAttackPatternSetting CurrentPattern =>
            _currentPattern;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
            EnsureSettings();
            CancelAttack(null);
            DisableAllConfiguredAttackColliders();
        }

        public EnemyAttackPatternSetting GetPattern(int p_index)
        {
            return _patternSettings?.GetPattern(p_index);
        }

        public EnemyDistancePatternSetting GetDistancePattern(int p_index)
        {
            return p_index >= 0 && p_index < DistancePatternCount
                ? _distancePatterns[p_index]
                : null;
        }

        // 거리 안에 실행 가능한 패턴이 하나라도 있으면 공격 상태를 유지한다.
        public bool CanEngageTarget(Transform p_target)
        {
            if (!TryMeasureTarget(
                    p_target,
                    out _,
                    out float distance))
            {
                return false;
            }

            for (int index = 0; index < DistancePatternCount; index++)
            {
                EnemyDistancePatternSetting distancePattern =
                    _distancePatterns[index];
                EnemyAttackPatternSetting pattern =
                    GetPattern(distancePattern?.PatternIndex ?? -1);

                if (distancePattern != null &&
                    distancePattern.IsValid(PatternCount) &&
                    pattern != null &&
                    pattern.IsExecutable &&
                    distancePattern.IsWithinDistance(distance))
                {
                    return true;
                }
            }

            return false;
        }

        // 현재 거리에서 가장 적은 이동으로 진입할 수 있는 공격 거리 보정값을 구한다.
        // 양수는 대상에게서 멀어져야 하고, 음수는 대상에게 가까워져야 함을 뜻한다.
        public bool TryResolvePositioning(
            Transform p_target,
            out Vector3 p_directionToTarget,
            out float p_distanceAdjustment)
        {
            p_directionToTarget = Vector3.zero;
            p_distanceAdjustment = 0f;

            if (!TryMeasureTarget(
                    p_target,
                    out p_directionToTarget,
                    out float distance))
            {
                return false;
            }

            bool hasExecutablePattern = false;
            float closestAdjustment = 0f;
            float closestMovementDistance = float.PositiveInfinity;

            for (int index = 0; index < DistancePatternCount; index++)
            {
                EnemyDistancePatternSetting distancePattern =
                    _distancePatterns[index];
                EnemyAttackPatternSetting pattern =
                    GetPattern(distancePattern?.PatternIndex ?? -1);

                if (distancePattern == null ||
                    !distancePattern.IsValid(PatternCount) ||
                    pattern == null ||
                    !pattern.IsExecutable)
                {
                    continue;
                }

                hasExecutablePattern = true;

                if (distancePattern.IsWithinDistance(distance))
                {
                    p_distanceAdjustment = 0f;
                    return true;
                }

                float adjustment =
                    distance < distancePattern.MinimumDistance
                        ? distancePattern.MinimumDistance - distance
                        : distancePattern.MaximumDistance - distance;
                float movementDistance = Mathf.Abs(adjustment);

                if (movementDistance >= closestMovementDistance)
                    continue;

                closestMovementDistance = movementDistance;
                closestAdjustment = adjustment;
            }

            p_distanceAdjustment = closestAdjustment;
            return hasExecutablePattern;
        }

        // CombatFlow가 패턴 선택 전에 현재 타겟 거리와 실행 가능 여부를 검사한다.
        public bool CanStartPattern(
            int p_patternIndex,
            Transform p_target)
        {
            if (IsAttacking)
                return false;

            EnemyAttackPatternSetting pattern =
                GetPattern(p_patternIndex);

            if (pattern == null || !pattern.IsExecutable)
                return false;

            if (!TryMeasureTarget(
                    p_target,
                    out _,
                    out float distance))
            {
                return false;
            }

            for (int index = 0; index < DistancePatternCount; index++)
            {
                EnemyDistancePatternSetting distancePattern =
                    _distancePatterns[index];

                if (distancePattern != null &&
                    distancePattern.PatternIndex == p_patternIndex &&
                    distancePattern.IsValid(PatternCount) &&
                    distancePattern.IsWithinDistance(distance))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryBeginAttack(
            int p_patternIndex,
            Transform p_target,
            out EnemyAttackPatternSetting p_pattern)
        {
            p_pattern = GetPattern(p_patternIndex);

            if (!CanStartPattern(p_patternIndex, p_target))
            {
                p_pattern = null;
                return false;
            }

            _currentPattern = p_pattern;
            _currentTarget = p_target;
            _didActivateAttack = false;
            PrepareAttackTimingExecution();

            return true;
        }

        // Flow가 결정한 실행 시점에 선택된 타입의 실제 공격을 시작한다.
        public bool ActivateAttack(Transform p_target)
        {
            if (!IsAttacking || _didActivateAttack)
                return false;

            if (p_target != null && p_target.gameObject.activeInHierarchy)
                _currentTarget = p_target;

            _didActivateAttack = true;
            BeginCurrentAttack();
            return true;
        }

        // Rush 이동처럼 매 Frame 갱신이 필요한 공격 기능을 처리한다.
        public void TickActiveAttack(
            EnemyLocomotionModule p_locomotion,
            float p_deltaTime)
        {
            if (!IsAttackActivated ||
                _currentPattern.AttackType != EEnemyAttackType.Rush)
            {
                return;
            }

            _rushAttack.Tick(
                ResolveOwner(),
                p_locomotion,
                _currentPattern,
                p_deltaTime,
                !_currentPattern.HasExecutableTiming(
                    EEnemyAttackTimingType.Collider));
        }

        // Animation View가 전달한 진행률에서 아직 실행하지 않은 타이밍을 처리한다.
        public void UpdateAttackAnimationProgress(
            float p_normalizedTime)
        {
            if (!IsAttackActivated)
                return;

            ProcessAttackTimings(Mathf.Clamp01(p_normalizedTime));
        }

        public void EndAttackExecution(
            EnemyLocomotionModule p_locomotion)
        {
            ResetAttackTimingExecution();
            _rushAttack.End();

            if (_currentPattern != null &&
                _currentPattern.AttackType == EEnemyAttackType.Rush)
            {
                p_locomotion?.Stop();
            }
        }

        // 공격 애니메이션이 끝난 패턴의 실행 정보를 정리한다.
        public void CompleteAttack(
            EnemyLocomotionModule p_locomotion)
        {
            if (!IsAttacking)
                return;

            EndAttackExecution(p_locomotion);
            ClearCurrentAttack();
        }

        public void CancelAttack(EnemyLocomotionModule p_locomotion)
        {
            EndAttackExecution(p_locomotion);

            ClearCurrentAttack();
        }

        public bool TryMeasureTarget(
            Transform p_target,
            out Vector3 p_direction,
            out float p_distance)
        {
            p_direction = Vector3.zero;
            p_distance = float.PositiveInfinity;

            Transform owner = ResolveOwner();

            if (owner == null ||
                p_target == null ||
                !p_target.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 origin = owner.position;
            Vector3 targetPoint = p_target.position;

            Collider targetCollider =
                p_target.GetComponent<Collider>() ??
                p_target.GetComponentInChildren<Collider>(true);

            if (targetCollider != null && targetCollider.enabled)
            {
                targetPoint = ColliderPointUtility.GetClosestPoint(
                    targetCollider,
                    origin);
            }

            p_direction = targetPoint - origin;
            p_direction.y = 0f;
            p_distance = p_direction.magnitude;

            return true;
        }

        private void BeginCurrentAttack()
        {
            Transform owner = ResolveOwner();
            bool usesColliderTiming =
                _currentPattern.HasExecutableTiming(
                    EEnemyAttackTimingType.Collider);

            switch (_currentPattern.AttackType)
            {
                case EEnemyAttackType.Melee:
                    if (!usesColliderTiming)
                        _meleeAttack.Execute(owner, _currentPattern);
                    break;

                case EEnemyAttackType.Range:
                    if (!_currentPattern.HasExecutableTiming(
                            EEnemyAttackTimingType.Projectile))
                    {
                        _rangeAttack.Execute(
                            owner,
                            _currentTarget,
                            _currentPattern);
                    }
                    break;

                case EEnemyAttackType.Rush:
                    _rushAttack.Begin(
                        owner,
                        _currentTarget,
                        _currentPattern);
                    break;

                case EEnemyAttackType.Area:
                    if (!usesColliderTiming)
                    {
                        _meleeAttack.Execute(
                            owner,
                            _currentPattern,
                            _currentPattern.AreaAttackArea);
                    }
                    break;

                case EEnemyAttackType.Arena:
                    if (!usesColliderTiming)
                    {
                        _meleeAttack.Execute(
                            owner,
                            _currentPattern,
                            _currentPattern.ArenaAttackArea);
                    }
                    break;
            }
        }

        private void PrepareAttackTimingExecution()
        {
            ResetAttackTimingExecution();

            int timingCount = _currentPattern?.AttackTimingCount ?? 0;

            if (_startedAttackTimings.Length != timingCount)
            {
                _startedAttackTimings = new bool[timingCount];
                _completedAttackTimings = new bool[timingCount];
            }
            else
            {
                Array.Clear(_startedAttackTimings, 0, timingCount);
                Array.Clear(_completedAttackTimings, 0, timingCount);
            }

            _lastAttackNormalizedTime = -1f;
        }

        private void ProcessAttackTimings(float p_normalizedTime)
        {
            int timingCount = _currentPattern.AttackTimingCount;

            for (int index = 0; index < timingCount; index++)
            {
                EnemyAttackTimingSetting timing =
                    _currentPattern.GetAttackTiming(index);

                if (timing == null ||
                    !timing.IsExecutable(_currentPattern.AttackType))
                {
                    continue;
                }

                bool startedThisUpdate = false;

                if (!_startedAttackTimings[index] &&
                    HasReachedTiming(
                        timing.StartNormalizedTime,
                        p_normalizedTime))
                {
                    _startedAttackTimings[index] = true;
                    startedThisUpdate = true;

                    switch (timing.EventType)
                    {
                        case EEnemyAttackTimingType.Projectile:
                            _rangeAttack.Execute(
                                ResolveOwner(),
                                _currentTarget,
                                _currentPattern);
                            _completedAttackTimings[index] = true;
                            break;

                        case EEnemyAttackTimingType.Collider:
                            if (!ActivateTimingCollider(
                                    timing.AttackCollider))
                            {
                                _completedAttackTimings[index] = true;
                            }
                            break;
                    }
                }

                if (timing.EventType !=
                        EEnemyAttackTimingType.Collider ||
                    !_startedAttackTimings[index] ||
                    _completedAttackTimings[index] ||
                    startedThisUpdate ||
                    p_normalizedTime < timing.EndNormalizedTime)
                {
                    continue;
                }

                DeactivateTimingCollider(timing.AttackCollider);
                _completedAttackTimings[index] = true;
            }

            _lastAttackNormalizedTime = Mathf.Max(
                _lastAttackNormalizedTime,
                p_normalizedTime);
        }

        private bool HasReachedTiming(
            float p_timing,
            float p_currentNormalizedTime)
        {
            return p_currentNormalizedTime >= p_timing &&
                   (_lastAttackNormalizedTime < 0f ||
                    _lastAttackNormalizedTime < p_timing);
        }

        private bool ActivateTimingCollider(Collider p_collider)
        {
            if (p_collider == null)
                return false;

            if (_activeTimingColliderCounts.TryGetValue(
                    p_collider,
                    out int activeCount))
            {
                _activeTimingColliderCounts[p_collider] =
                    activeCount + 1;
                return true;
            }

            EnemyAttackColliderSource source =
                p_collider.GetComponent<EnemyAttackColliderSource>() ??
                p_collider.gameObject.AddComponent<
                    EnemyAttackColliderSource>();

            AttackSession session = new(
                GetNextAttackId(),
                ResolveOwner(),
                _currentPattern.DamageProfile);

            if (!source.Activate(p_collider, session))
                return false;

            if (!p_collider.isTrigger)
            {
                Debug.LogWarning(
                    $"[{name}] 공격 Collider는 Trigger 설정이 필요합니다: " +
                    p_collider.name,
                    p_collider);
            }

            _activeTimingColliderCounts.Add(p_collider, 1);
            _activeTimingColliderSources.Add(p_collider, source);
            return true;
        }

        private void DeactivateTimingCollider(Collider p_collider)
        {
            if (p_collider == null ||
                !_activeTimingColliderCounts.TryGetValue(
                    p_collider,
                    out int activeCount))
            {
                return;
            }

            activeCount--;

            if (activeCount > 0)
            {
                _activeTimingColliderCounts[p_collider] = activeCount;
                return;
            }

            if (_activeTimingColliderSources.TryGetValue(
                    p_collider,
                    out EnemyAttackColliderSource source))
            {
                source.Deactivate();
            }
            else
            {
                p_collider.enabled = false;
            }

            _activeTimingColliderCounts.Remove(p_collider);
            _activeTimingColliderSources.Remove(p_collider);
        }

        private void ResetAttackTimingExecution()
        {
            foreach (EnemyAttackColliderSource source in
                     _activeTimingColliderSources.Values)
            {
                source?.Deactivate();
            }

            _activeTimingColliderCounts.Clear();
            _activeTimingColliderSources.Clear();
            _lastAttackNormalizedTime = -1f;
        }

        private void DisableAllConfiguredAttackColliders()
        {
            for (int patternIndex = 0;
                 patternIndex < PatternCount;
                 patternIndex++)
            {
                EnemyAttackPatternSetting pattern =
                    GetPattern(patternIndex);

                for (int timingIndex = 0;
                     timingIndex < (pattern?.AttackTimingCount ?? 0);
                     timingIndex++)
                {
                    Collider attackCollider = pattern
                        .GetAttackTiming(timingIndex)
                        ?.AttackCollider;

                    if (attackCollider == null)
                        continue;

                    attackCollider
                        .GetComponent<EnemyAttackColliderSource>()
                        ?.Deactivate();
                    attackCollider.enabled = false;
                }
            }
        }

        private int GetNextAttackId()
        {
            int attackId = _nextAttackId;
            _nextAttackId = _nextAttackId == int.MaxValue
                ? 1
                : _nextAttackId + 1;
            return attackId;
        }

        private void ClearCurrentAttack()
        {
            _currentTarget = null;
            _currentPattern = null;
            _didActivateAttack = false;
        }

        private Transform ResolveOwner()
        {
            if (_owner != null)
                return _owner;

            EnemyCore core = GetComponentInParent<EnemyCore>();
            return core != null
                ? core.transform
                : transform.parent;
        }

        private void EnsureSettings()
        {
            _patternSettings = ResolvePatternSettings();
            MigrateLegacySettings();

            _patternSettings?.Validate();

            if (_distancePatterns == null ||
                _distancePatterns.Length == 0)
            {
                _distancePatterns =
                    new[] { new EnemyDistancePatternSetting() };
            }

            for (int index = 0;
                 index < _distancePatterns.Length;
                 index++)
            {
                _distancePatterns[index] ??=
                    new EnemyDistancePatternSetting();
                _distancePatterns[index].Validate(PatternCount);
            }
        }

        private void MigrateLegacySettings()
        {
            if (_legacyAttackPatterns == null ||
                _legacyAttackPatterns.Length == 0)
            {
                return;
            }

            _patternSettings = ResolvePatternSettings();

            if (_patternSettings == null)
                return;

            _patternSettings.ReplacePatterns(_legacyAttackPatterns);

            _distancePatterns =
                new EnemyDistancePatternSetting[
                    _legacyAttackPatterns.Length];

            for (int index = 0;
                 index < _legacyAttackPatterns.Length;
                 index++)
            {
                EnemyAttackPatternSetting pattern =
                    _legacyAttackPatterns[index] ??
                    new EnemyAttackPatternSetting();

                _distancePatterns[index] =
                    new EnemyDistancePatternSetting(
                        pattern.PatternName,
                        pattern.LegacyMinimumDistance,
                        pattern.ProjectileMaximumDistance,
                        index,
                        pattern.LegacySelectionWeight);
            }

            _legacyAttackPatterns =
                System.Array.Empty<EnemyAttackPatternSetting>();
        }

        private EnemyAttackPatternSettings ResolvePatternSettings()
        {
            if (_patternSettings != null)
                return _patternSettings;

            _patternSettings =
                GetComponent<EnemyAttackPatternSettings>();

            return _patternSettings;
        }

        private void OnDisable()
        {
            EnemyCore core = GetComponentInParent<EnemyCore>();
            CancelAttack(core != null
                ? core.LocomotionModule
                : null);
        }

        private void OnValidate()
        {
            EnsureSettings();
        }
    }
}
