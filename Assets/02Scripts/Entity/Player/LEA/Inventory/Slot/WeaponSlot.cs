namespace Alpha.Player.Inventory
{
    public class WeaponSlot : SlotBase
    {
        public EWeaponType WeaponType { get; }

        public WeaponSlot(EWeaponType p_weaponType)
        {
            WeaponType = p_weaponType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is WeaponDTO weapon && weapon.WeaponType == WeaponType;
        }
    }
}