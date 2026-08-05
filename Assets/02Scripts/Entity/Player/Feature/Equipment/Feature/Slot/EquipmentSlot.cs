using System;

namespace Alpha.Player.Equipment
{
    public abstract class EquipmentSlot
    {
        public ItemDTO Item { get; private set; }
        public bool IsEmpty => Item == null;

        public event Action<EquipmentSlot> OnChanged;

        // 슬롯 종류에 맞는 아이템인지 검사한다.
        public abstract bool CanEquip(ItemDTO p_item);

        internal bool Equip(ItemDTO p_item)
        {
            if (p_item == null || !CanEquip(p_item))
            {
                return false;
            }

            if (ReferenceEquals(Item, p_item))
            {
                return false;
            }

            Item = p_item;
            NotifyChanged();

            return true;
        }

        internal ItemDTO Unequip()
        {
            if (IsEmpty)
            {
                return null;
            }

            ItemDTO previousItem = Item;
            Item = null;

            NotifyChanged();

            return previousItem;
        }

        private void NotifyChanged()
        {
            OnChanged?.Invoke(this);
        }
    }
}
