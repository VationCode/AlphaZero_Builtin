using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // 이동 모드 내부 State의 생명주기와 전환 순서를 공통으로 관리한다.
    public abstract class StateFlowBase
    {
        protected readonly PlayerCore _Core;
        protected TransitionRule _Rule;

        // 전달받은 값으로 초기 상태를 구성한다.
        protected StateFlowBase(PlayerCore p_core)
        {
            _Core = p_core;
            _Rule = new TransitionRule();
        }

        public StateBase CurrentState { get; private set; }

        // 이동 모드 진입 시 지정된 시작 State를 활성화한다.
        internal void EnterFlow(ELocoStateType p_entryState)
        {
            ChangeState(p_entryState);
        }

        // State 타입을 현재 Flow가 소유한 인스턴스로 변환해 전환한다.
        internal void ChangeState(ELocoStateType p_nextStateType)
        {
            StateBase nextState = GetState(p_nextStateType);
            ChangeState(nextState);
        }

        // 이전 State 종료 → Context 갱신 → 새 State 진입 순서로 전환한다.
        private void ChangeState(StateBase p_nextState)
        {
            if (p_nextState == null ||
                ReferenceEquals(CurrentState, p_nextState))
            {
                return;
            }

            Debug.Log($"[GroundState] " + $"{CurrentState?.Type.ToString() ?? "None"} → {p_nextState.Type}");

            // 기존 State를 먼저 종료한 뒤 새 상태를 확정한다.
            CurrentState?.ExitState();

            CurrentState = p_nextState;

            // 상태 확정 후 Context와 View 구독자에게 알린다.
            _Core.LocomotionContext.SetCurrentState(CurrentState.Type);

            CurrentState.EnterState();
        }
        // 현재 활성 State의 프레임 갱신을 실행한다.
        internal void TickFlow()
        {
            CurrentState?.TickState();
        }

        // 이동 모드를 나가며 현재 State를 종료하고 참조를 비운다.
        internal void ExitFlow()
        {
            CurrentState?.ExitState();
            CurrentState = null;
        }

        // 각 Flow가 자신의 Dictionary에서 State를 반환한다.
        protected abstract StateBase GetState(ELocoStateType p_stateType);
        // 현재 State에서 다른 이동 모드로 전환할 수 있는지 판정한다.
        internal abstract bool CanChangeMode(out ELocomotionMode p_nextMode, out ELocoStateType p_entryState);
    }
}
