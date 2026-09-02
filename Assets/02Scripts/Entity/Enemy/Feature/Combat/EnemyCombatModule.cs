using System;
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

        private Transform _currentTarget;
        private EnemyAttackPatternSetting _currentPattern;
        private bool _didActivateAttack;
        private bool[] _firedProjectileTimes = Array.Empty<bool>();
        private float _lastAttackElapsedTime = -1f;

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
            PrepareAttackExecution();

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
                p_deltaTime);
        }

        // Animation View가 전달한 경과 초에서 Area와 Projectile 실행 시점을 처리한다.
        public void UpdateAttackAnimationTime(
            float p_elapsedSeconds,
            float p_durationSeconds)
        {
            if (!IsAttackActivated)
                return;

            float elapsedSeconds = Mathf.Max(0f, p_elapsedSeconds);

            ProcessProjectileFireTimes(elapsedSeconds);
            ProcessAttackArea(elapsedSeconds);

            _lastAttackElapsedTime = Mathf.Max(
                _lastAttackElapsedTime,
                elapsedSeconds);

            if (_currentPattern.AttackType == EEnemyAttackType.Rush)
            {
                _rushAttack.SynchronizeAnimationTime(
                    p_elapsedSeconds,
                    p_durationSeconds,
                    _currentPattern.RushJumpStartTimeSeconds,
                    _currentPattern.RushLandingTimeSeconds);
            }
        }

        // Rush 종료 직전 남은 이동량을 마지막 물리 Tick에서 적용할 수 있게 한다.
        public void CompleteAttackAnimationTime()
        {
            if (!IsAttackActivated ||
                _currentPattern.AttackType != EEnemyAttackType.Rush)
            {
                return;
            }

            _rushAttack.CompleteAnimationTime();
        }

        public void EndAttackExecution(
            EnemyLocomotionModule p_locomotion)
        {
            ResetAttackExecution();
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

            switch (_currentPattern.AttackType)
            {
                case EEnemyAttackType.Melee:
                case EEnemyAttackType.Area:
                case EEnemyAttackType.Arena:
                    _meleeAttack.Begin();
                    break;

                case EEnemyAttackType.Range:
                    if (_currentPattern.ProjectileFireTimeCount == 0)
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
            }
        }

        private void PrepareAttackExecution()
        {
            ResetAttackExecution();

            int fireTimeCount =
                _currentPattern?.ProjectileFireTimeCount ?? 0;

            if (_firedProjectileTimes.Length != fireTimeCount)
            {
                _firedProjectileTimes = new bool[fireTimeCount];
            }
            else
            {
                Array.Clear(
                    _firedProjectileTimes,
                    0,
                    fireTimeCount);
            }

            _lastAttackElapsedTime = -1f;
        }

        private void ProcessProjectileFireTimes(float p_elapsedSeconds)
        {
            if (_currentPattern.AttackType != EEnemyAttackType.Range)
                return;

            int fireTimeCount = _currentPattern.ProjectileFireTimeCount;

            for (int index = 0; index < fireTimeCount; index++)
            {
                if (_firedProjectileTimes[index] ||
                    !HasReachedTime(
                        _currentPattern.GetProjectileFireTime(index),
                        p_elapsedSeconds))
                {
                    continue;
                }

                _firedProjectileTimes[index] = true;
                _rangeAttack.Execute(
                    ResolveOwner(),
                    _currentTarget,
                    _currentPattern);
            }

        }

        private void ProcessAttackArea(float p_elapsedSeconds)
        {
            Transform owner = ResolveOwner();
            EnemyAttackAreaSetting area =
                _currentPattern.AttackType switch
                {
                    EEnemyAttackType.Melee =>
                        _currentPattern.MeleeArea,
                    EEnemyAttackType.Area =>
                        _currentPattern.AreaAttackArea,
                    EEnemyAttackType.Arena =>
                        _currentPattern.ArenaAttackArea,
                    _ => null
                };

            if (area == null ||
                (!area.IsActive(p_elapsedSeconds) &&
                 !HasReachedTime(
                     area.ActivationTimeSeconds,
                     p_elapsedSeconds)))
            {
                return;
            }

            _meleeAttack.Execute(
                owner,
                _currentPattern,
                area);
        }

        private bool HasReachedTime(
            float p_timingSeconds,
            float p_currentElapsedTime)
        {
            return p_currentElapsedTime >= p_timingSeconds &&
                   (_lastAttackElapsedTime < 0f ||
                    _lastAttackElapsedTime < p_timingSeconds);
        }

        private void ResetAttackExecution()
        {
            _meleeAttack.End();
            Array.Clear(
                _firedProjectileTimes,
                0,
                _firedProjectileTimes.Length);
            _lastAttackElapsedTime = -1f;
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
