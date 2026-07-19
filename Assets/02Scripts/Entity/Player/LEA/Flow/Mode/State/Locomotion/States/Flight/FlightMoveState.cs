namespace Alpha.Player
{
    public class FlightMoveState : FlightLocomotionState
    {
        public FlightMoveState(PlayerCore p_core, FlightLocomotionStateFlow p_flow) : base(p_core, p_flow){}

        public override void Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}
