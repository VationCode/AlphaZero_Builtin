using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // WeaponActionState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class WeaponActionState : CombatStateBase
    {
        private bool _isMeleePrimaryAction;
        private bool _isMeleeSecondaryAction;
        private bool _isRangePrimaryAction;
        private RangeWeapon _activeRangeWeapon;
        private int _playedComboIndex = -1;
        private int _facingComboIndex = -1;
        private bool _hasMeleeFacingDirection;
        private Vector3 _meleeFacingDirection;

        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponActionState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        public override ECombatStateType Type => ECombatStateType.WeaponAction;


        protected override void Enter()
        {
            if (!_Context.HasPendingWeaponAction)
            {
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            EWeaponActionType actionType =
                _Context.PendingWeaponActionType;

            _Context.ClearPendingWeaponAction();

            bool isRangePrimaryRequest =
                actionType == EWeaponActionType.Primary &&
                _Core.CombatModule.CurrentWeapon is RangeWeapon;

            RangeWeapon requestedRangeWeapon =
                isRangePrimaryRequest
                    ? _Core.CombatModule.CurrentRangeWeapon
                    : null;

            // 첫 발사 프레임부터 마지막 발사 방향을 Domain과 View가 공유한다.
            if (isRangePrimaryRequest &&
                _Core.CombatModule.TryGetRangeAimDirection(
                    out Vector3 aimDirection))
            {
                _Context.SetAimDirection(aimDirection);
                _Core.RigView?.SetAimDirection(
                    aimDirection);
            }

            // 첫 발이 실행되기 전에 Range 무기를 카메라 조준 방향으로 회전시킨다.
            if (isRangePrimaryRequest &&
                _Core.CombatModule.TryGetRangeFacingDirection(
                    out Vector3 facingDirection))
            {
                _Core.LocomotionModule.FaceGroundDirection(
                    facingDirection,
                    _Core.CameraCore?.RenderCamera?.transform,
                    true);
            }

            // 저장된 행동을 현재 무기에서 시작하지 못하면 Idle로 복귀한다.
            if (!_Core.CombatModule.TryBeginWeaponAction(actionType))
            {
                if (isRangePrimaryRequest &&
                    !_Context.IsAiming &&
                    !_Flow.TryRestoreRangeFacingHold())
                {
                    _Context.ClearAimDirection();
                    _Core.RigView?.ClearAimDirection();
                }

                TryChangeState(ECombatStateType.Idle);
                return;
            }

            _isRangePrimaryAction =
                isRangePrimaryRequest;

            _activeRangeWeapon =
                requestedRangeWeapon;

            _Context.SetRangePrimaryActive(
                _isRangePrimaryAction);

            _Context.SetRangeAttacking(
                _isRangePrimaryAction &&
                _activeRangeWeapon != null &&
                _activeRangeWeapon.DidFireDuringPrimaryAction);

            // TPS를 포함한 모든 Camera View에서 Range 공격 중 상체 Rig를 활성화한다.
            _Flow.RefreshRangeAimRigPresentation();

            _isMeleePrimaryAction =
                actionType == EWeaponActionType.Primary &&
                _Core.CombatModule.CurrentWeapon is MeleeWeapon;

            _isMeleeSecondaryAction =
                actionType == EWeaponActionType.Secondary &&
                _Core.CombatModule.CurrentWeapon is MeleeWeapon;

            if (_isMeleePrimaryAction)
            {
                if (!_Core.LocomotionModule.BeginRootMotion(ERootMotionMode.Ground))
                {
                    _Core.CombatModule.CancelWeaponAction();
                    TryChangeState(ECombatStateType.Idle);
                    return;
                }
            }

            if (_isMeleeSecondaryAction)
            {
                _Core.LocomotionModule.BeginInputLock();
                _Core.AnimationView?.PlayMeleeGuard();
            }

            _playedComboIndex = -1;
            ResetMeleeFacing();
            PlayCurrentMeleeCombo();
        }

        protected override void Tick()
        {
            CombatModule module = _Core.CombatModule;

            if (!_Core.CanUseCombat || !module.HasActiveAction)
            {
                module.CancelWeaponAction();
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            // Range 공격 중 점프하거나 공중 상태가 되면 추가 발사 전에 행동을 취소한다.
            if (_isRangePrimaryAction &&
                !_Flow.CanUseRangeAttack())
            {
                module.CancelWeaponAction();
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            UpdateMeleeFacing();

            bool isInputHeld = module.ActiveActionType switch
            {
                EWeaponActionType.Primary => _Input.IsPrimaryAction,
                EWeaponActionType.Secondary => _Input.IsSecondaryAction,
                _ => false
            };

            bool isInputPressed = module.ActiveActionType switch
            {
                EWeaponActionType.Primary => _Input.IsPrimaryActionPressed,
                EWeaponActionType.Secondary => _Input.IsSecondaryActionPressed,
                _ => false
            };

            // State는 입력 상태만 전달하고, 사용 방식과 종료 시점은 Weapon이 판단한다.
            module.TickWeaponAction(
                isInputHeld,
                isInputPressed,
                Time.deltaTime);

            // 쿨다운 대기가 끝나 실제 첫 발이 나간 시점부터 공격 중으로 전환한다.
            if (_isRangePrimaryAction &&
                !_Context.IsRangeAttacking &&
                _activeRangeWeapon != null &&
                _activeRangeWeapon.DidFireDuringPrimaryAction)
            {
                _Context.SetRangeAttacking(true);
                _Context.SetAimDirection(
                    _activeRangeWeapon.LastFireDirection);
                _Core.RigView?.SetAimDirection(
                    _activeRangeWeapon.LastFireDirection);
            }

            PlayCurrentMeleeCombo();

            // Melee Primary처럼 무기 내부에서 완료한 행동은 Idle로 복귀한다.
            if (!module.HasActiveAction)
                TryChangeState(ECombatStateType.Idle);
        }

        protected override void Exit()
        {
            // 다른 상태에 의해 강제 전환됐을 때 진행 중인 행동을 정리한다.
            _Core.CombatModule.CancelWeaponAction();

            if (_isMeleePrimaryAction)
                _Core.LocomotionModule.EndRootMotion();

            if (_isMeleeSecondaryAction)
                _Core.LocomotionModule.EndInputLock();

            if (_isRangePrimaryAction)
            {
                bool didFire =
                    _activeRangeWeapon != null &&
                    _activeRangeWeapon.DidFireDuringPrimaryAction;

                _Context.SetRangePrimaryActive(false);
                _Context.SetRangeAttacking(false);

                if (!_Context.IsAiming)
                {
                    if (didFire)
                    {
                        _Context.SetAimDirection(
                            _activeRangeWeapon.LastFireDirection);
                        _Core.RigView?.SetAimDirection(
                            _activeRangeWeapon.LastFireDirection);

                        _Flow.BeginRangeFacingHold(
                            _activeRangeWeapon.PostAttackFacingDuration);
                    }
                    else if (!_Flow.TryRestoreRangeFacingHold())
                    {
                        _Context.ClearAimDirection();
                        _Core.RigView?.ClearAimDirection();
                    }
                }

                _Flow.RefreshRangeAimRigPresentation();
            }

            if (_isMeleePrimaryAction || _isMeleeSecondaryAction)
            {
                _Core.AnimationView?.StopMeleeAction();
            }

            _isMeleePrimaryAction = false;
            _isMeleeSecondaryAction = false;
            _isRangePrimaryAction = false;
            _activeRangeWeapon = null;
            _playedComboIndex = -1;
            ResetMeleeFacing();
        }

        // 콤보 하나에서 첫 유효 입력 방향만 저장하고 해당 방향으로 계속 회전한다.
        private void UpdateMeleeFacing()
        {
            if (!_isMeleePrimaryAction ||
                !(_Core.CombatModule.CurrentWeapon is MeleeWeapon))
            {
                return;
            }

            int comboIndex =
                _Core.CombatModule.CurrentMeleeComboIndex;

            if (comboIndex < 0)
                return;

            if (_facingComboIndex != comboIndex)
            {
                _facingComboIndex = comboIndex;
                _hasMeleeFacingDirection = false;
                _meleeFacingDirection = Vector3.zero;
            }

            Transform cameraTransform =
                _Core.CameraCore?.RenderCamera?.transform;

            if (!_hasMeleeFacingDirection)
            {
                if (!_Core.LocomotionModule.TryGetGroundInputDirection(
                        _Input.MoveInput,
                        cameraTransform,
                        out _meleeFacingDirection))
                {
                    return;
                }

                _hasMeleeFacingDirection = true;
            }

            // 입력은 다시 읽지 않고 저장된 방향을 향한 보간만 유지한다.
            _Core.LocomotionModule.FaceGroundDirection(
                _meleeFacingDirection,
                cameraTransform,
                false);
        }

        private void ResetMeleeFacing()
        {
            _facingComboIndex = -1;
            _hasMeleeFacingDirection = false;
            _meleeFacingDirection = Vector3.zero;
        }

        // MeleeWeapon이 실제 다음 콤보로 전환했을 때 해당 전신 애니메이션을 재생한다.
        private void PlayCurrentMeleeCombo()
        {
            if (!_isMeleePrimaryAction ||
                !(_Core.CombatModule.CurrentWeapon is MeleeWeapon))
            {
                return;
            }

            int comboIndex =
                _Core.CombatModule.CurrentMeleeComboIndex;
            string comboName =
                _Core.CombatModule.CurrentMeleeComboName;

            if (comboIndex < 0 ||
                string.IsNullOrWhiteSpace(comboName) ||
                comboIndex == _playedComboIndex)
            {
                return;
            }

            _Core.AnimationView.PlayMeleeCombo(comboName);
            _playedComboIndex = comboIndex;
        }

    }
}
