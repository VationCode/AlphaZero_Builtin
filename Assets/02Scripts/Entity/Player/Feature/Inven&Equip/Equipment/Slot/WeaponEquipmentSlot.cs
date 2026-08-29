namespace Alpha.Player.Equipment
{
    // WeaponEquipmentSlot 상태와 아이템 수용 규칙을 관리한다.
    public class WeaponEquipmentSlot : EquipmentSlot
    {
        // 전달받은 값으로 초기 상태를 구성한다.
        public WeaponEquipmentSlot(EWeaponCategory p_weaponCategory)
        {
            WeaponCategory = p_weaponCategory;
        }

        public EWeaponCategory WeaponCategory { get; }
        public WeaponDTO Weapon => Item as WeaponDTO;

        // CanEquip 실행 가능 조건을 검사한다.
        public override bool CanEquip(ItemDTO p_item)
        {
            return p_item is WeaponDTO weapon &&
                   weapon.WeaponCategory == WeaponCategory;
        }
    }
}
