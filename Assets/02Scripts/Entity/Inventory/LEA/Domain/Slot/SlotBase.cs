using UnityEngine;

public abstract class SlotBase
{
    public ItemDTO Item { get; protected set; }
    public int Count { get; protected set; }
    public bool IsEmpty => Item == null;

    public virtual bool SetItem(ItemDTO p_item, int p_count = 1)
    {
        Item = p_item;
        Count = p_count;
        return true;
    }
    public virtual void Clear()
    {
        Item = null;
        Count = 0;
    }

    public virtual void AddCount(int p_amount)
    {
        Count += p_amount;
    }

    public virtual void RemoveCount(int p_amount)
    {
        Count -= p_amount;

        if (Count <= 0)
            Clear();
    }

}