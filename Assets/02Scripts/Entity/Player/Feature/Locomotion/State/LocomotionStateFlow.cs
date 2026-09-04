using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // 모든 Locomotion Mode가 공유하는 State의 생명주기와 전환 순서를 관리한다.
    public sealed class LocomotionStateFlow
    {
        private readonly PlayerCore _core;
        private readonly Dictionary<ELocoStateType, StateBase> _states;

        public StateBase CurrentState { get; private set; }

        public LocomotionStateFlow(PlayerCore p_core)
        {
            _core = p_core;
            _states = new Dictionary<ELocoStateType, StateBase>
            {
                { ELocoStateType.Move, new MoveState(p_core, this) },
                { ELocoStateType.Jump, new JumpState(p_core, this) },
                { ELocoStateType.Fall, new FallState(p_core, this) },
                { ELocoStateType.Land, new LandState(p_core, this) },
                { ELocoStateType.Dash, new DashState(p_core, this) },
                { ELocoStateType.Dodge, new DodgeState(p_core, this) }
            };
        }

        internal void EnterFlow(ELocoStateType p_entryState)
        {
            ChangeState(p_entryState);
        }

        // 이전 State 종료 후 Context를 갱신하고 새 State를 시작한다.
        internal bool ChangeState(ELocoStateType p_nextStateType)
        {
            if (!_states.TryGetValue(p_nextStateType, out StateBase nextState))
            {
                Debug.LogWarning($"[LocomotionState] 등록되지 않은 State: {p_nextStateType}");
                return false;
            }

            if (ReferenceEquals(CurrentState, nextState))
                return false;

            CurrentState?.ExitState();
            CurrentState = nextState;

            _core.LocomotionContext.SetCurrentState(CurrentState.Type);
            CurrentState.EnterState();
            return true;
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
    }
}
