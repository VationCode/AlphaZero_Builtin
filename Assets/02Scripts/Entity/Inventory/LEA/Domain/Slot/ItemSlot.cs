// 타입 필요없는 일반 슬롯
public class ItemSlot : SlotBase
{
    public override bool SetItem(ItemDTO p_item, int p_count)
    {
        return base.SetItem(p_item, p_count);
    }
}
