using UnityEngine;

namespace Alpha.Player.Combat
{
    // 선택된 장비 무기로 교체하고 애니메이션 종료까지 대기한다.
    public class WeaponSwapState : CombatStateBase
    {
        public override ECombatStateType Type => ECombatStateType.WeaponSwap;

        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponSwapState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        private const float SwapDuration = 0.25f;

        private float _remainingTime;
        private bool _isSwapStarted;

        // 상태 진입 시 필요한 값을 초기화하고 동작을 시작한다.
        protected override void Enter()
        {

            // Swap 애니메이션
            _Core.AnimationView?.PlayWeaponSwap();
            _remainingTime = SwapDuration;
        }

        // 현재 상태의 입력과 전환 조건을 매 프레임 처리한다.
        protected override void Tick()
        {
            if (!_isSwapStarted)
            {
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
                TryChangeState(ECombatStateType.Idle);
        }

        // 상태 종료 시 임시 값과 동작을 정리한다.
        protected override void Exit()
        {
            _isSwapStarted = false;
            _remainingTime = 0f;
            _Context.ClearPendingWeapon();
        }
    }
}
