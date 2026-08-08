using Alpha.Item.Weapon;

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
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            // Pending 요청의 실행 가능 여부는 현재 Idle State가 판단한다.
            if (_Context.HasPendingWeaponChange && CanEnterWeaponSwap())
            {
                TryChangeState(ECombatStateType.WeaponSwap);
                return;
            }

            if (_Input == null || _Core.BlockCombat)
            {
                return;
            }

            // 숫자 키 무기 교체를 공격 입력보다 먼저 처리한다.
            if (_Input.IsSwapInput)
            {
                // 키 입력 무기교체 요청.
                _Flow.RequestKeyWeaponSwap(_Input.SwapNum);
                return;
            }

            // 현재 무기가 없더라도 위의 Swap 키로 무기를 꺼낼 수 있어야 한다.
            if (!_Core.CombatModule.HasWeapon)
                return;

            TryRequestWeaponAction();
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
        }

        // 현재 이동 상태에서 무기 교체가 가능한지 판단한다.
        private bool CanEnterWeaponSwap()
        {
            return _Core.LocomotionContext.CurrentState switch
            {
                ELocoStateType.Dash => false,
                ELocoStateType.Jump => false,
                ELocoStateType.Die => false,
                _ => true
            };
        }

        // 입력을 한 번만 해석해 공통 WeaponAction 요청으로 전달한다.
        private bool TryRequestWeaponAction()
        {
            EWeaponActionType actionType = EWeaponActionType.None;

            if (_Input.IsPrimaryActionPressed)
                actionType = EWeaponActionType.Primary;
            else if (_Input.IsSecondaryActionPressed)
                actionType = EWeaponActionType.Secondary;

            if (!_Flow.RequestWeaponAction(actionType))
                return false;

            return TryChangeState(ECombatStateType.WeaponAction);
        }
    }
}
