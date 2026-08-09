using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Pending 무기를 실제로 적용하고 교체 연출 완료까지 대기한다.
    public class WeaponSwapState : CombatStateBase
    {
        public override ECombatStateType Type => ECombatStateType.WeaponSwap;
        private const float SwapDuration = 0.25f;

        private float _remainingTime;


        public WeaponSwapState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        protected override void Enter()
        {
            if (!_Context.HasPendingWeaponChange)
            {
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            WeaponDTO pendingWeapon = _Context.PendingWeapon;

            // 실제 무기 생성 또는 해제를 먼저 실행한다.
            if (!_Core.CombatModule.ApplyWeaponChange(pendingWeapon))
            {
                Debug.LogError("Pending 무기를 실제 무기로 적용하지 못했습니다.");

                _Context.ClearPendingWeaponChange();
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            ApplyWeaponAnimation();

            // 적용 완료 후에만 Pending 요청을 제거한다.
            _Context.ClearPendingWeaponChange();

            _remainingTime = SwapDuration;
            _Core.AnimationView?.PlayWeaponSwap();
        }

        protected override void Tick()
        {
            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
                TryChangeState(ECombatStateType.Idle);
        }

        // 상태 종료 시 교체 연출의 임시 시간만 초기화한다.
        protected override void Exit()
        {
            _remainingTime = 0f;
        }

        // 현재 런타임 무기에 맞는 Player 애니메이터를 구성한다.
        private void ApplyWeaponAnimation()
        {
            Weapon currentWeapon =
                _Core.CombatModule.CurrentWeapon;

            EWeaponType currentType =
                currentWeapon?.Data?.WeaponType ??
                EWeaponType.None;

            _Core.AnimationView?
                .ApplyWeaponOverrideController(currentType);

            if (currentWeapon is MeleeWeapon meleeWeapon)
            {
                _Core.AnimationView?
                    .ApplyMeleeWeapon(
                        meleeWeapon.ComboClips,
                        meleeWeapon.SecondaryClip);
            }
        }
    }
}
