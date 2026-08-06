using System;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 무기 교체와 조준 상태를 보관한다.
    public class CombatContext
    {
        public ECombatStateType CurrentState { get; internal set; } = ECombatStateType.Idle;
        public EWeaponType PendingWeaponType { get; internal set; } = EWeaponType.None;

        public bool IsBusy => CurrentState != ECombatStateType.Idle;
        public bool IsAiming { get; private set; }
        public bool HasPendingWeapon => PendingWeaponType != EWeaponType.None;

        public Vector3 AimDirection { get; private set; }
        public bool HasAimDirection => AimDirection.sqrMagnitude > 0.0001f;

        public event Action<ECombatStateType> OnStateChanged;

        // ClearPendingWeapon 상태를 초기값으로 비운다.
        internal void ClearPendingWeapon()
        {
            PendingWeaponType = EWeaponType.None;
        }

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
    }
}
