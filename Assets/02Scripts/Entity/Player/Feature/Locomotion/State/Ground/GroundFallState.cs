namespace Alpha.Player.Locomotion
{
    public class GroundFallState : StateBase
    {
        public GroundFallState(
            PlayerCore p_core,
            StateFlowBase p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type =>
            ELocoStateType.Fall;

        protected override void Enter()
        {
            _Core.AnimationView.PlayFall();
        }

        protected override void Tick()
        {
            _Core.LocomotionModule.MoveAirborne(
                _Core.LocomotionContext.LockedMoveDirection);

            if (_Core.LocomotionModule.IsGrounded &&
                _Core.LocomotionModule.VerticalVelocity <= 0f)
            {
                _StateFlow.ChangeState(ELocoStateType.Land);
            }
        }

        protected override void Exit()
        {
        }
    }
}
