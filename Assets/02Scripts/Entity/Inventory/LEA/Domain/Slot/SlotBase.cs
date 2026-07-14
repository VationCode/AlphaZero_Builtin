// 아이템 데이터 보관
// 아이템 수량 보관
// 슬롯 초기화
public abstract class SlotBase
{
   public ItemDTO Item { get; private set; }
    public int Count { get; private set; }

    public bool IsEmpty => Item == null;

    public bool SetItem(ItemDTO p_item, int p_count = 1)
    {
        if (p_item == null || p_count <= 0)
            return false;

        if (!CanStore(p_item))
            return false;

        if (!p_item.IsStackable && p_count > 1)
            return false;

        if (p_item.IsStackable && p_count > p_item.MaxStackCount)
            return false;

        Item = p_item;
        Count = p_count;

        return true;
    }

    public void Clear()
    {
        Item = null;
        Count = 0;
    }

    public abstract bool CanStore(ItemDTO p_item);

    public bool TryStack(ItemDTO p_item, int p_count = 1)
    {
        if (p_item == null || p_count <= 0)
            return false;

        if (IsEmpty || !Item.IsStackable)
            return false;

        if (Item.Id != p_item.Id)
            return false;

        if (Count + p_count > Item.MaxStackCount)
            return false;

        Count += p_count;
        return true;
    }

    public bool TryRemoveCount(int p_count = 1)
    {
        if (IsEmpty || p_count <= 0)
            return false;

        if (p_count > Count)
            return false;

        Count -= p_count;

        if (Count == 0)
            Clear();

        return true;
    }
}
