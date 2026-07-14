using UnityEngine;

public class WeaponInventoryModule : InventoryModuleBase
{
    public WeaponInventoryModule(int p_slotCountPerType)
    {
        if (p_slotCountPerType <= 0)
            p_slotCountPerType = 1;

        ExpandSlots(EWeaponType.Melee, p_slotCountPerType);
        ExpandSlots(EWeaponType.Range, p_slotCountPerType);
        ExpandSlots(EWeaponType.Special, p_slotCountPerType);
    }

    public void ExpandSlots(EWeaponType p_weaponType, int p_count)
    {
        if (p_weaponType == EWeaponType.None)
            return;

        if (p_count <= 0)
            return;

        for (int i = 0; i < p_count; i++)
        {
            AddSlot(new WeaponSlot(p_weaponType));
        }
    }

    protected override bool CanStore(ItemDTO p_item)
    {
        return p_item is WeaponDTO;
    }
}
