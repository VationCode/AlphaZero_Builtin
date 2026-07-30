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

        protected CombatStateBase(PlayerCore p_core, CombatFlow p_flow)
        {
            _Core = p_core;
            _Flow = p_flow;
        }

        internal void EnterState() => Enter();
        internal void TickState() => Tick();
        internal void ExitState() => Exit();

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
        protected bool TryExecutePendingWeaponSwap()
        {
            return _Flow.TryExecutePendingWeaponSwap();
        }

        protected bool CanStartAttack()
        {
            return _Flow.CanStartAttack();
        }

        protected bool TryPrepareBasicAttack()
        {
            return _Flow.TryPrepareBasicAttack();
        }

        protected abstract void Enter();
        protected abstract void Tick();
        protected abstract void Exit();
    }
}
