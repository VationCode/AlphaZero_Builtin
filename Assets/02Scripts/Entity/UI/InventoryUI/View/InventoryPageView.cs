using Alpha.Slot;
using System.Collections.Generic;

namespace Alpha.Inventory
{
    public class InventoryPageView : ViewBase
    {
        private List<SlotGroupView> _slotGroupList = new();

        public void Initialize()
        {
            _slotGroupList = new List<SlotGroupView>(GetComponentsInChildren<SlotGroupView>(true));
        }
        // 그룹 조회
        public bool TryGetViewGroup(int p_groupIndex, out SlotGroupView p_group)
        {
            p_group = null;

            if (p_groupIndex < 0 || p_groupIndex >= _slotGroupList.Count)
                return false;

            p_group = _slotGroupList[p_groupIndex];

            return p_group != null;
        }
    }
}