using System;

namespace Alpha.Player.Equipment
{
    // EquipmentSlot 상태와 아이템 수용 규칙을 관리한다.
    public abstract class EquipmentSlot
    {
        public ItemDTO Item { get; private set; }
        public bool IsEmpty => Item == null;

        public event Action<EquipmentSlot> OnChanged;

        // 슬롯 종류에 맞는 아이템인지 검사한다.
        public abstract bool CanEquip(ItemDTO p_item);

        // Equip 아이템을 조건에 맞는 장비 슬롯에 장착한다.
        internal bool Equip(ItemDTO p_item)
        {
            if (p_item == null || !IsEmpty || !CanEquip(p_item))
            {
                return false;
            }

            Item = p_item;
            NotifyChanged();

            return true;
        }

        // Unequip 장비를 슬롯에서 해제해 반환한다.
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

        // NotifyChanged 변경 사실을 구독자에게 알린다.
        private void NotifyChanged()
        {
            OnChanged?.Invoke(this);
        }
    }
}
