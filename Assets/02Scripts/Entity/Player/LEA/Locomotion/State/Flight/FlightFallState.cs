
namespace Alpha.Player.Locomotion
{
    public class FlightFallState : StateBase
    {
        public FlightFallState(PlayerCore p_core, StateFlowBase p_stateFlow) : base(p_core, p_stateFlow) { }

        public override EStateType Type => EStateType.Fall;

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
