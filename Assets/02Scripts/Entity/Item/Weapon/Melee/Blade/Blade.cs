using UnityEngine;

namespace Alpha.Item.Weapon.Melee.Blade
{
    public class Blade : Weapon
    {
        protected override bool CanInitialize(WeaponDTO p_data)
        {
            // Blade는 Melee 타입 데이터만 사용한다.
            return p_data is MeleeWeaponDTO;
        }
    }
}