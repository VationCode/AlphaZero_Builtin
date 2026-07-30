using UnityEngine;

namespace Alpha.Player.Combat
{
    // Combat State들이 공유하는 현재 상태와 요청 정보
    public class CombatContext
    {
        public ECombatStateType CurrentState { get; internal set; } = ECombatStateType.Idle;

        public EWeaponType PendingWeaponType { get; internal set; } = EWeaponType.None;
        public AttackDefinition BasicAttack { get; private set; }
        public AttackDefinition ActiveAttack { get; private set; }

        public bool IsBusy => CurrentState != ECombatStateType.Idle;
        public bool IsAiming { get; private set; }

        // Aim 또는 Attack 중이면 전투 방향 회전이 필요한 상태다.
        public bool IsCombatFacing => IsAiming || CurrentState == ECombatStateType.Attack;

        /// <summary>
        /// PendingWeaponType : Swap 입력으로 선택된 장비 슬롯 종류
        /// </summary>
        public bool HasPendingWeapon => PendingWeaponType != EWeaponType.None;
        public Vector3 AimDirection { get; private set; }

        public bool HasAimDirection => AimDirection.sqrMagnitude > 0.0001f;

        // Attack 
        public bool HasActiveAttack => ActiveAttack != null;

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
            AimDirection = p_direction.sqrMagnitude > 0.0001f? p_direction.normalized : Vector3.zero;
        }
        internal void ClearAimDirection()
        {
            AimDirection = Vector3.zero;
        }

        // 무기를 변경할 때 해당 무기의 기본 공격도 교체한다.
        internal void SetBasicAttack(AttackDefinition p_attack)
        {
            BasicAttack = p_attack;
            ActiveAttack = null;
        }

        internal bool TryActivateBasicAttack()
        {
            if (BasicAttack == null)
                return false;

            ActiveAttack = BasicAttack;
            return true;
        }

        // 이후 Skill 공격도 같은 경로로 등록할 수 있다.
        internal bool TrySetActiveAttack(AttackDefinition p_attack)
        {
            if (p_attack == null)
                return false;

            ActiveAttack = p_attack;
            return true;
        }

        internal void ClearActiveAttack()
        {
            ActiveAttack = null;
        }
    }
}
