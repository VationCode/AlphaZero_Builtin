namespace Alpha.Player.Inventory
{
    // 슬롯에 아이템을 적재하는 세부 Module
    public class InventoryStorageModule
    {
        private readonly InventoryContext _context;

        // 전달받은 값으로 초기 상태를 구성한다.
        public InventoryStorageModule(InventoryContext p_context)
        {
            _context = p_context;
        }

        // 같은 아이템의 남은 Stack과 빈 슬롯 순서로 보관 대상을 찾는다.
        public bool TryGetTargetSlot(ItemDTO p_item, out InventorySlot p_targetSlot)
        {
            p_targetSlot = null;

            if (p_item == null ||
                !_context.TryGetSlotList(p_item.ItemType, out var slotList))
            {
                return false;
            }

            foreach (InventorySlot slot in slotList)
            {
                if (!slot.IsEmpty &&
                    slot.IsSameItem(p_item) &&
                    slot.GetAddableCount(p_item) >= 1)
                {
                    p_targetSlot = slot;
                    return true;
                }
            }

            foreach (InventorySlot slot in slotList)
            {
                if (slot.IsEmpty && slot.GetAddableCount(p_item) >= 1)
                {
                    p_targetSlot = slot;
                    return true;
                }
            }

            return false;
        }

        // AddItem 대상을 가능한 범위만큼 추가한다.
        public int AddItem(ItemDTO p_item, int p_count)
        {
            if (p_item == null || p_count <= 0)
                return 0;

            // 픽업 아이템 타입에 맞는 SlotList 호출
            if (!_context.TryGetSlotList(p_item.ItemType, out var slotList))
            {
                return 0;
            }

            int remainingCount = p_count;

            // 기존 스택부터 채운다.
            foreach (InventorySlot slot in slotList)
            {
                if (remainingCount == 0)
                    break;

                if (slot.IsEmpty || !slot.IsSameItem(p_item))
                {
                    continue;
                }

                // MaxStack까지 채운 후 남은 수량
                remainingCount -= slot.Add(p_item, remainingCount);
            }

            // 남은 수량을 빈 슬롯에 저장한다.
            foreach (InventorySlot slot in slotList)
            {
                if (remainingCount == 0)
                    break;

                if (!slot.IsEmpty)
                    continue;

                remainingCount -= slot.Add(p_item, remainingCount);
            }

            return p_count - remainingCount;
        }
    }
}
