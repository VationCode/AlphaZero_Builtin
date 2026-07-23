
namespace Alpha.Player.Locomotion
{
    public class FlightDashState : StateBase
    {
        public FlightDashState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }

        public override EStateType Type => EStateType.Dash;

        protected override void Enter()
        {

        }
        protected override void Tick()
        {
            if (_Input.IsJump)
            {
                _StateFlow.ChangeState(EStateType.Move);
                return;
            }
        }
        protected override void Exit()
        {

        }
    }
}
