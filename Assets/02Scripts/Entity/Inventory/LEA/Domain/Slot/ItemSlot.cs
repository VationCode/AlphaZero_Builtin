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
        return p_item != null && p_item.ItemType == ItemType;
    }
}
