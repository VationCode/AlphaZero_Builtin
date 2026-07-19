using UnityEngine;

namespace Alpha.Player
{
    public class LocomotionModeFlow : MonoBehaviour
    {
        private GroundLocomotionStateFlow _groundFlow;
        private FlightLocomotionStateFlow _flightFlow;
        private SwimLocomotionStateFlow _swimFlow;

        private ILocomotionStateFlow _currentFlow;

        public ELocomotionMode CurrentMode { get; private set; }

        public void Bind(PlayerCore p_core)
        {
            _groundFlow = new GroundLocomotionStateFlow(p_core, this);

            _flightFlow = new FlightLocomotionStateFlow(p_core, this);

            //_swimFlow = new SwimLocomotionStateFlow(p_core, this);

            EnterGround(EGroundState.GroundMove);
        }

        private void Update()
        {
            // 현재 Mode의 StateFlow만 실행한다.
            _currentFlow?.Tick();
        }

        public void EnterGround(EGroundState p_entryState)
        {
            if (ChangeMode(_groundFlow))
                _groundFlow.Enter(p_entryState);
            else
                _groundFlow.ChangeState(p_entryState);
        }

        public void EnterFlight(EFlightState p_entryState)
        {
            if (ChangeMode(_flightFlow))
                _flightFlow.Enter(p_entryState);
            else
                _flightFlow.ChangeState(p_entryState);
        }

        /*public void EnterSwim(ESwimState p_entryState)
        {
            if (ChangeMode(_swimFlow))
                _swimFlow.Enter(p_entryState);
            else
                _swimFlow.ChangeState(p_entryState);
        }*/

        private bool ChangeMode(ILocomotionStateFlow p_nextFlow)
        {
            if (_currentFlow == p_nextFlow)
                return false;

            _currentFlow?.Exit();

            _currentFlow = p_nextFlow;
            CurrentMode = p_nextFlow.Mode;

            return true;
        }
    }
}
