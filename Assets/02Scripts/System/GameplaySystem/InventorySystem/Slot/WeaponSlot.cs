using UnityEngine;

public class WeaponSlot : SlotBase
{
    public EWeaponType WeaponType { get; }
    public WeaponSlot(EWeaponType weaponType)
    {
        WeaponType = weaponType;
    }
    public override bool SetItem(ItemDTO p_item, int p_count = 1)
    {
        if (p_item is not WeaponDTO weapon)
            return false;

        if (weapon.WeaponType != WeaponType)
            return false;

        return base.SetItem(p_item, 1);
    }
}
