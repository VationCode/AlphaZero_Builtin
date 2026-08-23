using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy
{
    // Enemy 전체 행동의 우선순위를 관리하고 하위 Combat·Locomotion Flow를 허용·차단한다.
    [DisallowMultipleComponent]
    public sealed class EnemyActionFlow : MonoBehaviour
    {
        [Header("Hit Type Response")]
        [SerializeField]
        private HitTypeResponseSettings _hitTypeResponseSettings = new();

        [Tooltip("연사 공격이 Light 피격 애니메이션을 매 프레임 다시 시작하지 않게 하는 간격입니다.")]
        [SerializeField, Min(0f)]
        private float _lightHitRepeatInterval = 0.15f;

        [Header("Knockdown")]
        [Tooltip("Knockdown 자세로 전환한 뒤 Down 단계에 진입하기까지의 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _knockdownFallDuration = 1.1f;

        [Tooltip("Down 회복 후 일반 행동을 다시 허용하기까지의 기상 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _standupDuration = 0.95f;

        [Header("Death")]
        [Tooltip("사망 후 Enemy 오브젝트를 제거하기까지의 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _corpseRemovalDelay = 3f;

        [Header("Debug")]
        [SerializeField]
        private bool _logStateChanges = true;

        private readonly HitReactionFlow _hitReactionFlow = new();

        private EnemyCore _core;
        private bool _hasCurrentState;
        private bool _isDead;

        public EEnemyActionState CurrentState { get; private set; } =
            EEnemyActionState.Normal;

        public EHitReaction ActiveHitReaction =>
            _hitReactionFlow.CurrentReaction;

        public EHitReactionPhase HitReactionPhase =>
            _hitReactionFlow.CurrentPhase;

        public bool AllowsCombat =>
            !_isDead && CurrentState == EEnemyActionState.Normal;

        public bool AllowsLocomotion =>
            !_isDead && CurrentState == EEnemyActionState.Normal;

        public event Action<EEnemyActionState> OnStateChanged;
        public event Action<EHitReaction> OnHitReactionStarted;
        public event Action<EHitReactionPhase> OnHitReactionPhaseChanged;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _hitReactionFlow.Reset();
            _hasCurrentState = false;
            _isDead = false;

            ChangeState(EEnemyActionState.Normal);
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

            ImpactReactionResult reactionResult =
                ImpactReactionSystem.Resolve(
                    p_damageInfo,
                    _hitTypeResponseSettings);

            ApplyKnockback(p_damageInfo, reactionResult);
            TryEnterHitReaction(reactionResult);
            _core.TargetingFlow.TryPrioritizeTarget(
                p_damageInfo.Attacker);
        }

        // 공격 방향과 Enemy가 소유한 타입별 거리/시간을 실제 넉백 요청으로 조합한다.
        private void ApplyKnockback(
            in DamageInfo p_damageInfo,
            in ImpactReactionResult p_result)
        {
            if (!p_result.HasKnockback)
                return;

            KnockbackInfo knockbackInfo = new(
                p_damageInfo.Attacker,
                p_damageInfo.Direction,
                p_result.KnockbackDistance,
                p_result.KnockbackDuration);

            // Locomotion 하위의 IKnockbackable 구현을 정확한 Entity 범위에서 찾는다.
            KnockbackSystem.TryApply(
                _core.LocomotionModule,
                knockbackInfo);
        }

        // 공용 충격 판정 결과를 Enemy 행동 상태로 전환한다.
        private bool TryEnterHitReaction(
            in ImpactReactionResult p_result)
        {
            if (!_hitReactionFlow.TryBegin(
                    p_result,
                    Time.time,
                    _lightHitRepeatInterval,
                    _knockdownFallDuration,
                    _standupDuration))
            {
                return false;
            }

            EHitReaction reaction =
                _hitReactionFlow.CurrentReaction;

            _core.CombatFlow?.CancelCombat();
            _core.LocomotionFlow?.Stop();

            EEnemyActionState reactionState =
                reaction is EHitReaction.Knockdown or
                    EHitReaction.Launch
                    ? EEnemyActionState.Knockdown
                    : EEnemyActionState.HitReaction;

            ChangeState(reactionState);
            OnHitReactionStarted?.Invoke(reaction);
            OnHitReactionPhaseChanged?.Invoke(
                _hitReactionFlow.CurrentPhase);
            return true;
        }

        private bool TickHitReaction(float p_deltaTime)
        {
            bool isKnockbackActive =
                _core.LocomotionModule != null &&
                _core.LocomotionModule.IsKnockbackActive;

            EHitReactionPhase previousPhase =
                _hitReactionFlow.CurrentPhase;

            bool isReactionActive = _hitReactionFlow.Tick(
                p_deltaTime,
                isKnockbackActive);

            if (previousPhase != _hitReactionFlow.CurrentPhase)
            {
                OnHitReactionPhaseChanged?.Invoke(
                    _hitReactionFlow.CurrentPhase);
            }

            if (!isReactionActive &&
                (CurrentState is EEnemyActionState.HitReaction or
                    EEnemyActionState.Knockdown))
            {
                ChangeState(EEnemyActionState.Normal);
            }

            return isReactionActive;
        }

        // 사망한 Enemy가 제거 전까지 이동하거나 공격하지 않도록 모든 행동을 종료한다.
        internal void HandleDeath()
        {
            if (_isDead || _core == null)
                return;

            _isDead = true;
            _hitReactionFlow.Clear();
            _core.TargetingFlow.ClearTarget();
            _core.LocomotionFlow?.Stop();
            ChangeState(EEnemyActionState.Dead);

            Destroy(
                _core.gameObject,
                Mathf.Max(0f, _corpseRemovalDelay));
        }

        private void FixedUpdate()
        {
            if (_isDead || _core == null)
                return;

            float deltaTime = Time.fixedDeltaTime;

            // 넉백 물리는 Action 상태가 일반 이동을 막아도 계속 갱신한다.
            _core.LocomotionFlow?.TickKnockback(deltaTime);

            // 피격 반응 중에는 이동·공격 상태가 애니메이션을 덮어쓰지 않게 한다.
            if (TickHitReaction(deltaTime))
                return;

            ChangeState(EEnemyActionState.Normal);
            TickNormal(deltaTime);
        }

        // Normal 상태에서는 하위 Flow의 실행 순서만 조정한다.
        private void TickNormal(float p_deltaTime)
        {
            EnemyLocomotionFlow locomotionFlow =
                _core.LocomotionFlow;

            _core.TargetingFlow.Tick();

            Transform target = _core.Target;
            EnemyCombatFlow combatFlow = _core.CombatFlow;
            bool combatWantsControl =
                combatFlow?.WantsControl(target) == true;

            bool canExecuteCombat = locomotionFlow.Tick(
                target,
                combatWantsControl,
                p_deltaTime);

            if (!combatWantsControl || !canExecuteCombat)
            {
                combatFlow?.CancelCombat();
                return;
            }

            combatFlow.Tick(target, p_deltaTime);
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
                string previousName =  hadCurrentState? previousState.ToString() : "None";

                string ownerName = _core != null? _core.name : name;

                //Debug.Log($"[{ownerName}] Enemy Action: " + $"{previousName} -> {CurrentState}", this);
            }

            OnStateChanged?.Invoke(CurrentState);
        }

        private void OnDisable()
        {
            if (_core == null)
                return;

            _core.TargetingFlow.Reset();
            _core.LocomotionFlow?.Reset();
            _hitReactionFlow.Clear();
            _hasCurrentState = false;
        }

        private void OnEnable()
        {
            if (_core != null && !_isDead)
                ChangeState(EEnemyActionState.Normal);
        }

        private void OnValidate()
        {
            _hitTypeResponseSettings ??= new HitTypeResponseSettings();
            _lightHitRepeatInterval =
                Mathf.Max(0f, _lightHitRepeatInterval);
            _knockdownFallDuration =
                Mathf.Max(0f, _knockdownFallDuration);
            _standupDuration =
                Mathf.Max(0f, _standupDuration);
            _corpseRemovalDelay =
                Mathf.Max(0f, _corpseRemovalDelay);
        }
    }
}
