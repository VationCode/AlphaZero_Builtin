using System;
using Alpha.Utility;
using UnityEngine;

namespace Alpha.Enemy
{
    // 공격 패턴 설정을 보관하고 선택된 타입의 실제 공격을 실행한다.
    [DisallowMultipleComponent]
    public sealed class EnemyCombatModule : MonoBehaviour
    {
        public const int MinimumPatternCount = 1;
        public const int MaximumPatternCount = 2;

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

        private float[] _cooldownEndTimes =
            new float[MinimumPatternCount];

        private Transform _currentTarget;
        private EnemyAttackPatternSetting _currentPattern;
        private int _currentPatternIndex = -1;
        private float _attackElapsedTime;
        private bool _didActivateAttack;

        public int PatternCount => _attackPatterns?.Length ?? 0;
        public Transform Owner => ResolveOwner();
        public bool IsAttacking => _currentPattern != null;
        public bool IsRushMovementActive =>
            IsAttacking &&
            _currentPattern.AttackType == EEnemyAttackType.Rush &&
            _rushAttack.IsActive;

        public EnemyAttackPatternSetting CurrentPattern =>
            _currentPattern;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
            EnsurePatternRange();
            EnsureCooldownStorage();
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

        // AttackFlow가 가중치 선택 전에 거리와 쿨타임 후보를 검사한다.
        public bool CanStartPattern(
            int p_patternIndex,
            Transform p_target)
        {
            if (IsAttacking ||
                Time.time < GetCooldownEndTime(p_patternIndex))
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
            return Mathf.Max(
                0f,
                GetCooldownEndTime(p_patternIndex) - Time.time);
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
            _attackElapsedTime = 0f;
            _didActivateAttack = false;

            return true;
        }

        // 현재 패턴의 선딜레이, 실행, 후딜레이 순서를 갱신한다.
        public bool TickAttack(
            Transform p_target,
            EnemyLocomotionModule p_locomotion,
            float p_deltaTime)
        {
            if (!IsAttacking)
                return false;

            if (p_target != null && p_target.gameObject.activeInHierarchy)
                _currentTarget = p_target;

            _attackElapsedTime += Mathf.Max(0f, p_deltaTime);

            if (!_didActivateAttack &&
                _attackElapsedTime >= _currentPattern.WindupDuration)
            {
                ActivateCurrentAttack();
            }

            if (_didActivateAttack &&
                _currentPattern.AttackType == EEnemyAttackType.Rush)
            {
                float rushEndTime =
                    _currentPattern.WindupDuration +
                    _currentPattern.RushDuration;

                if (_attackElapsedTime <= rushEndTime)
                {
                    _rushAttack.Tick(
                        ResolveOwner(),
                        p_locomotion,
                        _currentPattern,
                        p_deltaTime);
                }
                else
                {
                    _rushAttack.End();
                    p_locomotion?.Stop();
                }
            }

            if (_attackElapsedTime < _currentPattern.TotalDuration)
                return false;

            CompleteAttack(p_locomotion);
            return true;
        }

        public void CancelAttack(EnemyLocomotionModule p_locomotion)
        {
            if (_currentPattern != null && _didActivateAttack)
                StartCooldown(_currentPatternIndex, _currentPattern);

            _rushAttack.End();

            if (_currentPattern != null &&
                _currentPattern.AttackType == EEnemyAttackType.Rush)
            {
                p_locomotion?.Stop();
            }

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

        private void ActivateCurrentAttack()
        {
            _didActivateAttack = true;
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

        private void CompleteAttack(
            EnemyLocomotionModule p_locomotion)
        {
            EnemyAttackPatternSetting completedPattern =
                _currentPattern;
            int completedPatternIndex = _currentPatternIndex;

            _rushAttack.End();

            if (completedPattern.AttackType == EEnemyAttackType.Rush)
                p_locomotion?.Stop();

            StartCooldown(
                completedPatternIndex,
                completedPattern);
            ClearCurrentAttack();
        }

        private void StartCooldown(
            int p_patternIndex,
            EnemyAttackPatternSetting p_pattern)
        {
            EnsureCooldownStorage();

            if (p_patternIndex < 0 ||
                p_patternIndex >= _cooldownEndTimes.Length ||
                p_pattern == null)
            {
                return;
            }

            _cooldownEndTimes[p_patternIndex] =
                Time.time + p_pattern.Cooldown;
        }

        private float GetCooldownEndTime(int p_patternIndex)
        {
            EnsureCooldownStorage();

            return p_patternIndex >= 0 &&
                   p_patternIndex < _cooldownEndTimes.Length
                ? _cooldownEndTimes[p_patternIndex]
                : float.PositiveInfinity;
        }

        private void ClearCurrentAttack()
        {
            _currentTarget = null;
            _currentPattern = null;
            _currentPatternIndex = -1;
            _attackElapsedTime = 0f;
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
            else if (_attackPatterns.Length > MaximumPatternCount)
            {
                Array.Resize(
                    ref _attackPatterns,
                    MaximumPatternCount);
            }

            for (int index = 0; index < _attackPatterns.Length; index++)
            {
                _attackPatterns[index] ??=
                    new EnemyAttackPatternSetting();
                _attackPatterns[index].Validate();
            }
        }

        private void EnsureCooldownStorage()
        {
            int requiredCount = Mathf.Max(
                MinimumPatternCount,
                PatternCount);

            if (_cooldownEndTimes == null)
            {
                _cooldownEndTimes = new float[requiredCount];
                return;
            }

            if (_cooldownEndTimes.Length != requiredCount)
            {
                Array.Resize(
                    ref _cooldownEndTimes,
                    requiredCount);
            }
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
            EnsureCooldownStorage();
        }
    }
}
