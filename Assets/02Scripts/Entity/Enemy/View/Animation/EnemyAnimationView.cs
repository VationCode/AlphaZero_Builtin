using System;
using System.Collections.Generic;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy.Animation
{
    // 공격 타입과 AnimationIndex에 대응하는 Animator 상태 경로를 보관한다.
    [Serializable]
    public sealed class EnemyAttackAnimationBinding
    {
        [SerializeField]
        private EEnemyAttackType _attackType;

        [Tooltip("-1은 해당 공격 타입의 기본 설정입니다.")]
        [SerializeField, Min(-1)]
        private int _animationIndex = -1;

        [Tooltip("비워 두면 공격 대기 중 현재 애니메이션을 유지합니다.")]
        [SerializeField]
        private string _waitStatePath;

        [Tooltip("공격 시작 시 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _attackStatePath;

        [SerializeField, Min(0f)]
        private float _transitionDuration = 0.05f;

        public EEnemyAttackType AttackType => _attackType;
        public int AnimationIndex => _animationIndex;
        public string WaitStatePath => _waitStatePath;
        public string AttackStatePath => _attackStatePath;
        public float TransitionDuration => _transitionDuration;

        public EnemyAttackAnimationBinding()
        {
        }

        public EnemyAttackAnimationBinding(
            EEnemyAttackType p_attackType,
            int p_animationIndex,
            string p_waitStatePath,
            string p_attackStatePath)
        {
            _attackType = p_attackType;
            _animationIndex = Mathf.Max(-1, p_animationIndex);
            _waitStatePath = p_waitStatePath;
            _attackStatePath = p_attackStatePath;
        }

        public void Validate()
        {
            _animationIndex = Mathf.Max(-1, _animationIndex);
            _waitStatePath ??= string.Empty;
            _attackStatePath ??= string.Empty;
            _transitionDuration = Mathf.Max(0f, _transitionDuration);
        }
    }

    // Enemy의 피격·사망 등 Animator 표현을 담당한다.
    public class EnemyAnimationView : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        [Header("Locomotion States")]
        [Tooltip("Idle 상태에서 재생할 Base Layer 상태 경로입니다. 비워 두면 현재 애니메이션을 유지합니다.")]
        [SerializeField]
        private string _idleStatePath;

        [Tooltip("Patrol과 ReturnToArea 상태에서 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _patrolStatePath = "Base Layer.Patrol";

        [Tooltip("Chase 상태에서 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _chaseStatePath = "Base Layer.Chase";

        [Tooltip("추적 범위를 벗어나 시작 위치로 복귀할 때 재생할 Base Layer 상태 경로입니다.")]
        [SerializeField]
        private string _returnStatePath = "Base Layer.Patrol";

        [Header("Attack States")]
        [Tooltip(
            "AttackType과 AnimationIndex 조합에 대응하는 상태 경로입니다. " +
            "AnimationIndex -1은 해당 타입의 기본 설정입니다.")]
        [SerializeField]
        private List<EnemyAttackAnimationBinding> _attackAnimations = new()
        {
            new EnemyAttackAnimationBinding(
                EEnemyAttackType.Melee,
                -1,
                "Base Layer.MeleeWait",
                "Base Layer.MeleeAttack"),
            new EnemyAttackAnimationBinding(
                EEnemyAttackType.Range,
                -1,
                "Base Layer.Aiming",
                "Base Layer.RangeAttack"),
            new EnemyAttackAnimationBinding(
                EEnemyAttackType.Rush,
                -1,
                "Base Layer.RushWait",
                "Base Layer.RushAttack")
        };

        [Header("Action States")]
        [SerializeField]
        private string _lightHitStatePath = "Base Layer.LightHit";

        [SerializeField]
        private string _heavyHitStatePath = "Base Layer.HeavyHit";

        [SerializeField]
        private string _knockdownStatePath = "Base Layer.Knockdown";

        [SerializeField]
        private string _lyingDownStatePath = "Base Layer.Lying down";

        [SerializeField]
        private string _standUpStatePath = "Base Layer.StandUp";

        [SerializeField]
        private string _deathStatePath = "Base Layer.Death";

        [Header("Debug")]
        [SerializeField]
        private bool _logCrossFadeRequests = true;

        private const int BaseLayer = 0;

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

        // 공격 타입과 인덱스에 연결된 쿨타임 대기 상태를 재생한다.
        public bool PlayAttackWait(
            EEnemyAttackType p_attackType,
            int p_animationIndex)
        {
            if (!TryFindAttackAnimation(
                    p_attackType,
                    p_animationIndex,
                    out EnemyAttackAnimationBinding binding))
                return false;

            // 전용 Wait 상태가 없으면 현재 표현을 유지하고 실제 공격 시작을 기다린다.
            return string.IsNullOrWhiteSpace(binding.WaitStatePath) ||
                   CrossFadeBase(binding.WaitStatePath);
        }

        // 쿨타임이 끝난 공격 타입과 인덱스의 실행 상태를 처음부터 재생한다.
        public bool PlayAttack(
            EEnemyAttackType p_attackType,
            int p_animationIndex)
        {
            if (!TryFindAttackAnimation(
                    p_attackType,
                    p_animationIndex,
                    out EnemyAttackAnimationBinding binding))
                return false;

            if (string.IsNullOrWhiteSpace(binding.AttackStatePath))
            {
                Debug.LogError(
                    $"[{name}] 공격 Animation StatePath가 비어 있습니다: " +
                    $"Type={p_attackType}, Index={p_animationIndex}",
                    this);
                return false;
            }

            return CrossFadeBase(
                binding.AttackStatePath,
                binding.TransitionDuration,
                true);
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
                    CrossFadeBase(_lightHitStatePath, 0.05f, true),
                EHitReactionState.HeavyHit =>
                    CrossFadeBase(_heavyHitStatePath, 0.05f, true),
                EHitReactionState.Knockdown =>
                    CrossFadeBase(_knockdownStatePath, 0.05f, true),
                EHitReactionState.LyingDown =>
                    CrossFadeBase(_lyingDownStatePath, 0.05f),
                EHitReactionState.StandUp =>
                    CrossFadeBase(_standUpStatePath, 0.05f),
                _ => false
            };
        }

        public bool PlayDeath()
        {
            return CrossFadeBase(_deathStatePath, 0.05f);
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

                case EEnemyLocomotionState.ReturnToArea:
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
            EEnemyAttackType p_attackType,
            int p_animationIndex)
        {
            PlayAttackWait(
                p_attackType,
                p_animationIndex);
        }

        private void HandleAttackStarted(
            EEnemyAttackType p_attackType,
            int p_animationIndex)
        {
            PlayAttack(
                p_attackType,
                p_animationIndex);
        }

        // 정확한 인덱스를 우선 사용하고 없으면 타입별 기본값(-1)을 사용한다.
        private bool TryFindAttackAnimation(
            EEnemyAttackType p_attackType,
            int p_animationIndex,
            out EnemyAttackAnimationBinding p_binding)
        {
            p_binding = null;
            EnemyAttackAnimationBinding fallback = null;

            if (_attackAnimations != null)
            {
                foreach (EnemyAttackAnimationBinding binding in
                         _attackAnimations)
                {
                    if (binding == null ||
                        binding.AttackType != p_attackType)
                    {
                        continue;
                    }

                    if (binding.AnimationIndex == p_animationIndex)
                    {
                        p_binding = binding;
                        return true;
                    }

                    if (binding.AnimationIndex == -1)
                        fallback ??= binding;
                }
            }

            p_binding = fallback;

            if (p_binding != null)
                return true;

            Debug.LogError(
                $"[{name}] 공격 Animation 연결 설정이 없습니다: " +
                $"Type={p_attackType}, Index={p_animationIndex}",
                this);
            return false;
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
                Debug.LogError($"[{name}] Enemy Animation 상태가 없습니다: {p_statePath}", this);
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

        private void OnValidate()
        {
            if (_attackAnimations == null)
                return;

            foreach (EnemyAttackAnimationBinding binding in
                     _attackAnimations)
            {
                binding?.Validate();
            }
        }

    }
}
