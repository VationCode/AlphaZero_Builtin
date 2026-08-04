namespace Alpha.Player.Locomotion
{
    public class FlightMoveState : StateBase
    {
        public FlightMoveState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }
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
                _StateFlow.ChangeState(ELocoStateType.Fall);
                return;
            }
        }
        protected override void Exit()
        {

        }
    }
}
