using System.Collections.Generic;
using UnityEngine;
namespace Alpha.Player
{
    public class FlightLocomotionStateFlow : ILocomotionStateFlow
    {
        private readonly Dictionary<EFlightState, ILocomotionState> _states;
        public ELocomotionMode Mode => ELocomotionMode.Flight;

        private ILocomotionState _currentState;
        private bool _hasCurrentState;

        public EFlightState CurrentStateType { get; private set; }
        public LocomotionModeFlow ModeFlow { get; }
        public FlightLocomotionStateFlow(PlayerCore p_core, LocomotionModeFlow p_modeFlow)
        {
            ModeFlow = p_modeFlow;

            _states = new Dictionary<EFlightState, ILocomotionState>
            {
                { EFlightState.Ascend, new FlightAscendState(p_core, this) },
                { EFlightState.FlightMove, new FlightMoveState(p_core, this) },
                { EFlightState.Fall, new FlightFallState(p_core, this) },
                { EFlightState.Dash, new FlightDashState(p_core, this) }
            };
        }
        public void Enter(EFlightState p_entryState)
        {
            ChangeState(p_entryState);
        }

        public void Tick()
        {
            _currentState?.Tick();
        }

        public void ChangeState(EFlightState p_nextState)
        {
            if (_hasCurrentState && CurrentStateType == p_nextState)
            {
                return;
            }

            _currentState?.Exit();

            CurrentStateType = p_nextState;
            _currentState = _states[p_nextState];
            _hasCurrentState = true;

            _currentState.Enter();
        }

        public void Exit()
        {
            _currentState?.Exit();

            _currentState = null;
            _hasCurrentState = false;
        }
    }
}
