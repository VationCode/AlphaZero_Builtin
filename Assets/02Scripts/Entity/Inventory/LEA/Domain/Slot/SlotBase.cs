// 아이템 데이터 보관
// 아이템 수량 보관
// 슬롯 초기화
using System;

public abstract class SlotBase
{
   public ItemDTO Item { get; private set; }
    public int Count { get; private set; }

    public bool IsEmpty => Item == null;

    public event Action<SlotBase> Changed;

    public abstract bool CanStore(ItemDTO p_item);

    public bool SetItem(ItemDTO p_item, int p_count = 1)
    {
        if (p_item == null) return false;
        if(p_count <= 0) return false;
        if(!CanStore(p_item)) return false;

        int maxCount = p_item.IsStackable? p_item.MaxStackCount : 1;

        if (p_count > maxCount) return false;
        
        Item = p_item;
        Count = p_count;

        NotifyChanged();
        return true;
    }

    public void Clear()
    {
        Item = null;
        Count = 0;

        NotifyChanged();
    }

    // 동일한 Item의 수량을 추가한다.
    public bool TryStack(ItemDTO p_item, int p_count = 1)
    {
        if (p_item == null || p_count <= 0) return false;
        if (IsEmpty || !Item.IsStackable) return false;
        if (Item.Id != p_item.Id) return false;
        if (Count + p_count > Item.MaxStackCount) return false;

        Count += p_count;

        NotifyChanged();
        return true;
    }

    // Slot에서 지정한 수량을 제거한다.
    public bool TryRemoveCount(int p_count = 1)
    {
        if (IsEmpty || p_count <= 0) return false;
        if (p_count > Count) return false;

        Count -= p_count;

        if (Count == 0) Clear();
        else NotifyChanged();
        
        return true;
    }

    // Slot의 변경을 구독자에게 알린다.
    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
