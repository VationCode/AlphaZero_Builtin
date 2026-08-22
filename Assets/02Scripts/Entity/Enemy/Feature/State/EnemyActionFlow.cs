using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy
{
    // 감지 대상과 공격 거리, 순찰 영역을 기준으로 Enemy 행동을 조정한다.
    [DisallowMultipleComponent]
    public sealed class EnemyActionFlow : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        private float _targetScanInterval = 0.2f;

        [Header("Hit Reaction")]
        [SerializeField, Min(0f)]
        private float _lightHitRecoveryDuration = 0.2f;

        [SerializeField, Min(0f)]
        private float _heavyHitRecoveryDuration = 0.45f;

        [Tooltip("물리 넉백이 끝난 뒤에도 Enemy가 쓰러진 상태로 대기할 추가 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _knockdownRecoveryDuration = 0.8f;

        [Tooltip("연사 공격이 Light 피격 애니메이션을 매 프레임 다시 시작하지 않게 하는 간격입니다.")]
        [SerializeField, Min(0f)]
        private float _lightHitRepeatInterval = 0.15f;

        [Header("Death")]
        [Tooltip("사망 후 Enemy 오브젝트를 제거하기까지의 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _corpseRemovalDelay = 3f;

        [Header("Debug")]
        [SerializeField]
        private bool _logStateChanges = true;

        private EnemyCore _core;
        private float _nextTargetScanTime;
        private float _hitReactionRemainingTime;
        private float _nextLightHitTime;
        private bool _hasCurrentState;
        private bool _isDead;
        private EHitReaction _activeHitReaction = EHitReaction.None;

        public EEnemyActionState CurrentState { get; private set; } =
            EEnemyActionState.Patrol;

        public event Action<EEnemyActionState> OnStateChanged;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _nextTargetScanTime = Time.time;
            _nextLightHitTime = float.NegativeInfinity;
            _hitReactionRemainingTime = 0f;
            _activeHitReaction = EHitReaction.None;
            _hasCurrentState = false;
            _isDead = false;

            ChangeState(EEnemyActionState.Patrol);
        }

        // 추적 경계 안에서 공격한 대상을 우선 타깃으로 전환한다.
        internal void HandleDamaged(DamageInfo p_damageInfo)
        {
            if (_isDead ||
                _core == null ||
                !p_damageInfo.IsValid ||
                _core.HealthModule == null ||
                _core.HealthModule.CurrentHealth <= 0f ||
                _core.HealthModule.IsDead)
            {
                return;
            }

            TryEnterHitReaction(p_damageInfo.HitReaction);

            Transform attacker = p_damageInfo.Attacker;
            EnemyDetectionModule targetModule = _core.TargetModule;
            EnemyLocomotionModule locomotion = _core.LocomotionModule;

            if (targetModule == null ||
                locomotion == null ||
                !targetModule.IsValidTarget(attacker) ||
                locomotion.IsOutsideChaseBoundary(attacker.position))
            {
                return;
            }

            if (_core.Target != attacker)
            {
                _core.AttackFlow?.CancelAttack();
                _core.SetTarget(attacker);
            }

            _nextTargetScanTime =
                Time.time + Mathf.Max(0.05f, _targetScanInterval);

            // 피격 행동이 끝난 뒤 현재 타깃을 기준으로 전투 상태를 다시 선택한다.
            if (_activeHitReaction == EHitReaction.None)
                ChangeState(SelectState());
        }

        private bool TryEnterHitReaction(EHitReaction p_hitReaction)
        {
            if (p_hitReaction == EHitReaction.None ||
                GetHitReactionPriority(p_hitReaction) <
                GetHitReactionPriority(_activeHitReaction))
            {
                return false;
            }

            if (p_hitReaction == EHitReaction.Light &&
                Time.time < _nextLightHitTime)
            {
                return false;
            }

            float recoveryDuration =
                GetHitReactionDuration(p_hitReaction);

            if (recoveryDuration <= 0f)
                return false;

            if (p_hitReaction == EHitReaction.Light)
            {
                _nextLightHitTime =
                    Time.time + _lightHitRepeatInterval;
            }

            _activeHitReaction = p_hitReaction;
            _hitReactionRemainingTime = recoveryDuration;

            _core.AttackFlow?.CancelAttack();
            _core.LocomotionModule?.Stop();

            ChangeState(EEnemyActionState.HitReaction);
            _core.AnimationView?.PlayHit(p_hitReaction);
            return true;
        }

        private bool TickHitReaction(float p_deltaTime)
        {
            if (_activeHitReaction == EHitReaction.None)
                return false;

            bool waitsForKnockback =
                (_activeHitReaction == EHitReaction.Knockdown ||
                 _activeHitReaction == EHitReaction.Launch) &&
                _core.LocomotionModule != null &&
                _core.LocomotionModule.IsKnockbackActive;

            // Knockdown 회복 시간은 물리적인 밀림이 완전히 끝난 뒤부터 계산한다.
            if (waitsForKnockback)
                return true;

            _hitReactionRemainingTime = Mathf.Max(
                0f,
                _hitReactionRemainingTime - p_deltaTime);

            if (_hitReactionRemainingTime > 0f)
                return true;

            _activeHitReaction = EHitReaction.None;
            ChangeState(SelectState());
            return false;
        }

        private float GetHitReactionDuration(EHitReaction p_hitReaction)
        {
            return p_hitReaction switch
            {
                EHitReaction.Light => _lightHitRecoveryDuration,
                EHitReaction.Heavy => _heavyHitRecoveryDuration,
                EHitReaction.Knockdown => _knockdownRecoveryDuration,
                EHitReaction.Launch => _knockdownRecoveryDuration,
                _ => 0f
            };
        }

        private static int GetHitReactionPriority(
            EHitReaction p_hitReaction)
        {
            return p_hitReaction switch
            {
                EHitReaction.Light => 1,
                EHitReaction.Heavy => 2,
                EHitReaction.Knockdown => 3,
                EHitReaction.Launch => 4,
                _ => 0
            };
        }

        // 사망한 Enemy가 제거 전까지 이동하거나 공격하지 않도록 모든 행동을 종료한다.
        internal void HandleDeath()
        {
            if (_isDead || _core == null)
                return;

            _isDead = true;
            _activeHitReaction = EHitReaction.None;
            _hitReactionRemainingTime = 0f;
            _core.AttackFlow?.CancelAttack();
            _core.ClearTarget();
            _core.LocomotionModule?.Stop();
            _core.LocomotionModule?.SetPatrolEnabled(false);
            _core.AnimationView?.PlayDeath();
            _hasCurrentState = false;

            Destroy(
                _core.gameObject,
                Mathf.Max(0f, _corpseRemovalDelay));
        }

        private void FixedUpdate()
        {
            if (_isDead || _core == null)
                return;

            // 피격 반응 중에는 이동·공격 상태가 애니메이션을 덮어쓰지 않게 한다.
            if (TickHitReaction(Time.fixedDeltaTime))
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
            EnemyDetectionModule targetModule = _core.TargetModule;
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

            if (_logStateChanges)
            {
                string previousName = hadCurrentState
                    ? previousState.ToString()
                    : "None";
                string ownerName = _core != null
                    ? _core.name
                    : name;

                Debug.Log(
                    $"[{ownerName}] Enemy Action: " +
                    $"{previousName} -> {CurrentState}",
                    this);
            }

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

            PlayActionAnimation(CurrentState);
            OnStateChanged?.Invoke(CurrentState);
        }

        // 행동 상태에 대응하는 이동 애니메이션을 View에 요청한다.
        private void PlayActionAnimation(EEnemyActionState p_state)
        {
            if (_core?.AnimationView == null)
                return;

            switch (p_state)
            {
                case EEnemyActionState.Patrol:
                case EEnemyActionState.ReturnToPatrol:
                    _core.AnimationView.PlayPatrol();
                    break;

                case EEnemyActionState.Chase:
                    _core.AnimationView.PlayChase();
                    break;
            }
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
            _core.LocomotionModule?.SetPatrolEnabled(!_isDead);
            _activeHitReaction = EHitReaction.None;
            _hitReactionRemainingTime = 0f;
            _hasCurrentState = false;
        }

        private void OnValidate()
        {
            _targetScanInterval =
                Mathf.Max(0.05f, _targetScanInterval);
            _lightHitRecoveryDuration =
                Mathf.Max(0f, _lightHitRecoveryDuration);
            _heavyHitRecoveryDuration =
                Mathf.Max(0f, _heavyHitRecoveryDuration);
            _knockdownRecoveryDuration =
                Mathf.Max(0f, _knockdownRecoveryDuration);
            _lightHitRepeatInterval =
                Mathf.Max(0f, _lightHitRepeatInterval);
            _corpseRemovalDelay =
                Mathf.Max(0f, _corpseRemovalDelay);
        }
    }
}
