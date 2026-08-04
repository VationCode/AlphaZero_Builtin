using UnityEngine;

namespace Alpha.Player.Combat
{
    // 선택된 장비 무기로 교체하고 애니메이션 종료까지 대기한다.
    public class WeaponSwapState : CombatStateBase
    {
        public override ECombatStateType Type => ECombatStateType.WeaponSwap;

        public WeaponSwapState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        private const float SwapDuration = 0.25f;

        private float _remainingTime;
        private bool _isSwapStarted;

        protected override void Enter()
        {

            // Swap 애니메이션
            _Core.AnimationView?.PlayWeaponSwap();
            _remainingTime = SwapDuration;
        }

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

        protected override void Exit()
        {
            _isSwapStarted = false;
            _remainingTime = 0f;
            _Context.ClearPendingWeapon();
        }
    }
}
