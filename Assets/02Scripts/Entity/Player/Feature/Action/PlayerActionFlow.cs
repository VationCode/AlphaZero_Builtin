using Alpha.Combat;
using Alpha.Player.Combat;
using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player.Actions
{
    public enum EPlayerActionState
    {
        Normal,
        HitReaction,
        Knockdown,
        Dead
    }

    // 공용 충격 판정 결과를 Player 행동 상태와 Module/View 실행으로 변환한다.
    [DisallowMultipleComponent]
    public sealed class PlayerActionFlow : MonoBehaviour
    {
        private enum EReactionPhase
        {
            None,
            Hit,
            Knockdown,
            Down,
            Standup,
            DeadFalling,
            Dead
        }

        [Header("Hit Type Response")]
        [SerializeField]
        private HitTypeResponseSettings _hitTypeResponseSettings = new();

        [Header("Knockdown Animation")]
        [Tooltip("쓰러지는 애니메이션을 재생하는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _knockdownFallDuration = 1.1f;

        [Tooltip("기상 애니메이션이 끝날 때까지 행동을 잠그는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _standupDuration = 0.95f;

        [Header("Camera Shake")]
        [SerializeField]
        private string _lightShakeName = "Weak";

        [SerializeField]
        private string _heavyShakeName = "Medium";

        [SerializeField]
        private string _knockdownShakeName = "Strong";

        private PlayerCore _core;
        private EHitReaction _activeReaction = EHitReaction.None;
        private EReactionPhase _phase = EReactionPhase.None;
        private float _remainingTime;
        private float _downRecoveryDuration;
        private bool _ownsActionLock;
        private bool _hasCurrentState;
        private bool _isDead;

        public EPlayerActionState CurrentState { get; private set; } =
            EPlayerActionState.Normal;

        public bool IsReacting =>
            CurrentState == EPlayerActionState.HitReaction ||
            CurrentState == EPlayerActionState.Knockdown;

        public bool IsDead => _isDead;

        public event System.Action<EPlayerActionState> OnStateChanged;

        public void Bind(PlayerCore p_core)
        {
            _core = p_core;
            _activeReaction = EHitReaction.None;
            _phase = EReactionPhase.None;
            _remainingTime = 0f;
            _downRecoveryDuration = 0f;
            _ownsActionLock = false;
            _hasCurrentState = false;
            _isDead = false;

            ChangeState(EPlayerActionState.Normal);
        }

        public void Unbind()
        {
            ReleaseActionLock(true);
            _core = null;
            _hasCurrentState = false;
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

            ApplyKnockback(p_damageInfo, reactionResult);
            RequestDamageShake(reactionResult.Reaction);
            TryEnterHitReaction(reactionResult);
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
            if (!p_result.HasReaction ||
                p_result.Priority < (int)_activeReaction)
            {
                return false;
            }

            AcquireActionLock();
            _activeReaction = p_result.Reaction;

            if (p_result.Reaction == EHitReaction.Knockdown ||
                p_result.Reaction == EHitReaction.Launch)
            {
                _phase = EReactionPhase.Knockdown;
                _remainingTime = _knockdownFallDuration;
                _downRecoveryDuration = p_result.RecoveryDuration;
                ChangeState(EPlayerActionState.Knockdown);
                _core.AnimationView?.PlayHitReaction(p_result.Reaction);
                return true;
            }

            _phase = EReactionPhase.Hit;
            _remainingTime = p_result.RecoveryDuration;
            _downRecoveryDuration = 0f;
            ChangeState(EPlayerActionState.HitReaction);
            _core.AnimationView?.PlayHitReaction(p_result.Reaction);
            return true;
        }

        internal void HandleDeath()
        {
            if (_isDead || _core == null)
                return;

            _isDead = true;
            _activeReaction = EHitReaction.None;
            AcquireActionLock();
            RequestShake(_knockdownShakeName);

            _phase = EReactionPhase.DeadFalling;
            _remainingTime = _knockdownFallDuration;
            ChangeState(EPlayerActionState.Dead);
            _core.AnimationView?.PlayKnockdown();
        }

        private void Update()
        {
            if (_core == null || _phase == EReactionPhase.None)
                return;

            switch (_phase)
            {
                case EReactionPhase.Hit:
                    if (TickTimer(Time.deltaTime))
                        CompleteReaction();
                    break;

                case EReactionPhase.Knockdown:
                    if (TickTimer(Time.deltaTime))
                        EnterDownPhase();
                    break;

                case EReactionPhase.Down:
                    // Enemy와 동일하게 물리 넉백이 끝난 뒤 Down 회복 시간을 계산한다.
                    if (_core.LocomotionModule?.IsKnockbackActive == true)
                        break;

                    if (TickTimer(Time.deltaTime))
                        EnterStandupPhase();
                    break;

                case EReactionPhase.Standup:
                    if (TickTimer(Time.deltaTime))
                        CompleteReaction();
                    break;

                case EReactionPhase.DeadFalling:
                    if (TickTimer(Time.deltaTime))
                    {
                        _phase = EReactionPhase.Dead;
                        _core.AnimationView?.PlayKnockdownLoop();
                    }
                    break;
            }
        }

        private void EnterDownPhase()
        {
            _phase = EReactionPhase.Down;
            _remainingTime = _downRecoveryDuration;
            _core.AnimationView?.PlayKnockdownLoop();
        }

        private void EnterStandupPhase()
        {
            _phase = EReactionPhase.Standup;
            _remainingTime = _standupDuration;
            _core.AnimationView?.PlayKnockdownStandup();
        }

        private void CompleteReaction()
        {
            _activeReaction = EHitReaction.None;
            _phase = EReactionPhase.None;
            _remainingTime = 0f;
            _downRecoveryDuration = 0f;

            ReleaseActionLock();
            RestoreLocomotionPresentation();
            ChangeState(EPlayerActionState.Normal);
        }

        // Player의 상위 행동 상태가 이동·전투 Flow를 함께 중단한다.
        private void AcquireActionLock()
        {
            if (_ownsActionLock || _core == null)
                return;

            _ownsActionLock = true;
            _core.BeginCombatBlock();
            _core.LocomotionModule?.BeginInputLock();
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
            _core.EndCombatBlock();
        }

        private void RestoreLocomotionPresentation()
        {
            if (_core?.AnimationView == null)
                return;

            _core.AnimationView.EndDamageReaction();

            switch (_core.LocomotionContext.CurrentState)
            {
                case ELocoStateType.Jump:
                    _core.AnimationView.PlayJump();
                    break;

                case ELocoStateType.Fall:
                    _core.AnimationView.PlayFall();
                    break;

                case ELocoStateType.Land:
                    _core.AnimationView.PlayLand();
                    break;

                case ELocoStateType.Dash:
                    _core.AnimationView.PlayDash();
                    break;

                default:
                    _core.AnimationView.PlayGroundLocomotion(
                        Vector2.zero,
                        false,
                        _core.CombatContext.UsesAimFacing);
                    break;
            }
        }

        private void ChangeState(EPlayerActionState p_nextState)
        {
            if (_hasCurrentState && CurrentState == p_nextState)
                return;

            CurrentState = p_nextState;
            _hasCurrentState = true;
            OnStateChanged?.Invoke(CurrentState);
        }

        private bool TickTimer(float p_deltaTime)
        {
            _remainingTime = Mathf.Max(
                0f,
                _remainingTime - Mathf.Max(0f, p_deltaTime));

            return _remainingTime <= 0f;
        }

        private void RequestDamageShake(EHitReaction p_reaction)
        {
            if (p_reaction == EHitReaction.None)
                return;

            string shakeName = p_reaction switch
            {
                EHitReaction.Heavy => _heavyShakeName,
                EHitReaction.Knockdown or EHitReaction.Launch =>
                    _knockdownShakeName,
                _ => _lightShakeName
            };

            RequestShake(shakeName);
        }

        private void RequestShake(string p_name)
        {
            if (!string.IsNullOrWhiteSpace(p_name))
                _core?.CameraCore?.RequestShake(p_name.Trim());
        }

        private void OnValidate()
        {
            _hitTypeResponseSettings ??= new HitTypeResponseSettings();
            _knockdownFallDuration =
                Mathf.Max(0f, _knockdownFallDuration);
            _standupDuration =
                Mathf.Max(0f, _standupDuration);
        }
    }
}
