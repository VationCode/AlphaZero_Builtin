namespace Alpha.Player.Inventory
{
    // EInventoryTransferResult 관련 선택 값을 정의한다.
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
        // 대상 슬롯 상태에 따라 이동·병합·교환 규칙을 선택한다.
        public EInventoryTransferResult Transfer(InventorySlot p_source, InventorySlot p_target)
        {
            // 동일 슬롯, 빈 원본 등 이동할 수 없는 요청을 먼저 제외한다.
            if (p_source == null || p_target == null || ReferenceEquals(p_source, p_target) || p_source.IsEmpty)
            {
                return EInventoryTransferResult.Rejected;
            }

            // 빈 슬롯에는 전체 이동, 같은 아이템에는 병합, 나머지는 교환한다.
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

            // 대상에 전체 상태를 복사한 뒤 원본을 비운다.
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
