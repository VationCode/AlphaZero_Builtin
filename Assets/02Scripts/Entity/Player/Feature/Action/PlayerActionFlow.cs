using Alpha.Combat;
using Alpha.Player.Combat;
using Alpha.Player.Locomotion;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Actions
{
    public enum EPlayerActionState
    {
        Normal = 0,
        HitReaction = 1,
        Dead = 2
    }

    // Player 전체 행동의 우선순위를 소유하고 Combat과 Locomotion의 실행을 허용하거나 차단한다.
    // 피격·넉다운·사망처럼 일반 행동보다 우선하는 상태만 이 Flow에서 조정한다.
    [DisallowMultipleComponent]
    public sealed class PlayerActionFlow : MonoBehaviour
    {
        [Header("Hit Type Response")]
        [SerializeField]
        private HitTypeResponseSettings _hitTypeResponseSettings = new();

        [Header("Hit Reaction Immunity")]
        [SerializeField]
        private HitReactionImmunitySettings _hitReactionImmunitySettings =
            new();

        [Header("Hit Reaction Timing")]
        [Tooltip("Knockdown에서 LyingDown으로 전환하기까지의 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _knockdownFallDuration = 1.1f;

        [Tooltip("StandUp 상태가 끝날 때까지 행동을 잠그는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _standupDuration = 0.95f;

        private readonly HitReactionFlow _hitReactionFlow = new();
        private readonly HashSet<object> _externalActionBlockers = new();

        private PlayerCore _core;
        private float _deathFallRemainingTime;
        private bool _isDeathFalling;
        private bool _ownsActionLock;
        private bool _hasCurrentState;
        private bool _isDead;
        private bool _isCombatInputBlocked;

        public EPlayerActionState CurrentState { get; private set; } =
            EPlayerActionState.Normal;

        // 상위 Action이 Normal이고 외부 UI가 막지 않을 때만 CombatFlow를 허용한다.
        public bool AllowsCombat =>
            CurrentState == EPlayerActionState.Normal &&
            !_isCombatInputBlocked &&
            !IsExternallyBlocked;

        // 상위 Action이 Normal일 때만 LocomotionFlow의 일반 입력 이동을 허용한다.
        // Root Motion처럼 Combat이 별도로 소유한 잠금은 LocomotionModule이 추가로 판단한다.
        public bool AllowsLocomotion =>
            CurrentState == EPlayerActionState.Normal &&
            !IsExternallyBlocked;

        public bool IsExternallyBlocked =>
            _externalActionBlockers.Count > 0;

        public bool IsReacting =>
            CurrentState == EPlayerActionState.HitReaction;

        public bool IsDead => _isDead;
        public bool IsDeathFalling => _isDeathFalling;

        public EHitReactionState HitReactionState =>
            _hitReactionFlow.CurrentState;

        public event System.Action<EPlayerActionState> OnStateChanged;
        public event System.Action<EHitReactionState> OnHitReactionStateChanged;
        public event System.Action<EHitReaction> OnDamageFeedbackRequested;
        public event System.Action OnDeathStarted;
        public event System.Action OnDeathDownStarted;

        public void Bind(PlayerCore p_core)
        {
            _core = p_core;
            _hitReactionFlow.Reset();
            _deathFallRemainingTime = 0f;
            _isDeathFalling = false;
            _ownsActionLock = false;
            _externalActionBlockers.Clear();
            _hasCurrentState = false;
            _isDead = false;

            ChangeState(EPlayerActionState.Normal);
        }

        public void Unbind()
        {
            ReleaseActionLock(true);
            ClearExternalActionBlocks();
            _core = null;
            _hasCurrentState = false;
        }

        // Cinematic 등 외부 흐름이 Player의 일반 행동 전체를 중첩 차단한다.
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

            _core.InventoryFlow?.RequestCloseInventory();
            _core.LocomotionModule?.BeginInputLock();
            CancelLowerPriorityActions();
            _core.LocomotionModule?.CancelKnockback();

            // 연출 중에는 직전 이동 입력이 남지 않도록 Player 표현도 Idle로 정리한다.
            _core.AnimationView?.PlayGroundLocomotion(
                Vector2.zero,
                false,
                false);
            return true;
        }

        public bool EndExternalBlock(object p_owner)
        {
            if (p_owner == null ||
                !_externalActionBlockers.Remove(p_owner))
            {
                return false;
            }

            if (_externalActionBlockers.Count == 0)
                _core?.LocomotionModule?.EndInputLock();

            return true;
        }

        // Inventory처럼 Player 외부 상태가 Combat 입력만 일시적으로 차단할 때 사용한다.
        internal void SetCombatInputBlocked(bool p_isBlocked)
        {
            _isCombatInputBlocked = p_isBlocked;
        }

        // Core가 전달한 피해를 공용 충격 판정 후 Player 상태 전환으로 요청한다.
        internal void HandleDamaged(DamageInfo p_damageInfo)
        {
            if (_isDead || _core == null || !p_damageInfo.IsValid)
                return;

            // 치명타는 곧이어 전달되는 사망 이벤트에서 한 번만 처리한다.
            if (_core.HealthModule != null &&
                _core.HealthModule.CurrentHealth <= 0f)
            {
                return;
            }

            ImpactReactionResult reactionResult =
                ImpactReactionSystem.Resolve(
                    p_damageInfo,
                    _hitTypeResponseSettings);

            OnDamageFeedbackRequested?.Invoke(reactionResult.Reaction);

            if (TryEnterHitReaction(reactionResult))
                ApplyKnockback(p_damageInfo, reactionResult);
        }

        // 공격자가 전달한 방향과 거리/시간을 실제 넉백 요청으로 조합한다.
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

            KnockbackSystem.TryApply(this, knockbackInfo);
        }

        // 공용 반응 우선순위를 기준으로 현재 Player 행동을 중단한다.
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

            AcquireActionLock();

            ChangeState(EPlayerActionState.HitReaction);
            OnHitReactionStateChanged?.Invoke(
                _hitReactionFlow.CurrentState);
            return true;
        }

        internal void HandleDeath()
        {
            if (_isDead || _core == null)
                return;

            _isDead = true;
            _hitReactionFlow.Clear();
            AcquireActionLock();

            _isDeathFalling = true;
            _deathFallRemainingTime = _knockdownFallDuration;
            ChangeState(EPlayerActionState.Dead);
            OnDeathStarted?.Invoke();
        }

        private void Update()
        {
            if (_core == null)
                return;

            if (_isDeathFalling)
            {
                if (TickDeathFall(Time.deltaTime))
                {
                    _isDeathFalling = false;
                    OnDeathDownStarted?.Invoke();
                }

                return;
            }

            if (!_hitReactionFlow.IsActive)
                return;

            EHitReactionState previousState =
                _hitReactionFlow.CurrentState;

            bool isReactionActive = _hitReactionFlow.Tick(
                Time.deltaTime,
                _core.LocomotionModule?.IsKnockbackActive == true,
                Time.time);

            if (!isReactionActive)
            {
                CompleteReaction();
                return;
            }

            if (previousState != _hitReactionFlow.CurrentState)
            {
                OnHitReactionStateChanged?.Invoke(
                    _hitReactionFlow.CurrentState);
            }
        }

        private void CompleteReaction()
        {
            ReleaseActionLock();
            ChangeState(EPlayerActionState.Normal);
            OnHitReactionStateChanged?.Invoke(
                EHitReactionState.None);
        }

        // 우선 행동이 시작될 때 하위 Combat을 취소하고 Locomotion 입력을 잠근다.
        private void AcquireActionLock()
        {
            if (_ownsActionLock || _core == null)
                return;

            _ownsActionLock = true;
            _core.LocomotionModule?.BeginInputLock();
            CancelLowerPriorityActions();
        }

        private void CancelLowerPriorityActions()
        {
            if (_core == null)
                return;

            _core.CombatFlow?.TryChangeState(ECombatStateType.Idle);
            _core.CombatModule?.CancelWeaponAction();

            if (_core.LocomotionModeFlow?.CurrentMode ==
                    ELocomotionMode.Ground &&
                _core.LocomotionModule?.IsGrounded == true &&
                _core.LocomotionModeFlow.CurrentFlow?.CurrentState?.Type !=
                    ELocoStateType.Move)
            {
                _core.LocomotionModeFlow.CurrentFlow.ChangeState(
                    ELocoStateType.Move);
            }
        }

        private void ReleaseActionLock(bool p_force = false)
        {
            if (!_ownsActionLock || _core == null ||
                (_isDead && !p_force))
            {
                return;
            }

            _ownsActionLock = false;
            _core.LocomotionModule?.EndInputLock();
        }

        private void ClearExternalActionBlocks()
        {
            if (_externalActionBlockers.Count == 0)
                return;

            _externalActionBlockers.Clear();
            _core?.LocomotionModule?.EndInputLock();
        }

        private void ChangeState(EPlayerActionState p_nextState)
        {
            if (_hasCurrentState && CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            _hasCurrentState = true;
            OnStateChanged?.Invoke(CurrentState);
        }

        private bool TickDeathFall(float p_deltaTime)
        {
            _deathFallRemainingTime = Mathf.Max(
                0f,
                _deathFallRemainingTime - Mathf.Max(0f, p_deltaTime));

            return _deathFallRemainingTime <= 0f;
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
        }
    }
}
