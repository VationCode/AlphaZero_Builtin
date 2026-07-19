using UnityEngine;

public class WeaponInventoryModule : InventoryModuleBase
{
    public WeaponInventoryModule(int p_slotCountPerType)
    {
        AddSlots(EWeaponType.Melee, p_slotCountPerType);

        AddSlots(EWeaponType.Range, p_slotCountPerType);

        AddSlots(EWeaponType.Special, p_slotCountPerType);
    }

    private void AddSlots(EWeaponType p_weaponType, int p_count)
    {
        for (int i = 0; i < p_count; i++)
        {
            AddSlot(
                new WeaponSlot(p_weaponType));
        }
    }
}
