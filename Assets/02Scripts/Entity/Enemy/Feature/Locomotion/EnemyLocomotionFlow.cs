using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // ActionFlow가 허용한 동안 순찰·추적·복귀 상태를 선택하고 이동 Module을 실행한다.
    public sealed class EnemyLocomotionFlow
    {
        private EnemyCore _core;
        private bool _hasCurrentState;

        public EEnemyLocomotionState CurrentState { get; private set; } =
            EEnemyLocomotionState.Idle;

        public bool IsReturningToPatrol =>
            CurrentState == EEnemyLocomotionState.ReturnToPatrol;

        public event Action<EEnemyLocomotionState> OnStateChanged;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _hasCurrentState = false;
            ChangeState(EEnemyLocomotionState.Patrol);
        }

        // 물리 넉백은 일반 이동 허용 여부와 관계없이 계속 갱신한다.
        public bool TickKnockback(float p_deltaTime)
        {
            return _core?.LocomotionModule?.TickKnockback(p_deltaTime) == true;
        }

        // 이동 상태를 선택하고 Combat이 이동을 점유할 수 있는지 반환한다.
        public bool Tick(
            Transform p_target,
            bool p_combatOwnsMovement,
            float p_deltaTime)
        {
            EnemyLocomotionModule locomotion =
                _core != null ? _core.LocomotionModule : null;

            if (locomotion == null)
                return false;

            if (IsReturningToPatrol &&
                !locomotion.IsInsidePatrolArea(_core.transform.position))
            {
                locomotion.MoveTo(
                    locomotion.AreaCenter,
                    p_deltaTime);
                return false;
            }

            if (p_target != null &&
                locomotion.IsOutsideChaseBoundary(_core.transform.position))
            {
                _core.TargetingFlow.ClearTarget();
                ChangeState(EEnemyLocomotionState.ReturnToPatrol);
                locomotion.MoveTo(
                    locomotion.AreaCenter,
                    p_deltaTime);
                return false;
            }

            if (p_combatOwnsMovement)
            {
                ChangeState(EEnemyLocomotionState.Idle);
                return true;
            }

            ChangeState(
                p_target != null
                    ? EEnemyLocomotionState.Chase
                    : EEnemyLocomotionState.Patrol);

            if (CurrentState == EEnemyLocomotionState.Chase)
            {
                locomotion.MoveTo(
                    p_target.position,
                    p_deltaTime);
            }
            else
            {
                locomotion.TickPatrol(p_deltaTime);
            }

            return false;
        }

        // 피격·사망처럼 상위 Action이 이동을 막을 때 일반 이동만 정지한다.
        public void Stop()
        {
            _core?.LocomotionModule?.Stop();
            ChangeState(EEnemyLocomotionState.Idle);
        }

        public void Reset()
        {
            _core?.LocomotionModule?.Stop();
            CurrentState = EEnemyLocomotionState.Idle;
            _hasCurrentState = false;
        }

        private void ChangeState(EEnemyLocomotionState p_nextState)
        {
            if (_hasCurrentState && CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            _hasCurrentState = true;

            if (p_nextState is EEnemyLocomotionState.Idle or
                EEnemyLocomotionState.Patrol)
            {
                _core?.LocomotionModule?.Stop();
            }

            OnStateChanged?.Invoke(p_nextState);
        }
    }
}
