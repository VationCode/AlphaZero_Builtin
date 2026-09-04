using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // Ground Mode의 점프 시작과 상승 구간을 담당한다.
    public sealed class JumpState : StateBase
    {
        public JumpState(
            PlayerCore p_core,
            LocomotionStateFlow p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Jump;

        protected override void Enter()
        {
            Transform cameraTr = _Core.CameraCore?.RenderCamera.transform;

            _Core.LocomotionModule.StartJump(
                _Input.MoveInput,
                cameraTr,
                _Input.IsSprint);

            _Core.AnimationView.PlayJump();
        }

        protected override void Tick()
        {
            _Core.LocomotionModule.MoveAirborne(
                _Core.LocomotionContext.LockedMoveDirection);

            if (_Core.LocomotionModule.VerticalVelocity <= 0f)
                _StateFlow.ChangeState(ELocoStateType.Fall);
        }

        protected override void Exit()
        {
        }
    }
}
