using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
using Alpha.Item.Weapon.View;
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

            // Swap 애니메이션 동안에는 양손을 Animation Clip이 온전히 제어한다.
            _Core.RigView?
                .SetHandIKSuppressed(
                    true,
                    true);

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

            // 새 무기의 LeftHandAttach로 왼손을 부드럽게 복원한다.
            _Core.RigView?
                .SetHandIKSuppressed(false);
        }

        // 현재 런타임 무기에 맞는 Player 애니메이터를 구성한다.
        private void ApplyWeaponAnimation()
        {
            Weapon currentWeapon =
                _Core.CombatModule.CurrentWeapon;

            EWeaponCategory currentCategory =
                currentWeapon?.Data?.WeaponCategory ??
                EWeaponCategory.None;

            if (currentWeapon is MeleeWeapon meleeWeapon)
            {
                _Core.AnimationView?
                    .ApplyMeleeWeapon(
                        meleeWeapon.AnimatorOverrideController);
            }
            else
            {
                _Core.AnimationView?
                    .ApplyWeaponOverrideController(currentCategory);
            }

            // 오른손이 무기 기준이므로 Range의 왼손 지지점만 IK Target으로 연결한다.
            if (currentWeapon is RangeWeapon rangeWeapon)
            {
                _Core.RigView?
                    .SetLeftHandIKTarget(
                        rangeWeapon.LeftHandIKTarget);

            }
            else
            {
                _Core.RigView?
                    .ClearLeftHandIKTarget();
            }

            // Effect와 Audio의 구체 종류를 알지 않고 모든 무기 View를 연결한다.
            WeaponView[] weaponViews =
                currentWeapon?.GetComponentsInChildren<WeaponView>(true);

            if (weaponViews != null)
            {
                foreach (WeaponView weaponView in weaponViews)
                    weaponView.BindCamera(_Core.CameraCore);
            }

            _Core.RigView?.RefreshAnimatorLayer();
        }
    }
}
