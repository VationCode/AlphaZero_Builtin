using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public class GroundJumpState : StateBase
    {
        public GroundJumpState(
            PlayerCore p_core,
            StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type =>
            ELocoStateType.Jump;

        protected override void Enter()
        {
            Transform cameraTr = _Core.CameraCore?.RenderCamera.transform;

            bool isSprint = _Input.IsSprint;

            _Core.LocomotionModule.StartJump(
                _Input.MoveInput,
                cameraTr,
                isSprint);

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
