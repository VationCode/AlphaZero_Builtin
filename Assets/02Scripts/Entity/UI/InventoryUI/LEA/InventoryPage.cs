using Alpha.Slot;
using System.Collections.Generic;

namespace Alpha.Inventory
{
    public class InventoryPage
    {
        public Dictionary<int, SlotGroup> SlotGroupDict => _slotGroupDict;
        private readonly Dictionary<int, SlotGroup> _slotGroupDict = new();
        // 슬롯 그룹 등록
        public void AddSlotGroup(int p_groupIndex, SlotGroup p_slotGroup)
        {
            if (p_slotGroup == null)
                return;

            _slotGroupDict[p_groupIndex] = p_slotGroup;
        }

        // 슬롯 그룹 조회
        public bool TryGetSlotGroup(int p_groupIndex, out SlotGroup p_slotGroup)
        {
            return _slotGroupDict.TryGetValue(p_groupIndex, out p_slotGroup);
        }
    }
}