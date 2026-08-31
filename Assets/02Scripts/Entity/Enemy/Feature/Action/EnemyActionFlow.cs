using System;
using System.Collections.Generic;
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

        [Header("Hit Reaction Immunity")]
        [SerializeField]
        private HitReactionImmunitySettings _hitReactionImmunitySettings =
            new();

        [Header("Knockdown")]
        [Tooltip("Knockdown에서 LyingDown으로 전환하기까지의 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _knockdownFallDuration = 1.1f;

        [Tooltip("StandUp 상태가 끝날 때까지 행동을 잠그는 시간입니다.")]
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
        private readonly HashSet<object> _externalActionBlockers = new();

        private EnemyCore _core;
        private bool _hasCurrentState;
        private bool _isDead;

        public EEnemyActionState CurrentState { get; private set; } =
            EEnemyActionState.Normal;

        public EHitReactionState HitReactionState =>
            _hitReactionFlow.CurrentState;

        public bool AllowsCombat =>
            !_isDead &&
            !IsExternallyBlocked &&
            CurrentState == EEnemyActionState.Normal;

        public bool AllowsLocomotion =>
            !_isDead &&
            !IsExternallyBlocked &&
            CurrentState == EEnemyActionState.Normal;

        public bool IsExternallyBlocked =>
            _externalActionBlockers.Count > 0;

        public event Action<EEnemyActionState> OnStateChanged;
        public event Action<EHitReactionState> OnHitReactionStateChanged;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _hitReactionFlow.Reset();
            _externalActionBlockers.Clear();
            _hasCurrentState = false;
            _isDead = false;

            ChangeState(EEnemyActionState.Normal);
        }

        // Boss Intro 등 외부 흐름이 Enemy의 AI·이동·공격을 중첩 차단한다.
        public bool BeginExternalBlock(object p_owner)
        {
            if (p_owner == null ||
                _core == null ||
                !_externalActionBlockers.Add(p_owner))
            {
                return false;
            }

            if (_externalActionBlockers.Count > 1)
                return true;

            _core.TargetingFlow.ClearTarget();
            _core.CombatFlow?.CancelCombat();
            _core.LocomotionFlow?.Stop();
            return true;
        }

        public bool EndExternalBlock(object p_owner)
        {
            if (p_owner == null ||
                !_externalActionBlockers.Remove(p_owner))
            {
                return false;
            }

            if (_externalActionBlockers.Count == 0 &&
                !_isDead &&
                isActiveAndEnabled)
            {
                ChangeState(EEnemyActionState.Normal);
            }

            return true;
        }

        // 전투 구역 이탈로 Encounter를 재시작할 때 비사망 행동 상태를 초기화한다.
        public bool ResetForEncounter()
        {
            if (_core == null || _isDead)
                return false;

            bool hadHitReaction = _hitReactionFlow.IsActive;

            _hitReactionFlow.Reset();
            _core.TargetingFlow.Reset();
            _core.CombatFlow?.CancelCombat();
            _core.LocomotionFlow?.Reset();
            _hasCurrentState = false;
            ChangeState(EEnemyActionState.Normal);

            if (hadHitReaction)
            {
                OnHitReactionStateChanged?.Invoke(
                    EHitReactionState.None);
            }

            return true;
        }

        // 추적 경계 안에서 공격한 대상을 우선 타깃으로 전환한다.
        internal void HandleDamaged(DamageInfo p_damageInfo)
        {
            if (_isDead ||
                IsExternallyBlocked ||
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

            if (TryEnterHitReaction(reactionResult))
                ApplyKnockback(p_damageInfo, reactionResult);

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
                    _hitReactionImmunitySettings,
                    _knockdownFallDuration,
                    _standupDuration))
            {
                return false;
            }

            _core.CombatFlow?.CancelCombat();
            _core.LocomotionFlow?.Stop();

            ChangeState(EEnemyActionState.HitReaction);
            OnHitReactionStateChanged?.Invoke(
                _hitReactionFlow.CurrentState);
            return true;
        }

        private bool TickHitReaction(float p_deltaTime)
        {
            bool isKnockbackActive =
                _core.LocomotionModule != null &&
                _core.LocomotionModule.IsKnockbackActive;

            EHitReactionState previousState =
                _hitReactionFlow.CurrentState;

            bool isReactionActive = _hitReactionFlow.Tick(
                p_deltaTime,
                isKnockbackActive,
                Time.time);

            if (!isReactionActive)
            {
                if (CurrentState == EEnemyActionState.HitReaction)
                {
                    ChangeState(EEnemyActionState.Normal);
                    OnHitReactionStateChanged?.Invoke(
                        EHitReactionState.None);
                }

                return false;
            }

            if (previousState != _hitReactionFlow.CurrentState)
            {
                OnHitReactionStateChanged?.Invoke(
                    _hitReactionFlow.CurrentState);
            }

            return true;
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
            if (_isDead ||
                IsExternallyBlocked ||
                _core == null)
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
            if (_core != null &&
                !_isDead &&
                !IsExternallyBlocked)
                ChangeState(EEnemyActionState.Normal);
        }

        private void OnValidate()
        {
            _hitTypeResponseSettings ??= new HitTypeResponseSettings();
            _hitReactionImmunitySettings ??=
                new HitReactionImmunitySettings();
            _hitReactionImmunitySettings.Validate();
            _knockdownFallDuration =
                Mathf.Max(0f, _knockdownFallDuration);
            _standupDuration =
                Mathf.Max(0f, _standupDuration);
            _corpseRemovalDelay =
                Mathf.Max(0f, _corpseRemovalDelay);
        }
    }
}
