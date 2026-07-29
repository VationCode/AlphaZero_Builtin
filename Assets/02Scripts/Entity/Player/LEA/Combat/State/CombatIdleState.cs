using Unity.VisualScripting;

namespace Alpha.Player.Combat
{
    // 별도의 Combat 행동을 수행하지 않는 기본 대기 상태
    public class CombatIdleState : CombatStateBase
    {
        public override ECombatStateType Type => ECombatStateType.Idle;

        public CombatIdleState(PlayerCore p_core, CombatFlow p_flow) : base(p_core, p_flow){}

        protected override void Enter()
        {
            // 이전 Combat 행동의 요청 정보를 정리한다.
            _Context.ClearPendingWeapon();
        }

        protected override void Tick()
        {
            if (_Input == null || _Core.BlockCombat)
                return;

            // 무기 교체 입력을 우선 처리한다.
            if (_Input.IsSwapInput)
            {
                if (TryRequestWeaponSwap(_Input.SwapNum))
                    TryChangeState(ECombatStateType.WeaponSwap);

                return;
            }

            if (!_Input.IsAttack || !CanStartAttack())
                return;

            TryChangeState(ECombatStateType.Attack);
        }

        protected override void Exit()
        {
        }
    }
}
