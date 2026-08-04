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

        internal void ClearPendingWeapon()
        {
            PendingWeaponType = EWeaponType.None;
        }

        internal void SetAiming(bool p_isAiming)
        {
            IsAiming = p_isAiming;
        }

        internal void SetAimDirection(Vector3 p_direction)
        {
            AimDirection = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }

        internal void ClearAimDirection()
        {
            AimDirection = Vector3.zero;
        }

        internal void SetCurrentState(ECombatStateType p_state)
        {
            CurrentState = p_state;
            OnStateChanged?.Invoke(p_state);
        }
    }
}
