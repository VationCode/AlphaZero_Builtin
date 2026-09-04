using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // 카메라 기준 이동 방향을 Player 로컬 8방향 Dodge 표현으로 변환한다.
    public sealed class DodgeState : StateBase
    {
        private const float DirectionEpsilon = 0.0001f;

        private bool _hasStartedEvasion;

        public DodgeState(
            PlayerCore p_core,
            LocomotionStateFlow p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Dodge;

        protected override void Enter()
        {
            Transform cameraTr =
                _Core.CameraCore?.RenderCamera?.transform;
            ELocomotionMode mode =
                _Core.LocomotionContext.CurrentMode;

            if (!TryResolveDodgeDirection(
                    _Input.MoveInput,
                    cameraTr,
                    mode,
                    out Vector2 localDirection,
                    out Vector3 worldDirection))
            {
                ChangeToRecoveryState(mode);
                return;
            }

            _Core.LocomotionContext.LockedMoveDirection =
                worldDirection;

            _hasStartedEvasion =
                _Core.LocomotionModule.BeginEvasion(
                    EEvasionType.Dodge,
                    worldDirection,
                    mode);

            if (!_hasStartedEvasion)
            {
                ChangeToRecoveryState(mode);
                return;
            }

            // 회전은 유지하고 Blend Tree에 Player 로컬 방향만 전달한다.
            _Core.AnimationView.PlayDodge(localDirection);
        }

        protected override void Tick()
        {
            if (!_hasStartedEvasion ||
                !_Core.LocomotionModule.TickEvasion(Time.deltaTime))
            {
                return;
            }

            ChangeToRecoveryState(
                _Core.LocomotionContext.CurrentMode);
        }

        protected override void Exit()
        {
            _Core.LocomotionModule.EndEvasion();
            _hasStartedEvasion = false;
        }

        private bool TryResolveDodgeDirection(
            Vector2 p_moveInput,
            Transform p_cameraTransform,
            ELocomotionMode p_mode,
            out Vector2 p_localDirection,
            out Vector3 p_worldDirection)
        {
            Vector2 moveInput = Vector2.ClampMagnitude(
                p_moveInput,
                1f);
            p_localDirection = Vector2.zero;
            p_worldDirection = Vector3.zero;

            if (moveInput.sqrMagnitude < DirectionEpsilon)
                return false;

            // 일반 이동과 동일하게 방향키를 카메라 기준 월드 방향으로 변환한다.
            if (!_Core.LocomotionModule.TryGetInputDirection(
                    moveInput,
                    p_cameraTransform,
                    p_mode,
                    out p_worldDirection))
            {
                return false;
            }

            if (p_mode != ELocomotionMode.Flight)
            {
                p_worldDirection = Vector3.ProjectOnPlane(
                    p_worldDirection,
                    Vector3.up);
            }

            if (p_worldDirection.sqrMagnitude < DirectionEpsilon)
                return false;

            p_worldDirection.Normalize();

            // 이동할 월드 방향을 현재 Player 바라보는 방향의 Blend Tree 축으로 변환한다.
            Vector3 localDirection =
                _Core.PlayerTr.InverseTransformDirection(
                    p_worldDirection);

            p_localDirection = new Vector2(
                localDirection.x,
                localDirection.z);

            if (p_localDirection.sqrMagnitude < DirectionEpsilon)
                return false;

            p_localDirection.Normalize();
            return true;
        }

        private void ChangeToRecoveryState(ELocomotionMode p_mode)
        {
            ELocoStateType nextState =
                p_mode == ELocomotionMode.Flight ||
                _Core.LocomotionModule.IsGrounded
                    ? ELocoStateType.Move
                    : ELocoStateType.Fall;

            _StateFlow.ChangeState(nextState);
        }
    }
}
