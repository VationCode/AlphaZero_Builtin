using System;
using Alpha.Slot;
using UnityEngine;

namespace Alpha.Equipment
{
    /// <summary>
    /// Equipment 내부 기능을 하나의 진입점으로 조합한다.
    /// 외부에서는 SlotModule과 ItemModule을 직접 사용하지 않는다.
    /// </summary>
    [RequireComponent(typeof(EquipmentSlotModule), typeof(EquipmentItemModule))]
    public class EquipmentModule : MonoBehaviour
    {
        private EquipmentSlotModule _slotModule;
        private EquipmentItemModule _itemModule;

        public bool IsInitialized { get; private set; }

        // 특정 무기 슬롯의 장착 아이템이 변경됐음을 전달한다.
        public event Action<EWeaponType, WeaponDTO> OnEquippedWeaponChanged;
        // 특정 방어구 슬롯의 장착 아이템 변경을 전달한다.
        public event Action<EArmorType, ArmorDTO> OnEquippedArmorChanged;

        private void Awake()
        {
            // Equipment의 세부 기능은 동일 GameObject에서 조립한다.
            _slotModule = GetComponent<EquipmentSlotModule>();
            _itemModule = GetComponent<EquipmentItemModule>();
        }
        /// <summary>
        /// Slot을 먼저 생성하고 아이템 교환 기능을 연결한다.
        /// </summary>
        public bool Initialize()
        {
            if (IsInitialized)
                return true;

            if (_slotModule == null || _itemModule == null)
            {
                Debug.LogError($"{nameof(EquipmentModule)}의 " + "내부 Module이 설정되지 않았습니다.", this);
                return false;
            }

            if (!_slotModule.Initialize())
                return false;

            if (!_itemModule.Bind(_slotModule))
                return false;

            BindSlotEvents();

            IsInitialized = true;
            return true;
        }

        private void BindSlotEvents()
        {
            // 중복 구독을 방지한 뒤 Equipment 외부 이벤트로 연결한다.
            _slotModule.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
            _slotModule.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;

            _slotModule.OnEquippedArmorChanged -= HandleEquippedArmorChanged;
            _slotModule.OnEquippedArmorChanged += HandleEquippedArmorChanged;
        }

        #region ============================== Slot Lookup
        public bool TryGetWeaponSlot(EWeaponType p_type, out EquipmentWeaponSlot p_slot)
        {
            p_slot = null;

            return IsInitialized && _slotModule.TryGetWeaponSlot(p_type, out p_slot);
        }

        public bool TryGetArmorSlot(EArmorType p_type, out EquipmentArmorSlot p_slot)
        {
            p_slot = null;

            return IsInitialized && _slotModule.TryGetArmorSlot(p_type, out p_slot);
        }

        public bool TryGetEquippedWeapon(EWeaponType p_type, out WeaponDTO p_weapon)
        {
            p_weapon = null;

            return IsInitialized && _slotModule.TryGetEquippedWeapon(p_type, out p_weapon);
        }

        #endregion ============================== /Slot Lookup

        #region ============================== Equipment Item Change
        public bool TrySwapSlotItem(SlotBase p_source, SlotBase p_target)
        {
            return IsInitialized && _itemModule.TrySwapSlotItem(p_source, p_target);
        }
        #endregion ============================== /Equipment Item Change

        #region ============================== Slot Event
        private void HandleEquippedWeaponChanged(EWeaponType p_type, WeaponDTO p_weapon)
        {
            OnEquippedWeaponChanged?.Invoke(p_type, p_weapon);
        }

        private void HandleEquippedArmorChanged(EArmorType p_type, ArmorDTO p_armor)
        {
            OnEquippedArmorChanged?.Invoke(p_type, p_armor);
        }
        #endregion ============================== /Slot Event
        private void OnDestroy()
        {
            if (_slotModule != null)
            {
                _slotModule.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
                _slotModule.OnEquippedArmorChanged -= HandleEquippedArmorChanged;
            }

            OnEquippedWeaponChanged = null;
            OnEquippedArmorChanged = null;
        }
    }
}