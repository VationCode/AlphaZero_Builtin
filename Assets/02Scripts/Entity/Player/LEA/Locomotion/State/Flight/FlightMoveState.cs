namespace Alpha.Player.Locomotion
{
    public class FlightMoveState : StateBase
    {
        public FlightMoveState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
        public override EStateType Type => EStateType.Move;
        protected override void Enter()
        {

        }
        protected override void Tick()
        {
            if (_Input.IsDash)
            {
                _StateFlow.ChangeState(EStateType.Dash);
                return;
            }

            if (_Input.IsJump)
            {
                _StateFlow.ChangeState(EStateType.Fall);
                return;
            }
        }
        protected override void Exit()
        {

        }
    }
}
