using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // 현재 Mode의 공간 이동 규칙을 사용해 공통 Dash를 실행한다.
    public sealed class DashState : StateBase
    {
        private bool _hasStartedEvasion;

        public DashState(
            PlayerCore p_core,
            LocomotionStateFlow p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Dash;

        protected override void Enter()
        {
            Transform cameraTr = _Core.CameraCore?.RenderCamera.transform;
            ELocomotionMode mode = _Core.LocomotionContext.CurrentMode;

            Vector3 direction = ResolveDashDirection(
                _Input.MoveInput,
                cameraTr,
                mode);

            _Core.LocomotionContext.LockedMoveDirection = direction;
            _Core.LocomotionModule.FaceDirection(
                direction,
                cameraTr,
                mode,
                true);

            _hasStartedEvasion =
                _Core.LocomotionModule.BeginEvasion(
                    EEvasionType.Dash,
                    direction,
                    mode);

            if (!_hasStartedEvasion)
            {
                ChangeToRecoveryState(mode);
                return;
            }

            _Core.AnimationView.PlayDash();
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

        // Dash 입력은 현재 Mode의 카메라 기준 방향을 사용한다.
        private Vector3 ResolveDashDirection(
            Vector2 p_moveInput,
            Transform p_cameraTransform,
            ELocomotionMode p_mode)
        {
            if (_Core.LocomotionModule.TryGetInputDirection(
                    p_moveInput,
                    p_cameraTransform,
                    p_mode,
                    out Vector3 direction))
            {
                return direction;
            }

            direction = _Core.PlayerTr.forward;

            if (p_mode != ELocomotionMode.Flight)
                direction = Vector3.ProjectOnPlane(direction, Vector3.up);

            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
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
