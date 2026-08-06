using Alpha.Player.Inventory;
using Alpha.Player.Slot;

namespace Alpha.Player.Equipment
{
    // 장비 슬롯의 식별자와 요청 의미만 담당한다.
    public sealed class EquipmentSlotInteractionView :
        SlotInteractionViewBase
    {
        private EquipmentView _owner;

        public EquipmentSlot Slot { get; private set; }

        protected override bool HasValidSource => _owner != null && Slot != null;

        // 요청을 전달할 EquipmentView와 이 View가 나타내는 슬롯을 연결한다.
        public void Bind(EquipmentView p_owner, EquipmentSlot p_slot)
        {
            _owner = p_owner;
            Slot = p_slot;
        }

        // 재사용이나 해제 전에 소유 View와 슬롯 참조를 비운다.
        public void Unbind()
        {
            _owner = null;
            Slot = null;
        }

        // 더블 클릭한 장비를 인벤토리로 해제하도록 요청한다.
        protected override void HandleDoubleClick()
        {
            _owner.RequestUnequip(Slot);
        }

        // 인벤토리 아이템을 받은 경우 이 장비 슬롯으로 장착을 요청한다.
        protected override void HandleDrop(SlotInteractionViewBase p_source)
        {
            if (p_source is InventorySlotInteractionView inventorySource)
            {
                _owner.RequestEquip(inventorySource.SlotIndex, Slot);
            }
        }
    }
}
