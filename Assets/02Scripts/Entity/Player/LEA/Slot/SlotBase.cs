using System;

// 슬롯 데이터와 이동·합치기·교환
public abstract class SlotBase
{
    public ItemDTO Item { get; private set; }
    public int Count { get; private set; }

    public bool IsEmpty => Item == null;

    public event Action<ItemDTO, int> OnSlotChanged;

    // 해당 슬롯이 아이템을 보관할 수 있는지 판단
    public abstract bool CanStore(ItemDTO p_item);

    // 슬롯에 아이템을 추가하고 실제 추가된 수량을 반환
    public int Add(ItemDTO p_item, int p_count)
    {
        if (p_item == null || p_count <= 0 || !CanStore(p_item))
        {
            return 0;
        }

        if (!IsEmpty && !IsSameItem(Item, p_item))
            return 0;

        int availableCount = GetMaxCount(p_item) - Count;

        if (availableCount <= 0)
            return 0;

        int addedCount = Math.Min(p_count, availableCount);

        if (IsEmpty)
            Item = p_item;

        Count += addedCount;
        NotifyChanged();

        return addedCount;
    }

    public int Remove(int p_count)
    {
        if (IsEmpty || p_count <= 0)
            return 0;

        int removedCount = Math.Min(p_count, Count);

        Count -= removedCount;

        if (Count == 0)
            Item = null;

        NotifyChanged();

        return removedCount;
    }

    public void Clear()
    {
        if (IsEmpty)
            return;

        Item = null;
        Count = 0;

        NotifyChanged();
    }

    public bool TryMoveTo(SlotBase p_target)
    {
        if (p_target == null || p_target == this || IsEmpty)
            return false;

        // 빈 슬롯 이동 또는 동일 아이템 합치기 
        if (p_target.IsEmpty || IsSameItem(Item, p_target.Item))
        {
            int movedCount = p_target.Add(Item, Count);

            if (movedCount <= 0) return false;

            Remove(movedCount);
            return true;

        }

        // 서로 상대 아이템을 보관할 수 있어야 교환
        if (!p_target.CanStore(Item) || !CanStore(p_target.Item))
        {
            return false;
        }

        return SwapWith(p_target);
    }

    private bool SwapWith(SlotBase p_target)
    {
        ItemDTO targetItem = p_target.Item;
        int targetCount = p_target.Count;

        p_target.Item = Item;
        p_target.Count = Count;

        Item = targetItem;
        Count = targetCount;

        NotifyChanged();
        p_target.NotifyChanged();

        return true;
    }

    private static bool IsSameItem(ItemDTO p_left, ItemDTO p_right)
    {
        return p_left != null && p_right != null && p_left.ItemType == p_right.ItemType && p_left.Id == p_right.Id;
    }

    private static int GetMaxCount(ItemDTO p_item)
    {
        return p_item.IsStackable? Math.Max(1, p_item.MaxStackCount) : 1;
    }

    // 슬롯 데이터가 변경됐음을 외부에 알림
    private void NotifyChanged()
    {
        OnSlotChanged?.Invoke(Item, Count);
    }
}

