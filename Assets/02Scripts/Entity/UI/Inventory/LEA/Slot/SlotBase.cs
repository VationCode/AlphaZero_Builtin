using System;

namespace Alpha.Slot
{
    // 슬롯 데이터와 이동·합치기·교환
    public abstract class SlotBase
    {
        public ItemDTO Item { get; private set; }
        public int Count { get; private set; }

        public bool IsEmpty => Item == null;

        public event Action<ItemDTO, int> OnSlotChanged;

        #region ====================  검증
        // 해당 슬롯이 아이템을 보관할 수 있는지 판단
        public abstract bool CanStore(ItemDTO p_item);
        public bool CanAdd(ItemDTO p_item)
        {
            if (p_item == null || !CanStore(p_item))
                return false;

            if (IsEmpty)
                return true;

            return IsSameItem(Item, p_item) && Count < GetMaxCount(p_item);
        }
        #endregion ====================  검증

        #region ==================== 조회
        public int GetAddableCount(ItemDTO p_item)
        {
            if (!CanAdd(p_item))
                return 0;

            return GetMaxCount(p_item) - Count;
        }

        // 슬롯 종류에 따라 최대 저장 수량을 변경할 수 있도록 허용한다.
        protected virtual int GetMaxCount(ItemDTO p_item)
        {
            if (!p_item.IsStackable)
                return 1;

            return Math.Max(1, p_item.MaxStackCount);
        }

        internal bool IsSameItem(ItemDTO p_left, ItemDTO p_right)
        {
            return p_left != null &&
                   p_right != null &&
                   p_left.ItemType == p_right.ItemType &&
                   p_left.Id == p_right.Id;
        }
        #endregion ==================== 조회

        #region ==================== 입력 처리
        // 실제 추가된 수량 반환
        internal int AddItem(ItemDTO p_item, int p_count)
        {
            if (p_count <= 0) return 0;

            int addableCount = GetAddableCount(p_item);

            if (addableCount <= 0) return 0;

            int addedCount = Math.Min(p_count, addableCount);

            if (IsEmpty) Item = p_item;

            Count += addedCount;

            NotifyChanged();

            return addedCount;
        }

        // 실제 제거된 수량 반환
        internal int RemoveItem(int p_count)
        {
            if (IsEmpty || p_count <= 0) return 0;

            int removedCount = Math.Min(p_count, Count);

            Count -= removedCount;

            if (Count == 0) Item = null;

            NotifyChanged();

            return removedCount;
        }

        /// <summary>
        /// 슬롯 교환 시 전체 내용을 교체
        /// </summary>
        /// <returns></returns>
        internal bool TryReplace(ItemDTO p_item, int p_count)
        {
            if (p_item == null || p_count <= 0 || !CanStore(p_item) || p_count > GetMaxCount(p_item))
            {
                return false;
            }
            Item = p_item;
            Count = p_count;

            NotifyChanged();

            return true;
        }

        internal void Clear()
        {
            if (IsEmpty) return;

            Item = null;
            Count = 0;

            NotifyChanged();
        }
        #endregion ==================== /입력 처리

        // 변화 외부에 전달
        private void NotifyChanged()
        {
            OnSlotChanged?.Invoke(Item, Count);
        }
    }

}