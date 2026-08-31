using Alpha.Utility;
using UnityEngine;

namespace Alpha.Enemy
{
    // 공격 패턴 설정을 보관하고 선택된 타입의 실제 공격을 실행한다.
    [DisallowMultipleComponent]
    public sealed class EnemyCombatModule : MonoBehaviour
    {
        public const int MinimumPatternCount = 1;

        [SerializeField]
        private Transform _owner;

        [SerializeField]
        private EnemyAttackPatternSetting[] _attackPatterns =
        {
            new()
        };

        private readonly EnemyMeleeAttackModule _meleeAttack = new();
        private readonly EnemyRangeAttackModule _rangeAttack = new();
        private readonly EnemyRushAttackModule _rushAttack = new();
        private readonly EnemyAttackCooldown _attackCooldown = new();

        private Transform _currentTarget;
        private EnemyAttackPatternSetting _currentPattern;
        private int _currentPatternIndex = -1;
        private bool _didActivateAttack;

        public int PatternCount => _attackPatterns?.Length ?? 0;
        public Transform Owner => ResolveOwner();
        public bool IsAttacking => _currentPattern != null;
        public bool IsAttackActivated =>
            IsAttacking && _didActivateAttack;

        public EnemyAttackPatternSetting CurrentPattern =>
            _currentPattern;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
            EnsurePatternRange();
            ConfigureCooldown();
            CancelAttack(null);
        }

        public EnemyAttackPatternSetting GetPattern(int p_index)
        {
            return p_index >= 0 && p_index < PatternCount
                ? _attackPatterns[p_index]
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

            for (int index = 0; index < PatternCount; index++)
            {
                EnemyAttackPatternSetting pattern =
                    _attackPatterns[index];

                if (pattern != null &&
                    pattern.IsExecutable &&
                    pattern.IsWithinDistance(distance))
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

            for (int index = 0; index < PatternCount; index++)
            {
                EnemyAttackPatternSetting pattern =
                    _attackPatterns[index];

                if (pattern == null || !pattern.IsExecutable)
                    continue;

                hasExecutablePattern = true;

                if (pattern.IsWithinDistance(distance))
                {
                    p_distanceAdjustment = 0f;
                    return true;
                }

                float adjustment = distance < pattern.MinimumDistance
                    ? pattern.MinimumDistance - distance
                    : pattern.MaximumDistance - distance;
                float movementDistance = Mathf.Abs(adjustment);

                if (movementDistance >= closestMovementDistance)
                    continue;

                closestMovementDistance = movementDistance;
                closestAdjustment = adjustment;
            }

            p_distanceAdjustment = closestAdjustment;
            return hasExecutablePattern;
        }

        // CombatFlow가 가중치 선택 전에 거리와 쿨타임 후보를 검사한다.
        public bool CanStartPattern(
            int p_patternIndex,
            Transform p_target)
        {
            ConfigureCooldown();

            if (IsAttacking ||
                !_attackCooldown.IsReady(
                    p_patternIndex,
                    Time.time))
            {
                return false;
            }

            return CanPreparePattern(
                p_patternIndex,
                p_target);
        }

        // 쿨타임과 무관하게 현재 거리에서 대기할 수 있는 패턴인지 확인한다.
        public bool CanPreparePattern(
            int p_patternIndex,
            Transform p_target)
        {
            EnemyAttackPatternSetting pattern =
                GetPattern(p_patternIndex);

            if (pattern == null || !pattern.IsExecutable)
                return false;

            return TryMeasureTarget(
                       p_target,
                       out _,
                       out float distance) &&
                   pattern.IsWithinDistance(distance);
        }

        // 가장 먼저 준비될 패턴을 고를 수 있도록 남은 쿨타임을 반환한다.
        public float GetCooldownRemaining(int p_patternIndex)
        {
            ConfigureCooldown();
            return _attackCooldown.GetRemaining(
                p_patternIndex,
                Time.time);
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

            _currentPatternIndex = p_patternIndex;
            _currentPattern = p_pattern;
            _currentTarget = p_target;
            _didActivateAttack = false;

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
            ExecuteCurrentAttack();
            return true;
        }

        // Rush처럼 실행 구간 동안 지속 갱신이 필요한 공격만 처리한다.
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

        public void EndAttackExecution(
            EnemyLocomotionModule p_locomotion)
        {
            _rushAttack.End();

            if (_currentPattern != null &&
                _currentPattern.AttackType == EEnemyAttackType.Rush)
            {
                p_locomotion?.Stop();
            }
        }

        // Recovery가 끝난 패턴의 쿨타임을 시작하고 실행 정보를 정리한다.
        public void CompleteAttack(
            EnemyLocomotionModule p_locomotion)
        {
            if (!IsAttacking)
                return;

            EnemyAttackPatternSetting completedPattern =
                _currentPattern;
            int completedPatternIndex = _currentPatternIndex;

            EndAttackExecution(p_locomotion);
            StartCooldown(
                completedPatternIndex,
                completedPattern);
            ClearCurrentAttack();
        }

        public void CancelAttack(EnemyLocomotionModule p_locomotion)
        {
            if (_currentPattern != null && _didActivateAttack)
                StartCooldown(_currentPatternIndex, _currentPattern);

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

        private void ExecuteCurrentAttack()
        {
            Transform owner = ResolveOwner();

            switch (_currentPattern.AttackType)
            {
                case EEnemyAttackType.Melee:
                    _meleeAttack.Execute(owner, _currentPattern);
                    break;

                case EEnemyAttackType.Range:
                    _rangeAttack.Execute(
                        owner,
                        _currentTarget,
                        _currentPattern);
                    break;

                case EEnemyAttackType.Rush:
                    _rushAttack.Begin(
                        owner,
                        _currentTarget,
                        _currentPattern);
                    break;
            }
        }

        private void StartCooldown(
            int p_patternIndex,
            EnemyAttackPatternSetting p_pattern)
        {
            ConfigureCooldown();

            if (p_pattern == null)
                return;

            _attackCooldown.Start(
                p_patternIndex,
                p_pattern.Cooldown,
                Time.time);
        }

        private void ClearCurrentAttack()
        {
            _currentTarget = null;
            _currentPattern = null;
            _currentPatternIndex = -1;
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

        private void EnsurePatternRange()
        {
            if (_attackPatterns == null || _attackPatterns.Length == 0)
            {
                _attackPatterns =
                    new[] { new EnemyAttackPatternSetting() };
            }
            for (int index = 0; index < _attackPatterns.Length; index++)
            {
                _attackPatterns[index] ??=
                    new EnemyAttackPatternSetting();
                _attackPatterns[index].Validate();
            }
        }

        private void ConfigureCooldown()
        {
            _attackCooldown.Configure(PatternCount);
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
            EnsurePatternRange();
            ConfigureCooldown();
        }
    }
}
