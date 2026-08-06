namespace Alpha.Player.Combat
{
    // 모든 Combat State가 공유하는 생명주기와 접근점을 정의한다.
    public abstract  class CombatStateBase
    {
        protected readonly PlayerCore _Core;

        protected AlphaInputSystem _Input => _Core.Input;
        protected CombatContext _Context => _Core.CombatContext;

        private readonly CombatFlow _Flow;

        public abstract ECombatStateType Type { get; }

        // 전달받은 값으로 초기 상태를 구성한다.
        protected CombatStateBase(PlayerCore p_core)
        {
            _Core = p_core;
            _Flow = p_core.CombatFlow;
        }

        // 상태 진입 생명주기를 실행한다.
        internal void EnterState() => Enter();
        // 상태 갱신 생명주기를 실행한다.
        internal void TickState() => Tick();
        // 상태 종료 생명주기를 실행한다.
        internal void ExitState() => Exit();

        // TryChangeState 조건을 검사하고 성공 여부와 결과를 반환한다.
        protected bool TryChangeState(
            ECombatStateType p_nextState)
        {
            return _Flow.TryChangeState(p_nextState);
        }

        // 장착 무기 조회와 교체 가능 여부 판단은 CombatFlow에 위임한다.
        protected bool TryRequestWeaponSwap(int p_slotIndex)
        {
            return _Flow.TryRequestWeaponSwap(p_slotIndex);
        }

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected abstract void Enter();
        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected abstract void Tick();
        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected abstract void Exit();
    }
}
