using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // MeleeWeapon Prefab에서 선택할 수 있는 근접 무기 타입이다.
    public enum EMeleeWeaponType
    {
        None = (int)EWeaponType.None,
        Sword = (int)EWeaponType.Sword,
        Polearm = (int)EWeaponType.Polearm
    }

    // 근접 무기의 타입 식별만 제공하는 단순 무기 객체다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeapon : Weapon
    {
        [Header("Identity")]
        [SerializeField]
        private EMeleeWeaponType _weaponType = EMeleeWeaponType.None;

        public sealed override EWeaponType WeaponType =>
            (EWeaponType)_weaponType;
        public EMeleeWeaponType MeleeType => _weaponType;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data?.WeaponCategory == EWeaponCategory.Melee;
        }
    }
}
