using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player
{
    public class GroundLocomotionStateFlow : ILocomotionStateFlow
    {
        private readonly GroundLocomotionModule _module;
        private readonly Dictionary<EGroundState,ILocomotionState> _states;
        public ELocomotionMode Mode => ELocomotionMode.Ground;

        private ILocomotionState _currentState;
        private bool _hasCurrentState;

        public EGroundState CurrentStateType {get; private set;}
        public LocomotionModeFlow ModeFlow {get;}

        public GroundLocomotionStateFlow(PlayerCore p_core, LocomotionModeFlow p_modeFlow)
        {
            ModeFlow = p_modeFlow;

            _states = new Dictionary<EGroundState,ILocomotionState>
            {
                { EGroundState.GroundMove, new GroundMoveState(p_core, this) },
                { EGroundState.Jump, new GroundJumpState(p_core, this) },
                { EGroundState.Fall, new GroundFallState(p_core, this) },
                { EGroundState.Land, new GroundLandState(p_core, this) },
                { EGroundState.Dash, new GroundDashState(p_core, this) }
            };
        }

        public void Enter(EGroundState p_entryState)
        {
            ChangeState(p_entryState);
        }

        public void Tick()
        {
            // 모든 Ground 상태에서 접지와 중력을 갱신한다.
            _module.UpdateEnvironment(Time.deltaTime);

            _currentState?.Tick();
        }

        public void ChangeState(EGroundState p_nextState)
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
