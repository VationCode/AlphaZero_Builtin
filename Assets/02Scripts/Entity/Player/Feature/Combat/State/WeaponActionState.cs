using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // WeaponActionState 상태의 진입, 갱신, 종료 동작을 담당한다.
    public class WeaponActionState : CombatStateBase
    {
        private bool _isMeleePrimaryAction;
        private int _playedComboIndex = -1;

        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponActionState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        public override ECombatStateType Type => ECombatStateType.WeaponAction;


        protected override void Enter()
        {
            if (!_Context.HasPendingWeaponAction)
            {
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            EWeaponActionType actionType =
                _Context.PendingWeaponActionType;

            _Context.ClearPendingWeaponAction();

            // 저장된 행동을 현재 무기에서 시작하지 못하면 Idle로 복귀한다.
            if (!_Core.CombatModule.TryBeginWeaponAction(actionType))
            {
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            _isMeleePrimaryAction =
                actionType == EWeaponActionType.Primary &&
                _Core.CombatModule.CurrentWeapon is MeleeWeapon;

            if (_isMeleePrimaryAction)
            {
                if (!_Core.LocomotionModule.BeginRootMotion(ERootMotionMode.Ground))
                {
                    _Core.CombatModule.CancelWeaponAction();
                    TryChangeState(ECombatStateType.Idle);
                    return;
                }
            }

            _playedComboIndex = -1;
            PlayCurrentMeleeCombo();
        }

        protected override void Tick()
        {
            CombatModule module = _Core.CombatModule;

            if (_Core.BlockCombat || !module.HasActiveAction)
            {
                module.CancelWeaponAction();
                TryChangeState(ECombatStateType.Idle);
                return;
            }

            bool isInputHeld = module.ActiveActionType switch
            {
                EWeaponActionType.Primary => _Input.IsPrimaryAction,
                EWeaponActionType.Secondary => _Input.IsSecondaryAction,
                _ => false
            };

            bool isInputPressed = module.ActiveActionType switch
            {
                EWeaponActionType.Primary => _Input.IsPrimaryActionPressed,
                EWeaponActionType.Secondary => _Input.IsSecondaryActionPressed,
                _ => false
            };

            // State는 입력 상태만 전달하고, 사용 방식과 종료 시점은 Weapon이 판단한다.
            module.TickWeaponAction(
                isInputHeld,
                isInputPressed,
                Time.deltaTime);
            PlayCurrentMeleeCombo();

            // Melee Primary처럼 무기 내부에서 완료한 행동은 Idle로 복귀한다.
            if (!module.HasActiveAction)
                TryChangeState(ECombatStateType.Idle);
        }

        protected override void Exit()
        {
            // 다른 상태에 의해 강제 전환됐을 때 진행 중인 행동을 정리한다.
            _Core.CombatModule.CancelWeaponAction();

            if (_isMeleePrimaryAction)
            {
                _Core.LocomotionModule.EndRootMotion();
                _Core.AnimationView?.StopMeleeAction();
            }

            _isMeleePrimaryAction = false;
            _playedComboIndex = -1;
        }

        // MeleeWeapon이 실제 다음 콤보로 전환했을 때 해당 전신 애니메이션을 재생한다.
        private void PlayCurrentMeleeCombo()
        {
            if (!_isMeleePrimaryAction ||
                !(_Core.CombatModule.CurrentWeapon
                    is MeleeWeapon meleeWeapon))
            {
                return;
            }

            int comboIndex = meleeWeapon.CurrentComboIndex;

            if (comboIndex < 0 ||
                comboIndex == _playedComboIndex)
            {
                return;
            }

            _Core.AnimationView.PlayMeleeCombo(comboIndex);
            _playedComboIndex = comboIndex;
        }

    }
}
