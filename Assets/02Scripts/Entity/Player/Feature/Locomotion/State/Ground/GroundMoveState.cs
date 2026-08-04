using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundMoveState : StateBase
    {
        public GroundMoveState(PlayerCore p_core, StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Move;

        protected override void Enter()
        {
        }

        protected override void Tick()
        {
            if (_Input.IsDash)
            {
                _StateFlow.ChangeState(ELocoStateType.Dash);
                return;
            }

            if (_Input.IsJump)
            {
                _StateFlow.ChangeState(ELocoStateType.Jump);
                return;
            }

            if (!_Core.LocomotionModule.IsGrounded)
            {
                _Core.LocomotionModule.StartFall();
                _StateFlow.ChangeState(ELocoStateType.Fall);
                return;
            }

            Transform cameraTransform = _Core.CameraCore?.RenderCamera.transform;
            bool isSprint = _Input.IsSprint;
            Vector2 moveInput = _Input.MoveInput;

            bool isAimFacing =
                TryResolveAimFacingDirection(cameraTransform, out Vector3 facingDirection);

            _Core.LocomotionModule.MoveGround(
                moveInput,
                cameraTransform,
                isSprint,
                isAimFacing,
                facingDirection);

            Vector2 animationMoveInput = ResolveAnimationMoveInput(moveInput);
            _Core.AnimationView.PlayGroundLocomotion(
                animationMoveInput,
                isSprint,
                isAimFacing);
        }

        protected override void Exit()
        {
        }

        private Vector3 ResolveAimDirection(Transform p_cameraTransform)
        {
            bool usesQuarterAim = _Core.CameraCore.Context.CurrentViewType == ECameraViewType.Quarter;

            if (usesQuarterAim && _Core.MouseSystem != null)
            {
                Vector3 mousePosition = _Input.MouseInputPos;

                if (_Core.MouseSystem.TryGetWorldDirection(
                    new Vector2(mousePosition.x, mousePosition.y),
                    _Core.PlayerTr.position,
                    out Vector3 mouseDirection))
                {
                    return mouseDirection;
                }
            }

            // TPS에서는 카메라 정면을 조준 방향으로 사용한다.
            Vector3 cameraDirection =
                Vector3.ProjectOnPlane(p_cameraTransform.forward, Vector3.up);

            return cameraDirection.sqrMagnitude > 0.0001f
                ? cameraDirection.normalized
                : Vector3.zero;
        }

        // 실제 월드 이동 방향을 Player 기준 전후좌우 값으로 변환한다.
        private Vector2 ResolveAnimationMoveInput(Vector2 p_rawMoveInput)
        {
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(
                _Core.LocomotionModule.Velocity,
                Vector3.up);

            if (horizontalVelocity.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            Vector3 localMoveDirection =
                _Core.PlayerTr.InverseTransformDirection(horizontalVelocity.normalized);

            float inputMagnitude = Mathf.Clamp01(p_rawMoveInput.magnitude);
            Vector2 localMoveInput =
                new(localMoveDirection.x, localMoveDirection.z);

            return Vector2.ClampMagnitude(localMoveInput * inputMagnitude, 1f);
        }

        private bool TryResolveAimFacingDirection(
            Transform p_cameraTransform,
            out Vector3 p_facingDirection)
        {
            p_facingDirection = Vector3.zero;

            CombatContext context = _Core.CombatContext;

            if (_Core.BlockCombat || !context.IsAiming)
            {
                context.ClearAimDirection();
                return false;
            }

            Vector3 aimDirection = ResolveAimDirection(p_cameraTransform);
            context.SetAimDirection(aimDirection);

            if (!context.HasAimDirection)
                return false;

            p_facingDirection = context.AimDirection;
            return true;
        }
    }
}
