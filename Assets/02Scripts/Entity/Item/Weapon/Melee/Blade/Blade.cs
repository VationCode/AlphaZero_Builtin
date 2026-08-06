using UnityEngine;

namespace Alpha.Item.Weapon.Melee.Blade
{
    // MeleeWeaponDTO만 허용하는 근접 Blade 무기 구현이다.
    public class Blade : Weapon
    {
        // CanInitialize 실행 가능 조건을 검사한다.
        protected override bool CanInitialize(WeaponDTO p_data)
        {
            // Blade는 Melee 타입 데이터만 사용한다.
            return p_data is MeleeWeaponDTO;
        }
    }
}
