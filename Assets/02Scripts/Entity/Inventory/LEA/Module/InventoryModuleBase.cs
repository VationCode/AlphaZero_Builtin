using System;
using System.Collections.Generic;

// Slot 목록을 관리하고 Item 추가와 제거를 처리한다.
public abstract class InventoryModuleBase
{
    private readonly List<SlotBase> _slotList = new();

    public IReadOnlyList<SlotBase> SlotList => _slotList;

    private void TryAddOneItem(ItemDTO p_item)
    {
        if (p_item.IsStackable)
        {
            foreach (SlotBase slot in _slotList)
            {
                if (slot.TryStack(p_item)) 
                    return;
            }
        }

        foreach (SlotBase slot in _slotList)
        {
            if (slot.IsEmpty && slot.SetItem(p_item))
                return;
        }
    }

    // 보관 가능한 Slot에 Item을 추가한다.
    public bool TryAddItem(ItemDTO p_item, int p_count = 1)
    {
        if (p_item == null || p_count <= 0 || GetAvailableCount(p_item) < p_count)
        {
            return false;
        }

        for (int i = 0; i < p_count; i++)
        {
            TryAddOneItem(p_item);
        }

        return true;
    }

    public bool TryRemoveItem(int p_itemId, int p_count = 1)
    {
        if (p_count <= 0 || GetItemCount(p_itemId) < p_count)
        {
            return false;
        }

        int remainingCount = p_count;
        
        foreach (SlotBase slot in _slotList)
        {
            if (slot.IsEmpty || slot.Item.Id != p_itemId)
                continue;
            
            int removeCount = Math.Min(slot.Count, remainingCount);

            slot.TryRemoveCount(removeCount);
            remainingCount -= removeCount;

            if (remainingCount == 0)
                return true;
        }
        return false;
    }

    // Inventory에 보관된 Item 수량을 계산한다.
    private int GetItemCount(int p_itemId)
    {
        int itemCount = 0;

        foreach (SlotBase slot in _slotList)
        {
            if (!slot.IsEmpty && slot.Item.Id == p_itemId)
            {
                itemCount += slot.Count;
            }
        }
        return itemCount;
    }

    // Inventory에 새로운 Slot을 추가한다.
    protected void AddSlot(SlotBase p_slot)
    {
        if (p_slot == null)
            return;

        _slotList.Add(p_slot);
    }

    // Item을 추가할 수 있는 전체 수량을 계산한다.
    private int GetAvailableCount(ItemDTO p_item)
    {
        int availableCount = 0;

        foreach (SlotBase slot in _slotList)
        {
            // 저장 불가시
            if (!slot.CanStore(p_item)) continue;

            // 빈 슬롯이 IsStackable일 때
            if (slot.IsEmpty)
            {
                availableCount += p_item.IsStackable ? p_item.MaxStackCount : 1;
                continue;
            }

            // 같은 Id이고 스택 가능할 때
            if (slot.Item.Id == p_item.Id && p_item.IsStackable )
            {
                availableCount += slot.Item.MaxStackCount - slot.Count;
            }
        }

        return availableCount;
    }
}
