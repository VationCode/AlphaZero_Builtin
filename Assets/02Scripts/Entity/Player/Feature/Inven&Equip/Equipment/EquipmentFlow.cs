using System;
using Alpha.Player.Inventory;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    // Inventory와 Equipment 슬롯을 조회하고 이동 또는 교환 행동을 판단한다.
    public class EquipmentFlow : MonoBehaviour
    {
        private EquipmentModule _module;
        private InventoryContext _inventoryContext;

        // 무기 장비 슬롯의 최종 아이템을 Combat 연결부에 전달한다.
        // 해제된 경우 null을 전달한다.
        public event Action<WeaponDTO> OnWeaponChanged;

        public bool Bind(EquipmentModule p_module, InventoryContext p_inventoryContext)
        {
            if (p_module == null || p_inventoryContext == null)
                return false;

            _module = p_module;
            _inventoryContext = p_inventoryContext;

            return true;
        }

        // 대상 슬롯이 없으면 자동 장착하고, 있으면 지정 슬롯으로 장착한다.
        public void RequestEquip(int p_inventorySlotIndex, EquipmentSlot p_targetSlot = null)
        {
            TryEquip(p_inventorySlotIndex, p_targetSlot);
        }

        // 인벤토리 아이템을 자동 또는 지정 장비 슬롯으로 이동한다.
        private bool TryEquip(int p_inventorySlotIndex, EquipmentSlot p_targetSlot)
        {
            // 1. 인벤토리 원본 슬롯 확인
            if (!_inventoryContext.TryGetSlot(p_inventorySlotIndex, out InventorySlot inventorySlot) || inventorySlot.IsEmpty)
            {
                return false;
            }

            ItemDTO item = inventorySlot.Item;

            // 2. 더블 클릭이면 인벤토리 -> 장비 슬롯을 자동으로 결정
            if (p_targetSlot == null && !_module.TryGetTargetSlot(item, out p_targetSlot))
            {
                return false;
            }

            // 3. 전송이 완료된 뒤 변경된 무기 슬롯의 최종 상태를 알린다.
            return TransferAndNotify(inventorySlot, p_targetSlot, p_targetSlot);
        }

        // 대상 슬롯이 없으면 자동 해제하고, 있으면 지정 인벤토리 슬롯으로 이동한다.
        public void RequestUnequip(EquipmentSlot p_sourceSlot, int? p_targetInventorySlotIndex = null)
        {
            TryUnequip(p_sourceSlot, p_targetInventorySlotIndex);
        }

        // 장비를 자동 또는 지정 인벤토리 슬롯으로 이동한다.
        private bool TryUnequip(EquipmentSlot p_sourceSlot, int? p_targetInventorySlotIndex)
        {
            // 빈 슬롯 선택 시는 무시
            if (p_sourceSlot == null || p_sourceSlot.IsEmpty)
                return false;

            // 더블 클릭은 보관 가능한 인벤토리 슬롯을 자동 탐색한다.
            if (!p_targetInventorySlotIndex.HasValue)
            {
                if (!_module.TryGetInventoryTargetSlot(p_sourceSlot.Item, out InventorySlot targetSlot))
                    return false;

                return TransferAndNotify(p_sourceSlot, targetSlot, p_sourceSlot);
            }

            if (!_inventoryContext.TryGetSlot(p_targetInventorySlotIndex.Value, out InventorySlot inventorySlot))
            {
                return false;
            }

            // 지정 대상의 전송이 완료된 뒤 장비 슬롯의 최종 상태를 알린다.
            return TransferAndNotify(p_sourceSlot, inventorySlot, p_sourceSlot);
        }

        // 공통 슬롯 전송 성공 시 무기 장비 슬롯의 현재 아이템을 이벤트로 발행한다.
        private bool TransferAndNotify(ItemSlot p_source, ItemSlot p_target, EquipmentSlot p_changedEquipmentSlot)
        {
            if (!_module.Transfer(p_source, p_target))
                return false;

            if (p_changedEquipmentSlot is WeaponEquipmentSlot weaponSlot)
                OnWeaponChanged?.Invoke(weaponSlot.Weapon);

            return true;
        }
    }
}
