using Alpha.Player.Equipment;
using Alpha.Player.Slot;

namespace Alpha.Player.Inventory
{
    // 인벤토리 슬롯의 식별자와 요청 의미만 담당한다.
    public sealed class InventorySlotInteractionView :
        SlotInteractionViewBase
    {
        private InventoryView _owner;

        public int SlotIndex { get; private set; } = -1;

        protected override bool HasValidSource =>
            _owner != null && SlotIndex >= 0;

        // 요청을 전달할 InventoryView와 이 View가 나타내는 슬롯 번호를 연결한다.
        public void Bind(
            InventoryView p_owner,
            int p_slotIndex)
        {
            _owner = p_owner;
            SlotIndex = p_slotIndex;
        }

        // 더블 클릭한 인벤토리 아이템의 자동 장착을 요청한다.
        protected override void HandleDoubleClick()
        {
            _owner.RequestEquip(SlotIndex);
        }

        // 원본 View 종류에 따라 인벤토리 이동 또는 장비 해제를 요청한다.
        protected override void HandleDrop(
            SlotInteractionViewBase p_source)
        {
            // 같은 인벤토리 View는 이동, 장비 View는 해당 슬롯으로 해제한다.
            switch (p_source)
            {
                case InventorySlotInteractionView inventorySource:
                    _owner.RequestTransfer(
                        inventorySource.SlotIndex,
                        SlotIndex);
                    break;

                case EquipmentSlotInteractionView equipmentSource:
                    _owner.RequestUnequip(
                        equipmentSource.Slot,
                        SlotIndex);
                    break;
            }
        }
    }
}
