using Alpha.Player.Locomotion;

namespace Alpha.Player
{
    public abstract class StateBase
    {
        protected PlayerCore _Core;
        protected readonly StateFlowBase _StateFlow;
        protected AlphaInputSystem _Input => _Core.Input;
        
        public abstract ELocoStateType Type { get; }
        protected StateBase(PlayerCore p_core, StateFlowBase p_stateFlow)
        {
            _Core = p_core;
            _StateFlow = p_stateFlow;
        }

        // StateFlow만 호출하는 생명주기
        internal void EnterState() => Enter();
        internal void TickState() => Tick();
        internal void ExitState() => Exit();

        protected abstract void Enter();
        protected abstract void Tick();
        protected abstract void Exit();
    }
}