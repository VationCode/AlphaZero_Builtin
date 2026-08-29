using System.Collections.Generic;

namespace Alpha.Player.Inventory
{
    // 슬롯에 아이템을 적재하는 세부 Module
    public class InventoryStorageModule
    {
        private readonly InventoryContext _context;

        // 전달받은 값으로 초기 상태를 구성한다.
        public InventoryStorageModule(InventoryContext p_context)
        {
            _context = p_context;
        }

        // 같은 아이템의 남은 Stack과 빈 슬롯 순서로 보관 대상을 찾는다.
        public bool TryGetTargetSlot(ItemDTO p_item, out InventorySlot p_targetSlot)
        {
            p_targetSlot = null;

            if (p_item == null ||
                !TryGetStorageSlotList(p_item, out var slotList))
            {
                return false;
            }

            foreach (InventorySlot slot in slotList)
            {
                if (!slot.IsEmpty &&
                    slot.IsSameItem(p_item) &&
                    slot.GetAddableCount(p_item) >= 1)
                {
                    p_targetSlot = slot;
                    return true;
                }
            }

            foreach (InventorySlot slot in slotList)
            {
                if (slot.IsEmpty && slot.GetAddableCount(p_item) >= 1)
                {
                    p_targetSlot = slot;
                    return true;
                }
            }

            return false;
        }

        // AddItem 대상을 가능한 범위만큼 추가한다.
        public int AddItem(ItemDTO p_item, int p_count)
        {
            if (p_item == null || p_count <= 0)
                return 0;

            // 분류형 아이템은 Category 슬롯, QuestItem은 ItemType 슬롯을 사용한다.
            if (!TryGetStorageSlotList(p_item, out var slotList))
            {
                return 0;
            }

            int remainingCount = p_count;

            // 기존 스택부터 채운다.
            foreach (InventorySlot slot in slotList)
            {
                if (remainingCount == 0)
                    break;

                if (slot.IsEmpty || !slot.IsSameItem(p_item))
                {
                    continue;
                }

                // MaxStack까지 채운 후 남은 수량
                remainingCount -= slot.Add(p_item, remainingCount);
            }

            // 남은 수량을 빈 슬롯에 저장한다.
            foreach (InventorySlot slot in slotList)
            {
                if (remainingCount == 0)
                    break;

                if (!slot.IsEmpty)
                    continue;

                remainingCount -= slot.Add(p_item, remainingCount);
            }

            return p_count - remainingCount;
        }

        // 아이템에 맞는 실제 저장 대상 슬롯 목록을 선택한다.
        private bool TryGetStorageSlotList(
            ItemDTO p_item,
            out IReadOnlyList<InventorySlot> p_slotList)
        {
            if (p_item is WeaponDTO weapon)
            {
                if (_context.TryGetWeaponSlotList(
                        weapon.WeaponCategory,
                        out var weaponSlotList))
                {
                    p_slotList = weaponSlotList;
                    return true;
                }

                p_slotList = null;
                return false;
            }

            if (p_item is ArmorDTO armor)
            {
                if (_context.TryGetArmorSlotList(
                        armor.ArmorType,
                        out var armorSlotList))
                {
                    p_slotList = armorSlotList;
                    return true;
                }

                p_slotList = null;
                return false;
            }

            if (p_item is ConsumableDTO consumable)
            {
                if (_context.TryGetConsumableSlotList(
                        consumable.ConsumableType,
                        out var consumableSlotList))
                {
                    p_slotList = consumableSlotList;
                    return true;
                }

                p_slotList = null;
                return false;
            }

            if (p_item is MaterialDTO material)
            {
                if (_context.TryGetMaterialSlotList(
                        material.MaterialType,
                        out var materialSlotList))
                {
                    p_slotList = materialSlotList;
                    return true;
                }

                p_slotList = null;
                return false;
            }

            return _context.TryGetSlotList(
                p_item.ItemType,
                out p_slotList);
        }
    }
}
