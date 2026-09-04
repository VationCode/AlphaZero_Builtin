namespace Alpha.Player.Locomotion
{
    // Ground Mode의 공중 이동과 접지 전환을 담당한다.
    public sealed class FallState : StateBase
    {
        public FallState(
            PlayerCore p_core,
            LocomotionStateFlow p_stateFlow)
            : base(p_core, p_stateFlow)
        {
        }

        public override ELocoStateType Type => ELocoStateType.Fall;

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
