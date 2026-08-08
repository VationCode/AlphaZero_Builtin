using System;
using Alpha.Player;

namespace Alpha.Player.Equipment
{
    // EquipmentSlot 상태와 아이템 수용 규칙을 관리한다.
    public abstract class EquipmentSlot : ItemSlot
    {
        public event Action<EquipmentSlot> OnChanged;

        // 슬롯 종류에 맞는 아이템인지 검사한다.
        public abstract bool CanEquip(ItemDTO p_item);

        // 공통 슬롯이 사용하는 장비 수용 규칙을 연결한다.
        protected override bool CanAccept(ItemDTO p_item)
        {
            return CanEquip(p_item);
        }

        // 장비 슬롯은 아이템을 항상 한 개만 보유한다.
        protected override int GetMaxCount(ItemDTO p_item)
        {
            return 1;
        }

        // 장비 슬롯 변경 사실을 View와 Context에 전달한다.
        protected override void NotifyChanged()
        {
            OnChanged?.Invoke(this);
        }
    }
}
