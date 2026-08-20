using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // 감지 대상과 공격 거리, 순찰 영역을 기준으로 Enemy 행동을 조정한다.
    [DisallowMultipleComponent]
    public sealed class EnemyActionFlow : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        private float _targetScanInterval = 0.2f;

        private EnemyCore _core;
        private float _nextTargetScanTime;
        private bool _hasCurrentState;

        public EEnemyActionState CurrentState { get; private set; } =
            EEnemyActionState.Patrol;

        public event Action<EEnemyActionState> OnStateChanged;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _nextTargetScanTime = Time.time;
            _hasCurrentState = false;

            ChangeState(EEnemyActionState.Patrol);
        }

        private void FixedUpdate()
        {
            if (_core == null)
                return;

            // 복귀 중에는 같은 대상을 다시 감지하지 않는다.
            if (CurrentState == EEnemyActionState.ReturnToPatrol)
            {
                ExecuteCurrentState();
                return;
            }

            RefreshTarget();

            if (ShouldReturnToPatrol())
            {
                _core.ClearTarget();
                ChangeState(EEnemyActionState.ReturnToPatrol);
            }
            else
            {
                ChangeState(SelectState());
            }

            ExecuteCurrentState();
        }

        private void RefreshTarget()
        {
            EnemyTargetModule targetModule = _core.TargetModule;
            Transform currentTarget = _core.Target;

            // 이미 발견한 대상은 살아 있는 동안 유지한다.
            if (targetModule != null &&
                targetModule.IsValidTarget(currentTarget))
            {
                return;
            }

            if (currentTarget != null)
                _core.ClearTarget();

            if (targetModule == null ||
                Time.time < _nextTargetScanTime)
            {
                return;
            }

            _nextTargetScanTime =
                Time.time + Mathf.Max(0.05f, _targetScanInterval);

            if (targetModule.TryDetectClosestTarget(
                    out Transform detectedTarget))
            {
                _core.SetTarget(detectedTarget);
            }
        }

        private bool ShouldReturnToPatrol()
        {
            EnemyLocomotionModule locomotion =
                _core.LocomotionModule;

            if (_core.Target == null || locomotion == null)
                return false;

            return locomotion.IsOutsideChaseBoundary(
                _core.transform.position);
        }

        private EEnemyActionState SelectState()
        {
            Transform target = _core.Target;

            if (target == null)
                return EEnemyActionState.Patrol;

            EnemyCombatModule combatModule = _core.CombatModule;

            if (combatModule != null && combatModule.IsAttacking)
                return EEnemyActionState.Attack;

            return _core.AttackFlow != null &&
                   _core.AttackFlow.CanEnterAttack(target)
                ? EEnemyActionState.Attack
                : EEnemyActionState.Chase;
        }

        private void ChangeState(EEnemyActionState p_nextState)
        {
            if (_hasCurrentState && CurrentState == p_nextState)
                return;

            EEnemyActionState previousState = CurrentState;
            bool hadCurrentState = _hasCurrentState;

            CurrentState = p_nextState;
            _hasCurrentState = true;

            if (hadCurrentState &&
                previousState == EEnemyActionState.Attack &&
                p_nextState != EEnemyActionState.Attack)
            {
                _core?.AttackFlow?.CancelAttack();
            }

            EnemyLocomotionModule locomotion =
                _core != null ? _core.LocomotionModule : null;

            if (locomotion != null)
            {
                if (p_nextState == EEnemyActionState.Patrol)
                {
                    locomotion.Stop();
                    locomotion.SetPatrolEnabled(true);
                }
                else
                {
                    locomotion.SetPatrolEnabled(false);
                }
            }

            OnStateChanged?.Invoke(CurrentState);
        }

        private void ExecuteCurrentState()
        {
            EnemyLocomotionModule locomotion =
                _core.LocomotionModule;

            if (locomotion == null)
                return;

            Transform target = _core.Target;

            switch (CurrentState)
            {
                case EEnemyActionState.Chase when target != null:
                    locomotion.MoveTo(
                        target.position,
                        Time.fixedDeltaTime);
                    break;

                case EEnemyActionState.Attack when target != null:
                    ExecuteAttack(
                        locomotion,
                        target);
                    break;

                case EEnemyActionState.ReturnToPatrol:
                    ExecuteReturnToPatrol(locomotion);
                    break;
            }
        }

        private void ExecuteAttack(
            EnemyLocomotionModule p_locomotion,
            Transform p_target)
        {
            EnemyCombatModule combat = _core.CombatModule;
            bool isRushMoving =
                combat != null && combat.IsRushMovementActive;
            bool isFacingTarget = true;

            if (!isRushMoving)
            {
                p_locomotion.Stop();
                isFacingTarget = p_locomotion.RotateTo(
                    p_target.position,
                    Time.fixedDeltaTime);
            }

            _core.AttackFlow?.TickAttack(
                p_target,
                isFacingTarget,
                Time.fixedDeltaTime);
        }

        private void ExecuteReturnToPatrol(
            EnemyLocomotionModule p_locomotion)
        {
            if (p_locomotion.IsInsidePatrolArea(
                    _core.transform.position))
            {
                ChangeState(EEnemyActionState.Patrol);
                return;
            }

            p_locomotion.MoveTo(
                p_locomotion.AreaCenter,
                Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            if (_core == null)
                return;

            _core.AttackFlow?.CancelAttack();
            _core.ClearTarget();
            _core.LocomotionModule?.Stop();
            _core.LocomotionModule?.SetPatrolEnabled(true);
            _hasCurrentState = false;
        }

        private void OnValidate()
        {
            _targetScanInterval =
                Mathf.Max(0.05f, _targetScanInterval);
        }
    }
}
