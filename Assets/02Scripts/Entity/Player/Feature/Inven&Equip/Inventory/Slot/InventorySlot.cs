using System;
using Alpha.Player;

namespace Alpha.Player.Inventory
{
    // InventorySlot 상태와 아이템 수용 규칙을 관리한다.
    public abstract class InventorySlot : ItemSlot
    {
        public int Index { get; }

        // View 갱신 통지
        public event Action<InventorySlot> OnChanged;

        // 슬롯 종류에 따른 아이템 저장 가능 여부
        public abstract bool CanStore(ItemDTO p_item);

        // 전달받은 값으로 초기 상태를 구성한다.
        protected InventorySlot(int p_index)
        {
            Index = p_index;
        }

        // 공통 슬롯이 사용하는 인벤토리 수용 규칙을 연결한다.
        protected override bool CanAccept(ItemDTO p_item)
        {
            return CanStore(p_item);
        }

        // 인벤토리는 아이템의 Stack 설정을 최대 수량으로 사용한다.
        protected override int GetMaxCount(ItemDTO p_item)
        {
            return p_item.IsStackable ? Math.Max(1, p_item.MaxStackCount) : 1;
        }

        // 인벤토리 슬롯 변경 사실을 View에 전달한다.
        protected override void NotifyChanged()
        {
            OnChanged?.Invoke(this);
        }
    }
}
