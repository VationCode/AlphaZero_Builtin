namespace Alpha.Player.Inventory
{
    public enum EInventoryTransferResult
    {
        Rejected,   // 거부
        Moved,
        Merged,
        Swapped
    }

    // 두 슬롯 사이의 아이템 이전 규칙 담당
    public class InventoryTransferModule
    {
        public EInventoryTransferResult Transfer(InventorySlot p_source, InventorySlot p_target)
        {
            if (p_source == null || p_target == null || ReferenceEquals(p_source, p_target) || p_source.IsEmpty)
            {
                return EInventoryTransferResult.Rejected;
            }

            if (p_target.IsEmpty)
                return Move(p_source, p_target);

            if (p_target.IsSameItem(p_source.Item))
                return Merge(p_source, p_target);

            return Swap(p_source, p_target);
        }

        // 빈 슬롯으로 아이템 이동
        private EInventoryTransferResult Move(InventorySlot p_source, InventorySlot p_target)
        {
            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            if (!p_target.CanReplace(sourceItem, sourceCount))
            {
                return EInventoryTransferResult.Rejected;
            }

            p_target.Replace(sourceItem, sourceCount);
            p_source.Clear();

            return EInventoryTransferResult.Moved;
        }

        // 같은 아이템에 병합
        private EInventoryTransferResult Merge(InventorySlot p_source, InventorySlot p_target)
        {
            int movedCount = p_target.Add(p_source.Item, p_source.Count);

            if (movedCount <= 0)
                return EInventoryTransferResult.Rejected;

            p_source.Remove(movedCount);

            return EInventoryTransferResult.Merged;
        }

        // 다른 아이템끼리 교환
        private EInventoryTransferResult Swap(InventorySlot p_source, InventorySlot p_target)
        {
            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            ItemDTO targetItem = p_target.Item;
            int targetCount = p_target.Count;

            // 양쪽 슬롯을 모두 검증한 후 변경한다.
            if (!p_source.CanReplace(targetItem, targetCount) || !p_target.CanReplace(sourceItem, sourceCount))
            {
                return EInventoryTransferResult.Rejected;
            }

            // 교체
            p_source.Replace(targetItem, targetCount);
            p_target.Replace(sourceItem, sourceCount);

            return EInventoryTransferResult.Swapped;
        }
    }
}
