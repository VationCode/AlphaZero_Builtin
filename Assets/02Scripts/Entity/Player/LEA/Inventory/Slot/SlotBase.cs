using System;

public abstract class SlotBase
{
    public ItemDTO Item { get; private set; }
    public int Count { get; private set; }

    public bool IsEmpty => Item == null;

    public event Action<ItemDTO, int> OnSlotChanged;

    public abstract bool CanStore(ItemDTO p_item);

    private int GetMaxCount(ItemDTO p_item)
    {
        if (!p_item.IsStackable)
            return 1;

        return Math.Max(1, p_item.MaxStackCount);
    }

    // 실제 추가된 수량을 반환
    public int Add(ItemDTO p_item, int p_count)
    {
        if (p_item == null || p_count <= 0 || !CanStore(p_item))
            return 0;

        // 다른 아이템(Id)이 들어 있으면 추가 불가
        if (!IsEmpty && Item.Id != p_item.Id)
            return 0;

        int maxCount = GetMaxCount(p_item);
        int availableCount = maxCount - Count;

        // 0인경우는 현재 Count가 맥스라는 의미
        if (availableCount <= 0) return 0;

        int addedCount = Math.Min(p_count, availableCount);

        if (IsEmpty) Item = p_item;

        Count += addedCount;

        NotifyChanged();    // UI에 반영

        return addedCount;
    }

    // 실제 제거된 수량을 반환
    public int Remove(int p_count)
    {
        if (IsEmpty || p_count <= 0)
            return 0;

        int removedCount = Math.Min(p_count, Count);

        Count -= removedCount;

        if (Count == 0) Item = null;

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
    private void NotifyChanged()
    {
        OnSlotChanged?.Invoke(Item, Count);
    }
}

