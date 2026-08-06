using UnityEngine;

namespace Alpha.Player.Equipment
{
    // EEquipmentChangeResult 관련 선택 값을 정의한다.
    public enum EEquipmentChangeResult
    {
        Rejected,
        Equipped,
        SlotOccupied,
        Unequipped,
        InventoryFull
    }

    // EquipmentModule 기능의 실제 처리를 담당한다.
    public class EquipmentModule : MonoBehaviour
    {
        private EquipmentContext _context;

        // 외부 의존성을 연결하고 사용할 수 있는 상태로 준비한다.
        public bool Bind(EquipmentContext p_context)
        {
            if (p_context == null)
            {
                Debug.LogError($"{nameof(EquipmentModule)} 참조가 설정되지 않았습니다.", this);
                return false;
            }

            _context = p_context;

            return true;
        }

        // 아이템 타입으로 장비 슬롯을 찾은 뒤 장착을 실행한다.
        public EEquipmentChangeResult Equip(ItemDTO p_item)
        {
            if (p_item == null || _context == null)
            {
                return EEquipmentChangeResult.Rejected;
            }

            // 무기·방어구 세부 타입에 대응하는 슬롯을 조회한다.
            if (!TryGetTargetSlot(p_item, out EquipmentSlot targetSlot))
            {
                return EEquipmentChangeResult.Rejected;
            }

            return Equip(targetSlot, p_item);
        }

        // 지정 슬롯의 타입과 점유 상태를 확인한 뒤 아이템을 장착한다.
        public EEquipmentChangeResult Equip(EquipmentSlot p_targetSlot, ItemDTO p_item)
        {
            // 슬롯이 아이템을 받을 수 없는 경우 상태를 변경하지 않는다.
            if (_context == null || p_targetSlot == null ||
                p_item == null || !p_targetSlot.CanEquip(p_item))
            {
                return EEquipmentChangeResult.Rejected;
            }

            // 기존 장비 교환은 Flow가 처리하므로 Module은 빈 슬롯만 허용한다.
            if (!p_targetSlot.IsEmpty)
            {
                return EEquipmentChangeResult.SlotOccupied;
            }

            return p_targetSlot.Equip(p_item)? EEquipmentChangeResult.Equipped : EEquipmentChangeResult.Rejected;
        }

        // UnequipWeapon 장비를 슬롯에서 해제해 반환한다.
        public EEquipmentChangeResult UnequipWeapon(EWeaponType p_weaponType, out WeaponDTO p_weapon)
        {
            p_weapon = null;

            if (_context == null || !_context.TryGetWeaponSlot(p_weaponType, out WeaponEquipmentSlot slot))
            {
                return EEquipmentChangeResult.Rejected;
            }

            p_weapon = slot.Unequip() as WeaponDTO;

            return p_weapon != null? EEquipmentChangeResult.Unequipped : EEquipmentChangeResult.Rejected;
        }

        // UnequipArmor 장비를 슬롯에서 해제해 반환한다.
        public EEquipmentChangeResult UnequipArmor(EArmorType p_armorType, out ArmorDTO p_armor)
        {
            p_armor = null;

            if (_context == null || !_context.TryGetArmorSlot(p_armorType, out ArmorEquipmentSlot slot))
            {
                return EEquipmentChangeResult.Rejected;
            }

            p_armor = slot.Unequip() as ArmorDTO;

            return p_armor != null? EEquipmentChangeResult.Unequipped : EEquipmentChangeResult.Rejected;
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


    }
}
