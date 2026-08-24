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

        [Header("Debug")]
        [SerializeField]
        private bool _logCrossFadeRequests = true;

        private const int BaseLayer = 0;

        private const string PatrolStatePath = "Base Layer.Patrol";
        private const string ChaseStatePath = "Base Layer.Chase";
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

        private static readonly int PatrolState =
            Animator.StringToHash(PatrolStatePath);

        private static readonly int ChaseState =
            Animator.StringToHash(ChaseStatePath);

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
        private EnemyActionFlow _actionFlow;
        private EnemyLocomotionFlow _locomotionFlow;
        private EnemyCombatFlow _combatFlow;
        private bool _isActionSubscribed;
        private bool _isLocomotionSubscribed;
        private bool _isCombatSubscribed;

        public event Action OnDeathAnimationCompleted;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
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
            if (_animator == null)
                return false;

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
            if (_animator == null)
                return false;

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
            if (_animator == null)
                return false;

            return CrossFadeBase(PatrolState, PatrolStatePath);
        }

        // 추적 이동 상태를 직접 재생한다.
        public bool PlayChase()
        {
            if (_animator == null)
                return false;

            return CrossFadeBase(ChaseState, ChaseStatePath);
        }

        // 공용 피격 행동 상태를 Enemy Animator 상태에 직접 연결한다.
        public bool PlayHitReaction(EHitReactionState p_state)
        {
            if (_animator == null)
                return false;

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
            if (_animator == null)
                return false;

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
                case EEnemyLocomotionState.Patrol:
                case EEnemyLocomotionState.ReturnToPatrol:
                    PlayPatrol();
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
        private bool CrossFadeBase(int p_stateHash, string p_statePath, float p_transitionDuration = 0.15f, bool p_restart = false)
        {
            if (_animator == null)
            {
                Debug.LogError($"[{name}] Enemy Animator가 없습니다. " + $"요청 상태: {p_statePath}", this);
                return false;
            }

            RuntimeAnimatorController controller =
                _animator.runtimeAnimatorController;
            bool hasLayer = _animator.layerCount > BaseLayer;
            bool hasState = hasLayer &&
                            _animator.HasState(
                                BaseLayer,
                                p_stateHash);
            int currentStateHash = hasLayer
                ? _animator
                    .GetCurrentAnimatorStateInfo(BaseLayer)
                    .fullPathHash
                : 0;

            /*if (_logCrossFadeRequests)
            {
                Debug.Log(
                    $"[{name}] Enemy Animation 요청: {p_statePath} | " +
                    $"Animator={_animator.name}, " +
                    $"Controller={(controller != null ? controller.name : "None")}, " +
                    $"Initialized={_animator.isInitialized}, " +
                    $"Active={_animator.gameObject.activeInHierarchy}, " +
                    $"Enabled={_animator.enabled}, " +
                    $"HasState={hasState}, " +
                    $"CurrentHash={currentStateHash}, " +
                    $"TargetHash={p_stateHash}",
                    this);
            }*/

            if (controller == null ||
                !_animator.isInitialized ||
                !hasState)
            {
                Debug.LogError(
                    $"[{name}] Enemy Animation 전환 실패: {p_statePath}",
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
        }

    }
}
