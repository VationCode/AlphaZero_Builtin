using UnityEngine;
using System.Collections.Generic;

namespace Alpha.Slot
{
    public class SlotGroup
    {
        private readonly List<SlotBase> _slotList = new();

        public List<SlotBase> SlotList => _slotList;

        // 슬롯 추가
        public void AddSlot(SlotBase p_slot)
        {
            if (p_slot == null)
                return;

            _slotList.Add(p_slot);
        }

        // 슬롯 조회
        public bool TryGetSlot(int p_slotIndex, out SlotBase p_slot)
        {
            p_slot = null;

            if (p_slotIndex < 0 || p_slotIndex >= _slotList.Count)
                return false;

            p_slot = _slotList[p_slotIndex];

            return p_slot != null;
        }
    }
}
