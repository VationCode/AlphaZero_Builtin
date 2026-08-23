using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // ActionFlow가 허용한 동안 공격 패턴의 준비·실행·회복 상태를 조정한다.
    [DisallowMultipleComponent]
    public sealed class EnemyCombatFlow : MonoBehaviour
    {
        private readonly EnemyAttackPatternSelector _patternSelector = new();

        private EnemyCore _core;
        private int _preparedPatternIndex = -1;
        private float _stateElapsedTime;
        private bool _hasCurrentState;

        public EEnemyCombatState CurrentState { get; private set; } =
            EEnemyCombatState.Idle;

        public bool IsBusy => CurrentState != EEnemyCombatState.Idle;

        public event Action<EEnemyCombatState> OnStateChanged;
        public event Action<EEnemyAttackType> OnAttackWaitStarted;
        public event Action<EEnemyAttackType> OnAttackStarted;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _hasCurrentState = false;
            ClearPreparedPattern();
            _core?.CombatModule?.CancelAttack(
                _core.LocomotionModule);
            ChangeState(EEnemyCombatState.Idle);
        }

        // 공격 중이거나 현재 거리에서 준비 가능한 패턴이 있으면 이동 점유를 요청한다.
        public bool WantsControl(Transform p_target)
        {
            if (_core == null || p_target == null)
                return false;

            EnemyCombatModule combat = _core.CombatModule;

            if (combat == null)
                return false;

            return CurrentState is EEnemyCombatState.Attack or
                       EEnemyCombatState.Recovery ||
                   combat.CanEngageTarget(p_target);
        }

        public void Tick(
            Transform p_target,
            float p_deltaTime)
        {
            EnemyCombatModule combat =
                _core != null ? _core.CombatModule : null;

            if (combat == null || p_target == null)
            {
                CancelCombat();
                return;
            }

            switch (CurrentState)
            {
                case EEnemyCombatState.Idle:
                    if (!TryPreparePattern(combat, p_target))
                        return;

                    ChangeState(EEnemyCombatState.Prepare);
                    TickPrepare(p_target, p_deltaTime);
                    break;

                case EEnemyCombatState.Prepare:
                    if (!combat.CanEngageTarget(p_target))
                    {
                        CancelCombat();
                        return;
                    }

                    TickPrepare(p_target, p_deltaTime);
                    break;

                case EEnemyCombatState.Attack:
                    TickAttack(p_target, p_deltaTime);
                    break;

                case EEnemyCombatState.Recovery:
                    TickRecovery(p_target, p_deltaTime);
                    break;
            }
        }

        public void CancelCombat()
        {
            ClearPreparedPattern();

            if (_core != null)
            {
                _core.CombatModule?.CancelAttack(
                    _core.LocomotionModule);
            }

            ChangeState(EEnemyCombatState.Idle);
        }

        private void TickPrepare(
            Transform p_target,
            float p_deltaTime)
        {
            EnemyCombatModule combat = _core.CombatModule;

            if (!TryPreparePattern(combat, p_target))
            {
                CancelCombat();
                return;
            }

            EnemyLocomotionModule locomotion =
                _core.LocomotionModule;
            bool isFacingTarget = locomotion == null;

            if (locomotion != null)
            {
                locomotion.Stop();
                isFacingTarget = locomotion.RotateTo(
                    p_target.position,
                    p_deltaTime);
            }

            if (!isFacingTarget ||
                !combat.CanStartPattern(
                    _preparedPatternIndex,
                    p_target) ||
                !combat.TryBeginAttack(
                    _preparedPatternIndex,
                    p_target,
                    out EnemyAttackPatternSetting pattern))
            {
                return;
            }

            OnAttackStarted?.Invoke(pattern.AttackType);
            ChangeState(EEnemyCombatState.Attack);
            TickAttack(p_target, p_deltaTime);
        }

        private void TickAttack(
            Transform p_target,
            float p_deltaTime)
        {
            EnemyCombatModule combat = _core.CombatModule;
            EnemyAttackPatternSetting pattern = combat.CurrentPattern;

            if (pattern == null)
            {
                CancelCombat();
                return;
            }

            float deltaTime = Mathf.Max(0f, p_deltaTime);
            _stateElapsedTime += deltaTime;

            if (!combat.IsAttackActivated)
            {
                EnemyLocomotionModule locomotion =
                    _core.LocomotionModule;

                if (locomotion != null)
                {
                    locomotion.Stop();
                    locomotion.RotateTo(
                        p_target.position,
                        deltaTime);
                }

                if (_stateElapsedTime < pattern.WindupDuration)
                    return;

                if (!combat.ActivateAttack(p_target))
                {
                    CancelCombat();
                    return;
                }

                if (pattern.AttackType != EEnemyAttackType.Rush)
                {
                    ChangeState(EEnemyCombatState.Recovery);
                    return;
                }
            }

            float activeEndTime =
                pattern.WindupDuration + pattern.RushDuration;

            if (_stateElapsedTime <= activeEndTime)
            {
                combat.TickActiveAttack(
                    _core.LocomotionModule,
                    deltaTime);
                return;
            }

            combat.EndAttackExecution(
                _core.LocomotionModule);
            ChangeState(EEnemyCombatState.Recovery);
        }

        private void TickRecovery(
            Transform p_target,
            float p_deltaTime)
        {
            EnemyCombatModule combat = _core.CombatModule;
            EnemyAttackPatternSetting pattern = combat.CurrentPattern;

            if (pattern == null)
            {
                CancelCombat();
                return;
            }

            _stateElapsedTime += Mathf.Max(0f, p_deltaTime);

            if (_stateElapsedTime < pattern.RecoveryDuration)
                return;

            combat.CompleteAttack(
                _core.LocomotionModule);
            ClearPreparedPattern();

            if (!TryPreparePattern(combat, p_target))
            {
                ChangeState(EEnemyCombatState.Idle);
                return;
            }

            ChangeState(EEnemyCombatState.Prepare);
        }

        // 유효한 패턴을 예약하고 타입에 맞는 공격 대기 표현을 요청한다.
        private bool TryPreparePattern(
            EnemyCombatModule p_combat,
            Transform p_target)
        {
            if (_preparedPatternIndex >= 0)
            {
                if (p_combat.CanStartPattern(
                        _preparedPatternIndex,
                        p_target))
                {
                    return true;
                }

                // 대기 중 다른 패턴이 먼저 준비되면 해당 패턴으로 교체한다.
                if (_patternSelector.TrySelectReadyPattern(
                        p_combat,
                        p_target,
                        out int readyPatternIndex))
                {
                    return PreparePattern(
                        p_combat,
                        readyPatternIndex);
                }

                if (p_combat.CanPreparePattern(
                        _preparedPatternIndex,
                        p_target))
                {
                    return true;
                }
            }

            ClearPreparedPattern();

            if (!_patternSelector.TrySelectPattern(
                    p_combat,
                    p_target,
                    out int patternIndex))
            {
                return false;
            }

            return PreparePattern(
                p_combat,
                patternIndex);
        }

        private bool PreparePattern(
            EnemyCombatModule p_combat,
            int p_patternIndex)
        {
            EnemyAttackPatternSetting pattern =
                p_combat.GetPattern(p_patternIndex);

            if (pattern == null)
                return false;

            _preparedPatternIndex = p_patternIndex;
            OnAttackWaitStarted?.Invoke(pattern.AttackType);
            return true;
        }

        private void ChangeState(EEnemyCombatState p_nextState)
        {
            if (_hasCurrentState && CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            _stateElapsedTime = 0f;
            _hasCurrentState = true;
            OnStateChanged?.Invoke(p_nextState);
        }

        private void ClearPreparedPattern()
        {
            _preparedPatternIndex = -1;
        }

        private void OnDisable()
        {
            CancelCombat();
        }
    }
}
