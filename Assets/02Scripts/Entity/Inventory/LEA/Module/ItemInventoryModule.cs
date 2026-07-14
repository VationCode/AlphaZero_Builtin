using UnityEngine;

public class ItemInventoryModule : InventoryModuleBase
{
    public EItemType ItemType { get; }

    public ItemInventoryModule(EItemType p_itemType, int p_slotCount)
    {
        ItemType = p_itemType;

        if (p_slotCount <= 0)
            p_slotCount = 1;

        ExpandSlots(p_slotCount);
    }

    public void ExpandSlots(int p_count)
    {
        if (p_count <= 0)
            return;

        for (int i = 0; i < p_count; i++)
        {
            AddSlot(new ItemSlot(ItemType));
        }
    }

    protected override bool CanStore(ItemDTO p_item)
    {
        if (p_item == null)
            return false;

        if (ItemType == EItemType.None ||
            ItemType == EItemType.Weapon ||
            ItemType == EItemType.Armor ||
            ItemType != p_item.ItemType)
            return false;

        return p_item.ItemType == ItemType;
    }
}
