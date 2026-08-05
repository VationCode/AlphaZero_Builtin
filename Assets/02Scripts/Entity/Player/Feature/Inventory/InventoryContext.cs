using System;
using System.Collections.Generic;

namespace Alpha.Player.Inventory
{
    // Player가 보유한 모든 InventorySlot 상태를 보관한다.
    public sealed  class InventoryContext
    {
        // 아이템 타입에 맞는 리스트들(Weapon(melee, range, special), Armor(,,,,), Consumable....)을 Dict로 관리
        private readonly Dictionary<EItemType, List<InventorySlot>> _slotGroupDict = new();

        public event Action<EItemType, InventorySlot> OnSlotAdded;

        public bool TryGetSlotList(EItemType p_itemType, out IReadOnlyList<InventorySlot> p_slotList)
        {
            if (_slotGroupDict.TryGetValue(p_itemType, out List<InventorySlot> slots))
            {
                p_slotList = slots;
                return true;
            }

            p_slotList = null;
            return false;
        }

        internal void AddSlot(EItemType p_itemType, InventorySlot p_slot)
        {
            if (p_slot == null)
                return;

            // 같은 아이템 타입의 슬롯 그룹이 존재하지 않으면 새로 생성하고 그룹관리에 추가한다.
            if (!_slotGroupDict.TryGetValue(p_itemType, out List<InventorySlot> slotList))
            {
                slotList = new List<InventorySlot>();
                _slotGroupDict.Add(p_itemType, slotList);
            }

            slotList.Add(p_slot);

            // 논리 Slot이 추가됐음을 알린다.
            OnSlotAdded?.Invoke(p_itemType, p_slot);
        }

        internal void Clear()
        {
            _slotGroupDict.Clear();
        }
    }
}
