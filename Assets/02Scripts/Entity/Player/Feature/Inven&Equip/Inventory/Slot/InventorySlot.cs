using System;

namespace Alpha.Player.Inventory
{
    // InventorySlot 상태와 아이템 수용 규칙을 관리한다.
    public abstract class InventorySlot
    {
        public int Index { get; }
        public ItemDTO Item { get; private set; }
        public int Count { get; private set; }

        public bool IsEmpty => Item == null;

        // View 갱신 통지
        public event Action<InventorySlot> OnChanged;

        // 슬롯 종류에 따른 아이템 저장 가능 여부
        public abstract bool CanStore(ItemDTO p_item);

        // 전달받은 값으로 초기 상태를 구성한다.
        protected InventorySlot(int p_index)
        {
            Index = p_index;
        }

        // 가능한 만큼 추가하고 실제 추가 수량 반환
        internal int Add(ItemDTO p_item, int p_count)
        {
            if (p_count <= 0)
                return 0;

            // 슬롯 규칙과 최대 Stack 수량 안에서 실제 추가량을 제한한다.
            int addCount = Math.Min(p_count, GetAddableCount(p_item));

            if (addCount <= 0)
                return 0;

            // 빈 슬롯은 첫 추가 시 아이템 참조를 함께 보관한다.
            if (IsEmpty)
                Item = p_item;

            Count += addCount;
            NotifyChanged();

            return addCount;
        }

        // Stack 가능한 동일 아이템인지 확인
        internal bool IsSameItem(ItemDTO p_item)
        {
            return Item != null && p_item != null &&
                   Item.ItemType == p_item.ItemType &&
                   Item.Id == p_item.Id;
        }

        // 현재 슬롯에 추가 가능한 수량 계산
        internal int GetAddableCount(ItemDTO p_item)
        {
            if (p_item == null || !CanStore(p_item))
                return 0;

            int maxCount = GetMaxCount(p_item);

            if (IsEmpty)
                return maxCount;

            if (!IsSameItem(p_item))
                return 0;

            return Math.Max(0, maxCount - Count);
        }

        // 가능한 만큼 제거하고 실제 제거 수량 반환

        internal int Remove(int p_count)
        {
            if (IsEmpty || p_count <= 0)
                return 0;

            // 현재 보유량을 넘지 않는 범위에서만 제거한다.
            int removeCount = Math.Min(p_count, Count);

            Count -= removeCount;

            // 마지막 수량이 제거되면 슬롯을 완전히 빈 상태로 되돌린다.
            if (Count == 0)
            {
                Item = null;
            }

            NotifyChanged();

            return removeCount;
        }

        // 드래그 앤 드롭 Swap 시 슬롯 전체 상태 교체
        // 검증
        internal bool CanReplace(ItemDTO p_item, int p_count)
        {
            return p_item != null && p_count > 0 &&
                   CanStore(p_item) && p_count <= GetMaxCount(p_item);
        }

        // 교체
        internal bool Replace(ItemDTO p_item, int p_count)
        {
            if (!CanReplace(p_item, p_count))
                return false;

            Item = p_item;
            Count = p_count;

            NotifyChanged();
            return true;
        }

        // GetMaxCount 결과를 현재 상태에서 계산해 반환한다.
        private static int GetMaxCount(ItemDTO p_item)
        {
            return p_item.IsStackable? Math.Max(1, p_item.MaxStackCount) : 1;
        }

        // Clear 상태를 초기값으로 비운다.
        internal void Clear()
        {
            if (IsEmpty)
                return;

            Item = null;
            Count = 0;

            NotifyChanged();
        }



        // NotifyChanged 변경 사실을 구독자에게 알린다.
        private void NotifyChanged()
        {
            OnChanged?.Invoke(this);
        }
    }
}
