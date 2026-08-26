using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy.Animation
{
    // Enemy의 피격·사망 등 Animator 표현을 담당한다.
    public class EnemyAnimationView : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        [Header("Locomotion States")]
        [Tooltip("Idle 상태에서 재생할 Base Layer 상태 경로입니다. 비워 두면 현재 애니메이션을 유지합니다.")]
        [SerializeField]
        private string _idleStatePath;

        [Tooltip("Patrol과 ReturnToPatrol 상태에서 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _patrolStatePath = "Base Layer.Patrol";

        [Tooltip("Chase 상태에서 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _chaseStatePath = "Base Layer.Chase";

        [Tooltip("추적 범위를 벗어나 시작 위치로 복귀할 때 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _returnStatePath = "Base Layer.Patrol";

        [Header("Debug")]
        [SerializeField]
        private bool _logCrossFadeRequests = true;

        private const int BaseLayer = 0;

        private const string RangeWaitStatePath = "Base Layer.Aiming";
        private const string RangeAttackStatePath = "Base Layer.RangeAttack";
        private const string MeleeWaitStatePath = "Base Layer.MeleeWait";
        private const string MeleeAttackStatePath = "Base Layer.MeleeAttack";
        private const string RushWaitStatePath = "Base Layer.RushWait";
        private const string RushAttackStatePath = "Base Layer.RushAttack";
        private const string LightHitStatePath = "Base Layer.LightHit";
        private const string HeavyHitStatePath = "Base Layer.HeavyHit";
        private const string KnockdownStatePath = "Base Layer.Knockdown";
        private const string LyingDownStatePath = "Base Layer.Lying down";
        private const string StandUpStatePath = "Base Layer.StandUp";
        private const string DeadStatePath = "Base Layer.Death";

        private static readonly int RangeWaitState =
            Animator.StringToHash(RangeWaitStatePath);

        private static readonly int RangeAttackState =
            Animator.StringToHash(RangeAttackStatePath);

        private static readonly int MeleeWaitState =
            Animator.StringToHash(MeleeWaitStatePath);

        private static readonly int MeleeAttackState =
            Animator.StringToHash(MeleeAttackStatePath);

        private static readonly int RushWaitState =
            Animator.StringToHash(RushWaitStatePath);

        private static readonly int RushAttackState =
            Animator.StringToHash(RushAttackStatePath);

        private static readonly int LightHitState =
            Animator.StringToHash(LightHitStatePath);

        private static readonly int HeavyHitState =
            Animator.StringToHash(HeavyHitStatePath);

        private static readonly int KnockdownState =
            Animator.StringToHash(KnockdownStatePath);

        private static readonly int LyingDownState =
            Animator.StringToHash(LyingDownStatePath);

        private static readonly int StandUpState =
            Animator.StringToHash(StandUpStatePath);

        private static readonly int DeadState =
            Animator.StringToHash(DeadStatePath);

        private int _currentBaseState;
        private bool _hasCurrentBaseState;
        private int _pendingBaseState;
        private string _pendingBaseStatePath;
        private float _pendingTransitionDuration;
        private bool _pendingRestart;
        private bool _hasPendingBaseState;
        private EnemyActionFlow _actionFlow;
        private EnemyLocomotionFlow _locomotionFlow;
        private EnemyCombatFlow _combatFlow;
        private bool _isActionSubscribed;
        private bool _isLocomotionSubscribed;
        private bool _isCombatSubscribed;

        public event Action OnDeathAnimationCompleted;

        private void Awake()
        {
            ResolveAnimator();
        }

        // Entity Flow의 상태 이벤트를 Animation 표현에 연결한다.
        public void Bind(
            EnemyActionFlow p_actionFlow,
            EnemyLocomotionFlow p_locomotionFlow,
            EnemyCombatFlow p_combatFlow)
        {
            UnsubscribeFromAction();
            UnsubscribeFromLocomotion();
            UnsubscribeFromCombat();

            _actionFlow = p_actionFlow;
            _locomotionFlow = p_locomotionFlow;
            _combatFlow = p_combatFlow;

            SubscribeToAction();
            SubscribeToLocomotion();
            SubscribeToCombat();
        }

        public void Unbind()
        {
            UnsubscribeFromAction();
            UnsubscribeFromLocomotion();
            UnsubscribeFromCombat();

            _actionFlow = null;
            _locomotionFlow = null;
            _combatFlow = null;
        }

        // 공격 타입에 맞는 쿨타임 대기 상태를 재생한다.
        public bool PlayAttackWait(EEnemyAttackType p_attackType)
        {
            return p_attackType switch
            {
                EEnemyAttackType.Melee => CrossFadeBase(MeleeWaitState, MeleeWaitStatePath),
                EEnemyAttackType.Range => CrossFadeBase(RangeWaitState, RangeWaitStatePath),
                EEnemyAttackType.Rush => CrossFadeBase(RushWaitState, RushWaitStatePath),
                _ => false
            };
        }

        // 쿨타임이 끝난 공격 타입의 실행 상태를 직접 재생한다.
        public bool PlayAttack(EEnemyAttackType p_attackType)
        {
            return p_attackType switch
            {
                EEnemyAttackType.Melee => CrossFadeBase(MeleeAttackState, MeleeAttackStatePath,
                    0.05f),
                EEnemyAttackType.Range => CrossFadeBase(RangeAttackState, RangeAttackStatePath,
                    0.05f),
                EEnemyAttackType.Rush => CrossFadeBase(RushAttackState, RushAttackStatePath, 0.05f),
                _ => false
            };
        }

        // 순찰 이동 상태를 직접 재생한다.
        public bool PlayPatrol()
        {
            return CrossFadeBase(_patrolStatePath);
        }

        // 추적 이동 상태를 직접 재생한다.
        public bool PlayChase()
        {
            return CrossFadeBase(_chaseStatePath);
        }

        // 추적 경계 복귀는 Patrol 여부와 독립적인 이동 표현을 사용한다.
        public bool PlayReturn()
        {
            return CrossFadeBase(_returnStatePath);
        }

        // 이동을 멈춘 상태에 전용 경로가 있을 때만 Idle을 재생한다.
        public bool PlayIdle()
        {
            return CrossFadeBase(_idleStatePath);
        }

        // Boss Intro처럼 외부 연출이 지정한 Base Layer 상태를 처음부터 재생한다.
        public bool PlayCinematic(string p_statePath)
        {
            return CrossFadeBase(
                p_statePath,
                0f,
                true);
        }

        // 공용 피격 행동 상태를 Enemy Animator 상태에 직접 연결한다.
        public bool PlayHitReaction(EHitReactionState p_state)
        {
            return p_state switch
            {
                EHitReactionState.LightHit =>
                    CrossFadeBase(LightHitState, LightHitStatePath, 0.05f, true),
                EHitReactionState.HeavyHit =>
                    CrossFadeBase(HeavyHitState, HeavyHitStatePath, 0.05f, true),
                EHitReactionState.Knockdown =>
                    CrossFadeBase(KnockdownState, KnockdownStatePath, 0.05f, true),
                EHitReactionState.LyingDown =>
                    CrossFadeBase(LyingDownState, LyingDownStatePath, 0.05f),
                EHitReactionState.StandUp =>
                    CrossFadeBase(StandUpState, StandUpStatePath, 0.05f),
                _ => false
            };
        }

        public bool PlayDeath()
        {
            return CrossFadeBase(DeadState, DeadStatePath, 0.05f);
        }

        private void SubscribeToAction()
        {
            if (_isActionSubscribed ||
                !isActiveAndEnabled ||
                _actionFlow == null)
            {
                return;
            }

            _actionFlow.OnStateChanged +=
                HandleActionStateChanged;
            _actionFlow.OnHitReactionStateChanged +=
                HandleHitReactionStateChanged;
            _isActionSubscribed = true;

            SynchronizeActionState();
        }

        private void UnsubscribeFromAction()
        {
            if (!_isActionSubscribed)
                return;

            if (_actionFlow != null)
            {
                _actionFlow.OnStateChanged -=
                    HandleActionStateChanged;
                _actionFlow.OnHitReactionStateChanged -=
                    HandleHitReactionStateChanged;
            }

            _isActionSubscribed = false;
        }

        private void SynchronizeActionState()
        {
            HandleActionStateChanged(_actionFlow.CurrentState);

            if (_actionFlow.CurrentState ==
                    EEnemyActionState.HitReaction &&
                _actionFlow.HitReactionState !=
                    EHitReactionState.None)
            {
                HandleHitReactionStateChanged(
                    _actionFlow.HitReactionState);
            }
        }

        private void HandleActionStateChanged(
            EEnemyActionState p_state)
        {
            if (p_state == EEnemyActionState.Dead)
                PlayDeath();
        }

        private void HandleHitReactionStateChanged(
            EHitReactionState p_state)
        {
            PlayHitReaction(p_state);
        }

        private void SubscribeToLocomotion()
        {
            if (_isLocomotionSubscribed ||
                !isActiveAndEnabled ||
                _locomotionFlow == null)
            {
                return;
            }

            _locomotionFlow.OnStateChanged +=
                HandleLocomotionStateChanged;
            _isLocomotionSubscribed = true;

            // View가 다시 활성화된 경우 현재 이동 상태를 즉시 복원한다.
            HandleLocomotionStateChanged(
                _locomotionFlow.CurrentState);
        }

        private void UnsubscribeFromLocomotion()
        {
            if (!_isLocomotionSubscribed)
                return;

            if (_locomotionFlow != null)
            {
                _locomotionFlow.OnStateChanged -=
                    HandleLocomotionStateChanged;
            }

            _isLocomotionSubscribed = false;
        }

        private void HandleLocomotionStateChanged(
            EEnemyLocomotionState p_state)
        {
            switch (p_state)
            {
                case EEnemyLocomotionState.Idle:
                    PlayIdle();
                    break;

                case EEnemyLocomotionState.Patrol:
                    PlayPatrol();
                    break;

                case EEnemyLocomotionState.ReturnToPatrol:
                    PlayReturn();
                    break;

                case EEnemyLocomotionState.Chase:
                    PlayChase();
                    break;
            }
        }

        private void SubscribeToCombat()
        {
            if (_isCombatSubscribed ||
                !isActiveAndEnabled ||
                _combatFlow == null)
            {
                return;
            }

            _combatFlow.OnAttackWaitStarted +=
                HandleAttackWaitStarted;
            _combatFlow.OnAttackStarted +=
                HandleAttackStarted;
            _isCombatSubscribed = true;
        }

        private void UnsubscribeFromCombat()
        {
            if (!_isCombatSubscribed)
                return;

            if (_combatFlow != null)
            {
                _combatFlow.OnAttackWaitStarted -=
                    HandleAttackWaitStarted;
                _combatFlow.OnAttackStarted -=
                    HandleAttackStarted;
            }

            _isCombatSubscribed = false;
        }

        private void HandleAttackWaitStarted(
            EEnemyAttackType p_attackType)
        {
            PlayAttackWait(p_attackType);
        }

        private void HandleAttackStarted(
            EEnemyAttackType p_attackType)
        {
            PlayAttack(p_attackType);
        }

        // Death Animation Clip 마지막 프레임의 Animation Event에서 호출한다.
        public void NotifyDeathAnimationCompleted()
        {
            OnDeathAnimationCompleted?.Invoke();
        }

        // Base Layer 상태를 전환하고 같은 상태의 중복 재생을 막는다.
        private bool CrossFadeBase(
            string p_statePath,
            float p_transitionDuration = 0.15f,
            bool p_restart = false)
        {
            if (string.IsNullOrWhiteSpace(p_statePath))
                return false;

            return CrossFadeBase(
                Animator.StringToHash(p_statePath),
                p_statePath,
                p_transitionDuration,
                p_restart);
        }

        private bool CrossFadeBase(
            int p_stateHash,
            string p_statePath,
            float p_transitionDuration = 0.15f,
            bool p_restart = false)
        {
            if (!ResolveAnimator())
            {
                Debug.LogError(
                    $"[{name}] Enemy Animator가 없습니다. " +
                    $"요청 상태: {p_statePath}",
                    this);
                return false;
            }

            RuntimeAnimatorController controller =
                _animator.runtimeAnimatorController;

            if (controller == null)
            {
                Debug.LogError(
                    $"[{name}] Enemy Animation 전환 실패: {p_statePath}",
                    this);
                return false;
            }

            // Awake 직후 Animator가 아직 준비되지 않았다면 최신 요청을 보관한다.
            if (!_animator.isInitialized)
            {
                QueueBaseState(
                    p_stateHash,
                    p_statePath,
                    p_transitionDuration,
                    p_restart);
                return false;
            }

            bool hasLayer = _animator.layerCount > BaseLayer;

            if (!hasLayer)
            {
                _hasPendingBaseState = false;
                Debug.LogError(
                    $"[{name}] Enemy Animator Base Layer가 없습니다.",
                    this);
                return false;
            }

            _hasPendingBaseState = false;

            bool hasState = _animator.HasState(
                BaseLayer,
                p_stateHash);

            if (_logCrossFadeRequests)
            {
                //Debug.Log($"[{name}] Enemy Animation 요청: {p_statePath} | " + $"Controller={controller.name}, HasState={hasState}", this);
            }

            if (!hasState)
            {
                Debug.LogError(
                    $"[{name}] Enemy Animation 상태가 없습니다: {p_statePath}",
                    this);
                return false;
            }

            if (!p_restart &&
                _hasCurrentBaseState &&
                _currentBaseState == p_stateHash)
            {
                return true;
            }

            _animator.CrossFadeInFixedTime(
                p_stateHash,
                p_transitionDuration,
                BaseLayer,
                0f);

            // 유효한 Animator 상태에 전환을 요청한 뒤에만 캐시한다.
            _currentBaseState = p_stateHash;
            _hasCurrentBaseState = true;

            return true;
        }

        private bool ResolveAnimator()
        {
            _animator = GetComponent<Animator>();
            return _animator != null;
        }

        private void QueueBaseState(
            int p_stateHash,
            string p_statePath,
            float p_transitionDuration,
            bool p_restart)
        {
            _pendingBaseState = p_stateHash;
            _pendingBaseStatePath = p_statePath;
            _pendingTransitionDuration = p_transitionDuration;
            _pendingRestart = p_restart;
            _hasPendingBaseState = true;
        }

        private void LateUpdate()
        {
            if (!_hasPendingBaseState)
                return;

            CrossFadeBase(
                _pendingBaseState,
                _pendingBaseStatePath,
                _pendingTransitionDuration,
                _pendingRestart);
        }

        private void OnEnable()
        {
            SubscribeToAction();
            SubscribeToLocomotion();
            SubscribeToCombat();
        }

        private void OnDisable()
        {
            UnsubscribeFromAction();
            UnsubscribeFromLocomotion();
            UnsubscribeFromCombat();

            // 다시 활성화될 때 현재 Flow 상태를 Animator에 확실히 반영한다.
            _currentBaseState = 0;
            _hasCurrentBaseState = false;
            _hasPendingBaseState = false;
        }

    }
}
