using UnityEngine;

public class WeaponSlot : SlotBase
{
    public EWeaponType WeaponType { get; }

    public WeaponSlot(EWeaponType p_weaponType)
    {
        WeaponType = p_weaponType;
    }

    public override bool CanStore(ItemDTO p_item)
    {
        if (p_item is not WeaponDTO weapon) return false;

        return weapon.WeaponType == WeaponType;
    }
}
