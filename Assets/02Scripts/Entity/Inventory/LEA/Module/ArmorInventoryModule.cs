using UnityEngine;

public class ArmorInventoryModule : InventoryModuleBase
{
    public ArmorInventoryModule(int p_slotCountPerType)
    {
        if (p_slotCountPerType <= 0)
            p_slotCountPerType = 1;

        ExpandSlots(EArmorType.Helmet, p_slotCountPerType);
        ExpandSlots(EArmorType.Chest, p_slotCountPerType);
        ExpandSlots(EArmorType.Gloves, p_slotCountPerType);
        ExpandSlots(EArmorType.Boots, p_slotCountPerType);
    }

    public void ExpandSlots(EArmorType p_armorType, int p_count)
    {
        if (p_armorType == EArmorType.None)
            return;

        if (p_count <= 0)
            return;

        for (int i = 0; i < p_count; i++)
        {
            AddSlot(new ArmorSlot(p_armorType));
        }
    }

    protected override bool CanStore(ItemDTO p_item)
    {
        return p_item is ArmorDTO;
    }
}
