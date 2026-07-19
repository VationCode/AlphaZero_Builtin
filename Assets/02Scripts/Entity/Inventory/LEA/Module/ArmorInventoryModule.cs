using UnityEngine;

public class ArmorInventoryModule : InventoryModuleBase
{
    public ArmorInventoryModule(int p_slotCountPerType)
    {
        AddSlots(EArmorType.Helmet, p_slotCountPerType);

        AddSlots(EArmorType.Chest, p_slotCountPerType);

        AddSlots(EArmorType.Gloves, p_slotCountPerType);

        AddSlots(EArmorType.Boots, p_slotCountPerType);
    }

    private void AddSlots(EArmorType p_armorType, int p_count)
    {
        for (int i = 0; i < p_count; i++)
        {
            AddSlot(
                new ArmorSlot(p_armorType));
        }
    }
}
