using System;
using Alpha.Item.Weapon;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 무기 교체와 조준 상태를 보관한다.
    public class CombatContext
    {
        public ECombatStateType CurrentState { get; internal set; } = ECombatStateType.Idle;

        // null도 무기 해제 요청이므로 별도 요청 여부가 필요하다.
        public bool HasPendingWeaponChange { get; private set; }

        // 장착할 무기. null이면 현재 무기 해제
        public WeaponDTO PendingWeapon { get; private set; }

        // Idle에서 접수하고 WeaponActionState가 소비할 행동 요청이다.
        public EWeaponActionType PendingWeaponActionType { get; private set; }
            = EWeaponActionType.None;

        public bool HasPendingWeaponAction =>
            PendingWeaponActionType != EWeaponActionType.None;

        public bool IsBusy => CurrentState != ECombatStateType.Idle;
        public bool IsAiming { get; private set; }

        public Vector3 AimDirection { get; private set; }
        public bool HasAimDirection => AimDirection.sqrMagnitude > 0.0001f;

        public event Action<ECombatStateType> OnStateChanged;

        // SetAiming 상태 값을 갱신한다.
        internal void SetAiming(bool p_isAiming)
        {
            IsAiming = p_isAiming;
        }

        // SetAimDirection 상태 값을 갱신한다.
        internal void SetAimDirection(Vector3 p_direction)
        {
            AimDirection = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }

        // ClearAimDirection 상태를 초기값으로 비운다.
        internal void ClearAimDirection()
        {
            AimDirection = Vector3.zero;
        }

        // SetCurrentState 상태 값을 갱신한다.
        internal void SetCurrentState(ECombatStateType p_state)
        {
            CurrentState = p_state;
            OnStateChanged?.Invoke(p_state);
        }

        internal void SetPendingWeaponChange(WeaponDTO p_weapon)
        {
            PendingWeapon = p_weapon;
            HasPendingWeaponChange = true;
        }

        internal void ClearPendingWeaponChange()
        {
            PendingWeapon = null;
            HasPendingWeaponChange = false;
        }

        // 실행할 무기 행동을 State 전환 전까지 보관한다.
        internal void SetPendingWeaponAction(EWeaponActionType p_actionType)
        {
            PendingWeaponActionType = p_actionType;
        }

        internal void ClearPendingWeaponAction()
        {
            PendingWeaponActionType = EWeaponActionType.None;
        }

    }
}
