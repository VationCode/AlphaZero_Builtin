using System;
using UnityEngine;

namespace Alpha.Enemy
{
    // ActionFlow가 허용한 동안 공격 패턴의 준비·실행·대기 상태를 조정한다.
    [DisallowMultipleComponent]
    public sealed class EnemyCombatFlow : MonoBehaviour
    {
        [Header("Attack Cycle")]
        [Tooltip("공격 애니메이션이 끝난 뒤 다음 타겟 위치 확인까지 기다릴 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _nextAttackDelay = 1f;

        private readonly EnemyAttackPatternSelector _patternSelector = new();

        private EnemyCore _core;
        private int _preparedPatternIndex = -1;
        private float _stateElapsedTime;
        private bool _hasCurrentState;

        public EEnemyCombatState CurrentState { get; private set; } =
            EEnemyCombatState.Idle;

        public bool IsBusy => CurrentState != EEnemyCombatState.Idle;

        public event Action<EEnemyCombatState> OnStateChanged;
        public event Action<EEnemyAttackType, int> OnAttackStarted;

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
                       EEnemyCombatState.Wait ||
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
                    TickAttack(p_deltaTime);
                    break;

                case EEnemyCombatState.Wait:
                    TickWait(p_target, p_deltaTime);
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

            if (_preparedPatternIndex < 0 ||
                !combat.CanStartPattern(
                    _preparedPatternIndex,
                    p_target))
            {
                if (!TryPreparePattern(combat, p_target))
                {
                    CancelCombat();
                    return;
                }
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

            if (!isFacingTarget)
                return;

            if (!combat.TryBeginAttack(
                    _preparedPatternIndex,
                    p_target,
                    out EnemyAttackPatternSetting pattern))
            {
                return;
            }

            ChangeState(EEnemyCombatState.Attack);
            OnAttackStarted?.Invoke(
                pattern.AttackType,
                pattern.AnimationIndex);

            if (!combat.ActivateAttack(p_target))
                CancelCombat();
        }

        private void TickAttack(float p_deltaTime)
        {
            EnemyCombatModule combat = _core.CombatModule;
            EnemyAttackPatternSetting pattern = combat.CurrentPattern;

            if (pattern == null)
            {
                CancelCombat();
                return;
            }

            if (pattern.AttackType == EEnemyAttackType.Rush)
            {
                combat.TickActiveAttack(
                    _core.LocomotionModule,
                    Mathf.Max(0f, p_deltaTime));
            }
        }

        // Animation View의 경과 초를 실행 중인 패턴의 복수 타이밍에 전달한다.
        public void NotifyAttackAnimationElapsed(
            float p_elapsedSeconds)
        {
            if (CurrentState != EEnemyCombatState.Attack ||
                _core?.CombatModule == null)
            {
                return;
            }

            _core.CombatModule.UpdateAttackAnimationTime(
                p_elapsedSeconds);
        }

        // Animation View가 실제 공격 상태의 마지막 프레임을 확인한 뒤 호출한다.
        public void NotifyAttackAnimationCompleted()
        {
            if (CurrentState != EEnemyCombatState.Attack ||
                _core?.CombatModule == null)
            {
                return;
            }

            _core.CombatModule.CompleteAttack(
                _core.LocomotionModule);
            ClearPreparedPattern();
            ChangeState(EEnemyCombatState.Wait);
        }

        private void TickWait(
            Transform p_target,
            float p_deltaTime)
        {
            _stateElapsedTime += Mathf.Max(0f, p_deltaTime);

            if (_stateElapsedTime < _nextAttackDelay)
                return;

            ClearPreparedPattern();

            if (!TryPreparePattern(
                    _core.CombatModule,
                    p_target))
            {
                ChangeState(EEnemyCombatState.Idle);
                return;
            }

            ChangeState(EEnemyCombatState.Prepare);
        }

        // 대기가 끝난 현재 타겟 거리에서 실행 가능한 패턴 하나를 예약한다.
        private bool TryPreparePattern(
            EnemyCombatModule p_combat,
            Transform p_target)
        {
            ClearPreparedPattern();

            if (!_patternSelector.TrySelectPattern(
                    p_combat,
                    p_target,
                    out int patternIndex))
            {
                return false;
            }

            _preparedPatternIndex = patternIndex;
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

        private void OnValidate()
        {
            _nextAttackDelay = Mathf.Max(0f, _nextAttackDelay);
        }
    }
}
