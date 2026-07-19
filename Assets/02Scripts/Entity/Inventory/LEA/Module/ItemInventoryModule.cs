using UnityEngine;

public class ItemInventoryModule : InventoryModuleBase
{
    public EItemType ItemType { get; }

    public ItemInventoryModule(EItemType p_itemType, int p_slotCount)
    {
        ItemType = p_itemType;

        for (int i = 0; i < p_slotCount; i++)
        {
            AddSlot(new ItemSlot(p_itemType));
        }
    }
}
