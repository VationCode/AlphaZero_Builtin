using UnityEngine;

namespace Alpha.Player.Locomotion
{
    public abstract class StateFlowBase
    {
        protected readonly PlayerCore _Core;
        protected TransitionRule _Rule;
        protected StateFlowBase(PlayerCore p_core)
        {
            _Core = p_core;
            _Rule = new TransitionRule();
        }

        public StateBase CurrentState { get; private set; }

        internal void EnterFlow(ELocoStateType p_entryState)
        {
            ChangeState(p_entryState);
        }

        internal void ChangeState(ELocoStateType p_nextStateType)
        {
            StateBase nextState = GetState(p_nextStateType);
            ChangeState(nextState);
        }

        private void ChangeState(StateBase p_nextState)
        {
            if (p_nextState == null ||
                ReferenceEquals(CurrentState, p_nextState))
            {
                return;
            }

            Debug.Log($"[GroundState] " + $"{CurrentState?.Type.ToString() ?? "None"} → {p_nextState.Type}");

            CurrentState?.ExitState();

            CurrentState = p_nextState;

            // Context에 현재 State 기록
            _Core.LocomotionContext.CurrentState = CurrentState.Type;

            CurrentState.EnterState();
        }
        internal void TickFlow()
        {
            CurrentState?.TickState();
        }

        internal void ExitFlow()
        {
            CurrentState?.ExitState();
            CurrentState = null;
        }

        // 각 Flow가 자신의 Dictionary에서 State를 반환한다.
        protected abstract StateBase GetState(ELocoStateType p_stateType);
        internal abstract bool CanChangeMode(out ELocomotionMode p_nextMode, out ELocoStateType p_entryState);
    }
}