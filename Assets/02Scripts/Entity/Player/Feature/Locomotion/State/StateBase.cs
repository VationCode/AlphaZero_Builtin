using Alpha.Player.Locomotion;

namespace Alpha.Player
{
    // Player 이동 상태가 공유하는 의존성과 진입·갱신·종료 생명주기를 정의한다.
    public abstract class StateBase
    {
        protected PlayerCore _Core;
        protected readonly LocomotionStateFlow _StateFlow;
        protected AlphaInputSystem _Input => _Core.Input;
        
        public abstract ELocoStateType Type { get; }
        // 상태가 사용할 PlayerCore와 소유 StateFlow를 보관한다.
        protected StateBase(PlayerCore p_core, LocomotionStateFlow p_stateFlow)
        {
            _Core = p_core;
            _StateFlow = p_stateFlow;
        }

        // StateFlow만 호출하는 생명주기
        internal void EnterState() => Enter();
        // 상태 갱신 생명주기를 실행한다.
        internal void TickState() => Tick();
        // 상태 종료 생명주기를 실행한다.
        internal void ExitState() => Exit();

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected abstract void Enter();
        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected abstract void Tick();
        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected abstract void Exit();
    }
}
