namespace Alpha.Player.Equipment
{
    public class WeaponEquipmentSlot : EquipmentSlot
    {
        public WeaponEquipmentSlot(EWeaponType p_weaponType)
        {
            WeaponType = p_weaponType;
        }

        public EWeaponType WeaponType { get; }
        public WeaponDTO Weapon => Item as WeaponDTO;

        public override bool CanEquip(ItemDTO p_item)
        {
            return p_item is WeaponDTO weapon &&
                   weapon.WeaponType == WeaponType;
        }
    }
}
