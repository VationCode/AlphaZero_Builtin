namespace Alpha.Player.Combat
{
    // 별도의 Combat 행동을 수행하지 않는 기본 대기 상태
    public class CombatIdleState : CombatStateBase
    {
        public override ECombatStateType Type => ECombatStateType.Idle;

        // 전달받은 값으로 초기 상태를 구성한다.
        public CombatIdleState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
            // 이전 Combat 행동의 요청 정보를 정리한다.
            _Context.ClearPendingWeapon();
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            // 입력이 없거나 다른 행동이 전투를 막으면 Idle을 유지한다.
            if (_Input == null || _Core.BlockCombat)
                return;

            // 무기 교체 입력을 우선 처리한다.
            if (_Input.IsSwapInput)
            {
                if (TryRequestWeaponSwap(_Input.SwapNum))
                    TryChangeState(ECombatStateType.WeaponSwap);

                return;
            }

        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
        }
    }
}
