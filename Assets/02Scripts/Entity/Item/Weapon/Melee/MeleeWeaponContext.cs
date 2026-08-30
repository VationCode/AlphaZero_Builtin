using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // MeleeWeapon이 공격에 필요한 사용자 값만 내부 상태로 소유한다.
    internal sealed class MeleeWeaponContext
    {
        private MeleeWeaponUseContext _useContext;

        public bool HasUser => _useContext.IsValid;
        public Transform Attacker => _useContext.Attacker;
        public Transform AttackSource => _useContext.AttackSource;
        public float AdditionalDamage => _useContext.AdditionalDamage;

        public bool BindUser(in MeleeWeaponUseContext p_context)
        {
            if (!p_context.IsValid)
                return false;

            _useContext = p_context;
            return true;
        }

        public void ClearUser()
        {
            _useContext = default;
        }
    }
}
