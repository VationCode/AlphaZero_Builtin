using System;

namespace Alpha.Player
{
    // Inventory와 Equipment가 공유하는 아이템 상태와 수량 변경 규칙을 관리한다.
    public abstract class ItemSlot
    {
        public ItemDTO Item { get; private set; }
        public int Count { get; private set; }
        public bool IsEmpty => Item == null;

        // 슬롯 종류에 따른 아이템 수용 가능 여부를 검사한다.
        protected abstract bool CanAccept(ItemDTO p_item);

        // 슬롯 종류에 따른 최대 보유 수량을 반환한다.
        protected abstract int GetMaxCount(ItemDTO p_item);

        // 상태 변경 사실을 구체 슬롯의 구독자에게 알린다.
        protected abstract void NotifyChanged();

        // 가능한 만큼 추가하고 실제 추가 수량을 반환한다.
        internal int Add(ItemDTO p_item, int p_count)
        {
            if (p_count <= 0)
                return 0;

            int addCount = Math.Min(p_count, GetAddableCount(p_item));

            if (addCount <= 0)
                return 0;

            if (IsEmpty)
                Item = p_item;

            Count += addCount;
            NotifyChanged();

            return addCount;
        }

        // 현재 슬롯의 아이템과 전달받은 아이템이 같은 종류인지 검사한다.
        internal bool IsSameItem(ItemDTO p_item)
        {
            return Item != null && p_item != null &&
                   Item.ItemType == p_item.ItemType &&
                   Item.Id == p_item.Id;
        }

        // 현재 상태에서 추가로 받을 수 있는 수량을 계산한다.
        internal int GetAddableCount(ItemDTO p_item)
        {
            if (p_item == null || !CanAccept(p_item))
                return 0;

            int maxCount = GetMaxCount(p_item);

            if (IsEmpty)
                return maxCount;

            if (!IsSameItem(p_item))
                return 0;

            return Math.Max(0, maxCount - Count);
        }

        // 가능한 만큼 제거하고 실제 제거 수량을 반환한다.
        internal int Remove(int p_count)
        {
            if (IsEmpty || p_count <= 0)
                return 0;

            int removeCount = Math.Min(p_count, Count);

            Count -= removeCount;

            if (Count == 0)
                Item = null;

            NotifyChanged();

            return removeCount;
        }

        // 현재 슬롯 전체를 전달받은 상태로 교체할 수 있는지 검사한다.
        internal bool CanReplace(ItemDTO p_item, int p_count)
        {
            return p_item != null &&
                   p_count > 0 &&
                   CanAccept(p_item) &&
                   p_count <= GetMaxCount(p_item);
        }

        // 현재 슬롯 전체를 전달받은 아이템과 수량으로 교체한다.
        internal bool Replace(ItemDTO p_item, int p_count)
        {
            if (!CanReplace(p_item, p_count))
                return false;

            Item = p_item;
            Count = p_count;

            NotifyChanged();
            return true;
        }

        // 슬롯의 아이템과 수량을 초기 상태로 비운다.
        internal void Clear()
        {
            if (IsEmpty)
                return;

            Item = null;
            Count = 0;

            NotifyChanged();
        }
    }
}
