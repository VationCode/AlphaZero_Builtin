using System;
using System.Collections.Generic;
using Alpha.Slot;
using UnityEngine;

namespace Alpha.Equipment
{
    /// <summary>
    /// 무기와 방어구 Equipment Slot을 생성하고 보관한다.
    /// 장비 교환 실행과 UI 표현은 담당하지 않는다.
    /// </summary>
    public class EquipmentSlotModule : MonoBehaviour
    {
        private readonly Dictionary<EWeaponType, EquipmentWeaponSlot> _weaponSlots = new();

        private readonly Dictionary<EArmorType, EquipmentArmorSlot> _armorSlots = new();

        public bool IsInitialized { get; private set; }

        // 특정 무기 Slot의 장착 상태가 변경됐을 때 전달한다.
        public event Action<EWeaponType, WeaponDTO> OnEquippedWeaponChanged;

        // 특정 방어구 Slot의 장착 상태가 변경됐을 때 전달한다.
        public event Action<EArmorType, ArmorDTO> OnEquippedArmorChanged;

        public bool Initialize()
        {
            if (IsInitialized)
                return true;

            CreateWeaponSlots();
            CreateArmorSlots();

            IsInitialized = true;
            return true;
        }

        #region ============================== Slot Creation
        private void CreateWeaponSlots()
        {
            AddWeaponSlot(EWeaponType.Melee);
            AddWeaponSlot(EWeaponType.Range);
            AddWeaponSlot(EWeaponType.Special);
        }

        private void CreateArmorSlots()
        {
            AddArmorSlot(EArmorType.Helmet);
            AddArmorSlot(EArmorType.Chest);
            AddArmorSlot(EArmorType.Gloves);
            AddArmorSlot(EArmorType.Boots);
        }

        private void AddWeaponSlot(EWeaponType p_type)
        {
            if (_weaponSlots.ContainsKey(p_type))
                return;

            EquipmentWeaponSlot slot = new(p_type);

            // Slot의 아이템 변경을 무기 장착 상태 이벤트로 변환한다.
            slot.OnSlotChanged += (item, count) => HandleWeaponSlotChanged(p_type, item);

            _weaponSlots.Add(p_type, slot);
        }

        private void AddArmorSlot(EArmorType p_type)
        {
            if (_armorSlots.ContainsKey(p_type))
                return;

            EquipmentArmorSlot slot = new(p_type);

            // Slot의 아이템 변경을 방어구 장착 상태 이벤트로 변환한다.
            slot.OnSlotChanged += (item, count) => HandleArmorSlotChanged(p_type, item);

            _armorSlots.Add(p_type, slot);
        }
        #endregion ============================== /Slot Creation

        #region ============================== Slot Lookup
        public bool TryGetWeaponSlot(EWeaponType p_type, out EquipmentWeaponSlot p_slot)
        {
            p_slot = null;

            return IsInitialized && _weaponSlots.TryGetValue(p_type, out p_slot);
        }

        public bool TryGetArmorSlot(EArmorType p_type, out EquipmentArmorSlot p_slot)
        {
            p_slot = null;

            return IsInitialized && _armorSlots.TryGetValue(p_type, out p_slot);
        }

        public bool TryGetEquippedWeapon(EWeaponType p_type, out WeaponDTO p_weapon)
        {
            p_weapon = null;

            if (!TryGetWeaponSlot(p_type, out EquipmentWeaponSlot slot))
            {
                return false;
            }

            p_weapon = slot.Item as WeaponDTO;

            return p_weapon != null;
        }

        /// <summary>
        /// 전달된 Slot이 현재 Equipment가 소유한 Slot인지 검사한다.
        /// </summary>
        internal bool ContainsSlot(SlotBase p_slot)
        {
            if (!IsInitialized || p_slot == null)
                return false;

            foreach (EquipmentWeaponSlot slot in _weaponSlots.Values)
            {
                if (ReferenceEquals(slot, p_slot))
                    return true;
            }

            foreach (EquipmentArmorSlot slot in _armorSlots.Values)
            {
                if (ReferenceEquals(slot, p_slot))
                    return true;
            }

            return false;
        }
        #endregion ============================== /Slot Lookup

        #region ============================== Slot Changed
        private void HandleWeaponSlotChanged(EWeaponType p_type, ItemDTO p_item)
        {
            OnEquippedWeaponChanged?.Invoke(p_type, p_item as WeaponDTO);
        }

        private void HandleArmorSlotChanged(EArmorType p_type, ItemDTO p_item)
        {
            OnEquippedArmorChanged?.Invoke(p_type, p_item as ArmorDTO);
        }
        #endregion ============================== /Slot Changed
    }
}
