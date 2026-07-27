using Alpha.Slot;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Equipment
{
    // 장비 슬롯의 생성과 상태를 소유하는 컴포넌트
    public class EquipmentModule : MonoBehaviour
    {
        private readonly Dictionary<EWeaponType, EquipmentWeaponSlot> _weaponSlots = new();
        private readonly Dictionary<EArmorType, EquipmentArmorSlot> _armorSlots = new();

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            CreateWeaponSlots();
            CreateArmorSlots();

            IsInitialized = true;
        }

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
            _weaponSlots.Add(p_type, new EquipmentWeaponSlot(p_type));
        }

        private void AddArmorSlot(EArmorType p_type)
        {
            _armorSlots.Add(p_type, new EquipmentArmorSlot(p_type));
        }

        public bool TryGetWeaponSlot(EWeaponType p_type, out EquipmentWeaponSlot p_slot)
        {
            return _weaponSlots.TryGetValue(p_type, out p_slot);
        }

        public bool TryGetArmorSlot(EArmorType p_type, out EquipmentArmorSlot p_slot)
        {
            return _armorSlots.TryGetValue(p_type, out p_slot);
        }

        // 이 모듈이 소유한 장비 슬롯인지 확인한다.
        public bool ContainsSlot(SlotBase p_slot)
        {
            if (p_slot == null)
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
        #region ======================================== Slot Item Change
        // Inventory 슬롯과 Equipment 슬롯 사이의 이동 또는 교환
        public bool TrySwapSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!CanChangeSlotItem(p_source, p_target))
                return false;

            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            // 비어 있는 슬롯으로 전체 이동
            if (p_target.IsEmpty)
            {
                if (!p_target.TryReplace(sourceItem, sourceCount))
                    return false;

                p_source.Clear();
                return true;
            }

            ItemDTO targetItem = p_target.Item;
            int targetCount = p_target.Count;

            // 동일한 아이템의 교환은 상태 변화가 없다.
            if (p_source.IsSameItem(sourceItem, targetItem))
                return false;

            if (!p_source.CanStore(targetItem) || !p_target.CanStore(sourceItem))
            {
                return false;
            }

            if (!p_source.TryReplace(targetItem, targetCount))
                return false;

            if (p_target.TryReplace(sourceItem, sourceCount))
                return true;

            // Target 변경 실패 시 Source를 원래 상태로 복구한다.
            p_source.TryReplace(sourceItem, sourceCount);

            return false;
        }

        private bool CanChangeSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!IsInitialized || p_source == null ||
                p_target == null || p_source == p_target ||
                p_source.IsEmpty)
            {
                return false;
            }

            bool isSourceEquipmentSlot = ContainsSlot(p_source);
            bool isTargetEquipmentSlot = ContainsSlot(p_target);

            // 두 슬롯 중 정확히 하나만 Equipment 슬롯이어야 한다.
            return isSourceEquipmentSlot != isTargetEquipmentSlot;
        }
        #endregion ======================================== /Slot Item Change
    }
}