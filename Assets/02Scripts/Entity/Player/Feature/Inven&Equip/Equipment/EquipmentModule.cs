using Alpha.Player.Inventory;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    // 실제 이동·보관·교환 및 실패 복구 담당
    public class EquipmentModule : MonoBehaviour
    {
        private EquipmentContext _context;
        private InventoryModule _inventoryModule;
        private SlotTransferModule _transferModule;

        // 외부 의존성을 연결하고 사용할 수 있는 상태로 준비한다.
        public bool Bind(EquipmentContext p_context, InventoryModule p_inventoryModule, SlotTransferModule p_transferModule)
        {
            if (p_context == null ||
                p_inventoryModule == null ||
                p_transferModule == null)
            {
                return false;
            }

            _context = p_context;
            _inventoryModule = p_inventoryModule;
            _transferModule = p_transferModule;

            return true;
        }

        // DTO의 세부 장비 타입에 대응하는 Context 슬롯을 Flow에 제공한다.
        internal bool TryGetTargetSlot(ItemDTO p_item, out EquipmentSlot p_slot)
        {
            // DTO 형식과 세부 장비 종류를 사용해 대응하는 Context 슬롯을 조회한다.
            switch (p_item)
            {
                case WeaponDTO weapon:
                    if (_context.TryGetWeaponSlot(weapon.WeaponType, out WeaponEquipmentSlot weaponSlot))
                    {
                        p_slot = weaponSlot;
                        return true;
                    }

                    break;

                case ArmorDTO armor:
                    if (_context.TryGetArmorSlot(armor.ArmorType, out ArmorEquipmentSlot armorSlot))
                    {
                        p_slot = armorSlot;
                        return true;
                    }

                    break;
            }

            p_slot = null;
            return false;
        }

        // 더블 클릭으로 장비를 해제할 때 사용할 인벤토리 슬롯을 찾는다.
        internal bool TryGetInventoryTargetSlot(ItemDTO p_item, out InventorySlot p_slot)
        {
            if (_inventoryModule == null)
            {
                p_slot = null;
                return false;
            }

            return _inventoryModule.TryGetStorageSlot(p_item, out p_slot);
        }

        // Inventory와 Equipment 슬롯을 구분하지 않고 공통 이동 규칙을 실행한다.
        internal bool Transfer(ItemSlot p_source, ItemSlot p_target)
        {
            return _transferModule != null &&
                   _transferModule.Transfer(p_source, p_target);
        }
    }
}
