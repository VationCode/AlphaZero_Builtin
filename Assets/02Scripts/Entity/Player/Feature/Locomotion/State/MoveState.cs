using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // 모든 Mode의 일반 이동 입력과 지상 전용 상태 전환을 조정한다.
    public sealed class MoveState : StateBase
    {
        private const float FallGraceDuration = 0.03f;
        private float _ungroundedElapsed;

        public MoveState(
            PlayerCore p_core,
            LocomotionStateFlow p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Move;

        protected override void Enter()
        {
            _ungroundedElapsed = 0f;
        }

        protected override void Tick()
        {
            ELocomotionMode mode = _Core.LocomotionContext.CurrentMode;

            if (_Core.LocomotionModule.BlocksInput)
            {
                if (mode == ELocomotionMode.Ground)
                {
                    _Core.AnimationView.PlayGroundLocomotion(
                        Vector2.zero,
                        false,
                        _Core.CombatContext.IsCombatStanceActive);
                }

                return;
            }

            if (_Input.IsDash)
            {
                _StateFlow.ChangeState(ELocoStateType.Dash);
                return;
            }

            // Jump/Fall/Land는 중력을 사용하는 Ground Mode에서만 전환한다.
            if (mode == ELocomotionMode.Ground && TryChangeGroundState())
                return;

            Transform cameraTransform = _Core.CameraCore?.RenderCamera.transform;
            bool isSprint = _Input.IsSprint;
            Vector2 moveInput = _Input.MoveInput;
            bool isCombatStance = _Core.CombatContext.IsCombatStanceActive;

            Vector3 facingDirection = Vector3.zero;
            bool isAimFacing = mode == ELocomotionMode.Ground &&
                               TryResolveAimFacingDirection(out facingDirection);

            if (mode != ELocomotionMode.Ground)
                ClearRangeAimPresentation();

            float rangeMoveSpeedMultiplier = isAimFacing
                ? _Core.CombatModule.CurrentRangeWeapon?
                    .FireResponseSettings?.MoveSpeedMultiplier ?? 1f
                : 1f;

            _Core.LocomotionModule.Move(
                moveInput,
                cameraTransform,
                mode,
                isSprint,
                isCombatStance,
                facingDirection,
                rangeMoveSpeedMultiplier);

            // 현재 Flight View가 없으므로 기존 Ground Locomotion 표현만 유지한다.
            if (mode != ELocomotionMode.Ground)
                return;

            _Core.AnimationView.PlayGroundLocomotion(
                ResolveAnimationMoveInput(moveInput),
                isSprint,
                isCombatStance);
        }

        protected override void Exit()
        {
            _ungroundedElapsed = 0f;
            ClearRangeAimPresentation();
        }

        private bool TryChangeGroundState()
        {
            if (_Input.IsJump)
            {
                _StateFlow.ChangeState(ELocoStateType.Jump);
                return true;
            }

            if (!_Core.LocomotionModule.IsGrounded)
            {
                _ungroundedElapsed += Time.deltaTime;

                if (_ungroundedElapsed >= FallGraceDuration)
                {
                    _Core.LocomotionModule.StartFall();
                    _StateFlow.ChangeState(ELocoStateType.Fall);
                    return true;
                }
            }
            else
            {
                _ungroundedElapsed = 0f;
            }

            return false;
        }

        private Vector2 ResolveAnimationMoveInput(Vector2 p_rawMoveInput)
        {
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(
                _Core.LocomotionModule.Velocity,
                Vector3.up);

            if (horizontalVelocity.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            Vector3 localMoveDirection =
                _Core.PlayerTr.InverseTransformDirection(
                    horizontalVelocity.normalized);

            float inputMagnitude = Mathf.Clamp01(p_rawMoveInput.magnitude);
            Vector2 localMoveInput = new(
                localMoveDirection.x,
                localMoveDirection.z);

            return Vector2.ClampMagnitude(
                localMoveInput * inputMagnitude,
                1f);
        }

        private bool TryResolveAimFacingDirection(
            out Vector3 p_facingDirection)
        {
            p_facingDirection = Vector3.zero;
            CombatContext context = _Core.CombatContext;

            if (!_Core.CanUseCombat || !context.UsesAimFacing)
            {
                ClearRangeAimPresentation();
                return false;
            }

            if (!_Core.CombatModule.TryGetRangeAimDirection(
                    out Vector3 aimDirection))
            {
                ClearRangeAimPresentation();
                return false;
            }

            context.SetAimDirection(aimDirection);
            _Core.RigView?.SetAimDirection(aimDirection);

            if (!context.HasAimDirection)
                return false;

            p_facingDirection = Vector3.ProjectOnPlane(
                context.AimDirection,
                Vector3.up);

            if (p_facingDirection.sqrMagnitude <= 0.0001f)
                return false;

            p_facingDirection.Normalize();
            return true;
        }

        private void ClearRangeAimPresentation()
        {
            _Core.CombatContext.ClearAimDirection();
            _Core.RigView?.ClearAimDirection();
        }
    }
}
