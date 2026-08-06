using UnityEngine;

namespace Alpha.Player.Combat
{
    // WeaponActionState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class WeaponActionState : CombatStateBase
    {
        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponActionState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        public override ECombatStateType Type => ECombatStateType.WeaponAction;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {
            throw new System.NotImplementedException();
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
            throw new System.NotImplementedException();
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}