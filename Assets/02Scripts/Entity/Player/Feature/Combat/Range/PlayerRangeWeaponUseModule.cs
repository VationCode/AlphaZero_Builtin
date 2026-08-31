using Alpha.Item.Weapon.Range;
using System;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 공격 출처와 조준 결과만 현재 RangeWeapon에 전달한다.
    [Serializable]
    public sealed class PlayerRangeWeaponUseModule
    {
        private Transform _attacker;
        private RangeAimModule _aimModule;
        private RangeWeapon _activeWeapon;

        public bool Bind(
            Transform p_attacker,
            RangeAimModule p_aimModule)
        {
            if (p_attacker == null || p_aimModule == null)
                return false;

            UnbindCurrentWeapon();
            _attacker = p_attacker;
            _aimModule = p_aimModule;
            return true;
        }

        public bool TryBindWeapon(
            RangeWeapon p_weapon,
            float p_additionalDamage)
        {
            if (p_weapon == null ||
                !p_weapon.IsInitialized ||
                p_weapon.AttackType == ERangeAttackType.None ||
                _attacker == null ||
                _aimModule == null)
            {
                return false;
            }

            UnbindCurrentWeapon();

            RangeWeaponUseContext useContext = new(
                _attacker,
                p_additionalDamage);

            if (!p_weapon.BindUseContext(useContext))
                return false;

            _activeWeapon = p_weapon;
            RefreshAttackPose();
            return true;
        }

        public void UnbindCurrentWeapon()
        {
            if (_activeWeapon != null)
                _activeWeapon.UnbindUseContext();

            _activeWeapon = null;
        }

        public bool IsBoundTo(RangeWeapon p_weapon)
        {
            return p_weapon != null &&
                   ReferenceEquals(_activeWeapon, p_weapon) &&
                   p_weapon.HasUseContext;
        }

        // Player의 Camera 조준 결과를 현재 Weapon의 값 객체로 갱신한다.
        public bool RefreshAttackPose()
        {
            RangeWeapon weapon = _activeWeapon;

            if (weapon == null ||
                _aimModule == null ||
                weapon.Muzzle == null ||
                !_aimModule.TryResolveAttackPose(
                    weapon.Muzzle.position,
                    weapon.MaxDistance,
                    out Vector3 origin,
                    out Vector3 targetPoint))
            {
                weapon?.ClearAttackPose();
                return false;
            }

            if ((targetPoint - origin).sqrMagnitude <= 0.0001f)
            {
                weapon.ClearAttackPose();
                return false;
            }

            return weapon.SetAttackPose(
                new RangeWeaponAttackPose(origin, targetPoint));
        }

        public bool TryGetAttackPose(
            RangeWeapon p_weapon,
            out Vector3 p_origin,
            out Vector3 p_direction)
        {
            p_origin = Vector3.zero;
            p_direction = Vector3.zero;

            return IsBoundTo(p_weapon) &&
                   RefreshAttackPose() &&
                   p_weapon.TryGetAttackPose(
                       out p_origin,
                       out p_direction);
        }

    }
}
