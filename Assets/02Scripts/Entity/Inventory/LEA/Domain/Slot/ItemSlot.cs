// 타입 필요없는 일반 슬롯
public class ItemSlot : SlotBase
{
    public EItemType ItemType { get; }

    public ItemSlot(EItemType p_itemType)
    {
        ItemType = p_itemType;
    }
    public override bool CanStore(ItemDTO p_item)
    {
        if (p_item == null)
            return false;

        if (p_item.ItemType == EItemType.Weapon ||
            p_item.ItemType == EItemType.Armor)
            return false;

        return ItemType == p_item.ItemType;
    }
}
