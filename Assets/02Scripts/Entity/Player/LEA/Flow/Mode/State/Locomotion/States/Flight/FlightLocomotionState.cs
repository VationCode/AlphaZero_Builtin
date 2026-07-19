namespace Alpha.Player
{
    public abstract class FlightLocomotionState : ILocomotionState
    {
        protected PlayerCore _Core { get; }
        protected FlightLocomotionStateFlow _Flow { get; }
        protected LocomotionModeFlow _ModeFlow => _Flow.ModeFlow;

        protected FlightLocomotionState(PlayerCore p_core, FlightLocomotionStateFlow p_flow)
        {
            _Core = p_core;
            _Flow = p_flow;
        }
        public virtual void Enter() { }

        public abstract void Tick();

        public virtual void Exit() { }
    }
}
