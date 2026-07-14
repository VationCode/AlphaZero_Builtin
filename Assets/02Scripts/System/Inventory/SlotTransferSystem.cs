// 대상 슬롯이 비어 있으면 기존처럼 이동
// 대상 슬롯에 아이템이 있으면 서로 교환
// 양쪽 슬롯이 상대 아이템을 보관할 수 있어야 교환
// 서로 다른 Weapon/Armor 세부 타입이면 CanStore()에서 거부
public class SlotTransferSystem
{
    public bool TryMove(SlotBase p_fromSlot, SlotBase p_toSlot)
    {
        if (p_fromSlot == null || p_toSlot == null)
            return false;

        if (p_fromSlot == p_toSlot)
            return false;

        if (p_fromSlot.IsEmpty)
            return false;

        if (!p_toSlot.CanStore(p_fromSlot.Item))
            return false;

        ItemDTO fromItem = p_fromSlot.Item;
        int fromCount = p_fromSlot.Count;

        // from슬롯 -> to슬롯에서 to슬롯의 MaxStack까지와 덮어쓰고 from슬롯에서 빠진 count만큼 처리
        if (!p_toSlot.IsEmpty && p_toSlot.Item.Id == fromItem.Id)
        {
            int availableCount = p_toSlot.Item.MaxStackCount - p_toSlot.Count;

            if (availableCount <= 0)
                return false;

            int moveCount = fromCount;

            if (moveCount > availableCount)
                moveCount = availableCount;

            if (!p_toSlot.TryStack(fromItem, moveCount))
                return false;

            p_fromSlot.TryRemoveCount(moveCount);
            return true;
        }

        if (p_toSlot.IsEmpty)
        {
            if (!p_toSlot.SetItem(fromItem, fromCount))
                return false;

            p_fromSlot.Clear();
            return true;
        }

        if (!p_fromSlot.CanStore(p_toSlot.Item))
            return false;

        ItemDTO toItem = p_toSlot.Item;
        int toCount = p_toSlot.Count;

        p_toSlot.SetItem(fromItem, fromCount);
        p_fromSlot.SetItem(toItem, toCount);

        return true;
    }
}
