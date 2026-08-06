using Alpha.Player.Inventory;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    // Inventory와 Equipment 사이의 요청 순서와 복구를 조정한다.
    public class EquipmentFlow : MonoBehaviour
    {
        private EquipmentModule _equipmentModule;
        private InventoryContext _inventoryContext;
        private InventoryModule _inventoryModule;

        // 외부 의존성을 연결하고 사용할 수 있는 상태로 준비한다.
        public bool Bind(EquipmentModule p_equipmentModule, InventoryContext p_inventoryContext, InventoryModule p_inventoryModule)
        {
            if (p_equipmentModule == null || p_inventoryContext == null || p_inventoryModule == null)
            {
                Debug.LogError($"{nameof(EquipmentFlow)} 참조가 설정되지 않았습니다.", this);
                return false;
            }

            _equipmentModule = p_equipmentModule;
            _inventoryContext = p_inventoryContext;
            _inventoryModule = p_inventoryModule;

            return true;
        }

        // 인벤토리 아이템을 자동으로 결정된 장비 슬롯에 장착한다.
        public EEquipmentChangeResult RequestEquip(int p_inventorySlotIndex)
        {
            return RequestEquip(p_inventorySlotIndex, null);
        }

        // 인벤토리 아이템을 지정 슬롯에 장착하고 필요하면 기존 장비와 교환한다.
        public EEquipmentChangeResult RequestEquip(int p_inventorySlotIndex, EquipmentSlot p_targetSlot)
        {
            // 원본 슬롯과 장착할 아이템이 유효한지 먼저 확인한다.
            if (!_inventoryContext.TryGetSlot(p_inventorySlotIndex, out InventorySlot inventorySlot) ||
                                              inventorySlot.IsEmpty || inventorySlot.Count <= 0)
            {
                return EEquipmentChangeResult.Rejected;
            }

            ItemDTO item = inventorySlot.Item;

            // 더블 클릭처럼 대상 슬롯이 없으면 아이템 타입으로 자동 결정한다.
            if (p_targetSlot == null && !_equipmentModule.TryGetTargetSlot(item, out p_targetSlot))
            {
                return EEquipmentChangeResult.Rejected;
            }

            // 지정 슬롯과 타입이 다르면 드롭 요청을 거부한다.
            if (!p_targetSlot.CanEquip(item))
                return EEquipmentChangeResult.Rejected;

            // 대상이 차 있으면 장착 대신 두 아이템을 교환한다.
            if (!p_targetSlot.IsEmpty)
            {
                return SwapEquipment(inventorySlot, p_targetSlot, item);
            }

            EEquipmentChangeResult result = _equipmentModule.Equip(p_targetSlot, item);

            if (result != EEquipmentChangeResult.Equipped)
                return result;

            // 장착이 끝난 뒤 원본 인벤토리에서 한 개를 제거한다.
            if (_inventoryModule.RemoveItem(inventorySlot, 1) == 1)
            {
                return EEquipmentChangeResult.Equipped;
            }

            // 인벤토리 제거 실패 시 중복 보유를 막기 위해 장착을 되돌린다.
            RollbackEquip(item);
            return EEquipmentChangeResult.Rejected;
        }

        // 인벤토리 슬롯의 아이템과 현재 장착 아이템을 서로 교환한다.
        private EEquipmentChangeResult SwapEquipment(InventorySlot p_inventorySlot, EquipmentSlot p_targetSlot, ItemDTO p_inventoryItem)
        {
            // 어느 방향에서 요청해도 교환 가능한 두 슬롯과 호환 아이템인지 먼저 확인한다.
            if (p_inventorySlot == null || p_targetSlot == null ||
                p_inventorySlot.IsEmpty || p_targetSlot.IsEmpty ||
                p_inventoryItem == null || !p_targetSlot.CanEquip(p_inventoryItem))
            {
                return EEquipmentChangeResult.Rejected;
            }

            ItemDTO equippedItem = p_targetSlot.Item;

            // 기존 장비 하나를 원본 슬롯에 온전히 되돌릴 수 있어야 한다.
            if (p_inventorySlot.Count != 1 || !_inventoryModule.CanReplaceItem(p_inventorySlot, equippedItem, 1))
            {
                return EEquipmentChangeResult.Rejected;
            }

            // 기존 장비를 먼저 해제해 대상 슬롯을 비운다.
            EEquipmentChangeResult unequipResult = TryUnequip(p_targetSlot, out ItemDTO unequippedItem);

            if (unequipResult != EEquipmentChangeResult.Unequipped || unequippedItem == null)
            {
                return unequipResult;
            }

            // 비워진 정확한 대상 슬롯에 드래그한 아이템을 장착한다.
            EEquipmentChangeResult equipResult = _equipmentModule.Equip(p_targetSlot, p_inventoryItem);

            if (equipResult != EEquipmentChangeResult.Equipped)
            {
                // 새 장비 장착 실패 시 방금 해제한 장비를 원위치한다.
                RestoreEquipment(p_targetSlot, unequippedItem);
                return equipResult;
            }

            // 원본 인벤토리 슬롯에 해제된 기존 장비를 넣어 교환을 완료한다.
            if (_inventoryModule.ReplaceItem(p_inventorySlot, unequippedItem, 1))
            {
                return EEquipmentChangeResult.Equipped;
            }

            // 마지막 교체가 실패하면 새 장비를 해제하고 기존 상태를 복구한다.
            TryUnequip(p_targetSlot, out _);
            RestoreEquipment(p_targetSlot, unequippedItem);

            return EEquipmentChangeResult.Rejected;
        }

        // 장비를 해제하고 인벤토리의 사용 가능한 슬롯에 보관한다.
        public EEquipmentChangeResult RequestUnequip(EquipmentSlot p_slot)
        {
            EEquipmentChangeResult result = TryUnequip(p_slot, out ItemDTO item);

            return StoreUnequippedItem(result, item);
        }

        // 장비를 지정 인벤토리 슬롯에 보관하거나 해당 슬롯의 장비와 교환한다.
        public EEquipmentChangeResult RequestUnequip(EquipmentSlot p_slot, int p_targetInventorySlotIndex)
        {
            // 장비와 목표 인벤토리 슬롯을 함께 확인한다.
            if (p_slot == null || p_slot.IsEmpty ||
                !_inventoryContext.TryGetSlot(p_targetInventorySlotIndex, out InventorySlot targetSlot))
            {
                return EEquipmentChangeResult.Rejected;
            }

            ItemDTO item = p_slot.Item;

            // 빈 슬롯 또는 동일 Stack이면 장비를 일반 해제해 해당 슬롯에 보관한다.
            if (!_inventoryModule.CanAddItem(targetSlot, item, 1))
            {
                // 다른 장비가 한 개 들어 있다면 인벤토리 아이템과 현재 장비를 교환한다.
                if (targetSlot.Count == 1 && p_slot.CanEquip(targetSlot.Item))
                {
                    return SwapEquipment(targetSlot, p_slot, targetSlot.Item);
                }

                return EEquipmentChangeResult.Rejected;
            }

            EEquipmentChangeResult result = TryUnequip(p_slot, out ItemDTO unequippedItem);

            if (result != EEquipmentChangeResult.Unequipped || unequippedItem == null)
            {
                return result;
            }

            // 해제된 아이템을 지정 슬롯에 보관한다.
            if (_inventoryModule.AddItemToSlot(targetSlot, unequippedItem, 1) == 1)
            {
                return EEquipmentChangeResult.Unequipped;
            }

            // 보관 실패 시 아이템 손실을 막기 위해 원래 장비 슬롯에 되돌린다.
            RestoreEquipment(unequippedItem);

            return EEquipmentChangeResult.Rejected;
        }

        // 지정 무기 장비를 해제하고 인벤토리에 보관한다.
        public EEquipmentChangeResult RequestUnequipWeapon(EWeaponType p_weaponType)
        {
            EEquipmentChangeResult result = _equipmentModule.UnequipWeapon(p_weaponType, out WeaponDTO weapon);

            return StoreUnequippedItem(result, weapon);
        }

        // 지정 방어구 장비를 해제하고 인벤토리에 보관한다.
        public EEquipmentChangeResult RequestUnequipArmor(EArmorType p_armorType)
        {
            EEquipmentChangeResult result = _equipmentModule.UnequipArmor(p_armorType, out ArmorDTO armor);

            return StoreUnequippedItem(result, armor);
        }

        // 실제 슬롯 종류에 맞는 장비 해제 함수를 선택한다.
        private EEquipmentChangeResult TryUnequip(EquipmentSlot p_slot, out ItemDTO p_item)
        {
            p_item = null;

            // 실제 슬롯 형식에 맞는 Module 해제 함수를 호출하고 공통 ItemDTO로 변환한다.
            switch (p_slot)
            {
                case WeaponEquipmentSlot weaponSlot:
                    EEquipmentChangeResult weaponResult =
                        _equipmentModule.UnequipWeapon(weaponSlot.WeaponType, out WeaponDTO weapon);
                    p_item = weapon;
                    return weaponResult;

                case ArmorEquipmentSlot armorSlot:
                    EEquipmentChangeResult armorResult =
                        _equipmentModule.UnequipArmor(armorSlot.ArmorType, out ArmorDTO armor);
                    p_item = armor;
                    return armorResult;

                default:
                    return EEquipmentChangeResult.Rejected;
            }
        }

        // 해제된 장비를 인벤토리에 보관하고 실패하면 장착 상태를 복구한다.
        private EEquipmentChangeResult StoreUnequippedItem(EEquipmentChangeResult p_result, ItemDTO p_item)
        {
            if (p_result != EEquipmentChangeResult.Unequipped || p_item == null)
            {
                return p_result;
            }

            // 인벤토리 전체에서 보관 가능한 슬롯을 찾아 한 개를 추가한다.
            if (_inventoryModule.AddItem(p_item, 1) == 1)
                return EEquipmentChangeResult.Unequipped;

            // 빈 공간이 없으면 해제했던 아이템을 다시 장착한다.
            RestoreEquipment(p_item);
            return EEquipmentChangeResult.InventoryFull;
        }

        // 인벤토리 처리 실패 시 방금 수행한 장착을 취소한다.
        private void RollbackEquip(ItemDTO p_item)
        {
            switch (p_item)
            {
                case WeaponDTO weapon:
                    _equipmentModule.UnequipWeapon(weapon.WeaponType, out _);
                    break;

                case ArmorDTO armor:
                    _equipmentModule.UnequipArmor(armor.ArmorType, out _);
                    break;
            }
        }

        // 아이템 타입으로 원래 장비 슬롯을 찾아 복구한다.
        private void RestoreEquipment(ItemDTO p_item)
        {
            EEquipmentChangeResult rollbackResult = _equipmentModule.Equip(p_item);

            if (rollbackResult != EEquipmentChangeResult.Equipped)
            {
                Debug.LogError("장비 이동 실패 후 기존 장비를 복구하지 못했습니다.", this);
            }
        }

        // 교환 대상이었던 정확한 장비 슬롯에 기존 아이템을 복구한다.
        private void RestoreEquipment(EquipmentSlot p_targetSlot, ItemDTO p_item)
        {
            EEquipmentChangeResult rollbackResult = _equipmentModule.Equip(p_targetSlot, p_item);

            if (rollbackResult != EEquipmentChangeResult.Equipped)
            {
                Debug.LogError("장비 교환 실패 후 기존 장비를 복구하지 못했습니다.", this);
            }
        }
    }
}
