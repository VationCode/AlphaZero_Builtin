namespace Alpha.Player
{
    // Inventory와 Equipment를 구분하지 않고 두 슬롯 사이의 이동 규칙을 실행한다.
    public class SlotTransferModule
    {
        // 대상 슬롯 상태에 따라 이동·병합·교환 규칙을 선택한다.
        public bool Transfer(ItemSlot p_source, ItemSlot p_target)
        {
            // 1. 이동 요청 자체가 유효한지 판단
            if (p_source == null || p_target == null || ReferenceEquals(p_source, p_target) || p_source.IsEmpty)
                return false;

            // 2. 타겟이 빈 슬롯이면 전체 이동
            if (p_target.IsEmpty)
                return Move(p_source, p_target);

            // 3. 타겟이 동일 아이템이면 Stack 병합
            if (p_target.IsSameItem(p_source.Item))
                return Merge(p_source, p_target);

            // 4. 서로 다른 아이템이면 교환
            return Swap(p_source, p_target);
        }

        // 빈 대상 슬롯으로 원본 슬롯의 전체 상태를 이동한다.
        private bool Move(ItemSlot p_source, ItemSlot p_target)
        {
            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            if (!p_target.CanReplace(sourceItem, sourceCount))
                return false;

            if (!p_target.Replace(sourceItem, sourceCount))
                return false;

            p_source.Clear();

            return true;
        }

        // 같은 아이템을 대상 슬롯의 남은 수용량만큼 병합한다.
        private bool Merge(ItemSlot p_source, ItemSlot p_target)
        {
            int movedCount = p_target.Add(p_source.Item, p_source.Count);

            if (movedCount <= 0)
                return false;

            p_source.Remove(movedCount);

            return true;
        }

        // 서로 다른 아이템의 전체 상태를 양쪽 슬롯이 받을 수 있을 때 교환한다.
        private bool Swap(ItemSlot p_source, ItemSlot p_target)
        {
            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            ItemDTO targetItem = p_target.Item;
            int targetCount = p_target.Count;

            // 양쪽 슬롯을 모두 검증한 후 변경한다.
            if (!p_source.CanReplace(targetItem, targetCount) || !p_target.CanReplace(sourceItem, sourceCount))
                return false;

            if (!p_source.Replace(targetItem, targetCount))
                return false;

            return p_target.Replace(sourceItem, sourceCount);
        }
    }
}
