using System;
using System.Collections.Generic;

namespace Alpha.Player.Inventory
{
    // Player가 보유한 모든 InventorySlot 상태를 보관한다.
    public sealed  class InventoryContext
    {
        // ItemType별 슬롯 그룹(Weapon(melee, range, special), Armor(,,,,), Consumable....)을 관리
        private readonly Dictionary<EItemType, List<InventorySlot>> _slotGroupDict = new();

        // SlotIndex 기반 단일 슬롯 조회
        private readonly Dictionary<int, InventorySlot> _slotIndexDict = new();

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

        public bool TryGetSlot(int p_slotIndex, out InventorySlot p_slot)
        {
            if (p_slotIndex < 0)
            {
                p_slot = null;
                return false;
            }

            // _slotGroupDict에서 slotIndex조회시 O(N)속도이기에 평균 O(1)으로 관리

            return _slotIndexDict.TryGetValue(p_slotIndex, out p_slot);
        }

        internal void AddSlot(EItemType p_itemType, InventorySlot p_slot)
        {
            if (p_slot == null || p_slot.Index < 0)
                return;

            // 동일한 인덱스의 중복 슬롯 등록 방지
            if (_slotIndexDict.ContainsKey(p_slot.Index))
                return;

            // 같은 아이템 타입의 슬롯 그룹이 존재하지 않으면 새로 생성하고 그룹관리에 추가한다.
            if (!_slotGroupDict.TryGetValue(p_itemType, out List<InventorySlot> slotList))
            {
                slotList = new List<InventorySlot>();
                _slotGroupDict.Add(p_itemType, slotList);
            }

            slotList.Add(p_slot);

            // 이벤트 전에 인덱스 조회가 가능해야 한다.
            _slotIndexDict.Add(p_slot.Index, p_slot);

            // 논리 Slot이 추가됐음을 알린다.
            OnSlotAdded?.Invoke(p_itemType, p_slot);
        }

        internal void Clear()
        {
            _slotGroupDict.Clear();
            _slotIndexDict.Clear();
        }
    }
}
