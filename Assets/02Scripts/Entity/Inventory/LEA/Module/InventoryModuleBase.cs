using System.Collections.Generic;

// 논리 슬롯 목록 소유
// 외부에 읽기 전용 슬롯 목록 제공
// 현재 슬롯 수 제공
// 파생 인벤토리가 생성한 슬롯 추가
// 파생 인벤토리에 아이템 보관 조건 위임
public abstract class InventoryModuleBase
{
    private readonly List<SlotBase> _slotList = new();

    public IReadOnlyList<SlotBase> SlotList => _slotList;
    public int Capacity => _slotList.Count;

    private bool TryAddOne(ItemDTO p_item)
    {
        if (p_item.IsStackable)
        {
            foreach (SlotBase slot in _slotList)
            {
                if (slot.TryStack(p_item))
                    return true;
            }
        }

        foreach (SlotBase slot in _slotList)
        {
            if (!slot.IsEmpty)
                continue;

            if (slot.SetItem(p_item))
                return true;
        }

        return false;
    }

    public bool TryAdd(ItemDTO p_item, int p_count)
    {
        if (p_item == null || p_count <= 0)
            return false;

        if (!CanStore(p_item))
            return false;

        if (GetAvailableCount(p_item) < p_count)
            return false;

        for (int i = 0; i < p_count; i++)
        {
            if (!TryAddOne(p_item))
                return false;
        }

        return true;
    }

    public bool TryRemove(int p_itemId, int p_count = 1)
    {
        if (p_count <= 0)
            return false;

        int totalCount = 0;

        foreach (SlotBase slot in _slotList)
        {
            if (slot.IsEmpty)
                continue;

            if (slot.Item.Id == p_itemId)
                totalCount += slot.Count;
        }

        if (totalCount < p_count)
            return false;

        int remainingCount = p_count;

        foreach (SlotBase slot in _slotList)
        {
            if (slot.IsEmpty)
                continue;

            if (slot.Item.Id != p_itemId)
                continue;

            int removeCount = remainingCount;

            if (removeCount > slot.Count)
                removeCount = slot.Count;

            slot.TryRemoveCount(removeCount);
            remainingCount -= removeCount;

            if (remainingCount == 0)
                return true;
        }

        return false;
    }

    protected void AddSlot(SlotBase p_slot)
    {
        if (p_slot == null)
            return;

        _slotList.Add(p_slot);
    }

    protected abstract bool CanStore(ItemDTO p_item);

    private int GetAvailableCount(ItemDTO p_item)
    {
        int availableCount = 0;

        foreach (SlotBase slot in _slotList)
        {
            if (slot.IsEmpty)
            {
                if (!slot.CanStore(p_item))
                    continue;

                if (p_item.IsStackable)
                {
                    if (p_item.MaxStackCount > 0)
                        availableCount += p_item.MaxStackCount;
                }
                else
                {
                    availableCount += 1;
                }

                continue;
            }

            if (!p_item.IsStackable)
                continue;

            if (slot.Item.Id != p_item.Id)
                continue;

            int slotAvailableCount =
                slot.Item.MaxStackCount - slot.Count;

            if (slotAvailableCount > 0)
                availableCount += slotAvailableCount;
        }

        return availableCount;
    }
}
