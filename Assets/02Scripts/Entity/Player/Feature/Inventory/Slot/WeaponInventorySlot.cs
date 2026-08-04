using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class WeaponInventorySlot : InventorySlot
    {
        public EWeaponType WeaponType { get; }

        public WeaponInventorySlot(int p_index, EWeaponType p_weaponType) : base(p_index)
        {
            WeaponType = p_weaponType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is WeaponDTO weapon &&
                   weapon.WeaponType == WeaponType;
        }
    }
}
